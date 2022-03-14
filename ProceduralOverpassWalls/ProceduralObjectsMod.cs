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
using ProceduralObjects.UI;

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


        public static readonly string VERSION = "1.7.6";
        public static readonly string DOCUMENTATION_URL = "http://proceduralobjects.shoutwiki.com/wiki/";
        public static readonly string SETTINGS_FILENAME = "ProceduralObjectsSettings";
        public static SettingsFile SettingsFile;


        public static string PODirectoryPath
        {
            get
            {
                if (IsLinux)
                    return DataLocation.localApplicationData + @"/ProceduralObjects/";
                return DataLocation.localApplicationData + @"\ProceduralObjects\";
            }
        }
        public static string OldTextureConfigPath
        {
            get
            {
                if (IsLinux)
                    return DataLocation.localApplicationData + @"/ModConfig/ProceduralObjects/";
                return DataLocation.localApplicationData + @"\ModConfig\ProceduralObjects\";
            }
        }
        public static string TextureConfigPath
        {
            get
            {
                if (IsLinux)
                    return DataLocation.localApplicationData + @"/ProceduralObjects/Textures/";
                return DataLocation.localApplicationData + @"\ProceduralObjects\Textures\";
            }
        }
        public static string FontsPath
        {
            get
            {
                if (IsLinux)
                    return DataLocation.localApplicationData + @"/ProceduralObjects/Fonts/";
                return DataLocation.localApplicationData + @"\ProceduralObjects\Fonts\";
            }
        }
        public static string RequirementsListsPath
        {
            get
            {
                if (IsLinux)
                    return DataLocation.localApplicationData + @"/ProceduralObjects/RequirementsLists/";
                return DataLocation.localApplicationData + @"\ProceduralObjects\RequirementsLists\";
            }
        }
        public static string ExternalsConfigPath
        {
            get
            {
                if (IsLinux)
                    return DataLocation.localApplicationData + @"/ProceduralObjects/ExportedObjects/";
                return DataLocation.localApplicationData + @"\ProceduralObjects\ExportedObjects\";
            }
        }
        public static string OldExternalsConfigPath
        {
            get
            {
                if (IsLinux)
                    return DataLocation.localApplicationData + @"/ModConfig/SavedProceduralObjects/";
                return DataLocation.localApplicationData + @"\ModConfig\SavedProceduralObjects\";
            }
        }
        public static string[] WorkshopOrLocalFolders
        {
            get
            {
                var subItems = PlatformService.workshop.GetSubscribedItems();
                if (subItems.Length > 0)
                {
                    List<string> paths = new List<string>();
                    foreach (PublishedFileId fileId in subItems)
                    {
                        string path = PlatformService.workshop.GetSubscribedItemPath(fileId);
                        if (!Directory.Exists(path))
                            continue;
                        paths.Add(path);
                    }
                    return paths.ToArray();
                }
                else
                {
                    return (Directory.GetDirectories(DataLocation.addonsPath)
                        .Concat(Directory.GetDirectories(DataLocation.gameContentPath  + (ProceduralObjectsMod.IsLinux ? "/" : @"\") + "Mods")))
                        .ToArray();
                }
            }
        }
        public static void OpenURL(string url)
        {
            if (PlatformService.workshop.GetSubscribedItems().Length > 0)
                PlatformService.ActivateGameOverlayToWebPage(url);
            else
                Application.OpenURL(url);
        }
        public static bool IsLinux
        {
            get
            {
                return Application.platform == RuntimePlatform.LinuxEditor || Application.platform == RuntimePlatform.LinuxPlayer || Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer;
            }
        }

        public static GameObject gameLogicObject, editorHelperObject;
        public static Texture2D[] Icons = null, SelectionModeIcons = null;
        public static System.Random randomizer;

        public static List<POModuleType> ModuleTypes = new List<POModuleType>();

        public static ProceduralObjectContainer[] tempContainerData = null;
        public static Layer[] tempLayerData = null;

        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);

            var _initializeToolMan = ToolMan.Initialize();
            if (_initializeToolMan)
                Debug.Log("[ProceduralObjects] Successfully created the ProceduralTool");

            if (mode == LoadMode.NewGame || mode == LoadMode.LoadGame || mode == LoadMode.NewMap || mode == LoadMode.LoadMap)
            {
                if (gameLogicObject == null)
                {
                    if (!Directory.Exists(PODirectoryPath))
                        Directory.CreateDirectory(PODirectoryPath);

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
                            TextureUtils.LoadTextureFromAssembly("unlocked"),
                            TextureUtils.LoadTextureFromAssembly("painterPicker"),
                            TextureUtils.LoadTextureFromAssembly("painterSlider"),
                            TextureUtils.LoadTextureFromAssembly("picker"),
                            TextureUtils.LoadTextureFromAssembly("distance"),
                            TextureUtils.LoadTextureFromAssembly("angle"),
                            TextureUtils.LoadTextureFromAssembly("copy") };
                        SelectionModeIcons = new Texture2D[] { TextureUtils.LoadTextureFromAssembly("main_layers"),
                            TextureUtils.LoadTextureFromAssembly("main_exported"),
                            TextureUtils.LoadTextureFromAssembly("main_textures"),
                            TextureUtils.LoadTextureFromAssembly("main_fonts"),
                            TextureUtils.LoadTextureFromAssembly("main_modules"),
                            TextureUtils.LoadTextureFromAssembly("main_render"),
                            TextureUtils.LoadTextureFromAssembly("main_stats"),
                            TextureUtils.LoadTextureFromAssembly("main_measures") };
                    }
                    gameLogicObject = new GameObject("Logic_ProceduralObjects");
                    ProceduralObjectsLogic.instance = gameLogicObject.AddComponent<ProceduralObjectsLogic>();
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
                ProceduralObjectsLogic.instance = null;
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
            try {
                SettingsFile = new SettingsFile() { fileName = SETTINGS_FILENAME };
                GameSettings.AddSettingsFile(SettingsFile);
            }
            catch (Exception e) { Debug.LogError("[Procedural Objects] Failed to add the settings file :" + e); }
        }

        // Settings panel

        public static SavedFloat GlobalRDMultiplier = new SavedFloat("RDGlobalMult", SETTINGS_FILENAME, 1f, true);
        public static SavedBool UseDynamicRenderDist = new SavedBool("dynamicRenderDist", SETTINGS_FILENAME, true, true);
        public static SavedFloat DynamicRDMultiplier = new SavedFloat("dynRDmultiplier", SETTINGS_FILENAME, 70f, true);
        public static SavedFloat DynamicRDMinThreshold = new SavedFloat("dynRDminThreshold", SETTINGS_FILENAME, 250f, true);
        public static SavedFloat PropRenderDistance = new SavedFloat("propRenderDist", SETTINGS_FILENAME, 1400f, true);
        public static SavedFloat BuildingRenderDistance = new SavedFloat("buildingRenderDist", SETTINGS_FILENAME, 2000f, true);
        public static SavedFloat GizmoSize = new SavedFloat("gizmoSize", SETTINGS_FILENAME, 1.2f, true);
        public static SavedFloat GizmoOpacity = new SavedFloat("gizmoOpacity", SETTINGS_FILENAME, 1f, true);

        public static SavedInt DistanceUnits = new SavedInt("distanceUnits", SETTINGS_FILENAME, 0, true);
        public static SavedInt AngleUnits = new SavedInt("angleUnits", SETTINGS_FILENAME, 0, true);

        public static SavedBool UseUINightMode = new SavedBool("useNightMode", SETTINGS_FILENAME, false, true);
        public static SavedBool HideDisabledLayersIcon = new SavedBool("hideIconLayerHidden", SETTINGS_FILENAME, true, true);
        public static SavedBool UsePasteInto = new SavedBool("usePasteInto", SETTINGS_FILENAME, false, true);
        public static SavedBool AutoResizeDecals = new SavedBool("autoResizeDecals", SETTINGS_FILENAME, true, true);
        public static SavedBool IncludeSubBuildings = new SavedBool("includeSubBuildings", SETTINGS_FILENAME, true, true);
        public static SavedBool UseColorVariation = new SavedBool("useColorVar", SETTINGS_FILENAME, true, true);

        public static SavedInt ConfirmDeletionThreshold = new SavedInt("confirmDeletionPanelThreshold", SETTINGS_FILENAME, 2, true);
        public static SavedBool ShowConfirmDeletion = new SavedBool("showConfirmDeletion", SETTINGS_FILENAME, true, true);
        public static SavedString LanguageUsed = new SavedString("languageUsed", SETTINGS_FILENAME, "default", true);
        public static SavedBool ShowDeveloperTools = new SavedBool("showDevTools", SETTINGS_FILENAME, false, true);

        public static SavedBool ShowToolsControls = new SavedBool("showToolsControls", SETTINGS_FILENAME, true, true);

        private UISlider confirmDelThresholdSlider, gizmoSizeSlider, gizmoOpacitySlider;  // propRenderSlider, buildingRenderSlider 
        private UILabel confirmDelThresholdLabel, gizmoSizeLabel, gizmoOpacityLabel; // propRenderLabel, buildingRenderLabel
        private UICheckBox confirmDelCheckbox, showDevCheckbox, hideDisLayerIconCheckbox, useUINightModeCheckbox, autoResizeDecalsCheckbox, includeSubBuildingsCheckbox, useColorVariationCheckbox;
        private UIButton openKeybindingsButton;

        public void OnSettingsUI(UIHelperBase helper)
        {
            if (LocalizationManager.instance == null)
                LocalizationManager.CreateManager();
            SetUnits();

            ExtUITabstrip tabStrip = ExtUITabstrip.Create((UIHelper)helper);

            UIHelper gentab = tabStrip.AddTabPage("  " + LocalizationManager.instance.current["settings_GENERAL"] + "  ");
            UIHelperBase group = gentab.AddGroup("");
            UIHelper keybindingsTab = tabStrip.AddTabPage(" " + LocalizationManager.instance.current["settings_KB"] + " ");
            UIHelperBase kbGroup = keybindingsTab.AddGroup("");
           // UIHelperBase group = helper.AddGroup("    Procedural Objects");

            // KEY BINDINGS TAB

            ((UIPanel)((UIHelper)kbGroup).self).gameObject.AddComponent<OptionsKeymappingGeneral>();
            kbGroup.AddSpace(8);
            var KBPosGroup = kbGroup.AddGroup(LocalizationManager.instance.current["position"]);
            ((UIPanel)((UIHelper)KBPosGroup).self).gameObject.AddComponent<OptionsKeymappingPosition>();
            KBPosGroup.AddSpace(8);
            var KBRotGroup = kbGroup.AddGroup(LocalizationManager.instance.current["rotation"]);
            ((UIPanel)((UIHelper)KBRotGroup).self).gameObject.AddComponent<OptionsKeymappingRotation>();
            KBRotGroup.AddSpace(8);
            var KBScaleGroup = kbGroup.AddGroup(LocalizationManager.instance.current["scale_obj"]);
            ((UIPanel)((UIHelper)KBScaleGroup).self).gameObject.AddComponent<OptionsKeymappingScale>();
            KBScaleGroup.AddSpace(8);
            var KBSMActionsGroup = kbGroup.AddGroup(LocalizationManager.instance.current["CTActions"] + " (" + LocalizationManager.instance.current["selection_mode"] + ")");
            ((UIPanel)((UIHelper)KBSMActionsGroup).self).gameObject.AddComponent<OptionsKeymappingSelectionModeActions>();
            KBSMActionsGroup.AddSpace(8);
            openKeybindingsButton = (UIButton)kbGroup.AddButton(LocalizationManager.instance.current["open_kbd_cfg"], openKeybindings);

            KeyBindingsManager.Initialize();

            // GENERAL TAB
            UIPanel globalPanel = ((UIPanel)((UIHelper)group).self);

            gizmoSizeSlider = (UISlider)group.AddSlider(string.Format(LocalizationManager.instance.current["settings_GIZMO_label"], (GizmoSize.value * 100).ToString()), 0.2f, 3f, 0.1f, GizmoSize.value, gizmoSizeChanged);
            gizmoSizeSlider.width = 600;
            gizmoSizeSlider.height = 16;
            gizmoSizeSlider.tooltip = LocalizationManager.instance.current["settings_GIZMO_tooltip"];

            var gizmoSizePanel = globalPanel.Find<UIPanel>("OptionsSliderTemplate(Clone)");
            gizmoSizePanel.name = "GizmoSizePanel";
            gizmoSizeLabel = gizmoSizePanel.Find<UILabel>("Label");
            gizmoSizeLabel.width *= 3.5f;

            gizmoOpacitySlider = (UISlider)group.AddSlider(string.Format(LocalizationManager.instance.current["settings_GIZMO_OPACITY_label"], (GizmoOpacity.value * 100).ToString()), .1f, 1f, 0.05f, GizmoOpacity.value, gizmoOpacityChanged);
            gizmoOpacitySlider.width = 600;
            gizmoOpacitySlider.height = 16;

            var gizmoOpacityPanel = globalPanel.components.First(c => c.GetType() == typeof(UIPanel) && c != gizmoSizePanel);
            gizmoOpacityPanel.name = "GizmoOpacityPanel";
            gizmoOpacityLabel = gizmoOpacityPanel.Find<UILabel>("Label");
            gizmoOpacityLabel.width *= 3.5f;


            group.AddDropdown(LocalizationManager.instance.current["settings_DISTUNITS_label"],
                new string[] { LocalizationManager.instance.current["settings_DISTUNITS_m"] + " (m)", 
                    LocalizationManager.instance.current["settings_DISTUNITS_ft"] + " (ft)", 
                    LocalizationManager.instance.current["settings_DISTUNITS_yd"] + " (yd)" }, DistanceUnits.value, 
                    (int value) => { 
                        DistanceUnits.value = value; 
                        SetUnits();
                      // propRenderDistanceChanged(PropRenderDistance.value);
                      // buildingRenderDistanceChanged(BuildingRenderDistance.value);
                    });

            group.AddDropdown(LocalizationManager.instance.current["settings_ANGUNITS_label"],
                new string[] { LocalizationManager.instance.current["settings_ANGUNITS_deg"] + " (°)", 
                    LocalizationManager.instance.current["settings_ANGUNITS_rad"] + " (rad)" }, AngleUnits.value, (int value) => { AngleUnits.value = value; SetUnits(); });

            useUINightModeCheckbox = (UICheckBox)group.AddCheckbox(LocalizationManager.instance.current["settings_USEUINIGHTMODE_toggle"], UseUINightMode.value, (bool value) => { UseUINightMode.value = value; });
            
            hideDisLayerIconCheckbox = (UICheckBox)group.AddCheckbox(LocalizationManager.instance.current["settings_HIDEDISABLEDLAYERSICON_toggle"], HideDisabledLayersIcon.value, hideDisabledLayersIconChanged);

          // usePasteIntoCheckbox = (UICheckBox)group.AddCheckbox(LocalizationManager.instance.current["settings_USEPASTEINTO_toggle"], UsePasteInto.value, usePasteIntoChanged);

            autoResizeDecalsCheckbox = (UICheckBox)group.AddCheckbox(LocalizationManager.instance.current["settings_AUTORESIZEDECALS_toggle"], AutoResizeDecals.value, autoResizeDecalsChanged);

            includeSubBuildingsCheckbox = (UICheckBox)group.AddCheckbox(LocalizationManager.instance.current["settings_CONVERTSUBBUILDINGS_toggle"], IncludeSubBuildings.value, includeSubBuildingsChanged);

            useColorVariationCheckbox = (UICheckBox)group.AddCheckbox(LocalizationManager.instance.current["settings_USECOLORVAR_toggle"], UseColorVariation.value, useColorVariationChanged);

            group.AddSpace(10);
            var sliderGroup = gentab.AddGroup("  " + LocalizationManager.instance.current["settings_CONFDEL_title"]);

            confirmDelCheckbox = (UICheckBox)sliderGroup.AddCheckbox(LocalizationManager.instance.current["settings_CONFDEL_toggle"], ShowConfirmDeletion.value, confirmDeletionCheckboxChanged);

            confirmDelThresholdSlider = (UISlider)sliderGroup.AddSlider(string.Format(LocalizationManager.instance.current["settings_CONFDEL_SLIDER_label"], ConfirmDeletionThreshold.value.ToString()), 1f, 15f, 1f, ConfirmDeletionThreshold.value, confirmDeletionThresholdChanged);
            confirmDelThresholdSlider.width = 715;
            confirmDelThresholdSlider.height = 16;
            confirmDelThresholdSlider.tooltip = LocalizationManager.instance.current["settings_CONFDEL_SLIDER_tooltip"];

            confirmDelThresholdLabel = (UILabel)((UIPanel)((UIHelper)sliderGroup).self).components.First(c => c.GetType() == typeof(UIPanel) && c.components.Any(comp => comp.GetType() == typeof(UISlider)))
                .components.First(c => c.GetType() == typeof(UILabel));
            confirmDelThresholdLabel.width *= 3.5f;
            if (!ShowConfirmDeletion.value)
                confirmDelThresholdSlider.isEnabled = false;


            sliderGroup.AddSpace(10);
            var languageGroup = gentab.AddGroup("  " + LocalizationManager.instance.current["settings_LANG_title"]);
            languageGroup.AddDropdown(LocalizationManager.instance.current["settings_LANG_title"], LocalizationManager.instance.identifiers, LocalizationManager.instance.available.IndexOf(LocalizationManager.instance.current), languageChanged);

            languageGroup.AddSpace(10);

            showDevCheckbox = (UICheckBox)languageGroup.AddCheckbox(LocalizationManager.instance.current["settings_DEVTOOLS_toggle"], ShowDeveloperTools.value, (value) =>
            {
                ShowDeveloperTools.value = value;
            });
            showDevCheckbox.tooltip = LocalizationManager.instance.current["settings_DEVTOOLS_tooltip"];
        }
        /*
        private void propRenderDistanceChanged(float value)
        {
            PropRenderDistance.value = value;
            propRenderLabel.text = string.Format(LocalizationManager.instance.current["settings_RD_PROP_label"], Gizmos.ConvertRoundToDistanceUnit(value).ToString()) + distanceUnit;
        }
        private void buildingRenderDistanceChanged(float value)
        {
            BuildingRenderDistance.value = value;
            buildingRenderLabel.text = string.Format(LocalizationManager.instance.current["settings_RD_BUILDING_label"], Gizmos.ConvertRoundToDistanceUnit(value).ToString()) + distanceUnit;
        } */
        private void openKeybindings()
        {
            if (File.Exists(KeyBindingsManager.BindingsConfigPath))
                Application.OpenURL("file://" + KeyBindingsManager.BindingsConfigPath);
        }
        private void gizmoSizeChanged(float value)
        {
            GizmoSize.value = value;
            gizmoSizeLabel.text = string.Format(LocalizationManager.instance.current["settings_GIZMO_label"], (value * 100).ToString());
        }
        private void gizmoOpacityChanged(float value)
        {
            GizmoOpacity.value = value;
            gizmoOpacityLabel.text = string.Format(LocalizationManager.instance.current["settings_GIZMO_OPACITY_label"], (value * 100).ToString());
            Gizmos.GizmoRed = new Color(1f, 0f, 0f, value);
            Gizmos.GizmoBlue = new Color(0f, 0f, 1f, value);
            Gizmos.GizmoGreen = new Color(0f, 1f, 0f, value);
            Gizmos.GizmoYellow = new Color(1f, 0.92f, 0.016f, value);
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
        private void hideDisabledLayersIconChanged(bool value)
        {
            HideDisabledLayersIcon.value = value;
        }
        private void autoResizeDecalsChanged(bool value)
        {
            AutoResizeDecals.value = value;
        }
        private void includeSubBuildingsChanged(bool value)
        {
            IncludeSubBuildings.value = value;
        }
        private void useColorVariationChanged(bool value)
        {
            UseColorVariation.value = value;
        }
        private void usePasteIntoChanged(bool value)
        {
            UsePasteInto.value = value;
        }
        private void languageChanged(int value)
        {
            LocalizationManager.instance.SetCurrent(value);
            LanguageUsed.value = LocalizationManager.instance.current.identifier;
            openKeybindingsButton.text = LocalizationManager.instance.current["open_kbd_cfg"];
         // propRenderLabel.text = string.Format(LocalizationManager.instance.current["settings_RD_PROP_label"], Gizmos.ConvertRoundToDistanceUnit(PropRenderDistance.value).ToString()) + distanceUnit;
            confirmDelThresholdLabel.text = string.Format(LocalizationManager.instance.current["settings_CONFDEL_SLIDER_label"], ConfirmDeletionThreshold.value.ToString());
         // buildingRenderLabel.text = string.Format(LocalizationManager.instance.current["settings_RD_BUILDING_label"], Gizmos.ConvertRoundToDistanceUnit(BuildingRenderDistance.value).ToString()) + distanceUnit;
            confirmDelThresholdSlider.tooltip = LocalizationManager.instance.current["settings_CONFDEL_SLIDER_tooltip"];
         // buildingRenderSlider.tooltip = LocalizationManager.instance.current["settings_RD_BUILDING_tooltip"];
         // propRenderSlider.tooltip = LocalizationManager.instance.current["settings_RD_PROP_tooltip"];
            gizmoSizeLabel.text = string.Format(LocalizationManager.instance.current["settings_GIZMO_label"], (GizmoSize.value * 100).ToString());
            gizmoSizeSlider.tooltip = LocalizationManager.instance.current["settings_GIZMO_tooltip"];
            gizmoOpacityLabel.text = string.Format(LocalizationManager.instance.current["settings_GIZMO_OPACITY_label"], (GizmoOpacity.value * 100).ToString());
            confirmDelCheckbox.text = LocalizationManager.instance.current["settings_CONFDEL_toggle"];
            showDevCheckbox.text = LocalizationManager.instance.current["settings_DEVTOOLS_toggle"];
            showDevCheckbox.tooltip = LocalizationManager.instance.current["settings_DEVTOOLS_tooltip"];
            hideDisLayerIconCheckbox.text = LocalizationManager.instance.current["settings_HIDEDISABLEDLAYERSICON_toggle"];
            useUINightModeCheckbox.text = LocalizationManager.instance.current["settings_USEUINIGHTMODE_toggle"];
            autoResizeDecalsCheckbox.text = LocalizationManager.instance.current["settings_AUTORESIZEDECALS_toggle"];
            includeSubBuildingsCheckbox.text = LocalizationManager.instance.current["settings_CONVERTSUBBUILDINGS_toggle"];
            useColorVariationCheckbox.text = LocalizationManager.instance.current["settings_USECOLORVAR_toggle"];
         // usePasteIntoCheckbox.text = LocalizationManager.instance.current["settings_USEPASTEINTO_toggle"];
            if (ProceduralObjectsLogic.instance != null)
                ProceduralObjectsLogic.instance.SetupLocalizationInternally();
        }

        public static string distanceUnit, angleUnit;

        private void SetUnits()
        {
            switch (DistanceUnits.value)
            {
                case 0:
                    distanceUnit = " m";
                    break;
                case 1:
                    distanceUnit = " ft";
                    break;
                case 2:
                    distanceUnit = " yd";
                    break;
            }
            switch (AngleUnits.value)
            {
                case 0:
                    angleUnit = "°";
                    break;
                case 1:
                    angleUnit = " rad";
                    break;
            }
        }
    }
}
