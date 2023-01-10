﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;

namespace WpfControlLibrary1
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class formwork : IExternalCommand
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

                using (var tran = new Transaction(doc, "Calculator Formwork Area for Element"))
                {
                    tran.Start();
                    foreach (var ele in eles)
                    {
                        //Get BoundingBox from Element
                        BoundingBoxXYZ boundingBox = ele.get_BoundingBox(null);
                        //Get Outline 
                        Outline outline = new Outline(boundingBox.Min, boundingBox.Max);
                        BoundingBoxIntersectsFilter filter = new BoundingBoxIntersectsFilter(outline);
                        //BoundingBoxIntersectsFilter invertFilter = new BoundingBoxIntersectsFilter(outline,true);

                        var eleIntersect = new FilteredElementCollector(doc)
                            .WhereElementIsNotElementType()
                            .WherePasses(filter)
                            .ToElements();

                        foreach (var item in eleIntersect)
                        {
                            var joined = JoinGeometryUtils.AreElementsJoined(doc, ele, item);
                            if (joined == true)
                            {
                                JoinGeometryUtils.UnjoinGeometry(doc, ele, item);
                                //JoinGeometryUtils.JoinGeometry(doc, ele, item);
                                //JoinGeometryUtils.SwitchJoinOrder(doc, ele, item);
                            }
                            JoinGeometryUtils.JoinGeometry(doc, ele, item);
                            //JoinGeometryUtils.SwitchJoinOrder(doc, ele, item);
                        }
                    }
                    tran.Commit();
                }
                return Result.Succeeded;
            }
            catch (Exception exception)
            {

                message = exception.Message;
                return Result.Failed;
            }
        }
    }
}
