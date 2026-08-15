using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LPR_Solver.BusinessLogic
{
    public static class InputValidation
    {

        public static bool Validate(string input)
        {

            if (string.IsNullOrEmpty(input)) return false;
            if (input.Split('\n').Length <= 2) {  return false; }



            return true;
        }
    }
}
