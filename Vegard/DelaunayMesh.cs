using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace MeshFromPointCloud
{
    public class DelaunayMesh : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the DelaunayMesh class.
        /// </summary>
        public DelaunayMesh()
          : base("DelaunayMesh", "Nickname",
              "Description",
              "Master", "tools")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "", "", GH_ParamAccess.list);
            pManager.AddNumberParameter("Max Length", "", "Max length of lines in Mesh", GH_ParamAccess.item, 1e9);
            pManager.AddPlaneParameter("Plane", "", "Plane to project mesh to", GH_ParamAccess.item, 
                new Plane(new Point3d(0,0,0), new Vector3d(0,0,1)));
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "", "", GH_ParamAccess.item);
            pManager.AddLineParameter("Naked Edges","","",GH_ParamAccess.item);
            pManager.AddLineParameter("Interior Edges", "", "", GH_ParamAccess.item);           
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var pts = new List<Point3d>();
            double maxLength = 0;
            var plane = new Plane();

            DA.GetDataList(0, pts);
            DA.GetData(1, ref maxLength);
            DA.GetData(2, ref plane);

            var dMesh = Mesh.CreateFromTessellation(pts, new List<Polyline>(), plane, false);
            var triangleEdges = new List<Line>();
            var testing = new List<Line>();
            var edges = new List<Curve>();
            //DelauneyMethods.DelauneyTriangulation(pts, maxLength, triangleEdges, testing);

            //var newMesh = DelauneyMethods.RemoveShortLinesFromMesh(dMesh, maxLength, edges);            
            var newMesh = DelauneyMethods.RemoveShortEdges(dMesh, maxLength, edges);
            

            DA.SetData(0, newMesh);
            DA.SetDataList(1, edges);
            DA.SetDataList(2, testing);            

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
            get { return new Guid("F5F17261-EA94-485E-8BCE-4226374F2787"); }
        }
    }
}