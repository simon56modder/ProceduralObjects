using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using ProceduralObjects.Localization;
using ProceduralObjects.UI;

namespace ProceduralObjects.Classes
{
    public class LayerManager
    {
        public LayerManager(ProceduralObjectsLogic logic)
        {
            this.logic = logic;
            m_layers = new List<Layer>();
            newLayerText = LocalizationManager.instance.current["layer_new"];
        }
        public List<Layer> m_layers;

        public Rect winRect = new Rect(155, 500, 320, 360);
        public bool showWindow = false;
        private Vector2 scrollLayers = Vector2.zero;
        private string newLayerText;
        private ProceduralObjectsLogic logic;
        private bool expandingWindow = false;

        public void DrawWindow()
        {
            if (showWindow)
                winRect = GUIUtils.ClampRectToScreen(GUIUtils.Window(99045, winRect, draw, LocalizationManager.instance.current["layers"]));
        }
        public void Update()
        {
            if (!showWindow) return;
            if (expandingWindow)
            {
                var mouseposy = GUIUtils.MousePos.y;
                winRect.height = Mathf.Max(190, mouseposy - winRect.yMin);
                if (Input.GetMouseButtonUp(0))
                    expandingWindow = false;
            }
        }
        void draw(int id)
        {
            GUI.DragWindow(new Rect(0, 0, 268, 28));
            if (GUIUtils.CloseHelpButtons(winRect, "Layers"))
            {
                showWindow = false;
                expandingWindow = false;
            }
            GUI.Label(new Rect(5, 22, 310, 28), LocalizationManager.instance.current["layers_desc"]);
            GUIUtils.DrawSeparator(new Vector2(7, 46), 306);
            scrollLayers = GUI.BeginScrollView(new Rect(7, 52, 306, winRect.height - 69), scrollLayers, new Rect(0, 0, 282, m_layers.Count * 26 + 32));
            for (int i = 0; i < m_layers.Count; i++)
            {
                if (GUI.Button(new Rect(9, i * 26 + 1, 24, 24), ProceduralObjectsMod.Icons[m_layers[i].m_isHidden ? 3 : 4]))
                {
                    ProceduralObjectsLogic.PlaySound();
                    m_layers[i].m_isHidden = !m_layers[i].m_isHidden;
                }
                bool canBeMoved = CanLayerMoveUp(m_layers[i]) || CanLayerMoveDown(m_layers[i]);
                if (canBeMoved)
                {
                    if (CanLayerMoveUp(m_layers[i]))
                    {
                        if (GUI.Button(new Rect(35, i * 26 + 1, 25, 12), ProceduralObjectsMod.Icons[6]))
                        {
                            ProceduralObjectsLogic.PlaySound();
                            MoveLayerUp(m_layers[i]);
                        }
                    }
                    if (CanLayerMoveDown(m_layers[i]))
                    {
                        if (GUI.Button(new Rect(35, i * 26 + 13.5f, 25, 12), ProceduralObjectsMod.Icons[7]))
                        {
                            ProceduralObjectsLogic.PlaySound();
                            MoveLayerDown(m_layers[i]);
                        }
                    }
                }
                m_layers[i].m_name = GUI.TextField(new Rect(canBeMoved ? 62 : 35, i * 26 + 1, canBeMoved ? 192 : 219, 24), m_layers[i].m_name);
                GUI.color = Color.red;
                if (GUI.Button(new Rect(256, i * 26 + 1, 24, 24), "X"))
                {
                    ProceduralObjectsLogic.PlaySound();
                    RemoveLayer(m_layers[i], logic.proceduralObjects);
                }
                GUI.color = Color.white;
            }
            newLayerText = GUI.TextField(new Rect(35, m_layers.Count * 26 + 1, 219, 24), newLayerText);
            if (GUI.Button(new Rect(256, m_layers.Count * 26 + 1, 24, 24), "<size=14>+</size>"))
            {
                ProceduralObjectsLogic.PlaySound();
                AddLayer(newLayerText);
            }
            GUI.EndScrollView();

            if (GUI.RepeatButton(new Rect(120, winRect.height - 14, 80, 10), string.Empty))
                expandingWindow = true;
        }


        public Layer AddLayer(string name)
        {
            if (m_layers == null)
                m_layers = new List<Layer>();
            uint i = 1;
            foreach (Layer layer in m_layers)
            {
                if (layer.m_id > i)
                    i = layer.m_id;
            }
            var l = new Layer(name, i + 1);
            m_layers.Add(l);
            newLayerText = LocalizationManager.instance.current["layer_new"];
            return l;
        }
        public void RemoveLayer(Layer layer, List<ProceduralObject> objects)
        {
            foreach (var obj in objects)
            {
                if (obj.layer == layer)
                    obj.layer = null;
            }
            if (m_layers == null)
                return;
            if (!m_layers.Contains(layer))
                return;
            m_layers.Remove(layer);
        }
        public void MoveLayerUp(Layer layer)
        {
            if (!CanLayerMoveUp(layer))
                return;
            var buffer = new List<Layer>(m_layers);
            var index = buffer.IndexOf(layer);
            buffer.Remove(layer);
            m_layers.Clear();
            for (int i = 0; i < buffer.Count; i++)
            {
                if (i == index - 1)
                    m_layers.Add(layer);
                m_layers.Add(buffer[i]);
            }
        }
        public void MoveLayerDown(Layer layer)
        {
            if (!CanLayerMoveDown(layer))
                return;
            var buffer = new List<Layer>(m_layers);
            var index = buffer.IndexOf(layer);
            buffer.Remove(layer);
            m_layers.Clear();
            for (int i = 0; i < buffer.Count; i++)
            {
                m_layers.Add(buffer[i]);
                if (i == index)
                    m_layers.Add(layer);
            }
        }
        public bool CanLayerMoveUp(Layer layer)
        {
            if (!m_layers.Contains(layer))
                return false;
            if (m_layers.IndexOf(layer) == 0)
                return false;
            return true;
        }
        public bool CanLayerMoveDown(Layer layer)
        {
            if (!m_layers.Contains(layer))
                return false;
            if (m_layers.IndexOf(layer) == m_layers.Count - 1)
                return false;
            return true;
        }
        public void UpdateLocalization()
        {
            newLayerText = LocalizationManager.instance.current["layer_new"];
        }
    }

    [Serializable]
    public class Layer
    {
        public Layer(string name, uint id)
        {
            m_name = name;
            m_id = id;
        }
        public string m_name;
        public bool m_isHidden;
        public uint m_id;
    }
}
