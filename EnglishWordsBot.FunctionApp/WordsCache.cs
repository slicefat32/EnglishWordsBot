using EnglishWordsBot.DAL.Models;
using EnglishWordsBot.DAL.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishWordsBot.FunctionApp;

public static class WordsCache
{
    private static List<WordInfo> _cachedWords = new();
    private static readonly object _lock = new();

    public static async Task LoadCacheAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<CalendarWordsService>();

        var words = await service.GetAllWords();

        lock (_lock)
        {
            _cachedWords = words;
        }
    }

    public static List<WordInfo> GetAllWords()
    {
        lock (_lock)
        {
            return _cachedWords.ToList();
        }
    }

    public static int GetWordsCount()
    {
        lock (_lock)
        {
            return _cachedWords.Count;
        }
    }

    public static WordInfo? FindByName(string name)
    {
        lock (_lock)
        {
            return _cachedWords.FirstOrDefault(w => w.Name == name);
        }
    }

    public static List<WordInfo> Search(string query)
    {
        lock (_lock)
        {
            return _cachedWords
                .Where(w => w.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }
}
