using UnityEngine;
using UnityEngine.UI;

public class QuitButtonPresenter : MonoBehaviour
{
    [SerializeField] private Button button;

    private void Awake()
    {
        button.onClick.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        ClientRunner.Quit();
    }
}
