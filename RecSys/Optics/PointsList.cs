using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecSys.Optics
{
    public class PointsList
    {
        internal List<Point> _points;

        public PointsList()
        {
            _points = new List<Point>();
        }

        public void AddPoint(string id, double[] vector)
        {
            //_points索引从0开始,在添加点时自动生成
            var newPoint = new Point((UInt32)_points.Count, id, vector);
            _points.Add(newPoint);
        }
    }
}
