using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using ColossalFramework;
using UnityEngine;

using ColossalFramework.PlatformServices;

using ProceduralObjects.Classes;

namespace ProceduralObjects.UI
{
    public static class GUIUtils
    {
        private static Texture2D[] buttons = new Texture2D[]
        {
            TextureUtils.LoadTextureFromAssembly("close"),
            TextureUtils.LoadTextureFromAssembly("help")
        };

        /*
        // From ModTools by BloodyPenguin 
        // https://github.com/bloodypenguin/Skylines-ModTools

        public static void SetMouseScrolling(bool isEnabled)
        {
            try
            {
                var mouseWheelZoom = GetPrivate<SavedBool>(ToolsModifierControl.cameraController, "m_mouseWheelZoom");
                if (mouseWheelZoom.value != isEnabled)
                {
                    mouseWheelZoom.value = isEnabled;
                }
            }
            catch (Exception)
            {
            }
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
         * */
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

        public static bool CloseHelpButtons(Rect window, string wikiHelpPage)
        {
            return CloseHelpButtonsURL(window, ProceduralObjectsMod.DOCUMENTATION_URL + wikiHelpPage);
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
                PlatformService.ActivateGameOverlayToWebPage(ProceduralObjectsMod.DOCUMENTATION_URL + wikiHelpPage);
            }
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
            public float returnValue;
        }
    }
}
