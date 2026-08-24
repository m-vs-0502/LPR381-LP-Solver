using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace LPR_Solver.BusinessLogic
{
    public static class Simplex
    {
        public static (List<string>, List<List<double>>) SimplexSolver(List<string> modelData, string inputPath, bool validateSigns)
        {
            string outputPath = OutputBuilder.CreateTextFileOutput(inputPath);//makes output file
            var (matrixList, colHeads, rowHeads, varSigns, objFunc) = ConvertStandardForm.ConvertMatrix(modelData);//Gets usable matrix and headers

            bool validVarSigns = InputValidation.ValidateLP(varSigns);
            if (!validVarSigns && validateSigns)// ValidateSigns is used when only primal simplex is selected, thus we set it false when usging simplex for parts of other algorithms
            {
                return (new List<string> { "Invalid variable signs." }, null);//Invalid signs when we are using Primal simplex
            }

            var (pages, finalTableau, _) = SolveTable(matrixList, colHeads, rowHeads, objFunc, outputPath);
            return (pages, finalTableau);
        }

        public static (List<string> outputPages, List<List<double>> finalTableau, List<string> finalRowHeaders) SolveTable(
            List<List<double>> matrixList,
            List<string> colHeaders,
            List<string> rowHeaders,
            string objFunc = "max",//default value incase none is given
            string outputPath = null,
            bool showDialogs = true)//when false, suppress popups/success dialog (used by internal sensitivity solves)
        {
            List<string> outputPages = new List<string>();//output list of tables
            List<string> rowHeads = rowHeaders.Select(r => r.Trim()).ToList();//selects rowheaders with extra redundancy incase whitespace is before or after, preventing output formatting problems
            List<string> cols = colHeaders.Select(c => c.Trim()).ToList();// Same as above

            if (cols.Count > matrixList[0].Count)//makes sure that column headers = number of rows
            {
                cols.RemoveAt(0);
            }

            int tableNum = 1; //used for counting table iterations (T1,T2,T3..)
            int rhsCol = matrixList[0].Count - 1;//sets int value to last column index for reuse

            while (true)
            {
                // Print current table
                string tableTag = "T" + tableNum;//Table numbering output
                string matrixText = tableTag.PadLeft(7) + "\t";//Adds tabletag to the string output

                foreach (string col in cols)
                {
                    matrixText += col.PadLeft(7) + "\t";//Adds column headers in same line as tabletag
                }

                for (int i = 0; i < matrixList.Count; i++)
                {
                    matrixText += "\n";//newline
                    string rowLabel = (i < rowHeads.Count) ? rowHeads[i] : $"r{i}";//adds rowheader or default header if one does not exist
                    matrixText += rowLabel.PadLeft(7) + "\t";//adds to string output 

                    foreach (double val in matrixList[i])
                    {
                        matrixText += val.ToString("0.##").PadLeft(7) + "\t";//adds each value rounded to two optional decimals (we do not round for calculation, just output)
                    }
                }
                matrixText += "\n\n";// adds extra space between tables in output file

                outputPages.Add(matrixText);//adds table result to string output text file
                if (!string.IsNullOrEmpty(outputPath))//check if outputpath has value
                {
                    File.AppendAllText(outputPath, matrixText);//adds text
                }

                tableNum++;//increment table count

               //dual simplex functionality
                int dualPivotRow = -1;//starting value outside of existing row index to notice changes
                double minRhs = -1e-9;//sets a very small negative value

                for (int i = 1; i < matrixList.Count; i++)//for every rhs value except z-row
                {
                    if (matrixList[i][rhsCol] < minRhs)//Here we basically check for negative values to
                                                       //decide if we must do dual pivot or not. we could set
                                                       //minRhs to zero, however this value -1e-9 prevents very
                                                       //small negative floating points which we would consider
                                                       //to be zero to cause a dual pivot.
                    {
                        minRhs = matrixList[i][rhsCol];//replace minimun with most negative value to become pivot row
                        dualPivotRow = i;//pivot row index
                    }
                }

                if (dualPivotRow != -1)//if negative rhs value exists
                {
                    // Dual Pivot Column Selection (Ratio Test)
                    int dualPivotCol = -1;//again start value to see changes
                    double minRatio = double.MaxValue;//start with biggest double value to allow smaller positive values to replace it

                    for (int j = 0; j < rhsCol; j++) //checks each column up until before rhs col
                    {
                        double rowVal = matrixList[dualPivotRow][j];//sets value to pivot row value in j column
                        if (rowVal < -1e-9) //again check if acceptable negative value exist
                        {
                            double zVal = matrixList[0][j];//sets z row value  
                            double ratio = Math.Abs(zVal / rowVal);//get absolute ratio 
                            if (ratio < minRatio)//finds smallest positive
                            {
                                minRatio = ratio;//sets new smallest
                                dualPivotCol = j;//sets pivot column index
                            }
                        }
                    }

                    if (dualPivotCol == -1)
                    {
                        // Infeasible since rhs neg with no neg coefficients in row

                        if (showDialogs) MessageBox.Show("Infeasible Dual simplex problem");
                        return (outputPages, null, rowHeads);

                    }

                    // Pivot Dual Simplex
                    rowHeads[dualPivotRow] = cols[dualPivotCol];//sets row header to varibale that gets pivoted
                    matrixList = Pivot(matrixList, dualPivotRow, dualPivotCol);//performs pivot with given pivot row and column
                    continue; //Breaks loop iteration since dual pivot was completed, so next pivot would only happen after new iteration.
                }

                // primal simplex functionality 
                double bestZVal = 0.0;//stores optimal z
                int pivotCol = -1;//same strat as above, set pivotcol out of index for conditional checks later

                for (int j = 0; j < rhsCol; j++)
                {
                    double zCoeff = matrixList[0][j];// sets value 

                    if (objFunc.Equals("max", StringComparison.OrdinalIgnoreCase))//checks if model is max or not (ignores case)
                    {
                        if (zCoeff < -1e-9 && zCoeff < bestZVal)//looks for negative value that is also less than existing (0.0 at start so any neg replaces)
                        {
                            bestZVal = zCoeff;//set new biggest negative
                            pivotCol = j;//set pivot column index
                        }
                    }
                    else
                    {
                        if (zCoeff > 1e-9 && zCoeff > bestZVal)//looks for biggest positive value to replace if bigger than current
                        {
                            bestZVal = zCoeff;//sets biggest pos
                            pivotCol = j;//sets pivot column index
                        }
                    }
                }

                if (pivotCol == -1)
                {
                    // Optimal solution reached
                    break;//break while(true) loop and gives output
                }

                // Primal Pivot Row Selection
                double smallestRT = double.MaxValue;//same as dual, sets max positive value to be replaced to find the min pos value
                int pivotRow = -1;//index out of bound for later comparison

                for (int i = 1; i < matrixList.Count; i++)//each row except z row
                {
                    double pivotColEntry = matrixList[i][pivotCol];//pivot column value in row
                    double rhsVal = matrixList[i][rhsCol];//rhs value in row

                    if (pivotColEntry > 1e-9) //if value acceptable positive value
                    {
                        double rt = rhsVal / pivotColEntry; //do calculation
                        if (rt >= 0 && rt < smallestRT)
                        {
                            smallestRT = rt;//set new smallest rt value
                            pivotRow = i;//pivot row index
                        }
                    }
                }

                if (pivotRow == -1)
                {
                    // Unbounded
                    if (showDialogs) MessageBox.Show("Primal Simplex problem is unbounded");
                    return (outputPages, null, rowHeads);
                }

                // Pivot Primal Simplex
                rowHeads[pivotRow] = cols[pivotCol];//set row header to pivoted variable
                matrixList = Pivot(matrixList, pivotRow, pivotCol);//perform pivot with given row and col
            }


            if (showDialogs) MessageBox.Show("Succesfully completed solution. Find output file at:\n" + outputPath);
            return (outputPages, matrixList, rowHeads);
        }

        private static List<List<double>> Pivot(List<List<double>> matrix, int pivotRow, int pivotCol)//pivoting function
        {
            List<List<double>> nextMatrix = matrix.Select(r => r.ToList()).ToList();//copies matrix for iteration
            double pivotElement = matrix[pivotRow][pivotCol];//intersection for pivot row calculation

            for (int k = 0; k < matrix[pivotRow].Count; k++)// up to rhs values
            {
                nextMatrix[pivotRow][k] = matrix[pivotRow][k] / pivotElement;//sets new table pivot row values
            }

            for (int i = 0; i < matrix.Count; i++)//each row
            {
                if (i == pivotRow) continue;//skips pivot row
                double factor = matrix[i][pivotCol];// finds pivot col value in current row
                for (int j = 0; j < matrix[i].Count; j++)//each column
                {
                    nextMatrix[i][j] = matrix[i][j] - (factor * nextMatrix[pivotRow][j]);//calculation of cell value
                }
            }

            return nextMatrix;//return itteration
        }
    }
}