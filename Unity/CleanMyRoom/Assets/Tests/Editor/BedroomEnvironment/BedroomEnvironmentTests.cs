using System;
using System.Collections.Generic;
using System.IO;
using CleanMyRoom.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CleanMyRoom.Tests.BedroomEnvironment
{
    /// <summary>
    /// Issue #012 EditMode verification. The Bedroom scene is opened additively
    /// and closed again on teardown, so the scene currently open in the Editor
    /// is preserved. No scene asset is saved by these tests.
    /// </summary>
    public sealed class BedroomEnvironmentTests
    {
        private const string BedroomPath = "Assets/Scenes/Bedroom.unity";
        private const string MatlabModelFolder = "Assets/Models/MATLAB";
        private const string MaterialFolder = "Assets/Materials/Bedroom";

        private Scene openedScene;
        private Scene originalScene;
        private bool wasOpened;

        [SetUp]
        public void SetUp()
        {
            originalScene = SceneManager.GetActiveScene();
            openedScene = EditorSceneManager.OpenScene(BedroomPath, OpenSceneMode.Additive);
            wasOpened = openedScene.IsValid() && openedScene.isLoaded;
            Assert.IsTrue(wasOpened, "Bedroom scene could not be opened additively.");
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
        public void BedroomSceneAssetExistsAndPreservesArchitecture()
        {
            string absolutePath = Path.Combine(Application.dataPath, "Scenes", "Bedroom.unity");
            Assert.IsTrue(File.Exists(absolutePath), "Missing Bedroom scene asset.");
            Assert.IsTrue(File.Exists(absolutePath + ".meta"), "Missing Bedroom scene meta.");

            string yaml = File.ReadAllText(absolutePath);
            StringAssert.StartsWith("%YAML 1.1", yaml);
            StringAssert.Contains("m_Name: Main Camera", yaml,
                "The Issue #011 Main Camera must be preserved.");
            StringAssert.Contains("m_Name: Directional Light", yaml,
                "The Issue #011 Directional Light must be preserved.");
            StringAssert.Contains("m_Name: Bedroom_Floor", yaml);
            StringAssert.Contains("m_Name: Back_Wall", yaml);
            StringAssert.Contains("m_Name: Left_Wall", yaml);
            StringAssert.Contains("m_Name: Right_Wall", yaml,
                "The pre-existing right wall must be present (typo corrected).");

            Assert.AreEqual(6, SceneNames.All.Count, "The agreed six-scene architecture must remain.");
            Assert.IsTrue(EditorBuildSettings.scenes.Length >= 6);
            bool registered = false;
            foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
            {
                if (buildScene.path == BedroomPath && buildScene.enabled)
                {
                    registered = true;
                }
            }
            Assert.IsTrue(registered, "Bedroom must remain enabled in Build Settings.");

            int index;
            string error;
            Assert.IsTrue(SceneLoader.TryResolve(SceneNames.Bedroom, out index, out error),
                "Bedroom must still resolve through SceneLoader: " + error);
        }

        [Test]
        public void RequiredEnvironmentElementsExistWithValidTransforms()
        {
            string[] required =
            {
                "Bedroom_Floor", "Back_Wall", "Left_Wall", "Right_Wall",
                "Front_Wall_Left", "Front_Wall_Right", "Front_Wall_Lintel", "Ceiling",
                "Door_Leaf", "Door_Frame_Top", "Door_Mat",
                "Window_Back_Left", "Window_Back_Right", "Window_Right",
                "Light_Ceiling", "Main Camera", "Directional Light"
            };

            foreach (string name in required)
            {
                GameObject go = Find(name);
                Assert.IsNotNull(go, "Missing environment object: " + name);
                AssertValidTransform(go, name);
            }

            Assert.IsNotNull(FindComponent<Camera>("Main Camera"), "Main Camera has no Camera component.");
            Assert.IsNotNull(FindComponent<Light>("Directional Light"), "Directional Light has no Light.");
            Assert.IsNotNull(FindComponent<Light>("Light_Ceiling"), "Ceiling light has no Light.");

            // Every window must actually render glass, not an empty hole.
            foreach (string window in new[] { "Window_Back_Left", "Window_Back_Right", "Window_Right" })
            {
                MeshRenderer glass = FindDeepComponent<MeshRenderer>(Find(window), "Glass");
                Assert.IsNotNull(glass, window + " has no Glass renderer.");
                Assert.IsNotNull(glass.sharedMaterial, window + " glass has no material.");
            }
        }

        [Test]
        public void RequiredFurnitureExistsWithReasonableDimensionsAndColliders()
        {
            string[] required =
            {
                "Bed", "Nightstand", "Dining Table", "Study Desk", "Wardrobe",
                "Bookshelf", "Desk Wall Shelf", "Play Rug", "Room_Divider"
            };

            foreach (string name in required)
            {
                GameObject go = Find(name);
                Assert.IsNotNull(go, "Missing furniture or decoration: " + name);
                AssertValidTransform(go, name);
            }

            string[] collidable =
            {
                "Bed", "Nightstand", "Dining Table", "Study Desk", "Wardrobe",
                "Bookshelf", "Room_Divider"
            };
            foreach (string name in collidable)
            {
                Collider collider = Find(name).GetComponent<Collider>();
                Assert.IsNotNull(collider, name + " needs a navigation collider.");
                Assert.Greater(collider.bounds.size.x, 0.1f, name + " collider has no width.");
                Assert.Greater(collider.bounds.size.y, 0.1f, name + " collider has no height.");
                Assert.Greater(collider.bounds.size.z, 0.1f, name + " collider has no depth.");
            }

            // Chairs are prefab instances and must stay linked to their prefab.
            GameObject chair = Find("Chair_Study");
            Assert.IsNotNull(chair, "Study chair is missing.");
            Assert.IsNotNull(FindDeepComponent<MeshRenderer>(chair, "Seat"),
                "Chair has no visible renderer.");
            string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(chair);
            StringAssert.EndsWith("PF_Chair.prefab", prefabPath,
                "Chair must remain an instance of PF_Chair.prefab.");
        }

        [Test]
        public void MatlabGeneratedMeshesAreImportedAndUsedInScene()
        {
            string root = Path.Combine(Application.dataPath, "Models", "MATLAB");
            string[] modelAssets =
            {
                "Issue010/Issue010_Box_Unity.obj",
                "Issue010/Issue010_Box_Unity.mtl",
                "Issue007/MATLAB_Cube.obj",
                "Issue007/MATLAB_Sphere.obj",
                "Issue007/MATLAB_Cylinder.obj"
            };
            foreach (string relative in modelAssets)
            {
                string absolute = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
                Assert.IsTrue(File.Exists(absolute), "Missing MATLAB export copy: " + absolute);
            }

            string[] importedObj =
            {
                MatlabModelFolder + "/Issue010/Issue010_Box_Unity.obj",
                MatlabModelFolder + "/Issue007/MATLAB_Cube.obj",
                MatlabModelFolder + "/Issue007/MATLAB_Sphere.obj",
                MatlabModelFolder + "/Issue007/MATLAB_Cylinder.obj"
            };
            foreach (string assetPath in importedObj)
            {
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                Assert.IsNotNull(model, "Unity did not import " + assetPath);
                MeshFilter filter = model.GetComponentInChildren<MeshFilter>();
                Assert.IsNotNull(filter, assetPath + " has no MeshFilter.");
                Assert.Greater(filter.sharedMesh.vertexCount, 0, assetPath + " has no vertices.");
            }

            string[] placed =
            {
                "MATLAB_ToyBlock_1", "MATLAB_ToyBlock_2", "MATLAB_ToyBlock_3",
                "MATLAB_ToyBlock_4", "MATLAB_Ball", "MATLAB_Drum", "MATLAB_DisplayBlock"
            };
            foreach (string name in placed)
            {
                GameObject go = Find(name);
                Assert.IsNotNull(go, "MATLAB-generated object is not placed in the scene: " + name);
                MeshRenderer[] renderers = go.GetComponentsInChildren<MeshRenderer>();
                Assert.Greater(renderers.Length, 0, name + " has no MeshRenderer.");
                foreach (MeshRenderer renderer in renderers)
                {
                    Assert.IsNotNull(renderer.sharedMaterial, name + " has a missing material.");
                }
                AssertValidTransform(go, name);
            }
        }

        [Test]
        public void NavigationWaypointsAreNotBlockedByFurniture()
        {
            Vector3[] waypoints =
            {
                new Vector3(-3.5f, 0.6f, -7.0f),
                new Vector3(0f, 0.6f, -6.5f),
                new Vector3(-3.5f, 0.6f, 0f),
                new Vector3(3.5f, 0.6f, 4.0f),
                new Vector3(-3.5f, 0.6f, 7.0f)
            };

            // Collect colliders from every root, including the pre-existing shell.
            List<Collider> blockers = new List<Collider>();
            foreach (GameObject root in openedScene.GetRootGameObjects())
            {
                blockers.AddRange(root.GetComponentsInChildren<Collider>(true));
            }

            Assert.Greater(blockers.Count, 10, "The room should contain navigation colliders.");

            foreach (Vector3 waypoint in waypoints)
            {
                foreach (Collider collider in blockers)
                {
                    if (collider == null || !collider.enabled || collider is MeshCollider)
                    {
                        continue;
                    }
                    Bounds bounds = collider.bounds;
                    bounds.Expand(0.25f);
                    Assert.IsFalse(bounds.Contains(waypoint),
                        "Waypoint " + waypoint + " is blocked by " + collider.gameObject.name);
                }
            }
        }

        [Test]
        public void AllBedroomMaterialsAndPrefabAssetsExist()
        {
            Assert.IsTrue(AssetDatabase.IsValidFolder(MaterialFolder),
                "Bedroom material folder is missing.");
            Assert.IsTrue(AssetDatabase.IsValidFolder("Assets/Prefabs/Bedroom"),
                "Bedroom prefab folder is missing.");

            string[] prefabs =
            {
                "Assets/Prefabs/Bedroom/PF_Chair.prefab",
                "Assets/Prefabs/Bedroom/PF_WallPicture.prefab",
                "Assets/Prefabs/Bedroom/PF_MatlabToyBlock.prefab"
            };
            foreach (string path in prefabs)
            {
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(path),
                    "Missing prefab: " + path);
            }

            string[] materials =
            {
                "M_Floor_Wood", "M_Wall_Cream", "M_Wall_Sky", "M_Bed_Blanket",
                "M_Table_Top", "M_Desk_Top", "M_Window_Glass", "M_Toy_Red"
            };
            foreach (string name in materials)
            {
                string path = MaterialFolder + "/" + name + ".mat";
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Material>(path),
                    "Missing material: " + path);
            }
        }

        // -----------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------

        private GameObject Find(string name)
        {
            foreach (GameObject root in openedScene.GetRootGameObjects())
            {
                GameObject found = FindInHierarchy(root.transform, name);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        private static GameObject FindInHierarchy(Transform parent, string name)
        {
            if (parent.name == name)
            {
                return parent.gameObject;
            }
            foreach (Transform child in parent)
            {
                GameObject found = FindInHierarchy(child, name);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        private static T FindDeepComponent<T>(GameObject root, string childName) where T : Component
        {
            if (root == null)
            {
                return null;
            }
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == childName)
                {
                    T component = child.GetComponent<T>();
                    if (component != null)
                    {
                        return component;
                    }
                }
            }
            return null;
        }

        private T FindComponent<T>(string objectName) where T : Component
        {
            GameObject root = Find(objectName);
            return root != null ? root.GetComponent<T>() : null;
        }

        private static void AssertValidTransform(GameObject go, string name)
        {
            Vector3 position = go.transform.position;
            Vector3 scale = go.transform.lossyScale;
            Assert.IsFalse(float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z),
                name + " has a non-finite position.");
            Assert.Less(Mathf.Abs(position.x), 11f, name + " is outside the room on X.");
            Assert.Greater(position.y, -1f, name + " is below the floor.");
            Assert.Less(position.y, 6f, name + " is above the ceiling.");
            Assert.Less(Mathf.Abs(position.z), 11f, name + " is outside the room on Z.");

            foreach (float component in new[] { scale.x, scale.y, scale.z })
            {
                Assert.Greater(component, 0.001f, name + " has a zero/negative scale component.");
                Assert.Less(component, 30f, name + " has an unrealistic scale.");
            }
        }
    }
}
