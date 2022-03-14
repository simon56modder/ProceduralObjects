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
using ProceduralObjects.UI;

namespace ProceduralObjects.Classes
{
    public class ExternalProceduralObjectsManager
    {
        public ExternalProceduralObjectsManager()
        {
            m_externals = new List<ExternalInfo>();
            m_defaultPOsUponConversion = new Dictionary<string, ExternalInfo>();
        }

        public List<ExternalInfo> m_externals;
        public Dictionary<string, ExternalInfo> m_defaultPOsUponConversion;

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
            tw.WriteLine("renderDistLocked = " + pobj.renderDistLocked.ToString());
            tw.WriteLine("rotation = " + pobj.m_rotation.ToStringUnrounded());
            tw.WriteLine("disableRecalculation = " + pobj.disableRecalculation.ToString());
            if (pobj.tilingFactor != 8)
                tw.WriteLine("tilingFactor = " + pobj.tilingFactor.ToString());
            tw.WriteLine("color = " + ((SerializableColor)pobj.m_color).ToString());
            tw.WriteLine("flipFaces = " + pobj.flipFaces.ToString());
            tw.WriteLine("normalsRecalc = " + pobj.normalsRecalculation.ToString());
            tw.WriteLine("visibility = " + pobj.visibility.ToString());
            if (pobj.textParam != null)
            {
                if (pobj.textParam.Count() > 0)
                {
                    foreach (TextField field in pobj.textParam.m_textFields)
                        tw.WriteLine(TextField.SaveString(field));
                }
            }
            if (pobj.modules != null)
            {
                if (pobj.modules.Count > 0)
                    ModuleManager.WriteModules(tw, pobj.modules, false);
            }
            if (pobj.meshStatus == 1)
                tw.WriteLine("ORIGINALMODEL");
            else
            {
                tw.WriteLine("VERTICES " + pobj.allVertices.Count());
                for (int i = 0; i < pobj.allVertices.Count(); i++)
                {
                    tw.WriteLine("vertex " + i.ToString() + " = " + pobj.allVertices[i].ToStringUnrounded());
                }
                tw.Close();
            }
            if (!m_externals.Any(ext => ext.m_object == pobj))
                m_externals.Add(new ExternalInfo(name, path, pobj, false));
        }

