using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ActressMas;
using static floorplan_evacuation_mas.MessageType;
using Environment = ActressMas.Environment;

namespace floorplan_evacuation_mas
{
    class WorkerAgent : TurnBasedAgent
    {
        private const int waitForAnswerTurns = 4;
        private const int cooldownForTalking = 5;

        private int id;
        private Point position;
        private State state;
        private Direction direction;

        private FloorPlanMessage lastReceivedMessageFromMonitor = null;
        private int waitTurns;

        private Dictionary<int, int> blockList = new Dictionary<int, int>();


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
                switch (receivedMessage.type)
                {
                    case MessageType.Acknowledge:
                    {
                        this.position = receivedMessage.position;
                        var candidate = PickCandidate(receivedMessage);
                        FloorPlanMessage changePositionMessage = new FloorPlanMessage(MessageType.Move, candidate);
                        Send(MonitorAgent.Monitor, JsonSerializer.Serialize(changePositionMessage));
                        lastReceivedMessageFromMonitor = receivedMessage;
                        break;
                    }
                    case MessageType.Emergency:
                    {
                        this.position = receivedMessage.position;
                        state = State.MovingInConstantDirection;
                        var candidate = PickCandidate(receivedMessage);
                        FloorPlanMessage changePositionMessage = new FloorPlanMessage(MessageType.Move, candidate);
                        Send(MonitorAgent.Monitor, JsonSerializer.Serialize(changePositionMessage));

                        lastReceivedMessageFromMonitor = receivedMessage;
                        break;
                    }
                    case MessageType.Block:
                    {
                        this.position = receivedMessage.position;
                        Point candidate = PickCandidate(receivedMessage);
                        FloorPlanMessage changePositionMessage = new FloorPlanMessage(MessageType.Move, candidate);
                        Send(MonitorAgent.Monitor, JsonSerializer.Serialize(changePositionMessage));

                        lastReceivedMessageFromMonitor = receivedMessage;
                        break;
                    }
                    case Exit:
                    {
                        this.position = receivedMessage.position;
                        break;
                    }
                    case MessageType.PeerQuestion:
                    {
                        // string messageType = lastReceivedMessageFromMonitor.exitsInFieldOfViewPositions.Count > 0
                        // ? MessageType.PeerAffirmative
                        // : MessageType.PeerNegative;
                        string messageType = PeerNegative;
                        FloorPlanMessage response =
                            new FloorPlanMessage(messageType, lastReceivedMessageFromMonitor.position);
                        Send(message.Sender, JsonSerializer.Serialize(response));
                        break;
                    }
                    case MessageType.PeerAffirmative:
                    {
                        if (state == State.WaitingForPeerResponses)
                        {
                            state = State.FollowingOther;
                            var candidate = MoveNear(Utils.closestPoint(receivedMessage.exitsInFieldOfViewPositions,
                                this.position));
                            FloorPlanMessage changePositionMessage = new FloorPlanMessage(MessageType.Move, candidate);
                            Send(MonitorAgent.Monitor, JsonSerializer.Serialize(changePositionMessage));
                        }

                        break;
                    }
                    case MessageType.PeerNegative:
                    {
                        blockList[Utils.ParsePeer(message.Sender)] = cooldownForTalking;
                        break;
                    }
                    default:
                        throw new NotImplementedException();
                }


                blockList = blockList.Select(kvp => new KeyValuePair<int, int>(kvp.Key, kvp.Value - 1))
                    .Where(kvp => kvp.Value > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value
                    );

                // if (state == State.WaitingForPeerResponses)
                // {
                //     --waitTurns;
                //     if (wa)
                // }
            }
        }

        private Point PickCandidate(FloorPlanMessage receivedMessageFromMonitor)
        {
            Point candidate = null;
            if (state == State.MovingRandomly)
            {
                candidate = MoveRandomly();
            }
            else
            {
                if (receivedMessageFromMonitor.exitsInFieldOfViewPositions.Count > 0) // message from monitor
                {
                    state = State.MovingTowardsExit;
                    candidate = MoveNear(Utils.closestPoint(receivedMessageFromMonitor.exitsInFieldOfViewPositions,
                        this.position));
                }
                else if (receivedMessageFromMonitor.agentsInFieldOfViewPositions.Count > 0) // message from monitor
                {
                    receivedMessageFromMonitor.agentsInFieldOfViewPositions.ForEach(peerAgent =>
                    {
                        var peerQuestionMessage = new FloorPlanMessage(MessageType.PeerQuestion, this.position);
                        if (!blockList.ContainsKey(peerAgent.Id) ||
                            (blockList.ContainsKey(peerAgent.Id) && blockList[peerAgent.Id] <= 0))
                        {
                            Send("Worker " + peerAgent.Id, JsonSerializer.Serialize(peerQuestionMessage));
                        }
                    });

                    // We assume that it does not move until a number of turns pass or it gets a response
                    // TODO: move in a direction until you get a response
                    if (receivedMessageFromMonitor.type == MessageType.Block)
                    {
                        candidate = MoveInOtherDirection();
                    }
                    else
                    {
                        candidate = MoveInDirection();
                    }

                    state = State.WaitingForPeerResponses;
                }
                else //message from monitor
                {
                    if (receivedMessageFromMonitor.type == MessageType.Block)
                    {
                        candidate = MoveInOtherDirection();
                    }
                    else
                    {
                        candidate = MoveInDirection();
                    }
                }
            }

            return candidate;
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
            MovingTowardsExit,
            WaitingForPeerResponses,
            FollowingOther
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