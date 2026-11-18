#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MetaCricket.Core;
using MetaCricket.MatchEngine;
using MetaCricket.BallPhysics;
using MetaCricket.MotionDetection;
using MetaCricket.ShotDetection;
using MetaCricket.Calibration;
using MetaCricket.CareerMode;
using MetaCricket.UI.Screens;
using MetaCricket.Audio;

namespace MetaCricket.Editor
{
    /// <summary>
    /// Automated scene builder for Meta Cricket. Generates all 7 required Unity scenes
    /// with complete GameObject hierarchies, lighting, 3D pitch/stumps, physics systems,
    /// and configures the Unity Editor Build Settings.
    /// </summary>
    public static class MetaCricketSceneBuilder
    {
        private const string SCENES_PATH = "Assets/Scenes";

        [MenuItem("Meta Cricket/Scenes/Generate All 7 Scenes", false, 50)]
        public static void GenerateAllScenes()
        {
            if (!AssetDatabase.IsValidFolder(SCENES_PATH))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            var scenePaths = new List<string>();

            // Generate each scene
            scenePaths.Add(CreateSplashScreenScene());
            scenePaths.Add(CreateMainMenuScene());
            scenePaths.Add(CreateCalibrationScene());
            scenePaths.Add(CreateMatchScene());
            scenePaths.Add(CreateCareerHubScene());
            scenePaths.Add(CreateSettingsScene());
            scenePaths.Add(CreateLoadingScene());

            // Configure Build Settings
            var buildScenes = new EditorBuildSettingsScene[scenePaths.Count];
            for (int i = 0; i < scenePaths.Count; i++)
            {
                buildScenes[i] = new EditorBuildSettingsScene(scenePaths[i], true);
            }
            EditorBuildSettings.scenes = buildScenes;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[MetaCricketSceneBuilder] Successfully created and wired all {scenePaths.Count} scenes into Build Settings!");
            EditorUtility.DisplayDialog("Scenes Generated",
                $"Successfully created all 7 game scenes in '{SCENES_PATH}' and updated File > Build Settings!\n\nYou can now open 'SplashScreen' or 'Match' and hit Play.", "Awesome!");
        }

        #region Scene Generators

        private static string CreateSplashScreenScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            string path = $"{SCENES_PATH}/SplashScreen.unity";

            // Camera & Light
            CreateDirectionalLight();
            CreateCamera(new Vector3(0, 1, -10), Vector3.zero);

            // Core Persistent Systems
            GameObject coreObj = new GameObject("--- CORE SYSTEMS ---");
            coreObj.AddComponent<GameManager>();
            coreObj.AddComponent<SceneLoader>();

            GameObject audioObj = new GameObject("AudioManager");
            audioObj.AddComponent<AudioManager>();
            audioObj.transform.SetParent(coreObj.transform);

            // UI
            GameObject canvasObj = CreateCanvasWithEventSystem("SplashCanvas");
            canvasObj.AddComponent<SplashScreenUI>();

            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static string CreateMainMenuScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            string path = $"{SCENES_PATH}/MainMenu.unity";

            CreateDirectionalLight();
            CreateCamera(new Vector3(0, 1, -10), Vector3.zero);

            GameObject canvasObj = CreateCanvasWithEventSystem("MainMenuCanvas");
            canvasObj.AddComponent<MainMenuUI>();

            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static string CreateCalibrationScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            string path = $"{SCENES_PATH}/Calibration.unity";

            CreateDirectionalLight();
            CreateCamera(new Vector3(0, 1.5f, -3.5f), Vector3.zero);

            // Calibration systems
            GameObject calibObj = new GameObject("--- CALIBRATION SYSTEM ---");
            calibObj.AddComponent<CalibrationManager>();
            calibObj.AddComponent<TPoseDetector>();
            calibObj.AddComponent<MockPoseProvider>();

            GameObject canvasObj = CreateCanvasWithEventSystem("CalibrationCanvas");
            canvasObj.AddComponent<CalibrationScreenUI>();

            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static string CreateMatchScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            string path = $"{SCENES_PATH}/Match.unity";

            // Lighting
            CreateDirectionalLight();

            // Batting Camera (Behind batsman looking at bowler, classic cricket broadcast view)
            CreateCamera(new Vector3(0f, 1.8f, -4.5f), new Vector3(10f, 0f, 0f));

            // 3D Cricket Pitch & Environment
            GameObject envObj = new GameObject("--- CRICKET PITCH ENVIRONMENT ---");
            
            // Ground field plane
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "GroundField";
            ground.transform.SetParent(envObj.transform);
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(8f, 1f, 8f);

            // Pitch strip (20.12m long x 3.05m wide)
            GameObject pitch = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pitch.name = "PitchStrip";
            pitch.transform.SetParent(envObj.transform);
            pitch.transform.position = new Vector3(0f, 0.02f, 8f);
            pitch.transform.localScale = new Vector3(3.05f, 0.04f, 20.12f);

            // Batsman Stumps (Z = 0)
            CreateStumps("BatsmanStumps", new Vector3(0, 0, 0), envObj.transform);
            
