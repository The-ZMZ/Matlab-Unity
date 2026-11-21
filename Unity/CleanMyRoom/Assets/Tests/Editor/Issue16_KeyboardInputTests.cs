using CleanMyRoom.Player.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CleanMyRoom.Tests
{
    /// <summary>
    /// Issue #16 coverage for the shared keyboard input foundation: idle state,
    /// key-down/held/released, simultaneous keys and normalised movement.
    /// </summary>
    [Category("Issue16")]
    public sealed class Issue16_KeyboardInputTests : InputTestFixture
    {
        [TearDown]
        public void RestoreInputSource()
        {
            KeyboardInput.ResetToUnitySource();
        }

        [Test]
        public void MissingOrIdleKeyboardProducesAnEmptySnapshot()
        {
            KeyboardInput.Source = new StubKeyboardSource { Snapshot = default };

            KeyboardInputSnapshot snapshot = KeyboardInput.Read();

            Assert.IsTrue(snapshot.IsIdle);
            Assert.IsFalse(snapshot.HasInput);
            Assert.AreEqual(Vector2.zero, snapshot.Movement);
            Assert.IsFalse(snapshot.InteractDown);
            Assert.IsFalse(snapshot.InteractHeld);
            Assert.IsFalse(snapshot.InteractReleased);
        }

        [Test]
        public void WasdAndArrowKeysProduceTheSameMovementVector()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();

            Press(keyboard.wKey);
            AssertMovement(0f, 1f, KeyboardInput.Read().Movement, "W");
            Release(keyboard.wKey);

            Press(keyboard.leftArrowKey);
            AssertMovement(-1f, 0f, KeyboardInput.Read().Movement, "Left arrow");
            Release(keyboard.leftArrowKey);
        }

        [Test]
        public void SimultaneousMovementKeysAreTrackedAndNormalised()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();

            Press(keyboard.wKey);
            Press(keyboard.dKey);

            KeyboardInputSnapshot snapshot = KeyboardInput.Read();
            Assert.AreEqual(2, snapshot.HeldGameKeyCount);
            Assert.IsTrue(snapshot.MoveUpHeld);
            Assert.IsTrue(snapshot.MoveRightHeld);
            Assert.AreEqual(1f, snapshot.Movement.magnitude, 0.0001f);
            Assert.Greater(snapshot.Movement.x, 0f);
            Assert.Greater(snapshot.Movement.y, 0f);
        }

        [Test]
        public void OpposingKeysCancelAndReleasingEverythingReturnsToIdle()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();

            Press(keyboard.wKey);
            Press(keyboard.sKey);
            Assert.AreEqual(Vector2.zero, KeyboardInput.Read().Movement);

            Release(keyboard.sKey);
            Release(keyboard.wKey);
            Assert.IsTrue(KeyboardInput.Read().IsIdle);
        }

        [Test]
        public void InteractionKeyExposesDownHeldAndReleasedFrames()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();

            Press(keyboard.eKey);
            KeyboardInputSnapshot down = KeyboardInput.Read();
            Assert.IsTrue(down.InteractDown, "E press must be visible as key-down.");
            Assert.IsTrue(down.InteractHeld);
            Assert.IsFalse(down.InteractReleased);

            InputSystem.Update();
            KeyboardInputSnapshot held = KeyboardInput.Read();
            Assert.IsFalse(held.InteractDown, "A held key must not keep reporting key-down.");
            Assert.IsTrue(held.InteractHeld);

            Release(keyboard.eKey);
            KeyboardInputSnapshot released = KeyboardInput.Read();
            Assert.IsTrue(released.InteractReleased);
            Assert.IsFalse(released.InteractHeld);
        }

        [Test]
        public void MovementAndInteractionCanBeHeldInTheSameFrame()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();

            Press(keyboard.wKey);
            Press(keyboard.eKey);

            KeyboardInputSnapshot snapshot = KeyboardInput.Read();
            Assert.IsTrue(snapshot.IsMoving);
            Assert.IsTrue(snapshot.InteractHeld);
            Assert.AreEqual(2, snapshot.HeldGameKeyCount);
        }

        private static void AssertMovement(float expectedX, float expectedY, Vector2 actual, string key)
        {
            Assert.AreEqual(expectedX, actual.x, 0.0001f, key + " produced the wrong X value.");
            Assert.AreEqual(expectedY, actual.y, 0.0001f, key + " produced the wrong Y value.");
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
