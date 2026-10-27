using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityTools.SceneOptimization
{
    // Applies fixes after the user confirms them: scene changes in one Undo group, asset changes in one import batch.
    public static class AutoFixer
    {
        public const string UndoName = "Scene Audit Fix";

        public static void Confirm(IList<AuditIssue> issues, Action onApplied)
        {
            var fixable = new List<AuditIssue>();
            foreach (var issue in issues) if (issue.AutoFixable) fixable.Add(issue);
            if (fixable.Count == 0)
            {
                EditorUtility.DisplayDialog(Strings.Get("ui.fix.title"), Strings.Get("ui.fix.none"), Strings.Get("ui.fix.ok"));
                return;
            }
            FixConfirmWindow.Show(fixable, selected =>
            {
                Apply(selected);
                if (onApplied != null) onApplied();
            });
        }

        public static int Apply(IList<AuditIssue> issues)
        {
            var sceneFixes = new List<AuditIssue>();
            var assetFixes = new List<AuditIssue>();
            var prefabFixes = new List<AuditIssue>();
            foreach (var issue in issues)
            {
                if (!issue.AutoFixable) continue;
                if (issue.inPrefab) prefabFixes.Add(issue);
                else if (issue.fixTouchesAsset) assetFixes.Add(issue);
                else sceneFixes.Add(issue);
            }
            return ApplySceneFixes(sceneFixes) + ApplyPrefabFixes(prefabFixes) + ApplyAssetFixes(assetFixes);
        }

        // Fixes in prefabs: outside the asset batch, so the copies see the changed prefab at once; copies fixed on
        // themselves go into one Undo group.
        static int ApplyPrefabFixes(List<AuditIssue> fixes)
        {
            if (fixes.Count == 0) return 0;
            int applied = 0;
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            foreach (var issue in fixes)
            {
                if (!AuditContext.IsProjectAssetPath(issue.assetPath))
                {
                    Debug.LogWarning("[SceneAudit] Skipped " + issue.checkId + ": read-only prefab " + issue.assetPath);
                    continue;
                }
                try
                {
                    issue.fixAction();
                    issue.applied = true;
                    applied++;
                }
                catch (Exception e)
                {
                    Debug.LogError("[SceneAudit] Fix " + issue.checkId + " failed in prefab " + issue.assetPath + ": " + e.Message);
                }
            }
            Undo.CollapseUndoOperations(group);
            return applied;
        }

        static int ApplySceneFixes(List<AuditIssue> fixes)
        {
            if (fixes.Count == 0) return 0;
            int applied = 0;
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            foreach (var issue in fixes)
            {
                try
                {
                    if (issue.fixTarget != null) Undo.RecordObject(issue.fixTarget, UndoName);
                    issue.fixAction();
                    if (issue.fixTarget != null && PrefabUtility.IsPartOfPrefabInstance(issue.fixTarget))
                        PrefabUtility.RecordPrefabInstancePropertyModifications(issue.fixTarget);
                    var go = GameObjectOf(issue.fixTarget);
                    if (go != null) EditorSceneManager.MarkSceneDirty(go.scene);
                    Debug.Log("[SceneAudit] Fixed " + issue.checkId + ": " + issue.Location);
                    issue.applied = true;
                    applied++;
                }
                catch (Exception e)
                {
                    Debug.LogError("[SceneAudit] Fix " + issue.checkId + " failed for " + issue.Location + ": " + e.Message);
                }
            }
            Undo.CollapseUndoOperations(group);
            return applied;
        }

        static int ApplyAssetFixes(List<AuditIssue> fixes)
        {
            if (fixes.Count == 0) return 0;
            int applied = 0;
            bool save = false;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var issue in fixes)
                {
                    var path = AssetPathOf(issue);
                    if (!AuditContext.IsProjectAssetPath(path))
                    {
                        Debug.LogWarning("[SceneAudit] Skipped " + issue.checkId + ": read-only asset " + path);
                        continue;
                    }
                    try
                    {
                        issue.fixAction();
                        // Materials and terrain data are saved here; importers reimport themselves.
                        if (issue.fixTarget is Material || issue.fixTarget is TerrainData)
                        {
                            EditorUtility.SetDirty(issue.fixTarget);
                            save = true;
                        }
                        Debug.Log("[SceneAudit] Fixed " + issue.checkId + ": " + path);
                        issue.applied = true;
                        applied++;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("[SceneAudit] Fix " + issue.checkId + " failed for " + path + ": " + e.Message);
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            if (save) AssetDatabase.SaveAssets();
            return applied;
        }

        public static bool BecomesOverride(AuditIssue issue)
        {
            return !issue.fixTouchesAsset && issue.fixTarget != null && PrefabUtility.IsPartOfPrefabInstance(issue.fixTarget);
        }

        static string AssetPathOf(AuditIssue issue)
        {
            var importer = issue.fixTarget as AssetImporter;
            if (importer != null) return importer.assetPath;
            if (issue.fixTarget != null) return AssetDatabase.GetAssetPath(issue.fixTarget);
            return issue.assetPath;
        }

        static GameObject GameObjectOf(Object target)
        {
            var component = target as Component;
            return component != null ? component.gameObject : target as GameObject;
        }
    }
}
