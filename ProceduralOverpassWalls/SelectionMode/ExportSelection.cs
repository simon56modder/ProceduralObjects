using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProceduralObjects.Classes;
using UnityEngine;
using ProceduralObjects.Localization;

namespace ProceduralObjects.SelectionMode
{
    public class ExportSelection : SelectionModeAction
    {
        public ClipboardProceduralObjects clipboard;

        public override void OnOpen(List<ProceduralObject> selection)
        {
            selection = POGroup.AllObjectsInSelection(logic.pObjSelection, logic.selectedGroup);
            if (selection.Count <= 1)
            {
                ExitAction();
                return;
            }
            base.OnOpen(selection);
            clipboard = new ClipboardProceduralObjects(ClipboardProceduralObjects.ClipboardType.Selection);
            clipboard.MakeSelectionList(selection, null);
        }
        public override void OnSingleClick(ProceduralObject obj)
        {
            ProceduralObjectsLogic.PlaySound(2);
            ExitAction();
        }
        public override void OnActionGUI(Vector2 uiPos)
        {
            if (GUI.Button(new Rect(uiPos, new Vector2(180, 22)), LocalizationManager.instance.current["export_as_ploppable"]))
            {
                ProceduralObjectsLogic.PlaySound();
                clipboard.ExportSelection("Selection " + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"), logic.ExPObjManager, false);
                ExitAction();
            }
            if (GUI.Button(new Rect(uiPos.x, uiPos.y + 23, 180, 22), LocalizationManager.instance.current["export_as_fixed"]))
            {
                ProceduralObjectsLogic.PlaySound();
                clipboard.ExportSelection("Fixed Export " + DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"), logic.ExPObjManager, true);
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
    }
}
