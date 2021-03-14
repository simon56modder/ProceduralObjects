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
        /*
        public static Assembly MIAssembly;
        static Type t_MIPOLogic, t_MIPOobj;
        static bool isSetup = false;
        static object MIPOLogic;
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
            MIPOLogic = GameObject.Find("MIT_POLogic").GetComponent(t_MIPOLogic);

            flags = BindingFlags.Instance | BindingFlags.NonPublic;
            isSetup = true;
        }
        public static ProceduralObject GetPO(InstanceID id)
        {
            var MIPO_obj = t_MIPOLogic.GetMethod("GetPOById", flags).Invoke(MIPOLogic, new object[] { id.NetLane });
            var obj = t_MIPOobj.GetField("procObj", flags).GetValue(MIPO_obj);
            return (ProceduralObject)obj;
        }
        public static void ClonePO(object record, InstanceID target)
        {
            var clone = ProceduralObjectsLogic.instance.CloneObject((ProceduralObject)record);
            var MIPOclone = t_MIPOobj.GetConstructor(new Type[] { typeof(ProceduralObject) }).Invoke(null, new object[] { clone });
            target.NetLane = (uint)clone.id + 1;
        }
         * */
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
                return PO_MoveIt.GetPO(sourceInstanceID);
            }
            return null;
        }
        public override void Paste(InstanceID targetInstanceID, object record, Dictionary<InstanceID, InstanceID> map)
        {
            if (record is ProceduralObject)
            {
                PO_MoveIt.SetupMoveIt();
                PO_MoveIt.ClonePO(record, targetInstanceID);
            }
        }
        public override string Encode64(object record)
        {
            if (record is ProceduralObject)
            {
                PO_MoveIt.SetupMoveIt();
                var container = new ProceduralObjectContainer((ProceduralObject)record);
                return EncodeUtil.BinaryEncode64(container);
            }
            return null;
        }
        public override object Decode64(string base64Data, Version dataVersion)
        {
            if (base64Data == null || base64Data.Length == 0) return null;

            PO_MoveIt.SetupMoveIt();
            return new ProceduralObject((ProceduralObjectContainer)EncodeUtil.BinaryDecode64(base64Data), ProceduralObjectsLogic.instance.layerManager);
        }
    }
     * */
}
