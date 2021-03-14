using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using UnityEngine;
using ProceduralObjects.Classes;
using ProceduralObjects.UI;
using ProceduralObjects.Localization;

namespace ProceduralObjects
{
    public class ModuleManager
    {
        public ModuleManager(ProceduralObjectsLogic logic)
        {
            modules = new List<POModule>();
            enabledModules = new List<POModule>();
            openedUIModules = new List<POModule>();
            searchResults = new Dictionary<POModuleType, int>();
            searchTextfield = "";
            instance = this;
            this.logic = logic;
        }

        public static ModuleManager instance;

        public List<POModule> modules, enabledModules;
        public ProceduralObjectsLogic logic;

        public Rect managerWindow = new Rect(555, 200, 400, 395);
        public Rect windowRect = new Rect(200, 200, 250, 320);
        private Vector2 scrollModuleTypes;
        private POModuleType selectedModuleType;
        private int moduleCountMap;
        public ProceduralObject selectedObject;
        public List<POModule> openedUIModules;
        public bool showWindow, showManagerWindow;
        private string searchTextfield;
        private Dictionary<POModuleType, int> searchResults;
        private Vector2 scrollSearchResults;

        public void DrawWindow()
        {
            if (showManagerWindow)
                managerWindow = GUIUtils.ClampRectToScreen(GUIUtils.Window(503017480, managerWindow, draw, LocalizationManager.instance.current["modules_management"]));
        }
        private void draw(int id)
        {
            if (GUIUtils.CloseHelpButtons(managerWindow, "Modules"))
            {
                selectedModuleType = null;
                showManagerWindow = false;
            }
            GUI.DragWindow(new Rect(0, 0, 344, 23));

            GUI.Label(new Rect(10, 25, 380, 25), "<size=13><b>" + LocalizationManager.instance.current["MT_installed"] + "</b> (" + ProceduralObjectsMod.ModuleTypes.Count + ") :</size>");

            GUI.Box(new Rect(10, 52, 355, 200), string.Empty);
            scrollModuleTypes = GUI.BeginScrollView(new Rect(12, 54, 376, 196), scrollModuleTypes, new Rect(0, 0, 354, ProceduralObjectsMod.ModuleTypes.Count * 28 + 2));
            for (int i = 0; i < ProceduralObjectsMod.ModuleTypes.Count; i++)
            {
                var module = ProceduralObjectsMod.ModuleTypes[i];
                if (GUI.Button(new Rect(0, i * 28, 352, 23), "<b>" + module.Name + "</b> " + LocalizationManager.instance.current["made_by"] + " " + module.Author))
                {
                    ProceduralObjectsLogic.PlaySound();
                    selectedModuleType = module;
                    moduleCountMap = modules.Count(m => m.ModuleType == selectedModuleType);
                }
                GUI.DrawTexture(new Rect(2, i * 28 + 2, 20, 20), ProceduralObjectsMod.Icons[module.hide_all ? 3 : 4]);
            }
            GUI.EndScrollView();

            GUI.BeginGroup(new Rect(10, 258, 380, 130));
            GUI.Box(new Rect(0, 0, 380, 130), string.Empty);
            if (selectedModuleType != null)
            {
                GUI.Label(new Rect(6, 2, 370, 50), "<size=16>" + selectedModuleType.Name + "</size> " + LocalizationManager.instance.current["made_by"] + " " + selectedModuleType.Author + "\n" + LocalizationManager.instance.current["modules_count"] +
                    " : <b>" + moduleCountMap + "</b>" + (selectedModuleType.maxModulesOnMap == 0 ? "" : " /" + selectedModuleType.maxModulesOnMap));

                selectedModuleType.hide_all = GUI.Toggle(new Rect(2, 50, 300, 22), selectedModuleType.hide_all, LocalizationManager.instance.current["disable_all"]);
            }
            GUI.EndGroup();
        }
        public void SetPosition(float x, float y)
        {
            managerWindow.position = new Vector2(x, y);
            selectedModuleType = null;
        }

