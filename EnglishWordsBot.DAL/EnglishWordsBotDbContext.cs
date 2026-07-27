using EnglishWordsBot.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace EnglishWordsBot.DAL;

public sealed class EnglishWordsBotDbContext(DbContextOptions<EnglishWordsBotDbContext> options) : DbContext(options)
{
    public DbSet<WordInfo> WordsInfo => Set<WordInfo>();
    public DbSet<IntervalWordRepeatInfo> IntervalWordRepeatInfo => Set<IntervalWordRepeatInfo>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WordInfo>(entity =>
        {
            entity.ToTable("WordsInfo"); // имя таблицы строго как просили
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            // DateOnly -> SQL type DATE
            entity.Property(e => e.CreateDate)
                .HasColumnType("date");

            entity.Property(e => e.FileData)
                .HasColumnType("varbinary(max)");
        });

        modelBuilder.Entity<IntervalWordRepeatInfo>(e =>
        {
            e.HasKey(x => x.WordInfoId);
            e.ToTable("IntervalWordRepeatInfos");
            e.HasOne(x => x.WordInfo)
                .WithOne().HasForeignKey<IntervalWordRepeatInfo>(x => x.WordInfoId);
            e.Property(x => x.Repeatednterval)
                .HasConversion<int>()
                .HasColumnType("int")
                .IsRequired();
            e.HasIndex(x => x.Repeatednterval);
        });
    }
}