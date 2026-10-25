using System;
using Object = UnityEngine.Object;

namespace UnityTools.SceneOptimization
{
    public enum Severity { Critical = 0, Warning = 1, Info = 2 }

    public enum Category { DrawCalls, Meshes, Textures, Materials, Shaders, Lighting, Particles, Physics, Animators, Audio, VRSpecific, General }

    // Safe fixes run in a batch, Review fixes one row at a time after the user looks at them.
    public enum FixKind { None, Safe, Review }

    public sealed class AuditIssue
    {
        public string checkId;
        public Category category;
        public Severity severity;

        // GameObject, component or asset the issue points to; null for project settings.
        public Object target;
        public string scene = "";
        // Path of the scene file of the object, or of the scene a scene-wide issue was found in ("" when not known).
        public string scenePath = "";
        public string objectPath = "";
        public string assetPath = "";
        // Project Settings page path, e.g. "Project/Player".
        public string settingsPath = "";
        // Number of scene objects that use the asset (0 when not counted).
        public int users;

        // Texts are stored as keys and arguments and built in the current language when drawn.
        public string messageKey;
        public object[] args;

        // Marked as acceptable by the team or its check is turned off (AcceptedList); not counted, hidden by default.
        public bool accepted;
        // Key of the target in the accepted list, kept from the run: the object may be gone since (a closed scene).
        public string acceptKey = "";
        // Scenes an asset problem was found in when several scenes were checked one by one.
        public int inScenes;
        // Set when the fix was applied; a check of several scenes drops such rows of the closed scenes.
        public bool applied;

        public FixKind fixKind;
        public Action fixAction;
        // Object recorded for Undo (scene fixes) or marked dirty (asset fixes).
        public Object fixTarget;
        // True when the fix edits an asset or importer: project-wide and not covered by Undo.
        public bool fixTouchesAsset;
        // A fix of copies made once in their prefab (PrefabFixes); fixScope names the prefab and the copies.
        public bool inPrefab;
        public string fixScope = "";

        // The object in the nearest prefab or model asset the scene object is a copy of (PrefabFixes.SourceOf).
        public Object source;
        public bool sourceKnown;

        public string Message { get { return Strings.Format(messageKey, args); } }

        public string Suggestion
        {
            get
            {
                var key = messageKey + ".fix";
                return Strings.Has(key) ? Strings.Format(key, args) : "";
            }
        }

        public string FixDescription
        {
            get
            {
                var key = messageKey + ".do";
                var text = Strings.Has(key) ? Strings.Format(key, args) : Message;
                return fixScope.Length > 0 ? text + " — " + fixScope : text;
            }
        }

        // The object to fix was destroyed since the run: its scene was closed or the object deleted.
        public bool FixTargetGone { get { return !ReferenceEquals(fixTarget, null) && fixTarget == null; } }

        public bool AutoFixable { get { return fixKind != FixKind.None && fixAction != null && !FixTargetGone; } }

        public string Location
        {
            get
            {
                if (!string.IsNullOrEmpty(objectPath))
                    return string.IsNullOrEmpty(scene) ? objectPath : scene + ": " + objectPath;
                if (!string.IsNullOrEmpty(assetPath)) return assetPath;
                return settingsPath;
            }
        }

        // A row of a merged result; the run's own issue stays as it was.
        public AuditIssue Copy()
        {
            return (AuditIssue)MemberwiseClone();
        }

        public void SetFix(FixKind kind, Object recordTarget, bool touchesAsset, Action action)
        {
            fixKind = kind;
            fixTarget = recordTarget;
            fixTouchesAsset = touchesAsset;
            fixAction = action;
        }
    }
}
