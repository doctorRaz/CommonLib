<#
.SYNOPSIS
    Формирует staging-каталог с предсказуемой структурой релизного архива.

.DESCRIPTION
    Итоговая структура staging:

        <SolutionName>/
            <PublishGroupName>/
                <файлы всех основных проектов>
            <SubProjectGroupName>/
                <файлы всех проектов subProject>
            <содержимое assets с сохранением структуры>
        <Product>_<Version>.md

    PROJECT_TYPES_JSON содержит для каждого publish-проекта его тип и
    логическую группу. Все проекты одной группы объединяются непосредственно
    в каталог первого проекта этой группы. Это позволяет нескольким проектам
    одного subProject поставлять общий набор файлов без промежуточных
    каталогов отдельных проектов.

    Файлы publish с расширением .pdb исключаются из релиза.
    Для Library дополнительно исключаются .deps.json и .runtimeconfig.json.
    Для Exe эти файлы сохраняются, поскольку они необходимы для запуска.

    Main assets размещаются в каталоге solution с сохранением структуры.
    Assets из subProject в release не включаются.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$projectTypes = @($env:PROJECT_TYPES_JSON | ConvertFrom-Json)
if ($projectTypes.Count -eq 0) {
    throw 'PROJECT_TYPES_JSON does not contain any projects.'
}

$publishRoot = Join-Path $env:GITHUB_WORKSPACE 'publish'
$assetsRoot = Join-Path $env:GITHUB_WORKSPACE 'assets'
$releaseNotesPath = Join-Path $env:GITHUB_WORKSPACE 'release-notes.md'

$solutionName = $env:PRODUCT
if ([string]::IsNullOrWhiteSpace($solutionName)) {
    throw 'PRODUCT is empty; cannot determine solution directory.'
}

$stagingDirectory = Join-Path $env:GITHUB_WORKSPACE 'staging'
$solutionDirectory = Join-Path $stagingDirectory $solutionName

Remove-Item -LiteralPath $stagingDirectory -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $solutionDirectory -Force | Out-Null

$excludedExtensions = @('.pdb')

if (-not (Test-Path -LiteralPath $publishRoot -PathType Container)) {
    throw "Publish directory was not found: $publishRoot"
}

if (-not (Test-Path -LiteralPath $releaseNotesPath -PathType Leaf)) {
    throw "Release notes were not found: $releaseNotesPath"
}

$projectRoots = @()
foreach ($metadata in $projectTypes) {
    $projectPathText = [string]$metadata.Path
    $projectName = [string]$metadata.Name
    $groupName = [string]$metadata.GroupName
    $groupKind = [string]$metadata.GroupKind
    $projectType = [string]$metadata.Type

    if ([string]::IsNullOrWhiteSpace($projectPathText) -or
        [string]::IsNullOrWhiteSpace($projectName) -or
        [string]::IsNullOrWhiteSpace($groupName) -or
        [string]::IsNullOrWhiteSpace($groupKind) -or
        [string]::IsNullOrWhiteSpace($projectType)) {
        throw 'PROJECT_TYPES_JSON contains incomplete project metadata.'
    }

    $projectRoot = Join-Path $publishRoot $projectName
    if (-not (Test-Path -LiteralPath $projectRoot -PathType Container)) {
        throw "Published project directory was not found: $projectRoot"
    }

    $projectRoots += [pscustomobject]@{
        Path = $projectPathText
        Name = $projectName
        Type = $projectType
        GroupName = $groupName
        GroupKind = $groupKind
        Root = $projectRoot
    }
}

$files = Get-ChildItem -LiteralPath $publishRoot -File -Recurse |
    Where-Object { $excludedExtensions -notcontains $_.Extension.ToLowerInvariant() }

$stagedPublishFileCount = 0

