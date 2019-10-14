using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Text;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using ColossalFramework.IO;

using ProceduralObjects.ProceduralText;
using ProceduralObjects.Tools;

namespace ProceduralObjects.Classes
{
    public class ProceduralObject
    {
        public ProceduralObject() { }
        public ProceduralObject(ProceduralObjectContainer container, List<Texture2D> textures, LayerManager layerManager)
        {
            if (container.objectType == "PROP")
            {
                PropInfo sourceProp = Resources.FindObjectsOfTypeAll<PropInfo>().FirstOrDefault(info => info.name == container.basePrefabName);
                this._baseProp = sourceProp;
                this.id = container.id;
                this.basePrefabName = container.basePrefabName;
                this.baseInfoType = "PROP";
                this.isPloppableAsphalt = sourceProp.IsPloppableAsphalt();
                renderDistance = container.renderDistance;
                m_position = container.position.ToVector3();
                m_rotation = container.rotation.ToQuaternion();
                    /*
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
                    // }
                }
                else if (container.meshStatus == 1)
                {
                    m_mesh = sourceProp.m_mesh;
                    allVertices = sourceProp.m_mesh.vertices;
                }
                else if (container.meshStatus == 2)
                { */
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
               // }
                m_material = GameObject.Instantiate(sourceProp.m_material); // overkil ??
                if (sourceProp.m_mesh.name == "ploppableasphalt-prop" || sourceProp.m_mesh.name == "ploppableasphalt-decal")
                    m_material.ApplyPloppableColor();
                if (container.hasCustomTexture && textures != null)
                {
                    if (!textures.Any(tex => tex.name == container.customTextureName))
                        Debug.LogError("[ProceduralObjects] An object was found with a texture that doesn't exist anymore with the name " + container.customTextureName + ", therefore loading the default object texture");
                    else
                    {
                        var customTex = textures.FirstOrDefault(tex => tex.name == container.customTextureName);
                        m_material.mainTexture = customTex as Texture;
                        customTexture = customTex;
                    }
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
                renderDistance = container.renderDistance;
                m_position = container.position.ToVector3();
                m_rotation = container.rotation.ToQuaternion();
                    /*
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
                    //  m_mesh.colors = new Color[] { };
                    //  m_mesh.colors32 = new Color32[] { };}
                    // }
                }
                else if (container.meshStatus == 1)
                {
                    m_mesh = sourceProp.m_mesh;
                    allVertices = sourceProp.m_mesh.vertices;
                }
                else if (container.meshStatus == 2)
                {*/
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
               // }

                m_material = GameObject.Instantiate(sourceProp.m_material); // overkill ??
                if (container.hasCustomTexture && textures != null)
                {
                    if (!textures.Any(tex => tex.name == container.customTextureName))
                        Debug.LogError("[ProceduralObjects] An object was found with a texture that doesn't exist anymore at the specified path " + container.hasCustomTexture + ", therefore loading the default object texture");
                    else
                    {
                        var customTex = textures.FirstOrDefault(tex => tex.name == container.customTextureName);
                        m_material.mainTexture = customTex as Texture;
                        customTexture = customTex;
                    }
                }
            }
            m_visibility = container.visibility;
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
            disableRecalculation = container.disableRecalculation;
         //  this.flipFaces = container.flipFaces;
         //  if (this.flipFaces)
         //      VertexUtils.flipFaces(this);
            historyEditionBuffer = new HistoryBuffer(this);
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
        }
        public ProceduralObject(CacheProceduralObject sourceCacheObj, int id, Vector3 position)
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
                renderDistance = sourceCacheObj.renderDistance;
                m_position = position;
                m_rotation = sourceCacheObj.m_rotation;
                m_mesh = sourceProp.m_mesh.InstantiateMesh();
                allVertices = sourceCacheObj.allVertices;
                m_mesh.SetVertices(new List<Vector3>(allVertices));
                m_material = GameObject.Instantiate(sourceProp.m_material);
                if (sourceProp.m_mesh.name == "ploppableasphalt-prop" || sourceProp.m_mesh.name == "ploppableasphalt-decal")
                    m_material.ApplyPloppableColor();
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
                renderDistance = sourceCacheObj.renderDistance;
                m_position = position;
                m_rotation = sourceCacheObj.m_rotation;
                m_mesh = sourceBuilding.m_mesh.InstantiateMesh();
                allVertices = sourceCacheObj.allVertices;
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
           // this.flipFaces = sourceCacheObj.flipFaces;
           // if (this.flipFaces)
            //      VertexUtils.flipFaces(this);
            disableRecalculation = sourceCacheObj.disableRecalculation;
            this.tilingFactor = sourceCacheObj.tilingFactor;
        }

