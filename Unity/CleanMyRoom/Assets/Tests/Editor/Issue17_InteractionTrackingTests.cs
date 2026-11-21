using System;
using CleanMyRoom.Gameplay;
using CleanMyRoom.Player;
using CleanMyRoom.Player.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CleanMyRoom.Tests
{
    /// <summary>
    /// Issue #17 coverage: tracked interaction state, one action per E press,
    /// correct handling of held/released keys, and no interaction from movement
    /// keys or idle frames.
    /// </summary>
    [Category("Issue17")]
    public sealed class Issue17_InteractionTrackingTests : InputTestFixture
    {
        [SetUp]
        public void ClearNavigationListeners()
        {
            ActivityNavigation.ClearListenersForTests();
            KeyboardInput.ResetToUnitySource();
        }

        [TearDown]
        public void RestoreState()
        {
            KeyboardInput.ResetToUnitySource();
            ActivityNavigation.ClearListenersForTests();
        }

        [Test]
        public void NoInputNeverProducesAnInteraction()
        {
            KeyboardInput.Source = new StubKeyboardSource { Snapshot = default };
            InteractionInputTracker tracker = new InteractionInputTracker();

            Assert.IsFalse(tracker.ConsumePress());
        }

        [Test]
        public void MovementKeysDoNotTriggerAnInteraction()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            InteractionInputTracker tracker = new InteractionInputTracker();

            Press(keyboard.wKey);
            KeyboardInputSnapshot snapshot = KeyboardInput.Read();
            Assert.IsTrue(snapshot.IsMoving);

            Assert.IsFalse(tracker.ConsumePress(snapshot));
            Assert.IsFalse(tracker.IsInteractHeld);
        }

        [Test]
        public void OnePressIsConsumedOnceThenReatsAfterRelease()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            InteractionInputTracker tracker = new InteractionInputTracker();

            Press(keyboard.eKey);
            Assert.IsTrue(tracker.ConsumePress());
            Assert.IsFalse(tracker.ConsumePress(), "A second poll in the same frame must not fire.");
            Assert.IsTrue(tracker.IsInteractHeld);

            InputSystem.Update();
            Assert.IsFalse(tracker.ConsumePress(), "A held key must not repeat the interaction.");

            Release(keyboard.eKey);
            Assert.IsFalse(tracker.ConsumePress());
            Assert.IsTrue(tracker.WasInteractReleased);
            Assert.IsFalse(tracker.IsInteractHeld);

            Press(keyboard.eKey);
            Assert.IsTrue(tracker.ConsumePress(), "A fresh press after release must work.");
        }

        [Test]
        public void InteractionControllerFiresOncePerPress()
        {
            const string colorPath = "Assets/Scenes/Activities/ColorActivity.unity";

            GameObject furniture = new GameObject("TestBed");
            furniture.transform.position = new Vector3(1f, 0f, 0f);
            furniture.AddComponent<BoxCollider>();
            furniture.GetComponent<BoxCollider>().isTrigger = true;
            furniture.AddComponent<InteractableFurniture>().Configure(
                ActivityIds.Color, "Bed", colorPath, 3f);

            GameObject player = new GameObject("TestPlayer");
            player.transform.position = Vector3.zero;
            player.AddComponent<CharacterController>();
            player.AddComponent<PlayerController>();
            InteractionController controller = player.AddComponent<InteractionController>();

            int requests = 0;
            Action<string, string> handler = (activityId, scenePath) => requests++;
            ActivityNavigation.OpenRequested += handler;
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();

            try
            {
                Physics.SyncTransforms();
                controller.Refresh();

                Press(keyboard.eKey);
                Assert.IsTrue(controller.HandleInteractionInput());
                Assert.IsFalse(controller.HandleInteractionInput(),
                    "Holding or re-polling E must not repeat the activity request.");
                Assert.AreEqual(1, requests);

                Release(keyboard.eKey);
                Assert.IsFalse(controller.HandleInteractionInput(),
                    "The release frame must clear the latch without firing.");

                Press(keyboard.eKey);
                Assert.IsTrue(controller.HandleInteractionInput());
                Assert.AreEqual(2, requests);
            }
            finally
            {
                ActivityNavigation.OpenRequested -= handler;
                UnityEngine.Object.DestroyImmediate(player);
                UnityEngine.Object.DestroyImmediate(furniture);
            }
        }

        private sealed class StubKeyboardSource : IKeyboardInputSource
        {
            public KeyboardInputSnapshot Snapshot { get; set; }

            public KeyboardInputSnapshot ReadSnapshot()
            {
                return Snapshot;
            }
        }
    }
}
