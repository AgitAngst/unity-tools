using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityTools.SceneOptimization
{
    // What a quick re-check of a row takes; the first three can be re-checked, the rest only by a full run.
    public enum RecheckKind { Object, Asset, Setting, Scene, Closed, Folder }

    public sealed class RecheckSummary
    {
        // Objects, assets and settings re-checked; whether all of them were scene objects (the word of the notification).
        public int items;
        public bool onlyObjects;
        // Rows of the result that went, rows found again, rows that are new.
        public int went;
        public int left;
        public int added;
        public double seconds;
    }

    // Quick re-check of rows (2.7.0, owner, 27.10.2026: "a quick re-check of a single element, to update what I changed").
    // The scenes are collected as in a full run, so the use of every asset is that of the whole scenes; the checks then see
    // only the re-checked objects with their children and the objects that use the re-checked assets. Rows of those objects,
    // assets and settings are replaced; rows of checks over the whole scene are left as the full run found them.
    public static class Recheck
    {
        // Checks whose rows depend on other objects of the scene: counts, duplicates, budgets, coverage by other objects,
        // project settings reported only when the scene has something (Mixed lights, skinned meshes, particles).
        public static readonly HashSet<string> SceneChecks = new HashSet<string>
        {
            "DC_BUDGET", "FRAME_NO_VIEWPOINTS", "DC_SHADOW_CASTERS", "DC_INSTANCING_UNKNOWN", "DC_INSTANCING_OFF",
            "MESH_SCENE_TRIS", "MESH_DUPLICATE",
            "TEX_MEMORY", "TEX_DUPLICATE", "TEX_NOT_TRILINEAR",
            "MAT_DUPLICATE", "MAT_SIMPLE_STANDARD",
            "LIGHT_SHADOWED", "LIGHT_REALTIME_GI", "LIGHT_LIGHTMAP_MEMORY", "LIGHT_NO_PROBES", "LIGHT_OUTSIDE_PROBES",
            "LIGHT_WIDE_REALTIME", "LIGHT_IMPORTANT", "LIGHT_NOT_BAKED", "LIGHT_DISTANCE_SHADOWMASK",
            "PART_TOTAL", "PART_MAX_PARTICLES", "PART_LARGE_TRANSPARENT", "PART_SOFT_PARTICLES",
            "SHADER_ALPHA_TEST_MSAA",
            "LIGHT_LIGHTMAP_UNCOMPRESSED", "LIGHT_LIGHTMAP_DIRECTIONAL", "LIGHT_SHADOW_CASCADES",
            "MESH_QUALITY_SKIN_WEIGHTS", "MESH_GPU_SKINNING_OFF",
            "ANIM_COUNT",
            "AUDIO_SOURCE_COUNT", "AUDIO_PRIORITY_ZERO", "AUDIO_LISTENERS_MANY", "AUDIO_NO_LISTENER",
            "REFL_MEMORY", "REFL_OUTSIDE", "REFL_DUPLICATE",
            "VR_NO_CAMERA", "VR_NO_OCCLUSION", "VR_STEAMVR_MISSING", "VR_STEAMVR_NOT_IN_SCENE"
        };

        public static RecheckKind KindOf(AuditIssue issue, AuditResult result)
        {
            if (result != null && result.scope == ScopeKind.Folder) return RecheckKind.Folder;
            if (SceneChecks.Contains(issue.checkId)) return RecheckKind.Scene;
            if (!string.IsNullOrEmpty(issue.scenePath) && !AuditScope.IsLoaded(issue.scenePath)) return RecheckKind.Closed;
            bool hasTarget = !ReferenceEquals(issue.target, null);
            if (hasTarget && issue.objectPath.Length > 0) return RecheckKind.Object;
            if (hasTarget && issue.assetPath.Length > 0) return RecheckKind.Asset;
            if (!hasTarget && issue.settingsPath.Length > 0) return RecheckKind.Setting;
            return RecheckKind.Scene;
        }

        // Null when the rows hold nothing to re-check; the result is changed in place.
        public static RecheckSummary Run(AuditResult result, IList<AuditIssue> rows, VRBudgets budgets, bool showProgress)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var objects = new HashSet<GameObject>();
            var assets = new HashSet<Object>();
            // Settings by their page: one condition may give one of two checks (MSAA off or 8x, fixed step slow or fast).
            var settings = new HashSet<string>();
            var gone = new List<AuditIssue>();
            foreach (var row in rows)
            {
                switch (KindOf(row, result))
                {
                    case RecheckKind.Object:
                        var go = GameObjectOf(row.target);
                        if (go != null) objects.Add(go);
                        else gone.Add(row);
                        break;
                    case RecheckKind.Asset:
                        if (row.target != null) assets.Add(row.target);
                        else gone.Add(row);
                        break;
                    case RecheckKind.Setting:
                        settings.Add(row.settingsPath);
                        break;
                }
            }
            var summary = new RecheckSummary
            {
                items = objects.Count + assets.Count + settings.Count + gone.Select(g => g.objectPath + "|" + g.assetPath).Distinct().Count(),
                onlyObjects = assets.Count == 0 && settings.Count == 0 && gone.All(g => g.objectPath.Length > 0)
            };
            if (summary.items == 0) return summary;

            var title = Strings.Get("ui.title");
            try
            {
                if (showProgress) EditorUtility.DisplayProgressBar(title, Strings.Format("ui.rc.progress", Strings.Get("ui.progress.collect")), 0f);
                var ctx = new AuditContext(budgets ?? result.budgets);
                ctx.Collect();

                // What is replaced: the objects with their children, their assets and the assets asked for.
                var subtree = new HashSet<GameObject>();
                foreach (var go in objects)
                    foreach (var t in go.GetComponentsInChildren<Transform>(true)) subtree.Add(t.gameObject);
                var replaced = new HashSet<Object>(assets);
                if (objects.Count > 0)
                    foreach (var dependency in EditorUtility.CollectDependencies(objects.ToArray()))
                        // A shader is judged by every material that uses it: only a shader row asked for re-checks it.
                        if (dependency != null && EditorUtility.IsPersistent(dependency) && !(dependency is MonoScript) && !(dependency is Shader))
                            replaced.Add(dependency);
                // Built-in assets share one path: only project and package assets are replaced by path.
                var paths = new HashSet<string>(replaced.Select(AssetDatabase.GetAssetPath).Where(p => AuditContext.IsProjectAssetPath(p) || p != null && p.StartsWith("Packages/", StringComparison.Ordinal)));

                // The checks see the objects and every object that uses their assets, so that an asset is judged by all
                // of its users as in a full run.
                var focus = new HashSet<GameObject>(subtree);
                focus.UnionWith(UsersOf(ctx, replaced, paths));
                ctx.Focus(focus, replaced);

                var steps = SceneAuditor.Steps();
                for (int i = 0; i < steps.Length; i++)
                {
                    if (showProgress)
                        EditorUtility.DisplayProgressBar(title, Strings.Format("ui.rc.progress", Strings.CategoryName(steps[i].Key)), (i + 1f) / (steps.Length + 1f));
                    steps[i].Value(ctx);
                }

                Func<AuditIssue, bool> belongs = i => !SceneChecks.Contains(i.checkId) && Belongs(i, subtree, paths, settings);
                var fresh = ctx.issues.Where(belongs).OrderBy(i => (int)i.category).ThenBy(i => (int)i.severity).ToList();
                if (ctx.scenes.Count == 1) AcceptedList.KeepScene(fresh, ctx.scenes[0].path);
                AcceptedList.Load().Apply(fresh);

                Func<AuditIssue, bool> replace = i => belongs(i) || gone.Any(g => SameGone(i, g));
                var old = result.issues.Where(replace).ToList();
                var oldKeys = new Dictionary<string, AuditIssue>();
                foreach (var issue in old) oldKeys[Key(issue)] = issue;
                var now = DateTime.Now;
                var found = new HashSet<string>();
                foreach (var issue in fresh)
                {
                    issue.recheckedAt = now;
                    AuditIssue before;
                    var key = Key(issue);
                    if (oldKeys.TryGetValue(key, out before))
                    {
                        found.Add(key);
                        issue.inScenes = before.inScenes;
                        summary.left++;
                    }
                    else summary.added++;
                }
                summary.went = oldKeys.Keys.Count(k => !found.Contains(k));

                result.issues = Replace(result.issues, replace, fresh);
                if (result.allIssues != null) result.allIssues = Replace(result.allIssues, replace, fresh);
                result.rechecked = summary.items;
                result.recheckedObjects = summary.onlyObjects;
                result.recheckTime = now;
                summary.seconds = watch.Elapsed.TotalSeconds;
                Debug.Log("[SceneAudit] Re-check " + ToolVersion.Full + ": " + summary.items + " item(s), " + summary.went + " row(s) went, " +
                          summary.left + " left, " + summary.added + " new, " + summary.seconds.ToString("F1") + " s.");
                return summary;
            }
            finally
            {
                if (showProgress) EditorUtility.ClearProgressBar();
            }
        }

        // A row found again takes the place of the old one, so the list does not jump; new rows go to their group.
        static List<AuditIssue> Replace(List<AuditIssue> issues, Func<AuditIssue, bool> replace, List<AuditIssue> fresh)
        {
            var byKey = new Dictionary<string, Queue<AuditIssue>>();
            foreach (var issue in fresh)
            {
                Queue<AuditIssue> same;
                if (!byKey.TryGetValue(Key(issue), out same)) byKey[Key(issue)] = same = new Queue<AuditIssue>();
                same.Enqueue(issue);
            }
            var list = new List<AuditIssue>();
            foreach (var issue in issues)
            {
                if (!replace(issue))
                {
                    list.Add(issue);
                    continue;
                }
                Queue<AuditIssue> same;
                if (byKey.TryGetValue(Key(issue), out same) && same.Count > 0) list.Add(same.Dequeue());
            }
            foreach (var same in byKey.Values) list.AddRange(same);
            return list.OrderBy(i => (int)i.category).ThenBy(i => (int)i.severity).ToList();
        }

        // A row of a re-checked object (or its child), asset or setting.
        static bool Belongs(AuditIssue issue, HashSet<GameObject> subtree, HashSet<string> paths, HashSet<string> settings)
        {
            if (ReferenceEquals(issue.target, null))
                return issue.settingsPath.Length > 0 && settings.Contains(issue.settingsPath);
            if (issue.objectPath.Length > 0)
            {
                var go = GameObjectOf(issue.target);
                return go != null && subtree.Contains(go);
            }
            return issue.assetPath.Length > 0 && paths.Contains(issue.assetPath);
        }

        // A row of an object or asset deleted since the run: its rows and those of its children go.
        static bool SameGone(AuditIssue issue, AuditIssue gone)
        {
            if (ReferenceEquals(issue.target, null) || issue.target != null) return false;
            if (gone.objectPath.Length > 0)
                return issue.scenePath == gone.scenePath && (issue.objectPath == gone.objectPath || issue.objectPath.StartsWith(gone.objectPath + "/", StringComparison.Ordinal));
            return issue.assetPath.Length > 0 && issue.assetPath == gone.assetPath;
        }

        static string Key(AuditIssue issue)
        {
            return issue.checkId + "|" + issue.scenePath + "|" + issue.objectPath + "|" + issue.assetPath + "|" + issue.settingsPath;
        }

        // Objects of the loaded scenes that use these assets: renderers by mesh, material, texture or shader, mesh
        // colliders, particle meshes and audio sources.
        static HashSet<GameObject> UsersOf(AuditContext ctx, HashSet<Object> assets, HashSet<string> paths)
        {
            var users = new HashSet<GameObject>();
            if (assets.Count == 0) return users;
            // Shaders count only when asked for (a shader row): the Standard shader of an object would bring in the scene.
            foreach (var r in ctx.All<Renderer>())
                if (Uses(ctx, r, assets, paths)) users.Add(r.gameObject);
            foreach (var c in ctx.All<MeshCollider>())
                if (c.sharedMesh != null && assets.Contains(c.sharedMesh)) users.Add(c.gameObject);
            foreach (var p in ctx.All<ParticleSystemRenderer>())
                if (p.mesh != null && assets.Contains(p.mesh)) users.Add(p.gameObject);
            foreach (var s in ctx.All<AudioSource>())
                if (s.clip != null && assets.Contains(s.clip)) users.Add(s.gameObject);
            return users;
        }

        // A mesh of a re-checked model file counts by its path: rows of every mesh of the file are replaced.
        static bool Uses(AuditContext ctx, Renderer r, HashSet<Object> assets, HashSet<string> paths)
        {
            var mesh = AuditContext.MeshOf(r);
            if (mesh != null && (assets.Contains(mesh) || paths.Contains(AssetDatabase.GetAssetPath(mesh)))) return true;
            foreach (var material in r.sharedMaterials)
            {
                if (material == null) continue;
                if (assets.Contains(material) || material.shader != null && assets.Contains(material.shader)) return true;
                foreach (var pair in ctx.TexturesOf(material))
                    if (assets.Contains(pair.Key)) return true;
            }
            return false;
        }

        static GameObject GameObjectOf(Object target)
        {
            var go = target as GameObject;
            if (go != null) return EditorUtility.IsPersistent(go) ? null : go;
            var component = target as Component;
            return component != null && !EditorUtility.IsPersistent(component) ? component.gameObject : null;
        }
    }
}
