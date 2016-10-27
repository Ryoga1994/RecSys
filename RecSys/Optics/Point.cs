using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RecSys.Optics.PriorityQueue;

namespace RecSys.Optics
{
    internal class Point : RecSys.Optics.PriorityQueue.PriorityQueueNode
    {
        //点索引，点id，坐标向量
        public Point(UInt32 index, string id, double[] vector)
        {
            Index = index;
            Id = id;
            Vector = vector;

            WasProcessed = false;
            ReachabilityDistance = double.NaN;
        }

        public readonly string Id;//用于存储poiid
        public readonly double[] Vector;
        public readonly UInt32 Index;

        internal double ReachabilityDistance;
        internal bool WasProcessed;
    }
}
