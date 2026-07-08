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

    [Header("Reel Images - 7 images per reel")]
    [SerializeField] private List<ReelImages> reelImagesList;

    [Header("Spin Settings")]
    [SerializeField] private float symbolHeight = 100f;
    [SerializeField] private float spinSpeed = 0.05f;
    [SerializeField] private float reelStartStagger = 0.08f;
    [SerializeField] private float reelStopStagger = 0.12f;

    [Header("Start Animation Settings")]
    [SerializeField] private float anticipationUpDistance = 30f;
    [SerializeField] private float anticipationUpDuration = 0.15f;
    [SerializeField] private float dropDownDistance = 15f;
    [SerializeField] private float dropDownDuration = 0.12f;
    [SerializeField] private float settleBounceDuration = 0.18f;

    [Header("Stop Animation Settings")]
    [SerializeField] private float stopOvershootDistance = 50f;
    [SerializeField] private float stopOvershootDuration = 0.15f;
    [SerializeField] private float stopBounceBackDuration = 0.25f;

    [Header("Quick Spin Settings")]
    [SerializeField] private float quickStopStagger = 0.06f;
    [SerializeField] private float quickStopOvershoot = 20f;
    [SerializeField] private float quickStopDuration = 0.2f;
    [SerializeField] private int minSpinCyclesBeforeStop = 3;

    [Header("Win Animation Settings")]
    [SerializeField] private float winPopDuration = 0.4f;
    [SerializeField] private int winPopRepeat = 3;
    [SerializeField] private float winSymbolLoopDuration = 1.5f;
    [SerializeField] private int winSymbolLoopCount = 3;

    [Header("Blank Symbol Settings")]
    [SerializeField] private int blankSymbolId = 8;
    [SerializeField] private float blankSpacingValue = -100f;
    [SerializeField] private float blankMiddleYOffset = -160f;
    [SerializeField] private float blankMiddleSpacingValue = 60f;
    [SerializeField] private float blankTopBottomSpacingValue = 20f;
    [SerializeField] private float defaultSpacing = 0f;



    private float middlePosition = 0f;
    private float cycleDistance;


    private List<Tween> spinTweens = new List<Tween>();
    private List<Tween> winTweens = new List<Tween>();
    private List<int> reelCycleCount = new List<int>();
    private Coroutine winAnimationCoroutine;
    private VerticalLayoutGroup[] reelLayoutGroups;
    private BlankScenario[] currentBlankScenarios;


    internal List<List<int>> currentDisplayMatrix;

    private bool isSpinning;

    #region Initialization

    private void Start()
    {
        BuildSymbolSpriteArray();
        InitializeReels();
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

        // Cache VerticalLayoutGroup references from reel containers
        reelLayoutGroups = new VerticalLayoutGroup[reelTransforms.Length];
        currentBlankScenarios = new BlankScenario[reelTransforms.Length];
        for (int i = 0; i < reelTransforms.Length; i++)
        {
            if (reelTransforms[i] != null)
            {
                reelLayoutGroups[i] = reelTransforms[i].GetComponent<VerticalLayoutGroup>();
            }
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
            BlankScenario scenario = DetectBlankScenario(matrix[col]);
            ApplyBlankScenario(col, scenario, matrix[col]);
            // Override initial Y position based on blank scenario
            if (col < reelTransforms.Length && reelTransforms[col] != null)
            {
                float targetY = GetTargetYForScenario(scenario);
                reelTransforms[col].localPosition = new Vector3(
                    reelTransforms[col].localPosition.x,
                    targetY,
                    0
                );
            }
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

        if (reel.images == null || reel.images.Count != 7)
        {
            Debug.LogError($"SetReelSymbols: Reel {columnIndex} has invalid image count {reel.images?.Count}, expected 7");
            return;
        }

        int visibleRows = visibleSymbolIds.Count;
        // Visible rows sit at indices 2, 3, 4 (middle of 7)
        for (int row = 0; row < visibleRows; row++)
        {
            int imageIndex = 2 + row;
            int symbolId = visibleSymbolIds[row];
            reel.images[imageIndex].sprite = GetSymbolSprite(symbolId);
        }

        int maxSymbolId = gameManager?.gameConfig != null ? gameManager.gameConfig.symbols.Count : 9;

        // Fill 2 buffer images above the visible area
        for (int i = 0; i < 2; i++)
        {
            reel.images[i].sprite = GetSymbolSprite(Random.Range(0, maxSymbolId));
        }

        // Fill 2 buffer images below the visible area
        for (int i = 2 + visibleRows; i < reel.images.Count; i++)
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
        ResetBlankScenarios();

        for (int i = 0; i < reelCycleCount.Count; i++)
        {
            reelCycleCount[i] = 0;
        }

        int cols = currentDisplayMatrix != null ? currentDisplayMatrix.Count : 3;
        for (int col = 0; col < cols; col++)
        {
            InitializeTweening(col);
        }
    }

    private void InitializeTweening(int columnIndex)
    {
        if (columnIndex >= reelTransforms.Length) return;

        Transform slotTransform = reelTransforms[columnIndex];

        while (spinTweens.Count <= columnIndex) spinTweens.Add(null);
        if (spinTweens[columnIndex] != null) { spinTweens[columnIndex].Kill(); spinTweens[columnIndex] = null; }

        // Quick 2-step bounce: pull up then snap back (same as BreakingBad)
        Sequence startSeq = DOTween.Sequence();
        startSeq.Append(
            slotTransform.DOLocalMoveY(middlePosition + anticipationUpDistance, anticipationUpDuration)
                .SetEase(Ease.OutQuad)
        );
        startSeq.Append(
            slotTransform.DOLocalMoveY(middlePosition, anticipationUpDuration * 0.5f)
                .SetEase(Ease.InQuad)
        );
        startSeq.OnComplete(() => { if (isSpinning) StartReelCycle(columnIndex); });
        spinTweens[columnIndex] = startSeq;
        startSeq.Play();
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
        if (reel.images == null || reel.images.Count != 7) return;

        for (int i = 6; i > 0; i--)
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
                BlankScenario scenario = DetectBlankScenario(resultMatrix[col]);
                ApplyBlankScenario(col, scenario, resultMatrix[col]);
                if (col < reelTransforms.Length && reelTransforms[col] != null)
                {
                    float targetY = GetTargetYForScenario(scenario);
                    reelTransforms[col].localPosition = new Vector3(
                        reelTransforms[col].localPosition.x,
                        targetY,
                        0
                    );
                }
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

        // Wait until minimum spin cycles are complete
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

        // Start stopping each reel with stagger
        for (int col = 0; col < cols; col++)
        {
            float delay = col * stagger;
            StartCoroutine(StopSingleReel(col, resultMatrix[col], delay, isQuickStop));
        }

        // Calculate longest stop time
        float longestStopTime;
        if (isQuickStop)
        {
            longestStopTime = ((cols - 1) * stagger) + quickStopDuration;
        }
        else
        {
            longestStopTime = ((cols - 1) * stagger) + stopOvershootDuration + stopBounceBackDuration;
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

        // Detect and apply blank scenario before stopping
        BlankScenario scenario = DetectBlankScenario(targetSymbols);

        SetReelSymbols(columnIndex, targetSymbols, false);

        // Apply blank scenario (spacing, alpha, sprite overrides) after symbols are set
        ApplyBlankScenario(columnIndex, scenario, targetSymbols);

        float scenarioTargetY = GetTargetYForScenario(scenario);
        float currentY = slotTransform.localPosition.y;
        float offset = (currentY - scenarioTargetY) % cycleDistance;
        if (offset < 0) offset += cycleDistance;

        slotTransform.localPosition = new Vector3(
            slotTransform.localPosition.x,
            scenarioTargetY + offset,
            0
        );

        // ── Play reel-stop sound immediately when symbols lock in ──────────
        AudioManager.Instance?.PlayReelStop();

        // Detect wild symbols in this column for hit sounds
        if (currentDisplayMatrix != null && columnIndex < currentDisplayMatrix.Count)
        {
            bool hasWild = false;
            foreach (int sym in currentDisplayMatrix[columnIndex])
            {
                if (IsWildSymbol(sym)) hasWild = true;
            }
            if (hasWild) AudioManager.Instance?.PlayWildHit();
        }
        // ──────────────────────────────────────────────────────────────────

        if (isQuickStop)
        {
            Sequence quickStopSequence = DOTween.Sequence();

            quickStopSequence.Append(
                slotTransform.DOLocalMoveY(scenarioTargetY - quickStopOvershoot, quickStopDuration * 0.3f)
                    .SetEase(Ease.OutQuad)
            );

            quickStopSequence.Append(
                slotTransform.DOLocalMoveY(scenarioTargetY, quickStopDuration * 0.7f)
                    .SetEase(Ease.OutQuad)
            );

            quickStopSequence.OnComplete(() => PlayStopAnimationsForColumn(columnIndex));

            spinTweens[columnIndex] = quickStopSequence;
        }
        else
        {
            Sequence stopSequence = DOTween.Sequence();

            stopSequence.Append(
                slotTransform.DOLocalMoveY(scenarioTargetY - stopOvershootDistance, stopOvershootDuration)
                    .SetEase(Ease.OutQuad)
            );

            stopSequence.Append(
                slotTransform.DOLocalMoveY(scenarioTargetY, stopBounceBackDuration)
                    .SetEase(Ease.OutQuad)
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
                    BlankScenario scenario = DetectBlankScenario(resultMatrix[col]);
                    ApplyBlankScenario(col, scenario, resultMatrix[col]);
                    float targetY = GetTargetYForScenario(scenario);
                    reelTransforms[col].localPosition = new Vector3(
                        reelTransforms[col].localPosition.x,
                        targetY,
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
        int imageIndex = 2 + row;
        if (imageIndex >= reel.images.Count) return;
        if (reel.images[imageIndex] != null)
        {
            reel.images[imageIndex].DOKill();
            reel.images[imageIndex].transform.localScale = Vector3.one;
            // Restore alpha to full opacity
            Color c = reel.images[imageIndex].color;
            reel.images[imageIndex].color = new Color(c.r, c.g, c.b, 1f);
        }
        // Re-apply blank alpha if this column has an active blank scenario
        ReapplyBlankAlphaForColumn(col);
    }

    private void AnimateWinSymbol(int column, int row)
    {
        if (column >= reelImagesList.Count)
        {
            Debug.LogError($"[AnimateWinSymbol] Invalid column {column}, max is {reelImagesList.Count - 1}");
            return;
        }

        var reel = reelImagesList[column];
        if (reel.images == null || reel.images.Count < 5)
        {
            Debug.LogError($"[AnimateWinSymbol] Reel {column} has invalid images list");
            return;
        }

        int imageIndex = 2 + row;
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

        // Re-apply blank scenarios after restoring alphas
        ReapplyCurrentBlankScenarios();
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

    #region Blank Symbol Handling

    private enum BlankScenario
    {
        NoBlanks,          // Scenario 1: No blanks
        AllBlank,          // Scenario 2: All 3 positions blank
        TwoBlankTop,       // Scenario 3: Top 2 blank (row 0, 1)
        TwoBlankBottom,    // Scenario 4: Bottom 2 blank (row 1, 2)
        TwoBlankTopBottom, // Scenario 5: Top and bottom blank (row 0, 2)
        OneBlankTop,       // Scenario 6: Top blank (row 0)
        OneBlankBottom,    // Scenario 7: Bottom blank (row 2)
        OneBlankMiddle     // Scenario 8: Middle blank (row 1)
    }

    /// <summary>
    /// Analyzes a column's symbol IDs to determine which blank scenario applies.
    /// </summary>
    private BlankScenario DetectBlankScenario(List<int> columnSymbols)
    {
        if (columnSymbols == null || columnSymbols.Count != 3)
            return BlankScenario.NoBlanks;

        bool top = columnSymbols[0] == blankSymbolId;
        bool mid = columnSymbols[1] == blankSymbolId;
        bool bot = columnSymbols[2] == blankSymbolId;

        int blankCount = (top ? 1 : 0) + (mid ? 1 : 0) + (bot ? 1 : 0);

        if (blankCount == 0) return BlankScenario.NoBlanks;
        if (blankCount == 3) return BlankScenario.AllBlank;

        if (blankCount == 2)
        {
            if (top && mid) return BlankScenario.TwoBlankTop;
            if (mid && bot) return BlankScenario.TwoBlankBottom;
            if (top && bot) return BlankScenario.TwoBlankTopBottom;
        }

        // blankCount == 1
        if (top) return BlankScenario.OneBlankTop;
        if (bot) return BlankScenario.OneBlankBottom;
        return BlankScenario.OneBlankMiddle;
    }

    /// <summary>
    /// Universal method that applies spacing, alpha, and sprite overrides based on the blank scenario.
    /// Called after SetReelSymbols to override blank positions.
    /// </summary>
    private void ApplyBlankScenario(int columnIndex, BlankScenario scenario, List<int> targetSymbols)
    {
        if (columnIndex >= reelTransforms.Length || columnIndex >= reelImagesList.Count) return;

        var reel = reelImagesList[columnIndex];
        VerticalLayoutGroup layoutGroup = (reelLayoutGroups != null && columnIndex < reelLayoutGroups.Length)
            ? reelLayoutGroups[columnIndex] : null;

        // Track current scenario for re-application after win animations
        if (currentBlankScenarios != null && columnIndex < currentBlankScenarios.Length)
        {
            currentBlankScenarios[columnIndex] = scenario;
        }

        // Always hide buffer images (indices 0, 1, 5, 6)
        SetImageAlpha(columnIndex, 0, 0f);
        SetImageAlpha(columnIndex, 1, 0f);
        SetImageAlpha(columnIndex, 5, 0f);
        SetImageAlpha(columnIndex, 6, 0f);

        // Reset visible images to full alpha first
        SetImageAlpha(columnIndex, 2, 1f);
        SetImageAlpha(columnIndex, 3, 1f);
        SetImageAlpha(columnIndex, 4, 1f);

        switch (scenario)
        {
            case BlankScenario.NoBlanks: // Scenario 1
                if (layoutGroup != null) layoutGroup.spacing = blankSpacingValue;
                break;

            case BlankScenario.AllBlank: // Scenario 2
                if (layoutGroup != null) layoutGroup.spacing = defaultSpacing;
                SetImageAlpha(columnIndex, 2, 0f);
                SetImageAlpha(columnIndex, 3, 0f);
                SetImageAlpha(columnIndex, 4, 0f);
                break;

            case BlankScenario.TwoBlankTop: // Scenario 3
                if (layoutGroup != null) layoutGroup.spacing = blankSpacingValue;
                SetImageAlpha(columnIndex, 2, 0f);
                SetImageAlpha(columnIndex, 3, 0f);
                break;

            case BlankScenario.TwoBlankBottom: // Scenario 4
                if (layoutGroup != null) layoutGroup.spacing = blankSpacingValue;
                SetImageAlpha(columnIndex, 3, 0f);
                SetImageAlpha(columnIndex, 4, 0f);
                break;

            case BlankScenario.TwoBlankTopBottom: // Scenario 5
                if (layoutGroup != null) layoutGroup.spacing = blankTopBottomSpacingValue;
                // Show random non-blank sprites at blank positions (index 2 = row 0, index 4 = row 2)
                reel.images[2].sprite = GetRandomNonBlankSprite();
                reel.images[4].sprite = GetRandomNonBlankSprite();
                break;

            case BlankScenario.OneBlankTop: // Scenario 6
                if (layoutGroup != null) layoutGroup.spacing = blankSpacingValue;
                SetImageAlpha(columnIndex, 2, 0f);
                break;

            case BlankScenario.OneBlankBottom: // Scenario 7
                if (layoutGroup != null) layoutGroup.spacing = blankSpacingValue;
                SetImageAlpha(columnIndex, 4, 0f);
                break;

            case BlankScenario.OneBlankMiddle: // Scenario 8
                if (layoutGroup != null) layoutGroup.spacing = blankMiddleSpacingValue;
                // Index 3 shows last row result (row 2's symbol)
                if (targetSymbols != null && targetSymbols.Count > 2)
                {
                    reel.images[3].sprite = GetSymbolSprite(targetSymbols[2]);
                }
                break;
        }
    }

    /// <summary>
    /// Returns the reel Y position target based on the blank scenario.
    /// Scenario 8 (OneBlankMiddle) shifts the reel down so only indices 2 and 3 are visible.
    /// </summary>
    private float GetTargetYForScenario(BlankScenario scenario)
    {
        switch (scenario)
        {
            case BlankScenario.OneBlankMiddle:
                return middlePosition + blankMiddleYOffset;
            default:
                return middlePosition;
        }
    }

    /// <summary>
    /// Universal helper to set the alpha of a specific image in a reel.
    /// </summary>
    private void SetImageAlpha(int columnIndex, int imageIndex, float alpha)
    {
        if (columnIndex >= reelImagesList.Count) return;
        var reel = reelImagesList[columnIndex];
        if (reel.images == null || imageIndex >= reel.images.Count) return;

        Image img = reel.images[imageIndex];
        if (img != null)
        {
            Color c = img.color;
            img.color = new Color(c.r, c.g, c.b, alpha);
        }
    }

    /// <summary>
    /// Returns a random non-blank sprite for filling blank positions in Scenario 5.
    /// </summary>
    private Sprite GetRandomNonBlankSprite()
    {
        List<int> nonBlankIds = new List<int>();
        for (int i = 0; i < symbolSprites.Length; i++)
        {
            if (i != blankSymbolId && symbolSprites[i] != null)
            {
                nonBlankIds.Add(i);
            }
        }

        if (nonBlankIds.Count == 0) return symbolSprites[0];
        return symbolSprites[nonBlankIds[Random.Range(0, nonBlankIds.Count)]];
    }

    /// <summary>
    /// Resets all reels to default state: full alpha, default spacing, middlePosition Y.
    /// Called at the start of each spin.
    /// </summary>
    private void ResetBlankScenarios()
    {
        int cols = reelImagesList.Count;
        for (int col = 0; col < cols; col++)
        {
            if (col >= reelTransforms.Length) continue;

            // Reset Y position
            Transform slotTransform = reelTransforms[col];
            slotTransform.localPosition = new Vector3(
                slotTransform.localPosition.x,
                middlePosition,
                0
            );

            // Reset spacing
            if (reelLayoutGroups != null && col < reelLayoutGroups.Length && reelLayoutGroups[col] != null)
            {
                reelLayoutGroups[col].spacing = defaultSpacing;
            }

            // Reset all image alphas to 1
            var reel = reelImagesList[col];
            if (reel.images != null)
            {
                for (int i = 0; i < reel.images.Count; i++)
                {
                    if (reel.images[i] != null)
                    {
                        Color c = reel.images[i].color;
                        reel.images[i].color = new Color(c.r, c.g, c.b, 1f);
                    }
                }
            }

            // Reset tracked scenario
            if (currentBlankScenarios != null && col < currentBlankScenarios.Length)
            {
                currentBlankScenarios[col] = BlankScenario.NoBlanks;
            }
        }
    }

    /// <summary>
    /// Re-applies blank scenarios for all reels. Called after win animations restore alphas.
    /// </summary>
    private void ReapplyCurrentBlankScenarios()
    {
        if (currentBlankScenarios == null || currentDisplayMatrix == null) return;
        for (int col = 0; col < currentBlankScenarios.Length && col < currentDisplayMatrix.Count; col++)
        {
            if (currentBlankScenarios[col] != BlankScenario.NoBlanks)
            {
                ApplyBlankScenario(col, currentBlankScenarios[col], currentDisplayMatrix[col]);
            }
            else
            {
                // Even for NoBlanks, re-apply buffer alpha 0
                SetImageAlpha(col, 0, 0f);
                SetImageAlpha(col, 1, 0f);
                SetImageAlpha(col, 5, 0f);
                SetImageAlpha(col, 6, 0f);
            }
        }
    }

    /// <summary>
    /// Re-applies blank alpha for a single column. Called after ResetSymbolScale restores alpha.
    /// </summary>
    private void ReapplyBlankAlphaForColumn(int columnIndex)
    {
        if (currentBlankScenarios == null || columnIndex >= currentBlankScenarios.Length) return;
        if (currentDisplayMatrix == null || columnIndex >= currentDisplayMatrix.Count) return;

        ApplyBlankScenario(columnIndex, currentBlankScenarios[columnIndex], currentDisplayMatrix[columnIndex]);
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
    public List<Image> images = new List<Image>(7);
}
