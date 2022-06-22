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
    public class PopupStart : MonoBehaviour
    {
        public string CachePath = DataLocation.localApplicationData + (ProceduralObjectsMod.IsLinux ? "/CACHE_proceduralObjects.update" : "\\CACHE_proceduralObjects.update");

        private string OldVersion = "";
        public static bool AllowedToShow = true;
        private Rect uiRect = new Rect(130, 130, 600, 350);
        private int currentShowingChangelogIndex = 0, currentFailuresIndex = 0;
        private static bool displayChangelog, displayLoadingFailures;

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
                        if (!displayLoadingFailures)
                            AllowedToShow = false;
                    }
                    else
                        displayChangelog = true;
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
                "1.7.8 changelog :\n\n■ New Popup for POs that failed to load (keep in memory/discard choice)\n■ Fixed Page Up / Page Down issue in Move To\n■ 3D axis movement : Movement value (distance, angle..) are now displayed ; Use Alt for slow movements\n■ Added Undo/Redo in Distort tool\n■ Fixed PO-decals render distance compatiblity with ULOD mod",
                "1.7.7 changelog :\n\n■ New Selection mode actions : Distort, Project, in the \"more...\" dropdown menu. Works on selections of POs\n■ New options in Advanced Edition tools (cast shadows, reset 3D model)\n■ New Customization Tool actions (invert seleciton, split vertex)\n■ Minor tweaks (infinite render distance possible, draw tool improvements, etc.) & fixes\n\nCheckout the full changelog on the wiki",
                "1.7.6 changelog :\n\n■ Revert of the saving process changes back to pre-1.7.5. Changes made in between updates should not be lost, unless you have saved a game with a buggy PO load.\n■ Ploppable asphalt reading fix\n■ Text Customization improvements\n■ Key bindings in settings panel\n\nCheckout the full changelog on the wiki",
                "1.7.5 changelog :\n\n■ New 'Measurements' tab in Selection mode for distances and angles\n■ New 'Draw mouse tool' in Customization tool for all Circle Ploppable surfaces\n■ Efficiency improvements : data compression (lighter save files), other improvements\n■ 'Replace by copy' now works with selections\n■ 'Border' options for Color Rectangles in Text Customization\n■ Ability to copy/paste text fields between different POs\n■ Ability to make PO exports substitute for PO conversion of a given prop or building\n■ Picker filter now allows for multiple types of objects\n■ Fixes & tweaks \n\nCheckout the full changelog on the wiki",
                "1.7.4 changelog :\n\n■ Sub-buildings can be converted in PO. They automatically group with the main model.\n■ Procedural Objects compatible with other platforms than Steam.\n■ Ability to select texture after placement for all POs\n■ All axis available with the mouse for vertex movements\n■ New \"Randomize Rotation\" tool\n■ New Gizmos opacity setting in the settings panel\n■ Fixes & tweaks \n\nCheckout the full changelog on the wiki",
                "1.7.3 changelog :\n\n■ Performance enhancements, thanks to krzychu\n■ Group issues fix\n■ Dutch translation by Murie\n\nCheckout the full changelog on the wiki",
                "1.7.2 changelog :\n\n■ Selection mode UI revamp, new Filters and new Picker, performance enhancements.\n■ More Selection mode actions : Align rotations, align between 2, equal slope, set render distance, color gradient\n■ Align vertices action in Customization tool\n■ Escape key useable in PO, fixed scrolling through interface issue\n■ Gizmo size and UI improvement\n■ Exported objects optimization, new 'Fixed' type of export for static imports across saves\n■ Fixes and tweaks\n\nCheckout the full changelog on the wiki",
                "1.7.1 changelog :\n\n■ Line Copy in General tool\n    When using the position gizmo, hold Ctrl to enable copy, select the spacing, then press Shift to use the Line copy tool. Use arrow keys and PageUp/PageDown to change rotation. Release the click to confirm placement.\n■ New Render Options window with Dynamic Render distance\n     RECOMMENDED: Go to Render Options > Calculation : set to Dynamic, and click Recalculate all.\n■ Text customization improvements, RAM overuse fix\n■ Fixes and optimizations\n\nCheckout the full changelog on the wiki",
                "1.7 changelog :\n\n■ Added PO Groups\n■ Added PO Modules (external mods that add behaviours to POs)\n■ New Customization Tool actions : 'Conform to Terrain' and 'conform to networks...'\n■ Position fields moved from Adv. Edition tools to General Tool, new Rotation fields\n■ Rendering improvement & Thumbnail/High-res screenshot issue fix thanks to @krzychu124\n■ 'Paste into Selection' turned into an option, disabled by default.\n\nCheckout the full changelog on the wiki"
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
                uiRect = GUIUtils.ClampRectToScreen(GUIUtils.Window(this.GetInstanceID(), uiRect, DrawUpdateUI,
                    "Procedural Objects - " + LocalizationManager.instance.current["installed_version"] + " : " + ProceduralObjectsMod.VERSION));
            }
        }
        void DrawUpdateUI(int id)
        {
            GUI.DragWindow(new Rect(0, 0, 575, 22));
            if (displayChangelog)
            {
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
                {
                    ProceduralObjectsLogic.PlaySound();
                    if (displayLoadingFailures) displayChangelog = false;
                    else AllowedToShow = false;
                }
            }
            else if (displayLoadingFailures)
            {
                GUI.Label(new Rect(10, 23, 580, 25), LocalizationManager.instance.current["stats_failed"]);
                GUIUtils.DrawSeparator(new Vector2(10, 47), 580);

                var f = loading_failures[currentFailuresIndex];
                if (f.DrawUI(new Rect(10, 50, 580, 250)))
                {
                    if (f.sameForAllToggle)
                    {
                        for (int i = currentFailuresIndex; i < loading_failures.Count; i++)
                        {
                            loading_failures[i].keep = f.keep;
                        }
                        AllowedToShow = false;
                        return;
                    }
                    if (currentFailuresIndex == loading_failures.Count - 1)
                    {
                        AllowedToShow = false;
                    }
                    else
                        currentFailuresIndex += 1;
                }
                /*
                if (GUI.Button(new Rect(235, 310, 215, 30), LocalizationManager.instance.current["ok"]))
                    AllowedToShow = false; */
            }
        }

        public static List<POLoadingFailureGroup> loading_failures = new List<POLoadingFailureGroup>();
        public static void RegisterFailure(ProceduralObjectContainer container, Exception e, PropInfo[] props, BuildingInfo[] buildings)
        {
            string missingAsset = "";
            if (container != null)
            {
                if (container.basePrefabName != "" && container.basePrefabName != string.Empty && container.objectType != "" && container.objectType != string.Empty)
                {
                    if (container.objectType == "PROP")
                    {
                        if (!props.Any(p => p.name == container.basePrefabName))
                            missingAsset = container.basePrefabName;
                    }
                    else if (container.objectType == "BUILDING")
                    {
                        if (!buildings.Any(b => b.name == container.basePrefabName))
                            missingAsset = container.basePrefabName;
                    }
                }
            }
            POLoadingFailureGroup group = null;
            if (missingAsset != "")
            {
                foreach (var f in loading_failures)
                {
                    if (f.missing_asset == missingAsset)
                    {
                        group = f;
                        group.containers.Add(container, e);
                        return;
                    }
                }
            }
            if (group == null)
            {
                group = new POLoadingFailureGroup(missingAsset);
                group.containers.Add(container, e);
                loading_failures.Add(group);
            }
        }
        public static void LoadingDoneShowPopup()
        {
            if (loading_failures == null) return;
            if (loading_failures.Count == 0) return;
            AllowedToShow = true;
            displayLoadingFailures = true;
        }

        public static bool IsPopupOpen()
        {
            return AllowedToShow == true;
        }

        public class POLoadingFailureGroup
        {
            public POLoadingFailureGroup(string missing_asset)
            {
                containers = new Dictionary<ProceduralObjectContainer, Exception>();
                this.missing_asset = missing_asset;
            }

            public Dictionary<ProceduralObjectContainer, Exception> containers;
            public string missing_asset;
            public bool keep;
            public bool sameForAllToggle;

            public bool DrawUI(Rect rect)
            {
                GUI.BeginGroup(rect);
                string reason = (missing_asset == "") ? string.Format(LocalizationManager.instance.current["failed_error"], RetrieveId()) : 
                    string.Format(LocalizationManager.instance.current["failed_missing"], missing_asset, containers.Count);
                GUI.Label(new Rect(0, 0, rect.width, 25), reason);
                GUI.TextArea(new Rect(0, 28, rect.width, 157), containers.Values.ToArray()[0].ToString());
                sameForAllToggle = GUI.Toggle(new Rect(0, 190, 500, 25), sameForAllToggle, LocalizationManager.instance.current["failed_sameForAll"]);
                if (GUI.Button(new Rect(140, 220, 145, 25), LocalizationManager.instance.current["failed_keep"]))
                {
                    keep = true;
                    KeepMemory();
                    return true;
                }
                if (GUI.Button(new Rect(295, 220, 145, 25), LocalizationManager.instance.current["failed_discard"]))
                {
                    keep = false;
                    return true;
                }
                GUI.EndGroup();
                return false;
            }
            public void KeepMemory()
            {
                foreach (var c in containers.Keys)
                {
                    if (c == null) continue;
                    ProceduralObjectsLogic.instance.activeIds.Add(c.id);
                }
            }
            public string RetrieveId()
            {
                try
                {
                    var c = containers.Keys.ToList()[0];
                    if (c == null) return "???";
                    return c.id.ToString();
                }
                catch { return "???"; }
            }
        }
    }
}
