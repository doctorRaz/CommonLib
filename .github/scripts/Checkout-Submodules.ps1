function Get-SubmodulePaths([string]$gitmodulesPath) {
    if (-not (Test-Path -LiteralPath $gitmodulesPath)) {
        return @()
    }

    $lines = @(git config --file $gitmodulesPath --get-regexp '^submodule\..*\.path$')
    if ($LASTEXITCODE -ne 0) {
        return @()
    }

    foreach ($line in $lines) {
        if ($line -match '^submodule\..*\.path\s+(.+)$') {
            $Matches[1].Trim()
        }
    }
}

function Update-Submodule([string]$path) {
    Write-Host "Updating submodule: $path"

    # Сначала используем обычный доступ. Это позволяет public submodules
    # работать без передачи им private credentials.
    git submodule update --init -- "$path"
    if ($LASTEXITCODE -eq 0) {
        return
    }

    if ([string]::IsNullOrWhiteSpace($env:PRIVATE_SUBMODULE_TOKEN)) {
        throw "Submodule '$path' is not accessible with GITHUB_TOKEN and PRIVATE_SUBMODULE_TOKEN is not configured."
    }

    # Token применяется только к повторной GitHub-операции и не записывается
    # в постоянную git-конфигурацию runner.
    $auth = [Convert]::ToBase64String(
        [System.Text.Encoding]::ASCII.GetBytes("x-access-token:$env:PRIVATE_SUBMODULE_TOKEN")
    )

    Write-Host "Retrying submodule with private-submodule credentials: $path"
    git -c "http.https://github.com/.extraheader=AUTHORIZATION: basic $auth" submodule update --init -- "$path"

    if ($LASTEXITCODE -ne 0) {
        throw "Failed to checkout submodule '$path'."
    }
}

function Update-Submodules([string]$root) {
    $gitmodulesPath = Join-Path $root '.gitmodules'
    $paths = @(Get-SubmodulePaths $gitmodulesPath)

    foreach ($relativePath in $paths) {
        $fullPath = if ($root -eq '.') {
            $relativePath
        } else {
            Join-Path $root $relativePath
        }

        Update-Submodule $fullPath
        Update-Submodules $fullPath
    }
}

# Синхронизируем URL перед рекурсивным обходом, чтобы каждый submodule
# использовал актуальную конфигурацию .gitmodules.
git submodule sync --recursive
Update-Submodules '.'
