using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;

using ProceduralObjects.ProceduralText;
using System.Reflection;
using ICities;
using System.Runtime.InteropServices;
using ProceduralObjects.SelectionMode;
//using System.Diagnostics;
using Epic.OnlineServices.Presence;
using System.Runtime.InteropServices.ComTypes;
using System.Text.RegularExpressions;

namespace ProceduralObjects.Classes
{
    public class ClipboardProceduralObjects
    {
        public ClipboardProceduralObjects(ClipboardType type)
        {
            this.type = type;
        }
        public static Assembly ModToolsAssembly;
        static Type t_MTFbxConverter;
        static bool isMTSetup = false;

        public static void SetupModTools()
        {
            if (isMTSetup) return;

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.FullName.Substring(0, 8) == "ModTools")
                {
                    Debug.Log("[ProceduralObjects][SetupModTools] Found assembly matching ModTools, fetching types....");
                    if (assembly.GetType("ModTools.FbxUtil.FbxConverter") != null)
                    {
                        Debug.Log("[ProceduralObjects][SetupModTools] Found FbxConverter type at \'ModTools.FbxUtil.FbxConverter\'");
                        ModToolsAssembly = assembly;
                        t_MTFbxConverter = ModToolsAssembly.GetType("ModTools.FbxUtil.FbxConverter");
                        break;
                    } else if (assembly.GetType("FbxUtil.FbxConverter") != null)
                    {
                        Debug.Log("[ProceduralObjects][SetupModTools] Found FbxConverter type at \'FbxUtil.FbxConverter\'");
                        ModToolsAssembly = assembly;
                        t_MTFbxConverter = ModToolsAssembly.GetType("FbxUtil.FbxConverter");
                        break;
                    }
                }
            }

            if (ModToolsAssembly == null)
            {
                Debug.LogError("[ProceduralObjects] ModTools assembly not found !");
                return;
            }
        
