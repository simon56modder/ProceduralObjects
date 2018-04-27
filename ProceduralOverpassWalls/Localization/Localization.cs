using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ProceduralObjects.Classes;

namespace ProceduralObjects.Localization
{
    public class Localization
    {
        public Localization()
        {
            keys = new Dictionary<string, string>();
        }

        public string identifier;
        public Dictionary<string, string> keys;

        public void LoadFromFile(string path)
        {
            if (!File.Exists(path))
                return;
            keys = new Dictionary<string, string>();
            string[] lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Count(); i++)
            {
                if (lines[i].Contains("identifier = "))
                    identifier = lines[i].Replace("identifier = ", "");
                else if (lines[i].Contains(" = "))
                {
                    var kvp = lines[i].Split(new string[] { " = " }, StringSplitOptions.RemoveEmptyEntries);
                    if (kvp.Count() == 2)
                        keys[kvp[0]] = kvp[1];
                }
            }
        }

        public string this[string key]
        {
            get
            {
                if (key == "")
                    return "";
                if (!keys.ContainsKey(key))
                    return identifier.ToUpper() + ":NOTFOUND[" + key + "]";
                return keys[key].Replace("\\n", "\n");
            }
            set
            {
                keys[key] = value;
            }
        }

        public string visibilityString(ProceduralObjectVisibility visib)
        {
            switch (visib)
            {
                case ProceduralObjectVisibility.DayOnly:
                    return this["visibility_dayOnly"];
                case ProceduralObjectVisibility.NightOnly:
                    return this["visibility_nightOnly"];
            }
            return this["visibility_always"];
        }
    }
}
