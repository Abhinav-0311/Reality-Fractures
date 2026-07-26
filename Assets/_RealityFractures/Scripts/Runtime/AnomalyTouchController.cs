using UnityEngine;
using UnityEngine.EventSystems;

namespace RealityFractures
{
    public sealed class AnomalyTouchController : MonoBehaviour
    {
        [SerializeField] private float rotationSensitivity = 0.4f;
        [SerializeField] private float zoomSensitivity = 0.5f;
        [SerializeField] private float minScale = 0.2f;
        [SerializeField] private float maxScale = 5.0f;

        private Vector3 previousMousePosition;
        private bool isDragging = false;

        private void Update()
        {
            if (IsPointerOverUI())
            {
                isDragging = false;
                return;
            }

            HandleTouchInput();
            HandleMouseInput();
        }

        private void HandleTouchInput()
        {
            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Moved)
                {
                    float deltaX = touch.deltaPosition.x;
                    transform.Rotate(Vector3.up, -deltaX * rotationSensitivity, Space.World);
                }
            }
            else if (Input.touchCount == 2)
            {
                Touch touch0 = Input.GetTouch(0);
                Touch touch1 = Input.GetTouch(1);

                Vector2 prevPos0 = touch0.position - touch0.deltaPosition;
                Vector2 prevPos1 = touch1.position - touch1.deltaPosition;

                float prevDistance = Vector2.Distance(prevPos0, prevPos1);
                float curDistance = Vector2.Distance(touch0.position, touch1.position);

                float deltaDistance = curDistance - prevDistance;
                ApplyScaleDelta(deltaDistance * zoomSensitivity * 0.01f);
            }
        }

        private void HandleMouseInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                previousMousePosition = Input.mousePosition;
                isDragging = true;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
            }

            if (isDragging && Input.GetMouseButton(0))
            {
                float deltaX = Input.mousePosition.x - previousMousePosition.x;
                if (Mathf.Abs(deltaX) > 1f)
                {
                    transform.Rotate(Vector3.up, -deltaX * rotationSensitivity, Space.World);
                    previousMousePosition = Input.mousePosition;
                }
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                ApplyScaleDelta(scroll * 1.5f);
            }
        }

        private void ApplyScaleDelta(float delta)
        {
            Vector3 current = transform.localScale;
            float target = Mathf.Clamp(current.x + delta, minScale, maxScale);
            transform.localScale = Vector3.one * target;
        }

        private static bool IsPointerOverUI()
        {
            if (EventSystem.current == null) return false;
            if (Input.touchCount > 0)
            {
                return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
            }
            return EventSystem.current.IsPointerOverGameObject();
        }
    }
}
