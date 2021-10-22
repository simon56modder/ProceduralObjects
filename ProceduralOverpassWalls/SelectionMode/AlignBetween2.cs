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
    public class AlignBetween2 : SelectionModeAction
    {
        Vector3 firstPoint;
        bool firstPointSet;

        public override void OnOpen(List<ProceduralObject> selection)
        {
            base.OnOpen(selection);
            firstPointSet = false;
        }

        public override void OnSingleClick(ProceduralObject obj)
        {
            if (firstPointSet)
            {
                AlignBetween2Points(obj.m_position);
                ExitAction();
            }
            else
            {
                firstPoint = obj.m_position;
                firstPointSet = true;
            }
        }
        public void AlignBetween2Points(Vector3 secondPoint)
        {
            Vector3 diffVector = secondPoint - firstPoint;

            foreach (var po in selection)
            {
                if (po.isRootOfGroup && logic.selectedGroup == null)
                {
                    var newPos = firstPoint + Vector3.Project(po.m_position - firstPoint, diffVector);
                    var diffPos = newPos - po.m_position;
                    po.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.position, null);
                    po.SetPosition(newPos);
                    po.historyEditionBuffer.ConfirmNewStep(null);
                    foreach (var o in po.group.objects)
                    {
                        if (o == po)
                            continue;
                        o.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.position, null);
                        o.SetPosition(o.m_position + diffPos);
                        o.historyEditionBuffer.ConfirmNewStep(null);
                    }
                }
                else
                {
                    po.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.position, null);
                    po.SetPosition(firstPoint + Vector3.Project(po.m_position - firstPoint, diffVector));
                    po.historyEditionBuffer.ConfirmNewStep(null);
                }
            }
        }
    }
}
