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
            foreach (PublishedFileId fileId in PlatformService.workshop.GetSubscribedItems())
            {
                string path = PlatformService.workshop.GetSubscribedItemPath(fileId);
                if (!Directory.Exists(path))
                    continue;
                var localeFiles = Directory.GetFiles(path, "*.polocale", SearchOption.AllDirectories);
                if (localeFiles.Any())
                {
                    for (int i = 0; i < localeFiles.Count(); i++)
                    {
                        if (!File.Exists(localeFiles[i]))
                            continue;
                        var locale = new Localization();
                        locale.LoadFromFile(localeFiles[i]);
                        available.Add(locale);
                        if (locale.identifier == "en")
                            english = locale;
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
