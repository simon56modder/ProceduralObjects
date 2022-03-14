using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;

using ColossalFramework.PlatformServices;

using ProceduralObjects.Classes;

namespace ProceduralObjects.ProceduralText
{
    public class TextureFont
    {
        public TextureFont(string path)
        {
            m_path = path;
        }

        public void Initialize(string name, Texture2D texNormal, uint spaceWidth)
        {
            m_fontName = name;
            m_textureNormal = texNormal;
            m_charTexturesNormal = new Dictionary<char, Texture2D>();
            m_kerningNormal = new Dictionary<Vector2, int>();
            m_arabic_correspondances = new List<CharArabicCorrespondance>();
            m_spaceWidth = spaceWidth;
            m_italicExists = false;
            m_boldExists = false;
            m_stylesNames = null;
        }
        public void Initialize(string name, Texture2D texNormal, Texture2D texItalic, Texture2D texBold, uint spaceWidth)
        {
            Initialize(name, texNormal, spaceWidth);
            m_textureBold = texBold;
            m_textureItalic = texItalic;
            m_charTexturesItalic = new Dictionary<char, Texture2D>();
            m_charTexturesBold = new Dictionary<char, Texture2D>();
            m_kerningItalic = new Dictionary<Vector2, int>();
            m_kerningBold = new Dictionary<Vector2, int>();
            m_italicExists = true;
            m_boldExists = true;
        }

        public void BuildFont()
        {
            if (m_textureNormal == null)
                return;
            m_charTexturesNormal = new Dictionary<char, Texture2D>();
            for (int i = 0; i < m_orderedChars.Length; i++)
            {
                char c = m_orderedChars[i];
                m_charTexturesNormal[c] = GetCharInFont(c, m_textureNormal);
            }
            if (m_textureItalic != null && m_italicExists)
            {
                m_charTexturesItalic = new Dictionary<char, Texture2D>();
                for (int i = 0; i < m_orderedChars.Length; i++)
                {
                    char c = m_orderedChars[i];
                    m_charTexturesItalic[c] = GetCharInFont(c, m_textureItalic);
                }
            }
            if (m_textureBold != null && m_boldExists)
            {
                m_charTexturesBold = new Dictionary<char, Texture2D>();
                for (int i = 0; i < m_orderedChars.Length; i++)
                {
                    char c = m_orderedChars[i];
                    m_charTexturesBold[c] = GetCharInFont(c, m_textureBold);
                }
            }
        }

        public static readonly Color blank = new Color(0, 0, 0, 0);

        public Dictionary<char, Texture2D> m_charTexturesNormal, m_charTexturesItalic, m_charTexturesBold;
        public Texture2D m_textureNormal, m_textureBold, m_textureItalic;
        public string m_fontName, m_orderedChars, m_path;
        public string[] m_stylesNames;
        public uint m_spaceWidth, m_defaultSpacing, m_charSize;
        public bool m_italicExists, m_boldExists, m_disableColorOverwriting;
        public Dictionary<Vector2, int> m_kerningNormal, m_kerningItalic, m_kerningBold;
        public List<CharArabicCorrespondance> m_arabic_correspondances;
        public PublishedFileId file_id;
        
