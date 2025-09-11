using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.XR;

public class RecordingController : MonoBehaviour
{
    private string micDevice;
    private AudioClip recordedClip;
    private AudioClip trimmedClip = null;
    private bool isRecording = false;
    private bool lastPressed = false;
    private List<InputDevice> devices = new List<InputDevice>();

    private GameObject spinner;
    private TMP_Text description;
    private Button sendAudioBtn = null;
    private Button newAudioBtn = null;
    private Button visualizeBtn = null;
    private Button returnBtn = null;
    private string baseUrl;
    [SerializeField] private GameObject objectC;

    private Cubemap result;
    private Dictionary<FaceName, string> facesGenerated;

    private string imageObj_base64 = "";
    private string obj_base64 = "";

    private static RecordingController _instance;
    private bool wasSend = false;

    // Singleton
    public static RecordingController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<RecordingController>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("RecordingController");
                    _instance = obj.AddComponent<RecordingController>();
                }
            }
            return _instance;
        }
    }

    void Start()
    {
        baseUrl = "https://4dbbb1e6032f.ngrok-free.app";
        // Se ainda não tem a permissão, pede
        if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            Debug.Log("Pedindo permissão de microfone...");
            Application.RequestUserAuthorization(UserAuthorization.Microphone);
        }

        if (Microphone.devices.Length > 0)
            micDevice = Microphone.devices[0];
        else
            Debug.LogError("Nenhum microfone encontrado!");
    }

    private void Update()
    {
        if (StateController.Instance.GetState() == State.Recording)
        {
            /*InputDeviceCharacteristics leftHandCharacteristics = InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller;
            InputDevices.GetDevicesWithCharacteristics(leftHandCharacteristics, devices);
            devices[0].TryGetFeatureValue(CommonUsages.secondaryButton, out bool isPressed);

            if(isPressed && !lastPressed && !isRecording)
            {
                StartRecording();
            }
            if(!isPressed && lastPressed && isRecording)
            {
                StopAndSave();
            }

            lastPressed = isPressed;*/

            if (Input.GetKeyDown(KeyCode.L) && !isRecording)
            {
                StartRecording();
            }
            if (Input.GetKeyUp(KeyCode.L) && isRecording)
            {
                Stop();
            }
        }
    }

    public void SetAttributes(GameObject currentSp, TMP_Text desc, Button send, Button newAudio, Button back, Button visualize)
    {
        spinner = currentSp;
        description = desc;
        sendAudioBtn = send;
        newAudioBtn = newAudio;
        returnBtn = back;
        visualizeBtn = visualize;

        returnBtn.onClick.AddListener(ReturnStep);
        sendAudioBtn.onClick.AddListener(SendAudio);
        newAudioBtn.onClick.AddListener(NewAudio);
        visualizeBtn.onClick.AddListener(Visualize);
    }

    void StartRecording()
    {
        isRecording = true;
        recordedClip = Microphone.Start(micDevice, false, 40, 44100);
        description.text = "Recording...";
        spinner.SetActive(isRecording);
        Debug.Log("Gravação iniciada...");
    }

    void Stop()
    {
        isRecording = false;
        description.text = "Recording ended!";
        spinner.SetActive(isRecording);

        // Pega quantos samples realmente foram gravados
        int position = Microphone.GetPosition(micDevice);
        Microphone.End(micDevice);

        float[] samples = new float[recordedClip.samples * recordedClip.channels];
        recordedClip.GetData(samples, 0);

        // Cria um novo clip só com a parte usada
        float[] trimmedSamples = new float[position * recordedClip.channels];
        Array.Copy(samples, trimmedSamples, trimmedSamples.Length);

        trimmedClip = AudioClip.Create("TrimmedClip", position, recordedClip.channels, 44100, false);
        trimmedClip.SetData(trimmedSamples, 0);
    }

    private void ReturnStep()
    {
        GameController.Instance.ChangeState(StateController.Instance.GetLastState());
        FuncionalityController.Instance.SetFuncionality(Funcionality.NONE);
    }

    private void SendAudio()
    {
        if (trimmedClip == null)
            return;

        if (!wasSend) 
        {
            // converte para bytes WAV
            byte[] wavData = ConvertToWav(trimmedClip);

            // converte para Base64
            string base64Audio = Convert.ToBase64String(wavData);

            GameController.Instance.session_id = UnityEngine.Random.Range(0, 1000);
            if(FuncionalityController.Instance.GetFuncionality() == Funcionality.WORLD_GENERATION)
            {
                PayloadAudioGeneration payload = new PayloadAudioGeneration { audio_base64 = base64Audio, session_id = GameController.Instance.session_id };

                // 4) Serializa para JSON
                string json = JsonUtility.ToJson(payload);

                string url = $"{baseUrl}/generate_360";

                // envia para API
                StartCoroutine(SendToAPI(json, url));
            } else if (FuncionalityController.Instance.GetFuncionality() == Funcionality.ADD) // add
            {
                PayloadAudioGeneration payload = new PayloadAudioGeneration { audio_base64 = base64Audio, session_id = GameController.Instance.session_id };

                // 4) Serializa para JSON
                string json = JsonUtility.ToJson(payload);

                string url = $"{baseUrl}/generate_3d_64";
                // envia para API
                StartCoroutine(SendToAPI(json, url));
            }else if (FuncionalityController.Instance.GetFuncionality() == Funcionality.REMOVE)
            {
                string base64Image = FindFirstObjectByType<SettingPointsController>().GetImageInBase64(); 
                List<PointFacePair> payload_frag = new List<PointFacePair>();
                List<Vector2> coords = FindFirstObjectByType<SettingPointsController>().GetCoords();
                if (coords.Count == 2)
                {
                    PointFacePair pair = new PointFacePair();
                    pair.point[0] = new Point(coords[0].x, coords[0].y);
                    pair.point[1] = new Point(coords[1].x, coords[1].y);

                    CubemapFace currentFa = FindFirstObjectByType<SettingPointsController>().GetCurrentFace()[0];

                    if (currentFa == CubemapFace.NegativeX)
                    {
                        pair.faceName = FaceName.LEFT;
                    }
                    if (currentFa == CubemapFace.PositiveX)
                    {
                        pair.faceName = FaceName.RIGTH;
                    }
                    if (currentFa == CubemapFace.PositiveZ)
                    {
                        pair.faceName = FaceName.FRONT;
                    }
                    if (currentFa == CubemapFace.PositiveY)
                    {
                        pair.faceName = FaceName.UP;
                    }
                    if (currentFa == CubemapFace.NegativeY)
                    {
                        pair.faceName = FaceName.DOWN;
                    }
                    if (currentFa == CubemapFace.NegativeZ)
                    {
                        pair.faceName = FaceName.BACK;
                    }
                    payload_frag.Add(pair);
                }
                else
                {
                    PointFacePair pair1 = new PointFacePair();
                    pair1.point[0] = new Point(coords[0].x, coords[0].y);
                    pair1.point[1] = new Point(coords[1].x, coords[1].y);

                    List<CubemapFace> currentFa = FindFirstObjectByType<SettingPointsController>().GetCurrentFace();

                    if (currentFa[0] == CubemapFace.NegativeX)
                    {
                        pair1.faceName = FaceName.LEFT;
                    }
                    if (currentFa[0] == CubemapFace.PositiveX)
                    {
                        pair1.faceName = FaceName.RIGTH;
                    }
                    if (currentFa[0] == CubemapFace.PositiveZ)
                    {
                        pair1.faceName = FaceName.FRONT;
                    }
                    if (currentFa[0] == CubemapFace.PositiveY)
                    {
                        pair1.faceName = FaceName.UP;
                    }
                    if (currentFa[0] == CubemapFace.NegativeY)
                    {
                        pair1.faceName = FaceName.DOWN;
                    }
                    if (currentFa[0] == CubemapFace.NegativeZ)
                    {
                        pair1.faceName = FaceName.BACK;
                    }

                    PointFacePair pair2 = new PointFacePair();
                    pair2.point[0] = new Point(coords[2].x, coords[2].y);
                    pair2.point[1] = new Point(coords[3].x, coords[3].y);

                    if (currentFa[1] == CubemapFace.NegativeX)
                    {
                        pair1.faceName = FaceName.LEFT;
                    }
                    if (currentFa[1] == CubemapFace.PositiveX)
                    {
                        pair1.faceName = FaceName.RIGTH;
                    }
                    if (currentFa[1] == CubemapFace.PositiveZ)
                    {
                        pair1.faceName = FaceName.FRONT;
                    }
                    if (currentFa[1] == CubemapFace.PositiveY)
                    {
                        pair1.faceName = FaceName.UP;
                    }
                    if (currentFa[1] == CubemapFace.NegativeY)
                    {
                        pair1.faceName = FaceName.DOWN;
                    }
                    if (currentFa[1] == CubemapFace.NegativeZ)
                    {
                        pair1.faceName = FaceName.BACK;
                    }
                }

                PayloadRemove payload = new PayloadRemove { image_base64 = base64Image, 
                    audio_base64 = base64Audio, 
                    session_id = GameController.Instance.session_id,
                    points = payload_frag
                };

                // 4) Serializa para JSON
                string json = JsonUtility.ToJson(payload);

                string url = $"{baseUrl}/process_image";
                // envia para API
                StartCoroutine(SendToAPI(json, url));
            }
            else if (FuncionalityController.Instance.GetFuncionality() == Funcionality.CLONE)
            {
                PayloadAudioGeneration payload = new PayloadAudioGeneration { audio_base64 = base64Audio, session_id = GameController.Instance.session_id };

                // 4) Serializa para JSON
                string json = JsonUtility.ToJson(payload);

                string url = $"{baseUrl}/clone_3d";
            }
            wasSend = true;
            Invoke("ResetSend", 5f);
        }
    }

    private void ResetSend()
    {
        wasSend = false;
        trimmedClip = null;
    }

    byte[] ConvertToWav(AudioClip clip)
    {
        MemoryStream stream = new MemoryStream();
        int samples = clip.samples * clip.channels;
        float[] data = new float[samples];
        clip.GetData(data, 0);

        short[] intData = new short[samples];
        byte[] bytesData = new byte[samples * 2];
        int rescaleFactor = 32767;
        for (int i = 0; i < data.Length; i++)
        {
            intData[i] = (short)(data[i] * rescaleFactor);
            byte[] byteArr = BitConverter.GetBytes(intData[i]);
            byteArr.CopyTo(bytesData, i * 2);
        }

        int hz = clip.frequency;
        int channels = clip.channels;
        int byteRate = hz * channels * 2;

        stream.Write(Encoding.ASCII.GetBytes("RIFF"), 0, 4);
        stream.Write(BitConverter.GetBytes(bytesData.Length + 36), 0, 4);
        stream.Write(Encoding.ASCII.GetBytes("WAVE"), 0, 4);
        stream.Write(Encoding.ASCII.GetBytes("fmt "), 0, 4);
        stream.Write(BitConverter.GetBytes(16), 0, 4);
        stream.Write(BitConverter.GetBytes((short)1), 0, 2);
        stream.Write(BitConverter.GetBytes((short)channels), 0, 2);
        stream.Write(BitConverter.GetBytes(hz), 0, 4);
        stream.Write(BitConverter.GetBytes(byteRate), 0, 4);
        stream.Write(BitConverter.GetBytes((short)(channels * 2)), 0, 2);
        stream.Write(BitConverter.GetBytes((short)16), 0, 2);
        stream.Write(Encoding.ASCII.GetBytes("data"), 0, 4);
        stream.Write(BitConverter.GetBytes(bytesData.Length), 0, 4);
        stream.Write(bytesData, 0, bytesData.Length);

        return stream.ToArray();
    }

    IEnumerator SendToAPI(string json, string apiUrl)
    {
        description.text = "Waiting API response...";
        spinner.SetActive(true);

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            if (FuncionalityController.Instance.GetFuncionality() == Funcionality.WORLD_GENERATION)
            {
                Debug.Log("Resposta: " + request.downloadHandler.text);

                // Aqui você pode deserializar o JSON de volta para um objeto
                CubemapResponse response = JsonUtility.FromJson<CubemapResponse>(request.downloadHandler.text);

                Debug.Log("Status: " + response.status);
                Debug.Log("Message: " + response.message);

                result = SkyBoxController.Instance.BuildCubemap(response.cubemap_images);
                spinner.SetActive(false);
                description.text = "World generated!";
            }
            else if (FuncionalityController.Instance.GetFuncionality() == Funcionality.ADD)
            {
                Model3DResponse response = JsonUtility.FromJson<Model3DResponse>(request.downloadHandler.text);
                imageObj_base64 = response.image_base64;
                obj_base64 = response.glb_base64;
                description.text = "Object 3D generated!";
            }
            else if (FuncionalityController.Instance.GetFuncionality() == Funcionality.REMOVE)
            {
                ImageEditResponse response = JsonUtility.FromJson<ImageEditResponse>(request.downloadHandler.text);
                facesGenerated = response.images_base64;
                description.text = "Object removed!";
            }
            else if(FuncionalityController.Instance.GetFuncionality() == Funcionality.CLONE) {
                Model3DResponse response = JsonUtility.FromJson<Model3DResponse>(request.downloadHandler.text);
                imageObj_base64 = response.image_base64;
                obj_base64 = response.glb_base64;
                description.text = "Clone generated!";
            }
        }
        else
            Debug.LogError("Erro API: " + request.error);
    }

    private void Visualize()
    {
        if (FuncionalityController.Instance.GetFuncionality() == Funcionality.WORLD_GENERATION)
        {
            if (result != null) 
            { 
                SkyBoxController.Instance.ApplyCubemap(result);
                GameController.Instance.ChangeState(State.ChooseOptions);
            }
        }
        if (FuncionalityController.Instance.GetFuncionality() == Funcionality.REMOVE)
        {
            if (facesGenerated != null)
            {
                List<FaceName> currentF = new List<FaceName>();
                List<string> currentI = new List<string>();
                List<CubemapFace> faces = new List<CubemapFace>();
                if (facesGenerated.Count > 1)
                {
                    foreach (KeyValuePair<FaceName, string> obj in facesGenerated)
                    {
                        currentF.Add(obj.Key);
                        currentI.Add(obj.Value);
                        if (currentF[0] == FaceName.LEFT)
                        {
                            faces.Add(CubemapFace.NegativeX);
                        }
                        if (currentF[0] == FaceName.RIGTH)
                        {
                            faces.Add(CubemapFace.PositiveX);
                        }
                        if (currentF[0] == FaceName.FRONT)
                        {
                            faces.Add(CubemapFace.PositiveZ);
                        }
                        if (currentF[0] == FaceName.UP)
                        {
                            faces.Add(CubemapFace.PositiveY);
                        }
                        if (currentF[0] == FaceName.BACK)
                        {
                            faces.Add(CubemapFace.NegativeZ);
                        }
                        if (currentF[0] == FaceName.DOWN)
                        {
                            faces.Add(CubemapFace.NegativeY);
                        }
                    }
                } else {
                    currentF.Add(facesGenerated.First().Key);
                    currentI.Add(facesGenerated.First().Value);
                    if (currentF[0] == FaceName.LEFT)
                    {
                        faces[0] = CubemapFace.NegativeX;
                    }
                    if (currentF[0] == FaceName.RIGTH)
                    {
                        faces[0] = CubemapFace.PositiveX;
                    }
                    if (currentF[0] == FaceName.FRONT)
                    {
                        faces[0] = CubemapFace.PositiveZ;
                    }
                    if (currentF[0] == FaceName.UP)
                    {
                        faces[0] = CubemapFace.PositiveY;
                    }
                    if (currentF[0] == FaceName.BACK)
                    {
                        faces[0] = CubemapFace.NegativeZ;
                    }
                    if (currentF[0] == FaceName.DOWN)
                    {
                        faces[0] = CubemapFace.NegativeY;
                    }
                }
                SkyBoxController.Instance.BuildCubemap(FindFirstObjectByType<ChooseImageController>().GetCurrentFaces(), faces, currentI);
            }
        }
        if (FuncionalityController.Instance.GetFuncionality() == Funcionality.ADD)
        {
            if(imageObj_base64 != "" && obj_base64 != "")
                FindFirstObjectByType<InventoryController>().AddObject(imageObj_base64, obj_base64);
        }
        if (FuncionalityController.Instance.GetFuncionality() == Funcionality.CLONE) {
            if (imageObj_base64 != "" && obj_base64 != "")
                FindFirstObjectByType<InventoryController>().AddObject(imageObj_base64, obj_base64);
        }
    }

    private void NewAudio()
    {
        description.text = "Press Y to start recording...";
    }

    [System.Serializable]
    public class PayloadAudioGeneration
    {
        public string audio_base64;
        public int session_id;
    }

    [System.Serializable]
    public class PayloadRemove
    {
        public string image_base64;
        public string audio_base64;
        public int session_id;
        public List<PointFacePair> points;
    }

    [System.Serializable]
    public class Point
    {
        public float x;
        public float y;
        public Point(float coord_x, float coord_y)
        {
            x = coord_x;
            y = coord_y;
        }
    }


    [System.Serializable]
    public class PointFacePair
    {
        public Point[] point = new Point[2];
        public FaceName faceName;
    }


    [System.Serializable]
    public class PayloadClone3D
    {
        public string image_base64;
        public string audio_base64;
        public int session_id;
    }

    [System.Serializable]
    public class CubemapResponse
    {
        public string status;
        public string message;
        public string[] cubemap_images; 
    }

    [System.Serializable]
    public class Model3DResponse
    {
        public string status;
        public string message;
        public string glb_base64;
        public string image_base64;
    }

    public class ImageEditResponse
    {
        public string status;
        public string message;
        public Dictionary<FaceName, string> images_base64; 
    }

    public enum FaceName
    {
        LEFT,
        FRONT,
        RIGTH,
        BACK,
        UP,
        DOWN
    }
}
