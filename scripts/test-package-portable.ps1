$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$package = Join-Path $PSScriptRoot 'package-portable.ps1'
$testRoot = Join-Path ([IO.Path]::GetTempPath()) ('MorChranslator-package-test-' + [guid]::NewGuid().ToString('N'))
$source = Join-Path $testRoot 'arbitrary-version-folder'
New-Item -ItemType Directory -Path (Join-Path $source 'wwwroot/assets') -Force | Out-Null
function Assert([bool]$Condition, [string]$Message) { if (!$Condition) { throw $Message } }
function Assert-Throws([scriptblock]$Action, [string]$Message) {
    $failed = $false
    try { & $Action } catch { $failed = $true }
    Assert $failed $Message
}
try {
    [IO.File]::WriteAllText((Join-Path $source 'MorChranslator.exe'), 'fixture only')
    [IO.File]::WriteAllText((Join-Path $source 'wwwroot/assets/中文.txt'), 'portable resource')
    [IO.File]::WriteAllBytes((Join-Path $source 'empty.txt'), [byte[]]@())
    [IO.File]::WriteAllText((Join-Path $source 'hidden.txt'), 'hidden resource')
    (Get-Item -LiteralPath (Join-Path $source 'hidden.txt')).Attributes = [IO.FileAttributes]::Hidden
    $zip = Join-Path $testRoot 'MorChranslator-test-win-x64.zip'
    & $package -SourceDirectory $source -DestinationZip $zip
    $archive = [IO.Compression.ZipFile]::OpenRead($zip)
    try {
        $names = @($archive.Entries | ForEach-Object FullName)
        Assert ($names.Count -eq 4) 'Expected exact file coverage, including hidden and empty files.'
        Assert (@($names | Where-Object { !$_.StartsWith('MorChranslator/') -or $_.Contains('\') -or $_.Contains('../') }).Count -eq 0) 'Invalid ZIP root or path.'
        Assert ($names -contains 'MorChranslator/MorChranslator.exe') 'Missing executable under the product folder.'
        Assert ($names -contains 'MorChranslator/wwwroot/assets/中文.txt') 'Unicode resource missing.'
    } finally { $archive.Dispose() }
    'PASS exact single product folder, executable, nested/Unicode/empty/hidden file coverage'
    $extracted = Join-Path $testRoot 'extracted'
    [IO.Compression.ZipFile]::ExtractToDirectory($zip, $extracted)
    foreach ($file in Get-ChildItem -LiteralPath $source -File -Force -Recurse) {
        $relative = [IO.Path]::GetRelativePath($source, $file.FullName)
        Assert ((Get-FileHash -LiteralPath $file.FullName).Hash -eq (Get-FileHash -LiteralPath (Join-Path "$extracted/MorChranslator" $relative)).Hash) 'Extracted file hash differs.'
    }
    'PASS extracted hashes'
    $original = (Get-FileHash -LiteralPath $zip).Hash
    Assert-Throws { & $package $source $zip } 'Existing ZIP was not rejected.'
    Assert ((Get-FileHash -LiteralPath $zip).Hash -eq $original) 'Existing ZIP changed.'
    'PASS existing ZIP preserved'
    Assert-Throws { & $package "$testRoot/missing" "$testRoot/missing.zip" } 'Missing source was not rejected.'
    Assert-Throws { & $package $source "$source/inside.zip" } 'ZIP inside source was not rejected.'
    New-Item -ItemType Directory -Path "$testRoot/empty" | Out-Null
    Assert-Throws { & $package "$testRoot/empty" "$testRoot/empty.zip" } 'Empty package was not rejected.'
    'PASS missing/recursive/empty input rejected'
    Assert (@(Get-ChildItem -LiteralPath $testRoot -Filter '*.partial-*').Count -eq 0) 'Temporary ZIP leaked.'
    'PASS portable packaging regression suite'
} finally {
    # Only remove the unique directory created by this test, after verifying its parent and name.
    if ([IO.Path]::GetDirectoryName($testRoot.TrimEnd('\')) -eq [IO.Path]::GetTempPath().TrimEnd('\') -and
        [IO.Path]::GetFileName($testRoot) -like 'MorChranslator-package-test-*') {
        Remove-Item -LiteralPath $testRoot -Recurse -Force
    }
}
