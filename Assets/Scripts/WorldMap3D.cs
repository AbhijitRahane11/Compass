using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class WorldMap3D : MonoBehaviour
{
    public int zoomLevel = 2; // low zoom for full world view
    public float tileSize = 1f; // world units
    public Camera mapCamera;

    private int mapTiles; // 2^zoom
    private GameObject[,] tiles;
    public static WorldMap3D objWorldMap;

    private void Awake()
    {
        objWorldMap = this;
    }
    void Start()
    {
        mapTiles = 1 << zoomLevel; // 2^zoomLevel
        tiles = new GameObject[mapTiles, mapTiles];
        StartCoroutine(LoadMapTiles());
    }

    IEnumerator LoadMapTiles()
    {
        for (int x = 0; x < mapTiles; x++)
        {
            for (int y = 0; y < mapTiles; y++)
            {
                string url = $"https://tile.openstreetmap.org/{zoomLevel}/{x}/{y}.png";
                UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failed to load tile {x},{y}: {www.error}");
                    continue;
                }

                Texture2D texture = DownloadHandlerTexture.GetContent(www);
                GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);

                // Set texture
                Material mat = new Material(Shader.Find("Unlit/Texture"));
                mat.mainTexture = texture;
                quad.GetComponent<Renderer>().material = mat;

                // Position the quad (center the map around origin)
                float offsetX = x - mapTiles / 2f;
                float offsetY = y - mapTiles / 2f;

                quad.transform.localScale = Vector3.one * tileSize;
                quad.transform.position = new Vector3(offsetX * tileSize, -offsetY * tileSize, 0); // XY plane

                quad.transform.SetParent(this.transform);
                tiles[x, y] = quad;
            }
        }
    }

    /// <summary>
    /// Zoom into a given lat/lon coordinate.
    /// </summary>
    public void ZoomToLocation(float lat, float lon, float zoomFactor = 3f)
    {
        int tileX = LonToTileX(lon, zoomLevel);
        int tileY = LatToTileY(lat, zoomLevel);

        float x = (tileX - mapTiles / 2f) * tileSize;
        float y = -(tileY - mapTiles / 2f) * tileSize;

        // Move camera to location
        Vector3 targetPos = new Vector3(x, y, -10); // Orthographic Z
        mapCamera.transform.position = targetPos;

        // Zoom by adjusting orthographic size
        if (mapCamera.orthographic)
        {
            mapCamera.orthographicSize = tileSize * zoomFactor;
        }
        else
        {
            mapCamera.transform.position += new Vector3(0, 0, -5f); // move closer
        }
    }

    int LonToTileX(float lon, int zoom)
    {
        return (int)((lon + 180.0f) / 360.0f * (1 << zoom));
    }

    int LatToTileY(float lat, int zoom)
    {
        double latRad = lat * Mathf.Deg2Rad;
        return (int)((1.0 - Mathf.Log(Mathf.Tan((float)latRad) + 1 / Mathf.Cos((float)latRad)) / Mathf.PI) / 2.0 * (1 << zoom));
    }
}
