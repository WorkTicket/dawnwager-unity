using UnityEngine;

namespace Wolfsun
{
    public sealed partial class Game
    {
        private RectTransform previewCard;
        private RectTransform previewSource;

        private void ClearCardPreview()
        {
            if (previewCard) Destroy(previewCard.gameObject);
            previewCard = null;
            previewSource = null;
        }
    }
}
