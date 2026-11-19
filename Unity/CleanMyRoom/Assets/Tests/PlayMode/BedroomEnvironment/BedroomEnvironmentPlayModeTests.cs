using System.Collections;
using CleanMyRoom.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CleanMyRoom.Tests.BedroomEnvironment
{
    /// <summary>
    /// Issue #012 PlayMode verification: the real Bedroom scene loads through
    /// the Issue #011 SceneLoader and its environment, furniture, lighting and
    /// imported MATLAB objects are active at runtime.
    /// </summary>
    public sealed class BedroomEnvironmentPlayModeTests
    {
        [UnityTest]
        public IEnumerator BedroomLoadsThroughSceneLoaderWithEnvironmentIntact()
        {
            LoadBedroomIfNeeded();
            yield return null;
            yield return null;

            Scene active = SceneManager.GetActiveScene();
            Assert.AreEqual(SceneNames.Bedroom, active.name, "Bedroom did not become the active scene.");

            string[] required =
            {
                "Bedroom_Floor", "Back_Wall", "Left_Wall", "Right_Wall",
                "Front_Wall_Left", "Ceiling", "Door_Leaf", "Window_Back_Left",
                "Window_Right", "Bed", "Dining Table", "Study Desk", "Wardrobe",
                "Bookshelf", "Light_Ceiling", "Main Camera", "Directional Light"
            };

            foreach (string name in required)
            {
                Assert.IsNotNull(FindInActiveScene(name), "Runtime object missing: " + name);
            }

            int activeLights = 0;
            foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light != null && light.enabled)
                {
                    activeLights++;
                }
            }
            Assert.GreaterOrEqual(activeLights, 3,
                "Bedroom needs the sun, ceiling light and nightstand light at runtime.");

            int colliders = 0;
            foreach (Collider collider in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if (collider != null && collider.enabled)
                {
                    colliders++;
                }
            }
            Assert.Greater(colliders, 15, "Bedroom colliders were not active at runtime.");
        }

        [UnityTest]
        public IEnumerator MatlabImportedObjectsRenderWithMaterialsAtRuntime()
        {
            LoadBedroomIfNeeded();
            yield return null;
            yield return null;

            string[] placed =
            {
                "MATLAB_ToyBlock_1", "MATLAB_ToyBlock_2", "MATLAB_ToyBlock_3",
                "MATLAB_ToyBlock_4", "MATLAB_Ball", "MATLAB_Drum", "MATLAB_DisplayBlock"
            };

            int renderedMeshes = 0;
            foreach (string name in placed)
            {
                GameObject go = FindInActiveScene(name);
                Assert.IsNotNull(go, "MATLAB object missing at runtime: " + name);
                MeshRenderer[] renderers = go.GetComponentsInChildren<MeshRenderer>();
                Assert.Greater(renderers.Length, 0, name + " has no renderer at runtime.");
                foreach (MeshRenderer renderer in renderers)
                {
                    Assert.IsTrue(renderer.enabled, name + " renderer is disabled.");
                    Assert.IsNotNull(renderer.sharedMaterial, name + " has no material at runtime.");
                    renderedMeshes++;
                }
            }
            Assert.GreaterOrEqual(renderedMeshes, placed.Length,
                "Imported MATLAB meshes should all be visible.");
        }

        private static void LoadBedroomIfNeeded()
        {
            // SceneLoader deliberately treats a request for the active scene as
            // a no-op, so a second test in the same PlayMode session must not
            // insist that Load returns true again.
            if (SceneManager.GetActiveScene().name == SceneNames.Bedroom)
            {
                return;
            }
            Assert.IsTrue(SceneLoader.Load(SceneNames.Bedroom), "SceneLoader rejected Bedroom.");
        }

        private static GameObject FindInActiveScene(string name)
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
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
    }
}
