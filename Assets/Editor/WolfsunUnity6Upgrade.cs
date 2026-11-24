using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Wolfsun.Editor
{
    /// <summary>
    /// Clears Unity 6 deprecation warnings: Dynamic Batching off, Input System backend,
    /// and assigns a minimal URP asset so Built-in RP is no longer the active pipeline.
    /// </summary>
    public static class WolfsunUnity6Upgrade
    {
        private const string PipelineDir = "Assets/Settings";
        private const string RendererPath = PipelineDir + "/DawnwagerRenderer.asset";
        private const string PipelinePath = PipelineDir + "/DawnwagerURP.asset";

        [MenuItem("Dawnwager/Apply Unity 6 Platform Fixes")]
        public static void ApplyFromMenu()
        {
            Apply();
            Debug.Log("Dawnwager Unity 6 platform fixes applied (URP + Input System + Dynamic Batching off).");
        }

        /// <summary>Batchmode entry: -executeMethod Wolfsun.Editor.WolfsunUnity6Upgrade.Apply</summary>
        public static void Apply()
        {
            DisableDynamicBatching();
            EnsureInputSystemBackend();
            EnsureUrpPipeline();
            WolfsunBuild.EnsureDefaultVolumeProfileSaved();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void DisableDynamicBatching()
        {
            // Serialized PlayerSettings still owns the per-platform batching toggles.
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0) return;
            var so = new SerializedObject(assets[0]);
            var batching = so.FindProperty("m_BuildTargetBatching");
            if (batching == null || !batching.isArray) return;
            for (int i = 0; i < batching.arraySize; i++)
            {
                var entry = batching.GetArrayElementAtIndex(i);
                var dynamic = entry.FindPropertyRelative("m_DynamicBatching");
                if (dynamic != null) dynamic.boolValue = false;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureInputSystemBackend()
        {
            // 0 = Old, 1 = New, 2 = Both
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0) return;
            var so = new SerializedObject(assets[0]);
            var handler = so.FindProperty("activeInputHandler");
            if (handler != null)
            {
                handler.intValue = 1;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void EnsureUrpPipeline()
        {
            Directory.CreateDirectory(PipelineDir);

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }
            else
            {
                // Keep renderer reference valid after reimport.
                var so = new SerializedObject(pipeline);
                var list = so.FindProperty("m_RendererDataList");
                if (list != null && list.isArray)
                {
                    if (list.arraySize == 0) list.arraySize = 1;
                    list.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            EditorUtility.SetDirty(pipeline);
            EditorUtility.SetDirty(renderer);
        }
    }
}
