﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace floorplan_evacuation_mas
{
    public class Utils
    {
        public static int Size = 11;
        public static int NoExplorers = 5;
        public static int NoResources = 10;

        public static int Delay = 200;
        public static Random RandNoGen = new Random();

        public static void ParseMessage(string content, out string action, out List<string> parameters)
        {
            string[] t = content.Split();

            action = t[0];

            parameters = new List<string>();
            for (int i = 1; i < t.Length; i++)
                parameters.Add(t[i]);
        }

        public static void ParseMessage(string content, out string action, out string parameters)
        {
            string[] t = content.Split();

            action = t[0];

            parameters = "";

            if (t.Length > 1)
            {
                for (int i = 1; i < t.Length - 1; i++)
                    parameters += t[i] + " ";
                parameters += t[t.Length - 1];
            }
        }

        public static string Str(object p1, object p2)
        {
            return string.Format("{0} {1}", p1, p2);
        }

        public static string Str(object p1, object p2, object p3)
        {
            return string.Format("{0} {1} {2}", p1, p2, p3);
        }

        public static int ParsePeer(string sender)
        {
            return int.Parse(sender.Replace("Worker ", string.Empty));
        }

        public static Tuple<int, int> ParsePosition(string positionStr)
        {
            var positionList = positionStr.Split(' ').Select(numberStr => int.Parse(numberStr)).ToList();
            var position = new Tuple<int, int>(positionList[0], positionList[1]);
            return position;
        }

        public static int Distance(Tuple<int, int> exitPosition, Tuple<int, int> workerPosition)
        {
            return Math.Abs(exitPosition.Item1 - workerPosition.Item1) +
                   Math.Abs(exitPosition.Item2 - workerPosition.Item2);
        }
    }
}
