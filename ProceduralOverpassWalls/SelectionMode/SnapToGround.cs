using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ProceduralObjects.Classes;
using ProceduralObjects.Localization;


namespace ProceduralObjects.SelectionMode
{
    public class SnapToGround : SelectionModeAction
    {
        public override void OnSingleClick(ProceduralObject obj)
        {
            ProceduralObjectsLogic.PlaySound(2);
            ExitAction();
        }
        public override void OnActionGUI(Vector2 uiPos)
        {
            if (GUI.Button(new Rect(uiPos, new Vector2(180, 22)), LocalizationManager.instance.current["snapToGround"]))
            {
                ProceduralObjectsLogic.PlaySound();
                Snap();
                ExitAction();
            }
            if (GUI.Button(new Rect(uiPos.x, uiPos.y + 23, 180, 22), LocalizationManager.instance.current["cancel"]))
            {
                ProceduralObjectsLogic.PlaySound();
                ExitAction();
            }
        }
        public override Rect CollisionUI(Vector2 uiPos)
        {
            return new Rect(uiPos, new Vector2(180, 45));
        }

        public void Snap()
        {
            foreach (var po in selection)
            {
                var height = po.m_position.y;
                po.SnapToGround();
                var diffheight = po.m_position.y - height;
                if (po.isRootOfGroup && logic.selectedGroup == null)
                {
                    foreach (var o in po.group.objects)
                    {
                        if (o == po) continue;

                        o.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.position, null);
                        o.SetPosition(new Vector3(o.m_position.x, o.m_position.y + diffheight, o.m_position.z));
                        o.historyEditionBuffer.ConfirmNewStep(null);
                    }
                }
            }
        }
    }
}
