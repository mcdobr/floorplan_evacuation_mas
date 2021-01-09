using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ActressMas;
using Reactive;


/**
 *                              emergency declared
 * moving randomly each turn ---------------------------> moving constantly in one direction
 *
 *                                      has exit in field of view
 * moving constantly in one direction --------------------------------------> moving on shortest path (manhattan distance moves)
 *
 *                                      hits a wall
 * moving constantly in one direction --------------------------------------> moving constantly in one direction (but with different direction, from available directions e.g. corners)
 *
 *                                      agent sees exit
 * moving constantly in one direction ---------------------------------------> moving towards exit
 *
 *                                      other agent in FoV responds that he sees exit
 * moving constantly in one direction ---------------------------------------> following other agent
 *
 *                          number of max following moves without seeing exit
 * following other agent ---------------------------------> moving constantly in one direction
 *
 *                          sees the exit 
 * following other agent ---------------------------------> moving towards exit
 *
 * moving towards exit --------------------------------> reached exit
 *
 *
 */

namespace floorplan_evacuation_mas
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            TurnBasedEnvironment env = new TurnBasedEnvironment(0, 100);

            var monitorAgent = new MonitorAgent();
            env.Add(monitorAgent, "planet");

            for (int i = 1; i <= Utils.NoExplorers; i++)
            {
                var explorerAgent = new WorkerAgent();
                env.Add(explorerAgent, "explorer" + i);
            }

            env.Start();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FloorPlanForm());
        }
    }
}
