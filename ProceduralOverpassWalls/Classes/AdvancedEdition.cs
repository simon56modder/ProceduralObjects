using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using ColossalFramework;

using ProceduralObjects.Localization;

namespace ProceduralObjects.Classes
{
    public class AdvancedEditionManager
    {
        public AdvancedEditionManager(ProceduralObject obj, Action undo, Action redo, Action apply, Action storeHeight)
        {
            m_object = obj;
            oldTilingFactor = obj.tilingFactor;
            winRect = new Rect(555, 100, 400, 400);
            stretchFactor = 10;
            showWindow = false;
            this.undo = undo;
            this.redo = redo;
            this.apply = apply;
            this.storeHeight = storeHeight;
        }

        public ProceduralObject m_object;
        public Vertex[] m_vertices;
        private Rect winRect;
        private Action undo, redo, apply, storeHeight;
        public bool showWindow;
        private int oldTilingFactor;
        private int stretchFactor;

        public void DrawWindow()
        {
            if (showWindow)
                winRect = GUIUtils.ClampRectToScreen(GUI.Window(1094334748, winRect, draw, LocalizationManager.instance.current["adv_edition"]));
        }

        public void Update()
        {
            if (showWindow && m_object != null)
            {
                if (!m_object.disableRecalculation)
                {
                    if (oldTilingFactor != m_object.tilingFactor)
                    {
                        m_object.m_mesh.uv = Vertex.RecalculateUVMap(m_object, m_vertices);
                        oldTilingFactor = m_object.tilingFactor;
                    }
                }
            }
        }

        private void draw(int id)
        {
            GUI.DragWindow(new Rect(0, 0, 350, 28));
            if (GUI.Button(new Rect(356, 3, 29, 28), "X"))
            {
                ProceduralObjectsLogic.PlaySound();
                showWindow = false;
            }
            GUI.Label(new Rect(5, 27, 390, 27), "<b><size=15>" + LocalizationManager.instance.current["position"] + "</size></b>");
            GUI.Label(new Rect(6, 54, 25, 24), "X :");
            GUI.Label(new Rect(135, 54, 25, 24), "Y :");
            GUI.Label(new Rect(266, 54, 25, 24), "Z :");
            float newX, newY, newZ;
            if (float.TryParse(GUI.TextField(new Rect(31, 54, 100, 23), m_object.m_position.x.ToString()), out newX))
                m_object.m_position.x = newX;
            if (float.TryParse(GUI.TextField(new Rect(160, 54, 100, 23), m_object.m_position.y.ToString()), out newY))
                m_object.m_position.y = newY;
            if (float.TryParse(GUI.TextField(new Rect(289, 54, 100, 23), m_object.m_position.z.ToString()), out newZ))
                m_object.m_position.z = newZ;

            if (GUI.Button(new Rect(5, 80, 192.5f, 22), LocalizationManager.instance.current["snapToGround"]))
            {
                m_object.SnapToGround();
                Singleton<EffectManager>.instance.DispatchEffect(Singleton<BuildingManager>.instance.m_properties.m_placementEffect,
                    new EffectInfo.SpawnArea(m_object.m_position, Vector3.up, 10f), Vector3.zero, 0f, 1f, Singleton<AudioManager>.instance.DefaultGroup, 0u, true);
            }

            if (GUI.Button(new Rect(202.5f, 80, 192.5f, 22), LocalizationManager.instance.current["storeHeight"]))
            {
                ProceduralObjectsLogic.PlaySound();
                storeHeight.Invoke();
            }

            GUI.Label(new Rect(5, 105, 390, 27), "<b><size=15>" + LocalizationManager.instance.current["edition_history"] + "</size></b>");

            // undo
            GUI.BeginGroup(new Rect(5, 133, 135, 60));
            if (m_object.historyEditionBuffer.CanUndo)
            {
                if (GUI.Button(new Rect(0, 0, 135, 60), string.Empty))
                    undo.Invoke();
                else
                {
                    GUI.Label(new Rect(3, 3, 98, 64), "<size=13><b>" + LocalizationManager.instance.current["undo"] + "</b></size>");
                    GUI.Label(new Rect(3, 20, 98, 55), "<size=10>" + LocalizationManager.instance.current["action_type"] + " : "
                        + m_object.historyEditionBuffer.stepsDone[m_object.historyEditionBuffer.stepsDone.Count - 1].GetLocalizedStepString() + "</size>");
                }
            }
            else
            {
                GUI.Box(new Rect(0, 0, 135, 60), string.Empty);
                GUI.color = Color.gray;
                GUI.Label(new Rect(10, 10, 95, 40), "<i><size=13>" + LocalizationManager.instance.current["cant_undo"] + "</size></i>");
            }
            GUI.Label(new Rect(110, 12, 30, 30), "<size=28>↺</size>");
            GUI.color = Color.white;
            GUI.EndGroup();

            // redo
            GUI.BeginGroup(new Rect(145, 133, 135, 60));
            if (m_object.historyEditionBuffer.CanRedo)
            {
                if (GUI.Button(new Rect(0, 0, 135, 60), string.Empty))
                    redo.Invoke();
                else
                {
                    GUI.Label(new Rect(30, 3, 102, 64), "<size=13><b>" + LocalizationManager.instance.current["redo"] + "</b></size>");
                    GUI.Label(new Rect(30, 20, 102, 55), "<size=10>" + LocalizationManager.instance.current["action_type"] + " : "
                        + m_object.historyEditionBuffer.stepsUndone[m_object.historyEditionBuffer.stepsUndone.Count - 1].GetLocalizedStepString() + "</size>");
                }
            }
            else
            {
                GUI.Box(new Rect(0, 0, 135, 60), string.Empty);
                GUI.color = Color.gray;
                GUI.Label(new Rect(37, 10, 95, 40), "<i><size=13>" + LocalizationManager.instance.current["cant_redo"] + "</size></i>");
            }
            GUI.Label(new Rect(7, 12, 30, 30), "<size=28>↻</size>");
            GUI.color = Color.white;
            GUI.EndGroup();

            // erase history buffer
            var erase = new Rect(285, 133, 110, 60);
            if (GUI.Button(erase, string.Empty))
            {
                m_object.historyEditionBuffer.stepsDone.Clear();
                m_object.historyEditionBuffer.stepsUndone.Clear();
            }
            GUI.Label(erase, LocalizationManager.instance.current["erase_history"]);

            // mirror
            GUI.Label(new Rect(5, 197, 145, 27), "<b><size=15>" + LocalizationManager.instance.current["mirror_mesh"] + "</size></b>");
            GUI.Label(new Rect(150, 197, 270, 27), "<b><size=15>" + LocalizationManager.instance.current["stretch_mesh"] + "</size></b>");

            if (m_object.isPloppableAsphalt)
            {
                GUI.color = Color.gray;
                GUI.Box(new Rect(5, 224, 385, 26), "<i>" + LocalizationManager.instance.current["no_mirror_no_stretch"] + "</i>");
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = Color.red;
                if (GUI.Button(new Rect(5, 224, 35, 25), "<b>X</b>"))
                {
                    m_object.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.mirrorX, m_vertices);
                    VertexUtils.MirrorX(m_vertices);
                    m_object.historyEditionBuffer.ConfirmNewStep(m_vertices);
                    apply.Invoke();
                }
                GUI.color = Color.green;
                if (GUI.Button(new Rect(45, 224, 35, 26), "<b>Y</b>"))
                {
                    m_object.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.mirrorY, m_vertices);
                    VertexUtils.MirrorY(m_vertices);
                    m_object.historyEditionBuffer.ConfirmNewStep(m_vertices);
                    apply.Invoke();
                }
                GUI.color = Color.blue;
                if (GUI.Button(new Rect(85, 224, 35, 26), "<b>Z</b>"))
                {
                    m_object.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.mirrorZ, m_vertices);
                    VertexUtils.MirrorZ(m_vertices);
                    m_object.historyEditionBuffer.ConfirmNewStep(m_vertices);
                    apply.Invoke();
                }
                GUI.color = Color.white;

