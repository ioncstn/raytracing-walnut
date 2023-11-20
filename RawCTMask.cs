using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.RegularExpressions;

namespace rt
{
    public class RawCtMask : Geometry
    {
        private readonly Vector _position;
        private readonly double _scale;
        private readonly ColorMap _colorMap;
        private readonly byte[] _data;

        private readonly int[] _resolution = new int[3];
        private readonly double[] _thickness = new double[3];
        private readonly Vector _v0;
        private readonly Vector _v1;

        private readonly Plane FrontPlane;
        private readonly Plane BackPlane;
        private readonly Plane TopPlane;
        private readonly Plane BottomPlane;
        private readonly Plane LeftPlane;
        private readonly Plane RightPlane;

        public RawCtMask(string datFile, string rawFile, Vector position, double scale, ColorMap colorMap) : base(Color.NONE)
        {
            _position = position;
            _scale = scale;
            _colorMap = colorMap;

            var lines = File.ReadLines(datFile);
            foreach (var line in lines)
            {
                var kv = Regex.Replace(line, "[:\\t ]+", ":").Split(':');
                if (kv[0] == "Resolution")
                {
                    _resolution[0] = Convert.ToInt32(kv[1]);
                    _resolution[1] = Convert.ToInt32(kv[2]);
                    _resolution[2] = Convert.ToInt32(kv[3]);
                }
                else if (kv[0] == "SliceThickness")
                {
                    _thickness[0] = Convert.ToDouble(kv[1]);
                    _thickness[1] = Convert.ToDouble(kv[2]);
                    _thickness[2] = Convert.ToDouble(kv[3]);
                }
            }

            _v0 = position;
            _v1 = position + new Vector(_resolution[0] * _thickness[0] * scale, _resolution[1] * _thickness[1] * scale, _resolution[2] * _thickness[2] * scale);

            var vectorX = new Vector(1, 0, 0);
            var vectorY = new Vector(0, 1, 0);
            var vectorZ = new Vector(0, 0, 1);

            FrontPlane = new Plane(vectorX, _v0);
            BackPlane = new Plane(vectorX, _v1);
            BottomPlane = new Plane(vectorZ, _v0);
            TopPlane = new Plane(vectorZ, _v1);
            LeftPlane = new Plane(vectorY, _v0);
            RightPlane = new Plane(vectorY, _v1);

            var len = _resolution[0] * _resolution[1] * _resolution[2];
            _data = new byte[len];
            FileStream f = new FileStream(rawFile, FileMode.Open, FileAccess.Read);
            if (f.Read(_data, 0, len) != len)
            {
                throw new InvalidDataException($"Failed to read the {len}-byte raw data");
            }
        }

        private ushort Value(int x, int y, int z)
        {
            if (x < 0 || y < 0 || z < 0 || x >= _resolution[0] || y >= _resolution[1] || z >= _resolution[2])
            {
                return 0;
            }

            return _data[z * _resolution[1] * _resolution[0] + y * _resolution[0] + x];
        }

        public override Intersection GetIntersection(Line line, double minDist, double maxDist)
        {
            // ADD CODE HERE
            var intersections = GetClosestAndFurthestIntersectionWithPlane(line);

            if (intersections[0].T == 0)
            {
                return Intersection.NONE;
            }

            var result = new Intersection(intersections[0]);

            var length = intersections[1].T - intersections[0].T;

            var currentPosition = intersections[0].Position;
            var currentT = intersections[0].T;
            double step = length / 100;
            var Dx = intersections[0].Line.Dx;
            double finalAlpha = 0;
            var finalColor = new Color(0, 0, 0, 0);
            bool intersected = false;

            for (int i = 0; i < 100; i ++)
            {
                var color = GetColor(currentPosition);
                if (color != Color.NONE)
                {
                    if (!intersected)
                    {
                        intersected = true;
                        result.Position = currentPosition;
                        result.T = currentT;
                    }
/*                    color *= 10;
                    color.Alpha = 10;
                    result.Position = currentPosition;
                    result.Color = color;
                    result.Valid = true;
                    result.Visible = true;
                    result.Material = Material.FromColor(color);
                    result.Geometry = this;
                    result.Normal = GetNormal(currentPosition);
                    return result;*/
                    finalAlpha += color.Alpha;
                    //finalColor += color * 0.5;
                    finalColor.Red = Math.Max(finalColor.Red, color.Red);
                    finalColor.Green = Math.Max(finalColor.Green, color.Green);
                    finalColor.Blue = Math.Max(finalColor.Blue, color.Blue);
                }
                if (finalAlpha >= 1)
                {
                    break;
                }

                currentPosition += Dx * step;
                currentT += step;
            }
            if (finalAlpha == 0)
            {
                return Intersection.NONE;
                //return TryAgain(intersections[0], intersections[1]);
            }

            result.Color = finalColor;
            result.Material = Material.FromColor(finalColor);
            result.Visible = true;
            result.Valid = true;
            result.Geometry = this;
            result.Normal = GetNormal(result.Position);
            return result;
        }

