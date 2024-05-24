using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using AutoSave;
using Newtonsoft.Json;

namespace AutoSave
{
    public partial class Form1 : Form
    {
        private IBackupManager backupManager;
        private string selectedFolderPath;
        private List<string> selectedFiles;
        private int interval = 10; // Інтервал за замовчуванням - 10 хвилин
        private BackupSettingsModel backupSettings;
        private NotifyIcon trayIcon;
        private bool IsBackupRunning { get; set; }

        public List<string> SelectedFiles
        {
            get { return selectedFiles; }
            set { selectedFiles = value; }
        }

        public class CircularPictureBox : PictureBox
        {
            protected override void OnPaint(PaintEventArgs pe)
            {
                using (GraphicsPath path = new GraphicsPath())
                {
                    path.AddEllipse(0, 0, this.Width - 1, this.Height - 1);
                    this.Region = new Region(path);
                    pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    pe.Graphics.FillEllipse(new SolidBrush(this.BackColor), 0, 0, this.Width - 1, this.Height - 1);
                    base.OnPaint(pe);
                }
            }
        }

        public Form1()
        {
            InitializeComponent();
            InitializeCustomComponents();
            InitializeBackupManager();
            InitializeTrayIcon();
            LoadSettings();
        }

        private void InitializeCustomComponents()
        {
            this.pictureBoxStatus = new CircularPictureBox
            {
                Location = new Point(165, 230),
                Name = "pictureBoxStatus",
                Size = new Size(15, 15),
                TabIndex = 0,
                TabStop = false,
                BackColor = Color.Red // Initially set to indicate not running
            };
            this.Controls.Add(this.pictureBoxStatus);

            selectedFiles = new List<string>(); // Ініціалізація списку
            IsBackupRunning = false; // Початкове значення - процес не запущено

            this.FormClosing += Form1_FormClosing; // Підписка на подію закриття форми
        }

        private void InitializeBackupManager()
        {
            ILogger logger = new Logger();
            backupManager = new BackupManager(logger);
            backupManager.BackupStarted += BackupManager_BackupStarted;
            backupManager.BackupStopped += BackupManager_BackupStopped;
        }

        private void InitializeTrayIcon()
        {
            trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application, // You can replace this with your own icon
                Text = "Backup Manager",
                Visible = true
            };
            trayIcon.DoubleClick += TrayIcon_DoubleClick;
            trayIcon.ContextMenuStrip = new ContextMenuStrip();
            trayIcon.ContextMenuStrip.Items.Add("Відкрити", null, (s, e) => ShowForm());
            trayIcon.ContextMenuStrip.Items.Add("Закрити", null, (s, e) => ExitApplication()); // Add the exit option
        }

        private void UpdateStatusLabel()
        {
            if (IsBackupRunning)
            {
                Label_Status.Text = "Статус роботи програми: Запущено";
                pictureBoxStatus.BackColor = Color.Green;
            }
            else
            {
                Label_Status.Text = "Статус роботи програми: Не запущено";
                pictureBoxStatus.BackColor = Color.Red;
            }
        }

        private void BackupManager_BackupStarted(object sender, EventArgs e)
        {
            IsBackupRunning = true;
            UpdateStatusLabel();
        }

        private void BackupManager_BackupStopped(object sender, EventArgs e)
        {
            IsBackupRunning = false;
            UpdateStatusLabel();
        }

