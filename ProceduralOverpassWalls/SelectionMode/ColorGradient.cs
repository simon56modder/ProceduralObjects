using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ProceduralObjects.Classes;
using ProceduralObjects.Localization;
using ProceduralObjects.UI;

namespace ProceduralObjects.SelectionMode
{
    public class ColorGradient : SelectionModeAction
    {
        public GUIPainter painterA, painterB;
        public Color A = Color.white, B = Color.white;
        public Dictionary<ProceduralObject, Color> oldColors;
        public Texture2D gradientTex;
        public Gradient gradient;
        public KeyValuePair<ProceduralObject, ProceduralObject> maxObjects;

        private bool confirmed, clickedCenter;

        public override void OnOpen(List<ProceduralObject> selection)
        {
            confirmed = false;
            this.selection = selection = POGroup.AllObjectsInSelection(selection, logic.selectedGroup);
            if (selection.Count < 2)
            {
                ExitAction();
                return;
            }
            oldColors = new Dictionary<ProceduralObject, Color>();
            foreach (var obj in selection)
            {
                oldColors.Add(obj, obj.m_color);
            }
            gradientTex = TextureUtils.PlainTexture(100, 22, Color.white);
            maxObjects = VertexUtils.OutmostPoints(selection.ToArray());
            gradient = new Gradient();
        }
        public override void OnUpdate()
        {
            GUIPainter.UpdatePainter(painterA);
            GUIPainter.UpdatePainter(painterB);
        }
        public override void OnSingleClick(ProceduralObject obj)
        {
            if (!selection.Contains(obj)) return;

            if (clickedCenter)
                maxObjects = new KeyValuePair<ProceduralObject,ProceduralObject>(maxObjects.Key, obj);
            else
                maxObjects = new KeyValuePair<ProceduralObject, ProceduralObject>(obj, maxObjects.Value);

            clickedCenter = !clickedCenter;
            UpdateGradient();
        }
        public override void OnActionGUI(Vector2 uiPos)
        {
            GUI.DrawTexture(new Rect(uiPos.x, uiPos.y, 100, 22), gradientTex);
            painterA = GUIPainter.DrawPainter(painterA, new Vector2(uiPos.x, uiPos.y + 24), new Vector2(uiPos.x, uiPos.y + 46), A, (c) => { A = c; UpdateGradient(); }, () => { });
            painterB = GUIPainter.DrawPainter(painterB, new Vector2(uiPos.x + 74, uiPos.y + 24), new Vector2(uiPos.x + 74, uiPos.y + 46), B, (c) => { B = c; UpdateGradient(); }, () => { });
            if (GUI.Button(new Rect(uiPos.x + 102, uiPos.y, 32, 22), LocalizationManager.instance.current["ok"]))
            {
                ProceduralObjectsLogic.PlaySound();
                confirmed = true;
                ExitAction();
                return;
            }
            if (GUI.Button(new Rect(uiPos.x + 136, uiPos.y, 80, 22), LocalizationManager.instance.current["cancel"]))
            {
                ProceduralObjectsLogic.PlaySound();
                ExitAction();
                return;
            }
        }
        public override Rect CollisionUI(Vector2 uiPos)
        {
            return new Rect(uiPos, new Vector2(217, 24));
        }
        public void UpdateGradient()
        {
            gradient.colorKeys = new GradientColorKey[] { new GradientColorKey(A, 0f), new GradientColorKey(B, 1f) };
            for (int x = 0; x < 100; x++)
            {
                Color c = gradient.Evaluate(x / 100f);
                for (int y = 0; y < 22; y++)
                    gradientTex.SetPixel(x, y, c);
            }
            gradientTex.Apply();

            float distDiff = Vector2.Distance(maxObjects.Value.m_position.AsXZVector2(), maxObjects.Key.m_position.AsXZVector2());
            foreach (var po in selection)
            {
                float localdistdiff = Vector3.Project((po.m_position - maxObjects.Key.m_position).NullY(), (maxObjects.Value.m_position - maxObjects.Key.m_position).NullY()).magnitude;
                var color = gradient.Evaluate(localdistdiff / distDiff);
                po.m_color = color;
                po.m_material.color = color;
            }
        }
        public override void ExitAction()
        {
            if (oldColors != null)
            {
                if (!confirmed)
                {
                    foreach (var kvp in oldColors)
                    {
                        kvp.Key.m_color = kvp.Value;
                        kvp.Key.m_material.color = kvp.Value;
                    }
                }
                if (gradientTex != null)
                    gradientTex.DisposeTexFromMemory();
            }
            base.ExitAction();
        }
    }
}
