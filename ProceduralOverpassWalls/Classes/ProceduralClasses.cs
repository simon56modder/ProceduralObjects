using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Text;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

using ColossalFramework;
using ColossalFramework.IO;

using ProceduralObjects.ProceduralText;
using ProceduralObjects.Tools;
using ProceduralObjects.UI;

namespace ProceduralObjects.Classes
{
    public class ProceduralObject
    {
        public ProceduralObject() { }
        public ProceduralObject(ProceduralObjectContainer container, LayerManager layerManager)
        {
            if (container.objectType == "PROP")
            {
                PropInfo sourceProp = Resources.FindObjectsOfTypeAll<PropInfo>().FirstOrDefault(info => info.name == container.basePrefabName);
                this._baseProp = sourceProp;
                this.id = container.id;
                this.basePrefabName = container.basePrefabName;
                this.baseInfoType = "PROP";
                this.isPloppableAsphalt = sourceProp.IsPloppableAsphalt();
                m_position = container.position.ToVector3();
                m_rotation = container.rotation.ToQuaternion();
                if (container.meshStatus == 0)
                {
                    // CHECK FOR MESH REPETITION
                    if (ProceduralUtils.CheckMeshEquivalence(container.vertices, sourceProp.m_mesh.vertices))
                    {
                        meshStatus = 1;
                        m_mesh = sourceProp.m_mesh;
                        allVertices = sourceProp.m_mesh.vertices;
                    }
                    else
                    {
                        meshStatus = 2;
                        m_mesh = sourceProp.m_mesh.InstantiateMesh();
                        allVertices = SerializableVector3.ToStandardVector3Array(container.vertices);
                        if (container.scale != 0)
                        {
                            for (int i = 0; i < allVertices.Count(); i++)
                            {
                                allVertices[i] = new Vector3(allVertices[i].x * container.scale, allVertices[i].y * container.scale, allVertices[i].z * container.scale);
                            }
                        }
                        m_mesh.SetVertices(new List<Vector3>(allVertices));
                    }
                }
                else if (container.meshStatus == 1)
                {
                    m_mesh = sourceProp.m_mesh;
                    allVertices = sourceProp.m_mesh.vertices;
                }
                else
                {
                    meshStatus = 2;
                    m_mesh = sourceProp.m_mesh.InstantiateMesh();
                    allVertices = SerializableVector3.ToStandardVector3Array(container.vertices);
                    if (container.scale != 0)
                    {
                        for (int i = 0; i < allVertices.Count(); i++)
                        {
                            allVertices[i] = new Vector3(allVertices[i].x * container.scale, allVertices[i].y * container.scale, allVertices[i].z * container.scale);
                        }
                    }
                    m_mesh.SetVertices(new List<Vector3>(allVertices));
                }
                m_material = GameObject.Instantiate(sourceProp.m_material); // overkil ??
                if (sourceProp.m_mesh.name == "ploppableasphalt-prop" || sourceProp.m_mesh.name == "ploppableasphalt-decal")
                    m_color = m_material.ApplyPloppableColor();
                if (container.hasCustomTexture && TextureManager.instance != null)
                {
                    var customTex = TextureManager.instance.FindTexture(container.customTextureName);
                    m_material.mainTexture = customTex as Texture;
                    customTexture = customTex;
                }
            }
            else if (container.objectType == "BUILDING")// building
            {
                BuildingInfo sourceProp = Resources.FindObjectsOfTypeAll<BuildingInfo>().FirstOrDefault(info => info.name == container.basePrefabName);
                this._baseBuilding = sourceProp;
                this.id = container.id;
                this.basePrefabName = container.basePrefabName;
                this.baseInfoType = "BUILDING";
                this.isPloppableAsphalt = false;
                m_position = container.position.ToVector3();
                m_rotation = container.rotation.ToQuaternion();
                meshStatus = 2;
                m_mesh = sourceProp.m_mesh.InstantiateMesh();
                allVertices = SerializableVector3.ToStandardVector3Array(container.vertices);
                if (container.scale != 0)
                {
                    for (int i = 0; i < allVertices.Count(); i++)
                    {
                        allVertices[i] = new Vector3(allVertices[i].x * container.scale, allVertices[i].y * container.scale, allVertices[i].z * container.scale);
                    }
                }
                m_mesh.SetVertices(new List<Vector3>(allVertices));
                m_mesh.colors = new Color[] { };
                m_mesh.colors32 = new Color32[] { };

                m_material = GameObject.Instantiate(sourceProp.m_material); // overkill ??
                if (container.hasCustomTexture && TextureManager.instance != null)
                {
                    var customTex = TextureManager.instance.FindTexture(container.customTextureName);
                    m_material.mainTexture = customTex as Texture;
                    customTexture = customTex;
                }
            }
            m_visibility = container.visibility;
            renderDistance = container.renderDistance;
            renderDistLocked = container.renderDistLocked;
            if (container.textParam != null)
            {
                m_textParameters = TextParameters.Clone(container.textParam, true);
                for (int i = 0; i < m_textParameters.Count(); i++)
                {
                    if (m_textParameters[i].m_fontColor == null)
                    {
                        if (m_textParameters[i].serializableColor != null)
                            m_textParameters[i].m_fontColor = m_textParameters[i].serializableColor.ToColor();
                        else
                            m_textParameters[i].m_fontColor = Color.white;
                    }
                }
              //  m_textParameters.SetFonts();
                var originalTex = new Texture2D(m_material.mainTexture.width, m_material.mainTexture.height, TextureFormat.RGBA32, false);
                originalTex.SetPixels(((Texture2D)m_material.mainTexture).GetPixels());
                originalTex.Apply();
                m_material.mainTexture = m_textParameters.ApplyParameters(originalTex) as Texture;
            }
            else
                m_textParameters = null;
            if (container.belongsToGroup)
            {
                if (container.groupRootId == -1)
                {
                    isRootOfGroup = true;
                    _groupRootIdData = -1;
                }
                else
                {
                    _groupRootIdData = container.groupRootId;
                    isRootOfGroup = false;
                }
            }
            else
            {
                _groupRootIdData = -2;
                group = null;
                isRootOfGroup = false;
            }

            disableRecalculation = container.disableRecalculation;
            this.normalsRecalcMode = container.normalsRecalculation;
            this.flipFaces = container.flipFaces;
            if (this.flipFaces)
               VertexUtils.flipFaces(this);
            historyEditionBuffer = new HistoryBuffer(this);

            if (container.color == null)
            {
                if (!(m_mesh.name == "ploppableasphalt-prop" || m_mesh.name == "ploppableasphalt-decal"))
                    m_color = Color.white;
            }
            else
            {
                m_color = container.color;
                m_material.color = m_color;
            }

            if (container.layerId != 0)
            {
                if (layerManager.m_layers.Any(l => l.m_id == container.layerId))
                    layer = layerManager.m_layers.Single(l => l.m_id == container.layerId);
                else
                    Debug.LogError("[ProceduralObjects] Layer of an object not found !");
            }
            else
                layer = null;
            if (container.tilingFactor == 0)
                this.tilingFactor = 8;
            else
                this.tilingFactor = container.tilingFactor;

            m_modules = ModuleManager.LoadModulesFromData(container.modulesData, true, this);
        }
        public ProceduralObject(CacheProceduralObject sourceCacheObj, int id, Vector3 position, LayerManager layerManager)
        {
            if (sourceCacheObj.baseInfoType == "PROP")
            {
                PropInfo sourceProp = sourceCacheObj._baseProp;
                if (sourceCacheObj._baseProp == null)
                    sourceProp = Resources.FindObjectsOfTypeAll<PropInfo>().FirstOrDefault(info => info.name == sourceCacheObj.basePrefabName);
                this._baseProp = sourceProp;
                this.id = id;
                this.basePrefabName = sourceCacheObj.basePrefabName;
                this.baseInfoType = "PROP";
                this.isPloppableAsphalt = sourceProp.IsPloppableAsphalt();
                if (sourceCacheObj.meshStatus == 2)
                {
                    m_mesh = sourceProp.m_mesh.InstantiateMesh();
                    allVertices = sourceCacheObj.allVertices;
                    m_mesh.SetVertices(new List<Vector3>(allVertices));
                    meshStatus = 2;
                }
                else
                {
                    meshStatus = 1;
                    m_mesh = sourceProp.m_mesh;
                }
                m_material = GameObject.Instantiate(sourceProp.m_material);
                if (sourceProp.m_mesh.name == "ploppableasphalt-prop" || sourceProp.m_mesh.name == "ploppableasphalt-decal")
                    m_color = m_material.ApplyPloppableColor();
            }
            else if (sourceCacheObj.baseInfoType == "BUILDING")// building
            {
                BuildingInfo sourceBuilding = sourceCacheObj._baseBuilding;
                if (sourceCacheObj._baseBuilding == null)
                    sourceBuilding = Resources.FindObjectsOfTypeAll<BuildingInfo>().FirstOrDefault(info => info.name == sourceCacheObj.basePrefabName);
                this._baseBuilding = sourceBuilding;
                this.id = id;
                this.basePrefabName = sourceCacheObj.basePrefabName;
                this.baseInfoType = "BUILDING";
                this.isPloppableAsphalt = false;
                m_mesh = sourceBuilding.m_mesh.InstantiateMesh();
                allVertices = sourceCacheObj.allVertices;
                meshStatus = 2;
                m_mesh.SetVertices(new List<Vector3>(allVertices));
                m_mesh.colors = new Color[] { };
                m_mesh.colors32 = new Color32[] { };
                m_material = GameObject.Instantiate(sourceBuilding.m_material);
            }
            if (sourceCacheObj.customTexture != null)
            {
                m_material.mainTexture = sourceCacheObj.customTexture as Texture;
                customTexture = sourceCacheObj.customTexture;
            }
            m_visibility = sourceCacheObj.visibility;
            historyEditionBuffer = new HistoryBuffer(this);
            if (sourceCacheObj.textParam != null)
            {
                m_textParameters = TextParameters.Clone(sourceCacheObj.textParam, false);
                // m_textParameters.SetFonts();
                var originalTex = new Texture2D(m_material.mainTexture.width, m_material.mainTexture.height, TextureFormat.RGBA32, false);
                originalTex.SetPixels(((Texture2D)m_material.mainTexture).GetPixels());
                originalTex.Apply();
                m_material.mainTexture = m_textParameters.ApplyParameters(originalTex);
            }
            else
                m_textParameters = null;
            if (sourceCacheObj.layer != null)
            {
                if (layerManager.m_layers.Contains(sourceCacheObj.layer))
                    layer = sourceCacheObj.layer;
            }
            m_color = sourceCacheObj.m_color;
            m_material.color = m_color;
            this.flipFaces = sourceCacheObj.flipFaces;
            if (this.flipFaces)
                VertexUtils.flipFaces(this);
            renderDistance = sourceCacheObj.renderDistance;
            renderDistLocked = sourceCacheObj.renderDistLocked;
            m_position = position;
            m_rotation = sourceCacheObj.m_rotation;
            disableRecalculation = sourceCacheObj.disableRecalculation;
            normalsRecalcMode = sourceCacheObj.normalsRecalculation;
            this.tilingFactor = sourceCacheObj.tilingFactor;
            if (sourceCacheObj.modules != null)
            {
                m_modules = new List<POModule>();
                foreach (var m in sourceCacheObj.modules)
                {
                    POModule clone;
                    try { clone = m.Clone(); }
                    catch (Exception e)
                    {
                        Debug.LogError("[ProceduralObjects] Error inside module Clone() method!\n" + e);
                        continue;
                    }
                    ModuleManager.instance.modules.Add(clone);
                    if (clone.enabled)
                        ModuleManager.instance.enabledModules.Add(clone);
                    clone.parentObject = this;
                    try { clone.OnModuleCreated(ProceduralObjectsLogic.instance); }
                    catch (Exception e) { Debug.LogError("[ProceduralObjects] Error inside module OnModuleCreated() method!\n" + e); }
                    m_modules.Add(clone);
                }
            }
        }

