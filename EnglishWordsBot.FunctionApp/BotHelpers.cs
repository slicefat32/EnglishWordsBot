using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace EnglishWordsBot.FunctionApp;

public static class BotHelpers
{
    public static async Task SendNextWord(ITelegramBotClient bot, long chatId)
    {
        var userProgress = Words.userProgress.GetOrAdd(chatId, _ => new List<string>());

        var allWords = WordsCache.GetAllWords();
        var availableWords = allWords
            .Where(w => !userProgress.Contains(w.Name))
            .ToList();

        if (availableWords.Count == 0)
        {
            Words.userProgress[chatId] = new List<string>();
            availableWords = allWords.ToList();

            await bot.SendMessage(
                chatId: chatId,
                text: "Все слова повторены! Начинаем сначала."
            );
        }

        if (availableWords.Count == 0)
        {
            await bot.SendMessage(chatId, "В базе данных нет слов.");
            return;
        }

        var random = new Random();
        var selectedWord = availableWords[random.Next(availableWords.Count)];

        userProgress.Add(selectedWord.Name);

        if (selectedWord.FileData != null && selectedWord.FileData.Length > 0)
        {
            using var stream = new MemoryStream(selectedWord.FileData);
            await bot.SendPhoto(
                chatId: chatId,
                photo: new InputFileStream(stream, selectedWord.Name),
                caption: $"Повторим слово: {Path.GetFileNameWithoutExtension(selectedWord.Name)}",
                replyMarkup: new InlineKeyboardMarkup(
                    InlineKeyboardButton.WithCallbackData("➡ Следующее слово", "next")
                )
            );
        }
        else
        {
            await bot.SendMessage(chatId, $"Слово: {Path.GetFileNameWithoutExtension(selectedWord.Name)}\n(Изображение не загружено)");
        }
    }
}
