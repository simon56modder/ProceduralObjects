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
                "1.7.6 changelog :\n\n■ Revert of the saving process changes back to pre-1.7.5. Changes made in between updates should not be lost, unless you have saved a game with a buggy PO load.\n■ Ploppable asphalt reading fix\n■ Text Customization improvements\n■ Key bindings in settings panel\n\nCheckout the full changelog on the wiki",
                "1.7.5 changelog :\n\n■ New 'Measurements' tab in Selection mode for distances and angles\n■ New 'Draw mouse tool' in Customization tool for all Circle Ploppable surfaces\n■ Efficiency improvements : data compression (lighter save files), other improvements\n■ 'Replace by copy' now works with selections\n■ 'Border' options for Color Rectangles in Text Customization\n■ Ability to copy/paste text fields between different POs\n■ Ability to make PO exports substitute for PO conversion of a given prop or building\n■ Picker filter now allows for multiple types of objects\n■ Fixes & tweaks \n\nCheckout the full changelog on the wiki",
                "1.7.4 changelog :\n\n■ Sub-buildings can be converted in PO. They automatically group with the main model.\n■ Procedural Objects compatible with other platforms than Steam.\n■ Ability to select texture after placement for all POs\n■ All axis available with the mouse for vertex movements\n■ New \"Randomize Rotation\" tool\n■ New Gizmos opacity setting in the settings panel\n■ Fixes & tweaks \n\nCheckout the full changelog on the wiki",
                "1.7.3 changelog :\n\n■ Performance enhancements, thanks to krzychu\n■ Group issues fix\n■ Dutch translation by Murie\n\nCheckout the full changelog on the wiki",
                "1.7.2 changelog :\n\n■ Selection mode UI revamp, new Filters and new Picker, performance enhancements.\n■ More Selection mode actions : Align rotations, align between 2, equal slope, set render distance, color gradient\n■ Align vertices action in Customization tool\n■ Escape key useable in PO, fixed scrolling through interface issue\n■ Gizmo size and UI improvement\n■ Exported objects optimization, new 'Fixed' type of export for static imports across saves\n■ Fixes and tweaks\n\nCheckout the full changelog on the wiki",
                "1.7.1 changelog :\n\n■ Line Copy in General tool\n    When using the position gizmo, hold Ctrl to enable copy, select the spacing, then press Shift to use the Line copy tool. Use arrow keys and PageUp/PageDown to change rotation. Release the click to confirm placement.\n■ New Render Options window with Dynamic Render distance\n     RECOMMENDED: Go to Render Options > Calculation : set to Dynamic, and click Recalculate all.\n■ Text customization improvements, RAM overuse fix\n■ Fixes and optimizations\n\nCheckout the full changelog on the wiki",
                "1.7 changelog :\n\n■ Added PO Groups\n■ Added PO Modules (external mods that add behaviours to POs)\n■ New Customization Tool actions : 'Conform to Terrain' and 'conform to networks...'\n■ Position fields moved from Adv. Edition tools to General Tool, new Rotation fields\n■ Rendering improvement & Thumbnail/High-res screenshot issue fix thanks to @krzychu124\n■ 'Paste into Selection' turned into an option, disabled by default.\n\nCheckout the full changelog on the wiki",
                "1.6.3 changelog :\n\n■ Edit mode UI revamp\n■ New Customization Tool actions\n■ Type in values for distances, angles, stretch factors using the keyboard\n   in General Tool with Gizmos, or in Customization Tool with the Mouse tools while holding the movements, then press Enter.\n   (configurable units in the settings)\n■ New Font Management window\n■ Chinese translation by SteinsGateSG\n\nCheckout the full changelog on the wiki",
                "1.6.2 changelog :\n\n■ Selection mode improvements\n■ PO now available in the Map Editor !\n■ Use Ctrl while moving an object with the General Tool position gizmo to duplicate an object\n■ Customization Tool improvements (New snap to axis feature for mouse movements, new vertex look...)\n■ Text Customization & Layers improvements\n■ Various fixes\n\nCheckout the full changelog on the wiki",
                "1.6.1 changelog :\n\n■ Painter for POs\n■ Huge copy/paste speed increase thanks to Quboid\n■ Controls improvements\n   New gizmos, world/local gizmo settings, keyboard moves finetuning\n■ New Advanced Editions tools specific features\n■ Move To Tool improvements\n■ Ability to Ctrl+V objects into others to replace them\n■ Various fixes, including Ploppable Surfaces vertices edition issue\n\nCheckout the full changelog on the wiki",
                "1.6 changelog :\n\n■ Added Undo (Ctrl+Z) and Redo (Ctrl+Y) in Edit mode\n■ Added the Advanced Edition tools window accessible from the General Tool\n■ Text Customization improvements : rotation, sorting and color rectangles\n■ Added Customization Tool modes : Position, Rotation, Scale, Flatten (r-click to open the dropdown menu)\n■ Added \"Confirm Deletion\" and \"Not PO compatible\" popups\n■ Adaptive and configurable Gizmo size\n■ New tools for font creation\n■ Optimization of UI rendering and copy/paste, added sounds & effects\n■ Revamped the settings\n<b>See the full changelog on the wiki</b>"
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
