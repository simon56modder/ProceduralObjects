using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

using ProceduralObjects.Localization;
using ProceduralObjects.UI;

namespace ProceduralObjects.Classes
{
    public class POStatisticsManager
    {
        public POStatisticsManager(ProceduralObjectsLogic logic)
        {
            instance = this;
            this.logic = logic;
            RefreshCounters();
            window = new Rect(555, 200, 300, 235);
        }

        public static POStatisticsManager instance;

        public ProceduralObjectsLogic logic;
        public uint counter_POs, counter_layers, counter_ConvProps, counter_ConvBuildings, counter_Decals, counter_PSrfs, counter_PA, counter_customModels;
        public bool showWindow = false;
        public Rect window;
        
        public void DrawWindow()
        {
            if (showWindow)
                window = GUIUtils.ClampRectToScreen(GUIUtils.Window(99308274, window, draw, LocalizationManager.instance.current["stats"]));
        }
        private void draw(int id)
        {
            if (GUIUtils.CloseHelpButtons(window, "Statistics"))
                showWindow = false;
            if (GUI.Button(new Rect(222, 3, 23, 22), ProceduralObjectsMod.Icons[5]))
            {
                ProceduralObjectsLogic.PlaySound();
                RefreshCounters();
            }
            GUI.DragWindow(new Rect(0, 0, 223, 22));

            GUI.Label(new Rect(5, 27, 290, 200), LocalizationManager.instance.current["stats_total"] + "\n" + LocalizationManager.instance.current["stats_groups"] + "\n" + LocalizationManager.instance.current["stats_customModel"] + "\n" + LocalizationManager.instance.current["stats_failed"] + "\n" + LocalizationManager.instance.current["stats_loadingTime"] + "\n\n" + LocalizationManager.instance.current["stats_convProps"] + "\n    " + LocalizationManager.instance.current["stats_convProps_decals"] 
                + "\n    " + LocalizationManager.instance.current["stats_convProps_pSrf"]
                + "\n    " + LocalizationManager.instance.current["stats_convProps_pAsph"]
                + "\n" + LocalizationManager.instance.current["stats_convBuildings"]
                + "\n\n" + LocalizationManager.instance.current["stats_layers"]);
            GUI.skin.label.alignment = TextAnchor.UpperRight;
            try { GUI.Label(new Rect(5, 27, 290, 200), counter_POs + "\n" + logic.groups.Count + "\n" + counter_customModels + "\n" + logic.failedToLoadObjects + "\n"  + logic.loadingTime + " s\n\n" + counter_ConvProps + "\n" + counter_Decals + "\n" + counter_PSrfs + "\n" + counter_PA + "\n" + counter_ConvBuildings + "\n\n" + counter_layers); }
            catch { }
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
        }

        public void SetPosition(float x, float y)
        {
            window.position = new Vector2(x, y);
        }

        public void RefreshCounters()
        {
            counter_POs = (uint)logic.proceduralObjects.Count;
            counter_layers = (uint)logic.layerManager.m_layers.Count;
            counter_ConvProps = 0;
            counter_ConvBuildings = 0;
            counter_Decals = 0;
            counter_PSrfs = 0;
            counter_PA = 0;
            counter_customModels = 0;
            for (int i = 0; i < counter_POs; i++)
            {
                if (logic.proceduralObjects[i].baseInfoType == "PROP")
                {
                    counter_ConvProps += 1;
                    if (logic.proceduralObjects[i].meshStatus != 1)
                        counter_customModels += 1;
                    if (logic.proceduralObjects[i]._baseProp.m_isDecal)
                        counter_Decals += 1;
                    if (logic.proceduralObjects[i]._baseProp.IsPloppableAsphalt())
                        counter_PSrfs += 1;
                    if (logic.proceduralObjects[i]._baseProp.m_mesh.name == "ploppableasphalt-prop" || logic.proceduralObjects[i]._baseProp.m_mesh.name == "ploppableasphalt-decal")
                        counter_PA += 1;
                }
                else if (logic.proceduralObjects[i].baseInfoType == "BUILDING")
                {
                    counter_ConvBuildings += 1;
                    counter_customModels += 1;
                }
            }
        }
    }
}
