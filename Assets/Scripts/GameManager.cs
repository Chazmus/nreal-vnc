using Screen;
using UnityEngine;
using Zenject;

public class GameManager : MonoBehaviour
{

    [Inject]
    public void Construct(VncScreenController.Factory factory)
    {
#if UNITY_EDITOR
        // Create a screen for debugging if running in the editor
        factory.Create(new VncScreenController.CreationParameters("100.72.222.8", 5900, 1, "password123"));
#endif
    }
}