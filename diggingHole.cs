using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfControlLibrary1
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    class diggingHole : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var doc = commandData.Application.ActiveUIDocument.Document;
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
                var solids = GetTargetSolids(foundation);
                var solid = solids.OrderByDescending(s => s.Volume).FirstOrDefault();
                var botFace = solid.Faces.Cast<Face>().OfType<PlanarFace>().FirstOrDefault(f => Math.Round(f.FaceNormal.Z, 2) == -1);
                var topFace = solid.Faces.Cast<Face>().OfType<PlanarFace>().FirstOrDefault(f => Math.Round(f.FaceNormal.Z, 2) == 1);
                var offsetFace = CurveLoop.CreateViaOffset(topFace.GetEdgesAsCurveLoops().FirstOrDefault(),5,topFace.FaceNormal);

                var fdoc = commandData.Application.Application.NewFamilyDocument(@"C:\ProgramData\Autodesk\RVT 2020\Family Templates\English_I\Generic Model.rft");
                using (Transaction tran = new Transaction(fdoc, "new Blend"))
                {
                    tran.Start();
                    var plan = Plane.CreateByNormalAndOrigin(botFace.FaceNormal, botFace.Origin);
                    var sk = SketchPlane.Create(fdoc, plan);
                    var top = ConvertLoopToArray(offsetFace);
                    var baseface = ConvertLoopToArray(botFace.GetEdgesAsCurveLoops().FirstOrDefault());
                    fdoc.FamilyCreate.NewBlend(false,top,baseface, null);
                    //CreateBlend(fdoc, null);
                    tran.Commit();
                }
                fdoc.SaveAs($"{Path.GetTempPath()}{foundation.Id.ToString()}-{Guid.NewGuid().ToString()}.rfa");
                Family family = fdoc.LoadFamily(doc);
                fdoc.Close();
                using (Transaction tran = new Transaction(doc,"new void"))
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
        private Blend CreateBlend(Document familyDocument, SketchPlane sketchPlane)
        {
            Blend blend = null;

            if (true == familyDocument.IsFamilyDocument)
            {
                // Define top and base profiles for the blend
                CurveArray topProfile = new CurveArray();
                CurveArray baseProfile = new CurveArray();

                // create rectangular base profile
                XYZ p00 = XYZ.Zero;
                XYZ p01 = new XYZ(10, 0, 0);
                XYZ p02 = new XYZ(10, 10, 0);
                XYZ p03 = new XYZ(0, 10, 0);
                Line line01 = Line.CreateBound(p00, p01);
                Line line02 = Line.CreateBound(p01, p02);
                Line line03 = Line.CreateBound(p02, p03);
                Line line04 = Line.CreateBound(p03, p00);

                baseProfile.Append(line01);
                baseProfile.Append(line02);
                baseProfile.Append(line03);
                baseProfile.Append(line04);

                // create rectangular top profile
                XYZ p10 = new XYZ(5, 2, 10);
                XYZ p11 = new XYZ(8, 5, 10);
                XYZ p12 = new XYZ(5, 8, 10);
                XYZ p13 = new XYZ(2, 5, 10);
                Line line11 = Line.CreateBound(p10, p11);
                Line line12 = Line.CreateBound(p11, p12);
                Line line13 = Line.CreateBound(p12, p13);
                Line line14 = Line.CreateBound(p13, p10);

                topProfile.Append(line11);
                topProfile.Append(line12);
                topProfile.Append(line13);
                topProfile.Append(line14);

                // now create solid rectangular blend
                blend = familyDocument.FamilyCreate.NewBlend(true, topProfile, baseProfile, sketchPlane);

                if (null != blend)
                {
                    // move to proper place
                    XYZ transPoint1 = new XYZ(0, 11, 0);
                    ElementTransformUtils.MoveElement(familyDocument, blend.Id, transPoint1);
                }
                else
                {
                    throw new Exception("Create new Blend failed.");
                }
            }
            else
            {
                throw new Exception("Please open a Family document before invoking this command.");
            }

            return blend;
        }
        private CurveArray ConvertLoopToArray(CurveLoop loop)
        {
            CurveArray a = new CurveArray();
            if (loop.IsCounterclockwise(XYZ.BasisZ))
            {
                foreach (Curve c in loop)
                {
                    a.Append(c);
                }
            }
            else
            {
                foreach (Curve c in loop)
                {
                    a.Insert(c.CreateReversed(),0);
                }
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
