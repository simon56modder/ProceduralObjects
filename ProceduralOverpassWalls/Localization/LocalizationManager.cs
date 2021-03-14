using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColossalFramework.IO;
using System.IO;
using ColossalFramework.PlatformServices;
using ColossalFramework.Globalization;
using UnityEngine;

namespace ProceduralObjects.Localization
{
    public class LocalizationManager
    {
        public LocalizationManager() { }

        public static LocalizationManager instance;
        public static void CreateManager()
        {
            instance = new LocalizationManager();
            instance.available = new List<Localization>();
            instance.LoadLocalizations();
            instance.SelectCurrent();
        }

        public Localization current;
        public List<Localization> available;
        public Localization english;

        public void LoadLocalizations()
        {
            available = new List<Localization>();
            List<string> extensionFiles = new List<string>(), 
                baseFiles = new List<string>();

            foreach (PublishedFileId fileId in PlatformService.workshop.GetSubscribedItems())
            {
                string path = PlatformService.workshop.GetSubscribedItemPath(fileId);
                if (!Directory.Exists(path))
                    continue;

                var localeFiles = Directory.GetFiles(path, "*.polocale", SearchOption.AllDirectories);
                if (!localeFiles.Any())
                    continue;

                for (int i = 0; i < localeFiles.Count(); i++)
                {
                    if (!File.Exists(localeFiles[i]))
                        continue;

                    if (IsExtension(localeFiles[i]))
                        extensionFiles.Add(localeFiles[i]);
                    else
                        baseFiles.Add(localeFiles[i]);
                }
            }
            foreach (var path in baseFiles)
            {
                var locale = new Localization();
                locale.LoadFromFile(path);
                available.Add(locale);
                if (locale.identifier == "en")
                    english = locale;
            }
            foreach (var path in extensionFiles)
                LoadAsExtension(path);
        }
        public bool IsExtension(string path)
        {
            if (!File.Exists(path))
                return false;
            string[] lines = File.ReadAllLines(path);
            for (int i = 0; i < 6; i++)
            {
                if (lines[i].ToLower().Contains("type = extension"))
                    return true;
            }
            return false;
        }
        public void LoadAsExtension(string path)
        {
            if (!File.Exists(path))
                return;

            string[] lines = File.ReadAllLines(path);
            Localization loc = null;
            for (int i = 0; i < lines.Count(); i++)
            {
                if (lines[i].ToLower().Contains("type = extension"))
                    continue;
                else if (lines[i].Contains("identifier = "))
                {
                    var id = lines[i].Replace("identifier = ", "");
                    if (available.Any(l => l.identifier == id))
                        loc = available.First(l => l.identifier == id);
                    else
                        loc = null;
                }
                else if (lines[i].Contains(" = "))
                {
                    if (loc != null)
                    {
                        var kvp = lines[i].Split(new string[] { " = " }, StringSplitOptions.RemoveEmptyEntries);
                        if (kvp.Count() == 2)
                        {
                            if (!loc.keys.ContainsKey(kvp[0]))
                                loc.keys[kvp[0]] = kvp[1].Replace("\\n", "\n");
                        }
                    }
                }
            }
        }
        public void SelectCurrent()
        {
            var loadIdentifier = LocaleManager.instance.language.ToLower();
            if (ProceduralObjectsMod.LanguageUsed.value != "default")
                loadIdentifier = ProceduralObjectsMod.LanguageUsed.value;
            if (available.Any(locale => locale.identifier.ToLower() == loadIdentifier))
                current = available.First(locale => locale.identifier.ToLower() == loadIdentifier);
            else if (available.Count > 0)
                current = available.First(locale => locale.identifier == "en");
            else
                throw new Exception("ProceduralObjects localization exception : No localization was found to load !");
        }
        public void SetCurrent(int i)
        {
            current = this.available.ElementAt(i);
        }

        public string[] identifiers
        {
            get
            {
                var list = new List<string>();
                foreach (Localization l in available)
                    list.Add(l.identifier.ToUpper() + " (" + l.name + ")");
                return list.ToArray();
            }
        }
    }
}
