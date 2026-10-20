using System;
using System.Collections.Generic;
using BackpackAutoBattle.Inventory;
using BackpackAutoBattle.Items;
using UnityEngine;
using UnityEngine.UI;

namespace BackpackAutoBattle.Prototype
{
    /// <summary>Presentation copies only. Inventory is committed once before this animation starts.</summary>
    public sealed class PrototypeFusionAnimation : IDisposable
    {
        public readonly struct Material
        {
            public readonly ItemData Data;
            public readonly ItemRotation Rotation;
            public readonly Vector2 Position;
            public Material(ItemData data, ItemRotation rotation, Vector2 position)
            { Data = data; Rotation = rotation; Position = position; }
        }
        public sealed class Job
        {
            public readonly Material[] Materials;
            public readonly ItemData Result;
            public readonly ItemRotation Rotation;
            public readonly Vector2 Destination;
            public readonly PrototypeStorageItemView StoredView;
            public Job(Material[] materials, ItemData result, ItemRotation rotation, Vector2 destination, PrototypeStorageItemView storedView)
            { Materials = materials; Result = result; Rotation = rotation; Destination = destination; StoredView = storedView; }
        }
        private sealed class Visual
        {
            public Job Job;
            public RectTransform[] Materials;
            public CanvasGroup[] MaterialGroups;
            public RectTransform Result;
            public CanvasGroup ResultGroup;
            public PrototypeFusionGlow Glow;
            public Text Title;
            public float Delay;
        }
        public const float JobDuration = 1.32f;
        private readonly RectTransform _root;
        private readonly List<Visual> _visuals = new();
        private float _elapsed;
        public float Duration { get; }

        public PrototypeFusionAnimation(Transform parent, IReadOnlyList<Job> jobs)
        {
            var obj = new GameObject("Fusion Presentation", typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            _root = (RectTransform)obj.transform;
            BackpackPrototypeController.Stretch(_root);
            // A full-screen raycast blocker complements phase guards, including keyboard-driven actions.
            obj.GetComponent<Image>().color = new Color(0f, 0f, 0f, .12f);
            for (int i = 0; i < jobs.Count; i++)
            {
                Job job = jobs[i];
                var visual = new Visual { Job = job, Delay = i * Mathf.Min(.12f, .4f / Mathf.Max(1, jobs.Count - 1)),
                    Materials = new RectTransform[job.Materials.Length], MaterialGroups = new CanvasGroup[job.Materials.Length] };
                for (int m = 0; m < job.Materials.Length; m++)
                {
                    var material = job.Materials[m];
                    visual.Materials[m] = CreateShape($"Fusion Material {i}:{m}", material.Data, material.Rotation, material.Position, out visual.MaterialGroups[m]);
                }
                var glowObject = new GameObject("Fusion Glow", typeof(RectTransform), typeof(PrototypeFusionGlow));
                glowObject.transform.SetParent(_root, false);
                visual.Glow = glowObject.GetComponent<PrototypeFusionGlow>();
                visual.Glow.raycastTarget = false;
                BackpackPrototypeController.SetRect(visual.Glow.rectTransform, Vector2.one * .5f, job.Materials[0].Position, Vector2.one * 120f);
                visual.Result = CreateShape($"Fusion Result {i}", job.Result, job.Rotation, job.Materials[0].Position, out visual.ResultGroup);
                visual.Title = BackpackPrototypeController.CreateText(_root, "Fusion Success", "합성!", 22, FontStyle.Bold, TextAnchor.MiddleCenter);
                visual.Title.raycastTarget = false;
                visual.Title.gameObject.AddComponent<Outline>().effectColor = new Color(.05f, .06f, .09f, .95f);
                _visuals.Add(visual);
            }
            Duration = JobDuration + (jobs.Count > 0 ? _visuals[_visuals.Count - 1].Delay : 0);
            Render();
        }

        private RectTransform CreateShape(string name, ItemData data, ItemRotation rotation, Vector2 position, out CanvasGroup group)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
            obj.transform.SetParent(_root, false);
            var rect = (RectTransform)obj.transform;
            BackpackPrototypeController.SetRect(rect, Vector2.one * .5f, position, Vector2.zero);
            PrototypeItemShapeVisual.Rebuild(rect, data, rotation);
            group = obj.GetComponent<CanvasGroup>(); group.blocksRaycasts = false; group.interactable = false;
            foreach (var graphic in obj.GetComponentsInChildren<Graphic>()) graphic.raycastTarget = false;
            return rect;
        }

        public bool Tick(float deltaSeconds)
        {
            if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds) || deltaSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            _elapsed += deltaSeconds;
            Render();
            return _elapsed >= Duration;
        }

        private void Render()
        {
            foreach (var visual in _visuals)
            {
                float time = _elapsed - visual.Delay;
                Vector2 center = visual.Job.Materials[0].Position;
                float gather = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.08f, .42f, time));
                for (int i = 0; i < visual.Materials.Length; i++)
                {
                    visual.Materials[i].anchoredPosition = Vector2.Lerp(visual.Job.Materials[i].Position, center, gather);
                    visual.Materials[i].localScale = Vector3.one * Mathf.Lerp(1f, i == 0 ? .86f : .18f, gather);
                    visual.MaterialGroups[i].alpha = time < .42f ? 1f - gather * (i == 0 ? .15f : .7f) : 0f;
                }
                bool arriving = time >= 1.15f;
                if (visual.Job.StoredView != null) visual.Job.StoredView.gameObject.SetActive(arriving);
                float reveal = Mathf.InverseLerp(.42f, .64f, time);
                float overshoot = 1f + 2.70158f * Mathf.Pow(reveal - 1f, 3f) + 1.70158f * Mathf.Pow(reveal - 1f, 2f);
                float flight = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.66f, 1.15f, time));
                Vector2 control = (center + visual.Job.Destination) * .5f + Vector2.up * 62f;
                Vector2 position = (1 - flight) * (1 - flight) * center + 2 * (1 - flight) * flight * control
                    + flight * flight * visual.Job.Destination;
                visual.Result.anchoredPosition = position;
                visual.Result.localScale = Vector3.one * Mathf.LerpUnclamped(.6f, 1f, overshoot);
                visual.ResultGroup.alpha = time >= .42f && !arriving ? 1f : 0f;
                float pulse = time < .42f ? .10f * gather : time < .72f ? .7f * (1 - Mathf.InverseLerp(.42f, .72f, time))
                    : arriving ? .48f * (1 - Mathf.InverseLerp(1.15f, JobDuration, time)) : .08f;
                visual.Glow.rectTransform.anchoredPosition = position;
                visual.Glow.rectTransform.sizeDelta = Vector2.one * (arriving ? Mathf.Lerp(80, 130, Mathf.InverseLerp(1.15f, JobDuration, time)) : 130);
                visual.Glow.color = new Color(1f, .86f, .24f, pulse);
                visual.Title.color = new Color(1f, .9f, .4f, time >= .42f && time < .76f ? 1f - flight : 0f);
                BackpackPrototypeController.SetRect(visual.Title.rectTransform, Vector2.one * .5f,
                    center + Vector2.up * (visual.Result.sizeDelta.y * .5f + 22f), new Vector2(180, 32));
            }
        }

        public void Dispose()
        {
            foreach (var visual in _visuals)
                if (visual.Job.StoredView != null) visual.Job.StoredView.gameObject.SetActive(true);
            if (_root != null) { _root.gameObject.SetActive(false); UnityEngine.Object.Destroy(_root.gameObject); }
        }
    }
}
