using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using EnglishWordsBot.DAL;
using EnglishWordsBot.DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnglishWordsBot.FunctionApp;

public static class Words
{
    public static ConcurrentDictionary<long, List<string>> userProgress = new();
    public static ConcurrentDictionary<long, byte[]> pendingImages = new();

    public static async Task ResaveBlobImagesFromFolder(IServiceProvider serviceProvider, string sourceFolderPath, int maxSizeKb = 200)
    {
        int maxSizeBytes = maxSizeKb * 1024;

        if (!Directory.Exists(sourceFolderPath))
        {
            throw new DirectoryNotFoundException($"Source folder not found: {sourceFolderPath}");
        }

        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<EnglishWordsBotDbContext>();

        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
        var imageFiles = Directory.GetFiles(sourceFolderPath)
            .Where(file => imageExtensions.Contains(Path.GetExtension(file).ToLower()))
            .ToList();

        Console.WriteLine($"Found {imageFiles.Count} images to process...");

        int updated = 0;
        int created = 0;

        foreach (var imagePath in imageFiles)
        {
            try
            {
                var fileName = Path.GetFileName(imagePath);
                var existingWord = await dbContext.WordsInfo.FirstOrDefaultAsync(w => w.Name == fileName);

                var compressedData = await CompressImageToMaxSize(imagePath, maxSizeBytes);

                if (existingWord == null)
                {
                    var newWord = new WordInfo
                    {
                        Name = fileName,
                        CreateDate = DateOnly.FromDateTime(File.GetCreationTime(imagePath)),
                        FileData = compressedData
                    };

                    await dbContext.WordsInfo.AddAsync(newWord);
                    Console.WriteLine($"Created: {fileName} ({compressedData.Length / 1024} KB)");
                    created++;
                }
                else
                {
                    existingWord.FileData = compressedData;
                    Console.WriteLine($"Updated: {fileName} ({compressedData.Length / 1024} KB)");
                    updated++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing {Path.GetFileName(imagePath)}: {ex.Message}");
            }
        }

        await dbContext.SaveChangesAsync();
        Console.WriteLine($"Resave completed! Updated: {updated}, Created: {created}");

        await WordsCache.LoadCacheAsync(serviceProvider);
        Console.WriteLine("Cache reloaded!");
    }

    public static async Task SaveCompressedImagesFromFolder(string sourceFolderPath, string destinationFolderPath, int maxSizeKb = 200)
    {
        int maxSizeBytes = maxSizeKb * 1024;

        if (!Directory.Exists(sourceFolderPath))
        {
            throw new DirectoryNotFoundException($"Source folder not found: {sourceFolderPath}");
        }

        if (!Directory.Exists(destinationFolderPath))
        {
            Directory.CreateDirectory(destinationFolderPath);
        }

        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
        var imageFiles = Directory.GetFiles(sourceFolderPath)
            .Where(file => imageExtensions.Contains(Path.GetExtension(file).ToLower()))
            .ToList();

        Console.WriteLine($"Found {imageFiles.Count} images to compress...");

        foreach (var imagePath in imageFiles)
        {
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(imagePath);
                var destinationPath = Path.Combine(destinationFolderPath, $"{fileName}.jpg");

                var compressedData = await CompressImageToMaxSize(imagePath, maxSizeBytes);
                await File.WriteAllBytesAsync(destinationPath, compressedData);

                Console.WriteLine($"Compressed: {Path.GetFileName(imagePath)} ({compressedData.Length / 1024} KB)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error compressing {Path.GetFileName(imagePath)}: {ex.Message}");
            }
        }

        Console.WriteLine("Compression completed!");
    }

    private static async Task<byte[]> CompressImageToMaxSize(string imagePath, int maxSizeBytes)
    {
        return await Task.Run(() =>
        {
            using var originalImage = System.Drawing.Image.FromFile(imagePath);

            long quality = 95L;
            long minQuality = 10L;
            int width = originalImage.Width;
            int height = originalImage.Height;

            while (true)
            {
                using var bitmap = new Bitmap(width, height);
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    graphics.DrawImage(originalImage, 0, 0, width, height);
                }

                using var ms = new MemoryStream();
                var encoderParameters = new EncoderParameters(1);
                encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, quality);
                var jpegCodec = GetEncoderInfo("image/jpeg");

                bitmap.Save(ms, jpegCodec, encoderParameters);
                var imageData = ms.ToArray();

                if (imageData.Length <= maxSizeBytes)
                {
                    return imageData;
                }

                if (quality > minQuality)
                {
                    quality -= 5L;
                }
                else
                {
                    width = (int)(width * 0.9);
                    height = (int)(height * 0.9);
                    quality = 85L;

                    if (width < 100 || height < 100)
                    {
                        return imageData;
                    }
                }
            }
        });
    }

    private static ImageCodecInfo GetEncoderInfo(string mimeType)
    {
        var encoders = ImageCodecInfo.GetImageEncoders();
        return encoders.FirstOrDefault(e => e.MimeType == mimeType)!;
    }

    public static async Task<byte[]> CompressImageData(byte[] imageData, int maxSizeBytes)
    {
        return await Task.Run(() =>
        {
            using var ms = new MemoryStream(imageData);
            using var originalImage = System.Drawing.Image.FromStream(ms);

            long quality = 95L;
            long minQuality = 10L;
            int width = originalImage.Width;
            int height = originalImage.Height;

            while (true)
            {
                using var bitmap = new Bitmap(width, height);
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    graphics.DrawImage(originalImage, 0, 0, width, height);
                }

                using var outputMs = new MemoryStream();
                var encoderParameters = new EncoderParameters(1);
                encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, quality);
                var jpegCodec = GetEncoderInfo("image/jpeg");

                bitmap.Save(outputMs, jpegCodec, encoderParameters);
                var compressedData = outputMs.ToArray();

                if (compressedData.Length <= maxSizeBytes)
                {
                    return compressedData;
                }

                if (quality > minQuality)
                {
                    quality -= 5L;
                }
                else
                {
                    width = (int)(width * 0.9);
                    height = (int)(height * 0.9);
                    quality = 85L;

                    if (width < 100 || height < 100)
                    {
                        return compressedData;
                    }
                }
            }
        });
    }
}
