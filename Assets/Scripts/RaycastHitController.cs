using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class RaycastHitController : MonoBehaviour
{
    [SerializeField] private XRRayInteractor rayInteractor;
    [SerializeField] private GameObject canvas;

    private Vector3 spawnPos;
    private Quaternion spawnmRot;
    private List<InputDevice> devices = new List<InputDevice>();
    private bool canPress = true;

    private static RaycastHitController _instance;

    // Singleton
    public static RaycastHitController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<RaycastHitController>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("RayCastHitController");
                    _instance = obj.AddComponent<RaycastHitController>();
                }
            }
            return _instance;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (IsTriggerPressed())
        {
            if (canPress)
            {
                if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
                {
                    spawnPos = hit.point;
                    spawnmRot = Quaternion.LookRotation(hit.normal);

                    canPress = false;
                    canvas.SetActive(false);
                }
            }
        }
    }

    public Vector3 GetSpawnPos()
    {
        return spawnPos;
    }

    public Quaternion GetSpawnRot()
    {
        return spawnmRot;
    }

    public void ShowCanvas()
    {
        canvas.SetActive(true);
        canPress = true;
    }

    private bool IsTriggerPressed()
    {
#if UNITY_ANDROID
        InputDeviceCharacteristics leftHandCharacteristics = InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller;
        InputDevices.GetDevicesWithCharacteristics(leftHandCharacteristics, devices);
        devices[0].TryGetFeatureValue(CommonUsages.secondaryButton, out bool YButton);
        if (YButton)
            return true;
        return false;
#elif UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.M))
            return true;
        return false;
#endif
    }
}
