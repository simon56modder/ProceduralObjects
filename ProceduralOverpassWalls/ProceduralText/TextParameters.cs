using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using ProceduralObjects.Classes;
using ProceduralObjects.Localization;

namespace ProceduralObjects.ProceduralText
{
    [Serializable]
    public class TextParameters
    {
        public TextParameters()
        {
            m_textFields = new List<TextField>();
        }

        public List<TextField> m_textFields;

        public TextField this[int index]
        {
            get
            {
                return m_textFields[index];
            }
            set
            {
                m_textFields[index] = value;
            }
        }
        public TextField AddField(TextureFont defaultFont)
        {
            if (m_textFields == null)
                m_textFields = new List<TextField>();
            TextField field = new TextField();
            field.SetFont(defaultFont);
            m_textFields.Add(field);
            return field;
        }
        public TextField AddField(TextField field)
        {
            if (m_textFields == null)
                m_textFields = new List<TextField>();
            m_textFields.Add(field);
            return field;
        }
        public void SetFonts()
        {
            for (int i = 0; i < m_textFields.Count; i++)
            {
                if (m_textFields[i].m_font == null)
                    m_textFields[i].SetFont(m_textFields[i].m_fontName, FontManager.instance);
            }
        }
        public Texture2D ApplyParameters(Texture2D original)
        {
            var tex = (Texture2D)GameObject.Instantiate(original);
            for (int i = 0; i < m_textFields.Count; i++)
            {
                if (m_textFields[i].m_text != "")
                    m_textFields[i].Apply(tex);
            }
            return tex;
        }
        public static bool CanHaveTextParameters(ProceduralObject obj)
        {
            return !obj.isPloppableAsphalt;
        }
        public int Count()
        {
            if (m_textFields == null)
                return 0;
            return m_textFields.Count;
        }
        public void RemoveField(TextField f)
        {
            if (m_textFields == null)
                return;
            if (m_textFields.Contains(f))
                m_textFields.Remove(f);
        }
        public static TextParameters Clone(TextParameters paramSource, bool useFontname)
        {
            if (paramSource == null)
                return null;
            var param = new TextParameters();
            for (int i = 0; i < paramSource.Count(); i++)
            {
                param.m_textFields.Add(TextField.Clone(paramSource.m_textFields[i], useFontname));
            }
            return param;
        }
        public static bool IsDifference(TextParameters A, TextParameters B)
        {
            if (A.Count() != B.Count())
                return true;
            for (int i = 0; i < A.Count(); i++)
            {
                if (TextField.IsDifference(A.m_textFields[i], B.m_textFields[i]))
                    return true;
            }
            return false;
        }
    }

    [Serializable]
    public class TextField
    {
        public TextField()
        {
            m_text = "";
            m_fontSize = 20;
            m_spacing = 2;
            m_style = FontStyle.Normal;
            m_fontColor = Color.white;
            x = 0f;
            y = 0f;
            m_scaleX = 1f;
            m_scaleY = 1f;
            minimized = false;
        }

        public string m_text, m_fontName;
        public uint m_spacing, m_fontSize;
        public FontStyle m_style;
        public float x, y, m_scaleX, m_scaleY;
        // used as a 4 components item (RGBA), only for serialization
        public SerializableQuaternion serializableColor;
        [NonSerialized]
        public Color m_fontColor;
        [NonSerialized]
        public TextureFont m_font;
        [NonSerialized]
        public int texWidth;
        [NonSerialized]
        public int texHeight;
        [NonSerialized]
        public bool minimized;

