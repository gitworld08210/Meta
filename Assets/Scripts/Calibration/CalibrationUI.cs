using UnityEngine;
using UnityEngine.UI;

namespace MetaCricket.Calibration
{
    /// <summary>
    /// UI controller showing step-by-step calibration instructions,
    /// progress indicator, and visual guides for player positioning.
    /// </summary>
    public class CalibrationUI : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField]
        [Tooltip("Root panel for calibration UI.")]
        private GameObject _calibrationPanel;

        [SerializeField]
        [Tooltip("Panel shown during positioning phase.")]
        private GameObject _positioningPanel;

        [SerializeField]
        [Tooltip("Panel shown when waiting for T-pose.")]
        private GameObject _tposeInstructionPanel;

        [SerializeField]
        [Tooltip("Panel shown while holding T-pose.")]
        private GameObject _holdProgressPanel;

        [SerializeField]
        [Tooltip("Panel shown during processing.")]
        private GameObject _processingPanel;

        [SerializeField]
        [Tooltip("Panel shown when calibration is complete.")]
        private GameObject _completePanel;

        [SerializeField]
        [Tooltip("Panel shown when calibration fails.")]
        private GameObject _failedPanel;

        [Header("UI Elements")]
        [SerializeField]
        [Tooltip("Text displaying current instructions.")]
        private Text _instructionText;

        [SerializeField]
        [Tooltip("Progress bar for T-pose hold duration.")]
        private Slider _progressSlider;

        [SerializeField]
        [Tooltip("Circular progress indicator.")]
        private Image _circularProgress;

        [SerializeField]
        [Tooltip("Text showing hold progress percentage.")]
        private Text _progressText;

        [SerializeField]
        [Tooltip("Text displaying error/failure reason.")]
        private Text _failureReasonText;

        [SerializeField]
        [Tooltip("Body outline guide image for positioning.")]
        private Image _bodyOutlineGuide;

        [SerializeField]
        [Tooltip("T-pose silhouette guide image.")]
        private Image _tposeSilhouetteGuide;

        [Header("Animation Settings")]
        [SerializeField]
        [Tooltip("Fade duration for panel transitions.")]
        private float _fadeDuration = 0.3f;

        [SerializeField]
        [Tooltip("Color for successful progress.")]
        private Color _successColor = Color.green;

        [SerializeField]
        [Tooltip("Color for in-progress state.")]
        private Color _progressColor = Color.cyan;

        [SerializeField]
        [Tooltip("Color for failure state.")]
        private Color _failureColor = Color.red;

        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            HideAll();
        }

        /// <summary>
        /// Show the positioning instructions (step 1: get in frame).
        /// </summary>
        public void ShowPositioningInstructions()
        {
            HideAllPanels();

            if (_calibrationPanel != null)
                _calibrationPanel.SetActive(true);

            if (_positioningPanel != null)
                _positioningPanel.SetActive(true);

            if (_instructionText != null)
                _instructionText.text = "Stand in front of the camera\nMake sure your upper body is visible";

            if (_bodyOutlineGuide != null)
                _bodyOutlineGuide.gameObject.SetActive(true);

            if (_tposeSilhouetteGuide != null)
                _tposeSilhouetteGuide.gameObject.SetActive(false);
        }

        /// <summary>
        /// Show the T-pose instructions (step 2: hold arms out).
        /// </summary>
        public void ShowTPoseInstructions()
        {
            HideAllPanels();

            if (_calibrationPanel != null)
                _calibrationPanel.SetActive(true);

            if (_tposeInstructionPanel != null)
                _tposeInstructionPanel.SetActive(true);

            if (_instructionText != null)
                _instructionText.text = "Extend your arms horizontally\nHold a T-pose position";

            if (_bodyOutlineGuide != null)
                _bodyOutlineGuide.gameObject.SetActive(false);

            if (_tposeSilhouetteGuide != null)
                _tposeSilhouetteGuide.gameObject.SetActive(true);

            ResetProgressUI();
        }

        /// <summary>
        /// Show the hold progress (step 3: maintain T-pose for duration).
        /// </summary>
        /// <param name="progress">Current hold progress (0-1).</param>
        public void ShowHoldProgress(float progress)
        {
            if (_holdProgressPanel != null && !_holdProgressPanel.activeSelf)
            {
                HideAllPanels();
                if (_calibrationPanel != null)
                    _calibrationPanel.SetActive(true);
                _holdProgressPanel.SetActive(true);
            }

            if (_instructionText != null)
                _instructionText.text = "Hold still...";

            UpdateProgressUI(progress);
        }

        /// <summary>
        /// Show the processing state.
        /// </summary>
        public void ShowProcessing()
        {
            HideAllPanels();

            if (_calibrationPanel != null)
                _calibrationPanel.SetActive(true);

            if (_processingPanel != null)
                _processingPanel.SetActive(true);

            if (_instructionText != null)
                _instructionText.text = "Processing calibration data...";
        }

        /// <summary>
        /// Show calibration complete state.
        /// </summary>
        public void ShowComplete()
        {
            HideAllPanels();

            if (_calibrationPanel != null)
                _calibrationPanel.SetActive(true);

            if (_completePanel != null)
                _completePanel.SetActive(true);

            if (_instructionText != null)
                _instructionText.text = "Calibration Complete!";

            if (_circularProgress != null)
            {
                _circularProgress.fillAmount = 1f;
                _circularProgress.color = _successColor;
            }

            if (_progressText != null)
                _progressText.text = "Ready to play!";
        }

        /// <summary>
        /// Show calibration failed state with reason.
        /// </summary>
        /// <param name="reason">Reason for failure to display.</param>
        public void ShowFailed(string reason)
        {
            HideAllPanels();

            if (_calibrationPanel != null)
                _calibrationPanel.SetActive(true);

            if (_failedPanel != null)
                _failedPanel.SetActive(true);

            if (_instructionText != null)
                _instructionText.text = "Calibration Failed";

            if (_failureReasonText != null)
                _failureReasonText.text = reason;

            if (_circularProgress != null)
                _circularProgress.color = _failureColor;
        }

        /// <summary>
        /// Hide all calibration UI elements.
        /// </summary>
        public void HideAll()
        {
            if (_calibrationPanel != null)
                _calibrationPanel.SetActive(false);

            HideAllPanels();
        }

        /// <summary>
        /// Update the progress UI elements.
        /// </summary>
        private void UpdateProgressUI(float progress)
        {
            if (_progressSlider != null)
                _progressSlider.value = progress;

            if (_circularProgress != null)
            {
                _circularProgress.fillAmount = progress;
                _circularProgress.color = _progressColor;
            }

            if (_progressText != null)
                _progressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
        }

        /// <summary>
        /// Reset progress UI to zero state.
        /// </summary>
        private void ResetProgressUI()
        {
            if (_progressSlider != null)
                _progressSlider.value = 0f;

            if (_circularProgress != null)
            {
                _circularProgress.fillAmount = 0f;
                _circularProgress.color = _progressColor;
            }

            if (_progressText != null)
                _progressText.text = "0%";
        }

        /// <summary>
        /// Hide all sub-panels without hiding the root calibration panel.
        /// </summary>
        private void HideAllPanels()
        {
            if (_positioningPanel != null) _positioningPanel.SetActive(false);
            if (_tposeInstructionPanel != null) _tposeInstructionPanel.SetActive(false);
            if (_holdProgressPanel != null) _holdProgressPanel.SetActive(false);
            if (_processingPanel != null) _processingPanel.SetActive(false);
            if (_completePanel != null) _completePanel.SetActive(false);
            if (_failedPanel != null) _failedPanel.SetActive(false);

            if (_bodyOutlineGuide != null) _bodyOutlineGuide.gameObject.SetActive(false);
            if (_tposeSilhouetteGuide != null) _tposeSilhouetteGuide.gameObject.SetActive(false);
        }
    }
}
