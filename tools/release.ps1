<#
.SYNOPSIS
    Builds, tests and publishes a release of Simple Location Display.

.DESCRIPTION
    Without -Publish this is a dry run: it checks the repo, builds, runs the tests and says
    what it would do, without tagging or pushing anything.

    With -Publish it then:
      1. tags the commit as v<version> and pushes the branch and the tag,
      2. creates a GitHub release with the zip attached and the changelog section as notes.

    Publishing the release starts the "Upload release files" workflow, which uploads the zip to
    CurseForge with the CF_API_KEY repository secret and, once it is set up, to Nexus Mods with
    NEXUSMODS_API_KEY. The mod can't be built on a GitHub runner, because the build needs the
    game's files, so this script does the build on your PC and the workflow only does the uploads.

    The version comes from <Version> in SimpleLocationDisplay/SimpleLocationDisplay.csproj.
    Before releasing, change the heading for that version in CHANGELOG.md from "(not released)"
    to the release date.

    Needs the .NET SDK, git, and the GitHub CLI (gh) signed in.

.PARAMETER Publish
    Actually tag, push and create the GitHub release. Leave it off for a dry run.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools/release.ps1
    powershell -ExecutionPolicy Bypass -File tools/release.ps1 -Publish
#>
[CmdletBinding()]
param(
    [switch]$Publish
)

$ErrorActionPreference = 'Stop'

$repo = 'Kkthnx-Stardew/SimpleLocationDisplay'
$nexusPage = 'https://www.nexusmods.com/stardewvalley/mods/33749'

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

$isPrerelease = $version -match '-(alpha|beta|rc)\.'

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

# --- GitHub setup ----------------------------------------------------------------------------
Step 'Checking GitHub'
& gh auth status *> $null
if ($LASTEXITCODE -ne 0) { Fail 'gh is not signed in. Run "gh auth login" first.' }
$secrets = & gh secret list --repo $repo 2>$null
$variables = & gh variable list --repo $repo 2>$null
$hasSecret = [bool]($secrets | Where-Object { $_ -match '^CF_API_KEY\s' })
$nexusReady = [bool]($secrets | Where-Object { $_ -match '^NEXUSMODS_API_KEY\s' }) -and
    [bool]($variables | Where-Object { $_ -match '^NEXUSMODS_FILE_ID\s' }) -and
    [bool]($variables | Where-Object { $_ -match '^NEXUSMODS_MOD_ID\s' })
Write-Host "gh signed in."
Write-Host "CF_API_KEY secret (CurseForge): $(if ($hasSecret) { 'found' } else { 'MISSING, the CurseForge upload will fail' })"
Write-Host "Nexus Mods (NEXUSMODS_API_KEY, NEXUSMODS_FILE_ID, NEXUSMODS_MOD_ID): $(if ($nexusReady) { 'all found' } else { 'not set up, so Nexus will be skipped' })"

# --- Publish ---------------------------------------------------------------------------------
Step ($(if ($Publish) { 'Publishing' } else { 'Dry run: would publish' }))
Write-Host "  tag and push:    $tag on main"
Write-Host "  GitHub release:  $tag with the zip$(if ($isPrerelease) { ' (marked pre-release)' })"
Write-Host "  CurseForge:      uploaded by the 'Upload release files' workflow once the release is published"
if ($isPrerelease) { Write-Host "  Nexus Mods:      skipped, Nexus only gets full releases" }
elseif ($nexusReady) { Write-Host "  Nexus Mods:      uploaded by the same workflow" }
else { Write-Host "  Nexus Mods:      not set up, so upload the zip by hand: $nexusPage" }

if (-not $Publish) {
    Write-Host "`nDry run finished. Run again with -Publish to do it." -ForegroundColor Green
    return
}
if (-not $hasSecret) { Fail 'The CF_API_KEY secret is missing, so CurseForge would not get the file. Add it under Settings, Secrets and variables, Actions, then run this again.' }

$notesFile = Join-Path ([IO.Path]::GetTempPath()) "sld-release-notes-$version.md"
Set-Content -LiteralPath $notesFile -Value $notes -Encoding UTF8

try {
    Step 'Tagging and pushing'
    Run 'git' @('tag', '-a', $tag, '-m', "Simple Location Display $version")
    Run 'git' @('push', 'origin', 'main')
    Run 'git' @('push', 'origin', $tag)

    Step 'Creating the GitHub release'
    $ghArgs = @('release', 'create', $tag, $zip, '--repo', $repo, '--title', "Simple Location Display $version", '--notes-file', $notesFile)
    if ($isPrerelease) { $ghArgs += '--prerelease' }
    Run 'gh' $ghArgs
}
finally {
    Remove-Item -LiteralPath $notesFile -Force -ErrorAction SilentlyContinue
}

Write-Host "`nReleased $version on GitHub." -ForegroundColor Green
Write-Host "Watch the uploads: https://github.com/$repo/actions"
if ($isPrerelease -or -not $nexusReady) {
    Write-Host "Upload this file to Nexus Mods by hand ($nexusPage):"
    Write-Host "  $zip"
}
Write-Host "Update the Nexus page description from docs/nexus-description.bbcode if it changed."
