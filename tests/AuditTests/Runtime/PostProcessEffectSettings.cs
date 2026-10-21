using System;

// Test stub: "active" and "enabled.value" are the members the auditor reads.
namespace UnityEngine.Rendering.PostProcessing
{
    [Serializable]
    public class BoolParameter
    {
        public bool value;
    }

    public class PostProcessEffectSettings : ScriptableObject
    {
        public bool active = true;
        public BoolParameter enabled = new BoolParameter();
    }
}
