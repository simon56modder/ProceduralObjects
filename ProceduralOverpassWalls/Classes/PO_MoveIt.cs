using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
// using MoveItIntegration;
using UnityEngine;

namespace ProceduralObjects.Classes
{
    public static class PO_MoveIt
    {
        #region FORMER MI integration
        public static List<ProceduralObject> queuedCloning = new List<ProceduralObject>();
        public static Dictionary<ProceduralObject, ProceduralObject> doneCloning = new Dictionary<ProceduralObject, ProceduralObject>();

        public static HashSet<POConversionRequest> queuedConversion = new HashSet<POConversionRequest>();

        public static void RequestConversion(POConversionRequest request)
        {
            queuedConversion.Add(request);
        }
        public static byte TryRetrieveConverted(POConversionRequest request, out ProceduralObject po)
        {
            if (request.failed)
            {
                po = null;
                queuedConversion.Remove(request);
                return 2;
            }
            if (request.converted == null)
            {
                po = null;
                return 0;
            }
            else
            {
                po = request.converted;
                queuedConversion.Remove(request);
                return 1;
            }
        }

        public static void CallPOCloning(ProceduralObject src)
        {
            if (queuedCloning.Contains(src))
                return;
            queuedCloning.Add(src);
        }

        public static bool TryRetrieveClone(ProceduralObject src, out ProceduralObject clone, out uint cloneId)
        {
            if (queuedCloning.Contains(src) || !doneCloning.ContainsKey(src))
            {
                clone = null;
                cloneId = 0;
                return false;
            }
            clone = doneCloning[src];
            cloneId = (uint)clone.id;
            doneCloning.Remove(src);
            return true;
        }
        #endregion

        // new MI integration
        public static Assembly MIAssembly;
        static Type t_MIPOLogic, t_MIPOobj, t_MIPOManager;
        static bool isSetup = false;
        static object MIPOLogic;
        static object MIPOManager;
        static BindingFlags flags;

        public static void SetupMoveIt()
        {
            if (isSetup) return;

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.FullName.Substring(0, 6) == "MoveIt")
                {
                    if (assembly.GetType("MoveIt.PO_Manager") != null)
                    {
                        MIAssembly = assembly;
                        break;
                    }
                }
            }

            if (MIAssembly == null)
            {
                Debug.LogError("[ProceduralObjects] MoveIt assembly not found !");
                return;
            }

