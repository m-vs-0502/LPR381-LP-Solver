using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LPR_Solver.BusinessLogic;
using System.IO;


namespace LPR_Solver
{
    public partial class Main : Form
    {

        private readonly FileService _fileService = new FileService(); //Create instance of object for file 

        public Main()
        {
            InitializeComponent();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();

        }
        string textFilePath = null;
        private void btnBrowse_Click(object sender, EventArgs e)
        {
            // Create a new instance so Windows dialog state isn't cached/reused
            using (OpenFileDialog openFD = new OpenFileDialog())
            {
                // 1. Strict filter string 
                openFD.Filter = "Text Files (*.txt)|*.txt";

                // 2. Explicitly select the first filter option
                openFD.FilterIndex = 1;

                openFD.InitialDirectory = Path.Combine(AppContext.BaseDirectory, @"..\..\..\");


                if (openFD.ShowDialog() == DialogResult.OK)
                {
                    openFD.Filter = "Text Files (*.txt)|*.txt"; //Shows only .txt files
                    textFilePath = openFD.FileName; //Saves the path of selected file in variable

                    try
                    {
                        // 1. Call Business Logic to get file contents
                        string content = _fileService.GetFileContent(textFilePath);

                        // 2. Display content in the RichTextBox UI control
                        rTBDisplay.Text = content;
                        _lastModelText = content;   // keep raw model for sensitivity analysis
                        _sa = null;                 // invalidate any previous sensitivity context
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error Loading File", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            rTBDisplay.Clear();
            rTBDisplay.Text = "Model will be shown here once selected";

            btnSolve.Visible = true;
            btnEdit.Visible = true;
            btnNextTable.Visible = false;
            btnPreviousTable.Visible = false;

        }

        bool editMode = false;//used for edit button
        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (!editMode)
            {
                btnEdit.Text = "Save";

            }
            else
            {
                btnEdit.Text = "Edit";
            }
            rTBDisplay.Enabled = !rTBDisplay.Enabled;
            editMode = !editMode;


        }

        private void Main_Load(object sender, EventArgs e)
        {
            cmbAlgorithm.SelectedIndex = 0;
        }


        
        int tableNum = 0;
        List<string> modelOutput = new List<string>();
        private string _lastModelText = null;            // raw model text, kept for sensitivity analysis
        private SensitivityAnalysis _sa = null;          // cached sensitivity context (built on demand)
        private void btnSolve_Click(object sender, EventArgs e)
        {

            List<string> modelInput = new List<string>();
            modelOutput.Clear();

            _lastModelText = rTBDisplay.Text;            // remember the model before the view is overwritten
            _sa = null;                                  // force a rebuild of the sensitivity context

            modelInput.Add(rTBDisplay.Text);
            if (InputValidation.ValidateForm(rTBDisplay.Text))//universal validation
            {

                switch (cmbAlgorithm.SelectedIndex)
                {
                    case 0:
                        var (modelOutputList, tblOptimal) = Simplex.SimplexSolver(modelInput, textFilePath, true);
                        modelOutput = modelOutputList;
                        break;
                    case 2:
                        modelOutput = BranchAndBound.BaBSimplexSolver(modelInput, textFilePath);
                        break;
                    case 3:
                        modelOutput = Knapsack.KnapsackSolver(modelInput, textFilePath);
                        break;
                    default:
                        modelOutput.Add("No model selected.");
                        break;

                }

                rTBDisplay.Text = modelOutput[0];
                btnNextTable.Visible = true;
                btnPreviousTable.Visible = true;
                btnEdit.Visible = false;
                btnSolve.Visible = false;
                rTBDisplay.Enabled = true;
                


            }
        }

        private void btnNextTable_Click(object sender, EventArgs e)
        {
            if (tableNum + 1 <= modelOutput.Count() - 1)
            {
                rTBDisplay.Text = modelOutput[tableNum + 1];
                tableNum++;
            }
        }

        private void btnPreviousTable_Click(object sender, EventArgs e)
        {
            if (tableNum - 1 >= 0)
            {
                rTBDisplay.Text = modelOutput[tableNum - 1];
                tableNum--;
            }
        }

        // Updates the on-screen help so the user knows what to type into the two input boxes
        // for the currently selected sensitivity operation. Improves interface usability.
        private void cmbSensitivity_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (cmbSensitivity.SelectedIndex)
            {
                case 0: lblSAHelp.Text = "Box 1 = variable number (e.g. 2).\r\nBox 2 = (not used)."; break;
                case 1: lblSAHelp.Text = "Box 1 = variable number.\r\nBox 2 = new objective coefficient."; break;
                case 2: lblSAHelp.Text = "Box 1 = variable number.\r\nBox 2 = (not used)."; break;
                case 3: lblSAHelp.Text = "Box 1 = variable number.\r\nBox 2 = new objective coefficient."; break;
                case 4: lblSAHelp.Text = "Box 1 = constraint number.\r\nBox 2 = (not used)."; break;
                case 5: lblSAHelp.Text = "Box 1 = constraint number.\r\nBox 2 = new RHS value."; break;
                case 6: lblSAHelp.Text = "Box 1 = variable number.\r\nBox 2 = constraint number."; break;
                case 7: lblSAHelp.Text = "Box 1 = variable number.\r\nBox 2 = 'constraintNo newCoeff'."; break;
                case 8: lblSAHelp.Text = "Box 2 = 'objCoeff a1 a2 .. aM'\r\n(one column value per constraint)."; break;
                case 9: lblSAHelp.Text = "Box 2 = 'c1 c2 .. cN <= rhs'\r\n(relation may be <=, >= or =)."; break;
                case 10: lblSAHelp.Text = "Displays all shadow prices.\r\nNo input needed."; break;
                case 11: lblSAHelp.Text = "Builds & solves the dual,\r\nverifies strong/weak duality."; break;
                default: lblSAHelp.Text = "Select an operation."; break;
            }
        }

        // Builds the sensitivity context on demand from the last model, then runs the chosen operation.
        private void btnSensitivity_Click(object sender, EventArgs e)
        {
            // A model must have been loaded/solved first.
            if (string.IsNullOrWhiteSpace(_lastModelText) || !InputValidation.ValidateForm(_lastModelText))
            {
                MessageBox.Show("Please load and solve a valid Linear Programming model first.",
                    "Sensitivity Analysis", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Build (and cache) the optimal-solution context by solving the model silently.
            if (_sa == null)
            {
                _sa = SensitivityAnalysis.Build(new List<string> { _lastModelText }, textFilePath);
            }
            if (_sa == null || !_sa.IsOptimal)
            {
                MessageBox.Show(_sa?.Status ?? "The model could not be prepared for sensitivity analysis.",
                    "Sensitivity Analysis", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cmbSensitivity.SelectedIndex < 0)
            {
                MessageBox.Show("Please choose a sensitivity operation from the drop-down.",
                    "Sensitivity Analysis", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string result;
            try
            {
                result = RunSensitivityOperation();
            }
            catch (Exception ex)
            {
                // Friendly error rather than a crash when input parsing fails.
                result = "Could not perform the operation.\r\nReason: " + ex.Message +
                         "\r\nCheck the input format shown next to the drop-down.";
            }

            // Show the result in the display and stop the table-navigation buttons (this is not a tableau list).
            rTBDisplay.Enabled = true;
            rTBDisplay.Text = result;
            btnNextTable.Visible = false;
            btnPreviousTable.Visible = false;
        }

        // Parses the two input boxes according to the selected operation and calls the matching method.
        private string RunSensitivityOperation()
        {
            string box1 = (txtSAIndex.Text ?? "").Trim();
            string box2 = (txtSADelta.Text ?? "").Trim();

            switch (cmbSensitivity.SelectedIndex)
            {
                case 0: return _sa.NonBasicVariableRange(ParseInt(box1, "variable number"));
                case 1: return _sa.ApplyNonBasicVariableChange(ParseInt(box1, "variable number"), ParseDouble(box2, "new coefficient"));
                case 2: return _sa.BasicVariableRange(ParseInt(box1, "variable number"));
                case 3: return _sa.ApplyBasicVariableChange(ParseInt(box1, "variable number"), ParseDouble(box2, "new coefficient"));
                case 4: return _sa.RhsRange(ParseInt(box1, "constraint number"));
                case 5: return _sa.ApplyRhsChange(ParseInt(box1, "constraint number"), ParseDouble(box2, "new RHS"));
                case 6: return _sa.NonBasicColumnRange(ParseInt(box1, "variable number"), ParseInt(box2, "constraint number"));
                case 7:
                    {
                        var parts = box2.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length < 2) throw new FormatException("Box 2 must be 'constraintNo newCoeff'.");
                        return _sa.ApplyNonBasicColumnChange(ParseInt(box1, "variable number"),
                            ParseInt(parts[0], "constraint number"), ParseDouble(parts[1], "new coefficient"));
                    }
                case 8:
                    {
                        var nums = ParseDoubleList(box2, "'objCoeff a1 a2 .. aM'");
                        if (nums.Count < 2) throw new FormatException("Provide an objective coefficient and at least one column value.");
                        double objCoeff = nums[0];
                        double[] column = nums.Skip(1).ToArray();
                        return _sa.AddActivity(objCoeff, column, new List<string> { _lastModelText });
                    }
                case 9:
                    {
                        // Expect: c1 c2 .. cN <relation> rhs   e.g.  +1 +2 <=6
                        string rel = box2.Contains("<=") ? "<=" : box2.Contains(">=") ? ">=" : box2.Contains("=") ? "=" : null;
                        if (rel == null) throw new FormatException("Include a relation (<=, >= or =) in the constraint.");
                        int relPos = box2.IndexOf(rel, StringComparison.Ordinal);
                        string lhs = box2.Substring(0, relPos);
                        string rhs = box2.Substring(relPos + rel.Length);
                        double[] coeffs = ParseDoubleList(lhs, "constraint coefficients").ToArray();
                        double rhsVal = ParseDouble(rhs.Trim(), "constraint RHS");
                        return _sa.AddConstraint(coeffs, rel, rhsVal, new List<string> { _lastModelText });
                    }
                case 10: return _sa.ShadowPrices();
                case 11: return _sa.Duality();
                default: return "No sensitivity operation selected.";
            }
        }

        // ----- small parsing helpers with clear error messages -----
        private static int ParseInt(string s, string what)
        {
            if (!int.TryParse(s, out int v))
                throw new FormatException($"Expected a whole number for {what}, but got '{s}'.");
            return v;
        }

        private static double ParseDouble(string s, string what)
        {
            if (!double.TryParse(s, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double v))
                throw new FormatException($"Expected a number for {what}, but got '{s}'.");
            return v;
        }

        private static List<double> ParseDoubleList(string s, string what)
        {
            var list = new List<double>();
            foreach (var tok in (s ?? "").Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries))
                list.Add(ParseDouble(tok, what));
            return list;
        }
    }
}
