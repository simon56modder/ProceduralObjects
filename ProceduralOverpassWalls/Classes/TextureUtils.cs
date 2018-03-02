using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            if (!Directory.Exists(ProceduralObjectsMod.ModConfigPath))
            {
                Directory.CreateDirectory(ProceduralObjectsMod.ModConfigPath);
                Debug.Log("[ProceduralObjects] Local Texture Loading : ModConfig directory doesn't exist ! Creating it and skipping texture loading.");
            }
            else
            {
                foreach (string file in Directory.GetFiles(ProceduralObjectsMod.ModConfigPath, "*.png", SearchOption.AllDirectories))
                {
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
