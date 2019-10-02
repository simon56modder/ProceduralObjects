using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using ProceduralObjects.Localization;

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

        public Rect winRect = new Rect(300, 300, 320, 350);
        public bool showWindow = false;
        private Vector2 scrollLayers = Vector2.zero;
        private string newLayerText;
        private ProceduralObjectsLogic logic;

        public void DrawWindow()
        {
            if (showWindow)
                winRect = GUIUtils.ClampRectToScreen(GUI.Window(99045, winRect, draw, LocalizationManager.instance.current["layers"]));
        }
        void draw(int id)
        {
            GUI.DragWindow(new Rect(0, 0, 287, 28));
            if (GUI.Button(new Rect(289, 4, 28, 27), "X"))
            {
                ProceduralObjectsLogic.PlaySound();
                showWindow = false;
            }
            GUI.Label(new Rect(5, 22, 310, 28), LocalizationManager.instance.current["layers_desc"]);
            GUI.Box(new Rect(10, 50, 300, 295), string.Empty);
            scrollLayers = GUI.BeginScrollView(new Rect(7, 52, 306, 291), scrollLayers, new Rect(0, 0, 282, m_layers.Count * 26 + 32));
            for (int i = 0; i < m_layers.Count; i++)
            {
                if (GUI.Button(new Rect(9, i * 26 + 1, 24, 24), ProceduralObjectsMod.Icons[m_layers[i].m_isHidden ? 3 : 4]))
                {
                    ProceduralObjectsLogic.PlaySound();
                    m_layers[i].m_isHidden = !m_layers[i].m_isHidden;
                }
                m_layers[i].m_name = GUI.TextField(new Rect(35, i * 26 + 1, 219, 24), m_layers[i].m_name);
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
