[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$SourceDirectory,
    [Parameter(Mandatory)][string]$DestinationZip
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$source = (Resolve-Path -LiteralPath $SourceDirectory).Path
if (!(Test-Path -LiteralPath $source -PathType Container)) { throw 'Source must be a directory.' }
$destination = [IO.Path]::GetFullPath($DestinationZip)
if (Test-Path -LiteralPath $destination) { throw 'ZIP already exists; choose a new version.' }
if ($destination.StartsWith($source.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'ZIP must be outside the source directory.'
}
$items = @(Get-Item -LiteralPath $source) + @(Get-ChildItem -LiteralPath $source -Force -Recurse)
if ($items | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }) {
    throw 'Portable packages cannot contain filesystem links.'
}
$files = @($items | Where-Object { !$_.PSIsContainer } | Sort-Object FullName)
if (!$files.Count) { throw 'Portable package is empty.' }
# Build beside the destination and rename only a complete archive. Never replace an existing ZIP.
$temporary = $destination + '.partial-' + [guid]::NewGuid().ToString('N')
try {
    $archive = [IO.Compression.ZipFile]::Open($temporary, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($file in $files) {
            $relative = [IO.Path]::GetRelativePath($source, $file.FullName).Replace('\', '/')
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $archive, $file.FullName, "MorChranslator/$relative", [IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    } finally { $archive.Dispose() }
    [IO.File]::Move($temporary, $destination)
} finally {
    if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary }
}
Write-Host "Portable ZIP created with MorChranslator/ root: $destination"
