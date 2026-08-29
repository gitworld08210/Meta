using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using MetaCricket.Core;

namespace MetaCricket.UI
{
    /// <summary>
    /// Settings screen with glassmorphism card layout, toggle switches for
    /// Hindi/English commentary, audio sliders, camera sensitivity,
    /// calibration reset, and account section.
    /// </summary>
    public class SettingsUI : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GlassMorphismEffect _settingsPanel;
        [SerializeField] private AnimatedTransition _screenTransition;
        [SerializeField] private ScrollRect _scrollRect;

        [Header("Commentary Language")]
        [SerializeField] private Toggle _englishToggle;
        [SerializeField] private Toggle _hindiToggle;
        [SerializeField] private Text _languageLabel;

        [Header("Audio Settings")]
        [SerializeField] private Slider _masterVolumeSlider;
        [SerializeField] private Slider _musicVolumeSlider;
        [SerializeField] private Slider _sfxVolumeSlider;
        [SerializeField] private Text _masterVolumeText;
        [SerializeField] private Text _musicVolumeText;
        [SerializeField] private Text _sfxVolumeText;

        [Header("Gameplay Settings")]
        [SerializeField] private Slider _cameraSensitivitySlider;
        [SerializeField] private Text _cameraSensitivityText;
        [SerializeField] private Toggle _hapticsToggle;
        [SerializeField] private Toggle _highQualityToggle;
        [SerializeField] private Toggle _showTutorialsToggle;

        [Header("Calibration")]
        [SerializeField] private GoldGradientButton _recalibrateButton;
        [SerializeField] private Text _lastCalibrationText;

        [Header("Account")]
        [SerializeField] private GlassMorphismEffect _accountCard;
        [SerializeField] private Text _playerNameText;
        [SerializeField] private Text _playerIdText;
        [SerializeField] private GoldGradientButton _logoutButton;
        [SerializeField] private GoldGradientButton _deleteAccountButton;

        [Header("Navigation")]
        [SerializeField] private Button _backButton;

        private GameSettings _currentSettings;

        private void Start()
        {
            SetupListeners();
            LoadCurrentSettings();
        }

        private void OnEnable()
        {
            if (_screenTransition != null)
            {
                _screenTransition.Show();
            }
        }

        private void SetupListeners()
        {
            // Language toggles
            if (_englishToggle != null)
                _englishToggle.onValueChanged.AddListener(OnEnglishSelected);
            if (_hindiToggle != null)
                _hindiToggle.onValueChanged.AddListener(OnHindiSelected);

            // Audio sliders
            if (_masterVolumeSlider != null)
                _masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            if (_musicVolumeSlider != null)
                _musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            if (_sfxVolumeSlider != null)
                _sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

            // Gameplay
            if (_cameraSensitivitySlider != null)
                _cameraSensitivitySlider.onValueChanged.AddListener(OnCameraSensitivityChanged);
            if (_hapticsToggle != null)
                _hapticsToggle.onValueChanged.AddListener(OnHapticsChanged);
            if (_highQualityToggle != null)
                _highQualityToggle.onValueChanged.AddListener(OnHighQualityChanged);
            if (_showTutorialsToggle != null)
                _showTutorialsToggle.onValueChanged.AddListener(OnShowTutorialsChanged);

            // Buttons
            if (_recalibrateButton != null)
            {
                Button btn = _recalibrateButton.GetComponent<Button>();
                if (btn != null) btn.onClick.AddListener(OnRecalibrateClicked);
            }

            if (_logoutButton != null)
            {
                Button btn = _logoutButton.GetComponent<Button>();
                if (btn != null) btn.onClick.AddListener(OnLogoutClicked);
            }

            if (_backButton != null)
                _backButton.onClick.AddListener(OnBackClicked);
        }

        private void LoadCurrentSettings()
        {
            _currentSettings = GameSettings.CreateDefault();

            // Try to load from SaveSystem
            if (ServiceLocator.TryGet<SaveSystem>(out var saveSystem))
            {
                // Load settings from save system
            }

            ApplySettingsToUI();
        }

