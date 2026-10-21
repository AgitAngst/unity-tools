using UnityEngine.XR.Management;

// Test stub with the class name of the OpenVR XR Plugin loader; it never starts anything.
namespace Unity.XR.OpenVR
{
    public class OpenVRLoader : XRLoader
    {
        public override T GetLoadedSubsystem<T>()
        {
            return null;
        }
    }
}
