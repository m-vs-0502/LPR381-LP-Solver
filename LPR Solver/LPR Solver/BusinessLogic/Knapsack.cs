using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace LPR_Solver.BusinessLogic
{
    public static class Knapsack
    {
        public static List<string> KnapsackSolver(List<string> modelData, string inputPath)
        {
            string outputPath = OutputBuilder.CreateTextFileOutput(inputPath);
            List<string> outputPages = new List<string>();

            // Convert to standard form
            var (matrixList, colHeads, rowHeads, varSigns, objFunc) = ConvertStandardForm.ConvertMatrix(modelData);

            // Check for binary variable signs
            if (!InputValidation.ValidateBinIP(varSigns))
            {
                MessageBox.Show("Error: All variable signs must be \"Bin\" for knapsack.");
                string msg = "Error: All variables in a Knapsack problem must be binary (bin).";
                outputPages.Add(msg);
                if (!string.IsNullOrEmpty(outputPath)) File.AppendAllText(outputPath, msg + "\n");
                return outputPages;
            }

            // Validate that only one constraint exists (weight)
            int numConstraints = matrixList.Count - 1;
            if (numConstraints != 1)
            {
                MessageBox.Show("Error: Only 1 constraint (weight) allowed for knapsack.");
                string msg = $"Error: Knapsack problem expects exactly 1 constraint line, but found {numConstraints}.";
                outputPages.Add(msg);
                if (!string.IsNullOrEmpty(outputPath)) File.AppendAllText(outputPath, msg + "\n");
                return outputPages;
            }

            // Clean headers and determine RHS column
            if (colHeads.Count > matrixList[0].Count)
            {
                colHeads.RemoveAt(0);
            }

            int rhsCol = matrixList[0].Count - 1;

            // Extract genuine decision variables (ignore slacks/surplus 's' and 'e')
            List<int> itemColIndices = new List<int>();
            for (int j = 0; j < rhsCol; j++)
            {
                string header = colHeads[j].Trim();
                if (!header.StartsWith("s", StringComparison.OrdinalIgnoreCase) &&
                    !header.StartsWith("e", StringComparison.OrdinalIgnoreCase))
                {
                    itemColIndices.Add(j);
                }
            }

            int numVars = itemColIndices.Count;
            bool isMax = objFunc.Equals("max", StringComparison.OrdinalIgnoreCase);

            double[] values = new double[numVars];
            int[] weights = new int[numVars];
            List<string> itemNames = new List<string>();

            for (int i = 0; i < numVars; i++)
            {
                int colIdx = itemColIndices[i];
                itemNames.Add(colHeads[colIdx].Trim());
                values[i] = isMax ? Math.Abs(matrixList[0][colIdx]) : matrixList[0][colIdx];
                weights[i] = (int)Math.Round(matrixList[1][colIdx]);
            }

            int capacity = (int)Math.Round(matrixList[1][rhsCol]);

            // Dynamic Programming 0-1 Knapsack Execution
            double[,] dp = new double[numVars + 1, capacity + 1];

            for (int i = 1; i <= numVars; i++)
            {
                int w = weights[i - 1];
                double v = values[i - 1];

                for (int cap = 0; cap <= capacity; cap++)
                {
                    if (w <= cap)
                    {
                        dp[i, cap] = Math.Max(dp[i - 1, cap], dp[i - 1, cap - w] + v);
                    }
                    else
                    {
                        dp[i, cap] = dp[i - 1, cap];
                    }
                }
            }

            double maxProfit = dp[numVars, capacity];

            // Backtracking for selected items
            Dictionary<string, int> selectedVars = new Dictionary<string, int>();
            for (int i = 0; i < numVars; i++)
            {
                selectedVars[itemNames[i]] = 0;
            }

            int currentCap = capacity;
            for (int i = numVars; i > 0 && maxProfit > 0; i--)
            {
                if (Math.Abs(dp[i, currentCap] - dp[i - 1, currentCap]) > 1e-6)
                {
                    selectedVars[itemNames[i - 1]] = 1;
                    currentCap -= weights[i - 1];
                }
            }

            // Build output string format
            string page = "    Knapsack Results:    \n";
            page += $"Capacity (W): {capacity}\n";
            page += $"Number of Items: {numVars}\n\n";
            page += "Item Parameters:\n";

            for (int i = 0; i < numVars; i++)
            {
                page += $"  {itemNames[i]}: Value = {values[i]}, Weight = {weights[i]}\n";
            }

            page += "\n----------------------------------------\n";
            page += "OPTIMAL SOLUTION:\n";
            page += $"Optimal Z Value: {maxProfit}\n\n";
            page += "Decision Variable Assignments:\n";

            foreach (var kvp in selectedVars.OrderBy(k => k.Key))
            {
                page += $"  {kvp.Key} = {kvp.Value}\n";
            }

            int usedWeight = 0;
            for (int i = 0; i < numVars; i++)
            {
                if (selectedVars[itemNames[i]] == 1)
                {
                    usedWeight += weights[i];
                }
            }

            page += $"\nTotal Weight Used: {usedWeight} / {capacity}\n";

            outputPages.Add(page);

            if (!string.IsNullOrEmpty(outputPath))
            {
                File.AppendAllText(outputPath, page + "\n");
            }

            return outputPages;
        }
    }
}