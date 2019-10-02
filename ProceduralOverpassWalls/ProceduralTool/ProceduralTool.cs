using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColossalFramework;
using UnityEngine;

using ProceduralObjects.Classes;
using ProceduralObjects.Localization;

namespace ProceduralObjects.Tools
{
    public class ProceduralTool : ToolBase
    {
        // basically Empty ATM, all the related code is located in /ProceduralObjectsLogic.cs
        public static CursorInfo buildCursor = null, terrainLevel = null, terrainShift = null, moveVertices, rotateVertices, scaleVertices;
       
        public static void CreateCursors()
        {
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
                    base.ShowToolInfo(false, "", Vector3.zero);
                    break;
                case ToolAction.vertices:
                    string toolInfo = "";
                    switch (ProceduralObjectsLogic.verticesToolType)
                    {
                        case 0:
                            ToolCursor = moveVertices;
                            toolInfo = LocalizationManager.instance.current["position"];
                            break;
                        case 1:
                            ToolCursor = rotateVertices;
                            toolInfo = LocalizationManager.instance.current["rotation"];
                            break;
                        case 2:
                            ToolCursor = scaleVertices;
                            toolInfo = LocalizationManager.instance.current["scale_obj"];
                            break;
                    }
                    if (ProceduralObjectsLogic.tabSwitchTimer != 0f)
                    {
                        Vector3 pos = Vector3.zero;
                        ToolBase.RaycastInput rayInput = new ToolBase.RaycastInput(Camera.main.ScreenPointToRay(Input.mousePosition), Camera.main.farClipPlane);
                        ToolBase.RaycastOutput rayOutput;
                        if (ProceduralTool.TerrainRaycast(rayInput, out rayOutput))
                            pos = rayOutput.m_hitPos;
                        base.ShowToolInfo(true, toolInfo, pos);
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
            base.OnDisable();
            ToolCursor = (CursorInfo)null;
        }
        public static bool TerrainRaycast(RaycastInput raycastInput, out RaycastOutput raycastOutput)
        {
            return ToolBase.RayCast(raycastInput, out raycastOutput);
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
