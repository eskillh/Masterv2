using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;

namespace MeshFromPointCloud
{
    internal class Triangle
    {
        public Point3d V0 { get; }
        public Point3d V1 { get; }
        public Point3d V2 { get; }


        public Triangle(Point3d v0, Point3d v1, Point3d v2)
        {
            var V0 = v0;
            var V1 = v1;
            var V2 = v2;
        }

        public Circle CircumCircle()
        {
            Circle.TryFitCircleToPoints(new[] { V0, V1, V2 }, out Circle circumCircle);
            return circumCircle;
        }

        public bool InCircumCircle(Point3d pt)
        {
            var circumCircle = CircumCircle();
            return pt.DistanceTo(circumCircle.Center) <= circumCircle.Radius;
        }
        

        internal class Edge
        {
            public Point3d V0 { get; }
            public Point3d V1 { get; }

            public Edge(Point3d v0, Point3d v1)
            {
                V0 = v0; 
                V1 = v1;
            }

            public bool EdgeEquals(Edge edge)
            {
                return (V0.Equals(edge.V0) && V1.Equals(edge.V1)) 
                    || (V0.Equals(edge.V1) && V1.Equals(edge.V0));
            }
        }
    }


}
