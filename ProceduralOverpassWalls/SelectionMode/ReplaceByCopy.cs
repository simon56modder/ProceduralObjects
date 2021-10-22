using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProceduralObjects.Classes;
using ProceduralObjects.Localization;
using ColossalFramework.UI;
using ProceduralObjects.UI;

namespace ProceduralObjects.SelectionMode
{
    public class ReplaceByCopy : SelectionModeAction
    {
        public override void OnOpen(List<ProceduralObject> selection)
        {
            base.OnOpen(selection);
            if (logic.clipboard == null)
            {
                ExitAction();
                return;
            }
            if (logic.clipboard.type != ClipboardProceduralObjects.ClipboardType.Single)
            {
                ExitAction();
                return;
            }
            var source = logic.clipboard.single_object;
            if (source == null)
            {
                ExitAction();
                return;
            }

            GUIUtils.ShowModal(LocalizationManager.instance.current["confirmPasteInto_title"], string.Format(LocalizationManager.instance.current["confirmPasteInto_descSelection"], selection.Count),
                (bool ok) =>
                {
                    if (ok)
                    {
                        for (int i = 0; i < selection.Count; i++)
                        {
                            bool mustAddToGroup = false, mustSetAsRoot = false;
                            if (logic.selectedGroup == null)
                            {
                                if (selection[i].isRootOfGroup)
                                {
                                    POGroup.DeleteGroup(logic, selection[i].group);
                                    logic.activeIds.Add(selection[i].id);
                                }
                            }
                            else
                            {
                                mustAddToGroup = true;
                                if (selection[i] == logic.selectedGroup.root)
                                    mustSetAsRoot = true;
                                logic.selectedGroup.Remove(logic, selection[i]);
                            }
                            logic.proceduralObjects.Remove(selection[i]);
                            var obj = new ProceduralObject(source, selection[i].id, selection[i].m_position, logic.layerManager);
                            if (mustAddToGroup)
                            {
                                logic.selectedGroup.AddToGroup(obj);
                                if (mustSetAsRoot)
                                    logic.selectedGroup.ChooseAsRoot(obj);
                            }
                            obj.RecalculateBoundsNormalsExtras(obj.meshStatus);
                            if (obj.meshStatus != 1)
                            {
                                if (obj.RequiresUVRecalculation && !obj.disableRecalculation)
                                    obj.m_mesh.uv = Vertex.RecalculateUVMap(obj, Vertex.CreateVertexList(obj));
                            }
                            obj.m_rotation = selection[i].m_rotation;
                            logic.proceduralObjects.Add(obj);
                        }
                        logic.pObjSelection.Clear();
                    }
                    ExitAction();
                });
        }
    }
}