        private void ShowForm()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.BringToFront();
        }

        private void ExitApplication()
        {
            try
            {
                if (backupManager != null && backupManager.IsRunning)
                {
                    backupManager.StopBackup();
                    LogMessage("Backup process stopped.");
                }
                SaveSettings(); // Save any necessary settings before exit
            }
            catch (Exception ex)
            {
                LogMessage($"Error during exit: {ex.Message}");
            }
            finally
            {
                trayIcon.Visible = false; // Hide tray icon
                trayIcon.Dispose(); // Dispose of the tray icon
                Application.Exit(); // Attempt to exit the application

                // If the application doesn't exit, forcefully terminate it
                Environment.Exit(0);
            }
        }

        internal static class Program
        {
            [STAThread]
            private static void Main()
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
            }
        }

        private async void LoadSettings()
        {
            await LoadSelectedFiles();
            await LoadBackupSettings();
        }

        private async Task LoadSelectedFiles()
        {
            string filePath = "selected_files.json";
            if (!File.Exists(filePath))
            {
                LogMessage("No files selected for backup.");
                selectedFiles = new List<string>(); // Initialize an empty list if file doesn't exist
                return;
            }

            string json;
            using (var reader = new StreamReader(filePath))
            {
                json = await reader.ReadToEndAsync();
            }

            selectedFiles = JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>(); // Handle potential null value
            LogMessage("Selected files loaded.");
        }

        private async Task SaveSelectedFiles()
        {
            string filePath = "selected_files.json";
            string json = JsonConvert.SerializeObject(selectedFiles);
            using (var writer = new StreamWriter(filePath))
            {
                await writer.WriteAsync(json);
            }
            LogMessage("Selected files saved to disk.");
        }

        private async Task LoadBackupSettings()
        {
            string settingsPath = "backup_settings.json";
            if (!File.Exists(settingsPath))
            {
                LogMessage("No backup settings found.");
                backupSettings = new BackupSettingsModel(); // Initialize default settings if file doesn't exist
                return;
            }

            string json;
            using (var reader = new StreamReader(settingsPath))
            {
                json = await reader.ReadToEndAsync();
            }

            backupSettings = JsonConvert.DeserializeObject<BackupSettingsModel>(json) ?? new BackupSettingsModel(); // Handle potential null value
            selectedFolderPath = backupSettings.DestinationPath ?? string.Empty; // Default to empty string if null
            interval = backupSettings.IntervalMinutes > 0 ? backupSettings.IntervalMinutes : 10; // Default to 10 if invalid
            label_folder.Text = selectedFolderPath;
            textBoxInterval.Text = interval.ToString();
            LogMessage("Backup settings loaded.");
        }

        private async Task SaveBackupSettings()
        {
            var settings = new BackupSettingsModel
            {
                SourcePath = Environment.CurrentDirectory,
                DestinationPath = selectedFolderPath,
                IntervalMinutes = interval
            };

            string settingsPath = "backup_settings.json";
            string json = JsonConvert.SerializeObject(settings);
            using (var writer = new StreamWriter(settingsPath))
            {
                await writer.WriteAsync(json);
            }
            LogMessage("Backup settings saved to disk.");
        }

        private async void startButton_Click(object sender, EventArgs e)
        {
            if (backupManager.IsRunning)
            {
                MessageBox.Show("Backup process is already running.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogMessage("Backup process is already running.");
                return;
            }
            if (string.IsNullOrEmpty(selectedFolderPath))
            {
                MessageBox.Show("Please provide destination path.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogMessage("Destination path not provided.");
                return;
            }

            if (backupManager == null)
            {
                MessageBox.Show("BackupManager is not initialized.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogMessage("BackupManager is not initialized.");
                return;
            }

            if (interval <= 0)
            {
                interval = 10; // Встановлюємо інтервал за замовчуванням, якщо він не введений або некоректний
                LogMessage("Invalid input for interval. Resetting to default (10 minutes).");
            }

            var backupSettings = new BackupSettingsModel
            {
                SourcePath = Environment.CurrentDirectory, // Приклад вихідного шляху
                DestinationPath = selectedFolderPath,
                IntervalMinutes = interval
            };
            backupManager.Initialize(backupSettings);

            try
            {
                await backupManager.StartBackupAsync();
                LogMessage("Backup process started.");
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogMessage($"Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogMessage($"Unexpected error: {ex.Message}");
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true; // Cancel the default close operation
            this.Hide(); // Hide the form instead
            SaveSettings(); // Save settings when form is closed
            LogMessage("Form is hidden, but the background process continues to run.");
        }

        private void TrayIcon_DoubleClick(object sender, EventArgs e)
        {
            ShowForm();
        }

        private void textBoxInterval_TextChanged(object sender, EventArgs e)
        {
            if (!int.TryParse(textBoxInterval.Text, out interval) || interval <= 0)
            {
                interval = 10; // Встановлюємо інтервал за замовчуванням
                MessageBox.Show("Please enter a valid positive number for the interval. Resetting to default (10 minutes).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogMessage("Invalid input for interval. Resetting to default (10 minutes).");
            }
            else
            {
                LogMessage($"Backup interval set: {interval} minutes.");
            }
        }

        private void chooseFolderButton_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.Description = "Select a folder for backup";
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    selectedFolderPath = folderBrowserDialog.SelectedPath;
                    label_folder.Text = selectedFolderPath;
                    LogMessage($"Destination folder selected: {selectedFolderPath}");
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Form2 form2 = new Form2();
            form2.Owner = this;
            form2.FilesAdded += Form2_FilesAdded; // Підписуємось на подію
            form2.Show();
            LogMessage("Form2 opened.");
        }

        private void Form2_FilesAdded(List<string> files)
        {
            selectedFiles.AddRange(files);
            SaveSelectedFiles();
            LogMessage("Files added from Form2.");
        }

        private void Form2_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.Show();
            LogMessage("Form2 closed.");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                backupManager.StopBackup();
                LogMessage("Backup process stopped.");
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogMessage($"Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogMessage($"Unexpected error: {ex.Message}");
            }
        }

        private void SaveSettings()
        {
            SaveSelectedFiles();
            SaveBackupSettings();
        }

        private void LogMessage(string message)
        {
            Console.WriteLine(message);
        }
    }
}
