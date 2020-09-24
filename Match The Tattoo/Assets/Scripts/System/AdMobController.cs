using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using GameAnalyticsSDK;
using GoogleMobileAds.Api;

public class AdMobController : MonoBehaviour
{
    public static bool isRegularVideoReady
    {
        get
        {
            if (interstitial != null)
                return interstitial.IsLoaded();
            else
                return false;
        }
    }
    public static bool isRewardedVideoReady
    {
        get
        {
            if (rewarded != null)
                return rewarded.IsLoaded();
            else
                return false;
        }
    }

    private static BannerView banner;
    private static InterstitialAd interstitial;
    private static RewardBasedVideoAd rewarded;
    private static AdRequest request;

    public static void ShowRegularAd()
    {
        try
        {
            interstitial.Show();
            //GameAnalytics.NewAdEvent(GAAdAction.Request, GAAdType.Interstitial, "AdMob",PlacementType.interstitial.ToString());
        }
        catch (Exception e)
        {
            GameAnalytics.NewAdEvent(GAAdAction.FailedShow, GAAdType.Interstitial, "AdMob", PlacementType.interstitial.ToString());
            GameAnalytics.NewErrorEvent(GAErrorSeverity.Error, e.Message + Environment.NewLine + "-------Trace------" + Environment.NewLine + e.StackTrace);
        }
    }
    public static void ShowRewardedAd()
    {
        try
        {
            rewarded.Show();
            //GameAnalytics.NewAdEvent(GAAdAction.Request, GAAdType.RewardedVideo, "AdMob", PlacementType.rewardedVideo.ToString());
        }
        catch (Exception e)
        {
            GameAnalytics.NewAdEvent(GAAdAction.FailedShow, GAAdType.RewardedVideo, "AdMob", PlacementType.rewardedVideo.ToString());
            GameAnalytics.NewErrorEvent(GAErrorSeverity.Error, e.Message + Environment.NewLine + "-------Trace------" + Environment.NewLine + e.StackTrace);
        }
    }
    
    void Start()
    {
        try
        {

            Logger.UpdateContent(UILogDataType.Monetization, "Ads initialization start");
            string _ph;
            if (Engine.meta.GDPRAccepted)
            {
                request = new AdRequest.Builder().Build();
                _ph = "Personalized ads initialized";
            }
            else
            {
                request = new AdRequest.Builder().AddExtra("npa", "1").Build();
                _ph = "Non-personalized ads initialized";
            }
            loadBanner();
            loadInterstitial();
            loadRewarded();
            Logger.AddContent(UILogDataType.Monetization, _ph);
        }
        catch (Exception e)
        {
            GameAnalytics.NewErrorEvent(GAErrorSeverity.Error, e.Message + Environment.NewLine + "-------Trace------" + Environment.NewLine + e.StackTrace);
            Logger.UpdateContent(UILogDataType.Monetization, e.Message + Environment.NewLine + "-------Trace------" + Environment.NewLine + e.StackTrace);
        }
    }
    
    # region Loading ads methods

    private void loadBanner()
    {
        banner = new BannerView(Settings.adMobBannerId, AdSize.Banner, AdPosition.Bottom);
        banner.OnAdLoaded += bannerHandleOnAdLoaded;
        banner.OnAdFailedToLoad += bannerHandleOnAdFailedToLoad;
        banner.OnAdOpening += bannerHandleOnAdOpened;
        banner.OnAdClosed += bannerHandleOnAdClosed;
        banner.OnAdLeavingApplication += bannerHandleOnAdLeavingApplication;
        banner.LoadAd(request);
    }
    private void loadInterstitial()
    {
        interstitial = new InterstitialAd(Settings.adMobInterstitialId);
        interstitial.OnAdLoaded += interstitialHandleOnAdLoaded;
        interstitial.OnAdFailedToLoad += interstitialHandleOnAdFailedToLoad;
        interstitial.OnAdLeavingApplication += interstitialHandleOnAdLeavingApplication;
        interstitial.OnAdClosed += interstitialHandleOnAdClosed;
        interstitial.OnAdOpening += interstitialHandleOnAdOpened;
        interstitial.LoadAd(request);
    }
    private void loadRewarded()
    {
        rewarded = RewardBasedVideoAd.Instance;
        //rewarded.OnAdClosed no handler
        rewarded.OnAdFailedToLoad += rewardedHandleOnAdFailedToLoad;
        rewarded.OnAdLeavingApplication += rewardedHandleOnAdLeavingApplication;
        rewarded.OnAdLoaded += rewardedHandleOnAdLoaded;
        rewarded.OnAdOpening += rewardedHandleOnAdOpened;
        rewarded.OnAdRewarded += rewardedRewardBasedVideoRewarded;
        //rewarded.OnAdStarted no handler
        rewarded.LoadAd(request, Settings.adMobRewardedId);
    }
    //private void reloadRewarded()
    //{
    //    request = new AdRequest.Builder().Build();
    //    rewarded.LoadAd(request, Settings.adMobRewardedId);
    //}

    #endregion

