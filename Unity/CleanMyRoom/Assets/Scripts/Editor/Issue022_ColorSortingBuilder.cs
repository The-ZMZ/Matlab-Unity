using System;
using System.Collections.Generic;
using System.IO;
using CleanMyRoom.ColorSorting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace CleanMyRoom.Editor.Issue022
{
    /// <summary>
    /// Issue #022 - idempotent editor builder for Assets/Scenes/ColorSorting.unity.
    ///
    /// Creates the child-friendly sorting table, six labelled baskets, twelve
    /// draggable toys, instructions, pointer cursor, feedback labels and the
    /// controller that talks to MATLAB. Re-running Build replaces only the
    /// activity root, so the pre-existing Main Camera and Directional Light
    /// keep their identity from Issue #011.
    /// </summary>
    public static class ColorSortingBuilder
    {
        private const string ScenePath = "Assets/Scenes/ColorSorting.unity";
        private const string MaterialFolder = "Assets/Materials/ColorSorting";
        private const string PrefabFolder = "Assets/Prefabs/ColorSorting";
        private const string ScreenshotFolder = "Deliverables/Screenshots";
        private const string ActivityRootName = "Color Sorting Activity";

        private const float BasketY = 1.05f;
        private const float BasketZ = 2.75f;
        private const float RestY = 1.42f;
        private const float DragY = 1.85f;

        private static readonly string[] NeutralMaterials =
        {
            "M_ColorSorting_Floor", "M_ColorSorting_Wall", "M_ColorSorting_WallAccent",
            "M_ColorSorting_Board", "M_ColorSorting_Basket", "M_ColorSorting_BasketRim",
            "M_ColorSorting_Sign", "M_ColorSorting_Cursor", "M_ColorSorting_Correct",
            "M_ColorSorting_Error"
        };

        private static Scene scene;

        // -----------------------------------------------------------------
        // Entry points
        // -----------------------------------------------------------------

        [MenuItem("Clean My Room/Issue 022/Build Color Sorting Scene")]
        public static void Build()
        {
            try
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                EnsureFolder("Assets/Materials");
                EnsureFolder(MaterialFolder);
                EnsureFolder("Assets/Prefabs");
                EnsureFolder(PrefabFolder);

                CreateMaterials();
                GameObject basketPrefab = CreateBasketPrefab();
                GameObject toyPrefab = CreateToyPrefab();
                GameObject cursorPrefab = CreateCursorPrefab();

                RemovePreviousActivity();
                Transform root = Group(ActivityRootName, null);

                BuildRoom(root);
                BuildTable(root);
                BuildBaskets(root, basketPrefab);
                BuildToys(root, toyPrefab);
                BuildSignage(root);
                BuildFeedback(root);
                BuildPointer(cursorPrefab, root);
                BuildController(root);
                ConfigurePresentation();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                Debug.Log("[Issue022] Color Sorting built: 6 baskets, 12 toys, scene saved to " +
                          ScenePath + " (" + CountSceneObjects() + " objects)");
            }
            catch (Exception exception)
            {
                Debug.LogError("[Issue022] Color Sorting build FAILED: " + exception);
                throw;
            }
        }

        [MenuItem("Clean My Room/Issue 022/Capture Color Sorting Screenshots")]
        public static void CaptureScreenshots()
        {
            string outputFolder = Path.Combine(
                RepositoryRoot(), ScreenshotFolder.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(outputFolder);

            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Camera camera = FindComponent<Camera>("Main Camera");
            ColorSortingController controller = FindComponent<ColorSortingController>("Activity Controller");
            if (camera == null || controller == null)
            {
                Debug.LogError("[Issue022] Scene is incomplete; run the builder before capturing screenshots.");
                return;
            }

            Vector3 savedPosition = camera.transform.position;
            Quaternion savedRotation = camera.transform.rotation;
            float savedFov = camera.fieldOfView;

            try
            {
                WarmUpRender(camera);

                RestoreSceneState(controller);
                RenderToPng(camera, Path.Combine(outputFolder,
                        "Issue022_ColorSorting_Overview.png"), 1280, 720,
                    new Vector3(0f, 9.5f, -10.5f), new Vector3(0f, 1.2f, 0.8f), 50f);

                RestoreSceneState(controller);
                StageInteraction(controller);
                RenderToPng(camera, Path.Combine(outputFolder,
                        "Issue022_ColorSorting_Interaction.png"), 1280, 720,
                    new Vector3(0f, 9.5f, -10.5f), new Vector3(0f, 1.2f, 0.8f), 50f);

                RestoreSceneState(controller);
                StageCompleted(controller);
                RenderToPng(camera, Path.Combine(outputFolder,
                        "Issue022_ColorSorting_Completed.png"), 1280, 720,
                    new Vector3(0f, 9.5f, -10.5f), new Vector3(0f, 1.2f, 0.8f), 50f);

                RestoreSceneState(controller);
                Debug.Log("[Issue022] Screenshots written to " + outputFolder);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Issue022] Screenshot capture failed: " + exception);
            }
            finally
            {
                camera.transform.position = savedPosition;
                camera.transform.rotation = savedRotation;
                camera.fieldOfView = savedFov;
            }
        }

        // -----------------------------------------------------------------
        // Assets
        // -----------------------------------------------------------------

        private static void CreateMaterials()
        {
            CreateMaterial("M_ColorSorting_Floor", new Color(0.86f, 0.92f, 0.86f), 0.25f);
            CreateMaterial("M_ColorSorting_Wall", new Color(0.98f, 0.96f, 0.90f), 0.1f);
            CreateMaterial("M_ColorSorting_WallAccent", new Color(0.78f, 0.89f, 0.98f), 0.1f);
            CreateMaterial("M_ColorSorting_Board", new Color(0.94f, 0.83f, 0.66f), 0.28f);
            CreateMaterial("M_ColorSorting_Basket", new Color(0.98f, 0.97f, 0.94f), 0.22f);
            CreateMaterial("M_ColorSorting_BasketRim", new Color(0.96f, 0.96f, 0.96f), 0.35f);
            CreateMaterial("M_ColorSorting_Sign", new Color(1f, 1f, 1f), 0.15f);
            CreateMaterial("M_ColorSorting_Cursor", new Color(1f, 0.98f, 0.82f), 0.4f);
            CreateMaterial("M_ColorSorting_Correct", new Color(0.30f, 0.88f, 0.42f), 0.2f);
            CreateMaterial("M_ColorSorting_Error", new Color(0.95f, 0.38f, 0.34f), 0.2f);

            IReadOnlyList<ColorSortingCatalog.BasketInfo> baskets = ColorSortingCatalog.AllBaskets;
            for (int i = 0; i < baskets.Count; i++)
            {
                CreateMaterial("M_ColorSorting_" + BasketMaterialSuffix(baskets[i].Id),
                    baskets[i].Color, 0.32f);
            }
        }

        private static void CreateMaterial(string name, Color color, float smoothness)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            material.SetColor("_Color", color);
            material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
        }

        private static Material Mat(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/" + name + ".mat");
        }

        private static string BasketMaterialSuffix(string basketId)
        {
            // basket_red -> Red
            string name = basketId.Substring("basket_".Length);
            return char.ToUpperInvariant(name[0]) + name.Substring(1);
        }

        private static GameObject CreateBasketPrefab()
        {
            string path = PrefabFolder + "/PF_ColorBasket.prefab";
            GameObject root = new GameObject("PF_ColorBasket_Template");
            try
            {
                BoxCollider collider = root.AddComponent<BoxCollider>();
                collider.center = new Vector3(0f, 0.55f, 0f);
                collider.size = new Vector3(1.55f, 1.1f, 1.35f);

                Material body = Mat("M_ColorSorting_Basket");
                Material rim = Mat("M_ColorSorting_BasketRim");
                Material sign = Mat("M_ColorSorting_Sign");

                Box("Basket_Base", new Vector3(0f, 0.06f, 0f),
                    new Vector3(1.5f, 0.12f, 1.3f), body, root.transform);
                Box("Basket_Wall_Back", new Vector3(0f, 0.55f, 0.62f),
                    new Vector3(1.5f, 1f, 0.08f), body, root.transform);
                Box("Basket_Wall_Front", new Vector3(0f, 0.55f, -0.62f),
                    new Vector3(1.5f, 1f, 0.08f), body, root.transform);
                Box("Basket_Wall_Left", new Vector3(-0.71f, 0.55f, 0f),
                    new Vector3(0.08f, 1f, 1.2f), body, root.transform);
                Box("Basket_Wall_Right", new Vector3(0.71f, 0.55f, 0f),
                    new Vector3(0.08f, 1f, 1.2f), body, root.transform);
                GameObject rimFront = Box("Basket_Rim", new Vector3(0f, 1.02f, -0.62f),
                    new Vector3(1.5f, 0.12f, 0.1f), rim, root.transform);
                Box("Basket_Label_Back", new Vector3(0f, 0.55f, -0.68f),
                    new Vector3(1.34f, 0.5f, 0.04f), sign, root.transform);
                Text("Basket_Label", "LABEL", new Vector3(0f, 0.57f, -0.72f),
                    Quaternion.Euler(0f, 180f, 0f), 64, 0.3f,
                    new Color(0.16f, 0.19f, 0.28f), root.transform);

                ColorBasket basket = root.AddComponent<ColorBasket>();
                basket.Configure("basket_red", new[] { rimFront.GetComponent<Renderer>() });

                GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null)
                {
                    AssetDatabase.DeleteAsset(path);
                }
                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateToyPrefab()
        {
            string path = PrefabFolder + "/PF_SortableToy.prefab";
            GameObject root = new GameObject("PF_SortableToy_Template");
            try
            {
                SphereCollider collider = root.AddComponent<SphereCollider>();
                collider.radius = 0.46f;

                Material placeholder = Mat("M_ColorSorting_Red");
                GameObject sphere = Sphere("Toy_Sphere", Vector3.zero, 0.86f,
                    placeholder, root.transform);
                GameObject cube = Box("Toy_Cube", Vector3.zero, new Vector3(0.76f, 0.76f, 0.76f),
                    placeholder, root.transform);
                cube.SetActive(false);
                Text("Toy_Label", "Toy", new Vector3(0f, -0.65f, 0f),
                    Quaternion.Euler(0f, 180f, 0f), 32, 0.1f,
                    new Color(0.16f, 0.19f, 0.28f), root.transform);

                SortableObject sortable = root.AddComponent<SortableObject>();
                sortable.Configure("object_01", "Toy");

                GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null)
                {
                    AssetDatabase.DeleteAsset(path);
                }
                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateCursorPrefab()
        {
            string path = PrefabFolder + "/PF_ActivityPointer.prefab";
            GameObject root = new GameObject("PF_ActivityPointer_Template");
            try
            {
                Material cursorMaterial = Mat("M_ColorSorting_Cursor");
                GameObject disc = Cylinder("Pointer_Disc", Vector3.zero, 0.55f, 0.06f,
                    cursorMaterial, root.transform);
                // Unity has no cone primitive; a rotated cube gives a clear
                // diamond-shaped pointer tip without importing a model.
                GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tip.name = "Pointer_Tip";
                tip.transform.SetParent(root.transform, false);
                tip.transform.localPosition = new Vector3(0f, -0.3f, 0f);
                tip.transform.localRotation = Quaternion.Euler(0f, 45f, 45f);
                tip.transform.localScale = new Vector3(0.24f, 0.24f, 0.24f);
                tip.GetComponent<MeshRenderer>().sharedMaterial = cursorMaterial;
                RemoveCollider(tip);

                ActivityPointerCursor cursor = root.AddComponent<ActivityPointerCursor>();
                cursor.Configure(new[]
                {
                    disc.GetComponent<Renderer>(), tip.GetComponent<Renderer>()
                });

                GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null)
                {
                    AssetDatabase.DeleteAsset(path);
                }
                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        // -----------------------------------------------------------------
        // Scene content
        // -----------------------------------------------------------------

        private static void RemovePreviousActivity()
        {
            GameObject previous = GameObject.Find(ActivityRootName);
            if (previous != null)
            {
                UnityEngine.Object.DestroyImmediate(previous);
            }
            // Older builder runs may have left stray roots with these names.
            string[] legacyNames = { "Activity Feedback", "Activity Pointer", "Activity Controller" };
            for (int i = 0; i < legacyNames.Length; i++)
            {
                GameObject stray = GameObject.Find(legacyNames[i]);
                if (stray != null && stray.transform.parent == null)
                {
                    UnityEngine.Object.DestroyImmediate(stray);
                }
            }
        }

        private static void BuildRoom(Transform root)
        {
            Transform room = Group("Activity Room", root);
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Sorting_Floor";
            floor.transform.SetParent(room, false);
            floor.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            floor.transform.localScale = new Vector3(22f, 0.2f, 18f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = Mat("M_ColorSorting_Floor");
            RemoveCollider(floor);

            Box("Back_Wall", new Vector3(0f, 3f, 6.9f), new Vector3(22f, 6f, 0.2f),
                Mat("M_ColorSorting_WallAccent"), room);
            Box("Left_Wall", new Vector3(-10.9f, 3f, 0f), new Vector3(0.2f, 6f, 18f),
                Mat("M_ColorSorting_Wall"), room);
            Box("Right_Wall", new Vector3(10.9f, 3f, 0f), new Vector3(0.2f, 6f, 18f),
                Mat("M_ColorSorting_Wall"), room);

            // Friendly bunting across the back wall.
            Transform bunting = Group("Bunting", room);
            for (int i = 0; i < 12; i++)
            {
                GameObject flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
                flag.name = "Pennant_" + (i + 1).ToString("D2");
                flag.transform.SetParent(bunting, false);
                flag.transform.localPosition = new Vector3(-5.5f + i, 4.9f - (i % 2) * 0.28f, 6.75f);
                flag.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                flag.transform.localScale = new Vector3(0.42f, 0.42f, 0.04f);
                flag.GetComponent<MeshRenderer>().sharedMaterial =
                    Mat("M_ColorSorting_" + BasketMaterialSuffix(
                        ColorSortingCatalog.AllBaskets[i % ColorSortingCatalog.BasketCount].Id));
                RemoveCollider(flag);
            }
        }

        private static void BuildTable(Transform root)
        {
            Transform table = Group("Sorting Table", root);
            Box("Table_Top", new Vector3(0f, 0.5f, 0f), new Vector3(14f, 1f, 9.6f),
                Mat("M_ColorSorting_Board"), table);
            Box("Table_Trim_Front", new Vector3(0f, 0.48f, -4.86f), new Vector3(14.2f, 0.22f, 0.16f),
                Mat("M_ColorSorting_BasketRim"), table);
            Box("Table_Trim_Back", new Vector3(0f, 0.48f, 4.86f), new Vector3(14.2f, 0.22f, 0.16f),
                Mat("M_ColorSorting_BasketRim"), table);
            Box("Table_Leg_Left", new Vector3(-6.2f, -0.5f, -3.8f), new Vector3(0.45f, 1f, 0.45f),
                Mat("M_ColorSorting_Basket"), table);
            Box("Table_Leg_Right", new Vector3(6.2f, -0.5f, -3.8f), new Vector3(0.45f, 1f, 0.45f),
                Mat("M_ColorSorting_Basket"), table);
        }

        private static void BuildBaskets(Transform root, GameObject prefab)
        {
            Transform group = Group("Color Baskets", root);
            IReadOnlyList<ColorSortingCatalog.BasketInfo> infos = ColorSortingCatalog.AllBaskets;
            for (int i = 0; i < infos.Count; i++)
            {
                ColorSortingCatalog.BasketInfo info = infos[i];
                GameObject instance = InstantiateUnder(prefab, group);
                instance.name = "Basket_" + info.Label;
                instance.transform.position = new Vector3(
                    -5.25f + i * 2.1f, BasketY, BasketZ);

                Material colorMaterial = Mat("M_ColorSorting_" + BasketMaterialSuffix(info.Id));
                SetChildMaterial(instance, "Basket_Rim", colorMaterial);
                SetChildMaterial(instance, "Basket_Label_Back", colorMaterial);

                TextMesh label = FindInChildren<TextMesh>(instance.transform, "Basket_Label");
                if (label != null)
                {
                    label.text = info.Label;
                    label.color = new Color(0.12f, 0.15f, 0.24f);
                }

                ColorBasket basket = instance.GetComponent<ColorBasket>();
                basket.Configure(info.Id, new[]
                {
                    FindInChildren<Renderer>(instance.transform, "Basket_Rim")
                });
                RecordOverride(instance);
            }
        }

        private static void BuildToys(Transform root, GameObject prefab)
        {
            Transform group = Group("Sortable Toys", root);
            IReadOnlyList<ColorSortingCatalog.ObjectInfo> infos = ColorSortingCatalog.AllObjects;
            for (int i = 0; i < infos.Count; i++)
            {
                ColorSortingCatalog.ObjectInfo info = infos[i];
                GameObject instance = InstantiateUnder(prefab, group);
                instance.name = "Toy_" + info.Id + "_" + info.Label.Replace(" ", "_");
                int column = i % 6;
                int row = i / 6;
                instance.transform.position = new Vector3(
                    -4.25f + column * 1.7f, RestY, -2.6f + row * 1.55f);

                bool isCube = string.Equals(info.Shape, "cube", StringComparison.Ordinal);
                GameObject sphere = FindChild(instance.transform, "Toy_Sphere");
                GameObject cube = FindChild(instance.transform, "Toy_Cube");
                if (sphere != null)
                {
                    sphere.SetActive(!isCube);
                }
                if (cube != null)
                {
                    cube.SetActive(isCube);
                }
                if (isCube)
                {
                    Collider collider = instance.GetComponent<Collider>();
                    if (collider != null)
                    {
                        UnityEngine.Object.DestroyImmediate(collider);
                    }
                    BoxCollider box = instance.AddComponent<BoxCollider>();
                    box.size = new Vector3(0.82f, 0.82f, 0.82f);
                }

                Material colorMaterial = Mat(
                    "M_ColorSorting_" + BasketMaterialSuffix(info.ColorId));
                SetChildMaterial(instance, isCube ? "Toy_Cube" : "Toy_Sphere", colorMaterial);

                TextMesh label = FindInChildren<TextMesh>(instance.transform, "Toy_Label");
                if (label != null)
                {
                    label.text = info.Label;
                }

                SortableObject sortable = instance.GetComponent<SortableObject>();
                sortable.Configure(info.Id, info.Label);
                RecordOverride(instance);
            }
        }

        private static void BuildSignage(Transform root)
        {
            Transform signs = Group("Activity Signage", root);
            Box("Instruction_Board", new Vector3(0f, 3.45f, 6.7f), new Vector3(14f, 1.65f, 0.14f),
                Mat("M_ColorSorting_Sign"), signs);
            Text("Instruction_Label",
                "Sort the toys into the basket of the same colour!\n" +
                "Mouse: click, drag, release    |    Keyboard: arrows + Space    |    Esc: Bedroom",
                new Vector3(0f, 3.45f, 6.58f), Quaternion.Euler(0f, 180f, 0f),
                48, 0.19f, new Color(0.14f, 0.18f, 0.30f), signs);

            Box("Progress_Back", new Vector3(-4.5f, 2.3f, -4.5f), new Vector3(4.2f, 0.62f, 0.12f),
                Mat("M_ColorSorting_Sign"), signs);
            Box("Connection_Back", new Vector3(4.5f, 2.3f, -4.5f), new Vector3(5.4f, 0.62f, 0.12f),
                Mat("M_ColorSorting_Sign"), signs);
        }

        private static void BuildFeedback(Transform root)
        {
            GameObject host = new GameObject("Activity Feedback");
            host.transform.SetParent(root, false);
            ColorSortingFeedback feedback = host.AddComponent<ColorSortingFeedback>();

            TextMesh instruction = Text("Instruction_Text", string.Empty,
                new Vector3(0f, 3.45f, 6.54f), Quaternion.Euler(0f, 180f, 0f),
                48, 0.19f, new Color(0.14f, 0.18f, 0.30f), host.transform);
            TextMesh feedbackText = Text("Feedback_Text", string.Empty,
                new Vector3(0f, 2.3f, -4.62f), Quaternion.Euler(0f, 180f, 0f),
                44, 0.19f, new Color(0.16f, 0.20f, 0.32f), host.transform);
            TextMesh progress = Text("Progress_Text", "Matched: 0 / 12",
                new Vector3(-4.5f, 2.3f, -4.62f), Quaternion.Euler(0f, 180f, 0f),
                40, 0.16f, new Color(0.16f, 0.20f, 0.32f), host.transform);
            TextMesh connection = Text("Connection_Text", "MATLAB: DISCONNECTED",
                new Vector3(4.5f, 2.3f, -4.62f), Quaternion.Euler(0f, 180f, 0f),
                36, 0.13f, new Color(0.72f, 0.18f, 0.15f), host.transform);

            GameObject banner = Box("Completion_Banner", new Vector3(0f, 4.25f, 3.2f),
                new Vector3(9.5f, 1.1f, 0.12f), Mat("M_ColorSorting_Correct"), root);
            Text("Completion_Banner_Text", "Well done! First star earned!",
                new Vector3(0f, 0f, -0.09f), Quaternion.Euler(0f, 180f, 0f),
                64, 0.32f, new Color(1f, 1f, 1f), banner.transform);
            banner.SetActive(false);

            GameObject correct = Box("Correct_Badge", new Vector3(-1.6f, 2.05f, -4.6f),
                new Vector3(0.5f, 0.5f, 0.1f), Mat("M_ColorSorting_Correct"), host.transform);
            GameObject error = Box("Error_Badge", new Vector3(1.6f, 2.05f, -4.6f),
                new Vector3(0.5f, 0.5f, 0.1f), Mat("M_ColorSorting_Error"), host.transform);
            correct.SetActive(false);
            error.SetActive(false);

            feedback.Configure(instruction, feedbackText, progress, connection,
                banner, correct, error);
        }

        private static void BuildPointer(GameObject prefab, Transform root)
        {
            GameObject instance = InstantiateUnder(prefab, root);
            instance.name = "Activity Pointer";
            instance.transform.position = new Vector3(0f, DragY, 0f);
            RecordOverride(instance);
        }

        private static void BuildController(Transform root)
        {
            GameObject host = new GameObject("Activity Controller");
            host.transform.SetParent(root, false);
            ColorSortingController controller = host.AddComponent<ColorSortingController>();

            Camera camera = FindComponent<Camera>("Main Camera");
            ActivityPointerCursor cursor = FindAny<ActivityPointerCursor>();
            ColorSortingFeedback feedback = FindAny<ColorSortingFeedback>();
            SortableObject[] toys = UnityEngine.Object.FindObjectsByType<SortableObject>(
                FindObjectsSortMode.None);
            ColorBasket[] baskets = UnityEngine.Object.FindObjectsByType<ColorBasket>(
                FindObjectsSortMode.None);
            Array.Sort(toys, (a, b) => string.CompareOrdinal(a.ObjectId, b.ObjectId));
            Array.Sort(baskets, (a, b) => string.CompareOrdinal(a.BasketId, b.BasketId));

            controller.Configure(camera, cursor, feedback, toys, baskets);
        }

        private static void ConfigurePresentation()
        {
            Camera camera = FindComponent<Camera>("Main Camera");
            if (camera != null)
            {
                Vector3 eye = new Vector3(0f, 9.5f, -10.5f);
                camera.transform.position = eye;
                camera.transform.rotation = Quaternion.LookRotation(
                    new Vector3(0f, 1.2f, 0.8f) - eye, Vector3.up);
                camera.fieldOfView = 50f;
            }

            Light sun = FindComponent<Light>("Directional Light");
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
                sun.color = new Color(1f, 0.95f, 0.86f);
                sun.intensity = 1.3f;
                sun.shadows = LightShadows.Soft;
            }

            AddPointLight("Warm_Light_Left", new Vector3(-5.5f, 4.6f, 0f), 1.1f, 12f);
            AddPointLight("Warm_Light_Right", new Vector3(5.5f, 4.6f, 0f), 1.1f, 12f);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.66f, 0.72f, 1f);
            RenderSettings.ambientIntensity = 1f;
        }

        private static void AddPointLight(string name, Vector3 position, float intensity, float range)
        {
            GameObject host = GameObject.Find(name);
            if (host == null)
            {
                host = new GameObject(name);
            }
            host.transform.SetParent(GameObject.Find(ActivityRootName).transform, false);
            host.transform.position = position;
            Light light = host.GetComponent<Light>();
            if (light == null)
            {
                light = host.AddComponent<Light>();
            }
            light.type = LightType.Point;
            light.color = new Color(1f, 0.94f, 0.82f);
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }

        // -----------------------------------------------------------------
        // Screenshot staging - editor only, never used at runtime
        // -----------------------------------------------------------------

        private static void RestoreSceneState(ColorSortingController controller)
        {
            ColorSortingFeedback feedback = FindAny<ColorSortingFeedback>();
            if (feedback != null)
            {
                feedback.Reset();
            }
            SortableObject[] toys = UnityEngine.Object.FindObjectsByType<SortableObject>(
                FindObjectsSortMode.None);
            for (int i = 0; i < toys.Length; i++)
            {
                toys[i].ReturnHome();
            }
            ColorBasket[] baskets = UnityEngine.Object.FindObjectsByType<ColorBasket>(
                FindObjectsSortMode.None);
            for (int i = 0; i < baskets.Length; i++)
            {
                // Also restores shared-material colours changed by StageInteraction
                // while capture runs in the Editor.
                baskets[i].SetHighlighted(false);
            }
            ActivityPointerCursor cursor = FindAny<ActivityPointerCursor>();
            if (cursor != null)
            {
                cursor.SetPosition(new Vector3(0f, DragY, 0f));
                cursor.SetMode(ActivityPointerCursor.PointerMode.Normal);
            }
        }

        private static void StageInteraction(ColorSortingController controller)
        {
            ColorSortingFeedback feedback = FindAny<ColorSortingFeedback>();
            if (feedback != null)
            {
                feedback.ShowNotice("Dragging the red apple toward the red basket...");
            }
            SortableObject toy = FindToy("object_01");
            if (toy != null)
            {
                toy.MoveTo(new Vector3(-4.3f, DragY, 1.4f));
            }
            ActivityPointerCursor cursor = FindAny<ActivityPointerCursor>();
            if (cursor != null)
            {
                cursor.SetPosition(new Vector3(-4.3f, DragY, 1.4f));
                cursor.SetMode(ActivityPointerCursor.PointerMode.Grabbing);
                foreach (ColorBasket basket in UnityEngine.Object.FindObjectsByType<ColorBasket>(
                    FindObjectsSortMode.None))
                {
                    basket.SetHighlighted(string.Equals(basket.BasketId, "basket_red",
                        StringComparison.Ordinal));
                }
            }
        }

        private static void StageCompleted(ColorSortingController controller)
        {
            // The staging mapping is read from MATLAB's authoritative activity
            // definition, not invented in Unity. It is only used to arrange a
            // still image; gameplay still relies entirely on MATLAB replies.
            Dictionary<string, string> mapping = LoadMatlabAnswerKey();
            SortableObject[] toys = UnityEngine.Object.FindObjectsByType<SortableObject>(
                FindObjectsSortMode.None);
            ColorBasket[] baskets = UnityEngine.Object.FindObjectsByType<ColorBasket>(
                FindObjectsSortMode.None);
            for (int i = 0; i < toys.Length; i++)
            {
                string target;
                if (!mapping.TryGetValue(toys[i].ObjectId, out target))
                {
                    continue;
                }
                for (int j = 0; j < baskets.Length; j++)
                {
                    if (string.Equals(baskets[j].BasketId, target, StringComparison.Ordinal))
                    {
                        toys[i].MarkPlaced(target, baskets[j].SnapPoint(RestY - 0.1f));
                        break;
                    }
                }
            }

            ColorSortingFeedback feedback = FindAny<ColorSortingFeedback>();
            if (feedback != null)
            {
                feedback.UpdateStars(ColorSortingCatalog.ObjectCount, 1);
                feedback.ShowCompletion("All toys are in the right baskets! First star earned!");
            }
        }

        [Serializable]
        private sealed class DefinitionJson
        {
            public string id;
            public string title;
            public ObjectJson[] objects;
            public string[] targets;
        }

        [Serializable]
        private sealed class ObjectJson
        {
            public string id;
            public string target;
        }

        private static Dictionary<string, string> LoadMatlabAnswerKey()
        {
            Dictionary<string, string> mapping = new Dictionary<string, string>(StringComparer.Ordinal);
            string path = Path.Combine(RepositoryRoot(), "Shared_Data", "ActivityData",
                "color_sorting.json");
            if (!File.Exists(path))
            {
                Debug.LogWarning("[Issue022] MATLAB activity definition not found: " + path);
                return mapping;
            }
            try
            {
                DefinitionJson definition = JsonUtility.FromJson<DefinitionJson>(File.ReadAllText(path));
                if (definition == null || definition.objects == null)
                {
                    return mapping;
                }
                for (int i = 0; i < definition.objects.Length; i++)
                {
                    mapping[definition.objects[i].id] = definition.objects[i].target;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Issue022] Could not read MATLAB definition: " + exception.Message);
            }
            return mapping;
        }

        private static SortableObject FindToy(string objectId)
        {
            SortableObject[] toys = UnityEngine.Object.FindObjectsByType<SortableObject>(
                FindObjectsSortMode.None);
            for (int i = 0; i < toys.Length; i++)
            {
                if (string.Equals(toys[i].ObjectId, objectId, StringComparison.Ordinal))
                {
                    return toys[i];
                }
            }
            return null;
        }

        private static void WarmUpRender(Camera camera)
        {
            RenderTexture texture = new RenderTexture(64, 64, 16, RenderTextureFormat.ARGB32);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            try
            {
                camera.targetTexture = texture;
                camera.Render();
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void RenderToPng(Camera camera, string path, int width, int height,
            Vector3 eye, Vector3 lookAt, float fov)
        {
            camera.transform.position = eye;
            camera.transform.rotation = Quaternion.LookRotation(lookAt - eye, Vector3.up);
            camera.fieldOfView = fov;
            camera.aspect = (float)width / height;

            RenderTexture texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = texture;
                camera.Render();
                RenderTexture.active = texture;
                image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(image);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        // -----------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------

        private static GameObject InstantiateUnder(GameObject prefab, Transform parent)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            SceneManager.MoveGameObjectToScene(instance, scene);
            instance.transform.SetParent(parent, true);
            return instance;
        }

        private static void RecordOverride(GameObject instance)
        {
            EditorUtility.SetDirty(instance);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
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
            Material material, Transform parent)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            RemoveCollider(go);
            return go;
        }

        private static GameObject Sphere(string name, Vector3 localPosition, float diameter,
            Material material, Transform parent)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = Vector3.one * diameter;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            RemoveCollider(go);
            return go;
        }

        private static GameObject Cylinder(string name, Vector3 localPosition, float diameter,
            float height, Material material, Transform parent)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = new Vector3(diameter, height * 0.5f, diameter);
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            RemoveCollider(go);
            return go;
        }

        private static TextMesh Text(string name, string content, Vector3 localPosition,
            Quaternion rotation, int fontSize, float characterSize, Color color, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = rotation;
            TextMesh mesh = go.AddComponent<TextMesh>();
            mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (mesh.font == null)
            {
                mesh.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            mesh.fontSize = fontSize;
            mesh.characterSize = characterSize;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.richText = true;
            mesh.color = color;
            mesh.text = content;
            return mesh;
        }

        private static void RemoveCollider(GameObject go)
        {
            Collider existing = go.GetComponent<Collider>();
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }
        }

        private static void SetChildMaterial(GameObject root, string childName, Material material)
        {
            Renderer renderer = FindInChildren<Renderer>(root.transform, childName);
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static GameObject FindChild(Transform root, string name)
        {
            for (int i = 0; i < root.childCount; i++)
            {
                if (string.Equals(root.GetChild(i).name, name, StringComparison.Ordinal))
                {
                    return root.GetChild(i).gameObject;
                }
            }
            return null;
        }

        private static T FindInChildren<T>(Transform root, string name) where T : Component
        {
            GameObject child = FindChild(root, name);
            return child != null ? child.GetComponent<T>() : null;
        }

        private static T FindAny<T>() where T : Component
        {
            T[] found = UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None);
            return found.Length > 0 ? found[0] : null;
        }

        private static T FindComponent<T>(string objectName) where T : Component
        {
            GameObject go = GameObject.Find(objectName);
            return go != null ? go.GetComponent<T>() : null;
        }

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

        private static int CountSceneObjects()
        {
            GameObject[] roots = scene.GetRootGameObjects();
            int count = 0;
            for (int i = 0; i < roots.Length; i++)
            {
                count += CountRecursive(roots[i]);
            }
            return count;
        }

        private static int CountRecursive(GameObject go)
        {
            int count = 1;
            for (int i = 0; i < go.transform.childCount; i++)
            {
                count += CountRecursive(go.transform.GetChild(i).gameObject);
            }
            return count;
        }
    }
}
