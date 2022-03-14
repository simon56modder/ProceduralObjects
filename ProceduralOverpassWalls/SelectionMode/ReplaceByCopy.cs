using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProceduralObjects.Classes;
using ProceduralObjects.Localization;
using ColossalFramework.UI;
using ProceduralObjects.UI;
using UnityEngine;

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
            if (logic.clipboard.type == ClipboardProceduralObjects.ClipboardType.Single)
            {
                if (logic.clipboard.single_object == null)
                {
                    ExitAction();
                    return;
                }
            }
            else if (logic.clipboard.type == ClipboardProceduralObjects.ClipboardType.Selection)
            {
                if (logic.clipboard.selection_objects == null || logic.selectedGroup != null)
                {
                    ExitAction();
                    return;
                }
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
                            if (logic.clipboard.type == ClipboardProceduralObjects.ClipboardType.Single)
                            {
                                var obj = new ProceduralObject(logic.clipboard.single_object, selection[i].id, selection[i].m_position, logic.layerManager);
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
                            else if (logic.clipboard.type == ClipboardProceduralObjects.ClipboardType.Selection)
                            {
                                Quaternion qDiff = Quaternion.Inverse(logic.clipboard.selection_objects.Keys.ToArray()[0].m_rotation) * selection[i].m_rotation;
                                var cacheRealPairs = new Dictionary<CacheProceduralObject, ProceduralObject>();
                                for (int j = 0; j < logic.clipboard.selection_objects.Count; j++)
                                {
                                    var kvp = logic.clipboard.selection_objects.ElementAt(j);
                                    if (j == 0)
                                    {
                                        var obj = new ProceduralObject(kvp.Key, selection[i].id, selection[i].m_position, logic.layerManager);
                                        cacheRealPairs.Add(kvp.Key, obj);
                                        obj.RecalculateBoundsNormalsExtras(obj.meshStatus);
                                        if (obj.meshStatus != 1)
                                        {
                                            if (obj.RequiresUVRecalculation && !obj.disableRecalculation)
                                                obj.m_mesh.uv = Vertex.RecalculateUVMap(obj, Vertex.CreateVertexList(obj));
                                        }
                                        obj.m_rotation = selection[i].m_rotation;
                                        logic.proceduralObjects.Add(obj);
                                    }
                                    else
                                    {
                                        var obj = new ProceduralObject(kvp.Key, logic.proceduralObjects.GetNextUnusedId(), selection[i].m_position + (qDiff * kvp.Value), logic.layerManager);
                                        cacheRealPairs.Add(kvp.Key, obj);
                                        obj.RecalculateBoundsNormalsExtras(obj.meshStatus);
                                        if (obj.meshStatus != 1)
                                        {
                                            if (obj.RequiresUVRecalculation && !obj.disableRecalculation)
                                                obj.m_mesh.uv = Vertex.RecalculateUVMap(obj, Vertex.CreateVertexList(obj));
                                        }
                                        obj.m_rotation = obj.m_rotation * qDiff;
                                        logic.proceduralObjects.Add(obj);
                                    }
                                }
                                logic.clipboard.RecreateGroups(cacheRealPairs);
                            }
                        }
                        logic.pObjSelection.Clear();
                    }
                    ExitAction();
                });
        }
    }
}
