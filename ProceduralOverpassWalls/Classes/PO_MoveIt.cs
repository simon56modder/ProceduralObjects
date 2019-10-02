using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProceduralObjects.Classes
{
    public static class PO_MoveIt
    {
        public static List<ProceduralObject> queuedCloning = new List<ProceduralObject>();
        public static Dictionary<ProceduralObject, ProceduralObject> doneCloning = new Dictionary<ProceduralObject, ProceduralObject>();

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
}
