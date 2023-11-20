using System;


namespace rt
{
    public class Ellipsoid : Geometry
    {
        private Vector Center { get; }
        private Vector SemiAxesLength { get; }
        private double Radius { get; }
        
        
        public Ellipsoid(Vector center, Vector semiAxesLength, double radius, Material material, Color color) : base(material, color)
        {
            Center = center;
            SemiAxesLength = semiAxesLength;
            Radius = radius;
        }

        public Ellipsoid(Vector center, Vector semiAxesLength, double radius, Color color) : base(color)
        {
            Center = center;
            SemiAxesLength = semiAxesLength;
            Radius = radius;
        }

        public override Intersection GetIntersection(Line line, double minDist, double maxDist)
        {
            // TODO: ADD CODE HERE
            double a = CalculateSquareOfFraction(line.Dx.X, this.SemiAxesLength.X)
                + CalculateSquareOfFraction(line.Dx.Y, this.SemiAxesLength.Y)
                + CalculateSquareOfFraction(line.Dx.Z, this.SemiAxesLength.Z);
            double b = CalculateDiffForB(line.X0.X, line.Dx.X, this.Center.X, this.SemiAxesLength.X)
                + CalculateDiffForB(line.X0.Y, line.Dx.Y, this.Center.Y, this.SemiAxesLength.Y)
                + CalculateDiffForB(line.X0.Z, line.Dx.Z, this.Center.Z, this.SemiAxesLength.Z);
            double c = CalculateSquareOfFraction(line.X0.X - this.Center.X, this.SemiAxesLength.X)
                + CalculateSquareOfFraction(line.X0.Y - this.Center.Y, this.SemiAxesLength.Y)
                + CalculateSquareOfFraction(line.X0.Z - this.Center.Z, this.SemiAxesLength.Z)
                - Math.Pow(this.Radius, 2);
            double delta = Math.Pow(b, 2) - 4 * a * c;

            if (delta < 0)
            {
                return new Intersection();
            }

            Intersection result = new Intersection();
            double T;

            if (delta == 0)
            {
                T = (-b + Math.Sqrt(delta)) / (2 * a);
            }
            else
            {
                double T1 = (-b + Math.Sqrt(delta)) / (2 * a);
                double T2 = (-b - Math.Sqrt(delta)) / (2 * a);
                if (T1 > 0 && T2 > 0)
                {
                    T = Math.Min(T1, T2);
                }
                else
                {
                    T = Math.Max(T1, T2);
                }
            }

            result.Visible = false;
            result.Valid = true;
            result.Position = line.CoordinateToPosition(T);
            result.T = T;
            result.Geometry = this;
            result.Normal = new Vector(
                ((result.Position.X - this.Center.X) * 2) / Math.Pow(this.SemiAxesLength.X, 2),
                ((result.Position.Y - this.Center.Y) * 2) / Math.Pow(this.SemiAxesLength.Y, 2),
                ((result.Position.Z - this.Center.Z) * 2) / Math.Pow(this.SemiAxesLength.Z, 2)
                ).Normalize();
            result.Material = this.Material;
            result.Color = this.Color;

            if (minDist < T && T < maxDist)
            {
                result.Visible = true;
            }

            return result;
        }

        private double CalculateSquareOfFraction(double a, double b)
        {
            return Math.Pow(a, 2) / Math.Pow(b, 2);
        }

        private double CalculateDiffForB(double lineX0X, double lineDxX, double ellipsoidCenterX, double semiAxesLengthX)
        {
            return (2 * lineDxX * (lineX0X - ellipsoidCenterX)) / Math.Pow(semiAxesLengthX, 2);
        }
    }
}
