using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ProceduralObjects.Classes;
using ProceduralObjects.Localization;
using ProceduralObjects.UI;
using UnityEngine;

namespace ProceduralObjects.SelectionMode
{
    public class SelectTexture : SelectionModeAction
    {
        Vector2 scrolling = Vector2.zero;
        Texture tex;
        bool selected;

        public override void OnOpen(List<ProceduralObject> selection)
        {
            this.selection = POGroup.AllObjectsInSelection(selection, logic.selectedGroup);
        }
        public override void OnActionGUI(Vector2 uiPos)
        {
            if (selected)
            {
                if (GUI.Button(new Rect(uiPos, new Vector2(150, 150)), tex))
                {
                    ProceduralObjectsLogic.PlaySound();
                    tex = null;
                    selected = false;
                }
                if (GUI.Button(new Rect(uiPos.x, uiPos.y + 152, 72, 22), LocalizationManager.instance.current["ok"]))
                {
                    ProceduralObjectsLogic.PlaySound();
                    Apply();
                    ExitAction();
                }
                if (GUI.Button(new Rect(uiPos.x + 74, uiPos.y + 152, 76, 22), LocalizationManager.instance.current["cancel"]))
                {
                    ProceduralObjectsLogic.PlaySound();
                    ExitAction();
                }
            }
            else
            {
                var rect = new Rect(uiPos, new Vector2(390, 330));
                if (!ProceduralObjectsMod.UseUINightMode.value)
                    GUI.DrawTexture(rect, GUIUtils.bckgTex, ScaleMode.StretchToFill);
                GUI.Box(rect, string.Empty);
                if (GUIUtils.TextureSelector(new Rect(uiPos.x + 5, uiPos.y + 5, 380, 320), ref scrolling, out tex))
                    selected = true;
            }
        }
        public void Apply()
        {
            foreach (var obj in selection)
            {
                obj.customTexture = tex;
                var texture = (tex == null) ? ProceduralUtils.GetBasePrefabMainTex(obj) : tex;
                obj.m_material.mainTexture = texture;
                if (obj.m_textParameters != null)
                {
                    if (obj.m_textParameters.Count() > 0)
                    {
                        Texture original = ProceduralUtils.GetOriginalTexture(obj);
                        var originalTex = new Texture2D(original.width, original.height, TextureFormat.RGBA32, false);
                        originalTex.SetPixels(((Texture2D)original).GetPixels());
                        var newtex = (Texture2D)GameObject.Instantiate(originalTex);
                        obj.m_material.mainTexture = obj.m_textParameters.ApplyParameters(originalTex) as Texture;
                    }
                }
            }
        }
        public override Rect CollisionUI(Vector2 uiPos)
        {
            return selected ? new Rect(uiPos, new Vector2(150, 174)) : new Rect(uiPos, new Vector2(390, 330));
        }
    }
}
