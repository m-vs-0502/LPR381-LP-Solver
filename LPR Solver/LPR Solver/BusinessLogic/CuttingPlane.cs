using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LPR_Solver.BusinessLogic
{
    public static class CuttingPlane
    {
        // safety net so we don't loop forever if a model just refuses to go integer
        private const int MaxCuts = 50;

        // entry point, same vibe as the other Solver methods
        public static List<string> CuttingPlaneSolver(List<string> modelData, string inputPath)
        {
            string outputPath = OutputBuilder.CreateTextFileOutput(inputPath);
            var (matrixList, colHeads, rowHeads, varSigns, objFunc) = ConvertStandardForm.ConvertMatrix(modelData);

            if (colHeads.Count > matrixList[0].Count) // ditch the leading "T-i" header, don't need it
            {
                colHeads.RemoveAt(0);
            }

            // bin = 0 or 1, not just "any integer" . The ConvertMatrix doesn't add that upper
            // bound on its own, so we bolt on a x_j <= 1 row for every bin var before we startsolving
            AddBinaryUpperBounds(matrixList, colHeads, rowHeads, varSigns);

            return SolveCuttingPlane(matrixList, colHeads, rowHeads, varSigns, objFunc, outputPath);
        }

        // adds one x_j <= 1 constraint row per bin-restricted variable, straight into the
        // starting tableau, so bin vars are actually capped at 1 and not just forced integer
        private static void AddBinaryUpperBounds(
            List<List<double>> matrixList,
            List<string> colHeads,
            List<string> rowHeads,
            List<string> varSigns)
        {
            for (int varIdx = 0; varIdx < varSigns.Count && varIdx < colHeads.Count - 1; varIdx++)
            {
                if (!varSigns[varIdx].Trim().Equals("bin", StringComparison.OrdinalIgnoreCase)) continue; // only bin vars need this

                int newRhsIndex = colHeads.Count - 1; // recompute every loop, RHS keeps shifting right as we insert
                string newSlackName = "s" + rowHeads.Count; // sequential, matches the naming used everywhere else
                colHeads.Insert(newRhsIndex, newSlackName);

                for (int r = 0; r < matrixList.Count; r++)
                {
                    matrixList[r].Insert(newRhsIndex, 0.0); // every existing row gets a 0 in the new slack column
                }

                int newRhsCol = matrixList[0].Count - 1;
                List<double> newRow = new List<double>(new double[colHeads.Count]);
                newRow[varIdx] = 1.0;       // x_j
                newRow[newRhsIndex] = 1.0;  // + slack
                newRow[newRhsCol] = 1.0;    // = 1  ->  x_j <= 1

                matrixList.Add(newRow);
                rowHeads.Add(newSlackName);
            }
        }

        // solve LP -> check if it's integer yet -> if not, slice off the fraction and go again
        public static List<string> SolveCuttingPlane(
            List<List<double>> initialTable,
            List<string> colHeaders,
            List<string> rowHeaders,
            List<string> varSigns,
            string objFunc,
            string outputPath)
        {
            List<string> outputPages = new List<string>();

            // quick lookup so we know which vars actually need to be whole numbers
            Dictionary<string, string> varTypeMap = new Dictionary<string, string>();
            for (int i = 0; i < varSigns.Count && i < colHeaders.Count; i++)
            {
                varTypeMap[colHeaders[i].Trim()] = varSigns[i].Trim().ToLower();
            }

            List<string> colHeads = new List<string>(colHeaders);
            List<string> rowHeads = new List<string>(rowHeaders);

            // step 1, just solve the plain LP relaxation first, integer rules ignored for now
            var (lpOutputPages, solvedTable, solvedRowHeads) = Simplex.SolveTable(
                CloneMatrix(initialTable),
                new List<string>(colHeads),
                new List<string>(rowHeads),
                objFunc,
                outputPath
            );

            if (solvedTable == null) // relaxed LP itself is broken, nowhere to go from here
            {
                string msg = "Initial LP Relaxation is Infeasible or Unbounded.";
                outputPages.Add(msg);
                File.AppendAllText(outputPath, msg + "\n");
                return outputPages;
            }

            string initialPage = "       Relaxed LP Solution   \n";
            initialPage += string.Join("\n", lpOutputPages);
            outputPages.Add(initialPage);

            List<List<double>> tableau = solvedTable;
            rowHeads = solvedRowHeads;

            int cutNumber = 1;

            while (cutNumber <= MaxCuts)
            {
                var (decisionVars, slackVars) = GetCurrentSolution(tableau, rowHeads, colHeads);

                // hunt for the most fractional int/bin var, that's our cut target
                int sourceRow = -1;
                double largestFraction = 0.0;
                int rhsColCheck = tableau[0].Count - 1;

                for (int i = 1; i < tableau.Count; i++) // row 0 is the z row, skip it
                {
                    string basicVar = (i < rowHeads.Count) ? rowHeads[i].Trim() : "";
                    string vType = varTypeMap.ContainsKey(basicVar) ? varTypeMap[basicVar] : "pos";

                    if (vType != "int" && vType != "bin") continue; // continuous vars get a pass

                    double val = tableau[i][rhsColCheck];
                    double frac = val - Math.Floor(val);
                    double fractionality = Math.Min(frac, 1 - frac); // basically "how not-whole is this"

                    if (fractionality > 0.0001 && fractionality > largestFraction)
                    {
                        largestFraction = fractionality;
                        sourceRow = i;
                    }
                }

                if (sourceRow == -1) break; // nothing fractional left, we're done here

                int rhsCol = tableau[0].Count - 1;
                double sourceRhs = tableau[sourceRow][rhsCol];
                double rhsFrac = sourceRhs - Math.Floor(sourceRhs);

                string cutInfoPage = $"Cut Number: {cutNumber}\n";
                cutInfoPage += $"Source Row: {rowHeads[sourceRow]} (Value = {sourceRhs:0.##})\n\n";
                cutInfoPage += FormatVariablePairs(decisionVars, slackVars);
                outputPages.Add(cutInfoPage);
                File.AppendAllText(outputPath, cutInfoPage + "\n");

                // make room for a new slack column, right before RHS
                int newRhsIndex = colHeads.Count - 1;
                string newSlackName = "s" + rowHeads.Count;
                colHeads.Insert(newRhsIndex, newSlackName);

                for (int r = 0; r < tableau.Count; r++)
                {
                    tableau[r].Insert(newRhsIndex, 0.0); // everyone else gets a 0 here, only the new row cares
                }

                int newRhsCol = tableau[0].Count - 1;

                // this is the actual Gomory cut: negative frac coefficients + a slack, negative RHS on purpose
                // that negative RHS is the trick, it makes SolveTable dual-pivot for us automatically
                List<double> newRow = new List<double>(new double[colHeads.Count]);
                for (int j = 0; j < newRhsIndex; j++)
                {
                    double coeff = tableau[sourceRow][j];
                    double coeffFrac = coeff - Math.Floor(coeff);
                    newRow[j] = -coeffFrac;
                }
                newRow[newRhsIndex] = 1.0;
                newRow[newRhsCol] = -rhsFrac;

                tableau.Add(newRow);
                rowHeads.Add(newSlackName);

                // re-solve with the cut baked in
                var (cutOutputPages, newSolvedTable, newRowHeads) = Simplex.SolveTable(
                    CloneMatrix(tableau),
                    new List<string>(colHeads),
                    new List<string>(rowHeads),
                    objFunc,
                    outputPath
                );

                if (newSolvedTable == null) // shouldn't really happen with a valid cut, but just in case
                {
                    string infeasiblePage = "STATUS: Infeasible after adding cutting plane.\n";
                    outputPages.Add(infeasiblePage);
                    File.AppendAllText(outputPath, infeasiblePage + "\n");
                    return outputPages;
                }

                string iterationPage = string.Join("\n", cutOutputPages);
                outputPages.Add(iterationPage);

                tableau = newSolvedTable;
                rowHeads = newRowHeads;

                cutNumber++;
            }

            // wrap it up
            var (finalDecisionVars, finalSlackVars) = GetCurrentSolution(tableau, rowHeads, colHeads);
            int finalRhsCol = tableau[0].Count - 1;
            double finalZ = tableau[0][finalRhsCol];

            string finalPage = "     Optimal Cutting Plane Solution    \n";

            // only int/bin vars actually needed to be whole numbers, don't say "integer solution"
            // if nothing in the model was ever restricted that way
            bool anyIntRestricted = varTypeMap.Values.Any(v => v == "int" || v == "bin");

            if (cutNumber > MaxCuts)
            {
                finalPage += "STATUS: Maximum cut iterations reached without a fully integer solution.\n";
            }
            else if (anyIntRestricted)
            {
                finalPage += "STATUS: Optimal Integer Solution Found\n";
            }
            else
            {
                finalPage += "STATUS: Optimal Solution Found (no integer restrictions on this model)\n";
            }

            finalPage += $"OPTIMAL Z: {finalZ:0.##}\n\n";
            finalPage += "DECISION VARIABLE ASSIGNMENTS:\n";
            foreach (var kvp in finalDecisionVars.OrderBy(k => k.Key))
            {
                // only round vars that were actually required to be int/bin, leave continuous ones as solved
                string vType = varTypeMap.ContainsKey(kvp.Key) ? varTypeMap[kvp.Key] : "pos";
                bool isIntRestricted = vType == "int" || vType == "bin";
                string displayValue = isIntRestricted ? Math.Round(kvp.Value).ToString("0.##") : kvp.Value.ToString("0.##");
                finalPage += $"{kvp.Key} = {displayValue}\n";
            }

            outputPages.Add(finalPage);
            File.AppendAllText(outputPath, finalPage + "\n");

            return outputPages;
        }

        // pulls x/s values out of the tableau based on who's basic in each row
        private static (Dictionary<string, double> decisionVars, Dictionary<string, double> slackVars) GetCurrentSolution(
            List<List<double>> tableau,
            List<string> rowHeaders,
            List<string> colHeaders)
        {
            var decisionVars = new Dictionary<string, double>();
            var slackVars = new Dictionary<string, double>();
            int rhsCol = tableau[0].Count - 1;

            foreach (string col in colHeaders) // everything starts at 0, non-basic vars just stay that way
            {
                string cleanCol = col.Trim();
                if (cleanCol.StartsWith("x", StringComparison.OrdinalIgnoreCase)) decisionVars[cleanCol] = 0.0;
                else if (cleanCol.StartsWith("s", StringComparison.OrdinalIgnoreCase)) slackVars[cleanCol] = 0.0;
            }

            for (int i = 1; i < tableau.Count; i++)
            {
                string basicVar = (i < rowHeaders.Count) ? rowHeaders[i].Trim() : "";
                if (decisionVars.ContainsKey(basicVar)) decisionVars[basicVar] = tableau[i][rhsCol];
                else if (slackVars.ContainsKey(basicVar)) slackVars[basicVar] = tableau[i][rhsCol];
            }

            return (decisionVars, slackVars);
        }

        // just makes the variable dump look decent in the output
        private static string FormatVariablePairs(Dictionary<string, double> decisionVars, Dictionary<string, double> slackVars)
        {
            string result = "Decision Variables:\n";
            foreach (var kvp in decisionVars.OrderBy(k => k.Key))
            {
                result += $"  {kvp.Key} = {kvp.Value:0.##}\n";
            }

            result += "\nSlack Variables:\n";
            foreach (var kvp in slackVars.OrderBy(k => k.Key))
            {
                result += $"  {kvp.Key} = {kvp.Value:0.##}\n";
            }

            return result;
        }

        // deep copy so pivoting never messes with the caller's original matrix
        private static List<List<double>> CloneMatrix(List<List<double>> source)
        {
            return source.Select(r => new List<double>(r)).ToList();
        }
    }
}
















