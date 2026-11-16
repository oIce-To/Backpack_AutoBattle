using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BackpackAutoBattle.Prototype;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BackpackAutoBattle.Tests.Prototype
{
    public sealed class PrototypePreparationPixelTests
    {
        private string _previousProfile, _profile;

        [SetUp]
        public void IsolateProfile()
        {
            _previousProfile = Environment.GetEnvironmentVariable("BACKPACK_PROFILE_PATH");
            _profile = Path.Combine(Path.GetTempPath(), "BackpackPixelUi_" + Guid.NewGuid().ToString("N") + ".json");
            Environment.SetEnvironmentVariable("BACKPACK_PROFILE_PATH", _profile);
        }

        [TearDown]
        public void RestoreProfile()
        {
            Environment.SetEnvironmentVariable("BACKPACK_PROFILE_PATH", _previousProfile);
            foreach (string suffix in new[] { "", ".bak", ".tmp", ".settings.json" })
                if (File.Exists(_profile + suffix)) File.Delete(_profile + suffix);
        }

        [UnityTest]
        public IEnumerator Preparation_PressedButtonKeepsHitArea_AndEnemyModalBlocksBackground()
        {
            yield return SceneManager.LoadSceneAsync("Prototype_Backpack", LoadSceneMode.Single);
            yield return null;
            var controller = Object.FindFirstObjectByType<BackpackPrototypeController>();
            var canvas = controller.GetComponentInChildren<Canvas>();
            Transform Find(string name) => controller.GetComponentsInChildren<Transform>(true).Single(t => t.name == name);
            var portrait = Find("Preparation Character Illustration").GetComponent<Image>();
            Assert.That(portrait.sprite, Is.SameAs(controller.SelectedCharacter.Portrait));
            Canvas.ForceUpdateCanvases();
            foreach (var trim in controller.GetComponentsInChildren<PrototypeFantasyTrim>())
            {
                Assert.That(trim.raycastTarget, Is.False);
                Assert.That(trim.canvasRenderer.GetMesh().vertexCount, Is.GreaterThan(0), "Bag and shop decoration must render.");
            }
            PrototypeScreenCapture.CaptureIfRequested(canvas, "PixelPreparation.png");

            var start = Find("Start Battle Button").GetComponent<Button>();
            var caption = start.GetComponentInChildren<Text>();
            Vector2 captionPosition = caption.rectTransform.anchoredPosition;
            var cornersBefore = new Vector3[4];
            start.image.rectTransform.GetWorldCorners(cornersBefore);
            var pointer = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, start.transform.position)
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits.First().gameObject, Is.SameAs(start.gameObject));
            ExecuteEvents.Execute(start.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            Assert.That(caption.rectTransform.anchoredPosition.y, Is.LessThan(captionPosition.y));
            var cornersPressed = new Vector3[4];
            start.image.rectTransform.GetWorldCorners(cornersPressed);
            Assert.That(cornersPressed, Is.EqualTo(cornersBefore), "Pressing must not move the button's hit area.");
            PrototypeScreenCapture.CaptureIfRequested(canvas, "PixelButtonPressed.png");
            ExecuteEvents.Execute(start.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            Assert.That(caption.rectTransform.anchoredPosition, Is.EqualTo(captionPosition));

            var info = Find("Enemy Info Button").gameObject;
            ExecuteEvents.Execute(info, pointer, ExecuteEvents.pointerClickHandler);
            yield return null;
            Assert.That(controller.IsEnemyPreviewOpen, Is.True);
            Assert.That(start.IsInteractable(), Is.False);
            hits.Clear(); EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits.First().gameObject.name, Is.EqualTo("Enemy Preview Popup"));
            ExecuteEvents.Execute(start.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(controller.Phase, Is.EqualTo(PrototypePhase.Preparation));
            PrototypeScreenCapture.CaptureIfRequested(canvas, "PixelEnemyPreview.png");
            ExecuteEvents.Execute(Find("Enemy Preview Popup Close").gameObject, pointer, ExecuteEvents.pointerClickHandler);
            yield return null;
            Assert.That(controller.IsEnemyPreviewOpen, Is.False);
            Assert.That(start.IsInteractable(), Is.True);
            controller.BattleController.AutoTick = false;
            ExecuteEvents.Execute(start.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(controller.Phase, Is.EqualTo(PrototypePhase.Battle));
        }
    }
}
