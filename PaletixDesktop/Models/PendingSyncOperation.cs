using System;

namespace PaletixDesktop.Models
{
    public sealed class PendingSyncOperation
    {
        public string QueueId { get; init; } = "";
        public string EntityName { get; init; } = "";
        public string EntityLabel { get; init; } = "";
        public string EntityId { get; init; } = "";
        public string Method { get; init; } = "";
        public string OperationLabel { get; init; } = "";
        public string Status { get; init; } = "";
        public string StatusLabel => Status == "Error" ? "Error sync" : "Pendent";
        public string Endpoint { get; init; } = "";
        public string Payload { get; init; } = "";
        public string Summary { get; init; } = "";
        public DateTimeOffset UpdatedUtc { get; init; }
        public bool IsError => Status == "Error";
        public string UpdatedText => UpdatedUtc.ToLocalTime().ToString("dd/MM HH:mm");
    }
}
