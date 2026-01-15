# Техническое задание (ТЗ)
## Проект: Multi-tenant система генерации и публикации контента (MVP)

Дата: 2026-01-13
Язык документа: RU
Статус: Черновик ТЗ для реализации прототипа

---

## 1. Цель и суть продукта

Система предназначена для малого бизнеса (tenant), которому нужно вести несколько каналов публикаций. В MVP основной канал Telegram, дополнительно закладываются адаптеры Twitter/X и WordPress. Instagram фиксируется как планируемый адаптер.

Пользователь создаёт кампанию (папку) с общими параметрами генерации, вводит список строк-данных (items) и получает набор вариантов текста и изображений для каждого будущего поста. Далее пользователь выбирает лучшие варианты, при необходимости редактирует, утверждает и публикует сразу или по расписанию.

Ключевые особенности MVP:
- Multi-tenant: изоляция данных и настроек каждого tenant.
- Несколько targets внутри tenant (несколько Telegram-каналов). Адаптеры для Twitter/X, WordPress и Instagram планируются, но не реализуются в MVP.
- Генерация текста: multi-agent (минимум 2 агента) через Microsoft Agent Framework.
- Генерация изображений: Stable Diffusion локально или Azure, конфигурация на уровне tenant.
- Модерация: пользователь выбирает текст и картинку, утверждает или отменяет.
- Публикация “сейчас” и “по расписанию”.
- Планировщик: использование популярной библиотеки для .NET (рекомендация: Quartz.NET).

---

## 2. Термины и определения

- **Tenant**: предприятие (клиент) с отдельными каналами, кампаниями, ключами и данными.
- **Target / Channel**: конечная точка публикации (Telegram-канал, аккаунт X, сайт WordPress и т.д.).
- **Кампания (папка)**: контейнер, объединяющий пачку постов и общие настройки генерации (промпты, стиль изображений, число вариантов, расписание).
- **Item (строка данных)**: одна сущность входных данных, из которой получится один пост.
- **Post**: публикация, которая будет выпущена в выбранные targets.
- **PostVariant**: вариант текста для одного Post, сгенерированный LLM.
- **ImageVariant**: вариант изображения для одного Post, сгенерированный генератором изображений.

### 2.1 Статусы

**PostStatus**
- Created (пост создан, генерация не запускалась)
- Generating (идёт генерация текста и/или изображения)
- Draft (есть результаты генерации, доступна модерация)
- Approved (пользователь утвердил контент)
- Scheduled (назначено время публикации)
- Published (публикация выполнена)
- Failed (ошибка генерации или публикации, требуется действие)
- Cancelled (пост отменён пользователем)

**Post readiness (flags)**
- IsTextReady (текст готов)
- IsImageReady (картинка готова)

**ChannelPublishStatus (per target)**
- NotAttempted (не пытались публиковать)
- Succeeded (успешно опубликовано)
- Failed (ошибка публикации)

**PublishRollupStatus (computed)**
- None (не было попыток публикации по выбранным каналам)
- Partial (опубликовано не во всех выбранных каналах)
- Full (опубликовано во всех выбранных каналах)

---

## 3. Область охвата MVP

### 3.1 Входит в MVP
1) Управление tenant (создание и выбор tenant в UI).
2) Управление targets в tenant (создание, редактирование, выключение, сброс ошибки).
3) Управление кампаниями: общий промпт, стиль изображений, число вариантов, расписание.
4) Ввод списка items и автоматическое разворачивание в отдельные записи.
5) Генерация N вариантов текста и M вариантов изображений для каждого Post.
6) UI модерации: выбор текста и картинки, Markdown-редактор и preview.
7) Публикация “сейчас” и публикация “по расписанию”.
8) Планировщик задач и обработка пропущенных публикаций по настройке.
9) Ошибка target: блокировка публикаций в target до ручного сброса.
10) Заморозка опубликованных постов: история публикации сохраняется (см. раздел 5.10).

### 3.2 Не входит в MVP
- RAG и подключение внешних источников знаний.
- Детальная RBAC модель (в MVP доступ на уровне tenant, внутри tenant все могут всё).
- Продвинутая аналитика вовлечённости, A/B тесты.
- Полноценный конвертер Markdown под каждую платформу (в MVP заглушка).

