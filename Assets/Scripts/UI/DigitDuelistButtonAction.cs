using UnityEngine;
using UnityEngine.UI;

namespace DigitDuelist
{
    public enum ButtonActionType
    {
        StartGame,
        LoadGame,
        OpenSettings,
        QuitApp,
        SelectAddition,
        SelectSubtraction,
        SelectMultiplication,
        SelectDivision,
        SelectMDAS,
        BackToMainMenuFromMode,
        PauseGame,
        ResumeGame,
        SaveGame,
        QuitToMainMenuFromPause,
        CloseSettings,
        ProceedNextLevel,
        PostLevelToMainMenu,
        RetryLevel,
        GameOverToMainMenu,
        Choice0,
        Choice1,
        Choice2,
        Choice3,
        CastFreeze,
        CastCrystalBall,
        CastBubbleShield,
        ToggleZenMode,
        SetPresetNovice,
        SetPresetAdventurer,
        SetPresetMaster,
        DismissHint
    }

    /// <summary>
    /// Attach to any UI Button to automatically bind its click event at runtime to GameManager or DuelController.
    /// Provides zero-configuration runtime resilience for single-scene button routing.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class DigitDuelistButtonAction : MonoBehaviour
    {
        [SerializeField] private ButtonActionType actionType;

        public ButtonActionType ActionType
        {
            get => actionType;
            set => actionType = value;
        }

        private void Awake()
        {
            Button btn = GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(ExecuteAction);
            }
        }

        public void ExecuteAction()
        {
            GameManager gm = GameManager.Instance ?? FindFirstObjectByType<GameManager>();
            DuelController duel = FindFirstObjectByType<DuelController>();

            switch (actionType)
            {
                case ButtonActionType.StartGame:
                    if (gm != null) gm.OnStartButtonPressed();
                    break;

                case ButtonActionType.LoadGame:
                    if (gm != null) gm.OnLoadButtonPressed();
                    break;

                case ButtonActionType.OpenSettings:
                    if (gm != null) gm.OnSettingsButtonPressed();
                    break;

                case ButtonActionType.QuitApp:
                    if (gm != null) gm.OnQuitButtonPressed();
                    break;

                case ButtonActionType.SelectAddition:
                    if (gm != null) gm.SelectAdditionMode();
                    break;

                case ButtonActionType.SelectSubtraction:
                    if (gm != null) gm.SelectSubtractionMode();
                    break;

                case ButtonActionType.SelectMultiplication:
                    if (gm != null) gm.SelectMultiplicationMode();
                    break;

                case ButtonActionType.SelectDivision:
                    if (gm != null) gm.SelectDivisionMode();
                    break;

                case ButtonActionType.SelectMDAS:
                    if (gm != null) gm.SelectMDASMode();
                    break;

                case ButtonActionType.BackToMainMenuFromMode:
                    if (gm != null) gm.OnBackToMainMenuFromModeSelect();
                    break;

                case ButtonActionType.PauseGame:
                    if (gm != null) gm.PauseGame();
                    break;

                case ButtonActionType.ResumeGame:
                    if (gm != null) gm.ResumeGame();
                    break;

                case ButtonActionType.SaveGame:
                    if (gm != null) gm.OnPauseMenuSavePressed();
                    break;

                case ButtonActionType.QuitToMainMenuFromPause:
                    if (gm != null) gm.OnPauseMenuQuitToMainMenuPressed();
                    break;

                case ButtonActionType.CloseSettings:
                    if (gm != null) gm.CloseSettings();
                    break;

                case ButtonActionType.ProceedNextLevel:
                    if (gm != null) gm.OnProceedNextLevelPressed();
                    break;

                case ButtonActionType.PostLevelToMainMenu:
                    if (gm != null) gm.OnPostLevelReturnToMenuPressed();
                    break;

                case ButtonActionType.RetryLevel:
                    if (gm != null) gm.OnRetryLevelPressed();
                    break;

                case ButtonActionType.GameOverToMainMenu:
                    if (gm != null) gm.OnGameOverReturnToMenuPressed();
                    break;

                case ButtonActionType.Choice0:
                case ButtonActionType.Choice1:
                case ButtonActionType.Choice2:
                case ButtonActionType.Choice3:
                    int choiceIndex = (int)actionType - (int)ButtonActionType.Choice0;
                    if (duel != null && duel.CurrentQuestion.choices != null && choiceIndex < duel.CurrentQuestion.choices.Length)
                    {
                        int chosenVal = duel.CurrentQuestion.choices[choiceIndex];
                        PlayerData data = gm != null ? gm.CurrentPlayerData : null;
                        duel.SubmitAnswer(chosenVal, data);
                    }
                    break;

                case ButtonActionType.CastFreeze:
                    if (AssistSpellsController.Instance != null) AssistSpellsController.Instance.CastFreezeTime();
                    break;

                case ButtonActionType.CastCrystalBall:
                    if (AssistSpellsController.Instance != null) AssistSpellsController.Instance.CastCrystalBall();
                    break;

                case ButtonActionType.CastBubbleShield:
                    if (AssistSpellsController.Instance != null) AssistSpellsController.Instance.CastBubbleShield();
                    break;

                case ButtonActionType.ToggleZenMode:
                    if (GameModeManager.Instance != null) GameModeManager.Instance.ToggleZenMode();
                    break;

                case ButtonActionType.SetPresetNovice:
                    if (GameModeManager.Instance != null) GameModeManager.Instance.SetDifficultyPreset(AgeDifficultyPreset.Novice);
                    break;

                case ButtonActionType.SetPresetAdventurer:
                    if (GameModeManager.Instance != null) GameModeManager.Instance.SetDifficultyPreset(AgeDifficultyPreset.Adventurer);
                    break;

                case ButtonActionType.SetPresetMaster:
                    if (GameModeManager.Instance != null) GameModeManager.Instance.SetDifficultyPreset(AgeDifficultyPreset.Master);
                    break;

                case ButtonActionType.DismissHint:
                    VisualMathHintCard hint = FindFirstObjectByType<VisualMathHintCard>();
                    if (hint != null) hint.HideHint();
                    break;
            }
        }
    }
}
