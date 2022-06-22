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
            obj.SetPosition(NearestGroundPointVertical(obj.m_position));
            obj.historyEditionBuffer.ConfirmNewStep(null);
        }
        public static Vector3 NearestGroundPointVertical(Vector3 pos, bool andNetBuildings = false)
        {
            Vector3 hit = pos;
            if (TryRaycastTerrain(pos, Vector3.down, out hit, andNetBuildings))
                return hit;
            return pos;
        }
        public static bool TryRaycastTerrain(Vector3 pos, Vector3 direction, out Vector3 hitpoint, bool andNetBuildings = false)
        {
            ToolBase.RaycastInput rayInput = new ToolBase.RaycastInput(new Ray(pos, direction), 10000);
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
            {
                hitpoint = rayOutput.m_hitPos;
                return true;
            }
            else
            {
                rayInput = new ToolBase.RaycastInput(new Ray(pos, -direction), 10000);
                if (ProceduralTool.TerrainRaycast(rayInput, out rayOutput))
                {
                    hitpoint = rayOutput.m_hitPos;
                    return true;
                }
            }
            hitpoint = pos;
            return false;
        }
        public static void InvertSelection(ProceduralObject obj, List<int> editingVertexIndex, Vertex[] buffer)
        {
            var logic = ProceduralObjectsLogic.instance;
            foreach (Vertex v in buffer)
            {
                if (v.IsDependent) continue;
                if (editingVertexIndex.Contains(v.Index))
                    editingVertexIndex.Remove(v.Index);
                else
                    editingVertexIndex.Add(v.Index);
            }
            ProceduralUtils.UpdateVertexSelectedState(editingVertexIndex, obj);
        }
        public static void RecenterObjOrigin(ProceduralObject obj, Vertex[] buffer)
        {
            var bounds = new Bounds(buffer.First().Position, Vector3.zero);
            foreach (Vertex v in buffer)
                bounds.Encapsulate(v.Position);

            var bottomPoint = bounds.center;
            bottomPoint.y -= bounds.extents.y;

            SetObjOrigin(obj, buffer, bottomPoint, true);
        }
        public static void SetObjOrigin(ProceduralObject obj, Vertex[] buffer, Vector3 center, bool registerHistory)
        {
            if (registerHistory)
                obj.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.vertices, buffer);
            var centerWorldSpace = VertexWorldPosition(center, obj);
            foreach (Vertex v in buffer)
            {
                v.Position -= center;
            }
            if (registerHistory)
            {
                obj.historyEditionBuffer.ConfirmNewStep(buffer);
                obj.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.position, buffer);
            }
            obj.SetPosition(centerWorldSpace);
            if (registerHistory)
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
            try
            {
                if (PopupStart.loading_failures.Count > 0)
                {
                    foreach (var f in PopupStart.loading_failures)
                    {
                        if (!f.keep) continue;
                        for (int i = 0; i < f.containers.Count; i++)
                        {
                            list.Add(f.containers.Keys.ToList()[i]);
                        }
                    }
                }
            } 
            catch (Exception e)
            {
                Debug.LogError("[ProceduralObjects] Error implementing the POs that failed to load back into saving\n" + e.ToString());
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

            PropInfo[] props = Resources.FindObjectsOfTypeAll<PropInfo>();
            BuildingInfo[] buildings = Resources.FindObjectsOfTypeAll<BuildingInfo>();

            foreach (var c in containerArray)
            {
                try
                {
                    var obj = new ProceduralObject(c, logic.layerManager, props, buildings);
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
                    PopupStart.RegisterFailure(c, e, props, buildings);
                    logic.failedToLoadObjects += 1;
                }
            }
            PopupStart.LoadingDoneShowPopup();
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
        public static void UpdateVertexSelectedState(List<int> editingVertices, ProceduralObject obj)
        {
            if (editingVertices == null)
            {
                ClearVertexSelection(obj);
                return;
            }
            foreach (Vertex v in obj.vertices)
            {
                if (v.IsDependent) continue;
                if (editingVertices.Contains(v.Index))
                    v._selected = true;
                else
                    v._selected = false;
            }
        }
        public static void ClearVertexSelection(ProceduralObject obj)
        {
            foreach (Vertex v in obj.vertices)
            {
                v._selected = false;
            }
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
        public static void MakeUniqueMesh(this ProceduralObject obj, bool skipVertexListBuild = false)
        {
            if (obj.baseInfoType == "BUILDING" || obj.meshStatus == 2)
                return;

            obj.meshStatus = 2;
            obj.m_mesh = obj.m_mesh.InstantiateMesh();
            if (!skipVertexListBuild)
                obj.vertices = Vertex.CreateVertexList(obj);
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
        public static void ResetOriginalMesh(this ProceduralObject obj)
        {
            var originalVertices = (obj.baseInfoType == "PROP") ? obj._baseProp.m_mesh.vertices : obj._baseBuilding.m_mesh.vertices;
            for (int i = 0; i < obj.vertices.Length; i++)
            {
                obj.vertices[i].Position = originalVertices[i];
            }
            obj.ApplyModelChange();
        }
        public static POGroup ConstructSubBuildings(ProceduralObject obj)
        {
            var logic = ProceduralObjectsLogic.instance;
            if (obj.baseInfoType != "BUILDING")
            {
                return null;
            }
            var pos = new List<ProceduralObject>();
            pos.Add(obj);
            // Sub buildings
            var subBuildings = obj._baseBuilding.m_subBuildings;
            if (subBuildings.Length >= 1)
            {
                for (int i = 0; i < subBuildings.Length; i++)
                {
                    var subB = subBuildings[i];
                    if (subB == null)
                        continue;
                    if (subB.m_buildingInfo == null)
                        continue;
                    if (subB.m_buildingInfo.m_mesh == null)
                        continue;
                    if (!subB.m_buildingInfo.m_mesh.isReadable)
                        continue;
                    int id = 0;
                    try
                    {
                        ProceduralObject sub = new ProceduralObject();
                        id = logic.proceduralObjects.GetNextUnusedId();
                        sub.ConstructObject(subB.m_buildingInfo, id);
                        float a = -(subB.m_angle * Mathf.Rad2Deg) % 360f;
                        if (a < 0) a += 360f;
                        sub.m_rotation = Quaternion.Euler(sub.m_rotation.eulerAngles.x, a, sub.m_rotation.eulerAngles.z) * obj.m_rotation;
                        sub.m_position = VertexUtils.RotatePointAroundPivot(subB.m_position + obj.m_position, obj.m_position, obj.m_rotation);
                        pos.Add(sub);
                        logic.proceduralObjects.Add(sub);
                    }
                    catch
                    {
                        if (id != 0)
                        {
                            if (logic.activeIds.Contains(id))
                                logic.activeIds.Remove(id);
                        }
                    }
                }
            }
            if (pos.Count < 2) return null;
            var group = POGroup.MakeGroup(logic, pos, pos[0]);
            return group;
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

        public static Texture GetOriginalTexture(ProceduralObject obj)
        {
            if (obj.customTexture)
                return obj.customTexture as Texture;
            else
                return GetBasePrefabMainTex(obj);
        }
        public static Texture GetBasePrefabMainTex(ProceduralObject obj)
        {
            if (obj.baseInfoType == "PROP")
                return obj._baseProp.m_material.mainTexture;
            else
                return obj._baseBuilding.m_material.mainTexture;
        }

        public static bool IsPloppableAsphalt(this PropInfo sourceProp)
        {
            string name = "";
            try { name = sourceProp.m_mesh.name; }
            catch { return false; }
            return ((name == "ploppableasphalt-prop") ||
                (name == "ploppablecliffgrass") ||
                (name == "ploppablegravel"));
        }
        public static bool IsPloppableAsphalt(this ProceduralObject obj)
        {
            string name = "";
            try { name = obj._baseProp.m_mesh.name; }
            catch { return false; }
            return ((name == "ploppableasphalt-prop") ||
                (name == "ploppablecliffgrass") ||
                (name == "ploppablegravel"));
        }
        public static bool IsPloppableSrfCircle(this ProceduralObject obj)
        {
            if (!obj.basePrefabName.Contains(".")) return false;
            string postPoint = obj.basePrefabName.Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries)[1];
            return ((postPoint == "R69 Ploppable Pavement Circle_Data") ||
                (postPoint == "R69 Ploppable Asphalt Circle_Data") ||
                (postPoint == "R69 Ploppable Grass Circle8_Data") ||
                (postPoint == "R69 Ploppable Gravel Circle8_Data") ||
                (postPoint == "R69 Ploppable Pavement Circle8_Data") ||
                (postPoint == "R69 Ploppable Asphalt Circle8_Data") ||
                (postPoint == "R69 Ploppable Cliff Circle8_Data") ||
                (postPoint == "R69 Ploppable Grass Circle16_Data") ||
                (postPoint == "R69 Ploppable Gravel Circle16_Data") ||
                (postPoint == "R69 Ploppable Cliff Circle16_Data") ||
                (postPoint == "R69 Ploppable Asphalt Circle16_Data") ||
                (postPoint == "R69 Ploppable Pavement Circle16_Data"));
        }
        private static Color ploppableAsphaltColor;
        private static bool isPAsphColorSetup = false;
        public static void UpdatePloppableAsphaltCfg()
        {
            string path = Path.Combine(DataLocation.localApplicationData, "PloppableAsphalt.xml");
            if (!File.Exists(path))
            {
                Debug.LogWarning("[ProceduralObjects] No PloppableAsphalt.xml config was found in order to retrieve the Ploppable Asphalt color setup in the mod (harmless issue)");
                ploppableAsphaltColor = new Color(.5f, .5f, .5f, 1f);
                return;
            }

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
            ploppableAsphaltColor = new Color(int.Parse(r) / 255f, int.Parse(g) / 255f, int.Parse(b) / 255f, 1f);
        }
        public static Color GetPloppableAsphaltCfg()
        {
            if (!isPAsphColorSetup) UpdatePloppableAsphaltCfg();
            return ploppableAsphaltColor;
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

        public static string GetDisplayableAssetname(string basePrefabName)
        {
            if (basePrefabName.Length < 5)
                return basePrefabName;
            return basePrefabName.Substring(basePrefabName.IndexOf(".") + 1).Replace("_Data", "");
        }
        public static void ExportRequiredAssetsHTML(string path, List<CacheProceduralObject> list)
        {
            if (File.Exists(path)) File.Delete(path);

            var reqAssets = new Dictionary<uint, string>();
            foreach (var obj in list)
            {
                if (!obj.basePrefabName.Contains("_Data")) continue;
                if (!obj.basePrefabName.Contains(".")) continue;
                uint id;
                var split = obj.basePrefabName.Split(new string[]{"."}, StringSplitOptions.RemoveEmptyEntries);
                if (uint.TryParse(split[0], out id))
                {
                    if (reqAssets.ContainsKey(id)) continue;
                    PrefabInfo info = (obj.baseInfoType == "PROP") ? (PrefabInfo)(obj._baseProp) : (PrefabInfo)(obj._baseBuilding);
                    var name = "";
                    if (info == null)
                        name = split[1].Replace("_Data", "");
                    else
                        name = info.GetLocalizedTitle();
                    reqAssets.Add(id, name);
                }
            }
            if (reqAssets.Count == 0) return;

            TextWriter tw = new StreamWriter(path);
            tw.WriteLine("<html><head><style>table, td { border: 1px solid black; }</style></head><body><table style=\"width:25%\"><thead><tr><th colspan=\"2\">Required assets for PO export</th></tr></thead><tbody>");
            foreach (var req in reqAssets)
                tw.WriteLine("<tr><td>" + req.Value + "</td><td style=\"text-align:center;\"><input type=button onClick=\"parent.open('https://steamcommunity.com/sharedfiles/filedetails/?id="
                    + req.Key.ToString() + "')\" value='Show on the workshop'></td></tr>");
            tw.Close();
        }
    }
}
