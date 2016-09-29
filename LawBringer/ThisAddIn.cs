using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Excel = Microsoft.Office.Interop.Excel;
using Office = Microsoft.Office.Core;
using Microsoft.Office.Tools.Excel;

namespace LawBringer
{
    public partial class ThisAddIn
    {
        private EmployeeFromFile _fromFileControl;
        public Microsoft.Office.Tools.CustomTaskPane FromFilePane { get; set; }

        private void ThisAddIn_Startup(object sender, System.EventArgs e)
        {
            _fromFileControl = new EmployeeFromFile();
            FromFilePane = this.CustomTaskPanes.Add(_fromFileControl, "Law Bringer - Employee From File");
            FromFilePane.DockPosition = Office.MsoCTPDockPosition.msoCTPDockPositionFloating;
            FromFilePane.Width = 400;
            FromFilePane.Height = 200;
        }

        protected override Office.IRibbonExtensibility CreateRibbonExtensibilityObject()
        {
            return new LawBringerRibbon();
        }

        private void ThisAddIn_Shutdown(object sender, System.EventArgs e)
        {
        }

        #region VSTO generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InternalStartup()
        {
            this.Startup += new System.EventHandler(ThisAddIn_Startup);
            this.Shutdown += new System.EventHandler(ThisAddIn_Shutdown);
        }
        
        #endregion
    }
}
