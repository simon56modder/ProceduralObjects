using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ColossalFramework.IO;
using UnityEngine;

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
                var lines = File.ReadAllLines(CachePath);
                OldVersion = lines.FirstOrDefault(line => line.Contains("VERSION ")).Replace("VERSION ", "");
                if (OldVersion == ProceduralObjectsMod.VERSION)
                {
                    AllowedToShow = false;
                }
                File.Delete(CachePath);
            }
            TextWriter tw = new StreamWriter(CachePath);
            tw.WriteLine("VERSION " + ProceduralObjectsMod.VERSION);
            tw.Close();

            Changelog = new string[] {
                "1.3 changelog :\n\n■ You can now save Procedural Objects to externals save files to reuse them or share them later, even through the workshop!\n■ Changed the Main Mod Button to a more fancy and moveable one (it's also automatically hidden with Cinematic Camera)\n■ You can rotate objects by dragging the Right Mouse Button using the Move To tool or when placing them.\n■ Added a Total Procedural objects counter\n■ Issues fixes.",
                "1.2.2.2 changelog :\n\n■ Texture selection is sorted and easier (still room for improvements)\n■ When copying an object, its height is stored and when it's pasted, hold the Right Mouse Button to snap it to the stored height.",
                "1.2.2.1 changelog :\n\n■ Linux issue 2018A1 fixed",
                "1.2.2.0 changelog :\n\n■ All the Mod Keybindings can now be configured !\n■ Code improvements : the mod now runs faster",
                "1.2.1.1 changelog :\n\n■ Texture Packs can now have names\n■ Minor GUI Improvements",
                "1.2.1.0 changelog :\n\n■ Texture Packs ! Textures can now be shared via the workshop, more information in the mod Steam description soon.\n■The textures list can now be reloaded directly from the texture selection menu.\n■Fixed the 'click through' issue with the 'Move To' button\n■'Tab' shortcut enhanced\n■Bug fixes as always."
            };
        }
        void OnGUI()
        {
            if (AllowedToShow)
            {
                uiRect = GUI.Window(this.GetInstanceID(), uiRect, DrawUpdateUI, "Procedural Objects - Installed version : " + ProceduralObjectsMod.VERSION);
            }
        }
        void DrawUpdateUI(int id)
        {
            GUI.DragWindow(new Rect(0, 0, 600, 36));

            GUI.Label(new Rect(10, 40, 580, 260), "Version "
                + Changelog[currentShowingChangelogIndex]);

            if (currentShowingChangelogIndex != 0)
            {
                if (GUI.Button(new Rect(125, 310, 85, 30), "Next Version"))
                    currentShowingChangelogIndex -= 1;
            }
            if (currentShowingChangelogIndex != (Changelog.Count() - 1))
            {
                if (GUI.Button(new Rect(5, 310, 120, 30), "Previous Version"))
                    currentShowingChangelogIndex += 1;
            }

            if (GUI.Button(new Rect(225, 310, 215, 30), "OK"))
                AllowedToShow = false;
        }
    }
}
