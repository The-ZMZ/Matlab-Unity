namespace CleanMyRoom.Gameplay.Activities
{
    public enum DragPlacementStatus
    {
        None,
        Started,
        CompletedCorrect,
        CompletedIncorrect,
        Rejected,
        Cancelled,
        Unavailable
    }

    /// <summary>Presentation feedback for one mouse drag; not an answer decision.</summary>
    public readonly struct DragPlacementResult
    {
        public DragPlacementResult(
            DragPlacementStatus status,
            string itemId,
            string targetId,
            string message,
            string requestId = "")
        {
            Status = status;
            ItemId = itemId ?? string.Empty;
            TargetId = targetId ?? string.Empty;
            Message = message ?? string.Empty;
            RequestId = requestId ?? string.Empty;
        }

        public DragPlacementStatus Status { get; }

        public string ItemId { get; }

        public string TargetId { get; }

        public string Message { get; }

        public string RequestId { get; }
    }
}
