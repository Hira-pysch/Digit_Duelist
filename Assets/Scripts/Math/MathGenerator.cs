using System;
using System.Collections.Generic;
using UnityEngine;

namespace DigitDuelist
{
    /// <summary>
    /// Represents a generated arithmetic question and multiple-choice options.
    /// </summary>
    [Serializable]
    public struct MathQuestion
    {
        public int operand1;
        public int operand2;
        public MathOperation activeOperation;
        public string operatorSymbol;
        public int correctAnswer;
        public int[] choices;
        public int correctChoiceIndex;
        public string questionText;
    }

    /// <summary>
    /// Generates math problems and multiple choice answers based on mode and difficulty level.
    /// Handles single operations and MDAS (mixed) mode.
    /// </summary>
    public class MathGenerator : MonoBehaviour
    {
        [Header("Current Mode")]
        [SerializeField] private MathOperation selectedMode = MathOperation.Addition;

        public MathOperation SelectedMode
        {
            get => selectedMode;
            set => selectedMode = value;
        }

        /// <summary>
        /// Generates a new MathQuestion using the currently active mode and difficulty level.
        /// </summary>
        public MathQuestion GenerateQuestion(int level = 1)
        {
            return GenerateQuestion(selectedMode, level);
        }

        /// <summary>
        /// Generates a new MathQuestion for a specific mode and difficulty level.
        /// </summary>
        public MathQuestion GenerateQuestion(MathOperation mode, int level)
        {
            MathOperation opToUse = mode;

            // In MDAS mode, randomly pick between Addition (0), Subtraction (1), Multiplication (2), Division (3)
            if (mode == MathOperation.MDAS)
            {
                int randomOpIndex = UnityEngine.Random.Range(0, 4);
                opToUse = (MathOperation)randomOpIndex;
            }

            AgeDifficultyPreset preset = GameModeManager.Instance != null 
                ? GameModeManager.Instance.ActivePreset 
                : AgeDifficultyPreset.Adventurer;

            int op1 = 0;
            int op2 = 0;
            int answer = 0;
            string symbol = "+";

            switch (opToUse)
            {
                case MathOperation.Addition:
                    symbol = "+";
                    if (preset == AgeDifficultyPreset.Novice)
                    {
                        // Single-digit addition up to 10-15
                        op1 = UnityEngine.Random.Range(1, 9);
                        op2 = UnityEngine.Random.Range(1, 10 - op1 + Mathf.Min(5, level));
                    }
                    else if (preset == AgeDifficultyPreset.Adventurer)
                    {
                        // Addition up to 50
                        op1 = UnityEngine.Random.Range(5, 25 + (level * 2));
                        op2 = UnityEngine.Random.Range(3, 20 + (level * 2));
                    }
                    else
                    {
                        // Master mode
                        int addMin = Mathf.Max(10, level * 5);
                        int addMax = Mathf.Max(25, 25 + (level * 8));
                        op1 = UnityEngine.Random.Range(addMin, addMax);
                        op2 = UnityEngine.Random.Range(addMin, addMax);
                    }
                    answer = op1 + op2;
                    break;

                case MathOperation.Subtraction:
                    symbol = "-";
                    if (preset == AgeDifficultyPreset.Novice)
                    {
                        // Single digit subtraction with positive result
                        op1 = UnityEngine.Random.Range(3, 10);
                        op2 = UnityEngine.Random.Range(1, op1);
                    }
                    else if (preset == AgeDifficultyPreset.Adventurer)
                    {
                        // Subtraction up to 50
                        op1 = UnityEngine.Random.Range(12, 45);
                        op2 = UnityEngine.Random.Range(2, op1);
                    }
                    else
                    {
                        // Master mode
                        int subMin = Mathf.Max(15, level * 4);
                        int subMax = Mathf.Max(30, 30 + (level * 8));
                        op1 = UnityEngine.Random.Range(subMin + 5, subMax + 10);
                        op2 = UnityEngine.Random.Range(5, op1);
                    }
                    answer = op1 - op2;
                    break;

                case MathOperation.Multiplication:
                    symbol = "×";
                    if (preset == AgeDifficultyPreset.Novice)
                    {
                        // Starter tables (1, 2, 5, 10)
                        int[] noviceTables = new int[] { 1, 2, 3, 5, 10 };
                        op1 = noviceTables[UnityEngine.Random.Range(0, noviceTables.Length)];
                        op2 = UnityEngine.Random.Range(1, 6);
                    }
                    else if (preset == AgeDifficultyPreset.Adventurer)
                    {
                        // Tables 1 to 5
                        op1 = UnityEngine.Random.Range(2, 6);
                        op2 = UnityEngine.Random.Range(2, 10);
                    }
                    else
                    {
                        // Master mode (up to 12x12)
                        int multMaxA = Mathf.Clamp(3 + level, 4, 12);
                        int multMaxB = Mathf.Clamp(2 + level, 3, 12);
                        op1 = UnityEngine.Random.Range(2, multMaxA + 1);
                        op2 = UnityEngine.Random.Range(2, multMaxB + 1);
                    }
                    answer = op1 * op2;
                    break;

                case MathOperation.Division:
                    symbol = "÷";
                    if (preset == AgeDifficultyPreset.Novice)
                    {
                        // Simple division with small whole quotients
                        int quotient = UnityEngine.Random.Range(1, 5);
                        int divisor = UnityEngine.Random.Range(1, 4);
                        op1 = quotient * divisor;
                        op2 = divisor;
                        answer = quotient;
                    }
                    else if (preset == AgeDifficultyPreset.Adventurer)
                    {
                        // Division within 1 to 5 tables
                        int quotient = UnityEngine.Random.Range(2, 7);
                        int divisor = UnityEngine.Random.Range(2, 6);
                        op1 = quotient * divisor;
                        op2 = divisor;
                        answer = quotient;
                    }
                    else
                    {
                        // Master mode
                        int divQuotientMax = Mathf.Clamp(3 + level, 3, 12);
                        int divDivisorMax = Mathf.Clamp(2 + level, 2, 10);
                        int quotient = UnityEngine.Random.Range(2, divQuotientMax + 1);
                        int divisor = UnityEngine.Random.Range(2, divDivisorMax + 1);
                        op1 = quotient * divisor;
                        op2 = divisor;
                        answer = quotient;
                    }
                    break;
            }

            int[] choices = GenerateChoices(answer, opToUse, op1, op2);
            int correctIndex = Array.IndexOf(choices, answer);

            MathQuestion question = new MathQuestion
            {
                operand1 = op1,
                operand2 = op2,
                activeOperation = opToUse,
                operatorSymbol = symbol,
                correctAnswer = answer,
                choices = choices,
                correctChoiceIndex = correctIndex,
                questionText = $"{op1} {symbol} {op2} = ?"
            };

            return question;
        }