        public void ShowModulesWindow(ProceduralObject obj)
        {
            ProceduralObjectsLogic.PlaySound();
            windowRect.position = new Vector2(logic.window.xMax, logic.window.y);
            if (openedUIModules == null)
                openedUIModules = new List<POModule>();
            else
                openedUIModules.Clear();
            if (obj.m_modules == null)
                obj.m_modules = new List<POModule>();
            searchTextfield = "";
            UpdateSearchResults();
            selectedObject = obj;
            showWindow = true;
        }
        public void CloseWindow()
        {
            showWindow = false;
            openedUIModules.Clear();
            searchTextfield = "";
        }
        public void DrawWCustomizationindows()
        {
            if (selectedObject == null)
                return;

            windowRect.height = 241 + selectedObject.m_modules.Count * 26;
            if (showWindow)
                windowRect = GUIUtils.ClampRectToScreen(GUIUtils.Window(581644831, windowRect, drawCtWindow, LocalizationManager.instance.current["modules"]));

            if (openedUIModules.Count == 0)
                return;

            for (int i = 0; i < openedUIModules.Count; i++)
            {
                var m = openedUIModules[i];
                try { m.window = GUIUtils.ClampRectToScreen(GUIUtils.Window(581644838 + i, m.window, m.DrawCustomizationWindow, m.ModuleType.Name)); }
                catch (Exception e) { Debug.LogError("[ProceduralObjects] Error inside module DrawCustomizationWindow() method!\n" + e); }
            }
        }
        private void drawCtWindow(int id)
        {
            if (GUIUtils.CloseHelpButtons(windowRect, "Modules"))
            {
                ProceduralObjectsLogic.PlaySound();
                CloseWindow();
            }
            GUI.DragWindow(new Rect(0, 0, 189, 22));
            GUI.Label(new Rect(5, 22, 240, 18), "<size=12>" + LocalizationManager.instance.current["modules_current"] + "</size>");
            int fromtop = 40;
            foreach (var m in selectedObject.m_modules)
            {
                if (GUI.Button(new Rect(5, fromtop, 240, 24), m.ModuleType.Icon == null ? m.ModuleType.Name : string.Empty))
                {
                    ProceduralObjectsLogic.PlaySound();
                    m.window.position = new Vector2(windowRect.xMax, windowRect.y);
                    if (!openedUIModules.Contains(m))
                        openedUIModules.Add(m);
                }
                if (m.ModuleType.Icon != null)
                {
                    GUI.DrawTexture(new Rect(6, fromtop + 1, 22, 22), m.ModuleType.Icon);
                    GUI.Label(new Rect(30, fromtop, 212, 24), m.ModuleType.Name);
                }
                fromtop += 26;
            }
            GUIUtils.DrawSeparator(new Vector2(3, fromtop), 244);
            GUI.Label(new Rect(5, fromtop + 4, 240, 18), "<size=12>" + LocalizationManager.instance.current["modules_add_new"] + "</size>");
            GUI.BeginGroup(new Rect(5, fromtop + 24, 240, 172));
            var str = GUI.TextField(new Rect(0, 0, 240, 24), searchTextfield);
            if (str != searchTextfield)
            {
                searchTextfield = str;
                UpdateSearchResults();
            }
            scrollSearchResults = GUI.BeginScrollView(new Rect(0, 25, 240, 147), scrollSearchResults, new Rect(0, 0, 222, searchResults.Count * 26));
            for (int i = 0; i < searchResults.Count; i++)
            {
                var mType = searchResults.Keys.ToList()[i];
                var h = 1f + (i * 26);
                bool isMax = (mType.maxModulesOnMap == 0) ? false : (searchResults[mType] >= mType.maxModulesOnMap);
                if (isMax)
                    GUI.color = Color.red;
                if (GUI.Button(new Rect(2, h, 221, 24), (mType.Icon == null ? "<b>+</b> " + mType.Name : string.Empty) +
                    (isMax ? " (" + LocalizationManager.instance.current["max_reached"] + ")" : "")))
                {
                    ProceduralObjectsLogic.PlaySound();
                    if (!isMax)
                    {
                        var m = this.AddModule(mType, selectedObject);
                        m.window.position = new Vector2(windowRect.xMax, windowRect.y);
                        if (!openedUIModules.Contains(m))
                            openedUIModules.Add(m);
                    }
                }
                if (mType.Icon != null)
                {
                    GUI.DrawTexture(new Rect(2, h + 1, 22, 22), mType.Icon);
                    GUI.Label(new Rect(26, h, 176, 24), "<b>+</b>" + mType.Name);
                }
                GUI.color = Color.white;
            }
            GUI.EndScrollView();
            GUI.EndGroup();
        }
        private void UpdateSearchResults()
        {
            searchResults.Clear();
            if (searchTextfield == "")
            {
                foreach (var mType in ProceduralObjectsMod.ModuleTypes)
                    searchResults.Add(mType, 0);
            }
            else
            {
                foreach (var mType in ProceduralObjectsMod.ModuleTypes)
                {
                    if (mType.Name.ToString().ToLower().Replace(" ", "").Contains(searchTextfield.ToLower().Replace(" ", "")))
                        searchResults.Add(mType, 0);
                }
            }
            foreach (POModule m in modules)
            {
                if (!searchResults.ContainsKey(m.ModuleType))
                    continue;
                searchResults[m.ModuleType] += 1;
            }
        }

