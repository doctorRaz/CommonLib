<#
.SYNOPSIS
    Читает и валидирует конфигурацию release.

.DESCRIPTION
    Проверяет solution, группы publish, необязательные группы subProjects
    и modules, publicHere и remote.

    publish и subProjects состоят из объектов с name и массивом projects.
    name определяет каталог группы в release archive.

    Возвращает через $env:GITHUB_OUTPUT:
      - solution_path   - путь к solution;
      - publish_json    - JSON-массив основных групп публикации;
      - subprojects_json - JSON-массив групп дополнительных проектов;
      - modules_json    - JSON-массив внешних immutable modules;
      - product         - имя продукта;
      - public_here     - публиковать ли Release в текущем репозитории;
      - remote_json     - JSON-массив целевых mirror-репозиториев.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$configPath = Join-Path $env:GITHUB_WORKSPACE $env:RELEASE_CONFIG_FILE

if (-not (Test-Path -LiteralPath $configPath)) {
    throw "Release configuration was not found: $configPath"
}

$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json

if ([string]::IsNullOrWhiteSpace($config.solution)) {
    throw "Release configuration property 'solution' is empty."
}

if ($null -eq $config.publish -or @($config.publish).Count -eq 0) {
    throw "Release configuration property 'publish' must contain at least one group."
}

if ($null -eq $config.publicHere -or $config.publicHere.GetType() -ne [bool]) {
    throw "Release configuration property 'publicHere' must be a boolean."
}

if ($null -eq $config.remote) {
    throw "Release configuration property 'remote' must be an array. Use [] when no mirror repositories are configured."
}

$solutionPath = [string]$config.solution
$publishGroups = @($config.publish)
$remoteRepos = @($config.remote)
$subProjects = @($config.subProjects)
$modules = if ($null -eq $config.modules) { @() } else { @($config.modules) }

$ignoreSubProjects = $env:IGNORE_SUBPROJECTS -eq 'true'
if ($ignoreSubProjects) {
    $subProjects = @()
}

$solutionName = [System.IO.Path]::GetFileNameWithoutExtension($solutionPath)

if ([string]::IsNullOrWhiteSpace($solutionName)) {
    throw "Could not determine product name from solution: $solutionPath"
}

if (-not (Test-Path -LiteralPath (Join-Path $env:GITHUB_WORKSPACE $solutionPath))) {
    throw "Solution was not found: $solutionPath"
}

Write-Host "Solution: $solutionPath"