        public void ConstructObject(PropInfo sourceProp, int id, Texture customTex = null, bool skipDecalShrink = false)
        {
            this._baseProp = sourceProp;
            this.id = id;
            this.basePrefabName = sourceProp.name;
            this.isPloppableAsphalt = sourceProp.IsPloppableAsphalt();
            this.baseInfoType = "PROP";
            // this.flipFaces = false;
            this.tilingFactor = 8;
            m_position = ToolsModifierControl.cameraController.m_currentPosition;
            m_rotation = Quaternion.identity;
           // Mesh mesh = sourceProp.m_mesh.InstantiateMesh();
            // meshStatus = 1;
            if (sourceProp.m_isDecal && ProceduralObjectsMod.AutoResizeDecals.value && !skipDecalShrink)
            {
                m_mesh = sourceProp.m_mesh.InstantiateMesh();
                m_mesh.SetVertices(m_mesh.vertices.ResizeDecal());
                allVertices = sourceProp.m_mesh.vertices;
                meshStatus = 2;
            }
            else
            {
                m_mesh = sourceProp.m_mesh;
                meshStatus = 1;
                if (isPloppableAsphalt)
                    RecalculateBoundsNormalsExtras(1);
            }
            this.renderDistance = RenderOptions.instance.CalculateRenderDistance(this, true);
            m_material = GameObject.Instantiate(sourceProp.m_material);
            m_visibility = ProceduralObjectVisibility.Always;
            historyEditionBuffer = new HistoryBuffer(this);
            normalsRecalcMode = NormalsRecalculation.None;
            if (_baseProp.m_useColorVariations && ProceduralObjectsMod.UseColorVariation.value)
            {
                m_color = _baseProp.GetColor(ref Singleton<SimulationManager>.instance.m_randomizer);
                m_material.color = m_color;
            }
            else
            {
                if (sourceProp.m_mesh.name == "ploppableasphalt-prop" || sourceProp.m_mesh.name == "ploppableasphalt-decal")
                    m_color = m_material.ApplyPloppableColor();
                else
                    m_color = Color.white;
            }
            if (customTex != null)
            {
                m_material.mainTexture = customTex as Texture;
                customTexture = customTex;
                disableRecalculation = false;
            }
            m_modules = new List<POModule>();
        }
        public void ConstructObject(BuildingInfo sourceBuilding, int id, Texture customTex = null)
        {
            this._baseBuilding = sourceBuilding;
            this.id = id;
            this.basePrefabName = sourceBuilding.name;
            this.isPloppableAsphalt = false;
            this.baseInfoType = "BUILDING";
           // this.flipFaces = false;
            // this.recalculateNormals = true;
            // this.tilingFactor = 8;
            m_position = ToolsModifierControl.cameraController.m_currentPosition;
            m_rotation = Quaternion.identity;
            // m_mesh = sourceBuilding.m_mesh.InstantiateMesh();
            // meshStatus = 1;
            m_mesh = sourceBuilding.m_mesh.InstantiateMesh();
            m_mesh.colors = new Color[] { };
            m_mesh.colors32 = new Color32[] { };
            allVertices = m_mesh.vertices;
            meshStatus = 2;
            this.renderDistance = RenderOptions.instance.CalculateRenderDistance(this, true);
            m_material = GameObject.Instantiate(sourceBuilding.m_material);
            m_visibility = ProceduralObjectVisibility.Always;
            historyEditionBuffer = new HistoryBuffer(this);
            normalsRecalcMode = NormalsRecalculation.None;
            if (_baseBuilding.m_useColorVariations && ProceduralObjectsMod.UseColorVariation.value)
            {
                m_color = _baseBuilding.GetColor();
                m_material.color = m_color;
            }
            else
                m_color = Color.white;
            if (customTex != null)
            {
                m_material.mainTexture = customTex as Texture;
                customTexture = customTex;
                disableRecalculation = false;
            }
            m_modules = new List<POModule>();
        }

