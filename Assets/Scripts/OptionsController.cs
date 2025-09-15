using UnityEngine;
using UnityEngine.UI;

public class OptionsController : MonoBehaviour
{
    [SerializeField] private Button addtBtn;
    [SerializeField] private Button removeBtn;
    [SerializeField] private Button cloneBtn;
    [SerializeField] private Button getdbBtn;
    [SerializeField] private Button returnBtn;

    private bool stateChanged = false;

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
        if (!stateChanged)
        {
            FuncionalityController.Instance.SetFuncionality(Funcionality.ADD);
            GameController.Instance.ChangeState(State.Recording);
            stateChanged = true;
            Invoke("ResetChoice", 3f);
        }
    }
    private void RemoveObject() {
        if (!stateChanged)
        {
            FuncionalityController.Instance.SetFuncionality(Funcionality.REMOVE);
            GameController.Instance.ChangeState(State.SettingPoints);
            stateChanged = true;
            Invoke("ResetChoice", 3f);
        }
    }

    private void CloneObject() {
        if (!stateChanged)
        {
            FuncionalityController.Instance.SetFuncionality(Funcionality.CLONE);
            GameController.Instance.ChangeState(State.SettingPoints);
            stateChanged = true;
            Invoke("ResetChoice", 3f);
        }
    }

    private void GetOfTheDatabase() {
        if (!stateChanged)
        {
            FuncionalityController.Instance.SetFuncionality(Funcionality.INVENTORY);
            GameController.Instance.ChangeState(State.ConsultingInventory);
            stateChanged = true;
            Invoke("ResetChoice", 3f);
        }
    }

    private void ReturnStep()
    {
        if (GameController.Instance.currentWay == 0)
            GameController.Instance.ChangeState(State.ChooseWay);
        else
            GameController.Instance.ChangeState(State.ChooseImage);
        FuncionalityController.Instance.SetFuncionality(Funcionality.NONE);
        SkyBoxController.Instance.ResetExp();
    }

    private void ResetChoice()
    {
        stateChanged = false;
    }

}
