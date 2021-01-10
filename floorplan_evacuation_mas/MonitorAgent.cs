using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ActressMas;
using Reactive;
using static floorplan_evacuation_mas.MessageType;
using Message = ActressMas.Message;

namespace floorplan_evacuation_mas
{
    public class MonitorAgent : TurnBasedAgent
    {
        public static String Monitor = "monitor";
        public static int FieldOfViewSide = 5;
        public static int FieldOfViewRadius = FieldOfViewSide / 2;

        private FloorPlanForm guiForm;

        private int turnsUntilEmergency;

        private Dictionary<int, int> numberOfPositionChanges;
        public Dictionary<int, Tuple<int, int>> WorkerPositions { get; set; }
        public Dictionary<int, Tuple<int, int>> ExitPositions { get; set; }


        public MonitorAgent(
            int turnsUntilEmergency,
            Dictionary<int, Tuple<int, int>> workerPositions,
            Dictionary<int, Tuple<int, int>> exitPositions
        )
        {
            this.turnsUntilEmergency = turnsUntilEmergency;
            this.WorkerPositions = workerPositions;
            this.ExitPositions = exitPositions;
            this.numberOfPositionChanges = new Dictionary<int, int>();

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

        public override void Setup()
        {
            base.Setup();
        }

        public override void Act(Queue<Message> messages)
        {
            while (messages.Count > 0)
            {
                Message message = messages.Dequeue();
                Console.WriteLine("\t[{1} -> {0}]: {2}", this.Name, message.Sender, message.Content);

                string action;
                string parameters;
                Utils.ParseMessage(message.Content, out action, out parameters);

                switch (action)
                {
                    case Position:
                        HandlePosition(message.Sender, parameters);
                        break;
                    case ChangePosition:
                        HandleChangePosition(message.Sender, parameters);
                        break;
                    default:
                        break;
                }

                guiForm.UpdatePlanetGUI();
            }
        }

        private void HandlePosition(string sender, string position)
        {
            int senderId = Utils.ParsePeer(sender);
            WorkerPositions.Add(senderId, Utils.ParsePosition(position));
            numberOfPositionChanges[senderId] = 0;
            Send(sender, MessageType.Move);
        }

        private void HandleChangePosition(string sender, string parameters)
        {
            int senderId = Utils.ParsePeer(sender);
            WorkerPositions[senderId] = Utils.ParsePosition(parameters);
            if (++numberOfPositionChanges[senderId] == turnsUntilEmergency)
            {
                Send(sender, MessageType.Emergency);
                return;
            }

            foreach (int workerId in WorkerPositions.Keys)
            {
                if (workerId == senderId)
                    continue;
                if (WorkerPositions[workerId].Equals(WorkerPositions[senderId]))
                {
                    Send(sender, MessageType.Block);
                    return;
                }
            }

            var closestExit = ExitPositions.Select(kvp =>
                    new KeyValuePair<Tuple<int, int>, int>(kvp.Value,
                        Utils.Distance(kvp.Value, WorkerPositions[senderId])))
                .Where(kvp => ExitInFieldOfView(kvp.Key, WorkerPositions[senderId]))
                .OrderBy(kvp => kvp.Value)
                .FirstOrDefault();

            if (closestExit.Equals(default(KeyValuePair<Tuple<int, int>, int>)) ||
                numberOfPositionChanges[senderId] < turnsUntilEmergency)
            {
                Send(sender, MessageType.Move);
            }
            else
            {
                if (closestExit.Value == 0)
                {
                    Send(sender, Utils.Str(MessageType.Exit, (closestExit)));
                    WorkerPositions.Remove(senderId);
                    this.Environment.Remove(sender);
                    if (WorkerPositions.Count == 0)
                    {
                        this.Stop();
                    }
                }
                else
                {
                    Send(sender, Utils.Str(MessageType.ExitNearby, closestExit.Key.Item1, closestExit.Key.Item2));
                }
            }
        }

        private bool ExitInFieldOfView(Tuple<int, int> exitPosition, Tuple<int, int> workerPosition)
        {
            return Math.Abs(exitPosition.Item1 - workerPosition.Item1) <= FieldOfViewRadius &&
                   Math.Abs(exitPosition.Item2 - workerPosition.Item2) <= FieldOfViewRadius;
        }
    }
}