#requires -Version 5.1
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$executable = Join-Path $PSScriptRoot 'DesktopTranslator.exe'
if (!(Test-Path -LiteralPath $executable -PathType Leaf)) {
    throw '请在诊断便携包目录中运行此脚本，保持它与 DesktopTranslator.exe 放在一起。'
}

$identity = [Environment]::UserDomainName + '-' + [Environment]::UserName
$launcherMutex = [Threading.Mutex]::new($false, ('Local\DesktopTranslator.DiagnosticsLauncher-' + $identity))
$ownsLauncher = $false
$app = $null
$movedLogs = @()
$logIsolationReady = $false
$logsRoot = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::ApplicationData)) 'Kikitan Translator\logs'
$session = [DateTime]::Now.ToString('yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N')
$preserved = Join-Path $logsRoot ('diagnostic-preserved\' + $session)
$sessionLogs = Join-Path $logsRoot ('diagnostic-sessions\' + $session)

try {
    try { $ownsLauncher = $launcherMutex.WaitOne(0) }
    catch [Threading.AbandonedMutexException] { $ownsLauncher = $true }
    if (!$ownsLauncher) { throw '已有诊断启动器在运行。请先从托盘正常退出应用，再运行本脚本。' }

    # Use the same identity as the application's existing single-instance mutex.
    $existing = $null
    try { $existing = [Threading.Mutex]::OpenExisting('Local\DesktopTranslator.Main-' + $identity) }
    catch [Threading.WaitHandleCannotBeOpenedException] { }
    if ($existing) {
        try {
            $available = $false
            try { $available = $existing.WaitOne(0) }
            catch [Threading.AbandonedMutexException] { $available = $true }
            if ($available) { $existing.ReleaseMutex() }
            else { throw 'Desktop Translator 已在运行。请从托盘选择退出，而不是仅关闭设置窗口，再启动诊断。' }
        } finally { $existing.Dispose() }
    }

    # The pinned application's logger rotates root-level *.log files. Temporarily move
    # existing logs out of its retention scope; never delete or overwrite their contents.
    [IO.Directory]::CreateDirectory($preserved) | Out-Null
    [IO.Directory]::CreateDirectory($sessionLogs) | Out-Null
    foreach ($file in Get-ChildItem -LiteralPath $logsRoot -Filter '*.log' -File) {
        $target = Join-Path $preserved $file.Name
        if (Test-Path -LiteralPath $target) { throw '日志保护目录发生冲突，未覆盖任何文件。' }
        Move-Item -LiteralPath $file.FullName -Destination $target
        $movedLogs += $file.Name
    }
    $logIsolationReady = $true

    $startInfo = [Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $executable
    $startInfo.WorkingDirectory = $PSScriptRoot
    $startInfo.UseShellExecute = $false
    $startInfo.Arguments = '--start-stopped --settings'
    # This dictionary belongs to this child only; never change $env or persistent variables.
    $startInfo.EnvironmentVariables['KIKITAN_LATENCY_DIAGNOSTICS'] = '1'
    Write-Host '已启用本次应用进程的延迟诊断。不会自动开始录音；请在设置中检查后点击开始。'
    Write-Host '请保留此 PowerShell 窗口，测试结束后从应用托盘选择退出，等待旧日志归位。'
    $app = [Diagnostics.Process]::Start($startInfo)
    $app.WaitForExit()
    if ($app.ExitCode -ne 0) { throw "应用退出码为 $($app.ExitCode)，请仅导出诊断记录排查。" }
} finally {
    try {
        if ($ownsLauncher) {
            if (!$app -or $app.HasExited) {
                # Even if archiving fails, always attempt restoration of protected old logs.
                try {
                    if ($logIsolationReady) {
                        foreach ($file in Get-ChildItem -LiteralPath $logsRoot -Filter '*.log' -File) {
                            $target = Join-Path $sessionLogs $file.Name
                            if (Test-Path -LiteralPath $target) { throw '诊断日志归档路径冲突；所有文件均保留，未覆盖。' }
                            Move-Item -LiteralPath $file.FullName -Destination $target
                        }
                    }
                } finally {
                    foreach ($name in $movedLogs) {
                        $target = Join-Path $logsRoot $name
                        if (Test-Path -LiteralPath $target) {
                            Write-Warning '旧日志原路径已存在；旧文件仍完整保留在 diagnostic-preserved 子目录，请勿覆盖。'
                        } else {
                            try { Move-Item -LiteralPath (Join-Path $preserved $name) -Destination $target }
                            catch { Write-Warning '旧日志尚未归位，完整文件保留在 diagnostic-preserved 子目录，请按指南恢复。' }
                        }
                    }
                }
                if (Test-Path -LiteralPath $sessionLogs) { Write-Host "本次诊断日志目录：$sessionLogs" }
            } else {
                Write-Warning '应用仍在运行。旧日志完整保留在 logs\diagnostic-preserved；请正常退出后按测试指南恢复，不要覆盖文件。'
            }
        }
    } finally {
        if ($ownsLauncher) { $launcherMutex.ReleaseMutex() }
        if ($app) { $app.Dispose() }
        $launcherMutex.Dispose()
    }
}