        public void DeleteExternal(ExternalInfo info)
        {
            if (m_externals == null)
                return;
            if (!m_externals.Contains(info))
                return;
            if (info.isWorkshop)
                return;
            if (!File.Exists(info.m_filePath))
                return;

            File.Delete(info.m_filePath);
            if (File.Exists(info.m_filePath.Replace(".pobj", " - required assets.html")))
                File.Delete(info.m_filePath.Replace(".pobj", " - required assets.html"));
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
        public void LoadExternals(FontManager fManager)
        {
            m_externals = new List<ExternalInfo>();
            m_defaultPOsUponConversion = new Dictionary<string, ExternalInfo>();

            // local externals
            if (!Directory.Exists(ProceduralObjectsMod.ExternalsConfigPath))
            {
                if (Directory.Exists(ProceduralObjectsMod.OldExternalsConfigPath))
                {
                    try
                    {
                        Directory.Move(ProceduralObjectsMod.OldExternalsConfigPath, ProceduralObjectsMod.ExternalsConfigPath);
                        Debug.Log("[ProceduralObjects] Found old externals directory, moving to the new.");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("[ProceduralObjects] Failed to move the old externals directory to the new one : " + e);
                        Directory.CreateDirectory(ProceduralObjectsMod.ExternalsConfigPath);
                        return;
                    }
                }
                else
                {
                    Debug.Log("[ProceduralObjects] No Externals folder found (neither old nor new) : creating one and skipping loading.");
                    Directory.CreateDirectory(ProceduralObjectsMod.ExternalsConfigPath);
                    return;
                }
            }
            foreach (string path in Directory.GetFiles(ProceduralObjectsMod.ExternalsConfigPath, "*.pobj", SearchOption.AllDirectories))
            {
                if (!File.Exists(path))
                    continue;
                LoadOneExternal(path, false, fManager);
            }

            // workshop externals
            foreach (string path in ProceduralObjectsMod.WorkshopOrLocalFolders)
            {
                var pobjFiles = Directory.GetFiles(path, "*.pobj", SearchOption.AllDirectories);
                if (pobjFiles.Any())
                {
                    foreach (string file in pobjFiles)
                    {
                        if (!File.Exists(file))
                            continue;
                        LoadOneExternal(file, true, fManager);
                    }
                }
            }

        }
        private void LoadOneExternal(string path, bool fromWorkshop, FontManager fManager)
        {
            try
            {
                var first10lines = GUIUtils.GetFirstLinesOfFile(path, 12);
                bool isStatic = first10lines.Any(line => line.Contains("externaltype = static"));
                bool isPlopSelection = first10lines.Any(line => line.Contains("externaltype = selection"));
                bool isSubstitute = first10lines.Any(line => line.ToLower().Contains("substituteforconversionof"));
                string name = first10lines.First(line => line.StartsWith("name = ")).Replace("name = ", "").Trim();

                if (isStatic && isSubstitute)
                {
                    Debug.LogError("[ProceduralObjects] A PO  export named \"" + name + "\" was not loaded. Exported POs cannot be both static and substitutes for conversion !");
                    return;
                }

                ExternalInfo info = new ExternalInfo(name, path,
                    (isStatic || isPlopSelection) ? ClipboardProceduralObjects.ClipboardType.Selection : ClipboardProceduralObjects.ClipboardType.Single, isStatic, fromWorkshop);

                if (isSubstitute)
                    m_defaultPOsUponConversion.Add(first10lines.First(line => line.ToLower().Contains("substituteforconversionof")).GetStringAfter(" = "),  info);
                else
                    m_externals.Add(info);
            }
            catch
            {
                Debug.LogError("[ProceduralObjects] Couldn't load an External Procedural Object : Invalid or corrupted file at path : " + path);
            }
        }
    }
    public class ExternalInfo
    {
        public ExternalInfo() { }
        public ExternalInfo(string name, string path, ClipboardProceduralObjects.ClipboardType type, bool isStatic, bool isWorshop)
        {
            m_name = name;
            m_object = null;
            this.m_filePath = path;
            this.isWorkshop = isWorshop;
            this.m_selection = null;
            this.m_externalType = type;
            this.isStatic = isStatic;
            hasClipboard = false;
        }
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

