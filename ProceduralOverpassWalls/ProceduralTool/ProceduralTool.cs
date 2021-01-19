using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColossalFramework;
using UnityEngine;

using ProceduralObjects.Classes;
using ProceduralObjects.Localization;
using ProceduralObjects.UI;

namespace ProceduralObjects.Tools
{
    public class ProceduralTool : ToolBase
    {
        // basically Empty ATM, all the related code is located in /ProceduralObjectsLogic.cs
        public static CursorInfo buildCursor = null, terrainLevel = null, terrainShift = null, moveVertices, rotateVertices, scaleVertices;
       
        public static void CreateCursors()
        {
            var cursors = Resources.FindObjectsOfTypeAll<CursorInfo>();
            buildCursor = cursors.First(cursor => cursor.name == "Building Placement");
            terrainLevel = cursors.First(cursor => cursor.name == "Terrain Level");
            terrainShift = cursors.First(cursor => cursor.name == "Terrain Shift");

            var hotspot = new Vector2(6, 5);
            moveVertices = ScriptableObject.CreateInstance<CursorInfo>();
            moveVertices.m_texture = TextureUtils.LoadTextureFromAssembly("CursorMoveVertices");
            moveVertices.m_hotspot = hotspot;

            rotateVertices = ScriptableObject.CreateInstance<CursorInfo>();
            rotateVertices.m_texture = TextureUtils.LoadTextureFromAssembly("CursorRotateVertices");
            rotateVertices.m_hotspot = hotspot;

            scaleVertices = ScriptableObject.CreateInstance<CursorInfo>();
            scaleVertices.m_texture = TextureUtils.LoadTextureFromAssembly("CursorScaleVertices");
            scaleVertices.m_hotspot = hotspot;
        }
        protected override void OnToolUpdate()
        {
            base.OnToolUpdate();
            string toolInfo = "";
            switch (ProceduralObjectsLogic.toolAction)
            {
                case ToolAction.none:
                    switch (ProceduralObjectsLogic.axisState)
                    {
                        case AxisEditionState.none:
                            ToolCursor = (CursorInfo)null;
                            break;
                        case AxisEditionState.X:
                        case AxisEditionState.Z:
                            ToolCursor = terrainLevel;
                            break;
                        case AxisEditionState.Y:
                            ToolCursor = terrainShift;
                            break;
                    }
                    if (Gizmos.registeredString != "")
                    {
                        switch (ProceduralObjectsLogic.actionMode)
                        {
                            case 0:
                                toolInfo = Gizmos.registeredString + ProceduralObjectsMod.distanceUnit;
                                break;
                            case 1:
                                toolInfo = "x" + Gizmos.registeredString;
                                break;
                            case 2:
                                toolInfo = Gizmos.registeredString + ProceduralObjectsMod.angleUnit;
                                break;
                        }
                        base.ShowToolInfo(true, toolInfo, toolInfoPos);
                    }
                    else if (Gizmos.isSnappingPrevMove)
                    {
                        base.ShowToolInfo(true, LocalizationManager.instance.current["repeatPrevMov"], toolInfoPos);
                    }
                    else
                        base.ShowToolInfo(false, "", Vector3.zero);
                    break;
                case ToolAction.vertices:
                    switch (ProceduralObjectsLogic.verticesToolType)
                    {
                        case 0:
                            ToolCursor = moveVertices;
                            break;
                        case 1:
                            ToolCursor = rotateVertices;
                            break;
                        case 2:
                            ToolCursor = scaleVertices;
                            break;
                    }
                    if (ProceduralObjectsLogic.tabSwitchTimer != 0f)
                    {
                        switch (ProceduralObjectsLogic.verticesToolType)
                        {
                            case 0:
                                toolInfo = LocalizationManager.instance.current["position"];
                                break;
                            case 1:
                                toolInfo = LocalizationManager.instance.current["rotation"];
                                break;
                            case 2:
                                toolInfo = LocalizationManager.instance.current["scale_obj"];
                                break;
                        }
                        base.ShowToolInfo(true, toolInfo, toolInfoPos);
                    }
                    else if (Gizmos.registeredString != "")
                    {
                        switch (ProceduralObjectsLogic.verticesToolType)
                        {
                            case 0:
                                toolInfo = Gizmos.registeredString + ProceduralObjectsMod.distanceUnit;
                                break;
                            case 1:
                                toolInfo = Gizmos.registeredString + ProceduralObjectsMod.angleUnit;
                                break;
                            case 2:
                                toolInfo = "x" + Gizmos.registeredString;
                                break;
                        }
                        base.ShowToolInfo(true, toolInfo, toolInfoPos);
                    }
                    else
                        base.ShowToolInfo(false, "", Vector3.zero);
                    break;
                case ToolAction.build:
                    ToolCursor = buildCursor;
                    base.ShowToolInfo(true, LocalizationManager.instance.current["click_to_place"], ProceduralObjectsLogic.movingWholeRaycast);
                    break;
            }
        }
        protected override void OnDisable()
        {
            ToolCursor = (CursorInfo)null;
            base.OnDisable();
        }
        public static bool TerrainRaycast(RaycastInput raycastInput, out RaycastOutput raycastOutput)
        {
            return ToolBase.RayCast(raycastInput, out raycastOutput);
        }
        private Vector3 toolInfoPos
        {
            get
            {
                Vector3 pos = Vector3.zero;
                ToolBase.RaycastInput rayInput = new ToolBase.RaycastInput(Camera.main.ScreenPointToRay(Input.mousePosition), Camera.main.farClipPlane);
                ToolBase.RaycastOutput rayOutput;
                if (ProceduralTool.TerrainRaycast(rayInput, out rayOutput))
                    pos = rayOutput.m_hitPos;
                return pos;
            }
        }

