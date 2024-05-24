using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Timers;
using AutoSave;

namespace AutoSave
{
    public class BackupManager : IBackupManager
    {
        public event EventHandler BackupStarted;
        public event EventHandler BackupStopped;
        private CancellationTokenSource cancellationTokenSource;
        private readonly SemaphoreSlim semaphore;
        private readonly ILogger logger;
        private readonly string filePath = "selected_files.json";
        private System.Timers.Timer timer;
        private bool isRunning;

        public BackupSettingsModel Settings { get; set; }

        public bool IsRunning
        {
            get { return isRunning; }
            private set { isRunning = value; }
        }

        public BackupManager(ILogger logger)
        {
            semaphore = new SemaphoreSlim(3); // Limit to 3 concurrent tasks
            this.logger = logger;
        }

        public void Initialize(BackupSettingsModel settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings), "BackupSettingsModel cannot be null");
            }

            this.Settings = settings;
            InitializeTimer();
        }

        private void InitializeTimer()
        {
            if (Settings == null)
            {
                throw new InvalidOperationException("BackupSettingsModel must be set before initializing the timer");
            }

            timer = new System.Timers.Timer(Settings.IntervalMinutes * 1000); // Interval is in milliseconds
            timer.Elapsed += TimerElapsedAsync;
        }

        public async Task StartBackupAsync()
        {
            if (isRunning)
            {
                throw new InvalidOperationException("Backup process is already running.");
            }

            if (Settings == null)
            {
                throw new InvalidOperationException("BackupSettingsModel must be set before starting the backup");
            }

            cancellationTokenSource = new CancellationTokenSource();
            timer.Interval = Settings.IntervalMinutes * 1000;

            Console.WriteLine($"Interval Minutes: {Settings.IntervalMinutes}");
            Console.WriteLine($"Source Path: {Settings.SourcePath}");
            Console.WriteLine($"Destination Path: {Settings.DestinationPath}");

            BackupStarted?.Invoke(this, EventArgs.Empty);

            await BackupFilesAsync();

            timer.Start();
            isRunning = true;

            Task backupLoopTask = Task.Run(async () =>
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromMinutes(Settings.IntervalMinutes), cancellationTokenSource.Token);
                    if (!cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        await BackupFilesAsync();
                    }
                }
            }, cancellationTokenSource.Token);
            try
            {
                await backupLoopTask;
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Backup process was cancelled.");
            }
            finally
            {
                isRunning = false;
                BackupStopped?.Invoke(this, EventArgs.Empty);
            }
        }

        public void StopBackup()
        {
            if (!isRunning)
            {
                throw new InvalidOperationException("Backup process is not running.");
            }

            cancellationTokenSource.Cancel();
            timer.Stop();
            timer.Dispose();
            isRunning = false;
            Console.WriteLine("Backup process stopped.");
            BackupStopped?.Invoke(this, EventArgs.Empty);
        }

        private async void TimerElapsedAsync(object sender, ElapsedEventArgs e)
        {
            await BackupFilesAsync();
        }

        private async Task BackupFilesAsync()
        {
            try
            {
                await semaphore.WaitAsync(cancellationTokenSource.Token);
                await CopyFilesFromListAsync(filePath, Settings.DestinationPath);
                logger.Log("Backup completed successfully.");
            }
            catch (OperationCanceledException)
            {
                logger.Log("Backup operation was canceled.");
            }
            catch (Exception ex)
            {
                logger.Log($"Error during backup: {ex.Message}");
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task CopyFilesFromListAsync(string filePath, string destinationPath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string json;
                    using (var reader = new StreamReader(filePath))
                    {
                        json = await reader.ReadToEndAsync();
                    }

                    List<string> fileNames = JsonConvert.DeserializeObject<List<string>>(json);

                    if (!Directory.Exists(destinationPath))
                    {
                        Directory.CreateDirectory(destinationPath);
                    }

                    foreach (string fileName in fileNames)
                    {
                        string sourceFilePath = Path.Combine(Settings.SourcePath, fileName);
                        string destinationFilePath = Path.Combine(destinationPath, Path.GetFileName(fileName));

                        await CopyFileAsync(sourceFilePath, destinationFilePath, cancellationTokenSource.Token);
                        Console.WriteLine($"File {fileName} copied to {destinationFilePath}");
                    }

                    Console.WriteLine("Operation completed.");
                }
                else
                {
                    Console.WriteLine("File with names not found.");
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Copy operation was canceled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private async Task CopyFileAsync(string sourceFilePath, string destinationFilePath, CancellationToken cancellationToken)
        {
            const int bufferSize = 81920; // Default buffer size used by Stream.CopyToAsync
            try
            {
                using (var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync: true))
                using (var destinationStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, useAsync: true))
                {
                    await sourceStream.CopyToAsync(destinationStream, bufferSize, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                logger.Log($"Error copying file {sourceFilePath} to {destinationFilePath}: {ex.Message}");
                throw;
            }
        }
    }

    public class Logger : ILogger
    {
        private readonly string logFilePath = "log.txt";

        public void Log(string message)
        {
            string logMessage = $"[LOG] {DateTime.Now}: {message}";
            Console.WriteLine(logMessage);
            File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
        }
    }

    public class BackupSettingsModel : IBackupSettings
    {
        public string SourcePath { get; set; }
        public string DestinationPath { get; set; }
        public int IntervalMinutes { get; set; }
    }
}
