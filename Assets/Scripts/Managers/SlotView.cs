using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SlotView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;

    [Header("Symbol Sprites - Assign by Name")]
    [SerializeField] private Sprite spriteRedTriple;         // ID: 0
    [SerializeField] private Sprite spritePurpleDouble;      // ID: 1
    [SerializeField] private Sprite spriteBlueWild;          // ID: 2
    [SerializeField] private Sprite spriteRed7;              // ID: 3
    [SerializeField] private Sprite spriteGolden7;           // ID: 4
    [SerializeField] private Sprite spriteBlack7;            // ID: 5
    [SerializeField] private Sprite spriteDoubleBar;         // ID: 6
    [SerializeField] private Sprite spriteBar;               // ID: 7
    [SerializeField] private Sprite spriteBlank;             // ID: 8

    // Internal array built from named sprites
    private Sprite[] symbolSprites;

    [Header("Reel Containers")]
    [SerializeField] private Transform[] reelTransforms;

    [Header("Reel Images - 16 images per reel")]
    [SerializeField] private List<ReelImages> reelImagesList;

    [Header("Spin Settings")]
    [SerializeField] private float symbolHeight = 100f;
    [SerializeField] private float spinSpeed = 0.05f;
    [SerializeField] private float reelStartStagger = 0.08f;
    [SerializeField] private float reelStopStagger = 0.12f;

    [Header("Animation Settings - Casino Style")]
    [SerializeField] private float anticipationUpDistance = 30f;
    [SerializeField] private float anticipationUpDuration = 0.15f;
    [SerializeField] private float dropDownDistance = 15f;
    [SerializeField] private float dropDownDuration = 0.12f;
    [SerializeField] private float settleBounceDuration = 0.18f;

    [Header("Win Animation Settings")]
    [SerializeField] private float winPopDuration = 0.4f;
    [SerializeField] private int winPopRepeat = 3;


    [Header("Stop Animation Settings")]
    [SerializeField] private float stopOvershootDistance = 50f;
    [SerializeField] private float stopOvershootDuration = 0.15f;
    [SerializeField] private float stopBounceBackDistance = 15f;
    [SerializeField] private float stopBounceBackDuration = 0.25f;
    [SerializeField] private float stopSettleDuration = 0.35f;

    [Header("Quick Spin Settings")]
    [SerializeField] private float quickStopStagger = 0.06f;
    [SerializeField] private float quickStopOvershoot = 20f;
    [SerializeField] private float quickStopDuration = 0.2f;
    [SerializeField] private int minSpinCyclesBeforeStop = 3;

    [Header("Win Animation Settings")]
    [SerializeField] private float winSymbolLoopDuration = 1.5f;
    [SerializeField] private int winSymbolLoopCount = 3;

    [Header("Spin Mask System")]
    [SerializeField] private Image reelMask;


    private float middlePosition = 0f;
    private float cycleDistance;


    private List<Tween> spinTweens = new List<Tween>();
    private List<Tween> winTweens = new List<Tween>();
    private List<int> reelCycleCount = new List<int>();
    private Coroutine winAnimationCoroutine;


    internal List<List<int>> currentDisplayMatrix;

    private bool isSpinning;

    #region Initialization

    private void Start()
    {
        BuildSymbolSpriteArray();
        InitializeReels();
        DisableAllOverlays();
    }

    private void DisableAllOverlays()
    {
        // Initialize masks as disabled
        DisableAllMasks();
    }

    private void BuildSymbolSpriteArray()
    {
        symbolSprites = new Sprite[9];
        symbolSprites[0] = spriteRedTriple;
        symbolSprites[1] = spritePurpleDouble;
        symbolSprites[2] = spriteBlueWild;
        symbolSprites[3] = spriteRed7;
        symbolSprites[4] = spriteGolden7;
        symbolSprites[5] = spriteBlack7;
        symbolSprites[6] = spriteDoubleBar;
        symbolSprites[7] = spriteBar;
        symbolSprites[8] = spriteBlank;

        // Validate
        for (int i = 0; i < symbolSprites.Length; i++)
        {
            if (symbolSprites[i] == null)
            {
                Debug.LogError($"[SlotView] Symbol sprite at index {i} is not assigned in inspector!");
            }
        }
    }

    private void InitializeReels()
    {
        cycleDistance = symbolHeight;

        middlePosition = 0f;

        currentDisplayMatrix = new List<List<int>>();
        int defaultCols = 3;
        int defaultRows = 3;
        reelCycleCount.Clear();
        for (int col = 0; col < defaultCols; col++)
        {
            List<int> column = new List<int>();
            for (int r = 0; r < defaultRows; r++)
            {
                column.Add(0);
            }
            currentDisplayMatrix.Add(column);
            reelCycleCount.Add(0);
        }
    }

    internal void SetInitialMatrix(List<List<int>> matrix)
    {
        if (matrix == null || matrix.Count == 0) return;

        currentDisplayMatrix = matrix;

        int cols = matrix.Count;
        reelCycleCount.Clear();
        for (int col = 0; col < cols; col++)
        {
            reelCycleCount.Add(0);
        }

        for (int col = 0; col < cols; col++)
        {
            SetReelSymbols(col, matrix[col], true);
        }
    }

    #endregion

    #region Symbol Display

    private void SetReelSymbols(int columnIndex, List<int> visibleSymbolIds, bool isInitial = false)
    {
        if (columnIndex >= reelImagesList.Count)
        {
            Debug.LogError($"SetReelSymbols: Invalid column index {columnIndex}, max is {reelImagesList.Count - 1}");
            return;
        }

        int expectedRows = gameManager?.gameConfig != null ? gameManager.gameConfig.rowCount : 3;
        if (visibleSymbolIds == null || visibleSymbolIds.Count != expectedRows)
        {
            Debug.LogError($"SetReelSymbols: Invalid visibleSymbolIds count {visibleSymbolIds?.Count}, expected {expectedRows}");
            return;
        }

        var reel = reelImagesList[columnIndex];

        if (reel.images == null || reel.images.Count != 16)
        {
            Debug.LogError($"SetReelSymbols: Reel {columnIndex} has invalid image count {reel.images?.Count}, expected 16");
            return;
        }

        int visibleRows = visibleSymbolIds.Count;
        for (int row = 0; row < visibleRows; row++)
        {
            int imageIndex = 6 + row;
            int symbolId = visibleSymbolIds[row];
            reel.images[imageIndex].sprite = GetSymbolSprite(symbolId);
        }

        int maxSymbolId = gameManager?.gameConfig != null ? gameManager.gameConfig.symbols.Count : 9;

        for (int i = 0; i < 6; i++)
        {
            reel.images[i].sprite = GetSymbolSprite(Random.Range(0, maxSymbolId));
        }

        for (int i = 6 + visibleRows; i < reel.images.Count; i++)
        {
            reel.images[i].sprite = GetSymbolSprite(Random.Range(0, maxSymbolId));
        }

        if (isInitial && reelTransforms[columnIndex] != null)
        {
            reelTransforms[columnIndex].localPosition = new Vector3(
                reelTransforms[columnIndex].localPosition.x,
                middlePosition,
                0
            );
        }
    }

    private Sprite GetSymbolSprite(int symbolId)
    {
        // Validate symbolId range (0-15)
        if (symbolId < 0 || symbolId >= symbolSprites.Length)
        {
            Debug.LogWarning($"[SlotView] Invalid symbolId {symbolId}, using default sprite 0. Total sprites: {symbolSprites.Length}");
            return symbolSprites[0];
        }

        if (symbolSprites[symbolId] == null)
        {
            Debug.LogError($"[SlotView] Symbol sprite for ID {symbolId} is null!");
            return symbolSprites[0];
        }

        return symbolSprites[symbolId];
    }

    #endregion

    #region Spin Animation

    internal void StartSpin()
    {
        if (isSpinning) return;

        isSpinning = true;
        KillAllTweens();

        DisableAllOverlays();

        // Enable masks when spinning starts
        EnableAllMasks();

        for (int i = 0; i < reelCycleCount.Count; i++)
        {
            reelCycleCount[i] = 0;
        }

        int cols = currentDisplayMatrix != null ? currentDisplayMatrix.Count : 3;
        for (int col = 0; col < cols; col++)
        {
            StartReelCycleWithDelay(col, col * reelStartStagger);
        }
    }

    private void StartReelCycleWithDelay(int columnIndex, float delay)
    {
        if (columnIndex >= reelTransforms.Length) return;

        Transform slotTransform = reelTransforms[columnIndex];

        Sequence startSequence = DOTween.Sequence();

        if (delay > 0)
        {
            startSequence.AppendInterval(delay);
        }

        startSequence.Append(
            slotTransform.DOLocalMoveY(middlePosition + anticipationUpDistance, anticipationUpDuration)
                .SetEase(Ease.OutCubic)
        );

        startSequence.Append(
            slotTransform.DOLocalMoveY(middlePosition - dropDownDistance, dropDownDuration)
                .SetEase(Ease.InCubic)
        );

        startSequence.Append(
            slotTransform.DOLocalMoveY(middlePosition, settleBounceDuration)
                .SetEase(Ease.OutBounce)
        );

        startSequence.OnComplete(() => {
            if (isSpinning)
            {
                StartReelCycle(columnIndex);
            }
        });

        startSequence.Play();

        if (spinTweens.Count <= columnIndex)
            spinTweens.Add(startSequence);
        else
            spinTweens[columnIndex] = startSequence;
    }

    private void StartReelCycle(int columnIndex)
    {
        if (columnIndex >= reelTransforms.Length) return;
        if (!isSpinning) return;

        Transform slotTransform = reelTransforms[columnIndex];

        slotTransform.localPosition = new Vector3(slotTransform.localPosition.x, middlePosition, 0);

        float currentSpeed = spinSpeed;

        Sequence cycleSequence = DOTween.Sequence();

        cycleSequence.Append(
            slotTransform.DOLocalMoveY(middlePosition - cycleDistance, currentSpeed)
                .SetEase(Ease.Linear)
        );

        cycleSequence.OnComplete(() => {
            if (isSpinning)
            {
                CycleReelSymbols(columnIndex);

                slotTransform.localPosition = new Vector3(slotTransform.localPosition.x, middlePosition, 0);

                if (columnIndex < reelCycleCount.Count)
                {
                    reelCycleCount[columnIndex]++;
                }

                StartReelCycle(columnIndex);
            }
        });

        cycleSequence.Play();

        if (spinTweens.Count <= columnIndex)
            spinTweens.Add(cycleSequence);
        else
            spinTweens[columnIndex] = cycleSequence;
    }

    private void CycleReelSymbols(int columnIndex)
    {
        var reel = reelImagesList[columnIndex];
        if (reel.images == null || reel.images.Count != 16) return;

        Sprite bottomSprite = reel.images[15].sprite;

        for (int i = 15; i > 0; i--)
        {
            reel.images[i].sprite = reel.images[i - 1].sprite;
        }

        int maxSymbolId = gameManager?.gameConfig != null ? gameManager.gameConfig.symbols.Count : 9;
        reel.images[0].sprite = GetSymbolSprite(Random.Range(0, maxSymbolId));
    }

    #endregion

    #region Stop Spin

    internal void StopSpin(List<List<int>> resultMatrix, System.Action onComplete)
    {
        if (!isSpinning)
        {
            currentDisplayMatrix = resultMatrix;
            int cols = resultMatrix.Count;
            for (int col = 0; col < cols; col++)
            {
                SetReelSymbols(col, resultMatrix[col], false);
            }
            
            onComplete?.Invoke();
            return;
        }

        StartCoroutine(StopSpinSequence(resultMatrix, onComplete, false));
    }

    private IEnumerator StopSpinSequence(List<List<int>> resultMatrix, System.Action onComplete, bool isQuickStop)
    {
        currentDisplayMatrix = resultMatrix;

        int cols = resultMatrix.Count;

        while (true)
        {
            bool allReelsReady = true;
            for (int col = 0; col < cols; col++)
            {
                if (reelCycleCount[col] < minSpinCyclesBeforeStop)
                {
                    allReelsReady = false;
                    break;
                }
            }

            if (allReelsReady) break;
            yield return null;
        }

        float stagger = isQuickStop ? quickStopStagger : reelStopStagger;

        for (int col = 0; col < cols; col++)
        {
            float delay = col * stagger;
            StartCoroutine(StopSingleReel(col, resultMatrix[col], delay, isQuickStop));
        }

        float longestStopTime;
        if (isQuickStop)
        {
            longestStopTime = ((cols - 1) * stagger) + quickStopDuration;
        }
        else
        {
            longestStopTime = ((cols - 1) * stagger) + stopOvershootDuration + stopBounceBackDuration + stopSettleDuration;
        }

        yield return new WaitForSeconds(longestStopTime);

        isSpinning = false;

        onComplete?.Invoke();
    }

    private IEnumerator StopSingleReel(int columnIndex, List<int> targetSymbols, float delay, bool isQuickStop)
    {
        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }

        if (columnIndex < spinTweens.Count && spinTweens[columnIndex] != null)
        {
            spinTweens[columnIndex].Kill();
        }

        Transform slotTransform = reelTransforms[columnIndex];

        SetReelSymbols(columnIndex, targetSymbols, false);

        float currentY = slotTransform.localPosition.y;
        float targetY = middlePosition;
        float offset = (currentY - targetY) % cycleDistance;
        if (offset < 0) offset += cycleDistance;

        slotTransform.localPosition = new Vector3(
            slotTransform.localPosition.x,
            targetY + offset,
            0
        );

        // ── Play reel-stop sound immediately when symbols lock in ──────────
        AudioManager.Instance?.PlayReelStop();

        // Detect wild symbols in this column for hit sounds
        if (currentDisplayMatrix != null && columnIndex < currentDisplayMatrix.Count)
        {
            bool hasWild    = false;
            foreach (int sym in currentDisplayMatrix[columnIndex])
            {
                if (IsWildSymbol(sym))    hasWild    = true;
            }
            if (hasWild) AudioManager.Instance?.PlayWildHit();
        }
        // ──────────────────────────────────────────────────────────────────

        if (isQuickStop)
        {
            Sequence quickStopSequence = DOTween.Sequence();

            quickStopSequence.Append(
                slotTransform.DOLocalMoveY(middlePosition - quickStopOvershoot, quickStopDuration * 0.3f)
                    .SetEase(Ease.InCubic)
            );

            quickStopSequence.Append(
                slotTransform.DOLocalMoveY(middlePosition, quickStopDuration * 0.7f)
                    .SetEase(Ease.OutBack, 1.2f)
            );

            quickStopSequence.OnComplete(() => PlayStopAnimationsForColumn(columnIndex));

            spinTweens[columnIndex] = quickStopSequence;
        }
        else
        {
            Sequence stopSequence = DOTween.Sequence();

            stopSequence.Append(
                slotTransform.DOLocalMoveY(middlePosition - stopOvershootDistance, stopOvershootDuration)
                    .SetEase(Ease.InCubic)
            );

            stopSequence.Append(
                slotTransform.DOLocalMoveY(middlePosition + stopBounceBackDistance, stopBounceBackDuration)
                    .SetEase(Ease.OutCubic)
            );

            stopSequence.Append(
                slotTransform.DOLocalMoveY(middlePosition, stopSettleDuration)
                    .SetEase(Ease.OutBounce)
            );

            stopSequence.OnComplete(() => PlayStopAnimationsForColumn(columnIndex));

            spinTweens[columnIndex] = stopSequence;
        }
    }

    #endregion

    #region Quick Spin

    internal void QuickStop(List<List<int>> resultMatrix, System.Action onComplete = null)
    {
        if (!isSpinning)
        {
            currentDisplayMatrix = resultMatrix;
            int cols = resultMatrix.Count;
            for (int col = 0; col < cols; col++)
            {
                if (col < reelTransforms.Length)
                {
                    SetReelSymbols(col, resultMatrix[col], false);
                    reelTransforms[col].localPosition = new Vector3(
                        reelTransforms[col].localPosition.x,
                        middlePosition,
                        0
                    );
                    PlayStopAnimationsForColumn(col);
                }
            }
            
            onComplete?.Invoke();
            return;
        }

        StartCoroutine(StopSpinSequence(resultMatrix, onComplete, true));
    }

    #endregion




    #region Stop Symbol Animations

    private void PlayStopAnimationsForColumn(int col)
    {
    }

    #endregion

    #region Win Line Animation

    internal void ShowWinLineAnimation(List<WinLine> winLines, System.Action onComplete)
    {
        DisableAllMasks();

        if (winLines == null || winLines.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        KillWinTweens();
        winAnimationCoroutine = StartCoroutine(PlayWinLinesSequentially(winLines, onComplete));
    }

    private IEnumerator PlayWinLinesSequentially(List<WinLine> winLines, System.Action onComplete)
    {
        bool skipScreen = false;
        int loopCount = (gameManager != null && gameManager.isAutoPlaying) ? 1 : winSymbolLoopCount;
        float lineDuration = skipScreen ? 0.5f : winSymbolLoopDuration * loopCount;

        List<int> prevPositions = null;

        Debug.Log($"[PlayWinLinesSequentially] Starting win animation for {winLines.Count} lines");

        foreach (var winLine in winLines)
        {
            if (winLine.positions == null || winLine.positions.Count == 0) continue;

            if (prevPositions != null)
            {
                KillWinTweens(false);
                int cols = gameManager?.gameConfig != null ? gameManager.gameConfig.reelCount : 3;
                foreach (int flatIdx in prevPositions)
                {
                    int r = flatIdx / cols;
                    int c = flatIdx % cols;
                    ResetSymbolScale(c, r);
                }
            }

            AudioManager.Instance?.PlayWinLine();

            foreach (int flatIndex in winLine.positions)
            {
                int cols = gameManager?.gameConfig != null ? gameManager.gameConfig.reelCount : 3;
                int rows = gameManager?.gameConfig != null ? gameManager.gameConfig.rowCount : 3;
                int row = flatIndex / cols;
                int col = flatIndex % cols;

                if (col < 0 || col >= cols || row < 0 || row >= rows)
                {
                    Debug.LogWarning($"[PlayWinLinesSequentially] Invalid position! col: {col}, row: {row}");
                    continue;
                }

                AnimateWinSymbol(col, row);
            }

            prevPositions = new List<int>(winLine.positions);

            yield return new WaitForSeconds(lineDuration);
        }

        AudioManager.Instance?.StopWinLine();
        KillWinTweens(false);

        onComplete?.Invoke();
    }

    private void ResetSymbolScale(int col, int row)
    {
        if (col >= reelImagesList.Count) return;
        var reel = reelImagesList[col];
        if (reel.images == null) return;
        int imageIndex = 6 + row;
        if (imageIndex >= reel.images.Count) return;
        if (reel.images[imageIndex] != null)
        {
            reel.images[imageIndex].DOKill();
            reel.images[imageIndex].transform.localScale = Vector3.one;
            // Restore alpha to full opacity
            Color c = reel.images[imageIndex].color;
            reel.images[imageIndex].color = new Color(c.r, c.g, c.b, 1f);
        }
    }

    private void AnimateWinSymbol(int column, int row)
    {
        if (column >= reelImagesList.Count)
        {
            Debug.LogError($"[AnimateWinSymbol] Invalid column {column}, max is {reelImagesList.Count - 1}");
            return;
        }

        var reel = reelImagesList[column];
        if (reel.images == null || reel.images.Count < 10)
        {
            Debug.LogError($"[AnimateWinSymbol] Reel {column} has invalid images list");
            return;
        }

        int imageIndex = 6 + row;
        if (imageIndex >= reel.images.Count)
        {
            Debug.LogError($"[AnimateWinSymbol] Image index {imageIndex} out of range for reel {column}");
            return;
        }

        Image symbolImage = reel.images[imageIndex];
        if (symbolImage == null)
        {
            Debug.LogError($"[AnimateWinSymbol] Symbol image is NULL at col: {column}, row: {row}, imageIndex: {imageIndex}");
            return;
        }

        Sequence popSeq = DOTween.Sequence();
        popSeq.AppendCallback(() => {
            symbolImage.DOKill();
            symbolImage.transform.localScale = Vector3.one;
        });
        popSeq.Append(symbolImage.transform.DOScale(1.2f, 0.2f).SetEase(Ease.OutBack));
        popSeq.Append(symbolImage.transform.DOScale(1f, 0.2f).SetEase(Ease.InBack));
        winTweens.Add(popSeq);
    }

    private void KillWinTweens(bool stopCoroutine = true)
    {
        EnableAllMasks();
        foreach (var tween in winTweens)
        {
            tween?.Kill();
        }
        winTweens.Clear();

        if (stopCoroutine && winAnimationCoroutine != null)
        {
            StopCoroutine(winAnimationCoroutine);
            winAnimationCoroutine = null;
        }
        AudioManager.Instance?.StopWinLine();

        // Restore all symbol image alphas to full opacity
        foreach (var reel in reelImagesList)
        {
            if (reel.images != null)
            {
                foreach (var image in reel.images)
                {
                    if (image != null)
                    {
                        image.DOKill();
                        image.transform.localScale = Vector3.one;
                        Color c = image.color;
                        image.color = new Color(c.r, c.g, c.b, 1f);
                    }
                }
            }
        }
    }

    #endregion

    #region Mask and Non-Display Icon Management

    private void EnableAllMasks()
    {
        if (reelMask == null) return;
        reelMask.enabled = true;
    }

    private void DisableAllMasks()
    {
        if (reelMask == null) return;
        reelMask.enabled = false;
    }

    



    #endregion

    #region Helper Methods

    internal List<List<int>> GetCurrentDisplayMatrix()
    {
        return currentDisplayMatrix;
    }

    internal bool IsSpinning()
    {
        return isSpinning;
    }

    private bool IsWildSymbol(int symId)
    {
        if (gameManager != null && gameManager.gameConfig != null && gameManager.gameConfig.symbols != null)
        {
            var symbol = gameManager.gameConfig.symbols.Find(s => s.id == symId);
            if (symbol != null) return symbol.isWild;
        }
        return symId == 2;
    }

    private void KillAllTweens()
    {
        foreach (var tween in spinTweens)
        {
            tween?.Kill();
        }
        spinTweens.Clear();

        KillWinTweens();
    }

    #endregion

    #region Cleanup

    private void OnDestroy()
    {
        KillAllTweens();
    }

    #endregion
}

[System.Serializable]
public class ReelImages
{
    public List<Image> images = new List<Image>(16);
}
