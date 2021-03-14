using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Text;
using UnityEngine;
using ProceduralObjects.Tools;
using System.IO;

using ColossalFramework;
using ColossalFramework.IO;

namespace ProceduralObjects.Classes
{
    public static class ProceduralUtils
    {
        public static int GetNextUnusedId(this List<ProceduralObject> list)
        {
            var logic = ProceduralObjectsLogic.instance;
            for (int i = 0; true; i++)
            {
                if (!logic.activeIds.Contains(i))
                {
                    logic.activeIds.Add(i);
                    return i;
                }
            }
        }
        public static ProceduralObject GetObjectWithId(this List<ProceduralObject> list, int id)
        {
            if (list.Any(po => po.id == id))
            {
                return list.FirstOrDefault(po => po.id == id);
            }
            return null;
        }
        public static Vector2 WorldToGuiPoint(this Vector3 position)
        {
            var guiPosition = Camera.main.WorldToScreenPoint(position);
            guiPosition.y = Screen.height - guiPosition.y;
            return new Vector2(guiPosition.x, guiPosition.y);
        }
        public static Vector2 WorldToGuiPoint(this Vector3 position, Camera cam)
        {
            var guiPosition = cam.WorldToScreenPoint(position);
            guiPosition.y = Screen.height - guiPosition.y;
            return new Vector2(guiPosition.x, guiPosition.y);
        }
        public static Vector3 VertexWorldPosition(Vertex vertex, ProceduralObject obj)
        {
            if (obj.isPloppableAsphalt)
                return obj.m_rotation * vertex.Position.PloppableAsphaltPosition() + obj.m_position;
            return obj.m_rotation * vertex.Position + obj.m_position;
        }
        public static Vector3 VertexWorldPosition(Vector3 localPos, ProceduralObject obj)
        {
            if (obj.isPloppableAsphalt)
                return obj.m_rotation * localPos.PloppableAsphaltPosition() + obj.m_position;
            return obj.m_rotation * localPos + obj.m_position;
        }

        public static void SnapToGround(this ProceduralObject obj)
        {
            obj.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.position, null);
            obj.m_position = NearestGroundPointVertical(obj.m_position);
            obj.historyEditionBuffer.ConfirmNewStep(null);
        }
        public static Vector3 NearestGroundPointVertical(Vector3 pos, bool andNetBuildings = false)
        {
            ToolBase.RaycastInput rayInput = new ToolBase.RaycastInput(new Ray(pos, Vector3.down), 10000);
            if (andNetBuildings)
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
            ToolBase.RaycastOutput rayOutput;
            if (ProceduralTool.TerrainRaycast(rayInput, out rayOutput))
                return rayOutput.m_hitPos;
            else
            {
                rayInput = new ToolBase.RaycastInput(new Ray(pos, Vector3.up), 10000);
                if (ProceduralTool.TerrainRaycast(rayInput, out rayOutput))
                    return rayOutput.m_hitPos;
            }
            return pos;
        }
        public static void RecenterObjOrigin(ProceduralObject obj, Vertex[] buffer)
        {
            obj.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, buffer);
            var bounds = new Bounds(buffer.First().Position, Vector3.zero);
            foreach (Vertex v in buffer)
                bounds.Encapsulate(v.Position);

            var bottomPoint = bounds.center;
            bottomPoint.y -= bounds.extents.y;
            var centerWorldSpace = VertexWorldPosition(bottomPoint, obj);

