using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProceduralObjects.Classes
{
    public class MaterialOptions
    {
        public static void FixDecalRenderDist(ProceduralObject obj)
        {
            if (obj.baseInfoType != "PROP") return;
            if (obj._baseProp == null) return;
            if (!obj._baseProp.m_isDecal) return;
            if (obj.m_material == null) return;
            var fadeDistanceFactor = 1f / (obj.renderDistance * obj.renderDistance) * 2.6f;
            obj.m_material.SetFloat("_FadeDistanceFactor", fadeDistanceFactor);
        }
    }
}
