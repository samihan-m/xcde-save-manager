# xcde-save-manager
 A command-line tool for creating and managing "save states" of the Xenoblade Chronicles: Definitive Edition saves folder. Requires that you create a git repository in your saves folder.
 
 Once running, every time it detects an update to a `.sav` file in the chosen directory (as well as one guaranteed time on launch), the manager creates an  **almost completely accurate** `.png` visualization of what the Load Game screen would look like, places that visualization into the `README.md` of the repository, commits those changes, and (if remote has been set up for the repository) pushes those changes. You can see an example of it in action (me using it) at https://github.com/samihan-m/xcde-saves (DO NOT CLICK ON THIS IF YOU DO NOT WANT SPOILERS!). 
 
 I'm including a single visualization from very early in the game to prevent spoilers:

 ![saves](https://github.com/samihan-m/xcde-save-manager/assets/23440673/d95f253c-6942-4cdb-8c4f-7c62a405d4dc)


 Big thanks to the work at https://gitlab.com/damysteryman/XCDESave, a library which I used for this project (as XCDESave.dll). 
 
 I did some embarrassingly laborious investigation into the `.sav` format myself to try and find information about the Play Time and Story Progress fields to visualize them, and I was able to figure out where Play Time is stored (the bytes at index 4 through 8 are the total seconds of play time for the save) - check `Renderer.cs` for a little bit more detail. 
 
 However, finding and reading Story Progress still eludes me. I believe it's because despite rendering on the Load Game page as "Chapter X", based on some exploration of how Xenoblade Chronicles 2 handles saves (me looking at [https://gitlab.com/damysteryman/XC2SaveNETThingy](https://gitlab.com/damysteryman/XC2SaveNETThingy/-/blob/master/XC2SaveNETThingy/SaveObjects/XC2Save.cs), I believe the game uses flags or most recent event IDs to mark progress and extrapolates the chapter number from these values. 
 
 Maybe now that I have a reliable way to record saves from various points in the game (this manager and my repository) I'll be able to solve those mysteries. Maybe not. We'll see. If somebody else figures it out, let me know! I'd love to replace the "Not Yet Implemented" with the real chapter number. It would make things more informative.

## Do not look in the /character-icons folder if you don't want spoilers!

## How it works

In short,

1. Watch for save file writes on the provided folder
2. When a save file is written to, parse each save file to extract the information required for visualization
3. Fill out HTML templates with the extracted information
4. Render the HTML and take a screenshot of it
5. Save the screenshots as PNGs and link them in the README
6. Repeat
