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
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Image = UnityEngine.UI.Image;

public class InventoryController : MonoBehaviour
{
    [Header("Inventary")]
    [SerializeField] private Button returnBtn;
    [SerializeField] private Transform content;
    [SerializeField] private GameObject prefab;
    [SerializeField] private Slider size;
    [SerializeField] private GameObject ObjectStorage;
    private bool wasSpawned = false;

    [Header("Object Manipulation")]
    [SerializeField] private Button deleteObj;
    [SerializeField] private GameObject panel;
    private GameObject currentObj = null;
    private GameObject selectedObject = null;

    private Dictionary<Sprite, (GameObject, GameObject)> objects = new Dictionary<Sprite, (GameObject, GameObject)>();
    private int count=0;

    void Start()
    {
        returnBtn.onClick.AddListener(ReturnStep);
        deleteObj.onClick.AddListener(DeleteSelectedObject);
        size.onValueChanged.AddListener(SetSize);
    }

    private void SetSize(float value)
    {
        if (selectedObject!= null)
        {
            selectedObject.transform.localScale = Vector3.one*value;
            Debug.Log("Tamanho alterado!");
        }
        else
        {
            Debug.Log("Nenhum objeto selecionado para alterar o tamanho.");
        }
    }

    public GameObject GetStorage()
    {
        return ObjectStorage;
    }

    public void DisableCanvas()
    {
        panel.SetActive(false);
    }
    public void EnableCanvas()
    {
        panel.SetActive(true);
    }

    private void ReturnStep()
    {
        GameController.Instance.ChangeState(StateController.Instance.GetLastState());
        FuncionalityController.Instance.SetFuncionality(Funcionality.NONE);
        DisableCanvas();
    }

    public async void AddObject(string image_base64, string glb_base64)
    {
        byte[] glbBytes = Convert.FromBase64String(glb_base64);

        await SpawnGlbBytes(glbBytes);

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

        EnableCanvas();
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

        // --- FIND THE MESH GAMEOBJECT (assume a single mesh)
        var meshFilter = root.GetComponentInChildren<MeshFilter>();
        if (meshFilter == null)
        {
            Debug.LogError("[GlbSpawnFromBase64] N�o foi encontrado MeshFilter no GLB instanciado.");
            return;
        }

        var meshGO = meshFilter.gameObject;

        // --- RIGIDBODY (adiciona antes de usar)
        var rb =  meshGO.AddComponent<Rigidbody>();
        rb.isKinematic = true;      // padr�o: n�o cair enquanto n�o agarrado
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // --- MESH COLLIDER (assegura que sharedMesh est� apontando)
        var mc = meshGO.AddComponent<MeshCollider>();
        mc.sharedMesh = meshFilter.sharedMesh;
        // OBS: MeshCollider.convex � necess�rio se houver Rigidbody din�mico; se seu mesh for muito complexo, pode falhar
        mc.convex = true;

        // --- XRGrabInteractable (no MESMO GameObject que o collider + rigidbody)
        var grab = meshGO.AddComponent<XRGrabInteractable>();

        // OPTIONAL: ajustar o attach transform para o centro do mesh (melhora comportamento do grab)
        var attach = new GameObject("Attach");
        attach.transform.SetParent(meshGO.transform, false);
        attach.transform.localPosition = meshFilter.sharedMesh.bounds.center;
        grab.attachTransform = attach.transform;

        root.transform.parent = ObjectStorage.transform;
        root.transform.localPosition = Vector3.zero;

        grab.trackScale = false;

        grab.selectEntered.AddListener((SelectEnterEventArgs args) =>
        {
            rb.isKinematic = false;
            IXRSelectInteractable interactable = args.interactableObject;
            if (interactable != null)
            {
                selectedObject = interactable.transform.gameObject;
            }
        });

        // Ao soltar -> "congelar" novamente e zerar velocidades (evita que caia/escape)
        grab.selectExited.AddListener((SelectExitEventArgs args) =>
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            selectedObject = null;
        });

        currentObj = root;
    }

    // Chame esse método quando o botão deletar for pressionado
    public void DeleteSelectedObject()
    {
        if (selectedObject != null)
        {
            Destroy(selectedObject);
            selectedObject = null;
            Debug.Log("Objeto destruído.");
        }
        else
        {
            Debug.Log("Nenhum objeto selecionado para destruir.");
        }
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
            model3D.transform.localPosition = Vector3.zero;
            currentObj = model3D;
            EnableCanvas();
            wasSpawned = true;
            Invoke("ResetSpawn", 4f);
        }
    }
}