        private void ApplySettingsToUI()
        {
            if (_currentSettings == null) return;

            // Language
            if (_englishToggle != null)
                _englishToggle.isOn = _currentSettings.PreferredLanguage == CommentaryLanguage.English;
            if (_hindiToggle != null)
                _hindiToggle.isOn = _currentSettings.PreferredLanguage == CommentaryLanguage.Hindi;

            // Audio
            if (_masterVolumeSlider != null)
                _masterVolumeSlider.value = _currentSettings.MasterVolume;
            if (_musicVolumeSlider != null)
                _musicVolumeSlider.value = _currentSettings.MusicVolume;
            if (_sfxVolumeSlider != null)
                _sfxVolumeSlider.value = _currentSettings.SFXVolume;

            // Gameplay
            if (_cameraSensitivitySlider != null)
                _cameraSensitivitySlider.value = _currentSettings.CameraSensitivity;
            if (_hapticsToggle != null)
                _hapticsToggle.isOn = _currentSettings.HapticsEnabled;
            if (_highQualityToggle != null)
                _highQualityToggle.isOn = _currentSettings.HighQualityGraphics;
            if (_showTutorialsToggle != null)
                _showTutorialsToggle.isOn = _currentSettings.ShowTutorials;

            UpdateVolumeLabels();
        }

        private void UpdateVolumeLabels()
        {
            if (_masterVolumeText != null)
                _masterVolumeText.text = $"{Mathf.RoundToInt(_currentSettings.MasterVolume * 100)}%";
            if (_musicVolumeText != null)
                _musicVolumeText.text = $"{Mathf.RoundToInt(_currentSettings.MusicVolume * 100)}%";
            if (_sfxVolumeText != null)
                _sfxVolumeText.text = $"{Mathf.RoundToInt(_currentSettings.SFXVolume * 100)}%";
            if (_cameraSensitivityText != null)
                _cameraSensitivityText.text = $"{_currentSettings.CameraSensitivity:F1}x";
        }

        private void OnEnglishSelected(bool isOn)
        {
            if (isOn)
            {
                _currentSettings.PreferredLanguage = CommentaryLanguage.English;
                SaveSettings();
            }
        }

        private void OnHindiSelected(bool isOn)
        {
            if (isOn)
            {
                _currentSettings.PreferredLanguage = CommentaryLanguage.Hindi;
                SaveSettings();
            }
        }

        private void OnMasterVolumeChanged(float value)
        {
            _currentSettings.MasterVolume = value;
            UpdateVolumeLabels();
            SaveSettings();
        }

        private void OnMusicVolumeChanged(float value)
        {
            _currentSettings.MusicVolume = value;
            UpdateVolumeLabels();
            SaveSettings();
        }

        private void OnSFXVolumeChanged(float value)
        {
            _currentSettings.SFXVolume = value;
            UpdateVolumeLabels();
            SaveSettings();
        }

        private void OnCameraSensitivityChanged(float value)
        {
            _currentSettings.CameraSensitivity = value;
            UpdateVolumeLabels();
            SaveSettings();
        }

        private void OnHapticsChanged(bool isOn)
        {
            _currentSettings.HapticsEnabled = isOn;
            SaveSettings();
        }

        private void OnHighQualityChanged(bool isOn)
        {
            _currentSettings.HighQualityGraphics = isOn;
            SaveSettings();
        }

        private void OnShowTutorialsChanged(bool isOn)
        {
            _currentSettings.ShowTutorials = isOn;
            SaveSettings();
        }

        private void OnRecalibrateClicked()
        {
            EventBus.Publish(new UITransitionEvent
            {
                FromScreen = UIScreen.Settings,
                ToScreen = UIScreen.Calibration,
                Animated = true
            });
        }

        private void OnLogoutClicked()
        {
            Debug.Log("[SettingsUI] Logout requested");
            // Backend auth logout handled by SupabaseAuth
        }

        private void OnBackClicked()
        {
            EventBus.Publish(new UITransitionEvent
            {
                FromScreen = UIScreen.Settings,
                ToScreen = UIScreen.MainMenu,
                Animated = true
            });
        }

        private void SaveSettings()
        {
            // Save via event or direct service locator access
            Debug.Log("[SettingsUI] Settings updated");
        }

        /// <summary>
        /// Set account information display.
        /// </summary>
        public void SetAccountInfo(string playerName, string playerId)
        {
            if (_playerNameText != null) _playerNameText.text = playerName;
            if (_playerIdText != null) _playerIdText.text = $"ID: {playerId}";
        }
    }
}
