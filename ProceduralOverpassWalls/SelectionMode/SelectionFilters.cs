using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ProceduralObjects.Classes;
using ProceduralObjects.UI;
using ProceduralObjects.Localization;
using UnityEngine;

namespace ProceduralObjects.SelectionMode
{
    public class SelectionFilters
    {
        public SelectionFilters()
        {
            c_picker = true;
            c_props = true;
            c_buildings = true;
            c_decals = true;
            c_surfaces = true;
            c_groups = true;
            pickerPrefabName = "";
            timer = 0f;
        }

        public bool c_picker, c_props, c_buildings, c_decals, c_surfaces, c_groups;
        public string pickerPrefabName;
        public float timer;

        public static readonly float doubleClickTime = .25f;

        public bool FiltersAllow(ProceduralObject obj)
        {
            if (ProceduralObjectsLogic.instance.selectedGroup == null)
            {
                if (obj.isRootOfGroup)
                {
                    if (c_groups)
                        return true;
                    else
                        return false;
                }
            }
            if (c_picker)
            {
                if (pickerPrefabName == obj.basePrefabName)
                    return true;
            }
            if (obj.baseInfoType == "PROP")
            {
                if (c_decals)
                {
                    if (obj._baseProp.m_isDecal) return true;
                }
                if (c_surfaces)
                {
                    if (obj.isPloppableAsphalt) return true;
                }
                if (c_props)
                {
                    if (!obj._baseProp.m_isDecal && !obj.isPloppableAsphalt)
                        return true;
                }
            }
            if (c_buildings && obj.baseInfoType == "BUILDING")
            {
                return true;
            }
            return false;
        }
        public void Update()
        {
            if (timer > doubleClickTime)
                timer = 0f;
            if (timer > 0)
                timer += TimeUtils.deltaTime;
        }
        public bool DrawFilters(Rect rect)
        {
            GUI.Label(new Rect(2, 0, 90, 24), "<b>" + LocalizationManager.instance.current["filters"] + " :</b>");
            var newvalues = new bool[] { 
                GUI.Toggle(new Rect(95, 0, 225, 21), c_picker, " " + (pickerPrefabName == "" ? "<i>" : "") + LocalizationManager.instance.current["filters_picker"] + (pickerPrefabName == "" ? "</i>" : " (<i>" + GetShowName() + "</i>)")),
                GUI.Toggle(new Rect(95, 22, 95, 21), c_buildings, " " + LocalizationManager.instance.current["filters_buildings"]),
                GUI.Toggle(new Rect(200, 22, 95, 21), c_props, " " + LocalizationManager.instance.current["filters_props"]),
                GUI.Toggle(new Rect(95, 43, 95, 21), c_decals, " " + LocalizationManager.instance.current["filters_decals"]),
                GUI.Toggle(new Rect(200, 43, 95, 21), c_surfaces, " " + LocalizationManager.instance.current["filters_srfs"]),
                GUI.Toggle(new Rect(300, 22, 95, 21), c_groups, " " + LocalizationManager.instance.current["filters_grps"]) };

            if ((c_picker != newvalues[0]) || (c_buildings != newvalues[1]) || (c_props != newvalues[2]) || (c_decals != newvalues[3]) || (c_surfaces != newvalues[4]) || (c_groups != newvalues[5]))
            {
                if (timer == 0f)
                {
                    c_picker = newvalues[0];
                    c_buildings = newvalues[1];
                    c_props = newvalues[2];
                    c_decals = newvalues[3];
                    c_surfaces = newvalues[4];
                    c_groups = newvalues[5];
                    timer = 0.0001f;
                }
                else if (timer <= doubleClickTime)
                {
                    DisableAllButChanged(newvalues);
                    timer = 0f;
                }
                return true;
            }
            return false;
        }
        public bool DrawPicker(Rect rect)
        {
            if (GUI.Button(rect, ProceduralObjectsMod.Icons[12]))
            {
                ProceduralObjectsLogic.PlaySound();
                return true;
            }
            return false;
        }
        public void Pick(ProceduralObject obj)
        {
            pickerPrefabName = obj.basePrefabName;
            DisableAll();
            c_picker = true;
        }
        public void DisableAll()
        {
            c_picker = false;
            c_buildings = false;
            c_props = false;
            c_decals = false;
            c_surfaces = false;
            c_groups = false;
        }
        public void EnableAll()
        {
            c_picker = true;
            c_buildings = true;
            c_props = true;
            c_decals = true;
            c_surfaces = true;
            c_groups = true;
        }
        public string GetShowName()
        {
            if (pickerPrefabName.Length < 5)
                return pickerPrefabName;
            return pickerPrefabName.Substring(pickerPrefabName.IndexOf(".") + 1).Replace("_Data", "");
        }
        public void DisableAllButChanged(bool[] newValues)
        {
            string changed = "";
            if (c_picker != newValues[0]) changed = "pick";
            if (c_buildings != newValues[1]) changed = "build";
            if (c_props != newValues[2]) changed = "prop";
            if (c_decals != newValues[3]) changed = "decal";
            if (c_surfaces != newValues[4]) changed = "srf";
            if (c_groups != newValues[5]) changed = "grp";

            if (changed == "")
                return;
            DisableAll();

            if (changed == "pick")
                c_picker = newValues[0];
            if (changed == "build")
                c_buildings = newValues[1];
            if (changed == "prop")
                c_props = newValues[2];
            if (changed == "decal")
                c_decals = newValues[3];
            if (changed == "srf")
                c_surfaces = newValues[4];
            if (changed == "grp")
                c_groups = newValues[5];
        }
    }
}
