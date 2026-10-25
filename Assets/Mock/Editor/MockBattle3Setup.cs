using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DartsRoguelike.Mock.Editor
{
    /// <summary>
    ///     Mock1を元に3段階照準の試作シーンを用意する。
    /// </summary>
    public static class MockBattle3Setup
    {
        private const string SOURCE_SCENE = "Assets/Mock/Scenes/MockBattle.unity";
        private const string SCENE_PATH = "Assets/Mock/Scenes/MockBattle3.unity";

        /// <summary>
        ///     Mock3シーンを用意して開く。
        /// </summary>
        [MenuItem("Mock/Open Three Stage Aim Prototype")]
        public static void Open()
        {
            if (EditorApplication.isPlaying)
            {
                throw new InvalidOperationException("Stop Play Mode first.");
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).isDirty)
                {
                    throw new InvalidOperationException("Save your current scene before opening MockBattle3.");
                }
            }

            CreateScene();
            EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
        }

        /// <summary>
        ///     Mock1を複製してMock3シーンを作成する。
        /// </summary>
        [MenuItem("Mock/Create Three Stage Aim Scene")]
        public static void CreateScene()
        {
            if (!File.Exists(SOURCE_SCENE))
            {
                throw new FileNotFoundException("Create MockBattle first.", SOURCE_SCENE);
            }

            if (!File.Exists(SCENE_PATH) && !AssetDatabase.CopyAsset(SOURCE_SCENE, SCENE_PATH))
            {
                throw new InvalidOperationException("Could not copy MockBattle scene.");
            }

            AssetDatabase.ImportAsset(SCENE_PATH, ImportAssetOptions.ForceSynchronousImport);
            MockJapaneseFont.Build();
            Debug.Log("[MockBattle3Setup] Three stage aim scene ready: " + SCENE_PATH);
        }

        /// <summary>
        ///     3段階照準が順番どおりに確定されることを確認する。
        /// </summary>
        [MenuItem("Mock/Run Three Stage Aim Checks")]
        public static string RunChecks()
        {
            var passed = new List<string>();
            Action<string, bool> check = (name, isPassed) =>
            {
                if (!isPassed)
                {
                    throw new Exception("FAILED: " + name);
                }

                passed.Add(name);
            };

            var aim = new MockThreeStageAim(.5f, .16f, .06f);
            Vector2 pointer = new Vector2(.25f, -.4f);
            aim.Update(pointer, 0f, false, false, false);
            check("coarse range follows pointer", aim.CoarseAim == pointer && !aim.IsInProgress);

            bool shouldThrow = aim.Advance(pointer, 1f, false, false, false);
            check("first click starts drawing", !shouldThrow && aim.IsDrawing && aim.IsInProgress);

            aim.Update(pointer, 1.5f, false, false, false);
            check(
                "normal movement keeps a constant radius",
                Mathf.Approximately(Vector2.Distance(aim.CoarseAim, aim.Aim), .16f));
            Vector2 normalAim = aim.Aim;
            aim.Update(pointer, 1.5f, true, false, false);
            check("tremor disturbs normal movement", Vector2.Distance(normalAim, aim.Aim) > .01f);

            aim.Update(new Vector2(-.8f, .8f), 1.8f, true, false, false);
            check("draw center remains in coarse range", Vector2.Distance(aim.CoarseAim, aim.Aim) <= aim.CoarseRadius);
            Vector2 fixedCenter = aim.Aim;

            shouldThrow = aim.Advance(pointer, 1.8f, true, false, false);
            check("second click fixes center", !shouldThrow && aim.IsSelectingSpread);
            aim.Update(pointer, 2.2f, true, false, false);
            check("fixed center does not move", aim.Aim == fixedCenter);

            shouldThrow = aim.Advance(pointer, 2.2f, true, false, false);
            check("third click throws", shouldThrow);

            string summary = passed.Count + " three stage aim checks passed\n" + string.Join("\n", passed);
            Debug.Log("[MockBattle3Setup] " + summary);
            return summary;
        }
    }
}
