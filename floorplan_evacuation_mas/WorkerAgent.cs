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
        private Point position;
        private State state;
        private Direction direction;

        public WorkerAgent(int id, int x, int y)
        {
            this.id = id;
            this.position = new Point(x, y);
            this.state = State.MovingRandomly;
        }

        public int Id => id;


        public override void Setup()
        {
            Console.WriteLine("Starting worker " + Id);

            FloorPlanMessage positionMessage = new FloorPlanMessage();
            positionMessage.type = MessageType.Start;
            positionMessage.position = this.position;
            Send(MonitorAgent.Monitor, JsonSerializer.Serialize(positionMessage));
        }

        public override void Act(Queue<Message> messages)
        {
            while (messages.Count > 0)
            {
                Message message = messages.Dequeue();
                Console.WriteLine("\t[{1} -> {0}]: {2}", this.Name, message.Sender, message.Content);

                FloorPlanMessage receivedMessage = JsonSerializer.Deserialize<FloorPlanMessage>(message.Content);
                this.position = receivedMessage.position;
                switch (receivedMessage.type)
                {
                    case MessageType.Acknowledge:
                    {
                        Point candidate = null;
                        if (state == State.MovingRandomly)
                        {
                            candidate = MoveRandomly();
                        }
                        else
                        {
                            if (receivedMessage.exitsInFieldOfViewPositions.Count == 0)
                            {
                                candidate = MoveInDirection();
                            }
                            else
                            {
                                candidate = MoveNear(Utils.closestPoint(receivedMessage.exitsInFieldOfViewPositions,
                                    this.position));
                            }
                        }

                        FloorPlanMessage changePositionMessage = new FloorPlanMessage();
                        changePositionMessage.type = MessageType.Move;
                        changePositionMessage.position = candidate;
                        Send(MonitorAgent.Monitor, JsonSerializer.Serialize(changePositionMessage));
                        break;
                    }
                    case MessageType.Emergency:
                    {
                        state = State.MovingInConstantDirection;
                        var candidate = MoveInDirection();

                        FloorPlanMessage changePositionMessage = new FloorPlanMessage();
                        changePositionMessage.type = MessageType.Move;
                        changePositionMessage.position = candidate;
                        Send(MonitorAgent.Monitor, JsonSerializer.Serialize(changePositionMessage));
                        break;
                    }
                    case MessageType.Block:
                    {
                        // todo: could exclude blocked old dir
                        Point candidate = null;
                        if (state == State.MovingRandomly)
                        {
                            candidate = MoveRandomly();
                        }
                        else
                        {
                            candidate = MoveInOtherDirection();
                        }

                        FloorPlanMessage changePositionMessage = new FloorPlanMessage();
                        changePositionMessage.type = MessageType.Move;
                        changePositionMessage.position = candidate;
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

        private Point MoveInOtherDirection()
        {
            this.direction = GenerateDirection();
            return MoveInDirection();
        }

        private Point MoveInDirection()
        {
            while (!CanMoveWithinBounds())
            {
                this.direction = GenerateDirection();
            }

            switch (this.direction)
            {
                case Direction.Up:
                    return new Point(this.position.X - 1, this.position.Y);
                case Direction.Down:
                    return new Point(this.position.X + 1, this.position.Y);
                case Direction.Left:
                    return new Point(this.position.X, this.position.Y - 1);
                case Direction.Right:
                    return new Point(this.position.X, this.position.Y + 1);
                default:
                    throw new NotImplementedException();
            }
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
                    return (this.position.X > 0);
                case Direction.Down:
                    return (this.position.X < Utils.Size - 1);
                case Direction.Left:
                    return (this.position.Y > 0);
                case Direction.Right:
                    return (this.position.Y < Utils.Size - 1);
                default:
                    throw new NotImplementedException();
            }
        }

        // todo: wtf does this do?
        private bool IsInBounds(Point position)
        {
            return this.position.X >= 0 && this.position.X < Utils.Size && this.position.Y >= 0 &&
                   this.position.Y < Utils.Size;
        }

        private Point GetAdjacent(Direction dir)
        {
            switch (dir)
            {
                case Direction.Up:
                    return new Point(this.position.X - 1, this.position.Y);
                case Direction.Down:
                    return new Point(this.position.X + 1, this.position.Y);
                case Direction.Left:
                    return new Point(this.position.X, this.position.Y - 1);
                case Direction.Right:
                    return new Point(this.position.X, this.position.Y + 1);
                default:
                    throw new NotImplementedException();
            }
        }

        private Point MoveRandomly()
        {
            Point result = null;
            do
            {
                this.direction = GenerateDirection();
                switch (this.direction)
                {
                    case Direction.Up:
                        if (this.position.X > 0)
                            result = new Point(this.position.X - 1, this.position.Y);
                        break;
                    case Direction.Down:
                        if (this.position.X < Utils.Size - 1)
                            result = new Point(this.position.X + 1, this.position.Y);
                        break;
                    case Direction.Left:
                        if (this.position.Y > 0)
                            result = new Point(this.position.X, this.position.Y - 1);
                        break;
                    case Direction.Right:
                        if (this.position.Y < Utils.Size - 1)
                            result = new Point(this.position.X, this.position.Y + 1);
                        break;
                }
            } while (result == null);

            return result;
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