using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class TrafficCamera : MonoBehaviour
{
    [Header("Camera Settings")]
    public float captureInterval = 3.0f;
    public int resolutionWidth = 256;
    public int resolutionHeight = 512;

    [Header("Server Connection")]
    public string serverUrl = "http://127.0.0.1:8000/count_cars";

    private Camera cam;
    private RenderTexture renderTexture;
    private RoadSegment parentRoad;

    public int lastDetectedCars = 0;

    public void Setup(RoadSegment road)
    {
        parentRoad = road;
        cam = gameObject.AddComponent<Camera>();

        cam.fieldOfView = 36f;

        cam.enabled = false;

        renderTexture = new RenderTexture(resolutionWidth, resolutionHeight, 24);
        cam.targetTexture = renderTexture;

        Vector3 endOfRoadPos = road.GetWorldPositionOfSlot(0);

        transform.position = endOfRoadPos
                             + Vector3.up * 6.2f
                             + parentRoad.transform.forward * 8.0f;

        Vector3 lookDirection = (-road.transform.forward + Vector3.down * 0.4f).normalized;
        transform.rotation = Quaternion.LookRotation(lookDirection);

        StartCoroutine(CaptureRoutine());
    }

    private IEnumerator CaptureRoutine()
    {
        yield return new WaitForSeconds(Random.Range(1f, 3f));

        while (true)
        {
            yield return new WaitForSeconds(captureInterval);
            CaptureAndProcessImage();
        }
    }

    private void CaptureAndProcessImage()
    {
        cam.Render();

        RenderTexture.active = renderTexture;
        Texture2D image = new Texture2D(resolutionWidth, resolutionHeight, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, resolutionWidth, resolutionHeight), 0, 0);
        image.Apply();
        RenderTexture.active = null;

        byte[] bytes = image.EncodeToJPG(75);
        StartCoroutine(SendToYOLO(bytes));

        Destroy(image);
    }

    // Komunikacja z pythonem
    private IEnumerator SendToYOLO(byte[] imageBytes)
    {
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", imageBytes, "frame.jpg", "image/jpeg");

        using (UnityWebRequest www = UnityWebRequest.Post(serverUrl, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[YOLO Server] B³¹d na drodze {parentRoad.name}: {www.error}");
            }
            else
            {
                string jsonResponse = www.downloadHandler.text;
                YoloResponse response = JsonUtility.FromJson<YoloResponse>(jsonResponse);

                if (response != null && response.status == "success")
                {
                    lastDetectedCars = response.cars;
                }
            }
        }
    }

    void OnGUI()
    {
#if UNITY_EDITOR
        if (UnityEditor.Selection.activeGameObject == this.gameObject && renderTexture != null)
        {
            GUI.color = new Color(0, 0, 0, 0.8f);
            GUI.DrawTexture(new Rect(10, 10, 135, 290), Texture2D.whiteTexture);

            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(15, 15, 125, 250), renderTexture, ScaleMode.ScaleToFit, false);

            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = Color.yellow;
            style.fontSize = 14;
            style.fontStyle = FontStyle.Bold;

            GUI.Label(new Rect(15, 270, 250, 20), $"{parentRoad.name} | Auta: {lastDetectedCars}", style);
        }
#endif
    }

    [System.Serializable]
    private class YoloResponse
    {
        public string status;
        public int cars;
        public string message;
    }
}