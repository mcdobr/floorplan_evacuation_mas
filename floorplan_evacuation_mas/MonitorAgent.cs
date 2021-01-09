using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ActressMas;
using Reactive;

namespace floorplan_evacuation_mas
{
    public class MonitorAgent : TurnBasedAgent
    {
        private FloorPlanForm guiForm;
        public Dictionary<int, Tuple<int, int>> WorkerPositions { get; set; }
        public Dictionary<int, Tuple<int, int>> ExitPositions { get; set; }


        public MonitorAgent(Dictionary<int, Tuple<int, int>> workerPositions, Dictionary<int, Tuple<int, int>> exitPositions)
        {
            this.WorkerPositions = workerPositions;
            this.ExitPositions = exitPositions;

            Thread guiThread = new Thread(new ThreadStart(StartGuiThread));
            guiThread.Start();
        }

        private void StartGuiThread()
        {
            guiForm = new FloorPlanForm();
            guiForm.SetOwner(this);
            guiForm.ShowDialog();
            Application.Run();
        }
    }
}