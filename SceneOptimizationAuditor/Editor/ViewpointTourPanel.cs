using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityTools.SceneOptimization
{
    // Panel under "Measure in Play Mode": which viewpoints, frames, warm-up, directions and what to move, kept in
    // EditorPrefs. "Measure (~N)" starts the tour, "Cancel" or a click outside closes it.
    sealed class ViewpointTourPanel : PopupWindowContent
    {
        const string Prefix = "UnityTools.SceneAudit.Tour.";
        const float Width = 420f;
        const float LabelWidth = 160f;

        readonly AuditorWindow window;
        readonly int pickedCount;
        readonly int allCount;
        GUIStyle note;
        GameObject moved;

        public ViewpointTourPanel(AuditorWindow window, int pickedCount, int allCount)
        {
            this.window = window;
            this.pickedCount = pickedCount;
            this.allCount = allCount;
        }

        // 0 — worst by estimate, 1 — selected, 2 — all.
        public static int Which { get { return EditorPrefs.GetInt(Prefix + "Which", 0); } set { EditorPrefs.SetInt(Prefix + "Which", value); } }
        public static int Count { get { return EditorPrefs.GetInt(Prefix + "Count", 30); } set { EditorPrefs.SetInt(Prefix + "Count", value); } }
        public static int Frames { get { return EditorPrefs.GetInt(Prefix + "Frames", 30); } set { EditorPrefs.SetInt(Prefix + "Frames", value); } }
        public static int Warmup { get { return EditorPrefs.GetInt(Prefix + "Warmup", 15); } set { EditorPrefs.SetInt(Prefix + "Warmup", value); } }
        public static int Directions { get { return EditorPrefs.GetInt(Prefix + "Directions", 8); } set { EditorPrefs.SetInt(Prefix + "Directions", value); } }
        public static TourMover Mover { get { return (TourMover)EditorPrefs.GetInt(Prefix + "Mover", 0); } set { EditorPrefs.SetInt(Prefix + "Mover", (int)value); } }
        public static string ObjectPath { get { return EditorPrefs.GetString(Prefix + "Object", ""); } set { EditorPrefs.SetString(Prefix + "Object", value); } }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(Width, Mover == TourMover.Object ? 330f : 308f);
        }

        public override void OnOpen()
        {
            var path = ObjectPath;
            if (path.Length > 0) moved = GameObject.Find("/" + path.TrimStart('/'));
        }

        public override void OnGUI(Rect rect)
        {
            if (note == null) note = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
            GUILayout.BeginArea(new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, rect.height - 16f));
            GUILayout.Label(Strings.Get("ui.vp.tour.title"), EditorStyles.boldLabel);
            EditorGUIUtility.labelWidth = LabelWidth;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(Strings.Get("ui.vp.tour.which"), GUILayout.Width(LabelWidth - 4f));
            var options = new[] { Strings.Get("ui.vp.tour.worst"), Strings.Format("ui.vp.tour.selected", pickedCount), Strings.Format("ui.vp.tour.all", allCount) };
            Which = GUILayout.Toolbar(Which, options, EditorStyles.miniButton);
            EditorGUILayout.EndHorizontal();
            EditorGUI.BeginDisabledGroup(Which != 0);
            Count = Mathf.Clamp(EditorGUILayout.IntField(Strings.Get("ui.vp.tour.count"), Count), 1, VRBudgets.MaxViewpoints);
            EditorGUI.EndDisabledGroup();
            Frames = Mathf.Clamp(EditorGUILayout.IntField(Strings.Get("ui.vp.tour.frames"), Frames), 5, 1000);
            Warmup = Mathf.Clamp(EditorGUILayout.IntField(Strings.Get("ui.vp.tour.warmup"), Warmup), 0, 300);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(Strings.Get("ui.vp.tour.dirs"), GUILayout.Width(LabelWidth - 4f));
            Directions = GUILayout.Toolbar(Directions == 4 ? 1 : 0, new[] { "8", "4" }, EditorStyles.miniButton, GUILayout.Width(80f)) == 1 ? 4 : 8;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            var movers = new[] { Strings.Get("ui.vp.mover.Rig"), Strings.Get("ui.vp.mover.Object"), Strings.Get("ui.vp.mover.Camera") };
            Mover = (TourMover)EditorGUILayout.Popup(Strings.Get("ui.vp.tour.move"), (int)Mover, movers);
            if (Mover == TourMover.Object)
            {
                var picked = (GameObject)EditorGUILayout.ObjectField(Strings.Get("ui.vp.tour.object"), moved, typeof(GameObject), true);
                if (picked != moved)
                {
                    moved = picked;
                    ObjectPath = picked != null ? AuditContext.PathOf(picked.transform) : "";
                }
            }
            EditorGUIUtility.labelWidth = 0f;
            GUILayout.Space(4f);
            GUILayout.Label(Strings.Get("ui.vp.tour.headsetNote"), note);
            GUILayout.Label(Strings.Get("ui.vp.tour.note"), note);
            GUILayout.FlexibleSpace();

            // The plan the button would start: manual viewpoints and cameras have their own direction too.
            var plan = window != null ? window.TourPlanNow() : new TourPlan();
            int points = plan.stops.Count;
            float seconds = plan.Seconds;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(Strings.Format("ui.vp.tour.time", points, plan.Steps, Warmup + Frames, AuditorWindow.Duration(seconds)), note);
            if (GUILayout.Button(Strings.Get("ui.fix.cancel"), GUILayout.Width(80f))) editorWindow.Close();
            bool ready = points > 0 && (Mover != TourMover.Object || ObjectPath.Length > 0);
            EditorGUI.BeginDisabledGroup(!ready);
            if (GUILayout.Button(Strings.Format("ui.vp.tour.start", AuditorWindow.Duration(seconds)), GUILayout.MinWidth(130f)))
            {
                var target = window;
                editorWindow.Close();
                EditorApplication.delayCall += () => { if (target != null) target.StartTour(); };
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}
