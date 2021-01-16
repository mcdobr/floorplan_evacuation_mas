using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace floorplan_evacuation_mas
{
    public class Utils
    {
        public static int Size = 11;
        public static int NoExplorers = 5;
        public static int NoResources = 10;

        public static int Delay = 200;
        public static Random RandNoGen = new Random();

        public static int ParsePeer(string sender)
        {
            return int.Parse(sender.Replace("Worker ", string.Empty));
        }

        public static Point closestPoint(List<Point> candidates, Point relativeTo)
        {
            return candidates.Select(candidate =>
                    new KeyValuePair<Point, int>(candidate, Distance(candidate, relativeTo)))
                .OrderBy(kvp => kvp.Value)
                .First()
                .Key;
        }

        public static int Distance(Point exitPosition, Point workerPosition)
        {
            return Math.Abs(exitPosition.X - workerPosition.X) +
                   Math.Abs(exitPosition.Y - workerPosition.Y);
        }
    }
}
