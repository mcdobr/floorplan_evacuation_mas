using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ActressMas;

namespace floorplan_evacuation_mas
{
    class FloorPlanEnvironment : TurnBasedEnvironment
    {
        public FloorPlanEnvironment(int numberOfTurns = 0, int delayAfterTurn = 0, bool randomOrder = true, Random rand = null) : base(numberOfTurns, delayAfterTurn, randomOrder, rand)
        {
        }

        public override void TurnFinished(int turn)
        {
            Console.WriteLine("Environment: Finished turn {0}", turn);
        }
    }
}
