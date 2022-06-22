using System.Collections.Generic;
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

        public Vertex[] tempVerticesBuffer; // TO BE REMOVED - it is assume this will fix Move It compatibility

        public List<ProceduralObject> proceduralObjects, pObjSelection;
        public HashSet<int> activeIds;
        public ProceduralObject currentlyEditingObject;
        public List<ProceduralInfo> availableProceduralInfos;
        public List<POGroup> groups;
        public POGroup selectedGroup;
        public ProceduralInfo chosenProceduralInfo = null;
        public uint failedToLoadObjects;
        public double loadingTime;

        public ClipboardProceduralObjects clipboard = null;
        public float storedHeight = 0f, yOffset = 0f;

        public bool proceduralTool = false, editingVertex = false, selectingNewGrpRoot = false, movingWholeModel = false, placingSelection = false, editingWholeModel = false, generalShowUI = true, showExternals = false, getBackToGeneralTool = false, reselectingTex = false;
        public List<int> editingVertexIndex;
        public Rect window = new Rect(155, 100, 400, 400);
        public float _winHeight;
        public Rect externalsWindow = new Rect(555, 100, 400, 400);
        public string externalsSaveTextfield = "Enter object name here";
        public Vector2 scrollVertices = Vector2.zero, scrollObjects = Vector2.zero, scrollTextures = Vector2.zero, scrollExternals = Vector2.zero;
        public Type previousToolType;
        public static AxisEditionState axisState = AxisEditionState.none;
        public Vector3 axisHitPoint = Vector3.zero, gizmoOffset = Vector3.zero;
        private RotationWizardData rotWizardData = null;
        private VerticesWizardData vertWizardData = null;
        public DrawWizardData drawWizardData = null;
        public ProceduralObject SingleHoveredObj = null;
        public SelectionModeAction selectionModeAction = null;

        public static ToolAction toolAction = ToolAction.none;
        public static Vector3 movingWholeRaycast = Vector3.zero;
        private Dictionary<ProceduralObject, Transform> moveToSelection;

        public static byte verticesToolType = 0;
        public static float tabSwitchTimer = 0f;

        public Camera renderCamera;
        private Material redOverlayMat, purpleOverlayMat;
        public Color uiColor;
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
        public MeasurementsManager measurementsManager;
        public SelectionFilters filters;

        public bool showLayerSetScroll = false, showMoreTools = false;
        private Vector2 scrollLayerSet = Vector2.zero;

        private static AudioClip[] audiosClips;

        void Start()
        {
            Debug.Log("[ProceduralObjects] v" + ProceduralObjectsMod.VERSION + " - Game start procedure started.");
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
                    ClosePO(true);
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
            redOverlayMat = new Material(Shader.Find("Sprites/Default"));
            purpleOverlayMat = new Material(Shader.Find("Sprites/Default"));
            redOverlayMat.color = new Color(1f, 0.17f, 0.17f, .24f);
            purpleOverlayMat.color = new Color32(225, 130, 240, 62);
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
            measurementsManager = new MeasurementsManager(this);
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
                groups = new List<POGroup>();
                activeIds = new HashSet<int>();
            }
            new POStatisticsManager(this);
            ProceduralTool.CreateCursors();
            // CT default actions
            new POToolAction("recenterObjOrigin", TextureUtils.LoadTextureFromAssembly("CTA_recenterOrigin"), POActionType.Global, null, ProceduralUtils.RecenterObjOrigin);
            new POToolAction("invertSelection", TextureUtils.LoadTextureFromAssembly("CTA_invertVertexSelection"), POActionType.Selection, ProceduralUtils.InvertSelection, null);
            new POToolAction("splitVertex", TextureUtils.LoadTextureFromAssembly("CTA_splitVertex"), POActionType.SingleSelAtLeast, VertexUtils.SplitSelectedVertex, null);
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
            loadingTime = Math.Round((DateTime.Now - startTime).TotalSeconds, 2);
            Debug.Log("[ProceduralObjects] Game start procedure ended in " + loadingTime + " seconds");
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
                        {
                            po.ConstructObject(req.buildingInfo, proceduralObjects.GetNextUnusedId(), null);
                            if (ProceduralObjectsMod.IncludeSubBuildings.value)
                            {
                                if (po._baseBuilding != null)
                                {
                                    if (po._baseBuilding.m_subBuildings.Length >= 1)
                                        ProceduralUtils.ConstructSubBuildings(po);
                                }
                            }
                        }
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

            layerManager.Update();

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
                ColossalFramework.UI.UIView.SetFocus(mainButton);
                CallConvertToPO(ToolsModifierControl.toolController.CurrentTool);
            }

            if (proceduralObjects != null)
            {
                var sqrDynMinThreshold = ProceduralObjectsMod.DynamicRDMinThreshold.value * ProceduralObjectsMod.DynamicRDMinThreshold.value;
                bool isNightTime = Singleton<SimulationManager>.instance.m_isNightTime;
                for (int i = 0; i < proceduralObjects.Count; i++)
                {
                    var obj = proceduralObjects[i];
                    bool infiniteDist = obj.renderDistance >= 16001;
                    float sqrRd = 0;
                    if (infiniteDist)
                    {
                        obj._insideRenderView = true;
                    }
                    else
                    {
                        sqrRd = (obj.renderDistance * RenderOptions.instance.globalMultiplier);
                        sqrRd *= sqrRd;
                        obj._insideRenderView = obj._squareDistToCam <= sqrRd;
                    }
                    obj._squareDistToCam = (renderCamera.transform.position - obj.m_position).sqrMagnitude;

                    if (obj._insideRenderView)
                    {
                        if (renderCamera.WorldToScreenPoint(obj.m_position).z >= 0)
                            obj._insideUIview = infiniteDist ? true : (obj._squareDistToCam <= Mathf.Max(sqrRd * 0.7f, sqrDynMinThreshold));
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
                            if (RenderOptions.instance.CanRenderSingle(obj, isNightTime))
                                Graphics.DrawMesh(obj.m_mesh, obj.m_position, obj.m_rotation, obj.m_material, 0, null, 0, null, !obj.disableCastShadows, true);

                            if (SingleHoveredObj == obj || (selectedGroup == null ? (obj.group == null ? false : obj.group.root == SingleHoveredObj) : false))
                                Graphics.DrawMesh(obj.overlayRenderMesh, obj.m_position, obj.m_rotation, 
                                    selectedGroup == null ? (SingleHoveredObj.isRootOfGroup ? redOverlayMat : purpleOverlayMat) : purpleOverlayMat,
                                    0, null, 0, null, false, false);
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
                if (generalShowUI)
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
                            if (selectionModeAction == null && !movingWholeModel)
                            {
                                foreach (SMActionPrefab actionprefab in SelectionModeAction.allActions)
                                {
                                    if (actionprefab.keyBinding == null) continue;
                                    if (actionprefab.keyBinding.GetBindingDown())
                                    {
                                        var action = (SelectionModeAction)Activator.CreateInstance(actionprefab.type);
                                        action.logic = ProceduralObjectsLogic.instance;
                                        selectionModeAction = action;
                                        action.OnOpen(new List<ProceduralObject>(pObjSelection));
                                    }
                                }

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
                }
                GUIUtils.SetMouseScroll(!IsInWindowElement(GUIUtils.MousePos, true));
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
                {
                    if (!movingWholeModel)
                        DeleteObject();
                }

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
                                    currentlyEditingObject.SetRotation(Quaternion.AngleAxis(((rotWizardData.GUIMousePositionX - Input.mousePosition.x) * 400f) / Screen.width, Vector3.up) * currentlyEditingObject.m_rotation);
                                    rotWizardData.UpdateMouseCoords();
                                }
                            }
                        }
                        else if (Input.GetMouseButtonUp(1))
                        {
                            if (rotWizardData.clickTime <= .14f)
                            {
                                currentlyEditingObject.SetRotation(Quaternion.AngleAxis(45, Vector3.up) * currentlyEditingObject.m_rotation);
                            }
                            rotWizardData = null;
                        }

                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveUp").GetBinding())
                            yOffset += TimeUtils.deltaTime * (KeyBindingsManager.instance.GetBindingFromName("edition_smallMovements").GetBinding() ? 1f : 8f);
                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveDown").GetBinding())
                            yOffset -= TimeUtils.deltaTime * (KeyBindingsManager.instance.GetBindingFromName("edition_smallMovements").GetBinding() ? 1f : 8f);
                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveForward").GetBinding())
                            currentlyEditingObject.SetRotation(currentlyEditingObject.m_rotation.Rotate(20f * TimeUtils.deltaTime, 0, 0));
                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveBackward").GetBinding())
                            currentlyEditingObject.SetRotation(currentlyEditingObject.m_rotation.Rotate(-20f * TimeUtils.deltaTime, 0, 0));
                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveLeft").GetBinding())
                            currentlyEditingObject.SetRotation(currentlyEditingObject.m_rotation.Rotate(0, 0, 20f * TimeUtils.deltaTime));
                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveRight").GetBinding())
                            currentlyEditingObject.SetRotation(currentlyEditingObject.m_rotation.Rotate(0, 0, -20f * TimeUtils.deltaTime));


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
                                    currentlyEditingObject.SetPosition(ray.GetPoint(enter));
                            }
                            else
                            {
                                ProceduralTool.RaycastOutput rayOutput;
                                if (ProceduralTool.TerrainRaycast(rayInput, out rayOutput))
                                {
                                    if (!rayOutput.m_currentEditObject)
                                    {
                                        movingWholeRaycast = rayOutput.m_hitPos;
                                        currentlyEditingObject.SetPosition(new Vector3(rayOutput.m_hitPos.x, rayOutput.m_hitPos.y + yOffset, rayOutput.m_hitPos.z));
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
                            currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(currentlyEditingObject.vertices);
                            Gizmos.recordingStretch = 0f;
                            axisState = AxisEditionState.none;
                        }
                    }
                    if (axisState == AxisEditionState.none)
                    {
                        Vector2 objGuiPosition = currentlyEditingObject.m_position.WorldToGuiPoint();
                        Rect toolsRect = new Rect(Gizmos.RightMostGUIPosGizmo + new Vector2(-1, -28), new Vector2(110, 88));
                        if (Input.GetMouseButtonDown(0))
                        {
                            if (!toolsRect.IsMouseInside() && !IsInWindowElement(GUIUtils.MousePos, true))
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
                                        currentlyEditingObject.historyEditionBuffer.InitializeNewStep(type, currentlyEditingObject.vertices);
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
                                            Gizmos.tempBuffer = currentlyEditingObject.vertices.GetPositionsArray();
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
                            if (actionMode == 1)
                            {
                                Gizmos.DestroyGizmo();
                                verticesToolType = 0; // move
                                editingWholeModel = false;
                                toolAction = ToolAction.vertices;
                                tabSwitchTimer = TimeUtils.deltaTime;
                            }
                            else if (actionMode == 0)
                            {
                                actionMode = 2;
                                Gizmos.CreateRotationGizmo(currentlyEditingObject.m_position, true);
                            }
                            else if (actionMode == 2)
                            {
                                actionMode = 1;
                                Gizmos.CreateScaleGizmo(currentlyEditingObject.m_position, true);
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
                                    float slowFactor = KeyBindingsManager.instance.GetBindingFromName("edition_smallMovements").GetBinding() ? 0.2f : 1f;

                                    if (actionMode == 0) // position gizmo
                                    {
                                        switch (axisState)
                                        {
                                            case AxisEditionState.X:
                                                if (p.Raycast(r, out enter))
                                                {
                                                    Vector3 hit = Vector3.Lerp(axisHitPoint, r.GetPoint(enter), slowFactor);
                                                    if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                    {
                                                        if (Gizmos.registeredFloat != 0)
                                                            currentlyEditingObject.SetPosition(currentlyEditingObject.historyEditionBuffer.prevTempPos + new Vector3(Gizmos.GetStoredDistanceValue, 0, 0));
                                                        else
                                                            currentlyEditingObject.SetPosition(Gizmos.SnapToPreviousMove(new Vector3(hit.x - gizmoOffset.x, currentlyEditingObject.m_position.y, currentlyEditingObject.m_position.z),
                                                                AxisEditionState.X, currentlyEditingObject));
                                                    }
                                                    else
                                                    {
                                                        if (Gizmos.registeredFloat != 0)
                                                            currentlyEditingObject.SetPosition(currentlyEditingObject.historyEditionBuffer.prevTempPos + (currentlyEditingObject.m_rotation * new Vector3(Gizmos.GetStoredDistanceValue, 0, 0)));
                                                        else
                                                            currentlyEditingObject.SetPosition(Gizmos.SnapToPreviousMove(Vector3.Project(hit - currentlyEditingObject.m_position, currentlyEditingObject.m_rotation * Vector3.right) + currentlyEditingObject.m_position - gizmoOffset,
                                                                AxisEditionState.X, currentlyEditingObject));
                                                    }
                                                }
                                                break;
                                            case AxisEditionState.Y:
                                                if (p.Raycast(r, out enter))
                                                {
                                                    Vector3 hit = Vector3.Lerp(axisHitPoint, r.GetPoint(enter), slowFactor);
                                                    if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                    {
                                                        if (Gizmos.registeredFloat != 0)
                                                            currentlyEditingObject.SetPosition(currentlyEditingObject.historyEditionBuffer.prevTempPos + new Vector3(0, Gizmos.GetStoredDistanceValue, 0));
                                                        else
                                                            currentlyEditingObject.SetPosition(Gizmos.SnapToPreviousMove(new Vector3(currentlyEditingObject.m_position.x, hit.y - gizmoOffset.y, currentlyEditingObject.m_position.z),
                                                                AxisEditionState.Y, currentlyEditingObject));
                                                    }
                                                    else
                                                    {
                                                        if (Gizmos.registeredFloat != 0)
                                                            currentlyEditingObject.SetPosition(currentlyEditingObject.historyEditionBuffer.prevTempPos + (currentlyEditingObject.m_rotation * new Vector3(0, Gizmos.GetStoredDistanceValue, 0)));
                                                        else
                                                            currentlyEditingObject.SetPosition(Gizmos.SnapToPreviousMove(Vector3.Project(hit - currentlyEditingObject.m_position, currentlyEditingObject.m_rotation * Vector3.up) + currentlyEditingObject.m_position - gizmoOffset,
                                                                AxisEditionState.Y, currentlyEditingObject));
                                                    }
                                                }
                                                break;
                                            case AxisEditionState.Z:
                                                if (p.Raycast(r, out enter))
                                                {
                                                    Vector3 hit = Vector3.Lerp(axisHitPoint, r.GetPoint(enter), slowFactor);
                                                    if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                    {
                                                        if (Gizmos.registeredFloat != 0)
                                                            currentlyEditingObject.SetPosition(currentlyEditingObject.historyEditionBuffer.prevTempPos + new Vector3(0, 0, Gizmos.GetStoredDistanceValue));
                                                        else
                                                            currentlyEditingObject.SetPosition(Gizmos.SnapToPreviousMove(new Vector3(currentlyEditingObject.m_position.x, currentlyEditingObject.m_position.y, hit.z - gizmoOffset.z),
                                                                AxisEditionState.Z, currentlyEditingObject));
                                                    }
                                                    else
                                                    {
                                                        if (Gizmos.registeredFloat != 0)
                                                            currentlyEditingObject.SetPosition(currentlyEditingObject.historyEditionBuffer.prevTempPos + (currentlyEditingObject.m_rotation * new Vector3(0, 0, Gizmos.GetStoredDistanceValue)));
                                                        else
                                                            currentlyEditingObject.SetPosition(Gizmos.SnapToPreviousMove(Vector3.Project(hit - currentlyEditingObject.m_position, currentlyEditingObject.m_rotation * Vector3.forward) + currentlyEditingObject.m_position - gizmoOffset,
                                                                AxisEditionState.Z, currentlyEditingObject));
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
                                                    currentlyEditingObject.m_rotation, currentlyEditingObject.m_material, 0, renderCamera, 0, null, !currentlyEditingObject.disableCastShadows, true);
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
                                                                currentlyEditingObject.m_rotation, currentlyEditingObject.m_material, 0, renderCamera, 0, null, !currentlyEditingObject.disableCastShadows, true);
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
                                                        stretch = Mathf.Lerp(1f, stretch, slowFactor);
                                                    }
                                                    Gizmos.recordingStretch = stretch;
                                                    VertexUtils.StretchX(Gizmos.tempBuffer, currentlyEditingObject.vertices, stretch);
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
                                                        stretch = Mathf.Lerp(1f, stretch, slowFactor);
                                                    }
                                                    Gizmos.recordingStretch = stretch;
                                                    VertexUtils.StretchY(Gizmos.tempBuffer, currentlyEditingObject.vertices, stretch);
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
                                                        stretch = Mathf.Lerp(1f, stretch, slowFactor);
                                                    }
                                                    Gizmos.recordingStretch = stretch;
                                                    VertexUtils.StretchZ(Gizmos.tempBuffer, currentlyEditingObject.vertices, stretch);
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
                                                        angle = Mathf.Lerp(0f, angle, slowFactor);
                                                    }
                                                    if (Gizmos.referential == Gizmos.SpaceReferential.Local)
                                                    {
                                                        if (Gizmos.registeredFloat == 0)
                                                        {
                                                            if (Vector3.Dot(currentlyEditingObject.m_rotation * Vector3.up, Vector3.up) < 0)
                                                                angle = -angle;
                                                        }
                                                        currentlyEditingObject.SetRotation(Gizmos.initialRotationTemp * Quaternion.Euler(Vector3.up * angle));
                                                    }
                                                    else
                                                        currentlyEditingObject.SetRotation(Quaternion.Euler(Vector3.up * angle) * Gizmos.initialRotationTemp);
                                                    Gizmos.recordingAngle = angle;
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
                                                        angle = Mathf.Lerp(0f, angle, slowFactor);
                                                    }
                                                    if (Gizmos.referential == Gizmos.SpaceReferential.Local)
                                                    {
                                                        if (Gizmos.registeredFloat == 0)
                                                        {
                                                            if (Vector3.Dot(currentlyEditingObject.m_rotation * Vector3.forward, Vector3.forward) < 0)
                                                                angle = -angle;
                                                        }
                                                        currentlyEditingObject.SetRotation(Gizmos.initialRotationTemp * Quaternion.Euler(Vector3.forward * angle));
                                                    }
                                                    else
                                                        currentlyEditingObject.SetRotation(Quaternion.Euler(Vector3.forward * angle) * Gizmos.initialRotationTemp);
                                                    Gizmos.recordingAngle = angle;
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
                                                        angle = Mathf.Lerp(0f, angle, slowFactor);
                                                    }
                                                    if (Gizmos.referential == Gizmos.SpaceReferential.Local)
                                                    {
                                                        if (Gizmos.registeredFloat == 0)
                                                        {
                                                            if (Vector3.Dot(currentlyEditingObject.m_rotation * Vector3.right, Vector3.right) < 0)
                                                                angle = -angle;
                                                        }
                                                        currentlyEditingObject.SetRotation(Gizmos.initialRotationTemp * Quaternion.Euler(Vector3.right * angle));
                                                    }
                                                    else
                                                        currentlyEditingObject.SetRotation(Quaternion.Euler(Vector3.right * angle) * Gizmos.initialRotationTemp);
                                                    Gizmos.recordingAngle = angle;
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
                    else // vertex customization
                    {
                        if (KeyBindingsManager.instance.GetBindingFromName("switchGeneralToVertexTools").GetBindingDown())
                        {
                            PlaySound(2);
                            drawWizardData = null;
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
                        if (verticesToolType == 3)
                        {
                            if (drawWizardData != null)
                                drawWizardData.Update();
                        }
                        else
                        {
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
                                    currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(currentlyEditingObject.vertices);
                                    Gizmos.DisableKeyTyping();
                                    VerticesWizardData.DestroyLines();
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
                                                currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, currentlyEditingObject.vertices);
                                                if (verticesToolType != 0)
                                                    Gizmos.EnableKeyTyping();
                                                vertWizardData.Store(currentlyEditingObject.vertices.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))).ToArray(), currentlyEditingObject);
                                            }
                                            if (vertWizardData.toolType == 0)
                                            {
                                                if (Input.GetKeyDown(KeyCode.LeftControl))
                                                    VerticesWizardData.HideLines();
                                                else if (Input.GetKeyUp(KeyCode.LeftControl))
                                                    VerticesWizardData.ShowLines();

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
                                    foreach (Vertex vertex in currentlyEditingObject.vertices)
                                    {
                                        if (vertex.IsDependent) continue;
                                        if (region.Contains(ProceduralUtils.VertexWorldPosition(vertex, currentlyEditingObject).WorldToGuiPoint(), true))
                                        {
                                            editingVertexIndex.Add(vertex.Index);
                                        }
                                    }
                                    ProceduralUtils.UpdateVertexSelectedState(editingVertexIndex, currentlyEditingObject);
                                }
                            }
                        }
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
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, currentlyEditingObject.vertices);
                                    foreach (Vertex v in currentlyEditingObject.vertices.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        currentlyEditingObject.vertices[v.Index].Position.y += 5f * TimeUtils.deltaTime;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveDown").GetBinding())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, currentlyEditingObject.vertices);
                                    foreach (Vertex v in currentlyEditingObject.vertices.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        currentlyEditingObject.vertices[v.Index].Position.y -= 5f * TimeUtils.deltaTime;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveLeft").GetBinding())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, currentlyEditingObject.vertices);
                                    foreach (Vertex v in currentlyEditingObject.vertices.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        currentlyEditingObject.vertices[v.Index].Position.z += 5f * TimeUtils.deltaTime;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveRight").GetBinding())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, currentlyEditingObject.vertices);
                                    foreach (Vertex v in currentlyEditingObject.vertices.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        currentlyEditingObject.vertices[v.Index].Position.z -= 5f * TimeUtils.deltaTime;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveForward").GetBinding())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, currentlyEditingObject.vertices);
                                    foreach (Vertex v in currentlyEditingObject.vertices.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        currentlyEditingObject.vertices[v.Index].Position.x += 5f * TimeUtils.deltaTime;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveBackward").GetBinding())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, currentlyEditingObject.vertices);
                                    foreach (Vertex v in currentlyEditingObject.vertices.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        currentlyEditingObject.vertices[v.Index].Position.x -= 5f * TimeUtils.deltaTime;
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
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + new Vector3(0, 8f * TimeUtils.deltaTime, 0));
                                            else
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + (currentlyEditingObject.m_rotation * new Vector3(0, 8f * TimeUtils.deltaTime, 0)));
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveDown").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + new Vector3(0, -8f * TimeUtils.deltaTime, 0));
                                            else
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + (currentlyEditingObject.m_rotation * new Vector3(0, -8f * TimeUtils.deltaTime, 0)));
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveLeft").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + new Vector3(0, 0, 8f * TimeUtils.deltaTime));
                                            else
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + (currentlyEditingObject.m_rotation * new Vector3(0, 0, 8f * TimeUtils.deltaTime)));
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveRight").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + new Vector3(0, 0, -8f * TimeUtils.deltaTime));
                                            else
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + (currentlyEditingObject.m_rotation * new Vector3(0, 0, -8f * TimeUtils.deltaTime)));
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveForward").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + new Vector3(8f * TimeUtils.deltaTime, 0, 0));
                                            else
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + (currentlyEditingObject.m_rotation * new Vector3(8f * TimeUtils.deltaTime, 0, 0)));
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveBackward").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + new Vector3(-8f * TimeUtils.deltaTime, 0, 0));
                                            else
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + (currentlyEditingObject.m_rotation * new Vector3(-8f * TimeUtils.deltaTime, 0, 0)));
                                        }
                                        break;
                                    case 1:
                                        // SCALE

                                        if (KeyBindingsManager.instance.GetBindingFromName("scale_scaleUp").GetBinding())
                                        {
                                            currentlyEditingObject.Scale(currentlyEditingObject.vertices, 1 + (.3f * TimeUtils.deltaTime));
                                            Apply();
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("scale_scaleDown").GetBinding())
                                        {
                                            currentlyEditingObject.Scale(currentlyEditingObject.vertices, 1 - (.3f * TimeUtils.deltaTime));
                                            Apply();
                                        }
                                        break;
                                    case 2:
                                        // ROTATION

                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveUp").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetRotation(Quaternion.Euler(20f * TimeUtils.deltaTime, 0, 0) * currentlyEditingObject.m_rotation);
                                            else
                                                currentlyEditingObject.SetRotation(currentlyEditingObject.m_rotation.Rotate(20f * TimeUtils.deltaTime, 0, 0));
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveDown").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetRotation(Quaternion.Euler(-20f * TimeUtils.deltaTime, 0, 0) * currentlyEditingObject.m_rotation);
                                            else
                                                currentlyEditingObject.SetRotation(currentlyEditingObject.m_rotation.Rotate(-20f * TimeUtils.deltaTime, 0, 0));
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveLeft").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetRotation(Quaternion.Euler(0, 20f * TimeUtils.deltaTime, 0) * currentlyEditingObject.m_rotation);
                                            else
                                                currentlyEditingObject.SetRotation(currentlyEditingObject.m_rotation.Rotate(0, 20f * TimeUtils.deltaTime, 0));
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveRight").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetRotation(Quaternion.Euler(0, -20f * TimeUtils.deltaTime, 0) * currentlyEditingObject.m_rotation);
                                            else
                                                currentlyEditingObject.SetRotation(currentlyEditingObject.m_rotation.Rotate(0, -20f * TimeUtils.deltaTime, 0));
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveForward").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetRotation(Quaternion.Euler(0, 0, 20f * TimeUtils.deltaTime) * currentlyEditingObject.m_rotation);
                                            else
                                                currentlyEditingObject.SetRotation(currentlyEditingObject.m_rotation.Rotate(0, 0, 20f * TimeUtils.deltaTime));
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveBackward").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetRotation(Quaternion.Euler(0, 0, -20f * TimeUtils.deltaTime) * currentlyEditingObject.m_rotation);
                                            else
                                                currentlyEditingObject.SetRotation(currentlyEditingObject.m_rotation.Rotate(0, 0, -20f * TimeUtils.deltaTime));
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
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, currentlyEditingObject.vertices);
                                    foreach (Vertex v in currentlyEditingObject.vertices.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        currentlyEditingObject.vertices[v.Index].Position.y += TimeUtils.deltaTime;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveDown").GetBinding())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, currentlyEditingObject.vertices);
                                    foreach (Vertex v in currentlyEditingObject.vertices.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        currentlyEditingObject.vertices[v.Index].Position.y -= TimeUtils.deltaTime;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveLeft").GetBinding())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, currentlyEditingObject.vertices);
                                    foreach (Vertex v in currentlyEditingObject.vertices.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        currentlyEditingObject.vertices[v.Index].Position.z += TimeUtils.deltaTime;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveRight").GetBinding())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, currentlyEditingObject.vertices);
                                    foreach (Vertex v in currentlyEditingObject.vertices.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        currentlyEditingObject.vertices[v.Index].Position.z -= TimeUtils.deltaTime;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveForward").GetBinding())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, currentlyEditingObject.vertices);
                                    foreach (Vertex v in currentlyEditingObject.vertices.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        currentlyEditingObject.vertices[v.Index].Position.x += TimeUtils.deltaTime;
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveBackward").GetBinding())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, currentlyEditingObject.vertices);
                                    foreach (Vertex v in currentlyEditingObject.vertices.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        currentlyEditingObject.vertices[v.Index].Position.x -= TimeUtils.deltaTime;
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
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + new Vector3(0, TimeUtils.deltaTime, 0));
                                            else
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + (currentlyEditingObject.m_rotation * new Vector3(0, TimeUtils.deltaTime, 0)));
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveDown").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + new Vector3(0, -TimeUtils.deltaTime, 0));
                                            else
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + (currentlyEditingObject.m_rotation * new Vector3(0, -TimeUtils.deltaTime, 0)));
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveLeft").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + new Vector3(0, 0, TimeUtils.deltaTime));
                                            else
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + (currentlyEditingObject.m_rotation * new Vector3(0, 0, TimeUtils.deltaTime)));
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveRight").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + new Vector3(0, 0, -TimeUtils.deltaTime));
                                            else
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + (currentlyEditingObject.m_rotation * new Vector3(0, 0, -TimeUtils.deltaTime)));
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveForward").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + new Vector3(TimeUtils.deltaTime, 0, 0));
                                            else
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + (currentlyEditingObject.m_rotation * new Vector3(TimeUtils.deltaTime, 0, 0)));
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveBackward").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + new Vector3(-TimeUtils.deltaTime, 0, 0));
                                            else
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + (currentlyEditingObject.m_rotation * new Vector3(-TimeUtils.deltaTime, 0, 0)));
                                        }
                                        break;
                                    case 1:
                                        // SCALE

                                        if (KeyBindingsManager.instance.GetBindingFromName("scale_scaleUp").GetBinding())
                                        {
                                            currentlyEditingObject.Scale(currentlyEditingObject.vertices, 1 + (.12f * TimeUtils.deltaTime));
                                            Apply();
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("scale_scaleDown").GetBinding())
                                        {
                                            currentlyEditingObject.Scale(currentlyEditingObject.vertices, 1 - (.12f * TimeUtils.deltaTime));
                                            Apply();
                                        }
                                        break;
                                    case 2:
                                        // ROTATION

                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveUp").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetRotation(Quaternion.Euler(10f * TimeUtils.deltaTime, 0, 0) * currentlyEditingObject.m_rotation);
                                            else
                                                currentlyEditingObject.SetRotation(currentlyEditingObject.m_rotation.Rotate(10f * TimeUtils.deltaTime, 0, 0));
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveDown").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetRotation(Quaternion.Euler(-10f * TimeUtils.deltaTime, 0, 0) * currentlyEditingObject.m_rotation);
                                            else
                                                currentlyEditingObject.SetRotation(currentlyEditingObject.m_rotation.Rotate(-10f * TimeUtils.deltaTime, 0, 0));
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveLeft").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetRotation(Quaternion.Euler(0, 10f * TimeUtils.deltaTime, 0) * currentlyEditingObject.m_rotation);
                                            else
                                                currentlyEditingObject.SetRotation(currentlyEditingObject.m_rotation.Rotate(0, 10f * TimeUtils.deltaTime, 0));
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveRight").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetRotation(Quaternion.Euler(0, -10f * TimeUtils.deltaTime, 0) * currentlyEditingObject.m_rotation);
                                            else
                                                currentlyEditingObject.SetRotation(currentlyEditingObject.m_rotation.Rotate(0, -10f * TimeUtils.deltaTime, 0));
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveForward").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetRotation(Quaternion.Euler(0, 0, 10f * TimeUtils.deltaTime) * currentlyEditingObject.m_rotation);
                                            else
                                                currentlyEditingObject.SetRotation(currentlyEditingObject.m_rotation.Rotate(0, 0, 10f * TimeUtils.deltaTime));
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveBackward").GetBinding())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetRotation(Quaternion.Euler(0, 0, -10f * TimeUtils.deltaTime) * currentlyEditingObject.m_rotation);
                                            else
                                                currentlyEditingObject.SetRotation(currentlyEditingObject.m_rotation.Rotate(0, 0, -10f * TimeUtils.deltaTime));
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
                                currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(currentlyEditingObject.vertices);
                            }
                            if (KeyBindingsManager.instance.GetBindingFromName("position_moveDown").GetBindingUp())
                            {
                                currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(currentlyEditingObject.vertices);
                            }
                            if (KeyBindingsManager.instance.GetBindingFromName("position_moveLeft").GetBindingUp())
                            {
                                currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(currentlyEditingObject.vertices);
                            }
                            if (KeyBindingsManager.instance.GetBindingFromName("position_moveRight").GetBindingUp())
                            {
                                currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(currentlyEditingObject.vertices);
                            }
                            if (KeyBindingsManager.instance.GetBindingFromName("position_moveForward").GetBindingUp())
                            {
                                currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(currentlyEditingObject.vertices);
                            }
                            if (KeyBindingsManager.instance.GetBindingFromName("position_moveBackward").GetBindingUp())
                            {
                                currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(currentlyEditingObject.vertices);
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
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, currentlyEditingObject.vertices);
                                    foreach (Vertex v in currentlyEditingObject.vertices.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        currentlyEditingObject.vertices[v.Index].Position.y += 1f;
                                    if (vertWizardData == null)
                                        currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(currentlyEditingObject.vertices);
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveDown").GetBindingDown())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, currentlyEditingObject.vertices);
                                    foreach (Vertex v in currentlyEditingObject.vertices.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        currentlyEditingObject.vertices[v.Index].Position.y -= 1f;
                                    if (vertWizardData == null)
                                        currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(currentlyEditingObject.vertices);
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveLeft").GetBindingDown())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, currentlyEditingObject.vertices);
                                    foreach (Vertex v in currentlyEditingObject.vertices.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        currentlyEditingObject.vertices[v.Index].Position.z += 1f;
                                    if (vertWizardData == null)
                                        currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(currentlyEditingObject.vertices);
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveRight").GetBindingDown())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, currentlyEditingObject.vertices);
                                    foreach (Vertex v in currentlyEditingObject.vertices.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        currentlyEditingObject.vertices[v.Index].Position.z -= 1f;
                                    if (vertWizardData == null)
                                        currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(currentlyEditingObject.vertices);
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveForward").GetBindingDown())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, currentlyEditingObject.vertices);
                                    foreach (Vertex v in currentlyEditingObject.vertices.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        currentlyEditingObject.vertices[v.Index].Position.x += 1f;
                                    if (vertWizardData == null)
                                        currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(currentlyEditingObject.vertices);
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveBackward").GetBindingDown())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, currentlyEditingObject.vertices);
                                    foreach (Vertex v in currentlyEditingObject.vertices.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        currentlyEditingObject.vertices[v.Index].Position.x -= 1f;
                                    if (vertWizardData == null)
                                        currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(currentlyEditingObject.vertices);
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
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + new Vector3(0, 2f, 0));
                                            else
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + (currentlyEditingObject.m_rotation * new Vector3(0, 2f, 0)));
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveDown").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + new Vector3(0, -2f, 0));
                                            else
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + (currentlyEditingObject.m_rotation * new Vector3(0, -2f, 0)));
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveLeft").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + new Vector3(0, 0, 2f));
                                            else
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + (currentlyEditingObject.m_rotation * new Vector3(0, 0, 2f)));
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveRight").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + new Vector3(0, 0, -2f));
                                            else
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + (currentlyEditingObject.m_rotation * new Vector3(0, 0, -2f)));
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveForward").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + new Vector3(2f, 0, 0));
                                            else
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + (currentlyEditingObject.m_rotation * new Vector3(2f, 0, 0)));
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveBackward").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + new Vector3(-2f, 0, 0));
                                            else
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + (currentlyEditingObject.m_rotation * new Vector3(-2f, 0, 0)));
                                        }
                                        break;
                                    case 1:
                                        // SCALE

                                        if (KeyBindingsManager.instance.GetBindingFromName("scale_scaleUp").GetBindingDown())
                                        {
                                            currentlyEditingObject.Scale(currentlyEditingObject.vertices, 1.12f);
                                            Apply();
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("scale_scaleDown").GetBindingDown())
                                        {
                                            currentlyEditingObject.Scale(currentlyEditingObject.vertices, .88f);
                                            Apply();
                                        }
                                        break;
                                    case 2:
                                        // ROTATION

                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveUp").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetRotation(Quaternion.Euler(12f, 0, 0) * currentlyEditingObject.m_rotation);
                                            else
                                                currentlyEditingObject.SetRotation(currentlyEditingObject.m_rotation.Rotate(12f, 0, 0));
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveDown").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetRotation(Quaternion.Euler(-12f, 0, 0) * currentlyEditingObject.m_rotation);
                                            else
                                                currentlyEditingObject.SetRotation(currentlyEditingObject.m_rotation.Rotate(-12f, 0, 0));
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveLeft").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetRotation(Quaternion.Euler(0, 12f, 0) * currentlyEditingObject.m_rotation);
                                            else
                                                currentlyEditingObject.SetRotation(currentlyEditingObject.m_rotation.Rotate(0, 12f, 0));
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveRight").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetRotation(Quaternion.Euler(0, -12f, 0) * currentlyEditingObject.m_rotation);
                                            else
                                                currentlyEditingObject.SetRotation(currentlyEditingObject.m_rotation.Rotate(0, -12f, 0));
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveForward").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetRotation(Quaternion.Euler(0, 0, 12f) * currentlyEditingObject.m_rotation);
                                            else
                                                currentlyEditingObject.SetRotation(currentlyEditingObject.m_rotation.Rotate(0, 0, 12f));
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveBackward").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetRotation(Quaternion.Euler(0, 0, -12f) * currentlyEditingObject.m_rotation);
                                            else
                                                currentlyEditingObject.SetRotation(currentlyEditingObject.m_rotation.Rotate(0, 0, -12f));
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
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, currentlyEditingObject.vertices);
                                    foreach (Vertex v in currentlyEditingObject.vertices.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        currentlyEditingObject.vertices[v.Index].Position.y += 0.15f;
                                    if (vertWizardData == null)
                                        currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(currentlyEditingObject.vertices);
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveDown").GetBindingDown())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, currentlyEditingObject.vertices);
                                    foreach (Vertex v in currentlyEditingObject.vertices.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        currentlyEditingObject.vertices[v.Index].Position.y -= 0.15f;
                                    if (vertWizardData == null)
                                        currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(currentlyEditingObject.vertices);
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveLeft").GetBindingDown())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, currentlyEditingObject.vertices);
                                    foreach (Vertex v in currentlyEditingObject.vertices.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        currentlyEditingObject.vertices[v.Index].Position.z += 0.15f;
                                    if (vertWizardData == null)
                                        currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(currentlyEditingObject.vertices);
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveRight").GetBindingDown())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, currentlyEditingObject.vertices);
                                    foreach (Vertex v in currentlyEditingObject.vertices.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        currentlyEditingObject.vertices[v.Index].Position.z -= 0.15f;
                                    if (vertWizardData == null)
                                        currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(currentlyEditingObject.vertices);
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveForward").GetBindingDown())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, currentlyEditingObject.vertices);
                                    foreach (Vertex v in currentlyEditingObject.vertices.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        currentlyEditingObject.vertices[v.Index].Position.x += 0.15f;
                                    if (vertWizardData == null)
                                        currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(currentlyEditingObject.vertices);
                                    Apply();
                                }
                                if (KeyBindingsManager.instance.GetBindingFromName("position_moveBackward").GetBindingDown())
                                {
                                    currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, currentlyEditingObject.vertices);
                                    foreach (Vertex v in currentlyEditingObject.vertices.Where(v => (editingVertexIndex.Contains(v.Index) || (v.IsDependent && editingVertexIndex.Contains(v.DependencyIndex)))))
                                        currentlyEditingObject.vertices[v.Index].Position.x -= 0.15f;
                                    if (vertWizardData == null)
                                        currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(currentlyEditingObject.vertices);
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
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + new Vector3(0, 0.25f, 0));
                                            else
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + (currentlyEditingObject.m_rotation * new Vector3(0, 0.25f, 0)));
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveDown").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + new Vector3(0, -0.25f, 0));
                                            else
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + (currentlyEditingObject.m_rotation * new Vector3(0, -0.25f, 0)));
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveLeft").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + new Vector3(0, 0, 0.25f));
                                            else
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + (currentlyEditingObject.m_rotation * new Vector3(0, 0, 0.25f)));
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveRight").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + new Vector3(0, 0, -0.25f));
                                            else
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + (currentlyEditingObject.m_rotation * new Vector3(0, 0, -0.25f)));
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveForward").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + new Vector3(0.25f, 0, 0));
                                            else
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + (currentlyEditingObject.m_rotation * new Vector3(0.25f, 0, 0)));
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("position_moveBackward").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + new Vector3(-0.25f, 0, 0));
                                            else
                                                currentlyEditingObject.SetPosition(currentlyEditingObject.m_position + (currentlyEditingObject.m_rotation * new Vector3(-0.25f, 0, 0)));
                                        }
                                        break;
                                    case 1:
                                        // SCALE

                                        if (KeyBindingsManager.instance.GetBindingFromName("scale_scaleUp").GetBindingDown())
                                        {
                                            currentlyEditingObject.Scale(currentlyEditingObject.vertices, 1.06f);
                                            Apply();
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("scale_scaleDown").GetBindingDown())
                                        {
                                            currentlyEditingObject.Scale(currentlyEditingObject.vertices, .94f);
                                            Apply();
                                        }
                                        break;
                                    case 2:
                                        // ROTATION

                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveUp").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetRotation(Quaternion.Euler(5f, 0, 0) * currentlyEditingObject.m_rotation);
                                            else
                                                currentlyEditingObject.SetRotation(currentlyEditingObject.m_rotation.Rotate(5f, 0, 0));
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveDown").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetRotation(Quaternion.Euler(-5f, 0, 0) * currentlyEditingObject.m_rotation);
                                            else
                                                currentlyEditingObject.SetRotation(currentlyEditingObject.m_rotation.Rotate(-5f, 0, 0));
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveLeft").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetRotation(Quaternion.Euler(0, 5f, 0) * currentlyEditingObject.m_rotation);
                                            else
                                                currentlyEditingObject.SetRotation(currentlyEditingObject.m_rotation.Rotate(0, 5f, 0));
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveRight").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetRotation(Quaternion.Euler(0, -5f, 0) * currentlyEditingObject.m_rotation);
                                            else
                                                currentlyEditingObject.SetRotation(currentlyEditingObject.m_rotation.Rotate(0, -5f, 0));
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveForward").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetRotation(Quaternion.Euler(0, 0, 5f) * currentlyEditingObject.m_rotation);
                                            else
                                                currentlyEditingObject.SetRotation(currentlyEditingObject.m_rotation.Rotate(0, 0, 5f));
                                        }
                                        if (KeyBindingsManager.instance.GetBindingFromName("rotation_moveBackward").GetBindingDown())
                                        {
                                            if (Gizmos.referential == Gizmos.SpaceReferential.World)
                                                currentlyEditingObject.SetRotation(Quaternion.Euler(0, 0, -5f) * currentlyEditingObject.m_rotation);
                                            else
                                                currentlyEditingObject.SetRotation(currentlyEditingObject.m_rotation.Rotate(0, 0, -5f));
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

            MeasurementsManager.instance.DrawWindow();

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
                        var objScreenPos = obj.m_position.WorldToGuiPoint();
                        if (!obj._selected)
                        {
                            if (ProceduralObjectsMod.HideDisabledLayersIcon.value && obj.layer != null)
                            {
                                if (obj.layer.m_isHidden)
                                    continue;
                            }
                            if (!filters.FiltersAllow(obj))
                                continue;
                            if (!new Rect(0, 0, Screen.width, Screen.height).Contains(objScreenPos))
                                continue;
                            if (!obj._insideUIview)
                                continue;
                        }
                        if (IsInWindowElement(objScreenPos, true))
                            continue;
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
                                                EditObject(obj);
                                            }
                                        }
                                        if (GUI.Button(new Rect(objScreenPos + new Vector2(12, 12 + addedHeight), new Vector2(130, 22)), LocalizationManager.instance.current["move_to"]))
                                        {
                                            PlaySound();
                                            if (isRoot && selectedGroup == null)
                                            {
                                                // pObjSelection.Clear();
                                                MoveSelection(obj.group.objects, true);
                                            }
                                            else
                                            {
                                                pObjSelection.Clear();
                                                SetCurrentlyEditingObj(obj);
                                                obj.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.moveTo, currentlyEditingObject.vertices);
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
                    SingleHoveredObj = hoveredObj;
                }
                if (!movingWholeModel)
                {
                    var winrect = GUIUtils.ClampRectToScreen(GUIUtils.Window(1094334744, window, DrawUIWindow, "Procedural Objects v" + ProceduralObjectsMod.VERSION));
                    if (proceduralTool && editingWholeModel && !reselectingTex)
                        window = new Rect(winrect.x, winrect.y, winrect.width, (ProceduralObjectsMod.ShowToolsControls.value ? 640 : 475)); // general tool
                    else if (proceduralTool || chosenProceduralInfo != null || reselectingTex)
                        window = new Rect(winrect.x, winrect.y, winrect.width,
                            ((chosenProceduralInfo == null && !reselectingTex) ? 284 + (POToolAction.actions.Count * 26) + (ProceduralObjectsMod.ShowToolsControls.value ? 168 : 0) : 400)); // customization tool : tex select
                    else
                        window = new Rect(winrect.x, winrect.y, winrect.width, _winHeight); // selection mode 

                    if (showExternals)
                        externalsWindow = GUIUtils.ClampRectToScreen(GUIUtils.Window(1094334745, externalsWindow, DrawExternalsWindow, LocalizationManager.instance.current["saved_pobjs"]));

                    textManager.DrawWindow();

                    moduleManager.DrawCustomizationWindows();

                    if (advEdManager != null)
                    {
                        advEdManager.m_vertices = currentlyEditingObject.vertices;
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
                                foreach (Vertex vertex in currentlyEditingObject.vertices)
                                {
                                    if (vertex == null)
                                        continue;
                                    if (vertex.IsDependent)
                                        continue;
                                    if (currentlyEditingObject.m_mesh.name == "ploppablecliffgrass" && vertex.Index >= currentlyEditingObject.m_mesh.vertices.Length - 2)
                                        continue;
                                    var vertexWorldPos = ProceduralUtils.VertexWorldPosition(vertex, currentlyEditingObject);
                                    if (renderCamera.WorldToScreenPoint(vertexWorldPos).z < 0)
                                        continue;
                                    if (vertex._selected)
                                    {
                                        if (GUI.Button(new Rect(vertexWorldPos.WorldToGuiPoint() + new Vector2(-10, -10), new Vector2(20, 20)), VertexUtils.vertexIcons[1], GUI.skin.label))
                                        {
                                            if (verticesToolType != 3 && drawWizardData == null)
                                            {
                                                PlaySound();
                                                editingVertexIndex.Remove(vertex.Index);
                                                ProceduralUtils.UpdateVertexSelectedState(editingVertexIndex, currentlyEditingObject);
                                                if (editingVertexIndex.Count == 0)
                                                    editingVertex = false;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (GUI.Button(new Rect(vertexWorldPos.WorldToGuiPoint() + new Vector2(-10, -10), new Vector2(20, 20)), VertexUtils.vertexIcons[0], GUI.skin.label))
                                        {
                                            if (verticesToolType != 3 && drawWizardData == null)
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
                                                ProceduralUtils.UpdateVertexSelectedState(editingVertexIndex, currentlyEditingObject);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else if (currentlyEditingObject._insideUIview && axisState == AxisEditionState.none)
                        {
                            Vector2 objPosition = currentlyEditingObject.m_position.WorldToGuiPoint();

                            var posrect = new Rect(Gizmos.RightMostGUIPosGizmo + new Vector2(5, -26), new Vector2(32, 32));
                            if (GUI.Button(posrect, string.Empty))
                            {
                                PlaySound();
                                actionMode = 0;
                                Gizmos.CreatePositionGizmo(currentlyEditingObject.m_position, true);
                            }
                            GUIUtils.ColorizeIf(Color.red, actionMode == 0, () => { GUI.DrawTexture(posrect, ProceduralTool.moveVertices.m_texture); });

                            var rotrect = new Rect(Gizmos.RightMostGUIPosGizmo + new Vector2(38.5f, -26), new Vector2(32, 32));
                            if (GUI.Button(rotrect, string.Empty))
                            {
                                PlaySound();
                                Gizmos.CreateRotationGizmo(currentlyEditingObject.m_position, true);
                                actionMode = 2;
                            }
                            GUIUtils.ColorizeIf(Color.red, actionMode == 2, () => { GUI.DrawTexture(rotrect, ProceduralTool.rotateVertices.m_texture); });

                            var scalerect = new Rect(Gizmos.RightMostGUIPosGizmo + new Vector2(72, -26), new Vector2(32, 32));
                            if (GUI.Button(scalerect, string.Empty))
                            {
                                PlaySound();
                                actionMode = 1;
                                Gizmos.CreateScaleGizmo(currentlyEditingObject.m_position, true);
                            }
                            GUIUtils.ColorizeIf(Color.red, actionMode == 1, () => { GUI.DrawTexture(scalerect, ProceduralTool.scaleVertices.m_texture); });


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
                            Rect modeRect = new Rect(Gizmos.RightMostGUIPosGizmo + new Vector2(5, 7), new Vector2(100, 23));
                            GUI.Box(modeRect, string.Empty);
                            var anchor = GUI.skin.label.alignment;
                            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                            GUI.Label(modeRect, modeText);
                            GUI.skin.label.alignment = anchor;


                            if (GUI.Button(new Rect(Gizmos.RightMostGUIPosGizmo + new Vector2(5, 33), new Vector2(100, 23)), LocalizationManager.instance.current["move_to"]))
                            {
                                PlaySound();
                                getBackToGeneralTool = true;
                                currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.moveTo, currentlyEditingObject.vertices);
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
                                GUI.Box(new Rect(Gizmos.RightMostGUIPosGizmo + new Vector2(5, 59), new Vector2(100, 23)), "<i>" + LocalizationManager.instance.current["referential_local"] + "</i>");
                                GUI.color = Color.white;
                            }
                            else
                            {
                                // (formerly) LocalizationManager.instance.current["delete"])) { DeleteObject(); }
                                if (GUI.Button(new Rect(Gizmos.RightMostGUIPosGizmo + new Vector2(5, 59), new Vector2(100, 23)), LocalizationManager.instance.current[Gizmos.referential == Gizmos.SpaceReferential.World ? "referential_world" : "referential_local"]))
                                {
                                    PlaySound();
                                    if (Gizmos.referential == Gizmos.SpaceReferential.Local)
                                        Gizmos.referential = Gizmos.SpaceReferential.World;
                                    else
                                        Gizmos.referential = Gizmos.SpaceReferential.Local;
                                }
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


            if (proceduralTool && !reselectingTex)
            {
                if (currentlyEditingObject != null)
                {
                    GUIUtils.DrawSeparator(new Vector2(10, 26), 380);
                    GUI.BeginGroup(new Rect(10, 30, 385, 475));
                    if (editingWholeModel)
                    {
                        // GENERAL TOOL
                        GUI.Label(new Rect(27, 0, 300, 30), "<b><size=18>" + LocalizationManager.instance.current["general_tool"] + "</size></b>");
                        GUI.contentColor = Color.green;
                        GUI.Label(new Rect(0, 0, 23, 23), "<size=18>¤</size>", GUI.skin.button);
                        GUI.contentColor = Color.white;
                        ProceduralTool.DrawToolsControls(new Rect(0, 30, 380, 190), true);
                        GUI.EndGroup();

                        GUI.BeginGroup(new Rect(10, ProceduralObjectsMod.ShowToolsControls.value ? 255 : 90, 420, 400));

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

                        var newposx = new Vector3(currentlyEditingObject.transformInputFields[0].DrawField(new Rect(26, 27, 95, 23), "genPosX", currentlyEditingObject.m_position.x).returnValue,
                            currentlyEditingObject.m_position.y, currentlyEditingObject.m_position.z);
                        if (newposx != currentlyEditingObject.m_position)
                            currentlyEditingObject.SetPosition(newposx);

                        var newposy = new Vector3(currentlyEditingObject.m_position.x,
                            currentlyEditingObject.transformInputFields[1].DrawField(new Rect(152, 27, 95, 23), "genPosY", currentlyEditingObject.m_position.y).returnValue, currentlyEditingObject.m_position.z);
                        if (newposy != currentlyEditingObject.m_position)
                            currentlyEditingObject.SetPosition(newposy);

                        var newposz = new Vector3(currentlyEditingObject.m_position.x, currentlyEditingObject.m_position.y,
                            currentlyEditingObject.transformInputFields[2].DrawField(new Rect(278, 27, 95, 23), "genPosZ", currentlyEditingObject.m_position.z).returnValue);
                        if (newposz != currentlyEditingObject.m_position)
                            currentlyEditingObject.SetPosition(newposz);

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

                        euler.x = currentlyEditingObject.transformInputFields[3].DrawField(new Rect(26, 104, 95, 23), "genRotX", euler.x).returnValue;
                        euler.y = currentlyEditingObject.transformInputFields[4].DrawField(new Rect(152, 104, 95, 23), "genRotY", euler.y).returnValue;
                        euler.z = currentlyEditingObject.transformInputFields[5].DrawField(new Rect(278, 104, 95, 23), "genRotZ", euler.z).returnValue;
                        /*
                        if (float.TryParse(GUI.TextField(new Rect(26, 104, 95, 23), euler.x.ToString()), out newX))
                            euler.x = newX;
                        if (float.TryParse(GUI.TextField(new Rect(152, 104, 95, 23), euler.y.ToString()), out newY))
                            euler.y = newY;
                        if (float.TryParse(GUI.TextField(new Rect(278, 104, 95, 23), euler.z.ToString()), out newZ))
                            euler.z = newZ;*/
                        var quatEuler = Quaternion.Euler(euler);
                        if (quatEuler != currentlyEditingObject.m_rotation)
                            currentlyEditingObject.SetRotation(quatEuler);

                        if (GUI.Button(new Rect(0, 129, 380, 22f), LocalizationManager.instance.current["resetOrientation"]))
                        {
                            if (currentlyEditingObject.m_rotation == Quaternion.identity)
                                PlaySound(2);
                            else
                            {
                                PlaySound();
                                currentlyEditingObject.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.rotation, null);
                                currentlyEditingObject.SetRotation(Quaternion.identity);
                                currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(null);
                            }
                        }

                        GUIUtils.DrawSeparator(new Vector2(0, 153), 380);

                        GUI.Label(new Rect(0, 155, 380, 27), "<b><size=13>" + LocalizationManager.instance.current["material"] + "</size></b>");

                        if (TextParameters.CanHaveTextParameters(currentlyEditingObject))
                        {
                            if (GUI.Button(new Rect(0, 182, 200, 25), LocalizationManager.instance.current["text_customization"]))
                            {
                                PlaySound();
                                textManager.Edit(currentlyEditingObject, new Vector2(window.x + window.width, window.y));
                            }
                        }
                        if (GUI.Button(new Rect(320, 155, 57, 57), currentlyEditingObject.m_material.mainTexture))
                        {
                            PlaySound();
                            reselectingTex = true;
                        }

                        GUIUtils.DrawSeparator(new Vector2(0, 213), 380);

                        GUI.Label(new Rect(0, 215, 380, 27), "<b><size=13>" + LocalizationManager.instance.current["render_distance"] + " :</size></b> "
                            + ((currentlyEditingObject.renderDistance >= 16001f) ? LocalizationManager.instance.current["infinite_renderDist"] :
                            (Gizmos.ConvertRoundToDistanceUnit(currentlyEditingObject.renderDistance).ToString("N").Replace(".00", "") + ProceduralObjectsMod.distanceUnit
                            + ((RenderOptions.instance.globalMultiplier != 1f) ? ("  (x " + RenderOptions.instance.globalMultiplier + ")") : ""))));

                        var renderDistSlider = GUI.HorizontalSlider(new Rect(0, 240, 350, 20), Mathf.Floor(currentlyEditingObject.renderDistance), 50f, 16015f);
                        if (renderDistSlider != currentlyEditingObject.renderDistance)
                        {
                            currentlyEditingObject.renderDistance = renderDistSlider;
                            MaterialOptions.FixDecalRenderDist(currentlyEditingObject);
                            if (!currentlyEditingObject.renderDistLocked)
                            {
                                ProceduralObjectsLogic.PlaySound();
                                currentlyEditingObject.renderDistLocked = true;
                            }
                        }

                        if (GUI.Button(new Rect(353, 230, 25, 25), ProceduralObjectsMod.Icons[currentlyEditingObject.renderDistLocked ? 8 : 9]))
                        {
                            ProceduralObjectsLogic.PlaySound();
                            currentlyEditingObject.renderDistLocked = !currentlyEditingObject.renderDistLocked;
                        }

                        GUIUtils.DrawSeparator(new Vector2(0, 259), 380);

                        GUI.Label(new Rect(0, 262, 380, 25), "<b><size=13>" + LocalizationManager.instance.current["export_selection"] + "</size></b>");

                        externalsSaveTextfield = GUI.TextField(new Rect(0, 289, 285, 28), externalsSaveTextfield);
                        if (File.Exists(ProceduralObjectsMod.ExternalsConfigPath + externalsSaveTextfield.ToFileName() + ".pobj"))
                        {
                            GUI.color = Color.red;
                            GUI.Label(new Rect(290, 289, 90, 28), "X", GUI.skin.button);
                            GUI.color = Color.white;
                        }
                        else
                        {
                            if (GUI.Button(new Rect(290, 289, 90, 28), LocalizationManager.instance.current["export_selection"]))
                            {
                                PlaySound();
                                ExPObjManager.SaveToExternal(externalsSaveTextfield, new CacheProceduralObject(currentlyEditingObject));
                                externalsSaveTextfield = LocalizationManager.instance.current["enter_name"];
                            }
                        }

                        GUIUtils.DrawSeparator(new Vector2(0, 319), 380);

                        if (GUI.Button(new Rect(0, 351, 185, 25), "◄ " + LocalizationManager.instance.current["back"]))
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
                            if (GUI.Button(new Rect(0, 323, 185, 25), LocalizationManager.instance.current["modules"] + moduleCount))
                                moduleManager.ShowModulesWindow(currentlyEditingObject);

                            if (GUI.Button(new Rect(190, 323, 185, 25), LocalizationManager.instance.current["adv_edition"]))
                                ShowAdvEditionTools();
                            /*
                            if (TextParameters.CanHaveTextParameters(currentlyEditingObject))
                            {
                                if (GUI.Button(new Rect(0, 291, 185, 25), LocalizationManager.instance.current["text_customization"]))
                                {
                                    PlaySound();
                                  textManager.Edit(currentlyEditingObject, new Vector2(window.x + window.width, window.y));
                                }
                            }
                            */
                            if (GUI.Button(new Rect(190, 351, 185, 25), LocalizationManager.instance.current["vertex_customization"]))
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

                        // draw
                        if (currentlyEditingObject.IsPloppableSrfCircle())
                        {
                            if (GUI.Button(new Rect(189, 25, 58, 52), string.Empty))
                            {
                                PlaySound();
                                verticesToolType = 3;
                                drawWizardData = new DrawWizardData(currentlyEditingObject);
                            }
                            GUI.Label(new Rect(192, 24, 52, 40), ProceduralTool.draw.m_texture);
                            GUI.Label(new Rect(190, 56, 56, 22), (verticesToolType == 3 ? "<color=red><b>" : "") + LocalizationManager.instance.current["draw"] + (verticesToolType == 3 ? "</b></color>" : ""));
                        }
                        GUI.skin.label.alignment = prevAlign;

                        GUIUtils.DrawSeparator(new Vector2(0, 80), 380);

                        GUI.EndGroup();

                        int heightDueToCTA = POToolAction.actions.Count * 26 + 38;
                        GUI.BeginGroup(new Rect(10, (ProceduralObjectsMod.ShowToolsControls.value ? 168 : 0) + 160, 380, heightDueToCTA + 500));
                        GUI.Label(new Rect(0, 7, 380, 22), "<b><size=13>" + LocalizationManager.instance.current["CTActions"] + "</size></b>");

                        // PO actions
                        for (int i = 0; i < POToolAction.actions.Count; i++)
                        {
                            POToolAction.actions[i].ActionButton(new Rect(0, 29 + i * 26, 375, 24), currentlyEditingObject, editingVertexIndex, currentlyEditingObject.vertices, Apply);
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
                if (chosenProceduralInfo == null && !reselectingTex)
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
                            new SelectionModeTab("measurmt", 7, () => {
                                    measurementsManager.window.position = new Vector2(window.x + 400, window.y);
                                    if (measurementsManager.showWindow) measurementsManager.CloseWindow();
                                    else measurementsManager.showWindow = true; } ),
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
                                SelectionModeAction.CloseAction();
                                selectedGroup = null;
                                pObjSelection.Clear();
                            }

                            if (GUI.Button(new Rect(200, 4 + addedHeight, 190, 30), LocalizationManager.instance.current["choose_grp_root"]))
                            {
                                PlaySound();
                                SelectionModeAction.CloseAction();
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
                    string label = "";
                    if (reselectingTex)
                    {
                        if (currentlyEditingObject.baseInfoType == "PROP")
                            label = LocalizationManager.instance.current["choose_tex_to_apply"] + " \"" + currentlyEditingObject._baseProp.GetLocalizedTitle() + "\"";
                        else if (currentlyEditingObject.baseInfoType == "BUILDING")
                            label = LocalizationManager.instance.current["choose_tex_to_apply"] + " \"" + currentlyEditingObject._baseBuilding.GetLocalizedTitle() + "\"";
                    }
                    else
                    {
                        if (chosenProceduralInfo.infoType == "PROP")
                            label = LocalizationManager.instance.current["choose_tex_to_apply"] + " \"" + chosenProceduralInfo.propPrefab.GetLocalizedTitle() + "\"";
                        else if (chosenProceduralInfo.infoType == "BUILDING")
                            label = LocalizationManager.instance.current["choose_tex_to_apply"] + " \"" + chosenProceduralInfo.buildingPrefab.GetLocalizedTitle() + "\"";
                    }
                    GUI.Label(new Rect(10, 29, 380, 39), label);
                    GUIUtils.DrawSeparator(new Vector2(10, 69), 380);
                    // Texture selection
                    Texture tex = null;
                    if (GUIUtils.TextureSelector(new Rect(10, 70, 380, 320), ref scrollTextures, out tex))
                    {
                        if (reselectingTex && currentlyEditingObject != null)
                        {
                            currentlyEditingObject.customTexture = tex;
                            var texture = (tex == null) ? ProceduralUtils.GetBasePrefabMainTex(currentlyEditingObject) : tex;
                            currentlyEditingObject.m_material.mainTexture = texture;
                            if (currentlyEditingObject.m_textParameters != null)
                            {
                                if (currentlyEditingObject.m_textParameters.Count() > 0)
                                {
                                    Texture original = ProceduralUtils.GetOriginalTexture(currentlyEditingObject);
                                    var originalTex = new Texture2D(original.width, original.height, TextureFormat.RGBA32, false);
                                    originalTex.SetPixels(((Texture2D)original).GetPixels());
                                    var newtex = (Texture2D)GameObject.Instantiate(originalTex);
                                    currentlyEditingObject.m_material.mainTexture = currentlyEditingObject.m_textParameters.ApplyParameters(originalTex) as Texture;
                                }
                            }
                        }
                        else
                        {
                            editingVertex = false;
                            editingVertexIndex.Clear();
                            editingWholeModel = false;
                            proceduralTool = false;
                            ToolHelper.FullySetTool<DefaultTool>();
                            Gizmos.DestroyGizmo();
                            SpawnObject(chosenProceduralInfo, tex);
                            currentlyEditingObject.vertices = Vertex.CreateVertexList(currentlyEditingObject);
                            ToolHelper.FullySetTool<ProceduralTool>();
                            proceduralTool = true;
                            movingWholeModel = true;
                            toolAction = ToolAction.build;
                            placingSelection = false;
                            editingVertex = false;
                            chosenProceduralInfo = null;
                            TextureManager.instance.MinimizeAll();
                        }
                        reselectingTex = false;
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
                                        selectedGroup = null;
                                        external.CreateClipboard();
                                        if (external.m_externalType == ClipboardProceduralObjects.ClipboardType.Single)
                                        {
                                            var obj = PlaceCacheObject(external.m_object, false);
                                            obj.m_position = external.m_object._staticPos;
                                            pObjSelection.Add(obj);
                                        }
                                        else if (external.m_externalType == ClipboardProceduralObjects.ClipboardType.Selection)
                                        {
                                            var created = new Dictionary<CacheProceduralObject, ProceduralObject>();
                                            foreach (var cache in external.m_selection.selection_objects.Keys.ToList())
                                            {
                                                var obj = PlaceCacheObject(cache, false);
                                                obj.m_position = cache._staticPos;
                                                pObjSelection.Add(obj);
                                                created.Add(cache, obj);
                                            }
                                            external.m_selection.RecreateGroups(created);
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
                                PlaceExternal(external);
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
            currentlyEditingObject.ApplyModelChange();
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
                        obj.m_mesh.uv = Vertex.RecalculateUVMap(obj, currentlyEditingObject.vertices);
                    else
                        obj.m_mesh.uv = Vertex.RecalculateUVMap(obj, Vertex.CreateVertexList(obj));
                }
            }
            return obj;
        }
        public void PlaceExternal(ExternalInfo external)
        {
            external.CreateClipboard();
            if (external.m_externalType == ClipboardProceduralObjects.ClipboardType.Single)
                PlaceCacheObject(external.m_object, true);
            else
            {
                selectedGroup = null;
                ToolHelper.FullySetTool<ProceduralTool>();
                ToolsModifierControl.mainToolbar.CloseEverything();
                Paste(external.m_selection);
            }
        }
        public ProceduralObject CloneObject(ProceduralObject source)
        {
            var obj = new ProceduralObject();
            obj.id = proceduralObjects.GetNextUnusedId();
            CloneIntoObject(source, obj);
            proceduralObjects.Add(obj);
            return obj;
        }
        public void CloneIntoObject(ProceduralObject source, ProceduralObject destination)
        {
            // destination.id = proceduralObjects.GetNextUnusedId();
            destination.m_material = GameObject.Instantiate(source.m_material);
            destination.m_position = source.m_position;
            destination.m_rotation = source.m_rotation;
            destination.customTexture = source.customTexture;
            destination.layer = source.layer;
            destination.tilingFactor = source.tilingFactor;
            destination.renderDistance = source.renderDistance;
            destination.m_scale = source.m_scale;
            destination.isPloppableAsphalt = source.isPloppableAsphalt;
            destination.disableRecalculation = source.disableRecalculation;
            destination.m_textParameters = (source.m_textParameters == null) ? null : TextParameters.Clone(source.m_textParameters, false);
            destination.basePrefabName = source.basePrefabName;
            destination.baseInfoType = source.baseInfoType;
            destination._baseProp = source._baseProp;
            destination._baseBuilding = source._baseBuilding;
            destination.m_visibility = source.m_visibility;
            destination.flipFaces = source.flipFaces;
            destination.disableCastShadows = source.disableCastShadows;
            destination.normalsRecalcMode = source.normalsRecalcMode;
            destination.halfOverlayDiam = source.halfOverlayDiam;
            destination.m_color = source.m_color;

            if (source.meshStatus == 1 && source.baseInfoType == "PROP")
            {
                destination.m_mesh = source._baseProp.m_mesh;
                destination.meshStatus = 1;
            }
            else
            {
                destination.m_mesh = source.m_mesh.InstantiateMesh();
                destination.meshStatus = 2;
            }
            if (destination.isPloppableAsphalt)
                destination.m_correctedMeshPloppableAsph = source.m_correctedMeshPloppableAsph.InstantiateMesh();
            destination.vertices = Vertex.CreateVertexList(destination);
            destination.m_material.color = destination.m_color;
            destination.historyEditionBuffer = new HistoryBuffer(destination);
            destination.m_modules = (source.m_modules == null) ? new List<POModule>() : ModuleManager.instance.CloneModuleList(source.m_modules, destination);
            // proceduralObjects.Add(obj);
        }
        public void SwitchToMainTool(byte mode = 0)
        {
            editingVertexIndex.Clear();
            ProceduralUtils.ClearVertexSelection(currentlyEditingObject);
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
            drawWizardData = null;
        }
        private void EditModeBack()
        {
            textManager.CloseWindow();
            advEdManager = null;
            moduleManager.CloseWindow(false);
            if (drawWizardData != null)
                drawWizardData.Confirm();
            drawWizardData = null;
            CloseExternalsWindow();
            toolAction = ToolAction.none;
            ToolHelper.FullySetTool<ProceduralTool>();
            Gizmos.DestroyGizmo();
            editingVertex = false;
            editingVertexIndex.Clear();
            ProceduralUtils.ClearVertexSelection(currentlyEditingObject);
            editingWholeModel = false;
            proceduralTool = false;
            reselectingTex = false;
            chosenProceduralInfo = null;
            pObjSelection.Clear();
            pObjSelection.Add(currentlyEditingObject);
            SetCurrentlyEditingObj(null);
        }
        public void MainButtonClick()
        {
            if (PopupStart.IsPopupOpen()) return;

            var currentToolType = ToolsModifierControl.toolController.CurrentTool.GetType();
            ResetLayerScrollmenu();
            textManager.CloseWindow();
            advEdManager = null;
            CloseExternalsWindow();
            SelectionModeAction.CloseAction();
            ProceduralUtils.UpdatePloppableAsphaltCfg();
            editingVertex = false;
            editingVertexIndex.Clear();
            editingWholeModel = false;
            proceduralTool = false;
            reselectingTex = false;
            if (drawWizardData != null)
                drawWizardData.Confirm();
            drawWizardData = null;
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
        public void EditObject(ProceduralObject obj)
        {
            verticesToolType = 0;
            // toolAction = ToolAction.vertices;
            obj.MakeUniqueMesh();
            SetCurrentlyEditingObj(obj);
            pObjSelection.Clear();
            CloseAllSMWindows();
            proceduralTool = true;
            SingleHoveredObj = null;
            actionMode = 0;
            Gizmos.CreatePositionGizmo(currentlyEditingObject.m_position, true);
            editingVertex = false;
            editingWholeModel = true;
        }
        public void EscapePressed()
        {
            if (proceduralTool && !movingWholeModel && currentlyEditingObject != null) // leave edit mode
            { 
                if (textManager != null)
                {
                    if (textManager.showWindow)
                    {
                        textManager.CloseWindow();
                        return;
                    }
                }
                EditModeBack();
                return;
            }
            else if (ToolsModifierControl.toolController.CurrentTool is ProceduralTool && movingWholeModel) // cancel move to
            {
                var obj = (currentlyEditingObject == null) ? pObjSelection[0] : currentlyEditingObject;
                if (obj.historyEditionBuffer.currentStepType == EditingStep.StepType.moveTo)
                {
                    obj.SetPosition(obj.historyEditionBuffer.prevTempPos);
                    obj.SetRotation(obj.historyEditionBuffer.prevTempRot);
                    if (placingSelection && obj.tempObj != null)
                    {
                        obj.tempObj.transform.position = obj.historyEditionBuffer.prevTempPos;
                        obj.tempObj.transform.rotation = obj.historyEditionBuffer.prevTempRot;
                        for (int i = 0; i < obj.tempObj.transform.childCount; i++)
                        {
                            int id;
                            if (int.TryParse(obj.tempObj.transform.GetChild(i).gameObject.name, out id))
                            {
                                var po = proceduralObjects.GetObjectWithId(id);
                                if (po != null)
                                {
                                    po.SetPosition(po.tempObj.transform.position);
                                    po.SetRotation(po.tempObj.transform.rotation);
                                }
                            }
                        }
                    }
                    ConfirmMovingWhole(getBackToGeneralTool);
                }
                else
                {
                    List<ProceduralObject> delete = new List<ProceduralObject>();
                    delete.Add(obj);
                    if (placingSelection && obj.tempObj != null)
                    {
                        for (int i = 0; i < obj.tempObj.transform.childCount; i++)
                        {
                            int id;
                            if (int.TryParse(obj.tempObj.transform.GetChild(i).gameObject.name, out id))
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
            if (PopupStart.IsPopupOpen()) return;

            var type = tool.GetType();
            bool convertible = true;

            string assetname = "";
            if (type == typeof(PropTool))
            {
                var prop = ((PropTool)tool).m_prefab;
                assetname = prop.name;
                if (!prop.m_mesh.isReadable)
                    convertible = false;
            }
            else if (type == typeof(BuildingTool))
            {
                var building = ((BuildingTool)tool);
                assetname = building.name;
                if (!building.m_prefab.m_mesh.isReadable)
                    convertible = false;
            }
            if (ExPObjManager.m_defaultPOsUponConversion.ContainsKey(assetname))
            {
                PlaceExternal(ExPObjManager.m_defaultPOsUponConversion[assetname]);
                return;
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
                ProceduralInfo info = availableProceduralInfos.FirstOrDefault(pInf => pInf.propPrefab == ((PropTool)tool).m_prefab);
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
                    toolAction = ToolAction.build;
                    proceduralTool = true;
                    movingWholeModel = true;
                    toolAction = ToolAction.build;
                    placingSelection = false;
                    editingVertex = false;
                }
            }
            else if (tool.GetType() == typeof(BuildingTool))
            {
                ProceduralInfo info = availableProceduralInfos.FirstOrDefault(pInf => pInf.buildingPrefab == ((BuildingTool)tool).m_prefab);
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
                    proceduralTool = false;
                    ToolHelper.FullySetTool<DefaultTool>();
                    Gizmos.DestroyGizmo();
                    var obj = SpawnObject(info);
                    obj.m_rotation = Quaternion.Euler(0, angle % 360f, 0);
                    placingSelection = false;
                    if (ProceduralObjectsMod.IncludeSubBuildings.value)
                    {
                        if (obj._baseBuilding != null)
                        {
                            if (obj._baseBuilding.m_subBuildings.Length >= 1)
                            {
                                var group = ProceduralUtils.ConstructSubBuildings(obj);
                                if (group != null)
                                {
                                    MoveSelection(group.objects, false);
                                }
                            }
                        }
                    }
                    ToolHelper.FullySetTool<ProceduralTool>();
                    proceduralTool = true;
                    movingWholeModel = true;
                    toolAction = ToolAction.build;
                    editingWholeModel = true;
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
                    obj.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.moveTo, obj.vertices);
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
            placingSelection = true;
        }
        private void ConfirmMovingWhole(bool backToGeneralTool)
        {
            Vector3 effectPos = currentlyEditingObject.m_position;
            editingVertex = false;
            editingVertexIndex.Clear();
            ProceduralUtils.ClearVertexSelection(currentlyEditingObject);
            if (placingSelection)
            {
                pObjSelection.Clear();
                Singleton<EffectManager>.instance.DispatchEffect(Singleton<BuildingManager>.instance.m_properties.m_placementEffect,
                    new EffectInfo.SpawnArea(currentlyEditingObject.m_position, Vector3.up, 10f), Vector3.zero, 0f, 1f, Singleton<AudioManager>.instance.DefaultGroup, 0u, true);
                for (int i = 0; i < proceduralObjects.Count; i++)
                {
                    proceduralObjects[i].historyEditionBuffer.ConfirmNewStep(currentlyEditingObject.vertices);
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
                currentlyEditingObject.historyEditionBuffer.ConfirmNewStep(currentlyEditingObject.vertices);
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
            var type = currentlyEditingObject.historyEditionBuffer.UndoLastStep(currentlyEditingObject.vertices, out buffer);
            if (type == EditingStep.StepType.vertices || type == EditingStep.StepType.mirrorX || type == EditingStep.StepType.mirrorY || type == EditingStep.StepType.mirrorZ
                 || type == EditingStep.StepType.stretchX || type == EditingStep.StepType.stretchY || type == EditingStep.StepType.stretchZ)
            {
                if (type == EditingStep.StepType.vertices)
                    currentlyEditingObject.vertices = buffer;
                Apply();
            }
        }
        private void Redo()
        {
            Vertex[] buffer;
            var type = currentlyEditingObject.historyEditionBuffer.RedoUndoneStep(currentlyEditingObject.vertices, out buffer);
            if (type == EditingStep.StepType.vertices || type == EditingStep.StepType.mirrorX || type == EditingStep.StepType.mirrorY || type == EditingStep.StepType.mirrorZ
                 || type == EditingStep.StepType.stretchX || type == EditingStep.StepType.stretchY || type == EditingStep.StepType.stretchZ)
            {
                if (type == EditingStep.StepType.vertices)
                    currentlyEditingObject.vertices = buffer;
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
        private void ClosePO(bool fromPauseMenu = false)
        {
            ResetLayerScrollmenu();
            CloseExternalsWindow();
            advEdManager = null;
            fontManager.showWindow = false;
            moduleManager.CloseWindow(true);
            moduleManager.showManagerWindow = false;
            measurementsManager.CloseWindow();
            toolAction = ToolAction.none;
            editingVertex = false;
            editingVertexIndex.Clear();
            if (currentlyEditingObject != null)
                ProceduralUtils.ClearVertexSelection(currentlyEditingObject);
            editingWholeModel = false;
            proceduralTool = false;
            SetCurrentlyEditingObj(null);
            chosenProceduralInfo = null;
            reselectingTex = false;
            VerticesWizardData.DestroyLines();
            selectedGroup = null;
            selectingNewGrpRoot = false;
            vertWizardData = null;
            drawWizardData = null;
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
            if (!fromPauseMenu)
                ColossalFramework.UI.UIView.SetFocus(null);
            if (FindObjectOfType<ToolController>().CurrentTool is ProceduralTool)
                ToolHelper.FullySetTool<DefaultTool>();
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
            if (MeasurementsManager.instance.showWindow)
            {
                if (MeasurementsManager.instance.window.Contains(pos)) return true;
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
