using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.Rendering;
using UnityEngine;
using System.IO;

using ProceduralObjects.Classes;
using ProceduralObjects.Tools;

using ColossalFramework.UI;

namespace ProceduralObjects
{
    public class ProceduralObjectsLogic : MonoBehaviour
    {
        public List<ProceduralObject> proceduralObjects;
        public ProceduralObject currentlyEditingObject;
        public List<ProceduralInfo> availableProceduralInfos;
        public ProceduralInfo chosenProceduralInfo = null;

        public CacheProceduralObject copiedObject = null;
        public float storedHeight = 0f, yOffset = 0f;

        public bool proceduralTool = false, editingVertex = false, movingWholeModel = false, editingWholeModel = false, generalShowUI = true, showExternals = false;
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

    //  public Dictionary<Vertex, Vector3> vertexShifting;

        public GUIStyle redLabelStyle = new GUIStyle();
        public int actionMode = 0;

        // drag selection
        public Vector2 topLeftRegion = Vector2.zero, bottomRightRegion = Vector2.zero;
        public bool clickingRegion = false;

        ExternalProceduralObjectsManager ExPObjManager;
        ProceduralObjectsButton mainButton;

        void Start()
        {
            UIView view = UIView.GetAView();
            mainButton = view.AddUIComponent(typeof(ProceduralObjectsButton)) as ProceduralObjectsButton;
            mainButton.logic = this;

            KeyBindingsManager.Initialize();
            basicTextures = basicTextures.LoadModConfigTextures().OrderBy(tex => tex.name).ToList();
            availableProceduralInfos = ProceduralUtils.CreateProceduralInfosList();
            Debug.Log("[ProceduralObjects] Found " + availableProceduralInfos.Count.ToString() + " procedural infos.");
            ExPObjManager = new ExternalProceduralObjectsManager();
            ExPObjManager.LoadExternals(basicTextures);

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
        //  previousToolType = typeof(DefaultTool);
        }

