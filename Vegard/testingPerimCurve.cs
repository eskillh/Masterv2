using System;
using System.Collections.Generic;
using System.Linq;
using Ed.Eto;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using Rhino.Geometry;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

namespace MeshFromPointCloud
{
    public class testingPerimCurve : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the testingPerimCurve class.
        /// </summary>
        public testingPerimCurve()
          : base("testingPerimCurve", "Nickname",
              "Description",
              "Master", "PointCloud")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Points2d", "pts", "", GH_ParamAccess.tree);
            pManager.AddNumberParameter("tolDist", "", "", GH_ParamAccess.item, 3);
            pManager.AddNumberParameter("segLength", "", "", GH_ParamAccess.item, 10);
            pManager.AddNumberParameter("distBtwPtsCrack", "", "", GH_ParamAccess.item, 5);
            pManager.AddIntegerParameter("rebuildPts", "", "", GH_ParamAccess.item, 100);
            pManager.AddNumberParameter("deleteShortSeg", "", "", GH_ParamAccess.item, 2);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("crossSectionCurves", "", "", GH_ParamAccess.list);
            pManager.AddCurveParameter("getCrossSectionCurves", "", "", GH_ParamAccess.list);
            
            pManager.AddPointParameter("convexHullPts", "", "", GH_ParamAccess.tree);
            pManager.AddCurveParameter("crossSectionCurve", "", "", GH_ParamAccess.tree);            
            pManager.AddPointParameter("crossSectionPts", "", "", GH_ParamAccess.tree);
            pManager.AddCurveParameter("CH", "", "", GH_ParamAccess.tree);
            pManager.AddLineParameter("cracks", "", "", GH_ParamAccess.tree);
            
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<GH_Point> ghPtsTree;
            double inp_dist = 0;
            double inp_segLength = 0;
            double inp_distBtwPtsCrack = 0;
            int rebuildPts = 0;
            double deleteShortSeg = 0;

            DA.GetDataTree(0, out ghPtsTree);
            DA.GetData(1, ref inp_dist);
            DA.GetData(2, ref inp_segLength);
            DA.GetData(3, ref inp_distBtwPtsCrack);
            DA.GetData(4, ref rebuildPts);
            DA.GetData(5, ref deleteShortSeg);



            var crossSectionCurves = new List<Curve>();
            var getCrossSectionCurves = new List<Curve>();            

            var ghPtsSorted = new GH_Structure<GH_Point>();
            var ghCrossSectionCurve = new GH_Structure<GH_Curve>();
            var ghNewPtsSorted = new GH_Structure<GH_Point>();
            var ghConvexHull = new GH_Structure<GH_Curve>();
            var ghCracks = new GH_Structure<GH_Curve>();

