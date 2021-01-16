using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
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

            FloorPlanMessage positionMessage = new FloorPlanMessage();
            positionMessage.type = MessageType.Start;
            positionMessage.position = new Point(X, Y);
            Send(MonitorAgent.Monitor, JsonSerializer.Serialize(positionMessage));
        }

        public override void Act(Queue<Message> messages)
        {
            while (messages.Count > 0)
            {
                Message message = messages.Dequeue();
                Console.WriteLine("\t[{1} -> {0}]: {2}", this.Name, message.Sender, message.Content);

                FloorPlanMessage receivedMessage = JsonSerializer.Deserialize<FloorPlanMessage>(message.Content);
                switch (receivedMessage.type)
                {
                    case MessageType.Move:
                    {
                        if (state == State.MovingRandomly)
                        {
                            MoveRandomly();
                        }
                        else
                        {
                            MoveInDirection();
                        }

                        FloorPlanMessage changePositionMessage = new FloorPlanMessage();
                        changePositionMessage.type = MessageType.ChangePosition;
                        changePositionMessage.position = new Point(X, Y);
                        Send(MonitorAgent.Monitor, JsonSerializer.Serialize(changePositionMessage));
                        break;
                    }
                    case MessageType.Emergency:
                    {
                        state = State.MovingInConstantDirection;
                        MoveInDirection();

                        FloorPlanMessage changePositionMessage = new FloorPlanMessage();
                        changePositionMessage.type = MessageType.ChangePosition;
                        changePositionMessage.position = new Point(X, Y);
                        Send(MonitorAgent.Monitor, JsonSerializer.Serialize(changePositionMessage));
                        break;
                    }
                    case MessageType.Block:
                    {
                        // todo: could exclude blocked old dir
                        if (state == State.MovingRandomly)
                        {
                            MoveRandomly();
                        }
                        else
                        {
                            MoveInOtherDirection();
                        }

                        FloorPlanMessage changePositionMessage = new FloorPlanMessage();
                        changePositionMessage.type = MessageType.ChangePosition;
                        changePositionMessage.position = new Point(X, Y);
                        Send(MonitorAgent.Monitor, JsonSerializer.Serialize(changePositionMessage));
                        break;
                    }
                    case MessageType.ExitNearby:
                    {
                        state = State.MovingTowardsExit;
                        var point = MoveNear(Utils.closestPoint(receivedMessage.exitsInFieldOfViewPositions, new Point(X, Y)));
                        this.X = point.X;
                        this.Y = point.Y;
                        FloorPlanMessage changePositionMessage = new FloorPlanMessage();
                        changePositionMessage.type = MessageType.ChangePosition;
                        changePositionMessage.position = new Point(X, Y);
                        Send(MonitorAgent.Monitor, JsonSerializer.Serialize(changePositionMessage));
                        break;
                    }
                    case Exit:
                    {
                        break;
                    }
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        private Point MoveNear(Point exitPosition)
        {
            var closestPositionToExit = new List<Direction>
                    {Direction.Up, Direction.Down, Direction.Left, Direction.Right}
                .Select(dir => GetAdjacent(dir))
                .Where(adjacentPosition => IsInBounds(adjacentPosition))
                .Select(position =>
                    new KeyValuePair<Point, int>(position, Utils.Distance(exitPosition, position)))
                .OrderBy(kvp => kvp.Value)
                .First().Key;

            return closestPositionToExit;
        }

        private void MoveInOtherDirection()
        {
            this.direction = GenerateDirection();
            MoveInDirection();
        }

        private void MoveInDirection()
        {
            while (!CanMoveWithinBounds())
            {
                this.direction = GenerateDirection();
            }

            executeMoveInDirection();
        }

        private bool CanMoveWithinBounds()
        {
            return CanMoveWithinBounds(this.direction);
        }

        private bool CanMoveWithinBounds(Direction dir)
        {
            switch (dir)
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

        // todo: wtf does this do?
        private bool IsInBounds(Point position)
        {
            return X >= 0 && X < Utils.Size && Y >= 0 && Y < Utils.Size;
        }

        private Point GetAdjacent(Direction dir)
        {
            switch (dir)
            {
                case Direction.Up:
                    return new Point(X - 1, Y);
                case Direction.Down:
                    return new Point(X + 1, Y);
                case Direction.Left:
                    return new Point(X, Y - 1);
                case Direction.Right:
                    return new Point(X, Y + 1);
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
            MovingInConstantDirection,
            MovingTowardsExit
        };

        //todo: The labels are wrong because x is the column and y is the line (start from top left corner)
        public enum Direction
        {
            Up = 0,
            Down = 1,
            Left = 2,
            Right = 3
        }
    }
}