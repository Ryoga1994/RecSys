using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RecSys.Optics;
using System.Data;
using System.IO;

namespace RecSys
{
    /// <summary>
    /// 利用Optics聚类找中心点，再结合多核高斯模型和基于用户的协同过滤实现POI推荐
    /// </summary>
    class Optics_MGM_UCF
    {
        public DataSplitter ds = new DataSplitter();//all fields and methods relevant to Data IO should be included in DataSplitter

        Dictionary<string, Dictionary<string, int>> usercheckins;//train set
        Dictionary<string, Dictionary<string, int>> usercheckins_test;
        Dictionary<string, Tuple<double, double, string>> POIcategory;//包含train集中所有POI

        //记录train集中所有用户，两两之间的POI共现频率
        Dictionary<Tuple<string, string>, int> common;

        public void Initial(string train, string test)
        {
            //恢复已经产生的train集和test集，用于对比试验
            ds.Ini_retrieve("usercheckins_train_20160314.csv", "usercheckins_test_20160314.csv");

            usercheckins = ds.getSplittedData().train;
            usercheckins_test = ds.getSplittedData().test;
            POIcategory = ds.clean_poi;
            //load_common_all();
        }

        /// <summary>
        /// 返回train集中所有用户，两两之间的POI共现频率
        /// </summary>
        /// <returns></returns>
        public Dictionary<Tuple<string, string>, int> load_common_all()
        {
            if (!ds.isInitialized())
            {
                ds.Ini_retrieve("usercheckins_train_20160314.csv", "usercheckins_test_20160314.csv");
            }

            //用于存储所有具有共同访问项的用户对
            common = new Dictionary<Tuple<string, string>, int>();

            foreach (var item in ds.poi_user)//遍历poi_user倒排表
            {
                var users = item.Value.ToArray();//获得访问过该poi的所有用户列表
                for (int i = 0; i < (users.Length - 1); i++)
                {
                    for (int j = i + 1; j < users.Length; j++)
                    {
                        var pair = new Tuple<string, string>(users[i], users[j]);
                        if (!common.ContainsKey(pair))//当前用户对pair不在common字典内
                        {
                            common.Add(pair, 0);
                        }
                        common[pair]++;
                    }
                }
            }
            return common;
        }

        /// <summary>
        /// 传入poiid列表，返回由poiid，经纬度坐标组成的PointList，用于Optics聚类
        /// </summary>
        /// <param name="items">List（poiid）</param>
        /// <returns></returns>
        public PointsList create_points(List<string> items)
        {
            PointsList points = new PointsList();

            foreach (var item in items)
            {
                double[] pointVector = new double[2];
                //传入坐标点的经纬度
                pointVector[0] = POIcategory[item].Item1;
                pointVector[1] = POIcategory[item].Item2;
                //添加点到队列（poiid,[longitude latitude]）
                points.AddPoint(item, pointVector);
            }

            return points;

        }

        public DataTable Optics_poi(List<string> items, double maxRadius, int minPoints)
        {
            var points = create_points(items);
            //构造Optics聚类对象
            var optics = new Optics.OPTICS(maxRadius, minPoints, points);

            optics.BuildReachability();

            var reachablity = optics.ReachabilityPoints();

            DataTable dt = new DataTable();

            dt.Columns.Add("output_index");
            dt.Columns.Add("poiid");
            dt.Columns.Add("Reachablility");

            int counter = 0;

            foreach (var item in reachablity)
            {
                counter++;

                DataRow dr = dt.NewRow();
                dr["output_index"] = counter;
                dr["poiid"] = item.PointId;
                dr["Reachablility"] = item.Reachability;

                dt.Rows.Add(dr);
            }
            DataTableToCSV(dt, "D:/dissertation/data/result/Optics.csv");
            return dt;
        }

        /// <summary>
        /// 将DataTable输出为csv文件，并保存在指定路径下
        /// </summary>
        /// <param name="table"></param>
        /// <param name="filepath"></param>
        public void DataTableToCSV(DataTable table, string filepath)
        {
            string title = "";
            FileStream fs = new FileStream(filepath, FileMode.Create);
            StreamWriter sw = new StreamWriter(new BufferedStream(fs), System.Text.Encoding.Default);
            for (int i = 0; i < table.Columns.Count; i++)
            {
                title += table.Columns[i].ColumnName + ",";//获取列名
            }
            title = title.Substring(0, title.Length - 1) + "\n";

            sw.Write(title);

            foreach (DataRow row in table.Rows)
            {
                string line = "";
                for (int i = 0; i < table.Columns.Count; i++)
                {

                    line += row[i].ToString().Replace(",", " ") + ",";//字段中逗号都用空格replace
                }
                line = line.Substring(0, line.Length - 1) + "\n";
                sw.Write(line);
            }
            sw.Close();
            fs.Close();
        }

    }
}
