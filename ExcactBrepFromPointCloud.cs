using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Grasshopper.Kernel;
using MIConvexHull;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace MeshFromPointCloud
{
    public class ExcactBrepFromPointCloud : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ExcactBrepFromPointCloud class.
        /// </summary>
        public ExcactBrepFromPointCloud()
          : base("ExcactBrepFromPointCloud", "Nickname",
              "Description",
              "Master", "PointCloud")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {            
            pManager.AddMeshParameter("Mesh From Points", "mfp", "MeshFromPointscan", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh from input", "mfi", "same mesh", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Planes", "pls", "", GH_ParamAccess.list);
            pManager.AddCurveParameter("CSCurves", "", "", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {            
            Mesh mesh = new Mesh();
            DA.GetData(0, ref mesh);
            
            var vertices = mesh.Vertices;
            List<Point3d> pts = new List<Point3d>();

            foreach (var v in vertices)
            {
                pts.Add(new Point3d(v.X, v.Y, v.Z));
            }
            Line axis;
            Line.TryFitLineToPoints(pts, out axis);

            Curve fit_curve = axis.ToNurbsCurve();
            Plane[] perpFrames;
            Interval fc_dom = fit_curve.Domain;

            List<double> prms = new List<double>();
            
            double n = 10;
            for (int i=1; i < n-1; i++)
            {
                var par = Convert.ToDouble(i) / (n-1);
                prms.Add(fc_dom.ParameterAt(par));
            }
            perpFrames = fit_curve.GetPerpendicularFrames(prms);

            List<Curve> crossSections = new List<Curve>();
            List<List<Point3d>> crossSectionPoints = new List<List<Point3d>>();
            List<Point3d> middlePoints = new List<Point3d>();
            
            for (int i = 0; i < perpFrames.Length; i++)
            {
                var MeshPlaneInt = Intersection.MeshPlane(mesh, perpFrames[i]);

                if (MeshPlaneInt != null)
                {
                    var cS =  MeshPlaneInt[0].ToNurbsCurve();
                    var divisionParams = cS.DivideByCount(100, false).ToList();
                    //crossSectionPoints.AddRange(cS.PointAt(divisionParams));
                    crossSections.Add(cS);
                }
                    
                    
            }

           
            



            /*
            var setg = pLine[0].GetSegments().ToList();
            List<Point3d> intPnts = new List<Point3d>();
            foreach (var s in setg)
                intPnts.Add(s.PointAt(0.0));
            */

            
            DA.SetDataList(1, perpFrames);
            DA.SetDataList(2, crossSections);
            

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
            get { return new Guid("77C5BF0D-81C8-4DE2-86BD-27258B872059"); }
        }
    }
}