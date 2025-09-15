using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class SkyBoxController : MonoBehaviour
{
    [SerializeField] private Material skyboxMaterial;
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

    private Texture2D RotateTexture(Texture2D original, bool clockwise = true)
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

    public Texture2D FlipTextureY(Texture2D original)
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

    public Cubemap BuildCubemap(string[] imagesBase64, List<CubemapFace> face,  List<string> newImage, int resolution = 1024)
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

        if (face.Count == 1)
        {
            for (int i = 0; i < imagesBase64.Length && i < 6; i++)
            {
                byte[] imgBytes;
                if (faces[i] == face[0])
                    imgBytes = Convert.FromBase64String(newImage[0]);
                else
                    imgBytes = Convert.FromBase64String(imagesBase64[i]);

                Texture2D tex = new Texture2D(2, 2);
                tex.LoadImage(imgBytes);

                // Corrige a inversão vertical
                tex = FlipTextureY(tex);

                // Copia pixels da textura para a face do cubemap
                Color[] pixels = tex.GetPixels();
                cubemap.SetPixels(pixels, faces[i]);
            }
        }
        else {
            for (int i = 0; i < imagesBase64.Length && i < 6; i++)
            {
                byte[] imgBytes;
                for (int jj = 0; jj < face.Count; jj++)
                {
                    if (face[jj] == faces[i])
                        imgBytes = Convert.FromBase64String(newImage[jj]);
                    else
                        imgBytes = Convert.FromBase64String(imagesBase64[i]);

                    Texture2D tex = new Texture2D(2, 2);
                    tex.LoadImage(imgBytes);

                    // Corrige a inversão vertical
                    tex = FlipTextureY(tex);

                    // Copia pixels da textura para a face do cubemap
                    Color[] pixels = tex.GetPixels();
                    cubemap.SetPixels(pixels, faces[i]);
                }
            }
        }

        cubemap.Apply();

        FindFirstObjectByType<SettingPointsController>().SetCubemap(cubemap);
        return cubemap;
    }

    public Cubemap BuildCubemap(string[] imagesBase64, int resolution = 1024)
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

            if (FuncionalityController.Instance.GetFuncionality() == Funcionality.WORLD_GENERATION)
            {
                if (i == 4)
                    tex = RotateTexture(tex);

                if (i == 5)
                    tex = RotateTexture(tex, false);
            }

            // Copia pixels da textura para a face do cubemap
            Color[] pixels = tex.GetPixels();
            cubemap.SetPixels(pixels, faces[i]);
        }

        cubemap.Apply();

        FindFirstObjectByType<SettingPointsController>().SetCubemap(cubemap);
        return cubemap;
    }

    public void ApplyCubemap(Cubemap cubemap)
    {
        skyboxMaterial.SetTexture("_Tex", cubemap);
        RenderSettings.skybox = skyboxMaterial;
        DynamicGI.UpdateEnvironment(); // atualiza iluminação global
    }

    public void ResetExp()
    {
        skyboxMaterial.SetTexture("_Tex", defaultCubmap);
        RenderSettings.skybox = skyboxMaterial;
        DynamicGI.UpdateEnvironment(); // atualiza iluminação global
    }

    private void OnApplicationQuit()
    {
        ResetExp();
    }
}