            t_MIPOLogic = MIAssembly.GetType("MoveIt.PO_Logic");
            t_MIPOobj = MIAssembly.GetType("MoveIt.PO_Object");
            t_MIPOManager = MIAssembly.GetType("MoveIt.PO_Manager");
            MIPOLogic = GameObject.Find("MIT_POLogic").GetComponent(t_MIPOLogic);
            MIPOManager = MIAssembly.GetType("MoveIt.MoveItTool").GetField("PO", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(null);

            flags = BindingFlags.Instance | BindingFlags.NonPublic;
            isSetup = true;
        }
        public static ProceduralObject GetPO(InstanceID id)
        {
            var MIPO_obj = t_MIPOLogic.GetMethod("GetPOById", flags).Invoke(MIPOLogic, new object[] { id.NetLane });
            var obj = t_MIPOobj.GetField("procObj", flags).GetValue(MIPO_obj);
            return (ProceduralObject)obj;
        }
        public static void ClonePO(InstanceID original, InstanceID target)
        {
            var clone = ProceduralObjectsLogic.instance.CloneObject(GetPO(original));
            AddPOToMIVisibleObj(clone);
            t_MIPOLogic.GetMethod("Paste").Invoke(MIPOLogic, new object[] { original, target, clone.id });
        }
        public static void InitializeAsPO(InstanceID target, ProceduralObject po)
        {
            if (po.meshStatus != 1)
            {
                if (po.RequiresUVRecalculation && !po.disableRecalculation)
                    po.m_mesh.uv = Vertex.RecalculateUVMap(po, Vertex.CreateVertexList(po));
            }
            po.RecalculateBoundsNormalsExtras(po.meshStatus);
            AddPOToMIVisibleObj(po);
            t_MIPOLogic.GetMethod("Paste").Invoke(MIPOLogic, new object[] { null, target, po.id });
        }
        public static void AddPOToMIVisibleObj(ProceduralObject obj)
        {
            var mi_obj = t_MIPOobj.GetConstructor(new Type[] { typeof(object) }).Invoke(new object[] { obj });
            uint mi_id = (uint)(obj.id + 1);
            var visibleObjects = t_MIPOManager.GetField("visibleObjects", flags).GetValue(MIPOManager);
            visibleObjects.GetType().GetMethod("Add").Invoke(visibleObjects, new object[] { mi_id, mi_obj });
            var visibleIds = t_MIPOManager.GetField("visibleIds", flags).GetValue(MIPOManager);
            visibleIds.GetType().GetMethod("Add").Invoke(visibleIds, new object[] { mi_id });
        }
    }
    public class POConversionRequest
    {
        public POConversionRequest()
        {
            color = Color.white;
        }

        public PropInfo propInfo;
        public BuildingInfo buildingInfo;
        public Vector3 position;
        public Quaternion rotation;
        public Color color;

        public bool failed;
        public ProceduralObject converted;
    }

    /* 
    public class MoveItIntegrationFactory : IMoveItIntegrationFactory
    {
        public MoveItIntegrationBase GetInstance()
        {
            return new MoveItIntegration();
        }
    }
    public class MoveItIntegration : MoveItIntegrationBase
    {
        public override string ID
        {
            get { return "simonryr.proceduralobjects"; }
        }
        public override Version DataVersion
        {
            get { return new Version(1, 0); }
        }
        public override string Name
        {
            get { return "Procedural Objects"; }
        }
        public override string Description
        {
            get { return string.Empty; }
        }
        public override object Copy(InstanceID sourceInstanceID)
        {
            if (sourceInstanceID.Type == InstanceType.NetLane)
            {
                PO_MoveIt.SetupMoveIt();
                return sourceInstanceID;
            }
            return null;
        }
        public override void Paste(InstanceID targetInstanceID, object record, Dictionary<InstanceID, InstanceID> map)
        {
            if (record == null)
                return;

            if (record is InstanceID)
            {
                if (((InstanceID)record).Type != InstanceType.NetLane)
                    return;
                PO_MoveIt.SetupMoveIt();
                PO_MoveIt.ClonePO((InstanceID)record, targetInstanceID);
            }
            else if (record is ProceduralObject)
            {
                PO_MoveIt.SetupMoveIt();
                var obj = (ProceduralObject)record;
                obj.id = ProceduralObjectsLogic.instance.proceduralObjects.GetNextUnusedId();
                ProceduralObjectsLogic.instance.proceduralObjects.Add(obj);
                PO_MoveIt.InitializeAsPO(targetInstanceID, obj);
            }
        }
        public override string Encode64(object record)
        {
            if (!(record is InstanceID))
                return null;
            if (((InstanceID)record).Type != InstanceType.NetLane)
                return null;
            PO_MoveIt.SetupMoveIt();
            var container = new ProceduralObjectContainer(PO_MoveIt.GetPO((InstanceID)record));
            return EncodeUtil.BinaryEncode64(container);
        }
        public override object Decode64(string base64Data, Version dataVersion)
        {
            if (base64Data == null || base64Data.Length == 0) return null;

            PO_MoveIt.SetupMoveIt();
            object decoded = EncodeUtil.BinaryDecode64(base64Data);
            if (!(decoded is ProceduralObjectContainer))
                return null;
            try
            {
                var obj = new ProceduralObject((ProceduralObjectContainer)decoded, ProceduralObjectsLogic.instance.layerManager);
                return obj;
            }
            catch (Exception e)
            {
                Debug.LogError("[ProceduralObjects] [MoveItIntegration] Unable to decode a Procedural Object from a stored Move It import !\n" + e);
                return null;
            }
        }
    }
     * */
}
