using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using ProceduralObjects.Classes;
using ProceduralObjects.UI;
using ProceduralObjects.Localization;

namespace ProceduralObjects
{
    public abstract class POModule
    {
        public ProceduralObject parentObject;
        public bool enabled, showWindow;
        public Rect window;
        public POModuleType ModuleType;

        /// <summary>
        /// Initialize the module with the provided data set.
        /// </summary>
        public virtual void LoadData(Dictionary<string, string> data, bool fromSaveGame) { }
        public Dictionary<string, string> _get_data(bool forSaveGame)
        {
            var dict = new Dictionary<string, string>();
            dict.Add("TypeID", ModuleType.TypeID);
            dict.Add("enabled", enabled.ToString());
            try { GetData(dict, forSaveGame); }
            catch (Exception e) { Debug.LogError("[ProceduralObjects] Error inside module GetData() method!\n" + e); }
            return dict;
        }
        /// <summary>
        /// Use data.Add(string,string) calls here to be used as Key/Value data pairs inside LoadData()
        /// </summary>
        public virtual void GetData(Dictionary<string, string> data, bool forSaveGame) { }

        /// <summary>
        /// Invoked upon module creation in game, whether through Copy/paste, Creation or Loading. This method should take the enabled field into account, for it may be required upon cloning of the module.
        /// </summary>
        public virtual void OnModuleCreated(ProceduralObjectsLogic logic) { }
        /// <summary>
        /// Invoked upon module or parent object deletion
        /// </summary>
        public virtual void OnModuleRemoved(ProceduralObjectsLogic logic) { }
        /// <summary>
        /// Window drawing function for a module.
        /// </summary>
        public virtual void DrawCustomizationWindow(int id)
        {
            if (ModuleType.Icon != null)
                GUI.DrawTexture(new Rect(2f, 2f, 21f, 21f), ModuleType.Icon);

            if (ModuleType.help_URL != "")
            {
                if (GUIUtils.CloseButton(window))
                {
                    if (ModuleManager.instance.openedUIModules.Contains(this))
                        ModuleManager.instance.openedUIModules.Remove(this);
                    showWindow = false;
                }
                GUI.DragWindow(new Rect(0, 0, window.width - 26, 22));
            }
            else
            {
                if (GUIUtils.CloseHelpButtonsURL(window, ModuleType.help_URL))
                {
                    if (ModuleManager.instance.openedUIModules.Contains(this))
                        ModuleManager.instance.openedUIModules.Remove(this);
                    showWindow = false;
                }
                GUI.DragWindow(new Rect(0, 0, window.width - 50, 22));
            }
            if (GUI.Button(new Rect(5, 22, (window.width - 15f) / 2f, 23f), string.Format(LocalizationManager.instance.current["modules_enabled"], enabled.GetHashCode())))
            {
                ProceduralObjectsLogic.PlaySound();
                if (enabled)
                    ModuleManager.instance.DisableModule(this);
                else
                    ModuleManager.instance.EnableModule(this);
            }
            if (GUI.Button(new Rect(10f + (window.width - 15f) / 2f, 22, (window.width - 15f) / 2f, 23f), LocalizationManager.instance.current["modules_delete"]))
            {
                ProceduralObjectsLogic.PlaySound();
                ModuleManager.instance.RemoveModule(this);
            }
        }

        /// <summary>
        /// Invoked when the module is enabled (turned on).
        /// </summary>
        public virtual void OnModuleEnabled(ProceduralObjectsLogic logic) { }
        /// <summary>
        /// Invoked when the module is disabled.
        /// </summary>
        public virtual void OnModuleDisabled(ProceduralObjectsLogic logic) { }

        /// <summary>
        /// Update method, runs in Update() if the module is enabled regardless of object visibility.
        /// </summary>
        public virtual void UpdateModule(ProceduralObjectsLogic logic, bool simulationPaused, bool layerVisible) { }

        /// <summary>
        /// Overwrite this to provide a deeper copy of the module instance when cloned. If not overwritten this method uses MemberwiseClone() (see Microsoft Docs MemberwiseClone() documentation)
        /// </summary>
        public virtual POModule Clone()
        {
            return (POModule)this.MemberwiseClone();
        }

        /// <summary>
        /// Called on each call of the Apply Model changes method Logic.Apply() 
        /// </summary>
        public virtual void OnApplyModelChange(Vertex[] vertices) { }
    }
    public class POModuleType
    {
        public string Name, Author, TypeID;
        public Texture2D Icon;
        public int maxModulesOnMap;
        public string help_URL;
        public bool hide_all;

        public Type ModuleType;
    }
}
