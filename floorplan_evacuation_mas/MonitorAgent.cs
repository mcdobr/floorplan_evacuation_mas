using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.Json;
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
        public Dictionary<int, Point> WorkerPositions { get; set; }
        public Dictionary<int, Point> ExitPositions { get; set; }


        public MonitorAgent(
            int turnsUntilEmergency,
            Dictionary<int, Point> workerPositions,
            Dictionary<int, Point> exitPositions
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


                FloorPlanMessage floorPlanMessage = JsonSerializer.Deserialize<FloorPlanMessage>(message.Content);

                // string action;
                // string parameters;
                // Utils.ParseMessage(message.Content, out action, out parameters);

                switch (floorPlanMessage.type)
                {
                    case Position:
                        HandlePosition(message.Sender, floorPlanMessage);
                        break;
                    case ChangePosition:
                        HandleChangePosition(message.Sender, floorPlanMessage);
                        break;
                    default:
                        break;
                }

                guiForm.UpdatePlanetGUI();
            }
        }

        private void HandlePosition(string sender, FloorPlanMessage floorPlanMessage)
        {
            int senderId = Utils.ParsePeer(sender);
            WorkerPositions.Add(senderId, floorPlanMessage.position);
            numberOfPositionChanges[senderId] = 0;

            FloorPlanMessage planMessage = new FloorPlanMessage();
            planMessage.type = MessageType.Move;
            Send(sender, JsonSerializer.Serialize(planMessage));
        }

        private void HandleChangePosition(string sender, FloorPlanMessage floorPlanMessage)
        {
            int senderId = Utils.ParsePeer(sender);
            // todo: change position only if there is nobody there already
            WorkerPositions[senderId] = floorPlanMessage.position;
            if (++numberOfPositionChanges[senderId] == turnsUntilEmergency)
            {
                var planMessage = new FloorPlanMessage();
                planMessage.type = Emergency;
                Send(sender, JsonSerializer.Serialize(planMessage));
                return;
            }

            foreach (int workerId in WorkerPositions.Keys)
            {
                if (workerId == senderId)
                    continue;
                if (WorkerPositions[workerId].Equals(WorkerPositions[senderId]))
                {
                    var planMessage = new FloorPlanMessage();
                    planMessage.type = Block;
                    Send(sender, JsonSerializer.Serialize(planMessage));
                    return;
                }
            }

            var closestExit = ExitPositions.Select(kvp =>
                    new KeyValuePair<Point, int>(kvp.Value,
                        Utils.Distance(kvp.Value, WorkerPositions[senderId])))
                .Where(kvp => ExitInFieldOfView(kvp.Key, WorkerPositions[senderId]))
                .OrderBy(kvp => kvp.Value)
                .FirstOrDefault();

            if (closestExit.Equals(default(KeyValuePair<Point, int>)) ||
                numberOfPositionChanges[senderId] < turnsUntilEmergency)
            {
                var planMessage = new FloorPlanMessage();
                planMessage.type = MessageType.Move;
                Send(sender, JsonSerializer.Serialize(planMessage));
            }
            else
            {
                // If worker is on exit
                if (closestExit.Value == 0)
                {
                    var planMessage = new FloorPlanMessage();
                    planMessage.type = MessageType.Exit;
                    planMessage.exitsInFieldOfViewPositions.Add(closestExit.Key);
                    // todo: send all exits
                    Send(sender, JsonSerializer.Serialize(planMessage));

                    WorkerPositions.Remove(senderId);
                    this.Environment.Remove(sender);
                    if (WorkerPositions.Count == 0)
                    {
                        this.Stop();
                    }
                }
                else
                {
                    var planMessage = new FloorPlanMessage();
                    planMessage.type = MessageType.ExitNearby;
                    planMessage.exitsInFieldOfViewPositions.Add(closestExit.Key);
                    // todo: send all exits
                    Send(sender, JsonSerializer.Serialize(planMessage));
                }
            }
        }

        private bool ExitInFieldOfView(Point exitPosition, Point workerPosition)
        {
            return Math.Abs(exitPosition.X - workerPosition.X) <= FieldOfViewRadius &&
                   Math.Abs(exitPosition.Y - workerPosition.Y) <= FieldOfViewRadius;
        }
    }
}