        public void ChangeNormalsRecalc()
        {
            if (normalsRecalcMode == NormalsRecalculation.None)
            {
                normalsRecalcMode = NormalsRecalculation.Default;
                m_mesh.RecalculateNormals();
            }
            else if (normalsRecalcMode == NormalsRecalculation.Default)
            {
                normalsRecalcMode = NormalsRecalculation.Tolerance60;
                m_mesh.RecalculateNormals(60);
            }
            else if (normalsRecalcMode == NormalsRecalculation.Tolerance60)
            {
                normalsRecalcMode = NormalsRecalculation.Tolerance30;
                m_mesh.RecalculateNormals(30);
            }
            else if (normalsRecalcMode == NormalsRecalculation.Tolerance30)
            {
                normalsRecalcMode = NormalsRecalculation.Tolerance0;
                m_mesh.RecalculateNormals(0);
            }
            else if (normalsRecalcMode == NormalsRecalculation.Tolerance0)
            {
                if (baseInfoType == "BUILDING")
                    m_mesh.SetNormals(_baseBuilding.m_mesh.normals.ToList());
                else if (baseInfoType == "PROP")
                    m_mesh.SetNormals(_baseProp.m_mesh.normals.ToList());
                normalsRecalcMode = NormalsRecalculation.None;
            }
        }
        public void RecalculateBoundsNormalsExtras(byte meshStatus)
        {
            if (!isPloppableAsphalt && meshStatus != 1)
            {
                this.RecalculateNormals();
                this.m_mesh.RecalculateBounds();
            }
            else if (isPloppableAsphalt)
            {
                Bounds b;
                m_correctedMeshPloppableAsph = VertexUtils.GetCorrectedMeshPloppableAsph(m_mesh, m_correctedMeshPloppableAsph, out b);
                m_mesh.bounds = b;
            }
            halfOverlayDiam = Mathf.Max(m_mesh.bounds.extents.x, m_mesh.bounds.extents.z);
        }
        public Mesh overlayRenderMesh
        {
            get
            {
                if (isPloppableAsphalt && (m_correctedMeshPloppableAsph != null))
                    return m_correctedMeshPloppableAsph;
                return m_mesh;
            }
        }

