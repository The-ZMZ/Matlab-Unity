using System;
using System.IO;
using CleanMyRoom.ColorSorting;
using CleanMyRoom.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CleanMyRoom.Tests.ColorSorting
{
    /// <summary>
    /// Issue #022 EditMode verification of the saved ColorSorting scene.
    /// The scene is opened additively and closed again so the Editor state is
    /// preserved. No asset is modified by these tests.
    /// </summary>
    public sealed class ColorSortingSceneTests
    {
        private const string ScenePath = "Assets/Scenes/ColorSorting.unity";
        private const string MaterialFolder = "Assets/Materials/ColorSorting";
        private const string PrefabFolder = "Assets/Prefabs/ColorSorting";

        private Scene originalScene;
        private Scene openedScene;
        private bool wasOpened;

        [SetUp]
        public void SetUp()
        {
            originalScene = SceneManager.GetActiveScene();
            openedScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            wasOpened = openedScene.IsValid() && openedScene.isLoaded;
            Assert.IsTrue(wasOpened, "ColorSorting.unity could not be opened additively.");
        }

        [TearDown]
        public void TearDown()
        {
            if (wasOpened && openedScene.IsValid())
            {
                EditorSceneManager.CloseScene(openedScene, true);
            }
            if (originalScene.IsValid())
            {
                SceneManager.SetActiveScene(originalScene);
            }
        }

        [Test]
        public void SceneAssetExistsAndIsRegisteredForNavigation()
        {
            string absolutePath = Path.Combine(Application.dataPath, "Scenes", "ColorSorting.unity");
            Assert.IsTrue(File.Exists(absolutePath), "Missing ColorSorting scene asset.");
            Assert.IsTrue(File.Exists(absolutePath + ".meta"), "Missing ColorSorting scene meta.");

            string yaml = File.ReadAllText(absolutePath);
            StringAssert.StartsWith("%YAML 1.1", yaml);
            StringAssert.Contains("m_Name: Main Camera", yaml,
                "The Issue #011 Main Camera must be preserved.");
            StringAssert.Contains("m_Name: Directional Light", yaml,
                "The Issue #011 Directional Light must be preserved.");
            StringAssert.Contains("m_Name: Color Sorting Activity", yaml);

            bool registered = false;
            foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
            {
                if (buildScene.path == ScenePath && buildScene.enabled)
                {
                    registered = true;
                }
            }
            Assert.IsTrue(registered, "ColorSorting must be enabled in Build Settings.");

            int index;
            string error;
            Assert.IsTrue(SceneLoader.TryResolve(SceneNames.ColorSorting, out index, out error),
                "SceneLoader must resolve ColorSorting: " + error);
        }

        [Test]
        public void SixDistinctBasketsHaveLabelsAndColliders()
        {
            ColorBasket[] baskets = FindAll<ColorBasket>();
            Assert.AreEqual(ColorSortingCatalog.BasketCount, baskets.Length,
                "The activity needs exactly six baskets.");

            Type expected = typeof(ColorBasket);
            Assert.IsNotNull(expected);
            int[] seen = new int[baskets.Length];
            for (int i = 0; i < baskets.Length; i++)
            {
                ColorBasket basket = baskets[i];
                Assert.IsFalse(string.IsNullOrEmpty(basket.BasketId), "Basket has no id.");
                Assert.IsNotNull(basket.GetComponent<Collider>(),
                    basket.BasketId + " is not raycastable.");

                ColorSortingCatalog.BasketInfo info =
                    ColorSortingCatalog.GetBasket(basket.BasketId);
                Assert.IsNotNull(info, "Unknown basket id: " + basket.BasketId);

                TextMesh label = FindInChildren<TextMesh>(basket.transform, "Basket_Label");
                Assert.IsNotNull(label, basket.BasketId + " has no visible label.");
                Assert.AreEqual(info.Label, label.text, "Basket label does not match its id.");

                seen[i] = Array.IndexOf(ColorSortingCatalog.AllBaskets, info);
                Assert.GreaterOrEqual(seen[i], 0);
            }
            Assert.AreEqual(baskets.Length, new System.Collections.Generic.HashSet<int>(seen).Count,
                "Basket ids must be unique.");
        }

        [Test]
        public void TwelveDistinctSortableObjectsHaveColliders()
        {
            SortableObject[] toys = FindAll<SortableObject>();
            Assert.AreEqual(ColorSortingCatalog.ObjectCount, toys.Length,
                "The activity needs exactly twelve sortable objects.");

            int[] seen = new int[toys.Length];
            for (int i = 0; i < toys.Length; i++)
            {
                SortableObject toy = toys[i];
                Assert.IsFalse(string.IsNullOrEmpty(toy.ObjectId), "Toy has no id.");
                Assert.IsFalse(toy.IsPlaced, "A fresh scene must have no placed toys.");
                Assert.IsNotNull(toy.GetComponent<Collider>(),
                    toy.ObjectId + " is not selectable by the mouse raycast.");

                ColorSortingCatalog.ObjectInfo info = ColorSortingCatalog.GetObject(toy.ObjectId);
                Assert.IsNotNull(info, "Unknown object id: " + toy.ObjectId);
                Assert.AreEqual(info.Label, toy.DisplayName);

                Renderer[] renderers = toy.GetComponentsInChildren<Renderer>();
                Assert.Greater(renderers.Length, 0, toy.ObjectId + " is invisible.");

                seen[i] = Array.IndexOf(ColorSortingCatalog.AllObjects, info);
                Assert.GreaterOrEqual(seen[i], 0);
            }
            Assert.AreEqual(toys.Length, new System.Collections.Generic.HashSet<int>(seen).Count,
                "Object ids must be unique.");
        }

        [Test]
        public void ControllerCursorFeedbackCameraAndInstructionsAreWired()
        {
            ColorSortingController controller = FindFirst<ColorSortingController>();
            Assert.IsNotNull(controller, "The scene has no ColorSortingController.");
            Assert.AreEqual(ColorSortingCatalog.BasketCount, controller.Baskets.Count);
            Assert.AreEqual(ColorSortingCatalog.ObjectCount, controller.SortableObjects.Count);

            Assert.IsNotNull(FindFirst<ActivityPointerCursor>(),
                "The mouse/keyboard pointer cursor is missing.");
            Assert.IsNotNull(FindFirst<ColorSortingFeedback>(),
                "The feedback component is missing.");
            Assert.IsNotNull(GameObject.Find("Main Camera").GetComponent<Camera>());

            TextMesh instructions = FindAnyByExactName<TextMesh>("Instruction_Text");
            Assert.IsNotNull(instructions, "Instruction text is missing.");
            StringAssert.Contains("basket", instructions.text.ToLowerInvariant());
            StringAssert.Contains("mouse", instructions.text.ToLowerInvariant());
            StringAssert.Contains("keyboard", instructions.text.ToLowerInvariant());

            TextMesh progress = FindAnyByExactName<TextMesh>("Progress_Text");
            Assert.IsNotNull(progress, "Progress counter is missing.");
            StringAssert.Contains("0 / 12", progress.text);

            Light sun = GameObject.Find("Directional Light").GetComponent<Light>();
            Assert.IsNotNull(sun, "Directional light is missing.");
            Assert.Greater(sun.intensity, 0f);
        }

        [Test]
        public void ReusableAssetsExist()
        {
            string[] prefabs =
            {
                "PF_ColorBasket.prefab", "PF_SortableToy.prefab", "PF_ActivityPointer.prefab"
            };
            foreach (string prefab in prefabs)
            {
                string path = PrefabFolder + "/" + prefab;
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(path),
                    "Missing prefab: " + path);
            }

            string[] materials =
            {
                "M_ColorSorting_Floor", "M_ColorSorting_Wall", "M_ColorSorting_Board",
                "M_ColorSorting_Basket", "M_ColorSorting_Cursor",
                "M_ColorSorting_Red", "M_ColorSorting_Blue", "M_ColorSorting_Green",
                "M_ColorSorting_Yellow", "M_ColorSorting_Orange", "M_ColorSorting_Purple"
            };
            foreach (string material in materials)
            {
                string path = MaterialFolder + "/" + material + ".mat";
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Material>(path),
                    "Missing material: " + path);
            }
        }

        private static T FindFirst<T>() where T : Component
        {
            T[] found = UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None);
            return found.Length > 0 ? found[0] : null;
        }

        private static T[] FindAll<T>() where T : Component
        {
            return UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None);
        }

        private static T FindAnyByExactName<T>(string name) where T : Component
        {
            foreach (T component in UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None))
            {
                if (component.gameObject.name == name)
                {
                    return component;
                }
            }
            return null;
        }

        private static T FindInChildren<T>(Transform root, string name) where T : Component
        {
            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == name)
                {
                    return child.GetComponent<T>();
                }
                T nested = FindInChildren<T>(child, name);
                if (nested != null)
                {
                    return nested;
                }
            }
            return null;
        }
    }
}
