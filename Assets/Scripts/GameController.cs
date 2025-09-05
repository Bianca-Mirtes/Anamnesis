using UnityEngine;

public class GameController : MonoBehaviour
{
    public int currentStep = 0;
    public int lastStep = 0;

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
        transform.GetChild(currentStep).gameObject.SetActive(false);
        transform.GetChild(lastStep).gameObject.SetActive(true);
        currentStep = lastStep;

        if (currentStep == 0)
        {
            SkyBoxController.Instance.ResetExp();
            lastStep = 0;   
        }

    }

    public void NextStep()
    {
        lastStep = currentStep;
        currentStep++;
        transform.GetChild(currentStep).gameObject.SetActive(true);
        transform.GetChild(lastStep).gameObject.SetActive(false);
    }

    public void NextStep(int index)
    {
        lastStep = currentStep;
        currentStep = index;

        transform.GetChild(currentStep).gameObject.SetActive(true);
        transform.GetChild(lastStep).gameObject.SetActive(false);
    }
}
