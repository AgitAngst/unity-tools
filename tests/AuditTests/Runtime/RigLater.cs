using UnityEngine;

// Builds a VR rig a few frames into Play Mode, as games that spawn the player do: the viewpoint tour must find it
// after its settle frames. The head is 0.3 m off the rig root and turned 20°, like a tracked head.
public class RigLater : MonoBehaviour
{
    int frames;

    void Update()
    {
        if (++frames != 5) return;
        var rig = new GameObject("Spawned Rig");
        rig.transform.position = new Vector3(0f, 0f, -12f);
        rig.AddComponent<Rigidbody>().useGravity = false;
        var head = new GameObject("Head") { tag = "MainCamera" };
        head.transform.SetParent(rig.transform, false);
        head.transform.localPosition = new Vector3(0.3f, 1.6f, 0f);
        head.transform.localRotation = Quaternion.Euler(0f, 20f, 0f);
        head.AddComponent<Camera>().stereoTargetEye = StereoTargetEyeMask.Both;
    }
}
