// The following code is based on code from the Move It! mod by Quboid
// https://github.com/Quboid/CS-MoveIt/blob/master/MoveIt/OptionsKeymapping.cs

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using System.Reflection;
using UnityEngine;
using ProceduralObjects.Classes;
using ProceduralObjects.Localization;

namespace ProceduralObjects.UI
{
    public class OptionsKeymappingGeneral : OptionsKeymapping
    {
        private void Awake()
        {
            AddKeymapping(LocalizationManager.instance.current["convert_pobj"], convertToProcedural, "convertToProcedural");
            AddKeymapping(LocalizationManager.instance.current["copy_obj"], copy, "copy");
            AddKeymapping(LocalizationManager.instance.current["paste_obj"], paste, "paste");
            AddKeymapping(LocalizationManager.instance.current["quick_switch"], switchTool, "switchGeneralToVertexTools");
            AddKeymapping(LocalizationManager.instance.current["delete_desc"], delete, "deleteObject");
            AddKeymapping(LocalizationManager.instance.current["undo"], undo, "undo");
            AddKeymapping(LocalizationManager.instance.current["redo"], redo, "redo");
        }
    }
    public class OptionsKeymappingPosition : OptionsKeymapping
    {
        private void Awake()
        {
            AddKeymapping(LocalizationManager.instance.current["KB_pos_moveUp"], pos_moveUp, "position_moveUp");
            AddKeymapping(LocalizationManager.instance.current["KB_pos_moveDown"], pos_moveDown, "position_moveDown");
            AddKeymapping(LocalizationManager.instance.current["KB_pos_moveRight"], pos_moveRight, "position_moveRight");
            AddKeymapping(LocalizationManager.instance.current["KB_pos_moveLeft"], pos_moveLeft, "position_moveLeft");
            AddKeymapping(LocalizationManager.instance.current["KB_pos_moveForward"], pos_moveForward, "position_moveForward");
            AddKeymapping(LocalizationManager.instance.current["KB_pos_moveBackward"], pos_moveBackward, "position_moveBackward");
            AddKeymapping(LocalizationManager.instance.current["snap_height"], snapStoredHeight, "snapStoredHeight");
            AddKeymapping(LocalizationManager.instance.current["snapToBuildingsNetworks"], enableSnapping, "enableSnapping");
        }
    }
    public class OptionsKeymappingRotation : OptionsKeymapping
    {
        private void Awake()
        {
            AddKeymapping(LocalizationManager.instance.current["KB_rot_moveUp"], rot_moveUp, "rotation_moveUp");
            AddKeymapping(LocalizationManager.instance.current["KB_rot_moveDown"], rot_moveDown, "rotation_moveDown");
            AddKeymapping(LocalizationManager.instance.current["KB_rot_moveRight"], rot_moveRight, "rotation_moveRight");
            AddKeymapping(LocalizationManager.instance.current["KB_rot_moveLeft"], rot_moveLeft, "rotation_moveLeft");
            AddKeymapping(LocalizationManager.instance.current["KB_rot_moveForward"], rot_moveForward, "position_moveForward");
            AddKeymapping(LocalizationManager.instance.current["KB_rot_moveBackward"], rot_moveBackward, "rotation_moveForward");
        }
    }
    public class OptionsKeymappingScale : OptionsKeymapping
    {
        private void Awake()
        {
            AddKeymapping(LocalizationManager.instance.current["KB_scaleUp"], scale_scaleUp, "scale_scaleUp");
            AddKeymapping(LocalizationManager.instance.current["KB_scaleDown"], scale_scaleDown, "scale_scaleDown");
        }
    }
    public class OptionsKeymappingSelectionModeActions : OptionsKeymapping
    {
        private void Awake()
        {
            AddKeymapping(LocalizationManager.instance.current["align_heights"], align_heights, "align_heights");
            AddKeymapping(LocalizationManager.instance.current["align_rotations"], align_rotations, "align_rotations");
            AddKeymapping(LocalizationManager.instance.current["align_between2"], align_between2, "align_between2");
            AddKeymapping(LocalizationManager.instance.current["equal_slope"], equal_slope, "equal_slope");
            AddKeymapping(LocalizationManager.instance.current["snapToGround"], snapToGround, "snapToGround");
            AddKeymapping(LocalizationManager.instance.current["randomize_rot"], randomize_rot, "randomize_rot");
            AddKeymapping(LocalizationManager.instance.current["set_render_dists"], set_render_dists, "set_render_dists");
            AddKeymapping(LocalizationManager.instance.current["replace_by_copy"], replace_by_copy, "replace_by_copy");
            AddKeymapping(LocalizationManager.instance.current["color_gradient"], color_gradient, "color_gradient");
        }
    }

