using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public class RecordingController : MonoBehaviour
{
    private string micDevice;
    private AudioClip recordedClip;
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
        if (Microphone.devices.Length > 0)
            micDevice = Microphone.devices[0];
        else
            Debug.LogError("Nenhum microfone encontrado!");
    }

    private void Update()
    {
        if (GameController.Instance.currentStep == 3 || GameController.Instance.currentStep == 0)
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
        recordedClip = Microphone.Start(micDevice, false, 45, 44100);
        description.gameObject.SetActive(false);
        spinner.SetActive(isRecording);
        Debug.Log("Gravação iniciada...");
    }

    void StopAndSave()
    {
        isRecording = false;
        Microphone.End(micDevice);
        description.text = "Recording ended. Saving audio file...";
        description.gameObject.SetActive(true);
        spinner.SetActive(isRecording);

        string filename = "Gravacao_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".wav";
        string filepath = Path.Combine(Application.persistentDataPath, filename);

        SaveWav(filepath, recordedClip);

        Debug.Log("Áudio salvo em: " + filepath);
    }

    private void ReturnStep()
    {
        GameController.Instance.ReturnStep();
    }

    private void SendAudio()
    {
        // logica da API
    }

    private void NewAudio()
    {
        description.text = "Press Y to start recording...";
    }

    void SaveWav(string filepath, AudioClip clip)
    {
        if (clip == null) return;

        Directory.CreateDirectory(Path.GetDirectoryName(filepath));

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
