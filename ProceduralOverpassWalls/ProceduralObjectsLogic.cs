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
using ProceduralObjects.ProceduralText;

using ColossalFramework.UI;
using ColossalFramework.Globalization;
using ColossalFramework.PlatformServices;
using ColossalFramework;

namespace ProceduralObjects
{
    public class ProceduralObjectsLogic : MonoBehaviour
    {
        public List<ProceduralObject> proceduralObjects, pObjSelection, alignHeightObj;
        public HashSet<int> activeIds;
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
        public Vertex[] tempVerticesBuffer;
        public Type previousToolType;
        public List<Texture2D> basicTextures;
        public static AxisEditionState axisState = AxisEditionState.none;
        public Vector3 axisHitPoint = Vector3.zero;
        public LineRenderer xLine, yLine, zLine;
        private RotationWizardData rotWizardData = null;
        private VerticesWizardData vertWizardData = null;
        private ProceduralObject SingleHoveredObj = null;

        public static ToolAction toolAction = ToolAction.none;
        public static Vector3 movingWholeRaycast = Vector3.zero;

        public static byte verticesToolType = 0;
        public bool showVerticesToolChoice = false;
        public Vector2 VerticesToolChoicePos = Vector2.zero;
        public float rightClickTimer = 0f;
        public static float tabSwitchTimer = 0f;

        public Camera renderCamera;
        private Material spriteMat;
        private Color uiColor;

        public GUIStyle redLabelStyle = new GUIStyle();
        public int actionMode = 0;

        // drag selection
        public Vector2 topLeftRegion = Vector2.zero, bottomRightRegion = Vector2.zero;
        public bool clickingRegion = false;

        ProceduralObjectsButton mainButton;

        private UIButton ConfirmNoButton;

        private int renamingExternal = -1;
        private string renamingExternalString = "";

        ExternalProceduralObjectsManager ExPObjManager;
        private FontManager fontManager;
        private TextCustomizationManager textManager;
        public LayerManager layerManager;
        public AdvancedEditionManager advEdManager;

        private bool showLayerSetScroll = false;
        private Vector2 scrollLayerSet = Vector2.zero;

        private static AudioClip[] audiosClips;

        void Start()
        {
            Debug.Log("[ProceduralObjects] Game start procedure started.");
            var startTime = DateTime.Now;
            pObjSelection = new List<ProceduralObject>();
            alignHeightObj = new List<ProceduralObject>();
            UIView view = UIView.GetAView();
            mainButton = view.AddUIComponent(typeof(ProceduralObjectsButton)) as ProceduralObjectsButton;
            mainButton.logic = this;
            renderCamera = Camera.main;
            var pausePanel = view.GetComponentsInChildren<UIPanel>().First(panel => panel.name.Contains("PauseMenu"));
            if (pausePanel != null)
            {
                pausePanel.eventVisibilityChanged += (uiComponent, value) =>
                {
                    ClosePO();
                    layerManager.showWindow = false;
                };
            }
            foreach (UIComponent comp in view.GetComponentsInChildren<UIPanel>().First(panel => panel.name.Contains("ConfirmPanel")).components)
            {
                if (comp.name.ToLower() == "no")
                    ConfirmNoButton = (UIButton)comp;
            }
            var clips = Resources.FindObjectsOfTypeAll<AudioClip>();
            audiosClips = new AudioClip[] { clips.FirstOrDefault(a => a.name == "button_click"),
                clips.FirstOrDefault(a => a.name == "bulldozer_click"),
                clips.FirstOrDefault(a => a.name == "button_click_disabled"),
                clips.FirstOrDefault(a => a.name == "budget_sliders") };
            KeyBindingsManager.Initialize();
            basicTextures = basicTextures.LoadModConfigTextures().OrderBy(tex => tex.name).ToList();
            availableProceduralInfos = ProceduralUtils.CreateProceduralInfosList();
            Debug.Log("[ProceduralObjects] Found " + availableProceduralInfos.Count.ToString() + " procedural infos.");
            fontManager = new FontManager(); // loads fonts
            FontManager.instance = fontManager;
            textManager = new TextCustomizationManager(fontManager);
            ExPObjManager = new ExternalProceduralObjectsManager();
            ExPObjManager.LoadExternals(basicTextures, fontManager);
            spriteMat = new Material(Shader.Find("Sprites/Default"));
            spriteMat.color = new Color(1f, 0, 0, .27f);
            uiColor = new Color(1, 1, 1, .5f);
            if (LocalizationManager.instance == null)
                LocalizationManager.CreateManager();
            else
                LocalizationManager.instance.SelectCurrent();
            externalsSaveTextfield = LocalizationManager.instance.current["enter_name"];
            layerManager = new LayerManager(this);
            if (ProceduralObjectsMod.tempLayerData != null)
            {
                layerManager.m_layers = ProceduralObjectsMod.tempLayerData.ToList();
                ProceduralObjectsMod.tempLayerData = null;
            }
            if (ProceduralObjectsMod.tempContainerData != null)
            {
                this.LoadContainerData(ProceduralObjectsMod.tempContainerData);
                ProceduralObjectsMod.tempContainerData = null;
            }
            else
            {
                proceduralObjects = new List<ProceduralObject>();
                activeIds = new HashSet<int>();
            }
            var cursors = Resources.FindObjectsOfTypeAll<CursorInfo>();
            ProceduralTool.buildCursor = cursors.First(cursor => cursor.name == "Building Placement");
            ProceduralTool.terrainLevel = cursors.First(cursor => cursor.name == "Terrain Level");
            ProceduralTool.terrainShift = cursors.First(cursor => cursor.name == "Terrain Shift");
            ProceduralTool.CreateCursors();
            redLabelStyle.normal.textColor = Color.red;
            editingVertexIndex = new List<int>();
            LocaleManager.eventLocaleChanged += SetupLocalization;
            Debug.Log("[ProceduralObjects] Game start procedure ended in " + Math.Round((DateTime.Now - startTime).TotalSeconds, 2) + " seconds");
        }

