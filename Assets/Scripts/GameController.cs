using UnityEngine;

public class GameController : MonoBehaviour
{
    private static GameController _instance;
    public int session_id { get; set; }

    public GameObject ObjectStorage;

    public int currentWay = -1;
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

    public void ChangeState(State state)
    {
        // Desativa todos os filhos (todos os canvases)
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(false);
        }

        // Ativa só o painel do estado atual
        transform.GetChild((int)state).gameObject.SetActive(true);

        // Atualiza o controlador de estados
        StateController.Instance.SetState(state);
    }

    public void SpawnObject3D()
    {

    }
}
