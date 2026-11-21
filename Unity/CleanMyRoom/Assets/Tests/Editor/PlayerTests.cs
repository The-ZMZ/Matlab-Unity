using System.Collections.Generic;
using CleanMyRoom.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CleanMyRoom.Tests
{
    /// <summary>
    /// Issue #13 checks for the child character, animation controller, keyboard
    /// movement normalisation, play-area clamping, collision blocking and camera
    /// follow behaviour.
    /// </summary>
    [Category("Issue13")]
    public sealed class PlayerTests
    {
        private const string BedroomScenePath = "Assets/Scenes/Bedroom.unity";
        private const string PrefabPath = "Assets/Prefabs/ChildCharacter.prefab";
        private const string ControllerPath = "Assets/Animations/ChildCharacter.controller";

        [Test]
        public void Prefab_HasOneControllerAnimatorAndAnimatedLimbs()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, "ChildCharacter prefab is missing.");

            CharacterController[] controllers = prefab.GetComponentsInChildren<CharacterController>(true);
            Assert.AreEqual(1, controllers.Length, "Expected exactly one CharacterController.");
            Assert.Greater(controllers[0].height, 0f);
            Assert.Greater(controllers[0].radius, 0f);

            Collider[] colliders = prefab.GetComponentsInChildren<Collider>(true);
            Assert.AreEqual(1, colliders.Length,
                "Visual parts must not add extra colliders on top of the CharacterController.");

            Assert.IsNotNull(prefab.GetComponent<PlayerController>(), "PlayerController is missing.");

            Animator animator = prefab.GetComponent<Animator>();
            Assert.IsNotNull(animator, "Animator is missing.");
            Assert.IsNotNull(animator.runtimeAnimatorController,
                "Animator has no AnimatorController assigned.");

            Assert.IsNotNull(prefab.transform.Find("Visual/LeftArmPivot"));
            Assert.IsNotNull(prefab.transform.Find("Visual/RightArmPivot"));
            Assert.IsNotNull(prefab.transform.Find("Visual/LeftLegPivot"));
            Assert.IsNotNull(prefab.transform.Find("Visual/RightLegPivot"));
        }

        [Test]
        public void AnimatorController_HasIdleAndWalkWithSpeedDrivenTransitions()
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            Assert.IsNotNull(controller, "ChildCharacter AnimatorController is missing.");

            Assert.IsTrue(HasFloatParameter(controller, "Speed"),
                "AnimatorController needs a float Speed parameter.");

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState idle = FindState(machine, "Idle");
            AnimatorState walk = FindState(machine, "Walk");
            Assert.IsNotNull(idle, "Idle state is missing.");
            Assert.IsNotNull(walk, "Walk state is missing.");
            Assert.AreEqual(idle, machine.defaultState, "Idle must be the default state.");

            AnimationClip idleClip = idle.motion as AnimationClip;
            AnimationClip walkClip = walk.motion as AnimationClip;
            Assert.IsNotNull(idleClip, "Idle state has no animation clip.");
            Assert.IsNotNull(walkClip, "Walk state has no animation clip.");
            Assert.IsTrue(AnimationUtility.GetAnimationClipSettings(idleClip).loopTime);
            Assert.IsTrue(AnimationUtility.GetAnimationClipSettings(walkClip).loopTime);

            AnimatorStateTransition toWalk = FindTransition(idle, walk);
            AnimatorStateTransition toIdle = FindTransition(walk, idle);
            Assert.IsNotNull(toWalk, "Idle -> Walk transition is missing.");
            Assert.IsNotNull(toIdle, "Walk -> Idle transition is missing.");

            Assert.IsFalse(toWalk.hasExitTime);
            Assert.IsFalse(toIdle.hasExitTime);
            Assert.LessOrEqual(toWalk.duration, 0.25f);
            Assert.LessOrEqual(toIdle.duration, 0.25f);
            Assert.IsTrue(HasCondition(toWalk, AnimatorConditionMode.Greater, 0.1f));
            Assert.IsTrue(HasCondition(toIdle, AnimatorConditionMode.Less, 0.1f));
        }

        [Test]
        public void BedroomScene_HasPlayerFollowCameraAndRoomColliders()
        {
            string originalScene = SceneManager.GetActiveScene().path;
            EditorSceneManager.OpenScene(BedroomScenePath, OpenSceneMode.Single);
            try
            {
                GameObject player = GameObject.Find("ChildCharacter");
                Assert.IsNotNull(player, "ChildCharacter is not placed in the bedroom scene.");
                Assert.AreEqual(0f, player.transform.position.x, 0.01f);
                Assert.AreEqual(0f, player.transform.position.y, 0.01f);
                Assert.AreEqual(0f, player.transform.position.z, 0.01f);
                Assert.IsNotNull(player.GetComponent<PlayerController>());

                Camera mainCamera = Camera.main;
                Assert.IsNotNull(mainCamera, "Bedroom scene has no Main Camera.");
                FollowCamera follow = mainCamera.GetComponent<FollowCamera>();
                Assert.IsNotNull(follow, "Main Camera has no FollowCamera.");
                Assert.AreEqual(player.transform, follow.Target,
                    "FollowCamera target is not the child character.");
                Assert.GreaterOrEqual(follow.MinimumHeight, 5f,
                    "Camera must stay above the five-unit walls.");

                Assert.IsNotNull(GameObject.Find("Bedroom_Floor").GetComponent<MeshCollider>());
                Assert.IsNotNull(GameObject.Find("Back_Wall").GetComponent<BoxCollider>());
                Assert.IsNotNull(GameObject.Find("Left_Wall").GetComponent<BoxCollider>());
                Assert.IsNotNull(GameObject.Find("Righr_Wall").GetComponent<BoxCollider>());
            }
            finally
            {
                if (!string.IsNullOrEmpty(originalScene) && originalScene != BedroomScenePath)
                {
                    EditorSceneManager.OpenScene(originalScene, OpenSceneMode.Single);
                }
            }
        }

        [Test]
        public void BedroomScene_IsTheFirstEnabledBuildScene()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            Assert.Greater(scenes.Length, 0, "Build Settings has no scenes.");
            Assert.AreEqual(BedroomScenePath, scenes[0].path,
                "The bedroom must be the startup scene.");
            Assert.IsTrue(scenes[0].enabled, "The bedroom scene is disabled in Build Settings.");
        }

        [Test]
        public void DiagonalInputIsNormalised()
        {
            Vector2 diagonal = PlayerController.NormalizeForDiagonalMovement(new Vector2(1f, 1f));
            Assert.AreEqual(1f, diagonal.magnitude, 0.0001f,
                "Diagonal input must not be sqrt(2) times faster.");

            Vector2 axis = PlayerController.NormalizeForDiagonalMovement(new Vector2(0f, -1f));
            Assert.AreEqual(0f, axis.x, 0.0001f);
            Assert.AreEqual(-1f, axis.y, 0.0001f);
        }

        [Test]
        public void Movement_StaysInsideConfiguredPlayArea()
        {
            GameObject root = CreatePlayerRoot(Vector3.zero);
            try
            {
                PlayerController player = root.GetComponent<PlayerController>();
                player.MinPlayArea = new Vector2(-2f, -2f);
                player.MaxPlayArea = new Vector2(2f, 2f);
                root.transform.position = new Vector3(1.8f, 0f, 0f);
                Physics.SyncTransforms();

                player.ApplyMovement(new Vector2(1f, 0f), 0.5f);

                Assert.LessOrEqual(root.transform.position.x, 2f + 0.01f,
                    "Movement did not clamp at the play-area boundary.");
                Assert.IsTrue(player.IsMoving);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SolidWallCollider_BlocksPlayerMovement()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "TestFloor";
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(20f, 1f, 20f);

            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "TestWall";
            wall.transform.position = new Vector3(2f, 1f, 0f);
            wall.transform.localScale = new Vector3(0.2f, 2f, 6f);

            GameObject root = CreatePlayerRoot(Vector3.zero);
            try
            {
                Physics.SyncTransforms();

                for (int i = 0; i < 8; i++)
                {
                    root.GetComponent<PlayerController>().ApplyMovement(new Vector2(1f, 0f), 0.1f);
                }

                Assert.Greater(root.transform.position.x, 0.5f,
                    "Player did not move toward the wall before being blocked.");
                Assert.Less(root.transform.position.x, 1.7f,
                    "CharacterController passed through the wall collider.");
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(wall);
                Object.DestroyImmediate(floor);
            }
        }

        [Test]
        public void FollowCamera_SmoothlyFollowsAndStaysAboveWalls()
        {
            GameObject target = new GameObject("FollowTarget");
            target.transform.position = Vector3.zero;

            GameObject cameraObject = new GameObject("FollowCameraTest");
            cameraObject.AddComponent<Camera>();
            FollowCamera follow = cameraObject.AddComponent<FollowCamera>();
            follow.Target = target.transform;

            try
            {
                follow.SnapToTarget();
                Vector3 start = cameraObject.transform.position;
                Assert.GreaterOrEqual(start.y, follow.MinimumHeight);

                target.transform.position = new Vector3(4f, 0f, 2f);
                for (int i = 0; i < 30; i++)
                {
                    follow.Follow(0.05f);
                }

                Assert.Greater(cameraObject.transform.position.x, start.x + 1f,
                    "Camera did not follow the target horizontally.");
                Assert.Greater(cameraObject.transform.position.z, start.z + 0.5f,
                    "Camera did not follow the target in depth.");
                Assert.GreaterOrEqual(cameraObject.transform.position.y, follow.MinimumHeight,
                    "Camera dropped below the wall height.");
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(target);
            }
        }

        private static GameObject CreatePlayerRoot(Vector3 position)
        {
            GameObject root = new GameObject("TestPlayer");
            root.transform.position = position;

            CharacterController controller = root.AddComponent<CharacterController>();
            controller.height = 1.9f;
            controller.radius = 0.32f;
            controller.center = new Vector3(0f, 0.95f, 0f);

            root.AddComponent<PlayerController>();
            return root;
        }

        private static bool HasFloatParameter(AnimatorController controller, string name)
        {
            for (int i = 0; i < controller.parameters.Length; i++)
            {
                AnimatorControllerParameter parameter = controller.parameters[i];
                if (parameter.name == name &&
                    parameter.type == AnimatorControllerParameterType.Float)
                {
                    return true;
                }
            }
            return false;
        }

        private static AnimatorState FindState(AnimatorStateMachine machine, string name)
        {
            for (int i = 0; i < machine.states.Length; i++)
            {
                if (machine.states[i].state.name == name)
                {
                    return machine.states[i].state;
                }
            }
            return null;
        }

        private static AnimatorStateTransition FindTransition(AnimatorState from, AnimatorState to)
        {
            AnimatorStateTransition[] transitions = from.transitions;
            for (int i = 0; i < transitions.Length; i++)
            {
                if (transitions[i].destinationState == to)
                {
                    return transitions[i];
                }
            }
            return null;
        }

        private static bool HasCondition(AnimatorStateTransition transition, AnimatorConditionMode mode,
            float threshold)
        {
            List<AnimatorCondition> conditions = new List<AnimatorCondition>(transition.conditions);
            for (int i = 0; i < conditions.Count; i++)
            {
                if (conditions[i].mode == mode &&
                    Mathf.Approximately(conditions[i].threshold, threshold))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
