using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LPR_Solver.BusinessLogic
{
    public static class InputValidation
    {

        public static bool ValidateForm(string input)
        {

            if (string.IsNullOrEmpty(input)) return false;
            if (input.Split('\n').Length <= 2) { return false; }



            return true;
        }

        internal static bool ValidateLP(List<string> varSigns)
        {
            foreach (string sign in varSigns)
            {
                if (sign != "pos")
                {
                    return false;
                }

            }

            return true;
        }

        internal static bool ValidateBinIP(List<string> varSigns)
        {
            foreach (string sign in varSigns)
            {
                if (sign != "bin")
                {
                    return false;
                }

            }

            return true;
        }
        internal static bool ValidateIP(List<string> varSigns)
        {
            foreach (string sign in varSigns)
            {
                if (sign != "int")
                {
                    return false;
                }

            }

            return true;
        }
    }
}
