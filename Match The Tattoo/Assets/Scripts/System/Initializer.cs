using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameAnalyticsSDK;
using GoogleMobileAds.Api;
using System;
using UnityEngine.UI;

public class Initializer : MonoBehaviour
{
    private Transform progressLine
    {
        get
        {
            return transform.GetChild(2).GetChild(0).GetChild(0).GetChild(1);
        }
    }
    void Start()
    {
        StartCoroutine(LoadingSimulation());
        Debug.Log("Initialization started at :" + Time.realtimeSinceStartup + " seconds");
        float startTime = Time.realtimeSinceStartup;
        if (Engine.initialized)
        {
            transform.GetChild(2).gameObject.SetActive(false);
            return;
        }
        GameAnalytics.Initialize();
        try
        {
            //Advertisement.Initialize(Settings.googlePlayId, Settings.testMode); UNCOMMENT TO IMPLEMENT UNITY ADS
            Engine.Initialize();
            MobileAds.Initialize(initStatus => { });
            AdMobController adController = new GameObject().AddComponent<AdMobController>();
            adController.gameObject.name = "AdMobController";
            DontDestroyOnLoad(adController.gameObject);
        }
        catch (Exception e)
        {
            GameAnalytics.NewErrorEvent(GAErrorSeverity.Critical, e.Message + Environment.NewLine + "-------Trace------" + Environment.NewLine + e.StackTrace);
            Logger.AddContent(UILogDataType.Logic, e.Message + Environment.NewLine + "-------Trace------" + Environment.NewLine + e.StackTrace);
        }
        Engine.Events.Initialized();
        Debug.Log("Initialization done. Time spent: " + (Time.realtimeSinceStartup - startTime) + " seconds. Initialization start time: " + startTime + " seconds. Initialization end time: " + Time.realtimeSinceStartup + " seconds");
    }
    private IEnumerator LoadingSimulation()
    {
        for (int i = 0; i < 11; i++)
        {
            yield return new WaitForSeconds(0.07f);
            progressLine.GetComponent<RectTransform>().anchorMax = Vector2.right * (i / 10f) + Vector2.up;
        }
        yield return new WaitForSeconds(0.25f);
        while (!Engine.initialized)
            yield return new WaitForEndOfFrame();
        transform.GetChild(2).gameObject.SetActive(false);
    }
}