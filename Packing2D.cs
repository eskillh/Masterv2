using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace MeshFromPointCloud
{
    public class Packing2D : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Packing2D class.
        /// </summary>
        public Packing2D()
          : base("Packing2D", "Nickname",
              "Description",
              "Master", "Packing")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Perimeter Curve", "", "", GH_ParamAccess.item);            
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Cross Section","","",GH_ParamAccess.item);
            pManager.AddPointParameter("Intersection Points", "", "", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Plane of Perimeter Curve", "", "", GH_ParamAccess.item);
            pManager.AddRectangleParameter("Rectangles", "", "", GH_ParamAccess.list);
            pManager.AddIntegerParameter("testing", "", "", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve perim = null;
            DA.GetData(0, ref perim);

            perim.TryGetPlane(out Plane plane);
            var perimAreaMP = AreaMassProperties.Compute(perim);
            var perimSubCurves = perim.GetSubCurves();
            var perimLines = new List<Line>();
            foreach (var subCurve in perimSubCurves)
                perimLines.Add(new Line(subCurve.PointAtStart, subCurve.PointAtEnd));
            

            var centerPlane = new Plane(perimAreaMP.Centroid, plane.Normal);

            
            var angle = Math.PI;            
            
            var rectangles = new List<Rectangle3d>();
            var intPts = new List<Point3d>();

            
            var test = new List<int>();
            var rect = new Rectangle3d();

            

            double deg = 0;
            while (deg < angle)
            {
                var rotatingPlane = centerPlane;
                rotatingPlane.Rotate(deg, centerPlane.ZAxis);

                //var xNegStart = 100; var xPosStart = 100; var yNegStart = 100; var yPosStart = 100;
                double startValue = 10;
                var rectangleDimensions = new List<double>() 
                { startValue, startValue, startValue, startValue };

                var intersections = new List<int>() { 1, 1, 1, 1 };


                // should only check one relevant edge at a time
                while (intersections.Sum() > 0)
                {
                    for (int i = 0; i < rectangleDimensions.Count; i++)
                    {
                        while (intersections[i] != 0)
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

                            //var rectEdge = rectEdges[i];
                            var rectEdgeParams = new List<double>();
                            var perimParams = new List<double>();

                            foreach (var rectEdge in rectEdges)
                            {
                                var intEvents = Intersection.CurveLine(perim, rectEdge, 0.0001, 0.0001);
                                if (intEvents.Count > 0)
                                {
                                    foreach (var intersect in intEvents)
                                    {
                                        var a = intersect.ParameterB;

                                        if (a >= 0.0 && a <= 1.0)
                                        {
                                            rectEdgeParams.Add(a);
                                            break;
                                        }
                                    }

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
                    }
                }
                rectangles.Add(rect);
                deg += 0.01;
            }

            
            var biggestRectangle = new Rectangle3d();
            double biggestArea = 0;
            foreach (var rectangle in rectangles)
            {
                var rectangleMP = AreaMassProperties.Compute(rectangle.ToNurbsCurve());
                if (rectangleMP.Area > biggestArea)
                {
                    biggestArea = rectangleMP.Area;
                    biggestRectangle = rectangle;
                }
            }
                

            DA.SetData(0, biggestRectangle);
            DA.SetDataList(1, intPts);
            DA.SetData(2, centerPlane);
            DA.SetDataList(3, rectangles);
            DA.SetDataList(4, test);


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
            get { return new Guid("9429698B-0DA6-42B1-83C3-27E8ECE6C972"); }
        }
    }
}