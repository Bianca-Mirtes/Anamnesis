using UnityEngine;
using UnityEngine.UI;

public class InventoryController : MonoBehaviour
{
    [SerializeField] private Button returnBtn;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        returnBtn.onClick.AddListener(ReturnStep);
    }

    private void ReturnStep()
    {
        StateController.Instance.SetState(StateController.Instance.GetLastState());
    }
}
