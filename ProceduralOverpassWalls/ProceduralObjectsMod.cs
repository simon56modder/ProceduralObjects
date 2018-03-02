using System.Collections.Generic;
using System.Linq;
using System.Text;
using ICities;
using ColossalFramework.IO;
using ColossalFramework.Plugins;
using UnityEngine;
using ColossalFramework.PlatformServices;

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
        public const string VERSION = "1.2.2.2";
        public const string DOCUMENTATION_URL = "http://cscreators.referata.com/wiki/Procedural_Objects";

        public static string ModConfigPath
        {
            get
            {
                if (IsLinux)
                    return DataLocation.localApplicationData + @"/ModConfig/ProceduralObjects/";
                return DataLocation.localApplicationData + @"\ModConfig\ProceduralObjects\";
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
                    gameLogicObject = new GameObject("ProceduralOverpassWallsMod_logicObject");
                    gameLogicObject.AddComponent<ProceduralObjectsLogic>();
                    gameLogicObject.AddComponent<UpdateInformant>();
                }
            }
            else if (mode == LoadMode.NewAsset || mode == LoadMode.LoadAsset)
            {
                if (editorHelperObject == null)
                {
                    editorHelperObject = new GameObject("ProceduralOverpassWallsMod_editorHelper");
                    editorHelperObject.AddComponent<ProceduralEditorHelper>();
                }
            }
        }
        public override void OnLevelUnloading()
        {
            base.OnLevelUnloading();

            if (gameLogicObject != null)
            {
                Object.Destroy(gameLogicObject);
                gameLogicObject = null;
            }
            if (editorHelperObject != null)
            {
                Object.Destroy(editorHelperObject);
                editorHelperObject = null;
            }
        }
    }
}
