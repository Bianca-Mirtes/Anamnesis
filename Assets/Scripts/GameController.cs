using UnityEngine;

public class GameController : MonoBehaviour
{
    private static GameController _instance;

    // Singleton
    public static GameController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<GameController>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("GameController");
                    _instance = obj.AddComponent<GameController>();
                }
            }
            return _instance;
        }
    }

    private void Update()
    {
        switch (StateController.Instance.GetState()) {
            case State.ChooseWay:
                transform.GetChild(0).gameObject.SetActive(true);
                if((int)StateController.Instance.GetLastState() != (int)StateController.Instance.GetState())
                    transform.GetChild((int)StateController.Instance.GetLastState()).gameObject.SetActive(false);
                break;
            case State.ChooseImage:
                transform.GetChild(2).gameObject.SetActive(true);
                transform.GetChild((int)StateController.Instance.GetLastState()).gameObject.SetActive(false);
                break;
            case State.Recording:
                transform.GetChild(1).gameObject.SetActive(true);
                transform.GetChild((int)StateController.Instance.GetLastState()).gameObject.SetActive(false);
                break;
            case State.ChooseOptions:
                transform.GetChild(3).gameObject.SetActive(true);
                transform.GetChild((int)StateController.Instance.GetLastState()).gameObject.SetActive(false);
                break;
            case State.SettingPoints:
                transform.GetChild(4).gameObject.SetActive(true);
                transform.GetChild((int)StateController.Instance.GetLastState()).gameObject.SetActive(false);
                break;
            case State.ConsultingInventory:
                transform.GetChild(5).gameObject.SetActive(true);
                transform.GetChild((int)StateController.Instance.GetLastState()).gameObject.SetActive(false);
                break;
            default:
                break;
        }
    }
}
