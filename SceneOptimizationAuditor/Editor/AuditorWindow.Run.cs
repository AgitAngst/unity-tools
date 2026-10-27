using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityTools.SceneOptimization
{
    public sealed partial class AuditorWindow
    {
        // ---- run bar and the kept run (2.7.0) ----

        const float RunBarHeight = 36f;

        // The last run of the window: a window opened again or re-created by the editor shows it at once
        // (owner, 27.10.2026: "when the window loses focus, the current audit drops").
        static AuditResult kept;

        // A script reload clears the statics and every field that Unity does not serialize, the run among them: the
        // scope of the run is kept with the window and checked again by itself.
        [SerializeField] bool hadRun;
        [SerializeField] ScopeKind keptScope;
        [SerializeField] string keptFolder = "";
        [SerializeField] GameObject[] keptSelection;
        [SerializeField] int keptScenes;
        [SerializeField] string keptTime = "";
        [SerializeField] List<string> keptOpen = new List<string>();
        // The reload dropped a run that is not checked again by itself (build scenes) or whose check was cancelled.
        [SerializeField] bool dropped;
        [SerializeField] bool droppedCancelled;
        [NonSerialized] bool rerunDue;

        static Color BarLine { get { return EditorGUIUtility.isProSkin ? new Color(0.1f, 0.1f, 0.1f) : new Color(0.6f, 0.6f, 0.6f); } }

        // Scope on the left with a short line about the runs, Run on the right — as Build in Build Settings.
        void DrawRunBar(Rect rect)
        {
            if (Event.current.type == EventType.Repaint)
            {
                EditorStyles.toolbar.Draw(rect, false, false, false, false);
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), BarLine);
            }
            var run = new GUIContent(" " + Strings.Get(result == null ? "ui.run" : "ui.rerun"), playIcon, Strings.Get("ui.run.tip"));
            float runWidth = Mathf.Max(position.width < 640f ? 150f : 170f, runButton.CalcSize(run).x + 24f);
            var runRect = new Rect(rect.xMax - 8f - runWidth, rect.y + 4f, runWidth, 28f);
            if (GUI.Button(runRect, run, runButton)) EditorApplication.delayCall += Run;

            var scope = new GUIContent(Strings.Format("ui.scope.button", ScopeLabel()), Strings.Get("ui.scope.tip"));
            var dropStyle = (GUIStyle)"DropDownButton";
            // The style's size leaves no room for its arrow.
            float scopeWidth = Mathf.Min(dropStyle.CalcSize(scope).x + 18f, Mathf.Max(80f, runRect.x - rect.x - 24f));
            var scopeRect = new Rect(rect.x + 8f, rect.y + 8f, scopeWidth, 20f);
            if (EditorGUI.DropdownButton(scopeRect, scope, FocusType.Passive, dropStyle)) ScopeMenu();

            var status = BarStatus();
            var statusRect = new Rect(scopeRect.xMax + 8f, rect.y + 10f, Mathf.Max(0f, runRect.x - scopeRect.xMax - 16f), 16f);
            GUI.Label(statusRect, new GUIContent(status, status), EditorStyles.miniLabel);
        }

        // "Run at 14:32 · 3.4 s", then what happened to it since.
        string BarStatus()
        {
            if (result == null)
            {
                if (keptTime.Length > 0 && dropped) return Strings.Format("ui.bar.dropped", keptTime);
                if (keptTime.Length > 0 && rerunDue) return Strings.Format("ui.bar.waiting", keptTime);
                return Strings.Get("ui.bar.none");
            }
            var time = result.time.ToString("HH:mm", CultureInfo.InvariantCulture);
            var line = result.seconds > 0
                ? Strings.Format("ui.bar.run", time, FormatNumber(Math.Max(0.1, Math.Round(result.seconds, 1))))
                : Strings.Format("ui.bar.runAt", time);
            if (result.afterReload) line += "  ·  " + Strings.Get("ui.bar.afterReload");
            if (result.rechecked > 0)
                line += "  ·  " + Strings.Format("ui.bar.rechecked", Strings.Count(result.recheckedObjects ? "ui.rc.objects" : "ui.rc.items", result.rechecked),
                    result.recheckTime.ToString("HH:mm", CultureInfo.InvariantCulture));
            return line;
        }

        void Keep(AuditResult r)
        {
            kept = r;
            hadRun = true;
            dropped = false;
            droppedCancelled = false;
            keptScope = r.scope;
            keptFolder = r.folder ?? "";
            keptSelection = r.selection;
            keptScenes = r.scenes.Count;
            keptTime = r.time.ToString("HH:mm", CultureInfo.InvariantCulture);
        }

        void KeepOpenGroups()
        {
            keptOpen = expanded.Where(e => e.Value).Select(e => e.Key).ToList();
        }

        // A window opened again shows the last run; after a script reload the run is checked again once the editor is
        // out of Play Mode and done compiling, unless it opens scenes one by one.
        void RestoreKept()
        {
            if (result != null) return;
            if (kept != null)
            {
                Restore(kept);
                return;
            }
            if (!hadRun || dropped) return;
            if (KeptScenes) dropped = true;
            // A checked folder that is gone leaves nothing to check again.
            else if (keptScope == ScopeKind.Folder && !AssetDatabase.IsValidFolder(keptFolder)) hadRun = false;
            else rerunDue = true;
        }

        bool KeptScenes { get { return keptScope == ScopeKind.BuildScenes || keptScope == ScopeKind.Scenes; } }

        // The list as it was: no new history point, the open groups of the window if it kept them.
        void Restore(AuditResult r)
        {
            result = r;
            startShown = false;
            xrPreview = r.xr;
            groups = IssueGroup.Build(r.issues);
            acceptedList = AcceptedList.Load();
            prevPoint = PreviousPoint(r);
            openCopies.Clear();
            ListChanged();
            limits.Clear();
            picked.Clear();
            OpenGroups(keptOpen);
            Repaint();
        }

        void OpenGroups(List<string> open)
        {
            expanded.Clear();
            if (open != null && open.Count > 0)
                foreach (var key in open) expanded[key] = true;
            else
                foreach (var g in groups) if (g.severity == Severity.Critical) expanded[g.key] = true;
        }

        void RunKeptWhenDue()
        {
            if (!rerunDue || Event.current.type != EventType.Layout) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            rerunDue = false;
            EditorApplication.delayCall += RunKept;
        }

        // The scope of the dropped run again; the open groups stay and no history point is written.
        void RunKept()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            AuditResult r;
            switch (keptScope)
            {
                case ScopeKind.Selection:
                    var objects = (keptSelection ?? new GameObject[0]).Where(g => g != null).ToArray();
                    r = objects.Length > 0 ? AuditScope.RunSelection(objects, budgets, true) : SceneAuditor.RunFullAudit(budgets, true);
                    break;
                case ScopeKind.Folder:
                    r = AuditScope.RunFolder(keptFolder, budgets, true);
                    break;
                default:
                    r = SceneAuditor.RunFullAudit(budgets, true);
                    break;
            }
            if (r == null)
            {
                dropped = true;
                droppedCancelled = true;
                Repaint();
                return;
            }
            r.seconds = watch.Elapsed.TotalSeconds;
            r.afterReload = true;
            var open = keptOpen;
            Show(r, false, false);
            OpenGroups(open);
            ShowNotification(new GUIContent(Strings.Get("ui.kept.reloaded")));
        }

        // The run dropped by a script reload: build scenes again, or the same scope after a cancelled check.
        void RunDropped()
        {
            if (!KeptScenes)
            {
                RunKept();
                return;
            }
            var paths = AuditScope.BuildScenePaths();
            if (paths.Count == 0 || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Run();
                return;
            }
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var r = AuditScope.RunScenes(paths, budgets, true, ScopeKind.BuildScenes, null);
            if (r == null) return;
            r.seconds = watch.Elapsed.TotalSeconds;
            Show(r, false);
        }

        // Above "Getting started" while the dropped run waits for the user.
        void DrawDroppedBar()
        {
            if (!dropped || result != null || !hadRun) return;
            bool scenes = KeptScenes && !droppedCancelled;
            GUILayout.Space(2f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label(warnIcon, GUILayout.Width(18f), GUILayout.Height(18f));
            EditorGUILayout.BeginVertical();
            GUILayout.Label(Strings.Get(scenes ? "ui.kept.dropped.scenesTitle" : "ui.kept.dropped.title"), EditorStyles.boldLabel);
            var text = scenes ? Strings.Format("ui.kept.dropped.scenes", keptTime, keptScenes) : Strings.Format("ui.kept.dropped.cancelled", keptTime);
            GUILayout.Label(text, wrapMini);
            EditorGUILayout.EndVertical();
            var label = KeptScenes ? Strings.Format("ui.kept.runScenes", Strings.Count("ui.rc.scenes", keptScenes)) : Strings.Get("ui.rerun");
            if (GUILayout.Button(label, EditorStyles.miniButton, GUILayout.ExpandWidth(false))) EditorApplication.delayCall += RunDropped;
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(8f);
            EditorGUILayout.EndHorizontal();
        }

        // The point before the last one of the run's history, for the progress line, without writing a point.
        static HistoryPoint PreviousPoint(AuditResult r)
        {
            if (r.scope != ScopeKind.Loaded) return null;
            try
            {
                var h = AuditHistory.Load(AuditHistory.FileFor(r.scenePaths));
                return h.points.Count > 1 ? h.points[h.points.Count - 2] : null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
