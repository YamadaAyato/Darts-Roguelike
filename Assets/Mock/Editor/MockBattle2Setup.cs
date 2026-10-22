using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DartsRoguelike.Mock.Editor
{
    public static class MockBattle2Setup
    {
        const string SourceScene = "Assets/Mock/Scenes/MockBattle.unity";
        const string ScenePath = "Assets/Mock/Scenes/MockBattle2.unity";

        [MenuItem("Mock/Open Line Battle Prototype")]
        public static void Open()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save your current scene before opening MockBattle2.");
            CreateScene();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [MenuItem("Mock/Create Line Battle Scene")]
        public static void CreateScene()
        {
            if (!File.Exists(SourceScene)) throw new FileNotFoundException("Create MockBattle first.", SourceScene);
            if (!File.Exists(ScenePath))
            {
                if (!AssetDatabase.CopyAsset(SourceScene, ScenePath))
                    throw new InvalidOperationException("Could not copy MockBattle scene.");
            }
            AssetDatabase.ImportAsset(ScenePath, ImportAssetOptions.ForceSynchronousImport);
            MockJapaneseFont.Build();
            Debug.Log("Line battle scene ready: " + ScenePath);
        }

        [MenuItem("Mock/Run Line Battle Checks")]
        public static string RunChecks()
        {
            var passed = new List<string>();
            Action<string,bool> check = (name, ok) =>
            {
                if (!ok) throw new Exception("FAILED: " + name);
                passed.Add(name);
            };

            int emptyCells = 0, sampledCells = 0;
            for (int seed = 0; seed < 40; seed++)
            {
                var sample = new LinkBattleModel(seed);
                for (int sector = 0; sector < 20; sector++)
                    for (int band = 0; band < 4; band++)
                    {
                        sampledCells++;
                        if (sample.Board[sector].Cards[band] == CardKind.Empty) emptyCells++;
                    }
            }
            check("empty cells are the majority", emptyCells * 100 / sampledCells >= 55);

            var model = EmptyModel(100);
            check("starts with three darts", model.Remaining == 3);
            model.Throw(new Vector2(-.8f, 0), 0);
            check("first dart creates an anchor", model.Points.Count == 1 && model.Segments.Count == 0);
            check("first dart still scores", model.TurnScore > 0 && model.Remaining == 2);
            int before = model.EnemyHp;
            model.Throw(new Vector2(.8f, 0), 1);
            check("second dart completes line one", model.Points.Count == 2 && model.Segments.Count == 1);
            check("line crosses cells", model.Segments[0].ActivatedCells > 0 && model.ActivatedThisTurn.Count > 0);
            check("line has base damage", model.EnemyHp < before);
            model.Throw(new Vector2(0, .8f), 2);
            check("third dart completes line two", model.Segments.Count == 2);
            check("second line is double power", model.Segments[1].Power == 2);

            model = EmptyModel(101);
            model.Throw(new Vector2(1.2f, 0), 0);
            check("miss spends dart without anchor", model.Remaining == 2 && model.Points.Count == 0);

            model = EmptyModel(102);
            model.DangerCells.Clear();
            model.DangerCells.Add(80);
            model.Throw(new Vector2(-.8f, 0), 0);
            model.Throw(new Vector2(.8f, 0), 2);
            check("line triggers crossed danger", model.TriggeredDanger.Contains(80) && model.Hp == 63);

            model = EmptyModel(103);
            model.Board[20].Cards[0] = CardKind.Strike;
            model.Throw(new Vector2(-.8f, 0), 0);
            model.Throw(new Vector2(.8f, 0), 1);
            check("line activates crossed card", model.ActivatedThisTurn.Contains(80) && model.EnemyHp <= 139);
            model.EndTurn();
            check("turn clears the drawn route", model.Points.Count == 0 && model.Segments.Count == 0 && model.Remaining == 3);

            string summary = passed.Count + " line battle checks passed\n" + string.Join("\n", passed);
            Debug.Log(summary);
            return summary;
        }

        static LinkBattleModel EmptyModel(int seed)
        {
            var model = new LinkBattleModel(seed);
            model.DangerCells.Clear();
            for (int sector = 0; sector < model.Board.Length; sector++)
                for (int band = 0; band < 4; band++) model.Board[sector].Cards[band] = CardKind.Empty;
            return model;
        }

        // Entry point used by a headless Unity verification run.
        public static void BuildAndCheckBatch()
        {
            CreateScene();
            RunChecks();
            AssetDatabase.SaveAssets();
        }

        // Constructs the real runtime component in a temporary hierarchy and verifies its UI.
        public static void UiSmokeBatch()
        {
            CreateScene();
            GameObject root = null;
            try
            {
                MockJapaneseFont.Build();
                MockSetup.EnsureMaterial();
                root = new GameObject("MockBattle2SmokeRoot");
                var source = root.AddComponent<MockBattle>();
                var data = new SerializedObject(source);
                data.FindProperty("font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(MockJapaneseFont.AssetPath);
                data.FindProperty("uiMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/Mock/Art/MockUI.mat");
                data.ApplyModifiedPropertiesWithoutUndo();
                var battle = root.AddComponent<MockBattle2>();
                if (battle.Model == null)
                {
                    var awake = typeof(MockBattle2).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    awake.Invoke(battle, null);
                }
                if (battle == null || battle.Model == null) throw new Exception("MockBattle2 did not start.");
                if (GameObject.Find("MockBattle2Canvas") == null) throw new Exception("MockBattle2Canvas was not created.");
                if (root.GetComponentInChildren<UnityEngine.EventSystems.EventSystem>(true) == null)
                    throw new Exception("EventSystem was not created.");
                if (UnityEngine.Object.FindAnyObjectByType<LinkBoardGraphic>() == null) throw new Exception("Link board was not created.");
                Debug.Log("MockBattle2 runtime UI smoke check passed.");
            }
            finally { if (root != null) UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
