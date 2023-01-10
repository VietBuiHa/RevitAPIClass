using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using System.Windows;

namespace WpfControlLibrary1
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    class AreaFormwork : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            //Get UIdocument
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            //Get Document
            Document doc = uidoc.Document;
            try
            {
                //Get elements of Category
                var eles = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralColumns)
                    .WhereElementIsNotElementType()
                    .ToElements();

                using (var tran = new Transaction(doc, "Calculator Formwork for Element"))
                {
                    tran.Start();
                    foreach (var item in eles)
                    {
                        MessageBox.Show(item.Id.ToString());
                    }
                    tran.Commit();
                }
                return Result.Succeeded;
            }
            catch (Exception exception)
            {

                message= exception.Message;
                return Result.Failed;
            }
        }
    }
}
