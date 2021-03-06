﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using UnityEngine.Rendering;
using UnityEngine;
using System.IO;

using ProceduralObjects.Classes;
using ProceduralObjects.UI;
using ProceduralObjects.Tools;
using ProceduralObjects.SelectionMode;
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
        public static ProceduralObjectsLogic instance;

        public List<ProceduralObject> proceduralObjects, pObjSelection;
        public HashSet<int> activeIds;
        public ProceduralObject currentlyEditingObject;
        public List<ProceduralInfo> availableProceduralInfos;
        public List<POGroup> groups;
        public POGroup selectedGroup;
        public ProceduralInfo chosenProceduralInfo = null;
        public uint failedToLoadObjects;

        public ClipboardProceduralObjects clipboard = null;
        public float storedHeight = 0f, yOffset = 0f;

        public bool proceduralTool = false, editingVertex = false, selectingNewGrpRoot = false, movingWholeModel = false, placingSelection = false, editingWholeModel = false, generalShowUI = true, showExternals = false, getBackToGeneralTool = false;
        public List<int> editingVertexIndex;
        public Rect window = new Rect(155, 100, 400, 400);
        public float _winHeight;
        public Rect externalsWindow = new Rect(555, 100, 400, 400);
        public string externalsSaveTextfield = "Enter object name here";
        public Vector2 scrollVertices = Vector2.zero, scrollObjects = Vector2.zero, scrollTextures = Vector2.zero, scrollExternals = Vector2.zero;
        public Vertex[] tempVerticesBuffer;
        public Type previousToolType;
        public static AxisEditionState axisState = AxisEditionState.none;
        public Vector3 axisHitPoint = Vector3.zero, gizmoOffset = Vector3.zero;
        private RotationWizardData rotWizardData = null;
        private VerticesWizardData vertWizardData = null;
        private ProceduralObject SingleHoveredObj = null;
        public SelectionModeAction selectionModeAction = null;

        public static ToolAction toolAction = ToolAction.none;
        public static Vector3 movingWholeRaycast = Vector3.zero;
        private Dictionary<ProceduralObject, Transform> moveToSelection;

        public static byte verticesToolType = 0;
        public static float tabSwitchTimer = 0f;

        public Camera renderCamera;
        private Material spriteMat;
        private Color uiColor;
        public GUIPainter painter;

        public GUIStyle redLabelStyle = new GUIStyle();
        public static byte actionMode = 0;

        // drag selection
        public Vector2 topLeftRegion = Vector2.zero, bottomRightRegion = Vector2.zero;
        public bool clickingRegion = false;

        public ProceduralObjectsButton mainButton;
        private UIButton ConfirmNoButton;

        private int renamingExternal = -1;
        private string renamingExternalString = "";

        public ExternalProceduralObjectsManager ExPObjManager;
        private FontManager fontManager;
        private TextCustomizationManager textManager;
        public LayerManager layerManager;
        public AdvancedEditionManager advEdManager;
        public ModuleManager moduleManager;
        public SelectionFilters filters;

        public bool showLayerSetScroll = false, showMoreTools = false;
        private Vector2 scrollLayerSet = Vector2.zero;

        private static AudioClip[] audiosClips;

        void Start()
        {
            Debug.Log("[ProceduralObjects] Game start procedure started.");
            var startTime = DateTime.Now;
            pObjSelection = new List<ProceduralObject>();
            SelectionMode.SelectionModeAction.InitializeSMActions();
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
            // basicTextures = basicTextures.LoadTextures().OrderBy(tex => tex.name).ToList();
            new TextureManager().LoadTextures();
            availableProceduralInfos = ProceduralUtils.CreateProceduralInfosList();
            Debug.Log("[ProceduralObjects] Found " + availableProceduralInfos.Count.ToString() + " procedural infos.");
            fontManager = new FontManager(); // loads fonts
            FontManager.instance = fontManager;
            textManager = new TextCustomizationManager(fontManager);
            ExPObjManager = new ExternalProceduralObjectsManager();
            ExPObjManager.LoadExternals(fontManager);
            GUIPainter.GenerateHuePicker();
            spriteMat = new Material(Shader.Find("Sprites/Default"));
            spriteMat.color = new Color(1f, 0.17f, 0.17f, .24f);
            uiColor = new Color(1, 1, 1, .5f);
            GUIUtils.Setup();
            if (LocalizationManager.instance == null)
                LocalizationManager.CreateManager();
            else
                LocalizationManager.instance.SelectCurrent();
            externalsSaveTextfield = LocalizationManager.instance.current["enter_name"];
            ProceduralTool.SetupControlsStrings();
            RenderOptions.Initialize();
            layerManager = new LayerManager(this);
            moduleManager = new ModuleManager(this);
            filters = new SelectionFilters();
            if (ProceduralObjectsMod.tempLayerData != null)
            {
                layerManager.m_layers = ProceduralObjectsMod.tempLayerData.ToList();
                ProceduralObjectsMod.tempLayerData = null;
            }
            if (ProceduralObjectsMod.tempContainerData != null)
            {
                this.LoadContainerData(ProceduralObjectsMod.tempContainerData);
                groups = this.BuildGroupsFromData();
                ProceduralObjectsMod.tempContainerData = null;
            }
            else
            {
                proceduralObjects = new List<ProceduralObject>();
                activeIds = new HashSet<int>();
            }
            new POStatisticsManager(this);
            ProceduralTool.CreateCursors();
            // CT default actions
            new POToolAction("recenterObjOrigin", TextureUtils.LoadTextureFromAssembly("CTA_recenterOrigin"), POActionType.Global, null, ProceduralUtils.RecenterObjOrigin);
            new POToolAction("mergeVertices", TextureUtils.LoadTextureFromAssembly("CTA_mergeVertices"), POActionType.Selection, VertexUtils.MergeVertices, null);
            new POToolAction("alignVertices", TextureUtils.LoadTextureFromAssembly("CTA_alignVertices"), POActionType.Selection, VertexUtils.AlignVertices, null);
            new POToolAction("flatten_selection", TextureUtils.LoadTextureFromAssembly("CTA_flatten"), POActionType.Selection, VertexUtils.FlattenSelection, null);
            new POToolAction("snapSelectionToGround", TextureUtils.LoadTextureFromAssembly("CTA_snapSelG"), POActionType.Selection, VertexUtils.SnapSelectionToGround, null);
            new POToolAction("snapEachToGround", TextureUtils.LoadTextureFromAssembly("CTA_snapEachG"), POActionType.Selection, VertexUtils.SnapEachToGround, null);
            new POToolAction("conformTerrain", TextureUtils.LoadTextureFromAssembly("CTA_conformT"), POActionType.Selection, VertexUtils.ConformSelectionToTerrain, null);
            new POToolAction("conformTerrainNetBuildings", TextureUtils.LoadTextureFromAssembly("CTA_conformTNB"), POActionType.Selection, VertexUtils.ConformSelectionToTerrainNetBuildings, null);

            redLabelStyle.normal.textColor = Color.red;
            editingVertexIndex = new List<int>();
            LocaleManager.eventLocaleChanged += SelectSetupLocalization;
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

            // Conversion for Move It
            if (PO_MoveIt.queuedConversion.Count > 0)
            {
                var queued = new HashSet<POConversionRequest>(PO_MoveIt.queuedConversion);
                foreach (var req in queued)
                {
                    if (req.failed)
                        continue;
                    try
                    {
                        var po = new ProceduralObject();
                        Color c = req.color;
                        if (req.buildingInfo != null)
                            po.ConstructObject(req.buildingInfo, proceduralObjects.GetNextUnusedId(), null);
                        else if (req.propInfo != null)
                        {
                            po.ConstructObject(req.propInfo, proceduralObjects.GetNextUnusedId(), null, true);
                            if (req.propInfo.m_mesh.name == "ploppableasphalt-prop" || req.propInfo.m_mesh.name == "ploppableasphalt-decal")
                                c = ProceduralUtils.GetPloppableAsphaltCfg();
                        }
                        po.m_position = req.position;
                        po.m_rotation = req.rotation;
                        po.m_color = c;
                        po.m_material.color = c;
                        proceduralObjects.Add(po);
                        req.converted = po;
                    }
                    catch
                    {
                        req.failed = true;
                    }
                }
            }

            GUIPainter.UpdatePainter(painter);

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

            if (proceduralObjects != null)
            {
                var sqrDynMinThreshold = ProceduralObjectsMod.DynamicRDMinThreshold.value * ProceduralObjectsMod.DynamicRDMinThreshold.value;
                for (int i = 0; i < proceduralObjects.Count; i++)
                {
                    var obj = proceduralObjects[i];
                    var sqrRd = (obj.renderDistance * RenderOptions.instance.globalMultiplier);
                    sqrRd *= sqrRd;
                    var sqrDist = (renderCamera.transform.position - obj.m_position).sqrMagnitude;
                    obj._insideRenderView = sqrDist <= sqrRd;

                    if (obj._insideRenderView)
                    {
                        if (renderCamera.WorldToScreenPoint(obj.m_position).z >= 0)
                            obj._insideUIview = sqrDist <= Mathf.Clamp(sqrRd * 0.6f, sqrDynMinThreshold, 1400000);
                        else
                            obj._insideUIview = false;
                    }
                    else
                    {
                        obj._insideUIview = false;
                        continue;
                    }

                    bool show = obj.layer == null;
                    if (!show)
                        show = !obj.layer.m_isHidden;

                    if (show)
                    {
                        try
                        {
                            if (RenderOptions.instance.CanRenderSingle(obj))
                                Graphics.DrawMesh(obj.m_mesh, obj.m_position, obj.m_rotation, obj.m_material, 0, null, 0, null, true, true);

                            if (SingleHoveredObj == obj || (selectedGroup == null ? (obj.group == null ? false : obj.group.root == SingleHoveredObj) : false))
                                Graphics.DrawMesh(obj.overlayRenderMesh, obj.m_position, obj.m_rotation, spriteMat, 0, null, 0, null, true, true);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError("[ProceduralObjects] Error while rendering object " + obj.id.ToString() + " (" + obj.basePrefabName + " of type " + obj.baseInfoType
                                + " : " + e.Message + " - Stack Trace : " + e.StackTrace + " Sent to DrawMesh : " + (obj.m_mesh == null).ToString() + "," +
                                (obj.m_position).ToString() + "," +
                                (obj.m_rotation).ToString() + "," +
                                (obj.m_material == null).ToString() + "," +
                                (renderCamera == null).ToString());
                        }
                    }
                }

                if (moduleManager.enabledModules.Count > 0)
                {
                    bool simPaused = Singleton<SimulationManager>.instance.SimulationPaused;
                    for (int i = 0; i < moduleManager.enabledModules.Count; i++)
                    {
                        var m = moduleManager.enabledModules[i];
                        if (m.ModuleType.hide_all)
                            continue;
                        bool layerVisible = m.parentObject.layer == null;
                        if (!layerVisible)
                            layerVisible = !m.parentObject.layer.m_isHidden;
                        try { m.UpdateModule(this, simPaused, layerVisible); }
                        catch (Exception e) { Debug.LogError("[ProceduralObjects] Error inside module UpdateModule() method!\n" + e); }
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
                if (clipboard != null)
                {
                    if (KeyBindingsManager.instance.GetBindingFromName("paste").GetBindingDown())
                    {
                        Paste(clipboard);
                        SingleHoveredObj = null;
                    }
                }
                //  if (!proceduralTool && chosenProceduralInfo != null)
                //      GUIUtils.SetMouseScrolling(!(new Rect(window.x + 10, window.y + 60, 350, 330).IsMouseInside()));
                if (!proceduralTool && chosenProceduralInfo == null && pObjSelection != null)
                {
                    filters.Update();
                    ProceduralUtils.UpdateObjectsSelectedState(pObjSelection);
                    if (pObjSelection.Count > 0)
                    {
                        if (selectionModeAction == null)
                        {
                            if (KeyBindingsManager.instance.GetBindingFromName("deleteObject").GetBindingDown())
                            {
                                var inclusiveList = (selectedGroup == null) ? POGroup.AllObjectsInSelection(pObjSelection, selectedGroup) : pObjSelection;
                                YieldConfirmDeletePanel(inclusiveList.Count, inclusiveList[0].m_position, delegate()
                                {
                                    for (int i = 0; i < inclusiveList.Count; i++)
                                    {
                                        var obj = inclusiveList[i];
                                        if (obj.group != null)
                                            obj.group.Remove(this, obj);
                                        moduleManager.DeleteAllModules(obj);
                                        proceduralObjects.Remove(obj);
                                        activeIds.Remove(obj.id);
                                    }
                                    pObjSelection.Clear();
                                });
                            }
                        }
                        if (KeyBindingsManager.instance.GetBindingFromName("copy").GetBindingDown())
                        {
                            var inclusiveList = POGroup.AllObjectsInSelection(pObjSelection, selectedGroup);
                            if (inclusiveList.Count > 1)
                            {
                                clipboard = new ClipboardProceduralObjects(ClipboardProceduralObjects.ClipboardType.Selection);
                                clipboard.MakeSelectionList(inclusiveList, selectedGroup);
                            }
                            else
                            {
                                clipboard = new ClipboardProceduralObjects(ClipboardProceduralObjects.ClipboardType.Single);
                                clipboard.single_object = new CacheProceduralObject(inclusiveList[0]);
                            }
                            storedHeight = inclusiveList[0].m_position.y;
                        }
                    }

                    if (selectionModeAction != null)
                        selectionModeAction.OnUpdate();

                    else if (!selectingNewGrpRoot)
                    {
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
                            List<ProceduralObject> objects = (selectedGroup == null) ? proceduralObjects : selectedGroup.objects;
                            foreach (var obj in objects)
                            {
                                if (!obj._insideUIview)
                                    continue;
                                if (selectedGroup == null)
                                {
                                    if (obj.group != null && !obj.isRootOfGroup)
                                        continue;
                                }
                                if (obj.layer != null)
                                {
                                    if (ProceduralObjectsMod.HideDisabledLayersIcon.value && obj.layer.m_isHidden)
                                        continue;
                                }
                                if (!filters.FiltersAllow(obj))
                                    continue;
                                if (pObjSelection.Contains(obj))
                                    continue;
                                var screenPos = obj.m_position.WorldToGuiPoint();
                                if (!new Rect(0, 0, Screen.width, Screen.height).Contains(screenPos))
                                    continue;
                                if (region.Contains(screenPos, true))
                                {
                                    if (!IsInWindowElement(screenPos))
                                        pObjSelection.Add(obj);
                                }
                            }
                        }
                    }
                }
                GUIUtils.SetMouseScroll(!IsInWindowElement(GUIUtils.MousePos));
            }
            else
            {
                GUIUtils.SetMouseScroll(true);
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
                    if (Input.GetMouseButtonDown(0))
                    {
                        if (!new Rect(window.position, new Vector2(370, 230)).IsMouseInside())
                            ConfirmMovingWhole(getBackToGeneralTool);
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
                                    // float diff = (rotWizardData.GUIMousePositionX - Input.mousePosition.x);
                                    currentlyEditingObject.m_rotation = Quaternion.AngleAxis(((rotWizardData.GUIMousePositionX - Input.mousePosition.x) * 400f) / Screen.width, Vector3.up) * currentlyEditingObject.m_rotation;
                                    rotWizardData.UpdateMouseCoords();
                                }
                            }
                        }
                        else if (Input.GetMouseButtonUp(1))
                        {
                            if (rotWizardData.clickTime <= .14f)
                            {
                                currentlyEditingObject.m_rotation = Quaternion.AngleAxis(45, Vector3.up) * currentlyEditingObject.m_rotation;
                            }
                            rotWizardData = null;
                        }

                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveUp").GetBinding())
                            yOffset += TimeUtils.deltaTime * (KeyBindingsManager.instance.GetBindingFromName("edition_smallMovements").GetBinding() ? 1f : 8f);
                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveDown").GetBinding())
                            yOffset -= TimeUtils.deltaTime * (KeyBindingsManager.instance.GetBindingFromName("edition_smallMovements").GetBinding() ? 1f : 8f);
                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveForward").GetBinding())
                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(20f * TimeUtils.deltaTime, 0, 0);
                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveBackward").GetBinding())
                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(-20f * TimeUtils.deltaTime, 0, 0);
                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveLeft").GetBinding())
                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, 0, 20f * TimeUtils.deltaTime);
                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveRight").GetBinding())
                            currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, 0, -20f * TimeUtils.deltaTime);


                        var ray = renderCamera.ScreenPointToRay(Input.mousePosition);
                        ProceduralTool.RaycastInput rayInput = new ProceduralTool.RaycastInput(ray, renderCamera.farClipPlane);
                        if (KeyBindingsManager.instance.GetBindingFromName("enableSnapping").GetBinding())
                        {
                            // BloodyPenguin's code
                            // from Prop Snapping tool
                            // https://github.com/bloodypenguin/Skylines-PropSnapping/blob/master/PropSnapping/Detour/PropToolDetour.cs#L65
                            rayInput.m_ignoreBuildingFlags = Building.Flags.None;
                            rayInput.m_ignoreNodeFlags = NetNode.Flags.None;
                            rayInput.m_ignoreSegmentFlags = NetSegment.Flags.None;
                            rayInput.m_buildingService = new ProceduralTool.RaycastService(ItemClass.Service.None, ItemClass.SubService.None, ItemClass.Layer.Default);
                            rayInput.m_netService = new ProceduralTool.RaycastService(ItemClass.Service.None, ItemClass.SubService.None, ItemClass.Layer.Default);
                            rayInput.m_netService2 = new ProceduralTool.RaycastService(ItemClass.Service.None, ItemClass.SubService.None, ItemClass.Layer.Default);
                        }
                        try
                        {
                            if (KeyBindingsManager.instance.GetBindingFromName("snapStoredHeight").GetBinding() && storedHeight != 0)
                            {
                                Plane p = new Plane(Vector3.up, new Vector3(0, storedHeight, 0));
                                float enter;
                                if (p.Raycast(ray, out enter))
                                    currentlyEditingObject.m_position = ray.GetPoint(enter);
                            }
                            else
                            {
                                ProceduralTool.RaycastOutput rayOutput;
                                if (ProceduralTool.TerrainRaycast(rayInput, out rayOutput))
                                {
                                    if (!rayOutput.m_currentEditObject)
                                    {
                                        movingWholeRaycast = rayOutput.m_hitPos;
                                        currentlyEditingObject.m_position = new Vector3(rayOutput.m_hitPos.x, rayOutput.m_hitPos.y + yOffset, rayOutput.m_hitPos.z);
                                    }
                                }
                            }
                        }
                        catch { }
                        if (placingSelection && currentlyEditingObject.tempObj != null && moveToSelection != null)
                        {
                            currentlyEditingObject.tempObj.transform.position = currentlyEditingObject.m_position;
                            currentlyEditingObject.tempObj.transform.rotation = currentlyEditingObject.m_rotation;
                            foreach (var kvp in moveToSelection)
                            {
                                kvp.Key.m_position = kvp.Value.position;
                                kvp.Key.m_rotation = kvp.Value.rotation;
                            }
                        }
                    }
                }
                else
                {
                    if (Input.GetMouseButtonUp(0) || (Gizmos.registeredFloat != 0 && (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return))))
                    {
                        if (axisState != AxisEditionState.none)
                        {
                            axisHitPoint = Vector3.zero;
                            gizmoOffset = Vector3.zero;
                            Gizmos.initialRotationTemp = Quaternion.identity;
                            if (actionMode == 0)
                            {
                                if (Input.GetKey(KeyCode.LeftControl) || Gizmos.useLineTool)
                                {
                                    currentlyEditingObject.historyEditionBuffer.axisUsed = axisState;
                                    var obj = CloneObject(currentlyEditingObject);
                                    obj.m_position = currentlyEditingObject.historyEditionBuffer.prevTempPos;
                                    if (selectedGroup != null)
                                        selectedGroup.AddToGroup(obj);

                                    if (Gizmos.useLineTool)
                                    {
                                        if (Vector3.Angle(currentlyEditingObject.m_position - currentlyEditingObject.historyEditionBuffer.prevTempPos, Gizmos.posDiffSaved)
                                            < 5)
                                        {
                                            float currentDist = Vector3.Distance(currentlyEditingObject.m_position, currentlyEditingObject.historyEditionBuffer.prevTempPos);
                                            float savedDist = Gizmos.posDiffSaved.magnitude;
                                            int draw = Mathf.FloorToInt(currentDist / savedDist);
                                            if ((currentDist / savedDist) - (float)draw > 0.9f)
                                                draw += 1;
                                            Vector3 drawpos = currentlyEditingObject.historyEditionBuffer.prevTempPos + Gizmos.posDiffSaved;
                                            for (int i = 1; i < draw; i++)
                                            {
                                                var lineobj = CloneObject(currentlyEditingObject);
                                                lineobj.m_position = drawpos;
                                                drawpos += Gizmos.posDiffSaved;
                                                if (selectedGroup != null)
                                                    selectedGroup.AddToGroup(lineobj);
                                            }
                                        }
                                        Gizmos.useLineTool = false;
                                        Gizmos.posDiffSaved = Vector3.zero;
                                    }
                                }
                            }
                            else if (actionMode == 1)
                            {
                                switch (axisState)
                                {
                                    case AxisEditionState.X:
                                        currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.stretchX, Gizmos.recordingStretch);
                                        break;
                                    case AxisEditionState.Y:
                                        currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.stretchY, Gizmos.recordingStretch);
                                        break;
                                    case AxisEditionState.Z:
                                        currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.stretchZ, Gizmos.recordingStretch);
                                        break;
                                }
                            }
                            Gizmos.ReleaseAxis();
                            currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(tempVerticesBuffer);
                            Gizmos.recordingStretch = 0f;
                            axisState = AxisEditionState.none;
                        }
                    }
                    if (axisState == AxisEditionState.none)
                    {
                        Vector2 objGuiPosition = currentlyEditingObject.m_position.WorldToGuiPoint();
                        Rect toolsRect = new Rect(Gizmos.RightMostGUIPosGizmo + new Vector2(0, 30), new Vector2(110, 85));
                        if (Input.GetMouseButtonDown(0))
                        {
                            if (!toolsRect.IsMouseInside() && !IsInWindowElement(GUIUtils.MousePos))
                            {
                                RaycastHit hit;
                                Ray ray = renderCamera.ScreenPointToRay(Input.mousePosition);
                                if (Physics.Raycast(ray, out hit))
                                {
                                    if (hit.transform.gameObject.name.Contains("ProceduralAxis_"))
                                    {
                                        PlaySound(3);
                                        EditingStep.StepType type = EditingStep.StepType.none;
                                        if (actionMode == 0)
                                            type = EditingStep.StepType.position;
                                        else if (actionMode == 2)
                                            type = EditingStep.StepType.rotation;
                                        currentlyEditingObject.historyEditionBuffer.InitializeNewStep(type, tempVerticesBuffer);
                                        if (hit.transform.gameObject.name.Contains("ProceduralAxis_X"))
                                        {
                                            axisState = AxisEditionState.X;
                                            Plane p = Gizmos.CollisionPlane(actionMode, AxisEditionState.X, currentlyEditingObject.m_position, currentlyEditingObject.m_rotation, renderCamera);
                                            float enter;
                                            if (p.Raycast(ray, out enter))
                                                axisHitPoint = ray.GetPoint(enter);
                                            else
                                                axisHitPoint = hit.point;
                                        }
                                        else if (hit.transform.gameObject.name.Contains("ProceduralAxis_Y"))
                                        {
                                            axisState = AxisEditionState.Y;
                                            Plane p = Gizmos.CollisionPlane(actionMode, AxisEditionState.Y, currentlyEditingObject.m_position, currentlyEditingObject.m_rotation, renderCamera);
                                            float enter;
                                            if (p.Raycast(ray, out enter))
                                                axisHitPoint = ray.GetPoint(enter);
                                            else
                                                axisHitPoint = hit.point;
                                        }
                                        else if (hit.transform.gameObject.name.Contains("ProceduralAxis_Z"))
                                        {
                                            axisState = AxisEditionState.Z;
                                            Plane p = Gizmos.CollisionPlane(actionMode, AxisEditionState.Z, currentlyEditingObject.m_position, currentlyEditingObject.m_rotation, renderCamera);
                                            float enter;
                                            if (p.Raycast(ray, out enter))
                                                axisHitPoint = ray.GetPoint(enter);
                                            else
                                                axisHitPoint = hit.point;
                                        }

                                        if (actionMode == 1)
                                            Gizmos.tempBuffer = tempVerticesBuffer.GetPositionsArray();
                                        else if (actionMode == 2)
                                            Gizmos.initialRotationTemp = currentlyEditingObject.m_rotation;

                                        if (Gizmos.referential == Gizmos.SpaceReferential.Local || actionMode == 1)
                                        {
                                            if (axisState == AxisEditionState.X)
                                                gizmoOffset = Vector3.Project(axisHitPoint - currentlyEditingObject.m_position, currentlyEditingObject.m_rotation * Vector3.right);
                                            else if (axisState == AxisEditionState.Y)
                                                gizmoOffset = Vector3.Project(axisHitPoint - currentlyEditingObject.m_position, currentlyEditingObject.m_rotation * Vector3.up);
                                            else if (axisState == AxisEditionState.Z)
                                                gizmoOffset = Vector3.Project(axisHitPoint - currentlyEditingObject.m_position, currentlyEditingObject.m_rotation * Vector3.forward);
                                        }
                                        else
                                            gizmoOffset = axisHitPoint - currentlyEditingObject.m_position;
                                        Gizmos.ClickAxis(axisState);
                                    }
                                }
                            }
                        }
                    }

                    if (editingWholeModel)
                    {
                        if (KeyBindingsManager.instance.GetBindingFromName("switchGeneralToVertexTools").GetBindingDown())
                        {
                            PlaySound(2);
                            if (actionMode == 2)
                            {
                                Gizmos.DestroyGizmo();
                                verticesToolType = 0; // move
                                editingWholeModel = false;
                                toolAction = ToolAction.vertices;
                                tabSwitchTimer = TimeUtils.deltaTime;
                            }
                            else if (actionMode == 0)
                            {
                                actionMode = 1;
                                Gizmos.CreateScaleGizmo(currentlyEditingObject.m_position, true);
                            }
                            else if (actionMode == 1)
                            {
                                actionMode = 2;
                                Gizmos.CreateRotationGizmo(currentlyEditingObject.m_position, true);
                            }
                        }
                        #region Gizmo movement - WHOLE MODEL
                        if (axisState != AxisEditionState.none)
                        {
                            if (Input.GetMouseButton(0))
                            {
                                if (axisHitPoint != Vector3.zero)
                                {
                                    Ray r = renderCamera.ScreenPointToRay(Input.mousePosition);
                                    float enter = 0f;
                                    Plane p = Gizmos.CollisionPlane(actionMode, axisState, axisHitPoint, currentlyEditingObject.m_rotation, renderCamera);

                                    if (actionMode == 0) // position gizmo
                                    {
                                        switch (axisState)
                                        {
                                            case AxisEditionState.X:
                                                if (p.Raycast(r, out enter))
                                                {
                                                    Vector3 hit = r.GetPoint(enter);
                                                    if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                    {
                                                        if (Gizmos.registeredFloat != 0)
                                                            currentlyEditingObject.m_position = currentlyEditingObject.historyEditionBuffer.prevTempPos + new Vector3(Gizmos.GetStoredDistanceValue, 0, 0);
                                                        else
                                                            currentlyEditingObject.m_position = Gizmos.SnapToPreviousMove(new Vector3(hit.x - gizmoOffset.x, currentlyEditingObject.m_position.y, currentlyEditingObject.m_position.z),
                                                                AxisEditionState.X, currentlyEditingObject);
                                                    }
                                                    else
                                                    {
                                                        if (Gizmos.registeredFloat != 0)
                                                            currentlyEditingObject.m_position = currentlyEditingObject.historyEditionBuffer.prevTempPos + (currentlyEditingObject.m_rotation * new Vector3(Gizmos.GetStoredDistanceValue, 0, 0));
                                                        else
                                                            currentlyEditingObject.m_position = Gizmos.SnapToPreviousMove(Vector3.Project(hit - currentlyEditingObject.m_position, currentlyEditingObject.m_rotation * Vector3.right) + currentlyEditingObject.m_position - gizmoOffset,
                                                                AxisEditionState.X, currentlyEditingObject);
                                                    }
                                                }
                                                break;
                                            case AxisEditionState.Y:
                                                if (p.Raycast(r, out enter))
                                                {
                                                    Vector3 hit = r.GetPoint(enter);
                                                    if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                    {
                                                        if (Gizmos.registeredFloat != 0)
                                                            currentlyEditingObject.m_position = currentlyEditingObject.historyEditionBuffer.prevTempPos + new Vector3(0, Gizmos.GetStoredDistanceValue, 0);
                                                        else
                                                            currentlyEditingObject.m_position = Gizmos.SnapToPreviousMove(new Vector3(currentlyEditingObject.m_position.x, hit.y - gizmoOffset.y, currentlyEditingObject.m_position.z),
                                                                AxisEditionState.Y, currentlyEditingObject);
                                                    }
                                                    else
                                                    {
                                                        if (Gizmos.registeredFloat != 0)
                                                            currentlyEditingObject.m_position = currentlyEditingObject.historyEditionBuffer.prevTempPos + (currentlyEditingObject.m_rotation * new Vector3(0, Gizmos.GetStoredDistanceValue, 0));
                                                        else
                                                            currentlyEditingObject.m_position = Gizmos.SnapToPreviousMove(Vector3.Project(hit - currentlyEditingObject.m_position, currentlyEditingObject.m_rotation * Vector3.up) + currentlyEditingObject.m_position - gizmoOffset,
                                                                AxisEditionState.Y, currentlyEditingObject);
                                                    }
                                                }
                                                break;
                                            case AxisEditionState.Z:
                                                if (p.Raycast(r, out enter))
                                                {
                                                    Vector3 hit = r.GetPoint(enter);
                                                    if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                    {
                                                        if (Gizmos.registeredFloat != 0)
                                                            currentlyEditingObject.m_position = currentlyEditingObject.historyEditionBuffer.prevTempPos + new Vector3(0, 0, Gizmos.GetStoredDistanceValue);
                                                        else
                                                            currentlyEditingObject.m_position = Gizmos.SnapToPreviousMove(new Vector3(currentlyEditingObject.m_position.x, currentlyEditingObject.m_position.y, hit.z - gizmoOffset.z),
                                                                AxisEditionState.Z, currentlyEditingObject);
                                                    }
                                                    else
                                                    {
                                                        if (Gizmos.registeredFloat != 0)
                                                            currentlyEditingObject.m_position = currentlyEditingObject.historyEditionBuffer.prevTempPos + (currentlyEditingObject.m_rotation * new Vector3(0, 0, Gizmos.GetStoredDistanceValue));
                                                        else
                                                            currentlyEditingObject.m_position = Gizmos.SnapToPreviousMove(Vector3.Project(hit - currentlyEditingObject.m_position, currentlyEditingObject.m_rotation * Vector3.forward) + currentlyEditingObject.m_position - gizmoOffset,
                                                                AxisEditionState.Z, currentlyEditingObject);
                                                    }
                                                }
                                                break;
                                        }
                                        // HOLD Ctrl TO COPY STUFF + LINE COPY
                                        if (Input.GetKey(KeyCode.LeftControl) || Gizmos.useLineTool)
                                        {
                                            try
                                            {
                                                Graphics.DrawMesh(currentlyEditingObject.m_mesh, currentlyEditingObject.historyEditionBuffer.prevTempPos,
                                                    currentlyEditingObject.m_rotation, currentlyEditingObject.m_material, 0, renderCamera, 0, null, true, true);
                                            }
                                            catch { }
                                            if (Input.GetKeyDown(KeyCode.LeftShift))
                                            {
                                                if (Gizmos.useLineTool)
                                                {
                                                    Gizmos.useLineTool = false;
                                                    Gizmos.posDiffSaved = Vector3.zero;
                                                }
                                                else
                                                {
                                                    Gizmos.posDiffSaved = currentlyEditingObject.m_position - currentlyEditingObject.historyEditionBuffer.prevTempPos;
                                                    Gizmos.useLineTool = true;
                                                }
                                            }
                                            if (Gizmos.useLineTool)
                                            {
                                                if (Vector3.Angle(currentlyEditingObject.m_position - currentlyEditingObject.historyEditionBuffer.prevTempPos, Gizmos.posDiffSaved)
                                                    < 10)
                                                {
                                                    float currentDist = Vector3.Distance(currentlyEditingObject.m_position, currentlyEditingObject.historyEditionBuffer.prevTempPos);
                                                    float savedDist = Gizmos.posDiffSaved.magnitude;
                                                    int draw = Mathf.FloorToInt(currentDist / savedDist);
                                                    Vector3 drawpos = currentlyEditingObject.historyEditionBuffer.prevTempPos + Gizmos.posDiffSaved;
                                                    for (int i = 1; i < draw; i++)
                                                    {
                                                        try
                                                        {
                                                            Graphics.DrawMesh(currentlyEditingObject.m_mesh, drawpos,
                                                                currentlyEditingObject.m_rotation, currentlyEditingObject.m_material, 0, renderCamera, 0, null, true, true);
                                                        }
                                                        catch { }
                                                        drawpos += Gizmos.posDiffSaved;
                                                    }
                                                    currentlyEditingObject.m_position = drawpos;
                                                }
                                                Gizmos.DetectRotationKeyboard();
                                            }
                                        }
                                    }
                                    else if (actionMode == 1) // scale gizmo - Referential has no effect
                                    {
                                        switch (axisState)
                                        {
                                            case AxisEditionState.X:
                                                if (p.Raycast(r, out enter))
                                                {
                                                    float stretch;
                                                    if (Gizmos.registeredFloat != 0)
                                                        stretch = Gizmos.registeredFloat;
                                                    else
                                                    {
                                                        Vector3 hit = r.GetPoint(enter);
                                                        stretch = Vector3.Project(hit - currentlyEditingObject.m_position, currentlyEditingObject.m_rotation * Vector3.right).magnitude / gizmoOffset.magnitude;
                                                    }
                                                    Gizmos.recordingStretch = stretch;
                                                    VertexUtils.StretchX(Gizmos.tempBuffer, tempVerticesBuffer, stretch);
                                                    Apply();
                                                }
                                                break;
                                            case AxisEditionState.Y:
                                                if (p.Raycast(r, out enter))
                                                {
                                                    float stretch;
                                                    if (Gizmos.registeredFloat != 0)
                                                        stretch = Gizmos.registeredFloat;
                                                    else
                                                    {
                                                        Vector3 hit = r.GetPoint(enter);
                                                        stretch = Vector3.Project(hit - currentlyEditingObject.m_position, currentlyEditingObject.m_rotation * Vector3.up).magnitude / gizmoOffset.magnitude;
                                                    }
                                                    Gizmos.recordingStretch = stretch;
                                                    VertexUtils.StretchY(Gizmos.tempBuffer, tempVerticesBuffer, stretch);
                                                    Apply();
                                                }
                                                break;
                                            case AxisEditionState.Z:
                                                if (p.Raycast(r, out enter))
                                                {
                                                    float stretch;
                                                    if (Gizmos.registeredFloat != 0)
                                                        stretch = Gizmos.registeredFloat;
                                                    else
                                                    {
                                                        Vector3 hit = r.GetPoint(enter);
                                                        stretch = Vector3.Project(hit - currentlyEditingObject.m_position, currentlyEditingObject.m_rotation * Vector3.forward).magnitude / gizmoOffset.magnitude;
                                                    }
                                                    Gizmos.recordingStretch = stretch;
                                                    VertexUtils.StretchZ(Gizmos.tempBuffer, tempVerticesBuffer, stretch);
                                                    Apply();
                                                }
                                                break;
                                        }
                                    }
                                    else if (actionMode == 2) // rotation gizmo
                                    {
                                        switch (axisState)
                                        {
                                            case AxisEditionState.X:
                                                if (p.Raycast(r, out enter))
                                                {
                                                    float angle;
                                                    if (Gizmos.registeredFloat != 0)
                                                        angle = Gizmos.GetStoredAngleValue;
                                                    else
                                                    {
                                                        Vector3 hit = r.GetPoint(enter);
                                                        angle = Vector3.Angle(axisHitPoint - currentlyEditingObject.m_position, hit - currentlyEditingObject.m_position);
                                                        if (Vector3.Cross(axisHitPoint - currentlyEditingObject.m_position, hit - currentlyEditingObject.m_position).y < 0)
                                                            angle = -angle;
                                                    }
                                                    if (Gizmos.referential == Gizmos.SpaceReferential.Local)
                                                    {
                                                        if (Gizmos.registeredFloat == 0)
                                                        {
                                                            if (Vector3.Dot(currentlyEditingObject.m_rotation * Vector3.up, Vector3.up) < 0)
                                                                angle = -angle;
                                                        }
                                                        currentlyEditingObject.m_rotation = Gizmos.initialRotationTemp * Quaternion.Euler(Vector3.up * angle);
                                                    }
                                                    else
                                                        currentlyEditingObject.m_rotation = Quaternion.Euler(Vector3.up * angle) * Gizmos.initialRotationTemp;
                                                }
                                                break;
                                            case AxisEditionState.Y:
                                                if (p.Raycast(r, out enter))
                                                {
                                                    float angle;
                                                    if (Gizmos.registeredFloat != 0)
                                                        angle = Gizmos.GetStoredAngleValue;
                                                    else
                                                    {
                                                        Vector3 hit = r.GetPoint(enter);
                                                        angle = Vector3.Angle(axisHitPoint - currentlyEditingObject.m_position, hit - currentlyEditingObject.m_position);
                                                        if (Vector3.Cross(axisHitPoint - currentlyEditingObject.m_position, hit - currentlyEditingObject.m_position).z < 0)
                                                            angle = -angle;
                                                    }
                                                    if (Gizmos.referential == Gizmos.SpaceReferential.Local)
                                                    {
                                                        if (Gizmos.registeredFloat == 0)
                                                        {
                                                            if (Vector3.Dot(currentlyEditingObject.m_rotation * Vector3.forward, Vector3.forward) < 0)
                                                                angle = -angle;
                                                        }
                                                        currentlyEditingObject.m_rotation = Gizmos.initialRotationTemp * Quaternion.Euler(Vector3.forward * angle);
                                                    }
                                                    else
                                                        currentlyEditingObject.m_rotation = Quaternion.Euler(Vector3.forward * angle) * Gizmos.initialRotationTemp;
                                                }
                                                break;
                                            case AxisEditionState.Z:
                                                if (p.Raycast(r, out enter))
                                                {
                                                    float angle;
                                                    if (Gizmos.registeredFloat != 0)
                                                        angle = Gizmos.GetStoredAngleValue;
                                                    else
                                                    {
                                                        Vector3 hit = r.GetPoint(enter);
                                                        angle = Vector3.Angle(axisHitPoint - currentlyEditingObject.m_position, hit - currentlyEditingObject.m_position);
                                                        if (Vector3.Cross(axisHitPoint - currentlyEditingObject.m_position, hit - currentlyEditingObject.m_position).x < 0)
                                                            angle = -angle;
                                                    }
                                                    if (Gizmos.referential == Gizmos.SpaceReferential.Local)
                                                    {
                                                        if (Gizmos.registeredFloat == 0)
                                                        {
                                                            if (Vector3.Dot(currentlyEditingObject.m_rotation * Vector3.right, Vector3.right) < 0)
                                                                angle = -angle;
                                                        }
                                                        currentlyEditingObject.m_rotation = Gizmos.initialRotationTemp * Quaternion.Euler(Vector3.right * angle);
                                                    }
                                                    else
                                                        currentlyEditingObject.m_rotation = Quaternion.Euler(Vector3.right * angle) * Gizmos.initialRotationTemp;
                                                }
                                                break;
                                        }
                                    }
                                }
                            }
                        }
                        if (Gizmos.Exists)
                        {
                            Gizmos.Update(actionMode, Vector3.Distance(renderCamera.transform.position, currentlyEditingObject.m_position), currentlyEditingObject.m_position, currentlyEditingObject.m_rotation, renderCamera);
                        }
                        #endregion
                    }
                    else
                    {
                        if (KeyBindingsManager.instance.GetBindingFromName("switchGeneralToVertexTools").GetBindingDown())
                        {
                            PlaySound(2);
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
                        if (Input.GetMouseButtonDown(0))
                        {
                            if (editingVertexIndex.Count > 0 && vertWizardData == null)
                            {
                                if (!IsInWindowElement(GUIUtils.MousePos))
                                {
                                    vertWizardData = new VerticesWizardData(verticesToolType);
                                    vertWizardData.Store(GUIUtils.MousePos);
                                }
                            }
                        }
                        else if (Input.GetMouseButtonUp(0) || (Gizmos.registeredString != "" && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))))
                        {
                            if (vertWizardData != null)
                            {
                                currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(tempVerticesBuffer);
                                Gizmos.DisableKeyTyping();
                                vertWizardData.DestroyLines();
                                vertWizardData = null;
                            }
                        }
                        else if (Input.GetMouseButton(0))
                        {
                            if (editingVertexIndex.Count > 0)
                            {
                                if (vertWizardData != null)
                                {
                                    vertWizardData.IncrementStep();
                                    if (vertWizardData.enableMovement)
                                    {
                                        if (!vertWizardData.storedVertices)
                                        {
                                            currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, tempVerticesBuffer);
                                            if (verticesToolType != 0)
                                                Gizmos.EnableKeyTyping();
                                            vertWizardData.Store(tempVerticesBuffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))).ToArray(), currentlyEditingObject);
                                        }
                                        if (vertWizardData.toolType == 0)
                                        {
                                            if (Input.GetKeyDown(KeyCode.LeftControl))
                                                vertWizardData.HideLines();
                                            else if (Input.GetKeyUp(KeyCode.LeftControl))
                                                vertWizardData.ShowLines();

                                            Ray ray = renderCamera.ScreenPointToRay(Input.mousePosition);
                                            float enter;
                                            if (vertWizardData.movementPlane.Raycast(ray, out enter))
                                            {
                                                var point = ray.GetPoint(enter);
                                                vertWizardData.ApplyToNewPosition(point, currentlyEditingObject, !Input.GetKey(KeyCode.LeftControl));
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
                        else if (Input.GetMouseButton(1))
                        {
                            if (selectionModeAction == null)
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
                        }
                        else if (Input.GetMouseButtonUp(1))
                        {
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
                                    if (region.Contains(ProceduralUtils.VertexWorldPosition(vertex, currentlyEditingObject).WorldToGuiPoint(), true))
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
                            else if (editingWholeModel && axisState == AxisEditionState.none)
                            {
                                #region déplacement du modèle
                                switch (actionMode)
                                {
                                    case 0:
                                        // POSITION

                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveUp").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_position += new Vector3(0, 8f * TimeUtils.deltaTime, 0);
                                            else
                                                currentlyEditingObject.m_position += currentlyEditingObject.m_rotation * new Vector3(0, 8f * TimeUtils.deltaTime, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveDown").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_position += new Vector3(0, -8f * TimeUtils.deltaTime, 0);
                                            else
                                                currentlyEditingObject.m_position += currentlyEditingObject.m_rotation * new Vector3(0, -8f * TimeUtils.deltaTime, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveLeft").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_position += new Vector3(0, 0, 8f * TimeUtils.deltaTime);
                                            else
                                                currentlyEditingObject.m_position += currentlyEditingObject.m_rotation * new Vector3(0, 0, 8f * TimeUtils.deltaTime);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveRight").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_position += new Vector3(0, 0, -8f * TimeUtils.deltaTime);
                                            else
                                                currentlyEditingObject.m_position += currentlyEditingObject.m_rotation * new Vector3(0, 0, -8f * TimeUtils.deltaTime);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveForward").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_position += new Vector3(8f * TimeUtils.deltaTime, 0, 0);
                                            else
                                                currentlyEditingObject.m_position += currentlyEditingObject.m_rotation * new Vector3(8f * TimeUtils.deltaTime, 0, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveBackward").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_position += new Vector3(-8f * TimeUtils.deltaTime, 0, 0);
                                            else
                                                currentlyEditingObject.m_position += currentlyEditingObject.m_rotation * new Vector3(-8f * TimeUtils.deltaTime, 0, 0);
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
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_rotation = Quaternion.Euler(20f * TimeUtils.deltaTime, 0, 0) * currentlyEditingObject.m_rotation;
                                            else
                                                currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(20f * TimeUtils.deltaTime, 0, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveDown").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_rotation = Quaternion.Euler(-20f * TimeUtils.deltaTime, 0, 0) * currentlyEditingObject.m_rotation;
                                            else
                                                currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(-20f * TimeUtils.deltaTime, 0, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveLeft").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_rotation = Quaternion.Euler(0, 20f * TimeUtils.deltaTime, 0) * currentlyEditingObject.m_rotation;
                                            else
                                                currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, 20f * TimeUtils.deltaTime, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveRight").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_rotation = Quaternion.Euler(0, -20f * TimeUtils.deltaTime, 0) * currentlyEditingObject.m_rotation;
                                            else
                                                currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, -20f * TimeUtils.deltaTime, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveForward").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_rotation = Quaternion.Euler(0, 0, 20f * TimeUtils.deltaTime) * currentlyEditingObject.m_rotation;
                                            else
                                                currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, 0, 20f * TimeUtils.deltaTime);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveBackward").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_rotation = Quaternion.Euler(0, 0, -20f * TimeUtils.deltaTime) * currentlyEditingObject.m_rotation;
                                            else
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
                                        tempVerticesBuffer[v.Index].Position.y += TimeUtils.deltaTime;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveDown").GetBinding())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, tempVerticesBuffer);
                                    foreach (Vertex v in tempVerticesBuffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        tempVerticesBuffer[v.Index].Position.y -= TimeUtils.deltaTime;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveLeft").GetBinding())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, tempVerticesBuffer);
                                    foreach (Vertex v in tempVerticesBuffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        tempVerticesBuffer[v.Index].Position.z += TimeUtils.deltaTime;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveRight").GetBinding())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, tempVerticesBuffer);
                                    foreach (Vertex v in tempVerticesBuffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        tempVerticesBuffer[v.Index].Position.z -= TimeUtils.deltaTime;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveForward").GetBinding())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, tempVerticesBuffer);
                                    foreach (Vertex v in tempVerticesBuffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        tempVerticesBuffer[v.Index].Position.x += TimeUtils.deltaTime;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveBackward").GetBinding())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, tempVerticesBuffer);
                                    foreach (Vertex v in tempVerticesBuffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        tempVerticesBuffer[v.Index].Position.x -= TimeUtils.deltaTime;
                                    Apply();
                                }
                                #endregion
                            }
                            else if (editingWholeModel && axisState == AxisEditionState.none)
                            {
                                #region déplacement du modèle
                                switch (actionMode)
                                {
                                    case 0:
                                        // POSITION

                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveUp").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_position += new Vector3(0, TimeUtils.deltaTime, 0);
                                            else
                                                currentlyEditingObject.m_position += currentlyEditingObject.m_rotation * new Vector3(0, TimeUtils.deltaTime, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveDown").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_position += new Vector3(0, -TimeUtils.deltaTime, 0);
                                            else
                                                currentlyEditingObject.m_position += currentlyEditingObject.m_rotation * new Vector3(0, -TimeUtils.deltaTime, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveLeft").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_position += new Vector3(0, 0, TimeUtils.deltaTime);
                                            else
                                                currentlyEditingObject.m_position += currentlyEditingObject.m_rotation * new Vector3(0, 0, TimeUtils.deltaTime);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveRight").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_position += new Vector3(0, 0, -TimeUtils.deltaTime);
                                            else
                                                currentlyEditingObject.m_position += currentlyEditingObject.m_rotation * new Vector3(0, 0, -TimeUtils.deltaTime);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveForward").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_position += new Vector3(TimeUtils.deltaTime, 0, 0);
                                            else
                                                currentlyEditingObject.m_position += currentlyEditingObject.m_rotation * new Vector3(TimeUtils.deltaTime, 0, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveBackward").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_position += new Vector3(-TimeUtils.deltaTime, 0, 0);
                                            else
                                                currentlyEditingObject.m_position += currentlyEditingObject.m_rotation * new Vector3(-TimeUtils.deltaTime, 0, 0);
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
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_rotation = Quaternion.Euler(10f * TimeUtils.deltaTime, 0, 0) * currentlyEditingObject.m_rotation;
                                            else
                                                currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(10f * TimeUtils.deltaTime, 0, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveDown").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_rotation = Quaternion.Euler(-10f * TimeUtils.deltaTime, 0, 0) * currentlyEditingObject.m_rotation;
                                            else
                                                currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(-10f * TimeUtils.deltaTime, 0, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveLeft").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_rotation = Quaternion.Euler(0, 10f * TimeUtils.deltaTime, 0) * currentlyEditingObject.m_rotation;
                                            else
                                                currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, 10f * TimeUtils.deltaTime, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveRight").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_rotation = Quaternion.Euler(0, -10f * TimeUtils.deltaTime, 0) * currentlyEditingObject.m_rotation;
                                            else
                                                currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, -10f * TimeUtils.deltaTime, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveForward").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_rotation = Quaternion.Euler(0, 0, 10f * TimeUtils.deltaTime) * currentlyEditingObject.m_rotation;
                                            else
                                                currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, 0, 10f * TimeUtils.deltaTime);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveBackward").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_rotation = Quaternion.Euler(0, 0, -10f * TimeUtils.deltaTime) * currentlyEditingObject.m_rotation;
                                            else
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
                            else if (editingWholeModel && axisState == AxisEditionState.none)
                            {
                                #region déplacement du modèle
                                switch (actionMode)
                                {
                                    case 0:
                                        // position
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveUp").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_position += new Vector3(0, 2f, 0);
                                            else
                                                currentlyEditingObject.m_position += currentlyEditingObject.m_rotation * new Vector3(0, 2f, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveDown").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_position += new Vector3(0, -2f, 0);
                                            else
                                                currentlyEditingObject.m_position += currentlyEditingObject.m_rotation * new Vector3(0, -2f, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveLeft").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_position += new Vector3(0, 0, 2f);
                                            else
                                                currentlyEditingObject.m_position += currentlyEditingObject.m_rotation * new Vector3(0, 0, 2f);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveRight").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_position += new Vector3(0, 0, -2f);
                                            else
                                                currentlyEditingObject.m_position += currentlyEditingObject.m_rotation * new Vector3(0, 0, -2f);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveForward").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_position += new Vector3(2f, 0, 0);
                                            else
                                                currentlyEditingObject.m_position += currentlyEditingObject.m_rotation * new Vector3(2f, 0, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveBackward").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_position += new Vector3(-2f, 0, 0);
                                            else
                                                currentlyEditingObject.m_position += currentlyEditingObject.m_rotation * new Vector3(-2f, 0, 0);
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
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_rotation = Quaternion.Euler(12f, 0, 0) * currentlyEditingObject.m_rotation;
                                            else
                                                currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(12f, 0, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveDown").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_rotation = Quaternion.Euler(-12f, 0, 0) * currentlyEditingObject.m_rotation;
                                            else
                                                currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(-12f, 0, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveLeft").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_rotation = Quaternion.Euler(0, 12f, 0) * currentlyEditingObject.m_rotation;
                                            else
                                                currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, 12f, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveRight").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_rotation = Quaternion.Euler(0, -12f, 0) * currentlyEditingObject.m_rotation;
                                            else
                                                currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, -12f, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveForward").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_rotation = Quaternion.Euler(0, 0, 12f) * currentlyEditingObject.m_rotation;
                                            else
                                                currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, 0, 12f);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveBackward").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_rotation = Quaternion.Euler(0, 0, -12f) * currentlyEditingObject.m_rotation;
                                            else
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
                                        tempVerticesBuffer[v.Index].Position.y += 0.15f;
                                    if (vertWizardData == null)
                                        currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(tempVerticesBuffer);
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveDown").GetBindingDown())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, tempVerticesBuffer);
                                    foreach (Vertex v in tempVerticesBuffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        tempVerticesBuffer[v.Index].Position.y -= 0.15f;
                                    if (vertWizardData == null)
                                        currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(tempVerticesBuffer);
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveLeft").GetBindingDown())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, tempVerticesBuffer);
                                    foreach (Vertex v in tempVerticesBuffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        tempVerticesBuffer[v.Index].Position.z += 0.15f;
                                    if (vertWizardData == null)
                                        currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(tempVerticesBuffer);
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveRight").GetBindingDown())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, tempVerticesBuffer);
                                    foreach (Vertex v in tempVerticesBuffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        tempVerticesBuffer[v.Index].Position.z -= 0.15f;
                                    if (vertWizardData == null)
                                        currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(tempVerticesBuffer);
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveForward").GetBindingDown())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, tempVerticesBuffer);
                                    foreach (Vertex v in tempVerticesBuffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        tempVerticesBuffer[v.Index].Position.x += 0.15f;
                                    if (vertWizardData == null)
                                        currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(tempVerticesBuffer);
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveBackward").GetBindingDown())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, tempVerticesBuffer);
                                    foreach (Vertex v in tempVerticesBuffer.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        tempVerticesBuffer[v.Index].Position.x -= 0.15f;
                                    if (vertWizardData == null)
                                        currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(tempVerticesBuffer);
                                    Apply();
                                }
                                #endregion
                            }
                            else if (editingWholeModel && axisState == AxisEditionState.none)
                            {
                                #region déplacement du modèle
                                switch (actionMode)
                                {
                                    case 0:
                                        // POSITION

                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveUp").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_position += new Vector3(0, 0.25f, 0);
                                            else
                                                currentlyEditingObject.m_position += currentlyEditingObject.m_rotation * new Vector3(0, 0.25f, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveDown").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_position += new Vector3(0, -0.25f, 0);
                                            else
                                                currentlyEditingObject.m_position += currentlyEditingObject.m_rotation * new Vector3(0, -0.25f, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveLeft").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_position += new Vector3(0, 0, 0.25f);
                                            else
                                                currentlyEditingObject.m_position += currentlyEditingObject.m_rotation * new Vector3(0, 0, 0.25f);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveRight").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_position += new Vector3(0, 0, -0.25f);
                                            else
                                                currentlyEditingObject.m_position += currentlyEditingObject.m_rotation * new Vector3(0, 0, -0.25f);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveForward").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_position += new Vector3(0.25f, 0, 0);
                                            else
                                                currentlyEditingObject.m_position += currentlyEditingObject.m_rotation * new Vector3(0.25f, 0, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveBackward").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_position += new Vector3(-0.25f, 0, 0);
                                            else
                                                currentlyEditingObject.m_position += currentlyEditingObject.m_rotation * new Vector3(-0.25f, 0, 0);
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
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_rotation = Quaternion.Euler(5f, 0, 0) * currentlyEditingObject.m_rotation;
                                            else
                                                currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(5f, 0, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveDown").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_rotation = Quaternion.Euler(-5f, 0, 0) * currentlyEditingObject.m_rotation;
                                            else
                                                currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(-5f, 0, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveLeft").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_rotation = Quaternion.Euler(0, 5f, 0) * currentlyEditingObject.m_rotation;
                                            else
                                                currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, 5f, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveRight").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_rotation = Quaternion.Euler(0, -5f, 0) * currentlyEditingObject.m_rotation;
                                            else
                                                currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, -5f, 0);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveForward").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_rotation = Quaternion.Euler(0, 0, 5f) * currentlyEditingObject.m_rotation;
                                            else
                                                currentlyEditingObject.m_rotation = currentlyEditingObject.m_rotation.Rotate(0, 0, 5f);
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveBackward").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.m_rotation = Quaternion.Euler(0, 0, -5f) * currentlyEditingObject.m_rotation;
                                            else
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
            if (ToolsModifierControl.cameraController.m_freeCamera || !generalShowUI)
                return;

            layerManager.DrawWindow();

            TextureManager.instance.DrawWindow();

            FontManager.instance.DrawWindow();

            ModuleManager.instance.DrawWindow();

            RenderOptions.instance.DrawWindow();

            POStatisticsManager.instance.DrawWindow();

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
                    var uiObj = (selectedGroup == null ? proceduralObjects.ToList() : selectedGroup.objects.ToList());
                    foreach (ProceduralObject obj in uiObj)
                    {
                        if (selectedGroup == null)
                        {
                            if (obj.group != null && !obj.isRootOfGroup)
                                continue;
                        }
                        if (ProceduralObjectsMod.HideDisabledLayersIcon.value && obj.layer != null)
                        {
                            if (obj.layer.m_isHidden)
                                continue;
                        }
                        if (!filters.FiltersAllow(obj))
                            continue;

                        var objScreenPos = obj.m_position.WorldToGuiPoint();
                        if (!new Rect(0, 0, Screen.width, Screen.height).Contains(objScreenPos))
                            continue;
                        if (obj._insideUIview)
                        {
                            if (!IsInWindowElement(objScreenPos, true))
                            {
                                if (new Rect(objScreenPos + new Vector2(-15, -15), new Vector2(31, 30)).IsMouseInside())
                                    hoveredObj = obj;
                                if (pObjSelection.Count > 0)
                                {
                                    if (pObjSelection[0] == obj)
                                    {
                                        if (selectionModeAction == null)
                                        {
                                            if (pObjSelection.Count == 1)
                                            {
                                                #region
                                                bool isRoot = obj.group != null && obj.isRootOfGroup;
                                                float addedHeight = 0;
                                                if (isRoot && selectedGroup == null)
                                                {
                                                    if (GUI.Button(new Rect(objScreenPos + new Vector2(12, -11), new Vector2(130, 22)), LocalizationManager.instance.current["enter_group"]))
                                                    {
                                                        PlaySound();
                                                        pObjSelection.Clear();
                                                        selectedGroup = obj.group;
                                                        filters.EnableAll();
                                                    }
                                                    if (GUI.Button(new Rect(objScreenPos + new Vector2(12, 12), new Vector2(130, 22)), LocalizationManager.instance.current["explode_group"]))
                                                    {
                                                        PlaySound();
                                                        pObjSelection = POGroup.ExplodeGroup(this, obj.group);
                                                    }
                                                    addedHeight += 23;
                                                }
                                                else
                                                {
                                                    if (GUI.Button(new Rect(objScreenPos + new Vector2(12, -11), new Vector2(130, 22)), LocalizationManager.instance.current["edit"]))
                                                    {
                                                        PlaySound();
                                                        verticesToolType = 0;
                                                        // toolAction = ToolAction.vertices;
                                                        obj.MakeUniqueMesh();
                                                        SetCurrentlyEditingObj(obj);
                                                        pObjSelection.Clear();
                                                        tempVerticesBuffer = Vertex.CreateVertexList(currentlyEditingObject);
                                                        CloseAllSMWindows();
                                                        proceduralTool = true;
                                                        hoveredObj = null;
                                                        actionMode = 0;
                                                        Gizmos.CreatePositionGizmo(currentlyEditingObject.m_position, true);
                                                        editingVertex = false;
                                                        editingWholeModel = true;
                                                    }
                                                }
                                                if (GUI.Button(new Rect(objScreenPos + new Vector2(12, 12 + addedHeight), new Vector2(130, 22)), LocalizationManager.instance.current["move_to"]))
                                                {
                                                    PlaySound();
                                                    if (isRoot && selectedGroup == null)
                                                    {
                                                        // pObjSelection.Clear();
                                                        MoveSelection(obj.group.objects, true);
                                                        placingSelection = true;
                                                    }
                                                    else
                                                    {
                                                        pObjSelection.Clear();
                                                        SetCurrentlyEditingObj(obj);
                                                        obj.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.moveTo, tempVerticesBuffer);
                                                        placingSelection = false;
                                                    }
                                                    CloseAllSMWindows();
                                                    movingWholeModel = true;
                                                    toolAction = ToolAction.build;
                                                    editingWholeModel = true;
                                                    proceduralTool = true;
                                                    hoveredObj = null;
                                                }
                                                if (GUI.Button(new Rect(objScreenPos + new Vector2(12, 35 + addedHeight), new Vector2(130, 22)), LocalizationManager.instance.current["copy"]))
                                                {
                                                    PlaySound();
                                                    storedHeight = obj.m_position.y;
                                                    if (isRoot)
                                                    {
                                                        clipboard = new ClipboardProceduralObjects(ClipboardProceduralObjects.ClipboardType.Selection);
                                                        clipboard.MakeSelectionList(POGroup.AllObjectsInSelection(pObjSelection, selectedGroup), selectedGroup);
                                                    }
                                                    else
                                                    {
                                                        clipboard = new ClipboardProceduralObjects(ClipboardProceduralObjects.ClipboardType.Single);
                                                        clipboard.single_object = new CacheProceduralObject(obj);
                                                    }
                                                    hoveredObj = null;
                                                }
                                                if (GUI.Button(new Rect(objScreenPos + new Vector2(12, 58 + addedHeight), new Vector2(130, 22)), LocalizationManager.instance.current["layers_set"] + (showLayerSetScroll ? " ►" : "")))
                                                {
                                                    PlaySound();
                                                    // open layer scroll menu
                                                    showLayerSetScroll = !showLayerSetScroll;
                                                    showMoreTools = false;
                                                }
                                                if (GUI.Button(new Rect(objScreenPos + new Vector2(12, 81 + addedHeight), new Vector2(130, 22)), LocalizationManager.instance.current["more"] + (showMoreTools ? " ►" : "")))
                                                {
                                                    PlaySound();
                                                    // align height
                                                    selectingNewGrpRoot = false;
                                                    showMoreTools = !showMoreTools;
                                                    showLayerSetScroll = false;
                                                    /*
                                                    alignHeightObj.Clear();
                                                    alignHeightObj.Add(obj); */
                                                }
                                                if (GUI.Button(new Rect(objScreenPos + new Vector2(12, 104 + addedHeight), new Vector2(130, 22)), LocalizationManager.instance.current["delete"]))
                                                {
                                                    if (isRoot && selectedGroup == null)
                                                    {
                                                        YieldConfirmDeletePanel(obj.group.objects.Count, obj.m_position, delegate()
                                                        {
                                                            POGroup.DeleteGroup(this, obj.group);
                                                            pObjSelection.Clear();
                                                        });
                                                    }
                                                    else
                                                    {
                                                        YieldConfirmDeletePanel(1, obj.m_position, delegate()
                                                        {
                                                            if (obj.group != null)
                                                                obj.group.Remove(this, obj);
                                                            moduleManager.DeleteAllModules(obj);
                                                            proceduralObjects.Remove(obj);
                                                            activeIds.Remove(obj.id);
                                                            pObjSelection.Remove(obj);
                                                            hoveredObj = null;
                                                        });
                                                    }
                                                }
                                                Color c = Color.white;
                                                if (isRoot && selectedGroup == null)
                                                {
                                                    var inclusiveSelection = (selectedGroup == null) ? POGroup.AllObjectsInSelection(pObjSelection, selectedGroup) : pObjSelection;
                                                    for (int i = 0; i < inclusiveSelection.Count; i++)
                                                    {
                                                        if (i == 0)
                                                            c = inclusiveSelection[i].m_color;
                                                        else
                                                        {
                                                            if (inclusiveSelection[i].m_color != c)
                                                            {
                                                                c = Color.white;
                                                                break;
                                                            }
                                                        }
                                                    }
                                                }
                                                else
                                                    c = obj.m_color;

                                                painter = GUIPainter.DrawPainter(painter, objScreenPos + new Vector2(12, 127 + addedHeight), objScreenPos + new Vector2(12, 150 + addedHeight), c,
                                                    (color) =>
                                                    {
                                                        var inclusiveSelection = (selectedGroup == null) ? POGroup.AllObjectsInSelection(pObjSelection, selectedGroup) : pObjSelection;
                                                        foreach (var po in inclusiveSelection)
                                                        {
                                                            po.m_color = color;
                                                            po.m_material.color = color;
                                                        }
                                                    },
                                                    () => { showLayerSetScroll = false; scrollLayerSet = Vector2.zero; showMoreTools = false; });

                                                if (!(isRoot && selectedGroup == null))
                                                {
                                                    if (filters.DrawPicker(new Rect(objScreenPos + new Vector2(116, 127 + addedHeight), new Vector2(26, 20))))
                                                        filters.Pick(obj);
                                                }
                                                if (showMoreTools)
                                                {
                                                    SelectionModeAction.DrawActionsUI(objScreenPos + new Vector2(144, 81 + addedHeight));
                                                }
                                                if (showLayerSetScroll)
                                                {
                                                    GUI.Box(new Rect(objScreenPos + new Vector2(144, 58 + addedHeight), new Vector2(150, 160)), string.Empty);
                                                    scrollLayerSet = GUI.BeginScrollView(new Rect(objScreenPos + new Vector2(145, 59 + addedHeight), new Vector2(148, 158)), scrollLayerSet, new Rect(0, 0, 124, 24 * layerManager.m_layers.Count + 26));
                                                    for (int i = 0; i < layerManager.m_layers.Count; i++)
                                                    {
                                                        if (layerManager.m_layers[i] == obj.layer)
                                                            GUI.color = Color.red;
                                                        if (GUI.Button(new Rect(1, i * 24 + 1, 122, 23), layerManager.m_layers[i].m_name))
                                                        {
                                                            PlaySound();
                                                            ResetLayerScrollmenu();
                                                            obj.layer = layerManager.m_layers[i];
                                                            if (isRoot && selectedGroup == null)
                                                            {
                                                                foreach (var po in obj.group.objects)
                                                                    po.layer = layerManager.m_layers[i];
                                                            }
                                                            // pObjSelection.Clear();
                                                            // ClosePainter();
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
                                                        if (isRoot && selectedGroup == null)
                                                        {
                                                            foreach (var po in obj.group.objects)
                                                                po.layer = null;
                                                        }
                                                        // pObjSelection.Clear();
                                                        // ClosePainter();
                                                    }
                                                    GUI.color = Color.white;
                                                    GUI.EndScrollView();
                                                }
                                                #endregion
                                            }
                                            else
                                            {
                                                #region
                                                bool isRoot = obj.group != null && obj.isRootOfGroup;
                                                float addedHeight = 0;
                                                if (selectedGroup == null)
                                                {
                                                    if (GUI.Button(new Rect(objScreenPos + new Vector2(12, -11), new Vector2(130, 22)), LocalizationManager.instance.current["make_group"]))
                                                    {
                                                        PlaySound();
                                                        var group = POGroup.MakeGroup(this, pObjSelection, obj);
                                                        pObjSelection.Clear();
                                                        pObjSelection.Add(group.root);
                                                        if (!filters.c_groups)
                                                            filters.c_groups = true;
                                                    }
                                                    addedHeight += 23;
                                                }
                                                if (GUI.Button(new Rect(objScreenPos + new Vector2(12, -11 + addedHeight), new Vector2(130, 22)), LocalizationManager.instance.current["move_to"]))
                                                {
                                                    PlaySound();
                                                    // pObjSelection.Clear();
                                                    MoveSelection(POGroup.AllObjectsInSelection(pObjSelection, selectedGroup), true);
                                                    placingSelection = true;
                                                    CloseAllSMWindows();
                                                    movingWholeModel = true;
                                                    toolAction = ToolAction.build;
                                                    editingWholeModel = true;
                                                    proceduralTool = true;
                                                    hoveredObj = null;
                                                }
                                                if (GUI.Button(new Rect(objScreenPos + new Vector2(12, 12 + addedHeight), new Vector2(130, 22)), LocalizationManager.instance.current["copy"]))
                                                {
                                                    PlaySound();
                                                    clipboard = new ClipboardProceduralObjects(ClipboardProceduralObjects.ClipboardType.Selection);
                                                    clipboard.MakeSelectionList(POGroup.AllObjectsInSelection(pObjSelection, selectedGroup), selectedGroup);
                                                    storedHeight = obj.m_position.y;
                                                }
                                                if (GUI.Button(new Rect(objScreenPos + new Vector2(12, 35 + addedHeight), new Vector2(130, 22)), LocalizationManager.instance.current["export_selection"]))
                                                {
                                                    PlaySound();
                                                    SelectionModeAction.CreateAction<ExportSelection>();
                                                }
                                                if (GUI.Button(new Rect(objScreenPos + new Vector2(12, 58 + addedHeight), new Vector2(130, 22)), LocalizationManager.instance.current["layers_set"] + (showLayerSetScroll ? " ►" : "")))
                                                {
                                                    PlaySound();
                                                    // open layer scroll menu
                                                    showLayerSetScroll = !showLayerSetScroll;
                                                    showMoreTools = false;
                                                }
                                                if (GUI.Button(new Rect(objScreenPos + new Vector2(12, 81 + addedHeight), new Vector2(130, 22)), LocalizationManager.instance.current["more"] + (showMoreTools ? " ►" : "")))
                                                {
                                                    PlaySound();
                                                    // align height
                                                    selectingNewGrpRoot = false;
                                                    showMoreTools = !showMoreTools;
                                                    showLayerSetScroll = false;
                                                    /*
                                                    alignHeightObj.Clear();
                                                    alignHeightObj.AddRange(pObjSelection); */
                                                }
                                                if (GUI.Button(new Rect(objScreenPos + new Vector2(12, 104 + addedHeight), new Vector2(130, 22)), LocalizationManager.instance.current["delete"]))
                                                {
                                                    var inclusiveList = (selectedGroup == null) ? POGroup.AllObjectsInSelection(pObjSelection, selectedGroup) : pObjSelection;
                                                    YieldConfirmDeletePanel(inclusiveList.Count, inclusiveList[0].m_position, delegate()
                                                    {
                                                        for (int i = 0; i < inclusiveList.Count; i++)
                                                        {
                                                            var po = inclusiveList[i];
                                                            if (po.group != null)
                                                                po.group.Remove(this, po);
                                                            moduleManager.DeleteAllModules(po);
                                                            proceduralObjects.Remove(po);
                                                            activeIds.Remove(po.id);
                                                        }
                                                        pObjSelection.Clear();
                                                    });
                                                }
                                                Color c = Color.white;
                                                var inclusiveSelection = POGroup.AllObjectsInSelection(pObjSelection, selectedGroup);
                                                for (int i = 0; i < inclusiveSelection.Count; i++)
                                                {
                                                    if (i == 0)
                                                        c = inclusiveSelection[i].m_color;
                                                    else
                                                    {
                                                        if (inclusiveSelection[i].m_color != c)
                                                        {
                                                            c = Color.white;
                                                            break;
                                                        }
                                                    }
                                                }
                                                painter = GUIPainter.DrawPainter(painter, objScreenPos + new Vector2(12, 127 + addedHeight), objScreenPos + new Vector2(12, 150 + addedHeight), c,
                                                    (color) =>
                                                    {
                                                        var inclSelection = (selectedGroup == null) ? POGroup.AllObjectsInSelection(pObjSelection, selectedGroup) : pObjSelection;
                                                        foreach (var po in inclSelection)
                                                        {
                                                            po.m_color = color;
                                                            po.m_material.color = color;
                                                        }
                                                    },
                                                    () => { showLayerSetScroll = false; scrollLayerSet = Vector2.zero; showMoreTools = false; });
                                                if (showMoreTools)
                                                {
                                                    SelectionModeAction.DrawActionsUI(objScreenPos + new Vector2(144, 81 + addedHeight));
                                                }
                                                if (showLayerSetScroll)
                                                {
                                                    GUI.Box(new Rect(objScreenPos + new Vector2(144, 58 + addedHeight), new Vector2(150, 160)), string.Empty);
                                                    scrollLayerSet = GUI.BeginScrollView(new Rect(objScreenPos + new Vector2(145, 59 + addedHeight), new Vector2(148, 158)), scrollLayerSet, new Rect(0, 0, 124, 24 * layerManager.m_layers.Count + 26));
                                                    for (int i = 0; i < layerManager.m_layers.Count; i++)
                                                    {
                                                        if (GUI.Button(new Rect(1, i * 24 + 1, 122, 23), layerManager.m_layers[i].m_name))
                                                        {
                                                            PlaySound();
                                                            ResetLayerScrollmenu();
                                                            foreach (var o in inclusiveSelection)
                                                                o.layer = layerManager.m_layers[i];
                                                            pObjSelection.Clear();
                                                        }
                                                    }
                                                    if (GUI.Button(new Rect(1, layerManager.m_layers.Count * 24 + 1, 122, 23), "<i>" + LocalizationManager.instance.current["layers_none"] + "</i>"))
                                                    {
                                                        PlaySound();
                                                        ResetLayerScrollmenu();
                                                        foreach (var o in inclusiveSelection)
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
                                            selectionModeAction.OnActionGUI(objScreenPos + new Vector2(12, -11));
                                        }
                                        GUI.color = Color.red;
                                    }
                                    else
                                    {
                                        if (obj._selected)
                                            GUI.color = Color.red;
                                        else
                                            GUI.color = Color.white;
                                    }
                                }
                                else
                                    GUI.color = Color.white;
                                if (GUI.Button(new Rect(objScreenPos + new Vector2(-11, -11), new Vector2(23, 22)), "<size=20>+</size>"))
                                {
                                    PlaySound();
                                    ResetLayerScrollmenu();
                                    if (selectionModeAction != null)
                                    {
                                        selectionModeAction.OnSingleClick(obj);
                                        //     AlignHeights(obj.m_position.y);
                                    }
                                    else if (selectingNewGrpRoot && selectedGroup != null)
                                    {
                                        selectedGroup.ChooseAsRoot(obj);
                                        selectingNewGrpRoot = false;
                                    }
                                    else
                                    {
                                        if (Input.GetKey(KeyCode.LeftControl))
                                        {
                                            if (pObjSelection.Contains(obj))
                                            {
                                                pObjSelection.Remove(obj);
                                            }
                                            else
                                            {
                                                pObjSelection.Add(obj);
                                            }
                                        }
                                        else
                                        {
                                            if (pObjSelection.Count == 1)
                                            {
                                                if (obj == pObjSelection[0])
                                                {
                                                    pObjSelection.Clear();
                                                }
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
                    }
                    SingleHoveredObj = hoveredObj;
                }
                if (!movingWholeModel)
                {
                    var winrect = GUIUtils.ClampRectToScreen(GUIUtils.Window(1094334744, window, DrawUIWindow, "Procedural Objects v" + ProceduralObjectsMod.VERSION));
                    if (proceduralTool && editingWholeModel)
                        window = new Rect(winrect.x, winrect.y, winrect.width, (ProceduralObjectsMod.ShowToolsControls.value ? 608 : 443)); // general tool
                    else if (proceduralTool || chosenProceduralInfo != null)
                        window = new Rect(winrect.x, winrect.y, winrect.width,
                            (chosenProceduralInfo == null ? 284 + (POToolAction.actions.Count * 26) + (ProceduralObjectsMod.ShowToolsControls.value ? 168 : 0) : 400)); // customization tool : tex select
                    else
                        window = new Rect(winrect.x, winrect.y, winrect.width, _winHeight); // selection mode 

                    if (showExternals)
                        externalsWindow = GUIUtils.ClampRectToScreen(GUIUtils.Window(1094334745, externalsWindow, DrawExternalsWindow, LocalizationManager.instance.current["saved_pobjs"]));

                    textManager.DrawWindow();

                    moduleManager.DrawWCustomizationindows();

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

                            if (vertWizardAllows && !KeyBindingsManager.instance.GetBindingFromName("edition_smoothMovements").GetBinding())
                            {
                                foreach (Vertex vertex in tempVerticesBuffer)
                                {
                                    if (vertex == null)
                                        continue;
                                    if (vertex.IsDependent)
                                        continue;
                                    if (currentlyEditingObject.m_mesh.name == "ploppablecliffgrass" && vertex.Index >= currentlyEditingObject.allVertices.Length - 2)
                                        continue;
                                    var vertexWorldPos = ProceduralUtils.VertexWorldPosition(vertex, currentlyEditingObject);
                                    if (renderCamera.WorldToScreenPoint(vertexWorldPos).z < 0)
                                        continue;
                                    if ((editingVertex && !editingVertexIndex.Contains(vertex.Index)) || !editingVertex)
                                    {
                                        if (GUI.Button(new Rect(vertexWorldPos.WorldToGuiPoint() + new Vector2(-10, -10), new Vector2(20, 20)), VertexUtils.vertexIcons[0], GUI.skin.label))
                                        {
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
                                        }
                                    }
                                    else
                                    {
                                        if (GUI.Button(new Rect(vertexWorldPos.WorldToGuiPoint() + new Vector2(-10, -10), new Vector2(20, 20)), VertexUtils.vertexIcons[1], GUI.skin.label))
                                        {
                                            PlaySound();
                                            editingVertexIndex.Remove(vertex.Index);
                                            if (editingVertexIndex.Count == 0)
                                                editingVertex = false;
                                        }
                                    }
                                }
                            }
                        }
                        else if (currentlyEditingObject._insideUIview && axisState == AxisEditionState.none)
                        {
                            Vector2 objPosition = currentlyEditingObject.m_position.WorldToGuiPoint();
                            if (GUI.Button(new Rect(Gizmos.RightMostGUIPosGizmo + new Vector2(5, -26), new Vector2(100, 23)), LocalizationManager.instance.current["move_to"]))
                            {
                                PlaySound();
                                getBackToGeneralTool = true;
                                currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.moveTo, tempVerticesBuffer);
                                movingWholeModel = true;
                                toolAction = ToolAction.build;
                                placingSelection = false;
                                Gizmos.DestroyGizmo();
                                textManager.CloseWindow();
                                advEdManager = null;
                            }
                            if (actionMode == 1)
                            {
                                GUI.color = Color.grey;
                                GUI.Box(new Rect(Gizmos.RightMostGUIPosGizmo + new Vector2(5, 0), new Vector2(100, 23)), "<i>" + LocalizationManager.instance.current["referential_local"] + "</i>");
                                GUI.color = Color.white;
                            }
                            else
                            {
                                // (formerly) LocalizationManager.instance.current["delete"])) { DeleteObject(); }
                                if (GUI.Button(new Rect(Gizmos.RightMostGUIPosGizmo + new Vector2(5, 0), new Vector2(100, 23)), LocalizationManager.instance.current[Gizmos.referential == Gizmos.SpaceReferential.World ? "referential_world" : "referential_local"]))
                                {
                                    PlaySound();
                                    if (Gizmos.referential == Gizmos.SpaceReferential.Local)
                                        Gizmos.referential = Gizmos.SpaceReferential.World;
                                    else
                                        Gizmos.referential = Gizmos.SpaceReferential.Local;
                                }
                            }

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
                            Rect modeRect = new Rect(Gizmos.RightMostGUIPosGizmo + new Vector2(5, 26), new Vector2(100, 23));
                            if (modeRect.IsMouseInside())
                                modeText += " ►";
                            if (GUI.Button(modeRect, modeText))
                            {
                                PlaySound();
                                SwitchActionMode();
                            }
                        }
                    #endregion
                    }
                }
                else // if Move To Tool
                {
                    GUI.BeginGroup(new Rect(window.position, new Vector2(370, 230)));
                    var movetorect = new Rect(0, 0, 370, 230);
                    if (!ProceduralObjectsMod.UseUINightMode.value)
                        GUI.DrawTexture(movetorect, GUIUtils.bckgTex, ScaleMode.StretchToFill);
                    GUI.Box(movetorect, string.Empty);
                    GUIUtils.HelpButton(370, "Move_To_Tool");
                    GUI.Label(new Rect(5, 5, 360, 28), "<b><size=16>" + LocalizationManager.instance.current["move_to_tool"] + "</size></b>");
                    GUI.Label(new Rect(5, 38, 360, 187), "<b>" + LocalizationManager.instance.current["controls"] + "</b>\n" +
                        LocalizationManager.instance.current["LM_click"] + " : " + LocalizationManager.instance.current["confirm_placement"] + "\n" +
                        LocalizationManager.instance.current["moveTo_RMB_rotate"] + "\n" +
                        KeyBindingsManager.instance.GetBindingFromName("position_moveUp").m_fullKeys + "/" +
                        KeyBindingsManager.instance.GetBindingFromName("position_moveDown").m_fullKeys + " : " + LocalizationManager.instance.current["moveTo_UpDown"] + "\n" +
                        KeyBindingsManager.instance.GetBindingFromName("edition_smallMovements").m_fullKeys + " : " + LocalizationManager.instance.current["hold_slow_together"] + "\n\n" +
                        KeyBindingsManager.instance.GetBindingFromName("snapStoredHeight").m_fullKeys + " : " + LocalizationManager.instance.current["snapToPrevHeight"] + "\n" +
                        KeyBindingsManager.instance.GetBindingFromName("enableSnapping").m_fullKeys + " : " + LocalizationManager.instance.current["snapToBuildingsNetworks"]);
                    GUI.EndGroup();
                }
            }
        }


        public void DrawUIWindow(int id)
        {
            #region setup window
            GUI.DragWindow(new Rect(0, 0, 348, 30));
            string help = "Main_Page";
            if (proceduralTool && currentlyEditingObject != null)
            {
                if (editingWholeModel)
                    help = "General_Tool";
                else
                    help = "Customization_Tool";
            }
            if (GUIUtils.CloseHelpButtons(window, help))
                ClosePO();

            #endregion


            if (proceduralTool)
            {
                if (currentlyEditingObject != null)
                {
                    GUIUtils.DrawSeparator(new Vector2(10, 26), 380);
                    GUI.BeginGroup(new Rect(10, 30, 380, 442));
                    if (editingWholeModel)
                    {
                        // GENERAL TOOL
                        GUI.Label(new Rect(27, 0, 300, 30), "<b><size=18>" + LocalizationManager.instance.current["general_tool"] + "</size></b>");
                        GUI.contentColor = Color.green;
                        GUI.Label(new Rect(0, 0, 23, 23), "<size=18>¤</size>", GUI.skin.button);
                        GUI.contentColor = Color.white;
                        ProceduralTool.DrawToolsControls(new Rect(0, 30, 380, 190), true);
                        GUI.EndGroup();

                        GUI.BeginGroup(new Rect(10, ProceduralObjectsMod.ShowToolsControls.value ? 255 : 90, 380, 355));

                        Vector3 euler = currentlyEditingObject.m_rotation.eulerAngles;
                        if (currentlyEditingObject.transformInputFields == null)
                        {
                            currentlyEditingObject.transformInputFields = new GUIUtils.FloatInputField[] {
                                new GUIUtils.FloatInputField(currentlyEditingObject.m_position.x),
                                new GUIUtils.FloatInputField(currentlyEditingObject.m_position.y),
                                new GUIUtils.FloatInputField(currentlyEditingObject.m_position.z),
                                new GUIUtils.FloatInputField(euler.x),
                                new GUIUtils.FloatInputField(euler.y),
                                new GUIUtils.FloatInputField(euler.z),
                            };
                        }

                        GUI.Label(new Rect(0, 0, 390, 27), "<b><size=14>" + LocalizationManager.instance.current["position"] + "</size></b>");
                        GUI.Label(new Rect(0, 27, 25, 24), "X :");
                        GUI.Label(new Rect(126, 27, 25, 24), "Y :");
                        GUI.Label(new Rect(252, 27, 25, 24), "Z :");

                        currentlyEditingObject.m_position = new Vector3(currentlyEditingObject.transformInputFields[0].DrawField(new Rect(26, 27, 95, 23), currentlyEditingObject.m_position.x).returnValue,
                            currentlyEditingObject.m_position.y, currentlyEditingObject.m_position.z);

                        currentlyEditingObject.m_position = new Vector3(currentlyEditingObject.m_position.x,
                            currentlyEditingObject.transformInputFields[1].DrawField(new Rect(152, 27, 95, 23), currentlyEditingObject.m_position.y).returnValue, currentlyEditingObject.m_position.z);

                        currentlyEditingObject.m_position = new Vector3(currentlyEditingObject.m_position.x, currentlyEditingObject.m_position.y,
                            currentlyEditingObject.transformInputFields[2].DrawField(new Rect(278, 27, 95, 23), currentlyEditingObject.m_position.z).returnValue);

                        /*
                        float newX, newY, newZ;
                        if (float.TryParse(GUI.TextField(new Rect(26, 27, 95, 23), currentlyEditingObject.m_position.x.ToString()), out newX))
                            currentlyEditingObject.m_position = new Vector3(newX, currentlyEditingObject.m_position.y, currentlyEditingObject.m_position.z);
                        if (float.TryParse(GUI.TextField(new Rect(152, 27, 95, 23), currentlyEditingObject.m_position.y.ToString()), out newY))
                            currentlyEditingObject.m_position = new Vector3(currentlyEditingObject.m_position.x, newY, currentlyEditingObject.m_position.z);
                        if (float.TryParse(GUI.TextField(new Rect(278, 27, 95, 23), currentlyEditingObject.m_position.z.ToString()), out newZ))
                            currentlyEditingObject.m_position = new Vector3(currentlyEditingObject.m_position.x, currentlyEditingObject.m_position.y, newZ);
                         * */

                        if (GUI.Button(new Rect(0, 52, 187.5f, 22f), LocalizationManager.instance.current["snapToGround"]))
                        {
                            ProceduralObjectsLogic.PlaySound();
                            currentlyEditingObject.SnapToGround();
                        }
                        if (GUI.Button(new Rect(192.5f, 52, 187.5f, 22f), LocalizationManager.instance.current["storeHeight"]))
                        {
                            ProceduralObjectsLogic.PlaySound();
                            storedHeight = currentlyEditingObject.m_position.y;
                        }

                        GUI.Label(new Rect(0, 77, 390, 27), "<b><size=14>" + LocalizationManager.instance.current["rotation"] + "</size></b>");
                        GUI.Label(new Rect(0, 104, 25, 24), "X :");
                        GUI.Label(new Rect(126, 104, 25, 24), "Y :");
                        GUI.Label(new Rect(252, 104, 25, 24), "Z :");

                        euler.x = currentlyEditingObject.transformInputFields[3].DrawField(new Rect(26, 104, 95, 23), euler.x).returnValue;
                        euler.y = currentlyEditingObject.transformInputFields[4].DrawField(new Rect(152, 104, 95, 23), euler.y).returnValue;
                        euler.z = currentlyEditingObject.transformInputFields[5].DrawField(new Rect(278, 104, 95, 23), euler.z).returnValue;
                        /*
                        if (float.TryParse(GUI.TextField(new Rect(26, 104, 95, 23), euler.x.ToString()), out newX))
                            euler.x = newX;
                        if (float.TryParse(GUI.TextField(new Rect(152, 104, 95, 23), euler.y.ToString()), out newY))
                            euler.y = newY;
                        if (float.TryParse(GUI.TextField(new Rect(278, 104, 95, 23), euler.z.ToString()), out newZ))
                            euler.z = newZ;*/
                        currentlyEditingObject.m_rotation = Quaternion.Euler(euler);

                        if (GUI.Button(new Rect(0, 129, 380, 22f), LocalizationManager.instance.current["resetOrientation"]))
                        {
                            if (currentlyEditingObject.m_rotation == Quaternion.identity)
                                PlaySound(2);
                            else
                            {
                                PlaySound();
                                currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.rotation, null);
                                currentlyEditingObject.m_rotation = Quaternion.identity;
                                currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(null);
                            }
                        }

                        GUIUtils.DrawSeparator(new Vector2(0, 153), 380);

                        GUI.Label(new Rect(0, 155, 380, 27), "<b><size=13>" + LocalizationManager.instance.current["render_distance"] + " :</size></b> "
                            + Gizmos.ConvertRoundToDistanceUnit(currentlyEditingObject.renderDistance).ToString("N").Replace(".00", "") + ProceduralObjectsMod.distanceUnit
                            + ((RenderOptions.instance.globalMultiplier != 1f) ? ("  (x " + RenderOptions.instance.globalMultiplier + ")") : ""));

                        var renderDistSlider = GUI.HorizontalSlider(new Rect(0, 180, 350, 20), Mathf.Floor(currentlyEditingObject.renderDistance), 50f, 16000f);
                        if (renderDistSlider != currentlyEditingObject.renderDistance)
                        {
                            currentlyEditingObject.renderDistance = renderDistSlider;
                            if (!currentlyEditingObject.renderDistLocked)
                            {
                                ProceduralObjectsLogic.PlaySound();
                                currentlyEditingObject.renderDistLocked = true;
                            }
                        }

                        if (GUI.Button(new Rect(353, 170, 25, 25), ProceduralObjectsMod.Icons[currentlyEditingObject.renderDistLocked ? 8 : 9]))
                        {
                            ProceduralObjectsLogic.PlaySound();
                            currentlyEditingObject.renderDistLocked = !currentlyEditingObject.renderDistLocked;
                        }

                        GUIUtils.DrawSeparator(new Vector2(0, 199), 380);

                        GUI.Label(new Rect(0, 202, 380, 25), "<b><size=13>" + LocalizationManager.instance.current["export_selection"] + "</size></b>");

                        externalsSaveTextfield = GUI.TextField(new Rect(0, 229, 285, 28), externalsSaveTextfield);
                        if (File.Exists(ProceduralObjectsMod.ExternalsConfigPath + externalsSaveTextfield.ToFileName() + ".pobj"))
                        {
                            GUI.color = Color.red;
                            GUI.Label(new Rect(290, 229, 90, 28), "X", GUI.skin.button);
                            GUI.color = Color.white;
                        }
                        else
                        {
                            if (GUI.Button(new Rect(290, 229, 90, 28), LocalizationManager.instance.current["export_selection"]))
                            {
                                PlaySound();
                                ExPObjManager.SaveToExternal(externalsSaveTextfield, new CacheProceduralObject(currentlyEditingObject));
                                externalsSaveTextfield = LocalizationManager.instance.current["enter_name"];
                            }
                        }

                        GUIUtils.DrawSeparator(new Vector2(0, 259), 380);

                        if (GUI.Button(new Rect(0, 319, 185, 25), "◄ " + LocalizationManager.instance.current["back"]))
                        {
                            PlaySound();
                            EditModeBack();
                        }
                        else
                        {
                            string moduleCount = "";
                            if (currentlyEditingObject.m_modules != null)
                            {
                                if (currentlyEditingObject.m_modules.Count > 0)
                                    moduleCount = " (" + currentlyEditingObject.m_modules.Count + ")";
                            }
                            if (GUI.Button(new Rect(0, 263, 375, 25), LocalizationManager.instance.current["modules"] + moduleCount))
                                moduleManager.ShowModulesWindow(currentlyEditingObject);

                            if (GUI.Button(new Rect(190, 291, 185, 25), LocalizationManager.instance.current["adv_edition"]))
                                ShowAdvEditionTools();

                            if (TextParameters.CanHaveTextParameters(currentlyEditingObject))
                            {
                                if (GUI.Button(new Rect(0, 291, 185, 25), LocalizationManager.instance.current["text_customization"]))
                                {
                                    PlaySound();
                                    textManager.Edit(currentlyEditingObject, new Vector2(window.x + window.width, window.y));
                                }
                            }

                            if (GUI.Button(new Rect(190, 319, 185, 25), LocalizationManager.instance.current["vertex_customization"]))
                            {
                                PlaySound();
                                ProceduralTool.scrollControls = Vector2.zero;
                                toolAction = ToolAction.vertices;
                                if (Gizmos.Exists)
                                {
                                    Gizmos.DestroyGizmo();
                                }
                                editingWholeModel = false;
                            }

                        }
                        GUI.EndGroup();
                    }
                    else
                    {
                        // CUSTOMIZATION TOOL
                        GUI.Label(new Rect(27, 0, 300, 30), "<b><size=18>" + LocalizationManager.instance.current["vertex_tool"] + "</size></b>");
                        GUI.Label(new Rect(0, 0, 23, 23), VertexUtils.vertexIcons[0], GUI.skin.button);
                        ProceduralTool.DrawToolsControls(new Rect(0, 30, 380, 190), false);

                        GUI.EndGroup();

                        // MOUSE TOOL
                        GUI.BeginGroup(new Rect(10, ProceduralObjectsMod.ShowToolsControls.value ? 255 : 87, 380, 110));
                        GUI.Label(new Rect(0, 0, 380, 22), "<b><size=13>" + LocalizationManager.instance.current["mouse_tool"] + "</size></b>");

                        var prevAlign = GUI.skin.label.alignment;
                        GUI.skin.label.alignment = TextAnchor.MiddleCenter;

                        // pos
                        if (GUI.Button(new Rect(0, 25, 58, 52), string.Empty))
                        {
                            PlaySound();
                            verticesToolType = 0;
                        }
                        GUI.Label(new Rect(3, 24, 52, 40), ProceduralTool.moveVertices.m_texture);
                        GUI.Label(new Rect(1, 56, 56, 22), (verticesToolType == 0 ? "<color=red><b>" : "") + LocalizationManager.instance.current["position"] + (verticesToolType == 0 ? "</b></color>" : ""));

                        // rot
                        if (GUI.Button(new Rect(63, 25, 58, 52), string.Empty))
                        {
                            PlaySound();
                            verticesToolType = 1;
                        }
                        GUI.Label(new Rect(66, 24, 52, 40), ProceduralTool.rotateVertices.m_texture);
                        GUI.Label(new Rect(64, 56, 56, 22), (verticesToolType == 1 ? "<color=red><b>" : "") + LocalizationManager.instance.current["rotation"] + (verticesToolType == 1 ? "</b></color>" : ""));

                        // scale
                        if (GUI.Button(new Rect(126, 25, 58, 52), string.Empty))
                        {
                            PlaySound();
                            verticesToolType = 2;
                        }
                        GUI.Label(new Rect(129, 24, 52, 40), ProceduralTool.scaleVertices.m_texture);
                        GUI.Label(new Rect(127, 56, 56, 22), (verticesToolType == 2 ? "<color=red><b>" : "") + LocalizationManager.instance.current["scale_obj"] + (verticesToolType == 2 ? "</b></color>" : ""));
                        GUI.skin.label.alignment = prevAlign;

                        GUIUtils.DrawSeparator(new Vector2(0, 80), 380);

                        GUI.EndGroup();

                        int heightDueToCTA = POToolAction.actions.Count * 26 + 38;
                        GUI.BeginGroup(new Rect(10, (ProceduralObjectsMod.ShowToolsControls.value ? 168 : 0) + 160, 380, heightDueToCTA + 500));
                        GUI.Label(new Rect(0, 7, 380, 22), "<b><size=13>" + LocalizationManager.instance.current["CTActions"] + "</size></b>");

                        // PO actions
                        for (int i = 0; i < POToolAction.actions.Count; i++)
                        {
                            POToolAction.actions[i].ActionButton(new Rect(0, 29 + i * 26, 375, 24), currentlyEditingObject, editingVertexIndex, tempVerticesBuffer, Apply);
                        }

                        GUIUtils.DrawSeparator(new Vector2(0, heightDueToCTA - 5), 380);

                        // bottom buttons
                        if (GUI.Button(new Rect(190, 28 + heightDueToCTA, 185, 25), LocalizationManager.instance.current["adv_edition"]))
                            ShowAdvEditionTools();

                        if (GUI.Button(new Rect(0, heightDueToCTA, 375, 25), LocalizationManager.instance.current["modules"]))
                            moduleManager.ShowModulesWindow(currentlyEditingObject);

                        if (GUI.Button(new Rect(190, 56 + heightDueToCTA, 185, 25), LocalizationManager.instance.current["general_tool"]))
                        {
                            PlaySound();
                            ProceduralTool.scrollControls = Vector2.zero;
                            toolAction = ToolAction.none;
                            SwitchToMainTool();
                        }

                        if (TextParameters.CanHaveTextParameters(currentlyEditingObject))
                        {
                            if (GUI.Button(new Rect(0, 28 + heightDueToCTA, 185, 25), LocalizationManager.instance.current["text_customization"]))
                            {
                                PlaySound();
                                textManager.Edit(currentlyEditingObject, new Vector2(window.x + window.width, window.y));
                            }
                        }

                        if (GUI.Button(new Rect(0, 56 + heightDueToCTA, 185, 25), "◄ " + LocalizationManager.instance.current["back"]))
                        {
                            PlaySound();
                            EditModeBack();
                        }
                        GUI.EndGroup();
                    }
                }
            }
            else
            {
                if (chosenProceduralInfo == null)
                {
                    GUIUtils.DrawSeparator(new Vector2(10, 26), 380);
                    GUI.Label(new Rect(37, 30, 300, 30), "<b><size=18>" + LocalizationManager.instance.current["selection_mode"] + "</size></b>");
                    GUI.Label(new Rect(10, 30, 23, 23), "<size=18>+</size>", GUI.skin.button);

                    GUI.Label(new Rect(10, 53, 375, 20), "<size=11>" + LocalizationManager.instance.current["total_obj_count"] + " : " + proceduralObjects.Count.ToString("N").Replace(".00", "") + "</size>");

                    GUIUtils.DrawSeparator(new Vector2(10, 76), 380);

                    float addedHeight = 86;
                    if (proceduralObjects.Count == 0)
                    {
                        GUI.Label(new Rect(10, 78, 350, 50), LocalizationManager.instance.current["no_po_msg"]);
                        GUIUtils.HelpButton(new Rect(363, 90, 24, 24), "Creating_an_object");
                        GUIUtils.DrawSeparator(new Vector2(10, 128), 380);
                        addedHeight += 55;
                    }

                    if (SelectionModeTabDrawer.tabs == null)
                    {
                        SelectionModeTabDrawer.tabs = new List<SelectionModeTab>()
                        {
                            new SelectionModeTab("layers", 0, () => {
                                    layerManager.winRect.position = new Vector2(window.x + 400, window.y);
                                    layerManager.showWindow = !layerManager.showWindow; } ),
                            new SelectionModeTab("saved_pobjs", 1, () => {
                                if (showExternals)
                                    CloseExternalsWindow();
                                else
                                {
                                    renamingExternalString = "";
                                    renamingExternal = -1;
                                    externalsWindow.position = new Vector2(window.x + 400, window.y);
                                    showExternals = true;
                                } } ),
                            new SelectionModeTab("texture_management", 2, () => {
                                TextureManager.instance.SetPosition(window.x + 400, window.y);
                                TextureManager.instance.showWindow = !TextureManager.instance.showWindow;} ),
                            new SelectionModeTab("font_management", 3, () => {
                                FontManager.instance.SetPosition(window.x + 400, window.y);
                                FontManager.instance.showWindow = !FontManager.instance.showWindow; } ),
                            new SelectionModeTab("modules_management", 4, () => {
                                ModuleManager.instance.SetPosition(window.x + 400, window.y);
                                ModuleManager.instance.showManagerWindow = !ModuleManager.instance.showManagerWindow; } ),
                            new SelectionModeTab("render_options", 5, () => {
                                RenderOptions.instance.SetPosition(window.x + 400, window.y);
                                RenderOptions.instance.showWindow = !RenderOptions.instance.showWindow; } ),
                            new SelectionModeTab("stats", 6, () => {
                                POStatisticsManager.instance.SetPosition(window.x + 400, window.y);
                                POStatisticsManager.instance.RefreshCounters();
                                POStatisticsManager.instance.showWindow = !POStatisticsManager.instance.showWindow; } ),
                        };
                    }
                    addedHeight += SelectionModeTabDrawer.DrawTabsGetHeight(new Rect(8, addedHeight - 5, 384, 200), 7f);

                    GUIUtils.DrawSeparator(new Vector2(6, addedHeight), 385);
                    GUI.BeginGroup(new Rect(5, addedHeight + 3, 390, 65));
                    if (filters.DrawFilters(new Rect(0, 0, 390, 65)))
                    {
                        PlaySound();
                        pObjSelection.Clear();
                    }
                    GUI.EndGroup();
                    addedHeight += 71;
                    #region
                    /*
                    // LAYERS
                    if (GUI.Button(new Rect(6, 114, 384, 35), string.Empty))
                    {
                        PlaySound();
                        layerManager.winRect.position = new Vector2(window.x + 400, window.y + 114);
                        layerManager.showWindow = !layerManager.showWindow;
                    }
                    GUI.Label(new Rect(12, 116, 30, 30), ProceduralObjectsMod.SelectionModeIcons[0]);
                    GUI.Label(new Rect(46, 118, 320, 25), "<size=15>" + LocalizationManager.instance.current["layers"] + "</size>");

                    // EXPORTED OBJS
                    if (GUI.Button(new Rect(6, 152, 384, 35), string.Empty))
                    {
                        PlaySound();
                        if (showExternals)
                            CloseExternalsWindow();
                        else
                        {
                            renamingExternalString = "";
                            renamingExternal = -1;
                            externalsWindow.position = new Vector2(window.x + 400, window.y + 152);
                            showExternals = true;
                        }
                    }
                    GUI.Label(new Rect(12, 154, 30, 30), ProceduralObjectsMod.SelectionModeIcons[1]);
                    GUI.Label(new Rect(46, 156, 320, 25), "<size=15>" + LocalizationManager.instance.current["saved_pobjs"] + "</size>");

                    // TEX MANAGEMENT
                    if (GUI.Button(new Rect(6, 190, 384, 35), string.Empty))
                    {
                        PlaySound();
                        TextureManager.instance.SetPosition(window.x + 400, window.y + 190);
                        TextureManager.instance.showWindow = !TextureManager.instance.showWindow;
                    }
                    GUI.Label(new Rect(12, 192, 30, 30), ProceduralObjectsMod.SelectionModeIcons[2]);
                    GUI.Label(new Rect(46, 194, 320, 25), "<size=15>" + LocalizationManager.instance.current["texture_management"] + "</size>");

                    // FONT MANAGEMENT
                    if (GUI.Button(new Rect(6, 228, 384, 35), string.Empty))
                    {
                        PlaySound();
                        FontManager.instance.SetPosition(window.x + 400, window.y + 228);
                        FontManager.instance.showWindow = !FontManager.instance.showWindow;
                    }
                    GUI.Label(new Rect(12, 230, 30, 30), ProceduralObjectsMod.SelectionModeIcons[3]);
                    GUI.Label(new Rect(46, 232, 320, 25), "<size=15>" + LocalizationManager.instance.current["font_management"] + "</size>");

                    // MODULES MANAGEMENT
                    if (GUI.Button(new Rect(6, 266, 384, 35), string.Empty))
                    {
                        PlaySound();
                        ModuleManager.instance.SetPosition(window.x + 400, window.y + 266);
                        ModuleManager.instance.showManagerWindow = !ModuleManager.instance.showManagerWindow;
                    }
                    GUI.Label(new Rect(12, 268, 30, 30), ProceduralObjectsMod.SelectionModeIcons[4]);
                    GUI.Label(new Rect(46, 270, 320, 25), "<size=15>" + LocalizationManager.instance.current["modules_management"] + "</size>");

                    // RENDER MANAGER
                    if (GUI.Button(new Rect(6, 304, 384, 35), string.Empty))
                    {
                        PlaySound();
                        RenderOptions.instance.SetPosition(window.x + 400, window.y + 304);
                        RenderOptions.instance.showWindow = !RenderOptions.instance.showWindow;
                    }
                    GUI.Label(new Rect(12, 306, 30, 30), ProceduralObjectsMod.SelectionModeIcons[5]);
                    GUI.Label(new Rect(46, 308, 320, 25), "<size=15>" + LocalizationManager.instance.current["render_options"] + "</size>");

                    // STATS
                    if (GUI.Button(new Rect(6, 342, 384, 35), string.Empty))
                    {
                        PlaySound();
                        POStatisticsManager.instance.SetPosition(window.x + 400, window.y + 342);
                        POStatisticsManager.instance.RefreshCounters();
                        POStatisticsManager.instance.showWindow = !POStatisticsManager.instance.showWindow;
                    }
                    GUI.Label(new Rect(12, 344, 30, 30), ProceduralObjectsMod.SelectionModeIcons[6]);
                    GUI.Label(new Rect(46, 346, 320, 25), "<size=15>" + LocalizationManager.instance.current["stats"] + "</size>");
                        */
                    #endregion
                    if (selectedGroup != null)
                    {
                        GUIUtils.DrawSeparator(new Vector2(6, addedHeight), 385);

                        if (selectingNewGrpRoot)
                        {
                            GUI.Label(new Rect(8, 4 + addedHeight, 188, 33), LocalizationManager.instance.current["CRG_desc"]);
                            if (GUI.Button(new Rect(200, 4 + addedHeight, 190, 30), LocalizationManager.instance.current["cancel"]))
                            {
                                PlaySound();
                                selectingNewGrpRoot = false;
                            }
                        }
                        else
                        {
                            if (GUI.Button(new Rect(6, 4 + addedHeight, 190, 30), "◄ " + LocalizationManager.instance.current["leave_group"]))
                            {
                                PlaySound();
                                selectedGroup = null;
                                pObjSelection.Clear();
                            }

                            if (GUI.Button(new Rect(200, 4 + addedHeight, 190, 30), LocalizationManager.instance.current["choose_grp_root"]))
                            {
                                PlaySound();
                                SelectionModeAction.CloseAction();
                                // alignHeightObj.Clear();
                                pObjSelection.Clear();
                                selectingNewGrpRoot = true;
                            }
                        }
                        addedHeight += 38;
                    }
                    _winHeight = addedHeight;
                }
                else
                {
                    if (chosenProceduralInfo.infoType == "PROP")
                        GUI.Label(new Rect(10, 30, 350, 39), LocalizationManager.instance.current["choose_tex_to_apply"] + " \"" + chosenProceduralInfo.propPrefab.GetLocalizedTitle() + "\"");
                    else if (chosenProceduralInfo.infoType == "BUILDING")
                        GUI.Label(new Rect(10, 30, 350, 39), LocalizationManager.instance.current["choose_tex_to_apply"] + " \"" + chosenProceduralInfo.buildingPrefab.GetLocalizedTitle() + "\"");
                    // Texture selection
                    Texture tex = null;
                    if (GUIUtils.TextureSelector(new Rect(10, 70, 380, 320), ref scrollTextures, out tex))
                    {
                        editingVertex = false;
                        editingVertexIndex.Clear();
                        editingWholeModel = false;
                        proceduralTool = false;
                        ToolHelper.FullySetTool<DefaultTool>();
                        Gizmos.DestroyGizmo();
                        SpawnObject(chosenProceduralInfo, tex);
                        tempVerticesBuffer = Vertex.CreateVertexList(currentlyEditingObject);
                        ToolHelper.FullySetTool<ProceduralTool>();
                        proceduralTool = true;
                        movingWholeModel = true;
                        toolAction = ToolAction.build;
                        placingSelection = false;
                        editingVertex = false;
                        chosenProceduralInfo = null;
                        TextureManager.instance.MinimizeAll();
                    }
                }
            }
        }

        public void DrawExternalsWindow(int id)
        {
            GUI.DragWindow(new Rect(0, 0, 348, 30));
            if (GUIUtils.CloseHelpButtons(externalsWindow, "Exported_Objects"))
                CloseExternalsWindow();

            GUI.Label(new Rect(10, 30, 298, 37), LocalizationManager.instance.current["externals_desc"]);
            if (renamingExternal == -1)
            {
                if (GUI.Button(new Rect(310, 35, 85, 28), LocalizationManager.instance.current["refresh"]))
                    ExPObjManager.LoadExternals(textManager.fontManager);
            }
            if (ExPObjManager.m_externals.Count == 0)
            {
                GUI.Box(new Rect(10, 70, 380, 320), LocalizationManager.instance.current["no_externals_warning"]);
            }
            else
            {
                GUI.Box(new Rect(10, 70, 380, 320), string.Empty);
                scrollExternals = GUI.BeginScrollView(new Rect(10, 70, 380, 320), scrollExternals, new Rect(0, 0, 358, 40 * ExPObjManager.m_externals.Count + 5));
                for (int i = 0; i < ExPObjManager.m_externals.Count; i++)
                {
                    GUI.Box(new Rect(5, i * 40 + 2, 352, 36), string.Empty);
                    if (renamingExternal == i)
                    {
                        renamingExternalString = GUI.TextField(new Rect(8, i * 40 + 6, 249, 36), renamingExternalString);
                    }
                    else
                    {
                        var external = ExPObjManager.m_externals[i];
                        GUI.Label(new Rect(8, i * 40 + 6, 180, 36), external.m_name);
                        if (GUI.Button(new Rect(190, i * 40 + 5, 67, 30), LocalizationManager.instance.current[external.isStatic ? "import": "place"]))
                        {
                            PlaySound();
                            if (external.isStatic)
                            {
                                GUIUtils.ShowModal(LocalizationManager.instance.current["import"], LocalizationManager.instance.current["import_desc"], (bool ok) =>
                                {
                                    if (ok)
                                    {
                                        pObjSelection.Clear();
                                        external.CreateClipboard();
                                        foreach (var cache in ((external.m_externalType == ClipboardProceduralObjects.ClipboardType.Single) ? new List<CacheProceduralObject>() { external.m_object } : external.m_selection.selection_objects.Keys.ToList()))
                                        {
                                            var obj = PlaceCacheObject(cache, false);
                                            obj.m_position = cache._staticPos;
                                            pObjSelection.Add(obj);
                                        }
                                        movingWholeModel = false;
                                        toolAction = ToolAction.none;
                                        placingSelection = false;
                                        proceduralTool = false;
                                    }
                                });
                            }
                            else
                            {
                                external.CreateClipboard();
                                if (external.m_externalType == ClipboardProceduralObjects.ClipboardType.Single)
                                    PlaceCacheObject(external.m_object, true);
                                else
                                    Paste(external.m_selection);
                            }
                        }
                    }
                    if (ExPObjManager.m_externals[i].isWorkshop)
                        GUI.Label(new Rect(258, i * 40 + 5, 67, 30), "[<i>Workshop</i>]", GUI.skin.button);
                    else
                    {
                        if (GUI.Button(new Rect(258, i * 40 + 5, 70, 30), LocalizationManager.instance.current[(renamingExternal == i) ? "ok" : "rename"]))
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
                        }
                        if (renamingExternal != i)
                        {
                            GUI.color = Color.red;
                            if (GUI.Button(new Rect(330, i * 40 + 5, 25, 30), "X"))
                            {
                                PlaySound();
                                ExPObjManager.DeleteExternal(ExPObjManager.m_externals[i]);
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
            List<Vector3> posArray = new List<Vector3>(tempVerticesBuffer.GetPositionsArray());
            // sets mesh renderer vertices
            currentlyEditingObject.m_mesh.SetVertices(posArray);
            currentlyEditingObject.RecalculateBoundsNormalsExtras(currentlyEditingObject.meshStatus);

            // call Modules' OnApplyModelChange()
            if (currentlyEditingObject.m_modules != null)
            {
                if (currentlyEditingObject.m_modules.Count > 0)
                {
                    foreach (var m in currentlyEditingObject.m_modules)
                    {
                        try { m.OnApplyModelChange(tempVerticesBuffer); }
                        catch (Exception e) { Debug.LogError("[ProceduralObjects] Error inside module OnApplyModelChange() method!\n" + e); }
                    }
                }
            }

            // UV map recalculation
            if (currentlyEditingObject.RequiresUVRecalculation && !currentlyEditingObject.disableRecalculation)
            {
                try { currentlyEditingObject.m_mesh.uv = Vertex.RecalculateUVMap(currentlyEditingObject, tempVerticesBuffer); }
                catch { Debug.LogError("[ProceduralObjects] Error : Couldn't recalculate UV map on a procedural object of type " + currentlyEditingObject.basePrefabName + " (" + currentlyEditingObject.baseInfoType + ")"); }
            }

            // render distance calculation
            currentlyEditingObject.renderDistance = RenderOptions.instance.CalculateRenderDistance(currentlyEditingObject, false);
        }
        public ProceduralObject SpawnObject(ProceduralInfo infoBase, Texture customTex = null)
        {
            var v = new ProceduralObject();
            if (infoBase.infoType == "PROP")
                v.ConstructObject(infoBase.propPrefab, proceduralObjects.GetNextUnusedId(), customTex);
            else if (infoBase.infoType == "BUILDING")
                v.ConstructObject(infoBase.buildingPrefab, proceduralObjects.GetNextUnusedId(), customTex);
            proceduralObjects.Add(v);
            SetCurrentlyEditingObj(v);
            return v;
        }
        public ProceduralObject PlaceCacheObject(CacheProceduralObject cacheObj, bool setCurrentlyEditing)
        {
            ToolHelper.FullySetTool<ProceduralTool>();
            ToolsModifierControl.mainToolbar.CloseEverything();
            var obj = new ProceduralObject(cacheObj, proceduralObjects.GetNextUnusedId(), ToolsModifierControl.cameraController.m_currentPosition + new Vector3(0, -8, 0), layerManager);
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
            obj.RecalculateBoundsNormalsExtras(obj.meshStatus);
            if (obj.meshStatus != 1)
            {
                if (obj.RequiresUVRecalculation && !obj.disableRecalculation)
                {
                    if (obj == currentlyEditingObject)
                        obj.m_mesh.uv = Vertex.RecalculateUVMap(obj, tempVerticesBuffer);
                    else
                        obj.m_mesh.uv = Vertex.RecalculateUVMap(obj, Vertex.CreateVertexList(obj));
                }
            }
            return obj;
        }
        public ProceduralObject CloneObject(ProceduralObject source)
        {
            var obj = new ProceduralObject()
            {
                id = proceduralObjects.GetNextUnusedId(),
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
                flipFaces = source.flipFaces,
                normalsRecalcMode = source.normalsRecalcMode,
                m_color = source.m_color
            };
            if (source.meshStatus == 1 && source.baseInfoType == "PROP")
            {
                obj.m_mesh = source._baseProp.m_mesh;
                obj.meshStatus = 1;
            }
            else
            {
                obj.m_mesh = source.m_mesh.InstantiateMesh();
                obj.meshStatus = 2;
            }
            if (obj.isPloppableAsphalt)
                obj.m_correctedMeshPloppableAsph = source.m_correctedMeshPloppableAsph.InstantiateMesh();

            obj.m_material.color = obj.m_color;
            obj.allVertices = obj.m_mesh.vertices;
            obj.historyEditionBuffer = new HistoryBuffer(obj);
            obj.m_modules = (source.m_modules == null) ? new List<POModule>() : ModuleManager.instance.CloneModuleList(source.m_modules, obj);
            proceduralObjects.Add(obj);
            return obj;
        }
        public void SwitchToMainTool(byte mode = 0)
        {
            editingVertexIndex.Clear();
            if (mode == 0)
            {
                actionMode = 0;
                Gizmos.CreatePositionGizmo(currentlyEditingObject.m_position, true);
            }
            else if (mode == 1)
            {
                actionMode = 1;
                Gizmos.CreateScaleGizmo(currentlyEditingObject.m_position, true);
            }
            else if (mode == 2)
            {
                actionMode = 2;
                Gizmos.CreateRotationGizmo(currentlyEditingObject.m_position, true);
            }
            editingVertex = false;
            editingWholeModel = true;
        }
        public void SwitchActionMode()
        {
            switch (actionMode)
            {
                case 1:
                    Gizmos.CreateRotationGizmo(currentlyEditingObject.m_position, true);
                    actionMode = 2;
                    return;
                case 2:
                    actionMode = 0;
                    Gizmos.CreatePositionGizmo(currentlyEditingObject.m_position, true);
                    return;
                default: // this is really case 0 - 'default' is a safety measure
                    actionMode = 1;
                    Gizmos.CreateScaleGizmo(currentlyEditingObject.m_position, true);
                    return;
            }
        }
        private void EditModeBack()
        {
            textManager.CloseWindow();
            advEdManager = null;
            moduleManager.CloseWindow();
            CloseExternalsWindow();
            toolAction = ToolAction.none;
            ToolHelper.FullySetTool<ProceduralTool>();
            Gizmos.DestroyGizmo();
            editingVertex = false;
            editingVertexIndex.Clear();
            editingWholeModel = false;
            proceduralTool = false;
            chosenProceduralInfo = null;
            pObjSelection.Clear();
            pObjSelection.Add(currentlyEditingObject);
            SetCurrentlyEditingObj(null);
        }
        public void MainButtonClick()
        {
            var currentToolType = ToolsModifierControl.toolController.CurrentTool.GetType();
            ResetLayerScrollmenu();
            textManager.CloseWindow();
            advEdManager = null;
            CloseExternalsWindow();
            SelectionModeAction.CloseAction();
            editingVertex = false;
            editingVertexIndex.Clear();
            editingWholeModel = false;
            proceduralTool = false;
            SetCurrentlyEditingObj(null);
            chosenProceduralInfo = null;
            rotWizardData = null;
            Gizmos.DestroyGizmo();

            if (currentToolType == typeof(PropTool) || currentToolType == typeof(BuildingTool))
            {
                ColossalFramework.UI.UIView.SetFocus(mainButton);
                CallConvertToPO(ToolsModifierControl.toolController.CurrentTool);
            }
            else if (currentToolType != typeof(ProceduralTool))
            {
                ColossalFramework.UI.UIView.SetFocus(mainButton);
                ToolHelper.FullySetTool<ProceduralTool>();
                ToolsModifierControl.mainToolbar.CloseEverything();
            }
            else
            {
                ToolHelper.FullySetTool<DefaultTool>();
            }
        }
        public void EscapePressed()
        {
            if (proceduralTool && !movingWholeModel && currentlyEditingObject != null) // leave edit mode
            { 
                EditModeBack();
                return;
            }
            else if (ToolsModifierControl.toolController.CurrentTool is ProceduralTool && movingWholeModel && currentlyEditingObject != null) // cancel move to
            {
                if (currentlyEditingObject.historyEditionBuffer.currentStepType == EditingStep.StepType.moveTo)
                {
                    currentlyEditingObject.m_position = currentlyEditingObject.historyEditionBuffer.prevTempPos;
                    currentlyEditingObject.m_rotation = currentlyEditingObject.historyEditionBuffer.prevTempRot;
                    if (placingSelection && currentlyEditingObject.tempObj != null)
                    {
                        currentlyEditingObject.tempObj.transform.position = currentlyEditingObject.historyEditionBuffer.prevTempPos;
                        currentlyEditingObject.tempObj.transform.rotation = currentlyEditingObject.historyEditionBuffer.prevTempRot;
                        for (int i = 0; i < currentlyEditingObject.tempObj.transform.childCount; i++)
                        {
                            int id;
                            if (int.TryParse(currentlyEditingObject.tempObj.transform.GetChild(i).gameObject.name, out id))
                            {
                                var po = proceduralObjects.GetObjectWithId(id);
                                if (po != null)
                                {
                                    po.m_position = po.tempObj.transform.position;
                                    po.m_rotation = po.tempObj.transform.rotation;
                                }
                            }
                        }
                    }
                    ConfirmMovingWhole(getBackToGeneralTool);
                }
                else
                {
                    List<ProceduralObject> delete = new List<ProceduralObject>();
                    delete.Add(currentlyEditingObject);
                    if (placingSelection && currentlyEditingObject.tempObj != null)
                    {
                        for (int i = 0; i < currentlyEditingObject.tempObj.transform.childCount; i++)
                        {
                            int id;
                            if (int.TryParse(currentlyEditingObject.tempObj.transform.GetChild(i).gameObject.name, out id))
                            {
                                var po = proceduralObjects.GetObjectWithId(id);
                                if (po != null)
                                    delete.Add(po);
                            }
                        }
                    }
                    var inclusiveList = (selectedGroup == null) ? POGroup.AllObjectsInSelection(delete, selectedGroup) : delete;
                    for (int i = 0; i < inclusiveList.Count; i++)
                    {
                        var po = inclusiveList[i];
                        if (po.tempObj != null)
                            UnityEngine.Object.Destroy(po.tempObj);
                        if (po.group != null)
                            po.group.Remove(this, po);
                        moduleManager.DeleteAllModules(po);
                        proceduralObjects.Remove(po);
                        activeIds.Remove(po.id);
                    }
                    ConfirmMovingWhole(false);
                }
            }
            else if (selectionModeAction != null && !proceduralTool && chosenProceduralInfo == null && pObjSelection != null)
            {
                SelectionModeAction.CloseAction();
                return;
            }
            else if (ToolsModifierControl.toolController.CurrentTool is ProceduralTool && !proceduralTool && pObjSelection.Count > 0) // clear selection
            {
                pObjSelection.Clear();
                return;
            }
            else if (!proceduralTool && selectedGroup != null) // leave group
            {
                selectedGroup = null;
                return;
            }
            else if (!proceduralTool && chosenProceduralInfo != null) // cancel texture selection
            {
                chosenProceduralInfo = null;
                return;
            }
            else
                ClosePO();
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
                string prevText = ConfirmNoButton.text;
                ConfirmNoButton.isVisible = false;
                GUIUtils.ShowModal(LocalizationManager.instance.current["incompatibleAssetPopup_title"], LocalizationManager.instance.current["incompatibleAssetPopup_desc"], (bool ok) =>
                {
                    ConfirmNoButton.isVisible = true;
                });
            }
        }
        private void CloseAllSMWindows()
        {
            CloseExternalsWindow();
            layerManager.showWindow = false;
            TextureManager.instance.showWindow = false;
            FontManager.instance.showWindow = false;
            ModuleManager.instance.showManagerWindow = false;
            RenderOptions.instance.showWindow = false;
            POStatisticsManager.instance.showWindow = false;
        }
        private void CloseExternalsWindow()
        {
            showExternals = false;
            renamingExternal = -1;
            renamingExternalString = "";
        }
        private void ShowAdvEditionTools()
        {
            PlaySound();
            if (advEdManager != null)
                advEdManager.showWindow = !advEdManager.showWindow;
            else
            {
                advEdManager = new AdvancedEditionManager(currentlyEditingObject, Undo, Redo, Apply);
                advEdManager.showWindow = true;
            }
        }
        private void ResetLayerScrollmenu()
        {
            showLayerSetScroll = false;
            scrollLayerSet = Vector2.zero;
            showMoreTools = false;
        }
        private void ConvertToProcedural(ToolBase tool)
        {
            CloseAllSMWindows();
            if (availableProceduralInfos == null)
                availableProceduralInfos = ProceduralUtils.CreateProceduralInfosList();
            if (availableProceduralInfos.Count == 0)
                availableProceduralInfos = ProceduralUtils.CreateProceduralInfosList();

            if (tool.GetType() == typeof(PropTool))
            {
                ProceduralInfo info = availableProceduralInfos.Where(pInf => pInf.propPrefab != null).FirstOrDefault(pInf => pInf.propPrefab == ((PropTool)tool).m_prefab);
                var angle = ((PropTool)tool).m_angle;
                ToolsModifierControl.mainToolbar.CloseEverything();
                filters.EnableAll();
                if (info.isBasicShape)
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
                    SpawnObject(info).m_rotation = Quaternion.Euler(0, angle % 360f, 0);
                    //  tempVerticesBuffer = Vertex.CreateVertexList(currentlyEditingObject);
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
                var angle = ((BuildingTool)tool).m_angle;
                ToolsModifierControl.mainToolbar.CloseEverything();
                filters.EnableAll();
                if (info.isBasicShape)
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
                    SpawnObject(info).m_rotation = Quaternion.Euler(0, angle % 360f, 0);
                    // tempVerticesBuffer = Vertex.CreateVertexList(currentlyEditingObject);
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
                    var obj = PlaceCacheObject(clipboard.single_object, true);
                    if (selectedGroup != null)
                        selectedGroup.AddToGroup(obj);
                }
                else
                {
                    pObjSelection.Clear();
                    moveToSelection = new Dictionary<ProceduralObject, Transform>();
                    var created = new Dictionary<CacheProceduralObject, ProceduralObject>();
                    for (int i = 0; i < clipboard.selection_objects.Count; i++)
                    {
                        var cache = clipboard.selection_objects.ToList()[i].Key;
                        ProceduralObject obj = null;
                        if (i == 0)
                        {
                            obj = PlaceCacheObject(cache, true);
                            created.Add(cache, obj);
                            obj.tempObj = new GameObject(obj.id.ToString());
                            obj.tempObj.transform.position = obj.m_position;
                            obj.tempObj.transform.rotation = obj.m_rotation;
                        }
                        else
                        {
                            obj = PlaceCacheObject(cache, false);
                            created.Add(cache, obj);
                            obj.m_position = currentlyEditingObject.m_position + clipboard.selection_objects[cache];
                            obj.tempObj = new GameObject(obj.id.ToString());
                            obj.tempObj.transform.position = obj.m_position;
                            obj.tempObj.transform.rotation = obj.m_rotation;
                            obj.tempObj.transform.SetParent(currentlyEditingObject.tempObj.transform, true);
                        }
                        moveToSelection.Add(obj, obj.tempObj.transform);
                        if (selectedGroup != null)
                            selectedGroup.AddToGroup(obj);
                    }
                    clipboard.RecreateGroups(created);
                    placingSelection = true;
                }
            }
        }
        private void MoveSelection(List<ProceduralObject> objects, bool registerHistory)
        {
            moveToSelection = new Dictionary<ProceduralObject, Transform>();
            for (int i = 0; i < objects.Count; i++)
            {
                ProceduralObject obj = objects[i];
                if (registerHistory)
                    obj.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.moveTo, tempVerticesBuffer);
                if (i == 0)
                {
                    SetCurrentlyEditingObj(obj);
                    obj.tempObj = new GameObject(obj.id.ToString());
                    obj.tempObj.transform.position = obj.m_position;
                    obj.tempObj.transform.rotation = obj.m_rotation;
                }
                else
                {
                    obj.tempObj = new GameObject(obj.id.ToString());
                    obj.tempObj.transform.position = obj.m_position;
                    obj.tempObj.transform.rotation = obj.m_rotation;
                    obj.tempObj.transform.SetParent(currentlyEditingObject.tempObj.transform, true);
                }
                moveToSelection.Add(obj, obj.tempObj.transform);
            }
        }
        private void ConfirmMovingWhole(bool backToGeneralTool)
        {
            Vector3 effectPos = currentlyEditingObject.m_position;
            editingVertex = false;
            editingVertexIndex.Clear();
            if (placingSelection)
            {
                pObjSelection.Clear();
                Singleton<EffectManager>.instance.DispatchEffect(Singleton<BuildingManager>.instance.m_properties.m_placementEffect,
                    new EffectInfo.SpawnArea(currentlyEditingObject.m_position, Vector3.up, 10f), Vector3.zero, 0f, 1f, Singleton<AudioManager>.instance.DefaultGroup, 0u, true);
                for (int i = 0; i < proceduralObjects.Count; i++)
                {
                    proceduralObjects[i].historyEditionBuffer.ConfirmNewStep(tempVerticesBuffer);
                    if (proceduralObjects[i] == currentlyEditingObject)
                        continue;
                    if (proceduralObjects[i].tempObj == null)
                        continue;
                    if (proceduralObjects[i].tempObj.transform.parent == currentlyEditingObject.tempObj.transform)
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
                placingSelection = false;
            }
            else
            {
                currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(tempVerticesBuffer);
                pObjSelection.Clear();
                Singleton<EffectManager>.instance.DispatchEffect(Singleton<BuildingManager>.instance.m_properties.m_placementEffect,
                    new EffectInfo.SpawnArea(currentlyEditingObject.m_position, Vector3.up, 10f), Vector3.zero, 0f, 1f, Singleton<AudioManager>.instance.DefaultGroup, 0u, true);
                // StoreLineComponents(Gizmos.CreateGizmo(currentlyEditingObject.m_position, true));
            }
            ToolHelper.FullySetTool<ProceduralTool>();
            chosenProceduralInfo = null;
            movingWholeModel = false;
            moveToSelection = null;
            CloseExternalsWindow();
            rotWizardData = null;
            yOffset = 0f;
            toolAction = ToolAction.none;
            movingWholeRaycast = Vector3.zero;
            getBackToGeneralTool = false;
            if (backToGeneralTool)
            {
                SwitchToMainTool(actionMode);
            }
            else
            {
                editingWholeModel = false;
                SetCurrentlyEditingObj(null);
                proceduralTool = false;
            }
        }
        public bool IsSettingPosition()
        {
            return (Gizmos.Exists && axisState != AxisEditionState.none) || movingWholeModel;
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
                moduleManager.DeleteAllModules(currentlyEditingObject);
                proceduralObjects.Remove(currentlyEditingObject);
                activeIds.Remove(currentlyEditingObject.id);
                if (currentlyEditingObject.group != null)
                    currentlyEditingObject.group.Remove(this, currentlyEditingObject);
                //  Object.Destroy(currentlyEditingObject.gameObject);
                SetCurrentlyEditingObj(null);
                Gizmos.DestroyGizmo();
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
                PlaySound(1);
                GUIUtils.ShowModal(LocalizationManager.instance.current["confirmDeletionPopup_title"],
                    involvedCount > 1 ? string.Format(LocalizationManager.instance.current["confirmDeletionPopup_descSelection"], involvedCount) : LocalizationManager.instance.current["confirmDeletionPopup_descSingle"],
                    (bool ok) =>
                    {
                        if (ok)
                        {
                            a.Invoke();
                            Singleton<EffectManager>.instance.DispatchEffect(Singleton<BuildingManager>.instance.m_properties.m_bulldozeEffect,
                                new EffectInfo.SpawnArea(position, Vector3.up, 10f), Vector3.zero, 0f, 1f, Singleton<AudioManager>.instance.DefaultGroup, 0u, true);
                        }
                        else
                            toolAction = prevToolAction;
                    });
            }

        }
        private void ClosePO()
        {
            ResetLayerScrollmenu();
            CloseExternalsWindow();
            advEdManager = null;
            fontManager.showWindow = false;
            moduleManager.CloseWindow();
            moduleManager.showManagerWindow = false;
            toolAction = ToolAction.none;
            editingVertex = false;
            editingVertexIndex.Clear();
            editingWholeModel = false;
            proceduralTool = false;
            SetCurrentlyEditingObj(null);
            chosenProceduralInfo = null;
            if (vertWizardData != null)
                vertWizardData.DestroyLines();
            selectedGroup = null;
            selectingNewGrpRoot = false;
            vertWizardData = null;
            pObjSelection.Clear();
            SelectionModeAction.CloseAction();
            tabSwitchTimer = 0;
            getBackToGeneralTool = false;
            TextureManager.instance.showWindow = false;
            TextureManager.instance.MinimizeAll();
            RenderOptions.instance.showWindow = false;
            POStatisticsManager.instance.showWindow = false;
            Gizmos.DestroyGizmo();
            GUIUtils.SetMouseScroll(true);
            ColossalFramework.UI.UIView.SetFocus(null);
            if (FindObjectOfType<ToolController>().CurrentTool is ProceduralTool)
            {
                ToolHelper.FullySetTool<DefaultTool>();
            }
            SingleHoveredObj = null;
        }
        public bool IsInWindowElement(Vector2 pos, bool alsoIsInDropdowns = false)
        {
            if (window.Contains(pos))
                return true;
            if (showExternals)
            {
                if (externalsWindow.Contains(pos)) return true;
            }
            if (layerManager.showWindow)
            {
                if (layerManager.winRect.Contains(pos)) return true;
            }
            if (ModuleManager.instance.showManagerWindow)
            {
                if (ModuleManager.instance.managerWindow.Contains(pos)) return true;
            }
            if (ModuleManager.instance.showWindow)
            {
                if (ModuleManager.instance.windowRect.Contains(pos)) return true;
            }
            if (FontManager.instance.showWindow)
            {
                if (FontManager.instance.window.Contains(pos)) return true;
            }
            if (POStatisticsManager.instance.showWindow)
            {
                if (POStatisticsManager.instance.window.Contains(pos)) return true;
            }
            if (RenderOptions.instance.showWindow)
            {
                if (RenderOptions.instance.window.Contains(pos)) return true;
            }
            if (TextureManager.instance.showWindow)
            {
                if (TextureManager.instance.winrect.Contains(pos)) return true;
            }
            if (textManager != null)
            {
                if (textManager.showWindow) 
                { 
                    if (textManager.windowRect.Contains(pos)) 
                        return true; 
                    if (textManager.colorPickerSelected != null)
                    {
                        if (textManager.colorPickerSelected.pickerRect.Contains(pos))
                            return true;
                    }
                }
                if (textManager.selectedCharTable != null) { if (textManager.charTableRect.Contains(pos)) return true; }
            }
            if (advEdManager != null)
            {
                if (advEdManager.showWindow) { if (advEdManager.winRect.Contains(pos)) return true; }
            }
            if (!alsoIsInDropdowns)
                return false;
            if (pObjSelection.Count == 0)
                return false;

            var objPos = pObjSelection[0].m_position.WorldToGuiPoint();
            if (IsInWindowElement(objPos, false))
                return false;

            if (selectionModeAction != null)
            {
                if (selectionModeAction.CollisionUI(objPos + new Vector2(12, -11)).Contains(pos))
                    return true;
                return false;
            }

            float addedHeight = (pObjSelection.Count == 1 ? (pObjSelection[0].isRootOfGroup && selectedGroup == null ? 23 : 0) : 23);
            Rect dropdown = new Rect(objPos.x + 11, objPos.y - 11, 132, 145 + addedHeight);
            if (dropdown.Contains(pos))
                return true;
            if (painter != null)
            {
                if (painter.sampleRect.Contains(pos))
                    return true;
                if (painter.showPicker)
                {
                    if (painter.pickerRect.Contains(pos))
                        return true;
                }
            }
            if (showMoreTools)
            {
                if (new Rect(objPos + new Vector2(144, 81 + addedHeight), SelectionMode.SelectionModeAction.ActionsSize()).Contains(pos))
                    return true;
            }
            if (showLayerSetScroll)
            {
                if (new Rect(objPos + new Vector2(144, 58 + addedHeight), new Vector2(150, 160)).Contains(pos))
                    return true;
            }
            return false;
        }
        private void SetCurrentlyEditingObj(ProceduralObject obj)
        {
            currentlyEditingObject = obj;
        }
        private void SelectSetupLocalization()
        {
            ProceduralObjectsMod.LanguageUsed.value = "default";
            LocalizationManager.instance.SelectCurrent();
            SetupLocalizationInternally();
        }
        public void SetupLocalizationInternally()
        {
            externalsSaveTextfield = LocalizationManager.instance.current["enter_name"];
            layerManager.UpdateLocalization();
            ProceduralTool.SetupControlsStrings();
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
