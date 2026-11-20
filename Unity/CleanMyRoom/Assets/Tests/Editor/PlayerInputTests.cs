using CleanMyRoom.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CleanMyRoom.Tests
{
    /// <summary>
    /// Simulates a real keyboard device to prove that WASD and arrow keys are
    /// both read by PlayerController and produce the same movement vectors.
    /// </summary>
    [Category("Issue13")]
    public sealed class PlayerInputTests : InputTestFixture
    {
        [Test]
        public void WasdKeysProduceExpectedMovementVectors()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();

            Press(keyboard.wKey);
            AssertVector(0f, 1f, PlayerController.ReadKeyboardMovement(), "W");
            Release(keyboard.wKey);

            Press(keyboard.sKey);
            AssertVector(0f, -1f, PlayerController.ReadKeyboardMovement(), "S");
            Release(keyboard.sKey);

            Press(keyboard.aKey);
            AssertVector(-1f, 0f, PlayerController.ReadKeyboardMovement(), "A");
            Release(keyboard.aKey);

            Press(keyboard.dKey);
            AssertVector(1f, 0f, PlayerController.ReadKeyboardMovement(), "D");
            Release(keyboard.dKey);
        }

        [Test]
        public void ArrowKeysProduceExpectedMovementVectors()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();

            Press(keyboard.upArrowKey);
            AssertVector(0f, 1f, PlayerController.ReadKeyboardMovement(), "Up arrow");
            Release(keyboard.upArrowKey);

            Press(keyboard.downArrowKey);
            AssertVector(0f, -1f, PlayerController.ReadKeyboardMovement(), "Down arrow");
            Release(keyboard.downArrowKey);

            Press(keyboard.leftArrowKey);
            AssertVector(-1f, 0f, PlayerController.ReadKeyboardMovement(), "Left arrow");
            Release(keyboard.leftArrowKey);

            Press(keyboard.rightArrowKey);
            AssertVector(1f, 0f, PlayerController.ReadKeyboardMovement(), "Right arrow");
            Release(keyboard.rightArrowKey);
        }

        [Test]
        public void MixedInputCancelsOpposingKeysAndNormalisesDiagonals()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();

            Press(keyboard.wKey);
            Press(keyboard.sKey);
            Press(keyboard.dKey);

            Vector2 movement = PlayerController.ReadKeyboardMovement();
            Assert.AreEqual(0f, movement.y, 0.0001f,
                "W and S should cancel each other out.");
            Assert.AreEqual(1f, movement.magnitude, 0.0001f,
                "The remaining diagonal must be normalised.");

            Release(keyboard.dKey);
            Release(keyboard.sKey);
            Release(keyboard.wKey);
        }

        private static void AssertVector(float expectedX, float expectedY, Vector2 actual, string key)
        {
            Assert.AreEqual(expectedX, actual.x, 0.0001f, key + " produced the wrong X value.");
            Assert.AreEqual(expectedY, actual.y, 0.0001f, key + " produced the wrong Y value.");
        }
    }
}
