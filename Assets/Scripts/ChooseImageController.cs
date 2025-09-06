using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChooseImageController : MonoBehaviour
{
    [SerializeField] private Button nextButton;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button backBtn;
    [SerializeField] private Button chooseBtn;

    [SerializeField] private GameObject card;

    [SerializeField] private Sprite[] images;

    private int currentIndex = 0;
    // Start is called before the first frame update
    void Start()
    {
        nextButton.onClick.AddListener(NextCard);
        previousButton.onClick.AddListener(PreviousCard);
        chooseBtn.onClick.AddListener(SetSkybox);
        backBtn.onClick.AddListener(ReturnStep);

        // inicializa o card inicial
        card.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = images[currentIndex];
        card.transform.GetChild(1).GetChild(currentIndex).GetChild(0).gameObject.SetActive(true);

        int count = card.transform.GetChild(1).childCount;
        for (int ii = 1; ii < count; ii++)
        {
            card.transform.GetChild(1).GetChild(ii).GetChild(0).gameObject.SetActive(false);
        }

    }

    private void NextCard()
    {
        currentIndex++;

        if (currentIndex >= images.Length)
            currentIndex = images.Length-1;
        card.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = images[currentIndex];

        card.transform.GetChild(1).GetChild(currentIndex-1).GetChild(0).gameObject.SetActive(false);
        card.transform.GetChild(1).GetChild(currentIndex).GetChild(0).gameObject.SetActive(true);
    }

    private void PreviousCard()
    {
        currentIndex--;

        if (currentIndex < 0)
            currentIndex = 0;

        card.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = images[currentIndex];

        card.transform.GetChild(1).GetChild(currentIndex+1).GetChild(0).gameObject.SetActive(false);
        card.transform.GetChild(1).GetChild(currentIndex).GetChild(0).gameObject.SetActive(true);
    }

    private void SetSkybox()
    {
        SkyBoxController.Instance.SetSkybox(currentIndex);
        StateController.Instance.SetState(State.Recording);
    }

    private void ReturnStep()
    {
        StateController.Instance.SetState(StateController.Instance.GetLastState());
    }
}
