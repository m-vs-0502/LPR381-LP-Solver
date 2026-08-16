using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LPR_Solver.BusinessLogic
{
    internal static class OutputBuilder
    {
        internal static string CreateTextFileOutput(string inputPath)
        {
           
            string directory = Path.GetDirectoryName(inputPath);

            string outputPath = Path.Combine(directory ?? string.Empty, "lp_output.txt");

            File.Create(outputPath).Close();
            return outputPath;
           
        }


    }
}
