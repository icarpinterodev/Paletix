namespace PaletixDesktop.Models
{
    public sealed class SyncStatus
    {
        public bool IsOnline { get; init; }
        public bool IsSyncing { get; init; }
        public int PendingOperations { get; init; }
        public string Message { get; init; } = "";
    }
}
