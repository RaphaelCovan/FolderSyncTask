using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace VeeamTask
{
    class Program
    {
        public static string configfilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.txt");
        public static string taskSchedulePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "VeeamTask.exe");
        public static string logfileName = "logfile.txt";
        public static string logfilePath;
        public static bool exit = false;
        static void Main(string[] args)
        {
            loadLogFilePath();

            if (args.Length == 3) {
                string sourcePath = args[0];
                string replicaPath = args[1];
                string logfilePath = args[2];

                syncTask(sourcePath, replicaPath, logfilePath);
                return;
            }

            while (logfilePath == null) {
                Console.Clear();
                Console.WriteLine("Before using the program we ask you to choose a path for your log file: ");
                logfilePath = Console.ReadLine();

                if (!Directory.Exists(logfilePath)) {
                    Console.WriteLine("\nDirectory doesn't exist. Please enter a valid path.");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    logfilePath = null;
                } else {
                    Console.WriteLine($"\nLog file will be stored at: {logfilePath}");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    logfilePath = Path.Combine(logfilePath, logfileName);
                    logfilePathEntry(logfilePath, "Log file is now located at: ");
                    File.WriteAllText(configfilePath, logfilePath);
                }
            }

            while (!exit)
            {
                Console.Clear();
                Console.WriteLine("Choose an option:");
                Console.WriteLine("1. Sync folders");
                Console.WriteLine("2. View current syncs / Delete a sync");
                Console.WriteLine("3. Modify log file's path");
                Console.WriteLine("0. Exit");

                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        try
                        {   
                            Console.Clear();
                            Console.WriteLine("Enter the path of the source folder:");
                            string sourcePath = Console.ReadLine();

                            Console.WriteLine("Enter the path of the replica folder to be created:");
                            string replicaPath = Console.ReadLine();

                            if (Directory.Exists(sourcePath) && isValidPath(replicaPath))
                                syncFolders(sourcePath, replicaPath, logfilePath);
                            else {
                                Console.WriteLine("Please enter only valid paths.");
                                Console.WriteLine("Press any key to continue...");
                                Console.ReadKey();
                                break;
                            }

                            string log3 = $"{DateTime.Now}: The files from: '{sourcePath}' were copied to: '{replicaPath}'.";
                            File.AppendAllText(logfilePath, log3 + Environment.NewLine);

                            Console.Clear();
                            Console.WriteLine("Would you like to turn on auto-sync and set a time period? (y/n)");
                            var op = Console.ReadLine();

                            if (op.ToLower() == "y") {
                                Console.WriteLine("\nEnter the desired sync interval in minutes:");
                                var interval = Convert.ToInt32(Console.ReadLine());

                                if (interval > 0) {
                                    string taskName = $"FolderSyncTask_{DateTime.Now:yyyyMMdd_HHmmss}";
                                    string arguments = $"/create /tn \"{taskName}\" /tr \"\\\"{taskSchedulePath}\\\" \\\"{sourcePath}\\\" \\\"{replicaPath}\\\" \\\"{logfilePath}\\\"\" /sc minute /mo {interval} /f";

                                    try
                                    {
                                        Process.Start("schtasks", arguments);
                                        string log4 = $"{DateTime.Now}: The task {taskName} is associated to: Source: {sourcePath} | Replica: {replicaPath}";
                                        File.AppendAllText(logfilePath, log4 + Environment.NewLine);
                                        Console.WriteLine($"Scheduled sync-task '{taskName}' was created successfully.");
                                        Console.ReadKey();
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Error creating scheduled task: {ex.Message}");
                                    }
                                }
                                else {
                                    Console.WriteLine("Invalid Input! Interval has to be bigger than zero!");
                                    Console.ReadKey();
                                    break;
                                }
                            }
                            else
                                break;

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }

                        break;
                    case "2":
                        listSyncs();
                        break;
                    case "3":
                        Console.Clear();
                        Console.WriteLine($"Current path: {logfilePath}");

                        var temp = logfilePath;

                        Console.WriteLine("\nWould you like to change it? (y/n)");
                        var option = Console.ReadLine();

                        if (option.ToLower() == "y") {
                            Console.WriteLine("Enter the new desired path:");
                            logfilePath = Console.ReadLine();

                            if (!Directory.Exists(logfilePath)) {
                                Console.WriteLine("\nDirectory doesn't exist. Please enter a valid path.");
                                Console.WriteLine("Press any key to continue...");
                                Console.ReadKey();

                                logfilePath = temp;
                            } else {
                                Console.WriteLine($"\nLog file will be stored at: {logfilePath}");
                                logfilePath = Path.Combine(logfilePath, logfileName);
                                File.Move(temp, logfilePath, true);

                                logfilePathEntry(logfilePath, "Log file has been moved to: ");
                                File.WriteAllText(configfilePath, logfilePath);

                                Console.WriteLine("Press any key to continue...");
                                Console.ReadKey();
                            }

                            break;
                        }
                        else
                            break;
                    case "0":
                        exit = true;
                        break;
                    default:
                        Console.WriteLine("Invalid input! Try again.");
                        Console.WriteLine("Press any key to continue...");
                        Console.ReadKey();
                        break;
                }
            }
        }

        static void logfilePathEntry(string logfilePath, string msg)
        {
            string log = $"{DateTime.Now}: {msg}'{logfilePath}'";
            File.AppendAllText(logfilePath, log + Environment.NewLine);
        }

        static void loadLogFilePath()
        {
            if (File.Exists(configfilePath)) {
                string storedPath = File.ReadAllText(configfilePath).Trim();
                if (!string.IsNullOrEmpty(storedPath) && File.Exists(storedPath)) {
                    logfilePath = storedPath;
                }
            }
        }

        static void syncFolders(string sourcePath, string replicaPath, string logfilePath)
        {
            try
            {
                Directory.CreateDirectory(replicaPath);

                string log = $"{DateTime.Now}: A replica folder of '{sourcePath}' was just created at '{replicaPath}'.";
                File.AppendAllText(logfilePath, log + Environment.NewLine);
                Console.WriteLine("Directory created successfully!");

                copyFilesRecursively(new DirectoryInfo(sourcePath), new DirectoryInfo(replicaPath));
                Console.WriteLine("Files and subdirectories copied successfully!");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadKey();
            }
        }

        static void syncFoldersPeriodically(string sourcePath, string replicaPath, string logfilePath)
        {
            try
            {
                copyFilesRecursively(new DirectoryInfo(sourcePath), new DirectoryInfo(replicaPath));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadKey();
            }
        }

        static void copyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (DirectoryInfo dir in source.GetDirectories()) {
                copyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            }

            foreach (FileInfo file in source.GetFiles()) {
                file.CopyTo(Path.Combine(target.FullName, file.Name), true);
            }
        }

        static void syncTask(string sourcePath, string replicaPath, string logfilePath)
        {
            try
            {
                syncFoldersPeriodically(sourcePath, replicaPath, logfilePath);
                Console.WriteLine("Sync completed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static void listSyncs()
        {
            try
            {
                Console.Clear();

                if (!File.Exists(logfilePath)) {
                    Console.WriteLine("Log file not found.");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    return;
                }

                var logEntries = File.ReadAllLines(logfilePath).ToList();
                var syncLogEntries = logEntries.Where(line => line.Contains("The task")).ToList();

                if (syncLogEntries.Count == 0) {
                    Console.WriteLine("No sync tasks found in the log file.");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    return;
                }

                for (int i = 0; i < syncLogEntries.Count; i++) {
                    Console.WriteLine($"Sync [{i + 1}]: Source '{getSourcePath(syncLogEntries[i])}' | Replica '{getReplicaPath(syncLogEntries[i])}' | Task Name: '{getTaskName(syncLogEntries[i])}'");
                }

                Console.WriteLine("\nWould you like to delete a sync? (y/n)");
                var option = Console.ReadLine();

                if (option.ToLower() == "y")
                {
                    Console.WriteLine("Select the number of the sync you would like to delete: ([0] to cancel)");
                    var choice = Convert.ToInt32(Console.ReadLine());

                    if (choice <= 0 || choice > syncLogEntries.Count) {
                        Console.WriteLine("Deletion canceled.");
                        Console.WriteLine("Press any key to return to menu...");
                        Console.ReadKey();
                        return;
                    }

                    string logEntryToDelete = syncLogEntries[choice - 1];
                    string taskName = getTaskName(logEntryToDelete);

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "schtasks",
                        Arguments = $"/delete /tn \"{taskName}\" /f",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }).WaitForExit();

                    logEntries.Remove(logEntryToDelete);
                    File.WriteAllLines(logfilePath, logEntries);

                    string log = $"{DateTime.Now}:Task '{taskName}' associated to Source: {getSourcePath(logEntryToDelete)} | Replica: {getReplicaPath(logEntryToDelete)} is now deleted.";
                    File.AppendAllText(logfilePath, log + Environment.NewLine);

                    Console.WriteLine($"\nTask '{taskName}' deleted successfully.");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                }
                else
                {
                    Console.WriteLine("Press any key to return to menu...");
                    Console.ReadKey();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.ReadKey();
            }
        }

        static string getTaskName(string logEntry)
        {
            try
            {
                
                int startIndex = logEntry.IndexOf("task") + 5;
                int endIndex = logEntry.IndexOf("is associated");

                if (startIndex != -1 && endIndex != -1) {
                    return logEntry.Substring(startIndex, endIndex - startIndex).Trim();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting task name: {ex.Message}");
            }

            return null;
        }

        static string getSourcePath(string logEntry)
        {
            try
            {
                string sourceIdentifier = "Source:";
                int startIndex = logEntry.IndexOf(sourceIdentifier) + sourceIdentifier.Length;
                int endIndex = logEntry.IndexOf('|', startIndex);

                if (startIndex != -1 && endIndex != -1) {
                    return logEntry.Substring(startIndex, endIndex - startIndex).Trim();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return null;
        }

        static string getReplicaPath(string logEntry)
        {
            try
            {
                string replicaIdentifier = "|";
                int startIndex = logEntry.IndexOf(replicaIdentifier) + replicaIdentifier.Length;

                if (startIndex != -1) {
                    return logEntry.Substring(startIndex).Trim();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return null;
        }

        static bool isValidPath(string path)
        {
            try
            {
                if (path.IndexOfAny(Path.GetInvalidPathChars()) != -1) {
                    return false;
                }

                if (!Path.IsPathRooted(path)) {
                    return false;
                }

                string parentDirectory = Path.GetDirectoryName(path);

                if (Directory.Exists(parentDirectory) || Directory.Exists(path)) {
                    return true;
                }

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }


    }
}