---

## 4. Пример предметной области (для понимания)

Пример кампании: “Рецепты к праздникам”.

- В кампании задаётся общий промпт: “Сгенерируй короткий пост с рецептом традиционного праздничного блюда для указанного праздника”.
- Пользователь вставляет список праздников (каждая строка отдельный праздник).
- Система разворачивает строки в items и для каждого item генерирует несколько вариантов текста и изображений.
- Пользователь выбирает один вариант текста и одну картинку, редактирует и публикует.

Это пример только для пояснения структуры, без ограничения системы только рецептами.

---

## 5. Функциональные требования

### 5.1 Multi-tenant
- Все сущности имеют TenantId.
- В рамках tenant нет разграничения ролей (MVP).
- Данные tenant не доступны другим tenant.

### 5.2 Targets (каналы публикации)

Поддерживаемые типы:
- Telegram (обязательно, единственный реализованный адаптер в MVP)
- Twitter/X (планируемый адаптер, без реализации в MVP)
- WordPress (планируемый адаптер, без реализации в MVP)
- Instagram (планируемый адаптер, без реализации в MVP)

Требования:
- В одном tenant можно создать несколько targets одного типа.
- Target имеет только флаг ошибки:
- Error (публикация в target запрещена)
- При Error система не пытается публиковать в этот target, пока пользователь явно не сбросит Error.

Минимальные поля Target:
- Id, TenantId
- Type (пока только `TelegramChannel`)
- DisplayName
- HasError (bool)
- ErrorMessage (последняя ошибка, nullable)
- SettingsJson (настройки подключения)

### 5.3 Кампания (папка)

Кампания объединяет пачку постов и задаёт общие параметры генерации и публикации.
Items (строки данных) принадлежат кампании и разворачиваются в отдельные записи (см. 5.4).
Таргеты выбираются на уровне кампании и применяются ко всем постам кампании. История публикации фиксируется в `Post.PublicationLog` и (опционально) в `PostChannelPublish` (см. 5.10).

#### Параметры кампании (папки), которые задаются в UI

**Базовые**

* `Name` (string, обязателен)
* `Description` (string, опционально)
* Выбор targets в UI
  - Выбранные targets сохраняются в таблицу связи `CampaignTargets` (CampaignId, TargetId).
  - Ограничение: у кампании должен быть минимум 1 target.
* `RequiresModeration` (bool)
  `true` значит пользователь выбирает вариант текста и картинку, и явно утверждает.
  `false` значит допускается автопубликация без ручного утверждения.

**Генерация текста (1 пост = 1 выбранный текст)**

* `Text.VariantsPerPost` (int, >= 1)
  Количество вариантов текста на один пост.
* `Text.Prompt` (string, обязателен)
  Общий промпт кампании, в который подставляется `item`.
* `Text.EditorRulesPrompt` (string, обязателен)
  Промпт для Editor Agent: проверка на стиль, длину, запреты, требования кампании.
* `Text.Llm.Provider` (enum)
* `Text.Llm.Model` (string)
* `Text.Llm.Temperature` (double)
* `Text.Llm.MaxTokens` (int, > 0)

**Генерация изображения (1 пост = 1 выбранная картинка)**

* `Image.VariantsPerPost` (int, >= 1)
  Количество вариантов изображения на один пост.
* `Image.Provider` (enum: `StableDiffusionLocal` | `Azure`)
* `Image.PositivePrompt` (string, обязателен)
* `Image.NegativePrompt` (string, опционально)
* `Image.Options` (string, опционально)
  Строка параметров в формате `key=value; key=value; ...` (это не JSON).

**Расписание публикации (Quartz cron строка)**

* `Schedule.Cron` (string, обязателен)
  Cron в формате Quartz.
* `Schedule.Timezone` (string, обязателен)
  Например `Europe/Madrid`.
* `Schedule.MissedPolicy.CatchUpWithinMinutes` (int, >= 0)
* `Schedule.MissedPolicy.IfMissedLongerThanMinutes` (enum: `SkipAndNotify`)