function Get-ValidatedGroups {
    param(
        [Parameter(Mandatory)] [array]$Groups,
        [Parameter(Mandatory)] [string]$PropertyName
    )

    $result = @()
    $names = @{}
    $projects = @{}

    foreach ($group in $Groups) {
        if ([string]::IsNullOrWhiteSpace([string]$group.name)) {
            throw "Each $PropertyName entry must contain a non-empty name."
        }

        $groupName = [string]$group.name

        if ($groupName -match '[\\/]') {
            throw "Invalid $PropertyName group name '$groupName'. Group name must be a single directory name."
        }

        if ($groupName -in @('.', '..') -or $groupName -match '(^|[\\/])\.\.([\\/]|$)') {
            throw "Invalid $PropertyName group name '$groupName'."
        }

        if ($names.ContainsKey($groupName)) {
            throw "Duplicate $PropertyName group name: $groupName"
        }
        $names[$groupName] = $true

        if ($null -eq $group.projects -or @($group.projects).Count -eq 0) {
            throw "Each $PropertyName entry must contain at least one project."
        }

        $validatedProjects = @()

        foreach ($projectPathValue in @($group.projects)) {
            $projectPath = [string]$projectPathValue

            if ([string]::IsNullOrWhiteSpace($projectPath)) {
                throw "$PropertyName group '$groupName' contains an empty project path."
            }

            $normalizedPath = $projectPath.Replace('\','/')

            if ([System.IO.Path]::IsPathRooted($normalizedPath) -or $normalizedPath -match '(^|/)\.\.(/|$)') {
                throw "Invalid project path '$projectPath' in $PropertyName group '$groupName'."
            }

            if (-not (Test-Path -LiteralPath (Join-Path $env:GITHUB_WORKSPACE $projectPath) -PathType Leaf)) {
                throw "Project was not found: $projectPath"
            }

            if ($projects.ContainsKey($normalizedPath)) {
                throw "Project '$projectPath' is assigned to more than one release group."
            }

            $projects[$normalizedPath] = $true
            $validatedProjects += $projectPath
        }

        $result += [pscustomobject]@{
            name = $groupName
            projects = $validatedProjects
        }
    }

    return $result
}

$publishGroupsValidated = @(Get-ValidatedGroups -Groups $publishGroups -PropertyName 'publish')

$subProjectsValidated = if ($ignoreSubProjects -or $subProjects.Count -eq 0) {
    @()
} else {
    @(Get-ValidatedGroups -Groups $subProjects -PropertyName 'subProjects')
}

$allGroupNames = @{}
foreach ($group in @($publishGroupsValidated) + @($subProjectsValidated)) {
    $groupName = [string]$group.name

    if ($allGroupNames.ContainsKey($groupName)) {
        throw "Duplicate release group name across publish/subProjects: $groupName"
    }

    $allGroupNames[$groupName] = $true
}

Write-Host "Publish groups:"
foreach ($group in $publishGroupsValidated) {
    Write-Host "  [$($group.name)]"

    foreach ($projectPath in @($group.projects)) {
        Write-Host "    $projectPath"
    }
}

Write-Host "Subprojects:"
foreach ($group in $subProjectsValidated) {
    Write-Host "  [$($group.name)]"

    foreach ($projectPath in @($group.projects)) {
        Write-Host "    $projectPath"
    }
}

# modules являются внешними опубликованными artifacts. Их исходники не проверяются
# и не участвуют в restore/build; проверяем только структуру конфигурации.
Write-Host "Modules:"
foreach ($module in $modules) {
    if ([string]::IsNullOrWhiteSpace([string]$module.repository)) {
        throw "Each module must define a repository."
    }

    $moduleRepository = [string]$module.repository

    if ($moduleRepository -notmatch '^[^/\s]+/[^/\s]+$') {
        throw "Invalid module repository '$moduleRepository'. Expected owner/repository."
    }

    if ($null -eq $module.projects -or @($module.projects).Count -eq 0) {
        throw "Module '$moduleRepository' must contain at least one project."
    }

    foreach ($projectEntry in @($module.projects)) {
        $moduleProject = if ($projectEntry -is [string]) { [string]$projectEntry } else { [string]$projectEntry.path }
        if ([string]::IsNullOrWhiteSpace($moduleProject)) {
            throw "Module '$moduleRepository' contains a project entry without 'path'."
        }
        if ($projectEntry -isnot [string] -and $null -ne $projectEntry.notUnique -and $projectEntry.notUnique -isnot [bool]) {
            throw "Module '$moduleRepository' project '$moduleProject' property 'notUnique' must be a boolean."
        }
        $normalizedProject = $moduleProject.Replace('\','/').Trim('/')

        if ([string]::IsNullOrWhiteSpace($normalizedProject)) {
            throw "Module '$moduleRepository' contains an empty project path."
        }

        if ($normalizedProject -match '(^|/)\.\.(/|$)' -or [System.IO.Path]::IsPathRooted($normalizedProject)) {
            throw "Invalid project path '$moduleProject' in module '$moduleRepository'."
        }

        Write-Host "  [$moduleRepository] $normalizedProject"
    }
}

Write-Host "Remote repositories:"
foreach ($remoteRepo in $remoteRepos) {
    if ([string]::IsNullOrWhiteSpace([string]$remoteRepo)) {
        throw "Release configuration contains an empty remote repository."
    }

    if ([string]$remoteRepo -notmatch '^[^/\s]+/[^/\s]+$') {
        throw "Invalid remote repository '$remoteRepo'. Expected owner/repository."
    }

    Write-Host "  $remoteRepo"
}

$publishJson = $publishGroupsValidated | ConvertTo-Json -Compress -Depth 10

$subProjectsJson = if (@($subProjectsValidated).Count -eq 0) {
    '[]'
} else {
    $subProjectsValidated | ConvertTo-Json -Compress -Depth 10
}

$modulesJson = if (@($modules).Count -eq 0) {
    '[]'
} else {
    $modules | ConvertTo-Json -Compress -Depth 10
}

$remoteJson = if ($remoteRepos.Count -eq 0) {
    '[]'
} else {
    $remoteRepos | ConvertTo-Json -Compress
}

"solution_path=$solutionPath" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
"publish_json=$publishJson" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
"subprojects_json=$subProjectsJson" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
"modules_json=$modulesJson" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
"product=$solutionName" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
"public_here=$($config.publicHere.ToString().ToLowerInvariant())" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
"remote_json=$remoteJson" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
