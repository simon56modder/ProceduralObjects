using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;
using ColossalFramework.PlatformServices;
using ColossalFramework.Plugins;
using ICities;

using ProceduralObjects.ProceduralText;

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
            if (pobj.textParam != null)
            {
                if (pobj.textParam.Count() > 0)
                {
                    foreach (TextField field in pobj.textParam.m_textFields)
                    {
                        tw.WriteLine(TextField.SaveString(field));
                    }
                }
            }
            tw.WriteLine("VERTICES " + pobj.allVertices.Count());
            for (int i = 0; i < pobj.allVertices.Count(); i++)
            {
                tw.WriteLine("vertex " + i.ToString() + " = " + pobj.allVertices[i].ToString());
            }
            tw.Close();
            if (!m_externals.Any(ext => ext.m_object == pobj))
                m_externals.Add(new ExternalInfo(name, path, pobj, false));
        }

        public void DeleteExternal(ExternalInfo info, List<Texture2D> textures, FontManager fManager)
        {
            if (m_externals == null)
            {
                LoadExternals(textures, fManager);
                return;
            }
            if (!m_externals.Contains(info))
            {
                LoadExternals(textures, fManager);
                return;
            }
            if (info.isWorkshop)
            {
                LoadExternals(textures, fManager);
                return;
            }
            if (!File.Exists(info.m_filePath))
            {
                LoadExternals(textures, fManager);
                return;
            }
            File.Delete(info.m_filePath);
            if (m_externals.Contains(info))
                m_externals.Remove(info);
        }
        public void RenameExternal(ExternalInfo info, string newName)
        {
            if (info.isWorkshop)
            {
                Debug.LogWarning("[ProceduralObjects] Failed to rename an ExternalInfo instance because it came from the workshop");
                return;
            }
            if (!File.Exists(info.m_filePath))
            {
                Debug.LogError("[ProceduralObjects] Failed to rename an ExternalInfo instance because it was moved from its original directory");
                return;
            }
            var lines = File.ReadAllLines(info.m_filePath);
            for (int i = 0; i < lines.Count(); i++)
            {
                if (lines[i].Contains("name = "))
                    lines[i] = "name = " + newName;
            }
            File.Delete(info.m_filePath);
            File.WriteAllLines(info.m_filePath, lines);
            info.m_name = newName;
        }
        public void LoadExternals(List<Texture2D> availableTextures, FontManager fManager)
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
                LoadOneExternal(path, availableTextures, false, fManager);
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
                        LoadOneExternal(file, availableTextures, true, fManager);
                    }
                }
            }

        }
        private void LoadOneExternal(string path, List<Texture2D> availableTextures, bool fromWorkshop, FontManager fManager)
        {
            try
            {
                var lines = File.ReadAllLines(path);

                if (lines.Any(line => line.Contains("externaltype = selection")))
                {
                    LoadSelectionExternal(lines, path, availableTextures, fromWorkshop, fManager);
                }
                else
                {
                    CacheProceduralObject obj = new CacheProceduralObject();
                    string name = "";
                    for (int i = 0; i < lines.Count(); i++)
                    {
                        if (lines[i].Contains("name = "))
                            name = lines[i].Replace("name = ", "").Trim();
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
                        else if (lines[i].Contains("textParam: "))
                        {
                            if (obj.textParam == null)
                                obj.textParam = new TextParameters();
                            obj.textParam.AddField(TextField.Parse(lines[i], fManager));
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
            }
            catch
            {
                Debug.LogError("[ProceduralObjects] Couldn't load an External Procedural Object : Invalid, corrupted or edited file at path : " + path);
            } 
        }

        private void LoadSelectionExternal(string[] fileLines, string path, List<Texture2D> availableTextures, bool fromWorkshop, FontManager fManager)
        {
            Dictionary<CacheProceduralObject, Vector3> objects = new Dictionary<CacheProceduralObject, Vector3>();

            CacheProceduralObject obj = null;
            Vector3 relativePos = Vector3.zero;

            string name = "";
            for (int i = 0; i < fileLines.Count(); i++)
            {
                if (fileLines[i].Contains("name = "))
                    name = fileLines[i].Replace("name = ", "").Trim();
                else if (fileLines[i].Contains("{"))
                {
                    obj = new CacheProceduralObject();
                    relativePos = Vector3.zero;
                }
                else if (fileLines[i].Contains("}"))
                {
                    objects[obj] = relativePos;
                    obj = null;
                    relativePos = Vector3.zero;
                }
                else if (fileLines[i].Contains("baseInfoType = "))
                    obj.baseInfoType = fileLines[i].Replace("baseInfoType = ", "");
                else if (fileLines[i].Contains("basePrefabName = "))
                    obj.basePrefabName = fileLines[i].Replace("basePrefabName = ", "");
                else if (fileLines[i].Contains("relativePosition = "))
                    relativePos = fileLines[i].Replace("relativePosition = ", "").ParseVector3();
                else if (fileLines[i].Contains("renderDistance = "))
                    obj.renderDistance = float.Parse(fileLines[i].Replace("renderDistance = ", ""));
                //   else if (lines[i].Contains("scale = "))
                //       obj.scale = float.Parse(lines[i].Replace("scale = ", ""));
                else if (fileLines[i].Contains("isPloppableAsphalt = "))
                    obj.isPloppableAsphalt = bool.Parse(fileLines[i].Replace("isPloppableAsphalt = ", ""));
                else if (fileLines[i].Contains("rotation = "))
                    obj.m_rotation = VertexUtils.ParseQuaternion(fileLines[i].Replace("rotation = ", ""));
                else if (fileLines[i].Contains("textParam: "))
                {
                    if (obj.textParam == null)
                        obj.textParam = new TextParameters();
                    obj.textParam.AddField(TextField.Parse(fileLines[i], fManager));
                }
                else if (fileLines[i].Contains("customTexture = "))
                {
                    if (fileLines[i].Replace("customTexture = ", "") == "null")
                        obj.customTexture = null;
                    else if (!availableTextures.Any(tex => tex.name == fileLines[i].Replace("customTexture = ", "")))
                        Debug.LogError("[ProceduralObjects] A saved object was found with a texture that doesn't exist anymore with the name " + fileLines[i].Replace("customTexture = ", "") + ", therefore loading the default object texture");
                    else
                        obj.customTexture = availableTextures.FirstOrDefault(tex => tex.name == fileLines[i].Replace("customTexture = ", ""));
                }
                else if (fileLines[i].Contains("VERTICES "))
                    obj.allVertices = new Vector3[int.Parse(fileLines[i].Replace("VERTICES ", ""))];
                else if (fileLines[i].Contains("vertex"))
                {
                    string[] split = fileLines[i].Replace("vertex ", "").Split(new string[] { " = " }, StringSplitOptions.RemoveEmptyEntries);
                    obj.allVertices[int.Parse(split[0])] = VertexUtils.ParseVector3(split[1]);
                }
            }
            ClipboardProceduralObjects selec = new ClipboardProceduralObjects(ClipboardProceduralObjects.ClipboardType.Selection);
            selec.selection_objects = objects;
            ExternalInfo info = new ExternalInfo(name, path, selec, fromWorkshop);
            m_externals.Add(info);
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
            this.m_selection = null;
            this.m_externalType = ClipboardProceduralObjects.ClipboardType.Single;
        }
        public ExternalInfo(string name, string path, ClipboardProceduralObjects selection, bool isWorkshop)
        {
            m_name = name;
            m_object = null;
            this.m_filePath = path;
            this.isWorkshop = isWorkshop;
            this.m_selection = selection;
            this.m_externalType = ClipboardProceduralObjects.ClipboardType.Selection;
        }

        public string m_name, m_filePath;
        public CacheProceduralObject m_object;
        public ClipboardProceduralObjects m_selection;
        public ClipboardProceduralObjects.ClipboardType m_externalType;
        public bool isWorkshop;
    }
}
