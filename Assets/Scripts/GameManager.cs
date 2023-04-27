using Screen;
using UnityEngine;
using Zenject;

public class GameManager : MonoBehaviour
{

    [Inject]
    public void Construct(VncScreenController.Factory factory)
    {

        // TODO if unity editor disabled for now for testing purposes
// #if UNITY_EDITOR
        // Create a screen for debugging if running in the editor
        factory.Create(new VncScreenController.CreationParameters("localhost", 5900, 0, ""));
// #endif
    }
}