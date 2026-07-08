using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIImageFrameAnimator : MonoBehaviour
{
    public RectTransform parentRect;
    public RectTransform[] childImages;
    public float startDelay = 0.5f;
    public float displayTime = 2f;
    public float scrollSpeed = 100f;
    public float endPauseTime = 1f;

    private Vector2[] originalPositions;

    void Awake()
    {
        // Store ORIGINAL positions only once
        originalPositions = new Vector2[childImages.Length];

        for (int i = 0; i < childImages.Length; i++)
        {
            originalPositions[i] = childImages[i].anchoredPosition;
        }
    }

    void OnEnable()
    {
        StopAllCoroutines();
        StartCoroutine(PlaySequence());
    }

    IEnumerator PlaySequence()
    {
        int index = 0;

        while (true)
        {
            foreach (var img in childImages)
                img.gameObject.SetActive(false);

            RectTransform current = childImages[index];
            current.gameObject.SetActive(true);

            current.anchoredPosition = originalPositions[index];

            float parentWidth = parentRect.rect.width;
            float childWidth = current.rect.width;

            LayoutRebuilder.ForceRebuildLayoutImmediate(current);

            if (childWidth > parentWidth)
            {
                float startX = originalPositions[index].x;
                float targetX = startX - (childWidth - parentWidth);

                yield return new WaitForSeconds(startDelay);

                while (current.anchoredPosition.x > targetX)
                {
                    Vector2 pos = current.anchoredPosition;
                    pos.x -= scrollSpeed * Time.deltaTime;

                    if (pos.x <= targetX)
                        pos.x = targetX;

                    current.anchoredPosition = pos;

                    yield return null;
                }

                yield return new WaitForSeconds(endPauseTime);
            }
            else
            {
                yield return new WaitForSeconds(displayTime);
            }

            index++;
            if (index >= childImages.Length)
                index = 0;
        }
    }
}