using UnityEngine;
using UnityEngine.UI;

public class ShowConsoleButtonPresenter : MonoBehaviour
{
    [SerializeField] private Button button;

    private void Awake()
    {
        button.onClick.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        ConsoleActivator.Show(false);
    }
}
