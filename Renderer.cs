using XCDESave;
using PuppeteerSharp;

namespace xcde_save_manager
{
    internal class Renderer
    {
        private static DateTime getSaveFileWriteTime(XCDESaveData save) {
            byte[] saveFileBytes = save.ToRawData();
            UInt16 bytes_containing_year = BitConverter.ToUInt16(saveFileBytes, 14);
            // 0x1FA0 -> 0001 1111 1010 0000.
            //           ^^^^^^^^^^^^^^^^^ these bits are the year
            UInt16 year = bytes_containing_year >>= 2;


            byte byte13 = saveFileBytes[13];
            byte byte14 = saveFileBytes[14];
            // 0xA0CB -> 1010 0000 1100 1011
            //                  ^^ ^^ these bits are the month
            //           ^^^^^^^^^ byte 14
            //                     ^^^^^^^^^ byte 13
            byte byte13Mask = 0b11000000;
            byte byte14Mask = 0b00000011;
            int month = (((byte13 & byte13Mask) >> 6) | ((byte14 & byte14Mask) << 2));

            // TODO: Figure out what the rest of the bits in byte 13 are doing
            //byte mystery_bits = (byte) (byte13 & (byte)~byte13Mask);
            //Console.WriteLine("Mystery month-adjacent bits: {0}", Convert.ToString(mystery_bits, 2));
            
            byte byte12 = saveFileBytes[12];
            // 0x3E -> 0011 1110
            //         ^^^^^^^^^ byte 12
            //            ^ ^^^^ these bits are the day
            byte byte12Mask = 0b00011111;
            int day = (byte12 & byte12Mask);

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


        public static async Task generateSaveScreenRender(string absolutePathToSaveDir)
        {
            // Keeping track of the HTML for the save files and the FC save files separately so we can render them separately
            string saveFilesHtml = "";
            string fcSaveFilesHtml = "";

            // Get all of the save files in the directory
            string[] saveFiles = Directory.GetFiles(absolutePathToSaveDir, "*.sav");
            // Sort saveFiles such that any files with 'a' at the end (autosaves) are at the beginning
            saveFiles = saveFiles.OrderBy(file => !file.EndsWith("a.sav")).ToArray();
            // Exclude "bfssystem.sav" from the list of save files - this is a different kind of save file that tracks Game Settings and what's available in the Event Theater or something
            saveFiles = saveFiles.Where(file => Path.GetFileName(file) != "bfssystem.sav").ToArray();

            foreach (string file in saveFiles)
            {
                string thumbnailPath = Path.Combine(absolutePathToSaveDir, Path.GetFileNameWithoutExtension(file) + ".tmb");
                if(File.Exists(thumbnailPath) == true)
                {
                    XCDESaveThumbnail thumbnail = new(thumbnailPath);
                    thumbnail.Export(Path.Combine(Directory.GetCurrentDirectory(), Path.GetFileNameWithoutExtension(file) + ".bmp"), XCDESaveThumbnail.Type.BMP);
                }

                string localBitmapPath = Path.GetFileNameWithoutExtension(file) + ".bmp";

                byte[] saveFileData = File.ReadAllBytes(file);

                XCDESaveData saveData = new(saveFileData);

                DateTime saveFileWriteTime = getSaveFileWriteTime(saveData);
                string saveDate = saveFileWriteTime.ToString("MM/dd/yyyy");
                string saveTime = saveFileWriteTime.ToString("H:mm"); // H is for 24-hour time (instead of h for 12-hour time)

                ulong playTimeInSeconds = BitConverter.ToUInt32(saveFileData, 4);
                TimeSpan playTime = TimeSpan.FromSeconds(playTimeInSeconds);
                string playTimeString = $"{(24 * playTime.Days + playTime.Hours).ToString("00")}:{playTime.Minutes.ToString("00")}";

                bool isAutosave = Path.GetFileNameWithoutExtension(file).EndsWith("a");

                uint leadCharacterLevel = 0;
                if (saveData.Party.PartyMembersCount > 0)
                {
                    leadCharacterLevel = saveData.PartyMembers[0].Level;
                }

                string leadCharacterName = "Null";
                if (saveData.Party.PartyMembersCount > 0)
                {
                    leadCharacterName = saveData.Party.Characters[0].ToString();
                }

                string[] partyMemberNames = new string[saveData.Party.PartyMembersCount];
                for (int i = 0; i < saveData.Party.PartyMembersCount; i++)
                {
                    string name = saveData.Party.Characters[i].ToString();

                    // Trying not to spoil things :)
                    int[] nameAsASCII = name.Select(c => (int)c).ToArray();
                    if (nameAsASCII.SequenceEqual([70, 105, 111, 114, 97, 95, 50])) 
                    {
                        name = "Seven";
                    }

                    // Use Future Connected icons for Future Connected saves
                    if (Path.GetFileName(file).Contains("meria") == true)
                    {
                        // Use Future Connected icons
                        name = $"FC{name}";
                    }

                    partyMemberNames[i] = name;
                }

                string characterIconTemplate = File.ReadAllText("./CharacterIcon.html");
                string characterIconsHtml = "";
                foreach(string name in partyMemberNames)
                {
                    string iconHtml = characterIconTemplate.Replace("[CHARACTER_NAME]", name);
                    characterIconsHtml += iconHtml;
                }
                // Remove the trailing newline because Markdown's HTML rendering doesn't like when there's a newline before a closing tag
                if(characterIconsHtml.EndsWith("\n"))
                {
                    characterIconsHtml = characterIconsHtml.Remove(characterIconsHtml.Length - 1);
                }

                string saveFileTemplate = File.ReadAllText("./SaveFile.html");
                string saveFileHtml = saveFileTemplate
                    .Replace("[AUTOSAVE_CLASS_NAME]", isAutosave ? "autosave-file" : "")
                    .Replace("[SAVE_FILE_NAME]", Path.GetFileNameWithoutExtension(file))
                    .Replace("[AUTOSAVE_INDICATOR]", isAutosave ? "Auto-save" : "")
                    .Replace("[LEVEL]", leadCharacterLevel.ToString())
                    .Replace("[PARTY_LEADER]", leadCharacterName)
                    .Replace("[PLAY_TIME]", playTimeString)
                    .Replace("[SAVE_DATE]", saveDate)
                    .Replace("[SAVE_TIME]", saveTime)
                    .Replace("[CHARACTER_ICONS]", characterIconsHtml);

                if(Path.GetFileName(file).Contains("meria") == true)
                {
                    fcSaveFilesHtml += saveFileHtml + "\n";
                }
                else
                {
                    saveFilesHtml += saveFileHtml + "\n";
                }
            }

            async Task takeScreenshots(string[] htmlPath, string[] pngSavePath)
            {
                using (var browserFetcher = new BrowserFetcher())
                {
                    await browserFetcher.DownloadAsync();
                    var browser = await Puppeteer.LaunchAsync(new LaunchOptions
                    {
                        Headless = true
                    });
                    var page = await browser.NewPageAsync();
                    await page.SetViewportAsync(new ViewPortOptions
                    {
                        Width = 2560,
                        Height = 1440
                    });
                    for (int i = 0; i < htmlPath.Length; i++)
                    {
                        await page.GoToAsync(htmlPath[i]);
                        await page.ScreenshotAsync(pngSavePath[i]);
                    }
                }
            }

            string saveScreenHtml = File.ReadAllText("./SaveScreen.html").Replace("[BODY]", saveFilesHtml);
            string pngSavePath = Path.Combine(absolutePathToSaveDir, "saves.png");
            string saveScreenHtmlSavePath = Path.Combine(Path.GetFullPath(Directory.GetCurrentDirectory()), "save-screen.html");
            File.WriteAllText(saveScreenHtmlSavePath, saveScreenHtml);
            string browserSafeSaveHtmlPath = new Uri(saveScreenHtmlSavePath).AbsolutePath;
            string[] pagesToScreenshot = { browserSafeSaveHtmlPath };
            string[] pngSavePaths = { pngSavePath };
            
            string readmeText = $"![Save Screen Render](./saves.png)";

            if (fcSaveFilesHtml.Length != 0)
            {
                string fcSaveScreenHtml = File.ReadAllText("./SaveScreen.html").Replace("[BODY]", fcSaveFilesHtml);
                string fcPngSavePath = Path.Combine(absolutePathToSaveDir, "fcsaves.png");
                string fcSaveScreenHtmlSavePath = Path.Combine(Path.GetFullPath(Directory.GetCurrentDirectory()), "fc-save-screen.html");
                File.WriteAllText(fcSaveScreenHtmlSavePath, fcSaveScreenHtml);

                // Using a Uri AbsolutePath here so the path to the file is safe to be viewed in the browser
                string browserSafeFcHtmlPath = new Uri(fcSaveScreenHtmlSavePath).AbsolutePath;
                pagesToScreenshot = pagesToScreenshot.Append(browserSafeFcHtmlPath).ToArray();
                pngSavePaths = pngSavePaths.Append(fcPngSavePath).ToArray();

                readmeText += $"\n![FC Save Screen Render](./fcsaves.png)";
            }

            await takeScreenshots(pagesToScreenshot, pngSavePaths);

            File.WriteAllText(Path.Combine(absolutePathToSaveDir, "README.md"), readmeText);
        }
    }
}
