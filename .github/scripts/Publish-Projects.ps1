<#
.SYNOPSIS
    Публикует основные проекты и группы subProjects для release.

.DESCRIPTION
    Основные проекты передаются через PROJECTS_JSON, дополнительные группы —
    через SUBPROJECTS_JSON. Каждый проект публикуется в отдельный каталог,
    после чего Stage-ReleaseFiles.ps1 объединяет результаты внутри своей
    логической группы.

    Первый проект группы определяет имя каталога группы. Для каждого проекта
    сохраняется Path, Name, Type, GroupName и GroupKind, чтобы staging не
    терял принадлежность проекта к subProject даже при совпадении имён.

    Build и Revision общие для всех публикаций. Restore выполняется отдельно
    для subProject-проектов, потому что они могут находиться в подключённом
    Git submodule и не входить в основной solution.

    Для legacy MSBuild-проектов dotnet publish может успешно завершиться, но
    не заполнить указанный --output каталог. В этом случае результат берётся
    из TargetDir, чтобы такие проекты также попадали в единый publish tree.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$publishGroups = @($env:PUBLISH_JSON | ConvertFrom-Json)
$subProjects = if ([string]::IsNullOrWhiteSpace($env:SUBPROJECTS_JSON)) {
    @()
} else {
    @($env:SUBPROJECTS_JSON | ConvertFrom-Json)
}

if ($publishGroups.Count -eq 0) {
    throw 'PUBLISH_JSON does not contain any release groups.'
}

$publishRoot = Join-Path $env:GITHUB_WORKSPACE 'publish'
Remove-Item -LiteralPath $publishRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $publishRoot -Force | Out-Null

# Метаданные проекта являются контрактом между publish и staging.
$projectTypes = @()

function Publish-ReleaseProject {
    param(
        [Parameter(Mandatory)] [string]$ProjectPath,
        [Parameter(Mandatory)] [string]$GroupName,
        [Parameter(Mandatory)] [string]$GroupKind
    )

    if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
        throw 'Release configuration contains an empty project path.'
    }

    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($ProjectPath)
    if ([string]::IsNullOrWhiteSpace($projectName)) {
        throw "Could not determine project name from: $ProjectPath"
    }

    $projectDirectory = Join-Path $publishRoot $projectName

    # OutputType вычисляется MSBuild с учётом SDK defaults и импортов.
    $outputType = (dotnet msbuild "$ProjectPath" -getProperty:OutputType).Trim()
    if ([string]::IsNullOrWhiteSpace($outputType)) {
        throw "OutputType is empty for project: $ProjectPath"
    }

    switch ($outputType.ToLowerInvariant()) {
        'library' { $projectType = 'Library' }
        'exe'     { $projectType = 'Exe' }
        'winexe'  { $projectType = 'Exe' }
        default   { throw "Unsupported OutputType '$outputType' for project: $ProjectPath" }
    }

    # Используем script scope: функция выполняется в дочерней области PowerShell,
    # поэтому обычное += к переменной верхнего уровня не изменило бы исходный массив.
    $script:projectTypes += [pscustomobject]@{
        Path      = $ProjectPath
        Name      = $projectName
        Type      = $projectType
        GroupName = $GroupName
        GroupKind = $GroupKind
    }

    Write-Host "Publishing: $ProjectPath"
    Write-Host "Output: $projectDirectory"
    Write-Host "Project type: $projectType (OutputType=$outputType)"
    Write-Host "Release group: $GroupName ($GroupKind)"

    # Основной solution уже восстановлен workflow. Для subProject выполняем
    # restore здесь, так как его проекты могут не входить в solution.
    if ($GroupKind -eq 'SubProject') {
        dotnet restore "$ProjectPath"
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet restore failed for $ProjectPath with exit code $LASTEXITCODE"
        }
    }

    dotnet publish "$ProjectPath" `
        --configuration Release `
        --no-restore `
        --output "$projectDirectory" `
        --self-contained false `
        -p:Build=$env:BUILD `
        -p:Revision=$env:REVISION `
        -p:SolutionName=$env:PRODUCT

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed for $ProjectPath with exit code $LASTEXITCODE"
    }

    # SDK-style projects normally populate --output directly. Legacy MSBuild
    # projects can report success without doing so, поэтому проверяем результат
    # явно и переносим стандартный TargetDir только для такого случая.
    $publishedFiles = @(Get-ChildItem -LiteralPath $projectDirectory -File -Recurse -ErrorAction SilentlyContinue)
    if ($publishedFiles.Count -eq 0) {
        $targetDirectory = (dotnet msbuild "$ProjectPath" -getProperty:TargetDir).Trim()
        if ([string]::IsNullOrWhiteSpace($targetDirectory)) {
            throw "Project publish produced no files and TargetDir is empty: $ProjectPath"
        }

        if (-not (Test-Path -LiteralPath $targetDirectory -PathType Container)) {
            throw "Project publish produced no files and TargetDir was not found: $targetDirectory"
        }

        $targetFiles = @(Get-ChildItem -LiteralPath $targetDirectory -File -Recurse)
        if ($targetFiles.Count -eq 0) {
            throw "Project publish produced no files and TargetDir is empty: $targetDirectory"
        }

        Write-Host "dotnet publish did not populate output for legacy project; copying $($targetFiles.Count) file(s) from TargetDir: $targetDirectory"
        New-Item -ItemType Directory -Path $projectDirectory -Force | Out-Null
        Copy-Item -LiteralPath (Join-Path $targetDirectory '*') -Destination $projectDirectory -Recurse -Force

        $publishedFiles = @(Get-ChildItem -LiteralPath $projectDirectory -File -Recurse)
    }

    if ($publishedFiles.Count -eq 0) {
        throw "Project publish produced no files: $ProjectPath"
    }

    Write-Host "Published $($publishedFiles.Count) file(s) for: $ProjectPath"
}

foreach ($group in $publishGroups) {
    $groupName = [string]$group.name
    $groupProjects = @($group.projects)

    if ([string]::IsNullOrWhiteSpace($groupName)) {
        throw 'Each publish group must contain a name.'
    }
    if ($groupProjects.Count -eq 0) {
        throw "Publish group '$groupName' does not contain any projects."
    }

    foreach ($projectPath in $groupProjects) {
        Publish-ReleaseProject -ProjectPath ([string]$projectPath) -GroupName $groupName -GroupKind 'Main'
    }
}

foreach ($subProject in $subProjects) {
    $groupName = [string]$subProject.name
    $groupProjects = @($subProject.projects)

    if ([string]::IsNullOrWhiteSpace($groupName)) {
        throw 'Each subProjects group must contain a name.'
    }
    if ($groupProjects.Count -eq 0) {
        throw "SubProjects group '$groupName' does not contain any projects."
    }

    foreach ($projectPath in $groupProjects) {
        Publish-ReleaseProject -ProjectPath ([string]$projectPath) -GroupName $groupName -GroupKind 'SubProject'
    }
}

$projectTypesJson = $projectTypes | ConvertTo-Json -Compress -Depth 5
"project_types_json=$projectTypesJson" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
