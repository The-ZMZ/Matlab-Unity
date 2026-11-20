using System.Collections;
using CleanMyRoom.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CleanMyRoom.Tests.PlayMode
{
    /// <summary>
    /// Play Mode verification for the persistent GameManager introduced by
    /// Issue #011. DontDestroyOnLoad is a runtime-only Unity call, so the real
    /// persistence behaviour cannot be checked in Edit Mode; these tests run
    /// the actual runtime lifecycle instead.
    ///
    /// No educational validation is performed here. MATLAB remains the only
    /// authority for activity answers (AGENTS.md sections 3 and 4).
    /// </summary>
    public sealed class GameManagerPersistencePlayModeTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            // RuntimeInitializeOnLoadMethod(AfterSceneLoad) normally creates the
            // manager. Wait a frame for it, then fall back to creating one so
            // the test is stable even if the runner skipped that callback.
            yield return null;

            if (GameManager.Instance == null)
            {
                GameObject root = new GameObject("Game Manager (test bootstrap)");
                root.AddComponent<GameManager>();
                yield return null;
            }

            Assert.IsNotNull(GameManager.Instance,
                "GameManager was not created at runtime.");
        }

        [UnityTest]
        public IEnumerator GameManagerSurvivesSceneLoadsInPersistentScene()
        {
            GameManager first = GameManager.Instance;
            Assert.AreEqual("DontDestroyOnLoad", first.gameObject.scene.name,
                "GameManager root was not moved into the persistent scene at startup.");

            yield return SceneManager.LoadSceneAsync(SceneNames.MainMenu);
            yield return null;

            Assert.AreSame(first, GameManager.Instance,
                "Loading MainMenu replaced the persistent GameManager.");
            Assert.AreEqual("DontDestroyOnLoad", GameManager.Instance.gameObject.scene.name,
                "GameManager left the persistent scene after loading MainMenu.");

            yield return SceneManager.LoadSceneAsync(SceneNames.Bedroom);
            yield return null;

            Assert.AreSame(first, GameManager.Instance,
                "Loading Bedroom replaced the persistent GameManager.");
            Assert.AreEqual("DontDestroyOnLoad", GameManager.Instance.gameObject.scene.name,
                "GameManager left the persistent scene after loading Bedroom.");
            Assert.AreEqual(SceneNames.Bedroom, SceneManager.GetActiveScene().name,
                "Bedroom was not loaded as the active scene.");
        }

        [UnityTest]
        public IEnumerator DuplicateGameManagerIsRemovedAtRuntime()
        {
            GameManager first = GameManager.Instance;
            Assert.AreSame(first, GameManager.Instance);

            GameObject duplicateRoot = new GameObject("Duplicate Game Manager");
            duplicateRoot.AddComponent<GameManager>();

            // Register() destroys a second manager during Awake; Unity performs
            // the destruction at the end of the frame, hence the extra yield.
            yield return null;
            yield return null;

            Assert.IsTrue(duplicateRoot == null,
                "Duplicate GameManager root was not destroyed.");
            Assert.AreSame(first, GameManager.Instance,
                "Duplicate manager replaced the original Instance reference.");
        }
    }
}
