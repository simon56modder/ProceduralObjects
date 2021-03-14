using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

using ColossalFramework;
using ColossalFramework.PlatformServices;
using ColossalFramework.UI;

using ProceduralObjects.Classes;
using ProceduralObjects.Localization;

namespace ProceduralObjects.UI
{
    public static class GUIUtils
    {
        private static Texture2D[] buttons = new Texture2D[]
        {
            TextureUtils.LoadTextureFromAssembly("close"),
            TextureUtils.LoadTextureFromAssembly("help")
        };

        public static void SetMouseScroll(bool toState)
        {
            if (toState && !isMouseScrollEnabled)
            {
                SetMouseScrolling(true);
                isMouseScrollEnabled = true;
            }
            else if (isMouseScrollEnabled && !toState)
            {
                SetMouseScrolling(false);
                isMouseScrollEnabled = false;
            }
        }
        private static bool isMouseScrollEnabled = true;
        private static SavedBool mouseWheelZoom;
        // From ModTools by BloodyPenguin 
        // https://github.com/bloodypenguin/Skylines-ModTools
        private static void SetMouseScrolling(bool isEnabled)
        {
            try
            {
                if (mouseWheelZoom == null)
                    mouseWheelZoom = GetPrivate<SavedBool>(ToolsModifierControl.cameraController, "m_mouseWheelZoom");
                if (mouseWheelZoom.value != isEnabled)
                    mouseWheelZoom.value = isEnabled;
            }
            catch (Exception) { }
        }
        private static Q GetPrivate<Q>(object o, string fieldName)
        {
            var fields = o.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo field = null;

            foreach (var f in fields)
            {
                if (f.Name == fieldName)
                {
                    field = f;
                    break;
                }
            }

            return (Q)field.GetValue(o);
        }
        public static Rect ClampRectToScreen(Rect source)
        {
            var rect = new Rect(source);
            if (rect.width > Screen.width || rect.height > Screen.height)
                return rect;
            if (rect.x < 0)
                rect.x = 0;
            if (rect.y < 0)
                rect.y = 0;
            if (rect.x + rect.width > Screen.width)
                rect.x = Screen.width - rect.width;
            if (rect.y + rect.height > Screen.height)
                rect.y = Screen.height - rect.height;
            return rect;
        }
        public static Rect Window(int id, Rect clientRect, GUI.WindowFunction function, string title)
        {
            if (!ProceduralObjectsMod.UseUINightMode.value)
                GUI.DrawTexture(new Rect(clientRect.x + 2, clientRect.y + 2, clientRect.width - 4, clientRect.height - 4), bckgTex, ScaleMode.StretchToFill);
            return GUI.Window(id, clientRect, function, title);
        }
        public static Rect RectFromCorners(Vector2 topLeftCorner, Vector2 bottomRightCorner, bool fixInversed)
        {
            if (fixInversed)
            {
                if (bottomRightCorner.x >= topLeftCorner.x && bottomRightCorner.y >= topLeftCorner.y)
                    return new Rect(topLeftCorner, new Vector2(bottomRightCorner.x - topLeftCorner.x, bottomRightCorner.y - topLeftCorner.y));

                else if (bottomRightCorner.x < topLeftCorner.x && bottomRightCorner.y > topLeftCorner.y)
                    return new Rect(bottomRightCorner.x, topLeftCorner.y, topLeftCorner.x - bottomRightCorner.x, bottomRightCorner.y - topLeftCorner.y);

                else if (bottomRightCorner.x < topLeftCorner.x && bottomRightCorner.y < topLeftCorner.y)
                    return new Rect(bottomRightCorner.x, bottomRightCorner.y, topLeftCorner.x - bottomRightCorner.x, topLeftCorner.y - bottomRightCorner.y);

                else if (bottomRightCorner.x > topLeftCorner.x && bottomRightCorner.y < topLeftCorner.y)
                    return new Rect(topLeftCorner.x, bottomRightCorner.y, bottomRightCorner.x - topLeftCorner.x, topLeftCorner.y - bottomRightCorner.y);
            }
            return new Rect(topLeftCorner, new Vector2(bottomRightCorner.x - topLeftCorner.x, bottomRightCorner.y - topLeftCorner.y));
        }

