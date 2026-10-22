using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace DartsRoguelike.Mock.Editor
{
    public static class MockSetup
    {
        const string Root = "Assets/Mock";
        const string ScenePath = Root + "/Scenes/MockBattle.unity";
        [MenuItem("Mock/Open Battle Prototype")]
        public static void Open()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            for (int i=0;i<SceneManager.sceneCount;i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save your current scene before opening Mock.");
            if (!File.Exists(ScenePath)) Build();
            EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
        }

        [MenuItem("Mock/Create Battle Scene")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            if (File.Exists(ScenePath)) throw new InvalidOperationException("Mock scene already exists. Use Mock/Open Battle Prototype.");
            Directory.CreateDirectory(Root+"/Scenes");
            AssetDatabase.Refresh();
            MockJapaneseFont.Build();
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MockJapaneseFont.AssetPath);
            if(font==null) throw new InvalidOperationException("Run MockTextResources.Import first.");
            EnsureMaterial();
            Scene previous=SceneManager.GetActiveScene();
            Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var camera=new GameObject("MockCamera",typeof(Camera));
            camera.GetComponent<Camera>().clearFlags=CameraClearFlags.SolidColor;
            camera.GetComponent<Camera>().backgroundColor=new Color(.035f,.05f,.075f);
            camera.GetComponent<Camera>().orthographic=true;
            camera.transform.position=new Vector3(0,0,-10);
            var root=new GameObject("MockBattle");
            var battle=root.AddComponent<MockBattle>();
            var serialized=new SerializedObject(battle);
            serialized.FindProperty("font").objectReferenceValue=font;
            serialized.FindProperty("uiMaterial").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Art/MockUI.mat");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorSceneManager.CloseScene(scene,true);
            if(previous.IsValid()) SceneManager.SetActiveScene(previous);
            Debug.Log("Mock scene created: "+ScenePath);
        }

        public static void EnsureMaterial()
        {
            if(AssetDatabase.LoadAssetAtPath<Material>(Root+"/Art/MockUI.mat")!=null)return;
            var shader=AssetDatabase.LoadAssetAtPath<Shader>(Root+"/Art/MockUI.shader");
            if(shader==null)throw new InvalidOperationException("Missing MockUI shader");
            AssetDatabase.CreateAsset(new Material(shader),Root+"/Art/MockUI.mat");
        }
        public static void WireMaterial()
        {
            if(SceneManager.GetActiveScene().path!=ScenePath)throw new InvalidOperationException("Open Mock scene first.");
            EnsureMaterial();
            var battle=UnityEngine.Object.FindFirstObjectByType<MockBattle>();
            var so=new SerializedObject(battle);
            so.FindProperty("uiMaterial").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Art/MockUI.mat");
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        [MenuItem("Mock/Run Combat Checks")]
        public static string RunChecks()
        {
            var passed=new List<string>();
            Action<string,bool> check=(name,ok)=>{if(!ok)throw new Exception("FAILED: "+name);passed.Add(name);};
            check("20 at top",BattleModel.Evaluate(0,.8f).Score==20);
            check("double 20",BattleModel.Evaluate(0,.97f).Score==40);
            check("triple 20",BattleModel.Evaluate(0,.59f).Score==60);
            check("single bands are distinct",BattleModel.Evaluate(0,.4f).Band==BoardBand.InnerSingle&&
                BattleModel.Evaluate(0,.8f).Band==BoardBand.OuterSingle);
            check("inner bull",BattleModel.Evaluate(0,0).Score==50);
            check("outer bull",BattleModel.Evaluate(.06f,0).Score==25);
            check("outside misses",BattleModel.Evaluate(1.1f,0).Miss);
            check("right sector 6",BattleModel.Evaluate(.8f,0).Score==6);
            var m=new BattleModel(1);
            m.Board[0].Card=CardKind.Strike;m.Board[0].Hazard=HazardKind.None;
            m.Board[19].Cards[1]=CardKind.Empty;m.Board[1].Cards[1]=CardKind.Empty;
            m.Board[19].Hazard=m.Board[1].Hazard=HazardKind.None;
            int hp=m.EnemyHp;
            m.Throw(new Hit(0,3,60),0);
            check("triple own card and fang",m.EnemyHp==hp-11&&m.Score==60);
            check("used dart rejected",!m.Throw(new Hit(0,1,20),0)&&m.Remaining==2);
            m.Throw(new Hit(-1,0,0,true),1);
            check("miss spends dart",m.Remaining==1);
            m.Throw(new Hit(-1,0,0,true),2);
            check("three darts limit",m.Remaining==0&&!m.Throw(new Hit(0,1,20),2));
            m.EndTurn();
            check("turn replenishes darts",m.Turn==2&&m.Remaining==3);
            check("multiple enemy intents",m.Intents.Count==3);
            m=new BattleModel(2);m.Board[0].Card=CardKind.Coin;m.SetHazard(0,HazardKind.Mine,-1,1);
            m.Throw(new Hit(0,2,40),0);
            check("one-use mine",m.Hp==61&&m.Board[0].Hazard==HazardKind.None);
            check("coin from initial only",m.Coins==10&&m.Board[0].Cards[3]!=CardKind.Coin);
            m=new BattleModel(3);m.SetHazard(0,HazardKind.Curse,-1,-1);m.Board[0].Card=CardKind.Strike;
            m.Throw(new Hit(0,1,20),0);
            check("curse survives reroll",m.Board[0].Hazard==HazardKind.Curse&&m.Hp==66);
            m=new BattleModel(4);m.Intents.Clear();m.SetHazard(0,HazardKind.Fire,2,-1);
            m.EndTurn();check("fire survives first turn",m.Board[0].Hazard==HazardKind.Fire&&m.Board[0].Turns==1);
            m.Intents.Clear();m.EndTurn();check("fire expires second turn",m.Board[0].Hazard==HazardKind.None);
            m=new BattleModel(5);m.Hp=1;m.EnemyHp=1;m.Board[0].Card=CardKind.Strike;m.SetHazard(0,HazardKind.Mine,-1,1);
            m.Throw(new Hit(0,1,20),0);
            check("simultaneous death is defeat",m.Phase==BattlePhase.Defeat&&m.EnemyHp==0);
            check("no reroll after death",m.Board[0].Card==CardKind.Strike);
            m=new BattleModel(6);m.EnemyHp=1;m.Board[0].Card=CardKind.Strike;m.Board[0].Hazard=HazardKind.None;
            m.Throw(new Hit(0,1,20),0);
            check("victory locks battle",m.Phase==BattlePhase.Victory&&!m.EndTurn()&&!m.Throw(new Hit(0,1,20),1));
            m=new BattleModel(7);m.Poison=200;int oldHp=m.Hp;m.EndTurn();
            check("poison kill before enemy actions",m.Phase==BattlePhase.Victory&&m.Hp==oldHp);
            m=new BattleModel(8);m.Intents.Clear();m.Intents.Add(new Intent(IntentKind.Attack,10));m.Shield=7;m.EndTurn();
            check("block absorbs attack and expires",m.Hp==67&&m.Shield==0);
            m=new BattleModel(9);m.Hp=69;m.Board[0].Card=CardKind.Heal;m.Board[0].Hazard=HazardKind.None;
            m.Throw(new Hit(0,3,60),1);check("healing clamped",m.Hp==70);
            m=new BattleModel(10);
            for(int i=0;i<30&&m.Phase==BattlePhase.Player;i++) m.EndTurn();
            check("complete defeat path",m.Phase==BattlePhase.Defeat);
            m=new BattleModel(11);
            for(int turn=0;turn<20&&m.Phase==BattlePhase.Player;turn++)
            {
                for(int d=0;d<3&&m.Phase==BattlePhase.Player;d++)
                {
                    int sector=0;int best=-999;
                    for(int j=0;j<20;j++)
                    {
                        int value=0;
                        for(int offset=-1;offset<=1;offset++)
                        {
                            int index=(j+offset+20)%20;
                            CardKind card=m.Board[index].Cards[1];
                            value+=card==CardKind.Strike?8:card==CardKind.Drain?7:
                                card==CardKind.Poison?5:card==CardKind.Heal?3:card==CardKind.Guard?2:0;
                            if(m.Board[index].Hazard!=HazardKind.None)value-=12;
                        }
                        if(value>best){best=value;sector=j;}
                    }
                    m.Throw(new Hit(sector,3,BattleModel.Numbers[sector]*3),d);
                }
                if(m.Phase==BattlePhase.Player)m.EndTurn();
            }
            check("complete victory path",m.Phase==BattlePhase.Victory);
            for (int reward=0;reward<3;reward++)
            {
                BattleModel challenge=null;
                for(int seed=0;seed<2000;seed++)
                {
                    var candidate=new BattleModel(seed);
                    if(candidate.TargetScore==101&&candidate.TargetReward==(TargetRewardKind)reward)
                    { challenge=candidate;break; }
                }
                check("101 target seed "+reward,challenge!=null);
                foreach(int sector in new[]{2,0,12})
                { challenge.Board[sector].Card=CardKind.Guard;challenge.Board[sector].Hazard=HazardKind.None; }
                challenge.Board[1].Cards[1]=challenge.Board[3].Cards[1]=CardKind.Empty;
                challenge.Board[1].Hazard=challenge.Board[3].Hazard=HazardKind.None;
                challenge.Hp=30;challenge.Panic=challenge.Tremor=challenge.Drunk=1;
                challenge.Throw(new Hit(2,3,54),0);
                challenge.Throw(new Hit(0,2,40),1);
                check("target waits for exact score "+reward,challenge.TurnScore==94&&!challenge.TargetAchieved);
                challenge.Throw(new Hit(12,1,7),2);
                bool rewardApplied=reward==0?challenge.EnemyHp==87:
                    reward==1?challenge.Hp==55:
                    challenge.Shield==67&&challenge.Panic==0&&challenge.Tremor==0&&challenge.Drunk==0;
                check("101 exact reward "+reward,challenge.TurnScore==101&&challenge.TargetAchieved&&rewardApplied);
                challenge.Intents.Clear();challenge.EndTurn();
                check("turn target resets "+reward,challenge.Turn==2&&challenge.TurnScore==0&&
                    !challenge.TargetAchieved&&challenge.Score==101);
            }
            m=new BattleModel(20);
            m.Board[19].Cards[1]=m.Board[1].Cards[1]=CardKind.Empty;
            m.Board[19].Hazard=m.Board[1].Hazard=HazardKind.None;
            for(int d=0;d<3;d++)
            {
                m.Board[0].Card=CardKind.Guard;m.Board[0].Hazard=HazardKind.None;
                m.Throw(new Hit(0,3,60),d);
            }
            check("overshoot keeps normal throw results",m.TurnScore==180&&m.Score==180&&
                !m.TargetAchieved&&m.Phase==BattlePhase.Player);
            m=new BattleModel(21);
            m.Hp=50;
            foreach(int index in new[]{19,0,1})m.Board[index].Hazard=HazardKind.None;
            m.Board[19].Cards[1]=CardKind.Guard;
            m.Board[0].Cards[1]=CardKind.Strike;
            m.Board[1].Cards[1]=CardKind.Heal;
            m.Throw(new Hit(0,3,60),0);
            check("triple combines left center right",m.EnemyHp==124&&m.Hp==55&&m.Shield==7&&m.Score==60);
            m=new BattleModel(22);
            m.Board[19].Cards[1]=m.Board[0].Cards[1]=m.Board[1].Cards[1]=CardKind.Empty;
            m.SetHazard(19,HazardKind.Mine,-1,1);
            m.SetHazard(1,HazardKind.Fire,2,-1);
            m.Board[0].Hazard=HazardKind.None;
            m.Throw(new Hit(0,3,60),0);
            check("triple also activates adjacent hazards",m.Hp==56&&
                m.Board[19].Hazard==HazardKind.None&&m.Board[1].Hazard==HazardKind.Fire);
            m=new BattleModel(23);
            m.Board[0].Cards[3]=CardKind.Strike;m.Board[0].Hazard=HazardKind.None;
            m.Board[19].Cards[3]=m.Board[1].Cards[3]=CardKind.Guard;
            m.Throw(new Hit(0,2,40),0);
            check("double doubles only its own cell",m.EnemyHp==116&&m.Shield==0);
            m=new BattleModel(24);m.Hp=50;m.Board[0].Hazard=HazardKind.None;
            m.Board[0].Cards[0]=CardKind.Strike;m.Board[0].Cards[2]=CardKind.Heal;
            m.Throw(BattleModel.Evaluate(0,.4f),1);
            m.Throw(BattleModel.Evaluate(0,.8f),2);
            check("single bands use different cards",m.EnemyHp==127&&m.Hp==55&&m.Score==40);
            m=new BattleModel(25);m.Board[0].Cards[0]=CardKind.Empty;m.Board[0].Hazard=HazardKind.None;
            m.Throw(new Hit(0,1,20),1);
            check("empty cell scores but has no card",m.Score==20&&m.EnemyHp==135&&m.Shield==4);
            string summary=passed.Count+" checks passed\n"+string.Join("\n",passed);
            Debug.Log(summary);
            return summary;
        }
    }
}
