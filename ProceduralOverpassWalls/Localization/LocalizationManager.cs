using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColossalFramework.IO;
using System.IO;
using ColossalFramework.PlatformServices;
using ColossalFramework.Globalization;

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
                    }
                }
            }
        }

        public void SelectCurrent()
        {
            if (available.Any(locale => locale.identifier.ToLower() == LocaleManager.instance.language.ToLower()))
                current = available.First(locale => locale.identifier.ToLower() == LocaleManager.instance.language.ToLower());
            else if (available.Count > 0)
                current = available.First(locale => locale.identifier == "en");
            else
                throw new Exception("ProceduralObjects localization exception : No localization was found to load !");
        }
    }
}
