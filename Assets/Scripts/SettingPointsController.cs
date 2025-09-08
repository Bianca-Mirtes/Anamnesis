using System;
using System.Collections;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using static RecordingController;

public class SettingPointsController : MonoBehaviour
{
    public XRRayInteractor rayInteractor; // arraste seu Ray Interactor aqui
    private WorldTexture worldTextures;
    private CubemapFace currentFace;
    public Texture2D hdrTexture;
    [SerializeField] private TextMeshProUGUI description;
    [SerializeField] private Button sendBtn;

    private Vector2[] points = new Vector2[2];
    private int pointIndex = 0;
    private Texture2D box;

    public void SetWorldTextures(Texture2D tex, string local)
    {
        if (local == "back")
            worldTextures.back = tex;
        if(local == "up")
            worldTextures.up = tex;
        if(local == "left")
            worldTextures.left = tex;
        if(local =="right")
            worldTextures.right = tex;
        if(local == "down")
            worldTextures.down = tex;
        if (local == "front")
            worldTextures.front = tex;
    }

    public void SethdrTexture(Texture2D tex)
    {
        hdrTexture = tex;
    }

    // Update is called once per frame
    void Update()
    {
        if (StateController.Instance.GetState() == State.SettingPoints) 
        { 
            if (Input.GetKeyDown(KeyCode.Space)) // aqui use o input VR do seu controle
            {
                description.text = $"Setting the point {pointIndex + 1}...";
                // Pega a direção do raio
                Vector3 dir = rayInteractor.rayOriginTransform.forward.normalized;

                if (GameController.Instance.way == 1)
                {
                    // corrige alinhamento do HDR (gira 180° no eixo Y)
                    dir = new Vector3(-dir.x, dir.y, -dir.z);

                    // converte para UV
                    float u = 0.5f + (Mathf.Atan2(dir.z, dir.x) / (2 * Mathf.PI));
                    float v = 0.5f - (Mathf.Asin(dir.y) / Mathf.PI);

                    points[pointIndex] = new Vector2(u, v);
                    Debug.Log($"Ponto {pointIndex + 1} capturado: {points[pointIndex]}");

                    pointIndex++;

                    // Se já tiver 2 pontos, faz o crop
                    if (pointIndex >= 2)
                    {
                        CropHDR();
                        pointIndex = 0; // resetar para capturar novamente
                    }
                }
                else
                {
                    Tuple<float, float> coords = FindFace(dir);

                    points[pointIndex] = new Vector2(coords.Item1, coords.Item2);
                    Debug.Log($"Ponto {pointIndex + 1} capturado: {points[pointIndex]}");

                    pointIndex++;

                    // Se já tiver 2 pontos, faz o crop
                    if (pointIndex >= 2)
                    {
                        if (currentFace == CubemapFace.NegativeX)
                            Crop(worldTextures.left, points[0], points[1]);
                        if (currentFace == CubemapFace.PositiveZ)
                            Crop(worldTextures.front, points[0], points[1]);
                        if (currentFace == CubemapFace.PositiveX)
                            Crop(worldTextures.right, points[0], points[1]);
                        if (currentFace == CubemapFace.NegativeZ)
                            Crop(worldTextures.back, points[0], points[1]);
                        if (currentFace == CubemapFace.PositiveY)
                            Crop(worldTextures.up, points[0], points[1]);
                        if (currentFace == CubemapFace.NegativeY)
                            Crop(worldTextures.down, points[0], points[1]);
                        pointIndex = 0; // resetar para capturar novamente
                    }
                }
            }
        }
    }

    Texture2D MakeReadable(Texture2D source)
    {
        RenderTexture tmp = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        Graphics.Blit(source, tmp);

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = tmp;

        Texture2D readableTex = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        readableTex.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
        readableTex.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(tmp);

        return readableTex;
    }

