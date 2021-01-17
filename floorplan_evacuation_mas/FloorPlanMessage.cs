using System;
using System.Collections.Generic;

namespace floorplan_evacuation_mas
{
    public class FloorPlanMessage
    {
        public string type { get; set; }
        public Point position { get; set; }
        public List<Point> exitsInFieldOfViewPositions { get; set; }
        public List<Point> agentsInFieldOfViewPositions { get; set; }

        public FloorPlanMessage()
        {
            this.type = null;
            this.position = null;
            this.exitsInFieldOfViewPositions = new List<Point>();
            this.agentsInFieldOfViewPositions = new List<Point>();
        }

        public FloorPlanMessage(string typ, Point point)
        {
            this.type = typ;
            this.position = point;
            this.exitsInFieldOfViewPositions = new List<Point>();
            this.agentsInFieldOfViewPositions = new List<Point>();
        }
    }
}