using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;

namespace ProceduralObjects.ProceduralText
{
    public class TextureFont
    {
        public TextureFont(string name, Texture2D texNormal, uint spaceWidth)
        {
            m_fontName = name;
            m_textureNormal = texNormal;
            m_charTexturesNormal = new Dictionary<char, Texture2D>();
            m_kerningNormal = new Dictionary<Vector2, int>();
            m_spaceWidth = spaceWidth;
            m_italicExists = false;
            m_boldExists = false;
        }
        public TextureFont(string name, Texture2D texNormal, Texture2D texItalic, Texture2D texBold, uint spaceWidth)
        {
            m_fontName = name;
            m_textureNormal = texNormal;
            m_textureBold = texBold;
            m_textureItalic = texItalic;
            m_charTexturesNormal = new Dictionary<char, Texture2D>();
            m_charTexturesItalic = new Dictionary<char, Texture2D>();
            m_charTexturesBold = new Dictionary<char, Texture2D>();
            m_kerningNormal = new Dictionary<Vector2, int>();
            m_kerningItalic = new Dictionary<Vector2, int>();
            m_kerningBold = new Dictionary<Vector2, int>();
            m_spaceWidth = spaceWidth;
            m_italicExists = true;
            m_boldExists = true;
        }

        public void BuildFont()
        {
            if (m_textureNormal == null)
                return;
            m_charTexturesNormal = new Dictionary<char, Texture2D>();
            for (int i = 0; i < OrderedChars.Length; i++)
            {
                char c = OrderedChars[i];
                m_charTexturesNormal[c] = GetCharInFont(c, m_textureNormal);
            }
            if (m_textureItalic != null && m_italicExists)
            {
                m_charTexturesItalic = new Dictionary<char, Texture2D>();
                for (int i = 0; i < OrderedChars.Length; i++)
                {
                    char c = OrderedChars[i];
                    m_charTexturesItalic[c] = GetCharInFont(c, m_textureItalic);
                }
            }
            if (m_textureBold != null && m_boldExists)
            {
                m_charTexturesBold = new Dictionary<char, Texture2D>();
                for (int i = 0; i < OrderedChars.Length; i++)
                {
                    char c = OrderedChars[i];
                    m_charTexturesBold[c] = GetCharInFont(c, m_textureBold);
                }
            }
        }

        public static readonly Color blank = new Color(0, 0, 0, 0);
        public const string OrderedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzàáäåâãìíïîòóöøôõœèéëêùúü-ûð&ÿ'ñßç,!.?:0123456789+=%µ#§€$£;<>\"°@(){}/\\_*[]¤^²|~";

        public Dictionary<char, Texture2D> m_charTexturesNormal, m_charTexturesItalic, m_charTexturesBold;
        public Texture2D m_textureNormal, m_textureBold, m_textureItalic;
        public string m_fontName;
        public uint m_spaceWidth, m_defaultSpacing;
        public bool m_italicExists, m_boldExists, m_disableColorOverwriting;
        public Dictionary<Vector2, int> m_kerningNormal, m_kerningItalic, m_kerningBold;

