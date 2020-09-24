using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using GameAnalyticsSDK;
using UnityEngine.SceneManagement;
//using UnityEngine.Advertisements;
//using GoogleMobileAds.Api;

public static class Engine
{
    //UNCOMMENT TO IMPLEMENT UNITY ADS
    //public static bool isVideoReady
    //{
    //    get
    //    {
    //        return Advertisement.IsReady(PlacementType.video.ToString());
    //    }
    //}
    //public static bool isRewardedVideoReady
    //{
    //    get
    //    {
    //        return Advertisement.IsReady(PlacementType.rewardedVideo.ToString());
    //    }
    //}
    //public static bool isBannerReady
    //{
    //    get
    //    {
    //        return Advertisement.IsReady(PlacementType.banner.ToString());
    //    }
    //}
    public static int actualLevel
    { get { return meta.passedLevels + 1; } }
    public static GameSessionState sessionState
    { get { return currentSession.state; } }
    public static bool isRewardedVideoReady
    {
        get
        {
            if (currentSession == null)
                return false;
            else
                return /*currentSession.adController*/AdMobController.isRewardedVideoReady;
        }
    }
    public static bool paused
    {
        get
        {
            if (currentSession != null) 
                return currentSession.paused; 
            else
                return true;
        }
    }
    public static bool extraRewardReceivedInCurrentSession
    {
        get
        {
            if (currentSession == null)
                return false;
            else
                return currentSession.ExtraRewardRequired;
        }
    }
    public static bool isCoreActive
    {
        get
        {
            return currentSession != null;
        }
    }
    public static int rewardAmount
    {
        get
        {
            return currentSession.rewardAmount;
        }
    }
    public static bool initialized
    {
        get;
        private set;
    }
    internal static GameData meta;
    private static int totalHandcraftLevelsAmount
    {
        get
        {
            return SceneManager.sceneCountInBuildSettings - 2;
        }
    }
    private static LevelPlayingSession currentSession;

