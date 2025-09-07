using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChooseImageController : MonoBehaviour
{
    [Header("Categories")]
    [SerializeField] private Button nextButtonC;
    [SerializeField] private Button previousButtonC;
    [SerializeField] private Category[] categories;

    [Header("Images")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button currentimage;

    [SerializeField] private GameObject card;
    [SerializeField] private TextMeshProUGUI categoryText;
    [SerializeField] private Button backBtn;

    private Category currentCategory;

    private int currentIndex = 0;
    private int currentCategoryIndex = 0;
    // Start is called before the first frame update
    void Start()
    {
        nextButton.onClick.AddListener(NextCard);
        previousButton.onClick.AddListener(PreviousCard);
        nextButtonC.onClick.AddListener(NextCategory);
        previousButtonC.onClick.AddListener(PreviousCategory);

        backBtn.onClick.AddListener(ReturnStep);
        currentimage.onClick.AddListener(SetSkybox);

        currentCategory = categories[0];
        categoryText.text = currentCategory.name;
        card.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = currentCategory.images[currentIndex];
        card.transform.GetChild(1).GetChild(currentIndex).GetChild(0).gameObject.SetActive(true);

        int count = card.transform.GetChild(1).childCount;
        for (int ii = 1; ii < count; ii++)
        {
            card.transform.GetChild(1).GetChild(ii).GetChild(0).gameObject.SetActive(false);
        }
    }

    private void NextCategory()
    {
        currentCategoryIndex++;
        if (currentCategoryIndex == categories.Length)
            currentCategoryIndex = categories.Length - 1;
        currentCategory = categories[currentCategoryIndex];

        currentIndex = 0;
        categoryText.text = currentCategory.name;
        card.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = currentCategory.images[currentIndex];
        card.transform.GetChild(1).GetChild(currentIndex).GetChild(0).gameObject.SetActive(true);
        card.transform.GetChild(1).GetChild(1).GetChild(0).gameObject.SetActive(false);
        card.transform.GetChild(1).GetChild(2).GetChild(0).gameObject.SetActive(false);
    }

    private void PreviousCategory()
    {
        currentCategoryIndex--;
        if (currentCategoryIndex < 0)
            currentCategoryIndex = 0;

        currentCategory = categories[currentCategoryIndex];

        currentIndex = 0;
        categoryText.text = currentCategory.name;
        card.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = currentCategory.images[currentIndex];
        card.transform.GetChild(1).GetChild(currentIndex).GetChild(0).gameObject.SetActive(true);
        card.transform.GetChild(1).GetChild(1).GetChild(0).gameObject.SetActive(false);
        card.transform.GetChild(1).GetChild(2).GetChild(0).gameObject.SetActive(false);
    }

    private void NextCard()
    {
        currentIndex++;

        if (currentIndex >= currentCategory.images.Length)
            currentIndex = currentCategory.images.Length-1;
        card.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = currentCategory.images[currentIndex];

        card.transform.GetChild(1).GetChild(currentIndex-1).GetChild(0).gameObject.SetActive(false);
        card.transform.GetChild(1).GetChild(currentIndex).GetChild(0).gameObject.SetActive(true);
    }

    private void PreviousCard()
    {
        currentIndex--;

        if (currentIndex < 0)
            currentIndex = 0;

        card.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = currentCategory.images[currentIndex];

        card.transform.GetChild(1).GetChild(currentIndex + 1).GetChild(0).gameObject.SetActive(false);
        card.transform.GetChild(1).GetChild(currentIndex).GetChild(0).gameObject.SetActive(true);
    }

    private void SetSkybox()
    {
        //SkyBoxController.Instance.SetSkybox(currentIndex);
        StateController.Instance.SetState(State.ChooseOptions);
    }

    private void ReturnStep()
    {
        StateController.Instance.SetState(StateController.Instance.GetLastState());
    }
}
