using Screen;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class NewVncPanelController : MonoBehaviour
{
    [SerializeField] private TMP_InputField _hostInput;
    [SerializeField] private TMP_InputField _portInput;
    [SerializeField] private TMP_InputField _displayNumberInput;
    [SerializeField] private TMP_InputField _password;
    [SerializeField] private Button _create;
    private VncScreenController.Factory _vncScreenFactory;

    [Inject]
    public void Construct(VncScreenController.Factory vncScreenFactory)
    {
        _vncScreenFactory = vncScreenFactory;
    }

    private void Awake()
    {
        _create.onClick.AddListener(CreateBtnClickHandler);
    }

    private void CreateBtnClickHandler()
    {
        var newScreen = _vncScreenFactory.Create(new VncScreenController.CreationParameters(_hostInput.text, int.Parse(_portInput.text),
            int.Parse(_displayNumberInput.text), _password.text));
    }
}