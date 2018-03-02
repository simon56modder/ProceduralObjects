using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.Rendering;
using UnityEngine;

using ProceduralObjects.Classes;

namespace ProceduralObjects
{
    public class ProceduralEditorHelper : MonoBehaviour
    {
        public bool showUI, settingMainVertex = false, useRegion = false, clickingRegion = false;
        public string editingType;
        public PropInfo editingProp;
        public BuildingInfo editingBuilding;
        public Vertex[] allVertices;
        public List<DependencyGroup> dependencyGroups;
        public List<Vertex> lockedVertices;
        public DependencyGroup selectedDependencyGroup;
        public Rect window = new Rect(155, 100, 400, 500);
        public Vector2 scrollGroups = Vector2.zero, topLeftRegion = Vector2.zero, bottomRightRegion = Vector2.zero;
        public Vector3 levelVector = new Vector3(0, 60, 0);

        void Awake()
        {
            showUI = false;
            editingProp = null;
            editingBuilding = null;
            dependencyGroups = new List<DependencyGroup>();
            lockedVertices = new List<Vertex>();
            selectedDependencyGroup = null;
        }
        void Update()
        {
            if (useRegion && showUI && !settingMainVertex && selectedDependencyGroup != null)
            {
                if (Input.GetMouseButton(0))
                {
                    if (!clickingRegion)
                    {
                        topLeftRegion = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                        clickingRegion = true;
                    }
                    else
                        bottomRightRegion = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                }
                else if (clickingRegion)
                {
                    bottomRightRegion = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                    Rect region = CreateRectFromVector2s(topLeftRegion, bottomRightRegion);
                    clickingRegion = false;
                    foreach (Vertex vertex in allVertices.Where(v => !v.IsDependent))
                    {
                        if (!DependencyGroup.AlreadyBelongsToAGroup(vertex, dependencyGroups, true, selectedDependencyGroup) && !lockedVertices.Contains(vertex))
                        {
                            if (Input.GetKey(KeyCode.LeftShift))
                            {
                                if ((selectedDependencyGroup.mainVertex != vertex) && selectedDependencyGroup.subVertices.Contains(vertex))
                                {
                                    if (region.Contains((vertex.Position + levelVector).WorldToGuiPoint()))
                                        selectedDependencyGroup.subVertices.Remove(vertex);
                                }
                            }
                            else
                            {
                                if ((selectedDependencyGroup.mainVertex != vertex) && !selectedDependencyGroup.subVertices.Contains(vertex))
                                {
                                    if (region.Contains((vertex.Position + levelVector).WorldToGuiPoint()))
                                        selectedDependencyGroup.subVertices.Add(vertex);
                                }
                            }
                        }
                    } 
                }
            }
        }
        void OnGUI()
        {
            if (clickingRegion && useRegion)
                GUI.Box(CreateRectFromVector2s(topLeftRegion, bottomRightRegion), "");
            if (!showUI)
            {
                if (GUI.Button(new Rect(Screen.width - 200, 60, 195, 30), "Procedural Obj Helper"))
                {
                    try
                    {
                        if (ToolsModifierControl.toolController.m_editPrefabInfo.GetType() == typeof(PropInfo))
                        {
                            editingType = "PROP";
                            editingProp = ToolsModifierControl.toolController.m_editPrefabInfo as PropInfo;
                            allVertices = Vertex.CreateVertexList(editingProp);
                            showUI = true;
                        }
                        else if (ToolsModifierControl.toolController.m_editPrefabInfo.GetType() == typeof(BuildingInfo))
                        {
                            editingType = "BUILDING";
                            editingBuilding = ToolsModifierControl.toolController.m_editPrefabInfo as BuildingInfo;
                            allVertices = Vertex.CreateVertexList(editingBuilding);
                            showUI = true;
                        }
                    }
                    catch { }
                }
            }
            else
            {
                window = GUI.Window(this.GetInstanceID(), window, DrawWindow, "Procedural Objects Asset Creator Helper");
                if (settingMainVertex)
                {
                    #region when user is Setting the MAIN VERTEX
                    foreach (Vertex vertex in allVertices.Where(v => !v.IsDependent))
                    {
                        if (!DependencyGroup.AlreadyBelongsToAGroup(vertex, dependencyGroups, true, selectedDependencyGroup) && !lockedVertices.Contains(vertex))
                        {
                            if (selectedDependencyGroup.mainVertex != vertex)
                            {
                                if (GUI.Button(new Rect((vertex.Position + levelVector).WorldToGuiPoint() + new Vector2(-11, -11), new Vector2(23, 22)), "<size=20>+</size>"))
                                {
                                    if (selectedDependencyGroup.subVertices.Contains(vertex))
                                    {
                                        selectedDependencyGroup.subVertices.Add(selectedDependencyGroup.mainVertex);
                                        selectedDependencyGroup.mainVertex = vertex;
                                        selectedDependencyGroup.subVertices.Remove(vertex);
                                        settingMainVertex = false;
                                    }
                                    else
                                    {
                                        selectedDependencyGroup.mainVertex = vertex;
                                        settingMainVertex = false;
                                    }
                                }
                            }
                        }
                    }
                    #endregion
                }
                else if (selectedDependencyGroup != null)
                {
                        #region vertex edit tool
                        foreach (Vertex vertex in allVertices.Where(v => !v.IsDependent))
                        {
                            if (!DependencyGroup.AlreadyBelongsToAGroup(vertex, dependencyGroups, true, selectedDependencyGroup) && !lockedVertices.Contains(vertex))
                            {
                                if (selectedDependencyGroup.mainVertex == vertex)
                                {
                                    GUI.contentColor = Color.red;
                                    GUI.Label(new Rect((vertex.Position + levelVector).WorldToGuiPoint() + new Vector2(-8, -8), new Vector2(23, 22)), "<size=20><b>X</b></size>");
                                    GUI.contentColor = Color.white;
                                }
                                else if (selectedDependencyGroup.subVertices.Contains(vertex))
                                {
                                    GUI.contentColor = Color.green;
                                    if (GUI.Button(new Rect((vertex.Position + levelVector).WorldToGuiPoint() + new Vector2(-11, -11), new Vector2(23, 22)), "<size=20>x</size>"))
                                    {
                                        selectedDependencyGroup.subVertices.Remove(vertex);
                                    }
                                    GUI.contentColor = Color.white;
                                }
                                else
                                {
                                    if (GUI.Button(new Rect((vertex.Position + levelVector).WorldToGuiPoint() + new Vector2(-11, -11), new Vector2(23, 22)), "<size=20>+</size>"))
                                    {
                                        selectedDependencyGroup.subVertices.Add(vertex);
                                    }
                                }
                            }
                        }
                        #endregion
                }
            }

        }
        public void DrawWindow(int id)
        {
            GUI.DragWindow(new Rect(0, 0, 348, 28));
            if (GUI.Button(new Rect(356, 3, 30, 30), "X"))
            {
                showUI = false;
                editingType = string.Empty;
                editingProp = null;
                editingBuilding = null;
                allVertices = null;
                dependencyGroups = new List<DependencyGroup>();
                lockedVertices = new List<Vertex>();
                selectedDependencyGroup = null;
                scrollGroups = Vector2.zero;
            }

            if (selectedDependencyGroup == null)
            {
                #region main panel
                GUI.Label(new Rect(10, 30, 350, 30), "Dependency Groups");
                GUI.Box(new Rect(5, 58, 390, 302), string.Empty);
                scrollGroups = GUI.BeginScrollView(new Rect(10, 60, 350, 295), scrollGroups, new Rect(0, 0, 320, 35 * dependencyGroups.Count()));
                for (int i = 0; i < dependencyGroups.Count(); i++)
                {
                    if (GUI.Button(new Rect(10, i * 33 + 2, 300, 33), "Group " + (i + 1).ToString()))
                    {
                        selectedDependencyGroup = dependencyGroups[i];
                    }
                }
                GUI.EndScrollView();
                if (GUI.Button(new Rect(10, 422, 185, 25), "Save Data in Asset"))
                {
                    // saving process
                    string data = ProceduralObjectAssetUtils.SaveDependencyData(dependencyGroups, lockedVertices);
                    if (editingType == "PROP")
                        editingProp.m_material.name = data;
                    else
                        editingBuilding.m_material.name = data;
                    Debug.Log("[ProceduralObjects] Saved the following data in asset : " + data);
                    showUI = false;
                    editingProp = null;
                    editingBuilding = null;
                    allVertices = null;
                    dependencyGroups = new List<DependencyGroup>();
                    lockedVertices = new List<Vertex>();
                    selectedDependencyGroup = null;
                    scrollGroups = Vector2.zero;
                }
                if (GUI.Button(new Rect(205, 422, 185, 25), "Create Dependency Group"))
                {
                    var group = new DependencyGroup();
                    dependencyGroups.Add(group);
                    selectedDependencyGroup = group;
                    settingMainVertex = true;
                }
                #endregion
            }
            else if (selectedDependencyGroup != null)
            {
                #region group editor
                if (settingMainVertex)
                    GUI.Label(new Rect(50, 270, 380, 80), "Select a new main vertex");
                else
                {
                    if (GUI.Button(new Rect(10, 100, 380, 80), "Delete Group"))
                    {
                        dependencyGroups.Remove(selectedDependencyGroup);
                        selectedDependencyGroup = null;
                        useRegion = false;
                        topLeftRegion = Vector2.zero;
                        bottomRightRegion = Vector2.zero;
                    }
                    if (GUI.Button(new Rect(10, 270, 380, 80), "OK"))
                    {
                        selectedDependencyGroup = null;
                        useRegion = false;
                        topLeftRegion = Vector2.zero;
                        bottomRightRegion = Vector2.zero;
                    }
                    if (GUI.Button(new Rect(10, 185, 380, 80), "Change main vertex"))
                    {
                        settingMainVertex = true;
                        useRegion = false;
                        topLeftRegion = Vector2.zero;
                        bottomRightRegion = Vector2.zero;
                    }
                    useRegion = GUI.Toggle(new Rect(30, 360, 350, 30), useRegion, "Use Mouse Region selection");
                }
                #endregion
            }
        }
        public Rect CreateRectFromVector2s(Vector2 topLeftCorner, Vector2 bottomRightCorner)
        {
            return new Rect(topLeftCorner, new Vector2(bottomRightCorner.x - topLeftRegion.x, bottomRightCorner.y - topLeftRegion.y));
        }
    }
    public class DependencyGroup
    {
        public DependencyGroup()
        {
            subVertices = new List<Vertex>();
        }
        public Vertex mainVertex;
        public List<Vertex> subVertices;

        public static bool AlreadyBelongsToAGroup(Vertex vertex, List<DependencyGroup> dependencyGroups, bool exceptHisOwnGroup = false, DependencyGroup ownGroup = null)
        {
            if (exceptHisOwnGroup)
            {
                foreach (DependencyGroup group in dependencyGroups.Where(group => group != ownGroup))
                {
                    if (group.mainVertex == vertex || group.subVertices.Contains(vertex))
                        return true;
                }
                return false;
            }
            else
            {
                foreach (DependencyGroup group in dependencyGroups)
                {
                    if (group.mainVertex == vertex || group.subVertices.Contains(vertex))
                        return true;
                }
                return false;
            }
        }
    }
}
