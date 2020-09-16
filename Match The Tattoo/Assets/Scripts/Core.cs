using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Core: MonoBehaviour
{
    public GameObject camera;
    public List<Sprite> stencilSprites;
    public List<GameObject> templates;
    public Material stencilMaterial;
    public float _dragMultiplier;
    public float _positionMaxAccaptableShift;
    public float _scaleMaxAcceptableShift;
    public float _angleMaxAccaptableShift;

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
    private Stencil _currentStencilCandidate;
    private Stencil _currentStencilContaier;
    //private Vector3 _previousControlPosition;
    private Vector3[] _controlsPosition;
    private Vector3[] _initialControlsPosition;
    private Vector3 _initialPosition;
    private Vector3 _initialScale;
    private float _initialAngle;
    protected static List<Stencil> Stencils;
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
    private int _stencilsMask = 1 << 9;
    #endregion

    #region Unity events
    private void Start()
    {
        Input.multiTouchEnabled = true;
        _initialControlsPosition = new Vector3[2];
        _controlsPosition = new Vector3[2];
        Stencils = new List<Stencil>();
        GenetateLevel();
        BreakControlSequence();
    }
    void Update()
    {
        //Saving input data
        if (Input.touchCount >= 1)
        {
            _controlsPosition[0] = Input.touches[0].position;
            if (Input.touchCount >= 2)
                _controlsPosition[1] = Input.touches[1].position;
        }
        else
        {
            _controlsPosition[0] = Input.mousePosition;
        }
        //Logging
        Logger.UpdateContent(UILogDataType.Controls, "Controls position 0 " + _controlsPosition[0] + " Controls position 1 " + _controlsPosition[1] + " touch cont :" + Input.touchCount);
        if(_currentStencil != null)
        {
            Logger.UpdateContent(UILogDataType.Logic, "Current stencil position - " + _currentStencil.obj.transform.position +
                " scale - " + _currentStencil.obj.transform.localScale +
                " rotation - " + _currentStencil.obj.transform.rotation.eulerAngles,true);
        }
        if (EventSystem.current.IsPointerOverGameObject())
        {
            BreakControlSequence();
            return;
        }
        //Mobile scale and rotation start or processing
        if (Input.touchCount >= 2 && _currentStencil != null)
        {
            if (Input.touches[0].phase == TouchPhase.Began || Input.touches[1].phase == TouchPhase.Began)
            {
                ScaleAndRotationSequenceStart();
                return;
            }
            else
            {
                ProcessScaleAndRotation();
                return;
            }
        }

        //PC drag start
        if (Input.GetMouseButtonDown(0) && _currentStencil != null && !Input.GetKey("s"))
        {
            if (ProcessPick())
                return;
            else
            {
                DragSequenceStart();
                return;
            }
        }
        //Mobile drag start
        if (Input.touchCount == 1)
            if (Input.touches[0].phase == TouchPhase.Began && _currentStencil != null)
            {
                if (ProcessPick())
                    return;
                else
                {
                    DragSequenceStart();
                    return;
                }
            }
        //Mobile or PC drag processing
        if ((Input.touchCount == 1 || Input.GetMouseButton(0)) && _currentStencil != null && !Input.GetKey("s"))
        {
            if (ProcessPick())
                return;
            else
            {
                ProcessDrag();
                return;
            }
        }
        //Mobile or PC pick processing
        if ((Input.touchCount == 1 || Input.GetMouseButton(0)) && _currentStencil == null)
        {
            if (ProcessPick())
                return;
        }
        //Saving PC scale and rotation fake finger 
        if (Input.GetKeyDown("s"))
        {
            _controlsPosition[1] = _controlsPosition[0];
            Logger.UpdateContent(UILogDataType.Controls, "Controls position 0 " + _controlsPosition[0] + " Controls position 1 " + _controlsPosition[1]);
            return;
        }
        //PC scale and rotation processing
        if (Input.GetKey("s")&& _currentStencil != null)
            if (Input.GetMouseButtonDown(0))
            {
                ScaleAndRotationSequenceStart();
                //Debug.Log("Saving initial position. Controls position 0 " + _controlsPosition[0] + " Controls position 1 " + _controlsPosition[1]);
                Logger.UpdateContent(UILogDataType.Controls, "Controls position 0 " + _controlsPosition[0] + " Controls position 1 " + _controlsPosition[1]);
                Logger.AddContent(UILogDataType.Controls, " init position 0 " + _initialControlsPosition[0] + " init position 1 " + _initialControlsPosition[1]);
                return;
            }
            else
            if (Input.GetMouseButton(0))
            {
                Logger.UpdateContent(UILogDataType.Controls, "Controls position 0 " + _controlsPosition[0] + " Controls position 1 " + _controlsPosition[1]);
                Logger.AddContent(UILogDataType.Controls, " init position 0 " + _initialControlsPosition[0] + " init position 1 " + _initialControlsPosition[1]);
                ProcessScaleAndRotation();
                return;
            }
            else
                return;
        BreakControlSequence();
    }
    #endregion

    #region Internal logic
    private bool ProcessPick()
    {
        RaycastHit _hit;
        if (Physics.Raycast(_camera.ScreenPointToRay(_controlsPosition[0]), out _hit, 1000f, _stencilsMask))
        {
            _currentStencilCandidate = Stencils.Find(x => x.obj == _hit.transform.gameObject);
            //Debug.Log("Casted " + _hit.transform.name);
            if (_currentStencilCandidate.obj == _hit.transform.gameObject)
                return false;
            else
            {
                BreakControlSequence();
                return true;
            }
        }
        else return false;
    }
    private void DragSequenceStart()
    {
        _initialPosition = _currentStencil.obj.transform.position;
        _initialControlsPosition[0] = _controlsPosition[0];
    }
    private void ProcessDrag()
    {
        _currentStencil.obj.transform.position = _initialPosition +
            (Vector3.right * ((_controlsPosition[0].x - _initialControlsPosition[0].x) / Screen.width) +
            Vector3.up * ((_controlsPosition[0].y - _initialControlsPosition[0].y) / Screen.height))*_dragMultiplier;
    }
    private void ScaleAndRotationSequenceStart()
    {
        _initialAngle = _currentStencil.obj.transform.rotation.eulerAngles.z;
        _initialScale = _currentStencil.obj.transform.localScale;
        _initialControlsPosition[0] = _controlsPosition[0];
        _initialControlsPosition[1] = _controlsPosition[1];
    }
    private void ProcessScaleAndRotation()
    {
        if (_initialControlsPosition[0] == _initialControlsPosition[1])
        {
            Debug.LogError("Cant scale with same initial controls positions");
            return;
        }
        if (_currentStencil == null)
        {
            Debug.Log("Nothing to scale and rotate");
            return;
        }
        //Debug.Log("Controls position 0 " + _controlsPosition[0] + " Controls position 1 " + _controlsPosition[1] + " init position 0 " + _initialControlsPosition[0] + " init position 1 " + _initialControlsPosition[1]);
        //Debug.Log("Scaling " +
        //    (Vector3.Distance(_controlsPosition[0], _controlsPosition[1]) /
        //    Vector3.Distance(_initialControlsPosition[0], _initialControlsPosition[1])));
        _currentStencil.obj.transform.localScale =
            _initialScale *
            (Vector3.Distance(_controlsPosition[0], _controlsPosition[1]) /
            Vector3.Distance(_initialControlsPosition[0], _initialControlsPosition[1]));
        //Debug.Log("Angle: " +
        //    (Vector3.SignedAngle(_initialControlsPosition[0], _initialControlsPosition[1],Vector3.forward) -
        //    Vector3.SignedAngle(_controlsPosition[0], _controlsPosition[1], Vector3.forward)));
        _currentStencil.obj.transform.rotation = Quaternion.Euler(0f, 0f, _initialAngle + 
            Vector3.SignedAngle(_initialControlsPosition[0], _initialControlsPosition[1], Vector3.forward) -
            Vector3.SignedAngle(_controlsPosition[0], _controlsPosition[1], Vector3.forward));
    }
    private void BreakControlSequence()
    {
        //_previousControlPosition = _controlsPosition[0];
        _initialControlsPosition[0] = _controlsPosition[0];
        _initialControlsPosition[1] = _controlsPosition[1];
        _controlsPosition[1] = _controlsPosition[0];
        _currentStencil = _currentStencilCandidate;
        _initialScale = _brokenSequenceScale;
        _initialAngle = _brokenSequenceAngle;
        _initialPosition = _brokenSequencePosition;
        Logger.AddContent(UILogDataType.Controls, "Sequence broken");
    }
    private void MakeTemplateGrey(GameObject obj)
    {
        if (obj.GetComponent<SpriteRenderer>() != null)
            obj.GetComponent<SpriteRenderer>().color = new Color(
                obj.GetComponent<SpriteRenderer>().color.r,
                obj.GetComponent<SpriteRenderer>().color.g,
                obj.GetComponent<SpriteRenderer>().color.b,
                _templateTransparency);
        for(int i = 0; i<obj.transform.childCount; i++)
        {
            MakeTemplateGrey(obj.transform.GetChild(i).gameObject);
        }
    }
    private void UserFinishedLevel()
    {
        _points = Mathf.CeilToInt(Estimate()) * 100;
        GenetateLevel();
    }
    private void GenetateLevel()
    {
        Transform _t = _template.transform.parent;
        Destroy(_template);
        Instantiate(templates[UnityEngine.Random.Range(0, templates.Count)], _t);
        foreach(Stencil _s in Stencils)
        {
            _s.Remove();
        }
        Stencils = new List<Stencil>();
        MakeTemplateGrey(_t.gameObject);
    }
    private float Estimate()
    {
        List<GameObject> _alreadyMatched = new List<GameObject>();
        GameObject _currentMathchedObj;
        List<float> _estimations = new List<float>();
        for(int i = 0; i< _template.transform.childCount; i++)
        {
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
        return _result/_estimations.Count;
    }
    private GameObject MatchObject(GameObject Target, List<GameObject> ExceptedObjects)
    {
        float minMagnitude = float.PositiveInfinity;
        GameObject result = null;
        for (int i = 0; i < _stencilContainer.transform.childCount; i++)
            if (ExceptedObjects.Find(x => x == _stencilContainer.transform.GetChild(i).gameObject) == null)
                if (Vector3.Magnitude(Target.transform.position - _stencilContainer.transform.GetChild(i).transform.position) < minMagnitude)
                    result = _stencilContainer.transform.GetChild(i).gameObject;
        Debug.Log((result==null ? Target.name + " not matched" : Target.name + " matched with " + result.name));
        return result;
    }
    private float EstimateObjects(Transform Template, Transform Stencil)
    {
        float _distanceEstimation = ComputeEstimation(Vector3.Magnitude(Template.position - Stencil.position),_positionMaxAccaptableShift);
        float _scaleEstimation = ComputeEstimation(Vector3.Magnitude(Template.localScale - Stencil.localScale), _scaleMaxAcceptableShift);
        float _rotationEstimation = ComputeEstimation(Mathf.Abs(Template.rotation.eulerAngles.z - Stencil.rotation.eulerAngles.z), _angleMaxAccaptableShift);
        return (_distanceEstimation + _scaleEstimation + _rotationEstimation)/3f;
    }
    private float ComputeEstimation(float Value, float MaxAcceptableValue)
    {
        if (Value >= MaxAcceptableValue)
            return 0f;
        if (Value < 0)
        {
            Debug.LogError("Invalid value " + Value + " It can't be below zero. Used " + Mathf.Abs(Value) + " instead");
            Value = Mathf.Abs(Value);
        }
        return Value / MaxAcceptableValue;
    }
    #endregion

    #region UI Logic
    public void ToggleValueChanged(bool _isOn)
    {
        _template.SetActive(_isOn);
    }
    public void NewStencil(int _stenciTypelNum)
    {
        Stencils.Add(new Stencil((StencilType)_stenciTypelNum,this));
        _currentStencilCandidate = Stencils[Stencils.Count - 1];
    }
    public void RemoveStencil()
    {
        _currentStencil.Remove();
        Stencils.Remove(_currentStencil);
        _currentStencilCandidate = null;
        _currentStencil = null;
    }
    public void Done()
    {
        UserFinishedLevel();
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
            obj.name = Type.ToString() + "_" + Core.Stencils.Count.ToString();
            obj.transform.SetParent(GameData._stencilContainer.transform);
            obj.tag = "stencil";
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
public enum StencilType {Heart, Plus, Round }