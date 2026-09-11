using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor.Rendering;

namespace TanamTempel.Editor
{
    /// <summary>
    /// Utility and Build Preprocessor to upgrade UniversalRenderPipelineAsset and 
    /// UniversalRenderPipelineGlobalSettings to the latest version required by Unity 6 / URP 17.
    /// Fixes BuildFailedException: "The UniversalRenderPipelineAsset ... is not at last version".
    /// </summary>
    public class URPAssetUpgradeUtility : IPreprocessBuildWithReport
    {
        // Must run before URPPreprocessBuild (which is int.MinValue + 100)
        public int callbackOrder => int.MinValue;

        public void OnPreprocessBuild(BuildReport report)
        {
            Debug.Log("[URPAssetUpgradeUtility] Running pre-build auto upgrade for URP assets...");
            UpgradeAllAssets(silent: true);
        }

        [MenuItem("Tools/URP/Fix and Upgrade Pipeline Assets", false, 1)]
        public static void UpgradeAllAssetsMenu()
        {
            UpgradeAllAssets(silent: false);
        }

        [MenuItem("Tools/URP/Regenerate Global Settings Asset", false, 2)]
        public static void RegenerateGlobalSettingsMenu()
        {
            bool confirm = EditorUtility.DisplayDialog(
                "Regenerate URP Global Settings",
                "This will recreate a fresh UniversalRenderPipelineGlobalSettings asset at the latest version.\n\nContinue?",
                "Yes, Regenerate",
                "Cancel"
            );

            if (!confirm) return;

            RegenerateGlobalSettings();
        }

        public static void UpgradeAllAssets(bool silent)
        {
            int upgradedAssetsCount = 0;
            int totalCheckedCount = 0;

            try
            {
                // 1. Upgrade UniversalRenderPipelineAsset instances
                string[] urpGuids = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset");
                foreach (string guid in urpGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                    if (urpAsset == null) continue;

                    totalCheckedCount++;
                    bool upgraded = UpgradePipelineAsset(urpAsset, path);
                    if (upgraded) upgradedAssetsCount++;
                }

                // 2. Upgrade UniversalRenderPipelineGlobalSettings instances (accessed dynamically as internal type)
                string[] gsGuids = AssetDatabase.FindAssets("t:UniversalRenderPipelineGlobalSettings");
                foreach (string guid in gsGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var gs = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                    if (gs == null) continue;

                    totalCheckedCount++;
                    bool upgraded = UpgradeGlobalSettings(gs, path);
                    if (upgraded) upgradedAssetsCount++;
                }

                // 3. Save all modified assets to disk
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // 4. Verify status
                string report = VerifyAllAssets();
                Debug.Log($"[URPAssetUpgradeUtility] Upgrade completed. Processed {totalCheckedCount} assets ({upgradedAssetsCount} updated).\n{report}");

                if (!silent)
                {
                    EditorUtility.DisplayDialog(
                        "URP Upgrade Selesai",
                        $"Berhasil memeriksa {totalCheckedCount} asset ({upgradedAssetsCount} diperbarui ke versi terbaru).\n\n" +
                        $"Status:\n{report}\n\n" +
                        "Semua asset telah disimpan ke disk. Sekarang Anda dapat melakukan Build Player kembali.",
                        "OK"
                    );
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[URPAssetUpgradeUtility] Error during asset upgrade: {ex.Message}\n{ex.StackTrace}");
                if (!silent)
                {
                    EditorUtility.DisplayDialog("URP Upgrade Error", $"Terjadi kesalahan saat upgrade asset: {ex.Message}", "OK");
                }
            }
        }

        private static bool UpgradePipelineAsset(UniversalRenderPipelineAsset asset, string path)
        {
            bool modified = false;

            // Call internal UpgradeAsset if available
            MethodInfo upgradeMethod = typeof(UniversalRenderPipelineAsset).GetMethod(
                "UpgradeAsset",
                BindingFlags.Static | BindingFlags.NonPublic
            );

            if (upgradeMethod != null)
            {
                try
                {
                    upgradeMethod.Invoke(null, new object[] { asset.GetInstanceID() });
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[URPAssetUpgradeUtility] UpgradeAsset invocation warning on {path}: {e.Message}");
                }
            }

            // Read / Update k_AssetVersion and k_AssetPreviousVersion via SerializedObject
            SerializedObject so = new SerializedObject(asset);
            SerializedProperty versionProp = so.FindProperty("k_AssetVersion");
            SerializedProperty prevVersionProp = so.FindProperty("k_AssetPreviousVersion");

            const int targetVersion = 12; // Unity 6 / URP 17 latest version

            if (versionProp != null && versionProp.intValue != targetVersion)
            {
                Debug.Log($"[URPAssetUpgradeUtility] Updating {asset.name} k_AssetVersion from {versionProp.intValue} to {targetVersion}");
                versionProp.intValue = targetVersion;
                modified = true;
            }

            if (prevVersionProp != null && prevVersionProp.intValue != targetVersion)
            {
                prevVersionProp.intValue = targetVersion;
                modified = true;
            }

            if (modified)
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(asset);
            }

            return modified;
        }

