using UnityEngine;
using UnityEngine.UI;

namespace Wolfsun
{
    public sealed partial class Game
    {
        private const string FullscreenPreference = "dawnwager.fullscreen";
        private const string Identity = "Identity/";
        private RawImage sceneArtwork;
        private RectTransform viewport;
        private Vector2 viewportSize;
        private bool fullscreen;
        private readonly System.Collections.Generic.List<RectTransform> viewportShades = new System.Collections.Generic.List<RectTransform>();

        private void CreateBackdrop(Transform canvas)
        {
            viewport = (RectTransform)canvas;
            var holder = Box(canvas, "Full viewport artwork", 0, 0, 0, 0);
            holder.anchorMin = Vector2.zero;
            holder.anchorMax = Vector2.one;
            holder.offsetMin = holder.offsetMax = Vector2.zero;
            sceneArtwork = holder.gameObject.AddComponent<RawImage>();
            sceneArtwork.raycastTarget = false;
            sceneArtwork.color = Ink;
        }

        private void UpdateBackdrop()
        {
            string art;
            switch (page)
            {
                case Page.Home: art = "Foundry/TitleKey"; break;
                case Page.Story: art = "Foundry/RagnarokMap"; break;
                case Page.Battle: art = "Board"; break;
                case Page.Outcome: art = match != null && match.Winner == 0 ? "Foundry/OutcomeVictory" : match != null && match.Winner == 2 ? "Foundry/OutcomeDraw" : "Foundry/OutcomeDefeat"; break;
                case Page.Play: art = "Foundry/Emberbridge"; break;
                case Page.Collection: art = "Foundry/DawnPass"; break;
                default: art = "Foundry/DawnPass"; break;
            }
            var tex = Texture(art);
            if (tex == null && art != "Foundry/DawnPass") tex = Texture("Foundry/DawnPass");
            sceneArtwork.texture = tex;
            sceneArtwork.color = tex != null ? (page == Page.Battle ? new Color(.82f, .82f, .82f, 1f) : Color.white) : Ink;
            viewportSize = Vector2.zero;
            if (viewport) ResizeArtwork();
        }

        private void LateUpdate()
        {
            if (viewport && viewportSize != viewport.rect.size) ResizeArtwork();
        }

        private void ResizeArtwork()
        {
            viewportSize = viewport.rect.size;
            var tex = sceneArtwork.texture;
            if (tex == null || viewportSize.y <= 0) { sceneArtwork.uvRect = new Rect(0, 0, 1, 1); return; }
            float aspect = viewportSize.x / viewportSize.y, src = (float)tex.width / tex.height;
            if (src > aspect) { float crop = aspect / src; sceneArtwork.uvRect = new Rect((1 - crop) / 2, 0, crop, 1); }
            else { float crop = src / aspect; sceneArtwork.uvRect = new Rect(0, (1 - crop) / 2, 1, crop); }
        }

        private RectTransform ViewportShade(string name, Color color) =>
            ColorBox(root, name, 0, 0, 1920, 1080, color, true);

        private void SetDisplayMode(bool value)
        {
            fullscreen = value;
            if (Application.isEditor) return;
            var current = Screen.currentResolution;
            int width = value ? current.width : Mathf.Min(1600, Mathf.RoundToInt(current.width * .86f));
            int height = value ? current.height : Mathf.Min(900, Mathf.RoundToInt(current.height * .82f));
            Screen.SetResolution(width, height, value ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        }

        private void ToggleFullscreen()
        {
            if (Application.isEditor) return;
            SetDisplayMode(!Screen.fullScreen);
            PlayerPrefs.SetInt(FullscreenPreference, fullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
