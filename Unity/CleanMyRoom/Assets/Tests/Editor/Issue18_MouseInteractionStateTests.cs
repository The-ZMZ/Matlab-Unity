using CleanMyRoom.Player.Input;
using NUnit.Framework;
using UnityEngine;

namespace CleanMyRoom.Tests
{
    [Category("Issue18")]
    public sealed class Issue18_MouseInteractionStateTests
    {
        [TearDown]
        public void TearDown()
        {
            MouseInput.ResetToUnitySource();
        }

        [Test]
        public void StartsIdleWithNoPendingEvents()
        {
            MouseInteractionStateMachine machine = new MouseInteractionStateMachine();

            Assert.AreEqual(MouseInteractionState.IDLE, machine.State);
            Assert.IsFalse(machine.IsPressActive);
            Assert.IsFalse(machine.IsHolding);
            Assert.IsFalse(machine.ConsumePressStarted());
            Assert.IsFalse(machine.ConsumeHoldStarted());
            Assert.IsFalse(machine.ConsumeReleaseStarted());
        }

        [Test]
        public void PressHoldReleaseFollowsRequiredStateSequence()
        {
            MouseInteractionStateMachine machine = new MouseInteractionStateMachine();

            machine.Update(Press());
            Assert.AreEqual(MouseInteractionState.PRESSED, machine.State);
            Assert.IsTrue(machine.ConsumePressStarted());
            Assert.IsFalse(machine.ConsumeHoldStarted());
            Assert.IsFalse(machine.ConsumeReleaseStarted());

            machine.Update(Hold());
            Assert.AreEqual(MouseInteractionState.HOLDING, machine.State);
            Assert.IsTrue(machine.IsHolding);
            Assert.IsTrue(machine.ConsumeHoldStarted());
            Assert.IsFalse(machine.ConsumePressStarted());

            machine.Update(Release());
            Assert.AreEqual(MouseInteractionState.RELEASE, machine.State);
            Assert.IsTrue(machine.ConsumeReleaseStarted());
            Assert.IsFalse(machine.ConsumePressStarted());
            Assert.IsFalse(machine.ConsumeHoldStarted());

            machine.Update(Idle());
            Assert.AreEqual(MouseInteractionState.IDLE, machine.State);
        }

        [Test]
        public void RepeatedPressWhileButtonRemainsHeldDoesNotEmitAnotherPress()
        {
            MouseInteractionStateMachine machine = new MouseInteractionStateMachine();

            machine.Update(Press());
            Assert.IsTrue(machine.ConsumePressStarted());

            machine.Update(Hold());
            Assert.IsTrue(machine.ConsumeHoldStarted());

            machine.Update(Press());
            machine.Update(Hold());

            Assert.AreEqual(MouseInteractionState.HOLDING, machine.State);
            Assert.IsFalse(machine.ConsumePressStarted());
            Assert.IsFalse(machine.ConsumeHoldStarted());
            Assert.IsFalse(machine.ConsumeReleaseStarted());
        }

        [Test]
        public void RapidPressReleaseSequencesRearmCorrectly()
        {
            MouseInteractionStateMachine machine = new MouseInteractionStateMachine();

            machine.Update(Press());
            machine.Update(Release());
            machine.Update(Idle());
            Assert.IsTrue(machine.ConsumePressStarted());
            Assert.IsTrue(machine.ConsumeReleaseStarted());

            machine.Update(Press());
            machine.Update(Release());

            Assert.AreEqual(MouseInteractionState.RELEASE, machine.State);
            Assert.IsTrue(machine.ConsumePressStarted());
            Assert.IsTrue(machine.ConsumeReleaseStarted());
        }

        [Test]
        public void SameFramePressAndReleaseEmitsBothOneShotEvents()
        {
            MouseInteractionStateMachine machine = new MouseInteractionStateMachine();
            MouseInputSnapshot snapshot = MouseInputSnapshot.Create(
                new Vector2(12f, 34f), true, false, true);

            machine.Update(snapshot);

            Assert.AreEqual(MouseInteractionState.RELEASE, machine.State);
            Assert.IsTrue(machine.ConsumePressStarted());
            Assert.IsTrue(machine.ConsumeReleaseStarted());
            Assert.IsFalse(machine.ConsumeHoldStarted());
            Assert.AreEqual(new Vector2(12f, 34f), machine.LastScreenPosition);
        }

