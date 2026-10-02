using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Entities;
using TMPro;
using UnityEngine;

namespace Bielarusizatar.Valheim
{
    [BepInPlugin("lokar.Bielarusizatar_Valheim", "Bielarusizatar Valheim", "2.0.0")]
    public class BielarusizatarPlugin : BaseUnityPlugin
    {
        internal static BielarusizatarPlugin Instance { get; private set; }
        internal string PluginDir { get; private set; }
        internal ManualLogSource Log => Logger;

        // Valheim's own font assets lack the Belarusian short-u (ў/Ў), and every attempt to
        // rasterize it into their glyph atlas corrupts neighbouring glyphs (Ж broke this way).
        // U+00FD ý / U+00DD Ý — Latin y with an acute — exist in all six shipped fonts and are
        // visually close to ў, so we substitute them at the text level and never touch an atlas.
        // The substitution is idempotent and applies only to our own translated strings, so
        // player-typed Russian is untouched.
        internal const char ShortU = 'ў';
        internal const char ShortUUpper = 'Ў';
        internal const char ShortUSub = 'ý';
        internal const char ShortUSubUpper = 'Ý';

        private const float SweepDelay = 3f;
        private bool _swept;

        private void Awake()
        {
            Instance = this;
            PluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";

            Logger.LogInfo("Bielarusizatar Valheim started (plugin dir: " + PluginDir + ")");

            try
            {
                new Harmony("lokar.Bielarusizatar_Valheim").PatchAll();
                Logger.LogInfo("Localization substitution patch applied.");
            }
            catch (Exception e)
            {
                Logger.LogError("Localization patch failed: " + e);
            }
        }

        // Safety net for the case where Jotunn's JSON auto-loader writes straight into
        // CustomLocalization.Map instead of routing through AddTranslation. The map is an
        // instance property, so the instances come from LocalizationManager.Localizations.
        private void Update()
        {
            if (_swept) return;
            if (Time.realtimeSinceStartup < SweepDelay) return;
            _swept = true;

            try
            {
                var localizations = LocalizationList?.GetValue(null) as System.Collections.IEnumerable;
                if (localizations == null)
                {
                    Logger.LogWarning("Localization list unavailable; AddTranslation hook is the only path.");
                    return;
                }

                int locales = 0;
                int changed = 0;
                foreach (CustomLocalization loc in localizations)
                {
                    if (loc == null) continue;
                    var map = LocalizationMap?.GetValue(loc) as Dictionary<string, Dictionary<string, string>>;
                    if (map == null) continue;
                    locales++;

                    foreach (var language in new List<string>(map.Keys))
                    {
                        if (!IsBelarusian(language)) continue;
                        var entries = map[language];
                        if (entries == null) continue;

                        var keys = new List<string>(entries.Keys);
                        foreach (var key in keys)
                        {
                            string before = entries[key];
                            if (string.IsNullOrEmpty(before)) continue;
                            string after = Substitute(before);
                            if (after == before) continue;
                            entries[key] = after;
                            changed++;
                        }
                    }
                }

                Logger.LogInfo("Map sweep visited " + locales + " localization(s), rewrote " + changed + " string(s).");
            }
            catch (Exception e)
            {
                Logger.LogError("Localization map sweep failed: " + e);
            }
        }

        // Map is an instance property; LocalizationManager.Localizations holds the instances.
        internal static readonly PropertyInfo LocalizationMap =
            typeof(CustomLocalization).GetProperty("Map", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        internal static readonly FieldInfo LocalizationList =
            typeof(Jotunn.Managers.LocalizationManager).GetField("Localizations",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        internal static string Substitute(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            if (text.IndexOf(ShortU) < 0 && text.IndexOf(ShortUUpper) < 0) return text;

            var builder = new System.Text.StringBuilder(text.Length);
            foreach (char c in text)
            {
                if (c == ShortU) builder.Append(ShortUSub);
                else if (c == ShortUUpper) builder.Append(ShortUSubUpper);
                else builder.Append(c);
            }
            return builder.ToString();
        }

        internal static bool IsBelarusian(string language)
        {
            if (string.IsNullOrEmpty(language)) return false;
            return language.IndexOf("belarus", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   language.IndexOf("Белар", StringComparison.Ordinal) >= 0;
        }
    }

    // Every translation inserted into a CustomLocalization is rewritten here, whichever
    // loader (JSON, YAML, direct) delivered it. Both AddTranslation overloads expose a
    // by-ref 'translation' parameter, so one postfix covers them.
    [HarmonyPatch]
    internal static class AddTranslationPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(CustomLocalization), "AddTranslation",
                new[] { typeof(string).MakeByRefType(), typeof(string).MakeByRefType(), typeof(string) });
            yield return AccessTools.Method(typeof(CustomLocalization), "AddTranslation",
                new[] { typeof(string).MakeByRefType(), typeof(string) });
        }

