using System;
using System.Collections.Generic;
using CleanMyRoom.Net;

namespace CleanMyRoom.ColorSorting
{
    /// <summary>Outcome of one MATLAB message as seen by the activity UI.</summary>
    public enum SortingOutcome
    {
        None,
        Accepted,
        Rejected,
        InvalidResponse,
        ProtocolError,
        Timeout,
        Disconnected,
        ScoreUpdated,
        Completed
    }

    /// <summary>
    /// A small presentation event. It describes what MATLAB reported; it never
    /// makes the correctness decision itself.
    /// </summary>
    public sealed class SortingEvent
    {
        public bool Handled;
        public SortingOutcome Outcome;
        public string RequestId = string.Empty;
        public string ObjectId = string.Empty;
        public string TargetId = string.Empty;
        public string Message = string.Empty;
        public int Score;
        public int Stars;
        /// <summary>Value of ACTIVITY_RESULT.completed, verbatim from MATLAB.</summary>
        public bool CompletedFlag;

        public static SortingEvent None()
        {
            return new SortingEvent { Outcome = SortingOutcome.None };
        }
    }

    /// <summary>
    /// Tracks the state of one Color Sorting session from MATLAB messages.
    ///
    /// This is a plain C# class (no MonoBehaviour) so it can be unit tested
    /// without a running Unity scene. It does NOT know the correct answers:
    /// every correctness flag and the running activity score come from
    /// PLACE_RES, while stars/completion come from ACTIVITY_RESULT/GAME_PROGRESS.
    /// </summary>
    public sealed class ColorSortingProgress
    {
        private sealed class PendingPlacement
        {
            public string RequestId;
            public string ObjectId;
            public string TargetId;
            public float StartedAt;
        }

        private readonly Dictionary<string, PendingPlacement> pending =
            new Dictionary<string, PendingPlacement>(StringComparer.Ordinal);
        private readonly HashSet<string> confirmedObjects =
            new HashSet<string>(StringComparer.Ordinal);
        /// <summary>
        /// Request ids for which MATLAB has already sent a completing result.
        /// A non-completed result may precede completion on the same request;
        /// once completion has been raised, later messages for that request are
        /// duplicates and must not raise another star.
        /// </summary>
        private readonly HashSet<string> completedActivityResultRequestIds =
            new HashSet<string>(StringComparer.Ordinal);

        private int validatedScore;
        private int totalStars;
        private bool completionRaised;
        private string lastResolvedRequestId = string.Empty;

        /// <summary>Raised once when MATLAB confirms that all objects are placed.</summary>
        public event Action<int> CompletionAchieved;

        /// <summary>Raised for every handled placement/message outcome.</summary>
        public event Action<SortingEvent> OutcomeRaised;

        /// <summary>Number of correctly placed objects reported by MATLAB.</summary>
        public int CorrectlyPlacedCount { get { return validatedScore; } }

        /// <summary>Total stars reported by MATLAB (not computed locally).</summary>
        public int TotalStars { get { return totalStars; } }

        /// <summary>True only after a genuine MATLAB completion message.</summary>
        public bool IsComplete { get { return completionRaised; } }

        /// <summary>Number of requests waiting for a MATLAB reply.</summary>
        public int PendingCount { get { return pending.Count; } }

        public bool IsObjectConfirmed(string objectId)
        {
            return !string.IsNullOrEmpty(objectId) && confirmedObjects.Contains(objectId);
        }