    public class OptionsKeymapping : UICustomControl
    {
        protected static readonly string kKeyBindingTemplate = "KeyBindingTemplate";

        protected SavedInputKey m_EditingBinding;

        protected string m_EditingBindingCategory;

        public static readonly SavedInputKey convertToProcedural = new SavedInputKey("KB_convertToProcedural", ProceduralObjectsMod.SETTINGS_FILENAME, SavedInputKey.Encode(KeyCode.P, false, true, false), true);

        public static readonly SavedInputKey copy = new SavedInputKey("KB_copy", ProceduralObjectsMod.SETTINGS_FILENAME, SavedInputKey.Encode(KeyCode.C, true, false, false), true);
        public static readonly SavedInputKey paste = new SavedInputKey("KB_paste", ProceduralObjectsMod.SETTINGS_FILENAME, SavedInputKey.Encode(KeyCode.V, true, false, false), true);
        public static readonly SavedInputKey switchTool = new SavedInputKey("KB_switchGeneralToVertexTools", ProceduralObjectsMod.SETTINGS_FILENAME, SavedInputKey.Encode(KeyCode.Tab, false, false, false), true);
        public static readonly SavedInputKey delete = new SavedInputKey("KB_deleteObject", ProceduralObjectsMod.SETTINGS_FILENAME, SavedInputKey.Encode(KeyCode.Delete, false, false, false), true);

        public static readonly SavedInputKey undo = new SavedInputKey("KB_undo", ProceduralObjectsMod.SETTINGS_FILENAME, SavedInputKey.Encode(KeyCode.Z, true, false, false), true);
        public static readonly SavedInputKey redo = new SavedInputKey("KB_redo", ProceduralObjectsMod.SETTINGS_FILENAME, SavedInputKey.Encode(KeyCode.Y, true, false, false), true);

        public static readonly SavedInputKey pos_moveUp = new SavedInputKey("KB_position_moveUp", ProceduralObjectsMod.SETTINGS_FILENAME, SavedInputKey.Encode(KeyCode.PageUp, false, false, false), true);
        public static readonly SavedInputKey pos_moveDown = new SavedInputKey("KB_position_moveDown", ProceduralObjectsMod.SETTINGS_FILENAME, SavedInputKey.Encode(KeyCode.PageDown, false, false, false), true);
        public static readonly SavedInputKey pos_moveRight = new SavedInputKey("KB_position_moveRight", ProceduralObjectsMod.SETTINGS_FILENAME, SavedInputKey.Encode(KeyCode.RightArrow, false, false, false), true);
        public static readonly SavedInputKey pos_moveLeft = new SavedInputKey("KB_position_moveLeft", ProceduralObjectsMod.SETTINGS_FILENAME, SavedInputKey.Encode(KeyCode.LeftArrow, false, false, false), true);
        public static readonly SavedInputKey pos_moveForward = new SavedInputKey("KB_position_moveForward", ProceduralObjectsMod.SETTINGS_FILENAME, SavedInputKey.Encode(KeyCode.UpArrow, false, false, false), true);
        public static readonly SavedInputKey pos_moveBackward = new SavedInputKey("KB_position_moveBackward", ProceduralObjectsMod.SETTINGS_FILENAME, SavedInputKey.Encode(KeyCode.DownArrow, false, false, false), true);

