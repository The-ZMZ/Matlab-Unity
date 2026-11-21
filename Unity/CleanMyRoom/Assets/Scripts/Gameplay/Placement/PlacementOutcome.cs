namespace CleanMyRoom.Gameplay.Placement
{
    public enum PlacementOutcomeKind
    {
        None,
        AcceptedCorrect,
        AcceptedIncorrect,
        Rejected,
        Interrupted,
        TimedOut
    }

    /// <summary>
    /// Result of one PLACE_REQ. The correctness flag is copied from MATLAB's
    /// PLACE_RES; Unity never decides it locally.
    /// </summary>
    public readonly struct PlacementOutcome
    {
        public PlacementOutcome(
            PlacementOutcomeKind kind,
            string requestId,
            string activityId,
            string itemId,
            string targetId,
            int score,
            string code,
            string message)
        {
            Kind = kind;
            RequestId = requestId;
            ActivityId = activityId;
            ItemId = itemId;
            TargetId = targetId;
            Score = score;
            Code = code;
            Message = message;
        }

        public PlacementOutcomeKind Kind { get; }

        public string RequestId { get; }

        public string ActivityId { get; }

        public string ItemId { get; }

        public string TargetId { get; }

        public int Score { get; }

        public string Code { get; }

        public string Message { get; }

        public bool IsCorrect { get { return Kind == PlacementOutcomeKind.AcceptedCorrect; } }

        public bool IsAccepted
        {
            get
            {
                return Kind == PlacementOutcomeKind.AcceptedCorrect ||
                       Kind == PlacementOutcomeKind.AcceptedIncorrect;
            }
        }
    }
}
