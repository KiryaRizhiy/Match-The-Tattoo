﻿using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class Core : MonoBehaviour
{
    public GameObject controlFrame;
    public List<StencilInput> stencilSprites;
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
    public float _firstStarThreshold;
    public float _secondStarThreshold;

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
        { return transform.GetChild(0).GetChild(0).gameObject; }
    }
    protected Camera _camera
    {
        get
        {
            return camera.GetComponent<Camera>();
        }
    }
    protected Transform StencilTypeScrollsContainer
    {
        get
        {
            return transform.GetChild(1).GetChild(3).GetChild(0).GetChild(1);
        }
    }
    protected Transform StencilTypesScroll
    {
        get
        {
            return transform.GetChild(1).GetChild(3).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(0);
        }
    }
    protected Transform ColorSelectPanel
    {
        get
        {
            return transform.GetChild(1).GetChild(3).GetChild(1);
        }
    }
    protected Transform SmallTemplate
    {
        get
        {
            return camera.transform.GetChild(3).GetChild(1);
        }
    }
    protected GameObject camera
    {
        get
        {
            return Camera.main.gameObject;
        }
    }
    protected Transform yellowStarsPannel
    {
        get
        {
            return transform.GetChild(2).GetChild(1).GetChild(1).GetChild(1);
        }
    }
    protected Transform _customerEmotionImage
    {
        get
        {
            return transform.GetChild(0).GetChild(3).GetChild(0);
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
            _removeStencilButton.GetComponent<Button>().interactable = (Stencils.Count != 0);
        }
    }
    private Stencil _currentStencilContaier;
    private Vector3 _dragPosition;
    private Vector3 _previousDragPosition;
    private Vector3 _controlsPosition;
    private static Texture2D _angryEmotion;
    private static Texture2D _sadEmotion;
    private static Texture2D _thinkingEmotion;
    private static Texture2D _thumbsUpEmotion;
    private static Texture2D _heartEmotion;
    protected static List<Stencil> Stencils;
    public static Core Main;
    public static bool isDrag;
    private bool hold;
    private bool isTestNotInProgress;
    private bool isEmotionShown;
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
        isEmotionShown = false;
        Main = this;
        hold = false;
        isDrag = false;
        Stencils = new List<Stencil>();
        //GenetateLevel();
        ShowTemplateAvatar();
        MakeTemplateGrey(_template);
        Engine.Events.CoreReadyToChangeState(GameSessionState.InProgress);
        ShowEmotion();
    }
    void Update()
    {
        if (_currentStencil != null)
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
            if (!Physics.Raycast(Camera.main.ScreenPointToRay(_controlsPosition), out _hit, 100f, _controlCenterMask))
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
        if (Stencils.Count > 0)
        {
            if (isDrag || ControlElement.isRotating)
                isEmotionShown = false;
            else
            {
                if (!isEmotionShown)
                {
                    ShowEmotion();
                    isEmotionShown = true;
                }
            }
        }
    }

    #endregion
    #region External logic
    public static void LoadResources()
    {
        _angryEmotion = Resources.Load<Texture2D>("Textures/CustomerEmotions/angry");
        _sadEmotion = Resources.Load<Texture2D>("Textures/CustomerEmotions/sad");
        _thinkingEmotion = Resources.Load<Texture2D>("Textures/CustomerEmotions/thinking");
        _thumbsUpEmotion = Resources.Load<Texture2D>("Textures/CustomerEmotions/thumbsUp");
        _heartEmotion = Resources.Load<Texture2D>("Textures/CustomerEmotions/heart");
    }
    #endregion
    #region Internal logic
    private void MakeTemplateBlack(GameObject obj)
    {
        if (obj.GetComponent<MeshRenderer>() != null)
        {
            obj.GetComponent<MeshRenderer>().material.color = new Color(0f, 0f, 0f);
        }
        for (int i = 0; i < obj.transform.childCount; i++)
        {
            MakeTemplateGrey(obj.transform.GetChild(i).gameObject);
        }
    }
    private void MakeTemplateGrey(GameObject obj)
    {
        if (obj.GetComponent<MeshRenderer>() != null)
        {
            obj.GetComponent<MeshRenderer>().material.color = new Color(0f, 0f, 0f, _templateTransparency);
        }
        for (int i = 0; i < obj.transform.childCount; i++)
        {
            MakeTemplateGrey(obj.transform.GetChild(i).gameObject);
        }
    }
    private void ClearAllStencils()
    {
        foreach (Stencil _s in Stencils)
        {
            _s.Remove();
        }
        Stencils = new List<Stencil>();
        _currentStencil = null;
        ShowEmotion();
    }
    private void ShowTemplateAvatar()
    {
        Destroy(SmallTemplate.GetChild(0).gameObject);
        GameObject _sg = Instantiate(_template, SmallTemplate);
        _sg.transform.localScale = Vector3.one;
        _sg.transform.localPosition = Vector3.forward * (-0.000003f);
        MakeTemplateBlack(_sg);
    }
    private float Estimate()
    {
        List<GameObject> _alreadyMatched = new List<GameObject>();
        GameObject _currentMathchedObj;
        List<float> _estimations = new List<float>();
        Debug.Log("Comparing " + _template.name + " with " + _stencilContainer.name);
        for (int i = 0; i < _template.transform.childCount; i++)
        {
            if (_template.transform.GetChild(i).gameObject.layer == 8)
            {
                Debug.Log(_template.transform.GetChild(i).name + "skipped");
                continue;
            }
            _currentMathchedObj = MatchObject(_template.transform.GetChild(i).gameObject, _alreadyMatched);
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
        return _result / _estimations.Count;
    }
    private void ShowEmotion()
    {
        if (Stencils.Count == 0)
        {
            _customerEmotionImage.GetComponent<RawImage>().color = new Color(0f, 0f, 0f, 0f);
            return;
        }
        _customerEmotionImage.GetComponent<RawImage>().color = Color.white;
        float _est = Estimate();
        if (_est >= 0 && _est < 0.2f)
            _customerEmotionImage.GetComponent<RawImage>().texture = _angryEmotion;
        if (_est >= 0.2 && _est < 0.4f)
            _customerEmotionImage.GetComponent<RawImage>().texture = _sadEmotion;
        if (_est >= 0.4f && _est < 0.6f)
            _customerEmotionImage.GetComponent<RawImage>().texture = _thinkingEmotion;
        if (_est >= 0.6f && _est < 0.8f)
            _customerEmotionImage.GetComponent<RawImage>().texture = _thumbsUpEmotion;
        if (_est >= 0.8f && _est <= 1f)
            _customerEmotionImage.GetComponent<RawImage>().texture = _heartEmotion;
        if (_est < 0 || _est > 1f)
        {
            Debug.LogError("Impossible estimation score: " + _est);
            _customerEmotionImage.GetComponent<RawImage>().color = new Color(0f, 0f, 0f, 0f);
        }

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
        if (Template.tag != Stencil.tag)
        {
            Debug.Log(Stencil.name + " estimation is 0 becaause of different tags." + Environment.NewLine +
                Stencil.name + " tag is: " + Stencil.tag + Environment.NewLine +
                Template.name + " tag is: " + Template.tag);
            return 0f;
        }
        else
            Debug.Log(Stencil.name + "Tag check passed. " + Environment.NewLine +
                    Stencil.name + " tag is: " + Stencil.tag + Environment.NewLine +
                    Template.name + " tag is: " + Template.tag);
        if (
            !(
                (Template.GetComponent<MeshRenderer>().material.color.r == Stencil.GetComponent<MeshRenderer>().material.color.r)
                &&
                (Template.GetComponent<MeshRenderer>().material.color.g == Stencil.GetComponent<MeshRenderer>().material.color.g)
                &&
                (Template.GetComponent<MeshRenderer>().material.color.b == Stencil.GetComponent<MeshRenderer>().material.color.b)
             )
            )
        {
            Debug.Log(Stencil.name + " estimation is 0 becaause of different colors." + Environment.NewLine +
                Stencil.name + " color is: " + Stencil.GetComponent<MeshRenderer>().material.color + Environment.NewLine +
                Template.name + " color is: " + Template.GetComponent<MeshRenderer>().material.color);
            return 0f;
        }
        else
            Debug.Log(Stencil.name +  " color check passed." + Environment.NewLine +
                Stencil.name + " color is: " + Stencil.GetComponent<MeshRenderer>().material.color + Environment.NewLine +
                Template.name + " color is: " + Template.GetComponent<MeshRenderer>().material.color);
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
    private void CopyStencil()
    {
        DestroyImmediate(_currentStencil.obj.transform.GetChild(0).gameObject);
        Stencils.Add(new Stencil(_currentStencil));
        _currentStencil = Stencils[Stencils.Count - 1];
        Instantiate(controlFrame, _currentStencil.obj.transform);
    }
    private void RemoveStencil()
    {
        _currentStencil.Remove();
        Stencils.Remove(_currentStencil);
        //_currentStencilCandidate = null;
        _currentStencil = null;
        ShowEmotion();
    }
    private void SwitchStencilTypeScroll()
    {
        string _turnedOnTag = "Untagged";
        //find turned on type
        for (int i = 0; i < StencilTypesScroll.childCount; i++)
        {
            if (StencilTypesScroll.GetChild(i).GetComponent<Toggle>().isOn)
                _turnedOnTag = StencilTypesScroll.GetChild(i).tag;
        }
        //activate type and deactivate other types
        for (int i =0; i< StencilTypeScrollsContainer.childCount; i++)
        {
            if (StencilTypeScrollsContainer.GetChild(i).tag == _turnedOnTag)
                StencilTypeScrollsContainer.GetChild(i).gameObject.SetActive(true);
            else
                StencilTypeScrollsContainer.GetChild(i).gameObject.SetActive(false);

        }
    }
    private void SetStencilColor(Color _c)
    {
        if (_currentStencil != null)
            _currentStencil.obj.GetComponent<MeshRenderer>().material.color = _c;
    }
    private void UserFinishedDrawing()
    {
        ShowStarResult(Estimate());//Mathf.CeilToInt(Estimate() * 100);
        
        Engine.Events.CoreReadyToChangeState(GameSessionState.Won);
        AdMobController.ShowRegularAd();
    }
    public void ShowStarResult(float Estimation)
    {
        RectTransform _r;
        if (Estimation <= 0)
        {
            yellowStarsPannel.GetChild(0).gameObject.SetActive(false);
            yellowStarsPannel.GetChild(1).gameObject.SetActive(false);
            yellowStarsPannel.GetChild(2).gameObject.SetActive(false);
        }
        else
        {
            if (Estimation <= _firstStarThreshold / 100f)
            {
                yellowStarsPannel.GetChild(0).gameObject.SetActive(true);
                _r = yellowStarsPannel.GetChild(0).GetComponent<RectTransform>();
                _r.sizeDelta = new Vector2(_r.rect.width * (Estimation/ (_firstStarThreshold / 100f)), _r.rect.height);
                Debug.Log("Threshold: " + _r.rect.width * (Estimation / (_firstStarThreshold / 100f)));
                yellowStarsPannel.GetChild(1).gameObject.SetActive(false);
                yellowStarsPannel.GetChild(2).gameObject.SetActive(false);
            }
            else
            if (Estimation <= _secondStarThreshold / 100f)
            {
                yellowStarsPannel.GetChild(0).gameObject.SetActive(true);
                yellowStarsPannel.GetChild(1).gameObject.SetActive(true);
                _r = yellowStarsPannel.GetChild(1).GetComponent<RectTransform>();
                _r.sizeDelta = new Vector2(_r.rect.width * ((Estimation - _firstStarThreshold / 100f )/ ((_secondStarThreshold - _firstStarThreshold) / 100f)), _r.rect.height);
                Debug.Log("Threshold: " + _r.rect.width * ((Estimation - _firstStarThreshold / 100f) / ((_secondStarThreshold - _firstStarThreshold) / 100f)));
                yellowStarsPannel.GetChild(2).gameObject.SetActive(false);
            }
            else
            {
                yellowStarsPannel.GetChild(0).gameObject.SetActive(true);
                yellowStarsPannel.GetChild(1).gameObject.SetActive(true);
                yellowStarsPannel.GetChild(2).gameObject.SetActive(true);
                _r = yellowStarsPannel.GetChild(2).GetComponent<RectTransform>();
                _r.sizeDelta = new Vector2(_r.rect.width * ((Estimation - _secondStarThreshold / 100f)/ ((100 - _secondStarThreshold) / 100f)), _r.rect.height);
                Debug.Log("Threshold: " + _r.rect.width * ((Estimation - _secondStarThreshold / 100f) / ((100 - _secondStarThreshold) / 100f)));
            }
        }
    }
    private void ReadyToSwitchLevel()
    {
        Engine.Events.CoreReadyToSwitchLevel();
    }
    #endregion
    #region UI Logic
    public void ToggleValueChanged(bool _isOn)
    {
        _template.SetActive(_isOn);
    }
    public void NewStencil(int _stencilNum)
    {
        try
        {

            if (_currentStencil != null)
                Destroy(_currentStencil.obj.transform.GetChild(0).gameObject);
            Stencils.Add(new Stencil(_stencilNum));
            _currentStencil = Stencils[Stencils.Count - 1];
            Instantiate(controlFrame, _currentStencil.obj.transform);
        }
        catch(Exception e)
        {
            Logger.AddContent(UILogDataType.GameState, "Stencil add error " + e.Message + " trace: " + e.StackTrace);
        }
    }
    public void UIRemoveStencil()
    {
        RemoveStencil();
    }
    public void UICopyStencil()
    {
        CopyStencil();
    }
    public void RefreshLevel()
    {
        ClearAllStencils();
    }
    public void UIUserFinishedDrawing()
    {
        UserFinishedDrawing();
    }
    public void UIReadyToSwitchLevel()
    {
        ReadyToSwitchLevel();
    }
    public void ShowReward()
    {
        AdMobController.ShowRewardedAd();
    }
    public void ShowInterstitial()
    {
        AdMobController.ShowRegularAd();
    }
    public void TypesScrollValueChanged(bool on)
    {
        if (on)
            SwitchStencilTypeScroll();
    }
    public void ShowColors()
    {
        ColorSelectPanel.gameObject.SetActive(true);
    }
    public void HideColors()
    {
        ColorSelectPanel.gameObject.SetActive(false);
    }
    public void SetColor(string _col)
    {
        Color _c;
        if (!ColorUtility.TryParseHtmlString(_col, out _c))
        {
            Debug.LogError("Input color not recognized! " + _col);
            return;
        }
        SetStencilColor(_c);
        HideColors();
    }
    #endregion
    #region Classes
    public class Stencil
    {
        public StencilType type
        { get; private set; }
        public GameObject obj
        {
            get; private set;
        }
        private MeshRenderer _objRenderer => obj.GetComponent<MeshRenderer>();
        public Stencil(int StencilNum)
        {
            StencilInput _si = Core.Main.stencilSprites[StencilNum];
            type = _si.type;
            obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            obj.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            obj.name = type.ToString() + "_" + Core.Stencils.Count.ToString();
            obj.transform.SetParent(Core.Main._stencilContainer.transform);
            obj.transform.position = Vector3.zero;
            obj.tag = type.ToString();
            _objRenderer.material = _si._sm;
            obj.GetComponent<MeshRenderer>().material.color = Color.black;
            obj.layer = 9;
        }
        public Stencil(Stencil stencilToCopy)
        {
            type = stencilToCopy.type;
            obj = Instantiate(stencilToCopy.obj, Core.Main._stencilContainer.transform);
            obj.name = type.ToString() + "_" + Core.Stencils.Count.ToString();
            obj.transform.position = Vector3.zero;
            obj.transform.rotation = Quaternion.Euler(Vector3.zero);
            obj.transform.localScale = Vector3.one;
        }
        public void Remove()
        {
            Destroy(obj);
        }
    }
    [Serializable]
    public class StencilInput
    {
        public Material _sm;
        public StencilType type;
    }
    #endregion
}
public enum StencilType { emotions_and_faces, objects, food_and_drinks , animals_and_nature, people_and_gestures, symbols, activity_and_sport, transport,flags}