#### Хранение в БД
Параметры кампании (генерация текста, генерация изображений, расписание) хранятся в отдельных колонках таблицы `Campaigns`.
Связь кампании с таргетами хранится в таблице-связке `CampaignTargets` (many-to-many).

#### Колонки Campaign (DB)

**Campaign (таблица `Campaigns`)**
- Id, TenantId
- Name (string, required)
- Description (string, nullable)
- RequiresModeration (bool)

**Text (колонки)**
- TextVariantsPerPost (int, >= 1)
- TextPrompt (string, required)
- TextEditorRulesPrompt (string, required)
- TextLlmProvider (enum/int, required)
- TextLlmModel (string, required)
- TextLlmTemperature (double/real, required)
- TextLlmMaxTokens (int, > 0, required)

**Image (колонки)**
- ImageVariantsPerPost (int, >= 1)
- ImageProvider (enum/int, required)
- ImagePositivePrompt (string, required)
- ImageNegativePrompt (string, nullable)
- ImageOptions (string, nullable)  (строка `key=value; ...`, не JSON)

**Schedule (колонки)**
- ScheduleCron (string, required)
- ScheduleTimezone (string, required)
- MissedCatchUpWithinMinutes (int, >= 0)
- MissedIfMissedLongerThanMinutes (enum/int, required)

**CampaignTarget (таблица `CampaignTargets`)**
- CampaignId
- TargetId
- Unique индекс (CampaignId, TargetId)


### 5.4 Items (строки данных) и разворачивание

- Пользователь вводит список items в UI как текстовый блок.
- Система разворачивает его в отдельные записи Item.
- Каждая непустая строка это один item.
- Пробелы по краям удаляются.
- Опционально: строки, начинающиеся с `#`, игнорируются.

Поля Item:
- Id, TenantId, CampaignId
- SourceText
- OrderIndex
- CreatedUtc

### 5.5 Посты, варианты и готовность

Пост создаётся на основе item и кампании.

Поля Post:
- Id, TenantId, CampaignId, ItemId
- Status (Created/Generating/Draft/Approved/Scheduled/Published/Failed/Cancelled)
- IsTextReady (bool)
- IsImageReady (bool)
- SelectedTextVariantId (nullable)
- SelectedImageVariantId (nullable)
- UserEditedMarkdown (nullable)
- PublishAtUtc (nullable)
- PublishRollupStatus (None/Partial/Full, вычисляемый)
- PublicationLog (string, nullable, короткий лог публикации: куда и что отправлено)
- LastError (nullable)
- CreatedUtc, UpdatedUtc

Поля PostVariant:
- Id, TenantId, PostId
- MarkdownText
- ModelInfo (имя модели, параметры)
- EditorNotes (замечания редактора, опционально)
- CreatedUtc

Поля ImageVariant:
- Id, TenantId, PostId
- ImageUri (локальный путь или blob)
- PromptUsed
- GeneratorInfo (SD/Azure, параметры)
- CreatedUtc

### 5.6 Публикация по каналам (per target)

Для фиксации результата публикации по каждому выбранному target хранить отдельные записи.

Поля PostChannelPublish:
- Id, TenantId, PostId
- TargetId (nullable, ссылка на Target если существует)
- Status (NotAttempted/Succeeded/Failed)
- PlatformMessageId (nullable)
- PublishedAtUtc (nullable)
- ErrorMessage (nullable)

Правила вычисления PublishRollupStatus:
- None: по всем выбранным каналам Status = NotAttempted
- Full: по всем выбранным каналам Status = Succeeded
- Partial: иначе

### 5.7 Генерация текста: multi-agent (минимум 2 агента) через Microsoft Agent Framework.

Требования:
- C# и Microsoft Agent Framework.
- Минимум 2 агента:
 1) Content Creator Agent: генерирует **один** вариант текста для кампании и item.
 2) Editor Agent: проверяет этот вариант на соответствие требованиям кампании (длина, стиль, запреты) и формирует заметки.

