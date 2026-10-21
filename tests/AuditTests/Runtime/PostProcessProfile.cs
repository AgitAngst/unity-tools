using System.Collections.Generic;

// Test stub: a profile is a list of effect settings, as in Post Processing v2.
namespace UnityEngine.Rendering.PostProcessing
{
    public class PostProcessProfile : ScriptableObject
    {
        public List<PostProcessEffectSettings> settings = new List<PostProcessEffectSettings>();
    }
}
