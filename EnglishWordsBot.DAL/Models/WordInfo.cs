namespace EnglishWordsBot.DAL.Models;

public sealed class WordInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public DateOnly CreateDate { get; set; }
    public byte[]? FileData { get; set; }
}