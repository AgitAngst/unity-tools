using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityTools.SceneOptimization
{
    // Panel under "Place Viewpoints": several sources and values are set before one action, which a menu cannot do
    // (a click in a menu closes it). "Place (≈ N)" applies, "Cancel" or a click outside closes it.
    sealed class ViewpointPlacePanel : PopupWindowContent
    {
        const float Width = 420f;
        const float LabelWidth = 150f;

        readonly AuditorWindow window;
        readonly PlacementSettings settings;
        readonly List<ZoneRect> zone;
        ViewpointPlacer.Survey survey;
        int preview = -1;
        GUIStyle note;

        public ViewpointPlacePanel(AuditorWindow window, PlacementSettings current, List<ZoneRect> zone)
        {
            this.window = window;
            this.zone = zone;
            settings = JsonUtility.FromJson<PlacementSettings>(JsonUtility.ToJson(current));
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(Width, 330f);
        }

        public override void OnOpen()
        {
            survey = ViewpointPlacer.Look();
            Count();
        }

        void Count()
        {
            float step;
            preview = ViewpointPlacer.Place(settings, settings.onlyZone ? zone : null, out step).Count;
        }

        public override void OnGUI(Rect rect)
        {
            if (note == null) note = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
            GUILayout.BeginArea(new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, rect.height - 16f));
            GUILayout.Label(Strings.Get("ui.vp.panel.title"), EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            settings.navMesh = Source("ui.vp.panel.navmesh", settings.navMesh, Strings.Get(survey.navMesh ? "ui.vp.panel.found" : "ui.vp.panel.missing"), true);
            settings.teleports = Source("ui.vp.panel.teleports", settings.teleports, survey.HasTeleports
                ? Strings.Format("ui.vp.panel.teleports.found", survey.teleportPoints, survey.teleportAreas)
                : Strings.Get("ui.vp.panel.missing"), true);
            settings.cameras = Source("ui.vp.panel.cameras", settings.cameras, Strings.Format("ui.vp.panel.cameras.found", survey.cameras), true);
            settings.rays = Source("ui.vp.panel.rays", settings.rays, Strings.Get("ui.vp.panel.rays.info"), !survey.navMesh && !survey.HasTeleports);
            GUILayout.Space(4f);
            ZoneBlock();
            GUILayout.Space(4f);
            EditorGUIUtility.labelWidth = LabelWidth;
            settings.step = EditorGUILayout.Slider(Strings.Get("ui.vp.panel.step"), settings.step, VRBudgets.GridStepMin, VRBudgets.GridStepMax);
            settings.eyeHeight = Mathf.Clamp(EditorGUILayout.FloatField(Strings.Get("ui.vp.panel.eye"), settings.eyeHeight), 0f, 3f);
            EditorGUIUtility.labelWidth = 0f;
            if (EditorGUI.EndChangeCheck()) Count();
            GUILayout.Space(4f);
            GUILayout.Label(Strings.Get("ui.vp.panel.note"), note);
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(Strings.Get("ui.fix.cancel"), GUILayout.Width(90f))) editorWindow.Close();
            EditorGUI.BeginDisabledGroup(preview <= 0);
            if (GUILayout.Button(Strings.Format("ui.vp.panel.place", preview), GUILayout.MinWidth(120f)))
            {
                var chosen = settings;
                var target = window;
                editorWindow.Close();
                EditorApplication.delayCall += () => { if (target != null) target.PlaceViewpoints(chosen); };
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        // "Only in the player zone" (3.0.0): the zone found and a button that opens its editing in the Scene view.
        void ZoneBlock()
        {
            bool has = zone != null && zone.Count > 0;
            var info = has ? Strings.Format("ui.vp.panel.zone.info", Strings.Count("ui.vp.zone.rects", zone.Count), AuditorWindow.FormatNumber(System.Math.Round(zone.Sum(r => r.Area))))
                           : Strings.Get("ui.vp.panel.zone.none");
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(!has);
            bool now = EditorGUILayout.ToggleLeft(Strings.Get("ui.vp.panel.zone"), settings.onlyZone && has, EditorStyles.boldLabel, GUILayout.Width(LabelWidth + 10f));
            EditorGUI.EndDisabledGroup();
            if (has) settings.onlyZone = now;
            GUILayout.FlexibleSpace();
            GUILayout.Label(info, EditorStyles.miniLabel);
            if (GUILayout.Button(Strings.Get(has ? "ui.vp.panel.zone.edit" : "ui.vp.panel.zone.set"), EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
            {
                var target = window;
                var chosen = settings;
                editorWindow.Close();
                EditorApplication.delayCall += () => { if (target != null) target.EditZone(true, chosen); };
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Label(Strings.Get(has ? "ui.vp.panel.zone.hint" : "ui.vp.panel.zone.hintNone"), note);
        }

        // A source: checkbox, name, and what was found on the right.
        bool Source(string key, bool value, string found, bool enabled)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(!enabled);
            bool now = EditorGUILayout.ToggleLeft(Strings.Get(key), value && enabled, GUILayout.Width(LabelWidth + 10f));
            EditorGUI.EndDisabledGroup();
            GUILayout.FlexibleSpace();
            GUILayout.Label(found, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
            return enabled ? now : value;
        }
    }
}
