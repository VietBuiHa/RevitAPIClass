using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Linq;
using System.Windows;
using System.Collections.Generic;
using System;
using System.Web.UI.HtmlControls;
using System.Xml.Linq;
using System.Collections;

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
                    new ElementCategoryFilter(BuiltInCategory.OST_StructuralFraming),
                    new ElementCategoryFilter(BuiltInCategory.OST_Floors),
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
                        double totalIntersectArea = 0.0;
                        
                        //Get side face

                        if (geomElem!=null)
                        {
                            foreach (var obj1 in geomElem)
                            {
                                var solid1 = obj1 as Solid;
                                if (solid1 != null)
                                {
                                    // get DirectShape
                                    //GeometryObject[] geosolid = new GeometryObject[] { solid1 };
                                    //DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                                    //ds.SetShape(geosolid);

                                    foreach (Face face in solid1.Faces)
                                    {
                                        PlanarFace planarFace = face as PlanarFace;
                                        if (null != planarFace)
                                        {
                                            //XYZ origin = planarFace.Origin;
                                            //XYZ normal = planarFace.FaceNormal;
                                            //XYZ normal = planarFace.ComputeNormal(new UV(planarFace.Origin.X, planarFace.Origin.Y));
                                            //XYZ vectorX = planarFace.XVector;
                                            //XYZ vectorY = planarFace.YVector;
                                            if (Math.Round(planarFace.FaceNormal.Z, 2) == 0)
                                            {

                                                totalarea += face.Area;
                                                totalface++;

                                            }
                                        }
                                    }
                                }

                                //Get intersection
                                foreach (var obj2 in geomElem)
                                {
                                    var solid2 = obj2 as Solid;
                                    if (solid2 != null && solid2 != solid1)
                                    {
                                        // Create an IntersectionResult object
                                        IntersectionResult result = new IntersectionResult();

                                        

                                    }
                                }
                            }

                        }
                        totalarea = Math.Round(UnitUtils.Convert(totalarea, UnitTypeId.SquareFeet, UnitTypeId.SquareMeters),3);
                        //totalarea = Math.Floor(UnitUtils.Convert(totalarea, UnitTypeId.SquareFeet, UnitTypeId.SquareMeters));
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

        private object IntersectionResult(Solid solid1, Solid solid2, IntersectionResultArray results)
        {
            throw new NotImplementedException();
        }
    }    
}