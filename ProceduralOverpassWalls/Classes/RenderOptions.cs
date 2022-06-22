using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;

using ProceduralObjects.UI;
using ProceduralObjects.Localization;
using ProceduralObjects.Classes;

namespace ProceduralObjects.Classes
{
    public class RenderOptions
    {
        public RenderOptions()
        {
            window = new Rect(355, 500, 390, 250);
        }

        public static RenderOptions instance;
        public static void Initialize()
        {
            instance = new RenderOptions();
            instance.calculateDynamically = ProceduralObjectsMod.UseDynamicRenderDist.value;
            instance.globalMultiplier = ProceduralObjectsMod.GlobalRDMultiplier.value;
        }

        public bool showWindow, calculateDynamically;
        public float globalMultiplier;
        public Rect window;

        public void DrawWindow()
        {
            if (showWindow)
                window = GUIUtils.ClampRectToScreen(GUIUtils.Window(347116650, window, draw, LocalizationManager.instance.current["render_options"]));
        }
        private void draw(int id)
        {
            if (GUIUtils.CloseHelpButtons(window, "Render_Options"))
                showWindow = false;
            GUI.DragWindow(new Rect(0, 0, 360, 26));

            GUI.Label(new Rect(5, 32, 245, 24), LocalizationManager.instance.current["current_calc_method"] + " :");
            if (GUI.Button(new Rect(255, 30, 130, 27), LocalizationManager.instance.current[calculateDynamically ? "renderCalc_dyn" : "renderCalc_fix"]))
            {
                ProceduralObjectsLogic.PlaySound();
                calculateDynamically = !calculateDynamically;
                ProceduralObjectsMod.UseDynamicRenderDist.value = calculateDynamically;
            }
            GUIUtils.DrawSeparator(new Vector2(5, 60), 380);

            if (calculateDynamically)
            {
                GUI.Label(new Rect(5, 65, 380, 25), string.Format(LocalizationManager.instance.current["renderCalc_dyn_mult"],
                    ProceduralObjectsMod.DynamicRDMultiplier.value).ToString());
                var slider = Mathf.Round(GUI.HorizontalSlider(new Rect(5, 91, 380, 22), ProceduralObjectsMod.DynamicRDMultiplier.value, 40, 180) / 10f) * 10f;
                if (slider != ProceduralObjectsMod.DynamicRDMultiplier.value)
                    ProceduralObjectsMod.DynamicRDMultiplier.value = slider;

                GUI.Label(new Rect(5, 110, 380, 25), string.Format(LocalizationManager.instance.current["renderCalc_dyn_thre"],
                    Gizmos.ConvertRoundToDistanceUnit(ProceduralObjectsMod.DynamicRDMinThreshold.value).ToString()) + ProceduralObjectsMod.distanceUnit);
                var slider2 = Mathf.Round(GUI.HorizontalSlider(new Rect(5, 136, 380, 22), ProceduralObjectsMod.DynamicRDMinThreshold.value, 10, 1000) / 10f) * 10f;
                if (slider2 != ProceduralObjectsMod.DynamicRDMinThreshold.value)
                    ProceduralObjectsMod.DynamicRDMinThreshold.value = slider2;
            }
            else
            {
                GUI.Label(new Rect(5, 65, 380, 25), string.Format(LocalizationManager.instance.current["settings_RD_PROP_label"],
                    Gizmos.ConvertRoundToDistanceUnit(ProceduralObjectsMod.PropRenderDistance.value).ToString()) + ProceduralObjectsMod.distanceUnit);
                var slider = Mathf.Round(GUI.HorizontalSlider(new Rect(5, 91, 380, 22), ProceduralObjectsMod.PropRenderDistance.value, 10, 24000) / 10f) * 10f;
                if (slider != ProceduralObjectsMod.PropRenderDistance.value)
                    ProceduralObjectsMod.PropRenderDistance.value = slider;

                GUI.Label(new Rect(5, 110, 380, 25), string.Format(LocalizationManager.instance.current["settings_RD_BUILDING_label"],
                    Gizmos.ConvertRoundToDistanceUnit(ProceduralObjectsMod.BuildingRenderDistance.value).ToString()) + ProceduralObjectsMod.distanceUnit);
                var slider2 = Mathf.Round(GUI.HorizontalSlider(new Rect(5, 136, 380, 22), ProceduralObjectsMod.BuildingRenderDistance.value, 10, 24000) / 10f) * 10f;
                if (slider2 != ProceduralObjectsMod.BuildingRenderDistance.value)
                    ProceduralObjectsMod.BuildingRenderDistance.value = slider2;
            }
            if (GUI.Button(new Rect(5, 160, 380, 28), LocalizationManager.instance.current["renderCalc_RECALC"]))
            {
                ProceduralObjectsLogic.PlaySound();
                GUIUtils.ShowModal(LocalizationManager.instance.current["renderCalc_RECALC_title"],
                    LocalizationManager.instance.current["renderCalc_RECALC_confirm"],
                    (bool ok) =>
                    {
                        if (ok)
                            RecalculateAll();
                    });
            }
            GUIUtils.DrawSeparator(new Vector2(5, 192), 380);

            GUI.Label(new Rect(5, 195, 380, 25), LocalizationManager.instance.current["renderCalc_globalMul"] + " : " + globalMultiplier.ToString());
            var slider3 = GUIUtils.HorizontalSliderIncrements(new Rect(5, 220, 380, 22), ProceduralObjectsMod.GlobalRDMultiplier.value, 0.1f, 0.2f, 0.3f, 0.5f, 0.7f, 1f, 1.5f, 2f, 2.5f, 3f, 4f, 6f);
            if (slider3 != ProceduralObjectsMod.GlobalRDMultiplier.value)
            {
                ProceduralObjectsMod.GlobalRDMultiplier.value = slider3;
                globalMultiplier = slider3;
            }
        }
        public void SetPosition(float x, float y)
        {
            window.position = new Vector2(x, y);
        }

