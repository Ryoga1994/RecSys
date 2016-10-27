using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecSys
{
    /// <summary>
    /// 仅利用多核高斯模型MGM进行推荐
    /// </summary>
    class MGM
    {
        public DataSplitter ds = new DataSplitter();//all fields and methods relevant to Data IO should be included in DataSplitter

        Dictionary<string, Dictionary<string, int>> usercheckins;//train set
        Dictionary<string, Dictionary<string, int>> usercheckins_test;
        Dictionary<string, Tuple<double, double, string>> POIcategory;//包含train集中所有POI

        //记录train集中所有用户，两两之间的POI共现频率
        //Dictionary<Tuple<string, string>, int> common;

        public void Initial(string train, string test)
        {
            //恢复已经产生的train集和test集，用于对比试验
            ds.Ini_retrieve(train, test);

            usercheckins = ds.getSplittedData().train;
            usercheckins_test = ds.getSplittedData().test;
            POIcategory = ds.clean_poi;
            //load_common_all();
        }

        /// <summary>
        /// 计算指定用户的中心列表，并返回相应的高斯核函数参数
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="d">distance threshold as km</param>
        /// <param name="theta"></param>
        /// <returns>返回 center，mu_x,mu_y,sigma_x,sigma_y,checkinNum</returns>
        public Dictionary<string, Tuple<double, double, double, double, int>> Center_list3(string uid, double d = 15, double theta = 0.02)
        {
            //sort user's check-in POI by check-in frequency
            var temp = from item in usercheckins[uid] orderby item.Value descending select item;

            var poi_list = new List<string>();

            foreach (var item in temp)
            {
                poi_list.Add(item.Key);
            }

            //store POIs which have already been clustered
            Dictionary<string, string> POI_Center = new Dictionary<string, string>();

            //retult list store all <centers,check-in probability> for the given uid
            var center_list = new Dictionary<string, Tuple<double, double, double, double, int>>();

            //temp center list for a center
            var curr_center = new List<string>();


            int center_no = 0;//当前 center 的index
            int total_freq = 0;//指定 user 在该 center 的签到数目

            for (int i = 0; i < poi_list.Count; i++)
            {
                if (!POI_Center.ContainsKey(poi_list[i]))
                {
                    //reset regional variables
                    center_no++;
                    curr_center.Clear();
                    total_freq = 0;

                    curr_center.Add(poi_list[i]);
                    total_freq += usercheckins[uid][poi_list[i]];

                    for (int j = i + 1; j < poi_list.Count; j++)
                    {
                        if ((!POI_Center.ContainsKey(poi_list[j])) & (
                            DistanceOfTwoPoints(POIcategory[poi_list[i]].Item1, POIcategory[poi_list[i]].Item2,
                            POIcategory[poi_list[j]].Item1, POIcategory[poi_list[j]].Item2, GaussSphere.WGS84) < d))
                        {
                            curr_center.Add(poi_list[j]);
                            total_freq += usercheckins[uid][poi_list[j]];
                        }
                    }
                    if (total_freq >= (usercheckins[uid].Values.Sum() * theta))//满足center的条件
                    {
                        //center_list.Add(poi_list[i], (total_freq / (usercheckins[uid].Values.Sum() + 0.0)));
                        //center_list.Add(poi_list[i], new Tuple<double, double, double, double, int>());

                        //计算高斯分布 
                        double mu_x = 0.0, mu_y = 0.0, sigma_x = 0.0, sigma_y = 0.0;

                        foreach (var item in curr_center)//求均值
                        {
                            mu_x += POIcategory[item].Item1 * usercheckins[uid][item];//longi均值
                            mu_y += POIcategory[item].Item2 * usercheckins[uid][item];//lati均值
                            POI_Center.Add(item, poi_list[i]);
                        }
                        mu_x = mu_x / total_freq;
                        mu_y = mu_y / total_freq;

                        foreach (var item in curr_center)//求方差
                        {
                            sigma_x += Math.Pow(POIcategory[item].Item1 - mu_x, 2) * usercheckins[uid][item];
                            sigma_y += Math.Pow(POIcategory[item].Item2 - mu_y, 2) * usercheckins[uid][item];
                        }
                        sigma_x = Math.Sqrt(sigma_x / total_freq);
                        sigma_y = Math.Sqrt(sigma_y / total_freq);

                        center_list.Add(poi_list[i], new Tuple<double, double, double, double, int>
                            (mu_x, mu_y, sigma_x, sigma_y, total_freq));
                    }
                }
            }
            return center_list;
        }

        #region multi-gaussian distribution
        /// <summary>
        /// 通过经纬度坐标，计算点属于给定高斯分布的概率
        /// </summary>
        /// <param name="longi"></param>
        /// <param name="lati"></param>
        /// <param name="mu_x"></param>
        /// <param name="mu_y"></param>
        /// <param name="sigma_x"></param>
        /// <param name="sigma_y"></param>
        /// <returns></returns>
        public double prob_gauss(double longi, double lati, double mu_x, double mu_y,
            double sigma_x, double sigma_y)
        {
            //对于方差为0的情况，加系数以防止高斯分布概率的不合理计算
            sigma_x = (sigma_x == 0 ? 0.3 : sigma_x);
            sigma_y = (sigma_y == 0 ? 0.3 : sigma_y);

            double ratio_x = (longi - mu_x) / sigma_x;
            double ratio_y = (lati - mu_y) / sigma_y;

            //转换成正态分布计算概率
            double p = 1 / (2 * Math.PI) * Math.Exp((-0.5) * (Math.Pow(ratio_x, 2) + Math.Pow(ratio_y, 2)));


            return p;
        }


        #endregion

        /// <summary>
        /// 对给定的uid，候选项集计算gaussian评分
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="n">返回的推荐项数目</param>
        /// <param name="a">User CF评分占比</param>
        /// <param name="b">Gaussian评分占比</param>
        /// <param name="d">center选择的距离阈值d</param>
        /// <param name="theta">center选择的签到频率阈值</param>
        /// <returns></returns>
        public Dictionary<string, double> predict_gauss(string uid, int n,
            double d = 15, double theta = 0.02)
        {
            //修改获得的候选项集合个数
            //var candi = get_candidateItems(uid, 200);

            var gauss_dic = new Dictionary<string, double>();

            var centers = Center_list3(uid, d, theta);

            int n_checkin = 0;//用户在各个center的checkin总次数

            foreach (var cen in centers)
            {
                n_checkin += cen.Value.Item5;
            }

            //计算每个候选项的高斯位置评分
            foreach (var item in POIcategory)//遍历每个候选项
            {
                if (usercheckins[uid].ContainsKey(item.Key))
                {
                    continue;
                }

                gauss_dic.Add(item.Key, 0.0);

                foreach (var cen in centers)//遍历用户的每个高斯核
                {
                    //当前POI属于当前高斯核的概率
                    double score = prob_gauss(POIcategory[item.Key].Item1, POIcategory[item.Key].Item2,
                        cen.Value.Item1, cen.Value.Item2, cen.Value.Item3, cen.Value.Item4);
                    //位置评分 = 高斯核函数概率*该高斯核的签到权重
                    gauss_dic[item.Key] += score * cen.Value.Item5 / n_checkin;
                }
            }

            //综合gauss和ucf评分
            //foreach (var item in candi)
            //{

            //    gauss_dic[item.Key] = gauss_dic[item.Key] * b + candi[item.Key] * a;

            //}

            var sorted = from item in gauss_dic orderby item.Value descending select item;

            var result = new Dictionary<string, double>();

            int counter = 0;

            foreach (var user in sorted)
            {
                if (counter < n)
                {
                    result.Add(user.Key, user.Value);
                    counter++;
                }
            }

            return result;

        }

        /// <summary>
        /// 对给定的uid，候选项集计算gaussian评分
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="n">返回的推荐项数目</param>
        /// <param name="a">User CF评分占比</param>
        /// <param name="b">Gaussian评分占比</param>
        /// <param name="d">center选择的距离阈值d</param>
        /// <param name="theta">center选择的签到频率阈值</param>
        /// <returns></returns>
        public DataTable predict_gauss3(string uid, int n,
            double d = 15, double theta = 0.02)
        {
            //修改获得的候选项集合个数
            //var candi = get_candidateItems(uid, 200);

            var gauss_dic = new Dictionary<string, double>();

            var centers = Center_list3(uid, d, theta);

            int n_checkin = 0;//用户在各个center的checkin总次数

            foreach (var cen in centers)
            {
                n_checkin += cen.Value.Item5;
            }

            //计算每个候选项的高斯位置评分
            foreach (var item in POIcategory)//遍历每个候选项
            {
                if (usercheckins[uid].ContainsKey(item.Key))
                {
                    continue;
                }

                gauss_dic.Add(item.Key, 0.0);

                foreach (var cen in centers)//遍历用户的每个高斯核
                {
                    //当前POI属于当前高斯核的概率
                    double score = prob_gauss(POIcategory[item.Key].Item1, POIcategory[item.Key].Item2,
                        cen.Value.Item1, cen.Value.Item2, cen.Value.Item3, cen.Value.Item4);
                    //位置评分 = 高斯核函数概率*该高斯核的签到权重
                    gauss_dic[item.Key] += score * cen.Value.Item5 / n_checkin;
                }
            }

            //MGM高斯评分正则化
            double p_max = gauss_dic.Values.Max();
            var key2 = gauss_dic.Keys.ToArray<string>();

            foreach (var item in key2)
            {
                gauss_dic[item] = gauss_dic[item] / p_max;//评分正则化
                
            }

            var sorted = from item in gauss_dic orderby item.Value descending select item;

            var result = new Dictionary<string, double>();

            int counter = 0;

            DataTable dt = new DataTable();
            dt.Columns.Add("uid");
            dt.Columns.Add("poiid");
            dt.Columns.Add("interest");

            foreach (var user in sorted)
            {
                if (counter < n)
                {
                    //result.Add(user.Key, user.Value);

                    DataRow dr = dt.NewRow();
                    dr["uid"] = uid;
                    dr["poiid"] = user.Key;
                    dr["interest"] = user.Value;

                    dt.Rows.Add(dr);
                    counter++;
                }
            }

            return dt;

        }


        public void recommend_all(int n, /*double a, double b, */double d, double theta)
        {

            var rec = new Dictionary<string, Dictionary<string, double>>();

            //show dictionary to DataTable
            DataTable dt = new DataTable();

            dt.Columns.Add("uid");
            dt.Columns.Add("poiid");
            dt.Columns.Add("interest");

            int user_count = 0;

            foreach (var user in usercheckins)
            {
                var dic = predict_gauss(user.Key, n,/* a, b,*/ d, theta);

                foreach (var item in dic)
                {
                    DataRow dr = dt.NewRow();

                    dr["uid"] = user.Key;
                    dr["poiid"] = item.Key;
                    dr["interest"] = item.Value;

                    dt.Rows.Add(dr);
                }
                user_count++;

                //if ((user_count % 500) == 0)//每___个用户保存一次
                //{
                //    DataTableToCSV(dt, "D:/dissertation/data/result/recommendation_MGM_20160319_" + user_count + ".csv");
                //}

            }

            DataTableToCSV(dt, "D:/dissertation/data/result/recommendation_MGM_20160320_" + n + ".csv");

        }

        #region 工具集：输出csv，计算距离（km）

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

        #region 计算两坐标点间的距离
        /// <summary>
        /// 计算两坐标点间的距离，返回以米（m）为单位的距离
        /// </summary>
        /// <param name="lng1"></param>
        /// <param name="lat1"></param>
        /// <param name="lng2"></param>
        /// <param name="lat2"></param>
        /// <param name="gs"></param>
        /// <returns></returns>
        public static double DistanceOfTwoPoints(double lng1, double lat1, double lng2, double lat2, GaussSphere gs = GaussSphere.WGS84)
        {
            double radLat1 = Rad(lat1);
            double radLat2 = Rad(lat2);
            double a = radLat1 - radLat2;
            double b = Rad(lng1) - Rad(lng2);
            double s = 2 * Math.Asin(Math.Sqrt(Math.Pow(Math.Sin(a / 2), 2) +
             Math.Cos(radLat1) * Math.Cos(radLat2) * Math.Pow(Math.Sin(b / 2), 2)));
            s = s * (gs == GaussSphere.WGS84 ? 6378137.0 : (gs == GaussSphere.Xian80 ? 6378140.0 : 6378245.0));
            s = Math.Round(s * 10000) / 10000000;
            return s;
        }

        private static double Rad(double d)
        {
            return d * Math.PI / 180.0;
        }

        //GaussSphere 为自定义枚举类型
        /// <summary>
        /// 高斯投影中所选用的参考椭球
        /// </summary>
        public enum GaussSphere
        {
            Beijing54,
            Xian80,
            WGS84,
        }
        #endregion

        #endregion

    }
}
