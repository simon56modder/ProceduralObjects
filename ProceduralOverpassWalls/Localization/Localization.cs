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

        public string identifier, name;
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
                else if (lines[i].Contains("locale_name = "))
                    name = lines[i].Replace("locale_name = ", "");
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
                {
                    if (!LocalizationManager.instance.english.keys.ContainsKey(key))
                        return identifier.ToUpper() + ":NOTFOUND[" + key + "]";
                    else
                        return LocalizationManager.instance.english.keys[key].Replace("\\n", "\n");
                }
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

        public string normalsRecalcString(NormalsRecalculation recalc)
        {
            string s = this["normalsRecalc"] + " : ";
            if (recalc == NormalsRecalculation.None)
                s += this["normalsRecalc_none"];
            else if (recalc == NormalsRecalculation.Default)
                s += this["normalsRecalc_def"];
            else if (recalc == NormalsRecalculation.Tolerance0)
                s += string.Format(this["normalsRecalc_degTolerance"], "0");
            else if (recalc == NormalsRecalculation.Tolerance30)
                s += string.Format(this["normalsRecalc_degTolerance"], "30");
            else if (recalc == NormalsRecalculation.Tolerance60)
                s += string.Format(this["normalsRecalc_degTolerance"], "60");
            return s;
        }
    }
}
