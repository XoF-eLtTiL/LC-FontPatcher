using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using HarmonyLib;

namespace FontPatcher;

[HarmonyPatch]
class FontLoader
{
    class FontBundle
    {
        public TMP_FontAsset Normal;
        public TMP_FontAsset Transmit;
    }

    static List<FontBundle> fontBundles = new();

    public static void Load(string location)
    {
        try
        {
            string dirName = Path.GetDirectoryName(location);
            string fontsPath = Path.Combine(dirName, ResourcePath.FontPath);
            DirectoryInfo di = new DirectoryInfo(fontsPath);
            FileInfo[] fileInfos = di.GetFiles("*");

            foreach (FileInfo info in fileInfos)
            {
                AssetBundle bundle = AssetBundle.LoadFromFile(info.FullName);

                FontBundle tmp = new()
                {
                    Normal = bundle.LoadAsset<TMP_FontAsset>(ResourcePath.NormalFont),
                    Transmit = bundle.LoadAsset<TMP_FontAsset>(ResourcePath.TransmitFont)
                };

                if (tmp.Normal) tmp.Normal.name = $"{info.Name}(Normal)";
                if (tmp.Transmit) tmp.Transmit.name = $"{info.Name}(Transmit)";

                fontBundles.Add(tmp);
            }

            Plugin.Instance.LogInfo($"Font loaded!");
        }
        catch (Exception e)
        {
            Plugin.Instance.LogError(e.Message);
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(TMP_FontAsset), "Awake")]
    static void PrefixAwake(TMP_FontAsset __instance)
    {
        // 添加Normal字体回退
        if (Plugin.Instance.configNormalIngameFont.Value)
        {
            foreach (FontBundle bundle in fontBundles)
            {
                if (bundle.Normal != null)
                {
                    __instance.fallbackFontAssetTable.Add(bundle.Normal);
                }
            }
        }

        // 添加Transmit字体回退
        if (Plugin.Instance.configTransmitIngameFont.Value)
        {
            foreach (FontBundle bundle in fontBundles)
            {
                if (bundle.Transmit != null)
                {
                    __instance.fallbackFontAssetTable.Add(bundle.Transmit);
                }
            }
        }
    }

    static void DisableFont(TMP_FontAsset font)
    {
        font.characterLookupTable.Clear();
        font.atlasPopulationMode = AtlasPopulationMode.Static;
    }
}