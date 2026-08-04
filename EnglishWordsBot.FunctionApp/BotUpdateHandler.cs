using EnglishWordsBot.DAL.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace EnglishWordsBot.FunctionApp;

public class BotUpdateHandler
{
    private readonly ILogger<BotUpdateHandler> _logger;
    private readonly TelegramBotClient _botClient;
    private readonly CalendarWordsService _calendarWordsService;
    private readonly IServiceProvider _serviceProvider;
    private static readonly ConcurrentDictionary<long, ChatState> States = new();

    // JsonSerializerOptions для Telegram.Bot
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public BotUpdateHandler(
        ILogger<BotUpdateHandler> logger,
        TelegramBotClient botClient,
        CalendarWordsService calendarWordsService,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _botClient = botClient;
        _calendarWordsService = calendarWordsService;
        _serviceProvider = serviceProvider;
    }

    [Function("BotUpdate")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        _logger.LogInformation("TELEGRAMBOT UPDATE");
        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            _logger.LogInformation("BODY: {Body}", requestBody);

            var update = JsonSerializer.Deserialize<Update>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (update == null)
            {
                _logger.LogWarning("Update is null after deserialization");
                return new BadRequestResult();
            }

            _logger.LogInformation("Update Type: {Type}, UpdateId: {Id}", update.Type, update.Id);

            await HandleUpdate(update);
            return new OkResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing update");
            return new StatusCodeResult(500);
        }
    }

    private async Task HandleUpdate(Update update)
    {
        _logger.LogInformation("UpdateType: {Type}", update.Type);
        if (update.Type == UpdateType.Message)
        {
            _logger.LogInformation("Text: {Text}", update.Message?.Text);
            await HandleMessage(update.Message!);
        }
        else if (update.Type == UpdateType.CallbackQuery)
        {
            _logger.LogInformation("Callback: {Data}", update.CallbackQuery?.Data);
            await HandleCallbackQuery(update.CallbackQuery!);
        }
    }

    private async Task HandleMessage(Message message)
    {
        _logger.LogInformation("HandleMessage chatId={ChatId}", message.Chat.Id);
        var chatId = message.Chat.Id;
        var text = (message.Text ?? "").Trim();

        // Photo upload
        if (message.Photo != null && message.Photo.Any())
        {
            var photo = message.Photo.Last();
            var file = await _botClient.GetFile(photo.FileId);

            using var ms = new MemoryStream();
            await _botClient.DownloadFile(file.FilePath!, ms);
            var imageData = ms.ToArray();

            var compressedData = await Words.CompressImageData(imageData, 200 * 1024);
            Words.pendingImages[chatId] = compressedData;

            await _botClient.SendMessage(chatId, "Отправь название для этой картинки 🏷️");
            return;
        }

        // Image name after upload
        if (Words.pendingImages.TryGetValue(chatId, out var image))
        {
            var safeName = string.Join("_", message.Text!.Split(Path.GetInvalidFileNameChars()));
            var fileName = $"{safeName}.jpg";

            Words.pendingImages.TryRemove(chatId, out _);

            await _calendarWordsService.CreateWord(fileName, image);
            await WordsCache.LoadCacheAsync(_serviceProvider);

            await _botClient.SendMessage(chatId, $"Картинка сохранена как: {fileName} ✅");
            return;
        }

        // Upload button
        if (text == "📤 Загрузить картинку")
        {
            await _botClient.SendMessage(chatId, "Пожалуйста, отправь изображение.");
            return;
        }

        // /start command
        if (text.Equals("/start", StringComparison.OrdinalIgnoreCase))
        {
            await _botClient.SendMessage(
                chatId,
                "Привет! Нажмите «🔍 Поиск», чтобы искать, «➡ Следующее слово» — для карточки, или «📤 Загрузить картинку».",
                replyMarkup: GetMainKeyboard());
            return;
        }

        // Search button
        if (text == "🔍 Поиск")
        {
            States[chatId] = ChatState.WaitingForQuery;
            await _botClient.SendMessage(
                chatId,
                "Введите поисковый запрос (например: *apple*, *transport*, *lesson 12*):",
                replyMarkup: GetMainKeyboard());
            return;
        }

        // Next word button
        if (text == "➡ Следующее слово")
        {
            await BotHelpers.SendNextWord(_botClient, chatId);
            return;
        }

        // Cancel button
        if (text == "Отмена")
        {
            States[chatId] = ChatState.Idle;
            await _botClient.SendMessage(chatId, "Ок, отменил. Я на готове.", replyMarkup: GetMainKeyboard());
            return;
        }

        // Default
        await _botClient.SendMessage(
            chatId,
            "Нажмите «➡ Следующее слово» для карточки, «🔍 Поиск» — чтобы искать в базе, или «📤 Загрузить картинку».",
            replyMarkup: GetMainKeyboard());
    }

    private async Task HandleCallbackQuery(CallbackQuery callback)
    {
        var chatId = callback.Message!.Chat.Id;

        if (callback.Data == "next")
        {
            await BotHelpers.SendNextWord(_botClient, chatId);
            await _botClient.AnswerCallbackQuery(callback.Id);
        }
    }

    private static ReplyKeyboardMarkup GetMainKeyboard() => new(new[]
    {
        new KeyboardButton[] { "➡ Следующее слово", "🔍 Поиск" },
        new KeyboardButton[] { "📤 Загрузить картинку", "Отмена" }
    })
    {
        ResizeKeyboard = true,
        OneTimeKeyboard = false
    };
}

public enum ChatState { Idle, WaitingForQuery }
