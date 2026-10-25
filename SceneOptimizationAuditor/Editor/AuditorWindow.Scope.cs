using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityTools.SceneOptimization
{
    public sealed partial class AuditorWindow
    {
        // ---- scope ----

        // Scenes shown in the table before "Show all".
        const int SceneTableRows = 8;

        [SerializeField] bool sceneTableAll;

        // What "Run" checks, as the scope button names it.
        static string ScopeLabel()
        {
            switch (AuditScope.Kind)
            {
                case ScopeKind.Selection: return Strings.Format("ui.scope.selection", AuditScope.SelectedObjects().Length);
                case ScopeKind.BuildScenes: return Strings.Format("ui.scope.build", AuditScope.BuildScenePaths().Count);
                case ScopeKind.Folder: return Strings.Format("ui.scope.folder", FolderName(AuditScope.Folder));
                default: return Strings.Get("ui.scope.loaded");
            }
        }

        static string FolderName(string folder)
        {
            var slash = folder.LastIndexOf('/');
            return slash >= 0 ? folder.Substring(slash + 1) : folder;
        }

        void ScopeButton()
        {
            var content = new GUIContent(Strings.Format("ui.scope.button", ScopeLabel()), Strings.Get("ui.scope.tip"));
            if (GUILayout.Button(content, EditorStyles.toolbarDropDown, GUILayout.ExpandWidth(false))) ScopeMenu();
        }

        // A check at the current scope; a scope with nothing to check is greyed out.
        void ScopeMenu()
        {
            var kind = AuditScope.Kind;
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent(Strings.Get("ui.scope.loaded")), kind == ScopeKind.Loaded, () => AuditScope.Kind = ScopeKind.Loaded);
            int selected = AuditScope.SelectedObjects().Length;
            var selection = new GUIContent(Strings.Format("ui.scope.selection", selected));
            if (selected > 0) menu.AddItem(selection, kind == ScopeKind.Selection, () => AuditScope.Kind = ScopeKind.Selection);
            else menu.AddDisabledItem(selection, kind == ScopeKind.Selection);
            int scenes = AuditScope.BuildScenePaths().Count;
            var build = new GUIContent(Strings.Format("ui.scope.build", scenes));
            if (scenes > 0) menu.AddItem(build, kind == ScopeKind.BuildScenes, () => AuditScope.Kind = ScopeKind.BuildScenes);
            else menu.AddDisabledItem(build, kind == ScopeKind.BuildScenes);
            menu.AddSeparator("");
            menu.AddItem(new GUIContent(Strings.Get("ui.scope.pickFolder")), false, PickFolder);
            var folder = AuditScope.Folder;
            // A slash would make a submenu, so the path is shown with a division slash.
            if (AssetDatabase.IsValidFolder(folder))
                menu.AddItem(new GUIContent(folder.Replace("/", "∕")), kind == ScopeKind.Folder, () => AuditScope.Kind = ScopeKind.Folder);
            menu.ShowAsContext();
        }

        void PickFolder()
        {
            var start = AssetDatabase.IsValidFolder(AuditScope.Folder) ? AuditScope.Folder : "Assets";
            var picked = EditorUtility.OpenFolderPanel(Strings.Get("ui.scope.pickFolder.title"), start, "");
            if (string.IsNullOrEmpty(picked)) return;
            var folder = AuditScope.ProjectFolder(picked);
            if (folder.Length == 0 || !AssetDatabase.IsValidFolder(folder))
            {
                EditorUtility.DisplayDialog(Strings.Get("ui.title"), Strings.Get("ui.scope.outside"), Strings.Get("ui.fix.ok"));
                return;
            }
            AuditScope.Folder = folder;
            AuditScope.Kind = ScopeKind.Folder;
            Repaint();
        }

        // The line under the verdict: what was checked and when.
        string ScopeContext()
        {
            var r = result;
            var time = r.time.ToString("HH:mm", CultureInfo.InvariantCulture);
            switch (r.scope)
            {
                case ScopeKind.Selection:
                    return Strings.Format("ui.scope.ctx.selection", string.Join(", ", r.sceneNames), time);
                case ScopeKind.BuildScenes:
                case ScopeKind.Scenes:
                    var open = r.scenes.Where(s => s.Loaded).Select(s => s.name).ToArray();
                    var line = Strings.Format(r.scope == ScopeKind.BuildScenes ? "ui.scope.ctx.build" : "ui.scope.ctx.scenes", r.scenes.Count,
                        open.Length > 0 ? string.Join(", ", open) : "—", time);
                    return r.incomplete ? line + "  ·  " + Strings.Format("ui.scope.incomplete", r.scenes.Count, r.planned) : line;
                case ScopeKind.Folder:
                    return Strings.Format("ui.scope.ctx.folder", r.folder, time);
                default:
                    return Strings.Format("ui.scope.ctx.loaded", string.Join(", ", r.sceneNames), time);
            }
        }

        // Budget bars of the scenes, or what the scope checked when the bars would not be about a scene.
        void DrawScopeBudgets()
        {
            switch (result.scope)
            {
                case ScopeKind.Selection:
                    ScopeNote(Strings.Format("ui.scope.selection.note", result.selected, result.selectedWithChildren));
                    return;
                case ScopeKind.Folder:
                    var c = result.folderCounts ?? new FolderCounts();
                    ScopeNote(Strings.Format("ui.scope.folder.note", c.prefabs, c.models, c.materials, c.textures, c.clips));
                    return;
                case ScopeKind.BuildScenes:
                case ScopeKind.Scenes:
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(8f);
                    GUILayout.Label(Strings.Get("ui.scope.worst"), wrapMini);
                    EditorGUILayout.EndHorizontal();
                    DrawBudgetBars(false);
                    DrawSceneTable();
                    return;
                default:
                    DrawBudgetBars(false);
                    return;
            }
        }

        void ScopeNote(string text)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8f);
            GUILayout.Label(text, hint);
            GUILayout.Space(8f);
            EditorGUILayout.EndHorizontal();
        }

        // Every checked scene: its problems and budgets; a closed scene can be opened.
        void DrawSceneTable()
        {
            var scenes = result.scenes;
            if (scenes.Count == 0) return;
            GUILayout.Space(4f);
            SubTitle("ui.scope.scenes");
            bool narrow = position.width < VpNarrowWidth;
            float action = Mathf.Max(EditorStyles.miniButton.CalcSize(new GUIContent(Strings.Get("ui.scope.open"))).x,
                EditorStyles.miniLabel.CalcSize(new GUIContent(Strings.Get("ui.scope.isOpen"))).x) + 10f;
            var widths = narrow ? new[] { 30f, 34f, 62f, 46f, action } : new[] { 58f, 62f, 40f, 96f, 76f, 92f, action };
            var keys = narrow
                ? new[] { "ui.scope.col.critical.short|ui.scope.col.critical", "ui.scope.col.warning.short|ui.scope.col.warning", "ui.scope.col.tris.short|ui.scope.col.tris", "ui.scope.col.dc.short|ui.scope.col.dc", "" }
                : new[] { "ui.scope.col.critical", "ui.scope.col.warning", "ui.scope.col.info", "ui.scope.col.tris", "ui.scope.col.dc", "ui.scope.col.mb", "" };
            var head = Padded(GUILayoutUtility.GetRect(0f, 20f, GUILayout.ExpandWidth(true)));
            var headCells = LightColumns(head, widths, 0f);
            LightHeader(headCells, 1, new[] { "ui.scope.col.scene" }.Concat(keys).ToArray(), new[] { false }.Concat(keys.Select(k => k.Length > 0)).ToArray());
            int shown = sceneTableAll ? scenes.Count : Math.Min(scenes.Count, SceneTableRows);
            for (int i = 0; i < shown; i++)
            {
                var s = scenes[i];
                var stripe = s.critical > 0 ? CriticalColor : s.warning > 0 ? WarningColor : (Color?)null;
                var rect = LightRow(i, false, stripe, 22f);
                var cells = LightColumns(rect, widths, 0f);
                ObjectLink(cells[1], AssetDatabase.LoadAssetAtPath<SceneAsset>(s.path), s.name, s.path, 0f);
                // Millions only where a full number would not fit the narrow column.
                var tris = s.tris < 0 ? "—" : narrow && s.tris >= 1000000 ? FormatNumber(Math.Round(s.tris / 1000000.0, 1)) + Strings.Get("ui.scope.millions") : FormatNumber(s.tris);
                var dc = s.drawCalls < 0 ? "—" : FormatNumber(s.drawCalls);
                var values = narrow
                    ? new[] { FormatNumber(s.critical), FormatNumber(s.warning), tris, dc }
                    : new[] { FormatNumber(s.critical), FormatNumber(s.warning), FormatNumber(s.info), tris, dc, s.textureMB < 0 ? "—" : FormatNumber(Math.Round(s.textureMB)) };
                for (int c = 0; c < values.Length; c++)
                {
                    bool overTris = (narrow ? c == 2 : c == 3) && s.tris > result.budgets.sceneTrisWarning;
                    RightCell(cells[c + 2], values[c], c == 0 && s.critical > 0 ? TextCritical : overTris ? TextWarning : (Color?)null);
                }
                var last = cells[cells.Length - 1];
                if (s.Loaded) GUI.Label(new Rect(last.x, last.y + 3f, last.width, 16f), Strings.Get("ui.scope.isOpen"), centeredMini);
                else if (GUI.Button(new Rect(last.x, last.y + 2f, last.width, 18f), new GUIContent(Strings.Get("ui.scope.open"), Strings.Get("ui.scope.open.tip")), EditorStyles.miniButton))
                {
                    var path = s.path;
                    EditorApplication.delayCall += () => OpenSceneOf(path, "");
                }
            }
            if (scenes.Count > SceneTableRows)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(8f);
                var label = sceneTableAll ? Strings.Get("ui.scope.showFewer") : Strings.Format("ui.scope.showAll", scenes.Count);
                if (GUILayout.Button(label, EditorStyles.miniButton, GUILayout.ExpandWidth(false))) sceneTableAll = !sceneTableAll;
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.Space(4f);
        }

        // A row of a scene that is not open now: its objects cannot be fixed or selected until it is opened.
        static bool IsClosed(AuditIssue issue)
        {
            return !string.IsNullOrEmpty(issue.scenePath) && !AuditScope.IsLoaded(issue.scenePath);
        }

        // Opens a scene of the result (offering to save the open ones), checks it again so its rows can be fixed, then
        // selects the object of the row.
        void OpenSceneOf(string scenePath, string objectPath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (result != null && result.parts != null) Show(AuditScope.Refresh(result, budgets), true);
            SelectByPath(scenePath, objectPath);
        }

        // The row's object, found again by its path when the object of the run is gone (the scene was reopened).
        void SelectOrOpen(AuditIssue issue)
        {
            if (IsClosed(issue))
            {
                var scenePath = issue.scenePath;
                var objectPath = issue.objectPath;
                EditorApplication.delayCall += () => OpenSceneOf(scenePath, objectPath);
            }
            else if (Gone(issue.target)) SelectByPath(issue.scenePath, issue.objectPath);
            else Select(issue);
        }

        // In a check of several scenes a row of the whole scene names its scene.
        string RowLocation(AuditIssue issue)
        {
            var location = issue.Location;
            if (result == null || result.parts == null || string.IsNullOrEmpty(issue.scenePath)) return location;
            if (!string.IsNullOrEmpty(issue.objectPath) || !string.IsNullOrEmpty(issue.assetPath)) return location;
            var scene = System.IO.Path.GetFileNameWithoutExtension(issue.scenePath);
            return location.Length > 0 ? scene + ": " + location : scene;
        }

        // Destroyed since the run, not a row without an object.
        static bool Gone(UnityEngine.Object o)
        {
            return !ReferenceEquals(o, null) && o == null;
        }

        static void SelectByPath(string scenePath, string objectPath)
        {
            if (string.IsNullOrEmpty(objectPath) || !AuditScope.IsLoaded(scenePath)) return;
            var names = objectPath.Split('/');
            foreach (var root in SceneManager.GetSceneByPath(scenePath).GetRootGameObjects())
            {
                if (root.name != names[0]) continue;
                var t = root.transform;
                for (int i = 1; i < names.Length && t != null; i++) t = t.Find(names[i]);
                if (t == null) continue;
                Selection.activeGameObject = t.gameObject;
                EditorGUIUtility.PingObject(t.gameObject);
                return;
            }
        }

        // History points of a check of several scenes: each scene its own; after fixes only the open scenes are new.
        void RecordParts(AuditResult r, bool openOnly)
        {
            prevPoint = null;
            if (r.parts == null) return;
            foreach (var part in r.parts)
            {
                if (openOnly && !part.open) continue;
                try
                {
                    AuditHistory.Record(part, "");
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[SceneAudit] Could not save the history point: " + e.Message);
                }
            }
            history = null;
            historyLabels.Clear();
            histA = histB = -1;
        }
    }
}