Важно:
- Пара “Creator + Editor” формирует **один** вариант (одну `PostVariant`).
- Если `TextVariantsPerPost > 1`, система повторяет этот двухагентный цикл нужное число раз.
- Editor не выбирает лучший вариант, выбор делает пользователь.

Переходы статусов:
- Created -> Generating при запуске генерации.
- Generating -> Draft при появлении хотя бы одного варианта текста или изображения (и установке флагов готовности).
- Generating -> Failed при ошибке генерации, если не получено ни одного результата.

Флаги готовности:
- IsTextReady = true, если есть хотя бы один PostVariant.
- IsImageReady = true, если есть хотя бы один ImageVariant.

### 5.8 Генерация изображений

Поддержать 2 режима:
1) Stable Diffusion локально (HTTP API).
2) Azure (через соответствующий сервис).

Требования:
- Для каждого Post генерировать M изображений, сохранять как ImageVariant.
- Настройки подключения и ключи должны быть отдельно для каждого tenant (см. 11).
- Параметры на уровне кампании: StylePrompt, NegativePrompt, размер, шаги/CFG (опционально).

Хранение изображений:
- MVP: локальная файловая система.
- Структура папок данных должна быть строго по tenant:
  - `data/{tenantId}/` это корневая папка tenant, внутри только его данные.
  - Например: `data/{tenantId}/images/{postId}/...`.
- Папки разных tenant не пересекаются на уровне папок и не содержат файлов друг друга.
- В `ImageVariant.ImageUri` хранить относительный путь **от корня tenant** (например `images/{postId}/img_01.png`).

### 5.9 Модерация, утверждение, отмена

- Draft: пользователю доступны варианты текста и изображений, выбор “лучшего”.
- Пользователь может редактировать текст в Markdown, после чего сохраняется UserEditedMarkdown.
- Пользователь может:
 - Approve: PostStatus -> Approved
 - Cancel: PostStatus -> Cancelled (пост не публикуется)
 - Schedule: PostStatus -> Scheduled и установка PublishAtUtc
 - Publish now: попытка публикации немедленно

AutoPublish режим:
- После успешной генерации Post автоматически получает Approved и затем Scheduled/Publish now по настройкам кампании.

### 5.10 Неизменяемость опубликованных постов
- После перехода Post в статус **Published** пост считается **замороженным**.
- Изменения в кампании (промпты, стиль, расписание, привязки targets) не должны менять уже опубликованные посты.
- История публикации фиксируется в `Post.PublicationLog` и в полях результатов публикации (например `PlatformMessageId`).

Требования:
- Для каждого опубликованного поста система сохраняет короткий текстовый лог, достаточный для отладки: какие targets использовались, когда, результат, идентификаторы сообщений.
- Если target будет удален или изменен, `PublicationLog` должен позволять понять, куда был опубликован пост.

Разрешенные изменения после Published:
- Допускается добавление служебных полей (например заметка/тег), не влияющих на опубликованный контент.
- Если требуется изменить уже опубликованный контент, это делается как создание нового Post.

---

## 6. Нефункциональные требования (MVP минимально)

- Ожидаемая нагрузка: до 10 tenants, до 1 публикации в день на tenant.
- Приоритет: работоспособность и простота.
- Логи: структурированное логирование (ILogger, опционально Serilog).
- Поведение при рестарте: расписания и БД должны сохраняться между запусками.

---

## 7. Архитектура (MVP)

Рекомендуемая схема:
- Backend: ASP.NET Core (Web API + UI, например Blazor Server или MVC).
- ORM: Entity Framework Core.
- DB: SQLite.
- Планировщик: Quartz.NET.
- LLM: Microsoft Agent Framework.
- Генерация изображений: HTTP клиенты (SD local, Azure).

Логические модули:
1) Tenant Module
2) Targets Module
3) Campaigns Module
4) Posts Module (items, варианты, модерация)
5) Publishing Module (адаптеры платформ)
6) Scheduling Module
7) Content Generation Module (Agent Framework агенты)
8) Demo Seed Module (импорт demo-tenant)

---

## 8. UI (экраны MVP)

