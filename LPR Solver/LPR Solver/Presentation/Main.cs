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
        private void btnSolve_Click(object sender, EventArgs e)
        {

            List<string> modelInput = new List<string>();
            modelOutput.Clear();
            

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
                    case 4:
                        modelOutput = CuttingPlane.CuttingPlaneSolver(modelInput, textFilePath);
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
    }
}
