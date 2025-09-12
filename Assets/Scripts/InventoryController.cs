using GLTFast;
using GLTFast.Schema;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

public class InventoryController : MonoBehaviour
{
    [SerializeField] private Button returnBtn;
    [SerializeField] private Transform content;
    [SerializeField] private GameObject prefab;
    [SerializeField] private GameObject ObjectStorage;
    private GameObject currentObj = null;
    private bool wasSpawned = false;

    private Dictionary<Sprite, (GameObject, GameObject)> objects = new Dictionary<Sprite, (GameObject, GameObject)>();
    private int count=0;

    void Start()
    {
        returnBtn.onClick.AddListener(ReturnStep);
    }

    public GameObject GetStorage()
    {
        return ObjectStorage;
    }

    private void ReturnStep()
    {
        GameController.Instance.ChangeState(StateController.Instance.GetLastState());
        FuncionalityController.Instance.SetFuncionality(Funcionality.NONE);

        for (int ii = 0; ii < ObjectStorage.transform.childCount; ii++)
        {
            Destroy(ObjectStorage.transform.GetChild(ii).gameObject);
        }
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
        (GameObject, GameObject) pair;
        pair.Item1 = obj;
        pair.Item2 = currentObj;

        obj.GetComponent<Button>().onClick.AddListener(() => OnClick(pair.Item2));
        objects.Add(sprite, pair);

        FindFirstObjectByType<ObjectManipulationController>().SetObject(currentObj);
        FindFirstObjectByType<ObjectManipulationController>().EnableCanvas();
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

        var root = new GameObject("GLB_Root");
        root.tag = "Model3D";

        bool instantiated = await gltf.InstantiateMainSceneAsync(root.transform);
        if (!instantiated)
        {
            Debug.LogError("[GlbSpawnFromBase64] Falha ao instanciar cena principal do GLB.");
            return;
        }
        currentObj = root;
        root.transform.parent = ObjectStorage.transform;
        root.transform.localPosition = Vector3.zero;
    }

    private void ResetSpawn()
    {
        wasSpawned = false;
    }

    private void OnClick(GameObject model)
    {
        if (!wasSpawned)
        {
            GameObject model3D = Instantiate(model, ObjectStorage.transform);
            currentObj = model3D;
            model3D.transform.localPosition = Vector3.zero;
            wasSpawned = true;
            Invoke("ResetSpawn", 3f);
        }
    }
}
