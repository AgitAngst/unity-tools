using UnityEngine;

// Full-screen effect on a camera: the auditor looks for OnRenderImage.
public class ImageEffectStub : MonoBehaviour
{
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Graphics.Blit(source, destination);
    }
}
