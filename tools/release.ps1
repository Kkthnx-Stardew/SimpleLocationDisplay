<#
.SYNOPSIS
    Builds, tests and publishes a release of Simple Location Display.

.DESCRIPTION
    Without -Publish this is a dry run: it checks the repo, builds, runs the tests and says
    what it would do, without tagging, pushing or uploading anything.

    With -Publish it then:
      1. tags the commit as v<version> and pushes the branch and the tag,
      2. creates a GitHub release with the zip attached and the changelog section as notes,
      3. uploads the zip to CurseForge, if CURSEFORGE_API_TOKEN is set.

    The version comes from <Version> in SimpleLocationDisplay/SimpleLocationDisplay.csproj.
    Before releasing, change the heading for that version in CHANGELOG.md from "(not released)"
    to the release date.

    Needs the .NET SDK, git, and the GitHub CLI (gh) signed in. The CurseForge token is read
    from the environment and is never written to disk or to the repo. Create one under your
    CurseForge account's API tokens page.

.PARAMETER Publish
    Actually tag, push, create the GitHub release and upload. Leave it off for a dry run.

.PARAMETER GameVersion
    The Stardew Valley version to list on CurseForge.

.EXAMPLE
    ./tools/release.ps1
    ./tools/release.ps1 -Publish
#>
[CmdletBinding()]
param(
    [switch]$Publish,
    [string]$GameVersion = '1.6.15'
)

$ErrorActionPreference = 'Stop'

$CurseForgeProjectId = 1613049
$CurseForgeHost = 'https://stardewvalley.curseforge.com'

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

function Step($text) { Write-Host "`n== $text" -ForegroundColor Cyan }
function Fail($text) { throw $text }
function Run([string]$exe, [string[]]$arguments) {
    & $exe @arguments
    if ($LASTEXITCODE -ne 0) { Fail "'$exe $($arguments -join ' ')' failed with exit code $LASTEXITCODE." }
}

# --- Read the version ------------------------------------------------------------------------
Step 'Reading the version'
$csprojPath = Join-Path $root 'SimpleLocationDisplay/SimpleLocationDisplay.csproj'
[xml]$csproj = Get-Content -LiteralPath $csprojPath -Raw
$version = ($csproj.Project.PropertyGroup | ForEach-Object { $_.Version } | Where-Object { $_ } | Select-Object -First 1)
if (-not $version) { Fail "No <Version> found in $csprojPath." }
if ($version -notmatch '^\d+\.\d+\.\d+(-(alpha|beta|rc)\.\d+)?$') {
    Fail "Version '$version' isn't MAJOR.MINOR.PATCH with an optional -alpha.N, -beta.N or -rc.N suffix."
}
$tag = "v$version"
Write-Host "Version $version, tag $tag"

$releaseType = 'release'
if ($version -match '-alpha\.') { $releaseType = 'alpha' }
elseif ($version -match '-(beta|rc)\.') { $releaseType = 'beta' }
$isPrerelease = $releaseType -ne 'release'

# --- Check the repo --------------------------------------------------------------------------
Step 'Checking the repo'
$branch = (git branch --show-current).Trim()
if ($branch -ne 'main') { Fail "Release from main. You are on '$branch'." }