        public Mesh m_mesh, m_correctedMeshPloppableAsph;
        public Material m_material;
        public Vector3 m_position;
        public Quaternion m_rotation;
        public Texture customTexture;
        public Vector3[] allVertices;
        public Layer layer;
        public string basePrefabName, baseInfoType;
        public int id, tilingFactor;
        // mesh status : 0=undefined ; 1=equivalent to source; 2=custom (see m_mesh)
        public byte meshStatus;
        public float renderDistance, m_scale, halfOverlayDiam;
        public bool isPloppableAsphalt, disableRecalculation, renderDistLocked, flipFaces, _insideRenderView, _insideUIview, _selected;
        public ProceduralObjectVisibility m_visibility;
        public NormalsRecalculation normalsRecalcMode;
        public Color m_color;
        public TextParameters m_textParameters;
        public List<POModule> m_modules;

        public POGroup group;
        public bool isRootOfGroup;
        public int _groupRootIdData;

        public GUIUtils.FloatInputField[] transformInputFields;

        public PropInfo _baseProp;
        public BuildingInfo _baseBuilding;

        public GameObject tempObj;

        public HistoryBuffer historyEditionBuffer;

        public bool RequiresUVRecalculation
        {
            get 
            {
                return (this.basePrefabName == "1094334744.Cube_Data" || this.basePrefabName == "1094334744.Square_Data"
                     || this.basePrefabName == "NativeCube_Procedural.Cube_Data" || this.basePrefabName == "NativeSquare_Procedural.Square_Data");
            }
        }

    }

