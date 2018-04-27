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
using ColossalFramework.UI;

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


        public const string VERSION = "1.4.3-2";
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

        // Settings panel

        public static readonly SavedFloat PropRenderDistance = new SavedFloat("propRenderDist", OTHER_SETTINGS_FILENAME, 850f, true);
        public static readonly SavedFloat BuildingRenderDistance = new SavedFloat("buildingRenderDist", OTHER_SETTINGS_FILENAME, 1000f, true);

        private UISlider propRenderSlider, buildingRenderSlider;
        private UITextField propRenderLabel, buildingRenderLabel;

        public void OnSettingsUI(UIHelperBase helper)
        {
            UIHelperBase group = helper.AddGroup("Procedural Objects");

            propRenderLabel = (UITextField)group.AddTextfield(" ", "Default prop-converted render distance : " + PropRenderDistance.value.ToString(), (t) => { }, (t) => { });
            propRenderLabel.disabledTextColor = Color.white;
            propRenderLabel.tooltip = "The default render distance for props converted to Procedural Objects";
            propRenderLabel.width = 700f;
            propRenderLabel.Disable();

            propRenderSlider = (UISlider)group.AddSlider(" ", 0f, 16000f, 10f, PropRenderDistance.value, propRenderDistanceChanged);
            propRenderSlider.width = 700f;
            propRenderSlider.height = 10f;

            buildingRenderLabel = (UITextField)group.AddTextfield(" ", "Default building-converted render distance : " + BuildingRenderDistance.value.ToString(), (t) => { }, (t) => { });
            buildingRenderLabel.disabledTextColor = Color.white;
            buildingRenderLabel.tooltip = "The default render distance for buildings converted to Procedural Objects";
            buildingRenderLabel.width = 700f;
            buildingRenderLabel.Disable();

            buildingRenderSlider = (UISlider)group.AddSlider(" ", 0f, 16000f, 10f, BuildingRenderDistance.value, buildingRenderDistanceChanged);
            buildingRenderSlider.width = 700f;
            buildingRenderSlider.height = 10f;
        }
        private void propRenderDistanceChanged(float value)
        {
            PropRenderDistance.value = value;
            propRenderLabel.text = "Default prop-converted render distance : " + value.ToString();
        }
        private void buildingRenderDistanceChanged(float value)
        {
            BuildingRenderDistance.value = value;
            buildingRenderLabel.text = "Default building-converted render distance : " + value.ToString();
        }
    }
}
