using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LPR_Solver.BusinessLogic
{
    internal static class ConvertStandardForm
    {

        internal static (List<List<double>>,List<string>,List<string>,List<string>, string) ConvertMatrix (List<string> modelData)
        {
            List<List<double>> matrixList = new List<List<double>>(); //make 2D list matrix
            List<string> rowHeads = new List<string>();//List for row headings
            List<string> colHeads = new List<string>();//list for column headings
            colHeads.Add("T-i");
            string[] lines = modelData[0].Split('\n'); //split into seperate lines

            List<string> varSigns = lines[lines.Length - 1].Split(' ').ToList();//get last line split into each variable sign (pos/bin/int)

            lines[0] += " 0";//ads 0 to end of first line for even line length and rhs values.
            int numConstraints = lines.Length - 2;//number of constraints
            string objFunc = null;//used to store min/max



            //populate matrix

            for (int i = 0; i < lines.Length-1; i++)//loop trough lines
            {
                matrixList.Add(new List<double>());//add new row
                string[] tempLine = null;
                if (i == 0)//checks for first line
                {
                    objFunc = lines[i].Split(' ')[0]; //min or max
                    tempLine = lines[i].Split(' ').Skip(1).ToArray(); //temporary array with line values. Skips min/max
                    rowHeads.Add("z");//add z to z row header
                }
                else
                {
                    tempLine = lines[i].Split(' ');
                    rowHeads.Add(i.ToString());//adds constraint number to row header
                }
                //MessageBox.Show(tempLine.Length.ToString()); //**Testing purposes
                for (int j = 0; j < tempLine.Length; j++) //loop through values in each line
                {
                    //MessageBox.Show(tempLine[j]); //**Testing purposes
                    if (j == tempLine.Length - 1) { continue; }// skip <= sign
                    matrixList[i].Add(Double.Parse(tempLine[j]));//adds lhs values to 2D list row
                    if (i == 0) { matrixList[i][j] *= -1; colHeads.Add("x" + (j+1)); }
                    ;//makes obj function row negative
                }
                for (int c = 1; c <= numConstraints; c++)
                {
                    if (i == 0) { colHeads.Add("s" + c); }
                    matrixList[i].Add(c == i ? 1 : 0); //adds 1 where c=i, thus, where slack variables and their constraints match in the table
                }
                matrixList[i].Add(Double.Parse(tempLine[tempLine.Length - 1].TrimStart('<', '>', '=')));//add rhs values
                



            }
            colHeads.Add("RHS");



            return (matrixList,colHeads,rowHeads,varSigns, objFunc);
        }
        

    }
}
