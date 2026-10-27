using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityTools.SceneOptimization;

namespace AuditTestsEditor
{
    // Private fields of an editor window survive a script reload; styles and icons must not, or an update with the window
    // open leaves the newer ones empty (2.1.1: the Problems list failed on the styles of 2.0.0).
    public static partial class AuditTestViewpoints
    {
        public static void RunWindowFields()
        {
            Run("window-fields", WindowFields);
        }

        static void WindowFields()
        {
            // The fields of the utility's own windows, not those of EditorWindow (title, notification).
            foreach (var type in new[] { typeof(AuditorWindow), typeof(FixConfirmWindow) })
            {
                var cached = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .Where(f => typeof(GUIStyle).IsAssignableFrom(f.FieldType) || typeof(Texture).IsAssignableFrom(f.FieldType) ||
                                typeof(GUIContent).IsAssignableFrom(f.FieldType))
                    .ToList();
                var kept = cached.Where(f => !f.IsNotSerialized).Select(f => f.Name).ToArray();
                Check(cached.Count > 0 && kept.Length == 0,
                    type.Name + ": " + cached.Count + " styles and icons are not kept over a script reload" + (kept.Length > 0 ? " (kept: " + string.Join(", ", kept) + ")" : ""));
            }
            var ready = typeof(AuditorWindow).GetField("stylesReady", BindingFlags.Instance | BindingFlags.NonPublic);
            Check(ready != null && ready.IsNotSerialized, "the window makes its styles again after a reload (stylesReady is not kept)");
        }
    }
}