foreach ($file in $files) {
    $matchedProject = $null
    foreach ($projectRoot in $projectRoots) {
        $prefix = $projectRoot.Root + [System.IO.Path]::DirectorySeparatorChar
        if ($file.FullName.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            $matchedProject = $projectRoot
            break
        }
    }

    if ($null -eq $matchedProject) {
        throw "Could not determine publish project for file: $($file.FullName)"
    }

    $fileName = [System.IO.Path]::GetFileName($file.FullName)
    $isRuntimeMetadata =
        $fileName.EndsWith('.deps.json', [System.StringComparison]::OrdinalIgnoreCase) -or
        $fileName.EndsWith('.runtimeconfig.json', [System.StringComparison]::OrdinalIgnoreCase)

    if ($matchedProject.Type -eq 'Library' -and $isRuntimeMetadata) {
        Write-Host "Excluded for Library project: $($file.FullName)"
        continue
    }

    # Каталог назначения определяется явно заданной группой публикации.
    $groupDirectory = Join-Path $solutionDirectory $matchedProject.GroupName
    $relativePath = $file.FullName.Substring($matchedProject.Root.Length).TrimStart([char]'\', [char]'/')
    $destinationPath = Join-Path $groupDirectory $relativePath
    $destinationDirectory = Split-Path -Parent $destinationPath

    New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
    Copy-Item -LiteralPath $file.FullName -Destination $destinationPath -Force
    $stagedPublishFileCount++
}

# Assets основного решения размещаются в каталоге solution, а не внутри
# project/subProject. Каталоги assets самих subProject сюда не попадают.
if (Test-Path -LiteralPath $assetsRoot -PathType Container) {
    $assetFiles = @(Get-ChildItem -LiteralPath $assetsRoot -File -Recurse)

    foreach ($assetFile in $assetFiles) {
        $relativeAssetPath = $assetFile.FullName.Substring($assetsRoot.Length).TrimStart([char]'\', [char]'/')
        $destinationPath = Join-Path $solutionDirectory $relativeAssetPath
        $destinationDirectory = Split-Path -Parent $destinationPath

        New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
        Copy-Item -LiteralPath $assetFile.FullName -Destination $destinationPath -Force
    }

    Write-Host "Staged $($assetFiles.Count) asset file(s) from: $assetsRoot"
}
else {
    Write-Host "Assets directory not found; no additional release files to stage."
}

$markdownName = "${env:PRODUCT}_${env:FULL_VERSION}.md"
$stagedMarkdownPath = Join-Path $stagingDirectory $markdownName
Copy-Item -LiteralPath $releaseNotesPath -Destination $stagedMarkdownPath -Force

# Внешние modules добавляются после основного staging. Они получаются только
# из опубликованных Release artifacts; исходный код модулей здесь не checkout-ится
# и не собирается.
if ([string]::IsNullOrWhiteSpace($env:MODULES_JSON) -or $env:MODULES_JSON -ne '[]') {
    $moduleScript = Join-Path $env:GITHUB_WORKSPACE '.github/scripts/Add-ModuleReleaseArtifacts.ps1'
    if (-not (Test-Path -LiteralPath $moduleScript -PathType Leaf)) {
        throw "Module staging script was not found: $moduleScript"
    }

    $env:STAGING_DIRECTORY = $stagingDirectory
    & $moduleScript
}

$stagedFiles = @(Get-ChildItem -LiteralPath $stagingDirectory -File -Recurse |
    ForEach-Object {
        $_.FullName.Substring($stagingDirectory.Length).TrimStart([char]'\', [char]'/')
    } |
    Sort-Object)

Write-Host '--- Release staging tree ---'
if ($stagedFiles.Count -eq 0) {
    Write-Host '(empty)'
}
else {
    $stagedFiles | ForEach-Object { Write-Host $_ }
}
Write-Host "--- End release staging tree ($($stagedFiles.Count) file(s)) ---"
Write-Host "Staged $stagedPublishFileCount publish file(s), release notes and assets into: $stagingDirectory"

"staging_directory=$stagingDirectory" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
