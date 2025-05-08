using UnityEngine;
using UnityEngine.EventSystems;

public class FixedTouchField : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Vector2 TouchDist { get; private set; }
    private Vector2 _pointerOld;
    private int _pointerId;
    private bool _pressed;

    void Update()
    {
        TouchDist = Vector2.zero;

        if (_pressed)
        {
            if (_pointerId >= 0) // Touch input
            {
                bool touchFound = false;
                foreach (var touch in Input.touches)
                {
                    if (touch.fingerId == _pointerId)
                    {
                        TouchDist = touch.position - _pointerOld;
                        _pointerOld = touch.position;
                        touchFound = true;
                        break;
                    }
                }

                if (!touchFound)
                {
                    // Touch was released outside of OnPointerUp
                    _pressed = false;
                }
            }
            else // Mouse input
            {
                TouchDist = (Vector2)Input.mousePosition - _pointerOld;
                _pointerOld = Input.mousePosition;
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Only track the first touch/mouse click
        if (!_pressed)
        {
            _pressed = true;
            _pointerId = eventData.pointerId;
            _pointerOld = eventData.position;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Only reset if the released pointer is the one we're tracking
        if (eventData.pointerId == _pointerId)
        {
            _pressed = false;
            TouchDist = Vector2.zero;
        }
    }
}