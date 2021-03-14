using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ProceduralObjects.Classes;
using ProceduralObjects.Localization;
using ProceduralObjects.UI;

namespace ProceduralObjects.SelectionMode
{
    public class SetRenderDistances : SelectionModeAction
    {
        public GUIUtils.FloatInputField renderDistInput;
        public float value;

        public override void OnOpen(List<ProceduralObject> selection)
        {
            base.OnOpen(selection);
            value = Gizmos.ConvertRoundToDistanceUnit(GetAvgRenderDistance(POGroup.AllObjectsInSelection(selection, logic.selectedGroup)));
            renderDistInput = new GUIUtils.FloatInputField(value);
        }
        public override void OnActionGUI(Vector2 uiPos)
        {
            GUI.BeginGroup(new Rect(uiPos, new Vector2(195, 48)));
            value = renderDistInput.DrawField(new Rect(0, 0, 60, 22), value, false).returnValue;

            GUI.Label(new Rect(62, 0, 20, 22), ProceduralObjectsMod.distanceUnit.Trim());
            if (GUI.Button(new Rect(0, 24, 40, 22), LocalizationManager.instance.current["ok"]))
            {
                ProceduralObjectsLogic.PlaySound();
                SetRenderDistancesAll(value);
                ExitAction();
            }
            if (GUI.Button(new Rect(42, 24, 90, 22), LocalizationManager.instance.current["cancel"]))
            {
                ProceduralObjectsLogic.PlaySound();
                ExitAction();
            }
            GUI.EndGroup();
        }
        public override Rect CollisionUI(Vector2 uiPos)
        {
            return new Rect(uiPos, new Vector2(195, 48));
        }
        public void SetRenderDistancesAll(float value)
        {
            value = Mathf.Clamp(Gizmos.ConvertRoundBackToMeters(value), 50f, 16000f);
            foreach (var obj in POGroup.AllObjectsInSelection(selection, logic.selectedGroup))
            {
                if (!obj.renderDistLocked)
                    obj.renderDistance = value;
            }
        }
        public override void OnSingleClick(ProceduralObject obj)
        {
            ExitAction();
        }
        public float GetAvgRenderDistance(List<ProceduralObject> list)
        {
            uint totalRDs = 0;
            foreach (var obj in list)
                totalRDs += (uint)Mathf.Round(obj.renderDistance);
            return (float)Mathf.Round(totalRDs / list.Count);
        }
    }
}
