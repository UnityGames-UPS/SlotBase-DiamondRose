using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SlotBase.UI
{
    [System.Serializable]
    public class WinLineUI
    {
        [Tooltip("Exactly 2 elements: Index 0 is the Start Image, Index 1 is the End Image.")]
        public Image[] startEndImages = new Image[2];

        [Tooltip("The procedural image segments forming the line. The count is flexible based on the line's shape.")]
        public Image[] lineSegments;
    }

    public class WinLineDisplayManager : MonoBehaviour
    {
        public static WinLineDisplayManager Instance { get; private set; }

        [Header("Win Line Data")]
        [Tooltip("Assign the win lines in the Inspector in their initialization order (0, 1, 2, ...).")]
        [SerializeField] private List<WinLineUI> winLines = new List<WinLineUI>();

        [Header("Sprites")]
        [Tooltip("Normal sprite used at the start/default state.")]
        [SerializeField] private Sprite normalSprite;
        
        [Tooltip("Activated sprite used when the win line is highlighted.")]
        [SerializeField] private Sprite activatedSprite;

        [Header("Colors")]
        [Tooltip("Default gray color for line segments.")]
        [SerializeField] private Color normalColor = Color.gray;
        
        [Tooltip("Active red color for line segments when winning.")]
        [SerializeField] private Color activeColor = Color.red;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            ResetAllLines();
        }

        /// <summary>
        /// Resets all lines back to their normal gray color and normal sprites.
        /// </summary>
        public void ResetAllLines()
        {
            for (int i = 0; i < winLines.Count; i++)
            {
                SetLineState(i, false);
            }
        }

        /// <summary>
        /// Sets a specific win line state: active (red color, activated sprite) or inactive (gray color, normal sprite).
        /// </summary>
        /// <param name="lineIndex">The 0-based index of the win line matching its init order.</param>
        /// <param name="isActive">True to highlight/activate, false to reset to normal.</param>
        public void SetLineState(int lineIndex, bool isActive)
        {
            if (lineIndex < 0 || lineIndex >= winLines.Count)
            {
                Debug.LogWarning($"[WinLineDisplayManager] Line index {lineIndex} is out of bounds (Total lines: {winLines.Count}).");
                return;
            }

            var line = winLines[lineIndex];
            if (line == null) return;

            // Set the color for all procedural line segments
            if (line.lineSegments != null)
            {
                Color targetColor = isActive ? activeColor : normalColor;
                foreach (var segment in line.lineSegments)
                {
                    if (segment != null)
                    {
                        segment.color = targetColor;
                    }
                }
            }

            // Set the sprite for the start and end images
            if (line.startEndImages != null)
            {
                Sprite targetSprite = isActive ? activatedSprite : normalSprite;
                foreach (var img in line.startEndImages)
                {
                    if (img != null)
                    {
                        img.sprite = targetSprite;
                    }
                }
            }
        }

        /// <summary>
        /// Highlights the specified win line (sets to active) and resets all other lines to inactive.
        /// </summary>
        /// <param name="lineIndex">The 0-based index of the win line to highlight.</param>
        public void ShowOnlyLine(int lineIndex)
        {
            for (int i = 0; i < winLines.Count; i++)
            {
                SetLineState(i, i == lineIndex);
            }
        }
    }
}
