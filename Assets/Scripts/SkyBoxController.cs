using System;
using UnityEngine;

public class SkyBoxController : MonoBehaviour
{
    [SerializeField] private Material skyboxMaterial;
    [SerializeField] private Cubemap[] cubemaps;
    [SerializeField] private Cubemap defaultCubmap;
    private static SkyBoxController _instance;

    // Singleton
    public static SkyBoxController Instance
    {
        get {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<SkyBoxController>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("SkyBoxController");
                    _instance = obj.AddComponent<SkyBoxController>();
                }
            }
            return _instance;
        }
    }

    public void SetSkybox(int index)
    {
        Cubemap cubemap = cubemaps[index];

        skyboxMaterial.SetTexture("_Tex", cubemap);
        RenderSettings.skybox = skyboxMaterial;
    }
    public void ResetExp()
    {
        skyboxMaterial.SetTexture("_Tex", defaultCubmap);
        RenderSettings.skybox = skyboxMaterial;
    }

}
