using System;

namespace Wolfsun
{
    public static class ViewportLayout
    {
        public const int Width = 1920;
        public const int Height = 1080;

        // Center-crop the artwork to fill the viewport at any aspect ratio without distorting it.
        public static (float X, float Y, float Width, float Height) ArtworkCrop(float viewportWidth, float viewportHeight, int textureWidth, int textureHeight)
        {
            if (viewportWidth <= 0 || viewportHeight <= 0 || textureWidth <= 0 || textureHeight <= 0)
                return (0, 0, 1, 1);
            float aspect = viewportWidth / viewportHeight;
            float source = (float)textureWidth / textureHeight;
            float width = Math.Min(1f, aspect / source);
            float height = Math.Min(1f, source / aspect);
            return ((1f - width) * .5f, (1f - height) * .5f, width, height);
        }
    }
}
