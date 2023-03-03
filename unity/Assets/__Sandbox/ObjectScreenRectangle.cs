using UnityEditor;
using UnityEngine;

public class ObjectScreenRectangle : MonoBehaviour
{
    public MeshRenderer MeshRenderer;
    public MeshFilter Mesh;
    public Texture2D Texture = null;
    public Texture2D Texture2 = null;

    void OnDrawGizmos()
    {
        Bounds WorldBounds = MeshRenderer.bounds;

        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(WorldBounds.center, WorldBounds.size);
    }

    private void OnGUI()
    {
        if (this.Texture == null)
        {
            this.Texture = new Texture2D(1, 1);
            this.Texture.SetPixel(0, 0, Color.red);
            this.Texture.Apply();
        }

        if (this.Texture2 == null)
        {
            this.Texture2 = new Texture2D(1, 1);
            this.Texture2.SetPixel(0, 0, Color.green * 0.33f);
            this.Texture2.Apply();
        }

        var camera = Camera.main;

        Bounds WorldBounds = Mesh.mesh.bounds; // local space

        Vector3 _Center = WorldBounds.center;
        Vector3 _Extent = WorldBounds.extents;


        var _Corners = new Vector3[]
        {
            new Vector3(_Center.x - _Extent.x, _Center.y + _Extent.y, _Center.z - _Extent.z),  // Front top left corner
            new Vector3(_Center.x + _Extent.x, _Center.y + _Extent.y, _Center.z - _Extent.z),  // Front top right corner
            new Vector3(_Center.x - _Extent.x, _Center.y - _Extent.y, _Center.z - _Extent.z),  // Front bottom left corner
            new Vector3(_Center.x + _Extent.x, _Center.y - _Extent.y, _Center.z - _Extent.z),  // Front bottom right corner
            new Vector3(_Center.x - _Extent.x, _Center.y + _Extent.y, _Center.z + _Extent.z),  // Back top left corner
            new Vector3(_Center.x + _Extent.x, _Center.y + _Extent.y, _Center.z + _Extent.z),  // Back top right corner
            new Vector3(_Center.x - _Extent.x, _Center.y - _Extent.y, _Center.z + _Extent.z),  // Back bottom left corner
            new Vector3(_Center.x + _Extent.x, _Center.y - _Extent.y, _Center.z + _Extent.z)   // Back bottom right corner
        };

        float minX = +Mathf.Infinity;
        float minY = +Mathf.Infinity;
        float maxX = -Mathf.Infinity;
        float maxY = -Mathf.Infinity;

        for (int i = 0; i < _Corners.Length; ++i)
        {
            var corner = this.Mesh.transform.TransformPoint(_Corners[i]);
            corner = camera.WorldToViewportPoint(corner);

            float x = corner.x * camera.pixelWidth;
            float y = (1.0f - corner.y) * camera.pixelHeight;

            var rect = new Rect(x - 1.0f, y - 1.0f, 3, 3);
            GUI.DrawTexture(rect, this.Texture);

            if (x > maxX) maxX = x;
            if (x < minX) minX = x;
            if (y > maxY) maxY = y;
            if (y < minY) minY = y;

            minX = Mathf.Clamp(minX, 0, camera.pixelWidth);
            maxX = Mathf.Clamp(maxX, 0, camera.pixelWidth);
            minY = Mathf.Clamp(minY, 0, camera.pixelHeight);
            maxY = Mathf.Clamp(maxY, 0, camera.pixelHeight);
        }

        var rect2 = new Rect(minX, minY, maxX-minX, maxY-minY);
        GUI.DrawTexture(rect2, this.Texture2);
    }
}
