//using XCDESave;
//using static System.Runtime.InteropServices.JavaScript.JSType;
//{
//    byte[] c10 = File.ReadAllBytes(@"D:\Samihan\Documents\My Games\Yuzu Save Backups\XCDE\save-Jan05-250AM\bfsgame00.sav");
//    byte[] c9 = File.ReadAllBytes(@"D:\Samihan\Documents\My Games\Yuzu Save Backups\XCDE\save-Jan05-250AM\bfsgame01.sav");
//    byte[] c2 = File.ReadAllBytes(@"D:\Samihan\Documents\My Games\Yuzu Save Backups\XCDE\save-Jan05-250AM\bfsgame02.sav");
//    byte[] c1 = File.ReadAllBytes(@"D:\Samihan\Documents\My Games\Yuzu Save Backups\XCDE\save-Jan05-250AM\bfsgame00a.sav");

//    XCDESaveData s10 = new(c10);
//    XCDESaveData s9 = new(c9);
//    XCDESaveData s2 = new(c2);
//    XCDESaveData s1 = new(c1);

//    static List<(int, ulong, ulong)> searchBytes(byte[] s1, byte[] s2)
//    {

//        List<(ulong, int)> valuesObtained = new();
//        List<(ulong, int)> valuesObtained2 = new();

//        // read X bytes at a time and see if they equal to 47:36 (47*60 + 36 = 2856 = 0x0B28) - this could be little or big endian idk
//        // 47:36:000 -> 2856000
//        // 47:36:999 -> 2856999
//        // nanoseconds -> 2856000000 to 2856999999
//        for (int i = 0; i < s1.Length; i++)
//        {
//            //try { valuesObtained.Add((BitConverter.ToUInt64(s1, i), i)); }
//            //catch (Exception) { }

//            //try { valuesObtained.Add((BitConverter.ToUInt64(s2, i), i)); }
//            //catch (Exception) { }

//            try { valuesObtained.Add((BitConverter.ToUInt32(s1, i), i)); }
//            catch (Exception) { }

//            try { valuesObtained2.Add((BitConverter.ToUInt32(s2, i), i)); }
//            catch (Exception) { }

//            try { valuesObtained.Add((BitConverter.ToUInt16(s1, i), i)); }
//            catch (Exception) { }

//            try { valuesObtained2.Add((BitConverter.ToUInt16(s2, i), i)); }
//            catch (Exception) { }

//            try { valuesObtained.Add((s1[i], i)); }
//            catch (Exception) { }

//            try { valuesObtained2.Add((s2[i], i)); }
//            catch (Exception) { }
//        }

//        List<(int, ulong, ulong)> interesting = new();

//        foreach(((ulong, int), (ulong, int)) pair in valuesObtained.Zip(valuesObtained2))
//        {
//            ulong value = pair.Item1.Item1;
//            ulong value2 = pair.Item2.Item1;
//            int index = pair.Item1.Item2;

//            //// Ignore things that are in both
//            //if(solelyValues2Obtained.Contains(value))
//            //{
//            //    continue;
//            //}

//            //// It's at least as big as 2856
//            //// and value2 is at least as big as 48*60 + 20 = 2900
//            //if((value < 2856) || (value2 < 2900))
//            //{
//            //    continue;
//            //}

//            //// Value 2 has to be bigger than value
//            //// but definitely is not more than 2x value
//            //if((value2 <= value) || (value2 >= 1.016*value))
//            //{
//            //    continue;
//            //}

//            // value 2 is coming from an earlier chapter
//            if ((value2 == value))
//            {
//                continue;
//            }

//            Console.WriteLine($"Index {index} - {value} {pair.Item2.Item1}");
//            interesting.Add((index, value, value2));
//        }

//        return interesting;
//    }

//    XCDESaveData chapter2save = new(c2);
//    byte[] c2u1 = chapter2save.Unk_0x000000;
//    byte[] c2u2 = chapter2save.Unk_0x000014;
//    byte[] c2u3 = chapter2save.Unk_0x03B5B0;

//    XCDESaveData chapter1save = new(c1);
//    byte[] c1u1 = chapter1save.Unk_0x000000;
//    byte[] c1u2 = chapter1save.Unk_0x000014;
//    byte[] c1u3 = chapter1save.Unk_0x03B5B0;

