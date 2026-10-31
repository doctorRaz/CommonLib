<#
.SYNOPSIS
    Создаёт финальные release-архивы: обычный ZIP и защищённый 7z.

.DESCRIPTION
    Скрипт отвечает только за формирование физических архивов и их метаданных.
    Оба архива создаются из одного и того же подготовленного staging-каталога.

    Защищённый архив предназначен не для контроля доступа, а для повышения
    надёжности распространения через инфраструктуру, которая может фильтровать
    обычные ZIP-файлы. Значение для открытия архива является частью публичного
    контракта update.json и на текущем этапе совпадает с именем продукта.

    Outputs через GITHUB_OUTPUT:
      archive_name, archive_sha256, archive_size,
      protected_archive_name, protected_sha256, protected_size.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$product = $env:PRODUCT
$fullVersion = $env:FULL_VERSION
$stagingDirectory = $env:STAGING_DIRECTORY

if ([string]::IsNullOrWhiteSpace($product)) { throw 'PRODUCT is not set.' }
if ([string]::IsNullOrWhiteSpace($fullVersion)) { throw 'FULL_VERSION is not set.' }
if (-not (Test-Path -LiteralPath $stagingDirectory -PathType Container)) {
    throw "Staging directory was not found: $stagingDirectory"
}

$archiveName = "${product}_${fullVersion}.zip"
# Имя защищённого архива использует тот же префикс product_version,
# что и обычный ZIP, а суффикс -protected однозначно указывает его назначение.
$protectedArchiveName = "${product}_${fullVersion}-protected.7z"
$archivePath = Join-Path $env:GITHUB_WORKSPACE $archiveName
$protectedArchivePath = Join-Path $env:GITHUB_WORKSPACE $protectedArchiveName

# Значение не является секретом: оно публикуется в update.json как часть
# контракта и может быть заменено другой схемой без изменения формата 7z.
$archiveKey = $product

# Явно разрешаем путь к 7-Zip, чтобы workflow не зависел от PATH runner.
$sevenZipCandidates = @(
    (Get-Command 7z.exe -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -ErrorAction SilentlyContinue),
    (Get-Command 7zz.exe -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -ErrorAction SilentlyContinue),
    (Join-Path ${env:ProgramFiles} '7-Zip\7z.exe'),
    (Join-Path ${env:ProgramFiles} '7-Zip\7zz.exe')
) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

$sevenZipPath = $sevenZipCandidates |
    Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } |
    Select-Object -First 1

if ([string]::IsNullOrWhiteSpace($sevenZipPath)) {
    throw '7-Zip executable was not found on the runner.'
}

Write-Host "Using 7-Zip: $sevenZipPath"

# 7-Zip в режиме создания архива может обновить существующий файл, поэтому
# старый защищённый архив удаляем явно. Для ZIP это не требуется: -Force уже
# определяет поведение Compress-Archive при существующем файле.
Remove-Item -LiteralPath $protectedArchivePath -Force -ErrorAction SilentlyContinue

# Упаковываем содержимое staging, а не сам каталог, чтобы сохранить текущую
# структуру ZIP и 7z без дополнительного уровня staging.
$stagingContent = Join-Path $stagingDirectory '*'
Compress-Archive -Path $stagingContent -DestinationPath $archivePath -Force

if (-not (Test-Path -LiteralPath $archivePath -PathType Leaf)) {
    throw "Release archive was not created: $archivePath"
}

# Оба архива получают одинаковое содержимое. -mhe=on дополнительно шифрует
# заголовки 7z, включая имена файлов внутри архива. Формат 7z использует
# AES-256 для password-based encryption.
$sevenZipArguments = @(
    'a',
    '-t7z',
    '-mhe=on',
    "-p$archiveKey",
    $protectedArchivePath,
    $stagingContent
)

& $sevenZipPath @sevenZipArguments
if ($LASTEXITCODE -ne 0) {
    throw "7-Zip failed with exit code $LASTEXITCODE."
}

if (-not (Test-Path -LiteralPath $protectedArchivePath -PathType Leaf)) {
    throw "Protected release archive was not created: $protectedArchivePath"
}

$archiveFile = Get-Item -LiteralPath $archivePath
$protectedFile = Get-Item -LiteralPath $protectedArchivePath
$archiveSha256 = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
$protectedSha256 = (Get-FileHash -LiteralPath $protectedArchivePath -Algorithm SHA256).Hash.ToLowerInvariant()

Write-Host "Created archive: $archiveName"
Write-Host "Archive size: $($archiveFile.Length) bytes"
Write-Host "Archive SHA256: $archiveSha256"
Write-Host "Created protected archive: $protectedArchiveName"
Write-Host "Protected archive size: $($protectedFile.Length) bytes"
Write-Host "Protected archive SHA256: $protectedSha256"

"archive_name=$archiveName" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
"archive_sha256=$archiveSha256" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
"archive_size=$($archiveFile.Length)" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
"protected_archive_name=$protectedArchiveName" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
"protected_sha256=$protectedSha256" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
"protected_size=$($protectedFile.Length)" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
