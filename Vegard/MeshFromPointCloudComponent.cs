using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using MIConvexHull;
using Rhino.Display;
using System.Linq;
using System.Drawing.Text;
using System.Xml.Serialization;
using GH_IO.Types;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;
using Rhino.Geometry.Intersect;




namespace Masterv2.Vegard
{
    public class MeshFromPointCloudComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public MeshFromPointCloudComponent()
          : base("MeshFromPointCloudComponent", "Nickname",
            "Description",
            "Master", "PointCloud")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "pts", "points from scan", GH_ParamAccess.tree);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Raw Mesh from points", "rawmfp", "raw mesh created from points", GH_ParamAccess.tree);
            pManager.AddMeshParameter("Refined Mesh from points", "refinedmfp", "refined mesh created from points", GH_ParamAccess.tree);
            pManager.AddBrepParameter("Brep from points", "bfp", "Brep created from points", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<GH_Point> pts_tree = new GH_Structure<GH_Point>();
            DA.GetDataTree(0, out pts_tree);



            GH_Structure<GH_Mesh> rawHullMeshes = new GH_Structure<GH_Mesh>();
            GH_Structure<GH_Mesh> refinedHullMeshes = new GH_Structure<GH_Mesh>();
            GH_Structure<GH_Brep> hullBreps = new GH_Structure<GH_Brep>();
            int t = 0;
            foreach (var pts_GH in pts_tree.Branches)
            {
                var pts = new List<Point3d>();
                foreach (var pt_GH in pts_GH)
                {
                    pts.Add(new Point3d(pt_GH.Value));
                }
                if (pts == null || pts.Count < 4)
                {
                    Rhino.RhinoApp.WriteLine("At least 4 points are required to compute a 3D convex hull.");
                    return;
                }


                // Convert Points to MIConvexHull vertices
                List<Vertex> vertices = new List<Vertex>();
                foreach (var pt in pts)
                {
                    vertices.Add(new Vertex(pt));
                }

                var hullResult = ConvexHull.Create<Vertex, Face>(vertices);
                var hull = hullResult.Result;  // Extracting actual convex hull

                // Correct way to get vertices and faces
                var hullVertices = hull.Points;
                var hullFaces = hull.Faces;



                // Initialize the mesh
                Mesh hullMesh = new Mesh();
                Dictionary<Vertex, int> vertexIndexMap = new Dictionary<Vertex, int>();
                int index = 0;

                // Add unique vertices to the mesh
                foreach (var vertex in hullVertices)
                {
                    var rhinoVertex = new Point3d(vertex.Position[0], vertex.Position[1], vertex.Position[2]);
                    hullMesh.Vertices.Add(rhinoVertex);
                    vertexIndexMap[vertex] = index;
                    index++;
                }

                // Add faces using the ConvexHull Faces
                foreach (var face in hullFaces)
                {
                    if (face.Vertices.Length == 3) // Ensure it's a triangle
                    {
                        int i1 = vertexIndexMap[face.Vertices[0]];
                        int i2 = vertexIndexMap[face.Vertices[1]];
                        int i3 = vertexIndexMap[face.Vertices[2]];
                        hullMesh.Faces.AddFace(i1, i2, i3);
                    }
                }


                hullMesh.Vertices.CombineIdentical(true, true);
                hullMesh.Weld(0.01);

                hullMesh.Normals.ComputeNormals();


                var edges = new List<Curve>();
                var srf = new List<Brep>();
                for (int i = 0; i < hullMesh.Faces.Count; i++)
                {
                    var f = hullMesh.Faces[i];
                    PolylineCurve pl;

                    pl = new PolylineCurve(new[]
                    {
                        hullMesh.Vertices.Point3dAt(f.A),
                        hullMesh.Vertices.Point3dAt(f.B),
                        hullMesh.Vertices.Point3dAt(f.C),
                        hullMesh.Vertices.Point3dAt(f.A)
                        });

                    edges.Add(pl);
                    var crv = pl.GetSubCurves();
                    srf.Add(Brep.CreateEdgeSurface(crv));

                }

                var hullBrep = Brep.JoinBreps(srf, 0.0001);


                Line axis;
                Line.TryFitLineToPoints(pts, out axis);


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

                var path = new GH_Path(t);

                rawHullMeshes.Append(new GH_Mesh(hullMesh), path);

                Mesh hullReMesh = hullMesh.QuadRemesh(prm);
                refinedHullMeshes.Append(new GH_Mesh(hullReMesh), path);

                hullBreps.Append(new GH_Brep(hullBrep[0]), path);
                t += 1;
            }

            DA.SetDataTree(0, rawHullMeshes);
            DA.SetDataTree(1, refinedHullMeshes);
            DA.SetDataTree(2, hullBreps);

        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// You can add image files to your project resources and access them like this:
        /// return Resources.IconForThisComponent;
        /// </summary>
        protected override System.Drawing.Bitmap Icon => null;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("65669e33-73b0-45a1-8dc9-5d6e1ea4945d");
    }
}