using UnityEngine;

public class GameController : MonoBehaviour
{
    public int currentStep = 0;

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

    public void ReturnStep()
    {
        currentStep--;
        transform.GetChild(currentStep).gameObject.SetActive(true);
        transform.GetChild(currentStep+1).gameObject.SetActive(false);
    }

    public void NextStep()
    {
        currentStep++;
        transform.GetChild(currentStep).gameObject.SetActive(true);
        transform.GetChild(currentStep-1).gameObject.SetActive(false);
    }

    public void NextStep(int step, int previousStep)
    {
        currentStep = step;
        transform.GetChild(currentStep).gameObject.SetActive(true);
        transform.GetChild(previousStep).gameObject.SetActive(false);
    }
}