                // stretch
                GUI.Label(new Rect(150, 220, 125, 20), "x" + ((float)stretchFactor / 10f).ToString());
                stretchFactor = Mathf.FloorToInt(GUI.HorizontalSlider(new Rect(150, 237, 125, 20), stretchFactor, 1f, 30f));

                GUI.color = Color.red;
                if (GUI.Button(new Rect(280, 224, 35, 25), "<b>X</b>"))
                {
                    m_object.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.stretchX, (float)stretchFactor / 10f);
                    VertexUtils.StretchX(m_vertices, (float)stretchFactor / 10f);
                    m_object.historyEditionBuffer.ConfirmNewStep(m_vertices);
                    apply.Invoke();
                }
                GUI.color = Color.green;
                if (GUI.Button(new Rect(320, 224, 35, 26), "<b>Y</b>"))
                {
                    m_object.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.stretchY, (float)stretchFactor / 10f);
                    VertexUtils.StretchY(m_vertices, (float)stretchFactor / 10f);
                    m_object.historyEditionBuffer.ConfirmNewStep(m_vertices);
                    apply.Invoke();
                }
                GUI.color = Color.blue;
                if (GUI.Button(new Rect(360, 224, 35, 26), "<b>Z</b>"))
                {
                    m_object.historyEditionBuffer.InitializeNewStep(EditingStep.StepType.stretchZ, (float)stretchFactor / 10f);
                    VertexUtils.StretchZ(m_vertices, (float)stretchFactor / 10f);
                    m_object.historyEditionBuffer.ConfirmNewStep(m_vertices);
                    apply.Invoke();
                }
                GUI.color = Color.white;
            }



            // texture UV
            GUI.Label(new Rect(5, 254, 390, 27), "<b><size=15>" + LocalizationManager.instance.current["texture_tiling"] + "</size></b>");
            if (m_object.RequiresUVRecalculation)
            {
                if (GUI.Button(new Rect(5, 278, 235, 40), LocalizationManager.instance.current["tex_uv_mode"] + " : " + LocalizationManager.instance.current[(m_object.disableRecalculation ? "uv_stretch" : "uv_repeat")]))
                {
                    if (m_object.disableRecalculation)
                    {
                        m_object.disableRecalculation = false;
                        m_object.m_mesh.uv = Vertex.RecalculateUVMap(m_object, m_vertices);
                    }
                    else
                    {
                        m_object.disableRecalculation = true;
                        m_object.m_mesh.uv = Vertex.DefaultUVMap(m_object);
                    }
                }
                GUI.Label(new Rect(245, 277, 150, 22), string.Format(LocalizationManager.instance.current["tiling_factor"], m_object.tilingFactor));
                m_object.tilingFactor = (int)Mathf.FloorToInt(GUI.HorizontalSlider(new Rect(245, 299, 150, 22), (float)m_object.tilingFactor, 1, 20));

            }
            else
            {
                GUI.color = Color.gray;
                GUI.Box(new Rect(5, 277, 390, 42), "<i>" + LocalizationManager.instance.current["no_tex_tiling"] + "</i>");
                GUI.color = Color.white;
            }
        }

    }
}
