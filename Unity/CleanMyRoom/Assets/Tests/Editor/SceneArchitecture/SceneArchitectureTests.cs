using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using CleanMyRoom.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CleanMyRoom.Tests.SceneArchitecture
{
    /// <summary>
    /// EditMode verification for Issue #011: scene assets, build settings,
    /// name resolution and the persistent GameManager lifecycle helpers.
    ///
    /// These tests never call SceneManager.LoadScene for a valid scene, so
    /// they cannot change the scene currently open in the Editor.
    /// </summary>
    public sealed class SceneArchitectureTests
    {
        private List<GameManager> createdManagers;

        private static string ScenesDirectory
        {
            get { return Path.Combine(Application.dataPath, "Scenes"); }
        }

        private static string BuildSettingsPath
        {
            get
            {
                return Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                    "ProjectSettings", "EditorBuildSettings.asset");
            }
        }

        [SetUp]
        public void SetUp()
        {
            createdManagers = new List<GameManager>();
            if (GameManager.Instance != null)
            {
                GameManager leftover = GameManager.Instance;
                GameObject leftoverRoot = leftover.gameObject;
                GameManager.Unregister(leftover);
                if (leftoverRoot != null)
                {
                    Object.DestroyImmediate(leftoverRoot);
                }
            }
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < createdManagers.Count; i++)
            {
                GameManager manager = createdManagers[i];
                if (manager == null)
                {
                    continue;
                }

                GameManager.Unregister(manager);
                if (manager.gameObject != null)
                {
                    Object.DestroyImmediate(manager.gameObject);
                }
            }
            createdManagers.Clear();
        }

        [Test]
        public void AllSixScenesExistAsValidUnitySceneAssets()
        {
            Assert.AreEqual(6, SceneNames.All.Count,
                "The architecture must contain exactly the six agreed scenes.");

            for (int i = 0; i < SceneNames.All.Count; i++)
            {
                string name = SceneNames.All[i];
                string path = Path.Combine(ScenesDirectory, name + ".unity");

                Assert.IsTrue(File.Exists(path), "Missing scene asset: " + path);
                Assert.IsTrue(File.Exists(path + ".meta"), "Missing scene meta: " + path + ".meta");

                string yaml = File.ReadAllText(path);
                Assert.IsTrue(yaml.StartsWith("%YAML 1.1", StringComparison.Ordinal),
                    name + " is not a Unity YAML asset.");
                StringAssert.Contains("!u!29 &1", yaml, name + " has no OcclusionCullingSettings.");
                StringAssert.Contains("SceneRoots:", yaml, name + " has no SceneRoots section.");
                StringAssert.Contains("m_Name: Main Camera", yaml, name + " has no Main Camera.");
                StringAssert.Contains("m_Name: Directional Light", yaml, name + " has no light.");
            }

            // Bedroom already existed; Issue #011 must reuse it, not duplicate it.
            Assert.AreEqual(1, Directory.GetFiles(ScenesDirectory, "Bedroom*.unity").Length,
                "Bedroom.unity should be the single Bedroom scene asset.");
        }

        [Test]
        public void SceneMetaGuidsAreUniqueAndPresentInBuildSettings()
        {
            string buildSettings = File.ReadAllText(BuildSettingsPath);
            HashSet<string> guids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < SceneNames.All.Count; i++)
            {
                string name = SceneNames.All[i];
                string guid = ReadMetaGuid(Path.Combine(ScenesDirectory, name + ".unity.meta"));

                Assert.IsTrue(guids.Add(guid), "Duplicate scene GUID for " + name + ": " + guid);
                StringAssert.Contains("path: Assets/Scenes/" + name + ".unity", buildSettings);
                StringAssert.Contains("guid: " + guid, buildSettings);
            }
        }

        [Test]
        public void BuildSettingsEnableAllSixScenesWithMainMenuFirst()
        {
            List<string> paths = new List<string>();
            Dictionary<string, EditorBuildSettingsScene> byName =
                new Dictionary<string, EditorBuildSettingsScene>(StringComparer.Ordinal);

            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                paths.Add(scene.path);
                if (scene.path.StartsWith(SceneNames.ScenesFolder, StringComparison.Ordinal))
                {
                    byName[Path.GetFileNameWithoutExtension(scene.path)] = scene;
                }
            }

            Assert.AreEqual(0, paths.IndexOf(SceneNames.ScenesFolder + SceneNames.MainMenu + ".unity"),
                "MainMenu must be the startup scene at build index 0.");

            int previousIndex = -1;
            for (int i = 0; i < SceneNames.All.Count; i++)
            {
                string name = SceneNames.All[i];
                string expectedPath = SceneNames.ScenesFolder + name + ".unity";

                Assert.IsTrue(byName.ContainsKey(name), name + " is missing from build settings.");
                Assert.IsTrue(byName[name].enabled, name + " is disabled in build settings.");

                int currentIndex = paths.IndexOf(expectedPath);
                Assert.Greater(currentIndex, previousIndex,
                    name + " is out of order in the build settings.");
                previousIndex = currentIndex;
            }

            // The pre-existing SampleScene entry is intentionally preserved.
            CollectionAssert.Contains(paths, SceneNames.ScenesFolder + "SampleScene.unity");
        }

        [Test]
        public void RegisteredSceneNamesResolveToBuildIndices()
        {
            for (int i = 0; i < SceneNames.All.Count; i++)
            {
                string name = SceneNames.All[i];
                string path;
                int buildIndex;
                string error;

                Assert.IsTrue(SceneNames.TryGetPath(name, out path));
                Assert.AreEqual(SceneNames.ScenesFolder + name + ".unity", path);

                Assert.IsTrue(SceneLoader.TryResolve(name, out buildIndex, out error),
                    name + " did not resolve: " + error);
                Assert.GreaterOrEqual(buildIndex, 0, name + " has no valid build index.");
                Assert.IsEmpty(error);
            }
        }

        [Test]
        public void UnknownSceneNamesAreRejectedWithMeaningfulErrors()
        {
            string[] invalidNames = { null, string.Empty, "mainmenu", "HAND", "MissingScene" };

            for (int i = 0; i < invalidNames.Length; i++)
            {
                string name = invalidNames[i];
                int buildIndex;
                string error;

                Assert.IsFalse(SceneNames.IsKnown(name), "Unexpectedly accepted: " + name);
                Assert.IsFalse(SceneLoader.TryResolve(name, out buildIndex, out error),
                    "Unexpectedly resolved: " + name);
                Assert.AreEqual(-1, buildIndex);
                Assert.IsNotEmpty(error, "No error message for: " + name);
                StringAssert.Contains("not a registered game scene", error);
            }
        }

        [Test]
        public void ActiveSceneComparisonPreventsUnnecessaryReloads()
        {
            Scene active = SceneManager.GetActiveScene();
            Assert.IsTrue(active.IsValid(), "No active scene is open in the Editor.");

            Assert.IsTrue(SceneLoader.IsSameScene(active, active.name));
            Assert.IsFalse(SceneLoader.IsSameScene(active, active.name + "_Different"));
        }

        [Test]
        public void GameManagerRegistrationKeepsOneInstance()
        {
            GameManager first = CreateManager("Game Manager Registration A");
            GameManager second = CreateManager("Game Manager Registration B");

            Assert.IsTrue(GameManager.Register(first));
            Assert.AreSame(first, GameManager.Instance);

            // Re-registering the same object must not duplicate event hooks.
            Assert.IsTrue(GameManager.Register(first));
            Assert.AreSame(first, GameManager.Instance);

            // A second instance is refused and removed.
            Assert.IsFalse(GameManager.Register(second));
            Assert.AreSame(first, GameManager.Instance);
        }

        [Test]
        public void GameManagerUnregisterClearsStaticReference()
        {
            GameManager manager = CreateManager("Game Manager Unregister");
            Assert.IsTrue(GameManager.Register(manager));
            Assert.AreSame(manager, GameManager.Instance);

            GameManager.Unregister(manager);
            Assert.IsNull(GameManager.Instance);
        }

        [Test]
        public void PersistenceRequiresAnUnparentedRootObject()
        {
            // DontDestroyOnLoad is runtime-only in Unity, so EditMode verifies
            // the precondition that decides whether persistence is even valid.
            // The actual DontDestroyOnLoad call is covered by a PlayMode test.
            GameManager rootManager = CreateManager("Game Manager Persistence Root");
            Assert.IsTrue(GameManager.CanPersist(rootManager),
                "A root GameManager must satisfy the persistence precondition.");

            GameObject parent = new GameObject("Persistence Parent");
            rootManager.transform.SetParent(parent.transform);
            try
            {
                Assert.IsFalse(GameManager.CanPersist(rootManager),
                    "A parented GameManager must be rejected by the persistence precondition.");
            }
            finally
            {
                rootManager.transform.SetParent(null);
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void SceneChangedEventReportsLoadedScene()
        {
            GameManager manager = CreateManager("Game Manager Events");
            Assert.IsTrue(GameManager.Register(manager));

            int calls = 0;
            Scene reportedScene = default(Scene);
            LoadSceneMode reportedMode = LoadSceneMode.Additive;
            manager.SceneChanged += (scene, mode) =>
            {
                calls++;
                reportedScene = scene;
                reportedMode = mode;
            };

            Scene active = SceneManager.GetActiveScene();
            manager.HandleSceneLoaded(active, LoadSceneMode.Single);

            Assert.AreEqual(1, calls);
            Assert.AreEqual(active.name, manager.CurrentSceneName);
            Assert.AreEqual(active.name, reportedScene.name);
            Assert.AreEqual(LoadSceneMode.Single, reportedMode);
        }

        private static string ReadMetaGuid(string metaPath)
        {
            Assert.IsTrue(File.Exists(metaPath), "Missing meta file: " + metaPath);
            Match match = Regex.Match(File.ReadAllText(metaPath),
                @"^guid:\s*([0-9a-fA-F]{32})\s*$", RegexOptions.Multiline);
            Assert.IsTrue(match.Success, "No GUID found in " + metaPath);
            return match.Groups[1].Value;
        }

        private GameManager CreateManager(string objectName)
        {
            GameObject root = new GameObject(objectName);
            GameManager manager = root.AddComponent<GameManager>();
            createdManagers.Add(manager);
            return manager;
        }
    }
}
