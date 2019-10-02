using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using ProceduralObjects.Classes;
using ProceduralObjects.Localization;

namespace ProceduralObjects.ProceduralText
{
    public class TextCustomizationManager
    {
        public TextCustomizationManager(FontManager fManager)
        {
            windowRect = new Rect(175, 120, 400, 600);
            editingObject = null;
            showWindow = false;
            fontManager = fManager;
            updateTimer = 0f;
            dragTimer = 0f;
            zoomFactor = 1f;
            movingField = -1;
            dragTexPos = Vector2.zero;
        }

        public Rect windowRect;
        public bool showWindow;
        public ProceduralObject editingObject;
        public FontManager fontManager;
        public TextParameters parameters, parametersOld;

        public static readonly Color inactiveGrey = new Color(.2f, .2f, .2f, 1f);

        private Texture2D originalTex, windowTex;
        private Vector2 scrollParams, scrollTex, dragTexPos;
        private float updateTimer, dragTimer, zoomFactor;
        private int movingField;

        private TextureFont selectedCharTable = null;
        private Rect charTableRect = new Rect(575, 120, 421, 400);
        private Vector2 scrollCharTable = Vector2.zero;
        private string excludedChars = "";

        public void DrawWindow()
        {
            if (canDrawWindow)
                windowRect = GUI.Window(99043, windowRect, draw, LocalizationManager.instance.current["text_customization"]);
            if (selectedCharTable != null)
                charTableRect = GUI.Window(99044, charTableRect, drawCharTable, LocalizationManager.instance.current["char_table"]);
        }
        public void Update()
        {
            if (canDrawWindow)
            {
                /*
                if (showWindow)
                    GUIUtils.SetMouseScrolling(!windowRect.IsMouseInside());
                 * */
                updateTimer += TimeUtils.deltaTime;
                if (dragTimer < .14f)
                    dragTimer += TimeUtils.deltaTime;
                if (new Rect(windowRect.x + 5, windowRect.y + 30, 375, 285).IsMouseInside())
                {
                    if (movingField != -1)
                    {
                        parameters[movingField].x = (GUIUtils.MousePos.x - windowRect.x - 5 + scrollTex.x) / zoomFactor;
                        parameters[movingField].y = (GUIUtils.MousePos.y - windowRect.y - 30 + scrollTex.y) / zoomFactor;
                        if (Input.GetMouseButtonDown(0))
                        {
                            ProceduralObjectsLogic.PlaySound();
                            movingField = -1;
                        }
                    }
                    else
                    {
                        if (Input.GetMouseButton(0))
                        {
                            if (dragTimer >= .14f)
                            {
                                if (dragTexPos == Vector2.zero)
                                    dragTexPos = new Vector2((GUIUtils.MousePos.x - windowRect.x - 5 + scrollTex.x) / zoomFactor, (GUIUtils.MousePos.y - windowRect.y - 30 + scrollTex.y) / zoomFactor);
                            }
                        }
                    }
                }
                if (Input.GetMouseButton(0))
                {
                    if (dragTimer >= .14f)
                    {
                        if (dragTexPos != Vector2.zero)
                            scrollTex = new Vector2(zoomFactor * dragTexPos.x - GUIUtils.MousePos.x + windowRect.x + 5, zoomFactor * dragTexPos.y - GUIUtils.MousePos.y + windowRect.y + 30);
                    }
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    dragTexPos = Vector2.zero;
                    dragTimer = 0f;
                }
                if (updateTimer > .22f)
                {
                    if (TextParameters.IsDifference(parameters, parametersOld))
                    {
                        // apply changes
                        windowTex = parameters.ApplyParameters(originalTex);
                        editingObject.m_material.mainTexture = windowTex as Texture;
                        editingObject.m_textParameters = parameters;
                        // save textparameters to the editingObject instance
                    }
                    parametersOld = TextParameters.Clone(parameters, false);
                    updateTimer = 0f;
                }
            }
        }
        private void draw(int id)
        {
            GUI.DragWindow(new Rect(0, 0, 365, 28));
            if (GUI.Button(new Rect(370, 3, 28, 28), "X"))
            {
                ProceduralObjectsLogic.PlaySound();
                CloseWindow();
            }
            else
            {
                // texture display
                //  GUI.Box(new Rect(5, 30, 390, 300), string.Empty);
                //  GUI.Label(new Rect(10, 35, 380, 290), windowTex);
                scrollTex = GUI.BeginScrollView(new Rect(5, 30, 390, 300), scrollTex, new Rect(0, 0, windowTex.width * zoomFactor, windowTex.height * zoomFactor));
                GUI.DrawTexture(new Rect(0, 0, windowTex.width * zoomFactor, windowTex.height * zoomFactor), windowTex as Texture);

                if (movingField == -1)
                {
                    for (int i = 0; i < parameters.Count(); i++)
                    {
                        if (!parameters[i].locked)
                        {
                            if (GUI.Button(new Rect(parameters[i].x * zoomFactor, parameters[i].y * zoomFactor, parameters[i].texWidth * zoomFactor, parameters[i].texHeight * zoomFactor), string.Empty, GUI.skin.label))
                            {
                                ProceduralObjectsLogic.PlaySound();
                                movingField = i;
                            }
                        }
                    }
                }
                GUI.EndScrollView();

                GUI.Label(new Rect(11, 335, 260, 27), "<size=17>" + LocalizationManager.instance.current["text_fields"] + "</size>");
                GUI.Label(new Rect(285, 337, 220, 27), LocalizationManager.instance.current["zoom"]);
                if (GUI.RepeatButton(new Rect(340, 335, 28, 26), "<size=20><b>+</b></size>"))
                    zoomIn();
                if (GUI.RepeatButton(new Rect(370, 335, 28, 26), "<size=22><b>-</b></size>"))
                    zoomOut();
                // parameters box
                if (parameters.Count() == 0)
                    GUI.Box(new Rect(5, 363, 390, 232), string.Empty);
                scrollParams = GUI.BeginScrollView(new Rect(10, 368, 380, 222), scrollParams, new Rect(0, 0, 355, parameters.m_textFields.Where(p => !p.minimized).Count() * 125 + parameters.m_textFields.Where(p => p.minimized).Count() * 33 + 69));
                int j = 2;
                for (int i = 0; i < parameters.Count(); i++)
                {
                    bool minimized = parameters[i].minimized;
                    parameters[i].DrawUI(new Vector2(3, j), this, ShowCharTable, movingField == -1);
                    if (minimized)
                        j += 33;
                    else
                        j += 125;
                }
                if (GUI.Button(new Rect(3, j, 350, 30), "<b>+</b> " + LocalizationManager.instance.current["add_field"]))
                {
                    ProceduralObjectsLogic.PlaySound();
                    parameters.AddField(fontManager.Arial, 0);
                }
                if (GUI.Button(new Rect(3, j + 33, 350, 30), "<b>+</b> " + LocalizationManager.instance.current["add_color_rect"]))
                {
                    ProceduralObjectsLogic.PlaySound();
                    parameters.AddField(fontManager.Arial, 1);
                }
                GUI.EndScrollView();
            }
        }
        private void drawCharTable(int id)
        {
            GUI.DragWindow(new Rect(0, 0, 388, 28));
            GUI.Label(new Rect(7, 30, 385, 26), string.Format(LocalizationManager.instance.current["font_chars_available"], selectedCharTable.m_fontName));
            var height = (Mathf.FloorToInt(selectedCharTable.m_orderedChars.Length / 10) + 1) * 67 + (ProceduralObjectsMod.ShowDeveloperTools.value ? 52 : 6);
            scrollCharTable = GUI.BeginScrollView(new Rect(5, 57, 416, 338), scrollCharTable, new Rect(0, 0, 400, height));
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            for (int i = 0; i < selectedCharTable.m_orderedChars.Length; i++)
            {
                int line = Mathf.FloorToInt(i / 10);
                int leftOffset = i % 10;
                GUI.Box(new Rect(7 + leftOffset * 39, 3 + 67 * line, 35, 62), string.Empty);
                GUI.Label(new Rect(7 + leftOffset * 39, 2 + 67 * line, 35, 28), "<size=20>" + selectedCharTable.m_orderedChars[i] + "</size>");
                GUI.Label(new Rect(7 + leftOffset * 39, 30 + 67 * line, 35, 35), selectedCharTable.m_charTexturesNormal[selectedCharTable.m_orderedChars[i]]);
            }
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
            if (ProceduralObjectsMod.ShowDeveloperTools.value)
            {
                GUI.Label(new Rect(210, height - 45, 176, 20), "Excluded characters indexes");
                excludedChars = GUI.TextField(new Rect(210, height - 25, 176, 24), excludedChars);
                if (GUI.Button(new Rect(7, height - 45, 200, 44), "Calculate & Export Kerning data"))
                {
                    selectedCharTable.ExportKerning(excludedChars);
                }
            }
            GUI.EndScrollView();
            if (GUI.Button(new Rect(391, 3, 28, 28), "X"))
            {
                ProceduralObjectsLogic.PlaySound(); 
                CloseCharTable();
            }
        }
        public void ShowCharTable(TextureFont font)
        {
            if (selectedCharTable == null)
            {
                charTableRect.x = windowRect.x + 400;
                charTableRect.y = windowRect.y;
                scrollCharTable = Vector2.zero;
            }
            selectedCharTable = font;
        }
        public void CloseCharTable()
        {
            selectedCharTable = null;
            scrollCharTable = Vector2.zero;
        }
        public void CloseWindow()
        {
            CloseCharTable();
            showWindow = false;
            editingObject = null;
            originalTex = null;
            windowTex = null;
            parameters = null;
            parametersOld = null;
            scrollParams = Vector2.zero;
            scrollTex = Vector2.zero;
            updateTimer = 0f;
            dragTimer = 0f;
            zoomFactor = 1f;
            movingField = -1;
            dragTexPos = Vector2.zero;
            // GUIUtils.SetMouseScrolling(true);
        }
        public void Edit(ProceduralObject obj, Vector2 position)
        {
            editingObject = obj;
            scrollParams = Vector2.zero;
            scrollTex = Vector2.zero;
            movingField = -1;
            dragTexPos = Vector2.zero;
            windowRect.position = position;
            Texture tex = ProceduralUtils.GetOriginalTexture(obj);
            originalTex = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
            originalTex.SetPixels(((Texture2D)tex).GetPixels());
            windowTex = (Texture2D)GameObject.Instantiate(originalTex);
            // load stored data if it exists
            if (editingObject.m_textParameters == null)
            {
                parameters = new TextParameters();
                parametersOld = new TextParameters();
            }
            else
            {
                parameters = editingObject.m_textParameters;
                parametersOld = TextParameters.Clone(editingObject.m_textParameters, false);
                windowTex = parameters.ApplyParameters(originalTex);
                editingObject.m_material.mainTexture = windowTex as Texture;
            }
            updateTimer = 0f;
            dragTimer = 0f;
            zoomFactor = 1f;
            if (!showWindow)
                showWindow = true;
        }
        private bool canDrawWindow
        {
            get { return showWindow && windowTex != null && editingObject != null; }
        }
        private void zoomIn()
        {
            if (zoomFactor < 6f)
                zoomFactor += TimeUtils.deltaTime * .45f;
        }
        private void zoomOut()
        {
            if (zoomFactor >= .08f)
                zoomFactor -= TimeUtils.deltaTime * .45f;
        }
        public static int SelectedStyle(FontStyle s)
        {
            if (s == FontStyle.Bold)
                return 1;
            if (s == FontStyle.Italic)
                return 2;
            return 0;
        }
        public static FontStyle IntToStyle(int i)
        {
            if (i == 1)
                return FontStyle.Bold;
            if (i == 2)
                return FontStyle.Italic;
            return FontStyle.Normal;
        }
    }
}
