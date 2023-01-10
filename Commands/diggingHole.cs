using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfControlLibrary1.Extensions;
using WpfControlLibrary1.Filters;
using WpfControlLibrary1.Windows;
using WpfControlLibrary1.Windows.ViewModels;

namespace WpfControlLibrary1
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    class diggingHole : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uidoc = commandData.Application.ActiveUIDocument;
            var doc = uidoc.Document;

            //Get document file Link
            //var link = new FilteredElementCollector(doc).OfType<RevitLinkInstance>().FirstOrDefault(l => l.Name == "test.rvt");
            //var linkdoc = link.GetLinkDocument();
            var vm = new DiggingHoleViewModel();
            var window = new DiggingHole(vm);
            window.ShowDialog();


            if (window.DialogResult == true)
            {
                var topoffset = UnitUtils.ConvertToInternalUnits(double.Parse(vm.TopOffset), UnitTypeId.Millimeters);
                var botoffset = UnitUtils.ConvertToInternalUnits(double.Parse(vm.BotOffset), UnitTypeId.Millimeters);
                var filter = new FoundationFilter();
                var foundations = uidoc.Selection.PickObjects(Autodesk.Revit.UI.Selection.ObjectType.Element, filter)
                    .Select(r => doc.GetElement(r.ElementId))
                    .ToList();

                var results = new List<Element>();

                foreach (var foundation in foundations)
                {
                    //var otp = new Options();
                    //otp.DetailLevel = ViewDetailLevel.Fine;
                    //var geoElement = foundation.get_Geometry(otp);
                    //var instance = geoElement.Cast<GeometryObject>().OfType<GeometryInstance>().Select(i => i.GetInstanceGeometry()).ToList();
                    //var solids = geoElement.Cast<GeometryObject>().Concat(instance).OfType<Solid>().Where(s => s.Volume > 0 && s.Faces.Size > 0).ToList();

                    var solids = foundation.GetSolids();
                    var solid = solids.OrderByDescending(s => s.Volume).FirstOrDefault();
                    var botFace = solid.Faces.Cast<Face>().OfType<PlanarFace>().FirstOrDefault(f => Math.Round(f.FaceNormal.Z, 2) == -1);
                    var topFace = solid.Faces.Cast<Face>().OfType<PlanarFace>().FirstOrDefault(f => Math.Round(f.FaceNormal.Z, 2) == 1);
                    var offsettopFace = CurveLoop.CreateViaOffset(topFace.GetEdgesAsCurveLoops().FirstOrDefault(), topoffset, topFace.FaceNormal);

                    var offsetbotFace = CurveLoop.CreateViaOffset(botFace.GetEdgesAsCurveLoops().FirstOrDefault(), botoffset, botFace.FaceNormal);
                    offsetbotFace.Transform(Transform.CreateTranslation(new XYZ(0, 0, UnitUtils.ConvertToInternalUnits(-100, UnitTypeId.Millimeters))));
                    var fdoc = commandData.Application.Application.NewFamilyDocument(@"C:\ProgramData\Autodesk\RVT 2020\Family Templates\English\Metric Generic Model.rft");
                    using (Transaction tran = new Transaction(fdoc, "new Blend"))
                    {
                        tran.Start();
                        var plan = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, botFace.Origin);
                        var sketchPlane = SketchPlane.Create(fdoc, plan);
                        var top = ConvertLoopToArray(offsettopFace);
                        var baseface = ConvertLoopToArray(offsetbotFace);
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
                        var ele = doc.Create.NewFamilyInstance(XYZ.Zero, symbol, Autodesk.Revit.DB.Structure.StructuralType.Footing);
                        results.Add(ele);
                        tran.Commit();
                    }
                }
                foreach (var ele in results)
                {
                    var boundingBox = ele.get_BoundingBox(null);

                    var solids = ele.GetSolids();

                    var outline = new Outline(boundingBox.Min, boundingBox.Max);
                    var bbfilter = new BoundingBoxIntersectsFilter(outline);

                    var collectors = new FilteredElementCollector(doc, results.Select(e => e.Id).ToList())
                        .WhereElementIsNotElementType()
                        .WherePasses(bbfilter)
                        .ToElements();

                    var solid = ele.GetSolids().FirstOrDefault();
                    var firstZ = solid.Faces.Cast<Face>().OfType<PlanarFace>().FirstOrDefault(f => Math.Round(f.FaceNormal.Z, 2) == -1).Origin.Z;

                    foreach (var item in collectors)
                    {
                        var secondSolid = item.GetSolids().FirstOrDefault();
                        var secondZ = secondSolid.Faces.Cast<Face>().OfType<PlanarFace>().FirstOrDefault(f => Math.Round(f.FaceNormal.Z, 2) == -1).Origin.Z;
                        if (Math.Round(firstZ, 2) == Math.Round(secondZ, 2))
                            solid = BooleanOperationsUtils.ExecuteBooleanOperation(solid, item.GetSolids().FirstOrDefault(), BooleanOperationsType.Union);
                    }
                    var botFace = solid.Faces.Cast<Face>().OfType<PlanarFace>().FirstOrDefault(f => Math.Round(f.FaceNormal.Z, 2) == -1);
                    var topFace = solid.Faces.Cast<Face>().OfType<PlanarFace>().FirstOrDefault(f => Math.Round(f.FaceNormal.Z, 2) == 1);
                    var offsettopFace = CurveLoop.CreateViaOffset(topFace.GetEdgesAsCurveLoops().FirstOrDefault(), 0, topFace.FaceNormal);
                    var offsetbotFace = CurveLoop.CreateViaOffset(topFace.GetEdgesAsCurveLoops().FirstOrDefault(), - topoffset + botoffset, topFace.FaceNormal);
                    offsetbotFace.Transform(Transform.CreateTranslation(new XYZ(0, 0, -topFace.Origin.Z + botFace.Origin.Z)));
                    var fdoc = commandData.Application.Application.NewFamilyDocument(@"C:\ProgramData\Autodesk\RVT 2020\Family Templates\English\Metric Generic Model.rft");
                    using (Transaction tran = new Transaction(fdoc, "new Blend"))
                    {
                        tran.Start();
                        var plan = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, botFace.Origin);
                        var sketchPlane = SketchPlane.Create(fdoc, plan);
                        var top = ConvertLoopToArray(offsettopFace);
                        var baseface = ConvertLoopToArray(offsetbotFace);
                        var blend = fdoc.FamilyCreate.NewBlend(true, top, baseface, sketchPlane);
                        blend.LookupParameter("Second End").Set(Math.Abs(blend.LookupParameter("Second End").AsDouble()));
                        //CreateBlend(fdoc, null);
                        tran.Commit();
                    }
                    fdoc.SaveAs($"{Path.GetTempPath()}{ele.Id.ToString()}-{Guid.NewGuid().ToString()}.rfa");
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
    }
}
