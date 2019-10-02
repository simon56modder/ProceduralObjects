using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ColossalFramework.PlatformServices;
using System.IO;
using UnityEngine;

using ProceduralObjects.Classes;

namespace ProceduralObjects.ProceduralText
{
    public class FontManager
    {
        public FontManager()
        {
            m_fonts = new List<TextureFont>();
            LoadFonts();
            arial = m_fonts.FirstOrDefault(font => font.m_fontName.ToLower() == "arial");
        }
        public TextureFont Arial
        {
            get { return arial; }
        }
        private TextureFont arial;
        public List<TextureFont> m_fonts;

        public static FontManager instance;

        public TextureFont GetNextFont(TextureFont f)
        {
            if (m_fonts == null)
                return arial;
            if (!m_fonts.Contains(f))
                return arial;
            if (nextFontExists(f))
                return m_fonts[m_fonts.IndexOf(f) + 1];
            return arial;
        }
        public TextureFont GetPreviousFont(TextureFont f)
        {
            if (m_fonts == null)
                return arial;
            if (!m_fonts.Contains(f))
                return arial;
            if (previousFontExists(f))
                return m_fonts[m_fonts.IndexOf(f) - 1];
            return arial;
        }
        public bool previousFontExists(TextureFont f)
        {
            if (m_fonts.IndexOf(f) == 0)
                return false;
            return true;
        }
        public bool nextFontExists(TextureFont f)
        {
            if (m_fonts.IndexOf(f) == m_fonts.Count - 1)
                return false;
            return true;
        }
        public void LoadFonts()
        {
            m_fonts = new List<TextureFont>();
            foreach (PublishedFileId fileId in PlatformService.workshop.GetSubscribedItems())
            {
                string path = PlatformService.workshop.GetSubscribedItemPath(fileId);
                if (!Directory.Exists(path))
                    continue;
                var files = Directory.GetFiles(path, "*.pofont", SearchOption.AllDirectories);
            //    string infoPath = ProceduralObjectsMod.IsLinux ? path + @"/ProceduralObjectsTextures.cfg" : path + @"\ProceduralObjectsTextures.cfg";
                if (files.Any())
                {
                    for (int i = 0; i < files.Count(); i++)
                    {
                        if (!File.Exists(files[i]))
                            continue;
                        LoadSingleFont(files[i]);
                    }
                }
            }
        }
        public void LoadSingleFont(string infoFilePath)
        {
            string dir = Path.GetDirectoryName(infoFilePath);

            string[] lines = File.ReadAllLines(infoFilePath);
            string name = "", normal = "", italic = "", bold = "", orderedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzàáäåâãìíïîòóöøôõœèéëêùúü-ûð&ÿ'ñßç,!.?:0123456789+=%µ#§€$£;<>\"°@(){}/\\_*[]¤^²|~";
            uint spaceWidth = 0, defaultSpacing = 2;
            bool disableOverwriting = false;
            var kerningNormal = new Dictionary<Vector2, int>();
            var kerningItalic = new Dictionary<Vector2, int>();
            var kerningBold = new Dictionary<Vector2, int>();
            string[] stylesNames = null;

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("name = "))
                    name = lines[i].Replace("name = ", "");
                else if (lines[i].Contains("normal = "))
                    normal = lines[i].Replace("normal = ", "");
                else if (lines[i].Contains("italic = "))
                    italic = lines[i].Replace("italic = ", "");
                else if (lines[i].Contains("bold = "))
                    bold = lines[i].Replace("bold = ", "");
                else if (lines[i].Contains("spaceWidth = "))
                    spaceWidth = uint.Parse(lines[i].Replace("spaceWidth = ", "").Trim());
                else if (lines[i].Contains("defaultSpacing = "))
                    defaultSpacing = uint.Parse(lines[i].Replace("defaultSpacing = ", "").Trim());
                else if (lines[i].Contains("characters = "))
                {
                    orderedChars = "";
                    var charUnicodes = lines[i].Replace("characters = ", "").Trim().Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                    for (int j = 0; j < charUnicodes.Length; j++)
                    {
                        orderedChars += "\\u" + charUnicodes[j];
                    }
                    orderedChars = Regex.Unescape(orderedChars);
                }
                else if (lines[i].Contains("stylesNames = "))
                {
                    stylesNames = lines[i].GetStringAfter(" = ").Split(new string[] { ",", ";" }, StringSplitOptions.RemoveEmptyEntries);
                }
                else if (lines[i].Contains("krng"))
                {
                    // kerning
                    string[] chars = lines[i].GetStringBetween("(", ")").Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    Vector2 charIndexes = new Vector2(int.Parse(chars[0]), int.Parse(chars[1]));
                    if (lines[i].Contains("krngNormal("))
                        kerningNormal[charIndexes] = int.Parse(lines[i].GetStringAfter(" = "));
                    else if (lines[i].Contains("krngItalic("))
                        kerningItalic[charIndexes] = int.Parse(lines[i].GetStringAfter(" = "));
                    else if (lines[i].Contains("krngBold("))
                        kerningBold[charIndexes] = int.Parse(lines[i].GetStringAfter(" = "));
                    else if (lines[i].Contains("krngNormalItalic("))
                    {
                        kerningNormal[charIndexes] = int.Parse(lines[i].GetStringAfter(" = "));
                        kerningItalic[charIndexes] = kerningNormal[charIndexes];
                    }
                    else if (lines[i].Contains("krngNormalItalicBold("))
                    {
                        kerningNormal[charIndexes] = int.Parse(lines[i].GetStringAfter(" = "));
                        kerningItalic[charIndexes] = kerningNormal[charIndexes];
                        kerningBold[charIndexes] = kerningNormal[charIndexes];
                    }
                    else if (lines[i].Contains("krngNormalBold("))
                    {
                        kerningNormal[charIndexes] = int.Parse(lines[i].GetStringAfter(" = "));
                        kerningBold[charIndexes] = kerningNormal[charIndexes];
                    }
                }
                else if (lines[i].Replace(" ", "").ToLower() == "disablecoloroverwriting=true")
                    disableOverwriting = true;
            }
            if (normal == "")
            {
                Debug.LogError("[ProceduralObjects] Font error : No normal style specified for a font, couldn't create it. Occured at path " + infoFilePath);
                return;
            }
            Texture2D normaltex = null, italictex = null, boldtex = null;
            string normalpath = Path.Combine(dir, normal) + ".png";
            if (!File.Exists(normalpath))
            {
                Debug.LogError("[ProceduralObjects] Font error : Normal style for a font does not exist at the specified path, couldn't create it. Occured at path " + infoFilePath);
                return;
            }
            else
                normaltex = TextureUtils.LoadPNG(normalpath);
            if (italic != "")
            {
                string italicpath = Path.Combine(dir, italic) + ".png";
                if (File.Exists(italicpath))
                {
                    italictex = TextureUtils.LoadPNG(italicpath);
                }
            }
            if (bold != "")
            {
                string boldpath = Path.Combine(dir, bold) + ".png";
                if (File.Exists(boldpath))
                {
                    boldtex = TextureUtils.LoadPNG(boldpath);
                }
            }
            TextureFont font = null;
            if (italictex != null && boldtex != null)
                font = new TextureFont(name, normaltex, italictex, boldtex, spaceWidth);
            else
                font = new TextureFont(name, normaltex, spaceWidth);
            font.m_orderedChars = orderedChars;
            font.m_disableColorOverwriting = disableOverwriting;
            font.BuildFont();
            font.m_defaultSpacing = defaultSpacing;
            font.m_kerningNormal = kerningNormal;
            font.m_kerningItalic = kerningItalic;
            font.m_kerningBold = kerningBold;
            font.m_stylesNames = stylesNames;
            m_fonts.Add(font);
        }
    }
}
