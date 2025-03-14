using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Linq;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace Masterv2.Vegard
{
    public class CHCircles : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the CHCircles class.
        /// </summary>
        public CHCircles()
          : base("CHCircles", "Nickname",
              "Description",
              "Master", "Wasted")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "", "Points in 2D", GH_ParamAccess.list);
            pManager.AddNumberParameter("Length", "", "Length of lines to remove", GH_ParamAccess.item, 1e9);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("Convex Hull Middle Line", "", "", GH_ParamAccess.item);
            pManager.AddPointParameter("Convex Hull Points", "", "", GH_ParamAccess.list);
            pManager.AddCurveParameter("Convex Hull", "", "", GH_ParamAccess.item);
            pManager.AddCurveParameter("Circles", "", "", GH_ParamAccess.list);
            pManager.AddPointParameter("pts on curve", "", "", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var pts = new List<Point3d>();
            double maxLength = 0;

            DA.GetDataList(0, pts);
            DA.GetData(1, ref maxLength);

            var ptsSortedX = pts.OrderBy(p => p.X).ToList();

            var ptMin = ptsSortedX.First();
            var ptMax = ptsSortedX.Last();

            var line = new Line(ptMin, ptMax);

            var ptsTesting = new List<Point3d>();
            var ghPts = new GH_Structure<GH_Point>();
            var g = 0;
            var circles = new List<Curve>();
            var pCenters = new List<Point3d>();
            var r = Transform.Translation(-line.Direction * 0.1);
            var pSeam = ptMin;
            pSeam.Transform(r);
            //ConvexHullMethods.ConvexHullIterative(pts, ptMin, pSeam, ptMin, maxLength, ptsTesting, 
            //circles, pCenters, ghPts, g);

            var pLine = new Polyline(ptsTesting);

            DA.SetData(0, line);
            DA.SetDataList(1, pCenters);
            DA.SetData(2, pLine);
            DA.SetDataList(3, circles);
            DA.SetDataTree(4, ghPts);
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
            get { return new Guid("4BABE067-7EE1-4149-BBA7-55958939EF6B"); }
        }
    }
}