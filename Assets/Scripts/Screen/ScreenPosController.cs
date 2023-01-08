using InputManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

namespace Screen
{
    [RequireComponent(typeof(MeshCollider), typeof(MeshFilter), typeof(MeshRenderer))]
    public class ScreenPosController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;

        private Vector3 _screenPoint;
        private Vector3 _offset;

        [SerializeField] private Material _defaultMaterial;
        [SerializeField] private Material _selectedMaterial;
        private Transform _parent;
        private bool _isDragging;
        private Quaternion _previousControllerRotation;
        private NrealcontrollerInput _nrealcontrollerInput;

        [Inject]
        public void Construct(NrealcontrollerInput nrealcontrollerInput)
        {
            _nrealcontrollerInput = nrealcontrollerInput;
        }

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshCollider = GetComponent<MeshCollider>();
            _parent = transform.parent;
        }

        private void Update()
        {
            if (_isDragging)
            {
                // Move screen around according to how the controller is moved
                var currentControllerRotation = _nrealcontrollerInput.Rotation;
                var angleBetween = Quaternion.Angle(currentControllerRotation, _previousControllerRotation);
                var axisBetween = GetRotationAxis(_previousControllerRotation, currentControllerRotation);
                _parent.RotateAround(_nrealcontrollerInput.Position, axisBetween, angleBetween);
                var currentControllerPosition = _nrealcontrollerInput.Position;

                // Adjust rotation so that the screen is always facing the camera exactly
                _parent.rotation.SetLookRotation(_parent.position - currentControllerPosition);

                // Update previous rotation value with current
                _previousControllerRotation = currentControllerRotation;

                // Update position and scale using the delta touch (touchpad drag)
                var deltaTouch = _nrealcontrollerInput.DeltaTouch;
                var scale = deltaTouch.x;
                var distance = deltaTouch.y;
                _parent.localScale *= (1 + scale);
                _parent.Translate(new Vector3(0,0,distance), _nrealcontrollerInput.CameraCenter);

            }
        }

        private static Vector3 GetRotationAxis(Quaternion q1, Quaternion q2)
        {
            // Get the difference between the two quaternions
            var q = Quaternion.Inverse(q1) * q2;

            // If the quaternion is a rotation quaternion, its axis of rotation is the same as its imaginary part
            var axis = new Vector3(q.x, q.y, q.z);
            var angle = 2.0f * Mathf.Acos(q.w);

            // Return the axis of rotation
            return axis.normalized * angle;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _meshRenderer.enabled = true;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isDragging = true;
            _meshRenderer.material = _selectedMaterial;
            _previousControllerRotation = _nrealcontrollerInput.Rotation;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isDragging = false;
            _meshRenderer.material = _defaultMaterial;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _meshRenderer.enabled = false;
        }
    }
}