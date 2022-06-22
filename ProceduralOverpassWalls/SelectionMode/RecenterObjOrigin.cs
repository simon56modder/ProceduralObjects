using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ProceduralObjects.Classes;
using ProceduralObjects.Localization;
using ProceduralObjects.UI;
using UnityEngine;

namespace ProceduralObjects.SelectionMode
{
    public class RecenterObjOrigin : SelectionModeAction
    {
        public override void OnOpen(List<ProceduralObject> selection)
        {
            this.selection = POGroup.AllObjectsInSelection(selection, logic.selectedGroup);
            foreach (var po in this.selection)
            {
                if (po.meshStatus == 1)
                    continue;
                try
                {
                    po.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, po.vertices);
                    ProceduralUtils.RecenterObjOrigin(po, po.vertices);
                    po.ApplyModelChange();
                    po.historyEditionBuffer.ConfirmNewStep(po.vertices);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[ProceduralObjects] PO could not recenter object origin of object #" + po.id + " of type " + po.basePrefabName + "\n" + e);
                    po.historyEditionBuffer.ConfirmNewStep(po.vertices);
                }
            }
            ExitAction();
        }
    }
}
