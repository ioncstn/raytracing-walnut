using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rt
{
    public class Plane
    {
        public Vector Normal { get; set; }
        public Vector p0 { get; set; }

        public Plane(Vector normal, Vector p0)
        {
            Normal = normal;
            this.p0 = p0;
        }

        public Intersection GetIntersection(Line line)
        {
            double upper = (p0 - line.X0) * Normal;
            double lower = line.Dx * Normal;
            if (upper == 0 || lower == 0)
            {
                return Intersection.NONE;
            }

            double d = upper / lower;

            var result = new Intersection();
            result.Position = line.CoordinateToPosition(d);
            result.Line = line;
            result.T = d;

            return result;
        }
    }
}
