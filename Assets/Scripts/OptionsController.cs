using UnityEngine;
using UnityEngine.UI;

public class OptionsController : MonoBehaviour
{
    [SerializeField] private Button addtBtn;
    [SerializeField] private Button removeBtn;
    [SerializeField] private Button cloneBtn;
    [SerializeField] private Button getdbBtn;
    [SerializeField] private Button returnBtn;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        addtBtn.onClick.AddListener(AddObject);
        removeBtn.onClick.AddListener(RemoveObject);
        cloneBtn.onClick.AddListener(CloneObject);
        getdbBtn.onClick.AddListener(GetOfTheDatabase);
        returnBtn.onClick.AddListener(ReturnStep);
    }

    private void AddObject() {
        GameController.Instance.NextStep();
    }
    private void RemoveObject() {
        GameController.Instance.NextStep(3);
    }

    private void CloneObject() {
        GameController.Instance.NextStep();
    }

    private void GetOfTheDatabase() {
        GameController.Instance.NextStep(4);
    }

    private void ReturnStep()
    {
        GameController.Instance.ReturnStep();
    }

}