    public enum ProceduralObjectVisibility
    {
        Always,
        NightOnly,
        DayOnly
    }
    public enum NormalsRecalculation
    {
        Tolerance60 = 0,
        None = 1,
        Default = 2,
        Tolerance30 = 3,
        Tolerance0 = 4
    }

    public class CacheProceduralObject
    {
        public CacheProceduralObject() { }
        public CacheProceduralObject(ProceduralObject sourceObj)
        {
            renderDistance = sourceObj.renderDistance;
            renderDistLocked = sourceObj.renderDistLocked;
            if (sourceObj.meshStatus == 2)
            {
                allVertices = sourceObj.m_mesh.vertices;
                meshStatus = 2;
            }
            else
                meshStatus = 1;
            customTexture = sourceObj.customTexture;
            _staticPos = sourceObj.m_position;
            m_rotation = sourceObj.m_rotation;
            basePrefabName = sourceObj.basePrefabName;
            baseInfoType = sourceObj.baseInfoType;
            layer = sourceObj.layer;
            m_color = sourceObj.m_color;
            flipFaces = sourceObj.flipFaces;
            normalsRecalculation = sourceObj.normalsRecalcMode;
            tilingFactor = sourceObj.tilingFactor;
            switch (baseInfoType)
            {
                case "PROP":
                    _baseProp = sourceObj._baseProp;
                    break;
                case "BUILDING":
                    _baseBuilding = sourceObj._baseBuilding;
                    break;
            }
            visibility = sourceObj.m_visibility;
            textParam = TextParameters.Clone(sourceObj.m_textParameters, false);
            disableRecalculation = sourceObj.disableRecalculation;
            if (sourceObj.m_modules != null)
            {
                modules = new List<POModule>();
                foreach (var m in sourceObj.m_modules)
                {
                    try { modules.Add(m.Clone()); }
                    catch (Exception e) { Debug.LogError("[ProceduralObjects] Error inside module Clone() method!\n" + e); }
                }
            }
        }

