using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using ProceduralObjects.Classes;
using ProceduralObjects.Localization;

namespace ProceduralObjects.UI
{
    public class GUIPainter
    {
        public GUIPainter() { }

        public bool showPicker;
        public Vector2 samplePosition, pickerPosition;
        public Rect pickerRect
        {
            get { return new Rect(pickerPosition.x, pickerPosition.y, 225, 214); }
        }
        public Rect sampleRect
        {
            get { return new Rect(samplePosition.x, samplePosition.y, 26, 20); }
        }
        public float H, S, V;
        public Texture2D SVPicker;
        public Action<Color> onColorChanged;
        public bool clickingH, clickingSV;

        public static Texture2D HuePicker;

        public static void UpdatePainter(GUIPainter p)
        {
            if (p == null)
                return;
            if (Input.GetMouseButtonDown(1))
            {
                p.showPicker = false;
                return;
            }
            if (p.showPicker)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Vector2 guiMousePos = GUIUtils.MousePos;
                    if (!p.pickerRect.Contains(guiMousePos) && !p.sampleRect.Contains(guiMousePos))
                    {
                        p.clickingSV = false;
                        p.clickingH = false;
                        p.showPicker = false;
                        return;
                    }
                    if (new Rect(p.pickerPosition + new Vector2(5, 5), new Vector2(180, 180)).Contains(guiMousePos))
                    {
                        p.clickingSV = true;
                        ProceduralObjectsLogic.PlaySound(3);
                    }
                    else if (new Rect(p.pickerPosition + new Vector2(195, 5), new Vector2(25, 180)).Contains(guiMousePos))
                    {
                        p.clickingH = true;
                        ProceduralObjectsLogic.PlaySound(3);
                    }
                }
            }
            if (p.clickingSV)
                p.PickupSVApply();
            else if (p.clickingH)
                p.PickupHueApply();
            if (Input.GetMouseButtonUp(0))
            {
                p.clickingH = false;
                p.clickingSV = false;
            }
        }
        public static void UpdatePainter(GUIPainter p, Action onClosePicker)
        {
            if (p == null)
                return;
            if (Input.GetMouseButtonDown(1))
            {
                onClosePicker.Invoke();
                return;
            }
            if (p.showPicker)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Vector2 guiMousePos = GUIUtils.MousePos;
                    if (!p.pickerRect.Contains(guiMousePos) && !p.sampleRect.Contains(guiMousePos))
                    {
                        p.clickingSV = false;
                        p.clickingH = false;
                        onClosePicker.Invoke();
                        return;
                    }
                    if (new Rect(p.pickerPosition + new Vector2(5, 5), new Vector2(180, 180)).Contains(guiMousePos))
                    {
                        p.clickingSV = true;
                        ProceduralObjectsLogic.PlaySound(3);
                    }
                    else if (new Rect(p.pickerPosition + new Vector2(195, 5), new Vector2(25, 180)).Contains(guiMousePos))
                    {
                        p.clickingH = true;
                        ProceduralObjectsLogic.PlaySound(3);
                    }
                }
            }
            if (p.clickingSV)
                p.PickupSVApply();
            else if (p.clickingH)
                p.PickupHueApply();
            if (Input.GetMouseButtonUp(0))
            {
                p.clickingH = false;
                p.clickingSV = false;
            }
        }

        public static GUIPainter DrawPainter(GUIPainter painter, Vector2 samplePosition, Vector2 pickerPosition, Color color, Action<Color> onColorChanged, Action onSampleClick)
        {
            if (painter == null)
            {
                painter = new GUIPainter()
                {
                    showPicker = false,
                    samplePosition = samplePosition,
                    pickerPosition = pickerPosition,
                    clickingH = false,
                    clickingSV = false,
                    onColorChanged = onColorChanged
                };
                float h, s, v;
                Color.RGBToHSV(color, out h, out s, out v);
                painter.H = h;
                painter.S = s;
                painter.V = v;
                painter.GenerateSVPicker();
            }
            /* float nh, ns, nv;
            Color.RGBToHSV(color, out nh, out ns, out nv);
               painter.H = nh;
               painter.S = ns;
               painter.V = nv; */
            // painter.onColorChanged = onColorChanged;
            GUI.color = new Color(color.r, color.g, color.b);
            if (GUI.Button(new Rect(samplePosition, new Vector2(26, 20)), "████"))
            {
                ProceduralObjectsLogic.PlaySound();
                if (painter.showPicker == false)
                {
                    float nh, ns, nv;
                    Color.RGBToHSV(color, out nh, out ns, out nv);
                    painter.H = nh;
                    painter.S = ns;
                    painter.V = nv;
                    painter.GenerateSVPicker();
                }
                painter.showPicker = !painter.showPicker;
                onSampleClick.Invoke();
            }
            GUI.color = Color.white;
            if (painter.showPicker)
            {
                DrawPicker(painter, color);
            }
            painter.samplePosition = samplePosition;
            painter.pickerPosition = pickerPosition;
            return painter;
        }

        public static GUIPainter DrawPainterSampleOnly(GUIPainter painter, Vector2 samplePosition, Color color, Action<Color> onColorChanged, Action onSampleClick)
        {
            if (painter == null)
            {
                painter = new GUIPainter()
                {
                    showPicker = true,
                    samplePosition = samplePosition,
                    clickingH = false,
                    clickingSV = false
                };
                float h, s, v;
                Color.RGBToHSV(color, out h, out s, out v);
                painter.H = h;
                painter.S = s;
                painter.V = v;
                painter.GenerateSVPicker();
            }
            painter.onColorChanged = onColorChanged;
            /* float nh, ns, nv;
            Color.RGBToHSV(color, out nh, out ns, out nv);
               painter.H = nh;
               painter.S = ns;
               painter.V = nv; */
            // painter.onColorChanged = onColorChanged;
            GUI.color = new Color(color.r, color.g, color.b);
            if (GUI.Button(new Rect(samplePosition, new Vector2(26, 20)), "████"))
            {
                ProceduralObjectsLogic.PlaySound();
                    float nh, ns, nv;
                    Color.RGBToHSV(color, out nh, out ns, out nv);
                    painter.H = nh;
                    painter.S = ns;
                    painter.V = nv;
                    painter.GenerateSVPicker();
                painter.showPicker = true;
                onSampleClick.Invoke();
            }
            GUI.color = Color.white;
            painter.samplePosition = samplePosition;
            return painter;
        }
        public static void DrawPicker(GUIPainter painter, Color color)
        {
            GUI.BeginGroup(painter.pickerRect);
            GUI.Box(new Rect(0, 0, 225, 190), string.Empty);
            GUI.DrawTexture(new Rect(5, 5, 180, 180), painter.SVPicker);
            GUI.DrawTexture(new Rect(180f * painter.S - 2, 178 - painter.V * 180f, 14, 14), ProceduralObjectsMod.Icons[10]);
            GUI.DrawTexture(new Rect(195, 5, 25, 180), HuePicker);
            GUI.DrawTexture(new Rect(195, 183 - painter.H * 180f, 25, 4), ProceduralObjectsMod.Icons[11]);

            if (GUI.Button(new Rect(0, 192, 85, 22), LocalizationManager.instance.current["colorPicker_store"]))
            {
                ProceduralObjectsLogic.PlaySound();
                RegisterColor(color);
            }
            GUI.color = SavedColors[0];
            if (GUI.Button(new Rect(new Rect(87, 192, 26, 20)), "████"))
                painter.SetColor(SavedColors[0]);
            GUI.color = SavedColors[1];
            if (GUI.Button(new Rect(new Rect(115, 192, 26, 20)), "████"))
                painter.SetColor(SavedColors[1]);
            GUI.color = SavedColors[2];
            if (GUI.Button(new Rect(new Rect(143, 192, 26, 20)), "████"))
                painter.SetColor(SavedColors[2]);
            GUI.color = SavedColors[3];
            if (GUI.Button(new Rect(new Rect(171, 192, 26, 20)), "████"))
                painter.SetColor(SavedColors[3]);
            GUI.color = SavedColors[4];
            if (GUI.Button(new Rect(new Rect(199, 192, 26, 20)), "████"))
                painter.SetColor(SavedColors[4]);
            GUI.color = Color.white;
            GUI.EndGroup();
        }

        private void SetColor(Color color)
        {
            ProceduralObjectsLogic.PlaySound();
            float h, s, v;
            Color.RGBToHSV(color, out h, out s, out v);
            this.H = h;
            this.GenerateSVPicker();
            this.S = s;
            this.V = v;
            onColorChanged.Invoke(Color.HSVToRGB(this.H, this.S, this.V));
        }

        public static void GenerateHuePicker()
        {
            if (HuePicker != null)
                return;
            HuePicker = new Texture2D(25, 180);
            for (int y = 0; y < 180; y++)
            {
                Color c = Color.HSVToRGB(((float)y) / 180f, 1f, 1f);
                for (int xtex = 0; xtex < 25; xtex++)
                {
                    HuePicker.SetPixel(xtex, y, c);
                }
            }
            HuePicker.Apply();
        }
        public void GenerateSVPicker()
        {
            //Color hueSelected = new Color(.3f, .2f, .9f, 1f);
            Texture2D oldTex = null;
            if (SVPicker != null)
                oldTex = SVPicker;
            SVPicker = new Texture2D(180, 180);
            for (int x = 0; x < 180; x++)
            {
              //  Color saturated = new Color(((hueSelected.r - 1f) / 180) * x + 1, ((hueSelected.g - 1f) / 180) * x + 1, ((hueSelected.b - 1f) / 180) * x + 1, 1f);
                for (int y = 0; y < 180; y++)
                {
                    if (y == 179)
                        SVPicker.SetPixel(x, y, Color.black);
                    else
                        SVPicker.SetPixel(x, y, Color.HSVToRGB(H, ((float)x) / 180f, ((float)y) / 180f));
                }
            }
            SVPicker.Apply();
            if (oldTex != null)
                oldTex.DisposeTexFromMemory();
        }

        public void PickupHueApply()
        {
            var localHueMousePos = GUIUtils.MousePos - pickerPosition - new Vector2(195, 5);
            float yOffset = 180 - localHueMousePos.y;
            H = Mathf.Clamp(yOffset / 180f, 0f, 1f);
            GenerateSVPicker();
            onColorChanged.Invoke(Color.HSVToRGB(H, S, V));
        }
        public void PickupSVApply()
        {
            var localSVMousePos = GUIUtils.MousePos - pickerPosition - new Vector2(5, 5);
            S = Mathf.Clamp(localSVMousePos.x / 180f, 0f, 1f);
            V = Mathf.Clamp((180f - localSVMousePos.y) / 180f, 0f, 1f);
            onColorChanged.Invoke(Color.HSVToRGB(H, S, V));
        }

        public static Color[] SavedColors = new Color[] { Color.white, Color.white, Color.white, Color.white, Color.white };
        public static void RegisterColor(Color c)
        {
            SavedColors = new Color[] { c, SavedColors[0], SavedColors[1], SavedColors[2], SavedColors[3] };
        }
    }
}
