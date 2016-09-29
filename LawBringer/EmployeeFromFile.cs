using System;
using System.IO;
using System.Windows.Forms;
using LawBringer.Models;
using Microsoft.Office.Interop.Excel;

namespace LawBringer
{
    public partial class EmployeeFromFile : UserControl
    {
        public EmployeeFromFile()
        {
            InitializeComponent();
        }

        private void ChooseFile_Click_1(object sender, EventArgs e)
        {
            if (employeeFileDialog.ShowDialog() == DialogResult.OK)
            {
                using (var fileReader = new StreamReader(employeeFileDialog.FileName))
                {
                    var sheetWriter = new SheetWriter((Worksheet)Globals.ThisAddIn.Application.ActiveSheet);
                    var content = fileReader.ReadToEnd();
                    var parser = new FileParser(content);
                    var name = parser.GetName();

                    var employee = new Employee
                    {
                        Id = parser.GetSSN(),
                        TL = parser.GetTL(),
                        FirstName = name["first"],
                        LastName = name["last"],
                        PayPeriods = parser.GetPayPeriods()
                    };

                    //sheetWriter.SetName(name["last"] + ", " + name["first"]);
                    sheetWriter.WriteUnformattedPayPeriods(employee.PayPeriods, null, true);
                }
            }
        }
    }
}
