using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace WpfControlLibrary1
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    class diggingFoundation : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uidoc = commandData.Application.ActiveUIDocument;
            var doc = uidoc.Document;

            //Get document file Link
            //var link = new FilteredElementCollector(doc).OfType<RevitLinkInstance>().FirstOrDefault(l => l.Name == "test.rvt");
            //var linkdoc = link.GetLinkDocument();

            var foundations = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_StructuralFoundation)
                .WhereElementIsNotElementType()
                .ToElements();
            foreach (var foundation in foundations)
            {
                //var otp = new Options();
                //otp.DetailLevel = ViewDetailLevel.Fine;
                //var geoElement = foundation.get_Geometry(otp);
                //var instance = geoElement.Cast<GeometryObject>().OfType<GeometryInstance>().Select(i => i.GetInstanceGeometry()).ToList();
                //var solids = geoElement.Cast<GeometryObject>().Concat(instance).OfType<Solid>().Where(s => s.Volume > 0 && s.Faces.Size > 0).ToList();

                //Declare variable
                double offsetTop = 1000;
                offsetTop = UnitUtils.Convert(offsetTop, DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET);
                double offsetBot = 100;
                offsetBot = UnitUtils.Convert(offsetBot, DisplayUnitType.DUT_MILLIMETERS, DisplayUnitType.DUT_DECIMAL_FEET);

                var solids = GetTargetSolids(foundation);
                var solid = solids.OrderByDescending(s => s.Volume).FirstOrDefault();

                var botFace = solid.Faces.Cast<Face>().OfType<PlanarFace>().FirstOrDefault(f => Math.Round(f.FaceNormal.Z, 2) == -1);                
                var offsetbotFace = CurveLoop.CreateViaOffset(botFace.GetEdgesAsCurveLoops().FirstOrDefault(), offsetBot, botFace.FaceNormal);

                var topFace = solid.Faces.Cast<Face>().OfType<PlanarFace>().FirstOrDefault(f => Math.Round(f.FaceNormal.Z, 2) == 1);
                var offsettopFace = CurveLoop.CreateViaOffset(topFace.GetEdgesAsCurveLoops().FirstOrDefault(), offsetTop, topFace.FaceNormal);

                var fdoc = commandData.Application.Application.NewFamilyDocument(@"C:\ProgramData\Autodesk\RVT 2020\Family Templates\English\Metric Generic Model.rft");
                using (Transaction tran = new Transaction(fdoc, "new Blend"))
                {
                    tran.Start();
                    var plan = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, botFace.Origin);
                    var sketchPlane = SketchPlane.Create(fdoc, plan);
                    var top = ConvertLoopToArray(offsettopFace);
                    var baseface = ConvertLoopToArray(botFace.GetEdgesAsCurveLoops().FirstOrDefault());
                    var blend = fdoc.FamilyCreate.NewBlend(true, top, baseface, sketchPlane);
                    blend.LookupParameter("Second End").Set(Math.Abs(blend.LookupParameter("Second End").AsDouble()));
                    //CreateBlend(fdoc, null);
                    tran.Commit();
                }
                fdoc.SaveAs($"{Path.GetTempPath()}{foundation.Id.ToString()}-{Guid.NewGuid().ToString()}.rfa");
                Family family = fdoc.LoadFamily(doc);
                fdoc.Close();
                using (Transaction tran = new Transaction(doc, "new void"))
                {
                    tran.Start();
                    //offsetFace.ToList().ForEach(f => doc.Create.NewModelCurve(f, SketchPlane.Create(doc, Plane.CreateByThreePoints(f.GetEndPoint(0), f.GetEndPoint(1), new XYZ(0, 0, 1)))));
                    //botFace.GetEdgesAsCurveLoops().FirstOrDefault().ToList().ForEach(f => doc.Create.NewModelCurve(f, SketchPlane.Create(doc, Plane.CreateByThreePoints(f.GetEndPoint(0), f.GetEndPoint(1), new XYZ(0, 0, 1)))));
                    var symbol = doc.GetElement(family.GetFamilySymbolIds().FirstOrDefault()) as FamilySymbol;
                    symbol.Activate();
                    doc.Create.NewFamilyInstance(XYZ.Zero, symbol, Autodesk.Revit.DB.Structure.StructuralType.Footing);
                    tran.Commit();
                }
            }
            return Result.Succeeded;
        }
        private CurveArray ConvertLoopToArray(CurveLoop loop)
        {
            CurveArray a = new CurveArray();
            if (loop.IsCounterclockwise(XYZ.BasisZ))
            {
                loop.Flip();
            }
            foreach (Curve c in loop)
            {
                a.Append(c);
            }

            return a;
        }
        private static IList<Solid> GetTargetSolids(Element element)
        {
            List<Solid> solids = new List<Solid>();

            Options options = new Options();
            options.DetailLevel = ViewDetailLevel.Fine;
            GeometryElement geomElem = element.get_Geometry(options);
            foreach (GeometryObject geomObj in geomElem)
            {
                if (geomObj is Solid)
                {
                    Solid solid = (Solid)geomObj;
                    if (solid.Faces.Size > 0 && solid.Volume > 0.0)
                    {
                        solids.Add(solid);
                    }
                    // Single-level recursive check of instances. If viable solids are more than
                    // one level deep, this example ignores them.
                }
                else if (geomObj is GeometryInstance)
                {
                    GeometryInstance geomInst = (GeometryInstance)geomObj;
                    GeometryElement instGeomElem = geomInst.GetInstanceGeometry();
                    foreach (GeometryObject instGeomObj in instGeomElem)
                    {
                        if (instGeomObj is Solid)
                        {
                            Solid solid = (Solid)instGeomObj;
                            if (solid.Faces.Size > 0 && solid.Volume > 0.0)
                            {
                                solids.Add(solid);
                            }
                        }
                    }
                }
            }
            return solids;
        }
    }
}
