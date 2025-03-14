using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace Masterv2.Vegard
{
    public static class DeletedCode
    {

        // Given three collinear points p, q, r, the function checks if 
        // point q lies on line segment 'pr' 
        static bool OnSegment(Point3d p, Point3d q, Point3d r)
        {
            if (q.X <= Math.Max(p.X, r.X) && q.X >= Math.Min(p.X, r.X) &&
                q.Y <= Math.Max(p.Y, r.Y) && q.Y >= Math.Min(p.Y, r.Y))
                return true;

            return false;
        }

        public static int Orientation(Point3d p, Point3d q, Point3d r)
        {
            double val = (q.Y - p.Y) * (r.X - q.X) -
                (q.X - p.X) * (r.Y - q.Y);

            if (Convert.ToInt32(val) == 0) return 0; //colinear


            return Convert.ToInt32(val) > 0 ? 1 : 2; // clock or counterclock wise

        }

        // The main function that returns true if line segment 'p1q1' 
        // and 'p2q2' intersect. 
        static bool DoIntersect(Point3d p1, Point3d q1, Point3d p2, Point3d q2)
        {
            // Find the four orientations needed for general and 
            // special cases 
            int o1 = Orientation(p1, q1, p2);
            int o2 = Orientation(p1, q1, q2);
            int o3 = Orientation(p2, q2, p1);
            int o4 = Orientation(p2, q2, q1);

            // General case 
            if (o1 != o2 && o3 != o4)
                return true;

            // Special Cases 
            // p1, q1 and p2 are collinear and p2 lies on segment p1q1 
            if (o1 == 0 && OnSegment(p1, p2, q1)) return true;

            // p1, q1 and q2 are collinear and q2 lies on segment p1q1 
            if (o2 == 0 && OnSegment(p1, q2, q1)) return true;

            // p2, q2 and p1 are collinear and p1 lies on segment p2q2 
            if (o3 == 0 && OnSegment(p2, p1, q2)) return true;

            // p2, q2 and q1 are collinear and q1 lies on segment p2q2 
            if (o4 == 0 && OnSegment(p2, q1, q2)) return true;

            return false; // Doesn't fall in any of the above cases 
        }
        public static void PtBetween_1(List<Point3d> pts, Point3d p1, Point3d p2, double maxDistance, List<Point3d> hullPts)
        {
            int ind = -1;
            var longLine = new Line(p1, p2);
            var distToLine = maxDistance;

            for (int i = 0; i < pts.Count; i++)
            {
                var pt = pts[i];
                var ptLine = longLine.ClosestPoint(pt, true);


                if (ptLine.DistanceTo(pt) < distToLine && ptLine != p1 && ptLine != p2)
                {
                    distToLine = ptLine.DistanceTo(pt);
                    ind = i;
                }
            }

            if (ind == -1)
                return;

            hullPts.Add(pts[ind]);
            PtBetween_1(pts, pts[ind], p1, maxDistance, hullPts);
            PtBetween_1(pts, pts[ind], p2, maxDistance, hullPts);

        }

        public static void PtBetween_2(Line longLine, List<Point3d> pts, double lenTol, List<Point3d> hullPts)
        {
            int ind = -1;
            var p1 = longLine.PointAt(0.0);
            var p2 = longLine.PointAt(1.0);
            var shortestDistance = lenTol;

            if (longLine.Length > lenTol)
            {
                for (int i = 0; i < pts.Count; i++)
                {
                    var pt = pts[i];
                    var ptLine = longLine.ClosestPoint(pt, true);

                    if (ptLine.DistanceTo(pt) < shortestDistance && ptLine != p1 && ptLine != p2)
                    {
                        shortestDistance = ptLine.DistanceTo(pt);
                        ind = i;
                    }
                }
            }

            if (ind == -1)
                return;

            hullPts.Add(pts[ind]);
            PtBetween_2(new Line(p1, pts[ind]), pts, lenTol, hullPts);
            PtBetween_2(new Line(p2, pts[ind]), pts, lenTol, hullPts);

        }

        public static void QuickHull_2(List<Point3d> pts, int n, Point3d p1, Point3d p2, int side, double lenTol, List<Point3d> hullPts)
        {
            int ind = -1;
            double maxDist = 0;

            for (int i = 0; i < n; i++)
            {
                double dist = ConvexHullMethods.DistLine(p1, p2, pts[i]);
                if (ConvexHullMethods.SideOfLine(p1, p2, pts[i]) == side && dist > maxDist)
                {
                    ind = i;
                    maxDist = dist;
                }
            }
            if (ind == -1)
            {
                if (hullPts.Contains(p1) == false)
                    hullPts.Add(p1);


                if (hullPts.Contains(p2) == false)
                    hullPts.Add(p2);

                return;
            }

            QuickHull_2(pts, n, pts[ind], p1, -ConvexHullMethods.SideOfLine(pts[ind], p1, p2), lenTol, hullPts);
            QuickHull_2(pts, n, pts[ind], p2, -ConvexHullMethods.SideOfLine(pts[ind], p2, p1), lenTol, hullPts);
        }




        public static string ConvexHullWrapping(List<Point3d> pts, int n, List<Point3d> hullPts, double maxLength)
        {
            int l = 0;
            for (int i = 0; i < n; i++)
                if (pts[i].X < pts[l].X)
                    l = i;


            int p = l;
            int q;

            do
            {
                // Add current point to result
                hullPts.Add(pts[p]);

                // Search for a point 'q' such that 
                // orientation(p, q, x) is counterclockwise 
                // for all points 'x'. The idea is to keep 
                // track of last visited most counterclock-
                // wise point in q. If any point 'i' is more 
                // counterclock-wise than q, then update q.
                q = (p + 1) % n;

                for (int i = 0; i < n; i++)
                {
                    // If i is more counterclockwise than 
                    // current q, then update q
                    if (p > pts.Count - 1)
                    {
                        return "p is out of range";
                    }


                    var dist = pts[p].DistanceTo(pts[i]);
                    if (Orientation(pts[p], pts[i], pts[q]) == 2)
                        q = i;
                }

                if (pts[p].DistanceTo(pts[q]) > maxLength)
                {
                    return "Could not find any suitable points within length requirement";
                }

                // Now q is the most counterclockwise with
                // respect to p. Set p as q for next iteration, 
                // so that q is added to result 'hull'
                p = q;

            } while (p != l); // While we don't come to first 
                              // point
            return "This works atleast";

        }

        public static void PtBetween_1Slow(List<Point3d> pts, Point3d p1, Point3d p2, double maxLength,
            List<Point3d> ptsContainer, List<Line> shortestLines, int maxIt)
        {

            var longLine = new Line(p1, p2); // make line
            var ptsOnLongLine = new List<Point3d>(); // hold the projected points on line            
            var orthoLines = new List<Line>(); // hold orthogonal lines

            Point3d newPoint; // the new point on the line between p1 and p2

            if (longLine.Length > maxLength)
            {
                for (int i = 0; i < pts.Count; i++)
                {
                    var pt = pts[i];
                    var t = longLine.ClosestParameter(pt); // find the closest pt on line for all pts                    
                    var tol = 1e-9;

                    if (t > tol && t < 1 - tol) // only use the orthogonal points
                    {
                        ptsOnLongLine.Add(longLine.PointAt(t));
                        orthoLines.Add(new Line(longLine.PointAt(t), pt)); // make lines from ptOnLongLine (pointAt(0)) and pt (PointAt(1))
                    }

                }

                var orthoLinesSorted = orthoLines.OrderBy(l => l.Length).ToList(); // sort lines based on length                   



                shortestLines.Add(orthoLinesSorted[0]);
                var closestPoint = orthoLinesSorted[0].PointAt(1.0); // get pt from shortest line
                newPoint = orthoLinesSorted[0].PointAt(1.0); // get pt from shortest line
                ptsContainer.Add(closestPoint);
            }

            else
                return;

            maxIt += 1;
            PtBetween_1Slow(pts, newPoint, p1, maxLength, ptsContainer, shortestLines, maxIt); // check if part 1 of new line is too long
            PtBetween_1Slow(pts, newPoint, p2, maxLength, ptsContainer, shortestLines, maxIt); // check if part 2 of new line is too long          

        }

        public static void ConvexHullIterative(List<Point3d> pts, Point3d pCenter, Point3d pSeam, Point3d pFirst,
            double maxLength, List<Point3d> ptsOut,
            List<Curve> circles, List<Point3d> pCenters, GH_Structure<GH_Point> ghPts, int g)
        {
            var path = new GH_Path(g);
            var circle = new Circle(pCenter, maxLength).ToNurbsCurve();
            circles.Add(circle);
            pCenters.Add(pCenter);
            var ptsInside = new List<Point3d>();

            circle.ClosestPoint(pSeam, out double t);
            circle.ChangeClosedCurveSeam(t);

            foreach (var pt in pts)
            {
                var dist = pCenter.DistanceTo(pt);
                if (dist < maxLength)
                    ptsInside.Add(pt);
            }

            var ptsCurveSorted = Methods.SortPointsAlongCurve(circle, ptsInside, true);

            foreach (var pt in ptsCurveSorted)
                ghPts.Append(new GH_Point(pt), path);
            g += 1;
            var pNext = ptsCurveSorted[1];

            ptsOut.Add(pNext);

            pSeam = pCenter;
            pCenter = pNext;


            if (pNext == pFirst || ptsOut.Count == 100)
                return;

            ConvexHullIterative(pts, pCenter, pSeam, pFirst, maxLength, ptsOut, circles, pCenters, ghPts, g);
        }

        /*
            for (int i = 0; i < rectangleDimensions.Count; i++)
            {                
                var intersect = true;               

                while (intersect)
                {
                    rectangleDimensions[i] += 1;
                    var xNeg = rectangleDimensions[0];
                    var xPos = rectangleDimensions[1];
                    var yNeg = rectangleDimensions[2];
                    var yPos = rectangleDimensions[3];

                    var xInterval = new Interval(-xNeg, xPos);
                    var yInterval = new Interval(-yNeg, yPos);

                    rect = new Rectangle3d(centerPlane, xInterval, yInterval);
                    
                    var rectEdges = rect.ToPolyline().GetSegments().ToList();

                    // should only check one relevant edge at a time
                    var edgeInteger = (i + 3) % 4;
                    var rectEdge = rectEdges[edgeInteger];
                    test.Add(edgeInteger);

                    var curveIntersections = Intersection.CurveLine(perim, rectEdge, 0.0001, 0.0001);
                    if (curveIntersections.Count != 0) // get stack overflow prob because parallell lines
                        //maybe this check will fix it or add check before to see if they diagonal.
                        //they might be parallell and skewed so check if
                        // p1.X1 - p1.X2 == p2.X2 - p2.X2 or smth
                    {
                        foreach (var intersectionLocation in curveIntersections)
                        {
                            var intersectionParameter = intersectionLocation.ParameterB;
                            if (intersectionParameter >= 0.0 && intersectionParameter <= 1.0)
                            {
                                intPts.Add(intersectionLocation.PointB);
                                intersect = false;
                                break;
                            }
                        }
                    }                 
                   
                }
                rectangles.Add(rect);

            }  
                */

        /*
                do
                {
                    // should only check one relevant edge at a time                
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

                            var rectEdge = rectEdges[i];
                            var rectEdgeParams = new List<double>();
                            var perimParams = new List<double>();
                            foreach (var perimEdge in perimLines)
                            {
                                if (!Intersection.LineLine(rectEdge, perimEdge, out double a, out double b))
                                    continue; // Only process valid intersections

                                if (a >= 0.0 && a <= 1.0)
                                {
                                    rectEdgeParams.Add(a);
                                    break;
                                }

                                if (b >= 0.0 && b <= 1.0)
                                    perimParams.Add(b);
                            }

                            if (perimParams.Count == 0)
                            {
                                intersections[i] = 0;
                                rectangleDimensions[i] -= 1.1;
                                break;
                            }

                            if (rectEdgeParams.Count > 0)
                            {
                                intPts.Add(rectEdge.PointAt(rectEdgeParams[0]));
                                intersections[i] = 0;
                                rectangleDimensions[i] -= 1.1;
                                break;
                            }

                        }
                    }
                } while (intersections.Sum() > 1);
                */
    }
}