        public static readonly SavedInputKey snapStoredHeight = new SavedInputKey("KB_snapStoredHeight", ProceduralObjectsMod.SETTINGS_FILENAME, SavedInputKey.Encode(KeyCode.H, false, false, false), true);
        public static readonly SavedInputKey enableSnapping = new SavedInputKey("KB_enableSnapping", ProceduralObjectsMod.SETTINGS_FILENAME, SavedInputKey.Encode(KeyCode.S, false, false, false), true);

        public static readonly SavedInputKey rot_moveUp = new SavedInputKey("KB_rotation_moveUp", ProceduralObjectsMod.SETTINGS_FILENAME, SavedInputKey.Encode(KeyCode.PageUp, false, false, false), true);
        public static readonly SavedInputKey rot_moveDown = new SavedInputKey("KB_rotation_moveDown", ProceduralObjectsMod.SETTINGS_FILENAME, SavedInputKey.Encode(KeyCode.PageDown, false, false, false), true);
        public static readonly SavedInputKey rot_moveRight = new SavedInputKey("KB_rotation_moveRight", ProceduralObjectsMod.SETTINGS_FILENAME, SavedInputKey.Encode(KeyCode.RightArrow, false, false, false), true);
        public static readonly SavedInputKey rot_moveLeft = new SavedInputKey("KB_rotation_moveLeft", ProceduralObjectsMod.SETTINGS_FILENAME, SavedInputKey.Encode(KeyCode.LeftArrow, false, false, false), true);
        public static readonly SavedInputKey rot_moveForward = new SavedInputKey("KB_rotation_moveForward", ProceduralObjectsMod.SETTINGS_FILENAME, SavedInputKey.Encode(KeyCode.UpArrow, false, false, false), true);
        public static readonly SavedInputKey rot_moveBackward = new SavedInputKey("KB_rotation_moveBackward", ProceduralObjectsMod.SETTINGS_FILENAME, SavedInputKey.Encode(KeyCode.DownArrow, false, false, false), true);

        public static readonly SavedInputKey scale_scaleUp = new SavedInputKey("KB_scale_scaleUp", ProceduralObjectsMod.SETTINGS_FILENAME, SavedInputKey.Encode(KeyCode.PageUp, false, false, false), true);
        public static readonly SavedInputKey scale_scaleDown = new SavedInputKey("KB_scale_scaleDown", ProceduralObjectsMod.SETTINGS_FILENAME, SavedInputKey.Encode(KeyCode.PageDown, false, false, false), true);

        public static readonly SavedInputKey align_heights = new SavedInputKey("KB_align_heights", ProceduralObjectsMod.SETTINGS_FILENAME, SavedInputKey.Encode(KeyCode.H, true, false, false), true);
        public static readonly SavedInputKey align_rotations = new SavedInputKey("KB_align_rotations", ProceduralObjectsMod.SETTINGS_FILENAME, SavedInputKey.Encode(KeyCode.R, true, false, false), true);
        public static readonly SavedInputKey randomize_rot = new SavedInputKey("KB_randomize_rot", ProceduralObjectsMod.SETTINGS_FILENAME, SavedInputKey.Empty, true);
        public static readonly SavedInputKey align_between2 = new SavedInputKey("KB_align_between2", ProceduralObjectsMod.SETTINGS_FILENAME, SavedInputKey.Empty, true);
        public static readonly SavedInputKey equal_slope = new SavedInputKey("KB_equal_slope", ProceduralObjectsMod.SETTINGS_FILENAME, SavedInputKey.Empty, true);
        public static readonly SavedInputKey snapToGround = new SavedInputKey("KB_snapToGround", ProceduralObjectsMod.SETTINGS_FILENAME, SavedInputKey.Empty, true);
        public static readonly SavedInputKey set_render_dists = new SavedInputKey("KB_set_render_dists", ProceduralObjectsMod.SETTINGS_FILENAME, SavedInputKey.Empty, true);
        public static readonly SavedInputKey replace_by_copy = new SavedInputKey("KB_replace_by_copy", ProceduralObjectsMod.SETTINGS_FILENAME, SavedInputKey.Empty, true);
        public static readonly SavedInputKey color_gradient = new SavedInputKey("KB_color_gradient", ProceduralObjectsMod.SETTINGS_FILENAME, SavedInputKey.Empty, true);
        
