using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] internal SocketIOManager socketManager;
    [SerializeField] internal UIManager uiManager;
    [SerializeField] private PopupManager popupManager;
    [SerializeField] private SlotView slotView;

    [Header("Spin Settings")]
    [SerializeField] private float normalSpinDuration = 2.0f;

    internal GameConfig gameConfig;
    internal PlayerData playerData;
    internal SpinResult lastResult;

    internal GameState currentState;
    internal SpinSpeed currentSpinSpeed;

    internal int currentBetIndex;
    internal double currentBetAmount;

    internal bool isAutoPlaying;
    internal int autoPlayTotalRounds;
    internal int autoPlayRemainingRounds;

    internal bool isInitialized;
    internal bool initializationFailed;

    private Coroutine spinCoroutine;
    private bool stopRequested;
    private bool waitingForSpecialWin;
    internal bool WaitingForSpecialWin => waitingForSpecialWin;

    #region Initialization

    private void Start()
    {
        currentState = GameState.Initializing;
        currentSpinSpeed = SpinSpeed.Normal;
        isInitialized = false;
        initializationFailed = false;
    }

    internal void OnInitDataReceived(GameConfig config, PlayerData player, List<List<int>> initialMatrix)
    {
        gameConfig = config;
        playerData = player;
        currentBetIndex = playerData.currentBetIndex;
        UpdateBetAmount();

        if (initialMatrix != null && slotView != null)
        {
            slotView.SetInitialMatrix(initialMatrix);
        }

        isInitialized = true;
        currentState = GameState.Idle;

        uiManager.OnGameInitialized();
    }

    #endregion

    #region Bet Management

    internal void IncreaseBet()
    {
        if (currentState != GameState.Idle || isAutoPlaying) return;
        SetBetIndex((currentBetIndex + 1) % gameConfig.availableBets.Count);
    }

    internal void DecreaseBet()
    {
        if (currentState != GameState.Idle || isAutoPlaying) return;
        SetBetIndex((currentBetIndex - 1 + gameConfig.availableBets.Count) % gameConfig.availableBets.Count);
    }

    internal void SetBetIndex(int index)
    {
        currentBetIndex = index;
        UpdateBetAmount();
        uiManager.UpdateBetDisplay();
    }

    private void UpdateBetAmount()
    {
        currentBetAmount = gameConfig.availableBets[currentBetIndex];
    }

    #endregion

    #region Spin Control
    
    internal void RequestSpin()
    {
        if (currentState != GameState.Idle) return;
        if (!socketManager.isConnected) return;

        double totalBet = currentBetAmount * (gameConfig != null ? gameConfig.betMultiplier : 1);
        if (playerData.balance < totalBet)
        {
            if (popupManager != null)
            {
                popupManager.ShowInsufficientFundsError();
            }
            return;
        }

        StartSpin();
    }

    internal void RequestStop()
    {
        if (currentState == GameState.Spinning)
        {
            if (isAutoPlaying)
            {
                StopAutoPlay();
            }
            else
            {
                stopRequested = true;
                uiManager.DisableSpinButtonDuringStop();
            }
        }
    }

    private void StartSpin()
    {
        if (lastResult != null)
        {
            ProcessSpinResult();
        }

        lastResult = null;
        currentState = GameState.Spinning;
        stopRequested = false;

        uiManager.OnSpinStarted();

        if (slotView != null)
        {
            slotView.StartSpin();
        }

        socketManager.SendSpinRequest(currentBetIndex);

        if (spinCoroutine != null)
            StopCoroutine(spinCoroutine);
        spinCoroutine = StartCoroutine(SpinRoutine());
    }

    private IEnumerator SpinRoutine()
    {
        float spinDuration = GetSpinDuration();
        float elapsed = 0f;

        while (elapsed < spinDuration && !stopRequested)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Player pressed Stop manually — hold for 0.5s so the reels keep
        // spinning briefly before snapping, giving clear visual feedback.
        if (stopRequested)
        {
            yield return new WaitForSeconds(0.5f);
        }

        while (lastResult == null)
        {
            yield return null;
        }

        currentState = GameState.Stopping;

        if (slotView != null && lastResult.resultMatrix != null)
        {
            if (currentSpinSpeed == SpinSpeed.QuickSpin || stopRequested)
            {
                slotView.QuickStop(lastResult.resultMatrix);

                // Wait for the snap animation to settle before processing result
                float quickStopWaitTime = 0.5f;
                yield return new WaitForSeconds(quickStopWaitTime);

                OnReelsStoppedComplete();
            }
            else
            {
                slotView.StopSpin(lastResult.resultMatrix, OnReelsStoppedComplete);
            }
        }
        else
        {
            OnReelsStoppedComplete();
        }
    }

    private void OnReelsStoppedComplete()
    {
        SpinResult resultToUse = lastResult;
        if (resultToUse == null) return;

        if (resultToUse.winAmount > 0 && resultToUse.winLines != null && resultToUse.winLines.Count > 0)
        {
            double totalBet = currentBetAmount * (gameConfig != null ? gameConfig.betMultiplier : 1);
            double multiplier = totalBet > 0 ? (resultToUse.winAmount / totalBet) : 0;

            if (multiplier >= uiManager.BigWinThreshold)
            {
                uiManager.DisableControlsDuringWinAnimation();
                currentState = GameState.Idle;
                waitingForSpecialWin = true;
            }
            else
            {
                // For normal wins, trigger UI update immediately and enable controls
                uiManager.OnSpinStopping(resultToUse);
                uiManager.EnableControlsAfterWinAnimation();
                uiManager.OnSpinCompleted(resultToUse);
                currentState = GameState.Idle;
            }

            slotView.ShowWinLineAnimation(resultToUse.winLines, OnWinAnimationComplete);
            StartCoroutine(TriggerWinPopupWithDelay(1.5f, resultToUse));
        }
        else
        {
            uiManager.OnSpinStopping(resultToUse);
            currentState = GameState.Idle;
            OnWinAnimationComplete();
        }
    }

    private IEnumerator TriggerWinPopupWithDelay(float delay, SpinResult result)
    {
        double totalBet = currentBetAmount * (gameConfig != null ? gameConfig.betMultiplier : 1);
        double multiplier = totalBet > 0 ? (result.winAmount / totalBet) : 0;
        bool skipScreen = false;

        if (multiplier >= uiManager.BigWinThreshold && !skipScreen)
        {
            waitingForSpecialWin = true;
        }
        else
        {
            waitingForSpecialWin = false;
        }

        yield return new WaitForSeconds(delay);

        if (lastResult == result || (lastResult == null && currentState == GameState.Idle))
        {
            uiManager.TriggerBigWinPopupEarly(result, () =>
            {
                waitingForSpecialWin = false;
            });
        }
        else
        {
            waitingForSpecialWin = false;
        }
    }

    private void OnWinAnimationComplete()
    {
        if (lastResult != null)
        {
            double totalBet = currentBetAmount * (gameConfig != null ? gameConfig.betMultiplier : 1);
            double multiplier = totalBet > 0 ? (lastResult.winAmount / totalBet) : 0;

            // Only update UI here if it wasn't already updated in OnReelsStoppedComplete (multiplier < BigWinThreshold)
            if (multiplier >= uiManager.BigWinThreshold)
            {
                uiManager.OnSpinStopping(lastResult);
            }
        }

        if (isAutoPlaying)
        {
            StartCoroutine(DelayBeforeNextRound());
        }
        else
        {
            ProcessSpinResult();
        }
    }

    private IEnumerator DelayBeforeNextRound()
    {
        float delayTime = currentSpinSpeed == SpinSpeed.QuickSpin ? 0.3f : 0.5f;
        yield return new WaitForSeconds(delayTime);

        // Wait for special win popup using the flag and active state
        while (waitingForSpecialWin || uiManager.IsSpecialWinActive)
        {
            yield return null;
        }

        ProcessSpinResult();
    }

    private float GetSpinDuration()
    {
        return normalSpinDuration;
    }

    internal void OnSpinResultReceived(SpinResult result)
    {
        lastResult = result;
    }

    private void ProcessSpinResult()
    {
        playerData = lastResult.playerData;

        uiManager.OnSpinCompleted(lastResult);

        lastResult = null;

        if (isAutoPlaying)
        {
            autoPlayRemainingRounds--;

            if (autoPlayRemainingRounds <= 0)
            {
                currentState = GameState.Idle;
                StopAutoPlay();
            }
            else
            {
                // Before requesting the next spin, verify the player can still afford it.
                // If not, stop autoplay (restores all UI) then show the popup.
                double totalBet = currentBetAmount * (gameConfig != null ? gameConfig.betMultiplier : 1);
                if (playerData.balance < totalBet)
                {
                    currentState = GameState.Idle;
                    StopAutoPlay();
                    if (popupManager != null) popupManager.ShowInsufficientFundsError();
                }
                else
                {
                    currentState = GameState.Idle;
                    RequestSpin();
                }
            }
        }
        else
        {
            currentState = GameState.Idle;
        }
    }

    #endregion

    #region Spin Speed Control

    internal void SetSpinSpeed(SpinSpeed speed)
    {
        currentSpinSpeed = speed;
    }

    #endregion

    #region Auto Play

    internal void StartAutoPlay(int rounds)
    {
        if (currentState != GameState.Idle) return;

        // Check balance BEFORE locking any UI — if insufficient, show popup and bail.
        double totalBet = currentBetAmount * (gameConfig != null ? gameConfig.betMultiplier : 1);
        if (playerData.balance < totalBet)
        {
            if (popupManager != null) popupManager.ShowInsufficientFundsError();
            return;
        }

        isAutoPlaying = true;
        autoPlayTotalRounds = rounds;
        autoPlayRemainingRounds = rounds;

        uiManager.OnAutoPlayStarted();
        RequestSpin();
    }

    internal void StopAutoPlay()
    {
        isAutoPlaying = false;
        autoPlayRemainingRounds = 0;

        uiManager.OnAutoPlayStopped();
    }

    #endregion

    #region Connection Events

    internal void OnDisconnected()
    {
        if (spinCoroutine != null)
        {
            StopCoroutine(spinCoroutine);
            spinCoroutine = null;
        }

        if (isAutoPlaying)
        {
            StopAutoPlay();
        }

        currentState = GameState.Idle;
        // Note: The disconnection popup is shown by SocketIOManager.OnSocketDisconnected()
        // to avoid duplicates. GameManager only cleans up state here.
    }

    internal void ExitGame()
    {
        socketManager.CloseSocket();

    }

    #endregion

    #region Helper Methods

    internal bool CanAffordBet()
    {
        double totalBet = currentBetAmount * (gameConfig != null ? gameConfig.betMultiplier : 1);
        return playerData.balance >= totalBet;
    }

    internal bool IsSpinning()
    {
        return currentState == GameState.Spinning || currentState == GameState.Stopping;
    }



    #endregion
}