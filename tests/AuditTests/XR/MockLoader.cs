using UnityEngine.XR.Management;

// Test stub: a loader that is not OpenVR.
namespace AuditTestsXR
{
    public class MockLoader : XRLoader
    {
        public override T GetLoadedSubsystem<T>()
        {
            return null;
        }
    }
}