        protected int count = 0;

        protected void AddKeymapping(string label, SavedInputKey savedInputKey, string legacySaveFileName)
        {
            savedInputKey.value = savedInputKey.value;
            UIPanel uIPanel = component.AttachUIComponent(UITemplateManager.GetAsGameObject(kKeyBindingTemplate)) as UIPanel;
            uIPanel.name = legacySaveFileName;
            if (count++ % 2 == 1) uIPanel.backgroundSprite = null;

            UILabel uILabel = uIPanel.Find<UILabel>("Name");
            UIButton uIButton = uIPanel.Find<UIButton>("Binding");
            uIButton.eventKeyDown += new KeyPressHandler(this.OnBindingKeyDown);
            uIButton.eventMouseDown += new MouseEventHandler(this.OnBindingMouseDown);

            if (File.Exists(KeyBindingsManager.BindingsConfigPath))
            {
                var lines = new List<string>(File.ReadAllLines(KeyBindingsManager.BindingsConfigPath));
                string line = "########";
                foreach (string l in lines)
                {
                    if (l.Contains(legacySaveFileName))
                    {
                        line = l; break;
                    }
                }
                if (line != "########")
                {
                    lines.Remove(line);
                    KeyBindingsManager.RewriteCfgLines(lines);
                }
            }

            uILabel.text = label;
            uIButton.text = savedInputKey.ToLocalizedString("KEYNAME");
            uIButton.objectUserData = savedInputKey;
        }

        protected void OnEnable()
        {
            LocaleManager.eventLocaleChanged += new LocaleManager.LocaleChangedHandler(this.OnLocaleChanged);
        }

        protected void OnDisable()
        {
            LocaleManager.eventLocaleChanged -= new LocaleManager.LocaleChangedHandler(this.OnLocaleChanged);
        }

        protected void OnLocaleChanged()
        {
            this.RefreshBindableInputs();
        }

        protected bool IsModifierKey(KeyCode code)
        {
            return code == KeyCode.LeftControl || code == KeyCode.RightControl || code == KeyCode.LeftShift || code == KeyCode.RightShift || code == KeyCode.LeftAlt || code == KeyCode.RightAlt;
        }

        protected bool IsControlDown()
        {
            return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        }

        protected bool IsShiftDown()
        {
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        }

        protected bool IsAltDown()
        {
            return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        }

        protected bool IsUnbindableMouseButton(UIMouseButton code)
        {
            return code == UIMouseButton.Left || code == UIMouseButton.Right;
        }

        protected KeyCode ButtonToKeycode(UIMouseButton button)
        {
            if (button == UIMouseButton.Left)
            {
                return KeyCode.Mouse0;
            }
            if (button == UIMouseButton.Right)
            {
                return KeyCode.Mouse1;
            }
            if (button == UIMouseButton.Middle)
            {
                return KeyCode.Mouse2;
            }
            if (button == UIMouseButton.Special0)
            {
                return KeyCode.Mouse3;
            }
            if (button == UIMouseButton.Special1)
            {
                return KeyCode.Mouse4;
            }
            if (button == UIMouseButton.Special2)
            {
                return KeyCode.Mouse5;
            }
            if (button == UIMouseButton.Special3)
            {
                return KeyCode.Mouse6;
            }
            return KeyCode.None;
        }

