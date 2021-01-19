using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using ColossalFramework;
using ColossalFramework.IO;
using ColossalFramework.UI;
using ColossalFramework.PlatformServices;
using UnityEngine;

using ProceduralObjects.Localization;
using ProceduralObjects.UI;

namespace ProceduralObjects.Classes
{
    public class TextureManager
    {
        public TextureManager()
        {
            instance = this;
            scrollTextureResources = Vector2.zero;
            winrect = new Rect(355, 500, 390, 330);
        }

        public static TextureManager instance;

        public List<TextureResourceInfo> TextureResources = new List<TextureResourceInfo>();
        public TextureResourceInfo LocalTextures;
        public Vector2 scrollTextureResources;

        public int LocalTexturesCount = 0, TotalTexturesCount = 0;

        public bool showWindow;
        private Rect winrect;


        public void DrawWindow()
        {
            if (showWindow)
                winrect = GUIUtils.ClampRectToScreen(GUIUtils.Window(9900498, winrect, draw, LocalizationManager.instance.current["texture_management"]));
        }
        private void draw(int id)
        {
            if (GUIUtils.CloseHelpButtons(winrect, "Texture_Management"))
                showWindow = false;
            GUI.DragWindow(new Rect(0, 0, 360, 26));

            /*  if (TextureManager.instance.LocalTexturesCount == 0 && TextureManager.instance.TextureResources.Count == 0)
                    GUI.Label(new Rect(10, 45, 350, 45), LocalizationManager.instance.current["no_tex"] + "\n" + LocalizationManager.instance.current["cant_create_basic"]);
                else */
            GUI.Label(new Rect(10, 28, 350, 30), LocalizationManager.instance.current["local_tex"] + " : ");

            if (GUI.Button(new Rect(150, 27, 75, 28), LocalizationManager.instance.current["refresh"]))
            {
                ProceduralObjectsLogic.PlaySound();
                LoadTextures();
                // basicTextures = basicTextures.LoadTextures();
            }
            if (GUI.Button(new Rect(230, 27, 155, 28), LocalizationManager.instance.current["open_tex"]))
            {
                ProceduralObjectsLogic.PlaySound();
                if (Directory.Exists(ProceduralObjectsMod.TextureConfigPath))
                    Application.OpenURL("file://" + ProceduralObjectsMod.TextureConfigPath);
            }

            if (TextureResources.Count > 0)
            {
                GUI.Label(new Rect(10, 60, 375, 28), LocalizationManager.instance.current["wk_tex_loaded"] + " : " + TextureResources.Count.ToString());
                GUI.Box(new Rect(10, 85, 375, 170), string.Empty);
                scrollTextureResources = GUI.BeginScrollView(new Rect(10, 85, 375, 170), scrollTextureResources, new Rect(0, 0, 350, TextureResources.Count * 30));
                for (int i = 0; i < TextureResources.Count; i++)
                {
                    GUI.Label(new Rect(5, i * 30, 248, 28), TextureResources[i].HasCustomName ? TextureResources[i].m_name : "<i>" + LocalizationManager.instance.current["package_no_custom_name"] + "</i>");
                    GUI.Label(new Rect(255, i * 30, 99, 28), (TextureManager.instance.TextureResources[i].TexturesCount > 1)
                        ? "(" + TextureResources[i].TexturesCount + " " + LocalizationManager.instance.current["textures"] + ")"
                        : "(" + TextureResources[i].TexturesCount + " " + LocalizationManager.instance.current["texture"] + ")");
                }
                GUI.EndScrollView();
            }
            else
                GUI.Label(new Rect(10, 55, 375, 28), LocalizationManager.instance.current["no_wk_tex_loaded"]);

            GUI.Label(new Rect(10, 259, 375, 22), TotalTexturesCount.ToString() + " " + LocalizationManager.instance.current["tex_in_total"] + " : " +
                LocalTexturesCount.ToString() + " " + LocalizationManager.instance.current["local"]
                + " + " + (TotalTexturesCount - LocalTexturesCount) + " " + LocalizationManager.instance.current["from_wk"]);

            if (GUI.Button(new Rect(5, 284, 380, 40), LocalizationManager.instance.current["prepare_texPackSave"]))
            {
                ProceduralObjectsLogic.PlaySound();
                ProceduralObjectsLogic.instance.generalShowUI = false;
                ConfirmPanel.ShowModal(LocalizationManager.instance.current["prepare_texPackSave_title"],
                    LocalizationManager.instance.current["prepare_texPackSave_confirm"],
                    delegate(UIComponent comp, int ret)
                    {
                        if (ret ==1)
                            PrepareTexPackAndSave();
                        ProceduralObjectsLogic.instance.generalShowUI = true;
                    });
            }
        }

