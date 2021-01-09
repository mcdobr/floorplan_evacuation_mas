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
        private Direction direction;

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
                            MoveInDirection();
                        }

                        Send(MonitorAgent.Monitor, Utils.Str(MessageType.ChangePosition, X, Y));
                        break;
                    case MessageType.Emergency:
                        state = State.MovingInConstantDirection;
                        break;
                }
            }
        }

        private void MoveInDirection()
        {
            while (!canMoveInDirection())
            {
                this.direction = GenerateDirection();
            }
            executeMoveInDirection();
        }

        private bool canMoveInDirection()
        {
            switch (this.direction)
            {
                case Direction.Up:
                    return (X > 0);
                case Direction.Down:
                    return (X < Utils.Size - 1);
                case Direction.Left:
                    return (Y > 0);
                case Direction.Right:
                    return (Y < Utils.Size - 1);
                default:
                    throw new NotImplementedException();
            }
        }

        private void MoveRandomly()
        {
            this.direction = GenerateDirection();
            executeMoveInDirection();
        }

        private void executeMoveInDirection()
        {
            switch (this.direction)
            {
                case Direction.Up:
                    if (X > 0) X--;
                    break;
                case Direction.Down:
                    if (X < Utils.Size - 1) X++;
                    break;
                case Direction.Left:
                    if (Y > 0) Y--;
                    break;
                case Direction.Right:
                    if (Y < Utils.Size - 1) Y++;
                    break;
            }
        }

        private static Direction GenerateDirection()
        {
            return (Direction) Utils.RandNoGen.Next(4);
        }

        public enum State
        {
            MovingRandomly,
            MovingInConstantDirection
        };

        public enum Direction
        {
            Up = 0,
            Down = 1,
            Left = 2,
            Right = 3
        }
    }
}