#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Wolfsun.Editor
{
    /// <summary>Keep imported 8x8 VFX atlases configured for Canvas RawImage UV playback.</summary>
    public sealed class VfxAtlasImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.Contains("/Resources/Art/VFX/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
        }
    }
}
#endif
