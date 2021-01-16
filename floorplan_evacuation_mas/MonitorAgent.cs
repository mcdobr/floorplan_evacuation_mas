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

                switch (floorPlanMessage.type)
                {
                    case Start:
                        HandleStart(message.Sender, floorPlanMessage);
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

        private void HandleStart(string sender, FloorPlanMessage floorPlanMessage)
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
                var emergencyMessage = BuildFloorPlanMessage(MessageType.Emergency, WorkerPositions[senderId]);
                Send(sender, JsonSerializer.Serialize(emergencyMessage));
                return;
            }

            foreach (int workerId in WorkerPositions.Keys)
            {
                if (workerId == senderId)
                    continue;
                if (WorkerPositions[workerId].Equals(WorkerPositions[senderId]))
                {
                    var blockMessage = BuildFloorPlanMessage(MessageType.Block, WorkerPositions[senderId]);
                    Send(sender, JsonSerializer.Serialize(blockMessage));
                    return;
                }
            }

            var closestExit = ExitPositions.Select(kvp =>
                    new KeyValuePair<Point, int>(kvp.Value,
                        Utils.Distance(kvp.Value, WorkerPositions[senderId])))
                .Where(kvp => InFieldOfView(kvp.Key, WorkerPositions[senderId]))
                .OrderBy(kvp => kvp.Value)
                .FirstOrDefault();

            if (closestExit.Equals(default(KeyValuePair<Point, int>)) ||
                numberOfPositionChanges[senderId] < turnsUntilEmergency)
            {
                var moveMessage = BuildFloorPlanMessage(MessageType.Move, WorkerPositions[senderId]);
                Send(sender, JsonSerializer.Serialize(moveMessage));
            }
            else
            {
                // If worker is on exit
                if (closestExit.Value == 0)
                {
                    var exitMessage = BuildFloorPlanMessage(MessageType.Exit, WorkerPositions[senderId]);
                    Send(sender, JsonSerializer.Serialize(exitMessage));

                    WorkerPositions.Remove(senderId);
                    this.Environment.Remove(sender);
                    if (WorkerPositions.Count == 0)
                    {
                        this.Stop();
                    }
                }
                else
                {
                    var planMessage = BuildFloorPlanMessage(MessageType.ExitNearby, WorkerPositions[senderId]);
                    Send(sender, JsonSerializer.Serialize(planMessage));
                }
            }
        }

        private FloorPlanMessage BuildFloorPlanMessage(string type, Point position)
        {
            var planMessage = new FloorPlanMessage();
            planMessage.type = type;
            planMessage.position = position;
            planMessage.exitsInFieldOfViewPositions = ExitPositions.Values
                .Where(exitPosition => InFieldOfView(exitPosition, position))
                .ToList();
            planMessage.agentsInFieldOfViewPositions = WorkerPositions.Values
                .Where(agentPosition => InFieldOfView(agentPosition, position) && !agentPosition.Equals(position))
                .ToList();
            return planMessage;
        }

        private bool InFieldOfView(Point target, Point origin)
        {
            return Math.Abs(target.X - origin.X) <= FieldOfViewRadius &&
                   Math.Abs(target.Y - origin.Y) <= FieldOfViewRadius;
        }
    }
}