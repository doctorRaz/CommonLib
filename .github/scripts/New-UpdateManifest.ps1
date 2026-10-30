<#
.SYNOPSIS
    Формирует update.json из метаданных уже созданных release-архивов.

.DESCRIPTION
    Скрипт НЕ создаёт архивы. Архивы должны быть полностью сформированы до его
    запуска, а их имена, SHA-256 и размеры передаются через environment.

    Это разделяет упаковку и описание релиза: update.json содержит метаданные
    именно тех файлов, которые будут опубликованы, поэтому повторная упаковка
    после расчёта SHA-256 невозможна.

    Release notes добавляются в staging до упаковки и потому уже входят в оба
    финальных архива. Проверка их наличия здесь не дублируется: за staging
    отвечает Stage-ReleaseFiles.ps1, а за успешное создание обоих архивов —
    New-ReleaseArchives.ps1.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$product = $env:PRODUCT
$fullVersion = $env:FULL_VERSION
$mandatory = [System.Convert]::ToBoolean($env:MANDATORY)
$minimumVersionJson = $env:MINIMUM_VERSION_JSON
$commit = $env:COMMIT
$archiveName = $env:ARCHIVE_NAME
$archiveSha256 = $env:ARCHIVE_SHA256
$archiveSize = [long]$env:ARCHIVE_SIZE
$protectedArchiveName = $env:PROTECTED_ARCHIVE_NAME
$protectedSha256 = $env:PROTECTED_SHA256
$protectedSize = [long]$env:PROTECTED_SIZE

# Hashtable enumeration returns keys when iterated directly in PowerShell.
# GetEnumerator() is required here so that both the variable name (Key) and
# its value (Value) are available for validation under StrictMode.
foreach ($entry in @{
    PRODUCT = $product
    FULL_VERSION = $fullVersion
    COMMIT = $commit
    ARCHIVE_NAME = $archiveName
    ARCHIVE_SHA256 = $archiveSha256
    PROTECTED_ARCHIVE_NAME = $protectedArchiveName
    PROTECTED_SHA256 = $protectedSha256
}.GetEnumerator()) {
    if ([string]::IsNullOrWhiteSpace([string]$entry.Value)) {
        throw "$($entry.Key) is empty."
    }
}

$version = [System.Version]::Parse($fullVersion)
if ($version.Revision -lt 0) {
    throw "Revision is not available in version: $fullVersion"
}

$minimumVersion = if ($minimumVersionJson -eq 'null' -or [string]::IsNullOrWhiteSpace($minimumVersionJson)) {
    $null
}
else {
    $minimumVersionJson | ConvertFrom-Json
}

# Сохраняем существующий update.json contract и только добавляем новый
# protected-блок. Клиенты, использующие обычный ZIP, продолжают получать
# прежние поля без изменения их смысла.
$manifest = [ordered]@{
    product = $product
    version = [ordered]@{
        major = $version.Major
        minor = $version.Minor
        build = $version.Build
        revision = $version.Revision
    }
    tag = $env:GITHUB_REF_NAME
    asset = $archiveName
    sha256 = $archiveSha256
    size = $archiveSize
    commit = $commit
    mandatory = $mandatory
    minimumVersion = $minimumVersion
    publishedAt = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    protected = [ordered]@{
        asset = $protectedArchiveName
        sha256 = $protectedSha256
        size = $protectedSize
        format = '7z'
        encryption = 'AES-256'
        encryptedHeaders = $true
        # Пароль не является секретом и входит в публичный контракт manifest.
        # Способ его формирования можно изменить позже без изменения контракта.
        password = $product
    }
}

$manifestPath = Join-Path $env:GITHUB_WORKSPACE 'update.json'
$manifest | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $manifestPath -Encoding utf8NoBOM
