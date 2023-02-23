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
                    new ElementCategoryFilter(BuiltInCategory.OST_StructuralFoundation),
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
                        //declare variable
                        int n = 0;
                        int totalface1 = 0;                        
                        double totalArea1 = 0.0;
                        double totalAreaTBot1 = 0.0;
                        double totalAreaIntersect = 0.0;
                        double areaOfIntersection = 0.0;
                        double areaOfIntersectionTBot = 0.0;
                        double volumeOfIntersection = 0.0;
                        double AreaFormworkSide = 0.0;

                        Options options = new Options();
                        options.DetailLevel = ViewDetailLevel.Fine;
                        options.ComputeReferences = false;

                        GeometryElement geomElem = ele.get_Geometry(options);
                        if (geomElem != null)
                        {
                            foreach (var obj1 in geomElem)
                            {
                                //Get side face
                                var solid1 = obj1 as Solid;
                                if (solid1 != null)
                                {
                                    foreach (Face face1 in solid1.Faces)
                                    {
                                        PlanarFace planarFace1 = face1 as PlanarFace;
                                        if (null != planarFace1)
                                        {
                                            //XYZ origin = planarFace.Origin;
                                            //XYZ normal = planarFace.FaceNormal;
                                            //XYZ normal = planarFace.ComputeNormal(new UV(planarFace.Origin.X, planarFace.Origin.Y));
                                            //XYZ vectorX = planarFace.XVector;
                                            //XYZ vectorY = planarFace.YVector;
                                            if (Math.Round(planarFace1.FaceNormal.Z, 2) == 0)
                                            {
                                                totalArea1 += face1.Area;
                                                totalface1++;
                                            }
                                            else
                                            {
                                                totalAreaTBot1 += face1.Area;                                                
                                            }
                                        }
                                    }
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

                                        foreach (var eleI in elesIntersect)
                                        {
                                            if (eleI.Id.IntegerValue != ele.Id.IntegerValue)
                                            {
                                                GeometryElement geomElemI = eleI.get_Geometry(options);
                                                if (geomElemI != null)
                                                {
                                                    foreach (var obj2 in geomElemI)
                                                    {
                                                        var solid2 = obj2 as Solid;
                                                        if (solid2 != null)
                                                        {
                                                            Solid union = BooleanOperationsUtils.ExecuteBooleanOperation(solid1, solid2, BooleanOperationsType.Union);
                                                            if (union.Volume != solid1.Volume && union.Volume != solid2.Volume && union != null)
                                                            {
                                                                double totalArea2 = 0.0;
                                                                double totalAreaTBot2 = 0.0;
                                                                double totalAreaUnion = 0.0;
                                                                double totalAreaUnionTBot = 0.0;
                                                                double volumeInt = 0.0;
                                                                
                                                                //Get area surface of solid2
                                                                foreach (Face face2 in solid2.Faces)
                                                                {
                                                                    PlanarFace planarFace2 = face2 as PlanarFace;
                                                                    if (null != planarFace2)
                                                                    {
                                                                        if (Math.Round(planarFace2.FaceNormal.Z, 2) == 0)
                                                                        {                                                                            
                                                                            totalArea2 += face2.Area;
                                                                        }
                                                                        else
                                                                        {
                                                                            totalAreaTBot2 += face2.Area;
                                                                        }
                                                                    }
                                                                }
                                                                foreach (Face faceUnion in union.Faces)
                                                                {
                                                                    PlanarFace planarUnion = faceUnion as PlanarFace;
                                                                    if (planarUnion != null)
                                                                    {
                                                                        if (Math.Round(planarUnion.FaceNormal.Z, 2) == 0)
                                                                        {                                                                            
                                                                            totalAreaUnion += faceUnion.Area;                                                                            
                                                                        }
                                                                        else
                                                                        {
                                                                            totalAreaUnionTBot += faceUnion.Area;
                                                                        }
                                                                    }
                                                                }
                                                                ////get DirectShape
                                                                //GeometryObject[] geosolid = new GeometryObject[] { union };
                                                                //DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                                                                //ds.SetShape(geosolid);
                                                                volumeInt += union.Volume;
                                                                volumeOfIntersection += volumeInt;
                                                                areaOfIntersection += (totalArea1 + totalArea2 - totalAreaUnion) / 2;
                                                                //areaOfIntersectionTBot += (totalAreaTBot1 + totalAreaTBot2 - totalAreaUnionTBot) / 4;

                                                                double totalAreaBot2 = 0.0;
                                                                foreach (Face face1 in solid1.Faces)
                                                                {
                                                                    PlanarFace planarFace1 = face1 as PlanarFace;
                                                                    if (null != planarFace1)
                                                                    {                                                                        
                                                                        if (Math.Round(planarFace1.FaceNormal.Z, 2) == -1)
                                                                        {                                                                            
                                                                            foreach (Face face2 in solid2.Faces)
                                                                            {
                                                                                PlanarFace planarFace2 = face2 as PlanarFace;
                                                                                if (null != planarFace2)
                                                                                {
                                                                                    if (Math.Round(planarFace2.FaceNormal.Z, 2) != 0)
                                                                                    {
                                                                                        if (planarFace1.Intersect(planarFace2, out Curve curve) == FaceIntersectionFaceResult.Intersecting)
                                                                                        {
                                                                                            totalAreaBot2 += (totalAreaTBot1 + totalAreaTBot2 - totalAreaUnionTBot) / 4;
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            totalAreaBot2 += totalAreaBot2;
                                                                                        }
                                                                                        totalAreaBot2 += totalAreaBot2;
                                                                                    }                                                                                    
                                                                                }
                                                                            }                                                                            
                                                                        }                                                                        
                                                                    }
                                                                }
                                                                areaOfIntersectionTBot += totalAreaBot2;
                                                                //areaOfIntersectionTBot += areaOfIntersectionTBot;
                                                                n++;
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
                        
                        ele.LookupParameter("TestV").Set(n.ToString());

                        //Get all area of all side face intersection
                        totalAreaIntersect += areaOfIntersection;
                        //areaOfIntersection = Math.Round(UnitUtils.Convert(areaOfIntersection, UnitTypeId.SquareFeet, UnitTypeId.SquareMeters), 3);
                        ele.LookupParameter("TestA").Set(totalAreaIntersect);

                        //Get all area of all bottom face intersection
                        areaOfIntersectionTBot += areaOfIntersectionTBot;
                        //areaOfIntersectionTBot = Math.Round(UnitUtils.Convert(areaOfIntersectionTBot, UnitTypeId.SquareFeet, UnitTypeId.SquareMeters), 3);
                        ele.LookupParameter("AreaBotIntersect").Set(areaOfIntersectionTBot);

                        volumeOfIntersection = Math.Round(volumeOfIntersection, 3);
                        ele.LookupParameter("Test Volume").Set(volumeOfIntersection);                        

                        //Calculator Area Formwork Side
                        AreaFormworkSide = totalArea1 - totalAreaIntersect;
                        ele.LookupParameter("Area Formwork Side").Set(AreaFormworkSide);

                        //All side face
                        totalArea1 = Math.Round(UnitUtils.Convert(totalArea1, UnitTypeId.SquareFeet, UnitTypeId.SquareMeters), 3);
                        //totalArea1 = Math.Floor(UnitUtils.Convert(totalArea1, UnitTypeId.SquareFeet, UnitTypeId.SquareMeters));
                        ele.LookupParameter("Comments").Set(totalArea1.ToString());
                        ele.LookupParameter("Mark").Set(totalface1.ToString());

                        //All Bottom face
                        totalAreaTBot1 = Math.Round(totalAreaTBot1, 3);
                        ele.LookupParameter("AreaBot").Set(totalAreaTBot1);
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