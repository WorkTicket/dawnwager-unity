using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Wolfsun.Editor
{
    public static class WolfsunBuild
    {
        private const string Scene = "Assets/Scenes/TheLastDawn.unity";

        [MenuItem("Dawnwager/Open Game Scene")]
        public static void Open()
        {
            EditorSceneManager.OpenScene(Scene);
        }

        [MenuItem("Dawnwager/Build Windows Player")]
        public static void Windows()
        {
            EnsureDefaultVolumeProfileSaved();
            Directory.CreateDirectory("Builds/Dawnwager-Windows");
            PlayerSettings.productName = "Dawnwager";
            PlayerSettings.companyName = "WOLFSUN";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.bundleVersion = "0.6.0";
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultIsNativeResolution = true;
            PlayerSettings.resizableWindow = true;
            var icon = Resources.Load<Texture2D>("Art/Identity/Icon");
            var target = NamedBuildTarget.Standalone;
            var iconSizes = PlayerSettings.GetIconSizes(target, IconKind.Application);
            if (icon && iconSizes.Length > 0)
                PlayerSettings.SetIcons(target, iconSizes.Select(_ => icon).ToArray(), IconKind.Application);
            var report = BuildPipeline.BuildPlayer(new[] { Scene }, "Builds/Dawnwager-Windows/Dawnwager.exe",
                BuildTarget.StandaloneWindows64, BuildOptions.None);
            CleanupPerformanceTestArtifacts();
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Dawnwager build failed: " + report.summary.result);
            if (!File.Exists("Builds/Dawnwager-Windows/Dawnwager.exe") ||
                !File.Exists("Builds/Dawnwager-Windows/UnityPlayer.dll") ||
                !Directory.Exists("Builds/Dawnwager-Windows/Dawnwager_Data"))
                throw new InvalidOperationException("Windows export is missing runtime files.");
            Directory.CreateDirectory("Builds/Dawnwager-Windows/Credits");
            foreach (var credit in Directory.GetFiles("Credits", "*.txt"))
                File.Copy(credit, Path.Combine("Builds/Dawnwager-Windows/Credits", Path.GetFileName(credit)), true);
            foreach (var credit in Directory.GetFiles("Credits", "*.md"))
                File.Copy(credit, Path.Combine("Builds/Dawnwager-Windows/Credits", Path.GetFileName(credit)), true);
            EditorUtility.RevealInFinder("Builds/Dawnwager-Windows/Dawnwager.exe");
            Debug.Log("Built Builds/Dawnwager-Windows/Dawnwager.exe and verified its accompanying runtime files.");
        }

        public static void ImportSmoke()
        {
            Require<Texture2D>("Art/Foundry/DawnPass");
            Require<Texture2D>("Art/Foundry/Emberbridge");
            Require<Texture2D>("Art/Foundry/RagnarokMap");
            foreach (var name in new[]
                     {
                         "Wordmark", "Emblem", "Icon", "Primary", "Secondary", "PrimaryHover", "SecondaryHover",
                         "PrimaryPressed", "SecondaryPressed", "Disabled"
                     })
                Require<Texture2D>("Art/Identity/" + name);
            Require<Font>("Display");
            Require<Font>("Body");
            Require<AudioClip>("Audio/Menu");
            Require<AudioClip>("Audio/Cards/A23");
            foreach (var hero in Hero.All) Require<Texture2D>("Art/Foundry/Hero-" + hero.Id);
            foreach (var card in Catalog.All)
            {
                string group = card.Kind == Kind.Unit ? "Units" : card.Kind == Kind.Order ? "Cards" : "Places";
                Require<Texture2D>("Art/" + group + "/" + (card.Id == "X11" ? "X02" : card.Id));
                string frame = "Art/Foundry/Card-" + card.Faction + "-" + card.Kind +
                               (card.Legendary && card.Kind == Kind.Unit ? "-Mythic" : "");
                Require<Texture2D>(frame);
            }
            Open();
            Debug.Log("Dawnwager Unity import smoke passed.");
        }

        /// <summary>
        /// Persist URP default volume overrides so player builds stop asking to save them.
        /// </summary>
        internal static void EnsureDefaultVolumeProfileSaved()
        {
            GraphicsSettings.TryGetRenderPipelineSettings<URPDefaultVolumeProfileSettings>(out var settings);
            var profile = settings != null ? settings.volumeProfile : null;
            if (profile == null)
                profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/DefaultVolumeProfile.asset");
            if (profile == null) return;
            VolumeProfileUtils.EnsureAllOverridesForDefaultProfile(profile);
            if (settings != null) settings.volumeProfile = profile;
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// URP pulls in the performance test package, which drops temp JSON into Resources on build.
        /// </summary>
        internal static void CleanupPerformanceTestArtifacts()
        {
            string[] junk =
            {
                "Assets/Resources/PerformanceTestRunInfo.json",
                "Assets/Resources/PerformanceTestRunInfo.json.meta",
                "Assets/Resources/PerformanceTestRunSettings.json",
                "Assets/Resources/PerformanceTestRunSettings.json.meta"
            };
            bool removed = false;
            foreach (var path in junk)
            {
                if (!File.Exists(path) && !AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path)) continue;
                AssetDatabase.DeleteAsset(path);
                if (File.Exists(path)) File.Delete(path);
                removed = true;
            }
            if (removed) AssetDatabase.Refresh();
        }

        private static void Require<T>(string path) where T : UnityEngine.Object
        {
            if (Resources.Load<T>(path) == null)
                throw new InvalidOperationException("Unity resource failed to import: " + path);
        }
    }
}
