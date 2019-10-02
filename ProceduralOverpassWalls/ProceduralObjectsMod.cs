using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ICities;
using ColossalFramework.IO;
using ColossalFramework.Plugins;
using UnityEngine;
using ColossalFramework.PlatformServices;
using ColossalFramework;
using ColossalFramework.UI;
using ColossalFramework.Globalization;

using ProceduralObjects.Tools;
using ProceduralObjects.Classes;
using ProceduralObjects.Localization;

namespace ProceduralObjects
{
    public class ProceduralObjectsMod : LoadingExtensionBase, IUserMod
    {
        public string Name
        {
            get
            {
                return "Procedural Objects " + VERSION;
            }
        }
        public string Description
        {
            get { return "Extreme objects customization tool"; }
        }


        public const string VERSION = "1.6";
        public const string DOCUMENTATION_URL = "http://proceduralobjects.shoutwiki.com/wiki/Main_Page";
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
                return Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.LinuxPlayer || Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer;
            }
        }
        public static GameObject gameLogicObject, editorHelperObject;
        public static Texture2D[] Icons = null;

        public static ProceduralObjectContainer[] tempContainerData = null;
        public static Layer[] tempLayerData = null;

        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);

            var _initializeToolMan = ToolMan.Initialize();
            if (_initializeToolMan)
                Debug.Log("[ProceduralObjects] Successfully created the ProceduralTool");

            if (mode == LoadMode.NewGame || mode == LoadMode.LoadGame)
            {
                if (gameLogicObject == null)
                {
                    if (Icons == null)
                    {
                        Icons = new Texture2D[] { TextureUtils.LoadTextureFromAssembly("duplicate"), 
                            TextureUtils.LoadTextureFromAssembly("maximize"), 
                            TextureUtils.LoadTextureFromAssembly("minimize"), 
                            TextureUtils.LoadTextureFromAssembly("invisible"), 
                            TextureUtils.LoadTextureFromAssembly("visible"),
                            TextureUtils.LoadTextureFromAssembly("rotate_right"),
                            TextureUtils.LoadTextureFromAssembly("move_up"),
                            TextureUtils.LoadTextureFromAssembly("move_down"), 
                            TextureUtils.LoadTextureFromAssembly("locked"),
                            TextureUtils.LoadTextureFromAssembly("unlocked")};
                    }
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
                Debug.LogError("[Procedural Objects] Failed to add the settings file :");
                Debug.LogException(e);
            }
        }


        // Settings panel

        public static SavedFloat PropRenderDistance = new SavedFloat("propRenderDist", OTHER_SETTINGS_FILENAME, 1700f, true);
        public static SavedFloat BuildingRenderDistance = new SavedFloat("buildingRenderDist", OTHER_SETTINGS_FILENAME, 2200f, true);
        public static SavedFloat GizmoSize = new SavedFloat("gizmoSize", OTHER_SETTINGS_FILENAME, 1.2f, true);
        public static SavedInt ConfirmDeletionThreshold = new SavedInt("confirmDeletionPanelThreshold", OTHER_SETTINGS_FILENAME, 2, true);
        public static SavedBool ShowConfirmDeletion = new SavedBool("showConfirmDeletion", OTHER_SETTINGS_FILENAME, true, true);
        public static SavedBool ShowDeveloperTools = new SavedBool("showDevTools", OTHER_SETTINGS_FILENAME, false, true);
        public static SavedString LanguageUsed = new SavedString("languageUsed", OTHER_SETTINGS_FILENAME, "default", true);

        private UISlider propRenderSlider, buildingRenderSlider, confirmDelThresholdSlider, gizmoSizeSlider;
        private UILabel propRenderLabel, buildingRenderLabel, confirmDelThresholdLabel, gizmoSizeLabel;
        private UICheckBox confirmDelCheckbox, showDevCheckbox;

        public void OnSettingsUI(UIHelperBase helper)
        {
            if (LocalizationManager.instance == null)
                LocalizationManager.CreateManager();

            UIHelperBase group = helper.AddGroup("    Procedural Objects");
            UIPanel globalPanel = ((UIPanel)((UIHelper)group).self);

            propRenderSlider = (UISlider)group.AddSlider(string.Format(LocalizationManager.instance.current["settings_RD_PROP_label"], PropRenderDistance.value.ToString()), 0f, 16000f, 10f, PropRenderDistance.value, propRenderDistanceChanged);
            propRenderSlider.width = 715;
            propRenderSlider.height = 16;
            propRenderSlider.tooltip = LocalizationManager.instance.current["settings_RD_PROP_tooltip"];

            var propRenderPanel = globalPanel.Find<UIPanel>("OptionsSliderTemplate(Clone)");
            propRenderPanel.name = "PropRenderSliderPanel";
            propRenderLabel = propRenderPanel.Find<UILabel>("Label");
            propRenderLabel.width *= 3.5f;

            //  group.AddSpace(10);

            buildingRenderSlider = (UISlider)group.AddSlider(string.Format(LocalizationManager.instance.current["settings_RD_BUILDING_label"], BuildingRenderDistance.value.ToString()), 0f, 16000f, 10f, BuildingRenderDistance.value, buildingRenderDistanceChanged);
            buildingRenderSlider.width = 715;
            buildingRenderSlider.height = 16;
            buildingRenderSlider.tooltip = LocalizationManager.instance.current["settings_RD_BUILDING_tooltip"];

            var buildingRenderPanel = globalPanel.components.First(c => c.GetType() == typeof(UIPanel) && c != propRenderPanel);
            buildingRenderPanel.name = "BuildingRenderSliderPanel";
            buildingRenderLabel = buildingRenderPanel.Find<UILabel>("Label");
            buildingRenderLabel.width *= 3.5f;


            //  group.AddSpace(10);

            gizmoSizeSlider = (UISlider)group.AddSlider(string.Format(LocalizationManager.instance.current["settings_GIZMO_label"], GizmoSize.value.ToString()), 0.3f, 2.5f, 0.1f, GizmoSize.value, gizmoSizeChanged);
            gizmoSizeSlider.width = 715;
            gizmoSizeSlider.height = 16;
            gizmoSizeSlider.tooltip = LocalizationManager.instance.current["settings_GIZMO_tooltip"];

            var gizmoPanel = globalPanel.components.First(c => c.GetType() == typeof(UIPanel) && c != propRenderPanel && c != buildingRenderPanel);
            gizmoPanel.name = "GizmoSizePanel";
            gizmoSizeLabel = gizmoPanel.Find<UILabel>("Label");
            gizmoSizeLabel.width *= 3.5f;

            group.AddSpace(32);

            var sliderGroup = helper.AddGroup("  " + LocalizationManager.instance.current["settings_CONFDEL_title"]);

            confirmDelCheckbox = (UICheckBox)sliderGroup.AddCheckbox(LocalizationManager.instance.current["settings_CONFDEL_toggle"], ShowConfirmDeletion.value, confirmDeletionCheckboxChanged);

            //   sliderGroup.AddSpace(10);

            confirmDelThresholdSlider = (UISlider)sliderGroup.AddSlider(string.Format(LocalizationManager.instance.current["settings_CONFDEL_SLIDER_label"], ConfirmDeletionThreshold.value.ToString()), 1f, 15f, 1f, ConfirmDeletionThreshold.value, confirmDeletionThresholdChanged);
            confirmDelThresholdSlider.width = 715;
            confirmDelThresholdSlider.height = 16;
            confirmDelThresholdSlider.tooltip = LocalizationManager.instance.current["settings_CONFDEL_SLIDER_tooltip"];

            confirmDelThresholdLabel = (UILabel)((UIPanel)((UIHelper)sliderGroup).self).components.First(c => c.GetType() == typeof(UIPanel) && c.components.Any(comp => comp.GetType() == typeof(UISlider)))
                .components.First(c => c.GetType() == typeof(UILabel));
            confirmDelThresholdLabel.width *= 3.5f;
            if (!ShowConfirmDeletion.value)
                confirmDelThresholdSlider.isEnabled = false;

            sliderGroup.AddSpace(32);
            var languageGroup = helper.AddGroup("  " + LocalizationManager.instance.current["settings_LANG_title"]);
            languageGroup.AddDropdown("Language selection", LocalizationManager.instance.identifiers, LocalizationManager.instance.available.IndexOf(LocalizationManager.instance.current), languageChanged);

            languageGroup.AddSpace(32);

            showDevCheckbox = (UICheckBox)languageGroup.AddCheckbox(LocalizationManager.instance.current["settings_DEVTOOLS_toggle"], ShowDeveloperTools.value, (value) =>
            {
                ShowDeveloperTools.value = value;
            });
            showDevCheckbox.tooltip = LocalizationManager.instance.current["settings_DEVTOOLS_tooltip"];
        }
        private void propRenderDistanceChanged(float value)
        {
            PropRenderDistance.value = value;
            propRenderLabel.text = string.Format(LocalizationManager.instance.current["settings_RD_PROP_label"], value.ToString());
        }
        private void buildingRenderDistanceChanged(float value)
        {
            BuildingRenderDistance.value = value;
            buildingRenderLabel.text = string.Format(LocalizationManager.instance.current["settings_RD_BUILDING_label"], value.ToString());
        }
        private void gizmoSizeChanged(float value)
        {
            GizmoSize.value = value;
            gizmoSizeLabel.text = string.Format(LocalizationManager.instance.current["settings_GIZMO_label"], value.ToString());
        }
        private void confirmDeletionThresholdChanged(float value)
        {
            ConfirmDeletionThreshold.value = (int)value;
            confirmDelThresholdLabel.text = string.Format(LocalizationManager.instance.current["settings_CONFDEL_SLIDER_label"], value.ToString());
        }
        private void confirmDeletionCheckboxChanged(bool value)
        {
            ShowConfirmDeletion.value = value;
            if (value)
                confirmDelThresholdSlider.isEnabled = true;
            else
              confirmDelThresholdSlider.isEnabled = false;
        }
        private void languageChanged(int value)
        {
            LocalizationManager.instance.SetCurrent(value);
            LanguageUsed.value = LocalizationManager.instance.current.identifier;
            propRenderLabel.text = string.Format(LocalizationManager.instance.current["settings_RD_PROP_label"], PropRenderDistance.value.ToString());
            confirmDelThresholdLabel.text = string.Format(LocalizationManager.instance.current["settings_CONFDEL_SLIDER_label"], ConfirmDeletionThreshold.value.ToString());
            buildingRenderLabel.text = string.Format(LocalizationManager.instance.current["settings_RD_BUILDING_label"], BuildingRenderDistance.value.ToString());
            confirmDelThresholdSlider.tooltip = LocalizationManager.instance.current["settings_CONFDEL_SLIDER_tooltip"];
            buildingRenderSlider.tooltip = LocalizationManager.instance.current["settings_RD_BUILDING_tooltip"];
            propRenderSlider.tooltip = LocalizationManager.instance.current["settings_RD_PROP_tooltip"];
            gizmoSizeLabel.text = string.Format(LocalizationManager.instance.current["settings_GIZMO_label"], GizmoSize.value.ToString());
            gizmoSizeSlider.tooltip = LocalizationManager.instance.current["settings_GIZMO_tooltip"];
            confirmDelCheckbox.text = LocalizationManager.instance.current["settings_CONFDEL_toggle"];
            showDevCheckbox.text = LocalizationManager.instance.current["settings_DEVTOOLS_toggle"];
            showDevCheckbox.tooltip = LocalizationManager.instance.current["settings_DEVTOOLS_tooltip"];
        }
    }
}
