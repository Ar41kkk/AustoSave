using AutoSave;
using System.Threading.Tasks;
using System;

namespace AutoSave
{
    public interface IBackupManager
    {
        event EventHandler BackupStarted;
        event EventHandler BackupStopped;
        Task StartBackupAsync();
        void StopBackup();
        void Initialize(BackupSettingsModel settings);
        bool IsRunning { get; }
    }
}
