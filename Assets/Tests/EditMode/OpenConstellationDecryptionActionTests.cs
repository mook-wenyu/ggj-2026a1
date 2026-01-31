using System;

using NUnit.Framework;

using UnityEngine;

public sealed class OpenConstellationDecryptionActionTests
{
    [Test]
    public void TryExecute_InvokesOtherAction_WhenUiSolved()
    {
        var go = new GameObject("Test_OpenConstellationDecryptionAction");
        try
        {
            var ui = go.AddComponent<FakeConstellationDecryptionUI>();
            var open = go.AddComponent<OpenConstellationDecryptionAction>();
            var spy = go.AddComponent<SpyInteractionAction>();

            var ok = open.TryExecute(new InteractionContext(go, open));

            Assert.IsTrue(ok);
            Assert.IsFalse(spy.Executed);

            ui.SimulateSolved();

            Assert.IsTrue(spy.Executed);
            Assert.AreSame(go, spy.LastTarget);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    private sealed class FakeConstellationDecryptionUI : MonoBehaviour, IConstellationDecryptionUI
    {
        private Action _onSolved;
        private Action _onCancelled;

        public bool TryShowConstellationPatternLock(
            Action onSolved,
            Action onCancelled = null,
            int[] acceptedPattern1Override = null,
            int[] acceptedPattern2Override = null)
        {
            _onSolved = onSolved;
            _onCancelled = onCancelled;
            return true;
        }

        public void SimulateSolved()
        {
            _onSolved?.Invoke();
        }

        public void SimulateCancelled()
        {
            _onCancelled?.Invoke();
        }
    }

    private sealed class SpyInteractionAction : InteractionAction
    {
        public bool Executed { get; private set; }
        public GameObject LastTarget { get; private set; }

        public override bool TryExecute(in InteractionContext context)
        {
            Executed = true;
            LastTarget = context.Target;
            return true;
        }
    }
}
