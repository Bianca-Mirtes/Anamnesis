using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class InventoryController : MonoBehaviour
{
    [SerializeField] private Button returnBtn;
    [SerializeField] private Transform content;
    [SerializeField] private GameObject prefab;

    private Dictionary<Sprite, GameObject> objects;
    private int count=0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        returnBtn.onClick.AddListener(ReturnStep);
    }

    private void ReturnStep()
    {
        StateController.Instance.SetState(StateController.Instance.GetLastState());
    }

    public void AddObject(string image_base64, string glb_base64)
    {

        byte[] glbBytes = Convert.FromBase64String(glb_base64);

        // 2. Caminho de saída no disco
        string path = Path.Combine(Application.persistentDataPath, "modelo.glb");

        // 3. Salva como .glb
        File.WriteAllBytes(path, glbBytes);

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

        //obj.GetComponent<Button>().onClick.AddListener(()=>OnClick());
    }

    private void OnClick(GameObject model)
    {

    }
}
