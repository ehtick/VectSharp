﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace VectSharp.ThreeD
{
    public sealed class OrthographicCamera : CameraWithControls
    {
        public override Point TopLeft { get; protected set; }

        public override Size Size { get; protected set; }

        public override double ScaleFactor { get; }

        public Point3D Position { get; private set; }

        public override Point3D ViewPoint => this.Position;

        public NormalizedVector3D Direction { get; private set; }

        public double[,] RotationMatrix { get; private set; }
        public Point3D OrbitOrigin { get; private set; }

        private NormalizedVector3D RotationReference { get; set; }
        public double[,] CameraRotationMatrix { get; private set; }

        public OrthographicCamera(Point3D position, NormalizedVector3D direction, Size viewSize, double scaleFactor)
        {
            this.Position = position;
            this.Direction = direction;

            this.RotationMatrix = Matrix3D.RotationToAlignWithZ(direction);

            this.ScaleFactor = scaleFactor;

            this.TopLeft = new Point(-viewSize.Width * 0.5 * ScaleFactor, -viewSize.Height * 0.5 * ScaleFactor);
            this.Size = new Size(viewSize.Width * ScaleFactor, viewSize.Height * ScaleFactor);

            this.OrbitOrigin = position + direction * ((Vector3D)position).Modulus;

            if (new Vector3D(0, 1, 0) * this.Direction < 1)
            {
                this.RotationReference = (new Vector3D(0, 1, 0) - (new Vector3D(0, 1, 0) * this.Direction) * this.Direction).Normalize();
            }
            else
            {
                this.RotationReference = (new Vector3D(0, 0, 1) - (new Vector3D(0, 0, 1) * this.Direction) * this.Direction).Normalize();
            }

            Point3D rotatedY = this.RotationMatrix * (Point3D)(Vector3D)this.RotationReference;
            double rotationAngle = Math.PI / 2 - Math.Atan2(rotatedY.Y, rotatedY.X);
            this.CameraRotationMatrix = Matrix3D.RotationAroundAxis(new NormalizedVector3D(0, 0, 1), rotationAngle);
        }

        public int Compare(Element3D element1, Element3D element2, Graphics gpr)
        {
            if (element1.ZIndex != element2.ZIndex)
            {
                return element1.ZIndex - element2.ZIndex;
            }

            if (element1 is Point3DElement pt1)
            {
                Point proj1 = this.Project(pt1[0]);

                if (element2 is Point3DElement pt2)
                {
                    Point proj2 = this.Project(pt2[0]);

                    if ((proj1.X - proj2.X) * (proj1.X - proj2.X) + (proj1.Y - proj2.Y) * (proj1.Y - proj2.Y) < (pt1.Diameter * 0.5 + pt2.Diameter * 0.5) * (pt1.Diameter * 0.5 + pt2.Diameter * 0.5))
                    {
                        double dist1 = this.ZDepth(pt1[0]);
                        double dist2 = this.ZDepth(pt2[0]);

                        if (Math.Abs(dist1 - dist2) < Scene.Tolerance)
                        {
                            return 0;
                        }

                        return -Math.Sign(dist1 - dist2);
                    }
                    else
                    {
                        return 0;
                    }
                }
                else if (element2 is Line3DElement line)
                {
                    Point p0 = proj1;

                    Point p1 = this.Project(line[0]);
                    Point p2 = this.Project(line[1]);

                    double distSq = ((p2.Y - p1.Y) * p0.X - (p2.X - p1.X) * p0.Y + p2.X * p1.Y - p2.Y * p1.X) * ((p2.Y - p1.Y) * p0.X - (p2.X - p1.X) * p0.Y + p2.X * p1.Y - p2.Y * p1.X) / ((p2.X - p1.X) * (p2.X - p1.X) + (p2.Y - p1.Y) * (p2.Y - p1.Y));

                    double maxDistSq = (pt1.Diameter * 0.5 + line.Thickness * 0.5) * (pt1.Diameter * 0.5 + line.Thickness * 0.5);

                    Point v1 = new Point(p0.X - p1.X, p0.Y - p1.Y);
                    Point v2 = new Point(p2.X - p1.X, p2.Y - p1.Y);

                    double v2Length = v2.Modulus();

                    Point e2 = new Point(v2.X / v2Length, v2.Y / v2Length);

                    double dotProd = v1.X * e2.X + v1.Y * e2.Y;

                    if (dotProd >= -Math.Sqrt(maxDistSq) && dotProd <= v2Length + Math.Sqrt(maxDistSq) && distSq < maxDistSq)
                    {
                        Point pointOnLine = new Point(p1.X + dotProd * e2.X, p1.Y + dotProd * e2.Y);

                        Point3D pt = this.Deproject(pointOnLine, line);

                        double dist1 = this.ZDepth(pt1[0]);
                        double dist2 = this.ZDepth(pt);

                        if (Math.Abs(dist1 - dist2) < Scene.Tolerance)
                        {
                            return 0;
                        }

                        return -Math.Sign(dist1 - dist2);
                    }
                    else
                    {
                        return 0;
                    }
                }
                else if (element2 is Triangle3DElement triangle)
                {
                    Point p0 = proj1;

                    Point p1 = this.Project(triangle[0]);
                    Point p2 = this.Project(triangle[1]);
                    Point p3 = this.Project(triangle[2]);

                    double area = 0.5 * (-p2.Y * p3.X + p1.Y * (-p2.X + p3.X) + p1.X * (p2.Y - p3.Y) + p2.X * p3.Y);

                    double s = 1 / (2 * area) * (p1.Y * p3.X - p1.X * p3.Y + (p3.Y - p1.Y) * p0.X + (p1.X - p3.X) * p0.Y);
                    double t = 1 / (2 * area) * (p1.X * p2.Y - p1.Y * p2.X + (p1.Y - p2.Y) * p0.X + (p2.X - p1.X) * p0.Y);

                    if (s >= 0 && t >= 0 && 1 - s - t >= 0)
                    {
                        Point3D pt = this.Deproject(p0, triangle);

                        double dist1 = this.ZDepth(pt1[0]);
                        double dist2 = this.ZDepth(pt);

                        if (Math.Abs(dist1 - dist2) < Scene.Tolerance)
                        {
                            return 0;
                        }

                        return -Math.Sign(dist1 - dist2);
                    }
                    else
                    {
                        return 0;
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else if (element1 is Line3DElement line1)
            {
                if (element2 is Point3DElement)
                {
                    return -Compare(element2, element1, gpr);
                }
                else if (element2 is Line3DElement line2)
                {
                    Point l11 = this.Project(line1[0]);
                    Point l12 = this.Project(line1[1]);

                    Point l21 = this.Project(line2[0]);
                    Point l22 = this.Project(line2[1]);


                    (Point inters, double t, double s)? intersection = Intersections2D.Intersect(l11, l12, l21, l22);

                    if (intersection != null)
                    {
                        double vLength = new Point(l12.X - l11.X, l12.Y - l11.Y).Modulus();
                        double lLength = new Point(l22.X - l21.X, l22.Y - l21.Y).Modulus();

                        if (intersection?.t >= -line1.Thickness && intersection?.t <= vLength + line1.Thickness && intersection?.s >= -line1.Thickness && intersection?.s <= lLength + line1.Thickness)
                        {
                            Point3D point1 = this.Deproject(intersection.Value.inters, line1);
                            Point3D point2 = this.Deproject(intersection.Value.inters, line2);

                            double dist1 = (point1 - this.Position).Modulus;
                            double dist2 = (point2 - this.Position).Modulus;

                            if (Math.Abs(dist1 - dist2) < Scene.Tolerance)
                            {
                                return 0;
                            }

                            return -Math.Sign(dist1 - dist2);
                        }
                        else
                        {
                            return 0;
                        }
                    }
                    else
                    {
                        return 0;
                    }
                }
                else if (element2 is Triangle3DElement triangle)
                {
                    Point l11 = this.Project(line1[0]);
                    Point l12 = this.Project(line1[1]);

                    Point lineDirPerp = new Point(l12.Y - l11.Y, l11.X - l12.X);
                    lineDirPerp = new Point(lineDirPerp.X / lineDirPerp.Modulus(), lineDirPerp.Y / lineDirPerp.Modulus());

                    Point l21 = new Point(l11.X + lineDirPerp.X * line1.Thickness * 0.5, l11.Y + lineDirPerp.Y * line1.Thickness * 0.5);
                    Point l22 = new Point(l12.X + lineDirPerp.X * line1.Thickness * 0.5, l12.Y + lineDirPerp.Y * line1.Thickness * 0.5);

                    Point l31 = new Point(l11.X - lineDirPerp.X * line1.Thickness * 0.5, l11.Y - lineDirPerp.Y * line1.Thickness * 0.5);
                    Point l32 = new Point(l12.X - lineDirPerp.X * line1.Thickness * 0.5, l12.Y - lineDirPerp.Y * line1.Thickness * 0.5);


                    Point A = this.Project(triangle[0]);
                    Point B = this.Project(triangle[1]);
                    Point C = this.Project(triangle[2]);

                    List<Point> interss1 = Intersections2D.Intersect(l11, l12, A, B, C, line1.Thickness, Scene.Tolerance);
                    List<Point> interss2 = Intersections2D.Intersect(l21, l22, A, B, C, Scene.Tolerance);
                    List<Point> interss3 = Intersections2D.Intersect(l31, l32, A, B, C, Scene.Tolerance);

                    List<Point> interss = new List<Point>();

                    if (interss1 != null && interss1.Count > 0)
                    {
                        interss.AddRange(interss1);
                    }

                    if (interss2 != null && interss2.Count > 0)
                    {
                        interss.AddRange(from el in interss2 select Intersections2D.ProjectOnLine(el, l11, l12));
                    }

                    if (interss3 != null && interss3.Count > 0)
                    {
                        interss.AddRange(from el in interss3 select Intersections2D.ProjectOnLine(el, l11, l12));
                    }

                    if (interss != null && interss.Count > 0)
                    {

                        int sign = 0;

                        foreach (Point pt in interss)
                        {
                            Point3D pt3D1 = this.Deproject(pt, line1);
                            Point3D pt3D2 = this.Deproject(pt, triangle);

                            double dist1 = (pt3D1 - this.Position).Modulus;
                            double dist2 = (pt3D2 - this.Position).Modulus;

                            int currSign = -Math.Sign(dist1 - dist2);

                            if (Math.Abs(dist1 - dist2) < Scene.Tolerance)
                            {
                                currSign = 0;
                            }

                            if (sign == 0 || currSign == sign || currSign == 0)
                            {
                                sign = currSign;
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("Line intersects with triangle!");
                                return 0;
                            }
                        }

                        return sign;
                    }
                    else
                    {
                        return 0;
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else if (element1 is Triangle3DElement triangle1)
            {
                if (element2 is Point3DElement || element2 is Line3DElement)
                {
                    return -Compare(element2, element1, gpr);
                }
                else if (element2 is Triangle3DElement triangle2)
                {
                    Point[] triangle1Projection = triangle1.GetProjection();
                    Point[] triangle2Projection = triangle2.GetProjection();

                    Point A1 = triangle1Projection[0];
                    Point B1 = triangle1Projection[1];
                    Point C1 = triangle1Projection[2];

                    Point A2 = triangle2Projection[0];
                    Point B2 = triangle2Projection[1];
                    Point C2 = triangle2Projection[2];

                    List<Point> intersections = Intersections2D.IntersectTriangles(A1, B1, C1, A2, B2, C2, Scene.Tolerance);

                    if (intersections.Count >= 3)
                    {
                        double meanX = 0;
                        double meanY = 0;

                        foreach (Point p in intersections)
                        {
                            meanX += p.X;
                            meanY += p.Y;
                        }

                        meanX /= intersections.Count;
                        meanY /= intersections.Count;

                        Point pt = new Point(meanX, meanY);


                        Point3D pt3D1 = this.Deproject(pt, triangle1);
                        Point3D pt3D2 = this.Deproject(pt, triangle2);

                        double dist1 = (pt3D1 - this.Position).Modulus;
                        double dist2 = (pt3D2 - this.Position).Modulus;

                        if (double.IsNaN(dist1 - dist2))
                        {
                            return 0;
                        }


                        int sign = -Math.Sign(dist1 - dist2);

                        if (Math.Abs(dist1 - dist2) < Scene.Tolerance)
                        {
                            sign = 0;
                        }

                        return sign;
                    }
                    else
                    {
                        return 0;
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public override double ZDepth(Point3D point)
        {
            return (point - this.Position) * this.Direction;
        }

        public override bool IsCulled(Element3D element)
        {
            if (element is Point3DElement)
            {
                return (element[0] - this.Position) * this.Direction >= 0;
            }
            else if (element is Line3DElement)
            {
                foreach (Point3D pt in element)
                {
                    if ((pt - this.Position) * this.Direction > 0)
                    {
                        return false;
                    }
                }
                return true;
            }
            else if (element is Triangle3DElement triangle)
            {
                bool found = false;

                foreach (Point3D pt in element)
                {
                    if ((pt - this.Position) * this.Direction > 0)
                    {
                        found = true;
                    }
                }

                if (!found)
                {
                    return true;
                }
                else
                {
                    return triangle.ActualNormal * this.Direction < 0;
                }
            }
            else
            {
                return true;
            }
        }

        public override Point Project(Point3D point)
        {
            Point3D projectedPoint = (Point3D)(point + Direction * ((this.Position - point) * this.Direction) - this.Position);

            Point3D rotatedPoint = this.CameraRotationMatrix * (this.RotationMatrix * projectedPoint);

            return new Point(rotatedPoint.X * ScaleFactor, rotatedPoint.Y * ScaleFactor);
        }

        public override Point3D Deproject(Point point, Line3DElement line)
        {
            Point3D rotatedPoint = new Point3D(point.X / ScaleFactor, point.Y / ScaleFactor, 0);

            Point3D projectedPoint = RotationMatrix.Inverse() * (CameraRotationMatrix.Inverse() * rotatedPoint);
            Point3D cameraPlanePoint = projectedPoint + (Vector3D)this.Position;

            NormalizedVector3D v = this.Direction;
            NormalizedVector3D l = (line[1] - line[0]).Normalize();

            double t;

            if (v.X * l.Y - v.Y * l.X != 0)
            {
                t = (l.X * (cameraPlanePoint.Y - line[0].Y) - l.Y * (cameraPlanePoint.X - line[0].X)) / (v.X * l.Y - v.Y * l.X);
            }
            else if (v.Z * l.Y - v.Y * l.Z != 0)
            {
                t = (l.Z * (cameraPlanePoint.Y - line[0].Y) - l.Y * (cameraPlanePoint.Z - line[0].Z)) / (v.Z * l.Y - v.Y * l.Z);
            }
            else if (v.Z * l.X - v.X * l.Z != 0)
            {
                t = (l.Z * (cameraPlanePoint.X - line[0].X) - l.X * (cameraPlanePoint.Z - line[0].Z)) / (v.Z * l.X - v.X * l.Z);
            }
            else
            {
                throw new Exception("The lines do not intersect!");
            }

            Point3D pt = cameraPlanePoint + v * t;
            return pt;
        }

        public override Point3D Deproject(Point point, Triangle3DElement triangle)
        {
            Point3D rotatedPoint = new Point3D(point.X / ScaleFactor, point.Y / ScaleFactor, 0);

            Point3D projectedPoint = RotationMatrix.Inverse() * (CameraRotationMatrix.Inverse() * rotatedPoint);
            Point3D cameraPlanePoint = projectedPoint + (Vector3D)this.Position;

            Point3D centroid = (Point3D)(((Vector3D)triangle[0] + (Vector3D)triangle[1] + (Vector3D)triangle[2]) * (1.0 / 3.0));

            Vector3D l = Direction;

            double d = ((centroid - cameraPlanePoint) * triangle.ActualNormal) / (l * triangle.ActualNormal);

            Point3D pt = cameraPlanePoint + l * d;
            return pt;
        }

        public override void Orbit(double theta, double phi)
        {
            Vector3D vect = this.Position - this.OrbitOrigin;

            NormalizedVector3D yAxis = new NormalizedVector3D(0, 1, 0);
            NormalizedVector3D xAxis = (this.Direction ^ (Vector3D)yAxis).Normalize();

            phi *= Math.Sign(Math.Atan2(xAxis.Z, xAxis.X));
            theta *= Math.Sign(yAxis * this.RotationReference);

            double[,] thetaRotation = Matrix3D.RotationAroundAxis(yAxis, theta);

            double[,] phiRotation = Matrix3D.RotationAroundAxis(xAxis, phi);

            Vector3D rotatedVect = (Vector3D)(phiRotation * (thetaRotation * (Point3D)vect));

            this.Position = this.OrbitOrigin + rotatedVect;
            this.Direction = (this.OrbitOrigin - this.Position).Normalize();
            this.RotationReference = ((Vector3D)(phiRotation * (thetaRotation * (Point3D)(Vector3D)this.RotationReference))).Normalize();
            this.RotationMatrix = Matrix3D.RotationToAlignWithZ(this.Direction);

            Point3D rotatedY = this.RotationMatrix * (Point3D)(Vector3D)this.RotationReference;
            double rotationAngle = Math.PI / 2 - Math.Atan2(rotatedY.Y, rotatedY.X);
            this.CameraRotationMatrix = Matrix3D.RotationAroundAxis(new NormalizedVector3D(0, 0, 1), rotationAngle);
        }

        public override void Zoom(double amount)
        {
            this.Size = new Size(this.Size.Width * Math.Pow(2, -amount), this.Size.Height * Math.Pow(2, -amount));
            this.TopLeft = new Point(-this.Size.Width * 0.5, -this.Size.Height * 0.5);
        }

        public override void Pan(double x, double y)
        {
            Vector3D delta = this.RotationMatrix.Inverse() * new Point3D(-x / ScaleFactor, -y / ScaleFactor, 0);

            this.Position = this.Position + delta;

            this.OrbitOrigin = this.OrbitOrigin + delta;
        }
    }
}