        protected void OnBindingKeyDown(UIComponent comp, UIKeyEventParameter p)
        {
            if (this.m_EditingBinding != null && !this.IsModifierKey(p.keycode))
            {
                p.Use();
                UIView.PopModal();
                KeyCode keycode = p.keycode;
                InputKey inputKey = (p.keycode == KeyCode.Escape) ? this.m_EditingBinding.value : SavedInputKey.Encode(keycode, p.control, p.shift, p.alt);
                if (p.keycode == KeyCode.Backspace)
                {
                    KeyBindingsManager.instance.GetBindingFromName(comp.parent.name).SetEmpty();
                    inputKey = SavedInputKey.Empty;
                }
                else
                    KeyBindingsManager.instance.GetBindingFromName(comp.parent.name).ApplySavedInput(keycode, p.control, p.shift, p.alt);
                this.m_EditingBinding.value = inputKey;
                UITextComponent uITextComponent = p.source as UITextComponent;
                uITextComponent.text = this.m_EditingBinding.ToLocalizedString("KEYNAME");
                this.m_EditingBinding = null;
                this.m_EditingBindingCategory = string.Empty;
            }
        }

        protected void OnBindingMouseDown(UIComponent comp, UIMouseEventParameter p)
        {
            if (this.m_EditingBinding == null)
            {
                p.Use();
                this.m_EditingBinding = (SavedInputKey)p.source.objectUserData;
                this.m_EditingBindingCategory = p.source.stringUserData;
                UIButton uIButton = p.source as UIButton;
                uIButton.buttonsMask = (UIMouseButton.Left | UIMouseButton.Right | UIMouseButton.Middle | UIMouseButton.Special0 | UIMouseButton.Special1 | UIMouseButton.Special2 | UIMouseButton.Special3);
                uIButton.text = "Press any key";
                p.source.Focus();
                UIView.PushModal(p.source);
            }
            else if (!this.IsUnbindableMouseButton(p.buttons))
            {
                p.Use();
                UIView.PopModal();
                InputKey inputKey = SavedInputKey.Encode(this.ButtonToKeycode(p.buttons), this.IsControlDown(), this.IsShiftDown(), this.IsAltDown());

                this.m_EditingBinding.value = inputKey;
                UIButton uIButton2 = p.source as UIButton;
                uIButton2.text = this.m_EditingBinding.ToLocalizedString("KEYNAME");
                uIButton2.buttonsMask = UIMouseButton.Left;
                this.m_EditingBinding = null;
                this.m_EditingBindingCategory = string.Empty;
            }
        }

        protected void RefreshBindableInputs()
        {
            foreach (UIComponent current in component.GetComponentsInChildren<UIComponent>())
            {
                UITextComponent uITextComponent = current.Find<UITextComponent>("Binding");
                if (uITextComponent != null)
                {
                    SavedInputKey savedInputKey = uITextComponent.objectUserData as SavedInputKey;
                    if (savedInputKey != null)
                    {
                        uITextComponent.text = savedInputKey.ToLocalizedString("KEYNAME");
                    }
                }
                UILabel uILabel = current.Find<UILabel>("Name");
                if (uILabel != null)
                {
                    uILabel.text = Locale.Get("KEYMAPPING", uILabel.stringUserData);
                }
            }
        }

        protected InputKey GetDefaultEntry(string entryName)
        {
            FieldInfo field = typeof(DefaultSettings).GetField(entryName, BindingFlags.Static | BindingFlags.Public);
            if (field == null)
            {
                return 0;
            }
            object value = field.GetValue(null);
            if (value is InputKey)
            {
                return (InputKey)value;
            }
            return 0;
        }

        protected void RefreshKeyMapping()
        {
            foreach (UIComponent current in component.GetComponentsInChildren<UIComponent>())
            {
                UITextComponent uITextComponent = current.Find<UITextComponent>("Binding");
                SavedInputKey savedInputKey = (SavedInputKey)uITextComponent.objectUserData;
                if (this.m_EditingBinding != savedInputKey)
                {
                    uITextComponent.text = savedInputKey.ToLocalizedString("KEYNAME");
                }
            }
        }
    }
}
