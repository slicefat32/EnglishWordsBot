using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace EnglishWordsBot.FunctionApp;

public class AutoSendFunction
{
    private readonly ILogger<AutoSendFunction> _logger;
    private readonly TelegramBotClient _botClient;

    public AutoSendFunction(ILogger<AutoSendFunction> logger, TelegramBotClient botClient)
    {
        _logger = logger;
        _botClient = botClient;
    }

    // Runs every 2 hours: 0 */2 * * *
    [Function("AutoSendWords")]
    public async Task Run([TimerTrigger("0 */2 * * *")] MyInfo myTimer)
    {
        _logger.LogInformation("Auto-send timer triggered at: {time}", DateTime.Now);

        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next timer schedule at: {next}", myTimer.ScheduleStatus.Next);
        }

        foreach (var user in Words.userProgress.Keys)
        {
            try
            {
                await BotHelpers.SendNextWord(_botClient, user);
                _logger.LogInformation("Sent word to user: {userId}", user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send word to user: {userId}", user);
            }
        }

        _logger.LogInformation("Auto-send completed");
    }
}

public class MyInfo
{
    public MyScheduleStatus? ScheduleStatus { get; set; }

    public bool IsPastDue { get; set; }
}

public class MyScheduleStatus
{
    public DateTime Last { get; set; }

    public DateTime Next { get; set; }

    public DateTime LastUpdated { get; set; }
}
