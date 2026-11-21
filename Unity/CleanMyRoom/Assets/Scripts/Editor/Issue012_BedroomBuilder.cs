using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace CleanMyRoom.Editor.Issue012
{
    /// <summary>
    /// Issue #012 - builds the complete children's bedroom in the existing
    /// Assets/Scenes/Bedroom.unity scene.
    ///
    /// This is a one-shot, idempotent editor builder. It reuses the floor and
    /// three walls that already exist in the scene, adds the fourth wall,
    /// ceiling, entrance, windows, furniture, decorations, lighting and the
    /// MATLAB-generated meshes exported by Issues #007 and #010.
    ///
    /// Run from the command line with:
    ///   Unity.exe -batchmode -projectPath ... -executeMethod
    ///       CleanMyRoom.Editor.Issue012.BedroomEnvironmentBuilder.Build
    /// </summary>
    public static class BedroomEnvironmentBuilder
    {
        private const string ScenePath = "Assets/Scenes/Bedroom.unity";
        private const string MaterialFolder = "Assets/Materials/Bedroom";
        private const string ModelFolder = "Assets/Models/MATLAB";
        private const string PrefabFolder = "Assets/Prefabs/Bedroom";
        private const string ScreenshotFolder = "Deliverables/Screenshots";

        private static readonly Dictionary<string, Material> Materials =
            new Dictionary<string, Material>(StringComparer.Ordinal);

        private static Scene scene;

        // -----------------------------------------------------------------
        // Entry points
        // -----------------------------------------------------------------

        [MenuItem("Clean My Room/Issue 012/Build Bedroom Environment")]
        public static void Build()
        {
            try
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

                EnsureFolder("Assets/Materials");
                EnsureFolder(MaterialFolder);
                EnsureFolder("Assets/Models");
                EnsureFolder(ModelFolder);
                EnsureFolder("Assets/Prefabs");
                EnsureFolder(PrefabFolder);

                CreateMaterials();
                PrepareExistingShell();
                ImportMatlabAssets();

                Transform environment = Group("Bedroom Environment", null);
                Transform furniture = Group("Furniture", null);
                Transform decorations = Group("Decorations", null);

                BuildEnvironment(environment);
                BuildFurniture(furniture);
                BuildDecorations(decorations);
                PlaceImportedToys(decorations);
                ConfigureLighting();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                Debug.Log("[Issue012] Bedroom built: " + CountSceneObjects() +
                          " objects, " + Materials.Count + " materials, scene saved to " + ScenePath);
            }
            catch (Exception exception)
            {
                Debug.LogError("[Issue012] Bedroom build FAILED: " + exception);
                throw;
            }
        }

        [MenuItem("Clean My Room/Issue 012/Capture Bedroom Screenshots")]
        public static void CaptureScreenshots()
        {
            string repoRoot = RepositoryRoot();
            string outputFolder = Path.Combine(repoRoot, ScreenshotFolder.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(outputFolder);

            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Camera camera = FindComponent<Camera>("Main Camera");
            if (camera == null)
            {
                Debug.LogError("[Issue012] Main Camera not found - cannot capture screenshots.");
                return;
            }

            Vector3 savedPosition = camera.transform.position;
            Quaternion savedRotation = camera.transform.rotation;
            float savedFov = camera.fieldOfView;

            try
            {
                // URP needs one throwaway render after a batchmode scene load
                // before shaders/materials are ready. Without this the first
                // captured frame can be a flat grey fallback shader.
                WarmUpRender(camera);

                RenderToPng(camera, Path.Combine(outputFolder, "Issue012_Bedroom_Overview.png"),
                    1280, 720,
                    new Vector3(6.6f, 4.8f, -7.4f),
                    new Vector3(-1.0f, 1.35f, 2.6f), 62f);

                RenderToPng(camera, Path.Combine(outputFolder, "Issue012_Bedroom_DoorAndWardrobe.png"),
                    1280, 720,
                    new Vector3(4.2f, 2.7f, -5.2f),
                    new Vector3(-9.4f, 1.4f, -0.8f), 60f);

                RenderToPng(camera, Path.Combine(outputFolder, "Issue012_Bedroom_ActivityStations.png"),
                    1280, 720,
                    new Vector3(-1.5f, 4.4f, -7.8f),
                    new Vector3(0.5f, 0.9f, 4.0f), 66f);

                Debug.Log("[Issue012] Screenshots written to " + outputFolder);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Issue012] Screenshot capture unavailable in this environment: " +
                                 exception.Message);
            }
            finally
            {
                camera.transform.position = savedPosition;
                camera.transform.rotation = savedRotation;
                camera.fieldOfView = savedFov;
            }
        }

        private static void WarmUpRender(Camera camera)
        {
            var renderTexture = new RenderTexture(64, 64, 16, RenderTextureFormat.ARGB32);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        private static void RenderToPng(Camera camera, string path, int width, int height,
            Vector3 eye, Vector3 target, float fov)
        {
            camera.transform.position = eye;
            camera.transform.rotation = Quaternion.LookRotation(target - eye, Vector3.up);
            camera.fieldOfView = fov;
            camera.aspect = (float)width / height;

            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);

            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        // -----------------------------------------------------------------
        // Scene shell
        // -----------------------------------------------------------------

        private static void PrepareExistingShell()
        {
            GameObject floor = GameObject.Find("Bedroom_Floor");
            if (floor == null)
            {
                throw new InvalidOperationException("Bedroom_Floor is missing from " + ScenePath);
            }
            SetMaterial(floor, "M_Floor_Wood");

            GameObject back = GameObject.Find("Back_Wall");
            GameObject left = GameObject.Find("Left_Wall");
            GameObject right = GameObject.Find("Right_Wall");
            if (right == null)
            {
                right = GameObject.Find("Righr_Wall");
            }

            if (back == null || left == null || right == null)
            {
                throw new InvalidOperationException("One of the existing walls is missing from " + ScenePath);
            }

            // Correct the pre-existing typo without changing the wall's transform.
            if (right.name == "Righr_Wall")
            {
                right.name = "Right_Wall";
            }

            SetMaterial(back, "M_Wall_Sky");
            SetMaterial(left, "M_Wall_Cream");
            SetMaterial(right, "M_Wall_Cream");

            // The existing walls only cover three sides. Add a fourth wall with
            // a wide opening so the room reads as enclosed from every viewpoint.
            Transform shell = Group("Room Shell", null);

            Box("Front_Wall_Left", new Vector3(-5.5f, 2.5f, -9.9f),
                new Vector3(8.8f, 5f, 0.2f), Mat("M_Wall_Cream"), shell, true);
            Box("Front_Wall_Right", new Vector3(5.5f, 2.5f, -9.9f),
                new Vector3(8.8f, 5f, 0.2f), Mat("M_Wall_Cream"), shell, true);
            Box("Front_Wall_Lintel", new Vector3(0f, 4.15f, -9.9f),
                new Vector3(2.2f, 1.7f, 0.2f), Mat("M_Wall_Cream"), shell, true);
            Box("Ceiling", new Vector3(0f, 5.1f, 0f),
                new Vector3(20f, 0.2f, 20f), Mat("M_Ceiling_White"), shell, false);

            // Skirting boards hide the wall/floor seam.
            Box("Skirting_Back", new Vector3(0f, 0.09f, 9.68f),
                new Vector3(19.6f, 0.18f, 0.08f), Mat("M_Skirt_White"), shell, false);
            Box("Skirting_Left", new Vector3(-9.68f, 0.09f, 0f),
                new Vector3(0.08f, 0.18f, 19.6f), Mat("M_Skirt_White"), shell, false);
            Box("Skirting_Right", new Vector3(9.68f, 0.09f, 0f),
                new Vector3(0.08f, 0.18f, 19.6f), Mat("M_Skirt_White"), shell, false);
            Box("Skirting_Front_Left", new Vector3(-5.5f, 0.09f, -9.68f),
                new Vector3(8.8f, 0.18f, 0.08f), Mat("M_Skirt_White"), shell, false);
            Box("Skirting_Front_Right", new Vector3(5.5f, 0.09f, -9.68f),
                new Vector3(8.8f, 0.18f, 0.08f), Mat("M_Skirt_White"), shell, false);
        }

        private static void BuildEnvironment(Transform root)
        {
            Transform doorGroup = Group("Entrance Door", root);
            // The door sits in the left wall, between the wardrobe and the bed.
            Box("Door_Leaf", new Vector3(-9.78f, 1.2f, -0.5f),
                new Vector3(0.09f, 2.4f, 1.75f), Mat("M_Door_Wood"), doorGroup, true);
            Box("Door_Frame_Front", new Vector3(-9.72f, 1.27f, -1.43f),
                new Vector3(0.14f, 2.55f, 0.14f), Mat("M_Door_Frame"), doorGroup, false);
            Box("Door_Frame_Back", new Vector3(-9.72f, 1.27f, 0.43f),
                new Vector3(0.14f, 2.55f, 0.14f), Mat("M_Door_Frame"), doorGroup, false);
            Box("Door_Frame_Top", new Vector3(-9.72f, 2.51f, -0.5f),
                new Vector3(0.14f, 0.14f, 2.0f), Mat("M_Door_Frame"), doorGroup, false);
            Sphere("Door_Handle", new Vector3(-9.62f, 1.12f, 0.06f),
                0.16f, Mat("M_Toy_Yellow"), doorGroup, false);
            Box("Door_Mat", new Vector3(-8.95f, 0.025f, -0.5f),
                new Vector3(1.35f, 0.05f, 2.1f), Mat("M_Rug_Coral"), doorGroup, false);

            Transform windows = Group("Windows", root);
            BuildBackWindow(windows, -2.5f, "Window_Back_Left");
            BuildBackWindow(windows, 2.5f, "Window_Back_Right");
            BuildSideWindow(windows);

            // Low divider shelf and a cosy reading corner near the entrance.
            Box("Room_Divider", new Vector3(-4.4f, 0.45f, -7.8f),
                new Vector3(3.0f, 0.9f, 0.45f), Mat("M_Shelf_Wood"), root, true);
        }

        private static void BuildBackWindow(Transform parent, float x, string name)
        {
            Transform window = Group(name, parent);
            const float z = 9.7f;
            Box("Glass", new Vector3(x, 2.95f, z),
                new Vector3(2.7f, 1.75f, 0.05f), Mat("M_Window_Glass"), window, false);
            Box("Frame_Top", new Vector3(x, 3.87f, z - 0.03f),
                new Vector3(3.0f, 0.14f, 0.12f), Mat("M_Window_Frame"), window, false);
            Box("Frame_Bottom", new Vector3(x, 2.03f, z - 0.03f),
                new Vector3(3.0f, 0.14f, 0.12f), Mat("M_Window_Frame"), window, false);
            Box("Frame_Left", new Vector3(x - 1.43f, 2.95f, z - 0.03f),
                new Vector3(0.14f, 1.98f, 0.12f), Mat("M_Window_Frame"), window, false);
            Box("Frame_Right", new Vector3(x + 1.43f, 2.95f, z - 0.03f),
                new Vector3(0.14f, 1.98f, 0.12f), Mat("M_Window_Frame"), window, false);
            Box("Frame_Cross", new Vector3(x, 2.95f, z - 0.05f),
                new Vector3(0.1f, 1.9f, 0.1f), Mat("M_Window_Frame"), window, false);
            Box("Frame_Mid", new Vector3(x, 2.95f, z - 0.05f),
                new Vector3(2.8f, 0.1f, 0.1f), Mat("M_Window_Frame"), window, false);
            Box("Sill", new Vector3(x, 1.95f, z - 0.18f),
                new Vector3(3.2f, 0.1f, 0.4f), Mat("M_Window_Frame"), window, false);

            string curtainMaterial = x < 0 ? "M_Curtain_Pink" : "M_Curtain_Blue";
            Box("Curtain_Left", new Vector3(x - 1.5f, 3.0f, z - 0.35f),
                new Vector3(0.42f, 2.25f, 0.14f), Mat(curtainMaterial), window, false);
            Box("Curtain_Right", new Vector3(x + 1.5f, 3.0f, z - 0.35f),
                new Vector3(0.42f, 2.25f, 0.14f), Mat(curtainMaterial), window, false);
        }

        private static void BuildSideWindow(Transform parent)
        {
            Transform window = Group("Window_Right", parent);
            const float x = 9.7f;
            const float z = 0.5f;
            Box("Glass", new Vector3(x, 2.95f, z),
                new Vector3(0.05f, 1.75f, 2.7f), Mat("M_Window_Glass"), window, false);
            Box("Frame_Top", new Vector3(x - 0.03f, 3.87f, z),
                new Vector3(0.12f, 0.14f, 3.0f), Mat("M_Window_Frame"), window, false);
            Box("Frame_Bottom", new Vector3(x - 0.03f, 2.03f, z),
                new Vector3(0.12f, 0.14f, 3.0f), Mat("M_Window_Frame"), window, false);
            Box("Frame_Front", new Vector3(x - 0.03f, 2.95f, z - 1.43f),
                new Vector3(0.12f, 1.98f, 0.14f), Mat("M_Window_Frame"), window, false);
            Box("Frame_Back", new Vector3(x - 0.03f, 2.95f, z + 1.43f),
                new Vector3(0.12f, 1.98f, 0.14f), Mat("M_Window_Frame"), window, false);
            Box("Frame_Cross", new Vector3(x - 0.05f, 2.95f, z),
                new Vector3(0.1f, 1.9f, 0.1f), Mat("M_Window_Frame"), window, false);
            Box("Sill", new Vector3(x - 0.18f, 1.95f, z),
                new Vector3(0.4f, 0.1f, 3.2f), Mat("M_Window_Frame"), window, false);
            Box("Curtain_Front", new Vector3(x - 0.35f, 3.0f, z - 1.5f),
                new Vector3(0.14f, 2.25f, 0.42f), Mat("M_Curtain_Blue"), window, false);
            Box("Curtain_Back", new Vector3(x - 0.35f, 3.0f, z + 1.5f),
                new Vector3(0.14f, 2.25f, 0.42f), Mat("M_Curtain_Blue"), window, false);
        }

        // -----------------------------------------------------------------
        // Furniture
        // -----------------------------------------------------------------

        private static void BuildFurniture(Transform root)
        {
            BuildBed(root);
            BuildNightstand(root);
            BuildDiningTable(root);
            BuildStudyDesk(root);
            BuildWardrobe(root);
            BuildBookshelf(root);
        }

        private static void BuildBed(Transform root)
        {
            Transform bed = Group("Bed", root);
            bed.position = new Vector3(-7.8f, 0f, 5.4f);

            Box("Frame_Base", new Vector3(0f, 0.3f, 0f),
                new Vector3(2.6f, 0.5f, 3.6f), Mat("M_Bed_Frame"), bed, false);
            Box("Mattress", new Vector3(0f, 0.66f, 0f),
                new Vector3(2.42f, 0.22f, 3.32f), Mat("M_Bed_Mattress"), bed, false);
            Box("Blanket", new Vector3(0f, 0.81f, -0.65f),
                new Vector3(2.5f, 0.1f, 1.95f), Mat("M_Bed_Blanket"), bed, false);
            Box("Blanket_Fold", new Vector3(0f, 0.87f, 0.33f),
                new Vector3(2.5f, 0.06f, 0.2f), Mat("M_Rug_Cream"), bed, false);
            Box("Pillow_Left", new Vector3(-0.58f, 0.85f, 1.18f),
                new Vector3(0.95f, 0.16f, 0.55f), Mat("M_Bed_Pillow"), bed, false);
            Box("Pillow_Right", new Vector3(0.58f, 0.85f, 1.18f),
                new Vector3(0.95f, 0.16f, 0.55f), Mat("M_Bed_Pillow"), bed, false);
            Box("Headboard", new Vector3(0f, 0.95f, 1.86f),
                new Vector3(2.85f, 1.25f, 0.16f), Mat("M_Bed_Frame"), bed, false);
            Box("Footboard", new Vector3(0f, 0.55f, -1.86f),
                new Vector3(2.85f, 0.55f, 0.14f), Mat("M_Bed_Frame"), bed, false);
            Box("Leg_FL", new Vector3(-1.16f, 0.1f, -1.56f),
                new Vector3(0.18f, 0.2f, 0.18f), Mat("M_Table_Leg"), bed, false);
            Box("Leg_FR", new Vector3(1.16f, 0.1f, -1.56f),
                new Vector3(0.18f, 0.2f, 0.18f), Mat("M_Table_Leg"), bed, false);
            Box("Leg_BL", new Vector3(-1.16f, 0.1f, 1.56f),
                new Vector3(0.18f, 0.2f, 0.18f), Mat("M_Table_Leg"), bed, false);
            Box("Leg_BR", new Vector3(1.16f, 0.1f, 1.56f),
                new Vector3(0.18f, 0.2f, 0.18f), Mat("M_Table_Leg"), bed, false);

            BoxCollider collider = bed.gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.65f, 0f);
            collider.size = new Vector3(2.9f, 1.3f, 3.9f);
        }

        private static void BuildNightstand(Transform root)
        {
            Transform stand = Group("Nightstand", root);
            stand.position = new Vector3(-5.45f, 0f, 7.6f);
            Box("Body", new Vector3(0f, 0.32f, 0f),
                new Vector3(0.95f, 0.64f, 0.85f), Mat("M_Bed_Frame"), stand, false);
            Box("Drawer", new Vector3(0f, 0.44f, -0.44f),
                new Vector3(0.78f, 0.3f, 0.05f), Mat("M_Wardrobe_Door"), stand, false);
            Sphere("Drawer_Knob", new Vector3(0f, 0.44f, -0.49f),
                0.11f, Mat("M_Toy_Yellow"), stand, false);

            Cylinder("Lamp_Base", new Vector3(0f, 0.73f, 0f),
                0.24f, 0.1f, Mat("M_Table_Leg"), stand, false);
            Cylinder("Lamp_Stem", new Vector3(0f, 0.95f, 0f),
                0.06f, 0.4f, Mat("M_Table_Leg"), stand, false);
            Cylinder("Lamp_Shade", new Vector3(0f, 1.24f, 0f),
                0.42f, 0.28f, Mat("M_Lamp_Shade"), stand, false);

            var lightObject = new GameObject("Nightstand_Light");
            lightObject.transform.SetParent(stand, false);
            lightObject.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.91f, 0.72f);
            light.intensity = 0.8f;
            light.range = 5f;
            light.shadows = LightShadows.None;

            BoxCollider collider = stand.gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.32f, 0f);
            collider.size = new Vector3(1f, 0.68f, 0.9f);
        }

        private static void BuildDiningTable(Transform root)
        {
            Transform table = Group("Dining Table", root);
            table.position = new Vector3(0.8f, 0f, -1.5f);

            Box("Table_Top", new Vector3(0f, 0.76f, 0f),
                new Vector3(3.0f, 0.12f, 1.8f), Mat("M_Table_Top"), table, false);
            Box("Apron_Front", new Vector3(0f, 0.64f, -0.78f),
                new Vector3(2.6f, 0.12f, 0.08f), Mat("M_Table_Leg"), table, false);
            Box("Apron_Back", new Vector3(0f, 0.64f, 0.78f),
                new Vector3(2.6f, 0.12f, 0.08f), Mat("M_Table_Leg"), table, false);
            float[] xs = { -1.28f, 1.28f };
            float[] zs = { -0.68f, 0.68f };
            foreach (float x in xs)
            {
                foreach (float z in zs)
                {
                    Box("Leg_" + (x < 0 ? "L" : "R") + (z < 0 ? "F" : "B"),
                        new Vector3(x, 0.36f, z),
                        new Vector3(0.14f, 0.72f, 0.14f), Mat("M_Table_Leg"), table, false);
                }
            }

            BoxCollider collider = table.gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.45f, 0f);
            collider.size = new Vector3(3.05f, 0.9f, 1.85f);

            InstantiateChair(root, "Chair_Table_Front", new Vector3(0.8f, 0f, -3.15f), 0f);
            InstantiateChair(root, "Chair_Table_Back", new Vector3(0.8f, 0f, 0.15f), 180f);
            InstantiateChair(root, "Chair_Table_Right", new Vector3(2.85f, 0f, -1.5f), 270f);
        }

        private static void BuildStudyDesk(Transform root)
        {
            Transform desk = Group("Study Desk", root);
            desk.position = new Vector3(5.6f, 0f, 8.2f);

            Box("Desk_Top", new Vector3(0f, 0.75f, 0f),
                new Vector3(3.0f, 0.1f, 1.2f), Mat("M_Desk_Top"), desk, false);
            Box("Desk_Leg_L", new Vector3(-1.36f, 0.36f, -0.45f),
                new Vector3(0.12f, 0.72f, 0.12f), Mat("M_Shelf_Wood"), desk, false);
            Box("Desk_Leg_R", new Vector3(1.36f, 0.36f, -0.45f),
                new Vector3(0.12f, 0.72f, 0.12f), Mat("M_Shelf_Wood"), desk, false);
            Box("Drawer_Unit", new Vector3(1.02f, 0.38f, 0.05f),
                new Vector3(0.72f, 0.72f, 1.0f), Mat("M_Wardrobe_Body"), desk, false);
            Box("Drawer_Handle", new Vector3(1.02f, 0.45f, -0.48f),
                new Vector3(0.3f, 0.05f, 0.04f), Mat("M_Toy_Blue"), desk, false);

            Box("Homework_Sheet", new Vector3(-0.5f, 0.81f, 0f),
                new Vector3(0.65f, 0.02f, 0.45f), Mat("M_Bed_Mattress"), desk, false);
            Cylinder("Pencil_Cup", new Vector3(0.25f, 0.95f, 0.25f),
                0.18f, 0.24f, Mat("M_Toy_Green"), desk, false);
            Cylinder("Pencil_Red", new Vector3(0.21f, 1.2f, 0.24f),
                0.025f, 0.34f, Mat("M_Toy_Red"), desk, false);
            Cylinder("Pencil_Yellow", new Vector3(0.29f, 1.19f, 0.27f),
                0.025f, 0.32f, Mat("M_Toy_Yellow"), desk, false);
            Box("Desk_Book_1", new Vector3(-1.05f, 0.86f, 0.2f),
                new Vector3(0.42f, 0.08f, 0.3f), Mat("M_Book_Red"), desk, false);
            Box("Desk_Book_2", new Vector3(-1.05f, 0.94f, 0.2f),
                new Vector3(0.38f, 0.08f, 0.28f), Mat("M_Book_Blue"), desk, false);

            BoxCollider collider = desk.gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.45f, 0f);
            collider.size = new Vector3(3.05f, 0.9f, 1.25f);

            InstantiateChair(root, "Chair_Study", new Vector3(5.6f, 0f, 6.7f), 0f);

            // Wall shelf above the desk, clear of the two back windows.
            Transform shelf = Group("Desk Wall Shelf", root);
            shelf.position = new Vector3(5.6f, 0f, 9.55f);
            Box("Board_Lower", new Vector3(0f, 2.35f, 0f),
                new Vector3(2.6f, 0.1f, 0.42f), Mat("M_Shelf_Wood"), shelf, false);
            Box("Board_Upper", new Vector3(0f, 3.15f, 0f),
                new Vector3(2.6f, 0.1f, 0.42f), Mat("M_Shelf_Wood"), shelf, false);
            Box("Book_A", new Vector3(-0.8f, 2.62f, 0f),
                new Vector3(0.16f, 0.45f, 0.28f), Mat("M_Book_Red"), shelf, false);
            Box("Book_B", new Vector3(-0.58f, 2.62f, 0f),
                new Vector3(0.18f, 0.45f, 0.28f), Mat("M_Book_Blue"), shelf, false);
            Box("Book_C", new Vector3(-0.34f, 2.62f, 0f),
                new Vector3(0.14f, 0.45f, 0.28f), Mat("M_Book_Green"), shelf, false);
            Sphere("Shelf_Ball", new Vector3(0.7f, 2.66f, 0f),
                0.34f, Mat("M_Toy_Yellow"), shelf, false);
        }

        private static void BuildWardrobe(Transform root)
        {
            Transform wardrobe = Group("Wardrobe", root);
            wardrobe.position = new Vector3(-8.62f, 0f, -5.0f);

            Box("Body", new Vector3(0f, 1.32f, 0f),
                new Vector3(1.16f, 2.64f, 3.2f), Mat("M_Wardrobe_Body"), wardrobe, false);
            Box("Door_Left", new Vector3(-0.62f, 1.35f, -0.78f),
                new Vector3(0.06f, 2.44f, 1.52f), Mat("M_Wardrobe_Door"), wardrobe, false);
            Box("Door_Right", new Vector3(-0.62f, 1.35f, 0.78f),
                new Vector3(0.06f, 2.44f, 1.52f), Mat("M_Wardrobe_Door"), wardrobe, false);
            Sphere("Handle_Left", new Vector3(-0.68f, 1.35f, -0.2f),
                0.13f, Mat("M_Toy_Yellow"), wardrobe, false);
            Sphere("Handle_Right", new Vector3(-0.68f, 1.35f, 0.2f),
                0.13f, Mat("M_Toy_Yellow"), wardrobe, false);
            Box("Top_Moulding", new Vector3(0f, 2.7f, 0f),
                new Vector3(1.3f, 0.1f, 3.35f), Mat("M_Shelf_Wood"), wardrobe, false);

            BoxCollider collider = wardrobe.gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 1.35f, 0f);
            collider.size = new Vector3(1.3f, 2.75f, 3.35f);
        }

        private static void BuildBookshelf(Transform root)
        {
            Transform shelf = Group("Bookshelf", root);
            shelf.position = new Vector3(8.4f, 0f, 9.35f);

            Box("Back", new Vector3(0f, 1.15f, 0.22f),
                new Vector3(2.1f, 2.3f, 0.08f), Mat("M_Shelf_Wood"), shelf, false);
            Box("Side_Left", new Vector3(-1.03f, 1.15f, 0f),
                new Vector3(0.1f, 2.3f, 0.5f), Mat("M_Shelf_Wood"), shelf, false);
            Box("Side_Right", new Vector3(1.03f, 1.15f, 0f),
                new Vector3(0.1f, 2.3f, 0.5f), Mat("M_Shelf_Wood"), shelf, false);
            float[] levels = { 0.1f, 0.65f, 1.2f, 1.75f, 2.3f };
            for (int i = 0; i < levels.Length; i++)
            {
                Box("Shelf_Board_" + (i + 1), new Vector3(0f, levels[i], 0f),
                    new Vector3(2.1f, 0.09f, 0.5f), Mat("M_Shelf_Wood"), shelf, false);
            }

            string[] bookMaterials = { "M_Book_Red", "M_Book_Blue", "M_Book_Green", "M_Book_Yellow" };
            float[] shelfBottoms = { 0.15f, 0.7f, 1.25f, 1.8f };
            for (int row = 0; row < shelfBottoms.Length; row++)
            {
                for (int column = 0; column < 6; column++)
                {
                    string material = bookMaterials[(row + column) % bookMaterials.Length];
                    float height = 0.32f + ((row + column) % 3) * 0.045f;
                    Box("Book_" + row + "_" + column,
                        new Vector3(-0.78f + column * 0.3f, shelfBottoms[row] + height * 0.5f, -0.02f),
                        new Vector3(0.2f, height, 0.32f), Mat(material), shelf, false);
                }
            }
            Sphere("Shelf_Teddy", new Vector3(0.55f, 2.52f, -0.02f),
                0.34f, Mat("M_Teddy_Light"), shelf, false);

            BoxCollider collider = shelf.gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 1.2f, 0f);
            collider.size = new Vector3(2.2f, 2.4f, 0.6f);
        }

        // -----------------------------------------------------------------
        // Prefabs and decorations
        // -----------------------------------------------------------------

        private static GameObject chairPrefab;
        private static GameObject picturePrefab;
        private static GameObject toyBlockPrefab;

        private static void EnsurePrefabs()
        {
            if (chairPrefab == null)
            {
                chairPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/PF_Chair.prefab");
                if (chairPrefab == null)
                {
                    GameObject source = new GameObject("PF_Chair");
                    Box("Seat", new Vector3(0f, 0.46f, 0f),
                        new Vector3(0.62f, 0.09f, 0.6f), Mat("M_Chair_Seat"), source.transform, false);
                    Box("Back", new Vector3(0f, 0.82f, 0.28f),
                        new Vector3(0.62f, 0.66f, 0.09f), Mat("M_Chair_Seat"), source.transform, false);
                    Box("Leg_FL", new Vector3(-0.24f, 0.22f, -0.22f),
                        new Vector3(0.08f, 0.44f, 0.08f), Mat("M_Table_Leg"), source.transform, false);
                    Box("Leg_FR", new Vector3(0.24f, 0.22f, -0.22f),
                        new Vector3(0.08f, 0.44f, 0.08f), Mat("M_Table_Leg"), source.transform, false);
                    Box("Leg_BL", new Vector3(-0.24f, 0.22f, 0.22f),
                        new Vector3(0.08f, 0.44f, 0.08f), Mat("M_Table_Leg"), source.transform, false);
                    Box("Leg_BR", new Vector3(0.24f, 0.22f, 0.22f),
                        new Vector3(0.08f, 0.44f, 0.08f), Mat("M_Table_Leg"), source.transform, false);
                    BoxCollider collider = source.AddComponent<BoxCollider>();
                    collider.center = new Vector3(0f, 0.55f, 0f);
                    collider.size = new Vector3(0.65f, 1.1f, 0.65f);

                    chairPrefab = PrefabUtility.SaveAsPrefabAsset(source, PrefabFolder + "/PF_Chair.prefab");
                    UnityEngine.Object.DestroyImmediate(source);
                }
            }

            if (picturePrefab == null)
            {
                picturePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/PF_WallPicture.prefab");
                if (picturePrefab == null)
                {
                    GameObject source = new GameObject("PF_WallPicture");
                    Box("Frame", Vector3.zero,
                        new Vector3(1.05f, 0.82f, 0.08f), Mat("M_Picture_Frame"), source.transform, false);
                    Box("Panel", new Vector3(0f, 0f, -0.05f),
                        new Vector3(0.87f, 0.64f, 0.03f), Mat("M_Picture_A"), source.transform, false);
                    picturePrefab = PrefabUtility.SaveAsPrefabAsset(source, PrefabFolder + "/PF_WallPicture.prefab");
                    UnityEngine.Object.DestroyImmediate(source);
                }
            }

            if (toyBlockPrefab == null)
            {
                toyBlockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/PF_MatlabToyBlock.prefab");
                string modelPath = ModelFolder + "/Issue010/Issue010_Box_Unity.obj";
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (toyBlockPrefab == null && model != null)
                {
                    GameObject source = new GameObject("PF_MatlabToyBlock");
                    GameObject block = (GameObject)PrefabUtility.InstantiatePrefab(model);
                    block.name = "MATLAB_Issue010_Box";
                    block.transform.SetParent(source.transform, false);
                    block.transform.localRotation = Quaternion.identity;
                    block.transform.localScale = Vector3.one * 0.12f;
                    // Imported local bounds are centre (-1, 3, 2), size (2, 5, 3).
                    // Centre X/Z and rest the bottom of the scaled block on y = 0.
                    block.transform.localPosition = new Vector3(0.12f, -0.16f, -0.24f);

                    toyBlockPrefab = PrefabUtility.SaveAsPrefabAsset(source,
                        PrefabFolder + "/PF_MatlabToyBlock.prefab");
                    UnityEngine.Object.DestroyImmediate(source);
                }
            }
        }

        private static void InstantiateChair(Transform parent, string name, Vector3 position, float yaw)
        {
            EnsurePrefabs();
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(chairPrefab, scene);
            instance.name = name;
            instance.transform.position = position;
            instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            instance.transform.SetParent(parent, true);
        }

        private static void BuildDecorations(Transform root)
        {
            EnsurePrefabs();

            // Layered circular rug under the dining table.
            Transform rug = Group("Play Rug", root);
            rug.position = new Vector3(0.8f, 0f, -1.5f);
            Cylinder("Rug_Outer", new Vector3(0f, 0.015f, 0f),
                5.4f, 0.03f, Mat("M_Rug_Coral"), rug, false);
            Cylinder("Rug_Middle", new Vector3(0f, 0.035f, 0f),
                4.5f, 0.03f, Mat("M_Rug_Cream"), rug, false);
            Cylinder("Rug_Inner", new Vector3(0f, 0.055f, 0f),
                3.4f, 0.03f, Mat("M_Rug_Mint"), rug, false);

            // Framed pictures: two on the back wall, two on the left wall.
            PlacePicture("Wall_Picture_Back_1", new Vector3(-7.0f, 3.35f, 9.62f), 0f, "M_Picture_A");
            PlacePicture("Wall_Picture_Back_2", new Vector3(-5.1f, 3.35f, 9.62f), 0f, "M_Picture_B");
            PlacePicture("Wall_Picture_Left_1", new Vector3(-9.62f, 3.15f, -6.2f), 90f, "M_Picture_C");
            PlacePicture("Wall_Picture_Left_2", new Vector3(-9.62f, 3.15f, -3.9f), 90f, "M_Picture_A");
            PlacePicture("Wall_Picture_Right", new Vector3(9.62f, 3.35f, 5.0f), 270f, "M_Picture_B");

            // Colourful diamond bunting across the back wall.
            Transform bunting = Group("Wall Bunting", root);
            string[] buntingColors =
            {
                "M_Toy_Red", "M_Toy_Yellow", "M_Toy_Blue", "M_Toy_Green",
                "M_Toy_Red", "M_Toy_Yellow", "M_Toy_Blue", "M_Toy_Green"
            };
            for (int i = 0; i < buntingColors.Length; i++)
            {
                GameObject diamond = Box("Pennant_" + (i + 1),
                    new Vector3(-7.4f + i * 1.9f, 4.35f, 9.6f),
                    new Vector3(0.48f, 0.48f, 0.05f), Mat(buntingColors[i]), bunting, false);
                diamond.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }

            // Potted plant by the right window.
            Transform plant = Group("Plant", root);
            plant.position = new Vector3(8.6f, 0f, -0.4f);
            Cylinder("Pot", new Vector3(0f, 0.32f, 0f),
                0.62f, 0.64f, Mat("M_Pot_Terracotta"), plant, false);
            Sphere("Leaves_L", new Vector3(-0.22f, 1.0f, 0f),
                0.52f, Mat("M_Plant_Green"), plant, false);
            Sphere("Leaves_R", new Vector3(0.22f, 1.05f, 0.06f),
                0.5f, Mat("M_Plant_Green"), plant, false);
            Sphere("Leaves_Top", new Vector3(0f, 1.42f, -0.03f),
                0.55f, Mat("M_Plant_Green"), plant, false);

            // Soft teddy on the bed.
            Transform teddy = Group("Teddy", root);
            teddy.position = new Vector3(-7.8f, 1.0f, 5.6f);
            teddy.rotation = Quaternion.Euler(0f, 25f, 0f);
            Sphere("Body", new Vector3(0f, 0.22f, 0f),
                0.5f, Mat("M_Teddy_Brown"), teddy, false);
            Sphere("Head", new Vector3(0f, 0.68f, 0.06f),
                0.38f, Mat("M_Teddy_Brown"), teddy, false);
            Sphere("Ear_L", new Vector3(-0.25f, 0.92f, 0f),
                0.18f, Mat("M_Teddy_Brown"), teddy, false);
            Sphere("Ear_R", new Vector3(0.25f, 0.92f, 0f),
                0.18f, Mat("M_Teddy_Brown"), teddy, false);
            Sphere("Muzzle", new Vector3(0f, 0.6f, -0.3f),
                0.18f, Mat("M_Teddy_Light"), teddy, false);

            // Floor cushions by the shelf.
            Cylinder("Cushion_Blue", new Vector3(6.6f, 0.13f, 3.7f),
                0.8f, 0.26f, Mat("M_Toy_Blue"), root, false);
            Cylinder("Cushion_Green", new Vector3(7.5f, 0.13f, 4.2f),
                0.8f, 0.26f, Mat("M_Toy_Green"), root, false);
        }

        private static void PlacePicture(string name, Vector3 position, float yaw, string panelMaterial)
        {
            EnsurePrefabs();
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(picturePrefab, scene);
            instance.name = name;
            instance.transform.position = position;
            instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            Transform panel = instance.transform.Find("Panel");
            if (panel != null)
            {
                MeshRenderer renderer = panel.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = Mat(panelMaterial);
                }
            }
        }

        // -----------------------------------------------------------------
        // MATLAB-generated assets
        // -----------------------------------------------------------------

        private static readonly string[] MatlabCopies =
        {
            "Issue010/Issue010_Box_Unity.obj|Issue010/Issue010_Box_Unity.obj|true",
            "Issue010/Issue010_Box_Unity.mtl|Issue010/Issue010_Box_Unity.mtl|false",
            "cube.obj|Issue007/MATLAB_Cube.obj|false",
            "sphere.obj|Issue007/MATLAB_Sphere.obj|false",
            "cylinder.obj|Issue007/MATLAB_Cylinder.obj|false"
        };

        private static void ImportMatlabAssets()
        {
            string sourceRoot = Path.Combine(RepositoryRoot(), "Shared_Data", "Meshes");
            if (!Directory.Exists(sourceRoot))
            {
                throw new DirectoryNotFoundException("MATLAB mesh export folder not found: " + sourceRoot);
            }

            foreach (string entry in MatlabCopies)
            {
                string[] parts = entry.Split('|');
                string source = Path.Combine(sourceRoot, parts[0].Replace('/', Path.DirectorySeparatorChar));
                string relative = parts[1].Replace('/', Path.DirectorySeparatorChar);
                string destination = Path.Combine(
                    Path.Combine(Application.dataPath, "Models", "MATLAB"), relative);

                if (!File.Exists(source))
                {
                    throw new FileNotFoundException("Required MATLAB export is missing: " + source);
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.Copy(source, destination, true);

                string assetPath = "Assets/Models/MATLAB/" + parts[1].Replace('\\', '/');
                AssetDatabase.ImportAsset(assetPath,
                    ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

                if (!assetPath.EndsWith(".obj", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                if (importer == null)
                {
                    throw new InvalidOperationException("Unity did not create a ModelImporter for " + assetPath);
                }

                importer.globalScale = 1f;
                importer.importCameras = false;
                importer.importLights = false;
                importer.materialImportMode = parts[2] == "true"
                    ? ModelImporterMaterialImportMode.ImportStandard
                    : ModelImporterMaterialImportMode.None;
                importer.isReadable = false;
                importer.meshCompression = ModelImporterMeshCompression.Off;
                importer.animationType = ModelImporterAnimationType.None;
                importer.SaveAndReimport();

                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                MeshFilter filter = model != null ? model.GetComponentInChildren<MeshFilter>() : null;
                if (filter == null || filter.sharedMesh == null || filter.sharedMesh.vertexCount == 0)
                {
                    throw new InvalidOperationException("Imported MATLAB mesh has no geometry: " + assetPath);
                }
                Debug.Log("[Issue012] Imported " + assetPath + " (" +
                          filter.sharedMesh.vertexCount + " vertices)");
            }
        }

        private static void PlaceImportedToys(Transform root)
        {
            EnsurePrefabs();
            Transform toys = Group("MATLAB Toys", root);

            string[] blockColors =
            {
                "M_Toy_Red", "M_Toy_Yellow", "M_Toy_Blue", "M_Toy_Green"
            };
            Vector3[] blockPositions =
            {
                new Vector3(3.4f, 0f, -3.25f),
                new Vector3(3.85f, 0f, -3.35f),
                new Vector3(3.62f, 0f, -3.9f),
                new Vector3(-4.6f, 0f, 3.3f)
            };
            for (int i = 0; i < blockPositions.Length; i++)
            {
                GameObject block = (GameObject)PrefabUtility.InstantiatePrefab(toyBlockPrefab, scene);
                block.name = "MATLAB_ToyBlock_" + (i + 1);
                block.transform.position = blockPositions[i];
                block.transform.rotation = Quaternion.Euler(0f, i * 37f, 0f);
                block.transform.SetParent(toys, true);

                MeshRenderer[] renderers = block.GetComponentsInChildren<MeshRenderer>();
                for (int r = 0; r < renderers.Length; r++)
                {
                    renderers[r].sharedMaterial = Mat(blockColors[i % blockColors.Length]);
                }
            }

            string spherePath = ModelFolder + "/Issue007/MATLAB_Sphere.obj";
            string cylinderPath = ModelFolder + "/Issue007/MATLAB_Cylinder.obj";
            PlaceMatlabModel(spherePath, "MATLAB_Ball", new Vector3(-4.4f, 0f, 2.2f),
                Quaternion.Euler(-90f, 0f, 0f), 0.3f, "M_Toy_Yellow", toys);
            PlaceMatlabModel(cylinderPath, "MATLAB_Drum", new Vector3(-5.2f, 0f, 3.6f),
                Quaternion.Euler(-90f, 0f, 0f), 0.24f, "M_Toy_Red", toys);
            PlaceMatlabModel(ModelFolder + "/Issue010/Issue010_Box_Unity.obj",
                "MATLAB_DisplayBlock", new Vector3(-4.0f, 0f, -6.8f),
                Quaternion.identity, 0.32f, "M_Toy_Blue", toys);
        }

        private static void PlaceMatlabModel(string assetPath, string name, Vector3 floorPosition,
            Quaternion rotation, float scale, string materialName, Transform parent)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (model == null)
            {
                Debug.LogWarning("[Issue012] MATLAB model not available: " + assetPath);
                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model, scene);
            instance.name = name;
            instance.transform.rotation = rotation;
            instance.transform.localScale = Vector3.one * scale;

            Bounds bounds = RendererBounds(instance);
            Vector3 centre = bounds.center;
            instance.transform.position += new Vector3(
                floorPosition.x - centre.x,
                floorPosition.y - bounds.min.y,
                floorPosition.z - centre.z);
            instance.transform.SetParent(parent, true);

            MeshRenderer[] renderers = instance.GetComponentsInChildren<MeshRenderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].sharedMaterial = Mat(materialName);
            }
        }

        private static Bounds RendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(root.transform.position, Vector3.one);
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            return bounds;
        }

        // -----------------------------------------------------------------
        // Lighting
        // -----------------------------------------------------------------

        private static void ConfigureLighting()
        {
            Camera camera = FindComponent<Camera>("Main Camera");
            if (camera != null)
            {
                camera.transform.position = new Vector3(6.6f, 4.8f, -7.4f);
                camera.transform.rotation = Quaternion.LookRotation(
                    new Vector3(-1.0f, 1.35f, 2.6f) - camera.transform.position, Vector3.up);
                camera.fieldOfView = 62f;
                camera.backgroundColor = new Color(0.72f, 0.86f, 0.96f, 1f);
            }

            Light sun = FindComponent<Light>("Directional Light");
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(42f, -125f, 0f);
                sun.intensity = 1.35f;
                sun.color = new Color(1f, 0.96f, 0.86f);
                sun.shadows = LightShadows.Soft;
            }

            GameObject ceilingLightObject = GameObject.Find("Light_Ceiling");
            if (ceilingLightObject == null)
            {
                ceilingLightObject = new GameObject("Light_Ceiling");
            }
            ceilingLightObject.transform.position = new Vector3(0f, 4.55f, 0f);
            Light ceilingLight = ceilingLightObject.GetComponent<Light>();
            if (ceilingLight == null)
            {
                ceilingLight = ceilingLightObject.AddComponent<Light>();
            }
            ceilingLight.type = LightType.Point;
            ceilingLight.color = new Color(1f, 0.94f, 0.82f);
            ceilingLight.intensity = 1.7f;
            ceilingLight.range = 17f;
            ceilingLight.shadows = LightShadows.Soft;

            GameObject fixture = GameObject.Find("Ceiling_Light_Fixture");
            if (fixture == null)
            {
                fixture = new GameObject("Ceiling_Light_Fixture");
            }
            fixture.transform.SetParent(ceilingLightObject.transform, false);
            fixture.transform.localPosition = Vector3.zero;
            Cylinder("Fixture_Base", Vector3.zero, 0.7f, 0.12f,
                Mat("M_Window_Frame"), fixture.transform, false);
            Sphere("Fixture_Glow", new Vector3(0f, -0.16f, 0f), 0.5f,
                Mat("M_Lamp_Shade"), fixture.transform, false);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.58f, 0.6f, 0.64f, 1f);
            RenderSettings.ambientIntensity = 1f;
        }

        // -----------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------

        private static string RepositoryRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".."));
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }
            int slash = assetPath.LastIndexOf('/');
            string parent = assetPath.Substring(0, slash);
            string child = assetPath.Substring(slash + 1);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }
            AssetDatabase.CreateFolder(parent, child);
        }

        private static Transform Group(string name, Transform parent)
        {
            GameObject group = new GameObject(name);
            if (parent != null)
            {
                group.transform.SetParent(parent, false);
            }
            return group.transform;
        }

        private static GameObject Box(string name, Vector3 localPosition, Vector3 size,
            Material material, Transform parent, bool collider)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            RemoveCollider(go);
            if (collider)
            {
                go.AddComponent<BoxCollider>();
            }
            return go;
        }

        private static GameObject Cylinder(string name, Vector3 localPosition, float diameter,
            float height, Material material, Transform parent, bool collider)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            // Unity's cylinder primitive is 1 unit in diameter and 2 units tall.
            go.transform.localScale = new Vector3(diameter, height * 0.5f, diameter);
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            RemoveCollider(go);
            if (collider)
            {
                go.AddComponent<CapsuleCollider>();
            }
            return go;
        }

        private static GameObject Sphere(string name, Vector3 localPosition, float diameter,
            Material material, Transform parent, bool collider)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = Vector3.one * diameter;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            RemoveCollider(go);
            if (collider)
            {
                go.AddComponent<SphereCollider>();
            }
            return go;
        }

        private static void RemoveCollider(GameObject go)
        {
            Collider existing = go.GetComponent<Collider>();
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }
        }

        private static void SetMaterial(GameObject go, string materialName)
        {
            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = Mat(materialName);
            }
        }

        private static T FindComponent<T>(string objectName) where T : Component
        {
            GameObject go = GameObject.Find(objectName);
            return go != null ? go.GetComponent<T>() : null;
        }

        private static int CountSceneObjects()
        {
            return scene.GetRootGameObjects().Length == 0
                ? 0
                : CountRecursive(scene.GetRootGameObjects());
        }

        private static int CountRecursive(GameObject[] objects)
        {
            int count = 0;
            foreach (GameObject go in objects)
            {
                count++;
                count += go.transform.childCount;
            }
            return count;
        }

        // -----------------------------------------------------------------
        // Materials
        // -----------------------------------------------------------------

        private static void CreateMaterials()
        {
            Mat("M_Floor_Wood", "#D9A566", 0.22f);
            Mat("M_Wall_Cream", "#FFF4DF", 0.05f);
            Mat("M_Wall_Sky", "#DDEEFF", 0.05f);
            Mat("M_Ceiling_White", "#FFFDF7", 0.05f);
            Mat("M_Skirt_White", "#FFFFFF", 0.18f);

            Mat("M_Door_Wood", "#C98B5E", 0.28f);
            Mat("M_Door_Frame", "#FFFFFF", 0.22f);
            Mat("M_Window_Frame", "#FAFAFA", 0.35f);
            Mat("M_Window_Glass", "#BFE6FF", 0.85f, 0f, true);
            Mat("M_Curtain_Pink", "#F7AFC0", 0.18f);
            Mat("M_Curtain_Blue", "#A9D6F5", 0.18f);

            Mat("M_Bed_Frame", "#B47A4E", 0.25f);
            Mat("M_Bed_Mattress", "#FFFDF8", 0.12f);
            Mat("M_Bed_Blanket", "#A8E0C8", 0.3f);
            Mat("M_Bed_Pillow", "#FFFFFF", 0.15f);
            Mat("M_Table_Top", "#E0A96D", 0.3f);
            Mat("M_Table_Leg", "#A66E3F", 0.2f);
            Mat("M_Chair_Seat", "#F2C14E", 0.25f);
            Mat("M_Desk_Top", "#D9B382", 0.3f);
            Mat("M_Wardrobe_Body", "#E8C9A0", 0.22f);
            Mat("M_Wardrobe_Door", "#F3DCC0", 0.25f);
            Mat("M_Shelf_Wood", "#C89B6A", 0.2f);

            Mat("M_Picture_Frame", "#8B5E3C", 0.28f);
            Mat("M_Picture_A", "#FFD166", 0.25f);
            Mat("M_Picture_B", "#8AD7C1", 0.25f);
            Mat("M_Picture_C", "#F4978E", 0.25f);

            Mat("M_Rug_Coral", "#F4A6A6", 0.4f);
            Mat("M_Rug_Cream", "#FFF3E0", 0.4f);
            Mat("M_Rug_Mint", "#BFE8D8", 0.4f);

            Mat("M_Toy_Red", "#EF476F", 0.35f);
            Mat("M_Toy_Yellow", "#FFD166", 0.35f);
            Mat("M_Toy_Blue", "#4EA8DE", 0.35f);
            Mat("M_Toy_Green", "#06D6A0", 0.35f);
            Mat("M_Book_Red", "#E76F51", 0.2f);
            Mat("M_Book_Blue", "#457B9D", 0.2f);
            Mat("M_Book_Green", "#2A9D8F", 0.2f);
            Mat("M_Book_Yellow", "#E9C46A", 0.2f);

            Mat("M_Lamp_Shade", "#FFF1B8", 0.3f, 0f, false, new Color(0.7f, 0.6f, 0.3f));
            Mat("M_Plant_Green", "#4CAF50", 0.2f);
            Mat("M_Pot_Terracotta", "#C86B4A", 0.25f);
            Mat("M_Teddy_Brown", "#B9855A", 0.4f);
            Mat("M_Teddy_Light", "#E7C6A0", 0.4f);
        }

        private static Material Mat(string name)
        {
            Material material;
            if (Materials.TryGetValue(name, out material) && material != null)
            {
                return material;
            }
            material = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/" + name + ".mat");
            if (material == null)
            {
                throw new InvalidOperationException("Material was not created: " + name);
            }
            Materials[name] = material;
            return material;
        }

        private static Material Mat(string name, string hex, float smoothness,
            float metallic = 0f, bool transparent = false, Color? emission = null)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }
            if (shader == null)
            {
                throw new InvalidOperationException("No compatible Lit shader found.");
            }

            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            Color color;
            if (!ColorUtility.TryParseHtmlString(hex, out color))
            {
                throw new ArgumentException("Invalid colour " + hex + " for " + name);
            }
            if (transparent)
            {
                color.a = 0.38f;
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }
            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            material.SetFloat("_Surface", transparent ? 1f : 0f);
            if (transparent)
            {
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)RenderQueue.Transparent;
            }
            else
            {
                material.SetInt("_SrcBlend", (int)BlendMode.One);
                material.SetInt("_DstBlend", (int)BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = -1;
            }

            if (emission.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                if (material.HasProperty("_EmissionColor"))
                {
                    material.SetColor("_EmissionColor", emission.Value);
                }
            }
            else
            {
                material.DisableKeyword("_EMISSION");
            }

            EditorUtility.SetDirty(material);
            Materials[name] = material;
            return material;
        }
    }
}
