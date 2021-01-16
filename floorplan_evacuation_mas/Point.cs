using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace floorplan_evacuation_mas
{
    public class Point
    {
        public Point()
        {
        }

        public Point(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }


        public int X { get; set; }
        public int Y { get; set; }
        public override bool Equals(object obj)
        {
            //Check for null and compare run-time types.
            if ((obj == null) || this.GetType() != obj.GetType())
            {
                return false;
            }
            else
            {
                Point p = (Point)obj;
                return (X == p.X) && (Y == p.Y);
            }
        }

        public override string ToString()
        {
            return $"Point({X}, {Y})";
        }

        public static implicit operator Point(Tuple<int, int> t)
        {
            return new Point()
            {
                X = t.Item1,
                Y = t.Item2
            };
        }

        public static implicit operator Tuple<int, int>(Point t)
        {
            return Tuple.Create(t.X, t.Y);
        }
    }
}