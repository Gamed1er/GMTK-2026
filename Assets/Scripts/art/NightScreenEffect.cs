using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class NightScreenEffect : MonoBehaviour
{
    [SerializeField]
    private Material nightMaterial;

    private void OnRenderImage(
        RenderTexture source,
        RenderTexture destination)
    {
        if (nightMaterial == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        Graphics.Blit(source, destination, nightMaterial);
    }
}