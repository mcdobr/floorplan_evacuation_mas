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
            int senderId = ParsePeer(sender);
            WorkerPositions.Add(senderId, ParsePosition(position));
            numberOfPositionChanges[senderId] = 0;
            Send(sender, MessageType.Move);
        }
        private void HandleChangePosition(string sender, string parameters)
        {
            int senderId = ParsePeer(sender);
            WorkerPositions[senderId] = ParsePosition(parameters);
            if (++numberOfPositionChanges[senderId] == turnsUntilEmergency)
            {
                Send(sender, MessageType.Emergency);
            }

            Send(sender, MessageType.Move);
        }

        private int ParsePeer(string sender)
        {
            return int.Parse(sender.Replace("Worker ", string.Empty));
        }

        private Tuple<int, int> ParsePosition(string positionStr)
        {
            var positionList = positionStr.Split(' ').Select(numberStr => int.Parse(numberStr)).ToList();
            var position = new Tuple<int, int>(positionList[0], positionList[1]);
            return position;
        }
    }
}