    public static void Initialize()
    {
        Logger.AddContent(UILogDataType.Init, "Loading gamesave and initernal resources");
        try
        {
            Load();
        }
        catch (System.Exception e)
        {
            Logger.AddContent(UILogDataType.Init, e.Message);
            Logger.AddContent(UILogDataType.Init, "trace: " + e.StackTrace);
            Debug.LogError(e.Message);
        }
        Logger.AddContent(UILogDataType.Init, "Subscribing events");
        Subscribe();
        Logger.AddContent(UILogDataType.Init, "Loading resources");
        Localization.LoadLocals(Application.systemLanguage);
        initialized = true;
    }
    public static void InitializeTest()
    {
        if (currentSession == null)
            Initialize();
    }
    public static void ClearSaveFile()
    {
        File.Delete(Settings.saveFile);
        meta = new GameData();
        RestartLevel();
    }
    public static void CurrencyCollected()
    {
        CurrencyIncome(1);
    }
    public static void StartCoreGameplay()
    {
        //Core start logic
    }
    public static void CoreLevelDone()
    {
        Save();
        SwitchCoreLevel();
    }
    //public static void LevelFailed()
    //{
    //    if (isVideoReady)
    //        Advertisement.Show(PlacementType.video.ToString());
    //    else
    //        RestartLevel();
    //}
    public static void RestartLevel()
    {
        Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    public static void QuitCoreGameplay()
    {
        Save();
        currentSession.Close();
        currentSession = null;
        SceneManager.LoadScene(0);
    }
    public static void QuitApplcation()
    {
        Save();
        Application.Quit();
    }
    public static void SwitchPause()
    {
        if (currentSession.state != GameSessionState.Lost && currentSession.state != GameSessionState.Won)
            currentSession.paused = !currentSession.paused;
    }
    public static void AcceptGDPR()
    {
        meta.GDPRAccepted = true;
        Save();
    }
    private static void CurrencyIncome(int count)
    {
        meta.currencyAmount += count;
    }
    private static void SwitchCoreLevel()
    {
        //Level switch logic
    }
    private static void Save()
    {
        string jsonData = JsonUtility.ToJson(meta);
        File.WriteAllText(Settings.saveFile, jsonData);
        Debug.Log(jsonData);
    }
    private static void Load()
    {
        if (!Directory.Exists(Settings.savePath))
        {//Create directory if not exists
            Directory.CreateDirectory(Settings.savePath);
            Debug.Log("Saving path created " + Settings.savePath);
        }
        if (File.Exists(Settings.saveFile))
        {//load from file
            FileStream file = File.OpenRead(Settings.saveFile);
            StreamReader read = new StreamReader(file);
            string jsonData = read.ReadToEnd();
            Debug.Log("Data loaded from " + Settings.saveFile);
            int saveFileVersion = 0;
            if (jsonData.IndexOf("_version") > 0)
                saveFileVersion = Int32.Parse(jsonData.Substring(jsonData.IndexOf("_version") + 10, 1));
            else
            {
                Debug.LogError("Save file has no version");
                GameAnalytics.NewErrorEvent(GAErrorSeverity.Critical, "Save file has no version");
                meta = new GameData();
            }
            if (saveFileVersion == GameData.Version)
            {
                meta = JsonUtility.FromJson<GameData>(jsonData);
                read.Close();
                file.Close();
            }
            else
            {
                Debug.LogError("Save file has an old verson " + saveFileVersion);
                GameAnalytics.NewErrorEvent(GAErrorSeverity.Critical, "Save file has an old verson " + saveFileVersion);
                meta = new GameData();
            }
        }
        else
        {//load new game
            meta = new GameData();
        }

        //Loading resources
    }
    private static void Subscribe()
    {
        Events.extraRewardReceived += Save;
        Events.adFinished += OnAdFinished;
        SceneManager.activeSceneChanged += OnLevelChanged;
    }
    private static void OnLevelChanged(Scene current, Scene next)
    {
        Debug.Log("Scene change detected. Current active scene is " + next.name);
        if (currentSession != null)//if current scene is not initial
            currentSession.Close(); //Closing previous session only if it existed
        currentSession = new LevelPlayingSession(next);
        Logger.UpdateContent(UILogDataType.Level, next.name + ". Passed " + meta.passedLevels);
    }
    private static void OnAdFinished(PlacementType type)
    {
        //Ad finish processing
    }

    private class LevelPlayingSession/*: IUnityAdsListener UNCOMMENT TO IMPLEMENT UNITY ADS*/
    {
        public Scene level 
        { get; private set; }
        public GameSessionState state
        {
            get
            {
                return _state;
            }
            set
            {
                _state = value;
                Debug.Log("State changed");
                if (value == GameSessionState.Won)
                {
                    meta.passedLevels += 1;
                }
                switch (value)
                {
                    case GameSessionState.Won:
                        GameAnalytics.NewProgressionEvent(GAProgressionStatus.Complete, "Traffic way", level.name, "Level progress", actualLevel);
                        break;
                    case GameSessionState.Lost:
                        GameAnalytics.NewProgressionEvent(GAProgressionStatus.Fail, "Traffic way", level.name, "Level progress", actualLevel);
                        break;
                }
                Engine.Events.GameSessionStateChanged(_state);
            }
        }
        public int rewardAmount
        {
            get
            {
                if (ExtraRewardRequired)
                    return Settings.LevelCurrencyReward * Settings.LevelExtraRewardMultiplyer;
                else
                    return Settings.LevelCurrencyReward;
            }
        }
        public bool ExtraRewardRequired;
        public bool paused
        {
            get
            { 
                return _paused; 
            }
            set
            {
                if (value == _paused)
                    return;
                if (!_paused)
                {
                    _paused = value;
                    Engine.Events.Paused();
                }
                else
                {
                    _paused = value;
                    Engine.Events.Unpaused();
                }
            }
        }
        public bool bossFight
        {
            get;
            private set;
        }

        private bool _paused;
        private GameSessionState _state;

        public LevelPlayingSession(Scene lvl)
        {
            level = lvl;
            state = GameSessionState.InProgress;
            paused = false;
            ////Advertisement.AddListener(this); UNCOMMENT TO IMPLEMENT UNITY ADS
            GameAnalytics.NewProgressionEvent(GAProgressionStatus.Start,"Traffic way", level.name,"Level progress",actualLevel);
        }
        public void Close()
        {
            //meta.car.BoostOff();
            //Advertisement.RemoveListener(this); UNCOMMENT TO IMPLEMENT UNITY ADS
        }

        //UNCOMMENT TO IMPLEMENT UNITY ADS
        //public void OnUnityAdsReady(string placementId)
        //{
        //    if (placementId == PlacementType.banner.ToString())
        //        Advertisement.Banner.Show(PlacementType.banner.ToString());
        //    Debug.Log(placementId + " ready");
        //}
        //public void OnUnityAdsDidError(string message)
        //{
        //    GameAnalytics.NewErrorEvent(GAErrorSeverity.Error, message);
        //}
        //public void OnUnityAdsDidFinish(string placementId, ShowResult showResult)
        //{
        //    Logger.UpdateContent(UILogDataType.Monetization, placementId + " " + showResult, true, true);
        //    if (placementId == PlacementType.rewardedVideo.ToString())
        //        if (showResult == ShowResult.Finished)
        //        {
        //            ExtraRewardReceoved = true;
        //            Events.ExtraRewardReceived();
        //        }
        //    if (placementId == PlacementType.video.ToString())
        //    {
        //        if (state == GameSessionState.Won)
        //            SwitchLevel();
        //        if (state == GameSessionState.Lost)
        //            RestartLevel();
        //    }
        //}
        //public void OnUnityAdsDidStart(string placementId)
        //{
        //}
    }

    #region Metadata models versions    
    internal class GameData
    {
        public const int Version = 0;
        [SerializeField]
        private int _version;
        public int passedLevels;
        public int currencyAmount
        {
            get 
            { 
                return _currencyAmount;
            }
            set 
            {
                int diff = value - _currencyAmount;
                    _currencyAmount = value;
                if (diff>0)//Increase coins amount                
                    GameAnalytics.NewResourceEvent(GAResourceFlowType.Source, "Coin", diff,"Coin","Coin");                
                else
                    GameAnalytics.NewResourceEvent(GAResourceFlowType.Sink, "Coin", -diff, "Coin", "Coin");  
            }
        }
        [SerializeField]
        private int _currencyAmount;
        public bool GDPRAccepted;
        public GameData()
        {
            _version = Version;
            //if (Settings.testMode)
            //    coinsCount = 5000;
            GDPRAccepted = false;
        }
    }

    #endregion

    public static class Events
    {
        public delegate void GameStateHandler(GameSessionState state);
        public delegate void Fact();
        public delegate void AdsInfo(PlacementType type);

        public static event Fact extraRewardReceived;
        public static event Fact initialized;
        public static event Fact levelGenerated;
        public static event Fact paused;
        public static event Fact unpaused;
        public static event AdsInfo adLoaded;
        public static event AdsInfo adNotReady;
        public static event AdsInfo adFinished;
        public static event AdsInfo adSkipped;
        public static event AdsInfo adFailed;
        public static event AdsInfo adOpened;
        public static event AdsInfo adUserLeave;
        public static event GameStateHandler gameSessionStateChanged;

        public static void GameSessionStateChanged(GameSessionState state)
        {
            Debug.Log("Game session state change detected. New state: " + state.ToString());
            if (gameSessionStateChanged != null)
                gameSessionStateChanged(state);
        }
        public static void ExtraRewardReceived()
        {
            Debug.Log("Extra reward received");
            if (extraRewardReceived != null)
                extraRewardReceived();
        }
        public static void Initialized()
        {
            Debug.Log("Game engine initialized");
            if (initialized != null)
                initialized();
        }
        public static void LevelGenerated()
        {
            Debug.Log("Level generated");
            if (levelGenerated != null)
                levelGenerated();
        }
        public static void Paused()
        {
            Debug.Log("Paused");
            if (paused != null)
                paused();
        }
        public static void Unpaused()
        {
            Debug.Log("Unpaused");
            if (unpaused != null)
                unpaused();
        }
        public static void AdLoaded(PlacementType type)
        {
            Debug.Log(type + " placement loaded");
            if (adLoaded != null)
                adLoaded(type);
        }
        public static void AdNotReady(PlacementType type)
        {
            Debug.Log("Time to use " + type + " placement, but it is not ready");
            if (adNotReady != null)
                adNotReady(type);
        }
        public static void AdFinished(PlacementType type)
        {
            Debug.Log(type + " placement finished");
            if (adFinished != null)
                adFinished(type);
        }
        public static void AdSkipped(PlacementType type)
        {
            Debug.Log(type + " placement skipped");
            if (adSkipped != null)
                adSkipped(type);
        }
        public static void AdFailed(PlacementType type)
        {
            Debug.LogError(type + " placement failed");
            if (adFailed != null)
                adFailed(type);
        }
        public static void AdOpened(PlacementType type)
        {
            Debug.Log(type + " placement clicked");
            if (adOpened != null)
                adOpened(type);
        }
        public static void AdUserLeave(PlacementType type)
        {
            Debug.Log("User left, wathing advertisment " + type);
            if (adUserLeave != null)
                adUserLeave(type);
        }
    }
}
public enum GameSessionState { InProgress,Passed, Won, Lost }