    void CropHDR()
    {
        if (hdrTexture == null)
        {
            Debug.LogError("⚠️ Nenhuma HDR Texture atribuída!");
            return;
        }

        // Converte UV para pixels
        int x1 = Mathf.RoundToInt(points[0].x * hdrTexture.width);
        int y1 = Mathf.RoundToInt(points[0].y * hdrTexture.height);
        int x2 = Mathf.RoundToInt(points[1].x * hdrTexture.width);
        int y2 = Mathf.RoundToInt(points[1].y * hdrTexture.height);

        // Ordena para garantir canto inferior/esquerdo → superior/direito
        int minX = Mathf.Min(x1, x2);
        int maxX = Mathf.Max(x1, x2);
        int minY = Mathf.Min(y1, y2);
        int maxY = Mathf.Max(y1, y2);

        int w = maxX - minX;
        int h = maxY - minY;

        Texture2D readableTex = MakeReadable(hdrTexture); 

        // Pega pixels da região
        Color[] pixels = readableTex.GetPixels(minX, minY, w, h);

        // Cria textura recortada
        Texture2D cropped = new Texture2D(w, h, readableTex.format, false);
        cropped.SetPixels(pixels);
        cropped.Apply();

        // Salva como EXR para não perder HDR
        string path = Path.Combine(Application.dataPath, "crop.exr");
        File.WriteAllBytes(path, cropped.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat));

        Debug.Log($"✅ Crop salvo em: {path}");
    }

    private Tuple<float, float> FindFace(Vector3 dir)
    {
        Vector3 d = dir;
        float absX = Mathf.Abs(d.x);
        float absY = Mathf.Abs(d.y);
        float absZ = Mathf.Abs(d.z);

        float u = 0, v = 0;

        if (absX >= absY && absX >= absZ)
        {
            // Eixo X domina
            if (d.x > 0)
            { // +X
                currentFace = CubemapFace.PositiveX;
                u = -d.z / absX;
                v = -d.y / absX;
            }
            else
            { // -X
                currentFace = CubemapFace.NegativeX;
                u = d.z / absX;
                v = -d.y / absX;
            }
        }
        else if (absY >= absX && absY >= absZ)
        {
            // Eixo Y domina
            if (d.y > 0)
            { // +Y
                currentFace = CubemapFace.PositiveY;
                u = d.x / absY;
                v = d.z / absY;
            }
            else
            { // -Y
                currentFace = CubemapFace.NegativeY;
                u = d.x / absY;
                v = -d.z / absY;
            }
        }
        else
        {
            // Eixo Z domina
            if (d.z > 0)
            { // +Z
                currentFace = CubemapFace.PositiveZ;
                u = d.x / absZ;
                v = -d.y / absZ;
            }
            else
            { // -Z
                currentFace = CubemapFace.NegativeZ;
                u = -d.x / absZ;
                v = -d.y / absZ;
            }
        }

        // Normaliza para [0,1]
        u = 0.5f * (u + 1.0f);
        v = 0.5f * (v + 1.0f);

        return new Tuple<float, float>(u, v);
    }


    void Crop(Texture2D tex, Vector2 point1, Vector2 point2)
    {
        int x1 = Mathf.RoundToInt(point1.x * tex.width);
        int y1 = Mathf.RoundToInt(point1.y * tex.height);
        int x2 = Mathf.RoundToInt(point2.x * tex.width);
        int y2 = Mathf.RoundToInt(point2.y * tex.height);

        int minX = Mathf.Min(x1, x2);
        int maxX = Mathf.Max(x1, x2);
        int minY = Mathf.Min(y1, y2);
        int maxY = Mathf.Max(y1, y2);

        int w = maxX - minX;
        int h = maxY - minY;

        Color[] pixels = tex.GetPixels(minX, minY, w, h);
        Texture2D cropped = new Texture2D(w, h, tex.format, false);
        cropped.SetPixels(pixels);
        cropped.Apply();

        box = cropped;

        string folderPath = Path.Combine(Application.dataPath, "croppeds");
        Directory.CreateDirectory(folderPath);

        int id = UnityEngine.Random.Range(0, 1000);

        byte[] image = cropped.EncodeToEXR();
        string filePath = Path.Combine(folderPath, $"cropped_{id}.exr");
        File.WriteAllBytes(filePath, image);

        description.text = "Stored points!";
    }

    IEnumerator SendToAPI(string json, string apiUrl)
    {

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
        }
        else
            Debug.LogError("Erro API: " + request.error);
    }

    [Serializable]
    public class WorldTexture {
        public Texture2D up;
        public Texture2D down;
        public Texture2D front;
        public Texture2D back;
        public Texture2D left;
        public Texture2D right;
    }
}