### 8.1 Dashboard
- Сводка по tenant: количество постов по статусам.
- Список targets и их состояние (OK/Error).
- Последние ошибки.

### 8.2 Targets
- Список targets.
- Создание/редактирование target:
  - DisplayName
  - SettingsJson (настройки подключения)
  - Кнопка “Test connection” (опционально)
  - Индикация HasError и последней ErrorMessage
  - Кнопка “Reset error” (если HasError = true)


### 8.3 Campaigns (папки)
- Список кампаний.
- Создание/редактирование кампании:
  - Name, Description
  - RequiresModeration
  - Targets: выбор одного или нескольких targets, сохранение в `CampaignTargets` (минимум 1)
  - Text: VariantsPerPost, Prompt, EditorRulesPrompt, Llm.Provider/Model/Temperature/MaxTokens
  - Image: VariantsPerPost, Provider, PositivePrompt, NegativePrompt, Options
  - Schedule: Cron, Timezone, MissedPolicy

### 8.4 Posts
- Список постов в кампании (фильтр по статусам).
- Детальная карточка поста:
 - варианты текста (N) + заметки Editor Agent
 - варианты изображений (M)
 - выбор текста и картинки
 - Markdown editor + preview
 - действия Approve, Schedule, Publish now, Cancel
 - таблица публикаций по каналам (PostChannelPublish) и Rollup статус

---

## 9. Планировщик и расписания

### 9.1 Выбор библиотеки
Для MVP рекомендуется Quartz.NET как популярный планировщик .NET, удобный для interval-триггеров “каждые N дней” и cron.

### 9.2 Формат расписания (cron Quartz)
В MVP расписание задается строкой cron в формате Quartz и таймзоной кампании.

Расписание кампании хранится в колонках `Campaigns`: `ScheduleCron`, `ScheduleTimezone`, `MissedCatchUpWithinMinutes`, `MissedIfMissedLongerThanMinutes`. Quartz job читает эти колонки.

Пример (каждый день в 10:00 по Europe/Madrid):
- ScheduleCron = `0 0 10 ? * * *`
- ScheduleTimezone = `Europe/Madrid`

### 9.3 Поведение при пропусках
- При старте приложения scheduler проверяет посты со статусом Scheduled и PublishAtUtc в прошлом.
- Если просрочка <= CatchUpWithinMinutes, публикацию выполнить сразу.
- Иначе, не публиковать автоматически, пометить как Overdue и показать в UI.

### 9.4 Реализация в Quartz
- Quartz используется как механизм триггеров.
- Источник правды по расписанию кампании: колонки `Campaigns`.
- На кампанию создается Quartz job с cron-триггером из `Campaigns.ScheduleCron` и `Campaigns.ScheduleTimezone`.
- Job выполняет выборку “постов, готовых к публикации” для этой кампании и публикует их.
- Quartz используется как механизм триггеров, “источник правды” по расписанию хранится в нашей БД.


---

## 10. Публикация и адаптеры платформ

### 10.1 Контракт адаптера
Интерфейс:
- `IPublishAdapter.PublishAsync(PublishRequest req, CancellationToken ct)`

PublishRequest:
- TenantId
- TargetId (используется для публикации)
- Text (Markdown)
- ImagePath (nullable)
- Metadata (nullable)

### 10.2 Telegram
- Публикация текста и изображения.
- Формат Markdown/HTML минимальный.

### 10.3 Twitter/X
- Публикация текста и изображения (если доступно).
- Обработка ограничения длины.

### 10.4 WordPress
- Публикация как пост.
- Текст конвертировать в HTML минимально.

### 10.5 Заглушка конвертации Markdown
Слой:
- `IFormattingConverter.Convert(markdown, targetType)`
В MVP допускается простая реализация “минимально совместимого” форматирования.

---

## 11. Конфигурация и секреты

### 11.1 Глобальная конфигурация (ENV)
- Connection string к SQLite (например путь к файлу БД).
- Настройки логирования.
- Настройки LLM провайдера (модель, endpoint) как дефолт.

### 11.2 Секреты tenant
Требование: ключи и endpoint на уровне tenant.

