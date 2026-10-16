using System;
using System.IO;
using BackpackAutoBattle.Prototype;
using TMPro;
using UnityEditor;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using UnityEngine;
using UnityEngine.TextCore;
using UnityEngine.TextCore.LowLevel;

namespace BackpackAutoBattle.Editor
{
    public static class PrototypeTooltipTextBuilder
    {
        private const string Root = "Assets/_Project/Resources/Tooltip";
        private const string FontPath = "Assets/_Project/Fonts/NotoSansKR-Regular.ttf";
        private static bool _exitAfterBuild;

        public static void BuildForBatch()
        {
            _exitAfterBuild = true;
            Build();
        }

        [MenuItem("Tools/Backpack AutoBattle/Build Tooltip Text Assets")]
        public static void Build()
        {
            // Import Unity's official shaders/settings once; never overwrite project TMP settings.
            if (Resources.Load<TMP_Settings>("TMP Settings") == null)
            {
                PackageInfo package = PackageInfo.FindForAssembly(typeof(TMP_Text).Assembly);
                AssetDatabase.importPackageCompleted += OnImported;
                AssetDatabase.importPackageFailed += OnImportFailed;
                AssetDatabase.ImportPackage(Path.Combine(package.resolvedPath, "Package Resources/TMP Essential Resources.unitypackage"), false);
                return; // ImportPackage is asynchronous: create font assets only after the callback.
            }
            BuildReadyAssets();
        }

        private static void OnImported(string packageName)
        {
            AssetDatabase.importPackageCompleted -= OnImported;
            AssetDatabase.importPackageFailed -= OnImportFailed;
            try { BuildReadyAssets(); }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (_exitAfterBuild) EditorApplication.Exit(1);
            }
        }

        private static void OnImportFailed(string packageName, string error)
        {
            AssetDatabase.importPackageCompleted -= OnImported;
            AssetDatabase.importPackageFailed -= OnImportFailed;
            Debug.LogError($"Could not import TMP Essential Resources: {error}");
            if (_exitAfterBuild) EditorApplication.Exit(1);
        }

        private static void BuildReadyAssets()
        {
            Directory.CreateDirectory(Root);
            AssetDatabase.Refresh();
            TMP_Settings.LoadDefaultSettings();
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{Root}/Tooltip Korean SDF.asset");
            if (font == null)
            {
                Font source = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
                if (source == null) throw new InvalidOperationException($"Missing bundled Korean font: {FontPath}");
                font = TMP_FontAsset.CreateFontAsset(source, 48, 5, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
                if (font == null) throw new InvalidOperationException("Could not create the Korean tooltip font asset.");
                font.name = "Tooltip Korean SDF";
                font.material.name = "Tooltip Korean SDF Material";
                font.atlasTextures[0].name = "Tooltip Korean SDF Atlas";
                AssetDatabase.CreateAsset(font, $"{Root}/Tooltip Korean SDF.asset");
                AssetDatabase.AddObjectToAsset(font.material, font);
                AssetDatabase.AddObjectToAsset(font.atlasTextures[0], font);
            }
            if (AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>($"{Root}/Tooltip Icons.asset") == null)
                BuildIcons();

            // Keep Korean words together when wrapping, matching the accepted tooltip preview.
            TMP_Settings.useModernHangulLineBreakingRules = true;
            EditorUtility.SetDirty(TMP_Settings.instance);
            AssetDatabase.SaveAssets();
            Debug.Log("Tooltip inline sprite and Korean font assets are ready.");
            if (_exitAfterBuild) EditorApplication.Exit(0);
        }

        private static void BuildIcons()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Placement Diamond Atlas" };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // Supersampled outline, transparent interior; yellow is part of the sprite, not text tint.
                float coverage = 0f;
                for (int sy = 0; sy < 4; sy++)
                for (int sx = 0; sx < 4; sx++)
                {
                    float distance = Mathf.Abs(x + (sx + 0.5f) / 4f - size / 2f)
                        + Mathf.Abs(y + (sy + 0.5f) / 4f - size / 2f);
                    if (distance >= 20f && distance <= 28f) coverage += 1f / 16f;
                }
                pixels[y * size + x] = new Color(1f, 0.88f, 0f, coverage);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            var sprites = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
            sprites.name = "Tooltip Icons";
            var serialized = new SerializedObject(sprites);
            serialized.FindProperty("m_Version").stringValue = "1.1.0";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            sprites.spriteSheet = texture;
            var material = new Material(Shader.Find("TextMeshPro/Sprite")) { name = "Tooltip Icons Material", mainTexture = texture };
            sprites.material = material;
            var glyph = new TMP_SpriteGlyph(0, new GlyphMetrics(size, size, 0f, 54f, size), new GlyphRect(0, 0, size, size), 1f, 0);
            sprites.spriteGlyphTable.Add(glyph);
            sprites.spriteCharacterTable.Add(new TMP_SpriteCharacter(0xE000, sprites, glyph) { name = ItemTooltipContent.PlacementIconName });
            sprites.UpdateLookupTables();
            AssetDatabase.CreateAsset(sprites, $"{Root}/Tooltip Icons.asset");
            AssetDatabase.AddObjectToAsset(texture, sprites);
            AssetDatabase.AddObjectToAsset(material, sprites);
        }
    }
}
