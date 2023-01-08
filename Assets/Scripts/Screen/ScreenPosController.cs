using NRKernal;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Screen
{
    [RequireComponent(typeof(MeshCollider), typeof(MeshFilter), typeof(MeshRenderer))]
    public class ScreenPosController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IDragHandler, IBeginDragHandler, IPointerDownHandler,
        IPointerUpHandler
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
                var currentControllerRotation = NRInput.GetRotation();

                var angleBetween = Quaternion.Angle(currentControllerRotation, _previousControllerRotation);
                var axisBetween = GetRotationAxis(_previousControllerRotation, currentControllerRotation);

                _parent.RotateAround(NRInput.GetPosition(), axisBetween, angleBetween);




                _previousControllerRotation = currentControllerRotation;
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
            Debug.Log("Now dragging");
            _previousControllerRotation = NRInput.GetRotation();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isDragging = false;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _meshRenderer.enabled = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            // var curScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, _screenPoint.z);
            // var curPosition = Camera.main.ScreenToWorldPoint(curScreenPoint) + _offset;
            // _parent.position = curPosition;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // _meshRenderer.material = _selectedMaterial;
            // var parentPosition = _parent.position;
            // _screenPoint = Camera.main.WorldToScreenPoint(parentPosition);
            // _offset = parentPosition - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, _screenPoint.z));
        }
    }
}