//    var l1 = searchBytes(c2u1, c1u1);
//    Console.WriteLine("End of section 1.");
//    var l2 = searchBytes(c2u2, c1u2);
//    //searchBytes(c2u3, c1u3);

//    XCDESaveData chapter9save = new(c9);
//    byte[] c9u1 = chapter9save.Unk_0x000000;
//    byte[] c9u2 = chapter9save.Unk_0x000014;
//    byte[] c9u3 = chapter9save.Unk_0x03B5B0;

//    XCDESaveData chapter10save = new(c10);
//    byte[] c10u1 = chapter10save.Unk_0x000000;
//    byte[] c10u2 = chapter10save.Unk_0x000014;
//    byte[] c10u3 = chapter10save.Unk_0x03B5B0;

//    var l3 = searchBytes(c10u1, c9u1);
//    Console.WriteLine("End of section 1.");
//    var l4 = searchBytes(c10u2, c9u2);
//}


////using System.Collections;
////using System.Security.Cryptography;
////using XCDESave;
////{
////    // See https://aka.ms/new-console-template for more information
////    // Console.WriteLine("Hello, World!");

////    static void print_things(string save_path)
////    {
////        byte[] saveData = File.ReadAllBytes(save_path);

////        XCDESaveData save = new(saveData);

////        for (int i = 0; i < save.Party.PartyMembersCount; i++)
////        {
////            XCDESave.Character character = save.Party.Characters[i];
////            Console.WriteLine($"Character {i + 1}: {character}");
////        }

////        byte[] first_16_bytes = save.Unk_0x000000;
////        for (int i = 0; i < first_16_bytes.Length; i++)
////        {
////            Console.Write($"{first_16_bytes[i]} ");
////        }
////        Console.WriteLine();
////    }

////    // const string SavePath = @"D:\Samihan\Documents\Programming\C#\xcde-save-manager\bfsgame00a.sav";
////    // const string SavePath = @"D:\Samihan\Documents\My Games\Yuzu Save Backups\XCDE\save-Dec23-354AM\0000000000000000\9E98D3DFE1AB0BAF13EB6007BF59F20F\0100FF500E34A000\bfsgame00a.sav";
////    //const string SavePath = @"D:\Samihan\Downloads\XCDE\bfsgame00a.sav";
////    //print_things(SavePath);
////    //const string sp1 = @"D:\Samihan\Downloads\XCDE\bfsgame00.sav";
////    //print_things(sp1);
////    //const string sp2 = @"D:\Samihan\Downloads\XCDE\bfsgame01.sav";
////    //print_things(sp2);
////    //const string sp3 = @"D:\Samihan\Downloads\XCDE\bfsgame02.sav";
////    //print_things(sp3);

////    //const string SaveThumbnailPath = @"D:\Samihan\Documents\Programming\C#\xcde-save-manager\bfsgame00.tmb";

////    //var thumbnail = new XCDESaveThumbnail(SaveThumbnailPath);

////    //thumbnail.Export(@"D:\Samihan\Documents\Programming\C#\xcde-save-manager\bfsgame00.bmp", XCDESaveThumbnail.Type.BMP);

////    byte[] c17 = File.ReadAllBytes(@"D:\Samihan\Downloads\XCDE\bfsgame00a.sav");
////    byte[] c10 = File.ReadAllBytes(@"D:\Samihan\Documents\My Games\Yuzu Save Backups\XCDE\save-Jan05-1204AM\bfsgame00.sav");
////    byte[] c9 = File.ReadAllBytes(@"D:\Samihan\Documents\My Games\Yuzu Save Backups\XCDE\save-Jan04-1027PM\bfsgame01.sav");
////    byte[] c92 = File.ReadAllBytes(@"D:\Samihan\Documents\My Games\Yuzu Save Backups\XCDE\save-Dec23-354AM\bfsgame00.sav");
////    byte[] c2 = File.ReadAllBytes(@"D:\Samihan\Documents\My Games\Yuzu Save Backups\XCDE\save-Jan05-250AM\bfsgame02.sav");
////    byte[] c1 = File.ReadAllBytes(@"D:\Samihan\Documents\My Games\Yuzu Save Backups\XCDE\save-Jan05-1204AM\bfsgame02.sav");

