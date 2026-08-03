using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace EnglishWordsBot.FunctionApp;

public class SetWebhookFunction
{
    private readonly ILogger<SetWebhookFunction> _logger;
    private readonly TelegramBotClient _botClient;

    public SetWebhookFunction(
        ILogger<SetWebhookFunction> logger,
        TelegramBotClient botClient)
    {
        _logger = logger;
        _botClient = botClient;
    }

    [Function("SetWebhook")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    {
        try
        {
            // Автоматично визначаємо базовий URL з поточного запиту
            var baseUrl = $"{req.Scheme}://{req.Host}";
            var webhookUrl = $"{baseUrl}/api/BotUpdate";

            // Або можна вказати через query parameter
            var customUrl = req.Query["url"].ToString();
            if (!string.IsNullOrEmpty(customUrl))
            {
                webhookUrl = customUrl.EndsWith("/api/BotUpdate") 
                    ? customUrl 
                    : customUrl.TrimEnd('/') + "/api/BotUpdate";
            }

            // Встановлюємо webhook
            await _botClient.SetWebhook(
                url: webhookUrl,
                allowedUpdates: new[]
                {
                    UpdateType.Message,
                    UpdateType.CallbackQuery
                });

            _logger.LogInformation("Webhook set successfully to: {url}", webhookUrl);

            var webhookInfo = await _botClient.GetWebhookInfo();

            return new OkObjectResult(new
            {
                success = true,
                message = "Webhook set successfully",
                webhookUrl = webhookInfo.Url,
                pendingUpdateCount = webhookInfo.PendingUpdateCount,
                lastErrorDate = webhookInfo.LastErrorDate,
                lastErrorMessage = webhookInfo.LastErrorMessage
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting webhook");
            return new ObjectResult(new
            {
                success = false,
                error = ex.Message
            })
            {
                StatusCode = 500
            };
        }
    }

    [Function("GetWebhookInfo")]
    public async Task<IActionResult> GetInfo(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
    {
        try
        {
            var webhookInfo = await _botClient.GetWebhookInfo();

            return new OkObjectResult(new
            {
                url = webhookInfo.Url,
                hasCustomCertificate = webhookInfo.HasCustomCertificate,
                pendingUpdateCount = webhookInfo.PendingUpdateCount,
                lastErrorDate = webhookInfo.LastErrorDate,
                lastErrorMessage = webhookInfo.LastErrorMessage,
                maxConnections = webhookInfo.MaxConnections,
                allowedUpdates = webhookInfo.AllowedUpdates
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting webhook info");
            return new ObjectResult(new { error = ex.Message }) { StatusCode = 500 };
        }
    }

    [Function("DeleteWebhook")]
    public async Task<IActionResult> Delete(
        [HttpTrigger(AuthorizationLevel.Function, "post", "delete")] HttpRequest req)
    {
        try
        {
            await _botClient.DeleteWebhook();
            _logger.LogInformation("Webhook deleted successfully");

            return new OkObjectResult(new
            {
                success = true,
                message = "Webhook deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting webhook");
            return new ObjectResult(new { error = ex.Message }) { StatusCode = 500 };
        }
    }


    [Function("GetBotInfo")]
    public async Task<IActionResult> GetBotInfo(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
    {
        var me = await _botClient.GetMe();

        return new OkObjectResult(new
        {
            me.Id,
            me.Username,
            me.FirstName
        });
    }
}
