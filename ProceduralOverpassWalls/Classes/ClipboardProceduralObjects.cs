using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;

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

        public void ExportSelection(string name, ExternalProceduralObjectsManager manager)
        {
            if (selection_objects == null)
                return;
            if (selection_objects.Count <= 1)
                return;
            string path = ProceduralObjectsMod.ExternalsConfigPath + name.ToFileName() + ".pobj";
            if (File.Exists(path))
                return;

            TextWriter tw = new StreamWriter(path);
            tw.WriteLine("externaltype = selection");
            tw.WriteLine("name = " + name);
            foreach (KeyValuePair<CacheProceduralObject, Vector3> kvp in selection_objects)
            {
                tw.WriteLine("OBJECT");
                tw.WriteLine("{");
                tw.WriteLine("baseInfoType = " + kvp.Key.baseInfoType);
                tw.WriteLine("basePrefabName = " + kvp.Key.basePrefabName);
                tw.WriteLine("relativePosition = " + kvp.Value.ToString());
                tw.WriteLine("isPloppableAsphalt = " + kvp.Key.isPloppableAsphalt.ToString());
                //  tw.WriteLine("scale = " + pobj.scale.ToString());
                tw.WriteLine("customTexture = " + ((kvp.Key.customTexture == null) ? "null" : kvp.Key.customTexture.name));
                tw.WriteLine("renderDistance = " + kvp.Key.renderDistance.ToString());
                tw.WriteLine("rotation = " + kvp.Key.m_rotation.ToString());
                tw.WriteLine("VERTICES " + kvp.Key.allVertices.Count());
                for (int i = 0; i < kvp.Key.allVertices.Count(); i++)
                {
                    tw.WriteLine("vertex " + i.ToString() + " = " + kvp.Key.allVertices[i].ToString());
                }
                tw.WriteLine("}");
            }
            tw.Close();
        }


        public enum ClipboardType
        {
            Single,
            Selection
        }
    }
}
