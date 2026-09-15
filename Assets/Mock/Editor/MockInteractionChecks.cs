using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace DartsRoguelike.Mock.Editor
{
    public static class MockInteractionChecks
    {
        public static string Run()
        {
            if(!Application.isPlaying)throw new InvalidOperationException("Run in Play Mode.");
            var battle=UnityEngine.Object.FindFirstObjectByType<MockBattle>();
            var board=UnityEngine.Object.FindObjectsByType<DartBoardGraphic>(FindObjectsSortMode.None).First(x=>!x.Overlay);
            var mouse=Mouse.current;
            if(mouse==null)throw new InvalidOperationException("Mouse required.");
            Vector2 old=mouse.position.ReadValue();
            try
            {
                Canvas.ForceUpdateCanvases();
                Vector2 point=RectTransformUtility.WorldToScreenPoint(null,board.rectTransform.position);
                var pointer=new PointerEventData(EventSystem.current){position=point,button=PointerEventData.InputButton.Left};
                var hits=new System.Collections.Generic.List<RaycastResult>();
                EventSystem.current.RaycastAll(pointer,hits);
                if(hits.Count==0||hits[0].gameObject!=board.gameObject)throw new Exception("Board raycast blocked.");
                InputState.Change(mouse.position,point);
                ExecuteEvents.Execute(board.gameObject,pointer,ExecuteEvents.pointerDownHandler);
                if(!battle.Aiming)throw new Exception("Pointer down did not start aiming.");
                ExecuteEvents.Execute(board.gameObject,pointer,ExecuteEvents.pointerUpHandler);
                if(battle.Aiming||battle.Model.Remaining!=2||!battle.HasHit)throw new Exception("Pointer up did not throw.");
                var end=UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).First(x=>x.name=="EndTurn");
                ExecuteEvents.Execute(end.gameObject,pointer,ExecuteEvents.pointerClickHandler);
                if(battle.Model.Turn!=2||battle.Model.Remaining!=3||battle.Model.Hp>=70)throw new Exception("End turn button failed.");
                var restart=UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).First(x=>x.name=="Restart");
                ExecuteEvents.Execute(restart.gameObject,pointer,ExecuteEvents.pointerClickHandler);
                if(battle.Model.Turn!=1||battle.Model.Hp!=70||battle.Model.Remaining!=3)throw new Exception("Restart button failed.");
                var dart=UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).First(x=>x.name=="Dart2");
                ExecuteEvents.Execute(dart.gameObject,pointer,ExecuteEvents.pointerClickHandler);
                ExecuteEvents.Execute(board.gameObject,pointer,ExecuteEvents.pointerDownHandler);
                ExecuteEvents.Execute(board.gameObject,pointer,ExecuteEvents.pointerUpHandler);
                if(!battle.Model.Used[2])throw new Exception("Dart selection button failed.");
                battle.Restart();
                return "6 interaction checks passed: board raycast, aim, release, end turn, restart, dart selection.";
            }
            finally { InputState.Change(mouse.position,old); }
        }
    }
}
