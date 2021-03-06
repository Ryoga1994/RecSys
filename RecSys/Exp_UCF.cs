﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecSys
{
    /// <summary>
    /// 指数函数结合基于用户的协同过滤进行推荐
    /// </summary>
    class Exp_UCF
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
            load_common_all();
        }

        /// <summary>
        /// 返回train集中所有用户，两两之间的POI共现频率
        /// </summary>
        /// <returns></returns>
        public Dictionary<Tuple<string, string>, int> load_common_all()
        {
            if (ds.isRetrieved == false)
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
        /// 计算两个用户间的Jaccard相似度
        /// </summary>
        /// <param name="uid1"></param>
        /// <param name="uid2"></param>
        /// <returns></returns>
        public double Jaccard(string uid1, string uid2)
        {
            Tuple<string, string> pair1 = new Tuple<string, string>(uid1, uid2);
            Tuple<string, string> pair2 = new Tuple<string, string>(uid2, uid1);

            double com = 0;
            if (common.ContainsKey(pair1))
            {
                com += common[pair1];
            }
            if (common.ContainsKey(pair2))
            {
                com += common[pair2];
            }

            double J = (com + 0.0) / (usercheckins[uid1].Count + usercheckins[uid2].Count - com);
            return J;

        }

        /// <summary>
        /// 为指定用户返回K个最相似的用户
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="k"></param>
        /// <returns></returns>
        public Dictionary<string, double> get_top_users(string uid, int k)
        {
            var dic = new Dictionary<string, double>();

            foreach (var user in usercheckins)//遍历所有用户
            {
                if (user.Key == uid)
                {
                    continue;
                }
                var simi = Jaccard(uid, user.Key);

                if (simi >= 0)//不限定similarity
                {
                    dic.Add(user.Key, simi);
                }
            }
            var sorted = from item in dic orderby item.Value descending select item;

            var result = new Dictionary<string, double>();

            int counter = 0;

            foreach (var user in sorted)
            {
                if (counter < k)
                {
                    result.Add(user.Key, user.Value);
                    counter++;
                }
            }

            //return Dictionary<uid,hub_score>
            return result;
        }

        /// <summary>
        /// 为指定用户返回推荐程度最高的n个候选项
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public Dictionary<string, double> get_candidateItems(string uid, int n)
        {
            var simi_users = get_top_users(uid, 100);//获取的相似用户数目

            var simi_items = new Dictionary<string, double>();

            foreach (var user in simi_users)
            {
                foreach (var item in usercheckins[user.Key])//遍历相似用户访问过的所有POI
                {
                    if (usercheckins[uid].ContainsKey(item.Key))//如果目标用户曾经访问过该POI
                    {
                        continue;
                    }
                    if (!simi_items.ContainsKey(item.Key))
                    {
                        simi_items.Add(item.Key, 0.0);
                    }
                    //计算评分
                    simi_items[item.Key] += Jaccard(uid, user.Key) * usercheckins[user.Key][item.Key];
                }
            }
            var sorted = from item in simi_items orderby item.Value descending select item;

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

            //return Dictionary<uid,hub_score>
            return result;

        }

        /// <summary>
        /// 对给定的uid，候选项集计算指数分布评分
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="n">返回的推荐项数目</param>
        /// <param name="a">User CF评分占比</param>
        /// <param name="b">指数函数评分占比</param>
        /// <param name="d">center选择的距离阈值d</param>
        /// <param name="theta">center选择的签到频率阈值</param>
        /// <returns></returns>
        public Dictionary<string, double> predict_exp(string uid, int n,
            double a, double b)
        {
            //修改获得的候选项集合个数
            var candi = get_candidateItems(uid, 200);
            //获得当前用户的幂律分布参数
            var param = exponential(uid);//默认带宽 = 1km，步长0.1

            var temp = param[0];
            var temp1 = param[1];

            var pl_dic = new Dictionary<string, double>();

            //计算每个候选项的幂律分布评分
            foreach (var item in candi)//遍历每个候选项
            {
                pl_dic.Add(item.Key, 0.0);

                //计算候选项的高斯评分
                foreach (var poi in usercheckins[uid])
                {
                    //
                    var t = pl_dic[item.Key];

                    pl_dic[item.Key] += prob_pl(DistanceOfTwoPoints(POIcategory[item.Key].Item1,
                        POIcategory[item.Key].Item2, POIcategory[poi.Key].Item1,
                        POIcategory[poi.Key].Item2), param);
                }

                var te = pl_dic[item.Key];
            }

            //综合gauss和ucf评分
            foreach (var item in candi)
            {

                pl_dic[item.Key] = pl_dic[item.Key] * b + candi[item.Key] * a;
                var t3 = pl_dic[item.Key];

            }

            var sorted = from item in pl_dic orderby item.Value descending select item;

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

        public List<double> get_distance_vec(string uid)
        {
            //创建用户访问的poi完全列表 = 访问的POI点*签到次数 的总和
            List<string> checkinList = new List<string>();

            foreach (var item in usercheckins[uid])//遍历所有签到点
            {
                for (int i = 1; i <= usercheckins[uid][item.Key]; i++)
                {
                    checkinList.Add(item.Key);
                }
            }

            int n = checkinList.Count;//记录总数

            List<double> dis = new List<double>();

            for (int i = 0; i < n - 1; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    //以两点间距离（km）表示
                    dis.Add(DistanceOfTwoPoints(POIcategory[checkinList[i]].Item1,
                        POIcategory[checkinList[i]].Item2, POIcategory[checkinList[j]].Item1,
                        POIcategory[checkinList[j]].Item2));
                }
            }
            return dis;
        }

        /// <summary>
        /// 给定变量 x = 距离
        /// </summary>
        /// <param name="dis"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public double prob_pl(double dis, double[] param)
        {
            //传入参数由C和alpha组成
            //如果传入参数过小,为避免概率产生无穷大，影响实验结果
            if (dis < 0.5)
            {
                dis = 0.5;
            }

            double prob = param[0] * Math.Exp(-param[1] * dis);
            return prob;
        }


        /// <summary>
        /// 返回用于计算幂律分布公式的参数c和alpha
        /// </summary>
        /// <param name="uid"></param>
        /// <returns>给定用户的幂律分布参数(C,alpha)</returns>
        public double[] exponential(string uid, double width = 1.0, double xmin = 0.5)
        {
            //获取用户的概率密度函数
            var co = DF(uid, width, xmin);
            double[] x = co.Item1;
            double[] y = co.Item2;

            double[] theta = new double[2];
            theta[0] = 1; theta[1] = 1;//初始化theta为1

            //x,y变量取对数
            //double[] xval = new double[x.Length];
            double[] yval = new double[y.Length];

            for (int i = 0; i < x.Length; i++)
            {
                //xval[i] = Math.Log(x[i]);
                yval[i] = Math.Log(y[i]);
            }

            //设置学习度（步长）为0.5，循环次数为1000           
            var J_his = gradientDescent(x, yval, theta, 0.01, 1000);

            var temp = theta[0];
            var t2 = theta[1];

            //从线性拟合参数变形至幂律分布参数
            double C = Math.Exp(theta[0]);
            double alpha = -theta[1];

            double[] result = new double[2];
            result[0] = C;
            result[1] = alpha;

            return result;

        }


        /// <summary>
        /// 获取指定用户，poi点对间距离与签到概率的 概率密度曲线
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="width">带宽，用于平滑概率密度曲线</param>
        /// <param name="xmin">距离变量的最小起始值，防止取对数后得到无穷大</param>
        public Tuple<double[], double[]> DF(string uid, double width = 1.0, double xmin = 0.5)
        {
            var dis = get_distance_vec(uid);

            int n = dis.Count;



            double max = dis.Max();

            var temp = dis.Distinct<double>();
            var p = from d in temp where d >= xmin orderby d select d;

            double[] xval = new double[p.Count<double>()];

            int index = 0;

            foreach (var item in p)
            {
                //if (item < xmin)//除去小于x阈值的横坐标
                //    continue;

                xval[index] = item;
                index++;
            }

            var yval = new double[xval.Length];

            for (int i = 1; i <= xval.Length; i++)
            {

                //xval[i - 1] = max / 1000 * i;

                var q = from double d in dis
                        where d > (xval[i - 1] - width) && d < (xval[i - 1] + width)
                        select d;


                //频率分布
                //var t = q.Count<double>();
                yval[i - 1] = q.Count<double>() / (dis.Count + 0.0);//概率
                                                                    //var t1 = yval[i - 1];

            }

            Tuple<double[], double[]> result = new Tuple<double[], double[]>(xval, yval);


            return result;
        }



        /// <summary>
        /// 利用传入的变量x,y返回线性拟合参数theta和cost function
        /// </summary>
        /// <param name="xval"></param>
        /// <param name="yval"></param>
        /// <param name="theta">返回的参数，theta1和theta2</param>
        /// <param name="alpha">学习率，控制梯度下降的步长</param>
        /// <param name="iters">指定循环次数</param>
        /// <returns></returns>
        public double[] gradientDescent(double[] xval, double[] yval, double[] theta,
            double alpha, int iters)
        {
            int num = xval.Length;

            double[] J_his = new double[iters];

            for (int i = 0; i < iters; i++)
            {
                double th1 = 0.0, th2 = 0.0;
                for (int k = 0; k < num; k++)//x,y变量中的元素个数
                {
                    th1 += (theta[0] + theta[1] * xval[k]) - yval[k];
                    th2 += (theta[0] + theta[1] * xval[k]) - yval[k];
                }
                //更新theta
                theta[0] -= alpha / num * th1;
                var temp = theta[0];
                theta[1] -= alpha / num * th2;
                //计算当前theta下的cost function
                J_his[i] = computeCost(xval, yval, theta);
            }

            return J_his;

        }

        /// <summary>
        /// 给定theta，变量x,y计算拟合线性的cost fuction
        /// </summary>
        /// <param name="xval"></param>
        /// <param name="yval"></param>
        /// <param name="theta"></param>
        /// <returns></returns>
        public double computeCost(double[] xval, double[] yval, double[] theta)
        {
            double J = 0.0;

            int num = xval.Length;

            for (int i = 0; i < num; i++)
            {
                J += Math.Pow((theta[0] + theta[1] * xval[i]) - yval[i], 2);
            }

            var temp = J;
            J = (1.0 / (2 * num)) * J;

            return J;
        }

        public void recommend_all(int n, double a, double b)
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
                var dic = predict_exp(user.Key, n, a, b);

                foreach (var item in dic)
                {
                    DataRow dr = dt.NewRow();

                    dr["uid"] = user.Key;
                    dr["poiid"] = item.Key;
                    dr["interest"] = item.Value;

                    dt.Rows.Add(dr);
                }
                user_count++;

                //if ((user_count % 4800) == 0)//每___个用户保存一次
                //{
                //    DataTableToCSV(dt, "D:/dissertation/data/result/recommendation_PL_UCF_20160317_" + user_count + ".csv");
                //}
            }

            DataTableToCSV(dt, "D:/dissertation/data/result/recommendation_EXP_UCF_20160317_" + n + ".csv");
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
