# Release settings

Этот каталог содержит настройки и служебные значения, используемые CI и release workflows.

## Release pipeline

Публикация выполняется через release pipeline:

```
merge PR в master / develop / test/master
        ↓
create-release-tag.yml
        ↓
release/mandatory tag
        ↓
release.yml
        ↓
restore → test → publish → staging → archives → GitHub Release / configured remotes
```

### 1. Create release tag

`.github/workflows/create-release-tag.yml` запускается после закрытия PR в настроенной ветке, если PR действительно был merged и содержит label `publish`.

Workflow:

1. checkout-ит именно `merge_commit_sha`;
2. читает `release.config.json`;
3. берёт первый проект первой группы `publish`;
4. получает его эффективную версию через MSBuild;
5. определяет `Major.Minor`;
6. сравнивает Major с предыдущим release tag;
7. выбирает `release` или `mandatory`;
8. вычисляет следующий номер серии;
9. создаёт annotated tag на commit merged PR;
10. явно запускает `release.yml` через `workflow_dispatch` с созданным tag.

Формат tag:

```
<solution>_<major>.<minor>-releaseN
<solution>_<major>.<minor>-mandatoryN
```

Изменение Major является границей `mandatory`. Для `release` и `mandatory` используются независимые счётчики.

Создание tag само по себе не выполняет сборку и публикацию. После создания tag запускается `release.yml`.

### 2. Release

`.github/workflows/release.yml` отвечает за полный production release pipeline.

Он:

1. checkout-ит исходный репозиторий;
2. при наличии настроенных `subProjects` получает git submodules;
3. читает и валидирует `release.config.json`;
4. проверяет соответствие tag продукту;
5. один раз вычисляет полную версию;
6. выполняет restore и test;
7. публикует все проекты из групп `publish` и `subProjects`;
8. формирует единый staging;
9. добавляет внешние `modules` из опубликованных Release artifacts;
10. создаёт ZIP и защищённый 7z из одного и того же staging;
11. формирует `update.json` по метаданным уже созданных архивов;
12. публикует один и тот же набор release-файлов в текущий репозиторий и настроенные `remote`.

Повторной сборки или повторной упаковки для разных репозиториев нет: все targets получают один и тот же готовый результат.

### 3. CI

`.github/workflows/ci.yml` используется для проверки изменений.

Он берёт solution из `release.config.json` и выполняет:

```
restore → build → test
```

CI не создаёт release tag и не публикует Release.

CI запускается для PR в `master`, после изменения `master`, а также может быть запущен вручную через `workflow_dispatch`.

## Файлы

### `release.config.json`

Основная конфигурация публикации.

| Свойство | Назначение |
|---|---|
| `solution` | Solution, из которого определяется продукт и выполняется основная сборка. |
| `publish` | Основные группы публикации. Каждая группа имеет `name` и массив `projects`. |
| `subProjects` | Необязательные группы дополнительных проектов. Формат такой же: `name` + `projects`. |
| `modules` | Внешние опубликованные модули, из которых в release берутся выбранные каталоги проектов и один MD-файл из корня ZIP. |
| `publicHere` | Создавать ли GitHub Release в текущем репозитории. |
| `remote` | Репозитории, в которые публикуется тот же готовый набор release-файлов. |

Конфигурация читается и валидируется `.github/scripts/Read-ReleaseConfig.ps1` до restore, test и publish.

### `release-version.txt`

Минимальная версия для обычного release-канала.

Пустой файл означает, что минимальная версия для обычного релиза не задаётся.

### `mandatory-version.txt`

Минимальная версия для mandatory release.

Пустой файл означает, что минимальная версия для mandatory-релиза не задаётся.

## Группы публикации

### `publish`

`publish` определяет структуру основных проектов в release archive.

Каждая запись имеет вид:

```json
{
  "name": "NC",
  "projects": [
    "NC/LFIO_NC.csproj",
    "NC2/LFIO_NC21.csproj"
  ]
}
```

`name` — имя каталога группы в release.

Все проекты одной группы публикуются отдельно, но их результаты объединяются непосредственно в один каталог группы. Поэтому несколько проектов могут входить в одну группу.

Например:

```
LFIO/
├── NC/
│   ├── ... результаты LFIO_NC ...
│   ├── ... результаты LFIO_NC21 ...
│   └── ... результаты LFIO_NC26 ...
└── AC/
    ├── ... результаты LFIO_AC2018 ...
    ├── ... результаты LFIO_AC2019 ...
    └── ... результаты LFIO_AC2020 ...
```