        public float CalculateRenderDistance(ProceduralObject obj, bool resetIfNotDynamic)
        {
            if (obj.renderDistLocked)
                return obj.renderDistance;

            if (!calculateDynamically)
            {
                if (resetIfNotDynamic)
                    return (obj.baseInfoType == "PROP") ? ProceduralObjectsMod.PropRenderDistance.value : ProceduralObjectsMod.BuildingRenderDistance.value;

                return obj.renderDistance;
            }

            float multiplier = ProceduralObjectsMod.DynamicRDMultiplier.value;
            float supp = 200f;
            float threshold = ProceduralObjectsMod.DynamicRDMinThreshold.value;

            var size = obj.m_mesh.bounds.size;
            float max1 = Mathf.Max(size.x, size.y, size.z);
            float max2 = SecondMax(size.x, size.y, size.z);

            return Mathf.Clamp(Mathf.Ceil((multiplier * (max1 + max2) + supp) / 10f) * 10f, threshold, 16000);
        }
        public void RecalculateAll()
        {
            foreach (var obj in ProceduralObjectsLogic.instance.proceduralObjects)
            {
                if (obj.renderDistLocked)
                    continue;

                obj.renderDistance = CalculateRenderDistance(obj, true);
            }
        }

        public bool CanRenderSingle(ProceduralObject obj, bool nightTime)
        {
            if (obj.m_modules != null)
            {
                if (obj.m_modules.Count != 0)
                {
                    foreach (var module in obj.m_modules)
                    {
                        if (module.enabled)
                        {
                            try
                            {
                                if (!module.RenderParentThisFrame(ProceduralObjectsLogic.instance))
                                    return false;
                            }
                            catch (Exception e) { Debug.LogError("[ProceduralObjects] Error inside module RenderParentThisFrame() method!\n" + e); }
                        }
                    }
                }
            }

            if (obj.m_visibility == ProceduralObjectVisibility.Always)
                return true;
            return (obj.m_visibility == ProceduralObjectVisibility.NightOnly && nightTime) || (obj.m_visibility == ProceduralObjectVisibility.DayOnly && !nightTime);
        }

        public float SecondMax(params float[] values)
        {
            var list = values.ToList();
            list.Remove(Mathf.Max(values));
            return Mathf.Max(list.ToArray());
        }
    }
}
