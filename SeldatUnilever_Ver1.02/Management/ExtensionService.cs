﻿using SeldatUnilever_Ver1._02.Management.RobotManagent;
using System;
using System.Windows;
using static SeldatMRMS.Management.RobotManagent.RobotUnityControl;

namespace SeldatMRMS
{
    public static class ExtensionService
    {
        public static RobotLogOut LogOut;
        public static double CalDistance(Point p1, Point p2)
        {
            return Math.Sqrt((p2.X - p1.X) * (p2.X - p1.X) + (p2.Y - p1.Y) * (p2.Y - p1.Y));
        }
        public static bool IsInPolygon(Point[] poly, Point p)
        {
            Point p1, p2;
            bool inside = false;

            if (poly.Length < 3)
            {
                return inside;
            }

            var oldPoint = new Point(
                poly[poly.Length - 1].X, poly[poly.Length - 1].Y);

            for (int i = 0; i < poly.Length; i++)
            {
                var newPoint = new Point(poly[i].X, poly[i].Y);

                if (newPoint.X > oldPoint.X)
                {
                    p1 = oldPoint;
                    p2 = newPoint;
                }
                else
                {
                    p1 = newPoint;
                    p2 = oldPoint;
                }

                if ((newPoint.X < p.X) == (p.X <= oldPoint.X)
                    && (p.Y - (long)p1.Y) * (p2.X - p1.X)
                    < (p2.Y - (long)p1.Y) * (p.X - p1.X))
                {
                    inside = !inside;
                }

                oldPoint = newPoint;
            }

            return inside;
        }
        public static String PosetoString(Pose p)
        {
            return p.Position.X + "," + p.Position.Y + "," + p.AngleW;
        }

        public static bool FindHeaderInsideCircleArea(Point pheader, Point center, double r)
        {

            double leftSideEq = (pheader.X - center.X) * (pheader.X - center.X) + (pheader.Y - center.Y) * (pheader.Y - center.Y);
            if (Math.Sqrt(leftSideEq) <= r)
                return true;
            else
                return false;
        }

    }
}