        void Update()
        {
            var currentToolType = ToolsModifierControl.toolController.CurrentTool.GetType();

            if ((currentToolType == typeof(PropTool)) || (currentToolType == typeof(BuildingTool)))
                mainButton.text = "Convert this to PO";
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
                    try
                    {
                        proceduralObjects[i].rendererComponent.enabled = (Vector3.Distance(Camera.main.transform.position, proceduralObjects[i].gameObject.transform.position) <= proceduralObjects[i].renderDistance) ? true : false;
                    }
                    catch {}
                }
            }
            if (currentToolType == typeof(ProceduralTool))
            {
                // PASTE object
                if (KeyBindingsManager.instance.GetBindingFromName("paste").GetBindingDown())
                {
                    if (copiedObject != null)
                    {
                        PlaceCacheObject(copiedObject);
                    }
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
                    if (currentlyEditingObject != null)
                    {
                        storedHeight = currentlyEditingObject.gameObject.transform.position.y;
                        copiedObject = new CacheProceduralObject(currentlyEditingObject);
                    }
                }
                if (KeyBindingsManager.instance.GetBindingFromName("deleteObject").GetBindingDown())
                    DeleteObject();

                if (movingWholeModel)
                {
                    if (Input.GetMouseButton(0))
                    {
                        try
                        {
                            movingWholeModel = false;
                            editingWholeModel = false;
                            editingVertex = true;
                            showExternals = false;
                            rotWizardData = null;
                            yOffset = 0f;
                            // StoreLineComponents(Gizmos.CreateGizmo(currentlyEditingObject.m_position, true));
                            ToolHelper.FullySetTool<ProceduralTool>();
                        }
                        catch { movingWholeModel = true; }
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
                                    currentlyEditingObject.gameObject.transform.Rotate(0, (diff * 245f) / Screen.width, 0);
                                }
                                else
                                {
                                    currentlyEditingObject.gameObject.transform.Rotate(0, -(((-diff) * 245f) / Screen.width), 0);
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
                                        currentlyEditingObject.gameObject.transform.position = new Vector3(rayOutput.m_hitPos.x, storedHeight, rayOutput.m_hitPos.z);
                                    else
                                        currentlyEditingObject.gameObject.transform.position = new Vector3(rayOutput.m_hitPos.x, rayOutput.m_hitPos.y + yOffset, rayOutput.m_hitPos.z);
                                }
                            }
                        }
                        catch { }
                        // ToolsModifierControl.cameraController.m_currentPosition + new Vector3(0, -8, 0);
                        currentlyEditingObject.m_position = currentlyEditingObject.gameObject.transform.position;

                    }
                }
                else
                {

                    Vector2 objGuiPosition = currentlyEditingObject.m_position.WorldToGuiPoint();
                    Rect toolsRect = new Rect(objGuiPosition.x + 8, objGuiPosition.y - 30, 110, 85);
                    if (!toolsRect.IsMouseInside())
                    {
                        if (Input.GetMouseButton(0))
                        {
                            if (axisState == AxisEditionState.none)
                            {
                                RaycastHit hit;
                                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
                                {
                                    if (hit.transform.gameObject.name == "ProceduralAxis_X")
                                    {
                                        axisState = AxisEditionState.X;
                                        axisHitPoint = Gizmos.AxisHitPoint(hit.point, currentlyEditingObject.gameObject.transform.position);
                                    }
                                    else if (hit.transform.gameObject.name == "ProceduralAxis_Y")
                                    {
                                        axisState = AxisEditionState.Y;
                                        axisHitPoint = Gizmos.AxisHitPoint(hit.point, currentlyEditingObject.gameObject.transform.position);
                                    }
                                    else if (hit.transform.gameObject.name == "ProceduralAxis_Z")
                                    {
                                        axisState = AxisEditionState.Z;
                                        axisHitPoint = Gizmos.AxisHitPoint(hit.point, currentlyEditingObject.gameObject.transform.position);
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
                                            currentlyEditingObject.gameObject.transform.position = new Vector3(Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y,
                                                Vector3.Distance(Camera.main.transform.position, currentlyEditingObject.m_position))).x + axisHitPoint.x,
                                                currentlyEditingObject.gameObject.transform.position.y,
                                                currentlyEditingObject.gameObject.transform.position.z);
                                            break;
                                        case AxisEditionState.Y:
                                            currentlyEditingObject.gameObject.transform.position = new Vector3(currentlyEditingObject.gameObject.transform.position.x,
                                                Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y,
                                                Vector3.Distance(Camera.main.transform.position, currentlyEditingObject.m_position))).y + axisHitPoint.y,
                                                currentlyEditingObject.gameObject.transform.position.z);
                                            break;
                                        case AxisEditionState.Z:
                                            currentlyEditingObject.gameObject.transform.position = new Vector3(currentlyEditingObject.gameObject.transform.position.x,
                                                currentlyEditingObject.gameObject.transform.position.y,
                                                Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y,
                                                Vector3.Distance(Camera.main.transform.position, currentlyEditingObject.m_position))).z + axisHitPoint.z);
                                            break;
                                    }
                                }
                                currentlyEditingObject.m_position = currentlyEditingObject.gameObject.transform.position;
                            }
                        }
                        if (KeyBindingsManager.instance.GetBindingFromName("switchActionMode").GetBindingDown())
                        {
                            SwitchActionMode();
                        }
                        GameObject xAxis = GameObject.Find("ProceduralAxis_X");
                        if (xAxis != null)
                            xAxis.transform.position = currentlyEditingObject.gameObject.transform.position;
                        GameObject yAxis = GameObject.Find("ProceduralAxis_Y");
                        if (yAxis != null)
                            yAxis.transform.position = currentlyEditingObject.gameObject.transform.position;
                        GameObject zAxis = GameObject.Find("ProceduralAxis_Z");
                        if (zAxis != null)
                            zAxis.transform.position = currentlyEditingObject.gameObject.transform.position;
                        GameObject centerCollid = GameObject.Find("ProceduralGizmoCenter");
                        if (centerCollid != null)
                            centerCollid.transform.position = currentlyEditingObject.gameObject.transform.position;

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
                            if (Input.GetMouseButton(1))
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
                                           Vector3 vertexWorldPosition = currentlyEditingObject.gameObject.transform.rotation * (Vector3.Scale(temp_storageVertex[editingVertexIndex[0]].Position, currentlyEditingObject.gameObject.transform.localScale)) + currentlyEditingObject.m_position;
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
                                       xAxis.transform.position = currentlyEditingObject.gameObject.transform.rotation * (Vector3.Scale(temp_storageVertex[editingVertexIndex[0]].Position, currentlyEditingObject.gameObject.transform.localScale)) + currentlyEditingObject.m_position;
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
                                            currentlyEditingObject.gameObject.transform.position += new Vector3(0, 9f * Time.deltaTime, 0);
                                            currentlyEditingObject.m_position = currentlyEditingObject.gameObject.transform.position;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveDown").GetBinding())
                                        {
                                            currentlyEditingObject.gameObject.transform.position += new Vector3(0, -9f * Time.deltaTime, 0);
                                            currentlyEditingObject.m_position = currentlyEditingObject.gameObject.transform.position;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveLeft").GetBinding())
                                        {
                                            currentlyEditingObject.gameObject.transform.position += new Vector3(0, 0, 9f * Time.deltaTime);
                                            currentlyEditingObject.m_position = currentlyEditingObject.gameObject.transform.position;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveRight").GetBinding())
                                        {
                                            currentlyEditingObject.gameObject.transform.position += new Vector3(0, 0, -9f * Time.deltaTime);
                                            currentlyEditingObject.m_position = currentlyEditingObject.gameObject.transform.position;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveForward").GetBinding())
                                        {
                                            currentlyEditingObject.gameObject.transform.position += new Vector3(9f * Time.deltaTime, 0, 0);
                                            currentlyEditingObject.m_position = currentlyEditingObject.gameObject.transform.position;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveBackward").GetBinding())
                                        {
                                            currentlyEditingObject.gameObject.transform.position += new Vector3(-9f * Time.deltaTime, 0, 0);
                                            currentlyEditingObject.m_position = currentlyEditingObject.gameObject.transform.position;
                                        }
                                        break;
                                    case 1:
                                        // SCALE

                                        if (KeyBindingsManager.instance.GetBindingFromName("scale_scaleUp").GetBinding())
                                        {
                                            currentlyEditingObject.gameObject.transform.localScale += (new Vector3(.3f, .3f, .3f) * Time.deltaTime);
                                            currentlyEditingObject.m_position = currentlyEditingObject.gameObject.transform.position;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("scale_scaleDown").GetBinding())
                                        {
                                            currentlyEditingObject.gameObject.transform.localScale += (new Vector3(-.3f, -.3f, -.3f) * Time.deltaTime);
                                            currentlyEditingObject.m_position = currentlyEditingObject.gameObject.transform.position;
                                        }
                                        break;
                                    case 2:
                                        // ROTATION

                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveUp").GetBinding())
                                        {
                                            currentlyEditingObject.gameObject.transform.Rotate(20f * Time.deltaTime, 0, 0);
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.gameObject.transform.rotation;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveDown").GetBinding())
                                        {
                                            currentlyEditingObject.gameObject.transform.Rotate(-20f * Time.deltaTime, 0, 0);
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.gameObject.transform.rotation;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveLeft").GetBinding())
                                        {
                                            currentlyEditingObject.gameObject.transform.Rotate(0, 20f * Time.deltaTime, 0);
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.gameObject.transform.rotation;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveRight").GetBinding())
                                        {
                                            currentlyEditingObject.gameObject.transform.Rotate(0, -20f * Time.deltaTime, 0);
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.gameObject.transform.rotation;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveForward").GetBinding())
                                        {
                                            currentlyEditingObject.gameObject.transform.Rotate(0, 0, 20f * Time.deltaTime);
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.gameObject.transform.rotation;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveBackward").GetBinding())
                                        {
                                            currentlyEditingObject.gameObject.transform.Rotate(0, 0, -20f * Time.deltaTime);
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.gameObject.transform.rotation;
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
                                            currentlyEditingObject.gameObject.transform.position += new Vector3(0, 1.8f * Time.deltaTime, 0);
                                            currentlyEditingObject.m_position = currentlyEditingObject.gameObject.transform.position;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveDown").GetBinding())
                                        {
                                            currentlyEditingObject.gameObject.transform.position += new Vector3(0, -1.8f * Time.deltaTime, 0);
                                            currentlyEditingObject.m_position = currentlyEditingObject.gameObject.transform.position;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveLeft").GetBinding())
                                        {
                                            currentlyEditingObject.gameObject.transform.position += new Vector3(0, 0, 1.8f * Time.deltaTime);
                                            currentlyEditingObject.m_position = currentlyEditingObject.gameObject.transform.position;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveRight").GetBinding())
                                        {
                                            currentlyEditingObject.gameObject.transform.position += new Vector3(0, 0, -1.8f * Time.deltaTime);
                                            currentlyEditingObject.m_position = currentlyEditingObject.gameObject.transform.position;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveForward").GetBinding())
                                        {
                                            currentlyEditingObject.gameObject.transform.position += new Vector3(1.8f * Time.deltaTime, 0, 0);
                                            currentlyEditingObject.m_position = currentlyEditingObject.gameObject.transform.position;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveBackward").GetBinding())
                                        {
                                            currentlyEditingObject.gameObject.transform.position += new Vector3(-1.8f * Time.deltaTime, 0, 0);
                                            currentlyEditingObject.m_position = currentlyEditingObject.gameObject.transform.position;
                                        }
                                        break;
                                    case 1:
                                        // SCALE

                                        if (KeyBindingsManager.instance.GetBindingFromName("scale_scaleUp").GetBinding())
                                        {
                                            currentlyEditingObject.gameObject.transform.localScale += (new Vector3(.12f, .12f, .12f) * Time.deltaTime);
                                            currentlyEditingObject.m_position = currentlyEditingObject.gameObject.transform.position;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("scale_scaleDown").GetBinding())
                                        {
                                            currentlyEditingObject.gameObject.transform.localScale += (new Vector3(-.12f, -.12f, -.12f) * Time.deltaTime);
                                            currentlyEditingObject.m_position = currentlyEditingObject.gameObject.transform.position;
                                        }
                                        break;
                                    case 2:
                                        // ROTATION

                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveUp").GetBinding())
                                        {
                                            currentlyEditingObject.gameObject.transform.Rotate(10f * Time.deltaTime, 0, 0);
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.gameObject.transform.rotation;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveDown").GetBinding())
                                        {
                                            currentlyEditingObject.gameObject.transform.Rotate(-10f * Time.deltaTime, 0, 0);
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.gameObject.transform.rotation;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveLeft").GetBinding())
                                        {
                                            currentlyEditingObject.gameObject.transform.Rotate(0, 10f * Time.deltaTime, 0);
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.gameObject.transform.rotation;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveRight").GetBinding())
                                        {
                                            currentlyEditingObject.gameObject.transform.Rotate(0, -10f * Time.deltaTime, 0);
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.gameObject.transform.rotation;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveForward").GetBinding())
                                        {
                                            currentlyEditingObject.gameObject.transform.Rotate(0, 0, 10f * Time.deltaTime);
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.gameObject.transform.rotation;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveBackward").GetBinding())
                                        {
                                            currentlyEditingObject.gameObject.transform.Rotate(0, 0, -10f * Time.deltaTime);
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.gameObject.transform.rotation;
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
                                            currentlyEditingObject.gameObject.transform.position += new Vector3(0, 2f, 0);
                                            currentlyEditingObject.m_position = currentlyEditingObject.gameObject.transform.position;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveDown").GetBindingDown())
                                        {
                                            currentlyEditingObject.gameObject.transform.position += new Vector3(0, -2f, 0);
                                            currentlyEditingObject.m_position = currentlyEditingObject.gameObject.transform.position;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveLeft").GetBindingDown())
                                        {
                                            currentlyEditingObject.gameObject.transform.position += new Vector3(0, 0, 2f);
                                            currentlyEditingObject.m_position = currentlyEditingObject.gameObject.transform.position;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveRight").GetBindingDown())
                                        {
                                            currentlyEditingObject.gameObject.transform.position += new Vector3(0, 0, -2f);
                                            currentlyEditingObject.m_position = currentlyEditingObject.gameObject.transform.position;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveForward").GetBindingDown())
                                        {
                                            currentlyEditingObject.gameObject.transform.position += new Vector3(2f, 0, 0);
                                            currentlyEditingObject.m_position = currentlyEditingObject.gameObject.transform.position;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveBackward").GetBindingDown())
                                        {
                                            currentlyEditingObject.gameObject.transform.position += new Vector3(-2f, 0, 0);
                                            currentlyEditingObject.m_position = currentlyEditingObject.gameObject.transform.position;
                                        }
                                        break;
                                    case 1:
                                        // SCALE

                                        if (KeyBindingsManager.instance.GetBindingFromName("scale_scaleUp").GetBindingDown())
                                        {
                                            currentlyEditingObject.gameObject.transform.localScale += new Vector3(.12f, .12f, .12f);
                                            currentlyEditingObject.m_position = currentlyEditingObject.gameObject.transform.position;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("scale_scaleDown").GetBindingDown())
                                        {
                                            currentlyEditingObject.gameObject.transform.localScale += new Vector3(-0.12f, -0.12f, -0.12f);
                                            currentlyEditingObject.m_position = currentlyEditingObject.gameObject.transform.position;
                                        }
                                        break;
                                    case 2:
                                        // ROTATION

                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveUp").GetBindingDown())
                                        {
                                            currentlyEditingObject.gameObject.transform.Rotate(12f, 0, 0);
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.gameObject.transform.rotation;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveDown").GetBindingDown())
                                        {
                                            currentlyEditingObject.gameObject.transform.Rotate(-12f, 0, 0);
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.gameObject.transform.rotation;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveLeft").GetBindingDown())
                                        {
                                            currentlyEditingObject.gameObject.transform.Rotate(0, 12f, 0);
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.gameObject.transform.rotation;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveRight").GetBindingDown())
                                        {
                                            currentlyEditingObject.gameObject.transform.Rotate(0, -12f, 0);
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.gameObject.transform.rotation;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveForward").GetBindingDown())
                                        {
                                            currentlyEditingObject.gameObject.transform.Rotate(0, 0, 12f);
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.gameObject.transform.rotation;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveBackward").GetBindingDown())
                                        {
                                            currentlyEditingObject.gameObject.transform.Rotate(0, 0, -12f);
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.gameObject.transform.rotation;
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
                                            currentlyEditingObject.gameObject.transform.position += new Vector3(0, .6f, 0);
                                            currentlyEditingObject.m_position = currentlyEditingObject.gameObject.transform.position;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveDown").GetBindingDown())
                                        {
                                            currentlyEditingObject.gameObject.transform.position += new Vector3(0, -0.6f, 0);
                                            currentlyEditingObject.m_position = currentlyEditingObject.gameObject.transform.position;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveLeft").GetBindingDown())
                                        {
                                            currentlyEditingObject.gameObject.transform.position += new Vector3(0, 0, 0.6f);
                                            currentlyEditingObject.m_position = currentlyEditingObject.gameObject.transform.position;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveRight").GetBindingDown())
                                        {
                                            currentlyEditingObject.gameObject.transform.position += new Vector3(0, 0, -0.6f);
                                            currentlyEditingObject.m_position = currentlyEditingObject.gameObject.transform.position;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveForward").GetBindingDown())
                                        {
                                            currentlyEditingObject.gameObject.transform.position += new Vector3(0.6f, 0, 0);
                                            currentlyEditingObject.m_position = currentlyEditingObject.gameObject.transform.position;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveBackward").GetBindingDown())
                                        {
                                            currentlyEditingObject.gameObject.transform.position += new Vector3(-0.6f, 0, 0);
                                            currentlyEditingObject.m_position = currentlyEditingObject.gameObject.transform.position;
                                        }
                                        break;
                                    case 1:
                                        // SCALE

                                        if (KeyBindingsManager.instance.GetBindingFromName("scale_scaleUp").GetBindingDown())
                                        {
                                            currentlyEditingObject.gameObject.transform.localScale += new Vector3(.06f, .06f, .06f);
                                            currentlyEditingObject.m_position = currentlyEditingObject.gameObject.transform.position;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("scale_scaleDown").GetBindingDown())
                                        {
                                            currentlyEditingObject.gameObject.transform.localScale += new Vector3(-.06f, -.06f, -.06f);
                                            currentlyEditingObject.m_position = currentlyEditingObject.gameObject.transform.position;
                                        }
                                        break;
                                    case 2:
                                        // ROTATION

                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveUp").GetBindingDown())
                                        {
                                            currentlyEditingObject.gameObject.transform.Rotate(5f, 0, 0);
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.gameObject.transform.rotation;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveDown").GetBindingDown())
                                        {
                                            currentlyEditingObject.gameObject.transform.Rotate(-5f, 0, 0);
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.gameObject.transform.rotation;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveLeft").GetBindingDown())
                                        {
                                            currentlyEditingObject.gameObject.transform.Rotate(0, 5f, 0);
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.gameObject.transform.rotation;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveRight").GetBindingDown())
                                        {
                                            currentlyEditingObject.gameObject.transform.Rotate(0, -5f, 0);
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.gameObject.transform.rotation;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveForward").GetBindingDown())
                                        {
                                            currentlyEditingObject.gameObject.transform.Rotate(0, 0, 5f);
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.gameObject.transform.rotation;
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveBackward").GetBindingDown())
                                        {
                                            currentlyEditingObject.gameObject.transform.Rotate(0, 0, -5f);
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.gameObject.transform.rotation;
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
                        foreach (ProceduralObject obj in proceduralObjects)
                        {
                            var objScreenPos = obj.gameObject.transform.position.WorldToGuiPoint();
                            if (!window.Contains(objScreenPos))
                            {
                                if (GUI.Button(new Rect(objScreenPos + new Vector2(-11, -11), new Vector2(23, 22)), "<size=20>+</size>"))
                                {
                                    currentlyEditingObject = obj;
                                    temp_storageVertex = Vertex.CreateVertexList(currentlyEditingObject);
                                    proceduralTool = true;
                                }
                            }
                        }
                    }
                    if (!movingWholeModel)
                    {
                        var winrect = GUI.Window(1094334744, window, DrawUIWindow, "Procedural Objects v" + ProceduralObjectsMod.VERSION);
                        if (proceduralTool && editingWholeModel && !movingWholeModel)
                            window = new Rect(winrect.x, winrect.y, winrect.width, 435);
                        else
                            window = new Rect(winrect.x, winrect.y, winrect.width, 400);

                        if (showExternals)
                            externalsWindow = GUI.Window(1094334745, externalsWindow, DrawExternalsWindow, "Saved Procedural Objects");

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
                                if (GUI.Button(new Rect(objPosition + new Vector2(13, -26), new Vector2(100, 23)), "Move to"))
                                {
                                    movingWholeModel = true;
                                    Gizmos.DestroyGizmo();
                                    xLine = null;
                                    yLine = null;
                                    zLine = null;
                                }
                                if (GUI.Button(new Rect(objPosition + new Vector2(13, 0), new Vector2(100, 23)), "Delete"))
                                    DeleteObject();

                                string modeText = "<i>";
                                switch (actionMode)
                                {
                                    case 0:
                                        modeText += "Position</i>";
                                        break;
                                    case 1:
                                        modeText += "Scale</i>";
                                        break;
                                    case 2:
                                        modeText += "Rotation</i>";
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
                        GUI.Label(new Rect(Input.mousePosition.x + 18, Screen.height - Input.mousePosition.y + 18, 300, 30), "Click to place");
                }
            }
        }
        public void DrawUIWindow(int id)
        {
            #region setup window
            GUI.DragWindow(new Rect(0, 0, 350, 30));
            if (GUI.Button(new Rect(356, 3, 30, 30), "X"))
            {
                showExternals = false;
                editingVertex = false;
                editingVertexIndex.Clear();
                editingWholeModel = false;
                proceduralTool = false;
                currentlyEditingObject = null;
                chosenProceduralInfo = null;
                ToolHelper.FullySetTool<DefaultTool>();
                Gizmos.DestroyGizmo();
                xLine = null;
                yLine = null;
                zLine = null;
            }
            #endregion


            if (proceduralTool)
            {
                GUI.BeginGroup(new Rect(10, 30, 380, 412));
                if (editingWholeModel)
                {
                    if (movingWholeModel)
                    {
                        GUI.Label(new Rect(0, 0, 300, 30), "<b><size=18>\"Move To\" tool</size></b>");
                        GUI.Label(new Rect(0, 30, 380, 250), "<b>Controls:</b>\nLeft Mouse Click : Confirm placement");
                    }
                    else
                    {
                        GUI.Label(new Rect(35, 0, 300, 30), "<b><size=18>General tool</size></b>");
                        GUI.contentColor = Color.green;
                        GUI.Label(new Rect(0, 0, 23, 23), "<size=18>¤</size>", GUI.skin.button);
                        GUI.contentColor = Color.white;
                        GUI.Label(new Rect(0, 30, 380, 330), "<b>Controls:</b>\n" + KeyBindingsManager.instance.GetBindingFromName("edition_smoothMovements").m_fullKeys + " : Hold for smooth movements\n" +
                            KeyBindingsManager.instance.GetBindingFromName("edition_smallMovements").m_fullKeys + " : Hold for slow movements\n(can be used together)\n\n"+
                            KeyBindingsManager.instance.GetBindingFromName("switchActionMode").m_fullKeys + " : switch mode (position, scaling, rotation)\n" + 
                            KeyBindingsManager.instance.GetBindingFromName("position_moveUp").m_fullKeys + ", " +
                            KeyBindingsManager.instance.GetBindingFromName("position_moveDown").m_fullKeys + ", " +
                            KeyBindingsManager.instance.GetBindingFromName("position_moveRight").m_fullKeys + ", " +
                            KeyBindingsManager.instance.GetBindingFromName("position_moveLeft").m_fullKeys + ", " +
                            KeyBindingsManager.instance.GetBindingFromName("position_moveForward").m_fullKeys + ", " +
                            KeyBindingsManager.instance.GetBindingFromName("position_moveBackward").m_fullKeys + " : Move the Object in Position mode\n"+
                            KeyBindingsManager.instance.GetBindingFromName("rotation_moveUp").m_fullKeys + ", " +
                            KeyBindingsManager.instance.GetBindingFromName("rotation_moveDown").m_fullKeys + ", " +
                            KeyBindingsManager.instance.GetBindingFromName("rotation_moveRight").m_fullKeys + ", " +
                            KeyBindingsManager.instance.GetBindingFromName("rotation_moveLeft").m_fullKeys + ", " +
                            KeyBindingsManager.instance.GetBindingFromName("rotation_moveForward").m_fullKeys + ", " +
                            KeyBindingsManager.instance.GetBindingFromName("rotation_moveBackward").m_fullKeys + " : Rotate the Object in Rotation mode\n" +
                            KeyBindingsManager.instance.GetBindingFromName("scale_scaleUp").m_fullKeys + "/" +
                            KeyBindingsManager.instance.GetBindingFromName("scale_scaleDown").m_fullKeys + " : Scale the object up/down in Scale mode\n" +
                            "\n" + KeyBindingsManager.instance.GetBindingFromName("switchGeneralToVertexTools").m_fullKeys + " : Quick switch between General/Vertices tool\n\n<b>Buttons : </b>\nDelete : deletes the object\nMove to : move the object to a new position");
                        GUI.Label(new Rect(0, 330, 380, 30), "Render Distance : " + currentlyEditingObject.renderDistance.ToString("N").Replace(".00", ""));
                        currentlyEditingObject.renderDistance = GUI.HorizontalSlider(new Rect(0, 350, 380, 30), Mathf.Floor(currentlyEditingObject.renderDistance), 50f, 10000f);

                        externalsSaveTextfield = GUI.TextField(new Rect(0, 370, 285, 28), externalsSaveTextfield);
                        if (File.Exists(ProceduralObjectsMod.ExternalsConfigPath + externalsSaveTextfield.ToFileName() + ".pobj"))
                        {
                            GUI.color = Color.red;
                            GUI.Label(new Rect(290, 370, 90, 28), "X", GUI.skin.button);
                            GUI.color = Color.white;
                        }
                        else
                        {
                            if (GUI.Button(new Rect(290, 370, 90, 28), "Save"))
                            {
                                ExPObjManager.SaveToExternal(externalsSaveTextfield, new CacheProceduralObject(currentlyEditingObject));
                                externalsSaveTextfield = "Enter object name here";
                            }
                        }
                    }
                    GUI.EndGroup();
                }
                else
                {
                    GUI.Label(new Rect(35, 0, 300, 30), "<b><size=18>Vertex customization tool</size></b>");
                    GUI.Label(new Rect(0, 0, 23, 23), "<size=18>+</size>", GUI.skin.button);
                    GUI.Label(new Rect(0, 30, 380, 330), "<b>Controls:</b>\nShift : Hold for smooth movements\nAlt : Hold for slow movements\n(can be used together)\n\nCtrl : Hold to select multiple vertices at a time\nArrow keys : Move the selected vertices left/right and forwards/backwards\nPageUp/PageDown : Move the selected vertices up/down\nTab : Quick switch between General/Vertices tool");

                    GUI.EndGroup();
                    if (GUI.Button(new Rect(15, 345, 185, 25), "Delete"))
                        DeleteObject();
                }
                if (GUI.Button(new Rect(205, 345, 185, 25), (editingWholeModel ? "Vertex Customization" : "General Tool")))
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
                    {
                        SwitchToMainTool();
                    }
                }
            }
            else
            {
                if (chosenProceduralInfo == null)
                {
                    GUI.Label(new Rect(10, 30, 350, 30), "Spawn a new Procedural Object/Click one to edit it");

                    if (TextureUtils.LocalTexturesCount == 0)
                        GUI.Label(new Rect(10, 45, 350, 45), "No custom texture found !\nCan't create basic objects");
                    else
                        GUI.Label(new Rect(25, 60, 350, 30), "Local Textures : ");

                    if (GUI.Button(new Rect(170, 55, 55, 28), "Refresh"))
                        basicTextures = basicTextures.LoadModConfigTextures();
                    if (GUI.Button(new Rect(230, 55, 155, 28), "Open Texture Folder"))
                    {
                        if (Directory.Exists(ProceduralObjectsMod.TextureConfigPath))
                            Application.OpenURL("file://" + ProceduralObjectsMod.TextureConfigPath);
                    }
                    if (GUI.Button(new Rect(10, 84, 375, 24), "Go to the Documentation Wiki Page"))
                        Application.OpenURL(ProceduralObjectsMod.DOCUMENTATION_URL);
                    if (GUI.Button(new Rect(10, 110, 375, 24), "Open Key Bindings Config File (Requires Restart)"))
                    {
                        if (File.Exists(KeyBindingsManager.BindingsConfigPath))
                            Application.OpenURL("file://" + KeyBindingsManager.BindingsConfigPath);
                    }

                    if (TextureUtils.TextureResources.Count > 0)
                    {
                        GUI.Label(new Rect(10, 135, 375, 28), "Workshop Texture Packages loaded : " + TextureUtils.TextureResources.Count.ToString());
                        GUI.Box(new Rect(10, 160, 375, 170), string.Empty);
                        scrollTextureResources = GUI.BeginScrollView(new Rect(10, 160, 375, 170), scrollTextureResources, new Rect(0, 0, 350, TextureUtils.TextureResources.Count * 30));
                        for (int i = 0; i < TextureUtils.TextureResources.Count; i++)
                        {
                            GUI.Label(new Rect(5, i * 30, 248, 28), TextureUtils.TextureResources[i].HasCustomName ? TextureUtils.TextureResources[i].m_name : "<i>Package with no custom name</i>");
                            GUI.Label(new Rect(255, i * 30, 99, 28), 
                                (TextureUtils.TextureResources[i].TexturesCount > 1) ? "(" + TextureUtils.TextureResources[i].TexturesCount + " textures)" : "(" + TextureUtils.TextureResources[i].TexturesCount + " texture)");
                        }
                        GUI.EndScrollView();
                    }
                    else
                        GUI.Label(new Rect(10, 130, 375, 28), "No subscribed Workshop Texture Package loaded.");

                    GUI.Label(new Rect(10, 331, 375, 35), basicTextures.Count().ToString() + " textures in total : " + TextureUtils.LocalTexturesCount.ToString() + " local + " + TextureResourceInfo.TotalTextureCount(TextureUtils.TextureResources) + " from the Workshop\n<size=10>Total objects count on the map : " + proceduralObjects.Count.ToString("N").Replace(".00", "") + "</size>");

                    if (GUI.Button(new Rect(10, 365, 375, 28), "Saved Procedural Objects"))
                        showExternals = true;
                }
                else
                {
                    if (chosenProceduralInfo.infoType == "PROP")
                        GUI.Label(new Rect(10, 30, 350, 30), "Choose texture to apply to the model \"" + chosenProceduralInfo.propPrefab.GetLocalizedTitle() + "\"");
                    else if (chosenProceduralInfo.infoType == "BUILDING")
                        GUI.Label(new Rect(10, 30, 350, 30), "Choose texture to apply to the model \"" + chosenProceduralInfo.buildingPrefab.GetLocalizedTitle() + "\"");                       
                    // Texture selection
                    scrollTextures = GUI.BeginScrollView(new Rect(10, 60, 350, 330), scrollTextures, new Rect(0, 0, 320, 80 * basicTextures.Count() + 65));
                    GUI.Label(new Rect(10, 0, 300, 28), basicTextures.Count().ToString() + " textures in total : " + TextureUtils.LocalTexturesCount.ToString() + " local + " + TextureResourceInfo.TotalTextureCount(TextureUtils.TextureResources) + " from the Workshop");
                    if (GUI.Button(new Rect(10, 30, 147.5f, 30), "Open Folder"))
                    {
                        if (Directory.Exists(ProceduralObjectsMod.TextureConfigPath))
                            Application.OpenURL("file://" + ProceduralObjectsMod.TextureConfigPath);
                    }
                    if (GUI.Button(new Rect(162.5f, 30, 147.5f, 30), "Refresh list"))
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
                            editingVertex = false;
                            chosenProceduralInfo = null;
                        }
                        GUI.Label(new Rect(15, i * 80 + 65, 85, 74), basicTextures[i]);
                        GUI.Label(new Rect(105, i * 80 + 72, 190, 52), basicTextures[i].name.Replace(ProceduralObjectsMod.TextureConfigPath, ""));
                    }
                    GUI.EndScrollView();
                }
            }
        }
        public void DrawExternalsWindow(int id)
        {
            GUI.DragWindow(new Rect(0, 0, 350, 30));
            if (GUI.Button(new Rect(356, 3, 30, 30), "X"))
                showExternals = false;
            GUI.Label(new Rect(10, 30, 298, 37), "Find your saved procedural objects here. Won't work if an asset is missing.");
            if (GUI.Button(new Rect(310, 35, 85, 28), "Refresh"))
                ExPObjManager.LoadExternals(basicTextures);
            if (ExPObjManager.m_externals.Count == 0)
            {
                GUI.Box(new Rect(10, 70, 380, 320), "No Procedural Objects saved !\nEdit an object and go to the General Tool to save one");
            }
            else
            {
                GUI.Box(new Rect(10, 70, 380, 320), string.Empty);
                GUI.BeginScrollView(new Rect(10, 70, 380, 320), scrollExternals, new Rect(0, 0, 350, 40 * ExPObjManager.m_externals.Count + 5));
                for (int i = 0; i < ExPObjManager.m_externals.Count; i++)
                {
                    GUI.Box(new Rect(5, i * 40 + 2, 343, 36), string.Empty);
                    GUI.Label(new Rect(8, i * 40 + 12, 250, 30), ExPObjManager.m_externals[i].m_name);
                    if (GUI.Button(new Rect(190, i * 40 + 5, 67, 30), "Place"))
                    {
                        PlaceCacheObject(ExPObjManager.m_externals[i].m_object);
                    }
                    if (ExPObjManager.m_externals[i].isWorkshop)
                        GUI.Label(new Rect(260, i * 40 + 5, 80, 30), "[<i>Workshop</i>]", GUI.skin.button);
                    else
                    {
                        if (GUI.Button(new Rect(260, i * 40 + 5, 80, 30), "Delete"))
                        {
                            ExPObjManager.DeleteExternal(ExPObjManager.m_externals[i], basicTextures);
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
            MeshFilter meshFilter = currentlyEditingObject.gameObject.GetComponent<MeshFilter>();
            meshFilter.mesh.SetVertices(posArray);
            // sets mesh collider vertices
            //UV map recalculation
            if (currentlyEditingObject.RequiresUVRecalculation)
            {
                try
                {
                    meshFilter.mesh.uv = Vertex.RecalculateUVMap(currentlyEditingObject, temp_storageVertex);
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
        public void PlaceCacheObject(CacheProceduralObject cacheObj)
        {
            if (cacheObj.basePrefabName == "PROP")
            {
                if (!Resources.FindObjectsOfTypeAll<PropInfo>().Any(info => info.name == cacheObj.basePrefabName))
                    return;
            }
            else if (cacheObj.basePrefabName == "BUILDING")
            {
                if (!Resources.FindObjectsOfTypeAll<BuildingInfo>().Any(info => info.name == cacheObj.basePrefabName))
                    return;
            }
            ToolHelper.FullySetTool<ProceduralTool>();
            ToolsModifierControl.mainToolbar.CloseEverything();
            var obj = new ProceduralObject(cacheObj, proceduralObjects.GetNextUnusedId(), ToolsModifierControl.cameraController.m_currentPosition + new Vector3(0, -8, 0));
            proceduralObjects.Add(obj);
            currentlyEditingObject = obj;
            temp_storageVertex = Vertex.CreateVertexList(currentlyEditingObject);
            movingWholeModel = true;
            proceduralTool = true;
            if (currentlyEditingObject.RequiresUVRecalculation)
                obj.gameObject.GetComponent<MeshFilter>().mesh.uv = Vertex.RecalculateUVMap(obj, temp_storageVertex);
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
                       xAxis.transform.position = currentlyEditingObject.gameObject.transform.position;
                   GameObject yAxis = GameObject.Find("ProceduralAxis_Y");
                   if (yAxis != null)
                       yAxis.transform.position = currentlyEditingObject.gameObject.transform.position;
                   GameObject zAxis = GameObject.Find("ProceduralAxis_Z");
                   if (zAxis != null)
                       zAxis.transform.position = currentlyEditingObject.gameObject.transform.position;
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
                showExternals = false;
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
        private void ConvertToProcedural(ToolBase tool)
        {
            showExternals = false;
            if (availableProceduralInfos == null)
                availableProceduralInfos = ProceduralUtils.CreateProceduralInfosList();
            if (availableProceduralInfos.Count == 0)
                availableProceduralInfos = ProceduralUtils.CreateProceduralInfosList();

            if (tool.GetType() == typeof(PropTool))
            {
                ProceduralInfo info = availableProceduralInfos.Where(pInf => pInf.propPrefab != null).FirstOrDefault(pInf => pInf.propPrefab == ((PropTool)tool).m_prefab);
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
                    editingVertex = false;
                }
            }
        }

        public Vector3 VertexWorldPosition(Vertex vertex)
        {

            if (currentlyEditingObject.isPloppableAsphalt)
                return currentlyEditingObject.gameObject.transform.rotation * (Vector3.Scale(vertex.Position.PloppableAsphaltPosition(), currentlyEditingObject.gameObject.transform.localScale)) + currentlyEditingObject.m_position;
            return currentlyEditingObject.gameObject.transform.rotation * (Vector3.Scale(vertex.Position, currentlyEditingObject.gameObject.transform.localScale)) + currentlyEditingObject.m_position;
        }
        public void DeleteObject()
        {
            editingVertex = false;
            editingVertexIndex.Clear();
            editingWholeModel = false;
            proceduralTool = false;
            movingWholeModel = false;
            proceduralObjects.Remove(currentlyEditingObject);
            Object.Destroy(currentlyEditingObject.gameObject);
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