        private static void Postfix(ref string translation)
        {
            if (string.IsNullOrEmpty(translation)) return;
            string after = BielarusizatarPlugin.Substitute(translation);
            if (!ReferenceEquals(after, translation) && after != translation) translation = after;
        }
    }

    // Anything the player types — a character name, a chat message — never passes through
    // AddTranslation, so it has to be substituted on its way into the renderer. Patching the
    // base TMP_Text.text setter covers every label and the input field's own display; no
    // Valheim type overrides the accessor, so nothing bypasses this. The stored string keeps
    // its real ў because we only rewrite the value handed to the renderer.
    [HarmonyPatch(typeof(TMP_Text), "set_text")]
    internal static class TmpTextSetterPatch
    {
        private static void Prefix(ref string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            if (value.IndexOf(BielarusizatarPlugin.ShortU) < 0 &&
                value.IndexOf(BielarusizatarPlugin.ShortUUpper) < 0) return;
            value = BielarusizatarPlugin.Substitute(value);
        }
    }

    [HarmonyPatch(typeof(CinematicsManager), nameof(CinematicsManager.GetSubtitle))]
    internal static class SubtitlePatch
    {
        private static readonly FieldInfo CurrentVideo =
            typeof(CinematicsManager).GetField("m_currentVideo", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo VideoName =
            typeof(CinematicsManager.VideoEntry).GetField("m_name", BindingFlags.Public | BindingFlags.Instance);

        private static TextAsset _intro;
        private static TextAsset _outro;
        private static bool _loaded;

        private static void Postfix(string language, ref TextAsset __result)
        {
            try
            {
                if (!IsBelarusian(language)) return;
                if (!_loaded)
                {
                    _loaded = true;
                    _intro = LoadAsset("Valheim_intro_subtitles_be.srt");
                    _outro = LoadAsset("Valheim_outro_subtitles_be.srt");
                }

                var mgr = CinematicsManager.s_instance;
                object cur = mgr == null ? null : CurrentVideo?.GetValue(mgr);
                string name = cur == null ? null : VideoName?.GetValue(cur) as string;
                bool isOutro = name != null && name.IndexOf("outro", StringComparison.OrdinalIgnoreCase) >= 0;

                var pick = isOutro ? _outro : _intro;
                if (pick != null) __result = pick;
            }
            catch (Exception e)
            {
                BielarusizatarPlugin.Instance?.Log.LogError("Subtitle injection failed: " + e);
            }
        }

        private static TextAsset LoadAsset(string file)
        {
            try
            {
                var plugin = BielarusizatarPlugin.Instance;
                if (plugin == null) return null;
                string path = Path.Combine(plugin.PluginDir, "Assets", "Subtitles", file);
                if (!File.Exists(path))
                {
                    plugin.Log.LogWarning("Subtitle file not found: " + path);
                    return null;
                }
                string text = BielarusizatarPlugin.Substitute(File.ReadAllText(path));
                plugin.Log.LogInfo("Subtitles loaded: " + file + " (" + text.Length + " chars)");
                return new TextAsset(text);
            }
            catch (Exception e)
            {
                BielarusizatarPlugin.Instance?.Log.LogError("Failed to load " + file + ": " + e);
                return null;
            }
        }

        private static bool IsBelarusian(string language)
        {
            if (BielarusizatarPlugin.IsBelarusian(language)) return true;
            string pref = PlayerPrefs.GetString("language", "English");
            return BielarusizatarPlugin.IsBelarusian(pref);
        }
    }
}