        public void PrintString(Texture2D originalTex, string str, FontStyle style, Vector2 position, uint spacing, uint size, Color fontColor, byte rotation, out int width, out int height, float scaleX = 1f, float scaleY = 1f)
        {
            var stringTex = GetString(str, style, spacing);
            if (size != m_charSize || scaleX != 1f || scaleY != 1f)
            {
                TextureScale.Bilinear(stringTex, (int)((stringTex.width * size / m_charSize) * scaleX), (int)(size * scaleY));
            }
            if (rotation > 0)
            {
                for (byte b = 0; b <= rotation; b++)
                {
                    var dumpTex = stringTex;
                    stringTex = TextureUtils.RotateRight(stringTex);
                    dumpTex.DisposeTexFromMemory();
                }
            }
            width = stringTex.width;
            height = stringTex.height;
            position.y = originalTex.height - position.y - height;
            for (int x = 0; x < stringTex.width; x++)
            {
                if ((int)position.x + x < originalTex.width)
                {
                    for (int y = 0; y < stringTex.height; y++)
                    {
                        if ((int)position.y + y > 0)
                        {
                            Color stringColor = stringTex.GetPixel(x, y);
                            if (stringColor.a > 0)
                            {
                                if (!m_disableColorOverwriting)
                                    stringColor = new Color(fontColor.r, fontColor.g, fontColor.b, stringColor.a);
                                if (stringColor.a < 1)
                                    stringColor = TextureUtils.AverageColor(originalTex.GetPixel((int)position.x + x, (int)position.y + y), stringColor);

                                originalTex.SetPixel((int)position.x + x, (int)position.y + y, stringColor);
                            }
                        }
                    }
                }
            }
            originalTex.Apply();
            // attempt to reduce RAM usage (1.7.1)
            stringTex.DisposeTexFromMemory();
        }
        private int kerning(int i, string s, FontStyle style)
        {
            if (i == 0) return 0;
            return kerning(getArabicReplaced(i - 1, s), getArabicReplaced(i, s), style);
        }
        private int kerning(char prev, char next, FontStyle style)
        {
            if (!m_orderedChars.Contains(next) || !m_orderedChars.Contains(prev))
                return 0;
            Vector2 chars = new Vector2(m_orderedChars.IndexOf(prev), m_orderedChars.IndexOf(next));
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
            var tex = TextureUtils.PlainTexture(GetStringLength(str, style, spacing), (int)m_charSize, blank);
            int offsetX = 0;
            var charTextures = getStyleDictionary(style);

            for (int i = 0; i < str.Length; i++)
            {
                if (!charTextures.ContainsKey(str[i]))
                    str = str.Replace(str[i], ' ');
                if (str[i] == ' ')
                {
                    for (int y = 0; y < m_charSize; y++)
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
                    var chartex = charTextures[getArabicReplaced(i, str)];
                    var k = kerning(i, str, style);
                    for (int y = 0; y < m_charSize; y++)
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
                var character = getArabicReplaced(i, str);
                if (charTextures.ContainsKey(character))
                    length += charTextures[character].width + (int)spacing - kerning(i, str, style);
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
            if (!m_orderedChars.Contains(c))
                return null;
          //  int charPerLine = 10;
            int index = m_orderedChars.IndexOf(c);
            int line = Mathf.FloorToInt(index / 10);
            index %= 10;
            Texture2D charTex = new Texture2D((int)m_charSize, (int)m_charSize);

            int savedStart = -1, savedEnd = -1, heightDiff = font.height - (int)m_charSize;

            for (int x = 0; x < m_charSize; x++)
            {
                if (savedStart == -1)
                {
                    for (int y = 0; y < m_charSize; y++)
                    {
                        Color color = font.GetPixel(index * ((int)m_charSize + 2) + x, y + heightDiff - (((int)m_charSize + 2) * line));
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
                    for (int y = 0; y < m_charSize; y++)
                    {
                        Color color = font.GetPixel(index * ((int)m_charSize + 2) + x, y + heightDiff - (((int)m_charSize + 2) * line));
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
                var newTex = new Texture2D(savedEnd - savedStart, (int)m_charSize);
                for (int y = 0; y < m_charSize; y++)
                {
                    for (int x = 0; x < newTex.width; x++)
                    {
                        var color = charTex.GetPixel(x + savedStart, y);
                        if (m_disableColorOverwriting)
                            newTex.SetPixel(x, y, color);
                        else
                            newTex.SetPixel(x, y, new Color(1, 1, 1, color.a));
                    }
                }
                newTex.Apply();
                return newTex;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ProceduralObjects] Font error in font " + m_fontName + " : Failed to fully setup character " + c.ToString() + " : " + e.Message);
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
        private char getArabicReplaced(int i, string s)
        {
            // Used for Arabic writing which requires special attention
            // method that returns a replacement character for a given character at index i in a string s
            // depending on whether it has to be connected on the left, right, both or none.
            // see class CharArabicCorrespondance            
            if (m_arabic_correspondances == null) return s[i];
            if (m_arabic_correspondances.Count == 0) return s[i];
            if (!m_arabic_correspondances.Any(corres => corres.def == s[i])) return s[i];
            var correspondance = m_arabic_correspondances.FirstOrDefault(corres => corres.def == s[i]);
            bool connectLeft = false, connectRight = false;
            if (i > 0)
            { if (s[i - 1] != ' ') connectLeft = true; }
            if (i < s.Length - 2)
            { if (s[i + 1] != ' ') connectRight = true; }
            if (connectLeft && connectRight) return correspondance.right_left;
            else if (!connectLeft && connectRight) return correspondance.right;
            else if (connectLeft && !connectRight) return correspondance.left;
            else return correspondance.def;
        }

        public void ExportKerning(string excludedCharacters)
        {
            List<int> excluded = new List<int>();
            if (excludedCharacters.Trim() != "")
            {
                foreach (var str in excludedCharacters.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries))
                    excluded.Add(int.Parse(str));
            }

            CalculateNormalKerning(excluded);
            if (m_italicExists)
                CalculateItalicKerning(excluded);
            if (m_boldExists)
                CalculateBoldKerning(excluded);

            if (!Directory.Exists(ProceduralObjectsMod.FontsPath))
                Directory.CreateDirectory(ProceduralObjectsMod.FontsPath);

            if (File.Exists(ProceduralObjectsMod.FontsPath + "KerningData_" + m_fontName + ".txt"))
                File.Delete(ProceduralObjectsMod.FontsPath + "KerningData_" + m_fontName + ".txt");

            TextWriter tw = new StreamWriter(ProceduralObjectsMod.FontsPath + "KerningData_" + m_fontName + ".txt");
            for (int i = 0; i < m_orderedChars.Length; i++)
            {
                if (excluded.Contains(i))
                    continue;

                for (int j = 0; j < m_orderedChars.Length; j++)
                {
                    if (excluded.Contains(j))
                        continue;
                    var comb = new Vector2(i, j);
                    if (m_italicExists && m_boldExists)
                    {
                        if (m_kerningBold[comb] == m_kerningNormal[comb] && m_kerningNormal[comb] == m_kerningItalic[comb])
                        {
                            if (m_kerningNormal[comb] != 0)
                                tw.WriteLine("krngNormalItalicBold(" + i + "," + j + ") = " + m_kerningNormal[comb]);
                        }
                        else if (m_kerningNormal[comb] == m_kerningItalic[comb] && m_kerningBold[comb] != m_kerningNormal[comb])
                        {
                            if (m_kerningNormal[comb] != 0)
                                tw.WriteLine("krngNormalItalic(" + i + "," + j + ") = " + m_kerningNormal[comb]);
                            if (m_kerningBold[comb] != 0)
                                tw.WriteLine("krngBold(" + i + "," + j + ") = " + m_kerningBold[comb]);
                        }
                        else if (m_kerningNormal[comb] == m_kerningBold[comb] && m_kerningItalic[comb] != m_kerningNormal[comb])
                        {
                            if (m_kerningNormal[comb] != 0)
                                tw.WriteLine("krngNormalBold(" + i + "," + j + ") = " + m_kerningNormal[comb]);
                            if (m_kerningItalic[comb] != 0)
                                tw.WriteLine("krngItalic(" + i + "," + j + ") = " + m_kerningItalic[comb]);
                        }
                        else
                        {
                            if (m_kerningNormal[comb] != 0)
                                tw.WriteLine("krngNormal(" + i + "," + j + ") = " + m_kerningNormal[comb]);
                            if (m_kerningItalic[comb] != 0)
                                tw.WriteLine("krngItalic(" + i + "," + j + ") = " + m_kerningItalic[comb]);
                            if (m_kerningBold[comb] != 0)
                                tw.WriteLine("krngBold(" + i + "," + j + ") = " + m_kerningBold[comb]);
                        }
                    }
                    else
                    {
                        if (m_kerningNormal[comb] != 0)
                            tw.WriteLine("krngNormal(" + i + "," + j + ") = " + m_kerningNormal[comb]);
                    }
                }
            }
            tw.Close();
        }
        private void CalculateNormalKerning(List<int> excluded)
        {
            m_kerningNormal = new Dictionary<Vector2, int>();
            /*
            if (m_boldExists)
                m_kerningBold = new Dictionary<Vector2, int>();
            if (m_italicExists)
                m_kerningItalic = new Dictionary<Vector2, int>(); */

            var normalSpacesBefore = new Dictionary<char, List<int>>();
            var normalSpacesAfter = new Dictionary<char, List<int>>();
            for (int i = 0; i < m_orderedChars.Length; i++)
            {
                if (excluded.Contains(i))
                    continue;

                char c = m_orderedChars[i];
                var tex = m_charTexturesNormal[c];
                normalSpacesBefore[c] = new List<int>();
                normalSpacesAfter[c] = new List<int>();
                int smallestY = -1, verticalSize = 0;
                for (int y = 0; y < tex.height; y++)
                {
                    int spaceBef = -1;
                    int spaceAf = 0;
                    for (int x = 0; x < tex.width; x++)
                    {
                        if (tex.GetPixel(x, y).a > 0)
                        {
                            verticalSize = y;
                            if (smallestY == -1)
                                smallestY = y;
                            if (spaceBef == -1)
                                spaceBef = x;
                            if (spaceAf < x)
                                spaceAf = x;
                        }
                    }
                    if (spaceBef == -1 && spaceAf == 0)
                    {
                        int space = tex.width / 2;
                        normalSpacesBefore[c].Add(space);
                        normalSpacesAfter[c].Add(tex.width - space);
                    }
                    else
                    {
                        normalSpacesBefore[c].Add(spaceBef);
                        normalSpacesAfter[c].Add(tex.width - spaceAf);
                    }
                }
                verticalSize = Mathf.Max(verticalSize, smallestY) - Mathf.Min(verticalSize, smallestY);

                // fix thin characters being too tight (Before)
                int deltaXBef = Mathf.Max(normalSpacesBefore[c].ToArray()) - Mathf.Min(normalSpacesBefore[c].ToArray());
                int spaceSurplusBef = deltaXBef <= 5 ? (verticalSize >= 5 ? 3 : 2) : (deltaXBef <= 9 ? 1 : 0);
                for (int y = 0; y < normalSpacesBefore[c].Count; y++)
                    normalSpacesBefore[c][y] -= spaceSurplusBef;
                // (after)
                int deltaXAf = Mathf.Max(normalSpacesAfter[c].ToArray()) - Mathf.Min(normalSpacesAfter[c].ToArray());
                int spaceSurplusAf = deltaXAf <= 5 ? (verticalSize >= 6 ? 3 : 2) : (deltaXAf <= 9 ? 1 : 0);
                for (int y = 0; y < normalSpacesAfter[c].Count; y++)
                    normalSpacesAfter[c][y] -= spaceSurplusAf;
            }

            for (int i = 0; i < m_orderedChars.Length; i++)
            {
                if (excluded.Contains(i))
                    continue;
                char c1 = m_orderedChars[i];
                for (int j = 0; j < m_orderedChars.Length; j++)
                {
                    if (excluded.Contains(j))
                        continue;
                    char c2 = m_orderedChars[j];
                    int space = m_charTexturesNormal[c1].width;
                    for (int y = 0; y < normalSpacesAfter[c1].Count; y++)
                    {
                        int newSpace = normalSpacesAfter[c1][y] + normalSpacesBefore[c2][y];
                        space = Mathf.Min(newSpace, space);
                    }
                    m_kerningNormal[new Vector2(i, j)] = Mathf.FloorToInt((float)space * (5f / 7f));
                }
            }
        }
        private void CalculateItalicKerning(List<int> excluded)
        {
            m_kerningItalic = new Dictionary<Vector2, int>();
            /*
            if (m_boldExists)
                m_kerningBold = new Dictionary<Vector2, int>();
            if (m_italicExists)
                m_kerningItalic = new Dictionary<Vector2, int>(); */

            var italicSpacesBefore = new Dictionary<char, List<int>>();
            var italicSpacesAfter = new Dictionary<char, List<int>>();
            for (int i = 0; i < m_orderedChars.Length; i++)
            {
                if (excluded.Contains(i))
                    continue;

                char c = m_orderedChars[i];
                var tex = m_charTexturesItalic[c];
                italicSpacesBefore[c] = new List<int>();
                italicSpacesAfter[c] = new List<int>();
                int smallestY = -1, verticalSize = 0;
                for (int y = 0; y < tex.height; y++)
                {
                    int spaceBef = -1;
                    int spaceAf = 0;
                    for (int x = 0; x < tex.width; x++)
                    {
                        if (tex.GetPixel(x, y).a > 0)
                        {
                            verticalSize = y;
                            if (smallestY == -1)
                                smallestY = y;
                            if (spaceBef == -1)
                                spaceBef = x;
                            if (spaceAf < x)
                                spaceAf = x;
                        }
                    }
                    if (spaceBef == -1 && spaceAf == 0)
                    {
                        int space = tex.width / 2;
                        italicSpacesBefore[c].Add(space);
                        italicSpacesAfter[c].Add(tex.width - space);
                    }
                    else
                    {
                        italicSpacesBefore[c].Add(spaceBef);
                        italicSpacesAfter[c].Add(tex.width - spaceAf);
                    }
                }
                verticalSize = Mathf.Max(verticalSize, smallestY) - Mathf.Min(verticalSize, smallestY);

                // fix thin characters being too tight (Before)
                int deltaXBef = Mathf.Max(italicSpacesBefore[c].ToArray()) - Mathf.Min(italicSpacesBefore[c].ToArray());
                int spaceSurplusBef = deltaXBef <= 5 ? (verticalSize >= 5 ? 3 : 2) : (deltaXBef <= 9 ? 1 : 0);
                for (int y = 0; y < italicSpacesBefore[c].Count; y++)
                    italicSpacesBefore[c][y] -= spaceSurplusBef;
                // (after)
                int deltaXAf = Mathf.Max(italicSpacesAfter[c].ToArray()) - Mathf.Min(italicSpacesAfter[c].ToArray());
                int spaceSurplusAf = deltaXAf <= 5 ? (verticalSize >= 6 ? 3 : 2) : (deltaXAf <= 9 ? 1 : 0);
                for (int y = 0; y < italicSpacesAfter[c].Count; y++)
                    italicSpacesAfter[c][y] -= spaceSurplusAf;
            }

            for (int i = 0; i < m_orderedChars.Length; i++)
            {
                if (excluded.Contains(i))
                    continue;
                char c1 = m_orderedChars[i];
                for (int j = 0; j < m_orderedChars.Length; j++)
                {
                    if (excluded.Contains(j))
                        continue;
                    char c2 = m_orderedChars[j];
                    int space = m_charTexturesItalic[c1].width;
                    for (int y = 0; y < italicSpacesAfter[c1].Count; y++)
                    {
                        int newSpace = italicSpacesAfter[c1][y] + italicSpacesBefore[c2][y];
                        space = Mathf.Min(newSpace, space);
                    }
                    space = Mathf.FloorToInt((float)space * (5f / 7f));
                    m_kerningItalic[new Vector2(i, j)] = space;
                }
            }
        }
        private void CalculateBoldKerning(List<int> excluded)
        {
            m_kerningBold = new Dictionary<Vector2, int>();
            /*
            if (m_boldExists)
                m_kerningBold = new Dictionary<Vector2, int>();
            if (m_italicExists)
                m_kerningItalic = new Dictionary<Vector2, int>(); */

            var boldSpacesBefore = new Dictionary<char, List<int>>();
            var boldSpacesAfter = new Dictionary<char, List<int>>();
            for (int i = 0; i < m_orderedChars.Length; i++)
            {
                if (excluded.Contains(i))
                    continue;

                char c = m_orderedChars[i];
                var tex = m_charTexturesBold[c];
                boldSpacesBefore[c] = new List<int>();
                boldSpacesAfter[c] = new List<int>();
                int smallestY = -1, verticalSize = 0;
                for (int y = 0; y < tex.height; y++)
                {
                    int spaceBef = -1;
                    int spaceAf = 0;
                    for (int x = 0; x < tex.width; x++)
                    {
                        if (tex.GetPixel(x, y).a > 0)
                        {
                            verticalSize = y;
                            if (smallestY == -1)
                                smallestY = y;
                            if (spaceBef == -1)
                                spaceBef = x;
                            if (spaceAf < x)
                                spaceAf = x;
                        }
                    }
                    if (spaceBef == -1 && spaceAf == 0)
                    {
                        int space = tex.width / 2;
                        boldSpacesBefore[c].Add(space);
                        boldSpacesAfter[c].Add(tex.width - space);
                    }
                    else
                    {
                        boldSpacesBefore[c].Add(spaceBef);
                        boldSpacesAfter[c].Add(tex.width - spaceAf);
                    }
                }
                verticalSize = Mathf.Max(verticalSize, smallestY) - Mathf.Min(verticalSize, smallestY);

                // fix thin characters being too tight (Before)
                int deltaXBef = Mathf.Max(boldSpacesBefore[c].ToArray()) - Mathf.Min(boldSpacesBefore[c].ToArray());
                int spaceSurplusBef = deltaXBef <= 5 ? (verticalSize >= 5 ? 3 : 2) : (deltaXBef <= 9 ? 1 : 0);
                for (int y = 0; y < boldSpacesBefore[c].Count; y++)
                    boldSpacesBefore[c][y] -= spaceSurplusBef;
                // (after)
                int deltaXAf = Mathf.Max(boldSpacesAfter[c].ToArray()) - Mathf.Min(boldSpacesAfter[c].ToArray());
                int spaceSurplusAf = deltaXAf <= 5 ? (verticalSize >= 6 ? 3 : 2) : (deltaXAf <= 9 ? 1 : 0);
                for (int y = 0; y < boldSpacesAfter[c].Count; y++)
                    boldSpacesAfter[c][y] -= spaceSurplusAf;
            }

            for (int i = 0; i < m_orderedChars.Length; i++)
            {
                if (excluded.Contains(i))
                    continue;
                char c1 = m_orderedChars[i];
                for (int j = 0; j < m_orderedChars.Length; j++)
                {
                    if (excluded.Contains(j))
                        continue;
                    char c2 = m_orderedChars[j];
                    int space = m_charTexturesBold[c1].width;
                    for (int y = 0; y < boldSpacesAfter[c1].Count; y++)
                    {
                        int newSpace = boldSpacesAfter[c1][y] + boldSpacesBefore[c2][y];
                        space = Mathf.Min(newSpace, space);
                    }
                    space = Mathf.FloorToInt((float)space * (5f / 7f));
                    m_kerningBold[new Vector2(i, j)] = space;
                }
            }
        }
    }
    public class CharArabicCorrespondance
    {
        public CharArabicCorrespondance() { }

        public char def, right, left, right_left;
    }
}
