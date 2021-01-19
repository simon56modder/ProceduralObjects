using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ProceduralObjects.Classes
{
    public static class PO_MoveIt
    {
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
}
