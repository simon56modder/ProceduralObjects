using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using UnityEngine.Rendering;
using UnityEngine;
using System.IO;

using ProceduralObjects.Classes;
using ProceduralObjects.Tools;
using ProceduralObjects.Localization;

using ColossalFramework.UI;
using ColossalFramework.Globalization;
using ColossalFramework;

namespace ProceduralObjects
{
    public class ProceduralObjectsLogic : MonoBehaviour
    {
        public List<ProceduralObject> proceduralObjects, pObjSelection;
        public ProceduralObject currentlyEditingObject;
        public List<ProceduralInfo> availableProceduralInfos;
        public ProceduralInfo chosenProceduralInfo = null;

        public ClipboardProceduralObjects clipboard = null;
        public float storedHeight = 0f, yOffset = 0f;

        public bool proceduralTool = false, editingVertex = false, movingWholeModel = false, placingSelection = false, editingWholeModel = false, generalShowUI = true, showExternals = false;
        public List<int> editingVertexIndex;
        public Rect window = new Rect(155, 100, 400, 400);
        public Rect externalsWindow = new Rect(555, 100, 400, 400);
        public string externalsSaveTextfield = "Enter object name here";
        public Vector2 scrollVertices = Vector2.zero, scrollObjects = Vector2.zero, scrollTextures = Vector2.zero, scrollTextureResources = Vector2.zero, scrollExternals = Vector2.zero;
        public Vertex[] temp_storageVertex;
        // public System.Type previousToolType;
        public List<Texture2D> basicTextures;
        public AxisEditionState axisState = AxisEditionState.none;
        public Vector3 axisHitPoint = Vector3.zero;
        public LineRenderer xLine, yLine, zLine;
        private RotationWizardData rotWizardData = null;
        private VerticesWizardData vertWizardData = null;
        private ProceduralObject SingleHoveredObj = null;

        public Camera renderCamera;

        //  public Dictionary<Vertex, Vector3> vertexShifting;

        public GUIStyle redLabelStyle = new GUIStyle();
        public int actionMode = 0;

        // drag selection
        public Vector2 topLeftRegion = Vector2.zero, bottomRightRegion = Vector2.zero;
        public bool clickingRegion = false;

        ExternalProceduralObjectsManager ExPObjManager;
        ProceduralObjectsButton mainButton;

        private int renamingExternal = -1;
        private string renamingExternalString = "";

        private Material spriteMat;

        void Start()
        {
            Debug.Log("[ProceduralObjects] Game start procedure started.");
            pObjSelection = new List<ProceduralObject>();
            UIView view = UIView.GetAView();
            mainButton = view.AddUIComponent(typeof(ProceduralObjectsButton)) as ProceduralObjectsButton;
            mainButton.logic = this;
            renderCamera = Camera.main;
            KeyBindingsManager.Initialize();
            basicTextures = basicTextures.LoadModConfigTextures().OrderBy(tex => tex.name).ToList();
            availableProceduralInfos = ProceduralUtils.CreateProceduralInfosList();
            Debug.Log("[ProceduralObjects] Found " + availableProceduralInfos.Count.ToString() + " procedural infos.");
            ExPObjManager = new ExternalProceduralObjectsManager();
            ExPObjManager.LoadExternals(basicTextures);
            spriteMat = new Material(Shader.Find("Sprites/Default"));
            spriteMat.color = new Color(1f, 0, 0, .35f);

            if (ProceduralObjectsMod.tempContainerData != null)
            {
                this.LoadContainerData(ProceduralObjectsMod.tempContainerData);
                ProceduralObjectsMod.tempContainerData = null;
            }
            else
            {
                proceduralObjects = new List<ProceduralObject>();
            }
            redLabelStyle.normal.textColor = Color.red;
            editingVertexIndex = new List<int>();
            SetupLocalization();
            LocaleManager.eventLocaleChanged += SetupLocalization;
            Debug.Log("[ProceduralObjects] Game start procedure ended.");
        }

