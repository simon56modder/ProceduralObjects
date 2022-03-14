using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using ColossalFramework;
using ColossalFramework.IO;
using System.IO;

namespace ProceduralObjects.Classes
{
    public class KeyBindingInfo
    {
        public KeyBindingInfo() { }
        public KeyBindingInfo(string inputConfigLine)
        {
            if (inputConfigLine.Contains(" = "))
            {
                this.m_name = inputConfigLine.Split(new string[] { " = " }, StringSplitOptions.RemoveEmptyEntries)[0];

                var _fullBinding = inputConfigLine.Split(new string[] { " = " }, StringSplitOptions.RemoveEmptyEntries)[1].Replace(" ", "");

                this.m_fullKeys = _fullBinding;
                AdjustFullKeys();

                List<KeyCode> keyCodes = new List<KeyCode>();
                int plusCharCount = _fullBinding.Count(c => c == '+');
                if (plusCharCount == 0)
                {
                    KeyCode c;
                    try
                    {
                        c = (KeyCode)Enum.Parse(typeof(KeyCode), _fullBinding, true);
                        keyCodes.Add(c);
                    }
                    catch
                    {
                        Debug.LogError("[ProceduralObjects] KeyBindingInfo constructor failure : Syntax error - a binding couldn't be parsed, it was invalid");
                    }
                }
                else
                {
                    string[] splitedBinding = _fullBinding.Split(new string[] { "+" }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i <= plusCharCount; i++)
                    {
                        KeyCode c;
                        try
                        {
                            c = (KeyCode)Enum.Parse(typeof(KeyCode), splitedBinding[i], true);
                            keyCodes.Add(c);
                        }
                        catch
                        {
                            Debug.LogError("[ProceduralObjects] KeyBindingInfo constructor failure : Syntax error - a binding couldn't be parsed, it was invalid");
                        }
                    }
                }
                m_orderedKeys = keyCodes.ToArray();
            }
            else
                Debug.LogError("[ProceduralObjects] KeyBindingInfo constructor failure : Syntax error - no 'Equals' character found for a key binding line");
        }
        public KeyBindingInfo(string name, SavedInputKey key)
        {
            this.m_name = name;
            ApplySavedInput(key);
        }

        public string m_name, m_fullKeys;
        public KeyCode[] m_orderedKeys;

        public void ApplySavedInput(SavedInputKey key)
        {
            if (key == null)
            {
                return;
            }
            if (key.Key == KeyCode.None || key.value == SavedInputKey.Empty)
            {
                SetEmpty();
            }
            else
            {
                List<KeyCode> keys = new List<KeyCode>();
                if (key.Control) keys.Add(KeyCode.LeftControl);
                if (key.Shift) keys.Add(KeyCode.LeftShift);
                if (key.Alt) keys.Add(KeyCode.LeftAlt);
                keys.Add(key.Key);
                m_orderedKeys = keys.ToArray();
                m_fullKeys = (key.Control ? "LeftControl+" : "") + (key.Shift ? "LeftShift+" : "") + (key.Alt ? "LeftAlt+" : "") + key.Key.ToString();
                AdjustFullKeys();
            }
        }
        public void ApplySavedInput(KeyCode keycode, bool ctrl, bool shift, bool alt)
        {
            if (keycode == KeyCode.None)
            {
                m_orderedKeys = new KeyCode[] { };
                m_fullKeys = "(none)";
            }
            else
            {
                List<KeyCode> keys = new List<KeyCode>();
                if (ctrl) keys.Add(KeyCode.LeftControl);
                if (shift) keys.Add(KeyCode.LeftShift);
                if (alt) keys.Add(KeyCode.LeftAlt);
                keys.Add(keycode);
                m_orderedKeys = keys.ToArray();
                m_fullKeys = (ctrl ? "LeftControl" : "") + (shift ? "LeftShift" : "") + (alt ? "LeftAlt" : "") + keycode.ToString();
                AdjustFullKeys();
            }
        }
        public void SetEmpty()
        {
            m_orderedKeys = new KeyCode[] { };
            m_fullKeys = "(none)";
        }

        public bool GetBindingDown()
        {
            int count = m_orderedKeys.Count();
            if (count == 0)
                return false;
            else if (count == 1)
                return Input.GetKeyDown(m_orderedKeys[0]);
            else
            {
                if (!Input.GetKeyDown(m_orderedKeys[count - 1]))
                    return false;

                for (int i = 0; i < count; i++)
                {
                    if (i != count - 1)
                    {
                        if (!Input.GetKey(m_orderedKeys[i]))
                            return false;
                    }
                    else
                        return Input.GetKeyDown(m_orderedKeys[i]);
                }
            }
            return false;
        }

        public bool GetBinding()
        {
            int count = m_orderedKeys.Count();
            if (count == 0)
                return false;
            else if (count == 1)
                return Input.GetKey(m_orderedKeys[0]);
            else
            {
                if (!Input.GetKey(m_orderedKeys[count - 1]))
                    return false;

                for (int i = 0; i < count; i++)
                {
                    if (i != count - 1)
                    {
                        if (!Input.GetKey(m_orderedKeys[i]))
                            return false;
                    }
                    else
                        return Input.GetKey(m_orderedKeys[i]);
                }
            }
            return false;
        }

