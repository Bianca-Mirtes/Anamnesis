using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ObjectManipulationController : MonoBehaviour
{
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private GameObject panel;

    [SerializeField] private Button frontButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button upButton;
    [SerializeField] private Button downButton;


    private GameObject obj;
    // Start is called before the first frame update
    void Start()
    {
        leftButton.onClick.AddListener(RotateLeft);
        rightButton.onClick.AddListener(RotateRight);

        backButton.onClick.AddListener(MoveLeft);
        frontButton.onClick.AddListener(MoveRight);
        upButton.onClick.AddListener(MoveUp);
        downButton.onClick.AddListener(MoveDown);
        DisableCanvas();
    }

    public void SetObject(GameObject objC)
    {
        obj = objC;
    }

    public void DisableCanvas()
    {
        panel.SetActive(false);
    }
    public void EnableCanvas()
    {
        panel.SetActive(true);
    }

    private void MoveLeft()
    {
        if (obj != null)
        {
            Vector3 newPos = obj.transform.position + new Vector3(0, 0, -1);
            if(newPos.z >-4)
            {
                obj.transform.position += new Vector3(0,0, -1);
            }
            else
            {
                obj.transform.position = new Vector3(obj.transform.position.x, 0, -4);
            }
        }
    }

    private void MoveRight()
    {
        if (obj != null)
        {
            Vector3 newPos = obj.transform.position + new Vector3(0, 0, 1);
            if (newPos.z < 4)
            {
                obj.transform.position += new Vector3(0, 0, 1);
            }
            else
            {
                obj.transform.position = new Vector3(obj.transform.position.x, 0, 4);
            }
        }
    }

    private void MoveUp()
    {
        if (obj != null)
        {
            Vector3 newPos = obj.transform.position + new Vector3(-1, 0, 0);
            if (newPos.z > -1)
            {
                obj.transform.position += new Vector3(-1, 0, 0);
            }
            else
            {
                obj.transform.position = new Vector3(-1, 0, obj.transform.position.z);
            }
        }
    }
    private void MoveDown()
    {
        if (obj != null)
        {
            Vector3 newPos = obj.transform.position + new Vector3(1, 0, 0);
            if (newPos.z < 6)
            {
                obj.transform.position += new Vector3(1, 0, 0);
            }
            else
            {
                obj.transform.position = new Vector3(6, 0, obj.transform.position.z);
            }
        }
    }

    private void RotateRight()
    {
        if (obj != null)
            obj.transform.Rotate(0, -20, 0);
    }

    private void RotateLeft()
    {
        if (obj != null)
            obj.transform.Rotate(0, 20, 0);
    }
}
