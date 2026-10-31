<#
.SYNOPSIS
    Публикует финальные release assets в целевом GitHub-репозитории.

.DESCRIPTION
    Release notes используются только как описание GitHub Release.
    Финальные ZIP и защищённый 7z уже созданы до публикации и повторно
    не собираются. update.json содержит SHA-256 именно этих файлов.

    Репозиторий передаётся через TARGET_REPO из release.config.json.
    Workflow остаётся универсальным и не содержит имени конкретного проекта.

    При rerun существующий Release не удаляется: он обновляется, устаревшие
    assets удаляются, а актуальные assets загружаются с --clobber. Это сохраняет
    Release identity, created_at и существующий release tag.

    Входные параметры берутся из env:
      TARGET_REPO, PRODUCT, FULL_VERSION, ARCHIVE_NAME,
      PROTECTED_ARCHIVE_NAME и GH_TOKEN.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repo = $env:TARGET_REPO
$tag = $env:GITHUB_REF_NAME
$product = $env:PRODUCT
$fullVersion = $env:FULL_VERSION
$archiveName = $env:ARCHIVE_NAME
$protectedArchiveName = $env:PROTECTED_ARCHIVE_NAME

if ([string]::IsNullOrWhiteSpace($repo)) { throw 'TARGET_REPO is not set.' }
if ([string]::IsNullOrWhiteSpace($archiveName)) { throw 'ARCHIVE_NAME is not set.' }
if ([string]::IsNullOrWhiteSpace($protectedArchiveName)) { throw 'PROTECTED_ARCHIVE_NAME is not set.' }

# Целевой тег создаётся поверх default branch репозитория. Это устраняет
# зависимость workflow от имени ветки конкретного mirror.
$targetBranch = (gh repo view $repo --json defaultBranchRef --jq .defaultBranchRef.name).Trim()
if ([string]::IsNullOrWhiteSpace($targetBranch)) {
    throw "Could not determine default branch of $repo."
}

# Ошибка gh release view означает, что Release ещё отсутствует и его можно создать.
$releaseExists = $true
gh release view $tag --repo $repo --json tagName 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) { $releaseExists = $false }

$releaseTitle = "$product $fullVersion"
$expectedAssets = @($archiveName, $protectedArchiveName, 'update.json')

Write-Host "Publishing release to repository: $repo"
Write-Host "  Tag:    $tag"
Write-Host "  Target: $targetBranch"
Write-Host "  Title:  $releaseTitle"
Write-Host "  Assets: $($expectedAssets -join ', ')"

if ($releaseExists) {
    # Сохраняем существующий Release object при повторном запуске.
    gh release edit $tag `
        --repo $repo `
        --title $releaseTitle `
        --notes-file release-notes.md

    if ($LASTEXITCODE -ne 0) {
        throw "gh release edit failed for $repo with exit code $LASTEXITCODE"
    }

    # Удаляем старые assets, чтобы rerun не оставлял файлы от предыдущей публикации.
    $existingAssets = gh release view $tag --repo $repo --json assets --jq '.assets[].name'
    foreach ($assetName in $existingAssets) {
        if ($expectedAssets -notcontains $assetName) {
            Write-Host "Removing stale release asset: $assetName"
            gh release delete-asset $tag $assetName --repo $repo --yes
            if ($LASTEXITCODE -ne 0) {
                throw "gh release delete-asset failed for '$assetName' in $repo with exit code $LASTEXITCODE"
            }
        }
    }

    # Загружаем ровно тот же набор готовых файлов, что был рассчитан в manifest.
    gh release upload $tag `
        --repo $repo `
        --clobber `
        $archiveName `
        $protectedArchiveName `
        update.json

    if ($LASTEXITCODE -ne 0) {
        throw "gh release upload failed for $repo with exit code $LASTEXITCODE"
    }

    Write-Host "Release updated successfully in $repo."
}
else {
    # При первом запуске GitHub создаёт тег поверх default branch target repository.
    gh release create $tag `
        --repo $repo `
        --target $targetBranch `
        --title $releaseTitle `
        --notes-file release-notes.md `
        $archiveName `
        $protectedArchiveName `
        update.json

    if ($LASTEXITCODE -ne 0) {
        throw "gh release create failed for $repo with exit code $LASTEXITCODE"
    }

    Write-Host "Release created successfully in $repo."
}
