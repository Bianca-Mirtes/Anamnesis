using UnityEngine;

public class StateController : MonoBehaviour
{
    public static StateController Instance { get; private set; }

    protected State stateNow = State.ChooseWay;
    protected State lastState = State.ChooseWay;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        GameController.Instance.ChangeState(State.ChooseWay);
    }

    public void SetState(State state)
    {
        lastState = stateNow;
        stateNow = state;

        Debug.Log("Estado anterior: " + lastState);
        Debug.Log("Estado atual: " + stateNow);
    }

    public State GetState() { return stateNow; }
    public State GetLastState() { return lastState; }

    public bool CompareStates(State state)
    {
        if (state.ToString() == stateNow.ToString()) 
            return true;
        else
            return false;
    }
}
