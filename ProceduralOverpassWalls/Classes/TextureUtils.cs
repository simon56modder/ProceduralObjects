using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using ColossalFramework.IO;
using ColossalFramework.PlatformServices;
using UnityEngine;

namespace ProceduralObjects.Classes
{
    public static class TextureUtils
    {
        public static List<TextureResourceInfo> TextureResources = new List<TextureResourceInfo>();
        public static int LocalTexturesCount = 0;

        public static List<Texture2D> LoadModConfigTextures(this List<Texture2D> textures)
        {
            Debug.Log("[ProceduralObjects] Texture Loading : Started local texture loading.");
            textures = new List<Texture2D>();

            // local textures loading
            if (!Directory.Exists(ProceduralObjectsMod.TextureConfigPath))
            {
                Directory.CreateDirectory(ProceduralObjectsMod.TextureConfigPath);
                Debug.Log("[ProceduralObjects] Local Texture Loading : ModConfig directory doesn't exist ! Creating it and skipping texture loading.");
            }
            else
            {
                foreach (string file in Directory.GetFiles(ProceduralObjectsMod.TextureConfigPath, "*.png", SearchOption.AllDirectories))
                {
                    if (!File.Exists(file))
                        continue;
                    try
                    {
                        textures.Add(LoadPNG(file));
                    }
                    catch
                    {
                        Debug.LogError("[ProceduralObjects] Texture Loading Error : Failed to load a texture at " + file + " !");
                    }
                }
                LocalTexturesCount = textures.Count;
                Debug.Log("[ProceduralObjects] Texture Loading : Successfully loaded " + LocalTexturesCount + @" texture(s) from ModConfig\ProceduralObjects");
            }

            // workshop textures loading
            Debug.Log("[ProceduralObjects] Texture Loading : Starting Workshop textures loading.");
            TextureResources = new List<TextureResourceInfo>();
            foreach (PublishedFileId fileId in PlatformService.workshop.GetSubscribedItems())
            {
                string path = PlatformService.workshop.GetSubscribedItemPath(fileId);
                string infoPath = ProceduralObjectsMod.IsLinux ? path + @"/ProceduralObjectsTextures.cfg" : path + @"\ProceduralObjectsTextures.cfg";
                if (File.Exists(infoPath))
                {
                    var texResource = new TextureResourceInfo();
                    texResource.m_fullPath = path;
                    string[] files = File.ReadAllLines(infoPath);
                    for (int i = 0; i < files.Count(); i++)
                    {
                        if (files[i].Contains("name = "))
                        {
                            texResource.m_name = files[i].Replace("name = ", "");
                        }
                        else
                        {
                            if (File.Exists(path + (ProceduralObjectsMod.IsLinux ? "/" : @"\") + files[i] + ".png"))
                            {
                                var tex = LoadPNG(path + (ProceduralObjectsMod.IsLinux ? "/" : @"\") + files[i] + ".png");
                                textures.Add(tex);
                                texResource.m_textures.Add(tex);
                            }
                            else
                            {
                                Debug.LogError("[ProceduralObjects] Workshop texture : a file marked as a PO texture was not found at " + path + (ProceduralObjectsMod.IsLinux ? "/" : @"\") + files[i] + ".png");
                                texResource.m_failedToLoadTextures += 1;
                            }
                        }
                    }
                    TextureResources.Add(texResource);
                }
            }
            Debug.Log("[ProceduralObjects] Texture Loading : Successfully ended. " + TextureResources.Count + " workshop texture-containing folders loaded.");
            return textures;
        }

        public static Texture2D LoadPNG(string filePath, bool attributeName = true)
        {
            Texture2D tex = null;
            byte[] fileData;

            if (File.Exists(filePath))
            {
                fileData = File.ReadAllBytes(filePath);
                tex = new Texture2D(2, 2);
                tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
                if (attributeName)
                    tex.name = filePath;
            }
            return tex;
        }

        public static Texture2D LoadTextureFromAssembly(string filename)
        {
            Stream manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ProceduralObjects.Icons." + filename + ".png");

            byte[] array = new byte[manifestResourceStream.Length];
            manifestResourceStream.Read(array, 0, array.Length);

            Texture2D texture2D = new Texture2D(2, 2, TextureFormat.ARGB32, false);
            texture2D.LoadImage(array);

            return texture2D;
        }

        public static Texture2D RotateRight(Texture2D originalTexture)
        {
            Color32[] original = originalTexture.GetPixels32();
            Color32[] rotated = new Color32[original.Length];
            int w = originalTexture.width;
            int h = originalTexture.height;

           // int iRotated, iOriginal;

            for (int j = 0; j < h; ++j)
            {
                for (int i = 0; i < w; ++i)
                {
                    rotated[(i + 1) * h - j - 1] = original[original.Length - 1 - (j * w + i)];
                }
            }

            Texture2D rotatedTexture = new Texture2D(h, w);
            rotatedTexture.SetPixels32(rotated);
            rotatedTexture.Apply();
            return rotatedTexture;
        }

        public static Texture2D PlainTexture(int width, int height, Color color)
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

        public static void PrintRectangle(Texture2D originalTex, int x, int y, int width, int height, Color color)
        {
            var oldY = y;
            y = originalTex.height - y - height;
            for (int i = 1; i <= width; i++)
            {
                if (i + x < originalTex.width)
                {
                    for (int j = 1; j <= height; j++)
                    {
                        if (y + j >= 0)
                        {
                            /*
                            if (haveBorder)
                            {
                                bool isLeftBorder = (i >= bDistance && i < bDistance + bWidth) && (j <= originalTex.height - bDistance && j >= bDistance);
                                bool isRightBorder = (i > originalTex.width - bDistance - bWidth && i <= originalTex.width - bDistance) && (j <= originalTex.height - bDistance && j >= bDistance);
                                bool isTopBorder = (j >= bDistance && j < bDistance + bWidth) && (i <= originalTex.width - bDistance && i >= bDistance);
                                bool isBottomBorder = (j > originalTex.height - bDistance - bWidth && j <= originalTex.height - bDistance) && (i <= originalTex.width - bDistance && i >= bDistance);
                                if (isLeftBorder || isRightBorder || isTopBorder || isBottomBorder)
                                {
                                    float alpha = color.a;
                                    color = bColor;
                                    color.a = alpha;
                                }
                            } */
                            if (color.a < 1)
                                originalTex.SetPixel(i + x, j + y, AverageColor(originalTex.GetPixel(i + x, j + y), color));
                            else
                                originalTex.SetPixel(i + x, j + y, color);
                        }
                    }
                }
            }
            originalTex.Apply();
        }

        public static Color AverageColor(Color original, Color newColor)
        {
            return new Color(
                ((original.r * (100 - (newColor.a * 100))) + (newColor.r * (newColor.a * 100))) / 100,
                ((original.g * (100 - (newColor.a * 100))) + (newColor.g * (newColor.a * 100))) / 100,
                ((original.b * (100 - (newColor.a * 100))) + (newColor.b * (newColor.a * 100))) / 100,
                1);
        }
    }
    public class TextureResourceInfo
    {
        public TextureResourceInfo()
        {
            m_textures = new List<Texture2D>();
            m_name = string.Empty;
            m_failedToLoadTextures = 0;
        }
        public string m_name, m_fullPath;
        public List<Texture2D> m_textures;
        public int m_failedToLoadTextures;
        public int TexturesCount
        {
            get
            {
                if (m_textures != null)
                    return m_textures.Count;
                else
                    return 0;
            }
        }
        public bool HasCustomName
        {
            get { return (m_name != string.Empty); }
        }

        public static int TotalTextureCount(List<TextureResourceInfo> texResInfoList)
        {
            int i = 0;
            foreach (TextureResourceInfo info in texResInfoList)
            {
                i += info.m_textures.Count;
            }
            return i;
        }
    }
}
