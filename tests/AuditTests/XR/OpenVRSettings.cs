using UnityEngine;

// Test stub with the class and field names of the OpenVR XR Plugin settings the auditor reads.
namespace Unity.XR.OpenVR
{
    public class OpenVRSettings : ScriptableObject
    {
        public enum StereoRenderingModes
        {
            MultiPass,
            SinglePassInstanced
        }

        public StereoRenderingModes StereoRenderingMode = StereoRenderingModes.SinglePassInstanced;
    }
}
