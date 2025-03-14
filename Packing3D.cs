using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace MeshFromPointCloud
{
    public class Packing3D : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Packing3D class.
        /// </summary>
        public Packing3D()
          : base("Packing3D", "Nickname",
              "Description",
              "Master", "Packing")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Brep sections", "", "", GH_ParamAccess.list);
            pManager.AddBrepParameter("Brep", "", "", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Fitted beam", "", "", GH_ParamAccess.list);            
            pManager.AddCurveParameter("curves", "", "", GH_ParamAccess.list);
            pManager.AddCurveParameter("Cross sections", "", "", GH_ParamAccess.list);
            pManager.AddPointParameter("testing", "", "", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var brepSections = new List<Curve>();
            var brep = new Brep();

            DA.GetDataList(0, brepSections);
            DA.GetData(1, ref brep);

            
            var crossSections = new List<Rectangle3d>();
            foreach (var brepSection in brepSections)
            {
                crossSections.Add(PackingMethods.BiggestCrossSection(brepSection));
            }

            var brepVertices = brep.Vertices;
            var brepPts = new List<Point3d>();
            foreach (var vertex in brepVertices)
            {
                brepPts.Add(new Point3d(vertex.Location));
            }

            Line.TryFitLineToPoints(brepPts, out Line axis);
            var volumes = new List<Brep>();
            var curves = new List<Curve>();
            var testingList = new List<Point3d>();

            foreach (var crossSection in crossSections)
            {
                var curvesForLoft = new List<Curve>();
                var csPoint = axis.ClosestPoint(crossSection.PointAt(0), true);
                testingList.Add(csPoint);
                var csMin = PackingMethods.CrossSectionToVolume(crossSection, brep, csPoint, axis.PointAt(0)).ToNurbsCurve();
                var csMax = PackingMethods.CrossSectionToVolume(crossSection, brep, csPoint, axis.PointAt(1)).ToNurbsCurve();
                curvesForLoft.AddRange(new[] {csMin, csMax});
                var volume = Brep.CreateFromLoft(curvesForLoft, Point3d.Unset, Point3d.Unset, LoftType.Normal, false)[0];
                var volumeCapped = volume.CapPlanarHoles(0.0001);
                volumes.Add(volumeCapped);
                curves.AddRange(curvesForLoft);
            }
            curves.Add(axis.ToNurbsCurve());

            DA.SetDataList(0, volumes);
            DA.SetDataList(1, curves);
            DA.SetDataList(2, crossSections);
            DA.SetDataList(3, testingList);

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
            get { return new Guid("0C16B08E-B31D-4C67-9F15-15E1E72EA292"); }
        }
    }
}