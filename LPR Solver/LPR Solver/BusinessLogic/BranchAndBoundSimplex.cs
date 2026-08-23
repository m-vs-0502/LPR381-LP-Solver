using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LPR_Solver.BusinessLogic
{
    public static class BranchAndBound
    {
        public static List<string> BaBSimplexSolver(List<string> modelData, string inputPath)
        {
            string outputPath = OutputBuilder.CreateTextFileOutput(inputPath);
            var (matrixList, colHeads, rowHeads, varSigns, objFunc) = ConvertStandardForm.ConvertMatrix(modelData);

            if (colHeads.Count > matrixList[0].Count)
            {
                colHeads.RemoveAt(0);
            }

            return SolveBranchAndBound(matrixList, colHeads, rowHeads, varSigns, objFunc, outputPath);
        }

        private class BBNode
        {
            public List<List<double>> Tableau { get; set; }
            public List<string> ColHeaders { get; set; }
            public List<string> RowHeaders { get; set; }
            public string SplitNumber { get; set; }
        }

        public static List<string> SolveBranchAndBound(
            List<List<double>> initialTable,
            List<string> colHeaders,
            List<string> rowHeaders,
            List<string> varSigns,
            string objFunc,
            string outputPath)
        {
            List<string> outputPages = new List<string>();

            Dictionary<string, string> varTypeMap = new Dictionary<string, string>();
            for (int i = 0; i < varSigns.Count && i < colHeaders.Count; i++)
            {
                varTypeMap[colHeaders[i].Trim()] = varSigns[i].Trim().ToLower();
            }

            // solve relaxed lp
            var (lpOutputPages, solvedLpTable, _) = Simplex.SolveTable(
                CloneMatrix(initialTable),
                new List<string>(colHeaders),
                new List<string>(rowHeaders),
                objFunc,
                outputPath
            );

            if (solvedLpTable == null)
            {
                outputPages.Add("Initial LP Relaxation is Infeasible.");
                return outputPages;
            }

            string initialPage = "       Relaxed LP Solution   \n";
            initialPage += string.Join("\n", lpOutputPages);
            outputPages.Add(initialPage);

            // BaB stack for branch splitting
            Stack<BBNode> stack = new Stack<BBNode>();
            stack.Push(new BBNode //adds new B&BNode
            {
                Tableau = CloneMatrix(initialTable),
                ColHeaders = new List<string>(colHeaders),
                RowHeaders = new List<string>(rowHeaders),
                SplitNumber = "1" 
            });

            double bestZ = objFunc.Equals("max", StringComparison.OrdinalIgnoreCase) ? double.MinValue : double.MaxValue;
            Dictionary<string, double> bestSolution = null;

            while (stack.Count > 0)
            {
                BBNode currentNode = stack.Pop();

                var (_, solvedTable, solvedRowHeaders) = Simplex.SolveTable(
                    currentNode.Tableau,
                    currentNode.ColHeaders,
                    currentNode.RowHeaders,
                    objFunc,
                    null
                );

                string pageText = $"Split Number: {currentNode.SplitNumber}\n";

                if (solvedTable == null)
                {
                    pageText += "STATUS: Infeasible\n";
                    outputPages.Add(pageText);
                    File.AppendAllText(outputPath, pageText + "\n");
                    continue;
                }

                int rhsCol = solvedTable[0].Count - 1;
                double currentZ = solvedTable[0][rhsCol];
                var (decisionVars, slackVars) = GetCurrentSolution(solvedTable, solvedRowHeaders, currentNode.ColHeaders);

                bool isMax = objFunc.Equals("max", StringComparison.OrdinalIgnoreCase);
                bool canPruneByBound = isMax ? (currentZ <= bestZ) : (currentZ >= bestZ);

                if (canPruneByBound && bestSolution != null)
                {
                    pageText += $"STATUS: Pruned by Bound (Z = {currentZ:0.##})\n\n";
                    pageText += FormatVariablePairs(decisionVars, slackVars);
                    outputPages.Add(pageText);
                    File.AppendAllText(outputPath, pageText + "\n");
                    continue;
                }

                string fractionalVar = null;
                double fractionalVal = 0.0;

                foreach (var kvp in decisionVars)
                {
                    string vName = kvp.Key;
                    double vVal = kvp.Value;
                    string vType = varTypeMap.ContainsKey(vName) ? varTypeMap[vName] : "pos";

                    if (vType == "int" || vType == "bin")
                    {
                        if (Math.Abs(vVal - Math.Round(vVal)) > 0.0001)
                        {
                            fractionalVar = vName;
                            fractionalVal = vVal;
                            break;
                        }
                    }
                }

                if (fractionalVar == null)
                {
                    bool isNewBest = isMax ? (currentZ > bestZ) : (currentZ < bestZ);
                    if (isNewBest)
                    {
                        bestZ = currentZ;
                        bestSolution = decisionVars;
                        pageText += $"STATUS: Optimal Integer Candidate (Z = {currentZ:0.##})\n\n";
                    }
                    else
                    {
                        pageText += $"STATUS: Feasible Integer Solution (Z = {currentZ:0.##})\n\n";
                    }

                    pageText += FormatVariablePairs(decisionVars, slackVars);
                    outputPages.Add(pageText);
                    File.AppendAllText(outputPath, pageText + "\n");
                    continue;
                }

                double floorVal = Math.Floor(fractionalVal);
                double ceilVal = Math.Ceiling(fractionalVal);

                pageText += $"STATUS: Fractional - Branching on {fractionalVar} = {fractionalVal:0.##} (Z = {currentZ:0.##})\n\n";
                pageText += FormatVariablePairs(decisionVars, slackVars);
                outputPages.Add(pageText);
                File.AppendAllText(outputPath, pageText + "\n");

                // Right Branch: x_j >= ceilVal (Suffix '2')
                BBNode rightChild = CloneNode(currentNode);
                rightChild.SplitNumber += "2";
                AddGreaterOrEqualConstraint(rightChild, fractionalVar, ceilVal);
                stack.Push(rightChild);

                // Left Branch: x_j <= floorVal (Suffix '1')
                BBNode leftChild = CloneNode(currentNode);
                leftChild.SplitNumber += "1";
                AddLessOrEqualConstraint(leftChild, fractionalVar, floorVal);
                stack.Push(leftChild);
            }

            // Summary page
            string finalPage = "     Optimal Branch & Bound Solution    \n" ;

            if (bestSolution != null)
            {
                finalPage += "STATUS: Optimal Solution Found\n";
                finalPage += $"OPTIMAL Z: {bestZ:0.##}\n\n";
                finalPage += "DECISION VARIABLE ASSIGNMENTS:\n";
                foreach (var kvp in bestSolution.OrderBy(k => k.Key))
                {
                    finalPage += $"{kvp.Key} = {Math.Round(kvp.Value)}\n";
                }
            }
            else
            {
                finalPage += "STATUS: No Feasible Integer Solution Exists.\n";
            }

            outputPages.Add(finalPage);
            File.AppendAllText(outputPath, finalPage + "\n");

            return outputPages;
        }

        private static (Dictionary<string, double> decisionVars, Dictionary<string, double> slackVars) GetCurrentSolution(
            List<List<double>> tableau,
            List<string> rowHeaders,
            List<string> colHeaders)
        {
            var decisionVars = new Dictionary<string, double>();
            var slackVars = new Dictionary<string, double>();
            int rhsCol = tableau[0].Count - 1;

            foreach (string col in colHeaders)
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

        private static void AddLessOrEqualConstraint(BBNode node, string targetVar, double boundValue)
        {
            int targetColIndex = node.ColHeaders.IndexOf(targetVar);
            if (targetColIndex == -1) return;

            int rhsIndex = node.ColHeaders.Count - 1;
            string newSlackName = "s" + node.RowHeaders.Count;

            node.ColHeaders.Insert(rhsIndex, newSlackName);

            for (int i = 0; i < node.Tableau.Count; i++)
            {
                node.Tableau[i].Insert(rhsIndex, 0.0);
            }

            List<double> newRow = new List<double>(new double[node.ColHeaders.Count]);
            newRow[targetColIndex] = 1.0;
            newRow[rhsIndex] = 1.0;
            newRow[node.ColHeaders.Count - 1] = boundValue;

            node.Tableau.Add(newRow);
            node.RowHeaders.Add(newSlackName);
        }

        private static void AddGreaterOrEqualConstraint(BBNode node, string targetVar, double boundValue)
        {
            int targetColIndex = node.ColHeaders.IndexOf(targetVar);
            if (targetColIndex == -1) return;

            int rhsIndex = node.ColHeaders.Count - 1;
            string newSlackName = "s" + node.RowHeaders.Count;

            node.ColHeaders.Insert(rhsIndex, newSlackName);

            for (int i = 0; i < node.Tableau.Count; i++)
            {
                node.Tableau[i].Insert(rhsIndex, 0.0);
            }

            List<double> newRow = new List<double>(new double[node.ColHeaders.Count]);
            newRow[targetColIndex] = -1.0;
            newRow[rhsIndex] = 1.0;
            newRow[node.ColHeaders.Count - 1] = -boundValue;

            node.Tableau.Add(newRow);
            node.RowHeaders.Add(newSlackName);
        }

        private static BBNode CloneNode(BBNode source)
        {
            return new BBNode
            {
                Tableau = CloneMatrix(source.Tableau),
                ColHeaders = new List<string>(source.ColHeaders),
                RowHeaders = new List<string>(source.RowHeaders),
                SplitNumber = source.SplitNumber
            };
        }

        private static List<List<double>> CloneMatrix(List<List<double>> source)
        {
            return source.Select(r => new List<double>(r)).ToList();
        }
    }
}