        public void ConstructObject(PropInfo sourceProp, int id, Texture2D customTex = null)
        {
            this._baseProp = sourceProp;
            this.id = id;
            this.basePrefabName = sourceProp.name;
            this.isPloppableAsphalt = sourceProp.IsPloppableAsphalt();
            this.baseInfoType = "PROP";
            this.renderDistance = ProceduralObjectsMod.PropRenderDistance.value;
            // this.flipFaces = false;
            this.tilingFactor = 8;
            m_position = ToolsModifierControl.cameraController.m_currentPosition;
            m_rotation = Quaternion.identity;
           // Mesh mesh = sourceProp.m_mesh.InstantiateMesh();
            // meshStatus = 1;
            m_mesh = sourceProp.m_mesh.InstantiateMesh();
            allVertices = sourceProp.m_mesh.vertices;
            m_material = GameObject.Instantiate(sourceProp.m_material);
            if (sourceProp.m_mesh.name == "ploppableasphalt-prop" || sourceProp.m_mesh.name == "ploppableasphalt-decal")
                m_material.ApplyPloppableColor();
            m_visibility = ProceduralObjectVisibility.Always;
            historyEditionBuffer = new HistoryBuffer(this);
            if (customTex != null)
            {
                m_material.mainTexture = customTex as Texture;
                customTexture = customTex;
                disableRecalculation = false;
            }
        }
        public void ConstructObject(BuildingInfo sourceBuilding, int id, Texture2D customTex = null)
        {
            this._baseBuilding = sourceBuilding;
            this.id = id;
            this.basePrefabName = sourceBuilding.name;
            this.isPloppableAsphalt = false;
            this.baseInfoType = "BUILDING";
            this.renderDistance = ProceduralObjectsMod.BuildingRenderDistance.value;
            //  this.flipFaces = false;
            this.tilingFactor = 8;
            m_position = ToolsModifierControl.cameraController.m_currentPosition;
            m_rotation = Quaternion.identity;
            // m_mesh = sourceBuilding.m_mesh.InstantiateMesh();
            // meshStatus = 1;
            m_mesh = sourceBuilding.m_mesh.InstantiateMesh();
            m_mesh.colors = new Color[] { };
            m_mesh.colors32 = new Color32[] { };
            allVertices = m_mesh.vertices;
            m_material = GameObject.Instantiate(sourceBuilding.m_material);
            m_visibility = ProceduralObjectVisibility.Always;
            historyEditionBuffer = new HistoryBuffer(this);
            if (customTex != null)
            {
                m_material.mainTexture = customTex as Texture;
                customTexture = customTex;
                disableRecalculation = false;
            }
        }

        public Mesh m_mesh;
        public Material m_material;
        public Vector3 m_position;
        public Quaternion m_rotation;
        public Texture2D customTexture;
        public Vector3[] allVertices;
        public Layer layer;
        public string basePrefabName, baseInfoType;
        public int id, tilingFactor;
        // mesh status : 0=undefined ; 1=equivalent to source; 2=custom (see m_mesh)
        // public byte meshStatus;
        public float renderDistance, m_scale;
        public bool isPloppableAsphalt/*, flipFaces*/, disableRecalculation, _insideRenderView, _insideUIview;
        public ProceduralObjectVisibility m_visibility;
        public TextParameters m_textParameters;

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

    public class CacheProceduralObject
    {
        public CacheProceduralObject() { }
        public CacheProceduralObject(ProceduralObject sourceObj)
        {
            renderDistance = sourceObj.renderDistance;
            allVertices = sourceObj.m_mesh.vertices;
            customTexture = sourceObj.customTexture;
            m_rotation = sourceObj.m_rotation;
            basePrefabName = sourceObj.basePrefabName;
            baseInfoType = sourceObj.baseInfoType;
            //  flipFaces = sourceObj.flipFaces;
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
        }

        public PropInfo _baseProp;
        public BuildingInfo _baseBuilding;
        public float renderDistance;
        public bool isPloppableAsphalt, disableRecalculation/*, flipFaces*/;
        public int tilingFactor;
        public Quaternion m_rotation;
        public Texture2D customTexture;
        public string basePrefabName, baseInfoType;
        public Vector3[] allVertices;
        public ProceduralObjectVisibility visibility;
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

        public string GetShowName()
        {
            if (infoType == "PROP")
                return propPrefab.GetLocalizedTitle() + (isBasicShape ? " (Basic)" : string.Empty);
            else // if (infoType == "BUILDING")
                return buildingPrefab.GetLocalizedTitle() + (isBasicShape ? " (Basic)" : string.Empty);
        }
    }

