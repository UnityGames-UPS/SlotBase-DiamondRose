using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageAnimation : MonoBehaviour
{
    public enum ImageState
    {
        NONE,
        PLAYING,
        PAUSED
    }

    public enum AnimationMode
    {
        SINGLE_PHASE,
        TWO_PHASE
    }

    public static ImageAnimation Instance;

    public List<Sprite> textureArray;
    public Image rendererDelegate;
    public bool useSharedMaterial = true;
    public bool doLoopAnimation = true;
    
    [Header("Dynamic Timing")]
    public bool useDynamicFramerate = false;
    public float dynamicLoopDuration = 1.0f;
    public System.Action<int> onLoopComplete;
    private int currentLoopCount = 0;
    
    [SerializeField] private bool StartOnAwake;
    [SerializeField] private bool StartonEnable;

    [HideInInspector]
    public ImageState currentAnimationState;

    private int indexOfTexture;
    private float idealFrameRate = 0.0416666679f;
    private float delayBetweenAnimation;

    public float AnimationSpeed = 5f;
    public float delayBetweenLoop;

    [Header("Two Phase Animation (Optional)")]
    public AnimationMode animationMode = AnimationMode.SINGLE_PHASE;
    
    [Tooltip("Index where Phase 2 starts (Phase 1 is 0 to this index-1)")]
    public int phase2StartIndex = 0;
    
    [Tooltip("How many times Phase 1 should loop (-1 = infinite, 0 = skip phase 1, 1+ = specific count)")]
    public int phase1LoopCount = 1;
    
    [Tooltip("How many times Phase 2 should loop (-1 = infinite, 0 = skip phase 2, 1+ = specific count)")]
    public int phase2LoopCount = -1;

    // Two-phase tracking
    private int currentPhase = 1;
    private int phase1CurrentLoop = 0;
    private int phase2CurrentLoop = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        if (StartOnAwake)
        {
            StartAnimation();
        }
    }

    void Start()
    {
        //rendererDelegate= this.GetComponent<Image>();
    }

    private void OnEnable()
    {
        if (StartonEnable)
        {
            StartAnimation();
        }
    }

    private void OnDisable()
    {
        //rendererDelegate.sprite = textureArray[0];
        StopAnimation();
    }

    private void AnimationProcess()
    {
        SetTextureOfIndex();
        indexOfTexture++;

        // Use original logic if in SINGLE_PHASE mode
        if (animationMode == AnimationMode.SINGLE_PHASE)
        {
            if (indexOfTexture == textureArray.Count)
            {
                indexOfTexture = 0;
                currentLoopCount++;
                onLoopComplete?.Invoke(currentLoopCount);
                
                if (doLoopAnimation)
                {
                    Invoke("AnimationProcess", delayBetweenAnimation + delayBetweenLoop);
                }
            }
            else
            {
                Invoke("AnimationProcess", delayBetweenAnimation);
            }
        }
        else // TWO_PHASE mode
        {
            HandleTwoPhaseAnimation();
        }
    }

    private void HandleTwoPhaseAnimation()
    {
        // Phase 1 logic
        if (currentPhase == 1)
        {
            if (indexOfTexture >= phase2StartIndex)
            {
                // Phase 1 completed one loop
                phase1CurrentLoop++;
                
                // Check if we should continue Phase 1 or move to Phase 2
                if (phase1LoopCount == -1 || phase1CurrentLoop < phase1LoopCount)
                {
                    // Continue looping Phase 1
                    indexOfTexture = 0;
                    Invoke("AnimationProcess", delayBetweenAnimation + delayBetweenLoop);
                }
                else
                {
                    // Move to Phase 2
                    currentPhase = 2;
                    indexOfTexture = phase2StartIndex;
                    
                    // Skip Phase 2 if loop count is 0
                    if (phase2LoopCount == 0)
                    {
                        currentAnimationState = ImageState.NONE;
                        return;
                    }
                    
                    Invoke("AnimationProcess", delayBetweenAnimation + delayBetweenLoop);
                }
            }
            else
            {
                Invoke("AnimationProcess", delayBetweenAnimation);
            }
        }
        // Phase 2 logic
        else if (currentPhase == 2)
        {
            if (indexOfTexture >= textureArray.Count)
            {
                // Phase 2 completed one loop
                phase2CurrentLoop++;
                
                // Check if we should continue Phase 2 or stop
                if (phase2LoopCount == -1 || phase2CurrentLoop < phase2LoopCount)
                {
                    // Continue looping Phase 2
                    indexOfTexture = phase2StartIndex;
                    Invoke("AnimationProcess", delayBetweenAnimation + delayBetweenLoop);
                }
                else
                {
                    // Animation complete
                    currentAnimationState = ImageState.NONE;
                }
            }
            else
            {
                Invoke("AnimationProcess", delayBetweenAnimation);
            }
        }
    }

    public void StartAnimation()
    {
        if (textureArray == null || textureArray.Count == 0) return;

        CancelInvoke(nameof(AnimationProcess));
        indexOfTexture = 0;
        currentLoopCount = 0;
        
        // Reset two-phase tracking
        currentPhase = 1;
        phase1CurrentLoop = 0;
        phase2CurrentLoop = 0;

        if (currentAnimationState == ImageState.NONE)
        {
            RevertToInitialState();
            
            if (useDynamicFramerate && textureArray != null && textureArray.Count > 0)
            {
                delayBetweenAnimation = dynamicLoopDuration / textureArray.Count;
            }
            else
            {
                delayBetweenAnimation = idealFrameRate * (float)textureArray.Count / AnimationSpeed;
            }
            
            currentAnimationState = ImageState.PLAYING;
            
            // Skip Phase 1 if in TWO_PHASE mode and loop count is 0
            if (animationMode == AnimationMode.TWO_PHASE && phase1LoopCount == 0)
            {
                currentPhase = 2;
                indexOfTexture = phase2StartIndex;
            }
            
            Invoke("AnimationProcess", delayBetweenAnimation);
        }
    }

    public void PauseAnimation()
    {
        if (currentAnimationState == ImageState.PLAYING)
        {
            CancelInvoke("AnimationProcess");
            currentAnimationState = ImageState.PAUSED;
        }
    }

    public void ResumeAnimation()
    {
        if (currentAnimationState == ImageState.PAUSED && !IsInvoking("AnimationProcess"))
        {
            Invoke("AnimationProcess", delayBetweenAnimation);
            currentAnimationState = ImageState.PLAYING;
        }
    }

    public void StopAnimation()
    {
        if (currentAnimationState != 0)
        {
            if (textureArray != null && textureArray.Count > 0)
            {
                rendererDelegate.sprite = textureArray[0];
            }
            CancelInvoke("AnimationProcess");
            currentAnimationState = ImageState.NONE;
            
            // Reset two-phase tracking
            currentPhase = 1;
            phase1CurrentLoop = 0;
            phase2CurrentLoop = 0;
            currentLoopCount = 0;
        }
    }

    public void RevertToInitialState()
    {
        indexOfTexture = 0;
        currentPhase = 1;
        phase1CurrentLoop = 0;
        phase2CurrentLoop = 0;
        SetTextureOfIndex();
    }

    private void SetTextureOfIndex()
    {
        if (textureArray == null || textureArray.Count == 0 || indexOfTexture < 0 || indexOfTexture >= textureArray.Count) return;

        if (useSharedMaterial)
        {
            rendererDelegate.sprite = textureArray[indexOfTexture];
        }
        else
        {
            rendererDelegate.sprite = textureArray[indexOfTexture];
        }
    }
}