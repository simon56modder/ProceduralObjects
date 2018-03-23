using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICities;
using ColossalFramework.IO;
using ColossalFramework.Plugins;
using UnityEngine;
using ColossalFramework.PlatformServices;
using ColossalFramework;

using ProceduralObjects.Tools;
using ProceduralObjects.Classes;

namespace ProceduralObjects
{
    public class ProceduralObjectsMod : LoadingExtensionBase, IUserMod
    {
        public string Name
        {
            get
            {
                return "Procedural Objects mod";
            }
        }
        public string Description
        {
            get { return "Extreme procedural objects customization tool"; }
        }


        public const string VERSION = "1.3.0";
        public const string DOCUMENTATION_URL = "http://cscreators.referata.com/wiki/Procedural_Objects";
        public const string OTHER_SETTINGS_FILENAME = "ProceduralObjectsSettings";


        public static string TextureConfigPath
        {
            get
            {
                if (IsLinux)
                    return DataLocation.localApplicationData + @"/ModConfig/ProceduralObjects/";
                return DataLocation.localApplicationData + @"\ModConfig\ProceduralObjects\";
            }
        }
        public static string ExternalsConfigPath
        {
            get
            {
                if (IsLinux)
                    return DataLocation.localApplicationData + @"/ModConfig/SavedProceduralObjects/";
                return DataLocation.localApplicationData + @"\ModConfig\SavedProceduralObjects\";
            }
        }
        public static bool IsLinux
        {
            get
            {
                return Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.LinuxPlayer;
            }
        }
        public static GameObject gameLogicObject, editorHelperObject;
        public static ProceduralObjectContainer[] tempContainerData = null;

        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);

            var _initializeToolMan = ToolMan.Initialize();
            if (_initializeToolMan)
                Debug.Log("[ProceduralObj] Successfully created the ProceduralTool");

            if (mode == LoadMode.NewGame || mode == LoadMode.LoadGame)
            {
                if (gameLogicObject == null)
                {
                    gameLogicObject = new GameObject("Logic_ProceduralObjects");
                    gameLogicObject.AddComponent<ProceduralObjectsLogic>();
                    gameLogicObject.AddComponent<UpdateInformant>();
                }
            }
            else if (mode == LoadMode.NewAsset || mode == LoadMode.LoadAsset)
            {
                if (editorHelperObject == null)
                {
                    editorHelperObject = new GameObject("EditorHelper_ProceduralObjects");
                    editorHelperObject.AddComponent<ProceduralEditorHelper>();
                }
            }
        }
        public override void OnLevelUnloading()
        {
            base.OnLevelUnloading();

            if (gameLogicObject != null)
            {
                UnityEngine.Object.Destroy(gameLogicObject);
                gameLogicObject = null;
            }
            if (editorHelperObject != null)
            {
                UnityEngine.Object.Destroy(editorHelperObject);
                editorHelperObject = null;
            }
        }


        public ProceduralObjectsMod()
        {
            try
            {
                GameSettings.AddSettingsFile(new SettingsFile[] { new SettingsFile() { fileName = OTHER_SETTINGS_FILENAME } });
            }
            catch (Exception e)
            {
                Debug.Log("[ProceduralObj] Failed to add the settings file :");
                Debug.LogException(e);
            }
        }
    }
}