    public static class ProceduralUtils
    {
        public static int GetNextUnusedId(this List<ProceduralObject> list)
        {
            ProceduralObjectsLogic logic = ProceduralObjectsMod.gameLogicObject.GetComponent<ProceduralObjectsLogic>();
            for (int i = 0; true; i++)
            {
                if (!logic.activeIds.Contains(i))
                {
                    logic.activeIds.Add(i);
                    return i;
                }
            }
        }
        public static Vector2 WorldToGuiPoint(this Vector3 position)
        {
            var guiPosition = Camera.main.WorldToScreenPoint(position);
            guiPosition.y = Screen.height - guiPosition.y;
            return new Vector2(guiPosition.x, guiPosition.y);
        }
        public static void SnapToGround(this ProceduralObject obj)
        {
            obj.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.position, null);
            ToolBase.RaycastInput rayInput = new ToolBase.RaycastInput(new Ray(obj.m_position, Vector3.down), 10000);
            ToolBase.RaycastOutput rayOutput;
            if (ProceduralTool.TerrainRaycast(rayInput, out rayOutput))
                obj.m_position = rayOutput.m_hitPos;
            else
            {
                rayInput = new ToolBase.RaycastInput(new Ray(obj.m_position, Vector3.up), 10000);
                if (ProceduralTool.TerrainRaycast(rayInput, out rayOutput))
                    obj.m_position = rayOutput.m_hitPos;
            }
            obj.historyEditionBuffer.ConfirmNewStep(null);
        }
        /*
        public static void MakeMeshUnique(this ProceduralObject obj)
        {
            obj.meshStatus = 2;
            obj.m_mesh = obj.m_mesh.InstantiateMesh();
            obj.allVertices = obj.m_mesh.vertices;
          //  obj.m_mesh.SetVertices(new List<Vector3>(obj.allVertices));
        } */
        public static ProceduralObjectContainer[] GetContainerList(this ProceduralObjectsLogic logic)
        {
            var list = new List<ProceduralObjectContainer>();
            if (logic.proceduralObjects == null)
                return null;
            foreach (ProceduralObject obj in logic.proceduralObjects)
            {
                list.Add(new ProceduralObjectContainer(obj));
            }
            return list.ToArray();
        }