        public void EnableModule(POModule m)
        {
            m.enabled = true;
            if (!enabledModules.Contains(m))
                enabledModules.Add(m);
            try { m.OnModuleEnabled(logic); }
            catch (Exception e) { Debug.LogError("[ProceduralObjects] Error inside module OnModuleEnabled() method!\n" + e); }
        }
        public void DisableModule(POModule m)
        {
            m.enabled = false;
            if (enabledModules.Contains(m))
                enabledModules.Remove(m);
            try { m.OnModuleDisabled(logic); }
            catch (Exception e) { Debug.LogError("[ProceduralObjects] Error inside module OnModuleDisabled() method!\n" + e); }
        }
        public void RemoveModule(POModule m)
        {
            if (!modules.Contains(m))
                return;
            try { m.OnModuleRemoved(logic); }
            catch (Exception e) { Debug.LogError("[ProceduralObjects] Error inside module OnModuleRemoved() method!\n" + e); }
            if (openedUIModules.Contains(m))
                openedUIModules.Remove(m);
            if (m.parentObject.m_modules.Contains(m))
                m.parentObject.m_modules.Remove(m);
            modules.Remove(m);
            if (enabledModules.Contains(m))
                enabledModules.Remove(m);
        }
        public void DeleteAllModules(ProceduralObject obj)
        {
            if (obj.m_modules == null)
                return;
            if (obj.m_modules.Count == 0)
                return;
            if (obj == selectedObject && showWindow)
                CloseWindow();
            foreach (var m in obj.m_modules)
            {
                if (!modules.Contains(m))
                    continue;
                try { m.OnModuleRemoved(logic); }
                catch (Exception e) { Debug.LogError("[ProceduralObjects] Error inside module OnModuleRemoved() method!\n" + e); }
                modules.Remove(m);
                if (enabledModules.Contains(m))
                    enabledModules.Remove(m);
            }
            obj.m_modules.Clear();
        }
        public POModule AddModule(POModuleType type, ProceduralObject obj)
        {
            POModule module = (POModule)Activator.CreateInstance(type.ModuleType);
            module.ModuleType = type;
            module.parentObject = obj;
            obj.m_modules.Add(module);
            module.enabled = true;
            modules.Add(module);
            enabledModules.Add(module);
            try { module.OnModuleCreated(logic); }
            catch (Exception e) { Debug.LogError("[ProceduralObjects] Error inside module OnModuleCreated() method!\n" + e); }
            return module;
        }
        public List<POModule> CloneModuleList(List<POModule> modules, ProceduralObject obj)
        {
            if (modules == null)
                return new List<POModule>();

            if (modules.Count == 0)
                return new List<POModule>();

            var list = new List<POModule>();
            foreach (var m in modules)
            {
                POModule clone;
                try {  clone = m.Clone(); }
                catch (Exception e)
                {
                    Debug.LogError("[ProceduralObjects] Error inside module Clone() method!\n" + e);
                    continue;
                }
                list.Add(clone);
                ModuleManager.instance.modules.Add(clone);
                if (clone.enabled)
                    ModuleManager.instance.enabledModules.Add(clone);
                clone.parentObject = obj;
                try { clone.OnModuleCreated(ProceduralObjectsLogic.instance); }
                catch (Exception e) { Debug.LogError("[ProceduralObjects] Error inside module OnModuleCreated() method!\n" + e); }
            }
            return list;
        }