        public bool GetBindingUp()
        {
            int count = m_orderedKeys.Count();
            if (count == 0)
                return false;
            else if (count == 1)
                return Input.GetKeyUp(m_orderedKeys[0]);
            else
            {
                for (int i = 0; i < count; i++)
                {
                    if (i != count - 1)
                    {
                        if (!Input.GetKey(m_orderedKeys[i]))
                            return false;
                    }
                    else
                        return Input.GetKeyUp(m_orderedKeys[i]);
                }
            }
            return false;
        }

        private void AdjustFullKeys()
        {
            this.m_fullKeys = this.m_fullKeys.Replace("LeftControl", "Ctrl").Replace("LeftShift", "Shift").Replace("LeftAlt", "Alt")
                .Replace("LeftArrow", "⇦")
                .Replace("RightArrow", "⇨")
                .Replace("UpArrow", "⇧")
                .Replace("DownArrow", "⇩")
                .Replace("PageUp", "PgUp")
                .Replace("PageDown", "PgDown");
        }
    }

    public class KeyBindingsManager
    {
        public static KeyBindingsManager instance;
        public static string BindingsConfigPath
        {
            get
            {
                if (ProceduralObjectsMod.IsLinux)
                    return DataLocation.localApplicationData + "/ProceduralObjects_KeyBindings.cfg";

                return DataLocation.localApplicationData + "\\ProceduralObjects_KeyBindings.cfg";
            }
        }

        public static void Initialize()
        {
            if (instance != null) return;

            instance = new KeyBindingsManager();
            try { instance.LoadData(); }
            catch { Debug.LogError("[ProceduralObjects] Fatal Loading exception : couldn't load key bindings !"); }
        }

        public KeyBindingsManager()
        {
            m_keyBindings = new List<KeyBindingInfo>();
        }

        public List<KeyBindingInfo> m_keyBindings;
        private Dictionary<string, KeyBindingInfo> keyBindingsDictionary;

        public KeyBindingInfo GetBindingFromName(string name)
        {
            if (keyBindingsDictionary == null)
            {
                this.LoadData();
                GetBindingFromName(name);
            }
            if (!keyBindingsDictionary.ContainsKey(name))
            {
                throw new Exception("[ProceduralObjects] KeyBinding missing : A key binding with id '" + name + "' was missing in the config file");
            }
            return keyBindingsDictionary[name];
        }
        public void LoadData()
        {
            if (!File.Exists(BindingsConfigPath))
            {
                TextWriter tw = new StreamWriter(BindingsConfigPath);
                tw.WriteLine("---- this is the key bindings configuration file for Procedural Objects ----");
                tw.WriteLine("You can put any key binding you want for each of those actions. Just be");
                tw.WriteLine("sure they are the right names. For example don't right 'Ctrl' but \"LeftControl\"");
                tw.WriteLine("Use the 'plus' symbol to create any key combination, works for any keys or any number of keys.");
                tw.WriteLine("It works for any keys or combination of keys, the list of the names you should use can be found here:");
                tw.WriteLine("https://docs.unity3d.com/ScriptReference/KeyCode.html");
                tw.WriteLine("");
                tw.WriteLine("generalShowHideUI = LeftControl+P+O");
                tw.WriteLine("switchActionMode = LeftControl");
                tw.WriteLine("");
                tw.WriteLine("edition_smoothMovements = LeftShift");
                tw.WriteLine("edition_smallMovements = LeftAlt");
                tw.Close();
            }
            m_keyBindings = new List<KeyBindingInfo>();
            var lines = File.ReadAllLines(BindingsConfigPath).ToList();
            var keys = ProceduralObjectsMod.SettingsFile.ListKeys().Where(s => s.StartsWith("KB_"));
            foreach (string line in new List<string>(lines))
            {
                if (line != string.Empty && line.Contains("="))
                {
                    var key = line.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
                    if (keys.Any(k => k.Contains("KB_" + key)))
                    {
                        lines.Remove(line);
                    }
                    else
                    {
                        KeyBindingInfo kbInfo = new KeyBindingInfo(line);
                        m_keyBindings.Add(kbInfo);
                    }
                }
            }

            RewriteCfgLines(lines);
            
            if (ProceduralObjectsMod.SettingsFile != null)
            {
                foreach (var key in keys)
                {
                    var k = key.Replace("KB_", "");
                    if (m_keyBindings.Any(kb => kb.m_name == k))
                        continue;
                    KeyBindingInfo kbInfo = new KeyBindingInfo(k, new SavedInputKey(key, ProceduralObjectsMod.SETTINGS_FILENAME));
                    m_keyBindings.Add(kbInfo);
                }
            }
            keyBindingsDictionary = new Dictionary<string, KeyBindingInfo>();
            foreach (var kbInfo in m_keyBindings)
            {
                keyBindingsDictionary.Add(kbInfo.m_name, kbInfo);
            }
            Debug.Log("[ProceduralObjects] Key Bindings loading ended from " + BindingsConfigPath);
        }
        public static void RewriteCfgLines(List<string> lines)
        {
            if (File.Exists(BindingsConfigPath))
                File.Delete(BindingsConfigPath);
            TextWriter tw = new StreamWriter(BindingsConfigPath);
            for (int i = 0; i < lines.Count; i++)
            {
                tw.WriteLine(lines[i]);
            }
            tw.Close();
        }
    }
}