        public void CreateClipboard()
        {
            if (hasClipboard) return;

            if (m_externalType == ClipboardProceduralObjects.ClipboardType.Single)
                createSingleClipboard();
            else
                createSelectionClipboard();
            hasClipboard = true;
        }
        private void createSingleClipboard()
        {
            var lines = File.ReadAllLines(m_filePath);
            CacheProceduralObject obj = new CacheProceduralObject();
            try
            {
                obj.tilingFactor = 8;
                obj.m_color = Color.white;
                string name = "";
                var modulesData = new List<Dictionary<string, string>>();
                Dictionary<string, string> currentModuleData = null;
                bool original = lines.Any(s => s.Contains("ORIGINALMODEL"));
                if (original)
                    obj.meshStatus = 1;
                else
                    obj.meshStatus = 2;
                for (int i = 0; i < lines.Count(); i++)
                {
                    if (currentModuleData != null)
                    {
                        if (lines[i].Contains("{"))
                            continue;
                        else if (lines[i].Contains("}"))
                        {
                            modulesData.Add(currentModuleData);
                            currentModuleData = null;
                            continue;
                        }
                        else if (lines[i].Contains(" = "))
                        {
                            currentModuleData.Add(lines[i].GetUntilOrEmpty(" = ").Trim(), lines[i].GetStringAfter(" = ").Trim());
                        }
                    }
                    if (lines[i].Contains("name = "))
                        name = lines[i].Replace("name = ", "").Trim();
                    else if (lines[i].Contains("baseInfoType = "))
                        obj.baseInfoType = lines[i].Replace("baseInfoType = ", "");
                    else if (lines[i].Contains("basePrefabName = "))
                        obj.basePrefabName = lines[i].Replace("basePrefabName = ", "");
                    else if (lines[i].Contains("renderDistance = "))
                        obj.renderDistance = float.Parse(lines[i].Replace("renderDistance = ", ""));
                    else if (lines[i].Contains("renderDistLocked = "))
                        obj.renderDistLocked = bool.Parse(lines[i].Replace("renderDistLocked = ", ""));
                    //   else if (lines[i].Contains("scale = "))
                    //       obj.scale = float.Parse(lines[i].Replace("scale = ", ""));
                    else if (lines[i].Contains("isPloppableAsphalt = "))
                        obj.isPloppableAsphalt = bool.Parse(lines[i].Replace("isPloppableAsphalt = ", ""));
                    else if (lines[i].Contains("tilingFactor = "))
                        obj.tilingFactor = int.Parse(lines[i].Replace("tilingFactor = ", ""));
                    else if (lines[i].Contains("rotation = "))
                        obj.m_rotation = VertexUtils.ParseQuaternion(lines[i].Replace("rotation = ", ""));
                    else if (lines[i].Contains("disableRecalculation = "))
                        obj.disableRecalculation = bool.Parse(lines[i].Replace("disableRecalculation = ", ""));
                    else if (lines[i].Contains("color = "))
                        obj.m_color = SerializableColor.Parse(lines[i].Replace("color = ", ""));
                    else if (lines[i].Contains("flipFaces = "))
                        obj.flipFaces = bool.Parse(lines[i].Replace("flipFaces = ", ""));
                    else if (lines[i].Contains("normalsRecalc = "))
                        obj.normalsRecalculation = (NormalsRecalculation)Enum.Parse(typeof(NormalsRecalculation), lines[i].Replace("normalsRecalc = ", ""), true);
                    else if (lines[i].Contains("visibility = "))
                        obj.visibility = (ProceduralObjectVisibility)Enum.Parse(typeof(ProceduralObjectVisibility), lines[i].Replace("visibility = ", ""), true);
                    else if (lines[i].Contains("customTexture = "))
                    {
                        obj.customTexture = TextureManager.instance.FindTexture(lines[i].Replace("customTexture = ", ""));
                        /*
                        if (lines[i].Replace("customTexture = ", "") == "null")
                            obj.customTexture = null;
                        else if (!availableTextures.Any(tex => tex.name == lines[i].Replace("customTexture = ", "")))
                            Debug.LogError("[ProceduralObjects] A saved object was found with a texture that doesn't exist anymore with the name " + lines[i].Replace("customTexture = ", "") + ", therefore loading the default object texture");
                        else
                            obj.customTexture = availableTextures.FirstOrDefault(tex => tex.name == lines[i].Replace("customTexture = ", "")); */
                    }
                    else if (lines[i].Contains("textParam: "))
                    {
                        if (obj.textParam == null)
                            obj.textParam = new TextParameters();
                        obj.textParam.AddField(TextField.ParseText(lines[i], FontManager.instance));
                    }
                    else if (lines[i].Contains("colorRect: "))
                    {
                        if (obj.textParam == null)
                            obj.textParam = new TextParameters();
                        obj.textParam.AddField(TextField.ParseColorRect(lines[i]));
                    }
                    else if (lines[i].Contains("MODULE"))
                    {
                        try
                        {
                            if (lines[i + 1].Contains("{"))
                                currentModuleData = new Dictionary<string, string>();
                        }
                        catch { continue; }
                    }
                    else if (!original)
                    {
                        if (lines[i].Contains("VERTICES "))
                            obj.allVertices = new Vector3[int.Parse(lines[i].Replace("VERTICES ", ""))];
                        else if (lines[i].Contains("vertex"))
                        {
                            string[] split = lines[i].Replace("vertex ", "").Split(new string[] { " = " }, StringSplitOptions.RemoveEmptyEntries);
                            obj.allVertices[int.Parse(split[0])] = VertexUtils.ParseVector3(split[1]);
                        }
                    }
                }
                obj.modules = ModuleManager.LoadModulesFromData(modulesData, false, null);
            }
            catch
            {
                Debug.LogError("[ProceduralObjects] Couldn't load an External Procedural Object : Invalid, corrupted or edited file at path : " + m_filePath);
            }
            this.m_externalType = ClipboardProceduralObjects.ClipboardType.Single;
            this.m_object = obj;
        }
        private void createSelectionClipboard()
        {
            var fileLines = File.ReadAllLines(m_filePath);
            Dictionary<CacheProceduralObject, Vector3> objects = new Dictionary<CacheProceduralObject, Vector3>();

            CacheProceduralObject obj = null;
            Vector3 relativePos = Vector3.zero;
            var modulesData = new List<Dictionary<string, string>>();
            Dictionary<string, string> currentModuleData = null;

            string name = "";
            for (int i = 0; i < fileLines.Count(); i++)
            {
                try
                {
                    if (currentModuleData != null)
                    {
                        if (fileLines[i].Contains("{"))
                            continue;
                        else if (fileLines[i].Contains("}"))
                        {
                            modulesData.Add(currentModuleData);
                            currentModuleData = null;
                            continue;
                        }
                        else if (fileLines[i].Contains(" = "))
                        {
                            currentModuleData.Add(fileLines[i].GetUntilOrEmpty(" = ").Trim(), fileLines[i].GetStringAfter(" = ").Trim());
                        }
                        continue;
                    }
                    else if (fileLines[i].Contains("name = "))
                        name = fileLines[i].Replace("name = ", "").Trim();
                    else if (fileLines[i].Contains("{"))
                    {
                        obj = new CacheProceduralObject();
                        obj.tilingFactor = 8;
                        obj.m_color = Color.white;
                        obj.parent = -1;
                        obj.meshStatus = 2;
                        relativePos = Vector3.zero;
                        modulesData = new List<Dictionary<string, string>>();
                    }
                    else if (fileLines[i].Contains("}"))
                    {
                        obj.modules = ModuleManager.LoadModulesFromData(modulesData, false, null);
                        objects[obj] = relativePos;
                        modulesData = null;
                        obj = null;
                        relativePos = Vector3.zero;
                    }
                    else if (fileLines[i].Contains("baseInfoType = "))
                        obj.baseInfoType = fileLines[i].Replace("baseInfoType = ", "");
                    else if (fileLines[i].Contains("basePrefabName = "))
                        obj.basePrefabName = fileLines[i].Replace("basePrefabName = ", "");
                    else if (fileLines[i].Contains("relativePosition = "))
                        relativePos = fileLines[i].Replace("relativePosition = ", "").ParseVector3();
                    else if (fileLines[i].Contains("absPosition = "))
                        obj._staticPos = fileLines[i].Replace("absPosition = ", "").ParseVector3();
                    else if (fileLines[i].Contains("renderDistance = "))
                        obj.renderDistance = float.Parse(fileLines[i].Replace("renderDistance = ", ""));
                    else if (fileLines[i].Contains("renderDistLocked = "))
                        obj.renderDistLocked = bool.Parse(fileLines[i].Replace("renderDistLocked = ", ""));
                    else if (fileLines[i].Contains("isPloppableAsphalt = "))
                        obj.isPloppableAsphalt = bool.Parse(fileLines[i].Replace("isPloppableAsphalt = ", ""));
                    else if (fileLines[i].Contains("rotation = "))
                        obj.m_rotation = VertexUtils.ParseQuaternion(fileLines[i].Replace("rotation = ", ""));
                    else if (fileLines[i].Contains("disableRecalculation = "))
                        obj.disableRecalculation = bool.Parse(fileLines[i].Replace("disableRecalculation = ", ""));
                    else if (fileLines[i].Contains("tilingFactor = "))
                        obj.tilingFactor = int.Parse(fileLines[i].Replace("tilingFactor = ", ""));
                    else if (fileLines[i].Contains("parenting = "))
                    {
                        var splited = fileLines[i].Replace("parenting = ", "").Trim().Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                        obj.temp_id = int.Parse(splited[0]);
                        obj.parent = int.Parse(splited[1]);
                    }
                    else if (fileLines[i].Contains("disableRecalculation = "))
                        obj.disableRecalculation = bool.Parse(fileLines[i].Replace("disableRecalculation = ", ""));
                    else if (fileLines[i].Contains("flipFaces = "))
                        obj.flipFaces = bool.Parse(fileLines[i].Replace("flipFaces = ", ""));
                    else if (fileLines[i].Contains("color = "))
                        obj.m_color = SerializableColor.Parse(fileLines[i].Replace("color = ", ""));
                    else if (fileLines[i].Contains("normalsRecalc = "))
                        obj.normalsRecalculation = (NormalsRecalculation)Enum.Parse(typeof(NormalsRecalculation), fileLines[i].Replace("normalsRecalc = ", ""), true);
                    else if (fileLines[i].Contains("visibility = "))
                        obj.visibility = (ProceduralObjectVisibility)Enum.Parse(typeof(ProceduralObjectVisibility), fileLines[i].Replace("visibility = ", ""), true);
                    else if (fileLines[i].Contains("textParam: "))
                    {
                        if (obj.textParam == null)
                            obj.textParam = new TextParameters();
                        obj.textParam.AddField(TextField.ParseText(fileLines[i], FontManager.instance));
                    }
                    else if (fileLines[i].Contains("colorRect: "))
                    {
                        if (obj.textParam == null)
                            obj.textParam = new TextParameters();
                        obj.textParam.AddField(TextField.ParseColorRect(fileLines[i]));
                    }
                    else if (fileLines[i].Contains("customTexture = "))
                    {
                        obj.customTexture = TextureManager.instance.FindTexture(fileLines[i].Replace("customTexture = ", ""));
                        /*
                        if (fileLines[i].Replace("customTexture = ", "") == "null")
                            obj.customTexture = null;
                        else if (!availableTextures.Any(tex => tex.name == fileLines[i].Replace("customTexture = ", "")))
                            Debug.LogError("[ProceduralObjects] A saved object was found with a texture that doesn't exist anymore with the name " + fileLines[i].Replace("customTexture = ", "") + ", therefore loading the default object texture");
                        else
                            obj.customTexture = availableTextures.FirstOrDefault(tex => tex.name == fileLines[i].Replace("customTexture = ", "")); */
                    }
                    else if (fileLines[i].Contains("ORIGINALMODEL"))
                        obj.meshStatus = 1;
                    else if (fileLines[i].Contains("MODULE"))
                    {
                        try
                        {
                            if (fileLines[i + 1].Contains("{"))
                                currentModuleData = new Dictionary<string, string>();
                        }
                        catch { continue; }
                    }
                    else if (fileLines[i].Contains("VERTICES "))
                        obj.allVertices = new Vector3[int.Parse(fileLines[i].Replace("VERTICES ", ""))];
                    else if (fileLines[i].Contains("vertex"))
                    {
                        string[] split = fileLines[i].Replace("vertex ", "").Split(new string[] { " = " }, StringSplitOptions.RemoveEmptyEntries);
                        obj.allVertices[int.Parse(split[0])] = VertexUtils.ParseVector3(split[1]);
                    }
                }
                catch
                {
                    Debug.LogError("[ProceduralObjects] Error while loading an object from export at path : " + m_filePath);
                }
            }
            ClipboardProceduralObjects selec = new ClipboardProceduralObjects(ClipboardProceduralObjects.ClipboardType.Selection);
            selec.selection_objects = objects;
            var groupInfo = new Dictionary<CacheProceduralObject, CacheProceduralObject>();
            var objlist = objects.Keys.ToList();
            foreach (var o in objlist)
            {
                if (o.parent == -1) continue;
                try
                {
                    var parent = objlist.First(po => po.temp_id == o.parent);
                    groupInfo.Add(o, parent);
                }
                catch { continue; }
            }
            if (groupInfo.Count > 0) 
                selec.groupInformation = groupInfo;
            this.m_name = name;
            this.m_selection = selec;
            this.m_externalType = ClipboardProceduralObjects.ClipboardType.Selection;
        }

        public string m_name, m_filePath;
        public CacheProceduralObject m_object;
        public ClipboardProceduralObjects m_selection;
        public ClipboardProceduralObjects.ClipboardType m_externalType;
        public bool isWorkshop, isStatic, hasClipboard;
    }
}
