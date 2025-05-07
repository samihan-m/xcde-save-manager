using System.Management.Automation; // For running git commands
using System.Collections.ObjectModel; // For running git commands
using xcde_save_manager;
using PuppeteerSharp;

class Manager
{
    public static async Task Main(string[] args)
    {
        // Make sure Puppeteer has everything it needs downloaded off rip so it won't have to happen during a save or anything
        Console.WriteLine("Ensuring the HTML renderer has everything it needs downloaded...");
        using (var browserFetcher = new BrowserFetcher())
        {
            await browserFetcher.DownloadAsync();
        }
        Console.WriteLine("Done.");

        // First argument might be the absolute path to the directory with all of the save files
        string saveDirectory;

        if (args.Length >= 1)
        {
            saveDirectory = args[0];
        }
        else
        {
            Console.WriteLine("Error: expected the absolute path to the save directory as the first argument.");
            Console.WriteLine("What is the absolute path to the save directory? Type here:");
            string? path = Console.ReadLine();
            if (path == null || path == "")
            {
                Console.WriteLine("Error: no path provided. Exiting. Press any button to continue.");
                Console.ReadLine();
                return;
            }
            Console.WriteLine($"Using {path} as the save directory.");
            Console.WriteLine($"Consider creating a shortcut to this .exe with \"{path}\" as the first argument.");
            saveDirectory = path;
        }

        if (Directory.Exists(saveDirectory) == false)
        {
            Console.WriteLine($"Error: The provided save directory `{saveDirectory}` does not exist. Press enter to exit.");
            Console.ReadLine();
            return;
        }

        string GitDirectory = Path.Combine(saveDirectory, ".git");

        if (Directory.Exists(GitDirectory) == false)
        {
            Console.WriteLine($"Error: The provided save directory `{saveDirectory}` does not contain a git repository. Initialize one before running this manager. Press enter to exit.");
            Console.ReadLine();
            return;
        }

        bool didFindRemoteRepo;
        using (PowerShell powershell = PowerShell.Create())
        {
            powershell.AddScript($"cd \"{saveDirectory}\"");
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

            didFindRemoteRepo = results.Count > 0;
        }
        if (didFindRemoteRepo == false)
        {
            Console.WriteLine($"Warning: The git repository in `{saveDirectory}` does not have a remote repository set up. " +
                "Manager will not run `git push` after detecting changes. If this is a mistake, figure out your remote situation before running this manager.");
        }

        // Run git commit and git push on the save directory off rip

        Console.WriteLine("Performing initial check in...");

        await checkInSaveData(saveDirectory, didFindRemoteRepo);
        
        DateTime lastDirectoryWriteTime = Directory.GetLastWriteTime(saveDirectory);

        Console.WriteLine("Succeeded.");

        while (true)
        {
            DateTime currentDirectoryWriteTime = Directory.GetLastWriteTime(saveDirectory);
            IEnumerable<Task> tasks = [Task.Delay(1000)];
            bool isChangeDetected = currentDirectoryWriteTime > lastDirectoryWriteTime;
            if (isChangeDetected)
            {
                Console.WriteLine($"Detected change in {saveDirectory}");
                tasks = tasks.Append(checkInSaveData(saveDirectory, didFindRemoteRepo));
            }
            await Task.WhenAll(tasks);
            // Checking in the save data will update the last write time,
            // so update it post-check-in
            if (isChangeDetected)
            {
                lastDirectoryWriteTime = Directory.GetLastWriteTime(saveDirectory);
            }
        }
    }

    // From https://stackoverflow.com/a/1406853 - thanks!
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

        // When the save directory is overwritten (as via some save mechanisms)
        // it is overwritten with an old version of the .git folder
        // so we need to pull the latest git history
        PowerShellOutput pullResponse = pullLatest(absolutePath);
        if (pullResponse.Output != null)
        {
            Console.WriteLine("Git pull output:");
            foreach (PSObject output in pullResponse.Output)
            {
                Console.WriteLine(output.ToString());
            }
        }
        if (pullResponse.Errors != null)
        {
            Console.WriteLine("Error output: (note: this includes non-normal messages for some reason)");
            foreach (ErrorRecord error in pullResponse.Errors)
            {
                Console.WriteLine(error.ToString());
            }
        }

        Console.WriteLine($"Creating HTML render of the save data...");
        while (Path.Exists(absolutePath) == false)
        {
            await Task.Delay(100);
        }
        await Renderer.generateSaveScreenRender(absolutePath);

        Console.WriteLine("Performing git commit...");
        PowerShellOutput commitOutput = commitChanges(absolutePath, $"Checking in save data - {DateTime.Now}");
        if(commitOutput.Output != null)
        {
            Console.WriteLine("Git commit output:");
            foreach(PSObject output in commitOutput.Output)
            {
                Console.WriteLine(output.ToString());
            }
        }
        if(commitOutput.Errors != null)
        {
            Console.WriteLine("Error output: (note: this includes non-normal messages for some reason)");
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
            if (pushOutput.Output != null)
            {
                Console.WriteLine("Git push output:");
                foreach (PSObject output in pushOutput.Output)
                {
                    Console.WriteLine(output.ToString());
                }
            }
            if (pushOutput.Errors != null)
            {
                Console.WriteLine("Error output: (note: this includes non-normal messages for some reason)");
                for (int i = 0; i < pushOutput.Errors.Count; i++)
                {
                    Console.WriteLine(pushOutput.Errors[i].ToString());
                }
            }
        }

        return didCheckInSuccessfully;
    }

    private static PowerShellOutput pullLatest(string absolutePath)
    {
        using PowerShell powershell = PowerShell.Create();
        // the folder PowerShell opens to by default is the user's home directory, so we need to cd
        powershell.AddScript($"cd \"{absolutePath}\"");
        // Stash the latest save
        powershell.AddScript("git stash");
        // Restore git history
        powershell.AddScript("git pull");
        // "Force" restore the stashed latest-save
        powershell.AddScript("git checkout stash -- .");

        Collection<PSObject> results = powershell.Invoke();

        PowerShellOutput response = powershell.HadErrors == false ?
            new(results.ToList(), null) :
            new(null, powershell.Streams.Error.ToList());

        return response;
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