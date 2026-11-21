using System;
using System.Reflection;
using CleanMyRoom.Core;
using CleanMyRoom.Gameplay;
using CleanMyRoom.Net;
using CleanMyRoom.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CleanMyRoom.Tests
{
    /// <summary>
    /// Issue #015 EditMode coverage: asset wiring, 1024x768 layout, scene
    /// placement, Esc-only pause behaviour, authoritative feedback display and
    /// the AudioManager bootstrap/star event design.
    ///
    /// No educational answer is validated here; MATLAB remains the sole
    /// authority for activity correctness and completion.
    /// </summary>
    [Category("Issue15")]
    public sealed class Issue15Tests : InputTestFixture
    {
        private const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
        private const string BedroomPath = "Assets/Scenes/Bedroom.unity";
        private const string ColorPath = "Assets/Scenes/ColorSorting.unity";
        private const string TastePath = "Assets/Scenes/TasteSorting.unity";
        private const string MathPath = "Assets/Scenes/MathActivity.unity";

        private const string TextCatalogPath = "Assets/UI/Text/EnglishUiText.asset";
        private const string AudioCatalogPath = "Assets/Resources/Audio/UiAudioCatalog.asset";

        private static readonly string[] PrefabPaths =
        {
            "Assets/Prefabs/UI/PF_PauseMenu.prefab",
            "Assets/Prefabs/UI/PF_InstructionsPanel.prefab",
            "Assets/Prefabs/UI/PF_FeedbackPanel.prefab"
        };

        private string originalScenePath;

        [SetUp]
        public void SetUp()
        {
            originalScenePath = SceneManager.GetActiveScene().path;
            Time.timeScale = 1f;
            GameProgress.ClearListenersForTests();
            GameProgress.ResetSessionForTests();
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            GameProgress.ResetSessionForTests();
            GameProgress.ClearListenersForTests();

            // EditMode has no runtime bootstrap; remove any AudioManager that a
            // UI button callback created through Instance during the test.
            AudioManager[] managers = Object.FindObjectsByType<AudioManager>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < managers.Length; i++)
            {
                Object.DestroyImmediate(managers[i].gameObject);
            }

            RestoreOriginalScene();
        }

        [Test]
        public void UiAssets_ExistAndUseApprovedEnglishTextAndAudio()
        {
            UiTextCatalog text = AssetDatabase.LoadAssetAtPath<UiTextCatalog>(TextCatalogPath);
            Assert.IsNotNull(text, "English UI text catalog is missing.");

            Assert.AreEqual("Clean My Room!", text.Get(UiTextKeys.MainTitle));
            Assert.AreEqual("Start Game", text.Get(UiTextKeys.MainStart));
            Assert.AreEqual("Quit", text.Get(UiTextKeys.MainQuit));
            Assert.AreEqual("Paused", text.Get(UiTextKeys.PauseTitle));
            Assert.AreEqual("Resume", text.Get(UiTextKeys.PauseResume));
            Assert.AreEqual("Main Menu", text.Get(UiTextKeys.PauseMainMenu));
            Assert.AreEqual("How to Play", text.Get(UiTextKeys.InstructionsTitle));
            Assert.AreEqual("Got it!", text.Get(UiTextKeys.InstructionsClose));
            Assert.AreEqual("Well done!", text.Get(UiTextKeys.FeedbackSuccess));
            Assert.AreEqual("Oops! Something went wrong. Please try again.",
                text.Get(UiTextKeys.FeedbackError));
            Assert.AreEqual("OK", text.Get(UiTextKeys.FeedbackClose));

            string[] activityKeys =
            {
                UiTextKeys.InstructionsColor,
                UiTextKeys.InstructionsTaste,
                UiTextKeys.InstructionsMath
            };
            for (int i = 0; i < activityKeys.Length; i++)
            {
                Assert.AreNotEqual(activityKeys[i], text.Get(activityKeys[i]),
                    "Missing English text for " + activityKeys[i]);
            }

            UiAudioCatalog audio = AssetDatabase.LoadAssetAtPath<UiAudioCatalog>(AudioCatalogPath);
            Assert.IsNotNull(audio, "UI audio catalog is missing from Resources.");
            AssertClip(audio.ButtonClick, "Click");
            AssertClip(audio.Success, "Success");
            AssertClip(audio.Error, "Error");
            AssertClip(audio.StarReward, "StarReward");
        }

        [Test]
        public void UiPrefabs_AreAt1024By768ReferenceResolution()
        {
            for (int i = 0; i < PrefabPaths.Length; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPaths[i]);
                Assert.IsNotNull(prefab, "Missing UI prefab: " + PrefabPaths[i]);

                CanvasScaler scaler = prefab.GetComponentInChildren<CanvasScaler>(true);
                Assert.IsNotNull(scaler, PrefabPaths[i] + " has no CanvasScaler.");
                Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode);
                Assert.AreEqual(new Vector2(1024f, 768f), scaler.referenceResolution);
            }
        }

        [Test]
        public void MainMenu_HasTitleStartQuitAndIsWiredToEnglishText()
        {
            EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);

            GameObject canvasObject = GameObject.Find("MainMenuCanvas");
            Assert.IsNotNull(canvasObject, "MainMenuCanvas is missing.");

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            Assert.IsNotNull(scaler);
            Assert.AreEqual(new Vector2(1024f, 768f), scaler.referenceResolution);

            Transform startTransform = canvasObject.transform.Find("StartGameButton");
            Transform quitTransform = canvasObject.transform.Find("QuitButton");
            Transform titleTransform = canvasObject.transform.Find("GameTitle");
            Assert.IsNotNull(startTransform);
            Assert.IsNotNull(quitTransform);
            Assert.IsNotNull(titleTransform);

            Button startButton = startTransform.GetComponent<Button>();
            Button quitButton = quitTransform.GetComponent<Button>();
            Text title = titleTransform.GetComponent<Text>();
            Assert.IsNotNull(startButton);
            Assert.IsNotNull(quitButton);
            Assert.IsNotNull(title);

            MainMenuView view = canvasObject.GetComponent<MainMenuView>();
            Assert.IsNotNull(view, "MainMenuView is missing.");
            Assert.AreSame(title, GetPrivateField(view, "titleText"));
            Assert.AreSame(startButton, GetPrivateField(view, "startButton"));
            Assert.AreSame(quitButton, GetPrivateField(view, "quitButton"));

            MainMenuController controller = Object.FindFirstObjectByType<MainMenuController>();
            Assert.IsNotNull(controller);
            Assert.GreaterOrEqual(quitButton.onClick.GetPersistentEventCount(), 1,
                "Quit button has no persistent callback.");
            Assert.AreEqual(controller, quitButton.onClick.GetPersistentTarget(0));
            Assert.AreEqual("QuitGame", quitButton.onClick.GetPersistentMethodName(0));
        }

        [Test]
        public void GameplayScenes_HavePauseAndFeedbackOnlyWhereApproved()
        {
            AssertGameplayScene(BedroomPath, null);
            AssertGameplayScene(ColorPath, UiTextKeys.InstructionsColor);
            AssertGameplayScene(TastePath, UiTextKeys.InstructionsTaste);
            AssertGameplayScene(MathPath, UiTextKeys.InstructionsMath);
        }

        [Test]
        public void PauseMenu_UsesNewInputSystemEscapeOnlyInGameplayScene()
        {
            EditorSceneManager.OpenScene(ColorPath, OpenSceneMode.Single);
            PauseMenuController controller = Object.FindFirstObjectByType<PauseMenuController>();
            Assert.IsNotNull(controller);
            GameObject panel = (GameObject)GetPrivateField(controller, "panel");
            Assert.IsNotNull(panel);
            InvokePrivate(controller, "Awake");
            Assert.IsFalse(panel.activeSelf);

            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                Press(keyboard.escapeKey);
                InvokePrivate(controller, "Update");
                Assert.IsTrue(panel.activeSelf, "Esc did not open the pause panel.");
                Assert.AreEqual(0f, Time.timeScale, 0.0001f);

                Release(keyboard.escapeKey);
                Press(keyboard.escapeKey);
                InvokePrivate(controller, "Update");
                Assert.IsFalse(panel.activeSelf, "Esc did not resume the game.");
                Assert.AreEqual(1f, Time.timeScale, 0.0001f);
            }
            finally
            {
                Release(keyboard.escapeKey);
                Time.timeScale = 1f;
            }

            EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);
            GameObject outsideRoot = new GameObject("Pause Menu Outside Gameplay");
            PauseMenuController outside = outsideRoot.AddComponent<PauseMenuController>();
            GameObject outsidePanel = new GameObject("Panel");
            outsidePanel.SetActive(false);
            SetPrivateField(outside, "panel", outsidePanel);

            Press(keyboard.escapeKey);
            InvokePrivate(outside, "Update");
            Assert.IsFalse(outsidePanel.activeSelf,
                "Pause panel opened outside a gameplay scene.");
            Assert.AreEqual(1f, Time.timeScale, 0.0001f);

            Object.DestroyImmediate(outsideRoot);
            Object.DestroyImmediate(outsidePanel);
        }

        [Test]
        public void FeedbackPanel_DisplaysOnlyAuthoritativeMatLabResults()
        {
            UiTextCatalog catalog = AssetDatabase.LoadAssetAtPath<UiTextCatalog>(TextCatalogPath);
            Assert.IsNotNull(catalog);

            GameObject root = new GameObject("Feedback Panel Test");
            GameObject panel = new GameObject("Panel");
            panel.transform.SetParent(root.transform);
            Text title = CreateTextChild(panel.transform, "Title");
            Text message = CreateTextChild(panel.transform, "Message");
            Button close = CreateButtonChild(panel.transform, "CloseButton");

            FeedbackPanelController controller = root.AddComponent<FeedbackPanelController>();
            SetPrivateField(controller, "textCatalog", catalog);
            SetPrivateField(controller, "panel", panel);
            SetPrivateField(controller, "titleText", title);
            SetPrivateField(controller, "messageText", message);
            SetPrivateField(controller, "closeButton", close);

            try
            {
                controller.ProcessWireMessage(new WireMessage
                {
                    type = ProtocolConstants.TypePlaceResponse,
                    valid = false,
                    message = "Try again"
                });
                Assert.IsTrue(panel.activeSelf);
                Assert.AreEqual(catalog.Get(UiTextKeys.FeedbackError), title.text);
                Assert.AreEqual("Try again", message.text);
                Assert.AreEqual(catalog.Get(UiTextKeys.FeedbackClose),
                    close.GetComponentInChildren<Text>().text);

                controller.Hide();
                Assert.IsFalse(panel.activeSelf);

                controller.ProcessWireMessage(new WireMessage
                {
                    type = ProtocolConstants.TypeActivityResult,
                    completed = false,
                    message = "not complete"
                });
                Assert.IsFalse(panel.activeSelf,
                    "Non-completing result must not show success feedback.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void FloatingStar_RaisesRewardEventOnlyOnNewCompletion()
        {
            GameObject root = new GameObject("Floating Star Test");
            FloatingStar star = root.AddComponent<FloatingStar>();
            SetPrivateField(star, "activityId", ActivityIds.Color);

            int calls = 0;
            string raisedId = null;
            Action<string> handler = delegate(string activityId)
            {
                calls++;
                raisedId = activityId;
            };

            FloatingStar.StarCollected += handler;
            star.EnsureSubscribed();
            try
            {
                Assert.IsFalse(star.IsCompleted);
                Assert.IsTrue(GameProgress.TryComplete(ActivityIds.Color));
                Assert.AreEqual(1, calls);
                Assert.AreEqual(ActivityIds.Color, raisedId);
                Assert.IsTrue(star.IsCompleted);

                Assert.IsFalse(GameProgress.TryComplete(ActivityIds.Color));
                Assert.AreEqual(1, calls, "Duplicate completion raised another star reward.");
            }
            finally
            {
                FloatingStar.StarCollected -= handler;
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void AudioManager_UsesBeforeSceneLoadBootstrap()
        {
            MethodInfo bootstrap = typeof(AudioManager).GetMethod(
                "Bootstrap", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(bootstrap, "AudioManager bootstrap method is missing.");

            RuntimeInitializeOnLoadMethodAttribute attribute =
                bootstrap.GetCustomAttribute<RuntimeInitializeOnLoadMethodAttribute>();
            Assert.IsNotNull(attribute, "AudioManager bootstrap has no runtime init attribute.");
            Assert.AreEqual(RuntimeInitializeLoadType.BeforeSceneLoad, attribute.loadType);
        }

        private static void AssertClip(AudioClip clip, string expectedName)
        {
            Assert.IsNotNull(clip, "Missing audio clip: " + expectedName);
            Assert.AreEqual(expectedName, clip.name);
        }

        private static void AssertGameplayScene(string path, string instructionKey)
        {
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            PauseMenuController[] pauseMenus = Object.FindObjectsByType<PauseMenuController>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(1, pauseMenus.Length, path + " has an unexpected pause menu count.");

            FeedbackPanelController[] feedbackPanels =
                Object.FindObjectsByType<FeedbackPanelController>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.AreEqual(1, feedbackPanels.Length,
                path + " has an unexpected feedback panel count.");

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
                    path + " has an unexpected instructions panel count.");
                Assert.AreEqual(instructionKey, instructions[0].TextKey);
            }
        }

        private static Text CreateTextChild(Transform parent, string name)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child.AddComponent<Text>();
        }

        private static Button CreateButtonChild(Transform parent, string name)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            child.AddComponent<Image>();
            Button button = child.AddComponent<Button>();
            CreateTextChild(child.transform, "Text");
            return button;
        }

        private static object GetPrivateField(object target, string name)
        {
            FieldInfo field = target.GetType().GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "Missing private field: " + name);
            return field.GetValue(target);
        }

        private static void SetPrivateField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "Missing private field: " + name);
            field.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string name)
        {
            MethodInfo method = target.GetType().GetMethod(name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "Missing private method: " + name);
            method.Invoke(target, null);
        }

        private void RestoreOriginalScene()
        {
            if (string.IsNullOrEmpty(originalScenePath))
            {
                return;
            }
            if (SceneManager.GetActiveScene().path != originalScenePath)
            {
                EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
            }
        }
    }
}
