using RecSys.Optics.PriorityQueue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecSys.Optics
{
    public class OPTICS
    {
        private struct PointRelation //实例：存储一个指定中心点的邻域点
        {
            public readonly UInt32 To;//所指向的邻域点
            public readonly double Distance;//到中心点的距离

            public PointRelation(uint to, double distance)
            {
                this.To = to;
                this.Distance = distance;
            }
        }

        readonly Point[] _points;//样本集
        readonly double _eps;//邻域半径阈值
        readonly int _minPts;//定义核心对象的最小邻域点数MinPts
        //结果队列，用来存储样本点的输出次序，存储样本点的索引
        readonly List<UInt32> _outputIndexes;
        //有序队列：用于存储核心对象，及该核心对象的直接可达对象，并按可达距离升序排序
        readonly HeapPriorityQueue<Point> _seeds;

        /// <summary>
        /// 添加点到输出队列
        /// </summary>
        /// <param name="index"></param>
        private void AddOutputIndex(UInt32 index)
        {
            _outputIndexes.Add(index);
            if (_outputIndexes.Count % 250 == 0)
            {
                // TODO : add progress reporting interface
                Console.WriteLine("Progress {0}/{1}", _outputIndexes.Count, _outputIndexes.Capacity);
            }
        }

        /// <summary>
        /// Optics聚类构造函数
        /// </summary>
        /// <param name="eps">邻域半径阈值</param>
        /// <param name="minPts">定义核心对象的最小邻域点数MinPts</param>
        /// <param name="points">样本点数据集</param>
        public OPTICS(double eps, int minPts, PointsList points)
        {
            _points = points._points.ToArray();
            _eps = eps;
            _minPts = minPts;

            //创建新的结果队列对象
            _outputIndexes = new List<UInt32>(_points.Length);
            //创建长度 = 样本集长度的有序队列
            _seeds = new PriorityQueue.HeapPriorityQueue<Point>(_points.Length);

        }

        /// <summary>
        /// 计算两点间的欧几里得距离
        /// </summary>
        /// <param name="p1Index"></param>
        /// <param name="p2Index"></param>
        /// <returns></returns>
        public double EuclideanDistance(UInt32 p1Index, UInt32 p2Index)
        {
            double dist = 0;
            var vec1 = _points[p1Index].Vector;//存储用于计算距离的点属性
            var vec2 = _points[p2Index].Vector;

            for (int i = 0; i < vec1.Length; i++)
            {
                var diff = (vec1[i] - vec2[i]);
                dist += diff * diff;
            }

            return Math.Sqrt(dist);
        }

        // TODO add way to select which distance to use
        public double ManhatanDistance(UInt32 p1Index, UInt32 p2Index)
        {
            double dist = 0;
            var vec1 = _points[p1Index].Vector;
            var vec2 = _points[p2Index].Vector;

            for (int i = 0; i < vec1.Length; i++)
            {
                var diff = Math.Abs(vec1[i] - vec2[i]);
                dist += diff;
            }

            return dist;
        }

        /// <summary>
        /// 获取点p1的e-邻域内的所有点，并存在neighborhoodOut中输出
        /// </summary>
        /// <param name="p1Index"></param>
        /// <param name="neighborhoodOut">用于输出传入点的e-邻域内的点集合</param>
        private void GetNeighborhood(UInt32 p1Index, List<PointRelation> neighborhoodOut)
        {
            neighborhoodOut.Clear();

            //遍历样本集中的所有点
            for (UInt32 p2Index = 0; p2Index < _points.Length; p2Index++)
            {
                var distance = EuclideanDistance(p1Index, p2Index);

                if (distance <= _eps)
                {
                    //邻域集合存储格式<邻域点Index，到中心点的距离>
                    neighborhoodOut.Add(new PointRelation(p2Index, distance));
                }
            }
        }

        /// <summary>
        /// 通过e-邻域内的点列表，返回对象的核心距离 = 第minPts个点与中心的距离
        /// </summary>
        /// <param name="neighbors"></param>
        /// <returns></returns>
        private double CoreDistance(List<PointRelation> neighbors)
        {
            if (neighbors.Count < _minPts)//不满足核心对象条件
                return double.NaN;

            neighbors.Sort(pointComparison);//按照邻域内点到中心的距离 升序排序
            return neighbors[_minPts - 1].Distance;
        }

        //用于比较领域内点到中心点的距离，形成按距离升序的邻域点序列
        private static PointRelationComparison pointComparison = new PointRelationComparison();

        private class PointRelationComparison : IComparer<PointRelation>
        {
            public int Compare(PointRelation x, PointRelation y)
            {
                if (x.Distance == y.Distance)
                {
                    return 0;
                }
                return x.Distance > y.Distance ? 1 : -1;
            }
        }

        public void BuildReachability()
        {
            for (UInt32 pIndex = 0; pIndex < _points.Length; pIndex++)
            {
                if (_points[pIndex].WasProcessed)//点已经输出到结果队列
                    continue;

                List<PointRelation> neighborOfPoint = new List<PointRelation>();
                GetNeighborhood(pIndex, neighborOfPoint);//获取当前点的所有直接密度可达样本点

                _points[pIndex].WasProcessed = true;

                AddOutputIndex(pIndex);//将当前点放入结果队列中

                double coreDistance = CoreDistance(neighborOfPoint);//计算当前点的核心距离

                if (!double.IsNaN(coreDistance))//当前点满足核心对象条件
                {
                    _seeds.Clear();
                    Update(pIndex, neighborOfPoint, coreDistance);

                    List<PointRelation> neighborInner = new List<PointRelation>();
                    while (_seeds.Count > 0)//执行至顺序队列为空
                    {
                        UInt32 pInnerIndex = _seeds.Dequeue().Index;

                        GetNeighborhood(pInnerIndex, neighborInner);

                        _points[pInnerIndex].WasProcessed = true;

                        AddOutputIndex(pInnerIndex);

                        double coreDistanceInner = CoreDistance(neighborInner);

                        if (!double.IsNaN(coreDistanceInner))
                        {
                            Update(pInnerIndex, neighborInner, coreDistanceInner);
                        }
                    }
                }
            }
        }

        private void Update(UInt32 pIndex, List<PointRelation> neighbors, double coreDistance)
        {
            for (int i = 0; i < neighbors.Count; i++)//遍历邻域点集
            {
                UInt32 p2Index = neighbors[i].To;

                if (_points[p2Index].WasProcessed)
                    continue;

                //计算该邻域点到中心点pIndex的可达距离
                double newReachabilityDistance = Math.Max(coreDistance, neighbors[i].Distance);

                //如果该邻域点的可达距离不存在
                if (double.IsNaN(_points[p2Index].ReachabilityDistance))
                {
                    _points[p2Index].ReachabilityDistance = newReachabilityDistance;
                    //将该点加入有序队列
                    _seeds.Enqueue(_points[p2Index], newReachabilityDistance);
                }
                //该点的可达距离存在，则更新可达距离
                else if (newReachabilityDistance < _points[p2Index].ReachabilityDistance)
                {
                    _points[p2Index].ReachabilityDistance = newReachabilityDistance;
                    _seeds.UpdatePriority(_points[p2Index], newReachabilityDistance);
                }
            }
        }

        /// <summary>
        /// 输出结果队列,(点id，可达距离)
        /// </summary>
        /// <returns></returns>
        public IEnumerable<PointReachability> ReachabilityPoints()
        {
            foreach (var item in _outputIndexes)
            {
                yield return new PointReachability(_points[item].Id, _points[item].ReachabilityDistance);
            }
        }
    }
}