В MVP зафиксировать один из вариантов (допускается поддержать оба):
- Вариант A (предпочтительный с учётом ENV): хранить в БД только ссылки на ENV-переменные, а значения брать из окружения.
 - Пример: `TelegramBotTokenEnv = "TENANT_ACME_TG_TOKEN"`.
- Вариант B: хранить значения в БД, но в зашифрованном виде (ASP.NET Core Data Protection). Ключ защиты хранить в окружении/файле.

UI в варианте A показывает имена ENV-переменных, а не значения.

### 11.3 Demo-tenant для отладки (seed)
- В корне проекта должен лежать файл `demo-tenant.json` с демонстрационными данными одного tenant:
 - tenant
 - targets
 - кампании
 - items
- Файл не должен попадать в GitHub репозиторий. Он добавляется в `.gitignore`.
- При старте приложения:
 - если БД отсутствует и создаётся впервые, система импортирует `demo-tenant.json` в БД
 - если БД уже существует, импорт по умолчанию не выполняется
- Опционально: ENV-флаг `DEMO_TENANT_REIMPORT=true`, который принудительно переимпортирует demo tenant (с удалением только demo tenant данных по TenantId).

---

## 12. База данных (SQLite + EF Core)

### 12.1 Жизненный цикл БД
- База данных не должна пересоздаваться между запусками.
- При старте приложения:
 - если файла БД нет, создать БД и схему (минимально EnsureCreated, либо миграции если появятся позже)
 - если файл БД есть, использовать существующую БД без очистки
- Очистка допускается только как отдельная явная операция разработчика, но не автоматически при старте.

### 12.2 Сущности EF Core
- Tenants
- Targets
- Campaigns
- CampaignTargets
- Items
- Posts
- PostVariants
- ImageVariants
- PostChannelPublishes

Индексы:
- (TenantId, CampaignId) для Items и Posts
- (TenantId, Status) для Posts
- (TenantId, PostId) для PostChannelPublishes
- Unique (CampaignId, TargetId) для CampaignTargets
- (TenantId, Name) для Campaigns (опционально)

Удаление:
- При удалении Post каскадно удалить variants и изображения (файлы).
- При удалении target не удалять историю публикаций. Связанные записи PostChannelPublish сохраняются; при необходимости TargetId может стать NULL. Для понимания истории используется Post.PublicationLog.

---

## 13. Ошибки и устойчивость

- Внешние вызовы (LLM, SD/Azure, платформы) оборачивать в retry policy (Polly), с ограничением попыток.
- Ошибка генерации:
 - PostStatus -> Failed
 - LastError заполнить
- Ошибка публикации:
 - создать/обновить PostChannelPublish.Status = Failed
 - target.HasError = true
 - PostStatus = Failed, если публикация требовала действия пользователя
- Пользователь вручную решает, что делать с Failed постом: перепубликовать, сменить канал, отменить.

---

## 14. Тестирование (минимум для MVP)

Unit:
- разворачивание items из текста
- сборка prompt из кампании + item
- переходы статусов и вычисление rollup
- лог публикации и “заморозка”

Integration (по возможности):
- публикация в тестовый Telegram канал
- генерация в mock режиме (заглушки)

---

## 15. Требования к коду и структуре репозитория

- В `.gitignore` обязателен пункт для `demo-tenant.json`.
- Опционально: исключить локальные данные `data/` (БД и изображения) из репозитория.

Рекомендуемая структура solution:
- `App.Web` (UI + API)
- `App.Domain` (сущности, перечисления, бизнес-правила)
- `App.Infrastructure` (EF Core, адаптеры публикаций, клиенты LLM/Images)
- `App.Jobs` (Quartz jobs)
- `App.Tests`

Ключевые принципы:
- Адаптеры платформ через DI и интерфейсы.
- Чёткие переходы статусов, минимум “магии”.
- Копии для опубликованного контента.

---

## 16. Будущие расширения (out of MVP)
- Instagram адаптер.
- Полноценный конвертер Markdown под платформы.
- RAG (подключение сайта/документов).
- Уведомления (email, webhook).
- История версий постов и soft delete.
- Аналитика и отчёты.

