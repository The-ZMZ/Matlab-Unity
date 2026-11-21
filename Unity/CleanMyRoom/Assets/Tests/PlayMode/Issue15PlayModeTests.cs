using System.Collections;
using System.Reflection;
using CleanMyRoom.Core;
using CleanMyRoom.Gameplay;
using CleanMyRoom.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CleanMyRoom.Tests.PlayMode
{
    /// <summary>
    /// Issue #015 PlayMode coverage for the runtime-created AudioManager, the
    /// four approved clips, direct scene entry and scene transitions. Unity
    /// only displays results supplied by MATLAB; it never validates answers.
    /// </summary>
    public sealed class Issue15PlayModeTests
    {
        [UnityTest]
        public IEnumerator AudioManager_IsCreatedBeforeScenesAndSurvivesTransitions()
        {
            yield return null;

            AudioManager first = AudioManager.Instance;
            Assert.IsNotNull(first, "AudioManager was not created at runtime.");
            Assert.AreEqual("DontDestroyOnLoad", first.gameObject.scene.name,
                "AudioManager root was not made persistent at startup.");

            yield return SceneManager.LoadSceneAsync(SceneNames.MainMenu);
            yield return null;
            Assert.AreSame(first, AudioManager.Instance);

            yield return SceneManager.LoadSceneAsync(SceneNames.ColorSorting);
            yield return null;
            Assert.AreSame(first, AudioManager.Instance);
            Assert.AreEqual("DontDestroyOnLoad", AudioManager.Instance.gameObject.scene.name);
            Assert.AreEqual(SceneNames.ColorSorting, SceneManager.GetActiveScene().name);
        }

        [UnityTest]
        public IEnumerator DuplicateAudioManager_IsRemovedWithoutReplacingInstance()
        {
            yield return null;
            AudioManager first = AudioManager.Instance;
            Assert.IsNotNull(first);

            GameObject duplicateRoot = new GameObject("Duplicate Audio Manager");
            duplicateRoot.AddComponent<AudioManager>();
            yield return null;
            yield return null;

            Assert.IsTrue(duplicateRoot == null,
                "Duplicate AudioManager root was not destroyed.");
            Assert.AreSame(first, AudioManager.Instance);
        }

        [UnityTest]
        public IEnumerator AudioCatalog_ContainsTheFourApprovedKenneyClips()
        {
            yield return null;

            UiAudioCatalog catalog = Resources.Load<UiAudioCatalog>("Audio/UiAudioCatalog");
            Assert.IsNotNull(catalog, "Resources UI audio catalog is missing at runtime.");
            AssertClip(catalog.ButtonClick, "Click");
            AssertClip(catalog.Success, "Success");
            AssertClip(catalog.Error, "Error");
            AssertClip(catalog.StarReward, "StarReward");
        }

        [UnityTest]
        public IEnumerator MainMenu_LoadedDirectlyShowsEnglishTitleStartAndQuit()
        {
            yield return SceneManager.LoadSceneAsync(SceneNames.MainMenu);
            yield return null;

            GameObject canvasObject = GameObject.Find("MainMenuCanvas");
            Assert.IsNotNull(canvasObject);

            MainMenuView view = canvasObject.GetComponent<MainMenuView>();
            Assert.IsNotNull(view);
            UiTextCatalog catalog = view.TextCatalog;
            Assert.IsNotNull(catalog, "English UI text catalog is not assigned.");

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            Assert.IsNotNull(scaler);
            Assert.AreEqual(new Vector2(1024f, 768f), scaler.referenceResolution);

            Text title = FindNamedChild(canvasObject.transform, "GameTitle").GetComponent<Text>();
            Text start = FindNamedChild(canvasObject.transform, "StartGameButton")
                .GetComponentInChildren<Text>();
            Text quit = FindNamedChild(canvasObject.transform, "QuitButton")
                .GetComponentInChildren<Text>();

            Assert.AreEqual(catalog.Get(UiTextKeys.MainTitle), title.text);
            Assert.AreEqual(catalog.Get(UiTextKeys.MainStart), start.text);
            Assert.AreEqual(catalog.Get(UiTextKeys.MainQuit), quit.text);
        }

        [UnityTest]
        public IEnumerator GameplayScenes_LoadDirectlyWithCorrectUiAndNoLocalValidation()
        {
            // Read the real catalog from an activity scene, then verify every
            // gameplay scene can also be entered directly.
            yield return SceneManager.LoadSceneAsync(SceneNames.ColorSorting);
            yield return null;

            InstructionsPanelController[] sourceInstructions =
                Object.FindObjectsByType<InstructionsPanelController>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(1, sourceInstructions.Length);
            UiTextCatalog catalog = sourceInstructions[0].TextCatalog;
            Assert.IsNotNull(catalog, "English UI text catalog is not assigned.");

            yield return AssertGameplayScene(SceneNames.Bedroom, null, catalog);
            yield return AssertGameplayScene(SceneNames.ColorSorting,
                UiTextKeys.InstructionsColor, catalog);
            yield return AssertGameplayScene(SceneNames.TasteSorting,
                UiTextKeys.InstructionsTaste, catalog);
            yield return AssertGameplayScene(SceneNames.MathActivity,
                UiTextKeys.InstructionsMath, catalog);
        }

        private static IEnumerator AssertGameplayScene(
            string sceneName, string instructionKey, UiTextCatalog catalog)
        {
            yield return SceneManager.LoadSceneAsync(sceneName);
            yield return null;

            PauseMenuController[] pauseMenus = Object.FindObjectsByType<PauseMenuController>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(1, pauseMenus.Length,
                sceneName + " has an unexpected pause menu count.");
            PauseMenuController pause = pauseMenus[0];
            Assert.IsNotNull(pause, sceneName + " has no pause menu.");
            Assert.AreEqual(new Vector2(1024f, 768f),
                pause.GetComponent<CanvasScaler>().referenceResolution);
            GameObject pausePanel = (GameObject)GetPrivateField(pause, "panel");
            Assert.IsNotNull(pausePanel);
            Assert.IsFalse(pausePanel.activeSelf,
                sceneName + " pause panel was visible on direct entry.");

            FeedbackPanelController[] feedbackPanels =
                Object.FindObjectsByType<FeedbackPanelController>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(1, feedbackPanels.Length,
                sceneName + " has an unexpected feedback panel count.");
            FeedbackPanelController feedback = feedbackPanels[0];
            Assert.IsNotNull(feedback, sceneName + " has no feedback panel.");
            GameObject feedbackPanel = (GameObject)GetPrivateField(feedback, "panel");
            Assert.IsNotNull(feedbackPanel);
            Assert.IsFalse(feedbackPanel.activeSelf,
                sceneName + " feedback panel was visible on direct entry.");

            InstructionsPanelController[] instructions =
                Object.FindObjectsByType<InstructionsPanelController>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (instructionKey == null)
            {
                Assert.AreEqual(0, instructions.Length,
                    "Bedroom must not show an activity instructions panel.");
            }
            else
            {
                Assert.AreEqual(1, instructions.Length,
                    sceneName + " has an unexpected instructions panel count.");
                Assert.AreEqual(instructionKey, instructions[0].TextKey);

                Text body = (Text)GetPrivateField(instructions[0], "bodyText");
                Assert.IsNotNull(body);
                Assert.AreEqual(catalog.Get(instructionKey), body.text);
            }
        }

        private static Transform FindNamedChild(Transform root, string name)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name == name)
                {
                    return children[i];
                }
            }
            return null;
        }

        private static object GetPrivateField(object target, string name)
        {
            FieldInfo field = target.GetType().GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "Missing private field: " + name);
            return field.GetValue(target);
        }

        private static void AssertClip(AudioClip clip, string expectedName)
        {
            Assert.IsNotNull(clip, "Missing audio clip: " + expectedName);
            Assert.AreEqual(expectedName, clip.name);
        }
    }
}
