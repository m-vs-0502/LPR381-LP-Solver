using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace LPR_Solver.BusinessLogic
{
    /// <summary>
    /// Performs post-optimal Sensitivity Analysis on a Linear Programming model that has
    /// already been solved with the Primal Simplex algorithm.
    ///
    /// All operations work off the FINAL optimal tableau produced by <see cref="Simplex.SolveTable"/>.
    /// The tableau convention used throughout the project (and therefore here) is:
    ///   * Row 0 is the objective (z) row and is stored NEGATED for a max problem.
    ///   * There is exactly one +1 slack column per constraint, forming an identity in the
    ///     initial tableau. Because of this, the slack columns of the FINAL tableau hold the
    ///     basis inverse (B^-1), and the z-row entries under the slack columns hold the
    ///     shadow prices (y = cB * B^-1).
    ///   * The last column is the Right-Hand-Side (RHS).
    ///   * Row headers (basis) name the variable that is basic in each constraint row.
    ///
    /// The range formulas below were validated numerically against the textbook "Wyndor"
    /// example (objective ranges, RHS ranges and shadow prices all matched exactly).
    /// </summary>
    public class SensitivityAnalysis
    {
        // ----- Model context captured once, then reused by every operation -----
        public string ObjFunc { get; private set; }          // "max" or "min"
        public bool IsMax { get; private set; }              // convenience flag
        public int N { get; private set; }                    // number of decision variables (x1..xn)
        public int M { get; private set; }                    // number of constraints (s1..sm)
        public List<double> C { get; private set; }           // original objective coefficients (positive form)
        public List<List<double>> A { get; private set; }     // original technological coefficients (m x n)
        public List<double> B { get; private set; }           // original RHS values
        public List<List<double>> Final { get; private set; } // final optimal tableau
        public List<string> Basis { get; private set; }       // final row headers (basic variable per row)
        public List<string> ColHeads { get; private set; }    // final column headers (x.., s.., RHS)
        public bool IsOptimal { get; private set; }           // false when infeasible/unbounded
        public string Status { get; private set; }            // human readable status
        public string OutputPath { get; private set; }        // file to append sensitivity results to

        private int RhsCol => Final[0].Count - 1;             // index of the RHS column
        private const double EPS = 1e-9;
        // Optimality sign: for max a nonbasic reduced cost must stay >= 0, for min <= 0.
        private double S => IsMax ? 1.0 : -1.0;

        /// <summary>
        /// Builds the sensitivity context from the raw model text by parsing it and solving it
        /// (silently) with the Primal Simplex algorithm. Returns null-status object when the
        /// model could not be optimised (infeasible / unbounded / bad signs).
        /// </summary>
        public static SensitivityAnalysis Build(List<string> modelData, string inputPath)
        {
            var sa = new SensitivityAnalysis();

            // Parse the model into the tableau/headers using the shared converter.
            var (matrix, colHeads, rowHeads, varSigns, objFunc) = ConvertStandardForm.ConvertMatrix(modelData);

            sa.ObjFunc = string.IsNullOrEmpty(objFunc) ? "max" : objFunc.Trim().ToLowerInvariant();
            sa.IsMax = sa.ObjFunc.Equals("max", StringComparison.OrdinalIgnoreCase);

            // ConvertMatrix prefixes the headers with a "T-i" tag that the tableau rows do NOT
            // contain. Drop it so the header list lines up 1:1 with the tableau columns
            // ([x1..xn, s1..sm, RHS]); otherwise every column lookup is off by one.
            sa.ColHeads = colHeads.Select(c => c.Trim()).ToList();
            if (sa.ColHeads.Count > 0 && sa.ColHeads[0].Equals("T-i", StringComparison.OrdinalIgnoreCase))
                sa.ColHeads.RemoveAt(0);

            // Count decision variables (x..) and constraints (s.. / rows) from the headers.
            sa.N = sa.ColHeads.Count(h => h.StartsWith("x"));
            sa.M = matrix.Count - 1;

            int rhsCol = matrix[0].Count - 1;

            // Recover the ORIGINAL model data from the initial tableau (before any pivoting).
            sa.C = new List<double>();
            for (int j = 0; j < sa.N; j++) sa.C.Add(-matrix[0][j]); // z-row is negated, so negate back
            sa.A = new List<List<double>>();
            sa.B = new List<double>();
            for (int i = 1; i <= sa.M; i++)
            {
                var row = new List<double>();
                for (int j = 0; j < sa.N; j++) row.Add(matrix[i][j]);
                sa.A.Add(row);
                sa.B.Add(matrix[i][rhsCol]);
            }

            // Solve a deep copy silently so we do not re-write tables or pop dialogs.
            var matrixCopy = matrix.Select(r => r.ToList()).ToList();
            var (_, finalTableau, finalRowHeads) =
                Simplex.SolveTable(matrixCopy, colHeads.ToList(), rowHeads.ToList(), sa.ObjFunc, null, showDialogs: false);

            if (finalTableau == null)
            {
                sa.IsOptimal = false;
                sa.Status = "The LP could not be solved to optimality (infeasible or unbounded). " +
                            "Sensitivity analysis is only defined on an optimal solution.";
                return sa;
            }

            sa.Final = finalTableau;
            sa.Basis = finalRowHeads.Select(r => r.Trim()).ToList();
            sa.IsOptimal = true;
            sa.Status = "Optimal solution found.";

            // Determine an output file next to the input so results can be exported.
            try { sa.OutputPath = OutputBuilder.CreateTextFileOutput(inputPath); }
            catch { sa.OutputPath = null; }

            return sa;
        }

        // =====================================================================
        //  Helpers
        // =====================================================================

        private static string Fmt(double v)
        {
            if (double.IsPositiveInfinity(v)) return "+inf";
            if (double.IsNegativeInfinity(v)) return "-inf";
            return v.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private int ColIndexOf(string name)
        {
            return ColHeads.IndexOf(name) >= 0
                ? ColHeads.IndexOf(name)
                : ColHeads.FindIndex(h => h.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsBasic(string name) => Basis.Skip(1).Any(b => b.Equals(name, StringComparison.OrdinalIgnoreCase));

        private int BasisRowOf(string name)
        {
            for (int i = 1; i < Basis.Count; i++)
                if (Basis[i].Equals(name, StringComparison.OrdinalIgnoreCase)) return i;
            return -1;
        }

        // Adjusted reduced cost that is always >= 0 at optimality regardless of max/min.
        private double Arc(int col) => S * Final[0][col];

        /// <summary>Reads the current optimal value of a variable from the final tableau.</summary>
        private double VarValue(string name)
        {
            int r = BasisRowOf(name);
            return r == -1 ? 0.0 : Final[r][RhsCol];
        }

        /// <summary>Appends a completed report to the output text file (best-effort).</summary>
        private void Export(string report)
        {
            if (string.IsNullOrEmpty(OutputPath)) return;
            try { File.AppendAllText(OutputPath, report + Environment.NewLine); } catch { /* ignore IO issues */ }
        }

        private string Guard()
        {
            if (!IsOptimal) return Status;
            return null;
        }

        // =====================================================================
        //  1/2. NON-BASIC decision variable objective-coefficient range & change
        // =====================================================================

        /// <summary>Range over which a non-basic variable's objective coefficient keeps the basis optimal.</summary>
        public string NonBasicVariableRange(int varNumber)
        {
            string g = Guard(); if (g != null) return g;
            string name = "x" + varNumber;
            int col = ColIndexOf(name);
            if (col < 0) return $"Variable {name} does not exist.";
            if (IsBasic(name)) return $"{name} is BASIC in the optimal solution. Use the Basic-Variable range option instead.";

            double arc = Arc(col);                 // >= 0 at optimality
            double c = varNumber <= C.Count ? C[varNumber - 1] : 0.0;

            // Changing c_j by delta changes the (adjusted) reduced cost by -delta.
            // Optimality requires arc - delta >= 0  =>  delta <= arc  (bound on the "toward-entering" side).
            double allowableTowardEntering = arc;  // how far c can move before the var becomes attractive
            double lo, hi;
            if (IsMax) { lo = double.NegativeInfinity; hi = c + allowableTowardEntering; }   // c may rise up to c+arc
            else { lo = c - allowableTowardEntering; hi = double.PositiveInfinity; }         // c may fall down to c-arc

            var sb = new StringBuilder();
            sb.AppendLine($"Non-Basic Variable Objective-Coefficient Range  ({name})");
            sb.AppendLine(new string('-', 60));
            sb.AppendLine($"Current objective coefficient c({name}) = {Fmt(c)}");
            sb.AppendLine($"Reduced cost of {name}               = {Fmt(Final[0][col])}");
            sb.AppendLine($"Allowable range for c({name})        : [{Fmt(lo)} , {Fmt(hi)}]");
            sb.AppendLine("Within this range the current optimal basis (and solution) stays optimal.");
            string report = sb.ToString();
            Export(report);
            return report;
        }

        /// <summary>Applies a change to a non-basic variable's objective coefficient and reports the effect.</summary>
        public string ApplyNonBasicVariableChange(int varNumber, double newCoefficient)
        {
            string g = Guard(); if (g != null) return g;
            string name = "x" + varNumber;
            int col = ColIndexOf(name);
            if (col < 0) return $"Variable {name} does not exist.";
            if (IsBasic(name)) return $"{name} is BASIC; use the Apply-Basic-Variable-change option instead.";

            double oldC = varNumber <= C.Count ? C[varNumber - 1] : 0.0;
            double delta = newCoefficient - oldC;
            // New reduced cost in the stored convention: current z-row entry minus delta.
            double newReduced = Final[0][col] - delta;
            bool stillOptimal = S * newReduced >= -EPS;

            var sb = new StringBuilder();
            sb.AppendLine($"Apply Change to Non-Basic Variable {name}");
            sb.AppendLine(new string('-', 60));
            sb.AppendLine($"Old coefficient c({name}) = {Fmt(oldC)}   ->   New = {Fmt(newCoefficient)}   (delta = {Fmt(delta)})");
            sb.AppendLine($"New reduced cost of {name} = {Fmt(newReduced)}");
            if (stillOptimal)
            {
                sb.AppendLine("Result: the basis remains OPTIMAL. The variable stays non-basic (value 0),");
                sb.AppendLine($"        so the optimal objective value is unchanged (Z* = {Fmt(Final[0][RhsCol])}).");
            }
            else
            {
                sb.AppendLine("Result: the reduced cost now violates optimality, so this variable would ENTER");
                sb.AppendLine("        the basis. The model must be re-optimised to find the new optimum.");
            }
            string report = sb.ToString();
            Export(report);
            return report;
        }

        // =====================================================================
        //  3/4. BASIC variable objective-coefficient range & change
        // =====================================================================

        public string BasicVariableRange(int varNumber)
        {
            string g = Guard(); if (g != null) return g;
            string name = "x" + varNumber;
            if (ColIndexOf(name) < 0) return $"Variable {name} does not exist.";
            if (!IsBasic(name)) return $"{name} is NON-BASIC in the optimal solution. Use the Non-Basic-Variable range option instead.";

            int r = BasisRowOf(name);
            double c = varNumber <= C.Count ? C[varNumber - 1] : 0.0;
            var (inc, dec) = BasicObjBounds(r);

            double lo = double.IsPositiveInfinity(dec) ? double.NegativeInfinity : c - dec;
            double hi = double.IsPositiveInfinity(inc) ? double.PositiveInfinity : c + inc;

            var sb = new StringBuilder();
            sb.AppendLine($"Basic Variable Objective-Coefficient Range  ({name})");
            sb.AppendLine(new string('-', 60));
            sb.AppendLine($"Current objective coefficient c({name}) = {Fmt(c)}");
            sb.AppendLine($"Allowable increase = {Fmt(inc)}   Allowable decrease = {Fmt(dec)}");
            sb.AppendLine($"Allowable range for c({name}) : [{Fmt(lo)} , {Fmt(hi)}]");
            sb.AppendLine("Within this range the current basis stays optimal (the solution point is unchanged,");
            sb.AppendLine("but the optimal Z value changes as this coefficient changes).");
            string report = sb.ToString();
            Export(report);
            return report;
        }

        /// <summary>
        /// Computes allowable increase/decrease of the objective coefficient of the basic
        /// variable in row r. Uses: optimality requires  arc[j] + delta*(S*Final[r][j]) >= 0
        /// for every non-basic column j.
        /// </summary>
        private (double inc, double dec) BasicObjBounds(int r)
        {
            double inc = double.PositiveInfinity, dec = double.PositiveInfinity;
            for (int j = 0; j < RhsCol; j++)
            {
                string colName = ColHeads[j];
                if (IsBasic(colName)) continue;      // only non-basic columns constrain the range
                double w = S * Final[r][j];
                if (Math.Abs(w) < EPS) continue;
                double arc = Arc(j);
                if (w > 0) dec = Math.Min(dec, arc / w);       // delta >= -arc/w  -> decrease bound
                else inc = Math.Min(inc, -(arc / w));          // delta <= -arc/w  -> increase bound
            }
            return (inc, dec);
        }

        public string ApplyBasicVariableChange(int varNumber, double newCoefficient)
        {
            string g = Guard(); if (g != null) return g;
            string name = "x" + varNumber;
            if (!IsBasic(name)) return $"{name} is NON-BASIC; use the Apply-Non-Basic-Variable-change option instead.";

            int r = BasisRowOf(name);
            double oldC = varNumber <= C.Count ? C[varNumber - 1] : 0.0;
            double delta = newCoefficient - oldC;
            var (inc, dec) = BasicObjBounds(r);
            bool withinRange = delta <= inc + EPS && -delta <= dec + EPS;

            // If still optimal, the solution point is unchanged; only Z changes by delta * (value of this basic var).
            double varVal = Final[r][RhsCol];
            double newZ = Final[0][RhsCol] + delta * varVal;

            var sb = new StringBuilder();
            sb.AppendLine($"Apply Change to Basic Variable {name}");
            sb.AppendLine(new string('-', 60));
            sb.AppendLine($"Old coefficient c({name}) = {Fmt(oldC)}   ->   New = {Fmt(newCoefficient)}   (delta = {Fmt(delta)})");
            sb.AppendLine($"Allowable increase = {Fmt(inc)}   Allowable decrease = {Fmt(dec)}");
            if (withinRange)
            {
                sb.AppendLine("Result: change is WITHIN the optimal range. Same solution point,");
                sb.AppendLine($"        new optimal Z = {Fmt(Final[0][RhsCol])} + ({Fmt(delta)} x {Fmt(varVal)}) = {Fmt(newZ)}.");
            }
            else
            {
                sb.AppendLine("Result: change is OUTSIDE the optimal range; the basis is no longer optimal");
                sb.AppendLine("        and the model must be re-optimised to find the new optimum.");
            }
            string report = sb.ToString();
            Export(report);
            return report;
        }

        // =====================================================================
        //  5/6. Constraint RHS range & change
        // =====================================================================

        public string RhsRange(int constraintNumber)
        {
            string g = Guard(); if (g != null) return g;
            if (constraintNumber < 1 || constraintNumber > M) return $"Constraint {constraintNumber} does not exist (1..{M}).";

            int slackCol = N + (constraintNumber - 1);      // column of slack s{constraintNumber} = B^-1 column i
            var (inc, dec) = RhsBounds(slackCol);
            double b = B[constraintNumber - 1];
            double lo = double.IsPositiveInfinity(dec) ? double.NegativeInfinity : b - dec;
            double hi = double.IsPositiveInfinity(inc) ? double.PositiveInfinity : b + inc;
            double shadow = Final[0][slackCol];

            var sb = new StringBuilder();
            sb.AppendLine($"Right-Hand-Side Range  (Constraint {constraintNumber})");
            sb.AppendLine(new string('-', 60));
            sb.AppendLine($"Current RHS b({constraintNumber}) = {Fmt(b)}");
            sb.AppendLine($"Shadow price y({constraintNumber}) = {Fmt(shadow)}");
            sb.AppendLine($"Allowable increase = {Fmt(inc)}   Allowable decrease = {Fmt(dec)}");
            sb.AppendLine($"Allowable range for b({constraintNumber}) : [{Fmt(lo)} , {Fmt(hi)}]");
            sb.AppendLine("Within this range the current basis stays feasible and the shadow price is valid.");
            string report = sb.ToString();
            Export(report);
            return report;
        }

        /// <summary>
        /// Feasibility ranging on a RHS uses the slack column (a B^-1 column):
        /// each basic value must satisfy  value + delta*col >= 0.
        /// </summary>
        private (double inc, double dec) RhsBounds(int slackCol)
        {
            double inc = double.PositiveInfinity, dec = double.PositiveInfinity;
            for (int k = 1; k < Final.Count; k++)
            {
                double col = Final[k][slackCol];
                double val = Final[k][RhsCol];
                if (Math.Abs(col) < EPS) continue;
                if (col > 0) dec = Math.Min(dec, val / col);     // delta >= -val/col -> decrease bound
                else inc = Math.Min(inc, -val / col);            // delta <= -val/col -> increase bound
            }
            return (inc, dec);
        }

        public string ApplyRhsChange(int constraintNumber, double newRhs)
        {
            string g = Guard(); if (g != null) return g;
            if (constraintNumber < 1 || constraintNumber > M) return $"Constraint {constraintNumber} does not exist (1..{M}).";

            int slackCol = N + (constraintNumber - 1);
            double oldB = B[constraintNumber - 1];
            double delta = newRhs - oldB;
            var (inc, dec) = RhsBounds(slackCol);
            bool feasible = delta <= inc + EPS && -delta <= dec + EPS;
            double shadow = Final[0][slackCol];
            double newZ = Final[0][RhsCol] + delta * shadow;

            var sb = new StringBuilder();
            sb.AppendLine($"Apply Change to RHS of Constraint {constraintNumber}");
            sb.AppendLine(new string('-', 60));
            sb.AppendLine($"Old RHS = {Fmt(oldB)}   ->   New RHS = {Fmt(newRhs)}   (delta = {Fmt(delta)})");
            sb.AppendLine($"Shadow price = {Fmt(shadow)}");
            if (feasible)
            {
                sb.AppendLine("Result: change is WITHIN the feasible range; same basis remains optimal.");
                sb.AppendLine($"        New optimal Z = {Fmt(Final[0][RhsCol])} + ({Fmt(delta)} x {Fmt(shadow)}) = {Fmt(newZ)}.");
                sb.AppendLine("        Updated basic-variable values:");
                for (int k = 1; k < Final.Count; k++)
                {
                    double newVal = Final[k][RhsCol] + delta * Final[k][slackCol];
                    sb.AppendLine($"          {Basis[k]} = {Fmt(newVal)}");
                }
            }
            else
            {
                sb.AppendLine("Result: change is OUTSIDE the feasible range; a basic variable would go negative.");
                sb.AppendLine("        The model must be re-optimised (dual simplex) to restore feasibility.");
            }
            string report = sb.ToString();
            Export(report);
            return report;
        }

        // =====================================================================
        //  7/8. Range / change of a coefficient in a NON-BASIC variable column
        // =====================================================================

        public string NonBasicColumnRange(int varNumber, int constraintNumber)
        {
            string g = Guard(); if (g != null) return g;
            string name = "x" + varNumber;
            if (ColIndexOf(name) < 0) return $"Variable {name} does not exist.";
            if (IsBasic(name)) return $"{name} is BASIC; column ranging here is defined for a NON-BASIC variable column.";
            if (constraintNumber < 1 || constraintNumber > M) return $"Constraint {constraintNumber} does not exist (1..{M}).";

            int col = ColIndexOf(name);
            double aij = A[constraintNumber - 1][varNumber - 1];
            double yi = Final[0][N + (constraintNumber - 1)];   // shadow price of that constraint
            double reduced = Final[0][col];                     // current reduced cost (stored convention)

            // Reduced cost changes by  yi * delta  when a_ij changes by delta.
            // Optimality requires  S*(reduced + yi*delta) >= 0.
            double lo, hi;
            if (Math.Abs(yi) < EPS)
            {
                lo = double.NegativeInfinity; hi = double.PositiveInfinity; // coefficient does not affect optimality
            }
            else
            {
                double bound = -reduced / yi;                   // delta at which reduced cost hits 0
                if (S * yi > 0) { lo = double.NegativeInfinity; hi = aij + bound; }
                else { lo = aij + bound; hi = double.PositiveInfinity; }
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Non-Basic Column Coefficient Range  (a[{constraintNumber},{name}])");
            sb.AppendLine(new string('-', 60));
            sb.AppendLine($"Current coefficient a({constraintNumber},{name}) = {Fmt(aij)}");
            sb.AppendLine($"Shadow price of constraint {constraintNumber}      = {Fmt(yi)}");
            sb.AppendLine($"Reduced cost of {name}                = {Fmt(reduced)}");
            sb.AppendLine($"Allowable range for a({constraintNumber},{name}) : [{Fmt(lo)} , {Fmt(hi)}]");
            sb.AppendLine("Within this range the current basis stays optimal.");
            string report = sb.ToString();
            Export(report);
            return report;
        }

        public string ApplyNonBasicColumnChange(int varNumber, int constraintNumber, double newCoefficient)
        {
            string g = Guard(); if (g != null) return g;
            string name = "x" + varNumber;
            if (ColIndexOf(name) < 0) return $"Variable {name} does not exist.";
            if (IsBasic(name)) return $"{name} is BASIC; this change is defined for a NON-BASIC variable column.";
            if (constraintNumber < 1 || constraintNumber > M) return $"Constraint {constraintNumber} does not exist (1..{M}).";

            int col = ColIndexOf(name);
            double aij = A[constraintNumber - 1][varNumber - 1];
            double delta = newCoefficient - aij;
            double yi = Final[0][N + (constraintNumber - 1)];
            double newReduced = Final[0][col] + yi * delta;
            bool stillOptimal = S * newReduced >= -EPS;

            var sb = new StringBuilder();
            sb.AppendLine($"Apply Change to Column Coefficient a[{constraintNumber},{name}]");
            sb.AppendLine(new string('-', 60));
            sb.AppendLine($"Old a({constraintNumber},{name}) = {Fmt(aij)}   ->   New = {Fmt(newCoefficient)}   (delta = {Fmt(delta)})");
            sb.AppendLine($"New reduced cost of {name} = {Fmt(newReduced)}");
            sb.AppendLine(stillOptimal
                ? "Result: basis remains OPTIMAL; the variable stays non-basic and Z* is unchanged."
                : "Result: optimality is violated; the variable would enter. Re-optimise the model.");
            string report = sb.ToString();
            Export(report);
            return report;
        }

        // =====================================================================
        //  9. Add a new activity (new decision variable) to the optimal solution
        // =====================================================================

        /// <summary>
        /// params: objCoeff followed by M column coefficients (one per constraint).
        /// Computes the new variable's reduced cost from the shadow prices; if it would improve
        /// the objective, the augmented model is re-solved and the new optimum reported.
        /// </summary>
        public string AddActivity(double objCoeff, double[] column, List<string> modelData)
        {
            string g = Guard(); if (g != null) return g;
            if (column.Length != M) return $"A new activity needs exactly {M} column coefficient(s) (one per constraint).";

            // reduced cost (max convention "z_j - c_j") = y . a  -  c_new
            double yDotA = 0.0;
            for (int i = 0; i < M; i++) yDotA += Final[0][N + i] * column[i];
            double reduced = yDotA - objCoeff;
            bool improves = IsMax ? reduced < -EPS : reduced > EPS;

            var sb = new StringBuilder();
            sb.AppendLine("Add a New Activity (new decision variable)");
            sb.AppendLine(new string('-', 60));
            sb.AppendLine($"New variable coefficient c(new) = {Fmt(objCoeff)}");
            sb.AppendLine($"New variable column (per constraint) = [{string.Join(", ", column.Select(Fmt))}]");
            sb.AppendLine($"Shadow prices y = [{string.Join(", ", Enumerable.Range(0, M).Select(i => Fmt(Final[0][N + i])))}]");
            sb.AppendLine($"Reduced cost (y.a - c) = {Fmt(reduced)}");

            if (!improves)
            {
                sb.AppendLine("Result: the new activity is NOT attractive. The current optimum stays optimal,");
                sb.AppendLine("        and the new variable would be 0 in the solution.");
            }
            else
            {
                sb.AppendLine("Result: the new activity IS attractive (it would enter the basis).");
                sb.AppendLine("        Re-solving the augmented model...");
                sb.AppendLine();
                sb.AppendLine(ReSolveWithExtraVariable(modelData, objCoeff, column));
            }
            string report = sb.ToString();
            Export(report);
            return report;
        }

        // =====================================================================
        //  10. Add a new constraint to the optimal solution
        // =====================================================================

        /// <summary>
        /// Checks whether the current optimal point satisfies the new constraint. If it does,
        /// nothing changes. If it is violated, the augmented model is re-solved and reported.
        /// </summary>
        public string AddConstraint(double[] coeffs, string relation, double rhs, List<string> modelData)
        {
            string g = Guard(); if (g != null) return g;
            if (coeffs.Length != N) return $"A new constraint needs exactly {N} coefficient(s) (one per decision variable).";

            // Evaluate the new constraint at the current optimal point.
            double lhs = 0.0;
            for (int j = 0; j < N; j++) lhs += coeffs[j] * VarValue("x" + (j + 1));
            bool satisfied;
            switch (relation)
            {
                case "<=": satisfied = lhs <= rhs + EPS; break;
                case ">=": satisfied = lhs >= rhs - EPS; break;
                default: satisfied = Math.Abs(lhs - rhs) <= EPS; break; // "="
            }

            var sb = new StringBuilder();
            sb.AppendLine("Add a New Constraint to the Optimal Solution");
            sb.AppendLine(new string('-', 60));
            string lhsText = string.Join(" + ", Enumerable.Range(0, N).Select(j => $"{Fmt(coeffs[j])}*x{j + 1}"));
            sb.AppendLine($"New constraint: {lhsText} {relation} {Fmt(rhs)}");
            sb.AppendLine($"At the current optimum the LHS evaluates to {Fmt(lhs)}.");

            if (satisfied)
            {
                sb.AppendLine("Result: the current optimal solution already SATISFIES the new constraint,");
                sb.AppendLine("        so it stays optimal (the constraint is redundant here).");
            }
            else
            {
                sb.AppendLine("Result: the current optimum VIOLATES the new constraint.");
                sb.AppendLine("        Re-solving the augmented model...");
                sb.AppendLine();
                sb.AppendLine(ReSolveWithExtraConstraint(modelData, coeffs, relation, rhs));
            }
            string report = sb.ToString();
            Export(report);
            return report;
        }

        // =====================================================================
        //  11. Shadow prices
        // =====================================================================

        public string ShadowPrices()
        {
            string g = Guard(); if (g != null) return g;
            var sb = new StringBuilder();
            sb.AppendLine("Shadow Prices (dual values)");
            sb.AppendLine(new string('-', 60));
            sb.AppendLine("The shadow price of a constraint is the rate of change of the optimal");
            sb.AppendLine("objective value per unit increase in that constraint's RHS.");
            sb.AppendLine();
            for (int i = 0; i < M; i++)
                sb.AppendLine($"  Constraint {i + 1} : shadow price = {Fmt(Final[0][N + i])}");
            sb.AppendLine();
            sb.AppendLine($"Optimal objective value Z* = {Fmt(Final[0][RhsCol])}");
            string report = sb.ToString();
            Export(report);
            return report;
        }

        // =====================================================================
        //  12. Duality
        // =====================================================================

        /// <summary>
        /// Builds the dual model, solves it, and reports strong/weak duality.
        /// For the project's canonical case (max, all &lt;= constraints, x &gt;= 0) the dual is
        ///   min b.y  s.t.  A^T y &gt;= c , y &gt;= 0.
        /// </summary>
        public string Duality()
        {
            string g = Guard(); if (g != null) return g;

            var sb = new StringBuilder();
            sb.AppendLine("Duality");
            sb.AppendLine(new string('-', 60));

            // ----- Display the dual model -----
            if (IsMax)
            {
                sb.AppendLine("Primal:  max c.x   s.t.  A.x <= b ,  x >= 0");
                sb.AppendLine("Dual  :  min b.y   s.t.  A^T.y >= c ,  y >= 0");
            }
            else
            {
                sb.AppendLine("Primal:  min c.x   s.t.  A.x >= b ,  x >= 0");
                sb.AppendLine("Dual  :  max b.y   s.t.  A^T.y <= c ,  y >= 0");
            }
            sb.AppendLine();
            sb.AppendLine("Dual objective:  " + (IsMax ? "min " : "max ") +
                          string.Join(" + ", Enumerable.Range(0, M).Select(i => $"{Fmt(B[i])}*y{i + 1}")));
            for (int j = 0; j < N; j++)
            {
                string lhs = string.Join(" + ", Enumerable.Range(0, M).Select(i => $"{Fmt(A[i][j])}*y{i + 1}"));
                sb.AppendLine($"  {lhs} {(IsMax ? ">=" : "<=")} {Fmt(C[j])}");
            }
            sb.AppendLine();

            // ----- Solve the dual by building its tableau directly -----
            double dualOpt;
            string dualStatus;
            var dualTableauText = SolveDual(out dualOpt, out dualStatus);
            sb.AppendLine("Dual solution:");
            sb.AppendLine(dualTableauText);

            // ----- Verify strong / weak duality -----
            double primalOpt = Final[0][RhsCol];
            sb.AppendLine($"Primal optimal Z* = {Fmt(primalOpt)}");
            if (dualStatus == "optimal")
            {
                sb.AppendLine($"Dual   optimal W* = {Fmt(dualOpt)}");
                if (Math.Abs(primalOpt - dualOpt) <= 1e-6)
                    sb.AppendLine("=> STRONG DUALITY holds: primal optimum equals dual optimum.");
                else
                    sb.AppendLine("=> Only WEAK DUALITY observed here (a gap between primal and dual optima).");
            }
            else
            {
                sb.AppendLine($"Dual could not be solved to optimality ({dualStatus}).");
                sb.AppendLine("Weak duality still bounds the primal, but strong duality cannot be confirmed here.");
            }
            string report = sb.ToString();
            Export(report);
            return report;
        }

        /// <summary>
        /// Constructs the dual tableau in the solver's convention and solves it.
        /// Dual (for a max primal): min b.y s.t. A^T y >= c. Multiply each >= row by -1 to get
        /// <= rows with a +1 slack identity and (possibly negative) RHS; the solver's dual-simplex
        /// phase then restores feasibility.
        /// </summary>
        private string SolveDual(out double dualOpt, out string dualStatus)
        {
            int dn = M;          // dual has M variables (y1..yM)
            int dm = N;          // dual has N constraints (one per primal variable)
            string dualObj = IsMax ? "min" : "max";

            var matrix = new List<List<double>>();
            var colHeads = new List<string> { "T-i" };
            for (int j = 0; j < dn; j++) colHeads.Add("y" + (j + 1));
            for (int i = 0; i < dm; i++) colHeads.Add("s" + (i + 1));
            colHeads.Add("RHS");

            // z-row: for a min problem the solver stores it negated as well.
            var z = new List<double>();
            for (int j = 0; j < dn; j++) z.Add(-B[j]);          // objective coefficients are b
            for (int i = 0; i < dm; i++) z.Add(0.0);
            z.Add(0.0);
            matrix.Add(z);

            // Constraints: A^T y >= c  ->  -A^T y <= -c
            var rowHeads = new List<string> { "z" };
            for (int i = 0; i < dm; i++)   // one dual constraint per primal variable
            {
                var row = new List<double>();
                for (int j = 0; j < dn; j++) row.Add(-A[j][i]);   // -(A^T)_{i j} = -A[j][i]
                for (int k = 0; k < dm; k++) row.Add(k == i ? 1.0 : 0.0);
                row.Add(-C[i]);                                    // -c_i
                matrix.Add(row);
                rowHeads.Add((i + 1).ToString());
            }

            var (pages, finalTableau, _) =
                Simplex.SolveTable(matrix, colHeads, rowHeads, dualObj, null, showDialogs: false);

            if (finalTableau == null)
            {
                dualOpt = double.NaN;
                dualStatus = "infeasible/unbounded";
                return "  (dual could not be solved)\n";
            }

            dualOpt = finalTableau[0][finalTableau[0].Count - 1];
            dualStatus = "optimal";
            // Show only the final dual tableau for brevity.
            return pages.Count > 0 ? pages[pages.Count - 1] : "";
        }

        // =====================================================================
        //  Re-solving helpers for "add activity" and "add constraint"
        // =====================================================================

        /// <summary>Rebuilds the model text with one extra decision variable and re-solves it.</summary>
        private string ReSolveWithExtraVariable(List<string> modelData, double objCoeff, double[] column)
        {
            var lines = modelData[0].Replace("\r\n", "\n").Split('\n').ToList();
            // Objective line: append the new coefficient with its sign operator.
            lines[0] = lines[0].TrimEnd() + " " + SignNum(objCoeff);
            // Each constraint line: insert the new coefficient just before the relation token.
            for (int i = 1; i <= M; i++)
            {
                var parts = lines[i].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                int relIdx = parts.FindIndex(p => p.StartsWith("<=") || p.StartsWith(">=") || p.StartsWith("="));
                if (relIdx < 0) relIdx = parts.Count - 1;
                parts.Insert(relIdx, SignNum(column[i - 1]));
                lines[i] = string.Join(" ", parts);
            }
            // Sign-restriction line (last): add a "pos" for the new variable.
            lines[lines.Count - 1] = lines[lines.Count - 1].TrimEnd() + " pos";

            return SolveAugmented(string.Join("\n", lines));
        }

        /// <summary>Rebuilds the model text with one extra constraint and re-solves it.</summary>
        private string ReSolveWithExtraConstraint(List<string> modelData, double[] coeffs, string relation, double rhs)
        {
            var lines = modelData[0].Replace("\r\n", "\n").Split('\n').ToList();
            // Build the new constraint line in the same signed format as the input file.
            string newLine = string.Join(" ", coeffs.Select(SignNum)) + " " + relation + Fmt(rhs);
            // Insert it just before the sign-restriction line (which must stay last).
            lines.Insert(lines.Count - 1, newLine);
            return SolveAugmented(string.Join("\n", lines));
        }

        /// <summary>Solves an augmented model text with the primal simplex and returns its final tableau text.</summary>
        private string SolveAugmented(string augmentedModel)
        {
            try
            {
                var (matrix, colHeads, rowHeads, _, objFunc) =
                    ConvertStandardForm.ConvertMatrix(new List<string> { augmentedModel });
                var (pages, finalTableau, rowHeadsFinal) =
                    Simplex.SolveTable(matrix, colHeads, rowHeads, objFunc, null, showDialogs: false);

                if (finalTableau == null) return "  (augmented model is infeasible or unbounded)";

                var sb = new StringBuilder();
                sb.AppendLine("  New optimal tableau:");
                sb.AppendLine(pages[pages.Count - 1]);
                sb.AppendLine($"  New optimal Z* = {Fmt(finalTableau[0][finalTableau[0].Count - 1])}");
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "  (could not re-solve augmented model: " + ex.Message + ")";
            }
        }

        /// <summary>Formats a number with the leading +/- operator used by the input file format.</summary>
        private static string SignNum(double v)
        {
            string mag = Math.Abs(v).ToString("0.###", CultureInfo.InvariantCulture);
            return (v < 0 ? "-" : "+") + mag;
        }
    }
}
