using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProceduralObjects.Classes;
using UnityEngine;

namespace ProceduralObjects.SelectionMode
{
    public class AlignRotations : SelectionModeAction
    {
        public override void OnSingleClick(ProceduralObject obj)
        {
            foreach (var po in selection)
            {
                if (po.isRootOfGroup && logic.selectedGroup == null)
                {
                    Quaternion diff = Quaternion.Inverse(po.m_rotation) * obj.m_rotation;
                    foreach (var o in po.group.objects)
                    {
                        o.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.moveTo, null);
                        o.m_rotation = diff * o.m_rotation;
                        if (o != po)
                            o.m_position = VertexUtils.RotatePointAroundPivot(o.m_position, po.m_position, diff);
                        o.historyEditionBuffer.ConfirmNewStep(null);
                    }
                }
                else
                {
                    po.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.rotation, null);
                    po.m_rotation = obj.m_rotation;
                    po.historyEditionBuffer.ConfirmNewStep(null);
                }
            }
            ExitAction();
        }
    }
}