            isMTSetup = true;
        }

        public void MakeSelectionList(List<ProceduralObject> list, POGroup selectedGroup)
        {
            this.selection_objects = new Dictionary<CacheProceduralObject, Vector3>();
            for (int i = 0; i < list.Count; i++)
            {
                var obj = list[i];
                Vector3 relativePos;
                if (i == 0)
                    relativePos = Vector3.zero;
                else
                    relativePos = obj.m_position - list[0].m_position;
                var cachePO = new CacheProceduralObject(obj);
                cachePO.temp_id = obj.id;
                cachePO.parent = -1;
                this.selection_objects.Add(cachePO, relativePos);
            }
            if (selectedGroup != null)
            {
                groupInformation = null;
                return;
            }
            else
            {
                groupInformation = new Dictionary<CacheProceduralObject, CacheProceduralObject>();
                for (int i = 0; i < list.Count; i++)
                {
                    var obj = list[i];
                    if (obj.group == null)
                        continue;
                    if (obj.isRootOfGroup)
                        continue;
                    if (!list.Contains(obj.group.root))
                        continue;
                    groupInformation.Add(selection_objects.Keys.ToList()[i],
                        selection_objects.Keys.ToList()[list.IndexOf(obj.group.root)]);
                    selection_objects.Keys.ToList()[i].parent = obj.group.root.id;
                }
            }
        }

        public CacheProceduralObject single_object;
        public Dictionary<CacheProceduralObject, Vector3> selection_objects;
        public ClipboardType type;
        public Dictionary<CacheProceduralObject, CacheProceduralObject> groupInformation;

        public void RecreateGroups(Dictionary<CacheProceduralObject, ProceduralObject> createdObjects)
        {
            if (groupInformation == null)
                return;
            if (groupInformation.Count == 0)
                return;

            foreach (var kvp in createdObjects)
            {
                if (!groupInformation.ContainsKey(kvp.Key))
                    continue;

                if (createdObjects[groupInformation[kvp.Key]].group == null)
                {
                    var group = POGroup.CreateGroupWithRoot(createdObjects[groupInformation[kvp.Key]]);
                    ProceduralObjectsLogic.instance.groups.Add(group);
                }
                createdObjects[groupInformation[kvp.Key]].group.AddToGroup(kvp.Value);
            }
        }

        public void ExportSelection(string name, ExternalProceduralObjectsManager manager, ExportSelection.ExportMode exportMode)
        {
            Debug.Log("[ProceduralObjects][ExportSelection] Exporting Selection...");
            if (selection_objects == null)
                return;
            if (selection_objects.Count <= 1)
                return;
            string path = ProceduralObjectsMod.ExternalsConfigPath + name.ToFileName() + ".pobj";
            if (File.Exists(path))
                return;

            TextWriter tw = new StreamWriter(path);
            string externaltype;
            switch (exportMode)
            {
                case SelectionMode.ExportSelection.ExportMode.Ploppable:
                    externaltype = "selection";
                    break;
                case SelectionMode.ExportSelection.ExportMode.StaticImport:
                    externaltype = "static";
                    break;
                case SelectionMode.ExportSelection.ExportMode.FBX:
                    Debug.Log("[ProceduralObjects][ExportSelection] Exporting as FBX...");
                    externaltype = "selection";
                    break;
                default:
                    externaltype = "selection";
                    break;
            }
            tw.WriteLine("externaltype = " + externaltype);
            tw.WriteLine("name = " + name);
            int objNum = 0;
            foreach (KeyValuePair<CacheProceduralObject, Vector3> kvp in selection_objects)
            {
                objNum++;
                tw.WriteLine("OBJECT");
                tw.WriteLine("{");
                tw.WriteLine("baseInfoType = " + kvp.Key.baseInfoType);
                tw.WriteLine("basePrefabName = " + kvp.Key.basePrefabName);
                if (exportMode == SelectionMode.ExportSelection.ExportMode.StaticImport) //if (staticImport)
                    tw.WriteLine("absPosition = " + kvp.Key._staticPos.ToStringUnrounded());
                else
                    tw.WriteLine("relativePosition = " + kvp.Value.ToStringUnrounded());
                tw.WriteLine("isPloppableAsphalt = " + kvp.Key.isPloppableAsphalt.ToString());
                //  tw.WriteLine("scale = " + pobj.scale.ToString());
                tw.WriteLine("parenting = " + kvp.Key.temp_id + ";" + kvp.Key.parent);
                tw.WriteLine("customTexture = " + ((kvp.Key.customTexture == null) ? "null" : kvp.Key.customTexture.name));
                tw.WriteLine("renderDistance = " + kvp.Key.renderDistance.ToString());
                tw.WriteLine("renderDistLocked = " + kvp.Key.renderDistLocked.ToString());
                tw.WriteLine("rotation = " + kvp.Key.m_rotation.ToStringUnrounded());
                tw.WriteLine("disableRecalculation = " + kvp.Key.disableRecalculation.ToString());
                if (kvp.Key.tilingFactor != 8)
                    tw.WriteLine("tilingFactor = " + kvp.Key.tilingFactor.ToString());
                tw.WriteLine("color = " + ((SerializableColor)kvp.Key.m_color).ToString());
                tw.WriteLine("flipFaces = " + kvp.Key.flipFaces.ToString());
                tw.WriteLine("disableCastShadows = " + kvp.Key.disableCastShadows.ToString());
                tw.WriteLine("normalsRecalc = " + kvp.Key.normalsRecalculation.ToString());
                tw.WriteLine("visibility = " + kvp.Key.visibility.ToString());
                if (kvp.Key.textParam != null)
                {
                    if (kvp.Key.textParam.Count() > 0)
                    {
                        foreach (TextField field in kvp.Key.textParam.m_textFields)
                        {
                            tw.WriteLine(TextField.SaveString(field));
                        }
                    }
                }
                if (kvp.Key.modules != null)
                {
                    if (kvp.Key.modules.Count > 0)
                        ModuleManager.WriteModules(tw, kvp.Key.modules, false);
                }
                Debug.Log("[ProceduralObjects][Mesh_Serialization] Attempting to serialize mesh");
                if (exportMode == SelectionMode.ExportSelection.ExportMode.FBX)
                {
                    Debug.Log("[ProceduralObjects][FBX_Export] Attempting to export mesh");
                    SetupModTools();
                    if (!isMTSetup)
                        return;

                    MethodInfo m_ModToolsExportFBXInfo;
                    m_ModToolsExportFBXInfo = t_MTFbxConverter.GetMethod("ExportAsciiFbx", new Type[] { typeof(Mesh), typeof(Stream) });
                    if (m_ModToolsExportFBXInfo != null) 
                        Debug.Log("[ProceduralObjects][FBX_Export] Found ExportAsciiFbx method!");

                    string fbxPath = ProceduralObjectsMod.ExternalsConfigPath + name.ToFileName() + ".base_" + kvp.Key.basePrefabName + ".mesh" + objNum + ".fbx";
                    fbxPath = sanitizeFileName(name.ToFileName() + ".base_" + kvp.Key.basePrefabName + ".mesh" + objNum);
                    fbxPath = ProceduralObjectsMod.ExternalsConfigPath + fbxPath + ".fbx";
                    Debug.LogFormat("[ProceduralObjects][FBX_Export] Checking file: \'{0}\'!", fbxPath);
                    if (File.Exists(fbxPath))
                        return;

                    try
                    {
                        var stream = new FileStream(fbxPath, FileMode.Create);
                        Debug.Log("[ProceduralObjects][FBX_Export] Created the File Stream for the mesh");
                        var m_mesh = kvp.Key.mesh.InstantiateMesh();
                        Debug.LogFormat("[ProceduralObjects][FBX_Export] Instantiated the Mesh: {0}", m_mesh);
                        if (kvp.Key.meshStatus == 1)
                        {
                            Debug.Log("[ProceduralObjects][FBX_Export] The model is unchanged, trying export anyways!");
                            m_mesh.SetVertices(new List<Vector3>(m_mesh.vertices));
                        }
                        else
                        {
                            Debug.Log("[ProceduralObjects][FBX_Export] The model is different. Def need an export!");
                            m_mesh.SetVertices(new List<Vector3>(kvp.Key.allVertices));
                        }
                        Debug.LogFormat("[ProceduralObjects][FBX_Export] Set verts on mesh: \'{0}\'!", m_mesh.name);

                        var meow = m_ModToolsExportFBXInfo.Invoke(m_mesh, new object[] { m_mesh, stream });
                        if (meow != null)
                            Debug.LogFormat("[ProceduralObjects][FBX_Export] Invoked `ExportAsciiFbx`!!!!  :::: {0}", meow);
                        else
                            Debug.LogFormat("[ProceduralObjects][FBX_Export] Apparently invoke didn't like... do it's thing... This is `ExportAsciiFbx`:: {0}", meow);

                        tw.WriteLine("EXPORTEDMESH");
                        tw.WriteLine("exportPath = " + fbxPath);
                    }
                    catch 
                    {
                        Debug.LogError("Failed to invoke modtools method... at least clean up and ensure ploppable export works.");
                    }
                    finally
                    {
                        if (kvp.Key.meshStatus == 1)
                            tw.WriteLine("ORIGINALMODEL");
                        else
                        {
                            tw.WriteLine("VERTICES " + kvp.Key.allVertices.Count());
                            for (int i = 0; i < kvp.Key.allVertices.Count(); i++)
                            {
                                tw.WriteLine("vertex " + i.ToString() + " = " + kvp.Key.allVertices[i].ToStringUnrounded());
                            }
                        }
                    }

                }
                else
                {
                    if (kvp.Key.meshStatus == 1)
                        tw.WriteLine("ORIGINALMODEL");
                    else
                    {
                        tw.WriteLine("VERTICES " + kvp.Key.allVertices.Count());
                        for (int i = 0; i < kvp.Key.allVertices.Count(); i++)
                        {
                            tw.WriteLine("vertex " + i.ToString() + " = " + kvp.Key.allVertices[i].ToStringUnrounded());
                        }
                    }
                }
                tw.WriteLine("}");
            }
            tw.Close();

            ProceduralUtils.ExportRequiredAssetsHTML(ProceduralObjectsMod.ExternalsConfigPath + name.ToFileName() + " - required assets.html", selection_objects.Keys.ToList());
        }
        public enum ClipboardType
        {
            Single,
            Selection
        }

        private static string sanitizeFileName(string fileName)
        {
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);
            string newFileName = Regex.Replace(fileName, invalidRegStr, "_");
            if (newFileName.Length == 0)
                Debug.Log("File Name " + fileName + " results in a empty fileName!");
            return newFileName;
        }

    }
}
