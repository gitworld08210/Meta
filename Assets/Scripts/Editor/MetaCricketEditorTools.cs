#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace MetaCricket.Editor
{
    /// <summary>
    /// Editor utility tools for Meta Cricket project setup, validation, and asset management.
    /// Provides menu items for common development tasks.
    /// </summary>
    public static class MetaCricketEditorTools
    {
        private const string MENU_ROOT = "Meta Cricket/";
        private const string DATA_PATH = "Assets/Resources/Data/";
        private const string SCRIPTS_PATH = "Assets/Scripts/";

        #region Project Setup Validation

        [MenuItem(MENU_ROOT + "Validate/Project Setup", false, 100)]
        public static void ValidateProjectSetup()
        {
            int issues = 0;
            var report = new System.Text.StringBuilder();
            report.AppendLine("=== Meta Cricket Project Validation ===\n");

            // Check required directories
            string[] requiredDirs = new string[]
            {
                "Assets/Scripts/Core",
                "Assets/Scripts/MotionDetection",
                "Assets/Scripts/Calibration",
                "Assets/Scripts/ShotDetection",
                "Assets/Scripts/BallPhysics",
                "Assets/Scripts/MatchEngine",
                "Assets/Scripts/CareerMode",
                "Assets/Scripts/UI",
                "Assets/Scripts/Audio",
                "Assets/Scripts/VFX",
                "Assets/Scripts/Camera",
                "Assets/Scripts/Backend",
                "Assets/Resources/Data",
                "Assets/Materials",
                "Assets/Prefabs",
                "Assets/Scenes",
                "Assets/Settings"
            };

            report.AppendLine("[Directories]");
            foreach (string dir in requiredDirs)
            {
                if (AssetDatabase.IsValidFolder(dir))
                {
                    report.AppendLine($"  OK: {dir}");
                }
                else
                {
                    report.AppendLine($"  MISSING: {dir}");
                    issues++;
                }
            }

            // Check required data files
            report.AppendLine("\n[Data Files]");
            string[] requiredDataFiles = new string[]
            {
                "Assets/Resources/Data/CareerStages.json",
                "Assets/Resources/Data/Teams.json",
                "Assets/Resources/Data/Stadiums.json",
                "Assets/Resources/Data/ShotDefinitions.json"
            };

            foreach (string file in requiredDataFiles)
            {
                if (File.Exists(Path.Combine(Application.dataPath, "..", file)))
                {
                    report.AppendLine($"  OK: {file}");
                }
                else
                {
                    report.AppendLine($"  MISSING: {file}");
                    issues++;
                }
            }

            // Check for required packages
            report.AppendLine("\n[Packages]");
            string manifestPath = Path.Combine(Application.dataPath, "..", "Packages/manifest.json");
            if (File.Exists(manifestPath))
            {
                string manifest = File.ReadAllText(manifestPath);
                string[] requiredPackages = new string[]
                {
                    "com.unity.xr.arfoundation",
                    "com.unity.xr.arcore",
                    "com.unity.sentis",
                    "com.unity.render-pipelines.universal",
                    "com.unity.cinemachine",
                    "com.unity.textmeshpro",
                    "com.unity.inputsystem"
                };

                foreach (string pkg in requiredPackages)
                {
                    if (manifest.Contains(pkg))
                    {
                        report.AppendLine($"  OK: {pkg}");
                    }
                    else
                    {
                        report.AppendLine($"  MISSING: {pkg}");
                        issues++;
                    }
                }
            }
            else
            {
                report.AppendLine("  ERROR: manifest.json not found");
                issues++;
            }

            // Check build settings
            report.AppendLine("\n[Build Settings]");
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {
                report.AppendLine("  OK: Build target is Android");
            }
            else
            {
                report.AppendLine($"  WARNING: Build target is {EditorUserBuildSettings.activeBuildTarget} (should be Android)");
                issues++;
            }

            // Summary
            report.AppendLine($"\n=== Validation Complete: {issues} issue(s) found ===");

            if (issues == 0)
            {
                Debug.Log(report.ToString());
                EditorUtility.DisplayDialog("Project Validation", "All checks passed! Project is correctly set up.", "OK");
            }
            else
            {
                Debug.LogWarning(report.ToString());
                EditorUtility.DisplayDialog("Project Validation", $"Found {issues} issue(s). Check the Console for details.", "OK");
            }
        }

        #endregion

        #region Missing Reference Checker

        [MenuItem(MENU_ROOT + "Validate/Check Missing References", false, 101)]
        public static void CheckMissingReferences()
        {
            int missingCount = 0;
            var report = new System.Text.StringBuilder();
            report.AppendLine("=== Missing Reference Check ===\n");

            // Check all GameObjects in the current scene
            GameObject[] allObjects = Object.FindObjectsOfType<GameObject>(true);

            foreach (GameObject go in allObjects)
            {
                Component[] components = go.GetComponents<Component>();

                foreach (Component component in components)
                {
                    if (component == null)
                    {
                        report.AppendLine($"  MISSING SCRIPT on: {GetFullPath(go)}");
                        missingCount++;
                        continue;
                    }

                    SerializedObject serializedObject = new SerializedObject(component);
                    SerializedProperty property = serializedObject.GetIterator();

                    while (property.NextVisible(true))
                    {
                        if (property.propertyType == SerializedPropertyType.ObjectReference)
                        {
                            if (property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0)
                            {
                                report.AppendLine($"  MISSING REF: {GetFullPath(go)} -> {component.GetType().Name}.{property.name}");
                                missingCount++;
                            }
                        }
                    }
                }
            }

            // Check prefabs
            report.AppendLine("\n[Prefabs]");
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" });

            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab != null)
                {
                    Component[] components = prefab.GetComponentsInChildren<Component>(true);
                    foreach (Component component in components)
                    {
                        if (component == null)
                        {
                            report.AppendLine($"  MISSING SCRIPT in prefab: {path}");
                            missingCount++;
                        }
                    }
                }
            }

            report.AppendLine($"\n=== Check Complete: {missingCount} missing reference(s) found ===");

            if (missingCount == 0)
            {
                Debug.Log(report.ToString());
                EditorUtility.DisplayDialog("Missing References", "No missing references found!", "OK");
            }
            else
            {
                Debug.LogWarning(report.ToString());
                EditorUtility.DisplayDialog("Missing References", $"Found {missingCount} missing reference(s). Check the Console for details.", "OK");
            }
        }

        private static string GetFullPath(GameObject go)
        {
            string path = go.name;
            Transform parent = go.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }

        #endregion

        #region ScriptableObject Creation

        [MenuItem(MENU_ROOT + "Create/Supabase Config", false, 200)]
        public static void CreateSupabaseConfig()
        {
            CreateScriptableObjectAsset("SupabaseConfig", "Assets/Resources/Config");
        }

        [MenuItem(MENU_ROOT + "Create/Match Config", false, 201)]
        public static void CreateMatchConfig()
        {
            CreateScriptableObjectAsset("MatchConfig", "Assets/Resources/Config");
        }

        [MenuItem(MENU_ROOT + "Create/Audio Data", false, 202)]
        public static void CreateAudioData()
        {
            CreateScriptableObjectAsset("AudioData", "Assets/Resources/Config");
        }

        [MenuItem(MENU_ROOT + "Create/Theme Colors", false, 203)]
        public static void CreateThemeColors()
        {
            CreateScriptableObjectAsset("ThemeColors", "Assets/Resources/Config");
        }

        [MenuItem(MENU_ROOT + "Create/Career Stage Data", false, 205)]
        public static void CreateCareerStageData()
        {
            CreateScriptableObjectAsset("CareerStageData", "Assets/Resources/Config");
        }

        [MenuItem(MENU_ROOT + "Create/Team Data", false, 206)]
        public static void CreateTeamData()
        {
            CreateScriptableObjectAsset("TeamData", "Assets/Resources/Config");
        }

        [MenuItem(MENU_ROOT + "Create/Stadium Data", false, 207)]
        public static void CreateStadiumData()
        {
            CreateScriptableObjectAsset("StadiumData", "Assets/Resources/Config");
        }

        private static void CreateScriptableObjectAsset(string typeName, string directory)
        {
            // Ensure directory exists
            if (!AssetDatabase.IsValidFolder(directory))
            {
                string[] folders = directory.Split('/');
                string currentPath = folders[0];
                for (int i = 1; i < folders.Length; i++)
                {
                    string nextPath = currentPath + "/" + folders[i];
                    if (!AssetDatabase.IsValidFolder(nextPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, folders[i]);
                    }
                    currentPath = nextPath;
                }
            }

            // Find the type by searching all assemblies
            System.Type type = null;
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetTypes().FirstOrDefault(t => t.Name == typeName && t.IsSubclassOf(typeof(ScriptableObject)));
                if (type != null) break;
            }

            if (type == null)
            {
                EditorUtility.DisplayDialog("Error", $"Could not find ScriptableObject type: {typeName}", "OK");
                return;
            }

            ScriptableObject asset = ScriptableObject.CreateInstance(type);
            string assetPath = $"{directory}/{typeName}.asset";

            // Ensure unique name
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;

            Debug.Log($"Created {typeName} asset at: {assetPath}");
        }

        #endregion

        #region Utility Tools

        [MenuItem(MENU_ROOT + "Tools/Reload JSON Data", false, 300)]
        public static void ReloadJsonData()
        {
            AssetDatabase.Refresh();
            Debug.Log("[Meta Cricket] JSON data files reloaded.");
            EditorUtility.DisplayDialog("Reload Complete", "All JSON data files have been refreshed from disk.", "OK");
        }

        [MenuItem(MENU_ROOT + "Tools/Open Data Folder", false, 301)]
        public static void OpenDataFolder()
        {
            string fullPath = Path.Combine(Application.dataPath, "Resources/Data");
            if (Directory.Exists(fullPath))
            {
                EditorUtility.RevealInFinder(fullPath);
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Data folder not found. Run project validation first.", "OK");
            }
        }

        [MenuItem(MENU_ROOT + "Tools/Create Missing Directories", false, 302)]
        public static void CreateMissingDirectories()
        {
            string[] directories = new string[]
            {
                "Assets/Resources/Data",
                "Assets/Resources/Config",
                "Assets/Materials",
                "Assets/Prefabs",
                "Assets/Scenes",
                "Assets/Settings",
                "Assets/Models",
                "Assets/Textures",
                "Assets/Animations",
                "Assets/Fonts"
            };

            int created = 0;
            foreach (string dir in directories)
            {
                if (!AssetDatabase.IsValidFolder(dir))
                {
                    string[] parts = dir.Split('/');
                    string current = parts[0];
                    for (int i = 1; i < parts.Length; i++)
                    {
                        string next = current + "/" + parts[i];
                        if (!AssetDatabase.IsValidFolder(next))
                        {
                            AssetDatabase.CreateFolder(current, parts[i]);
                            created++;
                        }
                        current = next;
                    }
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"[Meta Cricket] Created {created} missing directories.");
            EditorUtility.DisplayDialog("Directories Created", $"Created {created} missing director(ies).", "OK");
        }

        [MenuItem(MENU_ROOT + "Tools/Validate JSON Files", false, 303)]
        public static void ValidateJsonFiles()
        {
            string dataPath = Path.Combine(Application.dataPath, "Resources/Data");
            if (!Directory.Exists(dataPath))
            {
                EditorUtility.DisplayDialog("Error", "Data folder not found.", "OK");
                return;
            }

            string[] jsonFiles = Directory.GetFiles(dataPath, "*.json");
            int valid = 0;
            int invalid = 0;
            var report = new System.Text.StringBuilder();
            report.AppendLine("=== JSON Validation ===\n");

            foreach (string file in jsonFiles)
            {
                string filename = Path.GetFileName(file);
                try
                {
                    string content = File.ReadAllText(file);
                    // Basic JSON validation - try to parse
                    JsonUtility.FromJson<object>(content);
                    report.AppendLine($"  OK: {filename}");
                    valid++;
                }
                catch (System.Exception ex)
                {
                    report.AppendLine($"  ERROR: {filename} - {ex.Message}");
                    invalid++;
                }
            }

            report.AppendLine($"\n=== {valid} valid, {invalid} invalid ===");
            Debug.Log(report.ToString());

            if (invalid == 0)
            {
                EditorUtility.DisplayDialog("JSON Validation", $"All {valid} JSON file(s) are valid.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("JSON Validation", $"{invalid} file(s) have errors. Check Console.", "OK");
            }
        }

        [MenuItem(MENU_ROOT + "Tools/Count Scripts by System", false, 304)]
        public static void CountScriptsBySystem()
        {
            string scriptsPath = Path.Combine(Application.dataPath, "Scripts");
            if (!Directory.Exists(scriptsPath))
            {
                EditorUtility.DisplayDialog("Error", "Scripts folder not found.", "OK");
                return;
            }

            var report = new System.Text.StringBuilder();
            report.AppendLine("=== Script Count by System ===\n");

            string[] systemDirs = Directory.GetDirectories(scriptsPath);
            int totalScripts = 0;

            foreach (string dir in systemDirs.OrderBy(d => d))
            {
                string systemName = Path.GetFileName(dir);
                int count = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories).Length;
                report.AppendLine($"  {systemName}: {count} script(s)");
                totalScripts += count;
            }

            report.AppendLine($"\n  TOTAL: {totalScripts} scripts");
            Debug.Log(report.ToString());
            EditorUtility.DisplayDialog("Script Count", $"Total: {totalScripts} scripts across {systemDirs.Length} systems.", "OK");
        }

        #endregion

        #region Quick Access

        [MenuItem(MENU_ROOT + "Documentation/Open README", false, 400)]
        public static void OpenReadme()
        {
            string readmePath = Path.Combine(Application.dataPath, "..", "README.md");
            if (File.Exists(readmePath))
            {
                Application.OpenURL("file://" + Path.GetFullPath(readmePath));
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "README.md not found in project root.", "OK");
            }
        }

        [MenuItem(MENU_ROOT + "Documentation/Open SETUP Guide", false, 401)]
        public static void OpenSetupGuide()
        {
            string setupPath = Path.Combine(Application.dataPath, "..", "SETUP.md");
            if (File.Exists(setupPath))
            {
                Application.OpenURL("file://" + Path.GetFullPath(setupPath));
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "SETUP.md not found in project root.", "OK");
            }
        }

        #endregion
    }
}
#endif
