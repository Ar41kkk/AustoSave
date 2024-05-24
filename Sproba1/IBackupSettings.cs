namespace AutoSave
{
    public interface IBackupSettings
    {
        string SourcePath { get; set; }
        string DestinationPath { get; set; }
        int IntervalMinutes { get; set; }
    }
}
