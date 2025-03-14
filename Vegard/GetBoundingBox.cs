using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Masterv2.Vegard
{
    public class GetBoundingBox : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GetBoundingBox class.
        /// </summary>
        public GetBoundingBox()
          : base("GetBoundingBox", "Nickname",
              "Description",
              "Master", "PointCloud")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "pts", "points from scan", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("bb from points", "bbfp", "HelpBrep created witb bb", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Point3d> pts = new List<Point3d>();
            DA.GetDataList(0, pts);

            Plane plane;
            Plane.FitPlaneToPoints(pts, out plane);

            Line axis;
            Line.TryFitLineToPoints(pts, out axis);

            Plane plane_rot = plane;
            double ang = Vector3d.VectorAngle(plane.XAxis, axis.Direction, plane);

            PointCloud pCloud = new PointCloud(pts);

            plane_rot.Rotate(ang, plane.ZAxis);
            Plane plane_rot_test = plane_rot;

            Box bb;
            pCloud.GetBoundingBox(plane_rot, out bb);

            /*
            var xDir = bb.X.Max - bb.X.Min;
            var yDir = bb.Y.Max - bb.Y.Min;

            Interval xInt = new Interval(-xDir / 2, xDir / 2);
            Interval yInt = new Interval(-yDir / 2, yDir / 2);

            var csHelp = new Rectangle3d(plane, xInt, yInt);
            */
            /*
            Mesh bbMesh = Mesh.CreateFromBox(bb, 100, 100, 100);

            int TQCount = Convert.ToInt32(axis.Length) * 10;

            QuadRemeshParameters prm = new QuadRemeshParameters
            {
                TargetQuadCount = TQCount,  // Use exact quad count if needed
                AdaptiveSize = 0,      // Adjust as needed
                AdaptiveQuadCount = false, // Keeps sharp features
                TargetEdgeLength = 0,    // Ensures boundary edges stay intact
                DetectHardEdges = true,     // Identifies and preserves hard edges
                PreserveMeshArrayEdgesMode = 2, // Adjust if using guide curves
                GuideCurveInfluence = 0           // Adjust for better quad uniformity
            };
            bbMesh.QuadRemesh(prm);
            */

            DA.SetData(0, bb);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override Bitmap Icon
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
            get { return new Guid("BCAD8495-7A9F-4C6C-8CBE-635151EE6180"); }
        }
    }
}