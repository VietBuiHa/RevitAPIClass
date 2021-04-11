using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Linq;
using System.Windows;
using WpfControlLibrary1.Windows;
using WpfControlLibrary1.Windows.ViewModels;

namespace WpfControlLibrary1
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    class command1 : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            var uidoc = commandData.Application.ActiveUIDocument;
            var doc = uidoc.Document;

            var categories = Enum.GetNames(typeof(BuiltInCategory));
            var viewmodel = new AutoJoinDialogViewModel(categories.OrderBy(s => s).ToArray());
            var window = new AutoJoinDialog(viewmodel);
            window.ShowDialog();

            if(window.DialogResult == true)
            {
                MessageBox.Show("Hello work Quyet");
            }



            return Result.Succeeded;
        }
    }
}
