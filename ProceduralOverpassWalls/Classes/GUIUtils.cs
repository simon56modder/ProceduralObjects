using System;
using System.Linq;
using System.Text;
using System.Reflection;
using ColossalFramework;
using UnityEngine;

namespace ProceduralObjects.Classes
{
    public static class GUIUtils
    {
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
        public static string GetStringBetween(this string source, string from, string to)
        {
            int pFrom = source.IndexOf(from) + from.Length;
            return source.Substring(pFrom, source.LastIndexOf(to) - pFrom);
        }
        public static string GetStringAfter(this string source, string str)
        {
            return source.Substring(source.IndexOf(str) + str.Length);
        }
    }
}