        public static void WriteModules(TextWriter writer, List<POModule> modules, bool forSaveGame)
        {
            foreach (var m in modules)
            {
                var data = m._get_data(forSaveGame);
                writer.WriteLine("MODULE");
                writer.WriteLine("{");
                foreach (var kvp in data)
                {
                    writer.WriteLine(kvp.Key + " = " + kvp.Value);
                }
                writer.WriteLine("}");
            }
        }
        public static List<POModule> LoadModulesFromData(List<Dictionary<string, string>> modulesData, bool fromSaveGame, ProceduralObject obj)
        {
            var modules = new List<POModule>();
            if (modulesData != null)
            {
                if (modulesData.Count > 0)
                {
                    foreach (var data in modulesData)
                    // LOAD DATA FROM MODULES
                    {
                        if (data.Count == 0)
                        {
                            Debug.LogError("[ProceduralObjects] Couldn't find any data for a module");
                            continue;
                        }
                        if (!ModuleManager.CanLoadModule(data))
                        {
                            Debug.LogError("[ProceduralObjects] Couldn't find Module Type for the module. Maybe you are missing a PO Module mod ?");
                            continue;
                        }
                        try
                        {
                            var m = ModuleManager.LoadModule(data, fromSaveGame);
                            if (fromSaveGame)
                            {
                                ModuleManager.instance.modules.Add(m);
                                if (m.enabled)
                                    ModuleManager.instance.enabledModules.Add(m);
                                m.parentObject = obj;
                                try { m.OnModuleCreated(ProceduralObjectsLogic.instance); }
                                catch (Exception e) { Debug.LogError("[ProceduralObjects] Error inside module OnModuleCreated() method!\n" + e); }
                            }
                            modules.Add(m);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError("[ProceduralObjects] Failed to load a module. " + e);
                        }
                    }
                }
            }
            return modules;
        }
        public static POModule LoadModule(Dictionary<string, string> data, bool fromSaveGame)
        {
            if (data == null)
            {
                Debug.LogWarning("[ProceduralObjects] This module will fail to load because it has no data");
                return null;
            }
            if (!data.ContainsKey("TypeID"))
            {
                Debug.LogWarning("[ProceduralObjects] This module will fail to load because it does not have a TypeID");
                return null;
            }
            if (!ProceduralObjectsMod.ModuleTypes.Any(mType => mType.TypeID == data["TypeID"].Trim()))
            {
                Debug.LogWarning("[ProceduralObjects] This module will fail to load because no Type was found with this TypeID. Are you missing a required mod ?");
                return null;
            }
            var type = ProceduralObjectsMod.ModuleTypes.First(mType => mType.TypeID == data["TypeID"].Trim());
            var module = (POModule)Activator.CreateInstance(type.ModuleType);
            module.ModuleType = type;
            bool enabled = true;
            if (data.ContainsKey("enabled"))
                enabled = bool.Parse(data["enabled"]);
            else
                Debug.LogWarning("[ProceduralObjects] The Enabled state of a module wasn't found, enabling by default.");
            module.enabled = enabled;
            try { module.LoadData(data, fromSaveGame); }
            catch (Exception e) { Debug.LogError("[ProceduralObjects] Error inside module LoadData() method!\n" + e); }
            return module;
        }
        public static bool CanLoadModule(Dictionary<string, string> data)
        {
            if (data == null)
                return false;
            if (!data.ContainsKey("TypeID"))
                return false;
            if (ProceduralObjectsMod.ModuleTypes.Any(mType => mType.TypeID == data["TypeID"].Trim()))
                return true;
            return false;
        }
    }
}