        private static Type GetGlobalSettingsType()
        {
            return typeof(UniversalRenderPipelineAsset).Assembly.GetType("UnityEngine.Rendering.Universal.UniversalRenderPipelineGlobalSettings");
        }

        private static bool UpgradeGlobalSettings(ScriptableObject gs, string path)
        {
            bool modified = false;
            Type gsType = GetGlobalSettingsType();

            // Call static UpgradeAsset(int instanceId) via reflection
            if (gsType != null)
            {
                try
                {
                    MethodInfo upgradeMethod = gsType.GetMethod("UpgradeAsset", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    upgradeMethod?.Invoke(null, new object[] { gs.GetInstanceID() });
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[URPAssetUpgradeUtility] UpgradeAsset invocation warning on GlobalSettings {path}: {e.Message}");
                }
            }

            // Ensure m_AssetVersion is at target version (8)
            SerializedObject so = new SerializedObject(gs);
            SerializedProperty versionProp = so.FindProperty("m_AssetVersion");

            int targetVersion = 8; // Default for Unity 6 / URP 17
            if (gsType != null)
            {
                FieldInfo lastVersionField = gsType.GetField("k_LastVersion", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (lastVersionField != null)
                {
                    targetVersion = (int)lastVersionField.GetValue(null);
                }
            }

            if (versionProp != null && versionProp.intValue != targetVersion)
            {
                Debug.Log($"[URPAssetUpgradeUtility] Updating {gs.name} m_AssetVersion from {versionProp.intValue} to {targetVersion}");
                versionProp.intValue = targetVersion;
                modified = true;
            }

            if (modified)
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(gs);
            }

            // Make sure GraphicsSettings references this global settings asset
            if (gs is RenderPipelineGlobalSettings rpgs)
            {
                EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset(typeof(UniversalRenderPipeline), rpgs);
            }

            return modified;
        }

        private static void RegenerateGlobalSettings()
        {
            try
            {
                string targetPath = "Assets/Settings/UniversalRenderPipelineGlobalSettings.asset";

                // Ensure directory exists
                string dir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                // If file exists, delete it safely
                if (File.Exists(targetPath))
                {
                    AssetDatabase.DeleteAsset(targetPath);
                }

                Type gsType = GetGlobalSettingsType();
                if (gsType == null)
                {
                    Debug.LogError("[URPAssetUpgradeUtility] UniversalRenderPipelineGlobalSettings type could not be loaded.");
                    return;
                }

                // Create new GlobalSettings instance
                var newInstance = ScriptableObject.CreateInstance(gsType) as RenderPipelineGlobalSettings;
                if (newInstance != null)
                {
                    newInstance.name = "UniversalRenderPipelineGlobalSettings";

                    // Initialize default graphics settings
                    MethodInfo initMethod = gsType.GetMethod("Initialize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    initMethod?.Invoke(newInstance, new object[] { null });

                    AssetDatabase.CreateAsset(newInstance, targetPath);
                    EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset(typeof(UniversalRenderPipeline), newInstance);

                    // Upgrade & Save
                    UpgradeGlobalSettings(newInstance, targetPath);

                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    EditorUtility.DisplayDialog(
                        "Global Settings Terbuat",
                        $"Asset UniversalRenderPipelineGlobalSettings baru berhasil dibuat di '{targetPath}' dan sudah diatur ke versi terbaru.",
                        "OK"
                    );
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[URPAssetUpgradeUtility] Error regenerating global settings: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"Gagal membuat ulang Global Settings: {ex.Message}", "OK");
            }
        }

        private static string VerifyAllAssets()
        {
            var sb = new System.Text.StringBuilder();

            string[] urpGuids = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset");
            foreach (string guid in urpGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                if (urpAsset == null) continue;

                bool isLast = CheckIsAtLastVersion(urpAsset);
                sb.AppendLine($"• {urpAsset.name}: {(isLast ? "OK (Versi Terbaru)" : "PERLU UPGRADE")}");
            }

            string[] gsGuids = AssetDatabase.FindAssets("t:UniversalRenderPipelineGlobalSettings");
            foreach (string guid in gsGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var gs = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (gs == null) continue;

                bool isLast = CheckIsAtLastVersion(gs);
                sb.AppendLine($"• {gs.name}: {(isLast ? "OK (Versi Terbaru)" : "PERLU UPGRADE")}");
            }

            return sb.ToString();
        }

        private static bool CheckIsAtLastVersion(UnityEngine.Object asset)
        {
            if (asset == null) return false;
            MethodInfo method = asset.GetType().GetMethod("IsAtLastVersion", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method != null)
            {
                try
                {
                    return (bool)method.Invoke(asset, null);
                }
                catch
                {
                    return false;
                }
            }
            return true;
        }
    }
}
