using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ActressMas;
using static floorplan_evacuation_mas.MessageType;

namespace floorplan_evacuation_mas
{
    class WorkerAgent : TurnBasedAgent
    {
        private int id;
        private int x;
        private int y;
        private State state;

        public WorkerAgent(int id, int x, int y)
        {
            this.id = id;
            this.x = x;
            this.y = y;
            this.state = State.MovingRandomly;
        }

        public int Id => id;

        public int X
        {
            get => x;
            set => x = value;
        }

        public int Y
        {
            get => y;
            set => y = value;
        }

        public override void Setup()
        {
            Console.WriteLine("Starting worker " + Id);
            Send(MonitorAgent.Monitor, Utils.Str(Position, X, Y));
        }

        public override void Act(Queue<Message> messages)
        {
            while (messages.Count > 0)
            {
                Message message = messages.Dequeue();
                Console.WriteLine("\t[{1} -> {0}]: {2}", this.Name, message.Sender, message.Content);
                string action;
                List<string> parameters;
                Utils.ParseMessage(message.Content, out action, out parameters);

                switch (action)
                {
                    case MessageType.Move:
                        if (state == State.MovingRandomly)
                        {
                            MoveRandomly();
                            
                        }
                        else
                        {
                            //MoveInDirection();
                        }
                        Send(MonitorAgent.Monitor, Utils.Str(MessageType.ChangePosition, X, Y));
                        break;
                }

            }
        }

        private void MoveRandomly()
        {
            int d = Utils.RandNoGen.Next(4);
            switch (d)
            {
                case 0: if (X > 0) X--; break;
                case 1: if (X < Utils.Size - 1) X++; break;
                case 2: if (Y > 0) Y--; break;
                case 3: if (Y < Utils.Size - 1) Y++; break;
            }
        }

        public enum State
        {
            MovingRandomly,
            MovingInConstantDirection
        };
    }
}