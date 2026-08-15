using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace LPR_Solver.Data
{
    public class FileRepo
    {
        public string ReadTextFile(string filePath)
        {
            // read whole text file into string
            return File.ReadAllText(filePath);
        }
    }
}
