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
        StateController.Instance.SetState(State.Recording);
    }
    private void RemoveObject() {
        StateController.Instance.SetState(State.SettingPoints);
    }

    private void CloneObject() {
        StateController.Instance.SetState(State.SettingPoints);
    }

    private void GetOfTheDatabase() {
        StateController.Instance.SetState(State.ConsultingInventory);
    }

    private void ReturnStep()
    {
        StateController.Instance.SetState(StateController.Instance.GetLastState());
    }

}
