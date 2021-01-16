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
        public const string Move = "move";
        public const string ChangePosition = "change";
        public const string Emergency = "emergency";
        public const string Block = "block";
        public const string Exit = "exit";
    }
}