        public static void DrawToolsControls(Rect rect, bool isGeneral)
        {
            GUI.BeginGroup(rect);
            // w 380
            if (GUI.Button(new Rect(0, 0, 22, 22), ProceduralObjectsMod.ShowToolsControls.value ? "▼" : "►"))
            {
                ProceduralObjectsLogic.PlaySound();
                scrollControls = Vector2.zero;
                ProceduralObjectsMod.ShowToolsControls.value = !ProceduralObjectsMod.ShowToolsControls.value;
            }

            GUI.Label(new Rect(24, 1, rect.width - 28, 20), "<b>" + LocalizationManager.instance.current["controls"] + ":</b>");
            if (ProceduralObjectsMod.ShowToolsControls.value)
            {
                var height = GUI.skin.label.CalcHeight(new GUIContent(isGeneral ? GTControls : CTControls), 358);

                GUI.Box(new Rect(0, 25, rect.width - 22, rect.height - 30), string.Empty);
                scrollControls = GUI.BeginScrollView(new Rect(2, 27, rect.width - 1, rect.height - 32), scrollControls, new Rect(0, 0, 358, height + 2));
                GUI.Label(new Rect(2, 0, 358, height), isGeneral ? GTControls : CTControls);
                GUI.EndScrollView();

                GUIUtils.DrawSeparator(new Vector2(0, rect.height - 1), rect.width);
            }
            else
                GUIUtils.DrawSeparator(new Vector2(0, 25), rect.width);

            GUI.EndGroup();
        }

        public static Vector2 scrollControls = Vector2.zero;
        private static string CTControls, GTControls;