        public void DrawUI(Vector2 position, TextCustomizationManager textManager, bool showDelete)
        {
            Rect rect = new Rect(position.x, position.y, 350, minimized ? 31 : 123);
            GUI.Box(rect, string.Empty);
            GUI.BeginGroup(rect);
            m_text = GUI.TextField(new Rect(3, 3, 268, 25), m_text);

            if (!minimized)
            {
                GUI.Label(new Rect(4, 26, 75, 22), "<size=12>" + LocalizationManager.instance.current["font_size"] + " : " + m_fontSize.ToString() + "</size>");
                m_fontSize = (uint)Mathf.FloorToInt(GUI.HorizontalSlider(new Rect(80, 32, 113, 25), m_fontSize, 5, 100));

                GUI.Label(new Rect(4, 47, 90, 22), "<size=12>" + LocalizationManager.instance.current["font_spacing"] + " : " + m_spacing.ToString() + "</size>");
                m_spacing = (uint)Mathf.FloorToInt(GUI.HorizontalSlider(new Rect(95, 53, 98, 25), m_spacing, 0, 9));

                if (!m_font.m_disableColorOverwriting)
                {
                    GUI.Label(new Rect(205, 28, 75, 22), "<size=12>" + LocalizationManager.instance.current["font_color"] + " :</size>");
                    m_fontColor.r = GUI.HorizontalSlider(new Rect(205, 51, 80, 18), m_fontColor.r, 0f, 1f);
                    m_fontColor.g = GUI.HorizontalSlider(new Rect(205, 68, 80, 18), m_fontColor.g, 0f, 1f);
                    m_fontColor.b = GUI.HorizontalSlider(new Rect(205, 85, 80, 18), m_fontColor.b, 0f, 1f);
                    GUI.Label(new Rect(289, 47, 75, 22), "<size=12>" + LocalizationManager.instance.current["rgb_r"] + " : " + (int)(m_fontColor.r * 255f) + "</size>");
                    GUI.Label(new Rect(289, 64, 75, 22), "<size=12>" + LocalizationManager.instance.current["rgb_g"] + " : " + (int)(m_fontColor.g * 255f) + "</size>");
                    GUI.Label(new Rect(289, 81, 75, 22), "<size=12>" + LocalizationManager.instance.current["rgb_b"] + " : " + (int)(m_fontColor.b * 255f) + "</size>");
                }

                if (textManager.fontManager.previousFontExists(m_font))
                {
                    if (GUI.Button(new Rect(4, 68, 26, 25), "◄"))
                        SetFont(textManager.fontManager.GetPreviousFont(m_font));
                }
                else
                {
                    GUI.skin.box.normal.textColor = TextCustomizationManager.inactiveGrey;
                    GUI.Label(new Rect(4, 68, 26, 25), "◄", GUI.skin.box);
                    GUI.skin.box.normal.textColor = Color.white;
                }
                GUI.Label(new Rect(30, 68, 147, 25), m_fontName, GUI.skin.button);
                if (textManager.fontManager.nextFontExists(m_font))
                {
                    if (GUI.Button(new Rect(177, 68, 26, 25), "►"))
                        SetFont(textManager.fontManager.GetNextFont(m_font));
                }
                else
                {
                    GUI.skin.box.normal.textColor = TextCustomizationManager.inactiveGrey;
                    GUI.Label(new Rect(177, 68, 26, 25), "►", GUI.skin.box);
                    GUI.skin.box.normal.textColor = Color.white;
                }
                if (m_font.m_boldExists && m_font.m_italicExists)
                    m_style = TextCustomizationManager.IntToStyle(GUI.Toolbar(new Rect(4, 95, 91, 25), TextCustomizationManager.SelectedStyle(m_style), 
                        new string[] { LocalizationManager.instance.current["textStyle_normal"], "<b>" + LocalizationManager.instance.current["textStyle_bold"] + "</b>", "<i>"+ LocalizationManager.instance.current["textStyle_italic"] + "</i>" }));

                GUI.Label(new Rect(98, 98, 67, 26), LocalizationManager.instance.current["scale"] + " :");

                if (GUI.Button(new Rect(165, 98, 10, 26), "X", GUI.skin.label))
                    m_scaleX = 1f;
                m_scaleX = GUI.HorizontalSlider(new Rect(176, 102, 78, 18), m_scaleX, 0.1f, 5f);
                if (GUI.Button(new Rect(255, 98, 10, 26), "Y", GUI.skin.label))
                    m_scaleY = 1f;
                m_scaleY = GUI.HorizontalSlider(new Rect(266, 102, 78, 18), m_scaleY, 0.1f, 5f);
            }
            if (GUI.Button(new Rect(272, 3, 25, 25), ProceduralObjectsMod.Icons[0]))
            {
                textManager.parameters.AddField(TextField.Clone(this, false));
            }
            if (GUI.Button(new Rect(298, 3, 25, 25), ProceduralObjectsMod.Icons[minimized ? 1 : 2]))
            {
                minimized = !minimized;
            }
            if (showDelete)
            {
                GUI.color = Color.red;
                if (GUI.Button(new Rect(324, 3, 25, 25), "X"))
                    textManager.parameters.RemoveField(this);
                GUI.color = Color.white;
            }
            GUI.EndGroup();
        }
        public void SetFont(TextureFont font)
        {
            this.m_font = font;
            this.m_fontName = font.m_fontName;
            this.m_spacing = font.m_defaultSpacing;
            if (!font.m_boldExists || !font.m_italicExists)
                m_style = FontStyle.Normal;
        }
        public void SetFont(string fontName, FontManager manager)
        {
            var font = manager.m_fonts.FirstOrDefault(f => f.m_fontName == fontName);
            this.m_font = font;
            this.m_fontName = font.m_fontName;
            this.m_spacing = font.m_defaultSpacing;
            if (!font.m_boldExists || !font.m_italicExists)
                m_style = FontStyle.Normal;
        }
        public static TextField Clone(TextField fieldSource, bool useFontname)
        {
            var field = new TextField();
            field.m_text = fieldSource.m_text;
            field.m_fontSize = fieldSource.m_fontSize;
            field.m_style = fieldSource.m_style;
            field.x = fieldSource.x;
            field.y = fieldSource.y;
            field.m_fontColor = fieldSource.m_fontColor;
            field.minimized = fieldSource.minimized;
            field.m_scaleX = fieldSource.m_scaleX;
            field.m_scaleY = fieldSource.m_scaleY;
            if (useFontname)
                field.SetFont(fieldSource.m_fontName, FontManager.instance);
            else
                field.SetFont(fieldSource.m_font);
            field.m_spacing = fieldSource.m_spacing;
            return field;
        }
        public static bool IsDifference(TextField A, TextField B)
        {
            if (A.x != B.x)
                return true;
            if (A.y != B.y)
                return true;
            if (A.m_text != B.m_text)
                return true;
            if (A.m_fontSize != B.m_fontSize)
                return true;
            if (A.m_fontColor != B.m_fontColor)
                return true;
            if (A.m_spacing != B.m_spacing)
                return true;
            if (A.m_style != B.m_style)
                return true;
            if (A.m_font != B.m_font)
                return true;
            if (A.m_scaleX != B.m_scaleX)
                return true;
            if (A.m_scaleY != B.m_scaleY)
                return true;
            return false;
        }
        public static string SaveString(TextField field)
        {
            return "textParam: m_font:" + field.m_fontName + " m_size:" + field.m_fontSize.ToString() + " m_spacing:" + field.m_spacing.ToString() + " m_style:" +
                            TextCustomizationManager.SelectedStyle(field.m_style).ToString() + " m_x:" + field.x.ToString() + " m_y:" + field.y.ToString() + " m_scaleX:" + field.m_scaleX.ToString() + " m_scaleY:" + field.m_scaleY.ToString() +
                            " m_fontcolor:" + field.m_fontColor.ToString() + " m_text:" + field.m_text;
        }
        public static TextField Parse(string s, FontManager fManager)
        {
            if (!s.Contains("textParam:"))
            {
                throw new FormatException("[ProceduralObjects] TextField Parse : Input string was not in the correct format");
            }
            try
            {
                TextField f = new TextField();
                f.SetFont(s.GetStringBetween(" m_font:", " m_size:"), fManager);
                f.m_fontSize = uint.Parse(s.GetStringBetween(" m_size:", " m_spacing:"));
                f.m_spacing = uint.Parse(s.GetStringBetween(" m_spacing:", " m_style:"));
                f.m_style = TextCustomizationManager.IntToStyle(int.Parse(s.GetStringBetween(" m_style:", " m_x:")));
                f.x = float.Parse(s.GetStringBetween(" m_x:", " m_y:"));
                f.y = float.Parse(s.GetStringBetween(" m_y:", " m_scaleX:"));
                f.m_scaleX = float.Parse(s.GetStringBetween(" m_scaleX:", " m_scaleY:"));
                f.m_scaleY = float.Parse(s.GetStringBetween(" m_scaleY:", " m_fontcolor:"));
                f.m_fontColor = TextureUtils.ParseColor(s.GetStringBetween(" m_fontcolor:", " m_text:").Replace("RGBA", ""));
                f.m_text = s.GetStringAfter(" m_text:");
                return f;
            }
            catch (Exception e)
            {
                Debug.LogError("[ProceduralObjects] Error : " + e.GetType().ToString() + " while parsing a TextField : " + e.Message);
                return new TextField();
            }
        }
        public void Apply(Texture2D texture)
        {
            m_font.PrintString(texture, m_text, m_style, new Vector2(x, y), m_spacing, m_fontSize, m_fontColor, out texWidth, out texHeight, m_scaleX, m_scaleY);
        }
    }
}
