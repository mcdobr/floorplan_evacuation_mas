using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ActressMas;

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
            base.Setup();
        }

        public override void Act(Queue<Message> messages)
        {
            base.Act(messages);
        }

        public enum State
        {
            MovingRandomly,
            MovingInConstantDirection
        };
    }
}