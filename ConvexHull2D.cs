using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;


namespace MeshFromPointCloud
{
    public class ConvexHull2D : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ConvexHull2D class.
        /// </summary>
        public ConvexHull2D()
          : base("ConvexHull2D", "Nickname",
              "Description",
              "Master", "tools")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "", "Points in 2D", GH_ParamAccess.list);
            pManager.AddNumberParameter("Length", "", "Length of lines to remove", GH_ParamAccess.item, 1e9);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("Convex Hull Middle Line", "", "", GH_ParamAccess.item);
            pManager.AddPointParameter("Convex Hull Points", "", "", GH_ParamAccess.list);
            pManager.AddCurveParameter("Convex Hull", "", "", GH_ParamAccess.item);
            pManager.AddCurveParameter("Shortest Lines", "", "", GH_ParamAccess.list);
            pManager.AddTextParameter("Error Message", "", "", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var pts = new List<Point3d>();
            double maxLength = 0;
            string errorMessage = "No Errors";
            DA.GetDataList(0, pts);
            DA.GetData(1, ref maxLength);

            var convexHullPts = new List<Point3d>();
            var curves = new List<Curve>();            
            var ptsSortedX = pts.OrderBy(p => p.X).ToList();

            var ptMin = ptsSortedX.First();
            var ptMax = ptsSortedX.Last();

            convexHullPts.Add(ptMin);
            convexHullPts.Add(ptMax);

            var line = new Line(ptMin, ptMax);            

            var hullPts = new List<Point3d>();
            var sortHullPts = new List<Point3d>();
            var shortestLines = new List<Line>();

            ConvexHullMethods.QuickHull(pts, pts.Count, ptMin, ptMax, 1, maxLength, hullPts, sortHullPts, shortestLines);
            ConvexHullMethods.QuickHull(pts, pts.Count, ptMin, ptMax, -1, maxLength, hullPts, sortHullPts, shortestLines);

            //ConvexHullMethods.QuickHull_2(pts, pts.Count, ptMin, ptMax, 1, lenTol, hullPts);
            //ConvexHullMethods.QuickHull_2(pts, pts.Count, ptMin, ptMax, -1, lenTol, hullPts);

            //errorMessage = ConvexHullMethods.ConvexHullWrapping(pts, pts.Count, hullPts, maxLength);

            //var sortHullPts = hullPts;

            var sortCurve = new Circle(line.PointAt(0.5), line.Length/2).ToNurbsCurve(); // make sortcurve for sorting the basic CH pts

            var sortHullPtsSorted = Methods.SortPointsAlongCurve(sortCurve, sortHullPts, true);

            var hullPtsSorted = Methods.SortPointsAlongCurve(sortCurve.ToNurbsCurve(), hullPts, true);

            var convexHull = new Polyline(sortHullPtsSorted);

            var segments = convexHull.GetSegments();            

            var ptsNewConvexHull = new List<Point3d>();
            ptsNewConvexHull.Add(sortHullPtsSorted[0]);

            
            for (int i = 0; i < sortHullPtsSorted.Count-1; i++)
            {
                var ptsBetween = new List<Point3d>();                
                var p1 = sortHullPtsSorted[i];
                var p2 = sortHullPtsSorted[i+1];
                
                ConvexHullMethods.PtBetween(pts, p1, p2, maxLength, ptsBetween, shortestLines);                
                var ptsBetweenSorted = Methods.SortPointsAlongCurve(new Line(p1, p2).ToNurbsCurve(), ptsBetween, false);
                
                ptsNewConvexHull.AddRange(ptsBetweenSorted);
                ptsNewConvexHull.Add(p2);                
            }

            var ptsNewConvexHullNoDuplicates = new List<Point3d>();
            ptsNewConvexHullNoDuplicates.Add(ptsNewConvexHull[0]);

            for (int i = 1; i < ptsNewConvexHull.Count - 1; i++)
            {
                bool duplicate = false;                
                for (int j = 1; j < ptsNewConvexHull.Count -1; j++)
                {
                    if (i != j)
                        if (ptsNewConvexHull[i] == ptsNewConvexHull[j])
                            duplicate = true;
                }

                if (duplicate == false)
                    ptsNewConvexHullNoDuplicates.Add(ptsNewConvexHull[i]);
            }

            ptsNewConvexHullNoDuplicates.Add(ptsNewConvexHull[0]);

            //var hullPtsSorted = Methods.SortPointsAlongCurve(convexHull.ToNurbsCurve(), hullPts, true);
            var pLine = new Polyline(ptsNewConvexHullNoDuplicates);
            
            
            DA.SetData(0, line);
            DA.SetDataList(1, ptsNewConvexHullNoDuplicates);
            DA.SetData(2, pLine);
            DA.SetDataList(3, shortestLines);
            DA.SetData(4, errorMessage);
            

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
            get { return new Guid("EA3C79E7-0BBB-432D-85D6-6DFC89EEA173"); }
        }
    }
}