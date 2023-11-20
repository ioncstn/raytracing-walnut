using System;

namespace rt
{
    class RayTracer
    {
        private Geometry[] geometries;
        private Light[] lights;

        public RayTracer(Geometry[] geometries, Light[] lights)
        {
            this.geometries = geometries;
            this.lights = lights;
        }

        private double ImageToViewPlane(int n, int imgSize, double viewPlaneSize)
        {
            return -n * viewPlaneSize / imgSize + viewPlaneSize / 2;
        }

        private Intersection FindFirstIntersection(Line ray, double minDist, double maxDist)
        {
            var intersection = Intersection.NONE;

            foreach (var geometry in geometries)
            {
                var intr = geometry.GetIntersection(ray, minDist, maxDist);

                if (!intr.Valid || !intr.Visible) continue;

                if (!intersection.Valid || !intersection.Visible)
                {
                    intersection = intr;
                }
                else if (intr.T < intersection.T)
                {
                    intersection = intr;
                }
            }

            return intersection;
        }

        private bool IsLit(Vector point, Light light)
        {
            Line ray = new Line(light.Position, point);

            var intersection = FindFirstIntersection(ray, 0, 100000);

            if (intersection.T == 0)
            {
                return false;
            }

            var position = intersection.Position;

            if (Math.Abs(position.X - point.X) < 0.1 && Math.Abs(position.Y - point.Y) < 0.1 && Math.Abs(position.Z - point.Z) < 0.1)
            {
                return true;
            }

            return false;
        }

        public void Render(Camera camera, int width, int height, string filename)
        {
            var background = new Color(0.2, 0.2, 0.2, 1.0);
            var viewParallel = (camera.Up ^ camera.Direction).Normalize();

            var image = new Image(width, height);

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    // TODO: ADD CODE HERE
                    double heightUnit = camera.ViewPlaneHeight / height;
                    double widthUnit = camera.ViewPlaneWidth / width;

                    double Kh = heightUnit * ((height / 2) - y);
                    double Kw = widthUnit * ((width / 2) - x);

                    Vector X0 = camera.Position;
                    Vector Dx = camera.Position + camera.Direction * camera.ViewPlaneDistance + camera.Up * Kh + viewParallel * Kw;

                    Line ray = new Line(X0, Dx);

                    Intersection intersection = FindFirstIntersection(ray, camera.FrontPlaneDistance, camera.BackPlaneDistance);

                    if (intersection.Valid && intersection.Visible)
                    {
                        //image.SetPixel(x, y, intersection.Color);
                        Color finalColor = new Color(0, 0, 0, 0);
                        foreach (Light light in lights)
                        {
                            if (IsLit(intersection.Position, light))
                            {
                                Material material = intersection.Material;
                                Color color = material.Ambient;
                                color *= light.Ambient;

                                Vector N = intersection.Normal.Normalize();
                                Vector T = (light.Position - intersection.Position).Normalize();
                                double dotProduct = N * T;

                                if (dotProduct > 0)
                                {
                                    color += material.Diffuse * light.Diffuse * dotProduct;
                                }

                                Vector E = (camera.Position - intersection.Position).Normalize();
                                Vector R = N * dotProduct * 2 - T;

                                if (E * R > 0)
                                {
                                    color += material.Specular * light.Specular * Math.Pow(E * R, material.Shininess);
                                }

                                color *= light.Intensity;

                                finalColor += color;
                            }
                        }

                        image.SetPixel(x, y, finalColor);

                        continue;
                    }

                    image.SetPixel(x, y, background);
                }
            }

            image.Store(filename);
        }
    }
}