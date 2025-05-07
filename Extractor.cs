using XCDESave;

namespace xcde_save_manager;

public class Extractor
{
    public enum SaveFileChapter
    {
        One,
        Two,
        Three,
        Four,
        Five,
        Six,
        Seven,
        Eight,
        Nine,
        Ten,
        Eleven,
        Twelve,
        Thirteen,
        Fourteen,
        Fifteen,
        Sixteen,
        Seventeen,
        /// <summary>
        ///  This shouldn't ever be read from a valid save file (i.e. non-tampered)
        ///  but I am including it for the sake of being able to handle tampered files.
        /// </summary>
        BeyondSeventeen
    }

    public static SaveFileChapter getSaveFileChapter(XCDESaveData save)
    {
        ushort storyCheckpointCounter = BitConverter.ToUInt16(save.ToRawData(), 3568);

        SaveFileChapter chapter = storyCheckpointCounter switch
        {
            >= 0 and <= 21 => SaveFileChapter.One,
            >= 22 and <= 41 => SaveFileChapter.Two,
            >= 42 and <= 56 => SaveFileChapter.Three,
            >= 57 and <= 79 => SaveFileChapter.Four,
            >= 80 and <= 101 => SaveFileChapter.Five,
            >= 102 and <= 114 => SaveFileChapter.Six,
            >= 115 and <= 147 => SaveFileChapter.Seven,
            >= 148 and <= 184 => SaveFileChapter.Eight,
            >= 185 and <= 215 => SaveFileChapter.Nine,
            >= 216 and <= 239 => SaveFileChapter.Ten,
            >= 240 and <= 266 => SaveFileChapter.Eleven,
            >= 267 and <= 292 => SaveFileChapter.Twelve,
            >= 293 and <= 314 => SaveFileChapter.Thirteen,
            >= 315 and <= 338 => SaveFileChapter.Fourteen,
            >= 339 and <= 356 => SaveFileChapter.Fifteen,
            >= 357 and <= 365 => SaveFileChapter.Sixteen,
            >= 366 and <= 404 => SaveFileChapter.Seventeen,
            _ => SaveFileChapter.BeyondSeventeen,
        };

        return chapter;
    }

    public static string getSaveFileChapterDisplayName(SaveFileChapter chapter)
    {
        string stringifiedChapterNumber = chapter switch
        {
            SaveFileChapter.One => "Chapter One",
            SaveFileChapter.Two => "Chapter Two",
            SaveFileChapter.Three => "Chapter Three",
            SaveFileChapter.Four => "Chapter Four",
            SaveFileChapter.Five => "Chapter Five",
            SaveFileChapter.Six => "Chapter Six",
            SaveFileChapter.Seven => "Chapter Seven",
            SaveFileChapter.Eight => "Chapter Eight",
            SaveFileChapter.Nine => "Chapter Nine",
            SaveFileChapter.Ten => "Chapter Ten",
            SaveFileChapter.Eleven => "Chapter Eleven",
            SaveFileChapter.Twelve => "Chapter Twelve",
            SaveFileChapter.Thirteen => "Chapter Thirteen",
            SaveFileChapter.Fourteen => "Chapter Fourteen",
            SaveFileChapter.Fifteen => "Chapter Fifteen",
            SaveFileChapter.Sixteen => "Chapter Sixteen",
            SaveFileChapter.Seventeen => "Chapter Seventeen",
            SaveFileChapter.BeyondSeventeen => "",
            _ => "Unknown"
        };

        return stringifiedChapterNumber;
    }

    public static DateTime getSaveFileWriteTime(XCDESaveData save)
    {
        byte[] saveFileBytes = save.ToRawData();
        ushort bytes_containing_year = BitConverter.ToUInt16(saveFileBytes, 14);
        // 0x1FA0 -> 0001 1111 1010 0000.
        //           ^^^^^^^^^^^^^^^^^ these bits are the year
        ushort year = bytes_containing_year >>= 2;


        byte byte13 = saveFileBytes[13];
        byte byte14 = saveFileBytes[14];
        // 0xA0CB -> 1010 0000 1100 1011
        //                  ^^ ^^ these bits are the month
        //           ^^^^^^^^^ byte 14
        //                     ^^^^^^^^^ byte 13
        byte byte13Mask = 0b11000000;
        byte byte14Mask = 0b00000011;
        int month = ((byte13 & byte13Mask) >> 6) | ((byte14 & byte14Mask) << 2);

        // TODO: Figure out what the rest of the bits in byte 13 are doing
        //byte mystery_bits = (byte) (byte13 & (byte)~byte13Mask);
        //Console.WriteLine("Mystery month-adjacent bits: {0}", Convert.ToString(mystery_bits, 2));

        byte byte12 = saveFileBytes[12];
        // 0x3E -> 0011 1110
        //         ^^^^^^^^^ byte 12
        //            ^ ^^^^ these bits are the day
        byte byte12Mask = 0b00011111;
        int day = byte12 & byte12Mask;

        // TODO: Figure out what the rest of the bits in byte 12 are doing
        //byte mystery_bits2 = (byte)(byte12 & (byte)~byte12Mask);
        //Console.WriteLine("Mystery day-adjacent bits: {0}", Convert.ToString(mystery_bits2, 2));

        byte byte11 = saveFileBytes[11];
        byte byte10 = saveFileBytes[10];
        // 0x0BBA -> 0000 1011 1011 1010
        //           ^^^^^^^^^ byte 11
        //                     ^^^^^^^^^ byte 10
        //           ^^^^ these 4 bits % 6 * 240 minutes +
        //                ^^ these 2 bits * 60 minutes +
        //                  ^^ these 2 bits * 16 minutes +
        //                     ^^^^ these 4 bits * 1 minute
        int minutes =
            (byte11 >> 4) % 6 * 240 +
            ((byte11 & 0b00001100) >> 2) * 60 +
            ((byte11 & 0b00000011) * 16) +
            (byte10 >> 4);

        DateTime saveFileWriteTime = new(year, month, day, minutes / 60, minutes % 60, 0);
        return saveFileWriteTime;
    }
}
