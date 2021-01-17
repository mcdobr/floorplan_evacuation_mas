using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace floorplan_evacuation_mas
{
    public class AgentSummary
    {
        public AgentSummary()
        {
            Id = 0;
            Position = null;
        }

        public AgentSummary(int key, Point value)
        {
            Id = key;
            Position = value;
        }

        public int Id { get; set; }
        public Point Position { get; set; }

    }
}
