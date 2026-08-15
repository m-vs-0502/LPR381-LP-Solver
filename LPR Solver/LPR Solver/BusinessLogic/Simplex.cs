using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;

namespace LPR_Solver.BusinessLogic
{
    public static class Simplex
    {
        public static string SimplexSolver(string modelData, string inputPath) {

            string outputPath = OutputBuilder.CreateTextFileOutput(inputPath);//makes output text file
            //max + 2 + 3 + 3 + 5 + 2 + 4
            //+ 11 + 8 + 6 + 14 + 10 + 10 <= 40
            //bin bin bin bin bin bin


            List<List<double>> matrixList = new List<List<double>>(); //make 2D list
            string[] lines = modelData.Split('\n'); //split into seperate lines
            lines[0] += " 0";//ads 0 to end of first line for even line length and ths values.
            int numConstraints = lines.Length-2;//number of constraints
            string objFunc = null;//used to store min/max

            

            //populate matrix

            for (int i = 0; i < lines.Length - 1; i++)//loop trough lines
            {
                matrixList.Add(new List<double>());//add new row
                string[] tempLine = null;
                if (i == 0)//checks for first line
                {
                    objFunc = lines[i].Substring(0, 3); //min or max
                    tempLine = lines[i].Substring(4).Split(' '); //temporary array with line values. Skips min/max
                    
                }
                else
                {
                    tempLine = lines[i].Split(' ');
                }
                //MessageBox.Show(tempLine.Length.ToString()); //**Testing purposes
                for (int j = 0; j < tempLine.Length; j++) //loop through values in each line
                {
                    //MessageBox.Show(tempLine[j]); //**Testing purposes
                    if (j == tempLine.Length-1) {continue;  }// skip <= sign
                    matrixList[i].Add(Double.Parse(tempLine[j]));//adds lhs values to 2D list row
                    if (i == 0) { matrixList[i][j] *= -1; };//makes obj function row negative
                }
                for (int c = 1;c <= numConstraints; c++)
                {
                    matrixList[i].Add(c==i?1:0); //adds 1 where c=i, thus, where slack variables and their constraints match in the table
                }
                matrixList[i].Add(Double.Parse(tempLine[tempLine.Length-1].TrimStart('<','>','=')));//add rhs values



            }
            

            if (objFunc.ToLower() == "max")
            {


                //LP iteration loop

                List<List<double>> matrixListIterate = matrixList
                        .Select(row => row.ToList())
                        .ToList();


                while (true)
                {
                    string matrix = null;
                    foreach (List<double> row in matrixList)
                    {

                        foreach (double colValue in row)
                        {
                            matrix += colValue + "\t|";
                        }
                        matrix += "\n" + new string('_', 50) + "\n";

                    }
                    ;
                    matrix += "\n";

                    File.AppendAllText(outputPath, matrix);
                    MessageBox.Show(matrix);


                    double mostNegValue = 0;
                    double smallestRT = double.MaxValue;
                    int pivotCol = -1;
                    int pivotRow = -1;  

                    for (int i = 0; i < matrixList[0].Count(); i++)//loop through objective function to get pivot column
                    {
                        if (matrixList[0][i] < 0 && matrixList[0][i] < mostNegValue)
                        {
                            mostNegValue = matrixList[0][i];
                            pivotCol = i;
                        }
                    }


                    if (pivotCol == -1)//check if optimal
                    {
                        break;
                    }


                    for (int j = 1; j < matrixList.Count; j++)// loop through rows to find pivot row
                    {
                        double rt = matrixList[j][matrixList[j].Count() - 1] / matrixList[j][pivotCol];//gets the rt value: rhs/pivot column

                        if (rt > 0 && rt < smallestRT)
                        {
                            pivotRow = j;
                        }
                    }
                   
                    for (int k =0; k < matrixList[pivotRow].Count; k++)
                    {
                        matrixListIterate[pivotRow][k] = matrixList[pivotRow][k] / matrixList[pivotRow][pivotCol];// does pivot row calculation first
                    }
                    for (int i = 0;  i < matrixList.Count;i++)
                    {
                        if (i == pivotRow) {  continue; }//skip pivot row
                        for (int j = 0;j < matrixList[i].Count; j++)
                        {
                            matrixListIterate[i][j] = matrixList[i][j] - (matrixList[i][pivotCol] * matrixListIterate[pivotRow][j]);
                        }
                    }

                     matrixList = matrixListIterate
                        .Select(row => row.ToList())
                        .ToList();

                }



                return outputPath;
            }
            else
            { }








            return null;
             }
    }
}