        /// <summary>
        /// Generates 4 unique choices including the correct answer and 3 realistic distractors.
        /// </summary>
        private int[] GenerateChoices(int correctAnswer, MathOperation op, int op1, int op2)
        {
            HashSet<int> choiceSet = new HashSet<int> { correctAnswer };
            List<int> candidateDeltas = new List<int> { -1, 1, -2, 2, -10, 10, -5, 5, -3, 3 };

            // Add contextual wrong answers (e.g. calculation slips)
            if (op == MathOperation.Multiplication)
            {
                choiceSet.Add(op1 + op2); // addition slip
                choiceSet.Add(correctAnswer + op1);
                choiceSet.Add(correctAnswer - op1);
                choiceSet.Add(correctAnswer + op2);
            }
            else if (op == MathOperation.Division)
            {
                choiceSet.Add(op1 - op2);
                choiceSet.Add(correctAnswer + 1);
                choiceSet.Add(Mathf.Max(1, correctAnswer - 1));
            }
            else if (op == MathOperation.Addition)
            {
                choiceSet.Add(Mathf.Abs(op1 - op2));
                choiceSet.Add(correctAnswer + 1);
                choiceSet.Add(correctAnswer - 1);
                choiceSet.Add(correctAnswer + 10);
            }
            else if (op == MathOperation.Subtraction)
            {
                choiceSet.Add(op1 + op2);
                choiceSet.Add(correctAnswer + 1);
                choiceSet.Add(Mathf.Max(0, correctAnswer - 1));
            }

            // Remove any invalid (negative if not allowed, or matching) and fill up to 4
            List<int> validChoices = new List<int>();
            foreach (int val in choiceSet)
            {
                if (val >= 0 && !validChoices.Contains(val))
                {
                    validChoices.Add(val);
                }
            }

            // Ensure we have at least 4 unique options
            int attempts = 0;
            while (validChoices.Count < 4 && attempts < 50)
            {
                attempts++;
                int delta = UnityEngine.Random.Range(-8, 9);
                if (delta == 0) delta = 1;
                int candidate = Mathf.Max(0, correctAnswer + delta);
                if (!validChoices.Contains(candidate))
                {
                    validChoices.Add(candidate);
                }
            }

            // Trim to exactly 4 options ensuring correctAnswer is present
            if (!validChoices.Contains(correctAnswer))
            {
                validChoices[0] = correctAnswer;
            }

            List<int> finalChoices = new List<int>(validChoices.GetRange(0, 4));

            // Shuffle using Fisher-Yates
            for (int i = finalChoices.Count - 1; i > 0; i--)
            {
                int swapIndex = UnityEngine.Random.Range(0, i + 1);
                int temp = finalChoices[i];
                finalChoices[i] = finalChoices[swapIndex];
                finalChoices[swapIndex] = temp;
            }

            return finalChoices.ToArray();
        }
    }
}
