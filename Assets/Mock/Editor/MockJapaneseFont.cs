using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DartsRoguelike.Mock.Editor
{
    public static class MockJapaneseFont
    {
        public const string AssetPath = "Assets/Mock/Art/Fonts/MockJapanese.asset";
        [MenuItem("Mock/日本語フォントを作成")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath) != null) return;
            var source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Mock/Art/Fonts/NotoSansCJKjp-Regular.otf");
            if (source == null) throw new InvalidOperationException("Noto Sans CJK JP is missing.");
            var font = TMP_FontAsset.CreateFontAsset(source, 48, 5,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic);
            if (font == null) throw new InvalidOperationException("Cannot create Japanese font.");
            font.name = "MockJapanese";
            string corpus = File.ReadAllText("Assets/Mock/Runtime/MockBattle.cs") +
                File.ReadAllText("Assets/Mock/Runtime/BattleModel.cs");
            string characters = new string(corpus.Where(c => !char.IsControl(c)).Distinct().OrderBy(c => c).ToArray());
            string missing;
            if (!font.TryAddCharacters(characters, out missing))
                throw new InvalidOperationException("Missing glyphs: " + missing);
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            font.isMultiAtlasTexturesEnabled = false;
            AssetDatabase.CreateAsset(font, AssetPath);
            foreach (var atlas in font.atlasTextures)
            {
                atlas.name = "MockJapanese Atlas";
                AssetDatabase.AddObjectToAsset(atlas, font);
                EditorUtility.SetDirty(atlas);
            }
            font.material.name = "MockJapanese Material";
            font.material.mainTexture = font.atlasTexture;
            AssetDatabase.AddObjectToAsset(font.material, font);
            EditorUtility.SetDirty(font.material);
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssetIfDirty(font);
            if (!AssetDatabase.LoadAllAssetsAtPath(AssetPath).Any(a => a is Material))
                throw new InvalidOperationException("Font material was not saved.");
            Debug.Log("Japanese font ready: " + characters.Length + " glyphs.");
        }
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != "Assets/Mock/Scenes/MockBattle.unity")
                throw new InvalidOperationException("Open the Mock scene first.");
            Build();
            var battle = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MockBattle>(true)).Single();
            var data = new SerializedObject(battle);
            data.FindProperty("font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath);
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
        }
    }
}