        private Intersection[] GetClosestAndFurthestIntersectionWithPlane(Line line)
        {
            var intersectionFront = FrontPlane.GetIntersection(line);
            var intersectionBack = BackPlane.GetIntersection(line);
            var intersectionBottom = BottomPlane.GetIntersection(line);
            var intersectionTop = TopPlane.GetIntersection(line);
            var intersectionLeft = LeftPlane.GetIntersection(line);
            var intersectionRight = RightPlane.GetIntersection(line);

            var list = new Intersection[6];
            list[0] = intersectionFront;
            list[1] = intersectionBack;
            list[2] = intersectionTop;
            list[3] = intersectionLeft;
            list[4] = intersectionRight;
            list[5] = intersectionBottom;

            int closest = 6, furthest = 6;
            double min = 10000, max = 0;
            for (int i = 0; i < 6; i++)
            {
                if (ValidIntersection(list[i]))
                {
                    if (list[i].T > max)
                    {
                        furthest = i;
                        max = list[i].T;
                    }
                    if (list[i].T < min)
                    {
                        closest = i;
                        min = list[i].T;
                    }
                }
            }

            var result = new Intersection[2];

            if (closest == 6)
            {
                result[0] = Intersection.NONE;
                result[1] = Intersection.NONE;
            }
            else
            {
                result[0] = list[closest];
                result[1] = list[furthest];
            }

            return result;
        }

        private bool ValidIntersection(Intersection intersection)
        {
            if (intersection.T == 0)
            {
                return false;
            }
            var point = intersection.Position;

            if (
                (Math.Abs(point.X - FrontPlane.p0.X) < 0.1 || Math.Abs(point.X - BackPlane.p0.X) < 0.1)
                && point.Y > _v0.Y && point.Y < _v1.Y
                && point.Z > _v0.Z && point.Z < _v1.Z
                )
            {
                return true;
            }

            if (
                (Math.Abs(point.Z - BottomPlane.p0.Z) < 0.1 || Math.Abs(point.Z - TopPlane.p0.Z) < 0.1)
                && point.Y > _v0.Y && point.Y < _v1.Y
                && point.X > _v0.X && point.X < _v1.X
                )
            {
                return true;
            }

            if (
                (Math.Abs(point.Y - LeftPlane.p0.Y) < 0.1 || Math.Abs(point.Y - RightPlane.p0.Y) < 0.1)
                && point.Z > _v0.Z && point.Z < _v1.Z
                && point.X > _v0.X && point.X < _v1.X
                )
            {
                return true;
            }

            return false;
        }

        private int[] GetIndexes(Vector v)
        {
            return new[]{
            (int)Math.Floor((v.X - _position.X) / _thickness[0] / _scale),
            (int)Math.Floor((v.Y - _position.Y) / _thickness[1] / _scale),
            (int)Math.Floor((v.Z - _position.Z) / _thickness[2] / _scale)};
        }
        private Color GetColor(Vector v)
        {
            int[] idx = GetIndexes(v);

            ushort value = Value(idx[0], idx[1], idx[2]);
            return _colorMap.GetColor(value);
        }

        private Vector GetNormal(Vector v)
        {
            int[] idx = GetIndexes(v);
            double x0 = Value(idx[0] - 1, idx[1], idx[2]);
            double x1 = Value(idx[0] + 1, idx[1], idx[2]);
            double y0 = Value(idx[0], idx[1] - 1, idx[2]);
            double y1 = Value(idx[0], idx[1] + 1, idx[2]);
            double z0 = Value(idx[0], idx[1], idx[2] - 1);
            double z1 = Value(idx[0], idx[1], idx[2] + 1);

            return new Vector(x1 - x0, y1 - y0, z1 - z0).Normalize();
        }
    }
}