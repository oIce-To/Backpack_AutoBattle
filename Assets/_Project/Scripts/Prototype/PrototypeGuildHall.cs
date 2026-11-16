using UnityEngine;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    // Keep the illustrated environment behind the live character and all interactive UI.
    public static class PrototypeGuildHall
    {
        internal static string StageResource(bool isOriginMine) => isOriginMine ? "UI/OriginMineSelection" : "UI/DungeonSelection";

        internal static Image Add(RectTransform parent, string resource = "UI/GuildHallIllustrated", float darkness = 0f,
            string prefix = "Guild Hall")
        {
            var clip = new GameObject(prefix + " Backdrop", typeof(RectTransform), typeof(RectMask2D));
            clip.transform.SetParent(parent, false);
            BackpackPrototypeController.Stretch((RectTransform)clip.transform);
            var illustration = SelectionUi.Image(clip.transform, prefix + " Illustration", Color.white);
            illustration.raycastTarget = false;
            BackpackPrototypeController.Stretch(illustration.rectTransform);
            // Cover the panel without distorting the source pixel-art aspect ratio.
            var aspect = illustration.gameObject.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            SetIllustration(illustration, resource);
            if (darkness > 0)
            {
                var shade = SelectionUi.Image(clip.transform, prefix + " Shade", new Color(0, 0, 0, Mathf.Clamp01(darkness)));
                BackpackPrototypeController.Stretch(shade.rectTransform);
            }
            return illustration;
        }

        internal static void SetIllustration(Image illustration, string resource)
        {
            illustration.sprite = Resources.Load<Sprite>(resource);
            illustration.GetComponent<AspectRatioFitter>().aspectRatio = illustration.sprite.rect.width / illustration.sprite.rect.height;
        }
    }
}
