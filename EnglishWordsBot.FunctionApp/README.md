# EnglishWordsBot Azure Function App

Telegram бот для вивчення англійських слів, розгорнутий як Azure Function App.

## 📋 Структура проекту

```
EnglishWordsBot.FunctionApp/
├── BotUpdateHandler.cs       - HTTP Trigger для обробки Telegram webhook
├── AutoSendFunction.cs       - Timer Trigger (автоматична розсилка кожні 2 години)
├── SetWebhookFunction.cs     - Функції для налаштування webhook
├── Words.cs                  - Утиліти для компресії зображень
├── WordsCache.cs            - Кеш для швидкого доступу до слів
├── BotHelpers.cs            - Допоміжні методи бота
└── Program.cs               - Налаштування DI та ініціалізація
```

## 🚀 Деплой

### 1. Створіть Azure Resources

```bash
# Створіть Resource Group
az group create --name EnglishWordsBotRG --location "West Europe"

# Створіть Storage Account
az storage account create --name englishwordsstorage --resource-group EnglishWordsBotRG --location "West Europe" --sku Standard_LRS

# Створіть Function App (.NET 10 Isolated)
az functionapp create --resource-group EnglishWordsBotRG --consumption-plan-location "West Europe" --runtime dotnet-isolated --runtime-version 10 --functions-version 4 --name EnglishWordsBotFunc --storage-account englishwordsstorage
```

### 2. Налаштуйте Application Settings

```bash
# Connection String для SQL Server
az functionapp config appsettings set --name EnglishWordsBotFunc --resource-group EnglishWordsBotRG --settings "ConnectionStrings__DefaultConnection=<your-sql-connection-string>"

# Telegram Bot Token
az functionapp config appsettings set --name EnglishWordsBotFunc --resource-group EnglishWordsBotRG --settings "TelegramBotToken=<your-bot-token>"

# Webhook URL (URL вашого Function App)
az functionapp config appsettings set --name EnglishWordsBotFunc --resource-group EnglishWordsBotRG --settings "WebhookUrl=https://EnglishWordsBotFunc.azurewebsites.net"
```

### 3. Деплой через Visual Studio

1. Клік правою кнопкою на проекті `EnglishWordsBot.FunctionApp`
2. Виберіть **Publish...**
3. Виберіть **Azure** → **Azure Function App (Windows)**
4. Виберіть створений Function App
5. Натисніть **Publish**

### 4. Налаштуйте Webhook

После деплою виконайте один з варіантів:

#### Варіант A: Через Function

Відкрийте в браузері:
```
https://EnglishWordsBotFunc.azurewebsites.net/api/SetWebhook?url=https://EnglishWordsBotFunc.azurewebsites.net
```

#### Варіант B: Через Telegram API

```bash
curl -X POST "https://api.telegram.org/bot<YOUR_BOT_TOKEN>/setWebhook?url=https://EnglishWordsBotFunc.azurewebsites.net/api/BotUpdate"
```

### 5. Перевірте статус Webhook

```bash
# Через Function
https://EnglishWordsBotFunc.azurewebsites.net/api/GetWebhookInfo

# Або через Telegram API
curl "https://api.telegram.org/bot<YOUR_BOT_TOKEN>/getWebhookInfo"
```

## 🔧 Локальна розробка

### Налаштування

1. Оновіть `local.settings.json`:
```json
{
  "Values": {
	"ConnectionStrings:DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=EnglishWordsBot;Trusted_Connection=True",
	"TelegramBotToken": "YOUR_BOT_TOKEN",
	"WebhookUrl": "https://your-ngrok-url.ngrok.io"
  }
}
```

2. Для локального тестування використовуйте **ngrok**:
```bash
ngrok http 7071
```

3. Налаштуйте webhook на ngrok URL:
```bash
curl -X POST "https://api.telegram.org/botYOUR_TOKEN/setWebhook?url=https://your-ngrok-url.ngrok.io/api/BotUpdate"
```

4. Запустіть Functions:
```bash
func start
```

## 📝 API Endpoints

### BotUpdate (POST)
- **URL**: `/api/BotUpdate`
- **Auth**: Function Level
- **Опис**: Обробка webhook від Telegram

### SetWebhook (GET/POST)
- **URL**: `/api/SetWebhook?url=<webhook-url>`
- **Auth**: Function Level
- **Опис**: Налаштування webhook для бота

### GetWebhookInfo (GET)
- **URL**: `/api/GetWebhookInfo`
- **Auth**: Function Level
- **Опис**: Отримання інформації про поточний webhook

### DeleteWebhook (POST/DELETE)
- **URL**: `/api/DeleteWebhook`
- **Auth**: Function Level
- **Опис**: Видалення webhook (для перемикання на long polling)

### AutoSendWords (Timer)
- **Schedule**: `0 */2 * * *` (кожні 2 години)
- **Опис**: Автоматична розсилка слів користувачам

## 🔐 Security

**ВАЖЛИВО**: Не комітьте `local.settings.json` з реальними токенами!

Додайте в `.gitignore`:
```
local.settings.json
```

Для Production використовуйте Azure App Settings або Azure Key Vault.

## 📊 Моніторинг

### Application Insights

Перегляд логів:
```bash
az monitor app-insights query --app <app-insights-name> --analytics-query "traces | where message contains 'Bot' | limit 50"
```

### Перегляд логів у реальному часі

```bash
func azure functionapp logstream EnglishWordsBotFunc
```

## 🐛 Troubleshooting

### Webhook не працює
1. Перевірте, чи правильно налаштований webhook:
   ```
   GET /api/GetWebhookInfo
   ```

2. Перевірте логи:
   ```bash
   az webapp log tail --name EnglishWordsBotFunc --resource-group EnglishWordsBotRG
   ```

### База даних недоступна
Перевірте Connection String та firewall rules SQL Server.

### Timer не запускається
Переконайтесь, що `AzureWebJobsStorage` правильно налаштований.

## 📚 Додаткові ресурси

- [Azure Functions Documentation](https://docs.microsoft.com/azure/azure-functions/)
- [Telegram Bot API](https://core.telegram.org/bots/api)
- [.NET Isolated Worker Model](https://docs.microsoft.com/azure/azure-functions/dotnet-isolated-process-guide)
