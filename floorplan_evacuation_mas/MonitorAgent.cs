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
                    case MessageType.Move:
                        HandleChangePosition(message.Sender, floorPlanMessage);
                        break;
                    default:
                        break;
                }

                guiForm.UpdatePlanetGUI();
            }
        }

        private void HandleStart(string sender, FloorPlanMessage startMessage)
        {
            int senderId = Utils.ParsePeer(sender);
            WorkerPositions.Add(senderId, startMessage.position);
            numberOfPositionChanges[senderId] = 0;

            FloorPlanMessage ackMessage = BuildFloorPlanMessage(MessageType.Acknowledge, WorkerPositions[senderId]);
            Send(sender, JsonSerializer.Serialize(ackMessage));
        }

        private void HandleChangePosition(string sender, FloorPlanMessage receivedMessage)
        {
            int senderId = Utils.ParsePeer(sender);
            // Block is handled regardless if in emergency or not
            foreach (int workerId in WorkerPositions.Keys)
            {
                if (workerId == senderId)
                    continue;
                if (WorkerPositions[workerId].Equals(receivedMessage.position))
                {
                    var blockMessage = BuildFloorPlanMessage(MessageType.Block, WorkerPositions[senderId]);
                    Send(sender, JsonSerializer.Serialize(blockMessage));
                    return;
                }
            }

            // todo: block if server is congested
            if (WorkerPositions.ContainsKey(senderId) && Utils.Distance(WorkerPositions[senderId], receivedMessage.position) > 1)
            {
                var blockMessage = BuildFloorPlanMessage(MessageType.Block, WorkerPositions[senderId]);
                Send(sender, JsonSerializer.Serialize(blockMessage));
                return;
            }

            // Allow the requested move
            if (!WorkerPositions.ContainsKey(senderId))
            {
                return;
            }
            else
            {
                WorkerPositions[senderId] = receivedMessage.position;
            }

            // Should declare emergency
            if (++numberOfPositionChanges[senderId] == turnsUntilEmergency)
            {
                var emergencyMessage = BuildFloorPlanMessage(MessageType.Emergency, WorkerPositions[senderId]);
                Send(sender, JsonSerializer.Serialize(emergencyMessage));
                return;
            }

            // Exit if emergency is declared 
            if (isEmergencyDeclared(senderId) && ExitPositions.Values.Contains(WorkerPositions[senderId]))
            {
                var exitMessage = BuildFloorPlanMessage(MessageType.Exit, WorkerPositions[senderId]);
                Send(sender, JsonSerializer.Serialize(exitMessage));

                WorkerPositions.Remove(senderId);
                this.Environment.Remove(sender);
                if (WorkerPositions.Count == 0)
                {
                    this.Stop();
                }
                return;
            }

            // Only thing remaining to do is acknowledge the move
            var moveMessage = BuildFloorPlanMessage(MessageType.Acknowledge, WorkerPositions[senderId]);
            Send(sender, JsonSerializer.Serialize(moveMessage));
        }

        private bool isEmergencyDeclared(int senderId)
        {
            return numberOfPositionChanges[senderId] >= turnsUntilEmergency;
        }

        private FloorPlanMessage BuildFloorPlanMessage(string type, Point position)
        {
            var planMessage = new FloorPlanMessage();
            planMessage.type = type;
            planMessage.position = position;
            planMessage.exitsInFieldOfViewPositions = ExitPositions.Values
                .Where(exitPosition => InFieldOfView(exitPosition, position))
                .ToList();
            planMessage.agentsInFieldOfViewPositions = WorkerPositions
                .Where(agentKvp => InFieldOfView(agentKvp.Value, position) && !agentKvp.Value.Equals(position))
                .Select(agentKvp => new AgentSummary(agentKvp.Key, agentKvp.Value))
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