<#
.SYNOPSIS
    Uploads a release zip to the CurseForge project for Simple Location Display.

.DESCRIPTION
    The GitHub workflow in .github/workflows/curseforge.yml runs this when a release is published.
    You can also run it by hand.

    The upload token comes from the CURSEFORGE_API_TOKEN environment variable and is never written
    to the repo. It is handed to curl through a temporary header file, so it doesn't show up in the
    process list. Create a token under your CurseForge account's API tokens page.

.PARAMETER Zip
    The release zip to upload.

.PARAMETER Version
    The version being released, such as 2.0.0 or 2.1.0-beta.1. The file is shown on CurseForge as
    "SimpleLocationDisplay <version>", which is the form SMAPI reads for update checks.

.PARAMETER NotesFile
    A markdown file with the release notes.

.PARAMETER GameVersion
    The Stardew Valley version to list on CurseForge.

.PARAMETER Endpoint
    The upload site. Only changed for testing.

.PARAMETER DryRun
    Check the inputs and show what would be sent, without uploading.

.EXAMPLE
    ./tools/upload-curseforge.ps1 -Zip "SimpleLocationDisplay 2.0.0.zip" -Version 2.0.0 -NotesFile notes.md -DryRun
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Zip,
    [Parameter(Mandatory)][string]$Version,
    [string]$NotesFile,
    [string]$GameVersion = '1.6.15',
    [string]$Endpoint = 'https://stardewvalley.curseforge.com',
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

$projectId = 1613049

if ($Version -notmatch '^\d+\.\d+\.\d+(-(alpha|beta|rc)\.\d+)?$') {
    throw "Version '$Version' isn't MAJOR.MINOR.PATCH with an optional -alpha.N, -beta.N or -rc.N suffix."
}
if (-not (Test-Path -LiteralPath $Zip)) { throw "The zip doesn't exist: $Zip" }

$releaseType = 'release'
if ($Version -match '-alpha\.') { $releaseType = 'alpha' }
elseif ($Version -match '-(beta|rc)\.') { $releaseType = 'beta' }

$notes = ''
if ($NotesFile) {
    if (-not (Test-Path -LiteralPath $NotesFile)) { throw "The notes file doesn't exist: $NotesFile" }
    $notes = (Get-Content -LiteralPath $NotesFile -Raw -Encoding UTF8).Trim()
}

$token = $env:CURSEFORGE_API_TOKEN
$url = "$Endpoint/api/projects/$projectId/upload-file"
$displayName = "SimpleLocationDisplay $Version"

Write-Host "File:         $displayName ($releaseType)"
Write-Host "Game version: $GameVersion"
Write-Host "Notes:        $(if ($notes) { "$($notes.Split("`n").Count) lines" } else { 'none' })"
Write-Host "Token:        $(if ($token) { 'set' } else { 'NOT SET' })"
Write-Host "Target:       $url"

if ($DryRun) {
    Write-Host 'Dry run: nothing was uploaded.'
    return
}
if (-not $token) { throw 'CURSEFORGE_API_TOKEN is not set.' }

$isWindowsHost = ($PSVersionTable.PSVersion.Major -lt 6) -or $IsWindows
$curl = if ($isWindowsHost) { 'curl.exe' } else { 'curl' }

$work = Join-Path ([IO.Path]::GetTempPath()) ("sld-curseforge-" + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work | Out-Null
try {
    # CurseForge names the file after the upload, so give it the same name as the display name
    $uploadZip = Join-Path $work "$displayName.zip"
    Copy-Item -LiteralPath $Zip -Destination $uploadZip

    $metadata = @{
        changelog        = $notes
        changelogType    = 'markdown'
        displayName      = $displayName
        gameVersionNames = @($GameVersion)
        releaseType      = $releaseType
    } | ConvertTo-Json -Compress
    $utf8 = New-Object Text.UTF8Encoding($false)
    $metadataFile = Join-Path $work 'metadata.json'
    $headerFile = Join-Path $work 'header.txt'
    [IO.File]::WriteAllText($metadataFile, $metadata, $utf8)
    [IO.File]::WriteAllText($headerFile, "X-Api-Token: $token", $utf8)

    $response = & $curl --silent --show-error --fail-with-body `
        --header "@$headerFile" `
        --form "metadata=<$metadataFile;type=application/json" `
        --form "file=@$uploadZip;type=application/zip" `
        $url
    if ($LASTEXITCODE -ne 0) { throw "The CurseForge upload failed (curl exit code $LASTEXITCODE): $response" }

    Write-Host "CurseForge accepted the file: $response"
}
finally {
    Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue
}