    void OnDestroy()
    {
        banner.Hide();
        banner.OnAdLoaded -= this.bannerHandleOnAdLoaded;
        banner.OnAdFailedToLoad -= this.bannerHandleOnAdFailedToLoad;
        banner.OnAdOpening -= this.bannerHandleOnAdOpened;
        banner.OnAdClosed -= this.bannerHandleOnAdClosed;
        banner.OnAdLeavingApplication -= this.bannerHandleOnAdLeavingApplication;
        banner.Destroy();
        interstitial.OnAdLoaded -= interstitialHandleOnAdLoaded;
        interstitial.OnAdFailedToLoad -= interstitialHandleOnAdFailedToLoad;
        interstitial.OnAdLeavingApplication -= interstitialHandleOnAdLeavingApplication;
        interstitial.OnAdClosed -= interstitialHandleOnAdClosed;
        interstitial.OnAdOpening -= interstitialHandleOnAdOpened;
        interstitial.Destroy();
        rewarded.OnAdFailedToLoad -= rewardedHandleOnAdFailedToLoad;
        rewarded.OnAdLeavingApplication -= rewardedHandleOnAdLeavingApplication;
        rewarded.OnAdLoaded -= rewardedHandleOnAdLoaded;
        rewarded.OnAdOpening -= rewardedHandleOnAdOpened;
        rewarded.OnAdRewarded -= rewardedRewardBasedVideoRewarded;
    }

    # region Banner handling

    public void bannerHandleOnAdLoaded(object sender, EventArgs args)
    {
        Engine.Events.AdLoaded(PlacementType.banner);
    }
    public void bannerHandleOnAdFailedToLoad(object sender, AdFailedToLoadEventArgs args)
    {
        Debug.Log("HandleFailedToReceiveAd event received with message: "
                            + args.Message);
        Logger.AddContent(UILogDataType.Monetization, "Load fail event handling");
        Engine.Events.AdFailed(PlacementType.banner);
        GameAnalytics.NewErrorEvent(GAErrorSeverity.Error, "Banner ad load error:" + Environment.NewLine + args.Message);
    }
    public void bannerHandleOnAdOpened(object sender, EventArgs args)
    {
        Engine.Events.AdOpened(PlacementType.banner);
        GameAnalytics.NewAdEvent(GAAdAction.Show, GAAdType.Banner, "AdMob", PlacementType.banner.ToString());
    }
    public void bannerHandleOnAdClosed(object sender, EventArgs args)
    {
        Engine.Events.AdSkipped(PlacementType.banner);
    }
    public void bannerHandleOnAdLeavingApplication(object sender, EventArgs args)
    {
        Engine.Events.AdUserLeave(PlacementType.banner);
    }
    
    #endregion

    #region Interstitial handling

    public void interstitialHandleOnAdLoaded(object sender, EventArgs args)
    {
        Engine.Events.AdLoaded(PlacementType.interstitial);
    }
    public void interstitialHandleOnAdFailedToLoad(object sender, AdFailedToLoadEventArgs args)
    {
        Debug.Log("HandleFailedToReceiveAd event received with message: "
                            + args.Message);
        Engine.Events.AdFailed(PlacementType.interstitial);
        GameAnalytics.NewErrorEvent(GAErrorSeverity.Error, "Interstitial ad load error:" + Environment.NewLine + args.Message);
    }
    public void interstitialHandleOnAdOpened(object sender, EventArgs args)
    {
        Engine.Events.AdOpened(PlacementType.interstitial);
    }
    public void interstitialHandleOnAdClosed(object sender, EventArgs args)
    {
        Engine.Events.AdFinished(PlacementType.interstitial);
        GameAnalytics.NewAdEvent(GAAdAction.Show, GAAdType.Interstitial, "AdMob", PlacementType.interstitial.ToString());
        interstitial.LoadAd(request);
    }
    public void interstitialHandleOnAdLeavingApplication(object sender, EventArgs args)
    {
        Engine.Events.AdUserLeave(PlacementType.interstitial);
    }

    #endregion

    #region Rewarded handling

    public void rewardedHandleOnAdLoaded(object sender, EventArgs args)
    {
        Engine.Events.AdLoaded(PlacementType.rewardedVideo);
    }
    public void rewardedHandleOnAdFailedToLoad(object sender, AdFailedToLoadEventArgs args)
    {
        Debug.Log("HandleFailedToReceiveAd event received with message: "
                            + args.Message);
        Engine.Events.AdFailed(PlacementType.rewardedVideo);
        GameAnalytics.NewErrorEvent(GAErrorSeverity.Error, "Rewarded ad load error:" + Environment.NewLine + args.Message);
    }
    public void rewardedHandleOnAdOpened(object sender, EventArgs args)
    {
        Engine.Events.AdOpened(PlacementType.rewardedVideo);
    }
    public void rewardedHandleOnAdLeavingApplication(object sender, EventArgs args)
    {
        Engine.Events.AdUserLeave(PlacementType.rewardedVideo);
    }
    public void rewardedRewardBasedVideoRewarded(object sender, Reward args)
    {
        Engine.Events.AdFinished(PlacementType.rewardedVideo);
        GameAnalytics.NewAdEvent(GAAdAction.Show, GAAdType.RewardedVideo, "AdMob", PlacementType.rewardedVideo.ToString());
        rewarded.LoadAd(request, Settings.adMobRewardedId);
    }

    #endregion
}