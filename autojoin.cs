﻿using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Linq;
using System.Windows;

namespace WpfControlLibrary1
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    class autojoin : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uidoc = commandData.Application.ActiveUIDocument;
            var doc = uidoc.Document;

            var eles = new FilteredElementCollector(doc).OfCategory((BuiltInCategory.OST_StructuralColumns))
                .WhereElementIsNotElementType()
                .ToElements();

            using (var tran = new Transaction(doc, "Join Floor and Column"))
            {
                tran.Start();
                foreach (var ele in eles)
                {
                    var boundingBox = ele.get_BoundingBox(null);
                    var outline = new Outline(boundingBox.Min, boundingBox.Max);
                    var filter = new BoundingBoxIntersectsFilter(outline);                    

                    var collectors = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Walls)
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
                        
                            JoinGeometryUtils.UnjoinGeometry(doc, ele, item);
                            JoinGeometryUtils.JoinGeometry(doc, ele, item);                          
                            //JoinGeometryUtils.SwitchJoinOrder(doc, ele, item);
                    }
                }
                tran.Commit();
            }
            return Result.Succeeded;
        }       
    }
}