            foreach (Vertex v in buffer)
            {
                v.Position -= bottomPoint;
            }
            obj.historyEditionBuffer.ConfirmNewStep(buffer);
            obj.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.position, buffer);
            obj.m_position = centerWorldSpace;
            obj.historyEditionBuffer.ConfirmNewStep(buffer);
        }

        public static Color GetColor(this BuildingInfo info)
        {
            if (!info.m_useColorVariations)
                return Color.white;
            else
            {
                switch (Singleton<SimulationManager>.instance.m_randomizer.Int32(0, 4))
                {
                    case 1:
                        return info.m_color1;
                    case 2:
                        return info.m_color2;
                    case 3:
                        return info.m_color3;
                    default:
                        return info.m_color0;
                }
            }
        }

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
            if (logic.availableProceduralInfos.Count < 0)
                logic.availableProceduralInfos = CreateProceduralInfosList();
            logic.failedToLoadObjects = 0;

            foreach (var c in containerArray)
            {
                try
                {
                    var obj = new ProceduralObject(c, logic.layerManager);
                    if (obj.meshStatus != 1)
                    {
                        if (obj.RequiresUVRecalculation && !obj.disableRecalculation)
                            obj.m_mesh.uv = Vertex.RecalculateUVMap(obj, Vertex.CreateVertexList(obj));
                    }
                    obj.RecalculateBoundsNormalsExtras(obj.meshStatus);
                    logic.proceduralObjects.Add(obj);
                    logic.activeIds.Add(obj.id);
                }
                catch (Exception e)
                {
                    Debug.LogError("[ProceduralObjects] Failed to load a Procedural Object : \n" + e.GetType().ToString() + " : " + e.Message + "\n" + e.StackTrace);
                    logic.failedToLoadObjects += 1;
                }
            }
        }
        public static List<POGroup> BuildGroupsFromData(this ProceduralObjectsLogic logic)
        {
            var poGroups = new List<POGroup>();
            try
            {
                foreach (var po in logic.proceduralObjects)
                {
                    if (po._groupRootIdData == -2)
                        continue;
                    if (po._groupRootIdData == -1)
                    {
                        if (po.group == null)
                            poGroups.Add(POGroup.CreateGroupWithRoot(po));
                    }
                    else
                    {
                        var root = logic.proceduralObjects.GetObjectWithId(po._groupRootIdData);
                        if (root == null)
                        {
                            Debug.LogWarning("[ProceduralObjects] Object #" + po.id + " was supposed to be part of a group but the root could not be found ! Assigning no group.");
                            continue;
                        }
                        if (root.group == null)
                            poGroups.Add(POGroup.CreateGroupWithRoot(root));

                        root.group.AddToGroup(po);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[ProceduralObjects] Error while loading the groups. Skipping all together. " + e);
            }
            return poGroups;
        }

        private static List<ProceduralObject> prevSelection;
        public static void UpdateObjectsSelectedState(List<ProceduralObject> selection)
        {
            var newSelection = new List<ProceduralObject>();
            if (prevSelection != null)
            {
                foreach (var obj in prevSelection)
                {
                    obj._selected = false;
                }
            }
            foreach (var obj in selection)
            {
                newSelection.Add(obj);
                obj._selected = true;
            }
            prevSelection = newSelection;
        }

        public static void RecalculateNormals(this ProceduralObject obj)
        {
            if (obj.normalsRecalcMode == NormalsRecalculation.None)
                return;
            if (obj.normalsRecalcMode == NormalsRecalculation.Default)
                obj.m_mesh.RecalculateNormals();
            else if (obj.normalsRecalcMode == NormalsRecalculation.Tolerance60)
                obj.m_mesh.RecalculateNormals(60);
            else if (obj.normalsRecalcMode == NormalsRecalculation.Tolerance30)
                obj.m_mesh.RecalculateNormals(30);
            else if (obj.normalsRecalcMode == NormalsRecalculation.Tolerance0)
                obj.m_mesh.RecalculateNormals(0);
        }
        public static void MakeUniqueMesh(this ProceduralObject obj)
        {
            if (obj.baseInfoType == "BUILDING" || obj.meshStatus == 2)
                return;

            obj.meshStatus = 2;
            obj.m_mesh = obj.m_mesh.InstantiateMesh();
            obj.allVertices = obj.m_mesh.vertices;
            //  obj.m_mesh.SetVertices(new List<Vector3>(obj.allVertices));
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
        } 

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
                sub.m_rotation = Quaternion.Euler(sub.m_rotation.eulerAngles.x, a, sub.m_rotation.eulerAngles.z);
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
                var desc = info.GetLocalizedDescription().ToLower();
                list.Add(new ProceduralInfo(info, desc.Contains("proceduralobj") && desc.Contains("basic")));
            }
            return list.ToArray();
        }
        public static ProceduralInfo[] ToProceduralInfoArray(this IEnumerable<BuildingInfo> source)
        {
            var list = new List<ProceduralInfo>();
            foreach (BuildingInfo info in source)
            {
                var desc = info.GetLocalizedDescription().ToLower();
                list.Add(new ProceduralInfo(info, desc.Contains("proceduralobj") && desc.Contains("basic")));
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
        public static Color ApplyPloppableColor(this Material mat)
        {
            var color = GetPloppableAsphaltCfg();
            mat.SetColor("_ColorV0", color);
            mat.SetColor("_ColorV1", color);
            mat.SetColor("_ColorV2", color);
            mat.SetColor("_ColorV3", color);
            mat.color = color;
            return color;
        }
    }
}
