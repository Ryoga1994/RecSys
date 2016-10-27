using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecSys.Optics
{
    public struct PointReachability
    {
        public PointReachability(string pointId, double reachability)
        {
            PointId = pointId;
            Reachability = reachability;
        }

        public readonly string PointId;
        public readonly double Reachability;
    }
}