        [Test]
        public void PressThatDoesNotTargetInteractableIsIgnoredUntilNextPress()
        {
            MouseInteractionStateMachine machine = new MouseInteractionStateMachine();

            machine.Update(Press(), false);
            machine.Update(Hold(), false);
            machine.Update(Release(), false);

            Assert.AreEqual(MouseInteractionState.IDLE, machine.State);
            Assert.IsFalse(machine.ConsumePressStarted());
            Assert.IsFalse(machine.ConsumeHoldStarted());
            Assert.IsFalse(machine.ConsumeReleaseStarted());

            machine.Update(Press(), true);

            Assert.AreEqual(MouseInteractionState.PRESSED, machine.State);
            Assert.IsTrue(machine.ConsumePressStarted());
        }

        [Test]
        public void ResetClearsStateAndAllLatches()
        {
            MouseInteractionStateMachine machine = new MouseInteractionStateMachine();
            machine.Update(Press());
            machine.Update(Hold());

            machine.Reset();

            Assert.AreEqual(MouseInteractionState.IDLE, machine.State);
            Assert.IsFalse(machine.ConsumePressStarted());
            Assert.IsFalse(machine.ConsumeHoldStarted());
            Assert.IsFalse(machine.ConsumeReleaseStarted());

            machine.Update(Press());
            Assert.AreEqual(MouseInteractionState.PRESSED, machine.State);
            Assert.IsTrue(machine.ConsumePressStarted());
        }

        [Test]
        public void InterruptRequiresReleaseBeforeAnotherPressCanStart()
        {
            MouseInteractionStateMachine machine = new MouseInteractionStateMachine();
            machine.Update(Press());

            machine.Interrupt();

            machine.Update(Press());
            machine.Update(Hold());
            Assert.AreEqual(MouseInteractionState.IDLE, machine.State);
            Assert.IsFalse(machine.ConsumePressStarted());

            machine.Update(Release());
            machine.Update(Press());

            Assert.AreEqual(MouseInteractionState.PRESSED, machine.State);
            Assert.IsTrue(machine.ConsumePressStarted());
        }

        [Test]
        public void MouseInputUsesInjectableSourceAndCanBeRestored()
        {
            MouseInputSnapshot expected = MouseInputSnapshot.Create(
                new Vector2(5f, 6f), true, true, false);
            StubMouseSource source = new StubMouseSource(expected);

            MouseInput.Source = source;

            MouseInputSnapshot actual = MouseInput.Read();
            Assert.AreEqual(expected.ScreenPosition, actual.ScreenPosition);
            Assert.IsTrue(actual.PressDown);
            Assert.IsTrue(actual.PressHeld);
            Assert.AreEqual(1, source.ReadCount);
            Assert.AreSame(source, MouseInput.Source);

            MouseInput.ResetToUnitySource();
            Assert.AreNotSame(source, MouseInput.Source);
        }

        private static MouseInputSnapshot Press()
        {
            return MouseInputSnapshot.Create(new Vector2(10f, 20f), true, true, false);
        }

        private static MouseInputSnapshot Hold()
        {
            return MouseInputSnapshot.Create(new Vector2(11f, 21f), false, true, false);
        }

        private static MouseInputSnapshot Release()
        {
            return MouseInputSnapshot.Create(new Vector2(12f, 22f), false, false, true);
        }

        private static MouseInputSnapshot Idle()
        {
            return MouseInputSnapshot.Create(new Vector2(12f, 22f), false, false, false);
        }

        private sealed class StubMouseSource : IMouseInputSource
        {
            private readonly MouseInputSnapshot snapshot;

            public StubMouseSource(MouseInputSnapshot snapshot)
            {
                this.snapshot = snapshot;
            }

            public int ReadCount { get; private set; }

            public MouseInputSnapshot ReadSnapshot()
            {
                ReadCount++;
                return snapshot;
            }
        }
    }
}
