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
            Container.BindInterfacesAndSelfTo<NrealcontrollerInput>().AsSingle();
            Container.BindFactory<Object, FloatingScreen, FloatingScreen.Factory>().FromFactory<PrefabFactory<FloatingScreen>>();
        }
    }
}