        void Update()
        {
            var currentToolType = ToolsModifierControl.toolController.CurrentTool.GetType();

            if ((currentToolType == typeof(PropTool)) || (currentToolType == typeof(BuildingTool)))
                mainButton.text = LocalizationManager.instance.current["convert_pobj"];
            else
                mainButton.text = "Procedural Objects";


            if (KeyBindingsManager.instance.GetBindingFromName("convertToProcedural").GetBindingDown())
            {
                ConvertToProcedural(ToolsModifierControl.toolController.CurrentTool);
            }

            if (KeyBindingsManager.instance.GetBindingFromName("generalShowHideUI").GetBindingDown())
            {
                generalShowUI = !generalShowUI;
            }
            if (proceduralObjects != null)
            {
                for (int i = 0; i < proceduralObjects.Count; i++)
                {
                    var obj = proceduralObjects[i];
                    if (Vector3.Distance(Camera.main.transform.position, obj.m_position) <= obj.renderDistance)
                    {
                        try
                        {
                            if ((obj.m_visibility == ProceduralObjectVisibility.Always)
                                || (obj.m_visibility == ProceduralObjectVisibility.NightOnly && Singleton<SimulationManager>.instance.m_isNightTime)
                                || (obj.m_visibility == ProceduralObjectVisibility.DayOnly && !Singleton<SimulationManager>.instance.m_isNightTime))
                                        Graphics.DrawMesh(obj.m_mesh, obj.m_position, obj.m_rotation, obj.m_material, 0, renderCamera, 0, null, true, true);
                            if (SingleHoveredObj == obj)
                                Graphics.DrawMesh(obj.m_mesh, obj.m_position, obj.m_rotation, spriteMat, 0, renderCamera, 0, null, true, true);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError("[ProceduralObjects] Error while rendering object " + obj.id.ToString() + " (" + obj.basePrefabName + " of type " + obj.baseInfoType
                                + " : " + e.Message + " - Stack Trace : " + e.StackTrace + " Values sent to DrawMesh were : " + (obj.m_mesh == null).ToString() + "," +
                                (obj.m_position).ToString() + "," +
                                (obj.m_rotation).ToString() + "," +
                                (obj.m_material == null).ToString() + "," +
                                (renderCamera == null).ToString()); 
                        }
                    }

                }
            }
            if (currentToolType == typeof(ProceduralTool))
            {
                // PASTE object
                if (KeyBindingsManager.instance.GetBindingFromName("paste").GetBindingDown())
                {
                    Paste(clipboard);
                }
                /* if (previousToolType != null)
                   {
                       if (previousToolType != ToolsModifierControl.toolController.CurrentTool.GetType())
                       {
                           previousToolType = ToolsModifierControl.toolController.CurrentTool.GetType();
                           editingVertex = false;
                           editingVertexIndex.Clear();
                           editingWholeModel = false;
                           proceduralTool = false;
                           showWindow = false;
                           Gizmos.DestroyGizmo();
                           xLine = null;
                           yLine = null;
                           zLine = null;
                       }
                   } */
            }
            if (proceduralTool)
            {
                // COPY objects
                if (KeyBindingsManager.instance.GetBindingFromName("copy").GetBindingDown())
                {
                    if (currentlyEditingObject != null && !Gizmos.Exists)
                    {
                        storedHeight = currentlyEditingObject.m_position.y;
                        clipboard = new ClipboardProceduralObjects(ClipboardProceduralObjects.ClipboardType.Single);
                        clipboard.single_object = new CacheProceduralObject(currentlyEditingObject);
                    }
                }
                if (KeyBindingsManager.instance.GetBindingFromName("deleteObject").GetBindingDown())
                    DeleteObject();

                if (movingWholeModel)
                {
                    if (Input.GetMouseButton(0))
                    {
                        ConfirmMovingWhole();
                    }
                    else
                    {
                        if (Input.GetMouseButton(1))
                        {
                            if (rotWizardData == null)
                            {
                                rotWizardData = RotationWizardData.GetCurrentRotationData(currentlyEditingObject);
                            }
                            else
                            {
                                float diff = (rotWizardData.GUIMousePositionX - Input.mousePosition.x);
                                if (diff < 0)
                                {
                                    currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, (diff * 300f) / Screen.width, 0);
                                }
                                else
                                {
                                    currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, -(((-diff) * 300f) / Screen.width), 0);
                                }
                                rotWizardData.UpdateMouseCoords();
                            }
                        }
                        else if (Input.GetMouseButtonUp(1))
                            rotWizardData = null;

                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveUp").GetBinding())
                            yOffset += Time.deltaTime * 8.7f;
                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveDown").GetBinding())
                            yOffset -= Time.deltaTime * 8.7f;


                        ToolBase.RaycastInput rayInput = new ToolBase.RaycastInput(Camera.main.ScreenPointToRay(Input.mousePosition), Camera.main.farClipPlane);
                        try
                        {
                            ToolBase.RaycastOutput rayOutput;
                            if (ProceduralObjRayCast.TerrainRaycast(rayInput, out rayOutput))
                            {
                                if (!rayOutput.m_currentEditObject)
                                {
                                    if (KeyBindingsManager.instance.GetBindingFromName("snapStoredHeight").GetBinding() && storedHeight != 0)
                                        currentlyEditingObject.m_position = new Vector3(rayOutput.m_hitPos.x, storedHeight, rayOutput.m_hitPos.z);
                                    else
                                        currentlyEditingObject.m_position = new Vector3(rayOutput.m_hitPos.x, rayOutput.m_hitPos.y + yOffset, rayOutput.m_hitPos.z);
                                }
                            }
                        }
                        catch { }
                        if (placingSelection && currentlyEditingObject.tempObj != null)
                        {
                            currentlyEditingObject.tempObj.transform.position = currentlyEditingObject.m_position;
                            currentlyEditingObject.tempObj.transform.rotation = currentlyEditingObject.m_rotation;
                            for (int i = 0; i < proceduralObjects.Count; i++)
                            {
                                if (proceduralObjects[i].tempObj == null)
                                    continue;
                                if (proceduralObjects[i].tempObj.transform.parent = currentlyEditingObject.tempObj.transform)
                                {
                                    proceduralObjects[i].m_position = proceduralObjects[i].tempObj.transform.position;
                                    proceduralObjects[i].m_rotation = proceduralObjects[i].tempObj.transform.rotation;
                                }
                            }
                        }
                    }
                }
                else
                {

                    Vector2 objGuiPosition = currentlyEditingObject.m_position.WorldToGuiPoint();
                    Rect toolsRect = new Rect(objGuiPosition.x + 8, objGuiPosition.y - 30, 110, 85);
                    if (!toolsRect.IsMouseInside())
                    {
                        if (Input.GetMouseButtonDown(0))
                        {
                            if (axisState == AxisEditionState.none)
                            {
                                RaycastHit hit;
                                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
                                {
                                    if (hit.transform.gameObject.name == "ProceduralAxis_X")
                                    {
                                        axisState = AxisEditionState.X;
                                        axisHitPoint = Gizmos.AxisHitPoint(hit.point, currentlyEditingObject.m_position);
                                    }
                                    else if (hit.transform.gameObject.name == "ProceduralAxis_Y")
                                    {
                                        axisState = AxisEditionState.Y;
                                        axisHitPoint = Gizmos.AxisHitPoint(hit.point, currentlyEditingObject.m_position);
                                    }
                                    else if (hit.transform.gameObject.name == "ProceduralAxis_Z")
                                    {
                                        axisState = AxisEditionState.Z;
                                        axisHitPoint = Gizmos.AxisHitPoint(hit.point, currentlyEditingObject.m_position);
                                    }
                                }
                            }
                        }
                    }
                    if (Input.GetMouseButtonUp(0))
                    {
                        if (axisState != AxisEditionState.none)
                        {
                            axisState = AxisEditionState.none;
                            axisHitPoint = Vector3.zero;
                            //   Debug.LogError("LMB up ");
                        }
                    }

                    if (editingWholeModel)
                    {
                        if (KeyBindingsManager.instance.GetBindingFromName("switchGeneralToVertexTools").GetBindingDown())
                        {
                            if (actionMode == 2)
                            {
                                if (Gizmos.Exists)
                                {
                                    Gizmos.DestroyGizmo();
                                    xLine = null;
                                    yLine = null;
                                    zLine = null;
                                }
                                editingWholeModel = false;
                            }
                            else
                            {
                                if (Gizmos.Exists)
                                {
                                    Gizmos.DestroyGizmo();
                                    xLine = null;
                                    yLine = null;
                                    zLine = null;
                                }
                                actionMode += 1;
                            }
                        }
                        #region Gizmo movement - WHOLE MODEL
                        if (Input.GetMouseButton(0))
                        {
                            if (axisState != AxisEditionState.none)
                            {
                                if (actionMode == 0 && axisHitPoint != Vector3.zero)
                                {
                                    switch (axisState)
                                    {
                                        // POSITION
                                        case AxisEditionState.X:
                                            currentlyEditingObject.m_position = new Vector3(Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y,
                                                Vector3.Distance(Camera.main.transform.position, currentlyEditingObject.m_position))).x + axisHitPoint.x,
                                                currentlyEditingObject.m_position.y,
                                                currentlyEditingObject.m_position.z);
                                            break;
                                        case AxisEditionState.Y:
                                            currentlyEditingObject.m_position = new Vector3(currentlyEditingObject.m_position.x,
                                                Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y,
                                                Vector3.Distance(Camera.main.transform.position, currentlyEditingObject.m_position))).y + axisHitPoint.y,
                                                currentlyEditingObject.m_position.z);
                                            break;
                                        case AxisEditionState.Z:
                                            currentlyEditingObject.m_position = new Vector3(currentlyEditingObject.m_position.x,
                                                currentlyEditingObject.m_position.y,
                                                Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y,
                                                Vector3.Distance(Camera.main.transform.position, currentlyEditingObject.m_position))).z + axisHitPoint.z);
                                            break;
                                    }
                                }
                            }
                        }
                        if (KeyBindingsManager.instance.GetBindingFromName("switchActionMode").GetBindingDown())
                        {
                            SwitchActionMode();
                        }
                        GameObject xAxis = GameObject.Find("ProceduralAxis_X");
                        if (xAxis != null)
                            xAxis.transform.position = currentlyEditingObject.m_position;
                        GameObject yAxis = GameObject.Find("ProceduralAxis_Y");
                        if (yAxis != null)
                            yAxis.transform.position = currentlyEditingObject.m_position;
                        GameObject zAxis = GameObject.Find("ProceduralAxis_Z");
                        if (zAxis != null)
                            zAxis.transform.position = currentlyEditingObject.m_position;
                        GameObject centerCollid = GameObject.Find("ProceduralGizmoCenter");
                        if (centerCollid != null)
                            centerCollid.transform.position = currentlyEditingObject.m_position;

                        if (xLine != null)
                            Gizmos.UpdateLinePositions(currentlyEditingObject.m_position, xLine, yLine, zLine);
                        #endregion
                    }
                    else
                    {
                        if (KeyBindingsManager.instance.GetBindingFromName("switchGeneralToVertexTools").GetBindingDown())
                            SwitchToMainTool();

                        if (currentlyEditingObject != null)
                        {
                            if (Input.GetMouseButton(0))
                            {
                                if (editingVertexIndex.Count > 0)
                                {
                                    if (vertWizardData == null)
                                    {
                                        ToolBase.RaycastInput rayInput = new ToolBase.RaycastInput(Camera.main.ScreenPointToRay(Input.mousePosition), Camera.main.farClipPlane);
                                        ToolBase.RaycastOutput rayOutput;
                                        if (ProceduralObjRayCast.TerrainRaycast(rayInput, out rayOutput))
                                        {
                                            vertWizardData = new VerticesWizardData();
                                            vertWizardData.Store(rayOutput.m_hitPos, temp_storageVertex.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))).ToArray(),
                                                currentlyEditingObject);
                                        }
                                        //foreach (Vertex v in temp_storageVertex.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        // { }
                                    }
                                    else
                                    {
                                        vertWizardData.IncrementStep();
                                        if (vertWizardData.enableMovement)
                                        {
                                            ToolBase.RaycastInput rayInput = new ToolBase.RaycastInput(Camera.main.ScreenPointToRay(Input.mousePosition), Camera.main.farClipPlane);
                                            ToolBase.RaycastOutput rayOutput;
                                            if (ProceduralObjRayCast.TerrainRaycast(rayInput, out rayOutput))
                                            {
                                                vertWizardData.ApplyToNewPosition(rayOutput.m_hitPos, currentlyEditingObject);
                                                Apply();
                                            }
                                        }
                                    }
                                }
                            }
                            else if (Input.GetMouseButtonUp(0))
                            {
                                vertWizardData = null;
                            }
                            else if (Input.GetMouseButton(1))
                            {
                                if (!clickingRegion)
                                {
                                    topLeftRegion = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                                    bottomRightRegion = topLeftRegion;
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
                                if (!Input.GetKey(KeyCode.LeftControl))
                                    editingVertexIndex.Clear();
                                editingVertex = true;
                                editingWholeModel = false;
                                foreach (Vertex vertex in temp_storageVertex.Where(v => !v.IsDependent))
                                {
                                    if (region.Contains(VertexWorldPosition(vertex).WorldToGuiPoint(), true))
                                    {
                                        editingVertexIndex.Add(vertex.Index);
                                    }
                                }
                            }
                        }
                        #region vertex gizmos
                        /* if (vertexShifting != null)
                           {
                               if (vertexShifting.Count > 0)
                               {

                                   #region Gizmo movement - VERTICES
                                   GameObject xAxis = GameObject.Find("ProceduralAxis_X");
                                   GameObject yAxis = GameObject.Find("ProceduralAxis_Y");
                                   GameObject zAxis = GameObject.Find("ProceduralAxis_Z");
                                   if (Input.GetMouseButton(0))
                                   {
                                       if (axisState != AxisEditionState.none)
                                       {
                                           Vector3 vertexWorldPosition = currentlyEditingObject.m_rotation * (Vector3.Scale(temp_storageVertex[editingVertexIndex[0]].Position, currentlyEditingObject.gameObject.transform.localScale)) + currentlyEditingObject.m_position;
                                           switch (axisState)
                                           {
                                               // POSITION
                                               case AxisEditionState.X:
                                                   temp_storageVertex[editingVertexIndex[0]].Position = new Vector3(Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y,
                                                       Vector3.Distance(Camera.main.transform.position, vertexWorldPosition))).x,
                                                       vertexWorldPosition.y,
                                                       vertexWorldPosition.z).WorldToLocalVertexPosition(currentlyEditingObject);
                                                   foreach (KeyValuePair<Vertex, Vector3> kvp in vertexShifting)
                                                       kvp.Key.Position = temp_storageVertex[editingVertexIndex[0]].Position + kvp.Value;
                                                   Apply();
                                                   break;
                                               case AxisEditionState.Y:
                                                   temp_storageVertex[editingVertexIndex[0]].Position = new Vector3(vertexWorldPosition.x,
                                                       Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y,
                                                       Vector3.Distance(Camera.main.transform.position, vertexWorldPosition))).y,
                                                       vertexWorldPosition.z).WorldToLocalVertexPosition(currentlyEditingObject);
                                                   foreach (KeyValuePair<Vertex, Vector3> kvp in vertexShifting)
                                                       kvp.Key.Position = temp_storageVertex[editingVertexIndex[0]].Position + kvp.Value; 
                                                   Apply();
                                                   break;
                                               case AxisEditionState.Z:
                                                   temp_storageVertex[editingVertexIndex[0]].Position = new Vector3(vertexWorldPosition.x,
                                                       vertexWorldPosition.y,
                                                       Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y,
                                                       Vector3.Distance(Camera.main.transform.position, vertexWorldPosition))).z)
                                                       .WorldToLocalVertexPosition(currentlyEditingObject);
                                                      foreach (KeyValuePair<Vertex, Vector3> kvp in vertexShifting)
                                                          kvp.Key.Position = temp_storageVertex[editingVertexIndex[0]].Position + kvp.Value; 
                                                   Apply();
                                                   break;
                                           }
                                       }
                                   }
                                   if (xAxis != null)
                                   {
                                       xAxis.transform.position = currentlyEditingObject.m_rotation * (Vector3.Scale(temp_storageVertex[editingVertexIndex[0]].Position, currentlyEditingObject.gameObject.transform.localScale)) + currentlyEditingObject.m_position;
                                       yAxis.transform.position = xAxis.transform.position;
                                       zAxis.transform.position = xAxis.transform.position;
                                   }

                                   if (xLine != null)
                                       Gizmos.UpdateLinePositions(xAxis.transform.position, xLine, yLine, zLine); 
                                   #endregion
                               }
                           } */

                        // |---------------------------|
                        #endregion
                    }
                    if (KeyBindingsManager.instance.GetBindingFromName("edition_smoothMovements").GetBinding())
                    {
                        // SMOOTH
                        if (!KeyBindingsManager.instance.GetBindingFromName("edition_smallMovements").GetBinding())
                        {
                            #region deplacement SMOOTH + VITE
                            if (editingVertex)
                            {
                                #region déplacement des vertex
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveUp").GetBinding())
                                {
                                    foreach (Vertex v in temp_storageVertex.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        temp_storageVertex[v.Index].Position.y += 5f * Time.deltaTime;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveDown").GetBinding())
                                {
                                    foreach (Vertex v in temp_storageVertex.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        temp_storageVertex[v.Index].Position.y -= 5f * Time.deltaTime;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveLeft").GetBinding())
                                {
                                    foreach (Vertex v in temp_storageVertex.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        temp_storageVertex[v.Index].Position.z += 5f * Time.deltaTime;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveRight").GetBinding())
                                {
                                    foreach (Vertex v in temp_storageVertex.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        temp_storageVertex[v.Index].Position.z -= 5f * Time.deltaTime;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveForward").GetBinding())
                                {
                                    foreach (Vertex v in temp_storageVertex.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        temp_storageVertex[v.Index].Position.x += 5f * Time.deltaTime;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveBackward").GetBinding())
                                {
                                    foreach (Vertex v in temp_storageVertex.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        temp_storageVertex[v.Index].Position.x -= 5f * Time.deltaTime;
                                    Apply();
                                }
                                #endregion
                            }
                            else if (editingWholeModel)
                            {
                                #region déplacement du modèle
                                switch (actionMode)
                                {
                                    case 0:
                                        // POSITION

                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveUp").GetBinding())
                                        {
                                            currentlyEditingObject.m_position += new Vector3(0, 9f * Time.deltaTime, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveDown").GetBinding())
                                        {
                                            currentlyEditingObject.m_position += new Vector3(0, -9f * Time.deltaTime, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveLeft").GetBinding())
                                        {
                                            currentlyEditingObject.m_position += new Vector3(0, 0, 9f * Time.deltaTime);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveRight").GetBinding())
                                        {
                                            currentlyEditingObject.m_position += new Vector3(0, 0, -9f * Time.deltaTime);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveForward").GetBinding())
                                        {
                                            currentlyEditingObject.m_position += new Vector3(9f * Time.deltaTime, 0, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveBackward").GetBinding())
                                        {
                                            currentlyEditingObject.m_position += new Vector3(-9f * Time.deltaTime, 0, 0);
                                        }
                                        break;
                                    case 1:
                                        // SCALE

                                        if (KeyBindingsManager.instance.GetBindingFromName("scale_scaleUp").GetBinding())
                                        {
                                            currentlyEditingObject.Scale(temp_storageVertex, 1.3f * Time.deltaTime);
                                            Apply();
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("scale_scaleDown").GetBinding())
                                        {
                                            currentlyEditingObject.Scale(temp_storageVertex, .7f * Time.deltaTime);
                                            Apply();
                                        }
                                        break;
                                    case 2:
                                        // ROTATION

                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveUp").GetBinding())
                                        {
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(20f * Time.deltaTime, 0, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveDown").GetBinding())
                                        {
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(-20f * Time.deltaTime, 0, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveLeft").GetBinding())
                                        {
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, 20f * Time.deltaTime, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveRight").GetBinding())
                                        {
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, -20f * Time.deltaTime, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveForward").GetBinding())
                                        {
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, 0, 20f * Time.deltaTime);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveBackward").GetBinding())
                                        {
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, 0, -20f * Time.deltaTime);
                                        }
                                        break;
                                }
                                #endregion
                            }
                            #endregion
                        }
                        else
                        {
                            #region deplacement SMOOTH + LENT
                            if (editingVertex)
                            {
                                #region déplacement des vertex
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveUp").GetBinding())
                                {
                                    foreach (Vertex v in temp_storageVertex.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        temp_storageVertex[v.Index].Position.y += 3f * Time.deltaTime;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveDown").GetBinding())
                                {
                                    foreach (Vertex v in temp_storageVertex.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        temp_storageVertex[v.Index].Position.y -= 3f * Time.deltaTime;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveLeft").GetBinding())
                                {
                                    foreach (Vertex v in temp_storageVertex.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        temp_storageVertex[v.Index].Position.z += 3f * Time.deltaTime;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveRight").GetBinding())
                                {
                                    foreach (Vertex v in temp_storageVertex.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        temp_storageVertex[v.Index].Position.z -= 3f * Time.deltaTime;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveForward").GetBinding())
                                {
                                    foreach (Vertex v in temp_storageVertex.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        temp_storageVertex[v.Index].Position.x += 3f * Time.deltaTime;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveBackward").GetBinding())
                                {
                                    foreach (Vertex v in temp_storageVertex.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        temp_storageVertex[v.Index].Position.x -= 3f * Time.deltaTime;
                                    Apply();
                                }
                                #endregion
                            }
                            else if (editingWholeModel)
                            {
                                #region déplacement du modèle
                                switch (actionMode)
                                {
                                    case 0:
                                        // POSITION

                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveUp").GetBinding())
                                        {
                                            currentlyEditingObject.m_position += new Vector3(0, 1.8f * Time.deltaTime, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveDown").GetBinding())
                                        {
                                            currentlyEditingObject.m_position += new Vector3(0, -1.8f * Time.deltaTime, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveLeft").GetBinding())
                                        {
                                            currentlyEditingObject.m_position += new Vector3(0, 0, 1.8f * Time.deltaTime);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveRight").GetBinding())
                                        {
                                            currentlyEditingObject.m_position += new Vector3(0, 0, -1.8f * Time.deltaTime);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveForward").GetBinding())
                                        {
                                            currentlyEditingObject.m_position += new Vector3(1.8f * Time.deltaTime, 0, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveBackward").GetBinding())
                                        {
                                            currentlyEditingObject.m_position += new Vector3(-1.8f * Time.deltaTime, 0, 0);
                                        }
                                        break;
                                    case 1:
                                        // SCALE

                                        if (KeyBindingsManager.instance.GetBindingFromName("scale_scaleUp").GetBinding())
                                        {
                                            currentlyEditingObject.Scale(temp_storageVertex, 1.12f * Time.deltaTime);
                                            Apply();
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("scale_scaleDown").GetBinding())
                                        {
                                            currentlyEditingObject.Scale(temp_storageVertex, .88f * Time.deltaTime);
                                            Apply();
                                        }
                                        break;
                                    case 2:
                                        // ROTATION

                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveUp").GetBinding())
                                        {
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(10f * Time.deltaTime, 0, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveDown").GetBinding())
                                        {
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(-10f * Time.deltaTime, 0, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveLeft").GetBinding())
                                        {
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, 10f * Time.deltaTime, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveRight").GetBinding())
                                        {
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, -10f * Time.deltaTime, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveForward").GetBinding())
                                        {
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, 0, 10f * Time.deltaTime);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveBackward").GetBinding())
                                        {
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, 0, -10f * Time.deltaTime);
                                        }
                                        break;
                                }
                                #endregion
                            }
                            #endregion
                        }
                    }
                    else
                    {
                        // SACADE
                        if (!KeyBindingsManager.instance.GetBindingFromName("edition_smallMovements").GetBinding())
                        {
                            #region deplacement SACADE + VITE
                            if (editingVertex)
                            {
                                #region déplacement des vertex
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveUp").GetBindingDown())
                                {
                                    foreach (Vertex v in temp_storageVertex.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        temp_storageVertex[v.Index].Position.y += 1f;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveDown").GetBindingDown())
                                {
                                    foreach (Vertex v in temp_storageVertex.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        temp_storageVertex[v.Index].Position.y -= 1f;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveLeft").GetBindingDown())
                                {
                                    foreach (Vertex v in temp_storageVertex.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        temp_storageVertex[v.Index].Position.z += 1f;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveRight").GetBindingDown())
                                {
                                    foreach (Vertex v in temp_storageVertex.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        temp_storageVertex[v.Index].Position.z -= 1f;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveForward").GetBindingDown())
                                {
                                    foreach (Vertex v in temp_storageVertex.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        temp_storageVertex[v.Index].Position.x += 1f;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveBackward").GetBindingDown())
                                {
                                    foreach (Vertex v in temp_storageVertex.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        temp_storageVertex[v.Index].Position.x -= 1f;
                                    Apply();
                                }
                                #endregion
                            }
                            else if (editingWholeModel)
                            {
                                #region déplacement du modèle
                                switch (actionMode)
                                {
                                    case 0:
                                        // position
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveUp").GetBindingDown())
                                        {
                                            currentlyEditingObject.m_position += new Vector3(0, 2f, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveDown").GetBindingDown())
                                        {
                                            currentlyEditingObject.m_position += new Vector3(0, -2f, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveLeft").GetBindingDown())
                                        {
                                            currentlyEditingObject.m_position += new Vector3(0, 0, 2f);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveRight").GetBindingDown())
                                        {
                                            currentlyEditingObject.m_position += new Vector3(0, 0, -2f);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveForward").GetBindingDown())
                                        {
                                            currentlyEditingObject.m_position += new Vector3(2f, 0, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveBackward").GetBindingDown())
                                        {
                                            currentlyEditingObject.m_position += new Vector3(-2f, 0, 0);
                                        }
                                        break;
                                    case 1:
                                        // SCALE

                                        if (KeyBindingsManager.instance.GetBindingFromName("scale_scaleUp").GetBindingDown())
                                        {
                                            currentlyEditingObject.Scale(temp_storageVertex, 1.12f);
                                            Apply();
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("scale_scaleDown").GetBindingDown())
                                        {
                                            currentlyEditingObject.Scale(temp_storageVertex, .88f);
                                            Apply();
                                        }
                                        break;
                                    case 2:
                                        // ROTATION

                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveUp").GetBindingDown())
                                        {
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(12f, 0, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveDown").GetBindingDown())
                                        {
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(-12f, 0, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveLeft").GetBindingDown())
                                        {
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, 12f, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveRight").GetBindingDown())
                                        {
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, -12f, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveForward").GetBindingDown())
                                        {
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, 0, 12f);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveBackward").GetBindingDown())
                                        {
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, 0, -12f);
                                        }
                                        break;
                                }
                                #endregion
                            }
                            #endregion
                        }
                        else
                        {
                            #region deplacement SACADE + LENT
                            if (editingVertex)
                            {
                                #region déplacement des vertex
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveUp").GetBindingDown())
                                {
                                    foreach (Vertex v in temp_storageVertex.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        temp_storageVertex[v.Index].Position.y += 0.5f;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveDown").GetBindingDown())
                                {
                                    foreach (Vertex v in temp_storageVertex.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        temp_storageVertex[v.Index].Position.y -= 0.5f;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveLeft").GetBindingDown())
                                {
                                    foreach (Vertex v in temp_storageVertex.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        temp_storageVertex[v.Index].Position.z += 0.5f;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveRight").GetBindingDown())
                                {
                                    foreach (Vertex v in temp_storageVertex.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        temp_storageVertex[v.Index].Position.z -= 0.5f;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveForward").GetBindingDown())
                                {
                                    foreach (Vertex v in temp_storageVertex.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        temp_storageVertex[v.Index].Position.x += 0.5f;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveBackward").GetBindingDown())
                                {
                                    foreach (Vertex v in temp_storageVertex.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        temp_storageVertex[v.Index].Position.x -= 0.5f;
                                    Apply();
                                }
                                #endregion
                            }
                            else if (editingWholeModel)
                            {
                                #region déplacement du modèle
                                switch (actionMode)
                                {
                                    case 0:
                                        // POSITION

                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveUp").GetBindingDown())
                                        {
                                            currentlyEditingObject.m_position += new Vector3(0, .6f, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveDown").GetBindingDown())
                                        {
                                            currentlyEditingObject.m_position += new Vector3(0, -0.6f, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveLeft").GetBindingDown())
                                        {
                                            currentlyEditingObject.m_position += new Vector3(0, 0, 0.6f);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveRight").GetBindingDown())
                                        {
                                            currentlyEditingObject.m_position += new Vector3(0, 0, -0.6f);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveForward").GetBindingDown())
                                        {
                                            currentlyEditingObject.m_position += new Vector3(0.6f, 0, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveBackward").GetBindingDown())
                                        {
                                            currentlyEditingObject.m_position += new Vector3(-0.6f, 0, 0);
                                        }
                                        break;
                                    case 1:
                                        // SCALE

                                        if (KeyBindingsManager.instance.GetBindingFromName("scale_scaleUp").GetBindingDown())
                                        {
                                            currentlyEditingObject.Scale(temp_storageVertex, 1.06f);
                                            Apply();
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("scale_scaleDown").GetBindingDown())
                                        {
                                            currentlyEditingObject.Scale(temp_storageVertex, .94f);
                                            Apply();
                                        }
                                        break;
                                    case 2:
                                        // ROTATION

                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveUp").GetBindingDown())
                                        {
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(5f, 0, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveDown").GetBindingDown())
                                        {
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(-5f, 0, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveLeft").GetBindingDown())
                                        {
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, 5f, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveRight").GetBindingDown())
                                        {
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, -5f, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveForward").GetBindingDown())
                                        {
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, 0, 5f);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveBackward").GetBindingDown())
                                        {
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, 0, -5f);
                                        }
                                        break;
                                }
                                #endregion
                            }
                            #endregion
                        }
                    }
                }
            }
        }

        void OnGUI()
        {
            if (!ToolsModifierControl.cameraController.m_freeCamera && generalShowUI)
            {
                /* if (toolType != typeof(ProceduralTool))
                {
                    if (GUI.Button(new Rect(Screen.width - 130, 60, 125, 30), "Procedural Objects"))
                    {
                        ToolHelper.FullySetTool<ProceduralTool>();
                        ToolsModifierControl.mainToolbar.CloseEverything();
                    }
                } 
                else*/
                if (ToolsModifierControl.toolController.CurrentTool.GetType() == typeof(ProceduralTool))
                {
                    if (!proceduralTool)
                    {
                        ProceduralObject hoveredObj = null;
                        foreach (ProceduralObject obj in proceduralObjects.ToList())
                        {
                            //  try { Transform _t = obj.gameObject.transform; }
                            //  catch { continue; }

                            var objScreenPos = obj.m_position.WorldToGuiPoint();
                            if (!window.Contains(objScreenPos))
                            {
                                if (new Rect(objScreenPos + new Vector2(-15, -15), new Vector2(31, 30)).IsMouseInside())
                                    hoveredObj = obj;
                                if (pObjSelection.Contains(obj))
                                {
                                    if (pObjSelection[0] == obj)
                                    {
                                        if (pObjSelection.Count == 1)
                                        {
                                            #region
                                            if (GUI.Button(new Rect(objScreenPos + new Vector2(12, -11), new Vector2(130, 22)), LocalizationManager.instance.current["edit"]))
                                            {
                                                currentlyEditingObject = obj;
                                                pObjSelection.Clear();
                                                temp_storageVertex = Vertex.CreateVertexList(currentlyEditingObject);
                                                proceduralTool = true;
                                                hoveredObj = null;
                                            }
                                            if (GUI.Button(new Rect(objScreenPos + new Vector2(12, 12), new Vector2(130, 22)), LocalizationManager.instance.current["delete"]))
                                            {
                                                proceduralObjects.Remove(obj);
                                                //   Object.Destroy(obj.gameObject);
                                                pObjSelection.Remove(obj);
                                                hoveredObj = null;
                                            }
                                            if (GUI.Button(new Rect(objScreenPos + new Vector2(12, 35), new Vector2(130, 22)), LocalizationManager.instance.current["copy"]))
                                            {
                                                storedHeight = obj.m_position.y;
                                                clipboard = new ClipboardProceduralObjects(ClipboardProceduralObjects.ClipboardType.Single);
                                                clipboard.single_object = new CacheProceduralObject(obj);
                                                hoveredObj = null;
                                            }
                                            if (GUI.Button(new Rect(objScreenPos + new Vector2(12, 58), new Vector2(130, 22)), LocalizationManager.instance.current["move_to"]))
                                            {
                                                pObjSelection.Clear();
                                                currentlyEditingObject = obj;
                                                placingSelection = false;
                                                movingWholeModel = true;
                                                editingWholeModel = true;
                                                proceduralTool = true;
                                                hoveredObj = null;
                                            }
                                            #endregion
                                        }
                                        else
                                        {
                                            #region
                                            if (GUI.Button(new Rect(objScreenPos + new Vector2(12, -11), new Vector2(130, 22)), LocalizationManager.instance.current["delete_selection"]))
                                            {
                                                for (int i = 0; i < pObjSelection.Count; i++)
                                                {
                                                    proceduralObjects.Remove(pObjSelection[i]);
                                                    // Object.Destroy(pObjSelection[i].gameObject);
                                                }
                                                pObjSelection.Clear();
                                            }
                                            if (GUI.Button(new Rect(objScreenPos + new Vector2(12, 12), new Vector2(130, 22)), LocalizationManager.instance.current["copy_selection"]))
                                            {
                                                clipboard = new ClipboardProceduralObjects(ClipboardProceduralObjects.ClipboardType.Selection);
                                                clipboard.MakeSelectionList(pObjSelection);
                                                storedHeight = obj.m_position.y;
                                            }

                                            if (GUI.Button(new Rect(objScreenPos + new Vector2(12, 35), new Vector2(130, 22)), LocalizationManager.instance.current["export_selection"]))
                                            {
                                                var selection = new ClipboardProceduralObjects(ClipboardProceduralObjects.ClipboardType.Selection);
                                                selection.MakeSelectionList(pObjSelection);
                                                selection.ExportSelection("Selection " + DateTime.Now.ToString("F"), ExPObjManager);
                                            }
                                            #endregion
                                        }
                                    }
                                    GUI.color = Color.red;
                                }
                                if (GUI.Button(new Rect(objScreenPos + new Vector2(-11, -11), new Vector2(23, 22)), "<size=20>+</size>"))
                                {
                                    if (Input.GetKey(KeyCode.LeftControl))
                                    {
                                        if (pObjSelection.Contains(obj))
                                            pObjSelection.Remove(obj);
                                        else
                                            pObjSelection.Add(obj);
                                    }
                                    else
                                    {
                                        if (pObjSelection.Count == 1)
                                        {
                                            pObjSelection.Clear();
                                        }
                                        else
                                        {
                                            pObjSelection.Clear();
                                            pObjSelection.Add(obj);
                                        }

                                    }

                                    /*
                                    currentlyEditingObject = obj;
                                    temp_storageVertex = Vertex.CreateVertexList(currentlyEditingObject);
                                    proceduralTool = true;
                                     */
                                }
                                GUI.color = Color.white;
                            }
                        }
                        SingleHoveredObj = hoveredObj;
                    }
                    if (!movingWholeModel)
                    {
                        var winrect = GUI.Window(1094334744, window, DrawUIWindow, "Procedural Objects v" + ProceduralObjectsMod.VERSION);
                        if (proceduralTool && editingWholeModel && !movingWholeModel)
                            window = new Rect(winrect.x, winrect.y, winrect.width, 463);
                        else
                            window = new Rect(winrect.x, winrect.y, winrect.width, 400);

                        if (showExternals)
                            externalsWindow = GUI.Window(1094334745, externalsWindow, DrawExternalsWindow, LocalizationManager.instance.current["saved_pobjs"]);

                        #region GUI when TOOL is active
                        if (currentlyEditingObject != null)
                        {
                            if (!editingWholeModel)
                            {
                                if (clickingRegion)
                                    GUI.Box(CreateRectFromVector2s(topLeftRegion, bottomRightRegion), "");

                                foreach (Vertex vertex in temp_storageVertex)
                                {
                                    if (vertex != null)
                                    {
                                        if (!vertex.IsDependent)
                                        {
                                            if (currentlyEditingObject.m_mesh.name == "ploppablecliffgrass" && vertex.Index >= currentlyEditingObject.allVertices.Count() - 2)
                                                continue;
                                            if ((editingVertex && !editingVertexIndex.Contains(vertex.Index)) || !editingVertex)
                                            {
                                                if (GUI.Button(new Rect(VertexWorldPosition(vertex).WorldToGuiPoint() + new Vector2(-11, -11), new Vector2(23, 22)), "<size=20>+</size>"))
                                                {
                                                    editingVertex = true;
                                                    editingWholeModel = false;
                                                    if (editingVertexIndex.Count == 0 || Input.GetKey(KeyCode.LeftControl))
                                                        editingVertexIndex.Add(vertex.Index);
                                                    else
                                                    {
                                                        editingVertexIndex.Clear();
                                                        editingVertexIndex.Add(vertex.Index);
                                                    }
                                                    Gizmos.DestroyGizmo();
                                                    xLine = null;
                                                    yLine = null;
                                                    zLine = null;
                                                    /*  if (Gizmos.Exists)
                                                            vertexShifting = CreateVertexShiftingDictionary(temp_storageVertex, editingVertexIndex, false, false);
                                                        else
                                                            vertexShifting = CreateVertexShiftingDictionary(temp_storageVertex, editingVertexIndex, true, true); */
                                                }
                                            }
                                            else
                                            {
                                                GUI.contentColor = Color.red;
                                                if (GUI.Button(new Rect(VertexWorldPosition(vertex).WorldToGuiPoint() + new Vector2(-11, -11), new Vector2(23, 22)), "<size=20><b>x</b></size>"))
                                                {
                                                    editingVertexIndex.Remove(vertex.Index);
                                                    if (editingVertexIndex.Count == 0)
                                                        editingVertex = false;
                                                    //  vertexShifting = CreateVertexShiftingDictionary(temp_storageVertex, editingVertexIndex, true, true);
                                                }
                                                GUI.contentColor = Color.white;

                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Vector2 objPosition = currentlyEditingObject.m_position.WorldToGuiPoint();
                                if (GUI.Button(new Rect(objPosition + new Vector2(13, -26), new Vector2(100, 23)), LocalizationManager.instance.current["move_to"]))
                                {
                                    movingWholeModel = true;
                                    placingSelection = false;
                                    Gizmos.DestroyGizmo();
                                    xLine = null;
                                    yLine = null;
                                    zLine = null;
                                }
                                if (GUI.Button(new Rect(objPosition + new Vector2(13, 0), new Vector2(100, 23)), LocalizationManager.instance.current["delete"]))
                                    DeleteObject();

                                string modeText = "<i>";
                                switch (actionMode)
                                {
                                    case 0:
                                        modeText += LocalizationManager.instance.current["position"] + "</i>";
                                        break;
                                    case 1:
                                        modeText += LocalizationManager.instance.current["scale"] + "</i>";
                                        break;
                                    case 2:
                                        modeText += LocalizationManager.instance.current["rotation"] + "</i>";
                                        break;
                                }
                                Rect modeRect = new Rect(objPosition + new Vector2(13, 26), new Vector2(100, 23));
                                if (modeRect.IsMouseInside())
                                    modeText += " ►";
                                if (GUI.Button(modeRect, modeText))
                                    SwitchActionMode();
                            }
                        }
                        #endregion
                    }
                    else
                        GUI.Label(new Rect(Input.mousePosition.x + 18, Screen.height - Input.mousePosition.y + 18, 300, 30), LocalizationManager.instance.current["click_to_place"]);
                }
            }
        }
        public void DrawUIWindow(int id)
        {
            #region setup window
            GUI.DragWindow(new Rect(0, 0, 350, 30));
            if (GUI.Button(new Rect(356, 3, 30, 30), "X"))
            {
                CloseExternalsWindow();
                editingVertex = false;
                editingVertexIndex.Clear();
                editingWholeModel = false;
                proceduralTool = false;
                currentlyEditingObject = null;
                chosenProceduralInfo = null;
                vertWizardData = null;
                pObjSelection.Clear();
                ToolHelper.FullySetTool<DefaultTool>();
                Gizmos.DestroyGizmo();
                xLine = null;
                yLine = null;
                zLine = null;
            }
            #endregion


            if (proceduralTool)
            {
                GUI.BeginGroup(new Rect(10, 30, 380, 442));
                if (editingWholeModel)
                {
                    /* if (movingWholeModel)
                       {
                           GUI.Label(new Rect(0, 0, 300, 30), "<b><size=18>" + LocalizationManager.instance.current["move_to_tool"] + "</size></b>");
                           GUI.Label(new Rect(0, 30, 380, 250), "<b>" + LocalizationManager.instance.current["controls"] + ":</b>\n" + LocalizationManager.instance.current["LM_click"] + " : " + LocalizationManager.instance.current["confirm_placement"]);
                       }
                       else
                       { */
                    GUI.Label(new Rect(35, 0, 300, 30), "<b><size=18>" + LocalizationManager.instance.current["general_tool"] + "</size></b>");
                    GUI.contentColor = Color.green;
                    GUI.Label(new Rect(0, 0, 23, 23), "<size=18>¤</size>", GUI.skin.button);
                    GUI.contentColor = Color.white;
                    GUI.Label(new Rect(0, 30, 380, 330), "<b>" + LocalizationManager.instance.current["controls"] + ":</b>\n" + KeyBindingsManager.instance.GetBindingFromName("edition_smoothMovements").m_fullKeys + " : " + LocalizationManager.instance.current["hold_for_smooth"] + "\n" +
                        KeyBindingsManager.instance.GetBindingFromName("edition_smallMovements").m_fullKeys + " : " + LocalizationManager.instance.current["hold_slow_together"] + "\n\n" +
                        KeyBindingsManager.instance.GetBindingFromName("switchActionMode").m_fullKeys + " : " + LocalizationManager.instance.current["switch_modes"] + "\n" +
                        KeyBindingsManager.instance.GetBindingFromName("position_moveUp").m_fullKeys + ", " +
                        KeyBindingsManager.instance.GetBindingFromName("position_moveDown").m_fullKeys + ", " +
                        KeyBindingsManager.instance.GetBindingFromName("position_moveRight").m_fullKeys + ", " +
                        KeyBindingsManager.instance.GetBindingFromName("position_moveLeft").m_fullKeys + ", " +
                        KeyBindingsManager.instance.GetBindingFromName("position_moveForward").m_fullKeys + ", " +
                        KeyBindingsManager.instance.GetBindingFromName("position_moveBackward").m_fullKeys + " : " + LocalizationManager.instance.current["move_objects_pos"] + "\n" +
                        KeyBindingsManager.instance.GetBindingFromName("rotation_moveUp").m_fullKeys + ", " +
                        KeyBindingsManager.instance.GetBindingFromName("rotation_moveDown").m_fullKeys + ", " +
                        KeyBindingsManager.instance.GetBindingFromName("rotation_moveRight").m_fullKeys + ", " +
                        KeyBindingsManager.instance.GetBindingFromName("rotation_moveLeft").m_fullKeys + ", " +
                        KeyBindingsManager.instance.GetBindingFromName("rotation_moveForward").m_fullKeys + ", " +
                        KeyBindingsManager.instance.GetBindingFromName("rotation_moveBackward").m_fullKeys + " : " + LocalizationManager.instance.current["rotate_objects_rot"] + "\n" +
                        KeyBindingsManager.instance.GetBindingFromName("scale_scaleUp").m_fullKeys + "/" +
                        KeyBindingsManager.instance.GetBindingFromName("scale_scaleDown").m_fullKeys + " : " + LocalizationManager.instance.current["scale_objects"] + "\n" +
                        "\n" + KeyBindingsManager.instance.GetBindingFromName("switchGeneralToVertexTools").m_fullKeys + " : " + LocalizationManager.instance.current["quick_switch"] + "\n\n<b>" + LocalizationManager.instance.current["buttons"] + " : </b>\n" + LocalizationManager.instance.current["delete_desc"] + "\n" + LocalizationManager.instance.current["move_to_desc"]);
                    GUI.Label(new Rect(0, 330, 380, 30), LocalizationManager.instance.current["render_distance"] + " : " + currentlyEditingObject.renderDistance.ToString("N").Replace(".00", ""));
                    currentlyEditingObject.renderDistance = GUI.HorizontalSlider(new Rect(0, 350, 380, 30), Mathf.Floor(currentlyEditingObject.renderDistance), 50f, 16000f);

                    externalsSaveTextfield = GUI.TextField(new Rect(0, 370, 285, 28), externalsSaveTextfield);
                    if (File.Exists(ProceduralObjectsMod.ExternalsConfigPath + externalsSaveTextfield.ToFileName() + ".pobj"))
                    {
                        GUI.color = Color.red;
                        GUI.Label(new Rect(290, 370, 90, 28), "X", GUI.skin.button);
                        GUI.color = Color.white;
                    }
                    else
                    {
                        if (GUI.Button(new Rect(290, 370, 90, 28), LocalizationManager.instance.current["save"]))
                        {
                            ExPObjManager.SaveToExternal(externalsSaveTextfield, new CacheProceduralObject(currentlyEditingObject));
                            externalsSaveTextfield = LocalizationManager.instance.current["enter_name"];
                        }
                    }
                    if (GUI.Button(new Rect(0, 400, 120, 28), "◄ " + LocalizationManager.instance.current["back"]))
                    {
                        CloseExternalsWindow();
                        ToolHelper.FullySetTool<ProceduralTool>();
                        Gizmos.DestroyGizmo();
                        xLine = null;
                        yLine = null;
                        zLine = null;
                        editingVertex = false;
                        editingVertexIndex.Clear();
                        editingWholeModel = false;
                        proceduralTool = false;
                        currentlyEditingObject = null;
                        chosenProceduralInfo = null;
                        pObjSelection.Clear();
                    }
                    GUI.EndGroup();
                    if (GUI.Button(new Rect(15, 335, 185, 25), LocalizationManager.instance.current.visibilityString(currentlyEditingObject.m_visibility)))
                    {
                        if (currentlyEditingObject.m_visibility == ProceduralObjectVisibility.Always)
                            currentlyEditingObject.m_visibility = ProceduralObjectVisibility.DayOnly;
                        else if (currentlyEditingObject.m_visibility == ProceduralObjectVisibility.DayOnly)
                            currentlyEditingObject.m_visibility = ProceduralObjectVisibility.NightOnly;
                        else if (currentlyEditingObject.m_visibility == ProceduralObjectVisibility.NightOnly)
                            currentlyEditingObject.m_visibility = ProceduralObjectVisibility.Always;
                    }
                }
                else
                {
                    GUI.Label(new Rect(35, 0, 300, 30), "<b><size=18>" + LocalizationManager.instance.current["vertex_tool"] + "</size></b>");
                    GUI.Label(new Rect(0, 0, 23, 23), "<size=18>+</size>", GUI.skin.button);
                    GUI.Label(new Rect(0, 30, 380, 330), "<b>" + LocalizationManager.instance.current["controls"] + ":</b>\n" + KeyBindingsManager.instance.GetBindingFromName("edition_smoothMovements").m_fullKeys + " : " + LocalizationManager.instance.current["hold_for_smooth"] +
                        "\n" + KeyBindingsManager.instance.GetBindingFromName("edition_smallMovements").m_fullKeys + " : " + LocalizationManager.instance.current["hold_slow_together"] + "\n\nCtrl : " + LocalizationManager.instance.current["hold_slow_together"] +
                        "\n" + LocalizationManager.instance.current["move_vertices"] + "\n" +
                        KeyBindingsManager.instance.GetBindingFromName("position_moveUp").m_fullKeys + "/" + KeyBindingsManager.instance.GetBindingFromName("position_moveDown").m_fullKeys +
                        " : " + LocalizationManager.instance.current["move_vertices_updown"] + "\n" +
                    KeyBindingsManager.instance.GetBindingFromName("switchGeneralToVertexTools").m_fullKeys + " : " + LocalizationManager.instance.current["quick_switch"] +
                    "\n" + KeyBindingsManager.instance.GetBindingFromName("copy").m_fullKeys + " : " + LocalizationManager.instance.current["copy_obj"] + "\n" +
                    KeyBindingsManager.instance.GetBindingFromName("paste").m_fullKeys + " : " + LocalizationManager.instance.current["paste_obj"]
                    + "\n" + LocalizationManager.instance.current["delete_desc"] + "\n\n" +
                    LocalizationManager.instance.current["rmb_marquee_vertices"] + "\n" +
                    LocalizationManager.instance.current["lmb_drag_vertices"]);

                    GUI.EndGroup();
                    if (GUI.Button(new Rect(15, 335, 185, 25), LocalizationManager.instance.current["delete"]))
                        DeleteObject();
                    if (GUI.Button(new Rect(15, 370, 120, 25), "◄ " + LocalizationManager.instance.current["back"]))
                    {
                        ToolHelper.FullySetTool<ProceduralTool>();
                        editingVertex = false;
                        editingVertexIndex.Clear();
                        editingWholeModel = false;
                        proceduralTool = false;
                        currentlyEditingObject = null;
                        chosenProceduralInfo = null;
                        pObjSelection.Clear();
                    }
                }
                if (GUI.Button(new Rect(205, 335, 185, 25), (editingWholeModel ? LocalizationManager.instance.current["vertex_customization"] : LocalizationManager.instance.current["general_tool"])))
                {
                    if (editingWholeModel)
                    {
                        if (Gizmos.Exists)
                        {
                            Gizmos.DestroyGizmo();
                            xLine = null;
                            yLine = null;
                            zLine = null;
                        }
                        editingWholeModel = false;
                    }
                    else
                        SwitchToMainTool();
                }
            }
            else
            {
                if (chosenProceduralInfo == null)
                {
                    GUI.Label(new Rect(5, 26, 350, 38), LocalizationManager.instance.current["spawn_new"]);

                    if (TextureUtils.LocalTexturesCount == 0)
                        GUI.Label(new Rect(10, 45, 350, 45), LocalizationManager.instance.current["no_tex"] + "\n" + LocalizationManager.instance.current["cant_create_basic"]);
                    else
                        GUI.Label(new Rect(10, 60, 350, 30), LocalizationManager.instance.current["local_tex"] + " : ");

                    if (GUI.Button(new Rect(150, 55, 75, 28), LocalizationManager.instance.current["refresh"]))
                        basicTextures = basicTextures.LoadModConfigTextures();
                    if (GUI.Button(new Rect(230, 55, 155, 28), LocalizationManager.instance.current["open_tex"]))
                    {
                        if (Directory.Exists(ProceduralObjectsMod.TextureConfigPath))
                            Application.OpenURL("file://" + ProceduralObjectsMod.TextureConfigPath);
                    }
                    if (GUI.Button(new Rect(10, 84, 375, 24), LocalizationManager.instance.current["go_to_wiki"]))
                        Application.OpenURL(ProceduralObjectsMod.DOCUMENTATION_URL);
                    if (GUI.Button(new Rect(10, 110, 375, 24), LocalizationManager.instance.current["open_kbd_cfg"]))
                    {
                        if (File.Exists(KeyBindingsManager.BindingsConfigPath))
                            Application.OpenURL("file://" + KeyBindingsManager.BindingsConfigPath);
                    }

                    if (TextureUtils.TextureResources.Count > 0)
                    {
                        GUI.Label(new Rect(10, 135, 375, 28), LocalizationManager.instance.current["wk_tex_loaded"] + " : " + TextureUtils.TextureResources.Count.ToString());
                        GUI.Box(new Rect(10, 160, 375, 170), string.Empty);
                        scrollTextureResources = GUI.BeginScrollView(new Rect(10, 160, 375, 170), scrollTextureResources, new Rect(0, 0, 350, TextureUtils.TextureResources.Count * 30));
                        for (int i = 0; i < TextureUtils.TextureResources.Count; i++)
                        {
                            GUI.Label(new Rect(5, i * 30, 248, 28), TextureUtils.TextureResources[i].HasCustomName ? TextureUtils.TextureResources[i].m_name : "<i>" + LocalizationManager.instance.current["package_no_custom_name"] + "</i>");
                            GUI.Label(new Rect(255, i * 30, 99, 28),
                                (TextureUtils.TextureResources[i].TexturesCount > 1) ? "(" + TextureUtils.TextureResources[i].TexturesCount + " " + LocalizationManager.instance.current["textures"] + ")" : "(" + TextureUtils.TextureResources[i].TexturesCount + " " + LocalizationManager.instance.current["texture"] + ")");
                        }
                        GUI.EndScrollView();
                    }
                    else
                        GUI.Label(new Rect(10, 130, 375, 28), LocalizationManager.instance.current["no_wk_tex_loaded"]);

                    GUI.Label(new Rect(10, 331, 375, 35), basicTextures.Count().ToString() + " " + LocalizationManager.instance.current["tex_in_total"] + " : " + TextureUtils.LocalTexturesCount.ToString() + " " + LocalizationManager.instance.current["local"] + " + " + TextureResourceInfo.TotalTextureCount(TextureUtils.TextureResources) + " " + LocalizationManager.instance.current["from_wk"] + "\n<size=10>" + LocalizationManager.instance.current["total_obj_count"] + " : " + proceduralObjects.Count.ToString("N").Replace(".00", "") + "</size>");

                    if (GUI.Button(new Rect(10, 365, 375, 28), LocalizationManager.instance.current["saved_pobjs"]))
                    {
                        renamingExternalString = "";
                        renamingExternal = -1;
                        ExPObjManager.LoadExternals(basicTextures);
                        showExternals = true;
                    }
                }
                else
                {
                    if (chosenProceduralInfo.infoType == "PROP")
                        GUI.Label(new Rect(10, 30, 350, 30), LocalizationManager.instance.current["choose_tex_to_apply"] + " \"" + chosenProceduralInfo.propPrefab.GetLocalizedTitle() + "\"");
                    else if (chosenProceduralInfo.infoType == "BUILDING")
                        GUI.Label(new Rect(10, 30, 350, 30), LocalizationManager.instance.current["choose_tex_to_apply"] + " \"" + chosenProceduralInfo.buildingPrefab.GetLocalizedTitle() + "\"");
                    // Texture selection
                    scrollTextures = GUI.BeginScrollView(new Rect(10, 60, 350, 330), scrollTextures, new Rect(0, 0, 320, 80 * basicTextures.Count() + 65));
                    GUI.Label(new Rect(10, 0, 300, 28), basicTextures.Count().ToString() + " " + LocalizationManager.instance.current["tex_in_total"] + " : " + TextureUtils.LocalTexturesCount.ToString() + " " + LocalizationManager.instance.current["local"] + " + " + TextureResourceInfo.TotalTextureCount(TextureUtils.TextureResources) + " " + LocalizationManager.instance.current["from_wk"]);
                    if (GUI.Button(new Rect(10, 30, 147.5f, 30), LocalizationManager.instance.current["open_folder"]))
                    {
                        if (Directory.Exists(ProceduralObjectsMod.TextureConfigPath))
                            Application.OpenURL("file://" + ProceduralObjectsMod.TextureConfigPath);
                    }
                    if (GUI.Button(new Rect(162.5f, 30, 147.5f, 30), LocalizationManager.instance.current["refresh"]))
                        basicTextures = basicTextures.LoadModConfigTextures();
                    for (int i = 0; i < basicTextures.Count(); i++)
                    {
                        if (GUI.Button(new Rect(10, i * 80 + 62, 300, 79), string.Empty))
                        {
                            editingVertex = false;
                            editingVertexIndex.Clear();
                            editingWholeModel = false;
                            proceduralTool = false;
                            ToolHelper.FullySetTool<DefaultTool>();
                            Gizmos.DestroyGizmo();
                            xLine = null;
                            yLine = null;
                            zLine = null;
                            SpawnObject(chosenProceduralInfo, basicTextures[i]);
                            temp_storageVertex = Vertex.CreateVertexList(currentlyEditingObject);
                            ToolHelper.FullySetTool<ProceduralTool>();
                            proceduralTool = true;
                            movingWholeModel = true;
                            placingSelection = false;
                            editingVertex = false;
                            chosenProceduralInfo = null;
                        }
                        GUI.Label(new Rect(15, i * 80 + 65, 85, 74), basicTextures[i]);
                        int pos = basicTextures[i].name.LastIndexOf(ProceduralObjectsMod.IsLinux ? "/" : @"\") + 1;
                        GUI.Label(new Rect(105, i * 80 + 72, 190, 52), basicTextures[i].name.Substring(pos, basicTextures[i].name.Length - pos).Replace(".png", ""));
                    }
                    GUI.EndScrollView();
                }
            }
        }
        public void DrawExternalsWindow(int id)
        {
            GUI.DragWindow(new Rect(0, 0, 350, 30));
            if (GUI.Button(new Rect(356, 3, 30, 30), "X"))
                CloseExternalsWindow();
            GUI.Label(new Rect(10, 30, 298, 37), LocalizationManager.instance.current["externals_desc"]);
            if (renamingExternal == -1)
            {
                if (GUI.Button(new Rect(310, 35, 85, 28), LocalizationManager.instance.current["refresh"]))
                    ExPObjManager.LoadExternals(basicTextures);
            }
            if (ExPObjManager.m_externals.Count == 0)
            {
                GUI.Box(new Rect(10, 70, 380, 320), LocalizationManager.instance.current["no_externals_warning"]);
            }
            else
            {
                GUI.Box(new Rect(10, 70, 380, 320), string.Empty);
                scrollExternals = GUI.BeginScrollView(new Rect(10, 70, 380, 320), scrollExternals, new Rect(0, 0, 350, 40 * ExPObjManager.m_externals.Count + 5));
                for (int i = 0; i < ExPObjManager.m_externals.Count; i++)
                {
                    GUI.Box(new Rect(5, i * 40 + 2, 344, 36), string.Empty);
                    if (renamingExternal == i)
                    {
                        renamingExternalString = GUI.TextField(new Rect(8, i * 40 + 6, 249, 36), renamingExternalString);
                    }
                    else
                    {
                        GUI.Label(new Rect(8, i * 40 + 6, 180, 36), ExPObjManager.m_externals[i].m_name);
                        if (GUI.Button(new Rect(190, i * 40 + 5, 67, 30), LocalizationManager.instance.current["place"]))
                        {
                            if (ExPObjManager.m_externals[i].m_externalType == ClipboardProceduralObjects.ClipboardType.Single)
                            {
                                PlaceCacheObject(ExPObjManager.m_externals[i].m_object, true);
                            }
                            else
                            {
                                Paste(ExPObjManager.m_externals[i].m_selection);
                            }
                        }
                    }
                    if (ExPObjManager.m_externals[i].isWorkshop)
                        GUI.Label(new Rect(258, i * 40 + 5, 67, 30), "[<i>Workshop</i>]", GUI.skin.button);
                    else
                    {
                        if (GUI.Button(new Rect(258, i * 40 + 5, 64, 30), LocalizationManager.instance.current[(renamingExternal == i) ? "ok" : "rename"]))
                        {
                            if (renamingExternal == -1)
                            {
                                renamingExternalString = ExPObjManager.m_externals[i].m_name;
                                renamingExternal = i;
                            }
                            else if (renamingExternal == i)
                            {
                                ExPObjManager.RenameExternal(ExPObjManager.m_externals[i], renamingExternalString);
                                renamingExternal = -1;
                                renamingExternalString = "";
                            }
                            // ExPObjManager.DeleteExternal(ExPObjManager.m_externals[i], basicTextures);
                        }
                        if (renamingExternal != i)
                        {
                            GUI.color = Color.red;
                            if (GUI.Button(new Rect(324, i * 40 + 5, 25, 30), "X"))
                            {
                                ExPObjManager.DeleteExternal(ExPObjManager.m_externals[i], basicTextures);
                            }
                            GUI.color = Color.white;
                        }
                    }
                }
                GUI.EndScrollView();
            }
        }

        public void Apply()
        {
            List<Vector3> posArray = new List<Vector3>(temp_storageVertex.GetPositionsArray());
            // sets mesh renderer vertices
            currentlyEditingObject.m_mesh.SetVertices(posArray);
            // sets mesh collider vertices
            //UV map recalculation
            if (currentlyEditingObject.RequiresUVRecalculation)
            {
                try
                {
                    currentlyEditingObject.m_mesh.uv = Vertex.RecalculateUVMap(currentlyEditingObject, temp_storageVertex);
                }
                catch
                {
                    Debug.LogError("[ProceduralObjects] Error : Couldn't recalculate UV map on a procedural object of type " + currentlyEditingObject.basePrefabName + " (" + currentlyEditingObject.baseInfoType + ")");
                }
            }
            //we used to reconstruct vertex list here, but it caused a lot of lag
            // temp_storageVertex = Vertex.CreateVertexList(currentlyEditingObject);
        }
        public void StoreLineComponents(GameObject[] gizmos)
        {
            xLine = gizmos[0].GetComponent<LineRenderer>();
            yLine = gizmos[1].GetComponent<LineRenderer>();
            zLine = gizmos[2].GetComponent<LineRenderer>();
        }
        public void SpawnObject(ProceduralInfo infoBase, Texture2D customTex = null)
        {
            var v = new ProceduralObject();
            if (infoBase.infoType == "PROP")
                v.ConstructObject(infoBase.propPrefab, proceduralObjects.GetNextUnusedId(), customTex);
            else if (infoBase.infoType == "BUILDING")
                v.ConstructObject(infoBase.buildingPrefab, proceduralObjects.GetNextUnusedId(), customTex);
            proceduralObjects.Add(v);
            currentlyEditingObject = v;
        }
        public ProceduralObject PlaceCacheObject(CacheProceduralObject cacheObj, bool setCurrentlyEditing)
        {
            if (cacheObj.basePrefabName == "PROP")
            {
                if (!Resources.FindObjectsOfTypeAll<PropInfo>().Any(info => info.name == cacheObj.basePrefabName))
                    return null;
            }
            else if (cacheObj.basePrefabName == "BUILDING")
            {
                if (!Resources.FindObjectsOfTypeAll<BuildingInfo>().Any(info => info.name == cacheObj.basePrefabName))
                    return null;
            }
            ToolHelper.FullySetTool<ProceduralTool>();
            ToolsModifierControl.mainToolbar.CloseEverything();
            var obj = new ProceduralObject(cacheObj, proceduralObjects.GetNextUnusedId(), ToolsModifierControl.cameraController.m_currentPosition + new Vector3(0, -8, 0));
            proceduralObjects.Add(obj);
            if (setCurrentlyEditing)
                currentlyEditingObject = obj;
            temp_storageVertex = Vertex.CreateVertexList(obj);
            movingWholeModel = true;
            placingSelection = false;
            proceduralTool = true;
            if (obj.RequiresUVRecalculation)
                obj.m_mesh.uv = Vertex.RecalculateUVMap(obj, temp_storageVertex);
            return obj;
        }
        public void SwitchToMainTool()
        {
            /*  if (editingVertexIndex.Count == 0)
                { */
            editingVertexIndex.Clear();
            actionMode = 0;
            StoreLineComponents(Gizmos.CreateGizmo(currentlyEditingObject.m_position, true));
            editingVertex = false;
            editingWholeModel = true;
            /* }
               else
               {
                   editingVertexIndex.Clear();
                   vertexShifting = CreateVertexShiftingDictionary(temp_storageVertex, editingVertexIndex, false, false);
                   GameObject xAxis = GameObject.Find("ProceduralAxis_X");
                   if (xAxis != null)
                       xAxis.transform.position = currentlyEditingObject.m_position;
                   GameObject yAxis = GameObject.Find("ProceduralAxis_Y");
                   if (yAxis != null)
                       yAxis.transform.position = currentlyEditingObject.m_position;
                   GameObject zAxis = GameObject.Find("ProceduralAxis_Z");
                   if (zAxis != null)
                       zAxis.transform.position = currentlyEditingObject.m_position;
                   xLine = GameObject.Find("ProceduralAxis_X").GetComponent<LineRenderer>();
                   yLine = GameObject.Find("ProceduralAxis_Y").GetComponent<LineRenderer>();
                   zLine = GameObject.Find("ProceduralAxis_Z").GetComponent<LineRenderer>();
                   Gizmos.UpdateLinePositions(xAxis.transform.position, xLine, yLine, zLine);
                   editingVertex = false;
                   editingWholeModel = true;
               } */
        }
        public void SwitchActionMode()
        {
            if (actionMode == 2)
            {
                actionMode = 0;
                StoreLineComponents(Gizmos.CreateGizmo(currentlyEditingObject.m_position, true));
            }
            else
            {
                if (Gizmos.Exists)
                {
                    Gizmos.DestroyGizmo();
                    xLine = null;
                    yLine = null;
                    zLine = null;
                }
                actionMode += 1;
            }
        }
        public void MainButtonClick()
        {
            var currentToolType = ToolsModifierControl.toolController.CurrentTool.GetType();

            if ((currentToolType == typeof(PropTool)) || (currentToolType == typeof(BuildingTool)))
            {
                ConvertToProcedural(ToolsModifierControl.toolController.CurrentTool);
            }
            else if (currentToolType != typeof(ProceduralTool))
            {
                ToolHelper.FullySetTool<ProceduralTool>();
                ToolsModifierControl.mainToolbar.CloseEverything();
            }
            else
            {
                CloseExternalsWindow();
                editingVertex = false;
                editingVertexIndex.Clear();
                editingWholeModel = false;
                proceduralTool = false;
                currentlyEditingObject = null;
                chosenProceduralInfo = null;
                rotWizardData = null;
                ToolHelper.FullySetTool<DefaultTool>();
                Gizmos.DestroyGizmo();
                xLine = null;
                yLine = null;
                zLine = null;
            }
        }
        private void CloseExternalsWindow()
        {
            showExternals = false;
            renamingExternal = -1;
            renamingExternalString = "";
        }

        private void ConvertToProcedural(ToolBase tool)
        {
            CloseExternalsWindow();
            if (availableProceduralInfos == null)
                availableProceduralInfos = ProceduralUtils.CreateProceduralInfosList();
            if (availableProceduralInfos.Count == 0)
                availableProceduralInfos = ProceduralUtils.CreateProceduralInfosList();

            if (tool.GetType() == typeof(PropTool))
            {
                ProceduralInfo info = availableProceduralInfos.Where(pInf => pInf.propPrefab != null).FirstOrDefault(pInf => pInf.propPrefab == ((PropTool)tool).m_prefab);
                if (info.isBasicShape && basicTextures.Count > 0)
                {
                    editingVertex = false;
                    editingVertexIndex.Clear();
                    editingWholeModel = false;
                    proceduralTool = false;
                    currentlyEditingObject = null;
                    ToolHelper.FullySetTool<DefaultTool>();
                    Gizmos.DestroyGizmo();
                    xLine = null;
                    yLine = null;
                    zLine = null;
                    chosenProceduralInfo = info;
                    ToolHelper.FullySetTool<ProceduralTool>();
                }
                else
                {
                    editingVertex = false;
                    editingVertexIndex.Clear();
                    editingWholeModel = false;
                    proceduralTool = false;
                    ToolHelper.FullySetTool<DefaultTool>();
                    Gizmos.DestroyGizmo();
                    xLine = null;
                    yLine = null;
                    zLine = null;
                    SpawnObject(info);
                    temp_storageVertex = Vertex.CreateVertexList(currentlyEditingObject);
                    ToolHelper.FullySetTool<ProceduralTool>();
                    proceduralTool = true;
                    movingWholeModel = true;
                    placingSelection = false;
                    editingVertex = false;
                }
            }
            else if (tool.GetType() == typeof(BuildingTool))
            {
                ProceduralInfo info = availableProceduralInfos.Where(pInf => pInf.buildingPrefab != null).FirstOrDefault(pInf => pInf.buildingPrefab == ((BuildingTool)tool).m_prefab);
                if (info.isBasicShape && basicTextures.Count > 0)
                {
                    chosenProceduralInfo = info;
                    ToolHelper.FullySetTool<ProceduralTool>();
                }
                else
                {
                    editingVertex = false;
                    editingVertexIndex.Clear();
                    editingWholeModel = false;
                    proceduralTool = false;
                    ToolHelper.FullySetTool<DefaultTool>();
                    Gizmos.DestroyGizmo();
                    xLine = null;
                    yLine = null;
                    zLine = null;
                    SpawnObject(info);
                    temp_storageVertex = Vertex.CreateVertexList(currentlyEditingObject);
                    ToolHelper.FullySetTool<ProceduralTool>();
                    proceduralTool = true;
                    movingWholeModel = true;
                    placingSelection = false;
                    editingVertex = false;
                }
            }
        }
        private void Paste(ClipboardProceduralObjects clipboard)
        {
            if (clipboard != null)
            {
                if (clipboard.type == ClipboardProceduralObjects.ClipboardType.Single)
                {
                    placingSelection = false;
                    pObjSelection.Clear();
                    currentlyEditingObject = PlaceCacheObject(clipboard.single_object, true);
                }
                else
                {
                    pObjSelection.Clear();
                    for (int i = 0; i < clipboard.selection_objects.Count; i++)
                    {
                        var cache = clipboard.selection_objects.ToList()[i].Key;
                        ProceduralObject obj = null;
                        if (i == 0)
                        {
                            obj = PlaceCacheObject(cache, true);
                            obj.tempObj = new GameObject();
                            obj.tempObj.transform.position = obj.m_position;
                            obj.tempObj.transform.rotation = obj.m_rotation;
                        }
                        else
                        {
                            obj = PlaceCacheObject(cache, false);
                            obj.m_position = currentlyEditingObject.m_position + clipboard.selection_objects[cache];
                            obj.tempObj = new GameObject();
                            obj.tempObj.transform.position = obj.m_position;
                            obj.tempObj.transform.rotation = obj.m_rotation;
                            obj.tempObj.transform.SetParent(currentlyEditingObject.tempObj.transform, true);
                        }
                    }
                    placingSelection = true;
                }
            }
        }
        public Vector3 VertexWorldPosition(Vertex vertex)
        {
            if (currentlyEditingObject.isPloppableAsphalt)
                return currentlyEditingObject.m_rotation * vertex.Position.PloppableAsphaltPosition() + currentlyEditingObject.m_position;
            return currentlyEditingObject.m_rotation * vertex.Position + currentlyEditingObject.m_position;
        }
        private void ConfirmMovingWhole()
        {
            if (placingSelection)
            {
                //   Debug.Log("Validated move To with Selection");
                pObjSelection.Clear();
                ToolHelper.FullySetTool<ProceduralTool>();
                editingVertex = false;
                editingVertexIndex.Clear();
                editingWholeModel = false;
                proceduralTool = false;
                for (int i = 0; i < proceduralObjects.Count; i++)
                {
                    if (proceduralObjects[i] == currentlyEditingObject)
                        continue;
                    if (proceduralObjects[i].tempObj == null)
                        continue;
                    if (proceduralObjects[i].tempObj.transform.parent = currentlyEditingObject.tempObj.transform)
                    {
                        proceduralObjects[i].tempObj.transform.parent = null;
                        UnityEngine.Object.Destroy(proceduralObjects[i].tempObj);
                        proceduralObjects[i].tempObj = null;
                    }
                }
                UnityEngine.Object.Destroy(currentlyEditingObject.tempObj);
                currentlyEditingObject.tempObj = null;
                currentlyEditingObject = null;
                chosenProceduralInfo = null;
                movingWholeModel = false;
                placingSelection = false;
                CloseExternalsWindow();
                rotWizardData = null;
                yOffset = 0f;
            }
            else
            {
                //  Debug.Log("Validated move To without Selection");
                movingWholeModel = false;
                editingWholeModel = false;
                editingVertex = true;
                CloseExternalsWindow();
                rotWizardData = null;
                pObjSelection.Clear();
                yOffset = 0f;
                // StoreLineComponents(Gizmos.CreateGizmo(currentlyEditingObject.m_position, true));
                ToolHelper.FullySetTool<ProceduralTool>();
            }
        }
        public void DeleteObject()
        {
            editingVertex = false;
            editingVertexIndex.Clear();
            editingWholeModel = false;
            proceduralTool = false;
            movingWholeModel = false;
            placingSelection = false;
            proceduralObjects.Remove(currentlyEditingObject);
            //  Object.Destroy(currentlyEditingObject.gameObject);
            currentlyEditingObject = null;
            Gizmos.DestroyGizmo();
            xLine = null;
            yLine = null;
            zLine = null;
        }
        public Rect CreateRectFromVector2s(Vector2 topLeftCorner, Vector2 bottomRightCorner)
        {
            return new Rect(topLeftCorner, new Vector2(bottomRightCorner.x - topLeftRegion.x, bottomRightCorner.y - topLeftRegion.y));
        }

        private void SetupLocalization()
        {
            LocalizationManager.CreateManager();
            externalsSaveTextfield = LocalizationManager.instance.current["enter_name"];
        }
        /* public Dictionary<Vertex, Vector3> CreateVertexShiftingDictionary(Vertex[] vertices, List<int> selectedVertices, bool createGizmo, bool removePreviousGizmo)
         {
             var dictionary = new Dictionary<Vertex, Vector3>();
             if (selectedVertices.Count == 0)
             {
                 if (removePreviousGizmo)
                 {
                     Gizmos.DestroyGizmo();
                     xLine = null;
                     yLine = null;
                     zLine = null;
                 }
                 return dictionary;
             }
             Vertex sourceVertex = vertices.First(v => v.Index == selectedVertices[0]);
             Vector3 sourcePosition = sourceVertex.Position;
             foreach (Vertex v in vertices.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
             {
                 if (v != sourceVertex)
                     dictionary[v] = v.Position - sourcePosition;
             }

             if (removePreviousGizmo)
             {
                 Gizmos.DestroyGizmo();
                 xLine = null;
                 yLine = null;
                 zLine = null;
             }
             if (createGizmo)
             {
                 StoreLineComponents(Gizmos.CreateGizmo(sourcePosition + currentlyEditingObject.m_position, true);
             }
             return dictionary;
         } */
    }
    public enum AxisEditionState
    {
        X,
        Y,
        Z,
        none
    }
}
