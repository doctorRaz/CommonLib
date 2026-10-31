<#
.SYNOPSIS
    Формирует тело GitHub Release и сохраняет его в release-notes.md.

.DESCRIPTION
    Логика:
      1. Достаёт сообщение тега:
         - для annotated tag - из git/tags (поле message);
         - для lightweight tag - из commit message, на который указывает тег.
      2. Запрашивает у GitHub автоматически сгенерированный changelog
         (releases/generate-notes) для этого тега.
      3. Склеивает: заголовок "# <tag>", сообщение тега, changelog.

    Результат пишется в $GITHUB_WORKSPACE/release-notes.md, откуда его
    заберут:
      - шаг создания архива (положит .md внутрь ZIP);
      - шаг создания релиза (через --notes-file или как отдельный asset).

    Важно: этот скрипт должен вызываться ДО упаковки архива, чтобы
    release notes успели попасть в staging и участвовать в расчёте sha256.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# GH_TOKEN уже проброшен через env на уровне workflow.
$tag = $env:GITHUB_REF_NAME
$repo = $env:GITHUB_REPOSITORY

# --- 1. Сообщение тега ---
# git/ref/tags/<tag> возвращает объект, на который указывает тег.
# Для annotated tag это tag-объект (нужно дополнительно запросить git/tags),
# для lightweight - сразу commit.
$tagRef = gh api "repos/$repo/git/ref/tags/$tag" | ConvertFrom-Json

if ($tagRef.object.type -eq 'tag') {
    # annotated tag: сообщение лежит в объекте tag.
    $tagMessage = (gh api "repos/$repo/git/tags/$($tagRef.object.sha)" | ConvertFrom-Json).message
} else {
    # lightweight tag: берём commit message.
    $tagMessage = (gh api "repos/$repo/commits/$($tagRef.object.sha)" | ConvertFrom-Json).commit.message.Trim()
}

# --- 2. Автоматический changelog ---
# GitHub сам строит список PR'ов/коммитов между предыдущим и текущим тегом.
# Требует полной истории (см. fetch-depth: 0 в checkout).
$generated = gh api --method POST "repos/$repo/releases/generate-notes" -f tag_name="$tag" | ConvertFrom-Json

# --- 3. Склейка ---
# Не обрабатываем содержимое tagMessage: это Markdown из PR body и символ '#' 
# должен остаться без изменений.
# `r`n - потому что артефакты потребляются в основном на Windows.
$body = "# $tag"
if (-not [string]::IsNullOrEmpty([string]$tagMessage))  { $body += "`r`n`r`n$tagMessage" }
if (-not [string]::IsNullOrEmpty([string]$generated.body)) { $body += "`r`n`r`n$($generated.body)" }

# utf8NoBOM - GitHub корректно отображает UTF-8 без BOM; BOM иногда
# приводит к появлению «» в начале markdown-файла.
$body | Set-Content -Path (Join-Path $env:GITHUB_WORKSPACE 'release-notes.md') -Encoding utf8NoBOM