        public PropInfo _baseProp;
        public BuildingInfo _baseBuilding;
        public List<POModule> modules; 
        public Color m_color;
        public Layer layer;
        public float renderDistance;
        public bool isPloppableAsphalt, disableRecalculation, renderDistLocked, flipFaces;
        public int tilingFactor;
        public byte meshStatus;
        public Quaternion m_rotation;
        public Vector3 _staticPos;
        public Texture customTexture;
        public string basePrefabName, baseInfoType;
        public Vector3[] allVertices;
        public ProceduralObjectVisibility visibility;
        public NormalsRecalculation normalsRecalculation;
        public TextParameters textParam;
    }
    
    public class ProceduralInfo
    {
        public ProceduralInfo() { }
        public ProceduralInfo(PropInfo info, bool basic)
        {
            isBasicShape = basic;
            propPrefab = info;
            infoType = "PROP";
            isReadable = false;
            if (info != null)
            {
                if (info.m_mesh != null)
                    isReadable = info.m_mesh.isReadable;
            }
            // isReadable = info.m_mesh.isReadable;
           // if (isReadable)
            //     vertices = Vertex.CreateVertexList(info);
        }
        public ProceduralInfo(BuildingInfo info, bool basic)
        {
            isBasicShape = basic;
            buildingPrefab = info;
            infoType = "BUILDING";
            isReadable = false;
            if (info != null)
            {
                if (info.m_mesh != null)
                    isReadable = info.m_mesh.isReadable;
            }
            // isReadable = info.m_mesh.isReadable;
            // if (isReadable)
            //     vertices = Vertex.CreateVertexList(info);
        }
        public PropInfo propPrefab;
        public BuildingInfo buildingPrefab;
        public bool isBasicShape, isReadable;
        public string infoType;
        public Vertex[] vertices;
    }

}
