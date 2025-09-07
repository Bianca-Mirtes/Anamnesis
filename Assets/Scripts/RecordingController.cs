using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    private AudioClip trimmedClip;
    private bool isRecording = false;
    private bool lastPressed = false;
    private List<InputDevice> devices = new List<InputDevice>();

    private GameObject spinner;
    private TMP_Text description;
    private Button sendAudioBtn = null;
    private Button newAudioBtn = null;
    private Button visualizeBtn = null;
    private Button returnBtn = null;
    private int sessionId;
    [SerializeField] private GameObject objectC;

    private Cubemap result;

    private static RecordingController _instance;

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
        description.text = "Recording ended. Sending audio to API...";
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

    private void StopAndSave()
    {
        isRecording = false;
        description.text = "Recording ended. Saving audio file...";
        description.gameObject.SetActive(true);
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

        string filename = "Gravacao_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".wav";
#if UNITY_EDITOR
        string folder = Path.Combine(Application.dataPath, "recordings");
#else
        string folder = Path.Combine(Application.persistentDataPath, "recordings");
#endif
        Directory.CreateDirectory(folder);

        string filepath = Path.Combine(folder, filename);

        SaveWav(filepath, trimmedClip);
        Debug.Log("Áudio salvo em: " + filepath);
    }

    private void ReturnStep()
    {
        StateController.Instance.SetState(StateController.Instance.GetLastState());
    }

    private void SendAudio()
    {
        // converte para bytes WAV
        byte[] wavData = ConvertToWav(trimmedClip);

        // converte para Base64
        string base64Audio = Convert.ToBase64String(wavData);

        sessionId = UnityEngine.Random.Range(0, 1000);

        PayloadAudio payload = new PayloadAudio { audio_base64 = base64Audio, session_id = sessionId };

        // 4) Serializa para JSON
        string json = JsonUtility.ToJson(payload);

        // envia para API
        StartCoroutine(SendToAPI(json));
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

    IEnumerator SendToAPI(string json)
    {
        description.text = "Waiting API response...";
        spinner.SetActive(true);
        string apiUrl = "https://6c348d9d03dd.ngrok-free.app/generate_360";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Resposta: " + request.downloadHandler.text);

            // Aqui você pode deserializar o JSON de volta para um objeto
            CubemapResponse response = JsonUtility.FromJson<CubemapResponse>(request.downloadHandler.text);

            Debug.Log("Status: " + response.status);
            Debug.Log("Message: " + response.message);

            BuildCubemap(response.cubemap_images);
        }
        else
            Debug.LogError("Erro API: " + request.error);
    }
    private void SaveBase64Images(string imagesBase64)
    {
#if UNITY_EDITOR
        string folderPath = Path.Combine(Application.dataPath, "Cubemaps");
#else
        string folderPath = Path.Combine(Application.persistentDataPath, "Cubemaps");
#endif
        Directory.CreateDirectory(folderPath);

        for (int i = 0; i < imagesBase64.Length; i++)
        {
            // Converte base64 → bytes
            byte[] imgBytes = Convert.FromBase64String(imagesBase64);

            // Salva como PNG
            string filePath = Path.Combine(folderPath, $"cubemap_{i}.png");
            File.WriteAllBytes(filePath, imgBytes);

            Debug.Log($"Imagem salva: {filePath}");
        }
        StateController.Instance.SetState(State.ChooseOptions);
    }

    public void BuildCubemap(string[] imagesBase64, int resolution = 1024)
    {
        // Cria um cubemap vazio
        Cubemap cubemap = new Cubemap(resolution, TextureFormat.RGBA32, false);

        // As 6 faces do cubemap
        CubemapFace[] faces = new CubemapFace[]
        {
            CubemapFace.NegativeX, // esquerda
            CubemapFace.PositiveZ, // frente
            CubemapFace.PositiveX, // direita
            CubemapFace.NegativeZ, // trás
            CubemapFace.PositiveY, // cima
            CubemapFace.NegativeY, // baixo
        };

        for (int i = 0; i < imagesBase64.Length && i < 6; i++)
        {
            byte[] imgBytes = Convert.FromBase64String(imagesBase64[i]);

            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(imgBytes);

            // Corrige a inversão vertical
            tex = FlipTextureY(tex);

            if (i == 4)
                tex = RotateTexture(tex);
            if(i == 5)
                tex = RotateTexture(tex, false);

            // Copia pixels da textura para a face do cubemap
            Color[] pixels = tex.GetPixels();
            cubemap.SetPixels(pixels, faces[i]);
        }

        cubemap.Apply();

        result = cubemap;
        spinner.SetActive(false);
        description.text = "World generated!";
    }

    private void Visualize()
    {
        if (result != null)
        {
            SkyBoxController.Instance.ApplyCubemap(result);
            StateController.Instance.SetState(State.ChooseOptions);
        }
    }

    Texture2D RotateTexture(Texture2D original, bool clockwise = true)
    {
        Color32[] originalPixels = original.GetPixels32();
        Color32[] newPixels = new Color32[originalPixels.Length];

        int w = original.width;
        int h = original.height;

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                int newX = clockwise ? h - y - 1 : y;
                int newY = clockwise ? x : w - x - 1;
                newPixels[newY * h + newX] = originalPixels[y * w + x];
            }
        }

        Texture2D rotated = new Texture2D(h, w);
        rotated.SetPixels32(newPixels);
        rotated.Apply();
        return rotated;
    }

    private void NewAudio()
    {
        description.text = "Press Y to start recording...";
    }
    Texture2D FlipTextureY(Texture2D original)
    {
        Texture2D flipped = new Texture2D(original.width, original.height, original.format, false);

        int width = original.width;
        int height = original.height;

        for (int y = 0; y < height; y++)
        {
            flipped.SetPixels(0, y, width, 1, original.GetPixels(0, height - 1 - y, width, 1));
        }

        flipped.Apply();
        return flipped;
    }


    void SaveWav(string filepath, AudioClip clip)
    {
        if (clip == null) return;

        using (FileStream fileStream = new FileStream(filepath, FileMode.Create))
        {
            int sampleCount = clip.samples * clip.channels;
            float[] samples = new float[sampleCount];
            clip.GetData(samples, 0);

            // converte float [-1,1] para int16 PCM
            short[] intData = new short[sampleCount];
            byte[] bytesData = new byte[sampleCount * 2];

            int rescaleFactor = 32767;
            for (int i = 0; i < sampleCount; i++)
            {
                intData[i] = (short)(samples[i] * rescaleFactor);
                BitConverter.GetBytes(intData[i]).CopyTo(bytesData, i * 2);
            }

            // Cabeçalho WAV
            int sampleRate = clip.frequency;
            int channels = clip.channels;
            int byteRate = sampleRate * channels * 2;

            fileStream.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"), 0, 4);
            fileStream.Write(BitConverter.GetBytes(36 + bytesData.Length), 0, 4);
            fileStream.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"), 0, 4);
            fileStream.Write(System.Text.Encoding.UTF8.GetBytes("fmt "), 0, 4);
            fileStream.Write(BitConverter.GetBytes(16), 0, 4); // Subchunk1Size (16 for PCM)
            fileStream.Write(BitConverter.GetBytes((short)1), 0, 2); // AudioFormat = 1 (PCM)
            fileStream.Write(BitConverter.GetBytes((short)channels), 0, 2);
            fileStream.Write(BitConverter.GetBytes(sampleRate), 0, 4);
            fileStream.Write(BitConverter.GetBytes(byteRate), 0, 4);
            fileStream.Write(BitConverter.GetBytes((short)(channels * 2)), 0, 2); // BlockAlign
            fileStream.Write(BitConverter.GetBytes((short)16), 0, 2); // BitsPerSample
            fileStream.Write(System.Text.Encoding.UTF8.GetBytes("data"), 0, 4);
            fileStream.Write(BitConverter.GetBytes(bytesData.Length), 0, 4);

            // Dados de áudio
            fileStream.Write(bytesData, 0, bytesData.Length);
        }
    }

    [System.Serializable]
    public class PayloadAudio
    {
        public string audio_base64;
        public int session_id;
    }

    [System.Serializable]
    public class CubemapResponse
    {
        public string status;
        public string message;
        public string[] cubemap_images; // cada imagem vem em Base64
    }
}
