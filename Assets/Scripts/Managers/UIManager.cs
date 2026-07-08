using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private PopupManager popupManager;
    [SerializeField] private JSFunctCalls jsFunctCalls;

    [Header("Loading & Intro")]
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private GameObject gameScreen;
    [SerializeField] private float initializationTimeout = 20f;

    [Header("Backgrounds")]
    [SerializeField] private GameObject normalSpinBackground;

    [Header("Bet Controls")]
    [SerializeField] private TMP_Text betAmountText;
    [SerializeField] private Button betPlusButton;
    [SerializeField] private Button betMinusButton;

    [Header("Max Bet Indicator")]
    [SerializeField] private GameObject maxBetObject;
    [SerializeField] private float maxBetDisplayDuration = 1f;

    [Header("Balance & Win")]
    [SerializeField] private TMP_Text balanceText;
    [SerializeField] private TMP_Text winAmountText;

    [Header("Display Panel - Game Rules & Win")]
    [SerializeField] private GameObject displayPanel;
    [SerializeField] private GameObject gameRuleObject;
    [SerializeField] private GameObject winDisplayObject;
    [SerializeField] private TMP_Text winDisplayText;
    [SerializeField] private float winDisplayDuration = 1f;

    [Header("Win Popup Panel")]
    [SerializeField] private GameObject winPopupPanel;
    [SerializeField] private GameObject winRingObject;
    [SerializeField] private ImageAnimation winPopupImageAnimation;
    [SerializeField] private RectTransform winPopupImageRect;
    [SerializeField] private TMP_Text winPopupText;
    [SerializeField] private List<Sprite> niceWinSprites;
    [SerializeField] private List<Sprite> bigWinSprites;
    [SerializeField] private List<Sprite> megaWinSprites;
    [SerializeField] private List<Sprite> superWinSprites;
    [SerializeField] private List<Sprite> ultimateWinSprites;

    [Header("Spin Button")]
    [SerializeField] private Button spinButton;
    [SerializeField] private Button stopButton;

    [Header("Auto Play")]
    [SerializeField] private Button autoPlayStartButton;



    [Header("Audio Toggles")]
    [Tooltip("Toggle for background music on/off.")]
    [SerializeField] private Toggle musicToggle;
    [Tooltip("Toggle for all SFX sounds on/off.")]
    [SerializeField] private Toggle sfxToggle;

    [Header("Game Rules Panel")]
    [SerializeField] private GameObject gameRulesPanel;
    [SerializeField] private RectTransform gameRulesPanelRect;
    [SerializeField] private Button gameRulesOpenButton;
    [SerializeField] private Button gameRulesBackButton;
    [SerializeField] private Button gameRulesNextPageButton;
    [SerializeField] private Button gameRulesPrevPageButton;

    [Tooltip("Assign exactly 6 page RectTransforms that live inside the panel.")]
    [SerializeField] private RectTransform[] gameRulePages;
    [SerializeField] private float pageSlideWidth = 800f;
    [SerializeField] private GameObject[] rulePageIndicators;

    [Header("Game Rules Dynamic Texts")]

    [SerializeField] private TMP_Text ruleSymbol0Text;
    [SerializeField] private TMP_Text ruleSymbol1Text;
    [SerializeField] private TMP_Text ruleSymbol2Text;
    [SerializeField] private TMP_Text ruleSymbol3Text;
    [SerializeField] private TMP_Text ruleSymbol4Text;
    [SerializeField] private TMP_Text ruleSymbol5Text;
    [SerializeField] private TMP_Text ruleSymbol6Text;
    [SerializeField] private TMP_Text ruleSymbol7Text;
    [SerializeField] private TMP_Text ruleSymbol8Text;
    [SerializeField] private TMP_Text ruleSymbol9Text;
    [SerializeField] private TMP_Text ruleSymbol10Text;
    [Header("Animation Settings")]
    [SerializeField] private float winCountDuration = 0.25f;
    [SerializeField] private float balanceCountDuration = 1.0f;

    [Header("Expand-Shrink Controls")]
    [SerializeField] private Button expandButton;
    [SerializeField] private Button shrinkButton;
    private bool isExpanded = false;

    private int selectedRounds = 10;
    private Tween balanceTween;
    private Tween winTween;

    private Coroutine maxBetCoroutine;

    // Optimistic balance: the locally-deducted balance shown while the spin is in flight
    private double optimisticBalance = 0;
    private bool hasOptimisticBalance = false;

    [Header("Rapid Stop Cooldown")]
    [Tooltip("Seconds the player must wait before pressing Stop again after an immediate stop.")]
    [SerializeField] private float rapidStopCooldown = 1f;
    private float lastRapidStopTime = -99f;
    private Coroutine winDisplayCoroutine;

    private int currentRulesPage = 0;
    private bool isPageAnimating;
    private double currentWinDisplayValue = 0;
    private bool isSpecialWinActive = false;
    public bool IsSpecialWinActive => isSpecialWinActive;
    public System.Action OnSpecialWinComplete;

    #region Initialization

    private void Start()
    {
        SetupButtons();
        SetupAutoPlayPanel();
        SetupSettingsPanel();
        SetupGameRulesPanel();

        InitializeDisplayPanel();
InitializeExpandShrink();

        InitializeBackgrounds();
        StartCoroutine(LoadingSequence());
        RegisterFullscreenListener();
    }

    private void InitializeBackgrounds()
    {
        if (normalSpinBackground) normalSpinBackground.SetActive(true);
    }

    private void InitializeUI()
    {
        if (spinButton) spinButton.gameObject.SetActive(true);
        if (stopButton) stopButton.gameObject.SetActive(false);

        if (maxBetObject) maxBetObject.SetActive(false);


        if (gameRulesPanel) gameRulesPanel.SetActive(false);
        if (winPopupPanel) winPopupPanel.SetActive(false);
        if (winRingObject) winRingObject.SetActive(false);
    }

    private void InitializeDisplayPanel()
    {
        if (gameRuleObject) gameRuleObject.SetActive(true);
        if (winDisplayObject) winDisplayObject.SetActive(false);
    }

    #endregion

    #region Loading & Intro Sequence

    private IEnumerator LoadingSequence()
    {
        if (loadingScreen) loadingScreen.SetActive(false);
        if (gameScreen) gameScreen.SetActive(false);

        // --- Wait for Initialization ---
        float timer = 0f;
        while (!gameManager.isInitialized && !gameManager.initializationFailed && timer < initializationTimeout)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (gameManager.initializationFailed || !gameManager.isInitialized)
        {
            if (gameManager.socketManager != null)
            {
                gameManager.socketManager.SetRaycastBlocker(false);
            }

            if (popupManager != null)
            {
                string errorMsg = gameManager.initializationFailed ? "Game failed to initialize." : "Initialization timed out. Please check your connection.";
                popupManager.ShowErrorPopup("Connection Error", errorMsg, true);
            }
            yield break;
        }
        // ------------------------------

        if (gameScreen) gameScreen.SetActive(true);
        AudioManager.Instance?.PlayBgMusic();
        InitializeUI();
    }

    #endregion

    #region Button Setup

    private void SetupButtons()
    {
        if (betPlusButton)  betPlusButton.onClick.AddListener(() => { AudioManager.Instance?.PlayBetPlus();  gameManager.IncreaseBet(); });
        if (betMinusButton) betMinusButton.onClick.AddListener(() => { AudioManager.Instance?.PlayBetMinus(); gameManager.DecreaseBet(); });
        if (spinButton) spinButton.onClick.AddListener(OnSpinButtonPressed);
        if (stopButton) stopButton.onClick.AddListener(OnStopButtonPressed);

        if (autoPlayStartButton)  autoPlayStartButton.onClick.AddListener(() => { AudioManager.Instance?.PlayButton(); ToggleAutoPlay(); });

        if(expandButton) expandButton.onClick.AddListener(() => { AudioManager.Instance?.PlayButton(); OnExpand(); });
        if(shrinkButton) shrinkButton.onClick.AddListener(() => { AudioManager.Instance?.PlayButton(); OnShrink(); });
    }

    private void SetupAutoPlayPanel()
    {
    }

    private void SetupSettingsPanel()
    {
        // Audio toggles — restore state from AudioManager then wire callbacks
        if (musicToggle)
        {
            if (AudioManager.Instance != null)
                musicToggle.isOn = AudioManager.Instance.MusicEnabled;
            musicToggle.onValueChanged.AddListener(OnMusicToggleChanged);
            RefreshToggleBgAlpha(musicToggle);
        }
        if (sfxToggle)
        {
            if (AudioManager.Instance != null)
                sfxToggle.isOn = AudioManager.Instance.SfxEnabled;
            sfxToggle.onValueChanged.AddListener(OnSfxToggleChanged);
            RefreshToggleBgAlpha(sfxToggle);
        }
    }

    private void SetupGameRulesPanel()
    {
        if (gameRulesOpenButton) gameRulesOpenButton.onClick.AddListener(OpenGameRulesPanel);
        if (gameRulesBackButton) gameRulesBackButton.onClick.AddListener(() => { AudioManager.Instance?.PlayPopupClose(); CloseGameRulesPanel(); });
        if (gameRulesNextPageButton) gameRulesNextPageButton.onClick.AddListener(NextRulesPage);
        if (gameRulesPrevPageButton) gameRulesPrevPageButton.onClick.AddListener(PrevRulesPage);
    }

    #endregion

    #region Game Events

    internal void OnGameInitialized()
    {
        currentWinDisplayValue = 0;
        UpdateBetDisplay();
        UpdateBalanceDisplay();
        UpdateWinDisplay(0);
    }

    internal void OnSpinStarted()
    {
        AudioManager.Instance?.PlaySpinStart();

        if (spinButton) spinButton.gameObject.SetActive(false);
        if (stopButton)
        {
            stopButton.gameObject.SetActive(true);
            stopButton.interactable = true;
        }

        SetBetControlsEnabled(false);
        if (autoPlayStartButton) autoPlayStartButton.interactable = gameManager.isAutoPlaying;

        // Stop win popup BG loop if a new spin starts while popup is still showing
        AudioManager.Instance?.StopWinPopupBg();

        if (winDisplayCoroutine != null)
        {
            StopCoroutine(winDisplayCoroutine);
            winDisplayCoroutine = null;
        }
        if (winDisplayObject) winDisplayObject.SetActive(false);
        if (winPopupPanel)
        {
            winPopupPanel.SetActive(false);
            if (winPopupImageAnimation) winPopupImageAnimation.StopAnimation();
        }
        if (winRingObject) winRingObject.SetActive(false);
        if (gameRuleObject) gameRuleObject.SetActive(true);
        isSpecialWinActive = false;

        // --- Optimistic balance deduction ---
        if (gameManager.gameConfig != null && gameManager.playerData != null)
        {
            double totalBet = gameManager.currentBetAmount * gameManager.gameConfig.betMultiplier;
            optimisticBalance = gameManager.playerData.balance - totalBet;
            hasOptimisticBalance = true;

            // Kill any previous balance tween and show deducted value immediately
            if (balanceTween != null) balanceTween.Kill();
            if (balanceText != null) balanceText.text = optimisticBalance.ToString("F2");
        }
        else
        {
            hasOptimisticBalance = false;
        }
        // ------------------------------------

        UpdateWinDisplay(0);
    }

    private bool earlyBigWinPopupTriggered = false;

    internal void TriggerBigWinPopupEarly(SpinResult result, System.Action onComplete = null)
    {
        double totalBetAmount = gameManager.currentBetAmount;
        if (gameManager.gameConfig != null)
        {
            totalBetAmount *= gameManager.gameConfig.betMultiplier;
        }
        
        // Always calculate multiplier for popup level based on the current result's winAmount
        double winAmount = result.winAmount;
        double multiplier = totalBetAmount > 0 ? (winAmount / totalBetAmount) : 0;

        bool skipScreen = false;

        if (multiplier >= 5 && !skipScreen)
        {
            earlyBigWinPopupTriggered = true;
            if (winDisplayCoroutine != null) StopCoroutine(winDisplayCoroutine);
            winDisplayCoroutine = StartCoroutine(ShowWinDisplayCoroutine(result, onComplete));
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    internal void OnSpinStopping(SpinResult result)
    {
        // If a special win popup is active, the balance is updated in sync with
        // the popup counter inside ShowWinDisplayCoroutine — skip it here.
        if (!isSpecialWinActive)
        {
            AnimateBalanceUpdate(result.playerData.balance);
        }

        double targetWin = result.winAmount;

        if (result.winAmount > 0)
        {
            if (!earlyBigWinPopupTriggered)
            {
                AnimateWinUpdate(targetWin);
            }
            
            double totalBetAmount = gameManager.currentBetAmount;
            if (gameManager.gameConfig != null)
            {
                totalBetAmount *= gameManager.gameConfig.betMultiplier;
            }
            double multiplier = totalBetAmount > 0 ? (result.winAmount / totalBetAmount) : 0;

            if (multiplier < 5 || !earlyBigWinPopupTriggered)
            {
                ShowWinDisplay(result);
            }
            earlyBigWinPopupTriggered = false;
        }
        else
        {
            UpdateWinDisplay(targetWin);
            earlyBigWinPopupTriggered = false;
        }
    }

    internal void OnSpinCompleted(SpinResult result)
    {
        if (isSpecialWinActive) return;

        if (gameManager.isAutoPlaying)
        {
            if (spinButton) spinButton.gameObject.SetActive(false);
            if (stopButton)
            {
                stopButton.gameObject.SetActive(true);
                stopButton.interactable = true;
            }
            if (autoPlayStartButton) autoPlayStartButton.interactable = true;
        }
        else
        {
            if (spinButton)
            {
                spinButton.gameObject.SetActive(true);
                spinButton.interactable = true;
            }
            if (stopButton) stopButton.gameObject.SetActive(false);

            SetBetControlsEnabled(true);
            if (autoPlayStartButton) autoPlayStartButton.interactable = true;
        }
    }

    internal void DisableControlsDuringWinAnimation()
    {
        SetBetControlsEnabled(false);
        if (spinButton) spinButton.interactable = false;
        if (stopButton) stopButton.interactable = false;
        
        if (gameManager != null && gameManager.lastResult != null)
        {
            double winAmount = gameManager.lastResult.winAmount;
            double totalBetAmount = gameManager.currentBetAmount;
            if (gameManager.gameConfig != null)
            {
                totalBetAmount *= gameManager.gameConfig.betMultiplier;
            }
            double multiplier = totalBetAmount > 0 ? (winAmount / totalBetAmount) : 0;
            
            bool skipScreen = false;

            if (multiplier >= 5 && !skipScreen)
            {
                if (winRingObject) winRingObject.SetActive(true);
            }
        }
    }

    internal void EnableControlsAfterWinAnimation()
    {
        if (isSpecialWinActive) return;

        if (gameManager.isAutoPlaying)
        {
            if (spinButton) spinButton.gameObject.SetActive(false);
            if (stopButton)
            {
                stopButton.gameObject.SetActive(true);
                stopButton.interactable = true;
            }
            if (autoPlayStartButton) autoPlayStartButton.interactable = true;
        }
        else
        {
            SetBetControlsEnabled(true);
            if (spinButton)
            {
                spinButton.gameObject.SetActive(true);
                spinButton.interactable = true;
            }
            if (stopButton) stopButton.gameObject.SetActive(false);
            if (autoPlayStartButton) autoPlayStartButton.interactable = true;
        }
    }

    private void ShowWinDisplay(SpinResult result)
    {
        if (winDisplayCoroutine != null) StopCoroutine(winDisplayCoroutine);
        winDisplayCoroutine = StartCoroutine(ShowWinDisplayCoroutine(result));
    }

    private IEnumerator ShowWinDisplayCoroutine(SpinResult result, System.Action onComplete = null)
    {
        double winAmount = result.winAmount;
        double totalBetAmount = gameManager.currentBetAmount;
        if (gameManager.gameConfig != null)
        {
            totalBetAmount *= gameManager.gameConfig.betMultiplier;
        }
        
        double multiplier = totalBetAmount > 0 ? (winAmount / totalBetAmount) : 0;
        bool skipScreen = false;

        // --- Capping Logic ---
        double startVal = 0;
        double endVal = winAmount;
        double popupWinAmount = winAmount;

        if (multiplier < 5 || skipScreen)
        {
            if (gameRuleObject) gameRuleObject.SetActive(false);
            if (winDisplayObject) winDisplayObject.SetActive(true);

            // Use the authoritative target win for the small display label
            if (winDisplayText) winDisplayText.text = $"WIN {endVal:F2}";

            AudioManager.Instance?.PlayWinNormal();

            yield return new WaitForSeconds(winDisplayDuration);

            if (winDisplayObject) winDisplayObject.SetActive(false);
            if (gameRuleObject) gameRuleObject.SetActive(true);
            if (winRingObject) winRingObject.SetActive(false);
            winDisplayCoroutine = null;
            yield break;
        }

        // --- Special Win Triggered ---
        isSpecialWinActive = true;
        DisableControlsDuringWinAnimation();

        // Big Win Popup Logic — based on the spin's winAmount field
        AudioManager.Instance?.PlayWinOpeningJingle(multiplier);
        AudioManager.Instance?.PlayWinPopupBg(multiplier);

        if (gameRuleObject) gameRuleObject.SetActive(false);
        
        if (winDisplayObject) 
        {
            winDisplayObject.SetActive(true);
            if (winDisplayText) winDisplayText.text = $"WIN {startVal:F2}";
        }

        List<Sprite> selectedSprites = null;
        float popupTime = 0f;

        if (multiplier >= 100) {
            selectedSprites = ultimateWinSprites;
            popupTime = 15f;
        } else if (multiplier >= 50) {
            selectedSprites = superWinSprites;
            popupTime = 12f;
        } else if (multiplier >= 25) {
            selectedSprites = megaWinSprites;
            popupTime = 8f;
        } else if (multiplier >= 10) {
            selectedSprites = bigWinSprites;
            popupTime = 8f;
        } else {
            selectedSprites = niceWinSprites;
            popupTime = 6f;
        }

        if (winPopupImageAnimation)
        {
            winPopupImageAnimation.textureArray = selectedSprites;
        }

        if (winPopupPanel) winPopupPanel.SetActive(true);

        if (winPopupImageAnimation)
        {
            winPopupImageAnimation.StartAnimation();
        }

        float animDuration = popupTime - 1f;

        if (winPopupImageRect)
        {
            winPopupImageRect.localScale = Vector3.zero;
            winPopupImageRect.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack).OnComplete(() => {
                winPopupImageRect.DOScale(new Vector3(1.2f, 1.2f, 1.2f), animDuration - 0.5f).SetEase(Ease.Linear);
            });
        }

        if (winPopupText)
        {
            winPopupText.text = "0.00";
            float currentAnimVal = (float)startVal;

            // Start balance animation in sync with the popup counter so both
            // count up together and finish at the same time.
            AnimateBalanceUpdate(result.playerData.balance, animDuration);

            DOTween.To(() => currentAnimVal, x => {
                currentAnimVal = x;
                
                // 1. Calculate progress from startVal to endVal
                double range = endVal - startVal;
                float progress = range > 0 ? (float)((currentAnimVal - startVal) / range) : 1f;
                progress = Mathf.Clamp01(progress);

                // 2. Update popup text based on spin win amount
                double currentPopupHit = progress * popupWinAmount;
                winPopupText.text = currentPopupHit.ToString("F2");

                // 3. Update main UI displays based on authoritative round total
                string formattedTotal = ((double)currentAnimVal).ToString("F2");
                if (winAmountText) winAmountText.text = formattedTotal;
                if (winDisplayText) winDisplayText.text = $"WIN {formattedTotal}";
                
                currentWinDisplayValue = (double)currentAnimVal;
            }, (float)endVal, animDuration).SetEase(Ease.OutQuad);
        }

        yield return new WaitForSeconds(popupTime);

        // Popup auto-closed — stop the looping BG
        AudioManager.Instance?.StopWinPopupBg();

        if (winPopupPanel) winPopupPanel.SetActive(false);
        if (winPopupImageAnimation) winPopupImageAnimation.StopAnimation();
        if (winRingObject) winRingObject.SetActive(false);
        
        // --- Keep normal win display visible for a moment after special win popup closes ---
        if (winDisplayObject && winDisplayObject.activeSelf)
        {
            yield return new WaitForSeconds(winDisplayDuration);
            winDisplayObject.SetActive(false);
        }

        if (gameRuleObject) gameRuleObject.SetActive(true);

        // --- Reset Controls ---
        isSpecialWinActive = false;
        EnableControlsAfterWinAnimation();
        OnSpinCompleted(null);

        onComplete?.Invoke();
        OnSpecialWinComplete?.Invoke();

        winDisplayCoroutine = null;
    }

    #endregion

    #region Spin Button

    private void OnSpinButtonPressed()
    {
        if (!gameManager.IsSpinning() && !gameManager.isAutoPlaying)
        {
            gameManager.RequestSpin();
        }
    }

    private void OnStopButtonPressed()
    {
        if (gameManager.IsSpinning())
        {
            // Rapid-stop cooldown: prevent the player from spamming the stop button
            if (Time.unscaledTime - lastRapidStopTime < rapidStopCooldown)
                return;

            lastRapidStopTime = Time.unscaledTime;
            gameManager.RequestStop();
        }
    }

    /// <summary>
    /// Called by GameManager.RequestStop when a forced/manual stop is accepted.
    /// Disables the stop button so the player cannot spam.
    /// Re-enabled in OnSpinCompleted once all reels have settled.
    /// </summary>
    internal void DisableSpinButtonDuringStop()
    {
        if (stopButton) stopButton.interactable = false;
    }

    #endregion

    #region Bet Controls

    internal void UpdateBetDisplay()
    {
        if (gameManager.gameConfig == null) return;

        double totalBetAmount = gameManager.currentBetAmount * gameManager.gameConfig.betMultiplier;

        if (betAmountText)
            betAmountText.text = totalBetAmount.ToString("F2");
        UpdateBetButtonStates();
        CheckMaxBetIndicator();
        UpdateGameRulesDynamicTexts();
    }

    private void UpdateBetButtonStates()
    {
        if (betMinusButton) betMinusButton.interactable = true;
        if (betPlusButton) betPlusButton.interactable = true;
    }

    private void CheckMaxBetIndicator()
    {
        bool isMaxBet = gameManager.currentBetIndex >= gameManager.gameConfig.availableBets.Count - 1;

        if (isMaxBet && maxBetObject && !maxBetObject.activeSelf)
        {
            if (maxBetCoroutine != null) StopCoroutine(maxBetCoroutine);
            maxBetCoroutine = StartCoroutine(ShowMaxBetIndicator());
        }
        else if (!isMaxBet && maxBetObject && maxBetObject.activeSelf)
        {
            maxBetObject.SetActive(false);
            if (maxBetCoroutine != null)
            {
                StopCoroutine(maxBetCoroutine);
                maxBetCoroutine = null;
            }
        }
    }

    private IEnumerator ShowMaxBetIndicator()
    {
        AudioManager.Instance?.PlayMaxBet();
        if (maxBetObject) maxBetObject.SetActive(true);
        yield return new WaitForSeconds(maxBetDisplayDuration);
        if (maxBetObject) maxBetObject.SetActive(false);
        maxBetCoroutine = null;
    }

    #endregion

    #region Auto Play Panel

    private void ToggleAutoPlay()
    {
        if (gameManager.isAutoPlaying)
        {
            gameManager.StopAutoPlay();
        }
        else
        {
            gameManager.StartAutoPlay(999999);
        }
    }

    internal void OnAutoPlayStarted()
    {
        if (spinButton) spinButton.gameObject.SetActive(false);
        if (stopButton)
        {
            stopButton.gameObject.SetActive(true);
            stopButton.interactable = true;
        }
        SetBetControlsEnabled(false);
        if (autoPlayStartButton) autoPlayStartButton.interactable = true;
    }

    internal void OnAutoPlayStopped()
    {
        // If game is not spinning and no round is in progress, restore controls
        bool isRoundActive = gameManager.IsSpinning() || gameManager.lastResult != null;

        if (!isRoundActive)
        {
            if (spinButton)
            {
                spinButton.gameObject.SetActive(true);
                spinButton.interactable = true;
            }
            if (stopButton) stopButton.gameObject.SetActive(false);

            SetBetControlsEnabled(true);
            if (autoPlayStartButton)    autoPlayStartButton.interactable    = true;
        }
        else if (isRoundActive)
        {
            // If round is active, we just requested to stop autoplay.
            // Disable stop button so user knows it's stopping.
            if (stopButton) stopButton.interactable = false;
        }
    }

    #endregion

    #region Audio Toggle Logic

    private void OnMusicToggleChanged(bool isOn)
    {
        AudioManager.Instance?.PlayButton();
        AudioManager.Instance?.SetMusicEnabled(isOn);
        RefreshToggleBgAlpha(musicToggle);
    }

    private void OnSfxToggleChanged(bool isOn)
    {
        AudioManager.Instance?.PlayButton();
        AudioManager.Instance?.SetSfxEnabled(isOn);
        RefreshToggleBgAlpha(sfxToggle);
    }

    private static void RefreshToggleBgAlpha(Toggle toggle)
    {
        if (toggle == null) return;
        Image bgImage = toggle.targetGraphic as Image;
        if (bgImage == null) return;
        Color c = bgImage.color;
        c.a = toggle.isOn ? 0f : 1f;
        bgImage.color = c;
    }

    #endregion

    #region Game Rules Panel

    private void OpenGameRulesPanel()
    {
        ShowGameRulesPanel();
    }

    private void ShowGameRulesPanel()
    {
        if (gameRulesPanel == null) return;

        currentRulesPage = 0;
        isPageAnimating = false;

        if (gameRulePages != null)
        {
            for (int i = 0; i < gameRulePages.Length; i++)
            {
                if (gameRulePages[i] == null) continue;
                gameRulePages[i].gameObject.SetActive(true);
                gameRulePages[i].anchoredPosition = new Vector2(i * pageSlideWidth, 0f);
            }
        }

        UpdateRulePageIndicators(currentRulesPage);

        gameRulesPanel.SetActive(true);

        if (gameRulesPanelRect)
        {
            gameRulesPanelRect.anchoredPosition = new Vector2(Screen.width, gameRulesPanelRect.anchoredPosition.y);
            gameRulesPanelRect.DOAnchorPosX(0f, 0.35f).SetEase(Ease.OutCubic);
        }

        UpdateGameRulesDynamicTexts();
    }

    private void CloseGameRulesPanel()
    {
        if (gameRulesPanel == null || !gameRulesPanel.activeSelf) return;

        if (gameRulesPanelRect)
        {
            gameRulesPanelRect.DOAnchorPosX(Screen.width, 0.35f)
                .SetEase(Ease.InCubic)
                .OnComplete(() =>
                {
                    gameRulesPanel.SetActive(false);
                    if (gameRulesPanelRect)
                        gameRulesPanelRect.anchoredPosition = new Vector2(0f, gameRulesPanelRect.anchoredPosition.y);
                });
        }
        else
        {
            gameRulesPanel.SetActive(false);
        }
    }

    private void NextRulesPage()
    {
        if (isPageAnimating || gameRulePages == null || gameRulePages.Length == 0) return;
        int next = (currentRulesPage + 1) % gameRulePages.Length;
        SlideToPage(currentRulesPage, next, slideLeft: true);
    }

    private void PrevRulesPage()
    {
        if (isPageAnimating || gameRulePages == null || gameRulePages.Length == 0) return;
        int prev = (currentRulesPage - 1 + gameRulePages.Length) % gameRulePages.Length;
        SlideToPage(currentRulesPage, prev, slideLeft: false);
    }

    private void SlideToPage(int fromIndex, int toIndex, bool slideLeft)
    {
        if (gameRulePages == null) return;
        if (fromIndex < 0 || fromIndex >= gameRulePages.Length) return;
        if (toIndex < 0 || toIndex >= gameRulePages.Length) return;

        RectTransform fromPage = gameRulePages[fromIndex];
        RectTransform toPage = gameRulePages[toIndex];
        if (fromPage == null || toPage == null) return;

        AudioManager.Instance?.PlayPageSwipe();
        isPageAnimating = true;

        float direction = slideLeft ? 1f : -1f;

        toPage.anchoredPosition = new Vector2(direction * pageSlideWidth, 0f);
        toPage.gameObject.SetActive(true);
        fromPage.anchoredPosition = new Vector2(0f, 0f);

        float slideDuration = 0.35f;

        fromPage.DOAnchorPosX(-direction * pageSlideWidth, slideDuration).SetEase(Ease.InOutCubic);

        toPage.DOAnchorPosX(0f, slideDuration)
            .SetEase(Ease.InOutCubic)
            .OnComplete(() =>
            {
                fromPage.anchoredPosition = new Vector2(direction * pageSlideWidth, 0f);
                currentRulesPage = toIndex;
                isPageAnimating = false;
                UpdateRulePageIndicators(currentRulesPage);
            });
    }

    private void UpdateRulePageIndicators(int activeIndex)
    {
        if (rulePageIndicators == null || rulePageIndicators.Length == 0) return;
        for (int i = 0; i < rulePageIndicators.Length; i++)
        {
            if (rulePageIndicators[i] == null) continue;
            // Enable the first child for the active index, disable for others
            if (rulePageIndicators[i].transform.childCount > 0)
            {
                rulePageIndicators[i].transform.GetChild(0).gameObject.SetActive(i == activeIndex);
            }
        }
    }

    #endregion


    #region Expand / Shrink

    private void InitializeExpandShrink()
    {

        SetExpandShrinkButtons(isExpanded: false);
    }

    private void OnExpand()
    {
        isExpanded = true;
        jsFunctCalls?.RequestExpandGame();
        SetExpandShrinkButtons(isExpanded: true);
    }

    private void OnShrink()
    {
        isExpanded = false;
        jsFunctCalls?.RequestShrinkGame();
        SetExpandShrinkButtons(isExpanded: false);
    }


    private void SetExpandShrinkButtons(bool isExpanded)
    {
        if (expandButton) expandButton.gameObject.SetActive(!isExpanded);
        if (shrinkButton) shrinkButton.gameObject.SetActive(isExpanded);
    }

    private void RegisterFullscreenListener()
    {
        jsFunctCalls?.RegisterFullscreenListener(gameObject.name);
    }
 internal void OnFullscreenChanged(string isFullscreen)
    {
        bool newExpandedState = isFullscreen == "1";
        Debug.Log($"[UI] OnFullscreenChanged callback: isFullscreen={isFullscreen}, newState={newExpandedState}");

        // Only update if state actually changed
        if (isExpanded != newExpandedState)
        {
            isExpanded = newExpandedState;
            SetExpandShrinkButtons(isExpanded);
            Debug.Log($"[UI] Button states synced to fullscreen: {(isExpanded ? "EXPANDED" : "SHRINK")}");
        }
    }
    
    #endregion

    #region Popup Animations (Generic)

    private void AnimatePopupOpen(RectTransform popupRect)
    {
        if (!popupRect) return;
        popupRect.localScale = Vector3.zero;
        popupRect.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
    }

    private void AnimatePopupClose(RectTransform popupRect, System.Action onComplete)
    {
        if (!popupRect) return;

        AudioManager.Instance?.PlayPopupClose();

        Sequence closeSeq = DOTween.Sequence();
        closeSeq.Append(popupRect.DOScale(1.1f, 0.1f));
        closeSeq.Append(popupRect.DOScale(0f, 0.2f).SetEase(Ease.InBack));
        closeSeq.OnComplete(() =>
        {
            popupRect.localScale = Vector3.one;
            onComplete?.Invoke();
        });
    }

    #endregion

    #region Display Updates

    private void UpdateBalanceDisplay()
    {
        if (balanceText)
            balanceText.text = gameManager.playerData.balance.ToString("F2");
    }

    private void UpdateWinDisplay(double amount)
    {
        currentWinDisplayValue = amount;
        if (winAmountText)
            winAmountText.text = amount.ToString("F2");
    }

    private void AnimateBalanceUpdate(double newBalance, float durationOverride = -1f)
    {
        if (balanceTween != null) balanceTween.Kill();

        // Start from the optimistic value (already shown at spin-start) if available,
        // otherwise start from the current stored player balance.
        double oldBalance = hasOptimisticBalance ? optimisticBalance : gameManager.playerData.balance;
        hasOptimisticBalance = false;

        float duration = durationOverride > 0f ? durationOverride : balanceCountDuration;

        // Always tween so the update is visibly confirmed when the server responds.
        balanceTween = DOTween.To(
            () => oldBalance,
            x => { if (balanceText != null) balanceText.text = x.ToString("F2"); },
            newBalance,
            duration
        ).SetEase(Ease.OutCubic)
         .OnComplete(() => { if (balanceText != null) balanceText.text = newBalance.ToString("F2"); });
    }

    private void AnimateWinUpdate(double winAmount)
    {
        if (winTween != null) winTween.Kill();

        if (winAmount > currentWinDisplayValue)
        {
            double startValue = currentWinDisplayValue;
            winTween = DOTween.To(
                () => startValue,
                x => UpdateWinDisplay(x),
                winAmount,
                winCountDuration
            ).SetEase(Ease.OutCubic)
             .OnComplete(() => UpdateWinDisplay(winAmount));
        }
        else
        {
            UpdateWinDisplay(winAmount);
        }
    }

    #endregion



    private void SetBetControlsEnabled(bool enabled)
    {
        if (betPlusButton) betPlusButton.interactable = enabled;
        if (betMinusButton) betMinusButton.interactable = enabled;
    }





    #region Dynamic Game Rules Updates

    private void UpdateGameRulesDynamicTexts()
    {
        if (gameManager.gameConfig == null) return;

        double totalBetAmount = gameManager.currentBetAmount * gameManager.gameConfig.betMultiplier;





        // 4. Symbol Multipliers
        // "5 - (currentbetamout*thatmultiper ) \n 4 - (currentbetamout*thatmultiper ) \n 3 - (currentbetamout*multiper )"
        TMP_Text[] symbolTexts = {
            ruleSymbol0Text, ruleSymbol1Text, ruleSymbol2Text, ruleSymbol3Text,
            ruleSymbol4Text, ruleSymbol5Text, ruleSymbol6Text, ruleSymbol7Text,
            ruleSymbol8Text, ruleSymbol9Text, ruleSymbol10Text
        };

        if (gameManager.gameConfig.symbols != null)
        {
            for (int i = 0; i < symbolTexts.Length; i++)
            {
                if (symbolTexts[i] == null) continue;

                // Find symbol by id
                var symbol = gameManager.gameConfig.symbols.Find(s => s.id == i);
                if (symbol != null)
                {
                    if (symbol.multipliers != null && symbol.multipliers.Count >= 3)
                    {
                        // multipliers list: index 0 is 5 matches, index 1 is 4 matches, index 2 is 3 matches
                        double originalBetAmount = gameManager.currentBetAmount;
                        double win5 = originalBetAmount * symbol.multipliers[0];
                        double win4 = originalBetAmount * symbol.multipliers[1];
                        double win3 = originalBetAmount * symbol.multipliers[2];

                        // Format nicely, e.g., F2 if decimal, or just let ToString format it based on game's styling
                        string text5 = $"5 - {win5.ToString("0.##")}";
                        string text4 = $"4 - {win4.ToString("0.##")}";
                        string text3 = $"3 - {win3.ToString("0.##")}";

                        symbolTexts[i].text = $"{text5}\n{text4}\n{text3}";
                    }
                    else if (symbol.payout > 0)
                    {
                        double originalBetAmount = gameManager.currentBetAmount;
                        double win3 = originalBetAmount * symbol.payout;
                        symbolTexts[i].text = $"3 - {win3.ToString("0.##")}";
                    }
                    else
                    {
                        symbolTexts[i].text = "";
                    }
                }
                else
                {
                    symbolTexts[i].text = "";
                }
            }
        }
    }

    #endregion



    #region Cleanup

    private void OnDestroy()
    {
        if (balanceTween != null) balanceTween.Kill();
        if (winTween != null) winTween.Kill();
        DOTween.KillAll();
    }

    #endregion

    #region Connection Popup Management


    private void OnExitButtonPressed()
    {
        if (gameManager != null) gameManager.ExitGame();
    }

    #endregion
}

[System.Serializable]
public class RoundButton
{
    public Button button;
    public int rounds;
    public GameObject selectedIndicator;
}