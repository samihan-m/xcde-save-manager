using System.Management.Automation; // For running git commands
using System.Collections.ObjectModel; // For running git commands
using xcde_save_manager;
using PuppeteerSharp;

class Manager
{
    public static async Task Main(string[] args)
    {
        // Make sure Puppeteer has everything it needs downloaded off rip so it won't have to happen during a save or anything
        using var browserFetcher = new BrowserFetcher();
        await browserFetcher.DownloadAsync();

        // First argument will be the absolute path to the directory with all of the save files

        if (args.Length < 1)
        {
            Console.WriteLine("Error: expected the absolute path to the save directory as the first argument.");
            return;
        }

        string SaveDirectory = args[0];

        if (Directory.Exists(SaveDirectory) == false)
        {
            Console.WriteLine($"Error: The provided save directory `{SaveDirectory}` does not exist.");
            return;
        }

        string GitDirectory = Path.Combine(SaveDirectory, ".git");

        if (Directory.Exists(GitDirectory) == false)
        {
            Console.WriteLine($"Error: The provided save directory `{SaveDirectory}` does not contain a git repository. Initialize one before running this manager.");
            return;
        }

        bool didFindRemoteRepo;
        using (PowerShell powershell = PowerShell.Create())
        {
            powershell.AddScript($"cd \"{SaveDirectory}\"");
            powershell.AddScript("git remote -v");

            Collection<PSObject> results = powershell.Invoke();

            if (powershell.HadErrors == true)
            {
                Console.WriteLine("Encountered error(s) when trying to check git remote:");
                for (int i = 0; i < powershell.Streams.Error.Count; i++)
                {
                    Console.WriteLine(powershell.Streams.Error[i].ToString());
                }
                return;
            }

            var x = powershell.Streams.Error.ToList();

            didFindRemoteRepo = results.Count > 0;
        }
        if (didFindRemoteRepo == false)
        {
            Console.WriteLine($"Warning: The git repository in `{SaveDirectory}` does not have a remote repository set up. " +
                "Manager will not run `git push` after detecting changes. If this is a mistake, figure out your remote situation before running this manager.");
        }

        // Run git commit and git push on the save directory off rip

        Console.WriteLine("Performing initial check in...");

        await checkInSaveData(SaveDirectory, didFindRemoteRepo);

        Console.WriteLine("Succeeded.");

        // Initialize a file watcher to watch for changes in the directory
        // Specifically, changes to .tmb or .sav files (from what I can tell, if file1.sav is changed, file1.tmb is also changed)
        // When changes are detected, run the image/README generation code, run a git commit, and then a git push

        Console.WriteLine("Initializing file watcher...");

        using FileSystemWatcher watcher = new(SaveDirectory);
        watcher.Filter = "*.sav";
        watcher.NotifyFilter = NotifyFilters.LastWrite;
        watcher.EnableRaisingEvents = true;

        Console.WriteLine($"Watching {SaveDirectory}.");

        watcher.Changed += async (sender, e) =>
        {
            Console.WriteLine($"Detected change in {e.FullPath} - {File.GetLastAccessTime(e.FullPath).Ticks}");
            // Wait until the file is done being written to
            while (!IsFileReady(e.FullPath)) { }
            Console.WriteLine("File is ready.");
            await checkInSaveData(SaveDirectory, didFindRemoteRepo);
        };

        while(true)
        {
            Console.WriteLine("Type 'STOP' to stop.");
            string? input = Console.ReadLine();
            if(input != null && input == "STOP")
            {
                break;
            }
        }
    }

    // From https://stackoverflow.com/a/1406853, thanks!
    public static bool IsFileReady(string filename)
    {
        // If the file can be opened for exclusive access it means that the file
        // is no longer locked by another process.
        try
        {
            using (FileStream inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.None))
                return inputStream.Length > 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static async Task<bool> checkInSaveData(string absolutePath, bool doPush)
    {
        bool didCheckInSuccessfully = true;
        string currentTimeString = DateTime.Now.ToString();

        Console.WriteLine($"Checking in save data at {currentTimeString}");

        Console.WriteLine($"Creating HTML render of the save data...");
        await Renderer.generateSaveScreenRender(absolutePath);

        Console.WriteLine("Performing git commit...");
        PowerShellOutput commitOutput = commitChanges(absolutePath, $"Checking in save data - {DateTime.Now.ToString()}");
        if(commitOutput.Errors != null)
        {
            Console.WriteLine("Encountered error(s) when trying to commit:");
            foreach(ErrorRecord error in commitOutput.Errors)
            {
                string errorMessage = error.ToString();
                if(errorMessage.Contains("LF will be replaced by CRLF the next time Git touches it") == false)
                {
                    Console.WriteLine(errorMessage);
                }
            }
        }
        Console.WriteLine("Commit done.");

        PowerShellOutput pushOutput;
        if (doPush)
        {
            Console.WriteLine("Performing git push...");
            pushOutput = pushCommits(absolutePath);
            if (pushOutput.Errors != null)
            {
                Console.WriteLine("Encountered error(s) when trying to push:");
                for (int i = 0; i < pushOutput.Errors.Count; i++)
                {
                    Console.WriteLine(pushOutput.Errors[i].ToString());
                }
            }
        }

        return didCheckInSuccessfully;
    }

    private static PowerShellOutput commitChanges(string absolutePath, string commitMessage)
    {
        using (PowerShell powershell = PowerShell.Create())
        {
            // the folder PowerShell opens to by default is the user's home directory, so we need to cd
            powershell.AddScript($"cd \"{absolutePath}\"");
            powershell.AddScript("git add *");
            powershell.AddScript($"git commit -m \"{commitMessage}\"");

            Collection<PSObject> results = powershell.Invoke();

            PowerShellOutput response = powershell.HadErrors == false ?
                new(results.ToList(), null) :
                new(null, powershell.Streams.Error.ToList());

            return response;
        }
    }

    private static PowerShellOutput pushCommits(string absolutePath)
    {
        using (PowerShell powershell = PowerShell.Create())
        {
            // the folder PowerShell opens to by default is the user's home directory, so we need to cd
            powershell.AddScript($"cd \"{absolutePath}\"");
            powershell.AddScript("git push");

            Collection<PSObject> results = powershell.Invoke();

            PowerShellOutput response = powershell.HadErrors == false ?
                new(results.ToList(), null) :
                new(null, powershell.Streams.Error.ToList());

            return response;
        }
    }
}

class PowerShellOutput
{
    public List<PSObject>? Output { get; set; }
    public List<ErrorRecord>? Errors { get; set; }

    public PowerShellOutput(List<PSObject>? output, List<ErrorRecord>? errors)
    {
        Output = output;
        Errors = errors;
    }
}