$dirty = git status --porcelain
if ($dirty) { Fail "The working tree has uncommitted changes. Commit or stash them first.`n$($dirty -join "`n")" }

if (git tag --list $tag) { Fail "Tag $tag already exists. A released version is never reused; bump the version." }
$remoteTag = git ls-remote --tags origin "refs/tags/$tag"
if ($remoteTag) { Fail "Tag $tag already exists on origin. A released version is never reused; bump the version." }

# --- Changelog section -----------------------------------------------------------------------
Step 'Reading the changelog'
$changelog = Get-Content -LiteralPath (Join-Path $root 'CHANGELOG.md') -Raw
$pattern = '(?ms)^## ' + [regex]::Escape($version) + '(?<heading>[^\r\n]*)\r?\n(?<body>.*?)(?=^## |\z)'
$match = [regex]::Match($changelog, $pattern)
if (-not $match.Success) { Fail "CHANGELOG.md has no '## $version' section." }
if ($match.Groups['heading'].Value -match 'not released') {
    Fail "CHANGELOG.md still says '$version' is not released. Put the release date in that heading first."
}
$notes = $match.Groups['body'].Value.Trim()
if (-not $notes) { Fail "The '## $version' section of CHANGELOG.md is empty." }
Write-Host "Found $($notes.Split("`n").Count) lines of release notes."

# --- Build and test --------------------------------------------------------------------------
Step 'Building'
Run 'dotnet' @('build', 'SimpleLocationDisplay.sln', '-c', 'Release', '--nologo', '-v', 'q')

Step 'Testing'
Run 'dotnet' @('test', 'SimpleLocationDisplay.sln', '-c', 'Release', '--no-build', '--nologo')

$zip = Join-Path $root "SimpleLocationDisplay/bin/Release/net6.0/SimpleLocationDisplay $version.zip"
if (-not (Test-Path -LiteralPath $zip)) { Fail "The release zip wasn't built: $zip" }

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead($zip)
try {
    $manifestEntry = $archive.Entries | Where-Object { $_.FullName -eq 'SimpleLocationDisplay/manifest.json' }
    if (-not $manifestEntry) { Fail 'The zip has no SimpleLocationDisplay/manifest.json.' }
    $reader = New-Object IO.StreamReader($manifestEntry.Open())
    try { $zipManifest = $reader.ReadToEnd() | ConvertFrom-Json } finally { $reader.Dispose() }
}
finally { $archive.Dispose() }
if ($zipManifest.Version -ne $version) { Fail "The manifest in the zip says $($zipManifest.Version), not $version." }
Write-Host "Zip OK: $([IO.Path]::GetFileName($zip)) ($([math]::Round((Get-Item -LiteralPath $zip).Length / 1KB, 1)) KB)"

# --- Publish ---------------------------------------------------------------------------------
$hasToken = [bool]$env:CURSEFORGE_API_TOKEN
Step ($(if ($Publish) { 'Publishing' } else { 'Dry run: would publish' }))
Write-Host "  tag and push:     $tag on main"
Write-Host "  GitHub release:   $tag with the zip$(if ($isPrerelease) { ' (marked pre-release)' })"
Write-Host "  CurseForge:       $(if ($hasToken) { "$releaseType for Stardew Valley $GameVersion" } else { 'skipped, CURSEFORGE_API_TOKEN is not set' })"

if (-not $Publish) {
    Write-Host "`nDry run finished. Run again with -Publish to do it." -ForegroundColor Green
    return
}

$notesFile = Join-Path ([IO.Path]::GetTempPath()) "sld-release-notes-$version.md"
Set-Content -LiteralPath $notesFile -Value $notes -Encoding UTF8

try {
    Step 'Tagging and pushing'
    Run 'git' @('tag', '-a', $tag, '-m', "Simple Location Display $version")
    Run 'git' @('push', 'origin', 'main')
    Run 'git' @('push', 'origin', $tag)

    Step 'Creating the GitHub release'
    $ghArgs = @('release', 'create', $tag, $zip, '--title', "Simple Location Display $version", '--notes-file', $notesFile)
    if ($isPrerelease) { $ghArgs += '--prerelease' }
    Run 'gh' $ghArgs

    if ($hasToken) {
        Step 'Uploading to CurseForge'
        $metadata = @{
            changelog        = $notes
            changelogType    = 'markdown'
            displayName      = "SimpleLocationDisplay $version"
            gameVersionNames = @($GameVersion)
            releaseType      = $releaseType
        } | ConvertTo-Json -Compress
        $metadataFile = Join-Path ([IO.Path]::GetTempPath()) "sld-curseforge-metadata-$version.json"
        $headerFile = Join-Path ([IO.Path]::GetTempPath()) "sld-curseforge-header-$version.txt"
        try {
            [IO.File]::WriteAllText($metadataFile, $metadata, (New-Object Text.UTF8Encoding($false)))
            # The token goes in a header file so it doesn't show up in the process list.
            [IO.File]::WriteAllText($headerFile, "X-Api-Token: $($env:CURSEFORGE_API_TOKEN)", (New-Object Text.UTF8Encoding($false)))
            $response = & curl.exe --silent --show-error --fail-with-body `
                --header "@$headerFile" `
                --form "metadata=<$metadataFile;type=application/json" `
                --form "file=@$zip" `
                "$CurseForgeHost/api/projects/$CurseForgeProjectId/upload-file"
            if ($LASTEXITCODE -ne 0) { Fail "The CurseForge upload failed: $response" }
            Write-Host "CurseForge accepted the file: $response"
        }
        finally {
            Remove-Item -LiteralPath $metadataFile, $headerFile -Force -ErrorAction SilentlyContinue
        }
    }
}
finally {
    Remove-Item -LiteralPath $notesFile -Force -ErrorAction SilentlyContinue
}

Write-Host "`nReleased $version." -ForegroundColor Green