Каждый проект может быть назначен только одной release-группе.

В `publish` указываются только проекты, которые должны попасть в release. Остальные проекты solution могут участвовать в restore/build/test, но не публикуются.

Имена групп `publish` должны быть уникальны. Они также не могут совпадать с именами групп `subProjects`.

### `subProjects`

`subProjects` имеет тот же формат группировки:

```json
{
  "name": "ChangeDBmod_NC",
  "projects": [
    "ChangedbMod/ChangeDBmod.NC/ChangeDBmod.NC.csproj",
    "ChangedbMod/ChangeDBmod.NC.21/ChangeDBmod.NC.21.0.csproj"
  ]
}
```

Каждая группа попадает в staging как отдельный каталог с указанным `name`.

Несколько проектов могут входить в одну группу `subProjects`.

`subProjects` предназначен, в частности, для проектов из git submodules. Такие проекты могут не входить в основной solution; для них `Publish-Projects.ps1` выполняет отдельный restore.

Если `subProjects` отсутствует или пуст, дополнительных проектов нет.

Checkout submodules выполняется `.github/scripts/Checkout-Submodules.ps1` рекурсивно. Сначала используется обычный Git-доступ, а при ошибке доступа используется `PRIVATE_SUBMODULE_TOKEN`.

### Итоговая структура

В общем случае staging имеет вид:

```
<solution>/
├── <publish-group>/
│   └── ...
├── <subProject-group>/
│   └── ...
└── assets/
    └── ...
<Product>_<version>.md
```

Имена каталогов групп берутся из `name`, а не из имени первого проекта.

Для основных `assets/` сохраняется исходная структура. Assets самих subProjects в основной release staging не добавляются.

При staging исключаются `.pdb`. Для проектов типа `Library` дополнительно исключаются `.deps.json` и `.runtimeconfig.json`; для `Exe` они сохраняются.

## Внешние modules

`modules` — это не submodule и не дополнительная сборка исходников.

Каждый module задаётся:

```json
{
  "repository": "owner/repository",
  "projects": [
    "path/to/Project1",
    {
      "path": "path/to/Project2",
      "notUnique": true
    }
  ]
}
```

Элемент `projects` может быть указан в двух формах:

- строка: `"path/to/Project"` — обычный проект с `notUnique = false`;
- объект: `{ "path": "path/to/Project", "notUnique": true }` — проект, для которого разрешено повторное использование конечного каталога.

Свойство `notUnique` необязательно. Если оно отсутствует, используется `false`.

По умолчанию конечное имя каталога проекта должно быть уникальным среди всех подключаемых module projects. Если два проекта имеют одинаковое конечное имя и оба указаны с `notUnique: true`, их содержимое объединяется в один каталог.

При объединении:

- разные файлы могут находиться в одном конечном каталоге;
- если одинаковый относительный путь к файлу встречается более одного раза, release завершается ошибкой;
- правило одинаково применяется к любым каталогам, а не только к `docs`.

Например, несколько модулей могут объединить свои документационные каталоги в общий `docs`:

```json
"projects": [
  {
    "path": "ChangeDBmod/docs",
    "notUnique": true
  },
  {
    "path": "LFIO/docs",
    "notUnique": true
  }
]
```

Release workflow:

1. получает последний опубликованный Release указанного репозитория;
2. выбирает его единственный ZIP asset;
3. проверяет SHA-256 asset;
4. извлекает только указанные каталоги проектов;
5. добавляет их в staging текущего продукта;
6. добавляет один MD-файл из корня ZIP внешнего Release.

Исходный код module в текущем репозитории не checkout-ится и не участвует в restore/build.

Несколько проектов одного module поддерживаются. Имена конечных каталогов берутся из последнего компонента указанного пути проекта. Уникальность конечных имён действует по умолчанию и может быть снята для конкретных проектов через `notUnique: true`.

### Контракт ZIP внешнего module

Обычный module Release должен содержать:

- ровно один ZIP asset;
- один корневой каталог внутри ZIP;
- ровно один MD-файл непосредственно в корне ZIP.

Mega Release не является module этого контракта и не должен подключаться через `modules`.

## Полный пример текущей конфигурации

Ниже приведён полный фактически используемый сейчас `.github/release-settings/release.config.json` из ветки `test/master`:

```json
{
    "solution": "LFIO.sln",
    "publish": [
        {
            "name": "NC",
            "projects": [
                "NC/LFIO_NC.csproj",
                "NC2/LFIO_NC21.csproj",
                "NC3/LFIO_NC26.csproj"
            ]
        },
        {
            "name": "AC",
            "projects": [
                "AC/LFIO_AC2018.csproj",
                "AC2/LFIO_AC2019.csproj",
                "AC3/LFIO_AC2020.csproj"
            ]
        }
    ],
    "subProjects": [
        {
            "name": "ChangeDBmod_NC",
            "projects": [
                "ChangedbMod/ChangeDBmod.NC/ChangeDBmod.NC.csproj",
                "ChangedbMod/ChangeDBmod.NC.21/ChangeDBmod.NC.21.0.csproj",
                "ChangedbMod/ChangeDBmod.NC.26/ChangeDBmod.NC.26.0.csproj"
            ]
        },
        {
            "name": "ChangeDBmod_AC",
            "projects": [
                "ChangedbMod/ChangeDBmod.AC2018/ChangeDBmod.AC2018.csproj"
            ]
        }
    ],
    "modules": [
        {
            "repository": "doctorRaz/Publish_Test",
            "projects": [
                "drzTools/Archivist",
                "drzTools/Text_LSP"
            ]
        }
    ],
    "publicHere": true,
    "remote": [
        "doctorRaz/LFIO"
    ]
}
```

В этой конфигурации:

- `NC` объединяет три основных проекта в один каталог release;
- `AC` объединяет три основных проекта в один каталог release;
- `ChangeDBmod_NC` объединяет три проекта subProject;
- `ChangeDBmod_AC` содержит один проект subProject;
- module `doctorRaz/Publish_Test` поставляет два внешних проекта;
- `doctorRaz/LFIO` получает тот же итоговый release как configured remote.

## Ключи и токены GitHub Actions

Workflow разделяет доступ для чтения зависимостей и публикации релизов.

### `PRIVATE_SUBMODULE_TOKEN`

Используется как fallback:

- при checkout приватного git submodule;
- при чтении Release artifacts приватного внешнего module.

Обычный доступ через `github.token` используется первым. Token применяется только если обычный доступ не сработал.

Этот ключ не используется для основного checkout и не используется для публикации Release.

### `DOC_PROPS_RELEASE_TOKEN`

Используется при публикации Release в настроенные `remote`:

```yaml
env:
  GH_TOKEN: ${{ secrets.DOC_PROPS_RELEASE_TOKEN }}
```

Его назначение — запись в целевые репозитории: создание или обновление GitHub Release и загрузка release assets.

### `github.token`

Встроенный token используется:

- для создания и push release tag;
- для запуска `release.yml` через `workflow_dispatch`;
- для создания Release в текущем репозитории;
- как основной token при чтении внешних module Releases.

Для `create-release-tag.yml` необходимы:

- `contents: write`;
- `actions: write`.

Для `release.yml` используется `contents: write`.

### Принцип разделения

| Операция | Token / secret | Назначение |
|---|---|---|
| Основной checkout | `github.token` | read |
| Checkout приватных submodules | `PRIVATE_SUBMODULE_TOKEN` | read, fallback |
| Чтение приватных module Releases | `PRIVATE_SUBMODULE_TOKEN` | read, fallback |
| Создание release tag | `github.token` | write |
| Запуск `release.yml` | `github.token` | actions: write |
| Release в текущем репозитории | `github.token` | write |
| Release в configured remotes | `DOC_PROPS_RELEASE_TOKEN` | write |

## Тестовые workflows

Файлы `test-*.yml` предназначены для технических проверок GitHub Actions и не входят в production release pipeline.

Их назначение определяется непосредственно содержимым соответствующего workflow; они могут запускаться вручную.

## Как изменять настройки

Изменения в `.github/release-settings` влияют на последующие публикации.

При изменении контракта `release.config.json` необходимо проверить как минимум:

- `.github/scripts/Read-ReleaseConfig.ps1`;
- `.github/scripts/Publish-Projects.ps1`;
- `.github/scripts/Stage-ReleaseFiles.ps1`;
- `.github/scripts/Add-ModuleReleaseArtifacts.ps1`;
- `.github/scripts/New-ReleaseArchives.ps1`;
- `.github/scripts/New-UpdateManifest.ps1`;
- `.github/workflows/create-release-tag.yml`;
- `.github/workflows/release.yml`;
- `.github/workflows/ci.yml`.

Документация должна отражать фактическое поведение workflows. При изменении триггеров, токенов, структуры конфигурации или формата release artifacts этот README следует обновлять вместе с реализацией.
