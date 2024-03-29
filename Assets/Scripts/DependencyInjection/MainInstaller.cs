using InputManagement;
using Screen;
using UnityEngine;
using Zenject;

namespace DependencyInjection
{
    public class MainInstaller : MonoInstaller
    {
        [SerializeField] private GameObject FloatingScreenPrefab;

        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<KeyboardAndMouseInput>().AsSingle();
            Container.BindInterfacesAndSelfTo<NrealcontrollerInput>().AsSingle();
            Container.BindFactory<VncScreenController.CreationParameters, VncScreenController, VncScreenController.Factory>()
                .FromComponentInNewPrefab(FloatingScreenPrefab).AsTransient();

            Container.BindInterfacesAndSelfTo<TestClick>().AsSingle().NonLazy();
        }
    }
}