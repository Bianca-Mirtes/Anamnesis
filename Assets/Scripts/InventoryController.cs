using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using GLTFast;
using System.Threading.Tasks;

public class InventoryController : MonoBehaviour
{
    [SerializeField] private Button returnBtn;
    [SerializeField] private Transform content;
    [SerializeField] private GameObject prefab;

    private Dictionary<Sprite, GameObject> objects;
    private int count=0;

    void Start()
    {
        returnBtn.onClick.AddListener(ReturnStep);
    }

    private void ReturnStep()
    {
        GameController.Instance.ChangeState(StateController.Instance.GetLastState());
        FuncionalityController.Instance.SetFuncionality(Funcionality.NONE);
    }

    public async void AddObject(string image_base64, string glb_base64)
    {
        byte[] glbBytes = Convert.FromBase64String(glb_base64);

        await SpawnGlbBytes(glbBytes);

        // 2. Caminho de saída no disco
        //string path = Path.Combine(Application.persistentDataPath, "modelo.glb");

        byte[] imgBytes = Convert.FromBase64String(image_base64);
        string path2 = Path.Combine(Application.persistentDataPath, "modelo" + count +".png");

        // 3. Salva como .png
        File.WriteAllBytes(path2, imgBytes);

        // 2. Cria uma textura
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(imgBytes);

        // 3. Cria um Sprite a partir da textura
        Sprite sprite = Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f)
        );

        GameObject obj = Instantiate(prefab, content);
        obj.GetComponent<Image>().sprite = sprite;

        objects.Add(sprite, obj);
    }

    /// <summary>
    /// Usa glTFast para carregar GLB a partir de bytes e instanciar na cena.
    /// </summary>
    public async Task SpawnGlbBytes(byte[] glbBytes)
    {
        var gltf = new GltfImport();

        // Assinatura correta do glTFast
        var success = await gltf.Load(glbBytes, null, new ImportSettings());
        if (!success)
        {
            Debug.LogError("[GlbSpawnFromBase64] glTFast falhou ao carregar o GLB.");
            return;
        }

        bool instantiated = await gltf.InstantiateMainSceneAsync(GameController.Instance.ObjectStorage.transform);
        if (!instantiated)
        {
            Debug.LogError("[GlbSpawnFromBase64] Falha ao instanciar cena principal do GLB.");
            return;
        }
    }

    private void OnClick(GameObject model)
    {
        Instantiate(model, content);
    }

    [System.Serializable]
    public class ZipResponse
    {
        public string zipFileBase64;
    }
}
