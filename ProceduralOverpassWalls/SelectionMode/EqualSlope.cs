using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using ProceduralObjects.Classes;
using ProceduralObjects.Localization;


namespace ProceduralObjects.SelectionMode
{
    public class EqualSlope : SelectionModeAction
    {
        public override void OnOpen(List<ProceduralObject> selection)
        {
            base.OnOpen(selection);
            if (selection.Count < 3)
            {
                ExitAction();
                return;
            }
        }
        public override void OnSingleClick(ProceduralObject obj)
        {
            ProceduralObjectsLogic.PlaySound(2);
            ExitAction();
        }
        public override void OnActionGUI(Vector2 uiPos)
        {
            if (GUI.Button(new Rect(uiPos, new Vector2(180, 22)), LocalizationManager.instance.current["equal_slope"]))
            {
                ProceduralObjectsLogic.PlaySound();
                DoSlope(false);
                ExitAction();
            }
            if (GUI.Button(new Rect(uiPos.x, uiPos.y + 23, 180, 22), LocalizationManager.instance.current["oriented_equal_slope"]))
            {
                ProceduralObjectsLogic.PlaySound();
                DoSlope(true);
                ExitAction();
            }
            if (GUI.Button(new Rect(uiPos.x, uiPos.y + 46, 180, 22), LocalizationManager.instance.current["cancel"]))
            {
                ProceduralObjectsLogic.PlaySound();
                ExitAction();
            }
        }
        public override Rect CollisionUI(Vector2 uiPos)
        {
            return new Rect(uiPos, new Vector2(180, 70));
        }

        public void DoSlope(bool oriented)
        {
            var maxObjects = VertexUtils.OutmostPoints(selection.ToArray());

            float heightDiff = maxObjects.Value.m_position.y - maxObjects.Key.m_position.y;
            float distDiff = Vector2.Distance(maxObjects.Value.m_position.AsXZVector2(), maxObjects.Key.m_position.AsXZVector2());
            Vector3 flatDirection = new Vector3(maxObjects.Value.m_position.x, 0, maxObjects.Value.m_position.z) - new Vector3(maxObjects.Key.m_position.x, 0, maxObjects.Key.m_position.z);
            Quaternion rotDiff = Quaternion.FromToRotation(flatDirection, maxObjects.Value.m_position - maxObjects.Key.m_position);

            foreach (var po in selection)
            {
                var localdistdiff = Vector3.Project((po.m_position - maxObjects.Key.m_position).NullY(), (maxObjects.Value.m_position - maxObjects.Key.m_position).NullY()).magnitude;
                var localheightdiff = (po == maxObjects.Key) ? 0f : ((po == maxObjects.Value) ? heightDiff : heightDiff * localdistdiff / distDiff);
                if (po.isRootOfGroup && logic.selectedGroup == null)
                {
                    foreach (var o in po.group.objects)
                    {
                        if (oriented)
                        {
                            o.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.moveTo, null);
                            o.SetPosition( new Vector3(o.m_position.x, localheightdiff + maxObjects.Key.m_position.y, o.m_position.z));
                            o.SetRotation( rotDiff * o.m_rotation);
                            if (o != po)
                                o.m_position = VertexUtils.RotatePointAroundPivot(o.m_position, po.m_position, rotDiff);
                        }
                        else
                        {
                            o.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.position, null);
                            o.SetPosition( new Vector3(o.m_position.x, localheightdiff + maxObjects.Key.m_position.y, o.m_position.z));
                        }
                        o.historyEditionBuffer.ConfirmNewStep(null);
                    }
                }
                else
                {
                    if (oriented)
                    {
                        po.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.moveTo, null);
                        po.SetRotation( rotDiff * po.m_rotation);
                    }
                    else
                    {
                        po.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.position, null);
                    }
                    po.SetPosition( new Vector3(po.m_position.x, localheightdiff + maxObjects.Key.m_position.y, po.m_position.z));
                    po.historyEditionBuffer.ConfirmNewStep(null);
                }
            }
        }
    }
}
