using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProceduralObjects.Classes;
using UnityEngine;

namespace ProceduralObjects.SelectionMode
{
    public class AlignHeights : SelectionModeAction
    {
        public override void OnSingleClick(ProceduralObject obj)
        {
            var height = obj.m_position.y;
            foreach (var po in selection)
            {
                if (po.isRootOfGroup && logic.selectedGroup == null)
                {
                    float diffToApply = po.m_position.y - height;
                    foreach (var o in po.group.objects)
                    {
                        o.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.position, null);
                        o.SetPosition(new Vector3(o.m_position.x, o.m_position.y - diffToApply, o.m_position.z));
                        o.historyEditionBuffer.ConfirmNewStep(null);
                    }
                }
                else
                {
                    po.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.position, null);
                    po.SetPosition(new Vector3(po.m_position.x, height, po.m_position.z));
                    po.historyEditionBuffer.ConfirmNewStep(null);
                }
            }
            ExitAction();
        }
    }
}
