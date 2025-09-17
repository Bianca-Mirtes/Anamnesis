using GLTFast.Schema;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class SettingPointsController : MonoBehaviour
{
    public XRRayInteractor rayInteractor; // arraste seu Ray Interactor aqui

    private List<CubemapFace> currentFaces = new List<CubemapFace>();
    private Cubemap currentCubemap;
    private Texture2D result;
    private List<Vector2> coords = new List<Vector2>();
    private float radius = 10f;

    [SerializeField] private TextMeshProUGUI description;
    [SerializeField] private Button sendBtn;
    [SerializeField] private Button backBtn;
    private Vector3[] directions = new Vector3[2];
    private List<GameObject> points = new List<GameObject>();
    private List<InputDevice> devices = new List<InputDevice>();
    private int pointIndex = 0;

    [SerializeField] private GameObject markerPrefab;

    public UnityEngine.Material lineMaterial;          // material emissivo para a linha
    //public UnityEngine.Material fillMaterial;          // material transparente para preencher (opcional)

    private LineRenderer lr;
    private GameObject fillQuad;

    private void MarkerInstance(Vector3 dir)
    {
        Vector3 pos = dir.normalized * radius + UnityEngine.Camera.main.transform.position;

        points.Add(Instantiate(markerPrefab, pos, Quaternion.Euler(0f, 90f, 0f)));
    }

    public void SetCubemap(Cubemap cub)
    {
        currentCubemap = cub;
    }

    public List<CubemapFace> GetCurrentFaces()
    {
        return currentFaces;
    }

    public List<Vector2> GetCoords()
    {
        return coords;
    }

    public string GetImageInBase64()
    {
        byte[] textureBytes = result.EncodeToPNG();
        // converte para Base64
        string base64Image = Convert.ToBase64String(textureBytes);
        return base64Image;
    }

    private void Start()
    {
        lr = GetComponent<LineRenderer>();
        lr.loop = true;
        lr.widthMultiplier = 0.05f;
        if (lineMaterial != null) lr.material = lineMaterial;


        description.text = "Press X to set the points...";
        sendBtn.onClick.AddListener(() => {
            GameController.Instance.ChangeState(State.Recording);
            rayInteractor.gameObject.SetActive(false);
            Destroy(points[0]);
            Destroy(points[1]);
            description.text = "Press X to set the points...";
        });
        backBtn.onClick.AddListener(ReturnStep);
    }

    private void ReturnStep()
    {
        GameController.Instance.ChangeState(StateController.Instance.GetLastState());
        rayInteractor.gameObject.SetActive(false);
        FuncionalityController.Instance.SetFuncionality(Funcionality.NONE);
        description.text = "Press X to set the points...";
    }

    // Update is called once per frame
    void Update()
    {
        if (StateController.Instance.GetState() == State.SettingPoints) 
        {
            rayInteractor.gameObject.SetActive(true);
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.Space))   
            {
                description.text = $"Setting the point {pointIndex + 1}...";
                Debug.Log($"Setting the point {pointIndex + 1}...");
                // Pega a direção do raio
                Vector3 dir = rayInteractor.rayOriginTransform.forward.normalized;

                MarkerInstance(dir);

                directions[pointIndex] = dir;

                pointIndex++;

                // Se já tiver 2 pontos, faz o crop
                if (pointIndex >= 2)
                {
                    result = CropBoxFromDirections(currentCubemap, directions[0], directions[1]);

                    DrawBox(directions[0], directions[1]);
                    /*string folderPath = Path.Combine(Application.dataPath, "croppeds");
                    Directory.CreateDirectory(folderPath);

                    int id = UnityEngine.Random.Range(0, 1000);

                    byte[] image = result.EncodeToPNG();
                    string filePath = Path.Combine(folderPath, $"cropped_{id}.png");
                    File.WriteAllBytes(filePath, image);*/

                    description.text = " 2 pontod set!";
                    rayInteractor.gameObject.SetActive(false);
                    pointIndex = 0; // resetar para capturar novamente
                }
            }
#else
            InputDeviceCharacteristics leftHandCharacteristics = InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller;
            InputDevices.GetDevicesWithCharacteristics(leftHandCharacteristics, devices);
            devices[0].TryGetFeatureValue(CommonUsages.primaryButton, out bool isPressed);

            if(isPressed)
            {
                description.text = $"Setting the point {pointIndex + 1}...";
                Debug.Log($"Setting the point {pointIndex + 1}...");
                // Pega a direção do raio
                Vector3 dir = rayInteractor.rayOriginTransform.forward.normalized;

                MarkerInstance(dir);

                directions[pointIndex] = dir;

                pointIndex++;

                // Se já tiver 2 pontos, faz o crop
                if (pointIndex >= 2)
                {
                    result = CropBoxFromDirections(currentCubemap, directions[0], directions[1]);

                    DrawBox(directions[0], directions[1]);
                    /*string folderPath = Path.Combine(Application.dataPath, "croppeds");
                    Directory.CreateDirectory(folderPath);

                    int id = UnityEngine.Random.Range(0, 1000);

                    byte[] image = result.EncodeToPNG();
                    string filePath = Path.Combine(folderPath, $"cropped_{id}.png");
                    File.WriteAllBytes(filePath, image);*/

                    description.text = " 2 pontod set!";
                    rayInteractor.gameObject.SetActive(false);
                    pointIndex = 0; // resetar para capturar novamente
                }
            }
#endif
        }
    }

    /// <summary>
    /// Marca um retângulo no skybox a partir de duas direções (normalizadas).
    /// </summary>
    public void DrawBox(Vector3 dirA, Vector3 dirB)
    {
        // Converte direções para UVs esféricos (como no HDR equiretangular)
        Vector2 uvA = DirToUV(dirA);
        Vector2 uvB = DirToUV(dirB);

        // Calcula os outros dois cantos combinando u/v
        Vector2 uvC = new Vector2(uvA.x, uvB.y);
        Vector2 uvD = new Vector2(uvB.x, uvA.y);

        // Converte de volta para direções 3D
        Vector3 cornerA = UVToDir(uvA) * radius + UnityEngine.Camera.main.transform.position;
        Vector3 cornerB = UVToDir(uvC) * radius + UnityEngine.Camera.main.transform.position;
        Vector3 cornerC = UVToDir(uvB) * radius + UnityEngine.Camera.main.transform.position;
        Vector3 cornerD = UVToDir(uvD) * radius + UnityEngine.Camera.main.transform.position;    

        // Desenha borda com LineRenderer
        lr.positionCount = 5;
        lr.SetPositions(new Vector3[] { cornerA, cornerB, cornerC, cornerD, cornerA });

        // Opcional: criar quad preenchido
        /*if (fillMaterial != null)
        {
            if (fillQuad == null)
            {
                fillQuad = new GameObject("BoxFill");
                var mf = fillQuad.AddComponent<MeshFilter>();
                var mr = fillQuad.AddComponent<MeshRenderer>();
                mr.material = fillMaterial;
            }

            UnityEngine.Mesh mesh = new UnityEngine.Mesh();
            mesh.vertices = new Vector3[] { cornerA, cornerB, cornerC, cornerD };
            mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateNormals();

            fillQuad.GetComponent<MeshFilter>().mesh = mesh;
        }*/
    }

    // Converte direção no mundo -> UV equiretangular
    private Vector2 DirToUV(Vector3 dir)
    {
        dir.Normalize();
        dir = new Vector3(-dir.x, dir.y, -dir.z); // alinhamento HDR

        float u = 0.5f + (Mathf.Atan2(dir.z, dir.x) / (2 * Mathf.PI));
        float v = 0.5f - (Mathf.Asin(dir.y) / Mathf.PI);

        return new Vector2(u, v);
    }

    // Converte UV equiretangular -> direção no mundo
    private Vector3 UVToDir(Vector2 uv)
    {
        float lon = (uv.x - 0.5f) * 2f * Mathf.PI;
        float lat = (0.5f - uv.y) * Mathf.PI;

        float x = Mathf.Cos(lat) * Mathf.Cos(lon);
        float y = Mathf.Sin(lat);
        float z = Mathf.Cos(lat) * Mathf.Sin(lon);

        Vector3 dir = new Vector3(-x, y, -z); // reverter alinhamento
        return dir.normalized;
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

    // pontos em faces diferentes (case hdrTex)
    public static Texture2D CropBox(Texture2D hdrTex, Vector2 uvA, Vector2 uvB)
    {
        // normaliza UV (0..1)
        float u1 = Mathf.Repeat(uvA.x, 1f);
        float v1 = Mathf.Clamp01(uvA.y);
        float u2 = Mathf.Repeat(uvB.x, 1f);
        float v2 = Mathf.Clamp01(uvB.y);

        // garante u1 <= u2, v1 <= v2
        if (u1 > u2) { float tmp = u1; u1 = u2; u2 = tmp; }
        if (v1 > v2) { float tmp = v1; v1 = v2; v2 = tmp; }

        int w = hdrTex.width;
        int h = hdrTex.height;

        // converte para pixels
        int x1 = Mathf.RoundToInt(u1 * w);
        int x2 = Mathf.RoundToInt(u2 * w);
        int y1 = Mathf.RoundToInt(v1 * h);
        int y2 = Mathf.RoundToInt(v2 * h);

        int cropW = x2 - x1;
        int cropH = y2 - y1;

        // calcula deltaU em UV space
        float deltaU = u2 - u1;

        if (deltaU <= 0.5f)
        {
            // crop normal
            Color[] pixels = hdrTex.GetPixels(x1, y1, cropW, cropH);
            Texture2D result = new Texture2D(cropW, cropH, hdrTex.format, false);
            result.SetPixels(pixels);
            result.Apply();
            return result;
        }
        else
        {
            // atravessa borda → dois crops
            int leftW = x1;         // 0 → u1
            int rightW = w - x2;    // u2 → 1

            int finalW = leftW + rightW;
            int finalH = cropH;

            Texture2D result = new Texture2D(finalW, finalH, hdrTex.format, false);

            // crop esquerda
            if (leftW > 0)
            {
                Color[] leftPixels = hdrTex.GetPixels(0, y1, leftW, cropH);
                result.SetPixels(0, 0, leftW, cropH, leftPixels);
            }

            // crop direita
            if (rightW > 0)
            {
                Color[] rightPixels = hdrTex.GetPixels(x2, y1, rightW, cropH);
                result.SetPixels(leftW, 0, rightW, cropH, rightPixels);
            }

            result.Apply();
            return result;
        }
    }

    #region 6 faces
    // Converte um vetor direção -> UV de uma face do cubemap
    public static void DirectionToFaceUV(Vector3 dir, out CubemapFace face, out Vector2 uv)
    {
        dir.Normalize();
        float absX = Mathf.Abs(dir.x);
        float absY = Mathf.Abs(dir.y);
        float absZ = Mathf.Abs(dir.z);

        if (absX >= absY && absX >= absZ)
        {
            if (dir.x > 0) { 
                face = CubemapFace.PositiveX; 
                uv = new Vector2(-dir.z, -dir.y) / absX; 
            }
            else { 
                face = CubemapFace.NegativeX; 
                uv = new Vector2(dir.z, -dir.y) / absX;
            }
        }
        else if (absY >= absX && absY >= absZ)
        {
            if (dir.y > 0) { face = CubemapFace.PositiveY; uv = new Vector2(dir.x, dir.z) / absY; }
            else { face = CubemapFace.NegativeY; uv = new Vector2(dir.x, -dir.z) / absY; }
        }
        else
        {
            if (dir.z > 0) { face = CubemapFace.PositiveZ; uv = new Vector2(dir.x, -dir.y) / absZ; }
            else { face = CubemapFace.NegativeZ; uv = new Vector2(-dir.x, -dir.y) / absZ; }
        }

        uv = (uv + Vector2.one) * 0.5f; // normaliza para [0,1]
    }

    // Crop em uma face do cubemap
    private static Texture2D CropFace(Cubemap cube, CubemapFace face, Vector2 uvA, Vector2 uvB)
    {
        int res = cube.width; // cubemap é quadrado
        int x1 = Mathf.RoundToInt(Mathf.Min(uvA.x, uvB.x) * res);
        int y1 = Mathf.RoundToInt(Mathf.Min(uvA.y, uvB.y) * res);
        int x2 = Mathf.RoundToInt(Mathf.Max(uvA.x, uvB.x) * res);
        int y2 = Mathf.RoundToInt(Mathf.Max(uvA.y, uvB.y) * res);

        int w = x2 - x1;
        int h = y2 - y1;

        Color[] pixels = cube.GetPixels(face);
        Texture2D faceTex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        faceTex.SetPixels(pixels);
        faceTex.Apply();

        Color[] crop = faceTex.GetPixels(x1, y1, w, h);
        Texture2D result = new Texture2D(w, h, TextureFormat.RGBA32, false);
        result.SetPixels(crop);
        result.Apply();

        return result;
    }

    // Função principal
    public Texture2D CropBoxFromDirections(Cubemap cube, Vector3 dirA, Vector3 dirB)
    {
        DirectionToFaceUV(dirA, out CubemapFace faceA, out Vector2 uvA);
        DirectionToFaceUV(dirB, out CubemapFace faceB, out Vector2 uvB);

        if (faceA == faceB)
        {
            // Ambos pontos na mesma face → crop direto
            Texture2D cropA = CropFace(cube, faceA, uvA, uvB);
            currentFaces.Add(faceA);
            coords.Add(uvA);
            coords.Add(uvB);
            Debug.Log($"coord 1 - {faceA}: " + uvA);
            Debug.Log($"coord 2 - {faceA}: " + uvB);
            return SkyBoxController.Instance.FlipTextureY(cropA);
        }
        else
        {
            // Estão em faces diferentes → precisa de duas texturas
            Texture2D cropA = CropFace(cube, faceA, uvA, new Vector2(1, 1));
            Texture2D cropB = CropFace(cube, faceB, new Vector2(0, 0), uvB);

            // Corrige a inversão vertical
            Texture2D cropAInverted = SkyBoxController.Instance.FlipTextureY(cropA);
            Texture2D cropBInverted = SkyBoxController.Instance.FlipTextureY(cropB);

            currentFaces.Add(faceA);
            currentFaces.Add(faceB);
            coords.Add(uvA);
            coords.Add(uvB);

            Debug.Log($"coord {faceA}: " + uvA);
            Debug.Log($"coord {faceB}: " + uvB);

            // Une as duas imagens horizontalmente
            int finalW = cropAInverted.width + cropBInverted.width;
            int finalH = Mathf.Max(cropAInverted.height, cropBInverted.height);
            Texture2D result = new Texture2D(finalW, finalH, TextureFormat.RGBA32, false);

            result.SetPixels(0, 0, cropAInverted.width, cropAInverted.height, cropAInverted.GetPixels());
            result.SetPixels(cropAInverted.width, 0, cropBInverted.width, cropBInverted.height, cropBInverted.GetPixels());
            result.Apply();

            return result;
        }
    }
    #endregion
}
