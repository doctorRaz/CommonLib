<#
.SYNOPSIS
    Создаёт GitHub Release с финальными assets.

.PARAMETER Repository
    owner/repo, куда создаётся релиз. По умолчанию - текущий ($GITHUB_REPOSITORY).

.PARAMETER VerifyTag
    Если указан, gh release create запускается с --verify-tag: релиз
    создаётся только если тег УЖЕ существует в репозитории. Для public-репо
    это НЕ подходит (там тег создаётся самим gh release create), поэтому
    для private используется этот флаг, для public - нет.

.DESCRIPTION
    Публикует обычный ZIP, пароль-защищённый 7z и update.json. Все три файла
    были созданы до этого шага и передаются сюда без повторной упаковки.
    release-notes.md используется только как описание GitHub Release.
#>

[CmdletBinding()]
param(
    [string]$Repository = $env:GITHUB_REPOSITORY,
    [switch]$VerifyTag
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$tag = $env:GITHUB_REF_NAME
$product = $env:PRODUCT
$fullVersion = $env:FULL_VERSION
$archiveName = $env:ARCHIVE_NAME
$protectedArchiveName = $env:PROTECTED_ARCHIVE_NAME
$releaseTitle = "$product $fullVersion"

# Hashtable enumerator must be used explicitly. Without GetEnumerator(),
# PowerShell returns only the keys, so .Value/.Key are unavailable under
# StrictMode and validation fails before the release is created.
foreach ($entry in @{
    ARCHIVE_NAME = $archiveName
    PROTECTED_ARCHIVE_NAME = $protectedArchiveName
}.GetEnumerator()) {
    if ([string]::IsNullOrWhiteSpace([string]$entry.Value)) {
        throw "$($entry.Key) is empty."
    }
}

Write-Host "=== Create GitHub Release ==="
Write-Host "Repository: $Repository"
Write-Host "Tag: $tag"
Write-Host "Title: $releaseTitle"
Write-Host "Archive: $archiveName"
Write-Host "Protected archive: $protectedArchiveName"
Write-Host "Manifest: update.json"
Write-Host "Verify tag: $VerifyTag"

# --- Идемпотентность для rerun ---
# Удаляем существующий Release, но сохраняем тег: для private-репозитория
# gh release create вызывается с --verify-tag и ожидает существующий тег.
Write-Host "Checking/removing existing release '$tag'..."
$deleteStarted = [System.Diagnostics.Stopwatch]::StartNew()
gh release delete $tag --repo $Repository --yes 2>$null
$deleteExitCode = $LASTEXITCODE
$deleteStarted.Stop()
Write-Host "Release delete finished with exit code $deleteExitCode in $($deleteStarted.Elapsed.TotalSeconds.ToString('F1')) s."

$args = @(
    'release', 'create', $tag,
    '--repo', $Repository,
    '--title', $releaseTitle,
    '--notes-file', 'release-notes.md',
    $archiveName,
    $protectedArchiveName,
    'update.json'
)

if ($VerifyTag) {
    $args += '--verify-tag'
}

Write-Host "Starting 'gh release create'..."
$createStarted = [System.Diagnostics.Stopwatch]::StartNew()
gh @args
$createExitCode = $LASTEXITCODE
$createStarted.Stop()
Write-Host "'gh release create' finished with exit code $createExitCode in $($createStarted.Elapsed.TotalSeconds.ToString('F1')) s."

if ($createExitCode -ne 0) {
    throw "gh release create failed for $Repository with exit code $createExitCode"
}

Write-Host "=== GitHub Release created successfully ==="