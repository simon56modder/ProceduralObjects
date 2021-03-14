using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProceduralObjects.Classes;
using ProceduralObjects.Localization;
using UnityEngine;

namespace ProceduralObjects.SelectionMode
{
    public static class SelectionModeTabDrawer
    {
        public static List<SelectionModeTab> tabs;

        public static float DrawTabsGetHeight(Rect rect, float spacing = 5f)
        {
            GUI.BeginGroup(rect);
            float tabCount = tabs.Count;
            float size = (rect.width - spacing * (tabCount - 1f)) / tabCount;
            for (int i = 0; i < tabCount; i++)
            {
                tabs[i].DrawButton(new Rect(i * (size + spacing), 0, size, size));
            }
            if (GUI.tooltip.Contains("[POSM]"))
            {
                GUI.Label(new Rect(0, size + 2, rect.width, 23), "<size=15>" + LocalizationManager.instance.current[GUI.tooltip.Replace("[POSM]", "")] + "</size>");
            }
            GUI.EndGroup();
            return size + 24;
        }
    }
    public class SelectionModeTab
    {
        public SelectionModeTab(string localization_id, int atlas, Action onClick)
        {
            LocalizationId = localization_id;
            Icon = ProceduralObjectsMod.SelectionModeIcons[atlas];
            this.onClick = onClick;
        }
        public string LocalizationId;
        public Texture2D Icon;
        public Action onClick;

        public void DrawButton(Rect rect)
        {
            if (GUI.Button(rect, new GUIContent(Icon, "[POSM]" + LocalizationId)))
            {
                ProceduralObjectsLogic.PlaySound();
                onClick.Invoke();
            }
        }
    }
}
