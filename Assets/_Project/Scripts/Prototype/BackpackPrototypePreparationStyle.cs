using UnityEngine;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    public sealed partial class BackpackPrototypeController
    {
        private Text _preparationStage;

        private void BuildPreparationHeader()
        {
            Image header = CreateImage(_shopScreenRoot, "Preparation Header", PrototypePixelSkin.Hex(0x1c2b33));
            SetRect(header.rectTransform, new Vector2(.5f, 1), new Vector2(0, -36), new Vector2(1280, 72));
            header.raycastTarget = false;
            Image rule = CreateImage(header.transform, "Header Brass Rule", PrototypePixelSkin.Hex(0x8b754d));
            SetRect(rule.rectTransform, new Vector2(.5f, 0), Vector2.zero, new Vector2(1204, 2));
            rule.raycastTarget = false;
            var title = CreateText(header.transform, "Preparation Title", "전투 준비", 25, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(title.rectTransform, new Vector2(0, .5f), new Vector2(116, 8), new Vector2(156, 38));
            title.color = PrototypePixelSkin.Ink;
            _preparationStage = CreateText(header.transform, "Preparation Stage", "", 12, FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(_preparationStage.rectTransform, new Vector2(0, .5f), new Vector2(208, -19), new Vector2(340, 22));
            _preparationStage.color = PrototypePixelSkin.Muted;
        }
    }
}
