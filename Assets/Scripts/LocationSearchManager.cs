
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using static UnityEngine.EventSystems.EventTrigger;

public class LocationSearchManager : MonoBehaviour
{
    [SerializeField] private TMP_Text compassTrueHeadingTxt, magneticHeadingTxt, currentLati, currentLong, centerLat, centerLon, dist, maxLonTxt, minLonTxt, maxLatTxt, minLatTxt, LocationNameTxt;
    [SerializeField] private Toggle togglePrefab, ToggleIPCheck;
    [SerializeField] private Transform toggleParent;
    [SerializeField] private Image TrueHead, LoadImg;
    [SerializeField] private GameObject ScrollView, LocationName;
    [SerializeField] private Button quit;
    [SerializeField] private TMP_InputField LocationSearchInputField;
    [SerializeField] private RawImage mapImage;
    public RectTransform mapContainer;
    Result[] results;
    List<List<List<Vector2>>> PolygonCoordinates = new List<List<List<Vector2>>>();
    float latitudeRegion, longitudeRegion, latitudeDevice, longitudeDevice, magneticHeading;
    string display_name, RegionType, AddressType;

    float minLat, maxLat, minLon, maxLon, distance = 0;
    float EARTH_RADIUS = 6371000f;


    private void Awake()
    {
        Loading(true);
        //StartCoroutine(LoadMap(73.8652f, 18.4529f, 10));
        Loading(false);

        LocationSearchInputField.onEndEdit.AddListener(delegate { OnIpChanged(); });
        ToggleIPCheck.onValueChanged.AddListener(delegate { OnToggleIPCheck(ToggleIPCheck); });
        ToggleIPCheck.isOn = false;
        ScrollView.gameObject.SetActive(false);
        quit.onClick.AddListener(delegate { StartCoroutine("Quit", 0); });
    }
    public void OnIpChanged()
    {
        if (LocationSearchInputField.text != "")
        {
            StartCoroutine(FetchLocationData(LocationSearchInputField.text));
        }
        else
        {
            ScrollView.gameObject.SetActive(false);
            DestroyChildToggle();
        }
    }
    public void OnToggleIPCheck(Toggle t)
    {
        if (t.isOn)
        {
            LocationSearchInputField.enabled = true;
            LocationSearchInputField.Select();
            t.transform.GetChild(2).gameObject.SetActive(false);
        }
        else
        {
            DestroyChildToggle();
            t.transform.GetChild(2).gameObject.SetActive(true);
            ScrollView.gameObject.SetActive(false);
            LocationSearchInputField.text = "";
            LocationSearchInputField.enabled = false;
        }
    }

