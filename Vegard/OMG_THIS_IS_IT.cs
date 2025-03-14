using System;
using System.Collections.Generic;

using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Display;
using Rhino.Geometry;



namespace Masterv2.Vegard
{
    public class OMG_THIS_IS_IT : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the OMG_THIS_IS_IT class.
        /// </summary>
        public OMG_THIS_IS_IT()
          : base("OMG_THIS_IS_IT", "Nickname",
              "Description",
              "Master", "PointCloud")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Points from Scan", "pfc", "List of points", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Divisions", "", "", GH_ParamAccess.item, 10);
            pManager.AddNumberParameter("tol", "", "", GH_ParamAccess.item, 5);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Brep of points", "", "", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Planes", "", "", GH_ParamAccess.list);
            pManager.AddPointParameter("Points close to plane", "", "", GH_ParamAccess.tree);
            pManager.AddPointParameter("points on Plane", "", "", GH_ParamAccess.tree);
            pManager.AddCurveParameter("crossSections", "", "", GH_ParamAccess.list);
            pManager.AddCurveParameter("convexHull", "", "", GH_ParamAccess.list);
            pManager.AddPointParameter("2dconvexhullpoints", "", "", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Import points
            var pts = new List<Point3d>();
            int div = 0;
            double tol = 0;

            DA.GetDataList(0, pts);
            DA.GetData(1, ref div);
            DA.GetData(2, ref tol);

            // Get centerline            
            Line.TryFitLineToPoints(pts, out Line axis);

            Curve axisCurve = axis.ToNurbsCurve();
            Interval acDomain = axisCurve.Domain;

            // Get Perp Frames along centerline
            List<double> parameters = new List<double>();
            //int n = Convert.ToInt32(div);

            for (int i = 0; i < div; i++)
            {
                var par = Convert.ToDouble(i) / (Convert.ToDouble(div) - 1);
                parameters.Add(acDomain.ParameterAt(par));
            }

            var perpFrames = axisCurve.GetPerpendicularFrames(parameters).ToList();



            int ptsCount = pts.Count;
            int frameCount = perpFrames.Count;


            // Get point coordinates related to perpframes[0] (all frames are along the same z-axis)
            Point3d[] ptsPlaneSpace = new Point3d[ptsCount];


            for (int i = 0; i < ptsCount; ++i)
            {
                Point3d ptInPlaneSpace = new Point3d();
                perpFrames[0].RemapToPlaneSpace(pts[i], out ptInPlaneSpace);
                ptsPlaneSpace[i] = ptInPlaneSpace;
            }

            var planeDist = new List<double>();

            for (int i = 0; i < frameCount; ++i) // Get distance to other planes so we dont have to change coordinates for each plane
            {
                var pl_0 = perpFrames[0].Origin;
                var pl_i = perpFrames[i].Origin;
                var dist = pl_0.DistanceTo(pl_i);

                planeDist.Add(dist);
            }


            // Find the points that are within a tolerance of the plane
            GH_Structure<GH_Point> ghPtsFrame = new GH_Structure<GH_Point>();

            for (int i = 0; i < frameCount; ++i)
            {
                var path = new GH_Path(i);

                var tol_b = planeDist[i] - tol; // adjust tolerance to fit z-coord of other planes so we only have to remap the pts ones
                var tol_t = planeDist[i] + tol;

                for (int j = 0; j < ptsCount; ++j)
                {
                    var pt = ptsPlaneSpace[j];

                    if (pt.Z > tol_b && pt.Z < tol_t) // cheack if point is within tolerance of plane
                    {
                        ghPtsFrame.Append(new GH_Point(pts[j]), path); // add points within tolerance                       
                    }
                }
            }

            // Project the points onto the plane and sort them in the same direction to make polylines
            var ghPtsProjected = new GH_Structure<GH_Point>();
            var crossSections = new List<Curve>();
            var convexHull = new List<PolylineCurve>();
            var test2d = new GH_Structure<GH_Point>();

            var worldPlane = new Plane(new Point3d(0, 0, 0), new Vector3d(1, 0, 0), new Vector3d(0, 1, 0));

            for (int i = 0; i < frameCount; ++i)
            {
                var path = new GH_Path(i);
                var ptsProjectedOnFrame = new List<Point3d>();
                var ptsProjectedOnFrameWorld = new List<Point3d>();
                var r = Transform.PlaneToPlane(perpFrames[i], worldPlane); // make transformation matrix from local plane to world plane

                for (int j = 0; j < ghPtsFrame[path].Count; ++j)
                {
                    var pt = ghPtsFrame[path][j];
                    perpFrames[i].ClosestParameter(pt.Value, out double s, out double t);  // get params of closest point on plane

                    var ptPlane = perpFrames[i].PointAt(s, t);
                    ptsProjectedOnFrame.Add(ptPlane); // point projected on local plane

                    ptPlane.Transform(r); // transform to world plane
                    ptsProjectedOnFrameWorld.Add(ptPlane); //point projected on world plane
                }

                var ptsProjectedOnFrame2d = new List<Point2d>(); // prepare list to hold 2dPoints for convex hull

                foreach (var pt in ptsProjectedOnFrameWorld)
                {
                    ptsProjectedOnFrame2d.Add(new Point2d(pt));
                    test2d.Append(new GH_Point(pt), path);
                }

                // make convex hull to use as curve to sort along, to deal with a lot of edge cases
                var sortCurve = PolylineCurve.CreateConvexHull2d(ptsProjectedOnFrame2d.ToArray(), out int[] hullIndices);

                convexHull.Add(sortCurve);
                var tParams = new List<double>();

                // get relative parameters from points rotated to same plane as convex hull
                foreach (var pt in ptsProjectedOnFrameWorld)
                {
                    sortCurve.ClosestPoint(pt, out double t);
                    tParams.Add(t);
                }

                var dataList = new List<(Point3d, double)>();

                // make list where pts on frame can be sorted according to parameter along sorting curve
                for (int j = 0; j < ptsProjectedOnFrame.Count; ++j)
                    dataList.Add((ptsProjectedOnFrame[j], tParams[j]));


                var dataListSorted = dataList.OrderBy(item => item.Item2).ToList(); // sort using pts as values and params as keys
                var ptsProjectSorted = new List<Point3d>();
                foreach (var item in dataListSorted)
                {
                    ghPtsProjected.Append(new GH_Point(item.Item1), path); // add sorted points to output
                    ptsProjectSorted.Add(item.Item1); // add sorted points to list
                }
                ptsProjectSorted.Add(dataListSorted[0].Item1); // add first point to the end of the list to make a closed curve

                var pLine = new Polyline(ptsProjectSorted); // fit a polyline through the sorted points to get perimeter curve
                pLine.DeleteShortSegments(2); // refine polyline
                crossSections.Add(pLine.ToNurbsCurve());


            }

            // loft all crossSection curves using LoftRebuild to get a nice brep
            var loftedBeam = Brep.CreateFromLoftRebuild(crossSections, Point3d.Unset, Point3d.Unset, LoftType.Tight, false, 500)[0];


            DA.SetData(0, loftedBeam);
            DA.SetDataList(1, perpFrames);
            DA.SetDataTree(2, ghPtsFrame);
            DA.SetDataTree(3, ghPtsProjected);
            DA.SetDataList(4, crossSections);
            DA.SetDataList(5, convexHull);
            DA.SetDataTree(6, test2d);



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
            get { return new Guid("E2357347-58A6-4FBD-8869-A8E56818B8C5"); }
        }
    }
}