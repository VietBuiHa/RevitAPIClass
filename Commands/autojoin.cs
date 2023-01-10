using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Linq;
using System.Windows;
using WpfControlLibrary1.Extensions;
using WpfControlLibrary1.Windows;
using WpfControlLibrary1.Windows.ViewModels;

namespace WpfControlLibrary1
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class autojoin : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uidoc = commandData.Application.ActiveUIDocument;
            var doc = uidoc.Document;

            var categories = Enum.GetNames(typeof(BuiltInCategory));
            var viewmodel = new AutoJoinDialogViewModel(categories.OrderBy(s => s).ToArray());
            var window = new AutoJoinDialog(viewmodel);
            window.ShowDialog();

            if (window.DialogResult == true)
            {
                Enum.TryParse(viewmodel.Category1, out BuiltInCategory category1);
                Enum.TryParse(viewmodel.Category2, out BuiltInCategory category2);

                var eles = new FilteredElementCollector(doc).OfCategory(category1)
                    .WhereElementIsNotElementType()
                    .ToElements();

                using (var tran = new Transaction(doc, "Join Floor and Column"))
                {
                    tran.Start();
                    foreach (var ele in eles)
                    {
                        var boundingBox = ele.get_BoundingBox(null);

                        var solids = ele.GetSolids();

                        var outline = new Outline(boundingBox.Min, boundingBox.Max);
                        var filter = new BoundingBoxIntersectsFilter(outline);

                        var collectors = new FilteredElementCollector(doc).OfCategory(category2)
                            .WhereElementIsNotElementType()
                            .WherePasses(filter)
                            .ToElements();

                        foreach (var item in collectors)
                        {
                            //Options options = new Options();
                            //options.ComputeReferences = true;
                            //options.DetailLevel = ViewDetailLevel.Fine;
                            //GeometryElement geoElement = item.get_Geometry(options);                        

                            bool joined = JoinGeometryUtils.AreElementsJoined(doc, ele, item);
                            if (joined == true)
                            {
                                JoinGeometryUtils.UnjoinGeometry(doc, ele, item);
                                JoinGeometryUtils.JoinGeometry(doc, ele, item);
                            }
                            else
                            {
                                JoinGeometryUtils.JoinGeometry(doc, ele, item);
                            }
                            //JoinGeometryUtils.SwitchJoinOrder(doc, ele, item);
                        }
                    }
                    tran.Commit();
                }
            }

           
            return Result.Succeeded;
        }       
    }
}
