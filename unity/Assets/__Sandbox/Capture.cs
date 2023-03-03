using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Capture : MonoBehaviour
{
    public Camera Camera;

    // Update is called once per frame
    void Start()
    {
        StartCoroutine(DoTakeSnapshot(1950*2, 1080*2));
    }

    public IEnumerator DoTakeSnapshot(int width, int height)
    {
        yield return new WaitForSeconds(1);
        Debug.Log("snapshot");

        // wait until entire frame is rendered
        yield return new WaitForEndOfFrame();

        // create new texture for final snapshot image
        Texture2D snapshotImage = new Texture2D(width, height, TextureFormat.ARGB32, false);
        RenderTexture RT = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32, 0);

        RenderTexture oldCameraRenderTexture = this.Camera.targetTexture;
        {
            // render current camera view to active render texture     
            this.Camera.targetTexture = RT;
            this.Camera.Render();

            // read rendet texture pixels
            RenderTexture oldActiveRenderTexture = RenderTexture.active;
            {
                RenderTexture.active = this.Camera.targetTexture;
                snapshotImage.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                snapshotImage.Apply();
            }
            // restore original active render texture
            RenderTexture.active = oldActiveRenderTexture;
        }
        this.Camera.targetTexture = oldCameraRenderTexture;

        // return image data
        {
            byte[] data = snapshotImage.EncodeToPNG(); ;

            System.IO.File.WriteAllBytes($"{System.IO.Directory.GetCurrentDirectory()}/test.png", data);

            RenderTexture.ReleaseTemporary(RT);
        }
    }
}