        void Update()
        {
            // Cloning for Move It
            if (PO_MoveIt.queuedCloning.Count > 0)
            {
                var queued = new List<ProceduralObject>(PO_MoveIt.queuedCloning);
                for (int i = 0; i < queued.Count; i++)
                {
                    var po = queued[i];
                    if (!PO_MoveIt.doneCloning.ContainsKey(po))
                    {
                        var clone = CloneObject(po);
                        PO_MoveIt.queuedCloning.Remove(po);
                        PO_MoveIt.doneCloning.Add(po, clone);
                    }
                }
            }


            var currentToolType = ToolsModifierControl.toolController.CurrentTool.GetType();

            if ((currentToolType == typeof(PropTool)) || (currentToolType == typeof(BuildingTool)))
                mainButton.text = LocalizationManager.instance.current["convert_pobj"];
            else
                mainButton.text = "Procedural Objects";

            if (currentToolType != typeof(ProceduralTool))
            {
                if (previousToolType == typeof(ProceduralTool))
                    ClosePO();
            }

            if (KeyBindingsManager.instance.GetBindingFromName("convertToProcedural").GetBindingDown())
            {
                CallConvertToPO(ToolsModifierControl.toolController.CurrentTool);
            }

            /* OUTDATEd (removed 1.6)
            if (KeyBindingsManager.instance.GetBindingFromName("generalShowHideUI").GetBindingDown())
            {
                generalShowUI = !generalShowUI;
            } */
            if (proceduralObjects != null)
            {
                for (int i = 0; i < proceduralObjects.Count; i++)
                {
                    var obj = proceduralObjects[i];
                    obj._insideRenderView = Vector3.Distance(Camera.main.transform.position, obj.m_position) <= obj.renderDistance;
                    if (!obj._insideRenderView)
                        obj._insideUIview = false;
                    else
                        obj._insideUIview = obj._insideRenderView && Camera.main.WorldToScreenPoint(obj.m_position).z >= 0;
                    if (!obj._insideRenderView)
                        continue;
                    bool show = obj.layer == null;
                    if (obj.layer != null)
                        show = !obj.layer.m_isHidden;
                    if (show)
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
                if (tabSwitchTimer != 0)
                {
                    if (tabSwitchTimer >= 1.7f)
                        tabSwitchTimer = 0f;
                    else
                        tabSwitchTimer += TimeUtils.deltaTime;
                }
                // PASTE object
                if (KeyBindingsManager.instance.GetBindingFromName("paste").GetBindingDown())
                {
                    Paste(clipboard);
                    SingleHoveredObj = null;
                }
                //    if (!proceduralTool && chosenProceduralInfo != null)
                //        GUIUtils.SetMouseScrolling(!(new Rect(window.x + 10, window.y + 60, 350, 330).IsMouseInside()));
                if (!proceduralTool && chosenProceduralInfo == null && pObjSelection != null)
                {
                    if (pObjSelection.Count > 0)
                    {
                        if (KeyBindingsManager.instance.GetBindingFromName("deleteObject").GetBindingDown())
                        {
                            YieldConfirmDeletePanel(pObjSelection.Count, pObjSelection[0].m_position, delegate()
                            {
                                for (int i = 0; i < pObjSelection.Count; i++)
                                {
                                    proceduralObjects.Remove(pObjSelection[i]);
                                }
                                pObjSelection.Clear();
                            });
                            /* generalShowUI = false;
                            ConfirmPanel.ShowModal(LocalizationManager.instance.current["confirmDeletionPopup_title"],
                                pObjSelection.Count > 1 ? string.Format(LocalizationManager.instance.current["confirmDeletionPopup_descSelection"], pObjSelection.Count) : LocalizationManager.instance.current["confirmDeletionPopup_descSingle"], 
                                delegate(UIComponent comp, int ret)
                            {
                                if (ret == 1)
                                {
                                }
                                generalShowUI = true;
                            }); */
                        }
                        if (KeyBindingsManager.instance.GetBindingFromName("copy").GetBindingDown())
                        {
                            clipboard = new ClipboardProceduralObjects(ClipboardProceduralObjects.ClipboardType.Selection);
                            clipboard.MakeSelectionList(pObjSelection);
                            storedHeight = pObjSelection[0].m_position.y;
                        }
                    }

                    if (Input.GetMouseButton(1))
                    {
                        if (!clickingRegion)
                        {
                            ResetLayerScrollmenu();
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
                        Rect region = GUIUtils.RectFromCorners(topLeftRegion, bottomRightRegion, false);
                        clickingRegion = false;
                        if (!Input.GetKey(KeyCode.LeftControl))
                            pObjSelection.Clear();
                        foreach (var obj in proceduralObjects.Where(po => po._insideRenderView))
                        {
                            if (region.Contains(obj.m_position.WorldToGuiPoint(), true))
                            {
                                pObjSelection.Add(obj);
                            }
                        }
                    }
                }
            }
            if (proceduralTool)
            {
                textManager.Update();

                if (advEdManager != null)
                    advEdManager.Update();

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

                if (KeyBindingsManager.instance.GetBindingFromName("redo").GetBindingDown() && currentlyEditingObject.historyEditionBuffer.CanRedo)
                    Redo();
                else if (KeyBindingsManager.instance.GetBindingFromName("undo").GetBindingDown() && currentlyEditingObject.historyEditionBuffer.CanUndo)
                    Undo();


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
                                rotWizardData.IncrementStep();
                                if (rotWizardData.clickTime > .14f)
                                {
                                    float diff = (rotWizardData.GUIMousePositionX - Input.mousePosition.x);
                                    if (diff < 0)
                                        currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, (diff * 370f) / Screen.width, 0);
                                    else
                                        currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, -(((-diff) * 370f) / Screen.width), 0);
                                    rotWizardData.UpdateMouseCoords();
                                }
                            }
                        }
                        else if (Input.GetMouseButtonUp(1))
                        {
                            if (rotWizardData.clickTime <= .14f)
                            {
                                currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, 45, 0);
                            }
                            rotWizardData = null;
                        }

                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveUp").GetBinding())
                            yOffset += TimeUtils.deltaTime * 8.7f;
                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveDown").GetBinding())
                            yOffset -= TimeUtils.deltaTime * 8.7f;
                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveForward").GetBinding())
                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(20f * TimeUtils.deltaTime, 0, 0);
                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveBackward").GetBinding())
                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(-20f * TimeUtils.deltaTime, 0, 0);
                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveLeft").GetBinding())
                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, 0, 20f * TimeUtils.deltaTime);
                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveRight").GetBinding())
                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, 0, -20f * TimeUtils.deltaTime);


                        ToolBase.RaycastInput rayInput = new ToolBase.RaycastInput(Camera.main.ScreenPointToRay(Input.mousePosition), Camera.main.farClipPlane);
                        try
                        {
                            ToolBase.RaycastOutput rayOutput;
                            if (ProceduralTool.TerrainRaycast(rayInput, out rayOutput))
                            {
                                if (!rayOutput.m_currentEditObject)
                                {
                                    movingWholeRaycast = rayOutput.m_hitPos;
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
                                if (proceduralObjects[i].tempObj.transform.parent == currentlyEditingObject.tempObj.transform)
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
                    if (!toolsRect.IsMouseInside() && (!textManager.showWindow || (textManager.showWindow && !textManager.windowRect.IsMouseInside())))
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
                                        Singleton<AudioManager>.instance.PlaySound(audiosClips[3]);
                                        currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.position, tempVerticesBuffer);
                                        axisState = AxisEditionState.X;
                                        axisHitPoint = Gizmos.AxisHitPoint(hit.point, currentlyEditingObject.m_position);
                                    }
                                    else if (hit.transform.gameObject.name == "ProceduralAxis_Y")
                                    {
                                        Singleton<AudioManager>.instance.PlaySound(audiosClips[3]);
                                        currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.position, tempVerticesBuffer);
                                        axisState = AxisEditionState.Y;
                                        axisHitPoint = Gizmos.AxisHitPoint(hit.point, currentlyEditingObject.m_position);
                                    }
                                    else if (hit.transform.gameObject.name == "ProceduralAxis_Z")
                                    {
                                        Singleton<AudioManager>.instance.PlaySound(audiosClips[3]);
                                        currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.position, tempVerticesBuffer);
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
                            currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(tempVerticesBuffer);
                            //   Debug.LogError("LMB up ");
                        }
                    }

                    if (editingWholeModel)
                    {
                        if (KeyBindingsManager.instance.GetBindingFromName("switchGeneralToVertexTools").GetBindingDown())
                        {
                            PlaySound(2);
                            if (Gizmos.Exists)
                            {
                                Gizmos.DestroyGizmo();
                                xLine = null;
                                yLine = null;
                                zLine = null;
                            }
                            if (actionMode == 2)
                            {
                                verticesToolType = 0; // move
                                editingWholeModel = false;
                                toolAction = ToolAction.vertices;
                                tabSwitchTimer = TimeUtils.deltaTime;
                            }
                            else
                                actionMode += 1;
                        }
                        var distance = Vector3.Distance(Camera.main.transform.position, currentlyEditingObject.m_position);
                        #region Gizmo movement - WHOLE MODEL
                        if (axisState != AxisEditionState.none)
                        {
                            if (Input.GetMouseButton(0))
                            {
                                if (actionMode == 0 && axisHitPoint != Vector3.zero)
                                {
                                    switch (axisState)
                                    {
                                        // POSITION
                                        case AxisEditionState.X:
                                            currentlyEditingObject.m_position = new Vector3(Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y,
                                                distance)).x + axisHitPoint.x,
                                                currentlyEditingObject.m_position.y,
                                                currentlyEditingObject.m_position.z);
                                            break;
                                        case AxisEditionState.Y:
                                            currentlyEditingObject.m_position = new Vector3(currentlyEditingObject.m_position.x,
                                                Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y,
                                                distance)).y + axisHitPoint.y,
                                                currentlyEditingObject.m_position.z);
                                            break;
                                        case AxisEditionState.Z:
                                            currentlyEditingObject.m_position = new Vector3(currentlyEditingObject.m_position.x,
                                                currentlyEditingObject.m_position.y,
                                                Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y,
                                                distance)).z + axisHitPoint.z);
                                            break;
                                    }
                                }
                            }
                        }
                        if (KeyBindingsManager.instance.GetBindingFromName("switchActionMode").GetBindingDown())
                        {
                            showVerticesToolChoice = false;
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
                            Gizmos.ResizeUpdatePos(distance, currentlyEditingObject.m_position, xLine, yLine, zLine);
                        #endregion
                    }
                    else
                    {
                        if (KeyBindingsManager.instance.GetBindingFromName("switchGeneralToVertexTools").GetBindingDown())
                        {
                            PlaySound(2);
                            showVerticesToolChoice = false;
                            if (verticesToolType < 2)
                            {
                                tabSwitchTimer = TimeUtils.deltaTime;
                                verticesToolType += 1;
                            }
                            else
                            {
                                verticesToolType = 0;
                                tabSwitchTimer = 0;
                                toolAction = ToolAction.none;
                                SwitchToMainTool();
                            }
                        }

                        if (currentlyEditingObject != null)
                        {
                            if (Input.GetMouseButton(0))
                            {
                                if (showVerticesToolChoice && !(new Rect(VerticesToolChoicePos, new Vector2(105, editingVertexIndex.Count > 0 ? 92 : 69))).IsMouseInside())
                                    showVerticesToolChoice = false;

                                if (editingVertexIndex.Count > 0)
                                {
                                    if (textManager.windowRect.IsMouseInside() && textManager.showWindow)
                                    {
                                        editingVertexIndex.Clear();
                                    }
                                    else
                                    {
                                        if (vertWizardData == null)
                                        {
                                            if (!window.IsMouseInside())
                                            {
                                                ToolBase.RaycastInput rayInput = new ToolBase.RaycastInput(Camera.main.ScreenPointToRay(Input.mousePosition), Camera.main.farClipPlane);
                                                ToolBase.RaycastOutput rayOutput;
                                                if (ProceduralTool.TerrainRaycast(rayInput, out rayOutput))
                                                {
                                                    vertWizardData = new VerticesWizardData(verticesToolType);
                                                    vertWizardData.Store(rayOutput.m_hitPos, GUIUtils.MousePos);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            vertWizardData.IncrementStep();
                                            if (vertWizardData.enableMovement)
                                            {
                                                if (!vertWizardData.storedVertices)
                                                {
                                                    // if (currentlyEditingObject.meshStatus == 1)
                                                    //      currentlyEditingObject.MakeMeshUnique();
                                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, tempVerticesBuffer);
                                                    vertWizardData.Store(tempVerticesBuffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))).ToArray());
                                                }
                                                if (vertWizardData.toolType == 0)
                                                {
                                                    ToolBase.RaycastInput rayInput = new ToolBase.RaycastInput(Camera.main.ScreenPointToRay(Input.mousePosition), Camera.main.farClipPlane);
                                                    ToolBase.RaycastOutput rayOutput;
                                                    if (ProceduralTool.TerrainRaycast(rayInput, out rayOutput))
                                                    {
                                                        vertWizardData.ApplyToNewPosition(rayOutput.m_hitPos, currentlyEditingObject);
                                                        Apply();
                                                    }
                                                }
                                                else if (vertWizardData.toolType == 1)
                                                {
                                                    vertWizardData.ApplyToNewPosition(Input.mousePosition.x);
                                                    Apply();
                                                }
                                                else if (vertWizardData.toolType == 2)
                                                {
                                                    vertWizardData.ApplyToNewPosition(GUIUtils.MousePos);
                                                    Apply();
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else if (Input.GetMouseButtonUp(0))
                            {
                                if (vertWizardData != null)
                                {
                                    currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(tempVerticesBuffer);
                                    vertWizardData = null;
                                }
                            }
                            else if (Input.GetMouseButton(1))
                            {
                                if (alignHeightObj.Count == 0)
                                {
                                    if (!clickingRegion && rightClickTimer == 0f)
                                    {
                                        topLeftRegion = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                                        bottomRightRegion = topLeftRegion;
                                    }
                                    else
                                        bottomRightRegion = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                                }

                                rightClickTimer += TimeUtils.deltaTime;
                                if (rightClickTimer >= .15f)
                                {
                                    showVerticesToolChoice = false;
                                    clickingRegion = true;
                                }
                            }
                            else if (Input.GetMouseButtonUp(1))
                            {
                                if (rightClickTimer < .15f)
                                {
                                    PlaySound();
                                    showVerticesToolChoice = true;
                                    VerticesToolChoicePos = topLeftRegion;
                                }
                                if (clickingRegion)
                                {
                                    bottomRightRegion = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                                    Rect region = GUIUtils.RectFromCorners(topLeftRegion, bottomRightRegion, false);
                                    clickingRegion = false;
                                    if (!Input.GetKey(KeyCode.LeftControl))
                                        editingVertexIndex.Clear();
                                    editingVertex = true;
                                    editingWholeModel = false;
                                    foreach (Vertex vertex in tempVerticesBuffer.Where(v => !v.IsDependent))
                                    {
                                        if (region.Contains(VertexWorldPosition(vertex).WorldToGuiPoint(), true))
                                        {
                                            editingVertexIndex.Add(vertex.Index);
                                        }
                                    }
                                }
                                rightClickTimer = 0f;
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
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, tempVerticesBuffer);
                                    foreach (Vertex v in tempVerticesBuffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        tempVerticesBuffer[v.Index].Position.y += 5f * TimeUtils.deltaTime;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveDown").GetBinding())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, tempVerticesBuffer);
                                    foreach (Vertex v in tempVerticesBuffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        tempVerticesBuffer[v.Index].Position.y -= 5f * TimeUtils.deltaTime;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveLeft").GetBinding())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, tempVerticesBuffer);
                                    foreach (Vertex v in tempVerticesBuffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        tempVerticesBuffer[v.Index].Position.z += 5f * TimeUtils.deltaTime;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveRight").GetBinding())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, tempVerticesBuffer);
                                    foreach (Vertex v in tempVerticesBuffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        tempVerticesBuffer[v.Index].Position.z -= 5f * TimeUtils.deltaTime;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveForward").GetBinding())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, tempVerticesBuffer);
                                    foreach (Vertex v in tempVerticesBuffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        tempVerticesBuffer[v.Index].Position.x += 5f * TimeUtils.deltaTime;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveBackward").GetBinding())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, tempVerticesBuffer);
                                    foreach (Vertex v in tempVerticesBuffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        tempVerticesBuffer[v.Index].Position.x -= 5f * TimeUtils.deltaTime;
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
                                            currentlyEditingObject.m_position += new Vector3(0, 9f * TimeUtils.deltaTime, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveDown").GetBinding())
                                        {
                                            currentlyEditingObject.m_position += new Vector3(0, -9f * TimeUtils.deltaTime, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveLeft").GetBinding())
                                        {
                                            currentlyEditingObject.m_position += new Vector3(0, 0, 9f * TimeUtils.deltaTime);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveRight").GetBinding())
                                        {
                                            currentlyEditingObject.m_position += new Vector3(0, 0, -9f * TimeUtils.deltaTime);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveForward").GetBinding())
                                        {
                                            currentlyEditingObject.m_position += new Vector3(9f * TimeUtils.deltaTime, 0, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveBackward").GetBinding())
                                        {
                                            currentlyEditingObject.m_position += new Vector3(-9f * TimeUtils.deltaTime, 0, 0);
                                        }
                                        break;
                                    case 1:
                                        // SCALE

                                        if (KeyBindingsManager.instance.GetBindingFromName("scale_scaleUp").GetBinding())
                                        {
                                            currentlyEditingObject.Scale(tempVerticesBuffer, 1 + (.3f * TimeUtils.deltaTime));
                                            Apply();
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("scale_scaleDown").GetBinding())
                                        {
                                            currentlyEditingObject.Scale(tempVerticesBuffer, 1 - (.3f * TimeUtils.deltaTime));
                                            Apply();
                                        }
                                        break;
                                    case 2:
                                        // ROTATION

                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveUp").GetBinding())
                                        {
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(20f * TimeUtils.deltaTime, 0, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveDown").GetBinding())
                                        {
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(-20f * TimeUtils.deltaTime, 0, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveLeft").GetBinding())
                                        {
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, 20f * TimeUtils.deltaTime, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveRight").GetBinding())
                                        {
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, -20f * TimeUtils.deltaTime, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveForward").GetBinding())
                                        {
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, 0, 20f * TimeUtils.deltaTime);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveBackward").GetBinding())
                                        {
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, 0, -20f * TimeUtils.deltaTime);
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
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, tempVerticesBuffer);
                                    foreach (Vertex v in tempVerticesBuffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        tempVerticesBuffer[v.Index].Position.y += 3f * TimeUtils.deltaTime;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveDown").GetBinding())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, tempVerticesBuffer);
                                    foreach (Vertex v in tempVerticesBuffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        tempVerticesBuffer[v.Index].Position.y -= 3f * TimeUtils.deltaTime;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveLeft").GetBinding())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, tempVerticesBuffer);
                                    foreach (Vertex v in tempVerticesBuffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        tempVerticesBuffer[v.Index].Position.z += 3f * TimeUtils.deltaTime;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveRight").GetBinding())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, tempVerticesBuffer);
                                    foreach (Vertex v in tempVerticesBuffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        tempVerticesBuffer[v.Index].Position.z -= 3f * TimeUtils.deltaTime;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveForward").GetBinding())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, tempVerticesBuffer);
                                    foreach (Vertex v in tempVerticesBuffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        tempVerticesBuffer[v.Index].Position.x += 3f * TimeUtils.deltaTime;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveBackward").GetBinding())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, tempVerticesBuffer);
                                    foreach (Vertex v in tempVerticesBuffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        tempVerticesBuffer[v.Index].Position.x -= 3f * TimeUtils.deltaTime;
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
                                            currentlyEditingObject.m_position += new Vector3(0, 1.8f * TimeUtils.deltaTime, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveDown").GetBinding())
                                        {
                                            currentlyEditingObject.m_position += new Vector3(0, -1.8f * TimeUtils.deltaTime, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveLeft").GetBinding())
                                        {
                                            currentlyEditingObject.m_position += new Vector3(0, 0, 1.8f * TimeUtils.deltaTime);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveRight").GetBinding())
                                        {
                                            currentlyEditingObject.m_position += new Vector3(0, 0, -1.8f * TimeUtils.deltaTime);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveForward").GetBinding())
                                        {
                                            currentlyEditingObject.m_position += new Vector3(1.8f * TimeUtils.deltaTime, 0, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveBackward").GetBinding())
                                        {
                                            currentlyEditingObject.m_position += new Vector3(-1.8f * TimeUtils.deltaTime, 0, 0);
                                        }
                                        break;
                                    case 1:
                                        // SCALE

                                        if (KeyBindingsManager.instance.GetBindingFromName("scale_scaleUp").GetBinding())
                                        {
                                            currentlyEditingObject.Scale(tempVerticesBuffer, 1 + (.12f * TimeUtils.deltaTime));
                                            Apply();
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("scale_scaleDown").GetBinding())
                                        {
                                            currentlyEditingObject.Scale(tempVerticesBuffer, 1 - (.12f * TimeUtils.deltaTime));
                                            Apply();
                                        }
                                        break;
                                    case 2:
                                        // ROTATION

                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveUp").GetBinding())
                                        {
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(10f * TimeUtils.deltaTime, 0, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveDown").GetBinding())
                                        {
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(-10f * TimeUtils.deltaTime, 0, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveLeft").GetBinding())
                                        {
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, 10f * TimeUtils.deltaTime, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveRight").GetBinding())
                                        {
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, -10f * TimeUtils.deltaTime, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveForward").GetBinding())
                                        {
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, 0, 10f * TimeUtils.deltaTime);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveBackward").GetBinding())
                                        {
                                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, 0, -10f * TimeUtils.deltaTime);
                                        }
                                        break;
                                }
                                #endregion
                            }
                            #endregion
                        }
                        if (editingVertex)
                        {
                            #region confirm edition steps
                            if (KeyBindingsManager.instance.GetBindingFromName("position_moveUp").GetBindingUp())
                            {
                                currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(tempVerticesBuffer);
                            }
                            if (KeyBindingsManager.instance.GetBindingFromName("position_moveDown").GetBindingUp())
                            {
                                currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(tempVerticesBuffer);
                            }
                            if (KeyBindingsManager.instance.GetBindingFromName("position_moveLeft").GetBindingUp())
                            {
                                currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(tempVerticesBuffer);
                            }
                            if (KeyBindingsManager.instance.GetBindingFromName("position_moveRight").GetBindingUp())
                            {
                                currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(tempVerticesBuffer);
                            }
                            if (KeyBindingsManager.instance.GetBindingFromName("position_moveForward").GetBindingUp())
                            {
                                currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(tempVerticesBuffer);
                            }
                            if (KeyBindingsManager.instance.GetBindingFromName("position_moveBackward").GetBindingUp())
                            {
                                currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(tempVerticesBuffer);
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
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, tempVerticesBuffer);
                                    foreach (Vertex v in tempVerticesBuffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        tempVerticesBuffer[v.Index].Position.y += 1f;
                                    if (vertWizardData == null)
                                        currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(tempVerticesBuffer);
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveDown").GetBindingDown())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, tempVerticesBuffer);
                                    foreach (Vertex v in tempVerticesBuffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        tempVerticesBuffer[v.Index].Position.y -= 1f;
                                    if (vertWizardData == null)
                                        currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(tempVerticesBuffer);
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveLeft").GetBindingDown())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, tempVerticesBuffer);
                                    foreach (Vertex v in tempVerticesBuffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        tempVerticesBuffer[v.Index].Position.z += 1f;
                                    if (vertWizardData == null)
                                        currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(tempVerticesBuffer);
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveRight").GetBindingDown())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, tempVerticesBuffer);
                                    foreach (Vertex v in tempVerticesBuffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        tempVerticesBuffer[v.Index].Position.z -= 1f;
                                    if (vertWizardData == null)
                                        currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(tempVerticesBuffer);
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveForward").GetBindingDown())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, tempVerticesBuffer);
                                    foreach (Vertex v in tempVerticesBuffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        tempVerticesBuffer[v.Index].Position.x += 1f;
                                    if (vertWizardData == null)
                                        currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(tempVerticesBuffer);
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveBackward").GetBindingDown())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, tempVerticesBuffer);
                                    foreach (Vertex v in tempVerticesBuffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        tempVerticesBuffer[v.Index].Position.x -= 1f;
                                    if (vertWizardData == null)
                                        currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(tempVerticesBuffer);
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
                                            currentlyEditingObject.Scale(tempVerticesBuffer, 1.12f);
                                            Apply();
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("scale_scaleDown").GetBindingDown())
                                        {
                                            currentlyEditingObject.Scale(tempVerticesBuffer, .88f);
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
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, tempVerticesBuffer);
                                    foreach (Vertex v in tempVerticesBuffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        tempVerticesBuffer[v.Index].Position.y += 0.5f;
                                    if (vertWizardData == null)
                                        currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(tempVerticesBuffer);
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveDown").GetBindingDown())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, tempVerticesBuffer);
                                    foreach (Vertex v in tempVerticesBuffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        tempVerticesBuffer[v.Index].Position.y -= 0.5f;
                                    if (vertWizardData == null)
                                        currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(tempVerticesBuffer);
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveLeft").GetBindingDown())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, tempVerticesBuffer);
                                    foreach (Vertex v in tempVerticesBuffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        tempVerticesBuffer[v.Index].Position.z += 0.5f;
                                    if (vertWizardData == null)
                                        currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(tempVerticesBuffer);
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveRight").GetBindingDown())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, tempVerticesBuffer);
                                    foreach (Vertex v in tempVerticesBuffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        tempVerticesBuffer[v.Index].Position.z -= 0.5f;
                                    if (vertWizardData == null)
                                        currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(tempVerticesBuffer);
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveForward").GetBindingDown())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, tempVerticesBuffer);
                                    foreach (Vertex v in tempVerticesBuffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        tempVerticesBuffer[v.Index].Position.x += 0.5f;
                                    if (vertWizardData == null)
                                        currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(tempVerticesBuffer);
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveBackward").GetBindingDown())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, tempVerticesBuffer);
                                    foreach (Vertex v in tempVerticesBuffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        tempVerticesBuffer[v.Index].Position.x -= 0.5f;
                                    if (vertWizardData == null)
                                        currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(tempVerticesBuffer);
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
                                            currentlyEditingObject.Scale(tempVerticesBuffer, 1.06f);
                                            Apply();
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("scale_scaleDown").GetBindingDown())
                                        {
                                            currentlyEditingObject.Scale(tempVerticesBuffer, .94f);
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
            previousToolType = ToolsModifierControl.toolController.CurrentTool.GetType();
        }

        void OnGUI()
        {
            if (!ToolsModifierControl.cameraController.m_freeCamera && generalShowUI)
            {
                layerManager.DrawWindow();

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
                    if (!proceduralTool && chosenProceduralInfo == null)
                    {
                        if (clickingRegion)
                        {
                            GUI.color = uiColor;
                            GUI.Box(GUIUtils.RectFromCorners(topLeftRegion, bottomRightRegion, true), "");
                            GUI.color = Color.white;
                        }
                        ProceduralObject hoveredObj = null;
                        foreach (ProceduralObject obj in proceduralObjects.ToList())
                        {
                            var objScreenPos = obj.m_position.WorldToGuiPoint();
                            if (!window.Contains(objScreenPos) && obj._insideUIview)
                            {
                                if (new Rect(objScreenPos + new Vector2(-15, -15), new Vector2(31, 30)).IsMouseInside())
                                    hoveredObj = obj;
                                if (pObjSelection.Contains(obj))
                                {
                                    if (pObjSelection[0] == obj)
                                    {
                                        if (alignHeightObj.Count == 0)
                                        {
                                            if (pObjSelection.Count == 1)
                                            {
                                                #region
                                                if (GUI.Button(new Rect(objScreenPos + new Vector2(12, -11), new Vector2(130, 22)), LocalizationManager.instance.current["edit"]))
                                                {
                                                    PlaySound();
                                                    verticesToolType = 0;
                                                    toolAction = ToolAction.vertices;
                                                    SetCurrentlyEditingObj(obj);
                                                    pObjSelection.Clear();
                                                    tempVerticesBuffer = Vertex.CreateVertexList(currentlyEditingObject);
                                                    proceduralTool = true;
                                                    hoveredObj = null;
                                                }
                                                if (GUI.Button(new Rect(objScreenPos + new Vector2(12, 12), new Vector2(130, 22)), LocalizationManager.instance.current["move_to"]))
                                                {
                                                    PlaySound();
                                                    pObjSelection.Clear();
                                                    SetCurrentlyEditingObj(obj);
                                                    obj.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.moveTo, tempVerticesBuffer);
                                                    placingSelection = false;
                                                    movingWholeModel = true;
                                                    toolAction = ToolAction.build;
                                                    editingWholeModel = true;
                                                    proceduralTool = true;
                                                    hoveredObj = null;
                                                }
                                                if (GUI.Button(new Rect(objScreenPos + new Vector2(12, 35), new Vector2(130, 22)), LocalizationManager.instance.current["copy"]))
                                                {
                                                    PlaySound();
                                                    storedHeight = obj.m_position.y;
                                                    clipboard = new ClipboardProceduralObjects(ClipboardProceduralObjects.ClipboardType.Single);
                                                    clipboard.single_object = new CacheProceduralObject(obj);
                                                    hoveredObj = null;
                                                }
                                                if (GUI.Button(new Rect(objScreenPos + new Vector2(12, 58), new Vector2(130, 22)), LocalizationManager.instance.current["layers_set"] + (showLayerSetScroll ? " ►" : "")))
                                                {
                                                    PlaySound();
                                                    // open layer scroll menu
                                                    showLayerSetScroll = !showLayerSetScroll;
                                                }
                                                if (GUI.Button(new Rect(objScreenPos + new Vector2(12, 81), new Vector2(130, 22)), LocalizationManager.instance.current["align_heights"]))
                                                {
                                                    PlaySound();
                                                    // align height
                                                    alignHeightObj.Clear();
                                                    alignHeightObj.Add(obj);
                                                }
                                                if (GUI.Button(new Rect(objScreenPos + new Vector2(12, 104), new Vector2(130, 22)), LocalizationManager.instance.current["delete"]))
                                                {
                                                    YieldConfirmDeletePanel(1, obj.m_position, delegate()
                                                    {
                                                        proceduralObjects.Remove(obj);
                                                        pObjSelection.Remove(obj);
                                                        activeIds.Remove(obj.id);
                                                        hoveredObj = null;
                                                    });
                                                }
                                                if (showLayerSetScroll)
                                                {
                                                    GUI.Box(new Rect(objScreenPos + new Vector2(144, 58), new Vector2(150, 160)), string.Empty);
                                                    scrollLayerSet = GUI.BeginScrollView(new Rect(objScreenPos + new Vector2(145, 59), new Vector2(148, 158)), scrollLayerSet, new Rect(0, 0, 124, 24 * layerManager.m_layers.Count + 26));
                                                    for (int i = 0; i < layerManager.m_layers.Count; i++)
                                                    {
                                                        if (layerManager.m_layers[i] == obj.layer)
                                                            GUI.color = Color.red;
                                                        if (GUI.Button(new Rect(1, i * 24 + 1, 122, 23), layerManager.m_layers[i].m_name))
                                                        {
                                                            PlaySound();
                                                            ResetLayerScrollmenu();
                                                            obj.layer = layerManager.m_layers[i];
                                                            pObjSelection.Clear();
                                                        }
                                                        GUI.color = Color.white;
                                                    }
                                                    if (obj.layer == null)
                                                        GUI.color = Color.red;
                                                    if (GUI.Button(new Rect(1, layerManager.m_layers.Count * 24 + 1, 122, 23), "<i>" + LocalizationManager.instance.current["layers_none"] + "</i>"))
                                                    {
                                                        PlaySound();
                                                        ResetLayerScrollmenu();
                                                        obj.layer = null;
                                                        pObjSelection.Clear();
                                                    }
                                                    GUI.color = Color.white;
                                                    GUI.EndScrollView();
                                                }
                                                #endregion
                                            }
                                            else
                                            {
                                                #region
                                                if (GUI.Button(new Rect(objScreenPos + new Vector2(12, -11), new Vector2(130, 22)), LocalizationManager.instance.current["copy"]))
                                                {
                                                    PlaySound();
                                                    clipboard = new ClipboardProceduralObjects(ClipboardProceduralObjects.ClipboardType.Selection);
                                                    clipboard.MakeSelectionList(pObjSelection);
                                                    storedHeight = obj.m_position.y;
                                                }
                                                if (GUI.Button(new Rect(objScreenPos + new Vector2(12, 12), new Vector2(130, 22)), LocalizationManager.instance.current["move_to"]))
                                                {
                                                    PlaySound();
                                                    // pObjSelection.Clear();
                                                    MoveSelection(pObjSelection, true);
                                                    placingSelection = true;
                                                    movingWholeModel = true;
                                                    toolAction = ToolAction.build;
                                                    editingWholeModel = true;
                                                    proceduralTool = true;
                                                    hoveredObj = null;
                                                }
                                                if (GUI.Button(new Rect(objScreenPos + new Vector2(12, 35), new Vector2(130, 22)), LocalizationManager.instance.current["export_selection"]))
                                                {
                                                    PlaySound();
                                                    var selection = new ClipboardProceduralObjects(ClipboardProceduralObjects.ClipboardType.Selection);
                                                    selection.MakeSelectionList(pObjSelection);
                                                    selection.ExportSelection("Selection " + DateTime.Now.ToString("F"), ExPObjManager);
                                                }
                                                if (GUI.Button(new Rect(objScreenPos + new Vector2(12, 58), new Vector2(130, 22)), LocalizationManager.instance.current["layers_set"] + (showLayerSetScroll ? " ►" : "")))
                                                {
                                                    PlaySound();
                                                    // open layer scroll menu
                                                    showLayerSetScroll = !showLayerSetScroll;
                                                }
                                                if (GUI.Button(new Rect(objScreenPos + new Vector2(12, 81), new Vector2(130, 22)), LocalizationManager.instance.current["align_heights"]))
                                                {
                                                    PlaySound();
                                                    // align height
                                                    alignHeightObj.Clear();
                                                    alignHeightObj.AddRange(pObjSelection);
                                                }
                                                if (GUI.Button(new Rect(objScreenPos + new Vector2(12, 104), new Vector2(130, 22)), LocalizationManager.instance.current["delete"]))
                                                {
                                                    YieldConfirmDeletePanel(pObjSelection.Count, pObjSelection[0].m_position, delegate()
                                                    {
                                                        for (int i = 0; i < pObjSelection.Count; i++)
                                                        {
                                                            proceduralObjects.Remove(pObjSelection[i]);
                                                            activeIds.Remove(pObjSelection[i].id);
                                                            // Object.Destroy(pObjSelection[i].gameObject);
                                                        }
                                                        pObjSelection.Clear();
                                                    });
                                                }
                                                if (showLayerSetScroll)
                                                {
                                                    GUI.Box(new Rect(objScreenPos + new Vector2(144, 58), new Vector2(150, 160)), string.Empty);
                                                    scrollLayerSet = GUI.BeginScrollView(new Rect(objScreenPos + new Vector2(145, 59), new Vector2(148, 158)), scrollLayerSet, new Rect(0, 0, 124, 24 * layerManager.m_layers.Count + 26));
                                                    for (int i = 0; i < layerManager.m_layers.Count; i++)
                                                    {
                                                        if (GUI.Button(new Rect(1, i * 24 + 1, 122, 23), layerManager.m_layers[i].m_name))
                                                        {
                                                            PlaySound();
                                                            ResetLayerScrollmenu();
                                                            foreach (var o in pObjSelection)
                                                                o.layer = layerManager.m_layers[i];
                                                            pObjSelection.Clear();
                                                        }
                                                    }
                                                    if (GUI.Button(new Rect(1, layerManager.m_layers.Count * 24 + 1, 122, 23), "<i>" + LocalizationManager.instance.current["layers_none"] + "</i>"))
                                                    {
                                                        PlaySound();
                                                        ResetLayerScrollmenu();
                                                        foreach (var o in pObjSelection)
                                                            o.layer = null;
                                                        pObjSelection.Clear();
                                                    }
                                                    GUI.EndScrollView();
                                                }
                                                #endregion
                                            }
                                        }
                                        else
                                        {
                                            if (GUI.Button(new Rect(objScreenPos + new Vector2(12, -11), new Vector2(130, 22)), LocalizationManager.instance.current["cancel"]))
                                                alignHeightObj.Clear();
                                        }
                                    }
                                    GUI.color = Color.red;
                                }
                                if (GUI.Button(new Rect(objScreenPos + new Vector2(-11, -11), new Vector2(23, 22)), "<size=20>+</size>"))
                                {
                                    PlaySound();
                                    ResetLayerScrollmenu();
                                    if (alignHeightObj.Count > 0)
                                    {
                                        AlignHeights(obj.m_position.y);
                                    }
                                    else
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
                                                if (obj == pObjSelection.First())
                                                    pObjSelection.Clear();
                                                else
                                                {
                                                    pObjSelection.Clear();
                                                    pObjSelection.Add(obj);
                                                }
                                            }
                                            else
                                            {
                                                pObjSelection.Clear();
                                                pObjSelection.Add(obj);
                                            }

                                        }
                                    }
                                }
                                GUI.color = Color.white;
                            }
                        }
                        SingleHoveredObj = hoveredObj;
                    }
                    if (!movingWholeModel)
                    {
                        var winrect = GUIUtils.ClampRectToScreen(GUI.Window(1094334744, window, DrawUIWindow, "Procedural Objects v" + ProceduralObjectsMod.VERSION));
                        if (proceduralTool && editingWholeModel)
                            window = new Rect(winrect.x, winrect.y, winrect.width, 463);
                        else
                            window = new Rect(winrect.x, winrect.y, winrect.width, 400);

                        if (showExternals)
                            externalsWindow = GUIUtils.ClampRectToScreen(GUI.Window(1094334745, externalsWindow, DrawExternalsWindow, LocalizationManager.instance.current["saved_pobjs"]));

                        textManager.DrawWindow();

                        if (advEdManager != null)
                        {
                            advEdManager.m_vertices = tempVerticesBuffer;
                            advEdManager.DrawWindow();
                        }

                        #region GUI when TOOL is active
                        if (currentlyEditingObject != null)
                        {
                            if (!editingWholeModel)
                            {
                                if (clickingRegion)
                                {
                                    GUI.color = uiColor;
                                    GUI.Box(GUIUtils.RectFromCorners(topLeftRegion, bottomRightRegion, true), "");
                                    GUI.color = Color.white;
                                }
                                bool vertWizardAllows = vertWizardData == null;
                                if (!vertWizardAllows)
                                    vertWizardAllows = !vertWizardData.enableMovement;

                                if (vertWizardAllows && showVerticesToolChoice)
                                {
                                    if (GUI.Button(new Rect(VerticesToolChoicePos, new Vector2(105, 22)), string.Empty))
                                    {
                                        PlaySound();
                                        verticesToolType = 0;
                                        showVerticesToolChoice = false;
                                    }
                                    if (GUI.Button(new Rect(VerticesToolChoicePos.x, VerticesToolChoicePos.y + 23, 105, 22), string.Empty))
                                    {
                                        PlaySound();
                                        verticesToolType = 1;
                                        showVerticesToolChoice = false;
                                    }
                                    if (GUI.Button(new Rect(VerticesToolChoicePos.x, VerticesToolChoicePos.y + 46, 105, 22), string.Empty))
                                    {
                                        PlaySound();
                                        verticesToolType = 2;
                                        showVerticesToolChoice = false;
                                    }
                                    if (editingVertexIndex.Count > 0)
                                    {
                                        if (GUI.Button(new Rect(VerticesToolChoicePos.x, VerticesToolChoicePos.y + 69, 105, 22), string.Empty))
                                        {
                                            PlaySound();
                                            showVerticesToolChoice = false;
                                            VerticesWizardData.FlattenSelection(currentlyEditingObject, editingVertexIndex, tempVerticesBuffer);
                                            Apply();
                                        }
                                        GUI.Label(new Rect(VerticesToolChoicePos.x, VerticesToolChoicePos.y + 69, 22, 22), ProceduralTool.terrainShift.m_texture);
                                        GUI.Label(new Rect(VerticesToolChoicePos.x + 23, VerticesToolChoicePos.y + 69, 80, 22), LocalizationManager.instance.current["flatten_selection"]);
                                    }
                                    GUI.Label(new Rect(VerticesToolChoicePos, new Vector2(22, 22)), ProceduralTool.moveVertices.m_texture);
                                    GUI.Label(new Rect(VerticesToolChoicePos.x, VerticesToolChoicePos.y + 23, 22, 22), ProceduralTool.rotateVertices.m_texture);
                                    GUI.Label(new Rect(VerticesToolChoicePos.x, VerticesToolChoicePos.y + 46, 22, 22), ProceduralTool.scaleVertices.m_texture);
                                    GUI.Label(new Rect(VerticesToolChoicePos.x + 23, VerticesToolChoicePos.y, 80, 22), LocalizationManager.instance.current["position"]);
                                    GUI.Label(new Rect(VerticesToolChoicePos.x + 23, VerticesToolChoicePos.y + 23, 80, 22), LocalizationManager.instance.current["rotation"]);
                                    GUI.Label(new Rect(VerticesToolChoicePos.x + 23, VerticesToolChoicePos.y + 46, 80, 22), LocalizationManager.instance.current["scale_obj"]);
                                }

                                if (vertWizardAllows && !KeyBindingsManager.instance.GetBindingFromName("edition_smoothMovements").GetBinding())
                                {
                                    foreach (Vertex vertex in tempVerticesBuffer)
                                    {
                                        if (vertex != null)
                                        {
                                            if (!vertex.IsDependent)
                                            {
                                                if (currentlyEditingObject.m_mesh.name == "ploppablecliffgrass" && vertex.Index >= currentlyEditingObject.allVertices.Length - 2)
                                                    continue;
                                                var vertexWorldPos = VertexWorldPosition(vertex);
                                                if (Camera.main.WorldToScreenPoint(vertexWorldPos).z < 0)
                                                    continue;
                                                if ((editingVertex && !editingVertexIndex.Contains(vertex.Index)) || !editingVertex)
                                                {
                                                    if (GUI.Button(new Rect(vertexWorldPos.WorldToGuiPoint() + new Vector2(-11, -11), new Vector2(23, 22)), "<size=20>+</size>"))
                                                    {
                                                        showVerticesToolChoice = false;
                                                        PlaySound();
                                                        editingVertex = true;
                                                        editingWholeModel = false;
                                                        if (editingVertexIndex.Count == 0 || Input.GetKey(KeyCode.LeftControl))
                                                            editingVertexIndex.Add(vertex.Index);
                                                        else
                                                        {
                                                            editingVertexIndex.Clear();
                                                            editingVertexIndex.Add(vertex.Index);
                                                        }
                                                        /*  if (Gizmos.Exists)
                                                                vertexShifting = CreateVertexShiftingDictionary(temp_storageVertex, editingVertexIndex, false, false);
                                                            else
                                                                vertexShifting = CreateVertexShiftingDictionary(temp_storageVertex, editingVertexIndex, true, true); */
                                                    }
                                                }
                                                else
                                                {
                                                    GUI.contentColor = Color.red;
                                                    if (GUI.Button(new Rect(vertexWorldPos.WorldToGuiPoint() + new Vector2(-11, -11), new Vector2(23, 22)), "<size=20><b>x</b></size>"))
                                                    {
                                                        PlaySound();
                                                        showVerticesToolChoice = false;
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
                            }
                            else if (Camera.main.WorldToScreenPoint(currentlyEditingObject.m_position).z >= 0)
                            {
                                Vector2 objPosition = currentlyEditingObject.m_position.WorldToGuiPoint();
                                if (GUI.Button(new Rect(objPosition + new Vector2(13, -26), new Vector2(100, 23)), LocalizationManager.instance.current["move_to"]))
                                {
                                    PlaySound();
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.moveTo, tempVerticesBuffer);
                                    movingWholeModel = true;
                                    toolAction = ToolAction.build;
                                    placingSelection = false;
                                    Gizmos.DestroyGizmo();
                                    xLine = null;
                                    yLine = null;
                                    zLine = null;
                                    textManager.CloseWindow();
                                    advEdManager = null;
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
                                        modeText += LocalizationManager.instance.current["scale_obj"] + "</i>";
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
                }
            }
        }
        public void DrawUIWindow(int id)
        {
            #region setup window
            GUI.DragWindow(new Rect(0, 0, 350, 30));
            if (GUI.Button(new Rect(356, 3, 30, 30), "X"))
            {
                PlaySound();
                ClosePO();
            }
            #endregion


            if (proceduralTool)
            {
                if (currentlyEditingObject != null)
                {
                    GUI.BeginGroup(new Rect(10, 30, 380, 442));
                    if (editingWholeModel)
                    {
                        // GENERAL TOOL
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
                        currentlyEditingObject.renderDistance = GUI.HorizontalSlider(new Rect(0, 352, 380, 20), Mathf.Floor(currentlyEditingObject.renderDistance), 50f, 16000f);

                        externalsSaveTextfield = GUI.TextField(new Rect(0, 368, 285, 28), externalsSaveTextfield);
                        if (File.Exists(ProceduralObjectsMod.ExternalsConfigPath + externalsSaveTextfield.ToFileName() + ".pobj"))
                        {
                            GUI.color = Color.red;
                            GUI.Label(new Rect(290, 368, 90, 28), "X", GUI.skin.button);
                            GUI.color = Color.white;
                        }
                        else
                        {
                            if (GUI.Button(new Rect(290, 368, 90, 28), LocalizationManager.instance.current["save"]))
                            {
                                PlaySound();
                                ExPObjManager.SaveToExternal(externalsSaveTextfield, new CacheProceduralObject(currentlyEditingObject));
                                externalsSaveTextfield = LocalizationManager.instance.current["enter_name"];
                            }
                        }

                        if (GUI.Button(new Rect(125, 400, 255, 28), LocalizationManager.instance.current["adv_edition"]))
                        {
                            PlaySound();
                            if (advEdManager != null)
                            {
                                advEdManager.showWindow = !advEdManager.showWindow;
                            }
                            else
                            {
                                advEdManager = new AdvancedEditionManager(currentlyEditingObject, Undo, Redo, Apply, () => { storedHeight = currentlyEditingObject.m_position.y; });
                                advEdManager.showWindow = true;
                            }
                        }

                        /*
                        if (currentlyEditingObject.RequiresUVRecalculation)
                        {
                            if (GUI.Button(new Rect(125, 400, 255, 28), LocalizationManager.instance.current["tex_uv_mode"] + " : " + LocalizationManager.instance.current[(currentlyEditingObject.disableRecalculation ? "uv_stretch" : "uv_repeat")]))
                            {
                                if (currentlyEditingObject.disableRecalculation)
                                {
                                    currentlyEditingObject.disableRecalculation = false;
                                    currentlyEditingObject.m_mesh.uv = Vertex.RecalculateUVMap(currentlyEditingObject, tempVerticesBuffer);
                                }
                                else
                                {
                                    currentlyEditingObject.disableRecalculation = true;
                                    currentlyEditingObject.m_mesh.uv = Vertex.DefaultUVMap(currentlyEditingObject);
                                }
                            }
                        }
                         * */
                        if (GUI.Button(new Rect(0, 400, 120, 28), "◄ " + LocalizationManager.instance.current["back"]))
                        {
                            PlaySound();
                            showVerticesToolChoice = false;
                            textManager.CloseWindow();
                            advEdManager = null;
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
                            chosenProceduralInfo = null;
                            pObjSelection.Clear();
                            SetCurrentlyEditingObj(null);
                        }
                        else
                        {
                            GUI.EndGroup();
                            if (GUI.Button(new Rect(15, 335, 185, 25), LocalizationManager.instance.current.visibilityString(currentlyEditingObject.m_visibility)))
                            {
                                PlaySound();
                                if (currentlyEditingObject.m_visibility == ProceduralObjectVisibility.Always)
                                    currentlyEditingObject.m_visibility = ProceduralObjectVisibility.DayOnly;
                                else if (currentlyEditingObject.m_visibility == ProceduralObjectVisibility.DayOnly)
                                    currentlyEditingObject.m_visibility = ProceduralObjectVisibility.NightOnly;
                                else if (currentlyEditingObject.m_visibility == ProceduralObjectVisibility.NightOnly)
                                    currentlyEditingObject.m_visibility = ProceduralObjectVisibility.Always;
                            }
                        }
                    }
                    else
                    {
                        // CUSTOMIZATION TOOL
                        GUI.Label(new Rect(35, 0, 300, 30), "<b><size=18>" + LocalizationManager.instance.current["vertex_tool"] + "</size></b>");
                        GUI.Label(new Rect(0, 0, 23, 23), "<size=18>+</size>", GUI.skin.button);
                        GUI.Label(new Rect(0, 30, 380, 330), "<b>" + LocalizationManager.instance.current["controls"] + ":</b>\n" + KeyBindingsManager.instance.GetBindingFromName("edition_smoothMovements").m_fullKeys + " : " + LocalizationManager.instance.current["hold_for_smooth"] +
                            "\n" + KeyBindingsManager.instance.GetBindingFromName("edition_smallMovements").m_fullKeys + " : " + LocalizationManager.instance.current["hold_slow_together"] + "\n\nCtrl : " + LocalizationManager.instance.current["hold_multiple_vertices"] +
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
                        if (TextParameters.CanHaveTextParameters(currentlyEditingObject))
                        {
                            if (GUI.Button(new Rect(205, 363, 185, 25), LocalizationManager.instance.current["text_customization"]))
                            {
                                PlaySound();
                                showVerticesToolChoice = false;
                                textManager.Edit(currentlyEditingObject, new Vector2(window.x + window.width, window.y));
                            }
                        }
                        if (GUI.Button(new Rect(15, 335, 185, 25), LocalizationManager.instance.current["delete"]))
                            DeleteObject();
                        if (GUI.Button(new Rect(15, 363, 185, 25), "◄ " + LocalizationManager.instance.current["back"]))
                        {
                            PlaySound();
                            showVerticesToolChoice = false;
                            textManager.CloseWindow();
                            advEdManager = null;
                            toolAction = ToolAction.none;
                            ToolHelper.FullySetTool<ProceduralTool>();
                            editingVertex = false;
                            editingVertexIndex.Clear();
                            editingWholeModel = false;
                            proceduralTool = false;
                            chosenProceduralInfo = null;
                            pObjSelection.Clear();
                            SetCurrentlyEditingObj(null);
                        }
                    }
                    if (GUI.Button(new Rect(205, 335, 185, 25), (editingWholeModel ? LocalizationManager.instance.current["vertex_customization"] : LocalizationManager.instance.current["general_tool"])))
                    {
                        PlaySound();
                        showVerticesToolChoice = false;
                        if (editingWholeModel)
                        {
                            toolAction = ToolAction.vertices;
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
                            toolAction = ToolAction.none;
                            SwitchToMainTool();
                        }
                    }
                }
            }
            else
            {
                if (chosenProceduralInfo == null)
                {
                    GUI.Label(new Rect(5, 26, 350, 38), LocalizationManager.instance.current["spawn_new"]);

                    if (TextureUtils.LocalTexturesCount == 0 && TextureUtils.TextureResources.Count == 0)
                        GUI.Label(new Rect(10, 45, 350, 45), LocalizationManager.instance.current["no_tex"] + "\n" + LocalizationManager.instance.current["cant_create_basic"]);
                    else
                        GUI.Label(new Rect(10, 60, 350, 30), LocalizationManager.instance.current["local_tex"] + " : ");

                    if (GUI.Button(new Rect(150, 55, 75, 28), LocalizationManager.instance.current["refresh"]))
                    {
                        PlaySound();
                        basicTextures = basicTextures.LoadModConfigTextures();
                    }
                    if (GUI.Button(new Rect(230, 55, 155, 28), LocalizationManager.instance.current["open_tex"]))
                    {
                        PlaySound();
                        if (Directory.Exists(ProceduralObjectsMod.TextureConfigPath))
                            Application.OpenURL("file://" + ProceduralObjectsMod.TextureConfigPath);
                    }
                    if (GUI.Button(new Rect(10, 84, 375, 24), LocalizationManager.instance.current["go_to_wiki"]))
                    {
                        PlaySound();
                        PlatformService.ActivateGameOverlayToWebPage(ProceduralObjectsMod.DOCUMENTATION_URL);
                    }
                    if (GUI.Button(new Rect(10, 110, 375, 24), LocalizationManager.instance.current["open_kbd_cfg"]))
                    {
                        PlaySound();
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

                    if (GUI.Button(new Rect(10, 365, 170, 28), LocalizationManager.instance.current["layers"]))
                    {
                        PlaySound();
                        layerManager.showWindow = !layerManager.showWindow;
                    }

                    if (GUI.Button(new Rect(185, 365, 205, 28), LocalizationManager.instance.current["saved_pobjs"]))
                    {
                        PlaySound();
                        renamingExternalString = "";
                        renamingExternal = -1;
                        ExPObjManager.LoadExternals(basicTextures, textManager.fontManager);
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
                    scrollTextures = GUI.BeginScrollView(new Rect(10, 60, 350, 330), scrollTextures, new Rect(0, 0, 320, 80 * basicTextures.Count() + 142));
                    GUI.Label(new Rect(10, 0, 300, 28), basicTextures.Count().ToString() + " " + LocalizationManager.instance.current["tex_in_total"] + " : " + TextureUtils.LocalTexturesCount.ToString() + " " + LocalizationManager.instance.current["local"] + " + " + TextureResourceInfo.TotalTextureCount(TextureUtils.TextureResources) + " " + LocalizationManager.instance.current["from_wk"]);
                    if (GUI.Button(new Rect(10, 30, 147.5f, 30), LocalizationManager.instance.current["open_folder"]))
                    {
                        PlaySound();
                        if (Directory.Exists(ProceduralObjectsMod.TextureConfigPath))
                            Application.OpenURL("file://" + ProceduralObjectsMod.TextureConfigPath);
                    }
                    if (GUI.Button(new Rect(162.5f, 30, 147.5f, 30), LocalizationManager.instance.current["refresh"]))
                    {
                        PlaySound();
                        basicTextures = basicTextures.LoadModConfigTextures();
                    }
                    if (GUI.Button(new Rect(10, 61, 300, 79), LocalizationManager.instance.current["none_defaulttex"]))
                    {
                        PlaySound();
                        editingVertex = false;
                        editingVertexIndex.Clear();
                        editingWholeModel = false;
                        proceduralTool = false;
                        ToolHelper.FullySetTool<DefaultTool>();
                        Gizmos.DestroyGizmo();
                        xLine = null;
                        yLine = null;
                        zLine = null;
                        SpawnObject(chosenProceduralInfo);
                        tempVerticesBuffer = Vertex.CreateVertexList(currentlyEditingObject);
                        ToolHelper.FullySetTool<ProceduralTool>();
                        proceduralTool = true;
                        movingWholeModel = true;
                        toolAction = ToolAction.build;
                        placingSelection = false;
                        editingVertex = false;
                        chosenProceduralInfo = null;
                    }
                    for (int i = 0; i < basicTextures.Count(); i++)
                    {
                        if (GUI.Button(new Rect(10, i * 80 + 142, 300, 79), string.Empty))
                        {
                            PlaySound();
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
                            tempVerticesBuffer = Vertex.CreateVertexList(currentlyEditingObject);
                            ToolHelper.FullySetTool<ProceduralTool>();
                            proceduralTool = true;
                            movingWholeModel = true;
                            toolAction = ToolAction.build;
                            placingSelection = false;
                            editingVertex = false;
                            chosenProceduralInfo = null;
                        }
                        GUI.Label(new Rect(15, i * 80 + 145, 85, 74), basicTextures[i]);
                        int pos = basicTextures[i].name.LastIndexOf(ProceduralObjectsMod.IsLinux ? "/" : @"\") + 1;
                        GUI.Label(new Rect(105, i * 80 + 152, 190, 52), basicTextures[i].name.Substring(pos, basicTextures[i].name.Length - pos).Replace(".png", ""));
                    }
                    GUI.EndScrollView();
                }
            }
        }
        public void DrawExternalsWindow(int id)
        {
            GUI.DragWindow(new Rect(0, 0, 350, 30));
            if (GUI.Button(new Rect(356, 3, 30, 30), "X"))
            {
                PlaySound();
                CloseExternalsWindow();
            }
            GUI.Label(new Rect(10, 30, 298, 37), LocalizationManager.instance.current["externals_desc"]);
            if (renamingExternal == -1)
            {
                if (GUI.Button(new Rect(310, 35, 85, 28), LocalizationManager.instance.current["refresh"]))
                    ExPObjManager.LoadExternals(basicTextures, textManager.fontManager);
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
                            PlaySound();
                        }
                    }
                    if (ExPObjManager.m_externals[i].isWorkshop)
                        GUI.Label(new Rect(258, i * 40 + 5, 67, 30), "[<i>Workshop</i>]", GUI.skin.button);
                    else
                    {
                        if (GUI.Button(new Rect(258, i * 40 + 5, 64, 30), LocalizationManager.instance.current[(renamingExternal == i) ? "ok" : "rename"]))
                        {
                            PlaySound();
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
                                PlaySound();
                                ExPObjManager.DeleteExternal(ExPObjManager.m_externals[i], basicTextures, textManager.fontManager);
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
            // currentlyEditingObject.meshStatus = 2;
            List<Vector3> posArray = new List<Vector3>(tempVerticesBuffer.GetPositionsArray());
            // sets mesh renderer vertices
            currentlyEditingObject.m_mesh.SetVertices(posArray);
            if (!currentlyEditingObject.isPloppableAsphalt)
            {
                currentlyEditingObject.m_mesh.RecalculateNormals(60);
                currentlyEditingObject.m_mesh.RecalculateBounds();
            }

            //UV map recalculation
            if (currentlyEditingObject.RequiresUVRecalculation && !currentlyEditingObject.disableRecalculation)
            {
                try
                {
                    currentlyEditingObject.m_mesh.uv = Vertex.RecalculateUVMap(currentlyEditingObject, tempVerticesBuffer);
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
            SetCurrentlyEditingObj(v);
        }
        public ProceduralObject PlaceCacheObject(CacheProceduralObject cacheObj, bool setCurrentlyEditing)
        {
            /*
             * "HOLLY CRAPPY SHITTY STUFF"
             * 
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
             * */
            ToolHelper.FullySetTool<ProceduralTool>();
            ToolsModifierControl.mainToolbar.CloseEverything();
            var obj = new ProceduralObject(cacheObj, proceduralObjects.GetNextUnusedId(), ToolsModifierControl.cameraController.m_currentPosition + new Vector3(0, -8, 0));
            proceduralObjects.Add(obj);
            if (setCurrentlyEditing)
            {
                SetCurrentlyEditingObj(obj);
                tempVerticesBuffer = Vertex.CreateVertexList(obj);
            }
            movingWholeModel = true;
            toolAction = ToolAction.build;
            placingSelection = false;
            proceduralTool = true;
            if (!obj.isPloppableAsphalt)
            {
                obj.m_mesh.RecalculateNormals(60);
                obj.m_mesh.RecalculateBounds();
            }
            if (obj.RequiresUVRecalculation && !obj.disableRecalculation)
            {
                if (obj == currentlyEditingObject)
                    obj.m_mesh.uv = Vertex.RecalculateUVMap(obj, tempVerticesBuffer);
                else
                    obj.m_mesh.uv = Vertex.RecalculateUVMap(obj, Vertex.CreateVertexList(obj));
            }
            return obj;
        }
        public ProceduralObject CloneObject(ProceduralObject source)
        {
            var obj = new ProceduralObject()
            {
                id = proceduralObjects.GetNextUnusedId(),
                m_mesh = source.m_mesh.InstantiateMesh(),
                m_material = GameObject.Instantiate(source.m_material),
                m_position = source.m_position,
                m_rotation = source.m_rotation,
                customTexture = source.customTexture,
                layer = source.layer,
                tilingFactor = source.tilingFactor,
                renderDistance = source.renderDistance,
                m_scale = source.m_scale,
                isPloppableAsphalt = source.isPloppableAsphalt,
                disableRecalculation = source.disableRecalculation,
                m_textParameters = (source.m_textParameters == null) ? null : TextParameters.Clone(source.m_textParameters, false),
                basePrefabName = source.basePrefabName,
                baseInfoType = source.baseInfoType,
                _baseProp = source._baseProp,
                _baseBuilding = source._baseBuilding,
                m_visibility = source.m_visibility,
            };
            obj.allVertices = obj.m_mesh.vertices;
            obj.historyEditionBuffer = new HistoryBuffer(obj);
            proceduralObjects.Add(obj);
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
            ResetLayerScrollmenu();
            textManager.CloseWindow();
            advEdManager = null;
            CloseExternalsWindow();
            alignHeightObj.Clear();
            editingVertex = false;
            editingVertexIndex.Clear();
            editingWholeModel = false;
            proceduralTool = false;
            SetCurrentlyEditingObj(null);
            chosenProceduralInfo = null;
            rotWizardData = null;
            Gizmos.DestroyGizmo();
            xLine = null;
            yLine = null;
            zLine = null;

            if (currentToolType == typeof(PropTool) || currentToolType == typeof(BuildingTool))
            {
                CallConvertToPO(ToolsModifierControl.toolController.CurrentTool);
            }
            else if (currentToolType != typeof(ProceduralTool))
            {
                ToolHelper.FullySetTool<ProceduralTool>();
                ToolsModifierControl.mainToolbar.CloseEverything();
            }
            else
            {
                ToolHelper.FullySetTool<DefaultTool>();
            }
        }
        public void CallConvertToPO(ToolBase tool)
        {
            /*
            var isBuilding = tool.GetType() == typeof(BuildingTool);
            BuildingInfo.SubInfo[] subbuildings = new BuildingInfo.SubInfo[] {};
            if (isBuilding)
                subbuildings = ((BuildingTool)ToolsModifierControl.toolController.CurrentTool).m_prefab.m_subBuildings; */
            var type = tool.GetType();
            bool convertible = true;
            if (type == typeof(PropTool))
            {
                if (!((PropTool)tool).m_prefab.m_mesh.isReadable)
                    convertible = false;
            }
            else if (type == typeof(BuildingTool))
            {
                if (!((BuildingTool)tool).m_prefab.m_mesh.isReadable)
                    convertible = false;
            }
            if (convertible)
                ConvertToProcedural(tool);
            else
            {
                generalShowUI = false;
                string prevText = ConfirmNoButton.text;
                ConfirmNoButton.isVisible = false;
                ConfirmPanel.ShowModal(LocalizationManager.instance.current["incompatibleAssetPopup_title"], LocalizationManager.instance.current["incompatibleAssetPopup_desc"], delegate(UIComponent comp, int r)
                {
                    generalShowUI = true;
                    ConfirmNoButton.isVisible = true;
                });
            }
            /*
            if (isBuilding)
            {
                if (subbuildings.Length > 0)
                {
                    movingWholeModel = false;
                    generalShowUI = false;
                    toolAction = ToolAction.none;
                    ConfirmPanel.ShowModal(LocalizationManager.instance.current[""], LocalizationManager.instance.current[""], delegate(UIComponent comp, int rep)
                    {
                        if (rep == 1)
                        {
                            var subs = ProceduralUtils.ConstructSubBuildings(currentlyEditingObject, proceduralObjects);
                            MoveSelection(subs.Keys.ToList(), false);
                            placingSelection = true;
                            movingWholeModel = true;
                            editingWholeModel = true;
                            proceduralTool = true;
                        }
                        else
                            movingWholeModel = true;
                        generalShowUI = true;
                        toolAction = ToolAction.build;
                    });
                }
            } */
        }
        private void CloseExternalsWindow()
        {
            showExternals = false;
            renamingExternal = -1;
            renamingExternalString = "";
        }
        private void ResetLayerScrollmenu()
        {
            showLayerSetScroll = false;
            scrollLayerSet = Vector2.zero;
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
                ToolsModifierControl.mainToolbar.CloseEverything();
                if (info.isBasicShape && basicTextures.Count > 0)
                {
                    movingWholeModel = false;
                    proceduralTool = false;
                    SetCurrentlyEditingObj(null);
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
                    tempVerticesBuffer = Vertex.CreateVertexList(currentlyEditingObject);
                    ToolHelper.FullySetTool<ProceduralTool>();
                    proceduralTool = true;
                    movingWholeModel = true;
                    toolAction = ToolAction.build;
                    placingSelection = false;
                    editingVertex = false;
                }
            }
            else if (tool.GetType() == typeof(BuildingTool))
            {
                ProceduralInfo info = availableProceduralInfos.Where(pInf => pInf.buildingPrefab != null).FirstOrDefault(pInf => pInf.buildingPrefab == ((BuildingTool)tool).m_prefab);
                ToolsModifierControl.mainToolbar.CloseEverything();
                if (info.isBasicShape && basicTextures.Count > 0)
                {
                    movingWholeModel = false;
                    proceduralTool = false;
                    SetCurrentlyEditingObj(null);
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
                    tempVerticesBuffer = Vertex.CreateVertexList(currentlyEditingObject);
                    ToolHelper.FullySetTool<ProceduralTool>();
                    proceduralTool = true;
                    movingWholeModel = true;
                    toolAction = ToolAction.build;
                    placingSelection = false;
                    editingVertex = false;
                }
            }
        }
        private void Paste(ClipboardProceduralObjects clipboard)
        {
            if (clipboard != null)
            {
                textManager.CloseWindow();
                advEdManager = null;
                if (clipboard.type == ClipboardProceduralObjects.ClipboardType.Single)
                {
                    placingSelection = false;
                    pObjSelection.Clear();
                    PlaceCacheObject(clipboard.single_object, true);
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
        private void AlignHeights(float height)
        {
            foreach (var po in alignHeightObj)
            {
                po.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.position, null);
                po.m_position.y = height;
                po.historyEditionBuffer.ConfirmNewStep(null);
            }
            alignHeightObj.Clear();
        }
        private void MoveSelection(List<ProceduralObject> objects, bool registerHistory)
        {
            for (int i = 0; i < objects.Count; i++)
            {
                ProceduralObject obj = objects[i];
                if (registerHistory)
                    obj.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.moveTo, tempVerticesBuffer);
                if (i == 0)
                {
                    SetCurrentlyEditingObj(obj);
                    obj.tempObj = new GameObject();
                    obj.tempObj.transform.position = obj.m_position;
                    obj.tempObj.transform.rotation = obj.m_rotation;
                }
                else
                {
                    obj.tempObj = new GameObject();
                    obj.tempObj.transform.position = obj.m_position;
                    obj.tempObj.transform.rotation = obj.m_rotation;
                    obj.tempObj.transform.SetParent(currentlyEditingObject.tempObj.transform, true);
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
            Vector3 effectPos = currentlyEditingObject.m_position;
            if (placingSelection)
            {
                pObjSelection.Clear();
                ToolHelper.FullySetTool<ProceduralTool>();
                editingVertex = false;
                editingVertexIndex.Clear();
                editingWholeModel = false;
                proceduralTool = false;
                Singleton<EffectManager>.instance.DispatchEffect(Singleton<BuildingManager>.instance.m_properties.m_placementEffect,
                    new EffectInfo.SpawnArea(currentlyEditingObject.m_position, Vector3.up, 10f), Vector3.zero, 0f, 1f, Singleton<AudioManager>.instance.DefaultGroup, 0u, true);
                for (int i = 0; i < proceduralObjects.Count; i++)
                {
                    proceduralObjects[i].historyEditionBuffer.ConfirmNewStep(tempVerticesBuffer);
                    if (proceduralObjects[i] == currentlyEditingObject)
                        continue;
                    if (proceduralObjects[i].tempObj == null)
                        continue;
                    if (proceduralObjects[i].tempObj.transform.parent = currentlyEditingObject.tempObj.transform)
                    {
                        Singleton<EffectManager>.instance.DispatchEffect(Singleton<BuildingManager>.instance.m_properties.m_placementEffect,
                            new EffectInfo.SpawnArea(proceduralObjects[i].tempObj.transform.position, Vector3.up, 10f), Vector3.zero, 0f, 1f, Singleton<AudioManager>.instance.DefaultGroup, 0u, true);
                        proceduralObjects[i].tempObj.transform.parent = null;
                        UnityEngine.Object.Destroy(proceduralObjects[i].tempObj);
                        proceduralObjects[i].tempObj = null;
                    }
                }
                UnityEngine.Object.Destroy(currentlyEditingObject.tempObj);
                currentlyEditingObject.tempObj = null;
                SetCurrentlyEditingObj(null);
                chosenProceduralInfo = null;
                movingWholeModel = false;
                placingSelection = false;
                CloseExternalsWindow();
                rotWizardData = null;
                yOffset = 0f;
            }
            else
            {
                currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(tempVerticesBuffer);
                movingWholeModel = false;
                editingWholeModel = false;
                editingVertex = false;
                editingVertexIndex.Clear();
                CloseExternalsWindow();
                rotWizardData = null;
                chosenProceduralInfo = null;
                pObjSelection.Clear();
                Singleton<EffectManager>.instance.DispatchEffect(Singleton<BuildingManager>.instance.m_properties.m_placementEffect, 
                    new EffectInfo.SpawnArea(currentlyEditingObject.m_position, Vector3.up, 10f), Vector3.zero, 0f, 1f, Singleton<AudioManager>.instance.DefaultGroup, 0u, true);
                SetCurrentlyEditingObj(null);
                yOffset = 0f;
                proceduralTool = false;
                // StoreLineComponents(Gizmos.CreateGizmo(currentlyEditingObject.m_position, true));
                ToolHelper.FullySetTool<ProceduralTool>();
            }
            toolAction = ToolAction.none;
            movingWholeRaycast = Vector3.zero;
        }
        private void Undo()
        {
            Vertex[] buffer;
            var type = currentlyEditingObject.historyEditionBuffer.UndoLastStep(tempVerticesBuffer, out buffer);
            if (type == EditingStep.StepType.vertices || type == EditingStep.StepType.mirrorX || type == EditingStep.StepType.mirrorY || type == EditingStep.StepType.mirrorZ
                 || type == EditingStep.StepType.stretchX || type == EditingStep.StepType.stretchY || type == EditingStep.StepType.stretchZ)
            {
                if (type == EditingStep.StepType.vertices)
                    tempVerticesBuffer = buffer;
                Apply();
            }
        }
        private void Redo()
        {
            Vertex[] buffer;
            var type = currentlyEditingObject.historyEditionBuffer.RedoUndoneStep(tempVerticesBuffer, out buffer);
            if (type == EditingStep.StepType.vertices || type == EditingStep.StepType.mirrorX || type == EditingStep.StepType.mirrorY || type == EditingStep.StepType.mirrorZ
                 || type == EditingStep.StepType.stretchX || type == EditingStep.StepType.stretchY || type == EditingStep.StepType.stretchZ)
            {
                if (type == EditingStep.StepType.vertices)
                    tempVerticesBuffer = buffer;
                Apply();
            }
        }
        public void DeleteObject()
        {
            YieldConfirmDeletePanel(1, currentlyEditingObject.m_position, delegate()
            {
                editingVertex = false;
                editingVertexIndex.Clear();
                editingWholeModel = false;
                proceduralTool = false;
                movingWholeModel = false;
                placingSelection = false;
                textManager.CloseWindow();
                advEdManager = null;
                proceduralObjects.Remove(currentlyEditingObject);
                activeIds.Remove(currentlyEditingObject.id);
                //  Object.Destroy(currentlyEditingObject.gameObject);
                SetCurrentlyEditingObj(null);
                Gizmos.DestroyGizmo();
                xLine = null;
                yLine = null;
                zLine = null;
            });
        }
        public void YieldConfirmDeletePanel(int involvedCount, Vector3 position, Action a)
        {
            if (involvedCount < ProceduralObjectsMod.ConfirmDeletionThreshold.value || !ProceduralObjectsMod.ShowConfirmDeletion.value)
            {
                Singleton<EffectManager>.instance.DispatchEffect(Singleton<BuildingManager>.instance.m_properties.m_placementEffect,
                    new EffectInfo.SpawnArea(position, Vector3.up, 10f), Vector3.zero, 0f, 1f, Singleton<AudioManager>.instance.DefaultGroup, 0u, true);
                a.Invoke();
                toolAction = ToolAction.none;
            }
            else
            {
                var prevToolAction = toolAction;
                toolAction = ToolAction.none;
                Singleton<AudioManager>.instance.PlaySound(audiosClips[1]);
                generalShowUI = false;
                ConfirmPanel.ShowModal(LocalizationManager.instance.current["confirmDeletionPopup_title"],
                    involvedCount > 1 ? string.Format(LocalizationManager.instance.current["confirmDeletionPopup_descSelection"], involvedCount) : LocalizationManager.instance.current["confirmDeletionPopup_descSingle"],
                    delegate(UIComponent comp, int ret)
                    {
                        if (ret == 1)
                        {
                            a.Invoke();
                            Singleton<EffectManager>.instance.DispatchEffect(Singleton<BuildingManager>.instance.m_properties.m_bulldozeEffect,
                                new EffectInfo.SpawnArea(position, Vector3.up, 10f), Vector3.zero, 0f, 1f, Singleton<AudioManager>.instance.DefaultGroup, 0u, true);
                        }
                        else
                            toolAction = prevToolAction;
                        generalShowUI = true;
                    });
            }

        }
        private void ClosePO()
        {
            ResetLayerScrollmenu();
            CloseExternalsWindow();
            advEdManager = null;
            toolAction = ToolAction.none;
            editingVertex = false;
            editingVertexIndex.Clear();
            editingWholeModel = false;
            proceduralTool = false;
            SetCurrentlyEditingObj(null);
            chosenProceduralInfo = null;
            showVerticesToolChoice = false;
            vertWizardData = null;
            pObjSelection.Clear();
            alignHeightObj.Clear();
            tabSwitchTimer = 0;
            ToolHelper.FullySetTool<DefaultTool>();
            SingleHoveredObj = null;
            Gizmos.DestroyGizmo();
            xLine = null;
            yLine = null;
            zLine = null;
        }
        private void SetCurrentlyEditingObj(ProceduralObject obj)
        {
            currentlyEditingObject = obj;
        }
        private void SetupLocalization()
        {
            ProceduralObjectsMod.LanguageUsed.value = "default";
            LocalizationManager.instance.SelectCurrent();
            externalsSaveTextfield = LocalizationManager.instance.current["enter_name"];
            layerManager.UpdateLocalization();
        }
        public static void PlaySound(int index = 0)
        {
            Singleton<AudioManager>.instance.PlaySound(audiosClips[index]);
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
    public enum ToolAction
    {
        build,
        vertices,
        none
    }
}
