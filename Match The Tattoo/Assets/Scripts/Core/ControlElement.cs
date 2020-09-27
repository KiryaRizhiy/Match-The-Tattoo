using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

public class ControlElement : MonoBehaviour
{
    public AvailableControlTypes ControlType;
    // Update is called once per frame
    private Vector3 mOffset;
    private float mZCoord;
    private Vector3 _center
    {
        get
        {
            return transform.parent.position;
        }
    }
    private float _offsetAngle
    {
        get
        {
            return Mathf.Asin(
                transform.localPosition.y /
                Mathf.Sqrt(
                    Mathf.Pow(transform.localPosition.x, 2f) + 
                    Mathf.Pow(transform.localPosition.y, 2f)))* Mathf.Rad2Deg;
        }
    }
    private bool hold;
    private float _initialScaleMultiplyer;
    private Vector3 _initialScale;
    public static bool isRotating
    {
        get; private set;
    }
    public static float angle
    {
        get; private set;
    }
    public static float scale;
    RaycastHit[] _hit;

    private void Start()
    {
        scale = 1f;
        angle = 0f;
        _initialScaleMultiplyer = Vector3.Magnitude(transform.localPosition);
        _initialScale = transform.localScale;
        transform.localScale = (1 / ControlElement.scale) * _initialScale;
    }

    private void Update()
    {
        try
        {
            transform.localScale = (1 / ControlElement.scale) * _initialScale;
            if (ControlType == AvailableControlTypes.ScaleAndRotation) isRotating = false;
            //if (EventSystem.current.IsPointerOverGameObject())
            //    return;
            if (!EventSystem.current.IsPointerOverGameObject() && (Input.GetMouseButtonDown(0) || (Input.touchCount == 1 ? Input.touches[0].phase == TouchPhase.Began : false)) && ControlType == AvailableControlTypes.ScaleAndRotation)
            {
                _hit = Physics.RaycastAll(Camera.main.ScreenPointToRay(Input.mousePosition));
                if (Array.FindAll<RaycastHit>(_hit, x => x.transform == transform).Length != 0)
                {
                    hold = true;
                    isRotating = true;
                    mZCoord = Camera.main.WorldToScreenPoint(
                        gameObject.transform.position).z;
                    // Store offset = gameobject world pos - mouse world pos
                    mOffset = gameObject.transform.position - GetMouseAsWorldPoint();
                }
            }
            if (hold && (Input.GetMouseButton(0) || (Input.touchCount == 1 ? Input.touches[0].phase != TouchPhase.Began : false)) && ControlType == AvailableControlTypes.ScaleAndRotation)
            {
                isRotating = true;
                angle = Vector3.SignedAngle(Vector3.right, (GetMouseAsWorldPoint() + mOffset) - _center, Vector3.forward) - _offsetAngle;
                float newScale = Vector3.Magnitude(GetMouseAsWorldPoint() + mOffset - _center) / _initialScaleMultiplyer;
                if (!(newScale < Core.Main._minScale || newScale > Core.Main._maxScale))
                    scale = newScale;
                //transform.position = GetMouseAsWorldPoint() + mOffset;
            }
            if ((Input.GetMouseButtonUp(0) || (Input.touchCount == 1 ? Input.touches[0].phase == TouchPhase.Ended : false)) && ControlType == AvailableControlTypes.ScaleAndRotation)
            {
                hold = false;
            }
            if ((Input.GetMouseButtonDown(0) || (Input.touchCount == 1 ? Input.touches[0].phase == TouchPhase.Began : false)) && ControlType == AvailableControlTypes.Copy)
            {
                _hit = Physics.RaycastAll(Camera.main.ScreenPointToRay(Input.mousePosition));
                if (Array.FindAll<RaycastHit>(_hit, x => x.transform == transform).Length != 0)
                {
                    Core.Main.UICopyStencil();
                }
            }
            if ((Input.GetMouseButtonDown(0) || (Input.touchCount == 1 ? Input.touches[0].phase == TouchPhase.Began : false)) && ControlType == AvailableControlTypes.Close)
            {
                _hit = Physics.RaycastAll(Camera.main.ScreenPointToRay(Input.mousePosition));
                if (Array.FindAll<RaycastHit>(_hit, x => x.transform == transform).Length != 0)
                {
                    Core.Main.RemoveStencil();
                }
            }
            //Logger.UpdateContent(UILogDataType.Controls, "Is rotating: " + isRotating + ", is hold " + hold + ", angle :" + angle + ", offset angle " + _offsetAngle);

        }
        catch (Exception e)
        {
            Logger.UpdateContent(UILogDataType.GameState, "Contril cycle error " + e.Message + " trace: " + e.StackTrace);
        }
    }
    private Vector3 GetMouseAsWorldPoint()
    {
        // Pixel coordinates of mouse (x,y)
        Vector3 mousePoint;
        if (Input.touchCount == 1)
            mousePoint = Input.touches[0].position;
        else
            mousePoint = Input.mousePosition;
        // z coordinate of game object on screen
        mousePoint.z = mZCoord;
        // Convert it to world points
        return Camera.main.ScreenToWorldPoint(mousePoint);
    }
    private void OnDestroy()
    {
        angle = 0;
        isRotating = false;
        hold = false;
    }
}
public enum AvailableControlTypes { Copy, ScaleAndRotation, Close}
