using Screen;
using UnityEngine;
using Zenject;

public class GameManager : MonoBehaviour
{
    public VNCScreen.VNCScreen _screen;

    [Inject]
    public void Construct(VncScreenController.Factory factory)
    {
        var screen = factory.Create(new VncScreenController.CreationParameters("100.72.222.8", 5900, 1, "password123"));
        //_screen = screen._screen;
    }

    private void Start()
    {

    }
}
