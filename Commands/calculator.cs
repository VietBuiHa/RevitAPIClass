﻿using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Linq;
using System.Windows;
using System.Collections.Generic;
using System;

namespace WpfControlLibrary1
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    class calculator : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            var uidoc = commandData.Application.ActiveUIDocument;
            var doc = uidoc.Document;

            try
            {
                //Get elements of Category
                var allelement = new LogicalOrFilter(new List<ElementFilter>()
                {
                    new ElementCategoryFilter(BuiltInCategory.OST_StructuralColumns),
                    new ElementCategoryFilter(BuiltInCategory.OST_Floors),
                    new ElementCategoryFilter(BuiltInCategory.OST_StructuralFraming),
                    new ElementCategoryFilter(BuiltInCategory.OST_Walls),
                    new ElementCategoryFilter(BuiltInCategory.OST_GenericModel),
                });
                var eles = new FilteredElementCollector(doc)
                    .WherePasses(allelement)
                    .WhereElementIsNotElementType()
                    .ToElements();
               // var floors = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Floors)
                    //.WhereElementIsNotElementType()
                    //.ToElements();

                using (var tran = new Transaction(doc, "Set information to Parameter"))
                {
                    tran.Start();
                    foreach (var ele in eles)

                    {
                        Options options = new Options();
                        options.DetailLevel = ViewDetailLevel.Fine;
                        options.ComputeReferences = true;
                        GeometryElement geomElem = ele.get_Geometry(options);

                        //declare variable
                        int totalface = 0;
                        double totalarea = 0.0;
                        
                        //Get side face

                        if (geomElem!=null)
                        {
                            foreach (var obj in geomElem)
                            {
                                var solid = obj as Solid;
                                if (solid != null)
                                {
                                    // get DirectShape
                                    GeometryObject[] dssolid = new GeometryObject[] { solid };
                                    DirectShape ds1 = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                                    ds1.SetShape(dssolid);

                                    foreach (Face face in solid.Faces)
                                    {
                                        PlanarFace planarFace = face as PlanarFace;
                                        if (null != planarFace)
                                        {
                                            //XYZ origin = planarFace.Origin;
                                            //XYZ normal = planarFace.FaceNormal;
                                            //XYZ normal = planarFace.ComputeNormal(new UV(planarFace.Origin.X, planarFace.Origin.Y));
                                            //XYZ vectorX = planarFace.XVector;
                                            //XYZ vectorY = planarFace.YVector;
                                            if (Math.Round(planarFace.FaceNormal.Z,2) == 0)
                                            {

                                                totalarea += face.Area;
                                                totalface++;
                                            }
                                        }
                                        //totalarea = face.Area;
                                        //totalface++;
                                    }

                                }
                            }

                        }
                        totalarea = UnitUtils.Convert(totalarea, UnitTypeId.SquareFeet, UnitTypeId.SquareMeters);
                        ele.LookupParameter("Comments").Set(totalarea.ToString());
                        ele.LookupParameter("Mark").Set(totalface.ToString());
                    }
                    tran.Commit();
                }
                return Result.Succeeded;
            }
            catch (Exception ex)
            {

                message = ex.Message;
                return Result.Failed;
            }

             
        }
    }    
}