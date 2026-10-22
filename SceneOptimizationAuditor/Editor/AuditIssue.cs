using System;
using Object = UnityEngine.Object;

namespace UnityTools.SceneOptimization
{
    public enum Severity { Critical = 0, Warning = 1, Info = 2 }

    public enum Category { DrawCalls, Meshes, Textures, Materials, Lighting, Physics, Animators, Audio, VRSpecific, General }

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
        public string objectPath = "";
        public string assetPath = "";
        // Project Settings page path, e.g. "Project/Player".
        public string settingsPath = "";
        // Number of scene objects that use the asset (0 when not counted).
        public int users;

        // Texts are stored as keys and arguments and built in the current language when drawn.
        public string messageKey;
        public object[] args;

        public FixKind fixKind;
        public Action fixAction;
        // Object recorded for Undo (scene fixes) or marked dirty (asset fixes).
        public Object fixTarget;
        // True when the fix edits an asset or importer: project-wide and not covered by Undo.
        public bool fixTouchesAsset;

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
                return Strings.Has(key) ? Strings.Format(key, args) : Message;
            }
        }

        public bool AutoFixable { get { return fixKind != FixKind.None && fixAction != null; } }

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

        public void SetFix(FixKind kind, Object recordTarget, bool touchesAsset, Action action)
        {
            fixKind = kind;
            fixTarget = recordTarget;
            fixTouchesAsset = touchesAsset;
            fixAction = action;
        }
    }
}
