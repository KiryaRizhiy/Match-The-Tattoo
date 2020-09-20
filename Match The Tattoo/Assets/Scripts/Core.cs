using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class Core: MonoBehaviour
{
    public GameObject camera;
    public GameObject controlFrame;
    public List<Sprite> stencilSprites;
    public List<GameObject> templates;
    public Material stencilMaterial;
    public float _positionGreenZone;
    public float _positionYellowZone;
    public float _scaleGreenZone;
    public float _scaleYellowZone;
    public float _angleGreenZone;
    public float _angleYellowZone;
    public float _minScale;
    public float _maxScale;
    public float _xBoarder;
    public float _yTopBoarder;
    public float _yBottomBoarder;

    #region Linking acceptors
    protected GameObject _template
    {
        get
        {
            return camera.transform.GetChild(0).GetChild(0).gameObject;
        }
    }
    protected GameObject _stencilContainer
    {
        get { return camera.transform.GetChild(1).gameObject; }
    }
    protected GameObject _removeStencilButton
    {
        get
        { return transform.GetChild(0).GetChild(3).gameObject; }
    }
    protected Camera _camera
    {
        get
        {
            return camera.GetComponent<Camera>();
        }
    }
    protected int _points
    {
        set 
        {
            transform.GetChild(0).GetChild(1).GetComponent<Text>().text = "Points: " + value.ToString();
        }
    }
    #endregion
    #region Logical propetries
    private Stencil _currentStencil
    {
        get
        { return _currentStencilContaier; }
        set
        {
            _currentStencilContaier = value;
            _removeStencilButton.GetComponent<Button>().interactable = (value != null);
        }
    }
    private Stencil _currentStencilContaier;
    private Vector3 _dragPosition;
    private Vector3 _previousDragPosition;
    private Vector3 _controlsPosition;
    private float mZCoord;
    protected static List<Stencil> Stencils;
    public static Core Main;
    public static bool isDrag;
    private bool hold;
    private bool isTestNotInProgress;
    #endregion
    #region Constants
    private const float _templateTransparency = 0.2f;
    private const float _brokenSequenceAngle = float.PositiveInfinity;
    private Vector3 _brokenSequencePosition
    {
        get
        {
            return Vector3.positiveInfinity;
        }
    }
    private Vector3 _brokenSequenceScale
    {
        get
        {
            return Vector3.positiveInfinity;
        }
    }
    private int _coordinateGridMask = 1 << 8;
    private int _controlCenterMask = 1 << 11;
    #endregion
    #region Unity events
    private void Start()
    {
        Main = this;
        hold = false;
        isDrag = false;
        Stencils = new List<Stencil>();
        GenetateLevel();
    }
    void Update()
    {
        if(_currentStencil != null)
        {
            //Logger.UpdateContent(UILogDataType.Logic, "Stencil postion: " + _currentStencil.obj.transform.position
            //    + ", rotation: " + _currentStencil.obj.transform.rotation.eulerAngles
            //    + ", scale: " + _currentStencil.obj.transform.localScale);
            _currentStencil.obj.transform.rotation = Quaternion.Euler(0f, 0f, ControlElement.angle);
            _currentStencil.obj.transform.localScale = Vector3.one * ControlElement.scale;
        }
        isDrag = false;
        RaycastHit _hit;
        if (Input.touchCount == 1)
            _controlsPosition = Input.touches[0].position;
        else
            _controlsPosition = Input.mousePosition;
        if (!EventSystem.current.IsPointerOverGameObject() && !ControlElement.isRotating && (Input.GetMouseButtonDown(0) || (Input.touchCount == 1 ? Input.touches[0].phase == TouchPhase.Began : false)))
        {
            //Logger.UpdateContent(UILogDataType.Controls, "Start drag");
            if (!Physics.Raycast(Camera.main.ScreenPointToRay(_controlsPosition), out _hit,100f,_controlCenterMask))
                Debug.Log("Stencil drag unavailable cause of empty raycast hit");
            else
            {
                _previousDragPosition = _hit.point;
                //Debug.Log("Init drag pos " + _previousDragPosition);
                isDrag = true;
                hold = true;
            }
        }
        if (_currentStencil != null && !ControlElement.isRotating && hold && (Input.GetMouseButton(0) || (Input.touchCount == 1 ? Input.touches[0].phase != TouchPhase.Began : false)))
        {
            //Logger.UpdateContent(UILogDataType.Controls, "Drag");
            isDrag = true;
            if (!Physics.Raycast(Camera.main.ScreenPointToRay(_controlsPosition), out _hit, _coordinateGridMask))
                Debug.LogError("Stencil drag unavailable cause of empty raycast hit");
            else
            {
                _dragPosition = _hit.point;
                //Debug.Log("Current drag data pos: " + _dragPosition + " prev pos: " + _previousDragPosition + " delta: " + (_previousDragPosition - _dragPosition).ToString());
            }
            Vector3 _newPos = _currentStencil.obj.transform.position + (_dragPosition - _previousDragPosition);
            _newPos.z = 0f;
            if (!(_newPos.y < _yBottomBoarder || _newPos.y > _yTopBoarder || Mathf.Abs(_newPos.x) > _xBoarder))
                _currentStencil.obj.transform.position += (_dragPosition - _previousDragPosition);
            _previousDragPosition = _dragPosition;
            //_currentStencil.obj.transform.position = GetMouseAsWorldPoint() + mOffset;
        }
        if (_currentStencil != null && (Input.GetMouseButtonUp(0) || (Input.touchCount == 1 ? Input.touches[0].phase == TouchPhase.Ended : false)))
        {
            //Logger.UpdateContent(UILogDataType.Controls, "Finish drag");
            hold = false;
        }
    }
    #endregion
    #region Internal logic
    private void MakeTemplateGrey(GameObject obj)
    {
        if (obj.GetComponent<MeshRenderer>() != null)
            obj.GetComponent<MeshRenderer>().material.color = new Color(
                obj.GetComponent<MeshRenderer>().material.color.r,
                obj.GetComponent<MeshRenderer>().material.color.g,
                obj.GetComponent<MeshRenderer>().material.color.b,
                _templateTransparency);
        for(int i = 0; i<obj.transform.childCount; i++)
        {
            MakeTemplateGrey(obj.transform.GetChild(i).gameObject);
        }
    }
    private void UserFinishedLevel()
    {
        _points = Mathf.CeilToInt(Estimate() * 100);
        GenetateLevel();
    }
    private void ClearAllStencils()
    {
        _currentStencil = null;
        foreach (Stencil _s in Stencils)
        {
            _s.Remove();
        }
        Stencils = new List<Stencil>();
    }
    private void GenetateLevel()
    {
        Transform _t = _template.transform.parent;
        Destroy(_template);
        Instantiate(templates[UnityEngine.Random.Range(0, templates.Count)], _t);
        ClearAllStencils();
        MakeTemplateGrey(_t.gameObject);
    }
    private float Estimate()
    {
        List<GameObject> _alreadyMatched = new List<GameObject>();
        GameObject _currentMathchedObj;
        List<float> _estimations = new List<float>();
        Debug.Log("Comparing " + _template.name + " with " + _stencilContainer.name);
        for(int i = 0; i< _template.transform.childCount; i++)
        {
            if (_template.transform.GetChild(i).gameObject.layer == 8)
            {
                Debug.Log(_template.transform.GetChild(i).name + "skipped");
                continue;
            }
            _currentMathchedObj = MatchObject(_template.transform.GetChild(i).gameObject,_alreadyMatched);
            if (_currentMathchedObj == null)
                _estimations.Add(0f);
            else
            {
                _alreadyMatched.Add(_currentMathchedObj);
                _estimations.Add(EstimateObjects(_template.transform.GetChild(i), _currentMathchedObj.transform));
            }
        }
        float _result = 0f;
        foreach (float _f in _estimations)
            _result += _f;
        Debug.Log("Total estimation result: " + (_result / _estimations.Count));
        return _result/_estimations.Count;
    }
    private GameObject MatchObject(GameObject Target, List<GameObject> ExceptedObjects)
    {
        Debug.Log("Matching " + Target.name);
        float minMagnitude = float.PositiveInfinity;
        GameObject result = null;
        for (int i = 0; i < _stencilContainer.transform.childCount; i++)
        {
            Debug.Log("Checking " + _stencilContainer.transform.GetChild(i).name);
            if (ExceptedObjects.Find(x => x == _stencilContainer.transform.GetChild(i).gameObject) == null 
                && _stencilContainer.transform.GetChild(i).gameObject.layer != 8)
            {
                float _currMag = Vector3.Magnitude(Target.transform.position - _stencilContainer.transform.GetChild(i).transform.position);
                Debug.Log("Difference: " + (Target.transform.position - _stencilContainer.transform.GetChild(i).transform.position) +
                    ", magnitude: " + _currMag +
                    ", minMagnitude " + minMagnitude);
                if (_currMag < minMagnitude)
                {
                    result = _stencilContainer.transform.GetChild(i).gameObject;
                    minMagnitude = _currMag;
                }
            }
            else
                Debug.Log(_stencilContainer.transform.GetChild(i).name + " excepted before");
        }
        Debug.Log((result==null ? Target.name + " not matched" : Target.name + " matched with " + result.name));
        return result;
    }
    private float EstimateObjects(Transform Template, Transform Stencil)
    {
        float _distanceEstimation = ComputeEstimation(Vector3.Magnitude(Template.position - Stencil.position), _positionGreenZone, _positionYellowZone);
        float _scaleEstimation = ComputeEstimation(Vector3.Magnitude(Template.localScale - Stencil.localScale), _scaleGreenZone, _scaleYellowZone);
        float _angleDiff = (Template.rotation.eulerAngles.z - Stencil.rotation.eulerAngles.z);
        if (_angleDiff < 0)
            _angleDiff += 360;
        float _rotationEstimation = ComputeEstimation(_angleDiff, _angleGreenZone, _angleYellowZone);
        Debug.Log(Stencil.name + " estimation: " + Environment.NewLine +
            "Template position: " + Template.position + " stencil position: " + Stencil.position + ", delta: " + (Template.position - Stencil.position) + "(" + (Template.position - Stencil.position).magnitude+ ")" + ", max delta: " + _positionYellowZone + ", effort:" + _distanceEstimation + Environment.NewLine +
            "Template scale: " + Template.localScale + " stencil scale: " + Stencil.localScale + ", delta: " + (Template.localScale - Stencil.localScale) + "(" + (Template.localScale - Stencil.localScale).magnitude + ")" + ", max delta: " + _scaleYellowZone + ", effort:" + _scaleEstimation + Environment.NewLine +
            "Template rotation: " + Template.rotation.eulerAngles.z + " stencil rotation: " + Stencil.rotation.eulerAngles.z + ", delta: " + _angleDiff + ", max delta: " + _angleYellowZone + ", effort:" + _rotationEstimation);
        return (_distanceEstimation + _scaleEstimation + _rotationEstimation)/3f;
    }
    private float ComputeEstimation(float Value, float GreenZone, float YellowZone)
    {
        if (Value >= YellowZone)
            return 0f;
        if (Value < 0)
        {
            Debug.LogError("Invalid value " + Value + " It can't be below zero. Used " + Mathf.Abs(Value) + " instead");
            Value = Mathf.Abs(Value);
        }
        if (Value <= GreenZone)
            return 1f;
        return 1f - Value / YellowZone;
    }
    #endregion
    #region UI Logic
    public void ToggleValueChanged(bool _isOn)
    {
        _template.SetActive(_isOn);
    }
    public void NewStencil(int _stenciTypelNum)
    {
        try
        {

            if (_currentStencil != null)
                Destroy(_currentStencil.obj.transform.GetChild(0).gameObject);
            Stencils.Add(new Stencil((StencilType)_stenciTypelNum, this));
            _currentStencil = Stencils[Stencils.Count - 1];
            Instantiate(controlFrame, _currentStencil.obj.transform);
        }
        catch(Exception e)
        {
            Logger.AddContent(UILogDataType.GameState, "Stencil add error " + e.Message + " trace: " + e.StackTrace);
        }
    }
    public void RemoveStencil()
    {
        _currentStencil.Remove();
        Stencils.Remove(_currentStencil);
        //_currentStencilCandidate = null;
        _currentStencil = null;
    }
    public void CopyStencil()
    {
        NewStencil((int)_currentStencil.type);
    }
    public void RefreshLevel()
    {
        ClearAllStencils();
    }
    public void Done()
    {
        UserFinishedLevel();
    }
    public void ExtimationTest()
    {
        Estimate();
    }
    #endregion

    public class Stencil
    {
        public StencilType type
        { get; private set; }
        public GameObject obj
        {
            get; private set;
        }
        private MeshRenderer _objRenderer => obj.GetComponent<MeshRenderer>();
        public Stencil(StencilType Type,Core GameData)
        {
            type = Type;
            obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            obj.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            obj.name = Type.ToString() + "_" + Core.Stencils.Count.ToString();
            obj.transform.SetParent(GameData._stencilContainer.transform);
            obj.transform.position = Vector3.zero;
            obj.tag = type.ToString();
            _objRenderer.material = GameData.stencilMaterial;
            _objRenderer.material.mainTexture = GameData.stencilSprites[(int)type].texture;
            obj.layer = 9;
        }
        public void Remove()
        {
            Destroy(obj);
        }
    }
}
public enum StencilType {Heart, Plus, Round}