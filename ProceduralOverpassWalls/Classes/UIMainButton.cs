using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.UI;

namespace ProceduralObjects.Classes
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
            playAudioEvents = true;
            if (savedX.value == -1000)
            {
                UIComponent radioButton = GetUIView().FindUIComponent<UIComponent>("RadioButton");
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
}
