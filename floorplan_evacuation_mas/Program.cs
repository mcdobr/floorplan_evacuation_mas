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


            List<WorkerAgent> workers = new List<WorkerAgent>();
            workers.Add(new WorkerAgent(0, 1, 3));
            workers.Add(new WorkerAgent(1, 7, 2));
            workers.Add(new WorkerAgent(2, 3, 2));
            workers.Add(new WorkerAgent(3, 9, 0));


            var exitPositions = new Dictionary<int, Tuple<int, int>>();

            exitPositions.Add(0, new Tuple<int, int>(4, 5));
            exitPositions.Add(1, new Tuple<int, int>(8, 9));

            int turnsUntilEmergency = 5;
            var monitorAgent = new MonitorAgent(turnsUntilEmergency, new Dictionary<int, Tuple<int, int>>(), exitPositions);
            env.Add(monitorAgent, MonitorAgent.Monitor);
            workers.ForEach(worker => env.Add(worker, getWorkerName(worker)));

            env.Start();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FloorPlanForm());
        }

        public static string getWorkerName(WorkerAgent worker)
        {
            return "Worker " + worker.Id;
        }
    }
}