////    XCDESaveData s17 = new(c17);
////    XCDESaveData s10 = new(c10);
////    XCDESaveData s9 = new(c9);
////    XCDESaveData s92 = new(c92);
////    XCDESaveData s2 = new(c2);
////    XCDESaveData s1 = new(c1);

////    ArrayList indices = new();

////    for(int i = 0; i < c10.Length; i++)
////    {
////        if ((c17[i] > c10[i]) && (c10[i] > c9[i]) && (c9[i] == c92[i]) && (c9[i] > c2[i]) && (c2[i] > c1[i]))
////        {
////            indices.Add(i);
////        }
////    }

////    int count = 0;

////    Console.WriteLine("Index Number \t C17 \t C10 \t C9 \t C92 \t C2 \t C1");
////    foreach (int index in indices)
////    {
////        // Check if index is is any of the unknown sections
////        //if ((index >= 0x14 && index < 0x14 + 0x3AFC) || (index >= 0x03B5B0 && index < 0x03B5B0 + 0x116590) || (index >= 0x151B44 && index < 0x151B44 + 0x7D4) || (index >= 0x152331 && index < 0x152331 + 0x37))
////        //{
////        //    count += 1;
////            Console.WriteLine($"Index {index} \t {c17[index]} \t {c10[index]} \t {c9[index]} \t {c92[index]} \t {c2[index]} \t {c1[index]}");
////        //}
////    }

////    Console.WriteLine($"Total: {indices.Count}; Filtered Count: {count}");

////    //static bool areAllDifferent(byte a, byte b, byte c, byte d)
////    //{
////    //    return a != b && a != c && a != d && b != c && b != d && c != d;
////    //}

////    ////static bool areAllDifferent(byte a, byte b, byte c, byte d, byte e)
////    ////{
////    ////    return a != b && a != c && a != d && a != e && b != c && b != d && b != e && c != d && c != e && d != e;
////    ////}

////    //ArrayList first_section_indices = new();

////    //for (int i = 0; i < s17.Unk_0x000014.Length; i++)
////    //{
////    //    if (areAllDifferent(s17.Unk_0x000014[i], s10.Unk_0x000014[i], s9.Unk_0x000014[i], s1.Unk_0x000014[i]))
////    //    {
////    //        first_section_indices.Add(i);
////    //    }
////    //}
////    //Console.WriteLine($"Done with Unk_0x000014 - indices count is {first_section_indices.Count}");

////    //Console.WriteLine("Index Number \t S17 \t S10 \t S9 \t S92 \t S1");
////    //foreach (int index in first_section_indices)
////    //{
////    //    Console.WriteLine($"Index {index} \t {s17.Unk_0x000014[index]} \t {s10.Unk_0x000014[index]} \t {s9.Unk_0x000014[index]} \t {s92.Unk_0x000014[index]} \t {s1.Unk_0x000014[index]}");
////    //}

////    //ArrayList second_section_indices = new();

////    //for (int i = 0; i < s17.Unk_0x03B5B0.Length; i++)
////    //{
////    //    if (areAllDifferent(s17.Unk_0x03B5B0[i], s10.Unk_0x03B5B0[i], s9.Unk_0x03B5B0[i], s1.Unk_0x03B5B0[i]))
////    //    {
////    //        second_section_indices.Add(i);
////    //    }
////    //}
////    //Console.WriteLine($"Done with Unk_0x03B5B0 - indices count is {second_section_indices.Count}");

////    //Console.WriteLine("Index Number \t S17 \t S10 \t S9 \t S92 \t S1");
////    //foreach (int index in second_section_indices)
////    //{
////    //    Console.WriteLine($"Index {index} \t {s17.Unk_0x03B5B0[index]} \t {s10.Unk_0x03B5B0[index]} \t {s9.Unk_0x03B5B0[index]} \t {s92.Unk_0x03B5B0[index]} \t {s1.Unk_0x03B5B0[index]}");
////    //}

////    //Console.WriteLine("Done.");

////    /*
     
////     An exe I can start at startup

////    It will watch the save folder, and when any of the files change, it will:
////    1. Create a bitmap of each thumbnail
////    2. Create a markdown file named the same thing as each save file
////        - The markdown file will contain some information about the save file and include the thumbnail
////        - Information: Date modified (date and time of the save), starting player level, party members and their levels, 
     
////     */
////}