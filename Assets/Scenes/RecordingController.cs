using System;
using System.IO;
using UnityEngine;

public class RecordingController : MonoBehaviour
{
    public KeyCode recordKey = KeyCode.JoystickButton1; // substitua pelo botão VR
    private AudioClip recordedClip;
    private string micDevice;
    private bool isRecording = false;

    void Start()
    {
        if (Microphone.devices.Length > 0)
            micDevice = Microphone.devices[0];
        else
            Debug.LogError("Nenhum microfone encontrado!");
    }

    // Update is called once per frame
    void Update()
    {
        if (GameController.Instance.currentStep == 2)
        {
            if (Input.GetKeyDown(recordKey) && !isRecording)
                StartRecording();

            if (Input.GetKeyUp(recordKey) && isRecording)
                StopAndSave();
        }
    }

    void StartRecording()
    {
        isRecording = true;
        recordedClip = Microphone.Start(micDevice, false, 45, 44100);
        Debug.Log("Gravação iniciada...");
    }

    void StopAndSave()
    {
        isRecording = false;
        Microphone.End(micDevice);
        Debug.Log("Gravação finalizada. Salvando arquivo...");

        string filename = "Gravacao_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".wav";
        string filepath = Path.Combine(Application.persistentDataPath, filename);

        SaveWav(filepath, recordedClip);

        Debug.Log("Áudio salvo em: " + filepath);
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
    }
}
