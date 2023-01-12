using System;
using NRKernal;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace InputManagement
{
    public class VirtualControllerManager : MonoBehaviour
    {
        private NrealcontrollerInput _nrealcontrollerInput;
        private MultiScreenController _multiScreenController;
        [SerializeField] private GameObject _baseControllerPanel;
        [SerializeField] private GameObject _customControllerPanel;

        [SerializeField] private Button _customPanelBackBtn;
        [SerializeField] private Button _basePanelAppBtn;

        [Inject]
        public void Construct(NrealcontrollerInput nrealcontrollerInput)
        {
            _nrealcontrollerInput = nrealcontrollerInput;
        }

        private void Awake()
        {
            _customPanelBackBtn.onClick.AddListener(handleCustomPanelBackBtnClick);
            _basePanelAppBtn.onClick.AddListener(handleAppBtnClick);
        }

        private void handleCustomPanelBackBtnClick()
        {
            toggleCustomPanel(false);
        }

        private void handleAppBtnClick()
        {
            toggleCustomPanel(true);
        }

        private void toggleCustomPanel(bool enableCustomPanel)
        {
            _baseControllerPanel.SetActive(!enableCustomPanel);
            _customControllerPanel.SetActive(enableCustomPanel);
        }
    }
}