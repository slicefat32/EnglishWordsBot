using EnglishWordsBot.DAL.Models;
using System;
using Microsoft.EntityFrameworkCore;

namespace EnglishWordsBot.DAL.Services
{
    public class CalendarWordsService
    {
        private readonly EnglishWordsBotDbContext _db;
        public CalendarWordsService(EnglishWordsBotDbContext db) => _db = db;

        public async Task InitializeDatabaseAsync()
        {
            await _db.Database.MigrateAsync();
            var count = await _db.WordsInfo.CountAsync();
            Console.WriteLine($"В базе {count} записей");
        }

        public async Task RunAsync(string path)
        {
            await _db.Database.MigrateAsync();

            var isFileCreated = await _db.WordsInfo.AnyAsync();
            if (!isFileCreated)
            {
                var filesMetaData = GetFilesMetaData(path);
                foreach (var fileMetaData in filesMetaData)
                {
                    var filePath = Path.Combine(path, fileMetaData.Item1);
                    byte[]? fileData = null;

                    if (File.Exists(filePath))
                    {
                        fileData = await File.ReadAllBytesAsync(filePath);
                    }

                    await _db.WordsInfo.AddAsync(new WordInfo()
                    {
                        Name = fileMetaData.Item1,
                        CreateDate = fileMetaData.Item2,
                        FileData = fileData
                    });
                }
                await _db.SaveChangesAsync();
            }

            var all = _db.WordsInfo.ToList();
            Console.WriteLine($"В базе {all.Count} записей");
        }


        public async Task CreateWord(string name)
        {
            var word = new WordInfo()
            {
                Name = name,
                CreateDate = DateOnly.FromDateTime(DateTime.Now)
            };

            await _db.WordsInfo.AddAsync(word);
            await _db.IntervalWordRepeatInfo.AddAsync(new IntervalWordRepeatInfo
            {
                WordInfo = word,
                Repeatednterval = Repeatednterval.None
            });

            await _db.SaveChangesAsync();
        }

        public async Task CreateWord(string name, string filePath)
        {
            byte[]? fileData = null;
            if (File.Exists(filePath))
            {
                fileData = await File.ReadAllBytesAsync(filePath);
            }

            var word = new WordInfo()
            {
                Name = name,
                CreateDate = DateOnly.FromDateTime(DateTime.Now),
                FileData = fileData
            };

            await _db.WordsInfo.AddAsync(word);
            await _db.IntervalWordRepeatInfo.AddAsync(new IntervalWordRepeatInfo
            {
                WordInfo = word,
                Repeatednterval = Repeatednterval.None
            });

            await _db.SaveChangesAsync();
        }

        public async Task CreateWord(string name, byte[] fileData)
        {
            var word = new WordInfo()
            {
                Name = name,
                CreateDate = DateOnly.FromDateTime(DateTime.Now),
                FileData = fileData
            };

            await _db.WordsInfo.AddAsync(word);
            await _db.IntervalWordRepeatInfo.AddAsync(new IntervalWordRepeatInfo
            {
                WordInfo = word,
                Repeatednterval = Repeatednterval.None
            });

            await _db.SaveChangesAsync();
        }

        public async Task<List<WordInfo>> GetAllWords()
        {
            return await _db.WordsInfo.ToListAsync();
        }

        public async Task<List<WordInfo>> FindWords(string search)
        {
            var result = await _db.WordsInfo
                .Where(x => x.Name.ToLower().Contains(search.ToLower()))
                .ToListAsync();

            return result;
        }

        public async Task<List<WordInfo>> GetWordsBy(DateOnly startDate, DateOnly endDate)
        {
            var result = await _db.WordsInfo
                .Where(x => x.CreateDate>= startDate && x.CreateDate <= endDate)
                .ToListAsync();
            return result;
        }


        private List<Tuple<string,DateOnly>> GetFilesMetaData(string folderPath)
        {
            string[] files = Directory.GetFiles(folderPath);
            var filesMetaData = new List<Tuple<string, DateOnly>>();
            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                DateTime created = File.GetCreationTime(file);
                DateOnly createdDate = DateOnly.FromDateTime(created);

               filesMetaData.Add(new Tuple<string, DateOnly>(fileName,createdDate));
            }

            return filesMetaData;
        }
    }
}