        public static bool IsMouseInside(this Rect rect)
        {
            return rect.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));
        }
        public static Vector2 MousePos
        {
            get
            {
                return new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            }
        }
        public static Vector2 RightMostPosition(params Vector2[] positions)
        {
            Vector2 rightMost = positions[0];
            for (int i = 1; i < positions.Length; i++)
            {
                if (rightMost.x < positions[i].x)
                    rightMost = positions[i];
            }
            return rightMost;
        }

        public static void ShowModal(string title, string desc, Action<bool> returnMethod)
        {
            var logic = ProceduralObjectsLogic.instance;
            if (logic == null)
                return;
            logic.generalShowUI = false;
            ConfirmPanel.ShowModal(title, desc, delegate(UIComponent comp, int r)
            {
                returnMethod.Invoke(r == 1);
                logic.generalShowUI = true;
                if (!UIView.HasFocus(logic.mainButton))
                    UIView.SetFocus(logic.mainButton);
            });
        }
        public static bool TextureSelector(Rect rect, ref Vector2 scrolling, out Texture output)
        {
            scrolling = GUI.BeginScrollView(rect, scrolling, new Rect(0, 0, rect.width - 23, TextureManager.instance.GetShownHeight()));
            GUI.Label(new Rect(0, 0, rect.width - 25, 32), TextureManager.instance.TotalTexturesCount.ToString() + " " + LocalizationManager.instance.current["tex_in_total"] + " : " + TextureManager.instance.LocalTexturesCount.ToString() + " " + LocalizationManager.instance.current["local"] + " + " + (TextureManager.instance.TotalTexturesCount - TextureManager.instance.LocalTexturesCount) + " " + LocalizationManager.instance.current["from_wk"]);
            if (GUI.Button(new Rect(0, 34, (rect.width - 35) / 2, 30), LocalizationManager.instance.current["open_folder"]))
            {
                ProceduralObjectsLogic.PlaySound();
                if (Directory.Exists(ProceduralObjectsMod.TextureConfigPath))
                    Application.OpenURL("file://" + ProceduralObjectsMod.TextureConfigPath);
            }
            if (GUI.Button(new Rect((rect.width - 35) / 2 + 10, 34, (rect.width - 35) / 2, 30), LocalizationManager.instance.current["refresh"]))
            {
                ProceduralObjectsLogic.PlaySound();
                TextureManager.instance.LoadTextures();
            }
            if (GUI.Button(new Rect(0, 65, rect.width - 25, 79), LocalizationManager.instance.current["none_defaulttex"]))
            {
                ProceduralObjectsLogic.PlaySound();
                output = null;
                GUI.EndScrollView();
                return true;
            }
            int y = 144;
            for (int i = 0; i < TextureManager.instance.TextureResources.Count; i++)
            {
                DrawSeparator(new Vector2(2, y), rect.width - 29, 1f);
                var texResource = TextureManager.instance.TextureResources[i];
                if (GUI.Button(new Rect(2, y + 3, 27, 25), texResource.minimized ? "►" : "▼"))
                    texResource.minimized = !texResource.minimized;
                GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                GUI.Label(new Rect(32, y + 4, rect.width - 58, 24), texResource.m_name);
                GUI.skin.label.alignment = TextAnchor.MiddleRight;
                GUI.Label(new Rect(32, y + 4, rect.width - 58, 24), "(" + ((texResource.TexturesCount > 1)
                        ? texResource.TexturesCount + " " + LocalizationManager.instance.current["textures"]
                        : texResource.TexturesCount + " " + LocalizationManager.instance.current["texture"]) + ")");
                GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                y += 30;
                if (!texResource.minimized)
                {
                    int j = 0;
                    foreach (var kvp in texResource.m_textures)
                    {
                        float xIndex = j % 3;
                        var rectwidth25 = rect.width - 25f;
                        if (GUI.Button(new Rect(xIndex * (rectwidth25 / 3f), y, (rectwidth25 / 3f) - 5, 79), string.Empty))
                        {
                            ProceduralObjectsLogic.PlaySound();
                            output = kvp.Value;
                            GUI.EndScrollView();
                            GUI.skin.label.alignment = TextAnchor.UpperLeft;
                            return true;
                        }
                        GUI.DrawTexture(new Rect(xIndex * (rectwidth25 / 3f) + 2, y + 1, (rectwidth25 / 3f) - 8, 55), kvp.Key, ScaleMode.ScaleToFit);
                        int pos = Mathf.Max(kvp.Value.name.LastIndexOf("/"), kvp.Value.name.LastIndexOf(@"\")) + 1;
                        GUI.Label(new Rect(xIndex * (rectwidth25 / 3f) + 2, y + 56.5f, (rectwidth25 / 3f) - 8, 22), kvp.Value.name.Substring(pos, kvp.Value.name.Length - pos));
                        if (xIndex == 2f || j == texResource.TexturesCount - 1)
                            y += 80;
                        j++;
                    }
                }
                GUI.skin.label.alignment = TextAnchor.UpperLeft;
            }
            GUI.EndScrollView();
            output = null;
            return false;
        }
        public static bool CloseHelpButtons(Rect window, string wikiHelpPage)
        {
            return CloseHelpButtonsURL(window, LocalizationManager.instance.current.LocalizedWikiLink(ProceduralObjectsMod.DOCUMENTATION_URL + wikiHelpPage));
        }
        public static bool CloseHelpButtonsURL(Rect window, string url)
        {
            if (GUI.Button(new Rect(window.width - 51, 2, 24, 24), buttons[1], GUI.skin.label))
            {
                ProceduralObjectsLogic.PlaySound();
                PlatformService.ActivateGameOverlayToWebPage(url);
            }
            return CloseButton(window);
        }
        public static bool CloseButton(Rect window)
        {
            if (GUI.Button(new Rect(window.width - 25, 2, 24, 24), buttons[0], GUI.skin.label))
            {
                ProceduralObjectsLogic.PlaySound();
                return true;
            }
            return false;
        }
        public static void HelpButton(float windowWidth, string wikiHelpPage)
        {
            if (GUI.Button(new Rect(windowWidth - 25, 2, 24, 24), buttons[1], GUI.skin.label))
            {
                ProceduralObjectsLogic.PlaySound();
                PlatformService.ActivateGameOverlayToWebPage(LocalizationManager.instance.current.LocalizedWikiLink(ProceduralObjectsMod.DOCUMENTATION_URL + wikiHelpPage));
            }
        }
        public static void HelpButton(Rect position, string wikiHelpPage)
        {
            if (GUI.Button(new Rect(position.x, position.y, 24, 24), buttons[1], GUI.skin.label))
            {
                ProceduralObjectsLogic.PlaySound();
                PlatformService.ActivateGameOverlayToWebPage(LocalizationManager.instance.current.LocalizedWikiLink(ProceduralObjectsMod.DOCUMENTATION_URL + wikiHelpPage));
            }
        }
        public static float HorizontalSliderIncrements(Rect rect, float value, params float[] values)
        {
            var count = values.Length;
            int currentIndex;
            if (values.Contains(value))
                currentIndex = Array.IndexOf<float>(values, value);
            else
                currentIndex = 0;
            return values[Mathf.RoundToInt(GUI.HorizontalSlider(rect, currentIndex, 0, count - 1))];
        }
        public static void DrawSeparator(Vector2 leftEndPos, float length, float thickness = 1.5f)
        {
            GUI.DrawTexture(new Rect(leftEndPos.x, leftEndPos.y, length, thickness), greyTex, ScaleMode.StretchToFill);
        }
        public static void Setup()
        {
            greyTex = TextureUtils.PlainTexture(2, 2, new Color(.7f, .7f, .7f, 1f));
            bckgTex = TextureUtils.PlainTexture(2, 2, new Color(1f, 1f, 1f, 0.46f));
        }
        private static Texture2D greyTex;
        public static Texture2D bckgTex = TextureUtils.PlainTexture(2, 2, new Color(1f, 1f, 1f, 0.46f));

        public static string GetStringBetween(this string source, string from, string to)
        {
            int pFrom = source.IndexOf(from) + from.Length;
            return source.Substring(pFrom, source.LastIndexOf(to) - pFrom);
        }
        public static string GetUntilOrEmpty(this string text, string stopAt)
        {
            if (text != "")
            {
                int charLocation = text.IndexOf(stopAt, StringComparison.Ordinal);
                if (charLocation > 0)
                    return text.Substring(0, charLocation);
            }
            return string.Empty;
        }
        public static string GetStringAfter(this string source, string str)
        {
            return source.Substring(source.IndexOf(str) + str.Length);
        }
        public static char GetLastChar(this string source)
        {
            return source.Substring(source.Length - 1)[0];
        }
        public static string[] GetFirstLinesOfFile(string path, int firstN)
        {
            List<string> lines = new List<string>();
            if (!File.Exists(path))
                return lines.ToArray();
            StreamReader sr = new StreamReader(path);
            for (int i = 0; i < firstN; i++)
            {
                lines.Add(sr.ReadLine());
            }
            sr.Close();
            return lines.ToArray();
        }

        public class FloatInputField
        {
            public FloatInputField(float value)
            {
                inputString = value.ToString();
                returnValue = value;
            }

            private string inputString;
            public void SetString(string s, bool allowNegatives)
            {
                s = s.Trim().Replace(",", ".");
                if (!allowNegatives)
                    s.Replace("-", "");
                float f;
                if (s == "" || s == string.Empty)
                {
                    returnValue = 0;
                    inputString = s;
                    return;
                }
                if (!float.TryParse(s.Trim(), out f))
                    return;

                try
                {
                    inputString = s;
                    if (s == "-")
                        returnValue = 0;
                    else if (s == "-0" || s == "-0.")
                        returnValue = 0;
                    else if (s.Last() == '.')
                        returnValue = float.Parse(s.TrimEnd('.'));
                    else
                        returnValue = float.Parse(s);
                }
                catch
                {
                    inputString = returnValue.ToString();
                }
            }
            public FloatInputField DrawField(Rect rect, float value, bool allowNegatives = true)
            {
                var str = GUI.TextField(rect, inputString);
                if (returnValue != value)
                {
                    returnValue = value;
                    inputString = value.ToString();
                }
                else
                    SetString(str, allowNegatives);
                return this;
            }
            public FloatInputField DrawField(Rect rect, float value, bool allowNegatives, float sliderMin, float sliderMax, bool showMaxButton, float maxValue)
            {
                var str = GUI.TextField(new Rect(rect.x, rect.y, rect.width - 40, rect.height - 14), inputString);
                if (returnValue != value)
                {
                    returnValue = value;
                    inputString = value.ToString();
                }
                else
                    SetString(str, allowNegatives);

                var clampedReturn = Mathf.Clamp(returnValue, sliderMin, sliderMax);
                var slider = GUI.HorizontalSlider(new Rect(rect.x, rect.yMax - 12, rect.width - 40, 12), clampedReturn, sliderMin, sliderMax);
                if (slider != clampedReturn)
                {
                    returnValue = Mathf.Round(slider);
                    inputString = returnValue.ToString();
                }
                if (showMaxButton)
                {
                    if (GUI.Button(new Rect(rect.xMax - 38, rect.y, 38, rect.height - 14), LocalizationManager.instance.current["max_reached"]))
                    {
                        ProceduralObjectsLogic.PlaySound();
                        returnValue = Mathf.Round(maxValue);
                        inputString = returnValue.ToString();
                    }
                }
                return this;
            }
            public float returnValue;
        }
    }
}
