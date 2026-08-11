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
            // Disk I/O operation
            return File.ReadAllText(filePath);
        }
    }
}
