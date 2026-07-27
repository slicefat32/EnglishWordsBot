// // EnglishWordsBot - IntervalWordRepeatInfo.cs
// // Copyright (c) 2025 All Rights Reserved
// // Datascope, Aleksandr Marchenko
// // someobj@gmail.com

namespace EnglishWordsBot.DAL.Models;

public class IntervalWordRepeatInfo
{
    public WordInfo WordInfo { get; set; }
    public int WordInfoId { get; set; }
    public Repeatednterval Repeatednterval { get; set; }

}


[Flags]
public enum Repeatednterval
{
    None = 0,
    AfterDay = 1 << 0, // 1
    AfterThreeDays = 1 << 1, // 2
    AfterWeek = 1 << 2, // 4
    AfterMonth = 1 << 3, // 8
}