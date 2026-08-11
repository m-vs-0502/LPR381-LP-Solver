using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LPR_Solver.Data;

namespace LPR_Solver.BusinessLogic
{
    public class FileService
    {
        private readonly FileRepo _fileRepository = new FileRepo();

        public string GetFileContent(string filePath)
        {
            // Business Rule: Ensure file exists before reading
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                throw new FileNotFoundException("The specified file could not be found.");
            }

            return _fileRepository.ReadTextFile(filePath);
        }
    }
}
