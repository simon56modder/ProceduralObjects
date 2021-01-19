using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ColossalFramework.IO;
using UnityEngine;

using ProceduralObjects.Localization;
using ProceduralObjects.UI;

namespace ProceduralObjects.Classes
{
    public class UpdateInformant : MonoBehaviour
    {
        public string CachePath = DataLocation.localApplicationData + (ProceduralObjectsMod.IsLinux ? "/CACHE_proceduralObjects.update" : "\\CACHE_proceduralObjects.update");

        private string OldVersion = "";
        private bool AllowedToShow = true;
        private Rect uiRect = new Rect(130, 130, 600, 350);
        private int currentShowingChangelogIndex = 0;

        public static string[] Changelog;

        void Start()
        {
            if (File.Exists(CachePath))
            {
                try
                {
                    var lines = File.ReadAllLines(CachePath);
                    OldVersion = lines.FirstOrDefault(line => line.Contains("VERSION ")).Replace("VERSION ", "");
                    if (OldVersion == ProceduralObjectsMod.VERSION)
                    {
                        AllowedToShow = false;
                    }
                }
                catch
                {
                    Debug.LogError("[ProceduralObjects] Update Informant found a cache file but was unable to read it !");
                }
                File.Delete(CachePath);
            }
            TextWriter tw = new StreamWriter(CachePath);
            tw.WriteLine("VERSION " + ProceduralObjectsMod.VERSION);
            tw.Close();

            Changelog = new string[] {
                "1.7 changelog :\n\n■ Added PO Groups\n■ Added PO Modules (external mods that add behaviours to POs)\n■ New Customization Tool actions : 'Conform to Terrain' and 'conform to networks...'\n■ Position fields moved from Adv. Edition tools to General Tool, new Rotation fields\n■ Rendering improvement & Thumbnail/High-res screenshot issue fix thanks to @krzychu124\n■ 'Paste into Selection' turned into an option, disabled by default.\n\nCheckout the full changelog on the wiki",
                "1.6.3 changelog :\n\n■ Edit mode UI revamp\n■ New Customization Tool actions\n■ Type in values for distances, angles, stretch factors using the keyboard\n   in General Tool with Gizmos, or in Customization Tool with the Mouse tools while holding the movements, then press Enter.\n   (configurable units in the settings)\n■ New Font Management window\n■ Chinese translation by SteinsGateSG\n\nCheckout the full changelog on the wiki",
                "1.6.2 changelog :\n\n■ Selection mode improvements\n■ PO now available in the Map Editor !\n■ Use Ctrl while moving an object with the General Tool position gizmo to duplicate an object\n■ Customization Tool improvements (New snap to axis feature for mouse movements, new vertex look...)\n■ Text Customization & Layers improvements\n■ Various fixes\n\nCheckout the full changelog on the wiki",
                "1.6.1 changelog :\n\n■ Painter for POs\n■ Huge copy/paste speed increase thanks to Quboid\n■ Controls improvements\n   New gizmos, world/local gizmo settings, keyboard moves finetuning\n■ New Advanced Editions tools specific features\n■ Move To Tool improvements\n■ Ability to Ctrl+V objects into others to replace them\n■ Various fixes, including Ploppable Surfaces vertices edition issue\n\nCheckout the full changelog on the wiki",
                "1.6 changelog :\n\n■Added Undo (Ctrl+Z) and Redo (Ctrl+Y) in Edit mode\n■Added the Advanced Edition tools window accessible from the General Tool\n■Text Customization improvements : rotation, sorting and color rectangles\n■Added Customization Tool modes : Position, Rotation, Scale, Flatten (right click to open the dropdown menu)\n■Added \"Confirm Deletion\" and \"Not PO compatible\" popups\n■Adaptive and configurable Gizmo size\n■New tools for font creation\n■Optimization of UI rendering and copy/paste, added sounds & effects\n■Revamped the settings\n<b>See the full changelog on the wiki</b>",
                "1.5.5 changelog :\n\n■ Added 'Align Heights' : click one or more Objects, click Align Heights, and select an object on which the others will have to align\n■ Rotation can be done on all axis in the 'Move To' mode with the Arrow keys",
                "1.5.4 changelog :\n\n■ Added the Layers system : show/hide parts of your builds/city through a system of layers (virtually no limit to the number of layers)\n■ Attempt to fix the Detail nature mod bug.\n■ Ability to Move entire selections without the need of copy/paste.\n■ Using 'Move To' doesn't make you enter Edit mode.\n■ Changed the order of edition buttons for better workflow.",
                "1.5.3 changelog :\n\n■ Added the Characters Table : click on the font name while editing a text field to make it appear.\n■ Russian translation now available thanks to Vitalii201",
                "1.5.2 changelog :\n\n■ Fonts support custom Unicode characters\n■ Fixed : objects that previously had text no longer disappear (if the save hasn't been overwritten)\n■ With the 'Move To' tool, a single Right Click will rotate by 45° increments.",
                "1.5.1 changelog :\n\n■ Added marquee selection for multiple Procedural Objects\n■ Fixed the text color saving bug\n■ Fixed the loading failure bug\n■ Fixed the zoom in/out bug.",
                "1.5 changelog :\n\n■ Added Text Customization\n   ■ Ability to add text on any procedural object, including accents, diacritics and symbols\n   ■ Fine text field settings : font, size, spacing, color, X and Y stretch scale, bold/italic/normal style\n   ■ In-game application of text with direct 3D and texture preview\n   ■ Ability to create custom fonts and share them on the workshop\n■ Added the \"Texture UV mode\" for objects for objects that handle UV recalculation (cube and square only)\n   ■ Stretch mode : no UV recalculation, the texture will stretch as for any object\n   ■ Repeat mode : size-based UV recalculation, the texture will repeat itself on the model\n■ Objects that previously required a custom texture can now take their original texture\n■ Vertex selection points hidden when moving them\n■ Reduced brightness of the mouse hover selection highlighting\n■ \"Vertex Customization\" tool name changed to \"Customization\"\n■ Bug fixed : Mouse scrolling zoom through menu selections\n■ Bug fixed : Shift-Scale issue",
                "1.4.5 changelog :\n\n■ Fixed the \"Too long array\" issue\n■ Fixed texture stretching, now they tile correctly\n■ Prepared Fonts loading, Custom Text Properties and Sign Creation windows for future updates",
                "1.4.4 changelog :\n\n■ Entire selections of objects can now be saved and shared by clicking \"Export Selection\" button, they later can be found in the \"Saved Procedural Objects\" window\n■ Spanish translation now available, thanks Armesto !\n■ Saved Procedural Objects can now be renamed\n■ External textures names fixed",
                "1.4.3 changelog :\n\n■ Objects selection improved : the objects are now highlighted when hovering the object button.\n■ Vertices can now me moved with the Left Mouse button.\n■ Added \"Show : Always/Day time only/Night time only\" parameter in the General tool.\n■ Ploppable Asphalt converted to Procedural Objects now have the proper color set in the mod settings.\n■ Fixed Ploppable Grass and Cliff vertices positions",
                "1.4.2 changelog :\n\n■ Render distances default values can be tweaked in the game settings.\n■ Fully translated the controls indications, other minor translations fixes\n■ Fixed the following issues : marquee selection problem, window/gizmo click-through, translation formatting, vertices showing when they shouldn't",
                "1.4.1-2 changelog :\n\n■ Subsequent issues to a main bug fixed",
                "1.4.1 changelog :\n\n■ Major issue fix : Now compatible again with Dynamic Resolution",
                "1.4 changelog :\n\n■ Group Selection ! Allows to copy/paste, delete or move entire objects selections.\n■ Completely redone the rendering system\n■ Translations now available !",
                "1.3.1 changelog :\n\n■ Major issue 2018A3 (reported by Sparks44) fixed",
                "1.3 changelog :\n\n■ You can now save Procedural Objects to externals save files to reuse them or share them later, even through the workshop!\n■ Changed the Main Mod Button to a more fancy and moveable one (it's also automatically hidden with Cinematic Camera)\n■ You can rotate objects by dragging the Right Mouse Button using the Move To tool or when placing them.\n■ Added a Total Procedural objects counter\n■ Issues fixes.",
                "1.2.2.2 changelog :\n\n■ Texture selection is sorted and easier (still room for improvements)\n■ When copying an object, its height is stored and when it's pasted, hold the Right Mouse Button to snap it to the stored height.",
                "1.2.2.1 changelog :\n\n■ Linux issue 2018A1 fixed",
                "1.2.2.0 changelog :\n\n■ All the Mod Keybindings can now be configured !\n■ Code improvements : the mod now runs faster",
                "1.2.1.1 changelog :\n\n■ Texture Packs can now have names\n■ Minor GUI Improvements",
                "1.2.1.0 changelog :\n\n■ Texture Packs ! Textures can now be shared via the workshop\n■ The textures list can now be reloaded directly from the texture selection menu.\n■ Fixed the 'click through' issue with the 'Move To' button\n■ 'Tab' shortcut enhanced\n■ Bug fixes as always."
            };
        }
        void OnGUI()
        {
            if (AllowedToShow)
            {
                if (LocalizationManager.instance == null)
                    return;
                if (LocalizationManager.instance.current == null)
                    return;
                uiRect = GUIUtils.Window(this.GetInstanceID(), uiRect, DrawUpdateUI, "Procedural Objects - " + LocalizationManager.instance.current["installed_version"] + " : " + ProceduralObjectsMod.VERSION);
            }
        }
        void DrawUpdateUI(int id)
        {
            GUI.DragWindow(new Rect(0, 0, 575, 22));
            GUIUtils.HelpButton(600, "Changelog");

            GUI.Label(new Rect(10, 25, 580, 275), LocalizationManager.instance.current["version"] + " " + Changelog[currentShowingChangelogIndex]);

            if (currentShowingChangelogIndex != 0)
            {
                if (GUI.Button(new Rect(125, 310, 110, 30), LocalizationManager.instance.current["next_version"]))
                    currentShowingChangelogIndex -= 1;
            }
            if (currentShowingChangelogIndex != (Changelog.Length - 1))
            {
                if (GUI.Button(new Rect(5, 310, 120, 30), LocalizationManager.instance.current["prev_version"]))
                    currentShowingChangelogIndex += 1;
            }

            if (GUI.Button(new Rect(235, 310, 215, 30), LocalizationManager.instance.current["ok"]))
                AllowedToShow = false;
        }
    }
}
