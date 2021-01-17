using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace floorplan_evacuation_mas
{
    public class MessageType
    {
        public const string Start = "start";
        public const string Acknowledge = "acknowledge";
        public const string Move = "move";
        public const string Emergency = "emergency";
        public const string Block = "block";
        public const string Exit = "exit";
        public const string PeerQuestion = "peerQuestion";
        public const string PeerAffirmative = "peerAffirmative";
        public const string PeerNegative = "peerNegative";
    }
}