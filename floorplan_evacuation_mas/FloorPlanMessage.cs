using System;
using System.Collections.Generic;

namespace floorplan_evacuation_mas
{
    public class FloorPlanMessage
    {
        private string type { get; set; }
        private Tuple<int, int> position { get; set; }
        private List<Tuple<int, int>> exitsInFieldOfViewPositions { get; set; }
        private List<Tuple<int, int>> agentsInFieldOfViewPositions { get; set; }
    }
}