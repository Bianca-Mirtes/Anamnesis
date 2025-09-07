using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public class ObjectRecordingController : MonoBehaviour
{
    [SerializeField] private GameObject spinner;
    [SerializeField] private TMP_Text description;
    [SerializeField] private Button sendAudioBtn;
    [SerializeField] private Button newAudioBtn;
    [SerializeField] private Button returnBtn;
    [SerializeField] private Button visualizeBtn;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RecordingController.Instance.SetAttributes(spinner, description, sendAudioBtn, newAudioBtn, returnBtn, visualizeBtn);
    }

    private void OnEnable()
    {
        RecordingController.Instance.SetAttributes(spinner, description, sendAudioBtn, newAudioBtn, returnBtn, visualizeBtn);
    }
}