        public static void SetupControlsStrings()
        {
            CTControls = KeyBindingsManager.instance.GetBindingFromName("edition_smoothMovements").m_fullKeys + " : " + LocalizationManager.instance.current["hold_for_smooth"] +
                            "\n" + KeyBindingsManager.instance.GetBindingFromName("edition_smallMovements").m_fullKeys + " : " + LocalizationManager.instance.current["hold_slow_together"] + "\n\nCtrl : " + LocalizationManager.instance.current["hold_multiple_vertices"] +
                            "\n" + LocalizationManager.instance.current["move_vertices"] + "\n" +
                            KeyBindingsManager.instance.GetBindingFromName("position_moveUp").m_fullKeys + "/" + KeyBindingsManager.instance.GetBindingFromName("position_moveDown").m_fullKeys +
                            " : " + LocalizationManager.instance.current["move_vertices_updown"] + "\n" +
                        KeyBindingsManager.instance.GetBindingFromName("switchGeneralToVertexTools").m_fullKeys + " : " + LocalizationManager.instance.current["quick_switch"] +
                        "\n" + KeyBindingsManager.instance.GetBindingFromName("copy").m_fullKeys + " : " + LocalizationManager.instance.current["copy_obj"] + "\n" +
                        KeyBindingsManager.instance.GetBindingFromName("paste").m_fullKeys + " : " + LocalizationManager.instance.current["paste_obj"]
                        + "\n" + LocalizationManager.instance.current["delete_desc"] + "\n\n" +
                        LocalizationManager.instance.current["rmb_marquee_vertices"] + "\n" +
                        LocalizationManager.instance.current["lmb_drag_vertices"] +
                        "\nCtrl : " + LocalizationManager.instance.current["disableAxisSnapping"];
            GTControls = KeyBindingsManager.instance.GetBindingFromName("edition_smoothMovements").m_fullKeys + " : " + LocalizationManager.instance.current["hold_for_smooth"] + "\n" +
                            KeyBindingsManager.instance.GetBindingFromName("edition_smallMovements").m_fullKeys + " : " + LocalizationManager.instance.current["hold_slow_together"] + "\n\nCtrl : " +
                //  KeyBindingsManager.instance.GetBindingFromName("switchActionMode").m_fullKeys + " : " + LocalizationManager.instance.current["switch_modes"] + "\n" +
                            LocalizationManager.instance.current["duplicateWhileMoving"] + "\n" +
                            KeyBindingsManager.instance.GetBindingFromName("position_moveUp").m_fullKeys + ", " +
                            KeyBindingsManager.instance.GetBindingFromName("position_moveDown").m_fullKeys + ", " +
                            KeyBindingsManager.instance.GetBindingFromName("position_moveRight").m_fullKeys + ", " +
                            KeyBindingsManager.instance.GetBindingFromName("position_moveLeft").m_fullKeys + ", " +
                            KeyBindingsManager.instance.GetBindingFromName("position_moveForward").m_fullKeys + ", " +
                            KeyBindingsManager.instance.GetBindingFromName("position_moveBackward").m_fullKeys + " : " + LocalizationManager.instance.current["move_objects_pos"] + "\n" +
                            KeyBindingsManager.instance.GetBindingFromName("rotation_moveUp").m_fullKeys + ", " +
                            KeyBindingsManager.instance.GetBindingFromName("rotation_moveDown").m_fullKeys + ", " +
                            KeyBindingsManager.instance.GetBindingFromName("rotation_moveRight").m_fullKeys + ", " +
                            KeyBindingsManager.instance.GetBindingFromName("rotation_moveLeft").m_fullKeys + ", " +
                            KeyBindingsManager.instance.GetBindingFromName("rotation_moveForward").m_fullKeys + ", " +
                            KeyBindingsManager.instance.GetBindingFromName("rotation_moveBackward").m_fullKeys + " : " + LocalizationManager.instance.current["rotate_objects_rot"] + "\n" +
                            KeyBindingsManager.instance.GetBindingFromName("scale_scaleUp").m_fullKeys + "/" +
                            KeyBindingsManager.instance.GetBindingFromName("scale_scaleDown").m_fullKeys + " : " + LocalizationManager.instance.current["scale_objects"] + "\n" +
                            "\n" + KeyBindingsManager.instance.GetBindingFromName("switchGeneralToVertexTools").m_fullKeys + " : " + LocalizationManager.instance.current["quick_switch"] +
                            "\n\n<b>" + LocalizationManager.instance.current["buttons"] + 
                            " : </b>\n" + LocalizationManager.instance.current["delete_desc"] +
                            "\n" + LocalizationManager.instance.current["move_to_desc"];
        }
    }
    public static class ToolHelper
    {
        public static void FullySetTool<T>() where T : ToolBase
        {
            ToolsModifierControl.toolController.CurrentTool = ToolsModifierControl.GetTool<T>();
            ToolsModifierControl.SetTool<T>();
           // ToolsModifierControl.mainToolbar.CloseEverything();
        }
    }
}
