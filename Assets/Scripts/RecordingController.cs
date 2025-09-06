using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
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
    private Button sendAudioBtn;
    private Button newAudioBtn;
    private Button returnBtn;

    [SerializeField] private GameObject objectC;

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
                StopAndSave();
            }
        }
    }

    public void SetAttributes(GameObject currentSp, TMP_Text desc, Button send, Button newAudio, Button back)
    {
        spinner = currentSp;
        description = desc;
        sendAudioBtn = send;
        newAudioBtn = newAudio;
        returnBtn = back;

        returnBtn.onClick.AddListener(ReturnStep);
        sendAudioBtn.onClick.AddListener(SendAudio);
        newAudioBtn.onClick.AddListener(NewAudio);
    }

    void StartRecording()
    {
        isRecording = true;
        recordedClip = Microphone.Start(micDevice, false, 40, 44100);
        description.gameObject.SetActive(false);
        spinner.SetActive(isRecording);
        Debug.Log("Gravação iniciada...");
    }

    void Stop()
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
    }

    void StopAndSave()
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

        Debug.Log("Tamanho Base64: " + base64Audio.Length);

        // envia para API
        StartCoroutine(SendToAPI(base64Audio));
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

        stream.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"), 0, 4);
        stream.Write(BitConverter.GetBytes(bytesData.Length + 36), 0, 4);
        stream.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"), 0, 4);
        stream.Write(System.Text.Encoding.ASCII.GetBytes("fmt "), 0, 4);
        stream.Write(BitConverter.GetBytes(16), 0, 4);
        stream.Write(BitConverter.GetBytes((short)1), 0, 2);
        stream.Write(BitConverter.GetBytes((short)channels), 0, 2);
        stream.Write(BitConverter.GetBytes(hz), 0, 4);
        stream.Write(BitConverter.GetBytes(byteRate), 0, 4);
        stream.Write(BitConverter.GetBytes((short)(channels * 2)), 0, 2);
        stream.Write(BitConverter.GetBytes((short)16), 0, 2);
        stream.Write(System.Text.Encoding.ASCII.GetBytes("data"), 0, 4);
        stream.Write(BitConverter.GetBytes(bytesData.Length), 0, 4);
        stream.Write(bytesData, 0, bytesData.Length);

        return stream.ToArray();
    }

    IEnumerator SendToAPI(string base64Audio)
    {
        string apiUrl = "https://suaapi.com/upload";
        WWWForm form = new WWWForm();
        form.AddField("audio", base64Audio);

        using (UnityWebRequest www = UnityWebRequest.Post(apiUrl, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
                Debug.LogError("Erro ao enviar áudio: " + www.error);
            else
                Debug.Log("✅ Áudio enviado com sucesso: " + www.downloadHandler.text);
        }
    }

    private void NewAudio()
    {
        description.text = "Press Y to start recording...";
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

        description.text = "Recording saved!";
    }

}
