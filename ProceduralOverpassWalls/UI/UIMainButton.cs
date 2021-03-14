using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;

namespace ProceduralObjects.UI
{
    public class ProceduralObjectsButton : UIButton
    {
        public static readonly SavedInt savedX = new SavedInt("savedX", ProceduralObjectsMod.OTHER_SETTINGS_FILENAME, -1000, true);
        public static readonly SavedInt savedY = new SavedInt("savedY", ProceduralObjectsMod.OTHER_SETTINGS_FILENAME, -1000, true);

        public ProceduralObjectsLogic logic;

        public override void Start()
        {
            /* foreach (var comp in GetUIView().GetComponents<UIComponent>())
            {
                Debug.Log("Found component of type " + comp.GetType().ToString() + " named " + comp.name + " (cache name : " + comp.cachedName + ")");
            } */
            text = "Procedural Objects";
            width = 180;
            height = 30;
            normalBgSprite = "ButtonMenu";
            disabledBgSprite = "ButtonMenuDisabled";
            hoveredBgSprite = "ButtonMenuHovered";
            focusedBgSprite = "ButtonMenuFocused";
            pressedBgSprite = "ButtonMenuPressed";
            textColor = new Color32(255, 255, 255, 255);
            disabledTextColor = new Color32(7, 7, 7, 255);
            hoveredTextColor = new Color32(7, 132, 255, 255);
            focusedTextColor = new Color32(255, 255, 255, 255);
            pressedTextColor = new Color32(30, 30, 44, 255);
            eventClick += mainButton_eventClick;
            this.gameObject.AddComponent<CustomKeyHandler>();
            playAudioEvents = true;
            if (savedX.value == -1000)
            {
                UIComponent radioButton = GetUIView().FindUIComponent<UIComponent>("RadioButton");
                if (radioButton == null)
                {
                    absolutePosition = Vector2.zero;
                    Debug.LogWarning("[ProceduralObjects] UI Main Button issue : Radio Button reference was not found, setting to zero, zero.");
                }
                else
                    absolutePosition = new Vector2(radioButton.absolutePosition.x - width + 2 * radioButton.width, radioButton.parent.absolutePosition.y + radioButton.height);
            }
            else
            {
                absolutePosition = new Vector2(savedX.value, savedY.value);
            }
        }

        private void mainButton_eventClick(UIComponent component, UIMouseEventParameter eventParam)
        {
            if (logic == null || !eventParam.buttons.IsFlagSet(UIMouseButton.Left))
                return;
            logic.MainButtonClick();
        }

        private Vector3 m_deltaPos;
        protected override void OnMouseDown(UIMouseEventParameter p)
        {
            if (p.buttons.IsFlagSet(UIMouseButton.Right))
            {
                Vector3 mousePos = Input.mousePosition;
                mousePos.y = m_OwnerView.fixedHeight - mousePos.y;

                m_deltaPos = absolutePosition - mousePos;
                BringToFront();
            }
        }

        protected override void OnMouseMove(UIMouseEventParameter p)
        {
            if (p.buttons.IsFlagSet(UIMouseButton.Right))
            {
                Vector3 mousePos = Input.mousePosition;
                mousePos.y = m_OwnerView.fixedHeight - mousePos.y;

                absolutePosition = mousePos + m_deltaPos;
                savedX.value = (int)absolutePosition.x;
                savedY.value = (int)absolutePosition.y;
            }
        }
    }
    // The following code is derived from Traffic Manager:President Edition
    // Special thanks to krzychu124 for the help
    // https://github.com/VictorPhilipp/Cities-Skylines-Traffic-Manager-President-Edition/blob/9d281aebc56e60ede65bc138e1ae02a72df716cd/TLM/TLM/UI/CustomKeyHandler.cs
    public class CustomKeyHandler : UICustomControl
    {
        public void OnKeyDown(UIComponent comp, UIKeyEventParameter p)
        {
            if (p.used || p.keycode != KeyCode.Escape)
                return;
            if (ProceduralObjectsLogic.instance != null)
                ProceduralObjectsLogic.instance.EscapePressed();
            p.Use();
        }
        
    }
}
