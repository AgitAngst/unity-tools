using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace UnityTools.SceneOptimization
{
    public enum Lang { En = 0, Ru = 1 }

    // Every window text in English and Russian. Console and log output stays English and is not here.
    public static partial class Strings
    {
        const string LangPref = "UnityTools.SceneAudit.Lang";

        static readonly Dictionary<string, string[]> table = new Dictionary<string, string[]>();
        static bool langLoaded;
        static Lang lang;

        public static Lang Current
        {
            get
            {
                if (!langLoaded)
                {
                    var fallback = Application.systemLanguage == SystemLanguage.Russian ? Lang.Ru : Lang.En;
                    lang = (Lang)EditorPrefs.GetInt(LangPref, (int)fallback);
                    langLoaded = true;
                }
                return lang;
            }
            set
            {
                lang = value;
                langLoaded = true;
                EditorPrefs.SetInt(LangPref, (int)value);
            }
        }

        public static bool Has(string key) { return table.ContainsKey(key); }

        public static IEnumerable<string> Keys { get { return table.Keys; } }

        public static string Get(string key)
        {
            string[] texts;
            if (key != null && table.TryGetValue(key, out texts)) return texts[(int)Current];
            return key ?? "";
        }

        public static string Format(string key, params object[] args)
        {
            var text = Get(key);
            if (args == null || args.Length == 0) return text;
            try
            {
                return string.Format(CultureInfo.InvariantCulture, text, args);
            }
            catch (FormatException)
            {
                return text;
            }
        }

        public static string CategoryName(Category category) { return Get("cat." + category); }

        public static string SeverityName(Severity severity) { return Get("sev." + severity); }

        static void Add(string key, string en, string ru) { table[key] = new[] { en, ru }; }

        static Strings()
        {
            AddUi();
            AddDrawCalls();
            AddMeshes();
            AddTextures();
            AddLighting();
            AddPhysics();
            AddAnimators();
            AddAudio();
            AddVr();
            AddGeneral();
            AddAssets();
            AddTitles();
            AddViewpoints();
            AddLight();
            AddHeavy();
            AddParticles();
            AddShaders();
            AddReflections();
            AddScope();
            AddGain();
            AddHelp();
            AddCheckHelp();
        }
    }
}
