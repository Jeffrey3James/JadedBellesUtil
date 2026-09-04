using System.Collections.Generic;
using UnityEngine;

namespace JadedBelles.Util.RandomUtil
{
    /// <summary>
    /// Dice-rolling helpers built on top of <see cref="UnityEngine.Random"/>. Lives in
    /// <c>JadedBelles.Util.RandomUtil</c> rather than <c>JadedBelles.Util.Random</c> so it doesn't
    /// clash with <see cref="UnityEngine.Random"/> at usage sites.
    /// </summary>
    public static class Dice
    {
        /// <summary>Rolls a single N-sided die and returns a value in [1, sides].</summary>
        public static int RollDice(int sides)
        {
            int diceRoll = UnityEngine.Random.Range(1, sides + 1);
            return diceRoll;
        }

        /// <summary>Rolls <paramref name="numberOfDice"/> N-sided dice and returns each result.</summary>
        public static List<int> RollMultipleDiceOfSameType(int numberOfDice, int sides)
        {
            List<int> rolls = new List<int>();

            for (int i = 0; i < numberOfDice; i++)
            {
                int diceRoll = RollDice(sides);
                rolls.Add(diceRoll);
                Debug.Log($"Rolled a {sides}-sided dice: {diceRoll}");
            }

            return rolls;
        }

        /// <summary>Rolls <paramref name="numberOfDice"/> N-sided dice and returns the sum.</summary>
        public static int AddAllDiceOfSameType(int numberOfDice, int sides)
        {
            var rolls = RollMultipleDiceOfSameType(numberOfDice, sides);
            int sumOfDice = 0;

            foreach (int roll in rolls)
            {
                sumOfDice += roll;
            }

            Debug.Log($"Sum of all rolled {sides}-sided dice: {sumOfDice}");
            return sumOfDice;
        }

        /// <summary>Rolls several dice pools of different sizes and returns every result concatenated.</summary>
        public static List<int> RollMultipleTypesOfDifferentDice(List<(int numberOfDice, int sides)> diceConfigs)
        {
            List<int> allRolls = new List<int>();

            foreach (var (numberOfDice, sides) in diceConfigs)
            {
                allRolls.AddRange(RollMultipleDiceOfSameType(numberOfDice, sides));
            }

            return allRolls;
        }

        /// <summary>Rolls several dice pools of different sizes and returns their combined sum.</summary>
        public static int AddAllDiceOfDifferentTypes(List<(int numberOfDice, int sides)> diceConfigs, List<int> rolls)
        {
            int totalSum = 0;
            foreach (var (numberOfDice, sides) in diceConfigs)
            {
                totalSum += AddAllDiceOfSameType(numberOfDice, sides);
            }
            Debug.Log($"Total sum of all rolled dice: {totalSum}");
            return totalSum;
        }
    }
}