        /// <summary>
        /// False when an object is already confirmed or already in flight. This
        /// prevents repeated sounds and duplicate scoring from one interaction.
        /// </summary>
        public bool CanStartPlacement(string objectId)
        {
            if (string.IsNullOrEmpty(objectId) || completionRaised ||
                confirmedObjects.Contains(objectId))
            {
                return false;
            }
            foreach (KeyValuePair<string, PendingPlacement> pair in pending)
            {
                if (string.Equals(pair.Value.ObjectId, objectId, StringComparison.Ordinal))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Call after the network layer has actually sent a PLACE_REQ.</summary>
        public bool RegisterPending(
            string requestId, string objectId, string targetId, float nowSeconds)
        {
            if (string.IsNullOrEmpty(requestId) || !CanStartPlacement(objectId))
            {
                return false;
            }
            if (pending.ContainsKey(requestId))
            {
                return false;
            }

            pending.Add(requestId, new PendingPlacement
            {
                RequestId = requestId,
                ObjectId = objectId,
                TargetId = targetId,
                StartedAt = nowSeconds
            });
            return true;
        }

        /// <summary>Handle a PLACE_RES message. MATLAB supplies 'correct'.</summary>
        public SortingEvent HandlePlaceResponse(WireMessage message)
        {
            if (message == null || !message.IsType(ProtocolConstants.TypePlaceResponse) ||
                string.IsNullOrEmpty(message.requestId))
            {
                return Publish(SortingEvent.None());
            }

            PendingPlacement placement;
            if (!pending.TryGetValue(message.requestId, out placement))
            {
                // Duplicate or late replies are ignored so one drop cannot score twice.
                return Publish(SortingEvent.None());
            }
            pending.Remove(message.requestId);
            lastResolvedRequestId = message.requestId;

            SortingEvent result = NewEvent(placement, message.requestId);
            if (!message.valid)
            {
                result.Outcome = SortingOutcome.InvalidResponse;
                result.Message = "MATLAB rejected the placement request.";
            }
            else
            {
                // PLACE_RES.score is MATLAB's running count of distinct correct
                // placements (Protocol.md 4.2). It is a reported value, not a
                // locally computed judgement, and an invalid reply cannot supply it.
                if (message.score > validatedScore)
                {
                    validatedScore = message.score;
                }

                if (message.correct)
                {
                    result.Outcome = SortingOutcome.Accepted;
                    confirmedObjects.Add(placement.ObjectId);
                    result.Message = string.IsNullOrEmpty(message.message)
                        ? "Correct placement"
                        : message.message;
                }
                else
                {
                    result.Outcome = SortingOutcome.Rejected;
                    result.Message = string.IsNullOrEmpty(message.message)
                        ? "Not quite - try another basket"
                        : message.message;
                }
            }
            return Publish(result);
        }

        /// <summary>
        /// Handle an ACTIVITY_RESULT message. Score and completion come only
        /// from MATLAB; Unity never increments progress on its own.
        /// </summary>
        public SortingEvent HandleActivityResult(WireMessage message)
        {
            if (message == null || !message.IsType(ProtocolConstants.TypeActivityResult) ||
                !string.Equals(message.activity, ColorSortingCatalog.ActivityId,
                    StringComparison.Ordinal) ||
                string.IsNullOrEmpty(message.requestId) ||
                !string.Equals(message.requestId, lastResolvedRequestId,
                    StringComparison.Ordinal) ||
                completedActivityResultRequestIds.Contains(message.requestId))
            {
                return Publish(SortingEvent.None());
            }

            if (message.score > validatedScore)
            {
                validatedScore = message.score;
            }
            if (message.stars > totalStars)
            {
                totalStars = message.stars;
            }

            SortingEvent result = SortingEvent.None();
            result.Handled = true;
            result.RequestId = message.requestId;
            result.Score = validatedScore;
            result.Stars = totalStars;
            result.CompletedFlag = message.completed;
            result.Message = message.message ?? string.Empty;

            bool allObjectsConfirmed = confirmedObjects.Count >= ColorSortingCatalog.ObjectCount;
            if (message.completed && validatedScore >= ColorSortingCatalog.ObjectCount &&
                allObjectsConfirmed && !completionRaised)
            {
                completionRaised = true;
                completedActivityResultRequestIds.Add(message.requestId);
                result.Outcome = SortingOutcome.Completed;
                Action<int> handler = CompletionAchieved;
                if (handler != null)
                {
                    handler(totalStars);
                }
            }
            else
            {
                result.Outcome = SortingOutcome.ScoreUpdated;
            }
            return Publish(result);
        }

        /// <summary>Handle a GAME_PROGRESS message (stars are MATLAB's total).</summary>
        public SortingEvent HandleGameProgress(WireMessage message)
        {
            if (message == null || !message.IsType(ProtocolConstants.TypeGameProgress))
            {
                return Publish(SortingEvent.None());
            }

            if (message.stars > totalStars)
            {
                totalStars = message.stars;
            }

            SortingEvent result = SortingEvent.None();
            result.Handled = true;
            result.Outcome = SortingOutcome.ScoreUpdated;
            result.Stars = totalStars;
            result.Score = validatedScore;
            result.Message = message.message ?? string.Empty;
            return Publish(result);
        }

        /// <summary>Handle a protocol ERROR belonging to a pending placement.</summary>
        public SortingEvent HandleError(WireMessage message)
        {
            if (message == null || !message.IsType(ProtocolConstants.TypeError))
            {
                return Publish(SortingEvent.None());
            }

            PendingPlacement placement;
            if (string.IsNullOrEmpty(message.requestId) ||
                !pending.TryGetValue(message.requestId, out placement))
            {
                return Publish(SortingEvent.None());
            }
            pending.Remove(message.requestId);

            SortingEvent result = NewEvent(placement, message.requestId);
            result.Outcome = SortingOutcome.ProtocolError;
            result.Message = string.IsNullOrEmpty(message.message)
                ? "MATLAB could not check that placement."
                : message.message;
            return Publish(result);
        }

        /// <summary>Expire placements that never received an answer from MATLAB.</summary>
        public SortingEvent[] CheckTimeouts(float nowSeconds, float timeoutSeconds)
        {
            if (timeoutSeconds <= 0f)
            {
                return new SortingEvent[0];
            }

            List<string> expired = new List<string>();
            foreach (KeyValuePair<string, PendingPlacement> pair in pending)
            {
                if (nowSeconds - pair.Value.StartedAt >= timeoutSeconds)
                {
                    expired.Add(pair.Key);
                }
            }

            List<SortingEvent> results = new List<SortingEvent>();
            for (int i = 0; i < expired.Count; i++)
            {
                PendingPlacement placement;
                if (pending.TryGetValue(expired[i], out placement))
                {
                    pending.Remove(expired[i]);
                    SortingEvent result = NewEvent(placement, expired[i]);
                    result.Outcome = SortingOutcome.Timeout;
                    result.Message = "MATLAB did not answer in time. Try again.";
                    results.Add(Publish(result));
                }
            }
            return results.ToArray();
        }

        /// <summary>Abandon all in-flight placements, e.g. after a disconnect.</summary>
        public SortingEvent[] CancelAllPending(SortingOutcome outcome, string message)
        {
            List<PendingPlacement> values = new List<PendingPlacement>(pending.Values);
            pending.Clear();
            List<SortingEvent> results = new List<SortingEvent>();
            for (int i = 0; i < values.Count; i++)
            {
                SortingEvent result = NewEvent(values[i], values[i].RequestId);
                result.Outcome = outcome;
                result.Message = message;
                results.Add(Publish(result));
            }
            return results.ToArray();
        }

        private static SortingEvent NewEvent(PendingPlacement placement, string requestId)
        {
            return new SortingEvent
            {
                Handled = true,
                RequestId = requestId,
                ObjectId = placement.ObjectId,
                TargetId = placement.TargetId,
                Score = 0,
                Stars = 0
            };
        }

        private SortingEvent Publish(SortingEvent result)
        {
            if (result.Handled)
            {
                result.Score = validatedScore;
                result.Stars = totalStars;

                Action<SortingEvent> handler = OutcomeRaised;
                if (handler != null)
                {
                    handler(result);
                }
            }
            return result;
        }
    }
}
