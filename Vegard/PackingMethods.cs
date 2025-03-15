using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhino;
using Rhino.DocObjects.Tables;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using static Rhino.Render.TextureGraphInfo;

namespace MeshFromPointCloud
{
    public static class PackingMethods
    {
        public static Rectangle3d CrossSectionToVolume(Rectangle3d crossSection, Brep brep,
            Point3d lineStart, Point3d lineEnd)
        {

            var line = new Line(lineStart, lineEnd);
            double dist = 0;

            var rectangle = new Rectangle3d();

            while (dist < line.Length)
            {
                var evalPlane = crossSection.Plane;
                var evalPt = line.PointAtLength(dist);
                var evalLine = new Line(lineStart, evalPt);
                evalPlane.Translate(evalLine.Direction);
                var lastRectangle = rectangle;
                rectangle = new Rectangle3d(evalPlane, crossSection.X, crossSection.Y);

                Intersection.BrepPlane(brep, evalPlane, Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance, out Curve[] intCrvs, out Point3d[] intPts);
                var perim = intCrvs[0];

                
                var perimSubCurves = perim.GetSubCurves();
                var perimLines = new List<Line>();
                foreach (var subCurve in perimSubCurves)
                    perimLines.Add(new Line(subCurve.PointAtStart, subCurve.PointAtEnd));

                var rectEdges = rectangle.ToPolyline().GetSegments().ToList();
                
                
                
                foreach (var rectEdge in rectEdges)
                {
                    var intEvents = Intersection.CurveLine(perim, rectEdge, Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance,
                        Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
                    if (intEvents.Count > 0)
                    {
                        foreach (var intersect in intEvents)
                        {
                            var a = intersect.ParameterB;

                            if (a >= 0.0 && a <= 1.0)
                            {
                                return lastRectangle;
                            }
                        }
                    }
                }
                
                dist += Math.Min(10, line.Length - dist);
            }
            return rectangle;
        }

        public static Rectangle3d BiggestCrossSection(Curve perim)
        {

            var angle = Math.PI;

            perim.TryGetPlane(out Plane plane);
            var perimAreaMP = AreaMassProperties.Compute(perim);
            var perimSubCurves = perim.GetSubCurves();
            var perimLines = new List<Line>();
            foreach (var subCurve in perimSubCurves)
                perimLines.Add(new Line(subCurve.PointAtStart, subCurve.PointAtEnd));

            var centerPlane = new Plane(perimAreaMP.Centroid, plane.Normal);
            var rectangles = new List<Rectangle3d>();

            var rect = new Rectangle3d();
            double deg = 0;
            while (deg < angle)
            {
                var rotatingPlane = centerPlane;
                rotatingPlane.Rotate(deg, centerPlane.ZAxis);

                //var xNegStart = 100; var xPosStart = 100; var yNegStart = 100; var yPosStart = 100;
                double startValue = 10;
                var rectangleDimensions = new List<double>()
                { startValue, startValue, startValue, startValue };

                var intersections = new List<int>() { 1, 1, 1, 1 };


                // should only check one relevant edge at a time
                while (intersections.Sum() > 0)
                {
                    for (int i = 0; i < rectangleDimensions.Count; i++)
                    {
                        if (intersections[i] != 0)
                        {
                            rectangleDimensions[i] += 1;
                            var yNeg = rectangleDimensions[0];
                            var xPos = rectangleDimensions[2];
                            var yPos = rectangleDimensions[1];
                            var xNeg = rectangleDimensions[3];

                            var xInterval = new Interval(-xNeg, xPos);
                            var yInterval = new Interval(-yNeg, yPos);

                            rect = new Rectangle3d(rotatingPlane, xInterval, yInterval);

                            var rectEdges = rect.ToPolyline().GetSegments().ToList();

                            //var rectEdge = rectEdges[i];
                            var rectEdgeParams = new List<double>();
                            var perimParams = new List<double>();

                            foreach (var rectEdge in rectEdges)
                            {
                                var intEvents = Intersection.CurveLine(perim, rectEdge, 0.0001, 0.0001);
                                if (intEvents.Count > 0)
                                {
                                    foreach (var intersect in intEvents)
                                    {
                                        var a = intersect.ParameterB;

                                        if (a >= 0.0 && a <= 1.0)
                                        {
                                            rectEdgeParams.Add(a);
                                            break;
                                        }
                                    }

                                }

                                if (rectEdgeParams.Count > 0)
                                {
                                    intersections[i] = 0;
                                    rectangleDimensions[i] -= 1.1;
                                    break;
                                }
                            }


                        }
                    }
                }
                rectangles.Add(rect);
                deg += 0.01;
            }

            var biggestRectangle = new Rectangle3d();
            double biggestArea = 0;
            foreach (var rectangle in rectangles)
            {
                var rectangleMP = AreaMassProperties.Compute(rectangle.ToNurbsCurve());
                if (rectangleMP.Area > biggestArea)
                {
                    biggestArea = rectangleMP.Area;
                    biggestRectangle = rectangle;
                }
            }
            return biggestRectangle;
        }
    }
}
