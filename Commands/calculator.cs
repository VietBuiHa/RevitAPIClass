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
using WpfControlLibrary1.Extensions;
using System.Net.Sockets;
using Autodesk.Revit.DB.IFC;

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

                using (var tran = new Transaction(doc, "Set information to Parameter"))
                {
                    tran.Start();
                    foreach (var ele in eles)
                    {
                        Options options = new Options();
                        options.DetailLevel = ViewDetailLevel.Fine;
                        options.ComputeReferences = false;
                        GeometryElement geomElem = ele.get_Geometry(options);

                        //Get element intersection
                        var boundingBox = ele.get_BoundingBox(null);
                        if (boundingBox != null)
                        {
                            //var solids = ele.GetSolids();
                            var outline = new Outline(boundingBox.Min, boundingBox.Max);
                            var filter = new BoundingBoxIntersectsFilter(outline);

                            //Get elements with 2 condition
                            var combine = new LogicalAndFilter(new List<ElementFilter>()
                            {
                                allelement,filter
                            });

                            var elesIntersect = new FilteredElementCollector(doc)
                                .WhereElementIsNotElementType()
                                .WherePasses(combine)
                                .ToElements();

                            //declare variable
                            int totalface = 0;
                            double totalarea = 0.0;
                            double volumeOfIntersection = 0.0;
                            double areaOfIntersection = 0.0;

                            //Get side face
                            if (geomElem != null)
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
                                        foreach (var item in elesIntersect)
                                        {
                                            if (item.GetTypeId() != ele.GetTypeId())
                                            {
                                                GeometryElement geomElemI = item.get_Geometry(options);
                                                if (geomElemI != null)
                                                {
                                                    foreach (var obj2 in geomElemI)
                                                    {
                                                        var solid2 = obj2 as Solid;
                                                        if (solid2 != null)
                                                        {

                                                            Solid intersection = BooleanOperationsUtils.ExecuteBooleanOperation(solid1, solid2, BooleanOperationsType.Intersect);
                                                            if (intersection != null)
                                                            {
                                                                //get DirectShape
                                                                GeometryObject[] geosolid = new GeometryObject[] { intersection };
                                                                DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                                                                ds.SetShape(geosolid);

                                                                volumeOfIntersection += intersection.Volume;
                                                            }
                                                            else
                                                            {
                                                                foreach (Face f1 in solid1.Faces)
                                                                {
                                                                    foreach (Face f2 in solid2.Faces)
                                                                    {
                                                                        PlanarFace planarf1 = f1 as PlanarFace;
                                                                        PlanarFace planarf2 = f2 as PlanarFace;
                                                                        if (planarf1 != null && planarf2 != null)
                                                                        {
                                                                            if (Math.Round(planarf1.FaceNormal.Z,2) == 0)
                                                                            {
                                                                                FaceIntersectionFaceResult s1 = planarf1.Intersect(planarf2,out Curve curve);
                                                                                if (s1 == FaceIntersectionFaceResult.Intersecting)
                                                                                {
                                                                                    CurveLoop curves = CurveLoop.Create((IList<Curve>)curve);
                                                                                    areaOfIntersection += ExporterIFCUtils.ComputeAreaOfCurveLoops((IList<CurveLoop>)curves);                                                                                                                                                                                                                                               
                                                                                }                                                                                
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }                                                                                                
                                            }
                                        }
                                    }
                                }
                            }

                            areaOfIntersection = Math.Round(UnitUtils.Convert(areaOfIntersection, UnitTypeId.SquareFeet, UnitTypeId.SquareMeters), 3);
                            ele.LookupParameter("TestA").Set(areaOfIntersection);

                            volumeOfIntersection = Math.Round(volumeOfIntersection, 3);
                            ele.LookupParameter("Test Volume").Set(volumeOfIntersection);

                            //All
                            totalarea = Math.Round(UnitUtils.Convert(totalarea, UnitTypeId.SquareFeet, UnitTypeId.SquareMeters), 3);
                            //totalarea = Math.Floor(UnitUtils.Convert(totalarea, UnitTypeId.SquareFeet, UnitTypeId.SquareMeters));
                            ele.LookupParameter("Comments").Set(totalarea.ToString());
                            ele.LookupParameter("Mark").Set(totalface.ToString());

                        }                     

                    }
                    tran.Commit();
                }
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                MessageBox.Show(ex.ToString());
                return Result.Failed;               
            }             
        }                
    }    
}