using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace UnityTools.SceneOptimization
{
    // Loaded: the open scenes; Scenes: scenes named on the command line, each on its own.
    public enum ScopeKind { Loaded, Selection, BuildScenes, Scenes, Folder }

    // One scene of a check of several scenes.
    public sealed class SceneSummary
    {
        public string path = "";
        public string name = "";
        public int critical;
        public int warning;
        public int info;
        // Budgets of the scene; -1 when it was checked together with other open scenes.
        public long tris = -1;
        public long drawCalls = -1;
        public double textureMB = -1;

        public bool Loaded { get { return AuditScope.IsLoaded(path); } }
    }

    // Assets of a checked folder by kind.
    public sealed class FolderCounts
    {
        public int prefabs;
        public int models;
        public int materials;
        public int textures;
        public int clips;
    }

    // What a run checks: the open scenes, the selected objects, scenes one by one, or the assets of a folder.
    public static class AuditScope
    {
        // Editor preferences are shared by all projects: the scope is kept per project.
        static string KindPref { get { return "UnityTools.SceneAudit.Scope." + PlayerSettings.productGUID; } }
        static string FolderPref { get { return "UnityTools.SceneAudit.ScopeFolder." + PlayerSettings.productGUID; } }

        // The scope of the window; Scenes is for the command line only.
        public static ScopeKind Kind
        {
            get
            {
                var kind = (ScopeKind)EditorPrefs.GetInt(KindPref, (int)ScopeKind.Loaded);
                return kind == ScopeKind.Scenes || !Enum.IsDefined(typeof(ScopeKind), kind) ? ScopeKind.Loaded : kind;
            }
            set { EditorPrefs.SetInt(KindPref, (int)value); }
        }

        public static string Folder
        {
            get { return EditorPrefs.GetString(FolderPref, ""); }
            set { EditorPrefs.SetString(FolderPref, value ?? ""); }
        }

        // Scene objects selected in the Hierarchy.
        public static GameObject[] SelectedObjects()
        {
            return Selection.gameObjects.Where(g => g != null && !EditorUtility.IsPersistent(g) && g.scene.IsValid()).ToArray();
        }

        // Enabled scenes of the build settings whose files exist.
        public static List<string> BuildScenePaths()
        {
            return EditorBuildSettings.scenes.Where(s => s.enabled && !string.IsNullOrEmpty(s.path) && File.Exists(s.path)).Select(s => s.path).ToList();
        }

        public static bool IsLoaded(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath)) return false;
            var scene = SceneManager.GetSceneByPath(scenePath);
            return scene.IsValid() && scene.isLoaded;
        }

        // "Assets/…" for a folder of the project picked on disk; "" when it is outside Assets.
        public static string ProjectFolder(string absolute)
        {
            if (string.IsNullOrEmpty(absolute)) return "";
            var data = Path.GetFullPath(Application.dataPath).Replace('\\', '/').TrimEnd('/');
            var full = Path.GetFullPath(absolute).Replace('\\', '/').TrimEnd('/');
            if (string.Equals(full, data, StringComparison.OrdinalIgnoreCase)) return "Assets";
            if (!full.StartsWith(data + "/", StringComparison.OrdinalIgnoreCase)) return "";
            return "Assets" + full.Substring(data.Length);
        }

        // The run of the window's scope; null when nothing was checked (cancelled, refused or nothing to check).
        public static AuditResult Run(VRBudgets budgets)
        {
            switch (Kind)
            {
                case ScopeKind.Selection:
                    var objects = SelectedObjects();
                    if (objects.Length == 0) return Refuse("ui.scope.noSelection");
                    return RunSelection(objects, budgets, true);
                case ScopeKind.BuildScenes:
                    var paths = BuildScenePaths();
                    if (paths.Count == 0) return Refuse("ui.scope.noBuildScenes");
                    if (EditorApplication.isPlayingOrWillChangePlaymode) return Refuse("ui.scope.playMode");
                    return RunScenes(paths, budgets, true, ScopeKind.BuildScenes, null);
                case ScopeKind.Folder:
                    if (!AssetDatabase.IsValidFolder(Folder)) return Refuse("ui.scope.noFolder");
                    return RunFolder(Folder, budgets, true);
                default:
                    return SceneAuditor.RunFullAudit(budgets, true);
            }
        }

        // The same check again after fixes: the scope of the result, not of the button — the same selected objects, the
        // same folder; scenes one by one check only the open scenes again.
        public static AuditResult Rerun(AuditResult current, VRBudgets budgets)
        {
            switch (current.scope)
            {
                case ScopeKind.Selection:
                    var objects = (current.selection ?? new GameObject[0]).Where(g => g != null).ToArray();
                    return objects.Length > 0 ? RunSelection(objects, budgets, true) : SceneAuditor.RunFullAudit(budgets, true);
                case ScopeKind.Folder:
                    return AssetDatabase.IsValidFolder(current.folder) ? RunFolder(current.folder, budgets, true) : null;
                case ScopeKind.BuildScenes:
                case ScopeKind.Scenes:
                    return current.parts != null ? Refresh(current, budgets) : SceneAuditor.RunFullAudit(budgets, true);
                default:
                    return SceneAuditor.RunFullAudit(budgets, true);
            }
        }

        static AuditResult Refuse(string key)
        {
            EditorUtility.DisplayDialog(Strings.Get("ui.title"), Strings.Get(key), Strings.Get("ui.fix.ok"));
            return null;
        }

        // ---- selection ----

        // The open scenes are checked as usual; the list keeps the issues of the objects (with their children) and of the
        // assets they use. Project settings and whole-scene issues have no object and are left out.
        public static AuditResult RunSelection(GameObject[] objects, VRBudgets budgets, bool showProgress)
        {
            var r = SceneAuditor.RunFullAudit(budgets, showProgress);
            if (r == null) return null;
            var inSet = new HashSet<GameObject>();
            foreach (var go in objects)
                foreach (var t in go.GetComponentsInChildren<Transform>(true)) inSet.Add(t.gameObject);
            var used = new HashSet<string>();
            foreach (var dependency in EditorUtility.CollectDependencies(objects))
            {
                var path = AssetDatabase.GetAssetPath(dependency);
                if (!string.IsNullOrEmpty(path)) used.Add(path);
            }
            r.allIssues = r.issues;
            r.issues = r.issues.Where(i => InSelection(i, inSet, used)).ToList();
            r.scope = ScopeKind.Selection;
            r.selection = objects;
            r.selected = objects.Length;
            r.selectedWithChildren = inSet.Count;
            return r;
        }

        static bool InSelection(AuditIssue issue, HashSet<GameObject> objects, HashSet<string> assets)
        {
            var go = GameObjectOf(issue.target);
            if (go != null && !EditorUtility.IsPersistent(go)) return objects.Contains(go);
            return !string.IsNullOrEmpty(issue.assetPath) && assets.Contains(issue.assetPath);
        }

        // ---- scenes one by one ----

        // Every scene on its own, then one result. interactive — the editor of the user: modified scenes are saved first,
        // the open scenes come back afterwards, and the open ones among the paths are checked last as they are, so that
        // their rows can be fixed. afterEach gets every part (history, viewpoints). Null when nothing was checked.
        public static AuditResult RunScenes(IList<string> paths, VRBudgets budgets, bool interactive, ScopeKind kind, Action<AuditResult> afterEach)
        {
            // A scene named twice is checked once.
            paths = paths.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var parts = new List<AuditResult>();
            if (!interactive)
            {
                foreach (var path in paths)
                {
                    EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    var part = SceneAuditor.RunFullAudit(budgets, false);
                    if (part == null) continue;
                    Tag(part);
                    Close(part);
                    if (afterEach != null) afterEach(part);
                    parts.Add(part);
                }
                return parts.Count > 0 ? InOrder(Merge(parts, kind), paths) : null;
            }

            var setup = EditorSceneManager.GetSceneManagerSetup();
            var open = paths.Where(p => setup.Any(s => s.isLoaded && SamePath(s.path, p))).ToList();
            var closed = paths.Where(p => !open.Contains(p)).ToList();
            // Saving and coming back are needed only when other scenes are opened.
            if (closed.Count > 0)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return null;
                setup = EditorSceneManager.GetSceneManagerSetup();
                if (setup.Any(s => string.IsNullOrEmpty(s.path))) return Refuse("ui.scope.untitled");
            }
            int total = closed.Count + (open.Count > 0 ? 1 : 0);
            bool cancelled = false;
            try
            {
                for (int i = 0; i < closed.Count; i++)
                {
                    var info = Strings.Format("ui.scope.progress", i + 1, total, Path.GetFileNameWithoutExtension(closed[i]));
                    if (EditorUtility.DisplayCancelableProgressBar(Strings.Get("ui.title"), info, (float)i / total))
                    {
                        cancelled = true;
                        break;
                    }
                    EditorSceneManager.OpenScene(closed[i], OpenSceneMode.Single);
                    var part = SceneAuditor.RunFullAudit(budgets, false);
                    if (part == null) continue;
                    Tag(part);
                    Close(part);
                    if (afterEach != null) afterEach(part);
                    parts.Add(part);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                if (closed.Count > 0) EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
            if (!cancelled && open.Count > 0)
            {
                var part = RunOpen(open, budgets);
                if (part != null)
                {
                    if (afterEach != null) afterEach(part);
                    parts.Add(part);
                }
            }
            if (parts.Count == 0) return null;
            var merged = InOrder(Merge(parts, kind), paths);
            merged.planned = paths.Count;
            merged.incomplete = merged.scenes.Count < paths.Count;
            return merged;
        }

        // The table of scenes in the order of the build (or of the command line).
        static AuditResult InOrder(AuditResult merged, IList<string> paths)
        {
            merged.scenes = merged.scenes.OrderBy(s => IndexOf(paths, s.path)).ToList();
            return merged;
        }

        static int IndexOf(IList<string> paths, string path)
        {
            for (int i = 0; i < paths.Count; i++)
                if (SamePath(paths[i], path)) return i;
            return paths.Count;
        }

        // A run whose scene is closed now: fixes of the scene that record no object (they would change nothing) are taken
        // away; the others go by their dead object (AuditIssue.FixTargetGone).
        static void Close(AuditResult part)
        {
            foreach (var issue in part.issues)
                if (SceneFixWithoutTarget(issue)) issue.SetFix(FixKind.None, null, false, null);
        }

        // A scene fix that records no object, such as giving the renderers of duplicate materials one of them.
        static bool SceneFixWithoutTarget(AuditIssue issue)
        {
            return issue.fixAction != null && !issue.fixTouchesAsset && !issue.inPrefab && ReferenceEquals(issue.fixTarget, null);
        }

        // The open scenes among the paths, checked together as they are.
        static AuditResult RunOpen(IList<string> paths, VRBudgets budgets)
        {
            var scenes = paths.Where(IsLoaded).Select(SceneManager.GetSceneByPath).ToList();
            if (scenes.Count == 0) return null;
            var part = SceneAuditor.RunFullAudit(budgets, true, scenes, null);
            if (part == null) return null;
            part.open = true;
            Tag(part);
            return part;
        }

        // After fixes in a check of several scenes: the scenes open now are checked again; rows of the other scenes whose
        // asset was fixed are dropped. Other rows of closed scenes stay until the next full run.
        public static AuditResult Refresh(AuditResult current, VRBudgets budgets)
        {
            // Only a fix of the asset itself is a fix for every scene.
            var applied = new HashSet<string>(current.issues.Where(i => i.applied && i.fixTouchesAsset && IsAssetIssue(i)).Select(AssetKey));
            var loaded = current.scenePaths.Where(IsLoaded).ToList();
            var parts = new List<AuditResult>();
            foreach (var part in current.parts)
            {
                if (part.scenePaths.Any(p => loaded.Contains(p))) continue;
                part.open = false;
                part.issues.RemoveAll(i => IsAssetIssue(i) && applied.Contains(AssetKey(i)));
                Close(part);
                parts.Add(part);
            }
            if (loaded.Count > 0)
            {
                var part = RunOpen(loaded, budgets);
                if (part != null) parts.Add(part);
            }
            if (parts.Count == 0) return current;
            var order = current.scenes.Select(s => s.path).ToList();
            var merged = InOrder(Merge(parts, current.scope), order);
            merged.planned = current.planned;
            merged.incomplete = current.incomplete;
            return merged;
        }

        // Counts of the table again, after rows were marked acceptable or released.
        public static void Summarize(AuditResult merged)
        {
            if (merged.parts == null) return;
            var accepted = AcceptedList.Load();
            var order = merged.scenes.Select(s => s.path).ToList();
            merged.scenes.Clear();
            foreach (var part in merged.parts)
            {
                accepted.Apply(part.issues);
                merged.scenes.AddRange(Summaries(part));
            }
            InOrder(merged, order);
        }

        static bool SamePath(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        // Issues of the whole scene (no object, no asset) and asset rows with a fix of the scene belong to the scene of a run
        // of one scene. Only the path is set: the location stays as in a run of the open scenes (history compares it).
        static void Tag(AuditResult part)
        {
            if (part.scenePaths.Length != 1) return;
            foreach (var issue in part.issues)
            {
                if (!string.IsNullOrEmpty(issue.scenePath) || IsProjectSetting(issue)) continue;
                if (!string.IsNullOrEmpty(issue.assetPath) && !SceneFixOnAsset(issue)) continue;
                issue.scenePath = part.scenePaths[0];
            }
        }

        // An asset row whose fix changes the scene (the renderers of duplicate materials): it belongs to its scene.
        static bool SceneFixOnAsset(AuditIssue issue)
        {
            return IsAssetIssue(issue) && issue.fixAction != null && !issue.fixTouchesAsset && !issue.inPrefab;
        }

        // ---- merging ----

        static bool IsProjectSetting(AuditIssue issue)
        {
            return issue.settingsPath.StartsWith("Project/", StringComparison.Ordinal);
        }

        // An asset problem: located by the asset path, not by a scene object.
        static bool IsAssetIssue(AuditIssue issue)
        {
            return !string.IsNullOrEmpty(issue.assetPath) && string.IsNullOrEmpty(issue.objectPath);
        }

        static string AssetKey(AuditIssue issue)
        {
            return issue.checkId + "|" + issue.messageKey + "|" + issue.assetPath + "|" + ArgsKey(issue);
        }

        static string ArgsKey(AuditIssue issue)
        {
            return string.Join("|", issue.args.Select(a => Convert.ToString(a, System.Globalization.CultureInfo.InvariantCulture)).ToArray());
        }

        // The worst frame of the scenes, each figure with its scene; null when no scene has viewpoints. Scenes without
        // viewpoints keep their own Info rows.
        static FrameEstimate WorstFrame(List<AuditResult> parts)
        {
            var withPoints = parts.Where(p => p.frame != null).ToList();
            if (withPoints.Count == 0) return null;
            var byCalls = withPoints.OrderByDescending(p => p.frame.drawCalls).First();
            var byTris = withPoints.OrderByDescending(p => p.frame.tris).First();
            return new FrameEstimate
            {
                points = withPoints.Sum(p => p.frame.points),
                drawCalls = byCalls.frame.drawCalls,
                drawCallsPoint = SceneLabel(byCalls) + byCalls.frame.drawCallsPoint,
                tris = byTris.frame.tris,
                trisPoint = SceneLabel(byTris) + byTris.frame.trisPoint,
                seconds = withPoints.Sum(p => p.frame.seconds)
            };
        }

        static string SceneLabel(AuditResult part)
        {
            return part.sceneNames.Length > 0 ? string.Join(", ", part.sceneNames) + ": " : "";
        }

        // One result of several runs: object and scene rows as they are; the same project setting once; the same asset
        // problem once with the users added up. Budgets — the worst scene.
        public static AuditResult Merge(List<AuditResult> parts, ScopeKind kind)
        {
            var last = parts[parts.Count - 1];
            var merged = new AuditResult
            {
                scope = kind,
                parts = parts,
                xr = last.xr,
                quality = last.quality,
                budgets = last.budgets,
                activeScenePath = last.activeScenePath,
                time = DateTime.Now,
                sceneTris = parts.Max(p => p.sceneTris),
                drawCallEstimate = parts.Max(p => p.drawCallEstimate),
                frame = WorstFrame(parts),
                textureMemoryMB = parts.Max(p => p.textureMemoryMB),
                sceneNames = parts.SelectMany(p => p.sceneNames).Distinct().ToArray(),
                scenePaths = parts.SelectMany(p => p.scenePaths).Distinct().ToArray()
            };
            var openPart = parts.FirstOrDefault(p => p.open);
            if (openPart != null)
            {
                merged.lighting = openPart.lighting;
                merged.heavy = openPart.heavy;
            }

            var issues = new List<AuditIssue>();
            var settings = new HashSet<string>();
            var assets = new Dictionary<string, AuditIssue>();
            // The open scenes first: their rows are alive and stand for the copies from closed scenes.
            foreach (var part in parts.OrderByDescending(p => p.open))
            {
                foreach (var issue in part.issues)
                {
                    if (IsProjectSetting(issue))
                    {
                        if (!settings.Add(issue.checkId + "|" + issue.settingsPath + "|" + ArgsKey(issue))) continue;
                        issues.Add(issue);
                    }
                    else if (IsAssetIssue(issue) && string.IsNullOrEmpty(issue.scenePath))
                    {
                        var key = AssetKey(issue);
                        AuditIssue row;
                        if (assets.TryGetValue(key, out row))
                        {
                            row.users += issue.users;
                            row.inScenes++;
                            continue;
                        }
                        // A copy: the users of the parts stay their own for the next merge.
                        row = issue.Copy();
                        row.inScenes = 1;
                        assets[key] = row;
                        issues.Add(row);
                    }
                    else issues.Add(issue);
                }
            }
            // Read/Write a scene needs (colliders, particles) wins over Read/Write another scene could do without.
            var needed = new HashSet<string>(issues.Where(i => i.checkId == "MESH_READ_WRITE_NEEDED").Select(i => i.assetPath));
            issues.RemoveAll(i => i.checkId == "MESH_READ_WRITE" && needed.Contains(i.assetPath));
            merged.issues = issues.OrderBy(i => (int)i.category).ThenBy(i => (int)i.severity).ToList();
            AcceptedList.Load().Apply(merged.issues);
            foreach (var part in parts) merged.scenes.AddRange(Summaries(part));
            return merged;
        }

        static IEnumerable<SceneSummary> Summaries(AuditResult part)
        {
            bool single = part.scenePaths.Length == 1;
            for (int i = 0; i < part.scenePaths.Length; i++)
            {
                var path = part.scenePaths[i];
                var summary = new SceneSummary { path = path, name = part.sceneNames[i] };
                foreach (var issue in part.issues)
                {
                    if (issue.accepted || IsProjectSetting(issue)) continue;
                    if (!single && issue.scenePath != path) continue;
                    if (issue.severity == Severity.Critical) summary.critical++;
                    else if (issue.severity == Severity.Warning) summary.warning++;
                    else summary.info++;
                }
                if (single)
                {
                    summary.tris = part.sceneTris;
                    summary.drawCalls = part.drawCallEstimate;
                    summary.textureMB = part.textureMemoryMB;
                }
                yield return summary;
            }
        }

        // ---- folder ----

        // The assets of a folder (with subfolders) without a scene: prefabs and models in a hidden scene, loose materials on
        // a quad, loose textures and clips as assets. The list keeps the issues of the folder's assets and of the objects
        // inside its prefabs and models; project settings and scene totals are left out.
        public static AuditResult RunFolder(string folder, VRBudgets budgets, bool showProgress)
        {
            folder = folder.Replace('\\', '/').TrimEnd('/');
            if (!AssetDatabase.IsValidFolder(folder)) return null;
            var counts = new FolderCounts();
            var extra = new List<Object>();
            var materials = new List<Material>();
            var used = new HashSet<Material>();
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                foreach (var path in AssetDatabase.FindAssets("", new[] { folder }).Select(AssetDatabase.GUIDToAssetPath).Distinct())
                {
                    if (AssetDatabase.IsValidFolder(path)) continue;
                    var type = AssetDatabase.GetMainAssetTypeAtPath(path);
                    if (type == null) continue;
                    if (type == typeof(GameObject))
                    {
                        var asset = AssetDatabase.LoadMainAssetAtPath(path) as GameObject;
                        if (asset == null) continue;
                        if (PrefabUtility.GetPrefabAssetType(asset) == PrefabAssetType.Model) counts.models++;
                        else counts.prefabs++;
                        var instance = PrefabUtility.InstantiatePrefab(asset, preview) as GameObject;
                        if (instance == null) continue;
                        foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
                            foreach (var m in r.sharedMaterials)
                                if (m != null) used.Add(m);
                    }
                    else if (type == typeof(Material))
                    {
                        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                        if (material == null) continue;
                        counts.materials++;
                        materials.Add(material);
                    }
                    else if (typeof(Texture).IsAssignableFrom(type))
                    {
                        var texture = AssetDatabase.LoadAssetAtPath<Texture>(path);
                        if (texture == null) continue;
                        counts.textures++;
                        extra.Add(texture);
                    }
                    else if (type == typeof(AudioClip))
                    {
                        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                        if (clip == null) continue;
                        counts.clips++;
                        extra.Add(clip);
                    }
                }
                foreach (var material in materials.Where(m => !used.Contains(m)))
                {
                    var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    Object.DestroyImmediate(quad.GetComponent<Collider>());
                    quad.name = material.name;
                    quad.GetComponent<MeshRenderer>().sharedMaterial = material;
                    SceneManager.MoveGameObjectToScene(quad, preview);
                }
                var result = SceneAuditor.RunFullAudit(budgets, showProgress, new[] { preview }, extra);
                if (result == null) return null;
                result.issues = ToFolder(result.issues, folder);
                AcceptedList.Load().Apply(result.issues);
                result.scope = ScopeKind.Folder;
                result.folder = folder;
                result.folderCounts = counts;
                result.lighting = null;
                result.heavy = null;
                // The hidden scene is no scene of the project: no budgets.
                result.sceneTris = 0;
                result.drawCallEstimate = 0;
                result.textureMemoryMB = 0;
                result.sceneNames = new string[0];
                result.scenePaths = new string[0];
                return result;
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        static bool InFolder(string path, string folder)
        {
            return !string.IsNullOrEmpty(path) && path.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase);
        }

        // Issues of the hidden scene as rows of the folder: assets of the folder as they are, objects as the objects in
        // their prefab or model ("<file> › <path>", no fix), the rest left out.
        static List<AuditIssue> ToFolder(List<AuditIssue> issues, string folder)
        {
            var kept = new List<AuditIssue>();
            foreach (var issue in issues)
            {
                if (issue.target == null) continue;
                if (EditorUtility.IsPersistent(issue.target))
                {
                    if (!InFolder(issue.assetPath, folder)) continue;
                    // A fix of the hidden scene's objects would change nothing.
                    if (SceneFixWithoutTarget(issue) || (!issue.fixTouchesAsset && !issue.inPrefab && issue.fixTarget != null && !EditorUtility.IsPersistent(issue.fixTarget)))
                        issue.SetFix(FixKind.None, null, false, null);
                    kept.Add(issue);
                    continue;
                }
                var source = PrefabUtility.GetCorrespondingObjectFromSource(issue.target);
                var path = source != null ? AssetDatabase.GetAssetPath(source) : "";
                if (!InFolder(path, folder)) continue;
                var inside = PathInAsset(GameObjectOf(source));
                issue.target = source;
                issue.assetPath = path;
                issue.objectPath = Path.GetFileName(path) + (inside.Length > 0 ? " › " + inside : "");
                issue.scene = "";
                issue.scenePath = "";
                issue.acceptKey = "";
                issue.SetFix(FixKind.None, null, false, null);
                kept.Add(issue);
            }
            return kept;
        }

        // Path of an object inside its asset, without the asset's root.
        static string PathInAsset(GameObject go)
        {
            if (go == null) return "";
            var names = new List<string>();
            for (var t = go.transform; t.parent != null; t = t.parent) names.Add(t.name);
            names.Reverse();
            return string.Join("/", names.ToArray());
        }

        static GameObject GameObjectOf(Object target)
        {
            var component = target as Component;
            return component != null ? component.gameObject : target as GameObject;
        }
    }
}
