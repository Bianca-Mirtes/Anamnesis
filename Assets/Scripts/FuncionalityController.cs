using UnityEngine;

public class FuncionalityController : MonoBehaviour
{
    public static FuncionalityController Instance { get; private set; }

    protected Funcionality currentFuncionality = Funcionality.NONE;

    private void Awake()
    {
        Instance = this;
    }
    public void SetFuncionality(Funcionality func)
    {
        currentFuncionality = func;
    }

    public Funcionality GetFuncionality() { return currentFuncionality; }

    public bool CompareStates(Funcionality func)
    {
        if (func.ToString() == currentFuncionality.ToString())
            return true;
        else
            return false;
    }
}
