using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProceduralObjects.Classes
{
    public class ClipboardProceduralObjects
    {
        public ClipboardProceduralObjects(ClipboardType type)
        {
            this.type = type;
        }

        public void MakeSelectionList(List<ProceduralObject> list)
        {
            this.selection_objects = new Dictionary<CacheProceduralObject, Vector3>();
            for (int i = 0; i < list.Count; i++)
            {
                Vector3 relativePos;
                if (i == 0)
                    relativePos = Vector3.zero;
                else
                    relativePos = list[i].m_position - list[0].m_position;
                this.selection_objects.Add(new CacheProceduralObject(list[i]), relativePos);
            }
        }

        public CacheProceduralObject single_object;
        public Dictionary<CacheProceduralObject, Vector3> selection_objects;
        public ClipboardType type;


        public enum ClipboardType
        {
            Single,
            Selection
        }
    }
}
