# Что это за задание №2

## Цель

Обновить экран **CreateTarget** так, чтобы при выборе типа таргета **TelegramChannel** в поле **Settings JSON** автоматически подставлялся пустой JSON-шаблон (template) с заготовкой настроек для Telegram.

Код-пример из другого приложения использовать только как ориентир для определения параметров, которые реально требуются Telegram API при отправке сообщений (и фото).

---

## Какие параметры реально нужны Telegram для публикации

### Обязательные

1. **Bot token**
   - Токен Telegram-бота (используется при формировании URL вида `https://api.telegram.org/bot{token}/...`).

2. **chat_id**
   - В примере: `const string chatId = "@RusEngMix"`.
   - Может быть:
     - `@channelusername` для публичного канала;
     - числовой id (часто выглядит как `-100...`) для групп/супергрупп/приватных каналов.
   - Для канала бот должен быть добавлен в администраторы и иметь право публиковать.

---

## Поля в Settings JSON (TelegramChannel)

### Обязательные (MVP)

- `botToken`
- `chatId`

### Дополнительные поля (по умолчанию `false`)

- `disableWebPagePreview`
- `disableNotification`
- `protectContent`

---

## Шаблон JSON, который должен появляться в CreateTarget при выборе TelegramChannel

Ниже пример template. Он «пустой», но валидный JSON и показывает ключи, которые понадобятся при отправке:

```json
{
  "telegram": {
    "chatId": "@YourChannelOrChat",
    "botToken": "",

    "disableWebPagePreview": false,
    "disableNotification": false,
    "protectContent": false
  }
}
```