    private IEnumerator FetchLocationData(string query)
    {
        DestroyChildToggle();
        LocationName.gameObject.SetActive(false);
        string url = $"https://nominatim.openstreetmap.org/search?q={UnityWebRequest.EscapeURL(query)}&format=json&limit=1000&polygon_geojson=1";

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            webRequest.SetRequestHeader("User-Agent", "YourAppName/1.0");
            Loading(true);
            yield return webRequest.SendWebRequest();
            Loading(false);

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                results = JsonHelper.FromJson<Result>(webRequest.downloadHandler.text);

                JArray obj = JArray.Parse(webRequest.downloadHandler.text);


                if (results.Length > 0)
                {
                    ScrollView.gameObject.SetActive(true);
                    for (int i = 0; i < results.Length; i++)
                    {

                        JObject Item = (JObject)obj[i];
                        JObject geojson = (JObject)Item["geojson"];

                        string Type = results[i].geojson.type;

                        results[i].geojson.coordinates.Clear();
                        results[i].geojson.coordinates = HandlePolygon(Type, geojson);
                        display_name = (results[i].display_name);
                        AddressType = results[i].addresstype;
                        Toggle t = Instantiate(togglePrefab, toggleParent);
                        t.gameObject.SetActive(true);
                        t.name = i.ToString();
                        t.gameObject.GetComponentInChildren<Text>().text = display_name + "(" + AddressType + ")";
                        t.onValueChanged.AddListener(delegate { OntoggleChecked(t); });
                    }
                }
                else
                {
                    MessageBox.messageBox.Show("No Result Found..\n Please try Search new keyword..");
                    LocationSearchInputField.text = "";
                }
            }
            else
            {
                Debug.LogError("Error fetching location: " + webRequest.error);
                MessageBox.messageBox.Show("Error fetching location");
            }
        }
    }

    public List<List<List<Vector2>>> HandlePolygon(string Type, JObject geojson)
    {
        Loading(true);
        List<List<Vector2>> polygonPoints = new List<List<Vector2>>();
        List<List<List<Vector2>>> MultiPolygonPoints = new List<List<List<Vector2>>>();
        if (Type == "MultiPolygon")
        {
            MultiPolygonGeoJson res1 = JsonConvert.DeserializeObject<MultiPolygonGeoJson>(geojson.ToString());
            List<List<List<List<float>>>> ring = res1.coordinates;
            for (int j = 0; j < ring.Count; j++)
            {
                foreach (var p in ring[j])
                {
                    var poly = new List<Vector2>();
                    foreach (var item in p)
                    {
                        float longi = item[0];
                        float lati = item[1];

                        poly.Add(new Vector2(longi, lati));
                    }
                    polygonPoints.Add(poly);
                }
                MultiPolygonPoints.Add(polygonPoints);
            }
        }
        else if (Type == "Polygon")
        {
            PolygonGeoJson res1 = JsonConvert.DeserializeObject<PolygonGeoJson>(geojson.ToString());
            List<List<List<float>>> ring = res1.coordinates;
            var poly = new List<Vector2>();
            foreach (var p in ring[0])
            {
                float longi = p[0];
                float lati = p[1];

                poly.Add(new Vector2(longi, lati));
            }
            polygonPoints.Add(poly);
            MultiPolygonPoints.Add(polygonPoints);
        }
        Loading(false);
        return MultiPolygonPoints;
    }



    void Loading(bool b)
    {
        LoadImg.gameObject.SetActive(b);
    }


    public void DestroyChildToggle()
    {
        for (int i = 0; i < toggleParent.childCount; i++)
        {
            Destroy(toggleParent.GetChild(i).gameObject);
        }
    }


    public void OntoggleChecked(Toggle t)
    {

        if (t.isOn)
        {
            PolygonCoordinates.Clear();
            ScrollView.gameObject.SetActive(false);
            DestroyChildToggle();
            ToggleIPCheck.isOn = false;
            int index = int.Parse(t.name);

            latitudeRegion = float.Parse(results[index].lat);
            longitudeRegion = float.Parse(results[index].lon);
            
            WorldMap3D.objWorldMap.ZoomToLocation(latitudeRegion,longitudeRegion);

            minLat = float.Parse(results[index].boundingbox[0]);
            maxLat = float.Parse(results[index].boundingbox[1]);
            minLon = float.Parse(results[index].boundingbox[2]);
            maxLon = float.Parse(results[index].boundingbox[3]);
            RegionType = results[index].geojson.type;
            PolygonCoordinates = results[index].geojson.coordinates;
            LocationName.gameObject.SetActive(true);
            LocationNameTxt.text = results[index].display_name;
            StartCoroutine(StartLocationService());

        }
    }


    IEnumerator StartLocationService()
    {

        Input.location.Start();

        Input.compass.enabled = true;

        int maxWait = 10;

        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            //LoadImg.gameObject.SetActive(true);
            yield return new WaitForSeconds(1);
            //LoadImg.gameObject.SetActive(false);
            maxWait--;
        }
        if (maxWait <= 0)
        {
            MessageBox.messageBox.Show("Timed out.");
            yield break;
        }
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            MessageBox.messageBox.Show("Getting location faild..");
            yield break;
        }
        if (!Input.location.isEnabledByUser)
        {
            MessageBox.messageBox.Show("Location not Enabled..");
            yield return new WaitForSeconds(5);
            //StartCoroutine(StartLocationService());
            Input.location.Start();
        }

        if (Input.location.isEnabledByUser)
        {
            InvokeRepeating("CheckLocation", 1f, .1f);
        }


    }

    bool inside;
    void CheckLocation()
    {

        latitudeDevice = Input.location.lastData.latitude;
        longitudeDevice = Input.location.lastData.longitude;
        magneticHeading = Input.compass.magneticHeading;

        distance = GetDistance(latitudeDevice, longitudeDevice, latitudeRegion, longitudeRegion);
        currentLati.text = latitudeDevice.ToString();
        currentLong.text = longitudeDevice.ToString();

        minLatTxt.text = minLat.ToString();
        maxLatTxt.text = maxLat.ToString();
        minLonTxt.text = minLon.ToString();
        maxLonTxt.text = maxLon.ToString();

        compassTrueHeadingTxt.text = magneticHeading.ToString("000");
        //magneticHeadingTxt.text = magneticHeading.ToString("000");
        TrueHead.gameObject.transform.eulerAngles = new Vector3(0, 0, magneticHeading);

        dist.text = (distance / 1000f).ToString();
        centerLat.text = latitudeRegion.ToString();
        centerLon.text = longitudeRegion.ToString();
        if (RegionType == "MultiPolygon" || RegionType == "Polygon")
        {
            Loading(true);
            Vector2 v = new Vector2(longitudeDevice, latitudeDevice);
            inside = GetContainingPolygonIndex(v, PolygonCoordinates);
            Loading(false);
        }
        else
        {
            inside = IsInsideBoundingBox(latitudeDevice, longitudeDevice, minLat, maxLat, minLon, maxLon);
        }

        if (!inside)
        {
            MessageBox.messageBox.Show("You Are Logout...\n" + latitudeDevice + " - " + longitudeDevice + "\nDevice is not in Region", 5);
            Clear();
            CancelInvoke();
            //StartCoroutine("Quit", 3);
        }

    }

    float GetDistance(float lat1, float lon1, float lat2, float lon2)
    {
        float dLat = (lat2 - lat1) * Mathf.Deg2Rad;
        float dLon = (lon2 - lon1) * Mathf.Deg2Rad;

        float radLat1 = lat1 * Mathf.Deg2Rad;
        float radLat2 = lat2 * Mathf.Deg2Rad;

        float a = Mathf.Sin(dLat / 2) * Mathf.Sin(dLat / 2) +
                  Mathf.Cos(radLat1) * Mathf.Cos(radLat2) *
                  Mathf.Sin(dLon / 2) * Mathf.Sin(dLon / 2);

        float c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));
        return EARTH_RADIUS * c;
    }
    bool IsInsideBoundingBox(float lat, float lon, float minLat, float maxLat, float minLon, float maxLon)
    {
        return lat >= minLat && lat <= maxLat && lon >= minLon && lon <= maxLon;
    }

    public bool GetContainingPolygonIndex(Vector2 point, List<List<List<Vector2>>> Multipolygon)
    {
        for (int polygonIndex = 0; polygonIndex < Multipolygon.Count; polygonIndex++)
        {
            var polygon = Multipolygon[polygonIndex];

            if (IsPointInPolygon(point, polygon))
            {
                return true;
            }
        }

        return false;
    }


    public bool IsPointInPolygon(Vector2 point, List<List<Vector2>> polygon)
    {
        if (polygon == null || polygon.Count == 0)
            return false;

        if (!IsPointInRing(point, polygon[0]))
            return false;

        for (int i = 1; i < polygon.Count; i++)
        {
            if (IsPointInRing(point, polygon[i]))
                return false; 
        }

        return true;
    }

    private bool IsPointInRing(Vector2 point, List<Vector2> ring)
    {
        bool inside = false;
        int count = ring.Count;

        for (int i = 0, j = count - 1; i < count; j = i++)
        {
            if (((ring[i].y > point.y) != (ring[j].y > point.y)) &&
                (point.x < (ring[j].x - ring[i].x) * (point.y - ring[i].y) /
                            (ring[j].y - ring[i].y) + ring[i].x))
            {
                inside = !inside;
            }
        }

        return inside;
    }


    void Clear()
    {
        latitudeDevice = 0;
        longitudeDevice = 0;
        latitudeRegion = 0;
        longitudeRegion = 0;
        currentLati.text = "";
        currentLong.text = "";
        centerLat.text = "";
        centerLon.text = "";
        compassTrueHeadingTxt.text = "";
        magneticHeadingTxt.text = "";
        minLatTxt.text = "";
        minLonTxt.text = "";
        maxLatTxt.text = "";
        maxLonTxt.text = "";
        dist.text = "";
    }
    IEnumerator Quit(float time)
    {
        yield return new WaitForSeconds(time);
        Application.Quit();
    }

}
[System.Serializable]
public class Result
{
    public long place_id;
    public string licence;
    public string osm_type;
    public long osm_id;
    public string lat;
    public string lon;
    public string @class; // 'class' is a keyword in C#
    public string type;
    public int place_rank;
    public double importance;
    public string addresstype;
    public string name;
    public string display_name;
    public List<string> boundingbox;
    public GeoJson geojson;
}
[System.Serializable]
public class GeoJson
{
    public string type;
    //public string[] coordinates;
    public List<List<List<Vector2>>> coordinates = new List<List<List<Vector2>>>();
}
public static class JsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        string newJson = "{ \"array\": " + json + "}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
        return wrapper.array;
    }

    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] array;
    }
}

public class PolygonGeoJson
{
    [JsonProperty("coordinates")]
    public List<List<List<float>>> coordinates;
}
public class MultiPolygonGeoJson
{
    [JsonProperty("coordinates")]
    public List<List<List<List<float>>>> coordinates;
}