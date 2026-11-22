using System;
using CleanMyRoom.ColorSorting;
using CleanMyRoom.Net;
using NUnit.Framework;

namespace CleanMyRoom.Tests.ColorSorting
{
    /// <summary>
    /// Issue #022 EditMode verification of progress tracking. MATLAB remains
    /// the authority: these tests inject protocol-shaped messages and check
    /// that Unity only mirrors what MATLAB reported.
    /// </summary>
    public sealed class ColorSortingLogicTests
    {
        [Test]
        public void CorrectResponseConfirmsObjectAndUsesMatlabScore()
        {
            ColorSortingProgress progress = new ColorSortingProgress();
            Assert.IsTrue(progress.RegisterPending("req_1", "object_01", "basket_red", 0f));

            SortingEvent result = progress.HandlePlaceResponse(PlaceResponse(
                "req_1", true, true, 1, "Correct placement"));

            Assert.AreEqual(SortingOutcome.Accepted, result.Outcome);
            Assert.IsTrue(result.Handled);
            Assert.IsTrue(progress.IsObjectConfirmed("object_01"));
            Assert.AreEqual(1, progress.CorrectlyPlacedCount,
                "Progress must come from PLACE_RES.score, not local counting.");
            Assert.AreEqual(0, progress.PendingCount);
            Assert.IsFalse(progress.CanStartPlacement("object_01"));
            Assert.IsFalse(progress.IsComplete);
        }

        [Test]
        public void IncorrectResponseDoesNotConfirmOrScoreAndObjectCanBeRetried()
        {
            ColorSortingProgress progress = new ColorSortingProgress();
            progress.RegisterPending("req_wrong", "object_01", "basket_blue", 0f);

            SortingEvent result = progress.HandlePlaceResponse(PlaceResponse(
                "req_wrong", true, false, 0, "Incorrect placement. Try again."));

            Assert.AreEqual(SortingOutcome.Rejected, result.Outcome);
            Assert.IsFalse(progress.IsObjectConfirmed("object_01"));
            Assert.AreEqual(0, progress.CorrectlyPlacedCount);
            Assert.IsTrue(progress.CanStartPlacement("object_01"),
                "A wrong basket must allow the child to try again.");

            SortingEvent duplicate = progress.HandlePlaceResponse(PlaceResponse(
                "req_wrong", true, false, 0, "Late duplicate"));
            Assert.IsFalse(duplicate.Handled, "A late duplicate must not trigger more feedback.");
        }

        [Test]
        public void TwoRequestsForTheSameObjectCannotBeInFlight()
        {
            ColorSortingProgress progress = new ColorSortingProgress();
            Assert.IsTrue(progress.RegisterPending("req_a", "object_01", "basket_red", 0f));
            Assert.IsFalse(progress.RegisterPending("req_b", "object_01", "basket_blue", 0f),
                "Duplicate placement prevention must work before MATLAB replies.");
            Assert.AreEqual(1, progress.PendingCount);
        }

        [Test]
        public void InvalidResponseAndErrorDoNotScore()
        {
            ColorSortingProgress progress = new ColorSortingProgress();
            progress.RegisterPending("req_invalid", "object_01", "basket_red", 0f);

            SortingEvent invalid = progress.HandlePlaceResponse(PlaceResponse(
                "req_invalid", false, true, 1, "bad reply"));
            Assert.AreEqual(SortingOutcome.InvalidResponse, invalid.Outcome);
            Assert.IsFalse(progress.IsObjectConfirmed("object_01"));

            progress.RegisterPending("req_error", "object_02", "basket_blue", 0f);
            WireMessage error = new WireMessage
            {
                type = ProtocolConstants.TypeError,
                requestId = "req_error",
                code = "INVALID_REQUEST",
                message = "MATLAB could not check that placement."
            };
            Assert.AreEqual(SortingOutcome.ProtocolError,
                progress.HandleError(error).Outcome);
            Assert.AreEqual(0, progress.CorrectlyPlacedCount);
        }

