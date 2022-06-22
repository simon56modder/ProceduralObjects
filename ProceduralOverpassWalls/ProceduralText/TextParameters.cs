using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using ProceduralObjects.Classes;
using ProceduralObjects.Localization;
using ProceduralObjects.UI;

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
        public TextField AddField(TextureFont defaultFont, byte type)
        {
            if (m_textFields == null)
                m_textFields = new List<TextField>();
            TextField field = new TextField(type);
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
                if ((m_textFields[i].m_text != "" && m_textFields[i].m_type == 0) || m_textFields[i].m_type == 1)
                    m_textFields[i].Apply(tex);
            }
            return tex;
        }
        public int Count()
        {
            if (m_textFields == null)
                return 0;
            return m_textFields.Count;
        }
        public bool CanFieldMoveUp(TextField f)
        {
            if (!m_textFields.Contains(f))
                return false;
            if (m_textFields.IndexOf(f) == 0)
                return false;
            return true;
        }
        public bool CanFieldMoveDown(TextField f)
        {
            if (!m_textFields.Contains(f))
                return false;
            if (m_textFields.IndexOf(f) == m_textFields.Count - 1)
                return false;
            return true;
        }

        public void RemoveField(TextField f)
        {
            if (m_textFields == null)
                return;
            if (m_textFields.Contains(f))
                m_textFields.Remove(f);
        }
        public void MoveFieldUp(TextField f)
        {
            if (!CanFieldMoveUp(f))
                return;
            var buffer = new List<TextField>(m_textFields);
            var index = buffer.IndexOf(f);
            buffer.Remove(f);
            m_textFields.Clear();
            for (int i = 0; i < buffer.Count; i++)
            {
                if (i == index - 1)
                    m_textFields.Add(f);
                m_textFields.Add(buffer[i]);
            }
        }
        public void MoveFieldDown(TextField f)
        {
            if (!CanFieldMoveDown(f))
                return;
            var buffer = new List<TextField>(m_textFields);
            var index = buffer.IndexOf(f);
            buffer.Remove(f);
            m_textFields.Clear();
            for (int i = 0; i < buffer.Count; i++)
            {
                m_textFields.Add(buffer[i]);
                if (i == index)
                    m_textFields.Add(f);
            }
        }

        public static bool CanHaveTextParameters(ProceduralObject obj)
        {
            return !obj.isPloppableAsphalt;
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
        public static bool IsEmpty(TextParameters param)
        {
            if (param == null)
                return true;
            if (param.Count() == 0)
                return true;
            return false;
        }
    }

    [Serializable]
    public class TextField
    {
        public TextField(byte type)
        {
            m_type = type;
            m_text = "";
            m_fontSize = 20;
            m_spacing = 2;
            m_style = FontStyle.Normal;
            m_fontColor = Color.white;
            borderColor = Color.white;
            x = 0f;
            y = 0f;
            m_scaleX = 1f;
            m_scaleY = 1f;
            locked = false;
        }

        public string m_text, m_fontName;
        public uint m_spacing, m_fontSize, m_width, m_height, borderSize;
        public byte m_rotation, m_type;
        public FontStyle m_style;
        public float x, y, m_scaleX, m_scaleY;
        // formerly used as a 4 components item (RGBA), only for serialization
        public SerializableQuaternion serializableColor;
        public SerializableColor m_fontColor, borderColor;
        [NonSerialized]
        public TextureFont m_font;
        [NonSerialized]
        public int texWidth;
        [NonSerialized]
        public int texHeight;
        [NonSerialized]
        public bool locked;
        [NonSerialized]
        public GUIPainter painter, borderPainter;
        [NonSerialized]
        public GUIUtils.FloatInputField posXfield, posYfield, sizeField, heightField, widthField;
        [NonSerialized]
        public Vector2 scrollFontsPos;
        [NonSerialized]
        public bool expandFontsSelector;

        public bool UIButton(Vector2 pos, TextCustomizationManager textManager, bool showDelete)
        {
            var rect = new Rect(pos, new Vector2(235, 31));
            if (GUI.Button(new Rect(pos.x, pos.y, textManager.parameters.Count() == 1 ? 156 : 130, 31), string.Empty))
            {
                ProceduralObjectsLogic.PlaySound();
                expandFontsSelector = false;
                scrollFontsPos = Vector2.zero;
                return true;
            }
            GUI.BeginGroup(rect);
            if (m_type == 0)
            {
                GUI.Label(new Rect(3, 5, textManager.parameters.Count() == 1 ? 153 : 127, 23), m_text);
            }
            else // if m_type == 1
            {
                GUI.color = m_fontColor;
                GUI.Label(new Rect(2, 4, 25, 24), "██");
                GUI.color = Color.white;
                GUI.Label(new Rect(26, 5, textManager.parameters.Count() == 1 ? 130 : 104, 23), "<i>" + LocalizationManager.instance.current["colorRect"] + "</i>");
            }
            if (GUI.Button(new Rect(textManager.parameters.Count() == 1 ? 158 : 132, 3, 25, 25), ProceduralObjectsMod.Icons[locked ? 8 : 9]))
            {
                ProceduralObjectsLogic.PlaySound();
                locked = !locked;
            }
            if (textManager.parameters.CanFieldMoveUp(this))
            {
                if (GUI.Button(new Rect(158, 15.5f, 25, 12), ProceduralObjectsMod.Icons[7]))
                {
                    ProceduralObjectsLogic.PlaySound();
                    textManager.parameters.MoveFieldUp(this);
                }
            }
            if (textManager.parameters.CanFieldMoveDown(this))
            {
                if (GUI.Button(new Rect(158, 3, 25, 12), ProceduralObjectsMod.Icons[6]))
                {
                    ProceduralObjectsLogic.PlaySound();
                    textManager.parameters.MoveFieldDown(this);
                }
            }
            if (GUI.Button(new Rect(184, 3, 25, 25), ProceduralObjectsMod.Icons[0]))
            {
                ProceduralObjectsLogic.PlaySound();
                textManager.parameters.AddField(TextField.Clone(this, false));
            }

            if (showDelete)
            {
                GUI.color = Color.red;
                if (GUI.Button(new Rect(210, 3, 25, 25), "X"))
                {
                    ProceduralObjectsLogic.PlaySound();
                    if (textManager.selectedField == this)
                        textManager.selectedField = null;
                    textManager.parameters.RemoveField(this);
                }
                GUI.color = Color.white;
            }
            GUI.EndGroup();
            return false;
        }
        public void DrawUI(Rect rect, TextCustomizationManager textManager, Action<TextureFont> openCharTable)
        {
            // GUI.Box(rect, string.Empty);
            GUI.BeginGroup(rect);
            // copy
            if (GUI.Button(new Rect(220, 3, 25, 23), ProceduralObjectsMod.Icons[15]))
            {
                ProceduralObjectsLogic.PlaySound();
                textManager.copiedField = TextField.Clone(this, false);
            }
            if (m_type == 0)
            {
                if (!m_font.m_disableColorOverwriting)
                {
                    painter = GUIPainter.DrawPainterSampleOnly(painter, new Vector2(3, 4), m_fontColor,
                        (c) => { m_fontColor = c; },
                        () =>
                        {
                            if (textManager.colorPickerSelected == painter)
                                textManager.colorPickerSelected = null;
                            else
                                textManager.colorPickerSelected = painter;
                        });
                }
                GUI.SetNextControlName("TextFieldPOTextCustom");
                m_text = GUI.TextField(new Rect(m_font.m_disableColorOverwriting ? 3 : 31, 3, m_font.m_disableColorOverwriting ? 214 : 186, 25), m_text);
            }
            else // if m_type == 1
            {
                painter = GUIPainter.DrawPainterSampleOnly(painter, new Vector2(3, 4), m_fontColor,
                    (c) => { m_fontColor = c.KeepAlphaFrom(m_fontColor); },
                    () =>
                    {
                        if (textManager.colorPickerSelected == painter)
                            textManager.colorPickerSelected = null;
                        else
                            textManager.colorPickerSelected = painter;
                    });
                GUI.Label(new Rect(30, 5, 190, 23), LocalizationManager.instance.current["colorRect"]);
            }


            GUIUtils.DrawSeparator(new Vector2(3, 75), 241);

            if (m_type == 0) // TEXT FIELD
            {
                //size
                GUI.Label(new Rect(3, 80, 95, 22), "<size=12>" + LocalizationManager.instance.current["font_size"] + " : " + m_fontSize.ToString() + "</size>");
                m_fontSize = (uint)Mathf.FloorToInt(GUI.HorizontalSlider(new Rect(100, 85, 140, 25), m_fontSize, 5, 150));

                //spacing
                GUI.Label(new Rect(3, 105, 95, 22), "<size=12>" + LocalizationManager.instance.current["font_spacing"] + " : " + m_spacing.ToString() + "</size>");
                m_spacing = (uint)Mathf.FloorToInt(GUI.HorizontalSlider(new Rect(100, 110, 140, 25), m_spacing, 0, 9));

                //scale
                GUI.Label(new Rect(3, 130, 120, 26), LocalizationManager.instance.current["scale_txt"] + " :");
                if (GUI.Button(new Rect(3, 155, 24, 26), "X :", GUI.skin.label))
                    m_scaleX = 1f;
                m_scaleX = GUI.HorizontalSlider(new Rect(30, 158, 90, 18), m_scaleX, 0.1f, 5f);
                if (GUI.Button(new Rect(125, 155, 24, 26), "Y :", GUI.skin.label))
                    m_scaleY = 1f;
                m_scaleY = GUI.HorizontalSlider(new Rect(150, 158, 90, 18), m_scaleY, 0.1f, 5f);

                //font
                bool supportStyles = m_font.m_boldExists && m_font.m_italicExists;
                if (GUI.Button(new Rect(3, 180, (supportStyles ? 126 : 213), 25), m_fontName))
                {
                    ProceduralObjectsLogic.PlaySound();
                    openCharTable.Invoke(m_font);
                }
                if (GUI.Button(new Rect((supportStyles ? 128 : 215), 180, 26, 25), "▼"))
                {
                    ProceduralObjectsLogic.PlaySound();
                    // SetFont(textManager.fontManager.GetNextFont(m_font));
                    scrollFontsPos = Vector2.zero;
                    expandFontsSelector = !expandFontsSelector;
                }
                if (supportStyles)
                {
                    string[] styles = new string[] { LocalizationManager.instance.current["textStyle_normal"], "<b>" + LocalizationManager.instance.current["textStyle_bold"] + "</b>", "<i>" + LocalizationManager.instance.current["textStyle_italic"] + "</i>" };
                    if (m_font.m_stylesNames != null)
                    {
                        if (m_font.m_stylesNames.Length == 3)
                            styles = m_font.m_stylesNames;
                    }
                    m_style = TextCustomizationManager.IntToStyle(GUI.Toolbar(new Rect(158, 180, 85, 25), TextCustomizationManager.SelectedStyle(m_style), styles));
                }
                if (expandFontsSelector)
                {
                    TextureFont fontSelected;
                    if (FontManager.instance.FontSelector(new Rect(3, 207, 240, 160), scrollFontsPos, out fontSelected, out scrollFontsPos))
                    {
                        SetFont(fontSelected);
                        expandFontsSelector = false;
                        scrollFontsPos = Vector2.zero;
                    }
                }
            }
            else if (m_type == 1) // COLOR RECT
            {
                GUI.Label(new Rect(4, 80, 100, 22), "<size=12>" + LocalizationManager.instance.current["colorRect_width"] + " :</size>");
                //   m_width = (uint)Mathf.FloorToInt(GUI.HorizontalSlider(new Rect(4, 44, 200, 25), m_width, 1, 2048));
                if (widthField == null)
                    widthField = new GUIUtils.FloatInputField(m_width);
                m_width = (uint)widthField.DrawField(new Rect(74, 79, 170, 35), m_width, false, 0f, 1024f, true, Mathf.Abs(Mathf.Round(textManager.windowTex.width - x))).returnValue;

                GUI.Label(new Rect(4, 115, 100, 22), "<size=12>" + LocalizationManager.instance.current["colorRect_height"] + " :</size>");
                if (heightField == null)
                    heightField = new GUIUtils.FloatInputField(m_height);
                m_height = (uint)heightField.DrawField(new Rect(74, 117, 170, 35), m_height, false, 0f, 1024f, true, Mathf.Abs(Mathf.Round(textManager.windowTex.height - y))).returnValue;

                GUI.Label(new Rect(4, 154, 200, 22), "<size=12>" + LocalizationManager.instance.current["colorRect_opacity"] + " : " + ((int)(m_fontColor.a * 100)).ToString() + "%</size>");
                m_fontColor.a = GUI.HorizontalSlider(new Rect(4, 176, 200, 25), m_fontColor.a, 0f, 1f);

                if (borderColor == null) borderColor = Color.white;
                GUI.Label(new Rect(4, 193, 200, 22), "<size=12>" + LocalizationManager.instance.current["colorRect_border"] + " : " + borderSize.ToString() + " px</size>");
                borderSize = (uint)Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(4, 215, 200, 25), borderSize, 0f, 20f));
                borderPainter = GUIPainter.DrawPainterSampleOnly(borderPainter, new Vector2(214, 198), borderColor,
                    (c) => { borderColor = c; },
                    () =>
                    {
                        if (textManager.colorPickerSelected == borderPainter)
                            textManager.colorPickerSelected = null;
                        else
                            textManager.colorPickerSelected = borderPainter;
                    });
            }

            // POSITION

            GUIUtils.DrawSeparator(new Vector2(3, 29), 241);

            GUI.Label(new Rect(3, 30, 75, 21), "<size=12>" + LocalizationManager.instance.current["position"] + " :</size>");
            //  float newX, newY;
            if (posXfield == null)
            {
                posXfield = new GUIUtils.FloatInputField(x);
                posYfield = new GUIUtils.FloatInputField(y);
            }
            GUI.Label(new Rect(3, 51, 24, 22), "<size=12>X :</size>");
            x = posXfield.DrawField(new Rect(27, 50, 83, 22), "textPosX", x, true).returnValue;
            GUI.Label(new Rect(111, 51, 24, 22), "<size=12>Y :</size>");
            y = posYfield.DrawField(new Rect(135, 50, 83, 22), "textPosY", y, true).returnValue;

            // rotation
            if (GUI.Button(new Rect(220, 50, 25, 23), ProceduralObjectsMod.Icons[5]))
            {
                ProceduralObjectsLogic.PlaySound();
                if (m_type == 0)
                {
                    if (m_rotation == 0)
                        m_rotation = 4;
                    else if (m_rotation == 4)
                        m_rotation = 1;
                    else if (m_rotation == 1)
                        m_rotation = 2;
                    else if (m_rotation == 2)
                        m_rotation = 0;
                }
                else if (m_type == 1)
                {
                    var h = m_height;
                    m_height = m_width;
                    m_width = h;
                }
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
            var font = manager.Arial;
            if (manager.m_fonts.Any(f => f.m_fontName == fontName))
                font = manager.m_fonts.First(f => f.m_fontName == fontName);
            else
                Debug.LogWarning("[ProceduralObjects] Font does not exist. Using Arial.");
            this.m_font = font;
            this.m_fontName = font.m_fontName;
            this.m_spacing = font.m_defaultSpacing;
            if (!font.m_boldExists || !font.m_italicExists)
                m_style = FontStyle.Normal;
        }
        public static TextField Clone(TextField fieldSource, bool useFontname)
        {
            var field = new TextField(fieldSource.m_type);
            field.m_text = fieldSource.m_text;
            field.m_fontSize = fieldSource.m_fontSize;
            field.m_style = fieldSource.m_style;
            field.x = fieldSource.x;
            field.y = fieldSource.y;
            field.m_rotation = fieldSource.m_rotation;
            field.m_fontColor = new SerializableColor(fieldSource.m_fontColor);
            field.m_scaleX = fieldSource.m_scaleX;
            field.m_scaleY = fieldSource.m_scaleY;
            field.m_width = fieldSource.m_width;
            field.m_height = fieldSource.m_height;
            field.borderSize = fieldSource.borderSize;
            field.borderColor = new SerializableColor(fieldSource.borderColor);
            if (field.m_type == 0)
            {
                if (useFontname)
                    field.SetFont(fieldSource.m_fontName, FontManager.instance);
                else
                    field.SetFont(fieldSource.m_font);
                field.m_spacing = fieldSource.m_spacing;
            }
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
            if (A.m_height != B.m_height)
                return true;
            if (A.m_width != B.m_width)
                return true;
            if (SerializableColor.Different(A.m_fontColor, B.m_fontColor))
                return true;
            if (A.m_spacing != B.m_spacing)
                return true;
            if (A.m_style != B.m_style)
                return true;
            if (A.m_font != B.m_font)
                return true;
            if (A.m_rotation != B.m_rotation)
                return true;
            if (A.m_scaleX != B.m_scaleX)
                return true;
            if (A.m_scaleY != B.m_scaleY)
                return true;
            if (A.borderSize != B.borderSize)
                return true;
            if (SerializableColor.Different(A.borderColor, B.borderColor))
                return true;
            return false;
        }
        public static string SaveString(TextField field)
        {
            if (field.m_type == 0)
                return "textParam: m_font:" + field.m_fontName + " m_size:" + field.m_fontSize.ToString() + " m_spacing:" + field.m_spacing.ToString() + " m_style:" +
                            TextCustomizationManager.SelectedStyle(field.m_style).ToString() + " m_x:" + field.x.ToString() + " m_y:" + field.y.ToString() + " m_scaleX:" + field.m_scaleX.ToString() + " m_scaleY:" + field.m_scaleY.ToString() +
                            " m_rotation:" + field.m_rotation.ToString() + " m_fontcolor:" + field.m_fontColor.ToString() + " m_text:" + field.m_text;
            else // if (m_type == 1)
                return "colorRect: m_x:" + field.x.ToString() + " m_y:" + field.y.ToString() + " m_width:" + field.m_width.ToString() + " m_height:" + field.m_height.ToString() +
                    (field.borderSize > 0 ? " m_borderSize:" + field.borderSize.ToString() + " m_borderColor:" + field.borderColor.ToString() : "") + " m_color:" + field.m_fontColor.ToString();
        }
        public static TextField ParseText(string s, FontManager fManager)
        {
            if (!s.Contains("textParam:"))
            {
                throw new FormatException("[ProceduralObjects] TextField Parse : Input string was not in the correct format");
            }
            try
            {
                TextField f = new TextField(0);
                bool newVersion = s.Contains(" m_rotation:");
                f.SetFont(s.GetStringBetween(" m_font:", " m_size:"), fManager);
                f.m_fontSize = uint.Parse(s.GetStringBetween(" m_size:", " m_spacing:"));
                f.m_spacing = uint.Parse(s.GetStringBetween(" m_spacing:", " m_style:"));
                f.m_style = TextCustomizationManager.IntToStyle(int.Parse(s.GetStringBetween(" m_style:", " m_x:")));
                f.x = float.Parse(s.GetStringBetween(" m_x:", " m_y:"));
                f.y = float.Parse(s.GetStringBetween(" m_y:", " m_scaleX:"));
                f.m_scaleX = float.Parse(s.GetStringBetween(" m_scaleX:", " m_scaleY:"));
                f.m_scaleY = float.Parse(s.GetStringBetween(" m_scaleY:", newVersion ? " m_rotation:" : " m_fontcolor:"));
                if (newVersion)
                    f.m_rotation = byte.Parse(s.GetStringBetween(" m_rotation:", " m_fontcolor:"));
                f.m_fontColor = SerializableColor.Parse(s.GetStringBetween(" m_fontcolor:", " m_text:"));
                f.m_text = s.GetStringAfter(" m_text:");
                return f;
            }
            catch (Exception e)
            {
                Debug.LogError("[ProceduralObjects] Error : " + e.GetType().ToString() + " while parsing a TextField : " + e.Message);
                return new TextField(0);
            }
        }
        public static TextField ParseColorRect(string s)
        {
            if (!s.Contains("colorRect:"))
            {
                throw new FormatException("[ProceduralObjects] TextField Parse : Input string was not in the correct format");
            }
            try
            {
                TextField f = new TextField(1);
                bool includesborder = s.Contains(" m_borderSize:");
                f.x = float.Parse(s.GetStringBetween(" m_x:", " m_y:"));
                f.y = float.Parse(s.GetStringBetween(" m_y:", " m_width:"));
                f.m_width = uint.Parse(s.GetStringBetween(" m_width:", " m_height:"));
                f.m_height = uint.Parse(s.GetStringBetween(" m_height:", includesborder ? " m_borderSize:" : " m_color:"));
                if (includesborder)
                {
                    f.borderSize = byte.Parse(s.GetStringBetween(" m_borderSize:", " m_borderColor:"));
                    f.borderColor = SerializableColor.Parse(s.GetStringBetween(" m_borderColor:", " m_color:"));
                }
                f.m_fontColor = SerializableColor.Parse(s.GetStringAfter(" m_color:"));
                return f;
            }
            catch (Exception e)
            {
                Debug.LogError("[ProceduralObjects] Error : " + e.GetType().ToString() + " while parsing a TextField : " + e.Message);
                return new TextField(0);
            }
        }
        public void Apply(Texture2D texture)
        {
            if (m_type == 0)
                m_font.PrintString(texture, m_text, m_style, new Vector2(x, y), m_spacing, m_fontSize, m_fontColor, m_rotation, out texWidth, out texHeight, m_scaleX, m_scaleY);
            else if (m_type == 1)
            {
                TextureUtils.PrintRectangle(texture, (int)x, (int)y, (int)m_width, (int)m_height, m_fontColor, (int)borderSize, borderColor);
                texWidth = (int)m_width;
                texHeight = (int)m_height;
            }
        }
    }

}
