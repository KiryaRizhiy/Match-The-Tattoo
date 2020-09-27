using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Advertisements;
using UnityEngine.EventSystems;

public class UserInteraction : MonoBehaviour
{
    public UserInteractionTypes Type;

    private Transform GDPRPanel
    {
        get
        {
            return transform.GetChild(2);
        }
    }

    void Update()
    {
    }
    void Awake()
    {
        Engine.Events.initialized += HideGDPRPanel;
        HideGDPRPanel();
    }
    void Start()
    {
    }
    void OnDestroy()
    {
        Engine.Events.initialized -= HideGDPRPanel;
    }

    public void NextLevel()
    {
        Engine.CoreLevelDone();
    }
    public void Restart()
    {
        Engine.RestartLevel();
    }
    public void SwitchPause()
    {
        Engine.SwitchPause();
    }
    public void ShowPrivacyPolicy()
    {
        Application.OpenURL(Settings.privacyPolicyLink);
    }
    public void RewardedWathced()
    {
        Engine.Events.AdFinished(PlacementType.rewardedVideo);
    }
    public void InterstitialWatched()
    {
        Engine.Events.AdFinished(PlacementType.interstitial);
    }
    public void Play()
    {
        Engine.StartCoreGameplay();
    }
    public void Quit()
    {
        Engine.QuitApplcation();
    }
    public void ToMainMenu()
    {
        Engine.QuitCoreGameplay();
    }
    public void ClearSaveFile()
    {
        Engine.ClearSaveFile();
    }
    public void AcceptGDPR()
    {
        Engine.AcceptGDPR();
        HideGDPRPanel();
    }
    public void HideGDPRPanel()
    {
        if (Engine.initialized && GDPRPanel.name == "GDPRAccept")
            GDPRPanel.gameObject.SetActive(!Engine.meta.GDPRAccepted);
    }
}
public enum UserInteractionTypes {Core, Lobby}