        [Test]
        public void TimeoutAndDisconnectReturnObjectsToThePool()
        {
            ColorSortingProgress progress = new ColorSortingProgress();
            progress.RegisterPending("req_timeout", "object_01", "basket_red", 10f);

            SortingEvent[] expired = progress.CheckTimeouts(18f, 8f);
            Assert.AreEqual(1, expired.Length);
            Assert.AreEqual(SortingOutcome.Timeout, expired[0].Outcome);
            Assert.IsTrue(progress.CanStartPlacement("object_01"));

            progress.RegisterPending("req_drop", "object_02", "basket_blue", 20f);
            SortingEvent[] cancelled = progress.CancelAllPending(
                SortingOutcome.Disconnected, "MATLAB disconnected");
            Assert.AreEqual(1, cancelled.Length);
            Assert.AreEqual(SortingOutcome.Disconnected, cancelled[0].Outcome);
            Assert.IsTrue(progress.CanStartPlacement("object_02"));
        }

        [Test]
        public void CompletionNeedsAllTwelveMatlabScoresAndOneActivityResult()
        {
            ColorSortingProgress progress = new ColorSortingProgress();
            string lastRequestId = string.Empty;

            for (int i = 0; i < ColorSortingCatalog.ObjectCount; i++)
            {
                ColorSortingCatalog.ObjectInfo info = ColorSortingCatalog.AllObjects[i];
                lastRequestId = "req_" + i;
                Assert.IsTrue(progress.RegisterPending(
                    lastRequestId, info.Id, info.ColorId, i));
                SortingEvent placement = progress.HandlePlaceResponse(PlaceResponse(
                    lastRequestId, true, true, i + 1, "Correct placement"));
                Assert.AreEqual(SortingOutcome.Accepted, placement.Outcome);
            }

            Assert.AreEqual(12, progress.CorrectlyPlacedCount);
            Assert.IsFalse(progress.IsComplete,
                "MATLAB must send ACTIVITY_RESULT before completion is declared.");

            SortingEvent completed = progress.HandleActivityResult(ActivityResult(
                lastRequestId, true, 1));
            Assert.AreEqual(SortingOutcome.Completed, completed.Outcome);
            Assert.IsTrue(progress.IsComplete);
            Assert.AreEqual(1, progress.TotalStars);

            SortingEvent repeated = progress.HandleActivityResult(ActivityResult(
                lastRequestId, true, 2));
            Assert.IsFalse(repeated.Handled, "A repeated ACTIVITY_RESULT must not award again.");
            Assert.AreEqual(1, progress.TotalStars);
            Assert.IsFalse(progress.CanStartPlacement("object_01"));
        }

        [Test]
        public void NotCompletedActivityResultOnlyUpdatesScore()
        {
            ColorSortingProgress progress = new ColorSortingProgress();
            progress.RegisterPending("req_1", "object_01", "basket_red", 0f);
            progress.HandlePlaceResponse(PlaceResponse("req_1", true, true, 1, "Correct placement"));

            SortingEvent result = progress.HandleActivityResult(ActivityResult("req_1", false, 0));
            Assert.AreEqual(SortingOutcome.ScoreUpdated, result.Outcome);
            Assert.IsFalse(progress.IsComplete);
            Assert.AreEqual(0, progress.TotalStars);
        }

        [Test]
        public void ActivityResultForAnotherActivityIsIgnored()
        {
            ColorSortingProgress progress = new ColorSortingProgress();
            progress.RegisterPending("req_1", "object_01", "basket_red", 0f);
            progress.HandlePlaceResponse(PlaceResponse("req_1", true, true, 1, "Correct placement"));

            WireMessage other = ActivityResult("req_1", true, 1);
            other.activity = "TASTE";
            Assert.IsFalse(progress.HandleActivityResult(other).Handled);
            Assert.IsFalse(progress.IsComplete);
        }

        private static WireMessage PlaceResponse(
            string requestId, bool valid, bool correct, int score, string message)
        {
            return new WireMessage
            {
                type = ProtocolConstants.TypePlaceResponse,
                requestId = requestId,
                activity = ColorSortingCatalog.ActivityId,
                valid = valid,
                correct = correct,
                score = score,
                message = message
            };
        }

        private static WireMessage ActivityResult(string requestId, bool completed, int stars)
        {
            return new WireMessage
            {
                type = ProtocolConstants.TypeActivityResult,
                requestId = requestId,
                activity = ColorSortingCatalog.ActivityId,
                completed = completed,
                stars = stars,
                score = 0,
                message = "Correct placement"
            };
        }
    }
}
