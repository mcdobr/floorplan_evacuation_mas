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

        private bool running;
        private FloorPlanMessage lastReceivedMessageFromMonitor = null;
        private int waitTurns;

        private Dictionary<int, int> blockList = new Dictionary<int, int>();


        public WorkerAgent(int id, int x, int y)
        {
            this.id = id;
            this.position = new Point(x, y);
            this.state = State.MovingRandomly;
            this.running = true;
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
            while (messages.Count > 0 && running)
            {
                Message message = messages.Dequeue();
                Console.WriteLine("\t[{1} -> {0}]: {2}", this.Name, message.Sender, message.Content);

                FloorPlanMessage receivedMessage = JsonSerializer.Deserialize<FloorPlanMessage>(message.Content);
                if (message.Sender == MonitorAgent.Monitor)
                {
                    HandleMessageFromMonitor(receivedMessage);
                }
                else
                {
                    HandleMessageFromPeer(receivedMessage, message);
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

        private void HandleMessageFromPeer(FloorPlanMessage receivedMessage, Message message)
        {
            switch (receivedMessage.type)
            {
                case MessageType.PeerQuestion:
                {
                    string messageType = lastReceivedMessageFromMonitor.exitsInFieldOfViewPositions.Count > 0
                        ? MessageType.PeerAffirmative
                        : MessageType.PeerNegative;
                    // string messageType = PeerNegative;
                    FloorPlanMessage response =
                        new FloorPlanMessage(messageType, lastReceivedMessageFromMonitor.position);
                    Send(message.Sender, JsonSerializer.Serialize(response));
                    break;
                }
                case MessageType.PeerAffirmative:
                {
                    // todo: following not working right, but deadlock cooldown
                    // is implemented by not asking that agent again for a number of turns
                    if (state == State.WaitingForPeerResponses)
                    {
                        state = State.FollowingOther;
                        var candidate = MoveNear(receivedMessage.position);
                        FloorPlanMessage changePositionMessage =
                            new FloorPlanMessage(MessageType.Move, candidate);
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
        }

        private void HandleMessageFromMonitor(FloorPlanMessage receivedMessage)
        {
            this.position = receivedMessage.position;
            switch (receivedMessage.type)
            {
                case MessageType.Acknowledge:
                {
                    var candidate = PickCandidate(receivedMessage);
                    FloorPlanMessage changePositionMessage = new FloorPlanMessage(MessageType.Move, candidate);
                    Send(MonitorAgent.Monitor, JsonSerializer.Serialize(changePositionMessage));
                    break;
                }
                case MessageType.Emergency:
                {
                    state = State.MovingInConstantDirection;
                    var candidate = PickCandidate(receivedMessage);
                    FloorPlanMessage changePositionMessage = new FloorPlanMessage(MessageType.Move, candidate);
                    Send(MonitorAgent.Monitor, JsonSerializer.Serialize(changePositionMessage));
                    break;
                }
                case MessageType.Block:
                {
                    Point candidate = PickCandidate(receivedMessage);
                    FloorPlanMessage changePositionMessage = new FloorPlanMessage(MessageType.Move, candidate);
                    Send(MonitorAgent.Monitor, JsonSerializer.Serialize(changePositionMessage));
                    break;
                }
                case Exit:
                {
                    running = false;
                    break;
                }
                default:
                    throw new NotImplementedException();
            }

            lastReceivedMessageFromMonitor = receivedMessage;
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
                if (receivedMessageFromMonitor.exitsInFieldOfViewPositions.Count > 0)
                {
                    state = State.MovingTowardsExit;
                    candidate = MoveNear(Utils.closestPoint(receivedMessageFromMonitor.exitsInFieldOfViewPositions,
                        this.position));
                }
                else if (receivedMessageFromMonitor.agentsInFieldOfViewPositions.Count > 0)
                {
                    receivedMessageFromMonitor.agentsInFieldOfViewPositions.ForEach(peerAgent =>
                    {
                        var peerQuestionMessage = new FloorPlanMessage(MessageType.PeerQuestion, peerAgent.Position);
                        if (!blockList.ContainsKey(peerAgent.Id) ||
                            (blockList.ContainsKey(peerAgent.Id) && blockList[peerAgent.Id] <= 0))
                        {
                            Send("Worker " + peerAgent.Id, JsonSerializer.Serialize(peerQuestionMessage));
                        }
                    });

                    // move in a direction until you get a response

                    // todo: our strategy might not be the best because then we have to handle inconsistencies by getting blocked by the monitor,
                    //  but otherwise we need other stateful solution to let time pass (we focused on turns which are linked to messages received, but if we do not
                    //  send message to monitor, we don't progress)
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
                else
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