        public static void LoadContainerData(this ProceduralObjectsLogic logic, ProceduralObjectContainer[] containerArray)
        {
            logic.proceduralObjects = new List<ProceduralObject>();
            logic.activeIds = new HashSet<int>();
            if (logic.availableProceduralInfos == null)
                logic.availableProceduralInfos = CreateProceduralInfosList();
            if (logic.availableProceduralInfos.Count < 0 )
                logic.availableProceduralInfos = CreateProceduralInfosList();

            foreach (var c in containerArray)
            {
                try
                {
                    var obj = new ProceduralObject(c, logic.basicTextures, logic.layerManager);
                    if (obj.RequiresUVRecalculation && !obj.disableRecalculation)
                    {
                        obj.m_mesh.uv = Vertex.RecalculateUVMap(obj, Vertex.CreateVertexList(obj));
                    }
                    if (!obj.isPloppableAsphalt)
                    {
                        obj.m_mesh.RecalculateNormals(60);
                        obj.m_mesh.RecalculateBounds();
                    }
                    logic.proceduralObjects.Add(obj);
                    logic.activeIds.Add(obj.id);
                }
                catch (Exception e)
                {
                    Debug.LogError("[ProceduralObjects] Failed to load a Procedural Object : \n" + e.GetType().ToString() + " : " + e.Message + "\n" + e.StackTrace);
                }
            }
        }
        public static Mesh InstantiateMesh(this Mesh source)
        {
            if (source == null)
                return null;
            var m = new Mesh();
            m.name = source.name;
            m.vertices = source.vertices;
            m.triangles = source.triangles;
            m.uv = source.uv;
            m.uv2 = source.uv2;
            m.uv3 = source.uv3;
            m.uv4 = source.uv4;
            m.tangents = source.tangents;
            m.bindposes = source.bindposes;
            m.colors = source.colors;
            m.colors32 = source.colors32;
            m.normals = source.normals;
            return m;
        }
        /*
        public static bool CheckMeshEquivalence(SerializableVector3[] savedVertices, Vector3[] meshVertices)
        {
            // avoid NullRefException, suppose they are different
            if (savedVertices == null || meshVertices == null)
                return false;

            if (savedVertices.Length != meshVertices.Length)
                return false;
            for (int i = 0; i < savedVertices.Length; i++)
            {
                if (savedVertices[i].ToVector3() != meshVertices[i])
                    return false;
            }
            // return true if everything is equivalent
            return true;
        } */
        public static Dictionary<ProceduralObject, Vector3> ConstructSubBuildings(ProceduralObject obj, List<ProceduralObject> allObjects)
        {
            if (obj.baseInfoType != "BUILDING")
            {
                var d = new Dictionary<ProceduralObject, Vector3>();
                d.Add(obj, Vector3.zero);
                return d;
            }
            var dict = new Dictionary<ProceduralObject, Vector3>();
            dict.Add(obj, Vector3.zero);
            var subs = obj._baseBuilding.m_subBuildings;
            for (int i = 0; i < subs.Length; i++)
            {
                if (subs[i] == null)
                    continue;
                if (subs[i].m_buildingInfo == null)
                    continue;
                ProceduralObject sub = new ProceduralObject();
                sub.ConstructObject(subs[i].m_buildingInfo, allObjects.GetNextUnusedId());
                float a = -(subs[i].m_angle * Mathf.Rad2Deg) % 360f;
                if (a < 0) a += 360f;
                sub.m_rotation.eulerAngles = new Vector3(sub.m_rotation.eulerAngles.x, a, sub.m_rotation.eulerAngles.z);
                sub.m_position = obj.m_position + subs[i].m_position;
                dict.Add(sub, subs[i].m_position);
            }
            return dict;
        }
        public static ProceduralInfo[] ToProceduralInfoArray(this IEnumerable<PropInfo> source)
        {
            var list = new List<ProceduralInfo>();
            foreach (PropInfo info in source)
            {
                list.Add(new ProceduralInfo(info, info.GetLocalizedDescription().ToLower().Contains("basic")));
            }
            return list.ToArray();
        }
        public static ProceduralInfo[] ToProceduralInfoArray(this IEnumerable<BuildingInfo> source)
        {
            var list = new List<ProceduralInfo>();
            foreach (BuildingInfo info in source)
            {
                list.Add(new ProceduralInfo(info, info.GetLocalizedDescription().ToLower().Contains("basic")));
            }
            return list.ToArray();
        }
        public static Texture GetOriginalTexture(ProceduralObject obj)
        {
            if (obj.customTexture)
                return obj.customTexture as Texture; 
            if (obj.baseInfoType == "PROP")
                return obj._baseProp.m_material.mainTexture;
            else
                return obj._baseBuilding.m_material.mainTexture;
        }
        public static List<ProceduralInfo> CreateProceduralInfosList()
        {
            try
            {
                return new List<ProceduralInfo>(new List<ProceduralInfo>(Resources.FindObjectsOfTypeAll<PropInfo>().ToProceduralInfoArray())
                    .Concat(new List<ProceduralInfo>(Resources.FindObjectsOfTypeAll<BuildingInfo>().ToProceduralInfoArray())));
            }
            catch
            {
                Debug.LogError("[ProceduralObjects] Fatal Loading exception : couldn't find all assets and make them procedural objects !");
            }
            return new List<ProceduralInfo>();
        }
        public static bool IsPloppableAsphalt(this PropInfo sourceProp)
        {
            return (sourceProp.m_mesh.name == "ploppableasphalt-prop") ||
                (sourceProp.m_mesh.name == "ploppablecliffgrass") ||
                (sourceProp.m_mesh.name == "ploppablegravel");
        }
        public static Color GetPloppableAsphaltCfg()
        {
            string path = Path.Combine(DataLocation.localApplicationData, "PloppableAsphalt.xml");
            if (!File.Exists(path))
                return new Color(.5f, .5f, .5f, 1f);

            string r = "", g = "", b = "";
            using (XmlReader reader = XmlReader.Create(path))
            {
                while (reader.Read())
                {
                    if (reader.IsStartElement())
                    {
                        switch (reader.Name)
                        {
                            case "r":
                                r = reader.ReadString();
                                break;
                            case "g":
                                g = reader.ReadString();
                                break;
                            case "b":
                                b = reader.ReadString();
                                break;
                        }
                    }
                }
            }
            return new Color(int.Parse(r) / 255f, int.Parse(g) / 255f, int.Parse(b) / 255f, 1f);
        }
        public static void ApplyPloppableColor(this Material mat)
        {
            var color = GetPloppableAsphaltCfg();
            mat.SetColor("_ColorV0", color);
            mat.SetColor("_ColorV1", color);
            mat.SetColor("_ColorV2", color);
            mat.SetColor("_ColorV3", color);
            mat.color = color;
        }
    }
}
