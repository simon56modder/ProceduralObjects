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
    public class TextCustomizationManager
    {
        public TextCustomizationManager(FontManager fManager)
        {
           // windowRect = new Rect(175, 120, 400, 600);
            windowRect = new Rect(175, 120, 600, 500);
            editingObject = null;
            showWindow = false;
            fontManager = fManager;
            placingRect = false;
            placingText = false;
            updateTimer = 0f;
            dragTimer = 0f;
            zoomFactor = 1f;
            movingField = -1;
            separatorListEditionZone = 260f;
            dragTexPos = Vector2.zero;
            placingRectFirstpoint = Vector2.down;
            colorPickerSelected = null;
            selectedField = null;
            instance = this;
        }

        public static TextCustomizationManager instance;

        public Rect windowRect;
        public bool showWindow;
        public ProceduralObject editingObject;
        public FontManager fontManager;
        public TextParameters parameters, parametersOld;
        public TextField selectedField;
        public TextField copiedField;

        private Texture2D originalTex;
        public Texture2D windowTex;
        private Vector2 scrollParams, scrollTex, dragTexPos;
        private float updateTimer, dragTimer, zoomFactor, separatorListEditionZone;
        private int movingField;
        private bool _justPlacedTextNowFocusField ;
        private Vector2 placingRectFirstpoint;
        public bool cursorIsInsideTextureArea, placingText, placingRect;
        public GUIPainter colorPickerSelected;

        public TextureFont selectedCharTable = null;
        public Rect charTableRect = new Rect(575, 120, 421, 400);
        private Vector2 scrollCharTable = Vector2.zero;
        private string excludedChars = "";

        public void DrawWindow()
        {
            if (canDrawWindow)
            {
                windowRect = GUIUtils.Window(99043, windowRect, draw, LocalizationManager.instance.current["text_customization"]);
                if (colorPickerSelected != null)
                {
                    colorPickerSelected.pickerPosition = new Vector2(windowRect.xMax + 2, windowRect.y + 265);
                    GUIPainter.DrawPicker(colorPickerSelected, Color.HSVToRGB(colorPickerSelected.H, colorPickerSelected.S, colorPickerSelected.V));
                }
            }
            if (selectedCharTable != null)
                charTableRect = GUIUtils.Window(99044, charTableRect, drawCharTable, LocalizationManager.instance.current["char_table"]);
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
                var mousePos = GUIUtils.MousePos;
                if (colorPickerSelected != null)
                {
                    GUIPainter.UpdatePainter(colorPickerSelected, () => { colorPickerSelected = null; });
                }

                if (dragTimer < .14f && dragTimer != 0f)
                    dragTimer += TimeUtils.deltaTime;
                cursorIsInsideTextureArea = windowRect.IsMouseInside();
                if (new Rect(windowRect.x + 5, windowRect.y + 30, windowRect.width - 285, windowRect.height - 80).IsMouseInside())
                {
                    if (Input.GetKey(KeyCode.LeftControl))
                    {
                        zoomFactor *= 1f + Input.mouseScrollDelta.y * 0.3f;
                    }

                    if (placingText)
                    {
                        if (Input.GetMouseButtonDown(0))
                        {
                            var field = parameters.AddField(fontManager.Arial, 0);
                            field.x = (mousePos.x - windowRect.x - 5 + scrollTex.x) / zoomFactor;
                            field.y = (mousePos.y - windowRect.y - 30 + scrollTex.y) / zoomFactor;
                            selectedField = field;
                            placingText = false;
                            _justPlacedTextNowFocusField = true;
                        }
                    }
                    else if (placingRect)
                    {
                        if (placingRectFirstpoint != Vector2.down)
                        {
                            var field = selectedField;
                            var secondPoint = new Vector2((mousePos.x - windowRect.x - 5 + scrollTex.x) / zoomFactor,
                                (mousePos.y - windowRect.y - 30 + scrollTex.y) / zoomFactor);
                            field.x = Mathf.Min(placingRectFirstpoint.x, secondPoint.x);
                            field.y = Mathf.Min(placingRectFirstpoint.y, secondPoint.y);
                            field.m_width = (uint)Mathf.RoundToInt(Mathf.Abs(secondPoint.x - placingRectFirstpoint.x));
                            field.m_height = (uint)Mathf.RoundToInt(Mathf.Abs(secondPoint.y - placingRectFirstpoint.y));
                        }
                        if (Input.GetMouseButtonDown(0))
                        {
                            if (placingRectFirstpoint == Vector2.down)
                            {
                                ProceduralObjectsLogic.PlaySound();
                                var field = parameters.AddField(fontManager.Arial, 1);
                                placingRectFirstpoint = new Vector2((mousePos.x - windowRect.x - 5 + scrollTex.x) / zoomFactor,
                                    (mousePos.y - windowRect.y - 30 + scrollTex.y) / zoomFactor);
                                field.x = placingRectFirstpoint.x;
                                field.y = placingRectFirstpoint.y;
                                selectedField = field;
                            }
                            else
                            {
                                ProceduralObjectsLogic.PlaySound();
                                placingRectFirstpoint = Vector2.down;
                                placingRect = false;
                            }
                        }
                    }
                    else
                    {
                        if (movingField > -1)
                        {
                            parameters[movingField].x = ((mousePos.x - windowRect.x - 5 + scrollTex.x) / zoomFactor) - dragTexPos.x;
                            parameters[movingField].y = ((mousePos.y - windowRect.y - 30 + scrollTex.y) / zoomFactor) - dragTexPos.y;
                            if (Input.GetMouseButtonDown(0))
                            {
                                ProceduralObjectsLogic.PlaySound();
                                dragTexPos = Vector2.zero;
                                movingField = -1;
                            }
                        }
                        else if (movingField == -1)
                        {
                            if (Input.GetMouseButtonDown(0))
                                dragTimer = 0.0001f;
                            if (Input.GetMouseButton(0))
                            {
                                if (dragTimer >= .14f)
                                {
                                    if (dragTexPos == Vector2.zero)
                                        dragTexPos = new Vector2((mousePos.x - windowRect.x - 5 + scrollTex.x) / zoomFactor, (mousePos.y - windowRect.y - 30 + scrollTex.y) / zoomFactor);
                                }
                            }
                        }
                    }
                }

                if (Input.GetMouseButton(0))
                {
                    if (movingField == -2)
                    {
                        var size = mousePos - windowRect.position;
                        windowRect.size = new Vector2(Mathf.Max(windowRect.x + 400, size.x), Mathf.Max(windowRect.y + 350, size.y));
                    }
                    else if (movingField == -3)
                    {
                        separatorListEditionZone = Mathf.Clamp(mousePos.y - windowRect.y, 125, 450);
                    }
                    else if (dragTimer >= .14f)
                    {
                        if (dragTexPos != Vector2.zero)
                            scrollTex = new Vector2(zoomFactor * dragTexPos.x - mousePos.x + windowRect.x + 5, zoomFactor * dragTexPos.y - mousePos.y + windowRect.y + 30);
                    }
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    if (movingField <= -2)
                        movingField = -1;
                    
                    if (movingField <= -1)
                        dragTexPos = Vector2.zero;
                    dragTimer = 0f;
                }
                else
                {
                    if (movingField == -2)
                        movingField = -1;
                }
                if (updateTimer > .1f)
                {
                    if (TextParameters.IsDifference(parameters, parametersOld))
                    {
                        var oldTex = windowTex;
                        // apply changes
                        windowTex = parameters.ApplyParameters(originalTex);
                        editingObject.m_material.mainTexture = windowTex as Texture;
                        // save textparameters to the editingObject instance
                        editingObject.m_textParameters = parameters;
                        // try save on RAM usage
                        if (!TextParameters.IsEmpty(parametersOld))
                            oldTex.DisposeTexFromMemory();
                    }
                    parametersOld = TextParameters.Clone(parameters, false);
                    updateTimer = 0f;
                }
            }
        }
        private void draw(int id)
        {
            GUI.DragWindow(new Rect(0, 0, windowRect.width - 52, 28));
            if (GUIUtils.CloseHelpButtons(windowRect, "Text_Customization"))
                CloseWindow();
            else
            {
                var scrollview = GUI.BeginScrollView(new Rect(5, 30, windowRect.width - 265, windowRect.height - 60), scrollTex, new Rect(0, 0, windowTex.width * zoomFactor, windowTex.height * zoomFactor));
                if (!Input.GetKey(KeyCode.LeftControl)) 
                    scrollTex = scrollview;
                GUI.DrawTexture(new Rect(0, 0, windowTex.width * zoomFactor, windowTex.height * zoomFactor), windowTex as Texture);

                if (movingField == -1)
                {
                    if (!placingRect && !placingText)
                    {
                        for (int i = 0; i < parameters.Count(); i++)
                        {
                            if (parameters[i].locked)
                                continue;
                            if (GUI.Button(new Rect(parameters[i].x * zoomFactor, parameters[i].y * zoomFactor, parameters[i].texWidth * zoomFactor, parameters[i].texHeight * zoomFactor), string.Empty, GUI.skin.label))
                            {
                                ProceduralObjectsLogic.PlaySound();
                                dragTexPos = new Vector2(((GUIUtils.MousePos.x - windowRect.x - 5 + scrollTex.x) / zoomFactor) - parameters[i].x,
                                    ((GUIUtils.MousePos.y - windowRect.y - 30 + scrollTex.y) / zoomFactor) - parameters[i].y);
                                movingField = i;
                                placingText = false;
                                placingRect = false;
                            }
                        }
                    }
                }
                GUI.EndScrollView();

                // zoom
                if (GUI.RepeatButton(new Rect(5, windowRect.height - 29, 27, 25), "<size=20><b>+</b></size>"))
                    zoomIn();
                if (GUI.RepeatButton(new Rect(35, windowRect.height - 29, 27, 25), "<size=22><b>-</b></size>"))
                    zoomOut();
                GUI.Label(new Rect(67, windowRect.height - 28, 220, 27), LocalizationManager.instance.current["zoom"]);

                // parameters box
                GUI.Label(new Rect(windowRect.width - 255, 32, 260, 27), "<size=17>" + LocalizationManager.instance.current["text_fields"] + "</size>");

                // copy
                if (copiedField == null)
                {
                    GUI.Box(new Rect(windowRect.width - 35, 32, 25, 23), ProceduralObjectsMod.Icons[0]);
                }
                else
                {
                    if (GUI.Button(new Rect(windowRect.width - 35, 32, 25, 23), ProceduralObjectsMod.Icons[0]))
                    {
                        ProceduralObjectsLogic.PlaySound();
                        selectedField = parameters.AddField(TextField.Clone(copiedField, false));
                    }
                }
                if (parameters.Count() == 0)
                    GUI.Box(new Rect(windowRect.width - 257, 60, 387, separatorListEditionZone - 60), string.Empty);
                scrollParams = GUI.BeginScrollView(new Rect(windowRect.width - 256, 62, 254, separatorListEditionZone - 64), scrollParams, new Rect(0, 0, 235, parameters.Count() * 33 + 69));
                int j = 2;
                for (int i = 0; i < parameters.Count(); i++)
                {
                    var param = parameters[parameters.Count() - i - 1];
                    if (param.UIButton(new Vector2(0, j), this, movingField == -1))
                    {
                        if (selectedField == param)
                            selectedField = null;
                        else
                            selectedField = param;
                        placingRect = false;
                        placingText = false;
                        placingRectFirstpoint = Vector2.down;
                    }
                    j += 33;
                }
                if (GUI.Button(new Rect(3, j, 245, 30), "<b>+</b> " + LocalizationManager.instance.current["add_field"]))
                {
                    ProceduralObjectsLogic.PlaySound();
                    placingText = true;
                    placingRect = false;
                    placingRectFirstpoint = Vector2.down;
                    // selectedField = parameters.AddField(fontManager.Arial, 0);
                }
                if (GUI.Button(new Rect(3, j + 33, 245, 30), "<b>+</b> " + LocalizationManager.instance.current["add_color_rect"]))
                {
                    ProceduralObjectsLogic.PlaySound();
                    placingRect = true;
                    placingText = false;
                    placingRectFirstpoint = Vector2.down;
                    // selectedField = parameters.AddField(fontManager.Arial, 1);
                }
                GUI.EndScrollView();

                if (GUI.RepeatButton(new Rect(windowRect.width - 257, separatorListEditionZone - 1.5f, 387, 6), string.Empty))
                    movingField = -3;

                GUI.Box(new Rect(windowRect.width - 257, separatorListEditionZone + 5, 387, windowRect.height - separatorListEditionZone - 18), string.Empty);
                if (selectedField != null)
                {
                    selectedField.DrawUI(new Rect(windowRect.width - 255, separatorListEditionZone + 9, 245, windowRect.height - 290), this, ShowCharTable);
                    if (_justPlacedTextNowFocusField)
                    {
                        GUI.FocusControl("TextFieldPOTextCustom");
                        _justPlacedTextNowFocusField = false;
                    }
                }

                if (GUI.RepeatButton(new Rect(windowRect.width - 17, windowRect.height - 12, 16, 11), string.Empty))
                    movingField = -2;
            }
        }
        private void drawCharTable(int id)
        {
            GUI.DragWindow(new Rect(0, 0, 395, 28));
            GUI.Label(new Rect(7, 30, 385, 26), string.Format(LocalizationManager.instance.current["font_chars_available"], selectedCharTable.m_fontName));
            var height = (Mathf.FloorToInt(selectedCharTable.m_orderedChars.Length / 10) + 1) * 67 + (ProceduralObjectsMod.ShowDeveloperTools.value ? 52 : 6);
            scrollCharTable = GUI.BeginScrollView(new Rect(3, 57, 415, 338), scrollCharTable, new Rect(0, 0, 395, height));
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            bool useButton = selectedField != null;
            if (useButton)
            {
                if (selectedField.m_type != 0 || selectedCharTable != selectedField.m_font)
                    useButton = false;
            }
            for (int i = 0; i < selectedCharTable.m_orderedChars.Length; i++)
            {
                int line = Mathf.FloorToInt(i / 10);
                int leftOffset = i % 10;
                var charRect = new Rect(7 + leftOffset * 39, 3 + 67 * line, 35, 62);
                if (useButton)
                {
                    if (GUI.Button(charRect, string.Empty))
                    {
                        ProceduralObjectsLogic.PlaySound();
                        selectedField.m_text += selectedCharTable.m_orderedChars[i];
                    }
                }
                else
                    GUI.Box(charRect, string.Empty);
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
            if (GUIUtils.CloseButton(charTableRect))
            {
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
            colorPickerSelected = null;
            if (selectedField != null)
            {
                selectedField.expandFontsSelector = false;
                selectedField.scrollFontsPos = Vector2.zero;
                selectedField = null;
            }
            scrollParams = Vector2.zero;
            scrollTex = Vector2.zero;
            updateTimer = 0f;
            dragTimer = 0f;
            placingText = false;
            placingRect = false;
            _justPlacedTextNowFocusField = false;
            placingRectFirstpoint = Vector2.down;
            zoomFactor = 1f;
            movingField = -1;
            dragTexPos = Vector2.zero;
            // GUIUtils.SetMouseScrolling(true);
        }
        public void Edit(ProceduralObject obj, Vector2 position)
        {
            selectedField = null;
            editingObject = obj;
            scrollParams = Vector2.zero;
            scrollTex = Vector2.zero;
            movingField = -1;
            dragTexPos = Vector2.zero;
         // windowRect.position = position;
            Texture tex = ProceduralUtils.GetOriginalTexture(obj);
            originalTex = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
            originalTex.SetPixels(((Texture2D)tex).GetPixels());
            windowTex = (Texture2D)GameObject.Instantiate(originalTex);
            colorPickerSelected = null;
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