        public void PrintString(Texture2D originalTex, string str, FontStyle style, Vector2 position, uint spacing, uint size, Color fontColor, out int width, out int height, float scaleX = 1f, float scaleY = 1f)
        {
            var stringTex = GetString(str, style, spacing);
            if (size != 50 || scaleX != 1f || scaleY != 1f)
            {
                TextureScale.Bilinear(stringTex, (int)((stringTex.width * size / 50) * scaleX), (int)(size * scaleY));
            }
            width = stringTex.width;
            height = stringTex.height;
            position.y = Mathf.Abs(position.y - originalTex.height) - stringTex.height;
            for (int x = 0; x < stringTex.width; x++)
            {
                if ((int)position.x + x < originalTex.width)
                {
                    for (int y = 0; y < stringTex.height; y++)
                    {
                        if ((int)position.y + y < originalTex.height)
                        {
                            Color stringColor = stringTex.GetPixel(x, y);
                            if (stringColor.a > 0)
                            {
                                if (!m_disableColorOverwriting)
                                    stringColor = new Color(fontColor.r, fontColor.g, fontColor.b, stringColor.a);
                                if (stringColor.a < 1)
                                    stringColor = AverageColor(originalTex.GetPixel((int)position.x + x, (int)position.y + y), stringColor);

                                originalTex.SetPixel((int)position.x + x, (int)position.y + y, stringColor);
                            }
                        }
                    }
                }
            }
            originalTex.Apply();
          //  return originalTex;
        }
        private Texture2D PlainTexture(int width, int height, Color color)
        {
            var texture = new Texture2D(width, height);
            Color[] resetColorArray = texture.GetPixels();

            for (int i = 0; i < resetColorArray.Length; i++)
            {
                resetColorArray[i] = color;
            }

            texture.SetPixels(resetColorArray);
            texture.Apply();
            return texture;
        }
        private int kerning(int i, string s, FontStyle style)
        {
            if (i == 0)
                return 0;
            if (!OrderedChars.Contains(s[i]) || !OrderedChars.Contains(s[i - 1]))
                return 0;
            Vector2 chars = new Vector2(OrderedChars.IndexOf(s[i - 1]), OrderedChars.IndexOf(s[i]));
            if (style == FontStyle.Bold)
            {
                if (m_kerningBold.ContainsKey(chars))
                    return m_kerningBold[chars];
            }
            else if (style == FontStyle.Italic)
            {
                if (m_kerningItalic.ContainsKey(chars))
                    return m_kerningItalic[chars];
            }
            else
            {
                if (m_kerningNormal.ContainsKey(chars))
                    return m_kerningNormal[chars];
            }
            return 0;
        }
        public Texture2D GetString(string str, FontStyle style, uint spacing)
        {
            var tex = PlainTexture(GetStringLength(str, style, spacing), 50, blank);
            int offsetX = 0;
            var charTextures = getStyleDictionary(style);

            for (int i = 0; i < str.Length; i++)
            {
                if (!charTextures.ContainsKey(str[i]))
                    str = str.Replace(str[i], ' ');
                if (str[i] == ' ')
                {
                    for (int y = 0; y < 50; y++)
                    {
                        for (int x = 0; x < m_spaceWidth; x++)
                        {
                            tex.SetPixel(offsetX + x, y, blank);
                        }
                    }
                    offsetX += (int)m_spaceWidth;
                }
                else
                {
                    var chartex = charTextures[str[i]];
                    var k = kerning(i, str, style);
                    for (int y = 0; y < 50; y++)
                    {
                        for (int x = 0; x < chartex.width + (int)spacing; x++)
                        {
                            if (x >= chartex.width)
                                tex.SetPixel(offsetX + x - k, y, blank);
                            else
                            {
                                var pix = chartex.GetPixel(x, y);
                                if (pix.a != 0)
                                    tex.SetPixel(offsetX + x - k, y, pix);
                            }
                        }
                    }
                    offsetX += chartex.width + (int)spacing - k;
                }
            }
            tex.Apply();
            return tex;
        }
        private int GetStringLength(string str, FontStyle style, uint spacing)
        {
            int length = 0;
            var charTextures = getStyleDictionary(style);

            for (int i = 0; i < str.Length; i++)
            {
                if (charTextures.ContainsKey(str[i]))
                    length += charTextures[str[i]].width + (int)spacing - kerning(i, str, style);
                else if (!charTextures.ContainsKey(str[i]))
                {
                    str.Replace(str[i], ' ');
                    length += (int)m_spaceWidth;
                }
                else if (str[i] == ' ')
                    length += (int)m_spaceWidth;
            }
            return length;
        }
        private Texture2D GetCharInFont(char c, Texture2D font)
        {
            if (!OrderedChars.Contains(c))
                return null;
          //  int charPerLine = 10;
            int index = OrderedChars.IndexOf(c);
            int line = Mathf.FloorToInt(index / 10);
            index %= 10;
            Texture2D charTex = new Texture2D(50, 50);

            int savedStart = -1, savedEnd = -1;

            for (int x = 0; x < 50; x++)
            {
                if (savedStart == -1)
                {
                    for (int y = 0; y < 50; y++)
                    {
                        Color color = font.GetPixel(index * 52 + x, y + 624 - (52 * line));
                        if (color.a > 0)
                        {
                            charTex.SetPixel(x, y, color);
                            savedStart = x;
                        }
                        else
                            charTex.SetPixel(x, y, blank);
                    }
                }
                else if (savedEnd == -1)
                {
                    bool foundEnd = true;
                    for (int y = 0; y < 50; y++)
                    {
                        Color color = font.GetPixel(index * 52 + x, y + 624 - (52 * line));
                        if (color.a > 0)
                        {
                            charTex.SetPixel(x, y, color);
                            foundEnd = false;
                        }
                        else
                            charTex.SetPixel(x, y, blank);
                    }
                    if (foundEnd)
                        savedEnd = x;
                }
            }
            charTex.Apply();
            try
            {
                var newTex = new Texture2D(savedEnd - savedStart, 50);
                for (int y = 0; y < 50; y++)
                {
                    for (int x = 0; x < newTex.width; x++)
                    {
                        newTex.SetPixel(x, y, charTex.GetPixel(x + savedStart, y));
                    }
                }
                newTex.Apply();
                return newTex;
            }
            catch (Exception e)
            {
                Debug.LogError("[ProceduralObjects] Font error : Exception with character " + c.ToString() + " : " + e.Message);
                return charTex;
            }
        }
        private Dictionary<char, Texture2D> getStyleDictionary(FontStyle style)
        {
            if ((style == FontStyle.Italic && !m_italicExists) || (style == FontStyle.Bold && !m_boldExists))
                return this.m_charTexturesNormal;
            if (style == FontStyle.Italic)
                return this.m_charTexturesItalic;
            if (style == FontStyle.Bold)
                return this.m_charTexturesBold;
            return this.m_charTexturesNormal;
        }

        private Color AverageColor(Color original, Color newColor)
        {
            return new Color(
                ((original.r * (100 - (newColor.a * 100))) + (newColor.r * (newColor.a * 100))) / 100,
                ((original.g * (100 - (newColor.a * 100))) + (newColor.g * (newColor.a * 100))) / 100,
                ((original.b * (100 - (newColor.a * 100))) + (newColor.b * (newColor.a * 100))) / 100,
                1);
        }
    }
}
