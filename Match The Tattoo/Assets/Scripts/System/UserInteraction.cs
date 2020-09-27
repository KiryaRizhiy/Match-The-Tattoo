using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Advertisements;
using UnityEngine.EventSystems;

public class UserInteraction : MonoBehaviour
{
    public UserInteractionTypes Type;
    public bool isLobbyShouldBeSkipped;

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
        if (Type == UserInteractionTypes.Lobby)
        {
            Engine.Events.initialized += HideGDPRPanel;
            Engine.Events.gdprAccepted += SkipLobby;
            Engine.Events.loadingCompleted += SkipLobby;
            HideGDPRPanel();
        }
    }
    void Start()
    {
    }
    void OnDestroy()
    {
        if (Type == UserInteractionTypes.Lobby)
        {
            Engine.Events.initialized -= HideGDPRPanel;
            Engine.Events.gdprAccepted -= SkipLobby;
            Engine.Events.loadingCompleted -= SkipLobby;
        }
    }
    public void SkipLobby()
    {
        if (!isLobbyShouldBeSkipped || Type != UserInteractionTypes.Lobby)
            return;
        if (Engine.initialized && Engine.meta.GDPRAccepted)
            Play();
    }
    public void LevelWon()
    {
        Engine.Events.CoreReadyToChangeState(GameSessionState.Won);
    }
    public void Restart()
    {
        Engine.Events.CoreReadyToChangeState(GameSessionState.Lost);
        Engine.Events.CoreReadyToSwitchLevel();
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