using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;

using ColossalFramework.PlatformServices;

namespace ProceduralObjects.Classes
{
    public class ExternalProceduralObjectsManager
    {
        public ExternalProceduralObjectsManager()
        {
            m_externals = new List<ExternalInfo>();
        }

        public List<ExternalInfo> m_externals;

        public void SaveToExternal(string name, CacheProceduralObject pobj)
        {
            string path = ProceduralObjectsMod.ExternalsConfigPath + name.ToFileName() + ".pobj";
            if (File.Exists(path))
                return;
            TextWriter tw = new StreamWriter(path);
            tw.WriteLine("name = " + name);
            tw.WriteLine("baseInfoType = " + pobj.baseInfoType);
            tw.WriteLine("basePrefabName = " + pobj.basePrefabName);
            tw.WriteLine("isPloppableAsphalt = " + pobj.isPloppableAsphalt.ToString());
          //  tw.WriteLine("scale = " + pobj.scale.ToString());
            tw.WriteLine("customTexture = " + ((pobj.customTexture == null) ? "null" : pobj.customTexture.name));
            tw.WriteLine("renderDistance = " + pobj.renderDistance.ToString());
            tw.WriteLine("rotation = " + pobj.m_rotation.ToString());
            tw.WriteLine("VERTICES " + pobj.allVertices.Count());
            for (int i = 0; i < pobj.allVertices.Count(); i++)
            {
                tw.WriteLine("vertex " + i.ToString() + " = " + pobj.allVertices[i].ToString());
            }
            tw.Close();
            if (!m_externals.Any(ext => ext.m_object == pobj))
                m_externals.Add(new ExternalInfo(name, path, pobj, false));
        }

        public void DeleteExternal(ExternalInfo info, List<Texture2D> textures)
        {
            if (m_externals == null)
            {
                LoadExternals(textures);
                return;
            }
            if (!m_externals.Contains(info))
            {
                LoadExternals(textures);
                return;
            }
            if (info.isWorkshop)
            {
                LoadExternals(textures);
                return;
            }
            if (!File.Exists(info.m_filePath))
            {
                LoadExternals(textures);
                return;
            }
            File.Delete(info.m_filePath);
            if (m_externals.Contains(info))
                m_externals.Remove(info);
        }

        public void LoadExternals(List<Texture2D> availableTextures)
        {
            m_externals = new List<ExternalInfo>();

            // local externals
            if (!Directory.Exists(ProceduralObjectsMod.ExternalsConfigPath))
            {
                Debug.Log("[ProceduralObjects] No Externals folder found : creating one and skipping loading.");
                Directory.CreateDirectory(ProceduralObjectsMod.ExternalsConfigPath);
                return;
            }
            if (!Directory.Exists(ProceduralObjectsMod.ExternalsConfigPath))
            {
                Debug.LogError("[ProceduralObjects] Fatal Directory error : couldn't create the Externals folder properly. Skipping Externals loading.");
                return;
            }
            foreach (string path in Directory.GetFiles(ProceduralObjectsMod.ExternalsConfigPath, "*.pobj", SearchOption.AllDirectories))
            {
                if (!File.Exists(path))
                    continue;
                LoadSingleExternal(path, availableTextures, false);
            }

            // workshop externals
            foreach (PublishedFileId fileId in PlatformService.workshop.GetSubscribedItems())
            {
                var dirPath = PlatformService.workshop.GetSubscribedItemPath(fileId);
                if (!Directory.Exists(dirPath))
                    continue;
                var pobjFiles = Directory.GetFiles(dirPath, "*.pobj", SearchOption.AllDirectories);
                if (pobjFiles.Any())
                {
                    foreach (string file in pobjFiles)
                    {
                        if (!File.Exists(file))
                            continue;
                        LoadSingleExternal(file, availableTextures, true);
                    }
                }
            }

        }
        private void LoadSingleExternal(string path, List<Texture2D> availableTextures, bool fromWorkshop)
        {
            try
            {
                var lines = File.ReadAllLines(path);
                CacheProceduralObject obj = new CacheProceduralObject();
                string name = "";
                for (int i = 0; i < lines.Count(); i++)
                {
                    if (lines[i].Contains("name = "))
                        name = lines[i].Replace("name = ", "");
                    else if (lines[i].Contains("baseInfoType = "))
                        obj.baseInfoType = lines[i].Replace("baseInfoType = ", "");
                    else if (lines[i].Contains("basePrefabName = "))
                        obj.basePrefabName = lines[i].Replace("basePrefabName = ", "");
                    else if (lines[i].Contains("renderDistance = "))
                        obj.renderDistance = float.Parse(lines[i].Replace("renderDistance = ", ""));
                 //   else if (lines[i].Contains("scale = "))
                 //       obj.scale = float.Parse(lines[i].Replace("scale = ", ""));
                    else if (lines[i].Contains("isPloppableAsphalt = "))
                        obj.isPloppableAsphalt = bool.Parse(lines[i].Replace("isPloppableAsphalt = ", ""));
                    else if (lines[i].Contains("rotation = "))
                        obj.m_rotation = VertexUtils.ParseQuaternion(lines[i].Replace("rotation = ", ""));
                    else if (lines[i].Contains("customTexture = "))
                    {
                        if (lines[i].Replace("customTexture = ", "") == "null")
                            obj.customTexture = null;
                        else if (!availableTextures.Any(tex => tex.name == lines[i].Replace("customTexture = ", "")))
                            Debug.LogError("[ProceduralObjects] A saved object was found with a texture that doesn't exist anymore with the name " + lines[i].Replace("customTexture = ", "") + ", therefore loading the default object texture");
                        else
                            obj.customTexture = availableTextures.FirstOrDefault(tex => tex.name == lines[i].Replace("customTexture = ", ""));
                    }
                    else if (lines[i].Contains("VERTICES "))
                        obj.allVertices = new Vector3[int.Parse(lines[i].Replace("VERTICES ", ""))];
                    else if (lines[i].Contains("vertex"))
                    {
                        string[] split = lines[i].Replace("vertex ", "").Split(new string[] { " = " }, StringSplitOptions.RemoveEmptyEntries);
                        obj.allVertices[int.Parse(split[0])] = VertexUtils.ParseVector3(split[1]);
                    }
                }
                ExternalInfo info = new ExternalInfo(name, path, obj, fromWorkshop);
                m_externals.Add(info);
            }
            catch
            {
                Debug.LogError("[ProceduralObjects] Couldn't load an External Procedural Object : Invalid, corrupted or edited file at path : " + path);
            } 
        }
    }
    public class ExternalInfo
    {
        public ExternalInfo() { }
        public ExternalInfo(string name, string path, CacheProceduralObject pObj, bool isWorkshop)
        {
            m_name = name;
            m_object = pObj;
            this.m_filePath = path;
            this.isWorkshop = isWorkshop;
        }

        public string m_name, m_filePath;
        public CacheProceduralObject m_object;
        public bool isWorkshop;
    }
}
