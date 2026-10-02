using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace Bielarusizatar.Valheim
{
    [BepInPlugin("lokar.Bielarusizatar_Valheim", "Bielarusizatar Valheim", "1.3.0")]
    public class BielarusizatarPlugin : BaseUnityPlugin
    {
        // Only the letters Valheim's own fonts genuinely lack. The shipped fonts already
        // contain the full Russian Cyrillic range, so adding more than this would just
        // waste atlas space and risk overlapping existing glyphs.
        private const string CharsToAdd = "ЎўЁёІі";

        private const float PollInterval = 1f;
        private const int MaxPolls = 600;

        // Patched TTFs shipped next to the DLL, matched to the game font family they back.
        private static readonly (string Match, string File)[] Sources =
        {
            ("AveriaSerifLibre", "Averia Serif Libre Regular.ttf"),
            ("AveriaSerifLibre", "Averia Serif Libre Bold.ttf"),
            ("AveriaSansLibre",  "Averia Sans Libre.ttf"),
            ("AveriaSansLibre",  "Averia Sans Libre Bold.ttf"),
            ("Norse",            "Norse.ttf"),
            ("Norse",            "Norsebold.ttf"),
        };

        private static readonly FieldInfo SourceFontFilePath =
            typeof(TMP_FontAsset).GetField("m_SourceFontFilePath", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo SourceFontFile =
            typeof(TMP_FontAsset).GetField("m_SourceFontFile", BindingFlags.NonPublic | BindingFlags.Instance);

        internal static BielarusizatarPlugin Instance { get; private set; }
        internal string PluginDir { get; private set; }
        internal ManualLogSource Log => Logger;

        private readonly HashSet<string> _done = new HashSet<string>(StringComparer.Ordinal);
        private float _lastPoll;
        private int _polls;

        private void Awake()
        {
            Instance = this;
            PluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";

            Logger.LogInfo("Bielarusizatar Valheim started (plugin dir: " + PluginDir + ")");

            foreach (var source in Sources)
            {
                string path = Path.Combine(PluginDir, source.File);
                if (!File.Exists(path)) Logger.LogWarning("Font file missing: " + path);
            }

            try
            {
                new Harmony("lokar.Bielarusizatar_Valheim").PatchAll();
                Logger.LogInfo("Subtitle patch applied.");
            }
            catch (Exception e)
            {
                Logger.LogError("Subtitle patch failed: " + e);
            }
        }

        private void Update()
        {
            if (_polls >= MaxPolls) return;
            if (Time.realtimeSinceStartup - _lastPoll < PollInterval) return;
            _lastPoll = Time.realtimeSinceStartup;
            _polls++;

            TMP_FontAsset[] gameFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            if (gameFonts == null || gameFonts.Length == 0) return;

            foreach (var source in Sources)
            {
                string match = source.Match;
                string file = source.File;
                if (_done.Contains(file)) continue;

                string path = Path.Combine(PluginDir, file);
                if (!File.Exists(path)) { _done.Add(file); continue; }

                bool isBold = file.IndexOf("Bold", StringComparison.OrdinalIgnoreCase) >= 0;
                var targets = MatchGameFonts(gameFonts, match, isBold);
                if (targets.Count == 0) continue;

                _done.Add(file);
                foreach (var font in targets) ApplyToFont(font, path, file);

                if (_done.Count >= Sources.Length)
                {
                    _polls = MaxPolls;
                    Logger.LogInfo("Font setup complete.");
                }
            }
        }

        private static List<TMP_FontAsset> MatchGameFonts(TMP_FontAsset[] all, string match, bool isBold)
        {
            var result = new List<TMP_FontAsset>();
            foreach (var gf in all)
            {
                if (gf == null) continue;
                string n = gf.name ?? string.Empty;
                if (n.IndexOf(match, StringComparison.OrdinalIgnoreCase) < 0) continue;
                bool gfBold = n.IndexOf("bold", StringComparison.OrdinalIgnoreCase) >= 0;
                if (gfBold != isBold) continue;
                if (gf.HasCharacter(0x45E) && gf.HasCharacter(0x40E)) continue;
                result.Add(gf);
            }
            return result;
        }

        private void ApplyToFont(TMP_FontAsset font, string ttfPath, string file)
        {
            try
            {
                string name = font.name ?? "?";
                int before = font.glyphTable?.Count ?? 0;

                // Dynamic population is what lets TMP rasterize the new glyphs.
                if (font.atlasPopulationMode != AtlasPopulationMode.Dynamic)
                {
                    Logger.LogInfo("Font [" + name + "]: AtlasPopulationMode " + font.atlasPopulationMode + " -> Dynamic.");
                    font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                }

                // Point the asset at our patched TTF and drop the cached Unity Font so
                // TMP reloads the face through its own LoadFontFace() on the next add.
                // We deliberately do NOT call FontEngine.LoadFontFace ourselves: it shares
                // one global face slot and corrupts the game's glyphs when raced with TMP.
                SourceFontFilePath?.SetValue(font, ttfPath);
                SourceFontFile?.SetValue(font, null);

                bool ok = font.TryAddCharacters(CharsToAdd, out string missing, false);
                var textures = font.atlasTextures;
                if (textures != null)
                {
                    foreach (var t in textures) if (t != null) t.Apply(false, false);
                }

                Logger.LogInfo("Font [" + name + "] <- " + Path.GetFileName(ttfPath) +
                               ": added=" + ok + " missing='" + (missing ?? "") + "'" +
                               " glyphs " + before + "->" + (font.glyphTable?.Count ?? 0) +
                               " ў=" + font.HasCharacter(0x45E) + " Ў=" + font.HasCharacter(0x40E) +
                               " Ж=" + font.HasCharacter(0x416) + " ж=" + font.HasCharacter(0x436) +
                               " і=" + font.HasCharacter(0x456));
            }
            catch (Exception e)
            {
                Logger.LogError("Font [" + (font == null ? "?" : font.name) + "]: " + e);
            }
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
                string text = File.ReadAllText(path);
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
            if (!string.IsNullOrEmpty(language) && MentionsBelarusian(language)) return true;
            string pref = PlayerPrefs.GetString("language", "English");
            return MentionsBelarusian(pref);
        }

        private static bool MentionsBelarusian(string s) =>
            s.IndexOf("belarus", StringComparison.OrdinalIgnoreCase) >= 0 ||
            s.IndexOf("Белар", StringComparison.Ordinal) >= 0;
    }
}
