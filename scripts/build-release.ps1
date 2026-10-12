[CmdletBinding()]
param(
    [string]$Version = '2.0.2-alpha.1',
    [switch]$SkipInstall
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if ($Version -notmatch '^\d+\.\d+\.\d+(-[a-zA-Z0-9.-]+)?$') { throw 'Use a semantic version such as 2.0.2-alpha.1.' }
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$releaseRoot = Join-Path $repoRoot 'release'
$destination = Join-Path $releaseRoot "MorChranslator-$Version-win-x64"
if (Test-Path -LiteralPath $destination) { throw "Release already exists: $destination. Choose a new version or move it before rebuilding." }
$zipPath = "$destination.zip"
if ((Test-Path -LiteralPath $zipPath) -or (Test-Path -LiteralPath "$zipPath.sha256")) { throw 'ZIP/checksum already exists. Choose a new version.' }
$staging = Join-Path $releaseRoot ('.staging-' + [guid]::NewGuid().ToString('N'))
function Run([string]$Program, [string[]]$Arguments) {
    & $Program @Arguments | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "$Program failed with exit code $LASTEXITCODE" }
}
Push-Location $repoRoot
try {
    & (Join-Path $PSScriptRoot 'test-package-portable.ps1')
    $frontend = Join-Path $repoRoot 'MorChranslator.Photino/UserInterface'
    if (!$SkipInstall) { Run npm @('ci', '--prefix', $frontend, '--no-audit', '--no-fund') }
    Run npm @('test', '--prefix', $frontend)
    Run npm @('run', 'build', '--prefix', $frontend)
    Run dotnet @('build', 'MorChranslator.sln', '--nologo', '-m:1', '-p:UseSharedCompilation=false')
    Run dotnet @('run', '--project', 'tests/MorChranslator.Tests', '--no-build', '--no-restore')
    New-Item -ItemType Directory -Path $staging -Force | Out-Null
    Run dotnet @('publish', 'MorChranslator.Photino/MorChranslator.Photino.csproj', '-c', 'Release', '-r', 'win-x64',
        '--self-contained', 'true', '-o', $staging, '-m:1', '-p:PublishSingleFile=true', '-p:IncludeNativeLibrariesForSelfExtract=true',
        '-p:DebugType=None', '-p:SkipVelopack=true', '-p:SkipNpmBuild=true', '-p:UseSharedCompilation=false', "-p:Version=$Version")
    foreach ($required in @('MorChranslator.exe', 'MorChranslator.Subtitles.exe', 'wwwroot/index.html', 'wwwroot/silero_vad.onnx')) {
        if (!(Test-Path -LiteralPath (Join-Path $staging $required))) { throw "Missing release component: $required" }
    }
    # WinExe tests must be explicitly awaited; do not let a hung UI test block a release indefinitely.
    $testOutput = Join-Path $staging 'subtitle-test.stdout'
    $testError = Join-Path $staging 'subtitle-test.stderr'
    $testProcess = Start-Process -FilePath (Join-Path $staging 'MorChranslator.Subtitles.exe') -ArgumentList '--self-test' -WindowStyle Hidden -PassThru -RedirectStandardOutput $testOutput -RedirectStandardError $testError
    try {
        if (!$testProcess.WaitForExit(30000)) {
            $testProcess.Kill()
            throw 'Published subtitle self-test exceeded 30 seconds.'
        }
        Get-Content -LiteralPath $testOutput | Out-Host
        Get-Content -LiteralPath $testError | Out-Host
        if ($testProcess.ExitCode -ne 0) { throw "Published subtitle self-test failed: $($testProcess.ExitCode)" }
    } finally {
        $testProcess.Dispose()
        Remove-Item -LiteralPath $testOutput, $testError -ErrorAction SilentlyContinue
    }
    # Compare every generated frontend file, not just index.html: this catches stale JS/CSS in packages.
    $dist = Join-Path $frontend 'dist'
    foreach ($asset in Get-ChildItem -LiteralPath $dist -File -Recurse) {
        $relative = [IO.Path]::GetRelativePath($dist, $asset.FullName)
        $packaged = Join-Path (Join-Path $staging 'wwwroot') $relative
        if (!(Test-Path -LiteralPath $packaged) -or
            (Get-FileHash -LiteralPath $asset.FullName).Hash -ne (Get-FileHash -LiteralPath $packaged).Hash) {
            throw "Packaged frontend does not match current build: $relative"
        }
    }
    Copy-Item -LiteralPath (Join-Path $repoRoot 'LICENSE.md') -Destination $staging
    Copy-Item -LiteralPath (Join-Path $repoRoot 'README.md') -Destination $staging
    Copy-Item -LiteralPath (Join-Path $repoRoot 'docs/PORTABLE_RELEASE_NOTES.md') -Destination (Join-Path $staging 'RELEASE_NOTES.md')
    Copy-Item -LiteralPath (Join-Path $repoRoot 'scripts/Start-LatencyDiagnostics.ps1') -Destination $staging
    Copy-Item -LiteralPath (Join-Path $repoRoot 'docs/DIAGNOSTIC_TESTING_ZH.md') -Destination $staging
    $packageDocs = Join-Path $staging 'docs'
    New-Item -ItemType Directory -Path $packageDocs -Force | Out-Null
    foreach ($document in @('ENGINEERING_AUDIT.md', 'MANUAL_TESTS.md', 'UI_DESIGN.md', 'PERFORMANCE.md', 'RENAMING.md', 'LATENCY_DIAGNOSTICS.md', 'SENTENCE_LATENCY.md')) {
        Copy-Item -LiteralPath (Join-Path $repoRoot "docs/$document") -Destination $packageDocs
    }
    & (Join-Path $PSScriptRoot 'verify-release.ps1') -Path $staging
    $hashes = Get-ChildItem -LiteralPath $staging -File -Recurse | Sort-Object FullName | ForEach-Object {
        $hash = Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256
        $relative = [IO.Path]::GetRelativePath($staging, $_.FullName).Replace('\', '/')
        "$($hash.Hash.ToLowerInvariant())  $relative"
    }
    $hashes | Set-Content -LiteralPath (Join-Path $staging 'SHA256SUMS.txt') -Encoding utf8
    Move-Item -LiteralPath $staging -Destination $destination
    & (Join-Path $PSScriptRoot 'package-portable.ps1') -SourceDirectory $destination -DestinationZip $zipPath
    $zipHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
    "$zipHash  $([IO.Path]::GetFileName($zipPath))" | Set-Content -LiteralPath "$zipPath.sha256" -Encoding utf8
    Write-Host "Release ready: $destination"
    Write-Host "Portable ZIP: $zipPath"
} finally { Pop-Location }