            for (int g = 0; g < ghPtsTree.PathCount; g++)
            {                
                var path0 = ghPtsTree.Paths[g];
                var ghPts = ghPtsTree.Branches[g];
                var pts = new List<Point3d>();
                var pts2d = new List<Point2d>();

                foreach (var ghPt in ghPts)
                {
                    var pt = new Point3d(ghPt.Value);
                    pts.Add(pt);
                    pts2d.Add(new Point2d(pt.X, pt.Y)); // make 2d list for convex hull
                }


                var convexHull = PolylineCurve.CreateConvexHull2d(pts2d.ToArray(), out int[] hullIndices); // get convex hull (CH)

                var ptsDistance = new List<double>(); // list to store distance between pt and the same pt on CH
                var parameters = new List<double>(); // list to store parameters of pts on CH
                ghConvexHull.Append(new GH_Curve(convexHull), path0);

                // find closest pt on CH and the distance between them
                foreach (var pt in pts)
                {
                    convexHull.ClosestPoint(pt, out double t); // get parameter of closest point on CH
                    var ptPline = new Point3d(convexHull.PointAt(t)); // make point on CH                
                    ptsDistance.Add(pt.DistanceTo(ptPline)); // add distance between the two points to list
                    parameters.Add(t);
                }

                var dataList = new List<(Point3d, double)>(); // list to sort points by parameter on CH

                
                var crossSectionPts = new List<Point3d>();
                var middlePts = new List<Point3d>();

                // find part of CH that matches the original pts
                for (int i = 0; i < ptsDistance.Count; ++i)
                {
                    var dist = ptsDistance[i];
                    if (dist < inp_dist)
                    {
                        crossSectionPts.Add(pts[i]);
                        dataList.Add((pts[i], parameters[i])); // add the original pts and the parameters from the closest pt on CH to list
                    }
                    else
                        middlePts.Add(pts[i]);

                }
                var sortedDataList = dataList.OrderBy(item => item.Item2).ToList(); // sort list based on keys (parameters on CH)

                var ptsSorted = new List<Point3d>();

                foreach (var item in sortedDataList)
                {
                    ptsSorted.Add(item.Item1);
                    ghPtsSorted.Append(new GH_Point(item.Item1));
                }
                    

                ptsSorted.Add(sortedDataList[0].Item1);
                ghPtsSorted.Append(new GH_Point(sortedDataList[0].Item1));

                var perimeterLine = new Polyline(ptsSorted);
                var segments = perimeterLine.GetSegments();

                // get all parts of polyline longer than a certain length, because it is a "crack"
                var cracks = new List<Line>();
                foreach (var seg in segments)
                    if (seg.Length > inp_segLength)
                        cracks.Add(seg);



                var extraCrossSectionPts = new List<Point3d>();

                // find the closest orthogonal points to the curve
                for (int c = 0; c < cracks.Count; c++)
                {
                    var path = new GH_Path(c);
                    var crack = cracks[c];

                    ghCracks.Append(new GH_Curve(crack.ToNurbsCurve()), path0);

                    var ptsCrack = new List<Point3d>();
                    var ptsMiddle = new List<Point3d>();

                    var crackStart = crack.PointAt(0);
                    var crackEnd = crack.PointAt(1);

                    for (int i = 0; i < middlePts.Count; ++i)
                    {
                        var pt = middlePts[i];
                        var ptCrack = crack.ClosestPoint(pt, true);

                        if (ptCrack != crackStart && ptCrack != crackEnd) // condition to only look at the orthogonal points
                        {
                            ptsCrack.Add(ptCrack);
                            ptsMiddle.Add(pt);
                        }
                    }

                    var crackMiddleLine = new List<Line>();



                    // create lines between the orthogonal pts and the closest pts on crackLine
                    for (int i = 0; i < ptsCrack.Count; ++i)
                        crackMiddleLine.Add(new Line(ptsCrack[i], ptsMiddle[i]));


                    // find the shortest line in each interval along the crack
                    var ptsShortestLines = new List<Line>();
                    
                    var spans = crack.Length / inp_distBtwPtsCrack;
                    for (int i = 0; i < Convert.ToInt32(spans); i++)
                    {
                        var path1 = new GH_Path(i);
                        var intStart = 1 / Convert.ToDouble(spans) * Convert.ToDouble(i); // interval start
                        var intEnd = 1 / Convert.ToDouble(spans) * Convert.ToDouble(i + 1); // interval end

                        var crackMiddleLineInterval = new List<Line>();
                        for (int j = 0; j < ptsCrack.Count; j++)
                        {
                            var ptCrack = ptsCrack[j];
                            var p = crack.ClosestParameter(ptCrack);

                            if (p > intStart && p < intEnd)
                                crackMiddleLineInterval.Add(crackMiddleLine[j]); // add all lines in interval to list
                        }

                        if (crackMiddleLineInterval.Count != 0)
                        {
                            var crackMiddleLineIntervalSorted = crackMiddleLineInterval.OrderBy(line => line.Length).ToList(); // sort list
                            var shortLine = crackMiddleLineIntervalSorted[0]; // get shortest line from interval                    
                            extraCrossSectionPts.Add(shortLine.PointAt(1.0)); // get endpoint from shortest line and add to the rest of pts for CS
                        }

                    }
                }


                foreach (var pt in extraCrossSectionPts) // need to sort points again, so find new parameters along CH
                {
                    convexHull.ClosestPoint(pt, out double t);
                    dataList.Add((pt, t));
                }

                var newDataListSorted = dataList.OrderBy(item => item.Item2).ToList(); // sort along CH
                var newPtsSorted = new List<Point3d>();

                foreach (var item in newDataListSorted)
                {
                    newPtsSorted.Add(item.Item1);
                    ghNewPtsSorted.Append(new GH_Point(item.Item1), path0);
                }
                    
                newPtsSorted.Add(newDataListSorted[0].Item1); // add first point to end of list for closed polyline
                ghNewPtsSorted.Append(new GH_Point(newDataListSorted[0].Item1), path0);

                var crossSectionCurve = new Polyline(newPtsSorted).ToNurbsCurve();
                var crossSectionCurveRebuilt = crossSectionCurve.Rebuild(rebuildPts, 1, false);
                crossSectionCurveRebuilt.RemoveShortSegments(deleteShortSeg);
                crossSectionCurves.Add(crossSectionCurveRebuilt);

                ghCrossSectionCurve.Append(new GH_Curve(crossSectionCurve), path0);
                var getCrossSectionCurve = GetCrossSection.CrossSectionEnd(pts, inp_dist, inp_segLength, inp_distBtwPtsCrack).ToNurbsCurve();
                getCrossSectionCurves.Add(getCrossSectionCurve);               
                

            }
            


            DA.SetDataList(0, crossSectionCurves);
            DA.SetDataList(1, getCrossSectionCurves);
            
            DA.SetDataTree(2, ghPtsSorted); // pts
            DA.SetDataTree(3, ghCrossSectionCurve); //curves            
            DA.SetDataTree(4, ghNewPtsSorted); // pts
            DA.SetDataTree(5, ghConvexHull); //crv
            DA.SetDataTree(6, ghCracks); //crv
            



        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("29243C82-0321-4CEE-89C0-D7A10230BF34"); }
        }
    }
}