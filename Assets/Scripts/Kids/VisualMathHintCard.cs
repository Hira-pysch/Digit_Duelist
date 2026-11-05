using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DigitDuelist
{
    /// <summary>
    /// Displays a visual educational breakdown for arithmetic problems when a child needs help
    /// or answers incorrectly. Replaces frustration with clarity and positive learning.
    /// </summary>
    public class VisualMathHintCard : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject cardRoot;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI visualGridText;
        [SerializeField] private TextMeshProUGUI explanationText;
        [SerializeField] private Button dismissButton;

        private void Awake()
        {
            if (cardRoot == null) cardRoot = gameObject;
            if (dismissButton != null)
            {
                dismissButton.onClick.RemoveAllListeners();
                dismissButton.onClick.AddListener(HideHint);
            }
        }

        /// <summary>
        /// Populates and displays the visual hint card based on the question.
        /// </summary>
        public void ShowHint(MathQuestion question)
        {
            if (cardRoot != null) cardRoot.SetActive(true);

            if (titleText != null)
            {
                titleText.text = "💡 Math Detective Hint!";
            }

            string visualContent = "";
            string explanation = "";

            switch (question.activeOperation)
            {
                case MathOperation.Multiplication:
                    int rows = Mathf.Clamp(question.operand1, 1, 10);
                    int cols = Mathf.Clamp(question.operand2, 1, 10);
                    explanation = $"<b>{rows} × {cols}</b> means <b>{rows} groups of {cols}</b>!";
                    
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    for (int r = 0; r < rows; r++)
                    {
                        for (int c = 0; c < cols; c++)
                        {
                            sb.Append(" ⭐ ");
                        }
                        sb.AppendLine();
                    }
                    sb.Append($"<size=80%><color=#88D49E>(Count them all up = {question.correctAnswer}!)</color></size>");
                    visualContent = sb.ToString();
                    break;

                case MathOperation.Addition:
                    int a = question.operand1;
                    int b = question.operand2;
                    int nearestTen = Mathf.CeilToInt(a / 10f) * 10;
                    if (nearestTen == a) nearestTen += 10;
                    int jumpToTen = nearestTen - a;

                    if (jumpToTen > 0 && jumpToTen < b)
                    {
                        int remainder = b - jumpToTen;
                        explanation = $"Friendly 10s Trick:\n" +
                                      $"• Start at <b>{a}</b>\n" +
                                      $"• Jump <b>+{jumpToTen}</b> to reach <b>{nearestTen}</b>\n" +
                                      $"• Add the rest <b>+{remainder}</b> = <b>{question.correctAnswer}</b>! 🎯";
                    }
                    else
                    {
                        explanation = $"Count forward from <b>{a}</b> by <b>{b}</b> steps to reach <b>{question.correctAnswer}</b>!";
                    }

                    visualContent = GenerateBlockLine(a, "🔵") + "\n+ PLUS +\n" + GenerateBlockLine(b, "🟡");
                    break;

                case MathOperation.Subtraction:
                    int big = question.operand1;
                    int take = question.operand2;
                    explanation = $"Start with <b>{big}</b>, take away <b>{take}</b>, and you are left with <b>{question.correctAnswer}</b>!";
                    visualContent = GenerateSubtractionVisual(big, take);
                    break;

                case MathOperation.Division:
                    int total = question.operand1;
                    int groups = question.operand2;
                    int each = question.correctAnswer;
                    explanation = $"Sharing <b>{total}</b> treasures equally among <b>{groups}</b> friends:\n" +
                                  $"Every friend gets <b>{each}</b>! 🎁";
                    visualContent = GenerateDivisionVisual(groups, each);
                    break;

                default:
                    explanation = $"The correct answer is <b>{question.correctAnswer}</b>! Keep going!";
                    break;
            }

            if (visualGridText != null) visualGridText.text = visualContent;
            if (explanationText != null) explanationText.text = explanation;
        }

        private string GenerateBlockLine(int count, string icon)
        {
            if (count > 15) return $"{icon} × {count}";
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < count; i++) sb.Append(icon + " ");
            return sb.ToString();
        }

        private string GenerateSubtractionVisual(int total, int takeAway)
        {
            if (total > 15) return $"Total items: {total}  —  Remove: {takeAway}  =  {total - takeAway}";
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < total - takeAway; i++) sb.Append(" 🟢 ");
            for (int i = 0; i < takeAway; i++) sb.Append(" ❌ ");
            return sb.ToString();
        }

        private string GenerateDivisionVisual(int groups, int countPerGroup)
        {
            if (groups > 6 || countPerGroup > 8) return $"{groups} groups of {countPerGroup}";
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int g = 0; g < groups; g++)
            {
                sb.Append($"[ ");
                for (int c = 0; c < countPerGroup; c++) sb.Append("💎 ");
                sb.Append($"]  ");
            }
            return sb.ToString();
        }

        public void HideHint()
        {
            if (cardRoot != null) cardRoot.SetActive(false);
        }
    }
}
