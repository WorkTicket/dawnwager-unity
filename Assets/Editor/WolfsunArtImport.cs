using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Wolfsun.Editor
{
    internal sealed class WolfsunArtImport : AssetPostprocessor
    {
        private static readonly string[] TextureRoots =
        {
            "Assets/Resources/Art/Foundry/",
            "Assets/Resources/Art/Identity/",
            "Assets/Resources/Art/Cards/",
            "Assets/Resources/Art/Units/",
            "Assets/Resources/Art/Orders/",
            "Assets/Resources/Art/Places/",
            "Assets/Resources/Art/Heroes/",
            "Assets/Resources/Art/BoardFrames/",
            "Assets/Resources/Art/Nameplates/",
            "Assets/Resources/Art/Campaign/",
        };

        private static readonly HashSet<string> SlicedSprites = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Assets/Resources/Art/Foundry/HistoryRail.png",
            "Assets/Resources/Art/Foundry/EndTurn.png",
            "Assets/Resources/Art/Foundry/EndTurnHover.png",
            "Assets/Resources/Art/Foundry/EndTurnPressed.png",
            "Assets/Resources/Art/Foundry/EndTurnDisabled.png",
            "Assets/Resources/Art/Foundry/EndTurnGlow.png",
            "Assets/Resources/Art/Foundry/Panel.png",
            "Assets/Resources/Art/Foundry/PanelGold.png",
            "Assets/Resources/Art/Foundry/ModalFrame.png",
            "Assets/Resources/Art/Foundry/Primary.png",
            "Assets/Resources/Art/Foundry/PrimaryHover.png",
            "Assets/Resources/Art/Foundry/PrimaryPressed.png",
            "Assets/Resources/Art/Foundry/PrimaryDisabled.png",
            "Assets/Resources/Art/Foundry/Secondary.png",
            "Assets/Resources/Art/Foundry/SecondaryHover.png",
            "Assets/Resources/Art/Foundry/SecondaryPressed.png",
            "Assets/Resources/Art/Foundry/SecondaryDisabled.png",
        };

        // Sprite-Borders.json uses left, top, right, bottom. Unity Sprite.border is left, bottom, right, top.
        private static readonly Dictionary<string, Vector4> AuthoredBorders = new Dictionary<string, Vector4>(StringComparer.OrdinalIgnoreCase)
        {
            ["Assets/Resources/Art/Foundry/HistoryRail.png"] = new Vector4(24, 26, 24, 26),
            ["Assets/Resources/Art/Foundry/EndTurn.png"] = new Vector4(24, 16, 24, 16),
            ["Assets/Resources/Art/Foundry/EndTurnHover.png"] = new Vector4(24, 16, 24, 16),
            ["Assets/Resources/Art/Foundry/EndTurnPressed.png"] = new Vector4(24, 16, 24, 16),
            ["Assets/Resources/Art/Foundry/EndTurnDisabled.png"] = new Vector4(24, 16, 24, 16),
            ["Assets/Resources/Art/Foundry/EndTurnGlow.png"] = new Vector4(24, 16, 24, 16),
            ["Assets/Resources/Art/Foundry/Panel.png"] = new Vector4(24, 24, 24, 24),
            ["Assets/Resources/Art/Foundry/PanelGold.png"] = new Vector4(24, 24, 24, 24),
            ["Assets/Resources/Art/Foundry/ModalFrame.png"] = new Vector4(28, 28, 28, 28),
            ["Assets/Resources/Art/Foundry/Primary.png"] = new Vector4(20, 20, 20, 20),
            ["Assets/Resources/Art/Foundry/PrimaryHover.png"] = new Vector4(20, 20, 20, 20),
            ["Assets/Resources/Art/Foundry/PrimaryPressed.png"] = new Vector4(20, 20, 20, 20),
            ["Assets/Resources/Art/Foundry/PrimaryDisabled.png"] = new Vector4(20, 20, 20, 20),
            ["Assets/Resources/Art/Foundry/Secondary.png"] = new Vector4(20, 20, 20, 20),
            ["Assets/Resources/Art/Foundry/SecondaryHover.png"] = new Vector4(20, 20, 20, 20),
            ["Assets/Resources/Art/Foundry/SecondaryPressed.png"] = new Vector4(20, 20, 20, 20),
            ["Assets/Resources/Art/Foundry/SecondaryDisabled.png"] = new Vector4(20, 20, 20, 20),
        };

        private void OnPreprocessTexture()
        {
            if (!IsArtPath(assetPath)) return;
            var texture = (TextureImporter)assetImporter;
            texture.npotScale = TextureImporterNPOTScale.None;
            texture.mipmapEnabled = false;
            texture.maxTextureSize = 2048;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.sRGBTexture = true;
            bool png = assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase);
            texture.alphaIsTransparency = png;

            if (SlicedSprites.Contains(assetPath) && AuthoredBorders.TryGetValue(assetPath, out var ltrb))
            {
                texture.textureType = TextureImporterType.Sprite;
                texture.spriteImportMode = SpriteImportMode.Single;
                texture.spritePixelsPerUnit = 100;
                // Unity 6: mesh type lives on TextureImporterSettings, not TextureImporter.
                var settings = new TextureImporterSettings();
                texture.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                texture.SetTextureSettings(settings);
                // JSON L,T,R,B → Unity L,B,R,T
                texture.spriteBorder = new Vector4(ltrb.x, ltrb.w, ltrb.z, ltrb.y);
                texture.textureCompression = TextureImporterCompression.Uncompressed;
            }
            else
            {
                texture.textureType = TextureImporterType.Default;
                // Edge fidelity for UI chrome and card frames.
                bool chrome = assetPath.IndexOf("/Foundry/", StringComparison.OrdinalIgnoreCase) >= 0
                    || assetPath.IndexOf("/Identity/", StringComparison.OrdinalIgnoreCase) >= 0
                    || assetPath.IndexOf("/Campaign/", StringComparison.OrdinalIgnoreCase) >= 0;
                texture.textureCompression = chrome
                    ? TextureImporterCompression.Uncompressed
                    : TextureImporterCompression.CompressedHQ;
            }
        }

        private static bool IsArtPath(string path)
        {
            for (int i = 0; i < TextureRoots.Length; i++)
                if (path.StartsWith(TextureRoots[i], StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}