            // Bowler Stumps (Z = 18m)
            CreateStumps("BowlerStumps", new Vector3(0, 0, 18f), envObj.transform);

            // Crease lines
            GameObject crease = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crease.name = "PoppingCrease";
            crease.transform.SetParent(envObj.transform);
            crease.transform.position = new Vector3(0f, 0.05f, 1.22f);
            crease.transform.localScale = new Vector3(2.6f, 0.02f, 0.1f);

            // Ball Object
            GameObject ballObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ballObj.name = "CricketBall";
            ballObj.transform.position = new Vector3(0f, 2.0f, 18f);
            ballObj.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);
            var rb = ballObj.AddComponent<Rigidbody>();
            rb.mass = 0.16f; // Standard 160g cricket ball
            ballObj.AddComponent<BallController>();
            ballObj.AddComponent<BallTrajectory>();

            // Bat Object
            GameObject batObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            batObj.name = "CricketBat";
            batObj.transform.position = new Vector3(0.4f, 0.8f, 0.2f);
            batObj.transform.localScale = new Vector3(0.12f, 0.85f, 0.06f);

            // Match Engine & Systems
            GameObject matchSystems = new GameObject("--- MATCH ENGINE SYSTEMS ---");
            var matchMgr = matchSystems.AddComponent<MatchManager>();
            var bowlingMachine = matchSystems.AddComponent<BowlingMachine>();
            var batCollision = matchSystems.AddComponent<BatCollision>();
            matchSystems.AddComponent<FieldingSimulator>();
            matchSystems.AddComponent<OpponentAI>();

            // Motion & Shot Detection
            GameObject shotSystems = new GameObject("--- MOTION & SHOT DETECTION ---");
            var mockPose = shotSystems.AddComponent<MockPoseProvider>();
            var smoother = shotSystems.AddComponent<JointSmoother>();
            var shotDetector = shotSystems.AddComponent<ShotDetector>();
            var shotClassifier = shotSystems.AddComponent<ShotClassifier>();
            shotSystems.AddComponent<TimingWindow>();

            // UI
            GameObject canvasObj = CreateCanvasWithEventSystem("MatchCanvas");
            canvasObj.AddComponent<MatchUI>();

            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static string CreateCareerHubScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            string path = $"{SCENES_PATH}/CareerHub.unity";

            CreateDirectionalLight();
            CreateCamera(new Vector3(0, 1, -10), Vector3.zero);

            GameObject careerObj = new GameObject("--- CAREER MANAGER ---");
            careerObj.AddComponent<CareerManager>();

            GameObject canvasObj = CreateCanvasWithEventSystem("CareerHubCanvas");
            canvasObj.AddComponent<CareerHubUI>();

            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static string CreateSettingsScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            string path = $"{SCENES_PATH}/Settings.unity";

            CreateDirectionalLight();
            CreateCamera(new Vector3(0, 1, -10), Vector3.zero);

            GameObject canvasObj = CreateCanvasWithEventSystem("SettingsCanvas");
            canvasObj.AddComponent<SettingsUI>();

            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static string CreateLoadingScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            string path = $"{SCENES_PATH}/Loading.unity";

            CreateDirectionalLight();
            CreateCamera(new Vector3(0, 1, -10), Vector3.zero);

            GameObject canvasObj = CreateCanvasWithEventSystem("LoadingCanvas");
            canvasObj.AddComponent<LoadingScreenUI>();

            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        #endregion

        #region Helpers

        private static void CreateDirectionalLight()
        {
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.intensity = 1.2f;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static void CreateCamera(Vector3 position, Vector3 rotation)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            camObj.AddComponent<AudioListener>();
            camObj.transform.position = position;
            camObj.transform.rotation = Quaternion.Euler(rotation);
        }

        private static GameObject CreateCanvasWithEventSystem(string name)
        {
            GameObject canvasObj = new GameObject(name);
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // EventSystem
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            eventSystem.transform.SetParent(canvasObj.transform);

            return canvasObj;
        }

        private static void CreateStumps(string name, Vector3 basePosition, Transform parent)
        {
            GameObject stumpsRoot = new GameObject(name);
            stumpsRoot.transform.SetParent(parent);
            stumpsRoot.transform.position = basePosition;

            float spacing = 0.11f; // ~4.5 inches between stumps
            for (int i = -1; i <= 1; i++)
            {
                GameObject stump = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stump.name = $"Stump_{i + 2}";
                stump.transform.SetParent(stumpsRoot.transform);
                stump.transform.position = basePosition + new Vector3(i * spacing, 0.355f, 0f);
                stump.transform.localScale = new Vector3(0.04f, 0.355f, 0.04f);
            }

            // Bails
            GameObject bail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bail.name = "Bail";
            bail.transform.SetParent(stumpsRoot.transform);
            bail.transform.position = basePosition + new Vector3(0f, 0.72f, 0f);
            bail.transform.localScale = new Vector3(0.24f, 0.02f, 0.02f);
        }

        #endregion
    }
}
#endif
