using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types.Transforms;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace MeshFromPointCloud
{
    public class testing : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the testing class.
        /// </summary>
        public testing()
          : base("testing", "Nickname",
              "Description",
               "Master", "PointCloud")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "", "", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("Curves", "", "", GH_ParamAccess.list);
            pManager.AddPointParameter("Points", "", "", GH_ParamAccess.list);
            pManager.AddLineParameter("Lengths", "", "", GH_ParamAccess.list);
            pManager.AddSurfaceParameter("srf", "", "", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh hullMesh = new Mesh();
            DA.GetData(0, ref hullMesh);

            List<Point3d> pts = new List<Point3d>();
            foreach (var v in hullMesh.Vertices)
            {
                pts.Add(new Point3d(v.X, v.Y, v.Z));
            }
            
            Line axis;
            Line.TryFitLineToPoints(pts, out axis);
            Curve fit_curve = axis.ToNurbsCurve();
            Plane planeMiddle;
            double dmn = fit_curve.Domain.Length;
            fit_curve.PerpendicularFrameAt(0.5*dmn, out planeMiddle);

            var pLine = Intersection.MeshPlane(hullMesh, planeMiddle);


            var seg = pLine[0].GetSegments().ToList();
            List<Point3d> intPnts = new List<Point3d>();
            foreach (var s in seg)
                intPnts.Add(s.PointAt(0.0));

            
            List<Line> diagCand = new List<Line>();            
            for (int i = 0; i < intPnts.Count; i++)
            {
                foreach (var intPnt in intPnts)
                {
                    var ln = new Line(intPnt, intPnts[i]);
                    diagCand.Add(ln);                    
                }
            }

            var diagCandSorted = diagCand.OrderBy(diag => diag.Length).ToList();

            diagCandSorted.Reverse();

            Line l1 = diagCandSorted[0];
            Line l2 = diagCandSorted[0];

            Point3d pl11 = l1.PointAt(0.0);
            Point3d pl12 = l1.PointAt(1.0);
            Point3d pl21 = l2.PointAt(0.0);
            Point3d pl22 = l2.PointAt(1.0);

            for (int i = 1; i < diagCandSorted.Count; i++)
            {
                if (pl11 == pl21 || pl11 == pl22
                || pl12 == pl21 || pl12 == pl22)
                {
                    l2 = diagCandSorted[i];
                    pl21 = l2.PointAt(0.0);
                    pl22 = l2.PointAt(1.0);
                }
                else
                    break;
            }



            NurbsSurface crossSect = NurbsSurface.CreateFromCorners(pl11, pl22, pl12, pl21);

            List<Point3d> rectPts = new List<Point3d>();
            rectPts.Add(pl11);
            rectPts.Add(pl12);
            rectPts.Add(pl21);
            rectPts.Add(pl22);



            DA.SetDataList(0, seg);
            DA.SetDataList(1, intPnts);
            DA.SetDataList(2, diagCandSorted);
            DA.SetData(3, crossSect);
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
            get { return new Guid("D7F1F2B2-48C2-49BF-ABEA-676DB570C118"); }
        }
    }
}