using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Wolfsun
{
    public sealed partial class Game
    {
        private readonly Dictionary<string, Sprite> foundrySprites = new Dictionary<string, Sprite>();

        private Sprite FoundrySprite(string name, Vector4? border = null)
        {
            string key = name + (border.HasValue ? border.Value.ToString() : "");
            if (foundrySprites.TryGetValue(key, out var sprite) && sprite != null && sprite.texture != null)
                return sprite;
            foundrySprites.Remove(key);
            var art = Texture(Foundry + name);
            if (art == null) return null;
            var pad = border ?? new Vector4(17, 17, 17, 17);
            sprite = Sprite.Create(art, new Rect(0, 0, art.width, art.height), new Vector2(.5f, .5f),
                100, 0, SpriteMeshType.FullRect, pad);
            foundrySprites[key] = sprite;
            return sprite;
        }

        private bool HasFoundry(string name) => Texture(Foundry + name) != null;

        private string RankMedalPath(int absolute)
        {
            if (absolute >= Ladder.LegendOrdinal) return "Rank-Legend";
            Ladder.FromAbsolute(absolute, out int league, out int rank);
            return "Rank-" + Ladder.Leagues[league] + rank;
        }
    }
}
