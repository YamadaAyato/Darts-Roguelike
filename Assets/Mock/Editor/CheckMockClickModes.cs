using System;
using System.Linq;
using System.Reflection;
using DartsRoguelike.Mock;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>
///     最初のMockBattleで操作方式の切替と投擲を確認する
/// </summary>
public static class CheckMockClickModes
{
    /// <summary>
    ///     最初のMockBattleを2クリック方式で開始する設定に保存する
    /// </summary>
    public static string Configure()
    {
        UnityEngine.SceneManagement.Scene scene =
            UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != "Assets/Mock/Scenes/MockBattle.unity")
        {
            throw new InvalidOperationException("編集モードで最初のMockBattleを開いてください");
        }

        MockBattle battle = UnityEngine.Object.FindAnyObjectByType<MockBattle>();
        Undo.RecordObject(battle, "Set mock click mode");
        var serialized = new SerializedObject(battle);
        serialized.FindProperty("_clickToThrow").boolValue = true;
        serialized.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return "MockBattle starts in click mode; the on-screen button toggles hold mode";
    }

    /// <summary>
    ///     UIイベントを通して両方式と切替時の状態を検証する
    /// </summary>
    public static string Run()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("再生中に実行してください");
        }

        MockBattle battle = UnityEngine.Object.FindAnyObjectByType<MockBattle>();
        DartBoardGraphic board = UnityEngine.Object.FindObjectsByType<DartBoardGraphic>()
            .First(item => !item.Overlay);
        UnityEngine.UI.Button modeButton = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>()
            .First(item => item.name == "ThrowMode");
        Mouse mouse = Mouse.current;
        Vector2 previousPosition = mouse.position.ReadValue();
        float previousRightButton = mouse.rightButton.ReadValue();
        try
        {
            battle.Restart();
            battle.SetClickToThrow(true);
            Canvas.ForceUpdateCanvases();
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, board.rectTransform.position);
            InputState.Change(mouse.position, screen);
            var pointer = new PointerEventData(EventSystem.current)
            {
                position = screen,
                button = PointerEventData.InputButton.Left
            };
            var raycasts = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, raycasts);
            Check(raycasts.Count > 0 && raycasts[0].gameObject == board.gameObject, "board raycast");

            ExecuteEvents.Execute(board.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            Check(battle.Aiming && battle.Model.Remaining == 3, "first click starts aim");
            float initialSpread = battle.Spread;
            ExecuteEvents.Execute(board.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            Check(battle.Aiming && battle.Model.Remaining == 3, "release keeps aim");

            // 待ち時間を固定して円の周期更新を確認する
            typeof(MockBattle).GetField("started", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(battle, Time.unscaledTime - 0.4f);
            typeof(MockBattle).GetMethod("UpdateAim", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(battle, null);
            Check(Mathf.Abs(initialSpread - battle.Spread) > 0.01f, "circle keeps cycling");

            ExecuteEvents.Execute(board.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            Check(!battle.Aiming && battle.Model.Remaining == 2 && battle.HasHit, "second click throws once");
            ExecuteEvents.Execute(board.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            Check(battle.Model.Remaining == 2, "second release does not throw twice");

            ExecuteEvents.Execute(board.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(board.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(modeButton.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Check(!battle.UsesClickToThrow && !battle.Aiming && battle.Model.Remaining == 2,
                "mode switch cancels without spending a dart");

            battle.Restart();
            ExecuteEvents.Execute(board.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            Check(battle.Aiming && battle.Model.Remaining == 3, "hold press starts aim");
            ExecuteEvents.Execute(board.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            Check(!battle.Aiming && battle.Model.Remaining == 2, "hold release throws");

            ExecuteEvents.Execute(modeButton.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            battle.Restart();
            ExecuteEvents.Execute(board.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(board.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            SetRightButton(mouse, 1f);
            typeof(MockBattle).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(battle, null);
            Check(!battle.Aiming && battle.Model.Remaining == 3, "right click cancels");

            battle.SetClickToThrow(false);
            string legacyChecks = DartsRoguelike.Mock.Editor.MockInteractionChecks.Run();
            battle.SetClickToThrow(true);
            battle.Restart();
            return "10 click-mode checks passed; " + legacyChecks;
        }
        finally
        {
            InputState.Change(mouse.position, previousPosition);
            SetRightButton(mouse, previousRightButton);
        }
    }

    /// <summary>
    ///     不一致の場合に検証項目名を付けて失敗させる
    /// </summary>
    private static void Check(bool condition, string name)
    {
        if (!condition)
        {
            throw new InvalidOperationException("Failed: " + name);
        }
    }

    /// <summary>
    ///     ボタンのビットを含むマウス全体の状態イベントで入力を更新する
    /// </summary>
    private static void SetRightButton(Mouse mouse, float value)
    {
        using (StateEvent.From(mouse, out InputEventPtr stateEvent))
        {
            mouse.rightButton.WriteValueIntoEvent(value, stateEvent);
            InputSystem.QueueEvent(stateEvent);
            InputSystem.Update();
        }
    }
}