        public void LoadTextures()
        {
            Debug.Log("[ProceduralObjects] Texture Loading : Started local texture loading.");
         //   textures = new List<Texture2D>();
            TextureResources = new List<TextureResourceInfo>();

            // local textures loading
            LocalTextures = new TextureResourceInfo();
            if (!Directory.Exists(ProceduralObjectsMod.TextureConfigPath))
            {
                if (Directory.Exists(ProceduralObjectsMod.OldTextureConfigPath))
                {
                    try
                    {
                        Directory.Move(ProceduralObjectsMod.OldTextureConfigPath, ProceduralObjectsMod.TextureConfigPath);
                        Debug.Log("[ProceduralObjects] Found old textures externals directory, moving to the new.");
                        LoadLocalTextures();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("[ProceduralObjects] Failed to move the old textures directory to the new one : " + e);
                        Directory.CreateDirectory(ProceduralObjectsMod.TextureConfigPath);
                    }

                }
                else
                {
                    Directory.CreateDirectory(ProceduralObjectsMod.TextureConfigPath);
                    Debug.Log("[ProceduralObjects] Local Texture Loading : directory doesn't exist ! Creating it and skipping texture loading.");
                }
            }
            else
            {
                LoadLocalTextures();
            }

            // workshop textures loading
            Debug.Log("[ProceduralObjects] Texture Loading : Starting Workshop textures loading.");
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
                                var tex = TextureUtils.LoadPNG(path + (ProceduralObjectsMod.IsLinux ? "/" : @"\") + files[i] + ".png");
                           //   textures.Add(tex);
                                tex.name = texResource.m_name + "/" + files[i];
                                texResource.m_textures.Add(tex);
                                TotalTexturesCount += 1;
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
         // return textures;
        }
        public void LoadLocalTextures()
        {
            LocalTextures = new TextureResourceInfo();
            LocalTextures.m_name = LocalizationManager.instance.current["local_tex"];
            foreach (string file in Directory.GetFiles(ProceduralObjectsMod.TextureConfigPath, "*.png", SearchOption.AllDirectories))
            {
                if (!File.Exists(file))
                    continue;
                try
                {
                    var tex = TextureUtils.LoadPNG(file);
                    // textures.Add(tex);
                    int pos = file.LastIndexOf(ProceduralObjectsMod.IsLinux ? "/" : @"\") + 1;
                    tex.name = "LOCALFOLDER/" + file.Substring(pos, file.Length - pos).Replace(".png", "");
                    LocalTextures.m_textures.Add(tex);
                    TotalTexturesCount += 1;
                    LocalTexturesCount += 1;
                }
                catch
                {
                    Debug.LogError("[ProceduralObjects] Texture Loading Error : Failed to load a texture at " + file + " !");
                    LocalTextures.m_failedToLoadTextures += 1;
                }
            }
            //   LocalTexturesCount = textures.Count;
            TextureResources.Add(LocalTextures);
            Debug.Log("[ProceduralObjects] Texture Loading : Successfully loaded " + LocalTexturesCount + @" texture(s) from ProceduralObjects\Textures");
        }
        public void MinimizeAll()
        {
            foreach (TextureResourceInfo info in TextureResources)
            {
                info.minimized = true;
            }
        }
        public void SetPosition(float x, float y)
        {
            this.winrect.position = new Vector2(x, y);
        }

        public int GetShownHeight()
        {
        //  80 * TextureManager.instance.TotalTexturesCount + 142
            int i = 146;
            foreach (var texResInfo in TextureResources)
            {
                i += 30;
                if (!texResInfo.minimized)
                    i += 80 * texResInfo.TexturesCount;
            }
            return i;
        }
        public Texture FindTexture(string input)
        {
            if (input.Trim().ToLower() == "null")
            {
                return null;
            }
            if (input.Contains(@"Cities_Skylines\ModConfig\ProceduralObjects") || input.Contains("Cities_Skylines/ModConfig/ProceduralObjects"))
            {
                /*
                if (File.Exists(input))
                {
                    try
                    {
                        return TextureUtils.LoadPNG(input);
                    }
                    catch
                    {
                        Debug.LogError("[ProceduralObjects] File data loading failure : could not load a custom texture at path " + input + " therefore loading the default object texture");
                        return null;
                    }
                }
                else */
                    return GetTextureFromName(input, true);
            }
            else
                return GetTextureFromName(input, false);
        }
        private Texture GetTextureFromName(string input, bool apparentlyPath)
        {
          begin:
            if (apparentlyPath)
            {
                int pos = input.LastIndexOf(ProceduralObjectsMod.IsLinux ? "/" : @"\") + 1;
                string name = input.Substring(pos, input.Length - pos).Replace(".png", "");
                foreach (TextureResourceInfo info in TextureResources)
                {
                    foreach (Texture tex in info.m_textures)
                    {
                        if (tex.name.EndsWith(name))
                            return tex;
                    }
                }
                foreach (TextureResourceInfo info in TextureResources)
                {
                    foreach (Texture tex in info.m_textures)
                    {
                        if (tex.name.Contains(name))
                            return tex;
                    }
                }
            }
            else
            {
                if (input.StartsWith("LOCALFOLDER/"))
                {
                    if (LocalTextures.m_textures.Any(tex => tex.name == input))
                        return LocalTextures.m_textures.First(tex => tex.name == input);
                }
                else
                {
                    foreach (TextureResourceInfo info in TextureResources)
                    {
                        if (LocalTextures == info)
                            continue;
                        if (!input.Contains(info.m_name))
                            continue;
                        foreach (Texture tex in info.m_textures)
                        {
                            if (tex.name == input)
                                return tex;
                        }
                    }
                }
            }

            if (!apparentlyPath)
            {
                // if reach there then try once more as apparentlyPath
                apparentlyPath = true;
                goto begin;
            }

            Debug.LogError("[ProceduralObjects] Workshop texture : a file marked as a PO texture was not found with input name \"" + input + "\"");
            return null;
        }

        private void PrepareTexPackAndSave()
        {
            string name = Singleton<SimulationManager>.instance.m_metaData.m_CityName;

            if (!Directory.Exists(ProceduralObjectsMod.PODirectoryPath + "SavesTexturePacks"))
                Directory.CreateDirectory(ProceduralObjectsMod.PODirectoryPath + "SavesTexturePacks");

            string linuxSafeSlash = (ProceduralObjectsMod.IsLinux ? "/" : @"\");
            var saveDir = ProceduralObjectsMod.PODirectoryPath + "SavesTexturePacks" + linuxSafeSlash + name;
            if (Directory.Exists(saveDir))
            {
                Debug.LogError("[ProceduralObjects] Failed to run PrepareTexPackAndSave() because the corresponding save folder already exists.");
                Directory.Delete(saveDir, true);
                return;
            }
            Directory.CreateDirectory(saveDir);

            List<string> alreadyAddedFiles = new List<string>();

            // SCAN POs, copy texture files to the saveDir, implement them in alreadyAddedFiles
            foreach (ProceduralObject obj in ProceduralObjectsLogic.instance.proceduralObjects)
            {
                if (obj.customTexture == null)
                    continue;
                if (!obj.customTexture.name.Contains("LOCALFOLDER/"))
                    continue;

                obj.customTexture.name = obj.customTexture.name.Replace("LOCALFOLDER/", name + " textures/");

                if (alreadyAddedFiles.Contains(obj.customTexture.name))
                    continue;

                var rootName = obj.customTexture.name.Replace(name + " textures/", "");
                var path = Directory.GetFiles(ProceduralObjectsMod.TextureConfigPath, "*.png", SearchOption.AllDirectories)
                    .FirstOrDefault(tex => tex.Contains(rootName));
                var dirPath = saveDir + linuxSafeSlash + rootName + ".png";
                File.Copy(path, dirPath);
                File.SetAttributes(dirPath, FileAttributes.Normal);
                alreadyAddedFiles.Add(obj.customTexture.name);
            }

            // WRITE THE CONFIG FILE
            TextWriter tw = new StreamWriter(saveDir + linuxSafeSlash + "ProceduralObjectsTextures.cfg");
            tw.WriteLine("name = " + name + " textures");
            for (int i = 0; i < alreadyAddedFiles.Count; i++)
                tw.WriteLine(alreadyAddedFiles[i].Replace(name + " textures/", ""));
            tw.Close();
        }
    }
    public static class TextureUtils
    {
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
        public static Texture2D LoadTextureFromAssembly(string filename, string assemblyName)
        {
            Stream manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(assemblyName + ".Icons." + filename + ".png");

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

        public static Color KeepAlphaFrom(this Color src, Color alphaSrc)
        {
            return new Color(src.r, src.g, src.b, alphaSrc.a);
        }
    }
    public class TextureResourceInfo
    {
        public TextureResourceInfo()
        {
            minimized = true;
            m_textures = new List<Texture>();
            m_name = string.Empty;
            m_failedToLoadTextures = 0;
        }
        public string m_name, m_fullPath;
        public List<Texture> m_textures;
        public int m_failedToLoadTextures;
        public bool minimized;
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

    }
}
