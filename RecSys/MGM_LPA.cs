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
    /// combine Multi-Gaussian Model with LPA method to POI recommendation
    /// </summary>
    class MGM_LPA
    {
        public DataSplitter ds = new DataSplitter();//all fields and methods relevant to Data IO should be included in DataSplitter

        Dictionary<string, Dictionary<string, int>> usercheckins;//train set
        Dictionary<string, Dictionary<string, int>> usercheckins_test;
        Dictionary<string, Tuple<double, double, string>> POIcategory;


        /// <summary>
        /// load data set and split usercheckins into train and test set
        /// </summary>
        /// <param name="p"></param>
        public void Initial(double p = 0.3)
        {
            ds.Initial(p);
            usercheckins = ds.getSplittedData().train;
            usercheckins_test = ds.getSplittedData().test;
            POIcategory = ds.clean_poi;
        }

        #region methods for Center


        public Boolean contains(string cid, string poiid,double r)
        {
            if (DistanceOfTwoPoints(POIcategory[cid].Item1, POIcategory[cid].Item2,
                POIcategory[poiid].Item1, POIcategory[poiid].Item2) < r)
                return true;

            return false;
        }
        #endregion

        #region about center selection
        /// <summary>
        /// calculate center list for the given user
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="d">distance threshold as km</param>
        /// <param name="theta"></param>
        /// <returns></returns>
        public Dictionary<string, int> Center_list1(string uid, double d = 15, double theta = 0.02)
        {

            //check whether datasets have been loaded
            if (!ds.isInitialized())
            {
                Initial();
            }
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
            var center_list = new Dictionary<string, int>();

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
                        center_list.Add(poi_list[i], total_freq);

                        foreach (var item in curr_center)
                        {
                            POI_Center.Add(item, poi_list[i]);
                        }
                    }
                }
            }
            return center_list;
        }

        /// <summary>
        /// calculate center list for the given user
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="d">distance threshold as km</param>
        /// <param name="theta"></param>
        /// <returns>返回 center，mu_x,mu_y,sigma_x,sigma_y,checkinNum</returns>
        public Dictionary<string, Tuple<double, double, double, double, int>> Center_list3(string uid, double d = 15, double theta = 0.02)
        {

            //check whether datasets have been loaded
            if (!ds.isInitialized())
            {
                Initial();
            }
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
            var center_list = new Dictionary<string, Tuple<double,double,double,double,int>>();

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


        /// <summary>
        /// calculate center list in detail for the given user 
        /// </summary>
        /// <param name="uid"></param>
        /// <returns>returns list(center),包括center的id，空间范围，poi列表</returns>
        public List<Center> Center_list2(string uid, double d = 0.1, double theta = 0.05)
        {

            //check whether datasets have been loaded
            if (!ds.isInitialized())
            {
                Initial();
            }
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
            var center_list = new List<Center>();

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

                    //设定当前center的spatial range
                    rectangle rec = new rectangle(POIcategory[poi_list[i]].Item1,
                        POIcategory[poi_list[i]].Item2, POIcategory[poi_list[i]].Item1,
                        POIcategory[poi_list[i]].Item2);


                    for (int j = i + 1; j < poi_list.Count; j++)
                    {
                        if ((!POI_Center.ContainsKey(poi_list[j])) & (
                             DistanceOfTwoPoints(POIcategory[poi_list[i]].Item1, POIcategory[poi_list[i]].Item2,
                             POIcategory[poi_list[j]].Item1, POIcategory[poi_list[j]].Item2, GaussSphere.WGS84) < d))
                        {
                            curr_center.Add(poi_list[j]);
                            total_freq += usercheckins[uid][poi_list[j]];
                            //更新rec范围
                            rec.update(POIcategory[poi_list[j]].Item1, POIcategory[poi_list[j]].Item2);
                        }
                    }
                    if (total_freq >= (usercheckins[uid].Values.Sum() * theta))//满足center条件
                    {
                        //center_list.Add(poi_list[i], (total_freq / (usercheckins[uid].Values.Sum() + 0.0)));

                        var cent = new Center(poi_list[i],rec,curr_center);

                        center_list.Add(cent);

                        foreach (var item in curr_center)
                        {
                            POI_Center.Add(item, poi_list[i]);
                        }
                    }
                }
            }

            return center_list;
        }

        /// <summary>
        /// calculate coverage of POIs for each user 
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="r"></param>
        /// <param name="d"></param>
        /// <param name="theta"></param>
        /// <returns></returns>
        public Tuple<double, double,double,double> coverage1(string uid, double r, double d = 15, double theta = 0.02)
        {
            //check how much POI in test set has been covered
            if (!usercheckins_test.ContainsKey(uid))//在test集中没有签到记录
            {
                return null;
            }

            var center = Center_list1(uid, d, theta);
            int train = 0, test = 0, c_train = 0, c_test = 0;
            double cov_train, cov_test, check_train, check_test;

            foreach (var item in usercheckins[uid])
            {
                foreach (var cen in center)
                {
                    if (DistanceOfTwoPoints(POIcategory[item.Key].Item1, POIcategory[item.Key].Item2,
                        POIcategory[cen.Key].Item1, POIcategory[cen.Key].Item2) < r)
                    {
                        train++;
                        c_train += usercheckins[uid][item.Key];
                        break;
                    }
                }
            }

            foreach (var item in usercheckins_test[uid])
            {
                foreach (var cen in center)
                {
                    if (DistanceOfTwoPoints(POIcategory[item.Key].Item1, POIcategory[item.Key].Item2,
                        POIcategory[cen.Key].Item1, POIcategory[cen.Key].Item2) < r)
                    {
                        test++;
                        c_test += usercheckins_test[uid][item.Key];
                        break;
                    }
                }
            }

            cov_train = (train + 0.0) / usercheckins[uid].Values.Count;
            cov_test = (test + 0.0) / usercheckins_test[uid].Values.Count;
            check_train = (c_train + 0.0) / usercheckins[uid].Values.Sum();
            check_test = (c_test + 0.0) / usercheckins_test[uid].Values.Sum();

            Tuple<double, double, double, double> result = new Tuple<double, double, double, double>(cov_train, cov_test, check_train, check_test);
            return result;

        }

        /// <summary>
        /// check how much POIs have been covered by region
        /// region用multi-gauss概率阈值限定
        /// </summary> 
        /// <param name="uid"></param>
        /// <param name="p">高斯模型的概率阈值</param>
        /// <param name="d"></param>
        /// <param name="theta"></param>
        /// <returns>cov_poi_train,cov_poi_test,cov_checkin_train,cov_checkin_test</returns>
        public Tuple<double, double,double,double> coverage2(string uid, double p, double d = 15, double theta = 0.02)
        {
            //check how much POI in test set has been covered
            if (!usercheckins_test.ContainsKey(uid))//在test集中没有签到记录
            {
                return new Tuple<double, double, double, double>(double.NaN,double.NaN,double.NaN,double.NaN);
            }

            var center = Center_list3(uid, d, theta);
            int train = 0, test = 0, c_train = 0, c_test = 0;
            double cov_train, cov_test,check_train,check_test;

            foreach (var item in usercheckins[uid])//计算对train集的覆盖率
            {
                foreach (var cen in center)
                {
                    //该点属于对应中心的概率＞概率阈值
                     if (prob_gauss(POIcategory[item.Key].Item1, POIcategory[item.Key].Item2,
                        cen.Value.Item1, cen.Value.Item2, cen.Value.Item3, cen.Value.Item4) > p)
                    {
                        train++;
                        c_train += usercheckins[uid][item.Key];
                        break;
                    }
                }
            }

            foreach (var item in usercheckins_test[uid])//计算对test集的覆盖率
            {
                foreach (var cen in center)
                {
                    if (prob_gauss(POIcategory[item.Key].Item1, POIcategory[item.Key].Item2,
                        cen.Value.Item1, cen.Value.Item2, cen.Value.Item3, cen.Value.Item4) > p)
                    {
                        test++;
                        c_test += usercheckins_test[uid][item.Key];
                        break;
                    }
                }
            }

            cov_train = (train + 0.0) / usercheckins[uid].Values.Count;
            cov_test = (test + 0.0) / usercheckins_test[uid].Values.Count;
            check_train = (c_train + 0.0) / usercheckins[uid].Values.Sum();
            check_test = (c_test + 0.0) / usercheckins_test[uid].Values.Sum();

            Tuple<double, double,double,double> result = new Tuple<double, double,double,double>(cov_train, cov_test,check_train,check_test);
            return result;

        }

        /// <summary>
        /// check how much POIs have been covered by region
        /// region用每个center的spatial range表示
        /// </summary> 
        /// <param name="uid"></param>
        /// <param name="p">高斯模型的概率阈值</param>
        /// <param name="d"></param>
        /// <param name="theta"></param>
        /// <returns>cov_poi_train,cov_poi_test,cov_checkin_train,cov_checkin_test</returns>
        public Tuple<double, double, double, double> coverage3(string uid, double d = 15, double theta = 0.02)
        {
            //check how much POI in test set has been covered
            if (!usercheckins_test.ContainsKey(uid))//在test集中没有签到记录
            {
                return new Tuple<double, double, double, double>(double.NaN, double.NaN, double.NaN, double.NaN);
            }

            var center = Center_list2(uid, d, theta);
            int train = 0, test = 0, c_train = 0, c_test = 0;
            double cov_train, cov_test, check_train, check_test;

            foreach (var item in usercheckins[uid])//计算对train集的覆盖率
            {
                foreach (var cen in center)
                {
                    //该点属于对应中心
                    if (cen.contains(POIcategory[item.Key].Item1, POIcategory[item.Key].Item2))
                    {
                        train++;
                        c_train += usercheckins[uid][item.Key];
                        break;
                    }
                }
            }

            foreach (var item in usercheckins_test[uid])//计算对test集的覆盖率
            {
                foreach (var cen in center)
                {
                    if (cen.contains(POIcategory[item.Key].Item1, POIcategory[item.Key].Item2))
                    {
                        test++;
                        c_test += usercheckins_test[uid][item.Key];
                        break;
                    }
                }
            }

            cov_train = (train + 0.0) / usercheckins[uid].Values.Count;
            cov_test = (test + 0.0) / usercheckins_test[uid].Values.Count;
            check_train = (c_train + 0.0) / usercheckins[uid].Values.Sum();
            check_test = (c_test + 0.0) / usercheckins_test[uid].Values.Sum();

            Tuple<double, double, double, double> result = new Tuple<double, double, double, double>(cov_train, cov_test, check_train, check_test);
            return result;

        }

        /// <summary>
        /// calculate coverage for all users in train set
        /// 当距离半径为定值时的corverage
        /// </summary>
        /// <param name="r">radius of region</param>
        /// <param name="d">distance threshold for center selection</param>
        /// <param name="theta">threshold of checkin frequency</param>
        /// <returns></returns>
        public Dictionary<string, Tuple<double, double,double,double>> coverage1_all(double r, double d, double theta)
        {
            if (!ds.isInitialized())
            {
                Initial();
            }

            var user_cov = new Dictionary<string, Tuple<double, double,double,double>>();

            foreach (var user in usercheckins)
            {
                var item = coverage1(user.Key, r, d, theta);
                user_cov.Add(user.Key, item);
            }

            return user_cov;
        }

        /// <summary>
        /// calculate coverage for all users in train set
        /// 当距离半径用高斯分布概率阈值p限定时
        /// </summary>
        /// <param name="r">radius of region</param>
        /// <param name="d">distance threshold for center selection</param>
        /// <param name="theta">threshold of checkin frequency</param>
        /// <returns></returns>
        public Dictionary<string, Tuple<double, double,double,double>> coverage2_all(double p, double d, double theta)
        {
            if (!ds.isInitialized())
            {
                Initial();
            }

            var user_cov = new Dictionary<string, Tuple<double, double,double,double>>();

            foreach (var user in usercheckins)
            {
                var item = coverage2(user.Key, p, d, theta);
                user_cov.Add(user.Key, item);
            }

            return user_cov;
        }

        /// <summary>
        /// calculate coverage for all users in train set
        /// 当距离半径用高斯分布概率阈值p限定时
        /// </summary>
        /// <param name="r">radius of region</param>
        /// <param name="d">distance threshold for center selection</param>
        /// <param name="theta">threshold of checkin frequency</param>
        /// <returns></returns>
        public Dictionary<string, Tuple<double, double, double, double>> coverage3_all(double d, double theta)
        {
            if (!ds.isInitialized())
            {
                Initial();
            }

            var user_cov = new Dictionary<string, Tuple<double, double, double, double>>();

            foreach (var user in usercheckins)
            {
                var item = coverage3(user.Key, d, theta);
                user_cov.Add(user.Key, item);
            }

            return user_cov;
        }

        #endregion

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
        /// 返回指定用户在每个center访问每个cate的频率，利用固定半径r构建region
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="r">用于构建region的半径</param>
        /// <param name="d"></param>
        /// <param name="theta"></param>
        /// <returns>(center_id,dictionary(cate,checkinNum))</returns>
        public Dictionary<string, Dictionary<string, List<string>>> get_cen_cate_checkinNum1(string uid,double r,double d,double theta)
        {

            var center = Center_list1(uid, d, theta);

            var cen_cate_checkinNum = new Dictionary<string, Dictionary<string, List<string>>>();

            foreach (var item in usercheckins[uid])//遍历train集用户访问过的POI
            {
                foreach (var cen in center)//tip：一个POI可能属于多个中心
                {
                    //该POI属于该center = POI与center距离小于半径阈值
                    if (DistanceOfTwoPoints(POIcategory[item.Key].Item1, POIcategory[item.Key].Item2,
                        POIcategory[cen.Key].Item1, POIcategory[cen.Key].Item2) < r)
                    {
                        if (!cen_cate_checkinNum.ContainsKey(cen.Key))//中心未创建
                        {
                            cen_cate_checkinNum.Add(cen.Key, new Dictionary<string, List<string>>());
                        }
                        if (!cen_cate_checkinNum[cen.Key].ContainsKey(POIcategory[item.Key].Item3))//中心下该category未创建
                        {
                            cen_cate_checkinNum[cen.Key].Add(POIcategory[item.Key].Item3, new List<string>());
                        }
                        cen_cate_checkinNum[cen.Key][POIcategory[item.Key].Item3].Add(item.Key);
                    }
                }
            }
            return cen_cate_checkinNum;
        }

        /// <summary>
        /// 返回指定用户
        /// 1. 在每个center，每个category下面的访问概率cen_cate_checkinNum
        /// 2. 每个center的每个category下面的推荐候选项cen_cate_candidateItem
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="r">用于构建center region的半径阈值</param>
        /// <param name="d"></param>
        /// <param name="theta"></param>
        /// <returns>center_id,category2,list of candidateItems' id</returns>
        public Tuple<Dictionary<string, Dictionary<string, List<string>>>, Dictionary<string, Dictionary<string, List<string>>>>  get_cen_cate_x(string uid, double r, double d, double theta)
        {

            var cen_cate_checkinNum = get_cen_cate_checkinNum1(uid, r, d, theta);
            var cen_cate_candidateItem = new Dictionary<string, Dictionary<string, List<string>>>();

            var sorted =  from item in cen_cate_checkinNum orderby item.Value.Count descending select item;

            foreach (var item in POIcategory)//遍历所有poi = 很慢？
            {
                if (usercheckins[uid].ContainsKey(item.Key))//candidateItem不包含用户访问过的点
                {
                    continue;
                }
                foreach (var cen in sorted)
                {
                    //当前POI点与center距离小于半径r，且用户在该center曾经访问过该类型的POI
                    //Tips：在一个center中可能缺少特定类型的候选项
                    if ((DistanceOfTwoPoints(POIcategory[item.Key].Item1, POIcategory[item.Key].Item2,
                        POIcategory[cen.Key].Item1, POIcategory[cen.Key].Item2) < r) & (
                        cen_cate_checkinNum[cen.Key].ContainsKey(POIcategory[item.Key].Item3)))
                    {
                        if (!cen_cate_candidateItem.ContainsKey(cen.Key))//目标center未创建
                        {
                            cen_cate_candidateItem.Add(cen.Key, new Dictionary<string, List<string>>());
                        }
                        //目标center下的category未创建
                        if (!cen_cate_candidateItem[cen.Key].ContainsKey(POIcategory[item.Key].Item3))
                        {
                            cen_cate_candidateItem[cen.Key].Add(POIcategory[item.Key].Item3, new List<string>());
                        }
                        //将poi添加到指定center指定category下的候选项列表中
                        //为提高运算效率，仅将候选项添加到权重最大的center下
                        cen_cate_candidateItem[cen.Key][POIcategory[item.Key].Item3].Add(item.Key);
                        break;
                    }
                }
            }
            var result = new Tuple<Dictionary<string, Dictionary<string, List<string>>>, Dictionary<string, Dictionary<string, List<string>>>>(cen_cate_checkinNum, cen_cate_candidateItem);
            return result;
        }

        /// <summary>
        /// 返回指定数目的推荐项
        /// </summary>
        /// <param name="uid">目标user</param>
        /// <param name="r">region的半径阈值</param>
        /// <param name="d">center选择参数：距离阈值d</param>
        /// <param name="theta">center选择参数：频率阈值theta</param>
        /// <param name="n">返回的推荐项数目</param>
        public Dictionary<string,double> PA_candi_select(string uid, double r, double d, double theta,int n)
        {
            var tup = get_cen_cate_x(uid, r, d, theta);
            var cen_cate_checkinNum = tup.Item1;
            var cen_cate_candidateItem = tup.Item2;

            //center_id,category,poiid,interest
            var candidate = new Dictionary<string, double>();
            var result = new Dictionary<string, double>();
            

            //逻辑：候选集中存在的category，用户访问集中一定存在，而用户访问集合中的cate，候选集中不一定存在
            foreach (var cen in cen_cate_candidateItem)//遍历所有候选项
            {
                foreach(var cate in cen_cate_candidateItem[cen.Key])//遍历center中的每一类
                {
                    //用双向加强法，获取local expert
                    var items = cen_cate_candidateItem[cen.Key][cate.Key].ToList<string>();

                    //存储候选项，用户兴趣度计算结果
                    Dictionary<string, double> candi_interest = new Dictionary<string, double>();

                    var users = get_users_by_pois(cen_cate_candidateItem[cen.Key][cate.Key]);//获取访问过候选项的用户集合
                    int temp = cen_cate_checkinNum[cen.Key][cate.Key].Count;
                    items.AddRange(cen_cate_checkinNum[cen.Key][cate.Key]);
                    //参数可调整：获取的local experts数目 & 加强循环运行次数
                    var experts = getLocalExpertise(items, users, 100, 3);



                    //计算用户对每个候选项的评分
                    foreach (var user in experts)
                    {
                        foreach (var item in cate.Value)//遍历该center该category下的所有candidateItem
                        {
                            if (usercheckins[user.Key].ContainsKey(item))//用户曾经访问过候选项
                            {
                                if (!candi_interest.ContainsKey(item))
                                {
                                    candi_interest.Add(item, 0.0);
                                }
                                candi_interest[item] += similar_UCF(uid, user.Key, items) * usercheckins[user.Key][item];
                            }
                        }
                    }
                    //将获得结果添加到candidate中
                    //该center下该cate返回的候选项数目 = 所需推荐项数目*用户在该center该cate签到次数/用户签到总次数
                    //int k =Convert.ToInt32( n * cen_cate_checkinNum[cen.Key][cate.Key] / (usercheckins[uid].Values.Sum()+0.0))+1;
                    //int counter = 0;
                    

                    foreach (var item in candi_interest)
                    {
                        if (!candidate.ContainsKey(item.Key))
                        {
                            candidate.Add(item.Key, 0.0);
                        }
                        //用user访问该center的可能性加权？？
                        candidate[item.Key] += item.Value;
                    }
                }            
            }
            //返回指定数目的推荐
            var sortedCandi = from item in candidate orderby item.Value descending select item;

            int counter = 0;

            foreach (var item in sortedCandi)
            {
                if (item.Value > 0 && counter < n)
                {
                    result.Add(item.Key, item.Value);
                    counter++;
                }
                else
                {
                    break;
                }
            }
            return result;
        }

        public void recommend_all(double r, double d, double theta, int n)
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
                var dic = PA_candi_select(user.Key, r, d, theta, n);

                foreach (var item in dic)
                {
                    DataRow dr = dt.NewRow();

                    dr["uid"] = user.Key;
                    dr["poiid"] = item.Key;
                    dr["interest"] = item.Value;

                    dt.Rows.Add(dr);                 
                }
                user_count++;

                if ((user_count % 300) == 0)//每500个用户保存一次
                {
                    DataTableToCSV(dt, "D:/dissertation/data/result/recommendation_r_LAP_20160314_" + user_count + ".csv");
                }
            }

            DataTableToCSV(dt, "D:/dissertation/data/result/recommendation_r_LAP_20160314_" + user_count + ".csv");

        }


        /// <summary>
        /// 给定一个poi列表，通过poi_user倒排表，获取所有访问过这些poi的user集合
        /// </summary>
        /// <param name="pois"></param>
        /// <returns>users</returns>
        public List<string> get_users_by_pois(List<string> pois)
        {
            List<string> users = new List<string>();

            foreach (var poi in pois)
            {
                var user = ds.poi_user[poi];
                foreach(var us in user)
                {
                    if (!users.Contains(us))
                    {
                        users.Add(us);
                    }
                }              
            }
            return users;
        }

        /// <summary>
        /// return a list of uid contains load experts for the item and user list
        /// </summary>
        /// <param name="item_list">指定center中包含的特定类别的poi，包括目标用户访问过的&推荐候选项</param>
        /// <param name="user_list">待筛选的local experts集合</param>
        /// <param name="n">返回的local experts数目</param>
        /// <param name="iter">加强循环的运行次数</param>
        /// <returns>dic(experts,interest)</experts></returns>
        public Dictionary<string, double> getLocalExpertise(List<string> item_list, List<string> user_list, int n, int iter)
        {

            var user_hub = new Dictionary<string, double>();
            var venue_authority = new Dictionary<string, double>();

            //initialize venues' authority score & users' hub score = number of users visited this venue
            //初始化：默认用户对所有类别的权威程度都为1
            foreach (var user in user_list)
            {
                user_hub.Add(user, 1.0);
            }
            foreach (var item in item_list)
            {
                venue_authority.Add(item, 0.0);
            }

            //iterative 
            for (int i = 0; i < iter; i++)
            {
                //update venues' authority score
                foreach (var ven in item_list)
                {
                    double a_score = 0;

                    var user_vistited = ds.poi_user[ven];

                    foreach (var user in user_vistited)//遍历访问过该地点的用户
                    {
                        if (user_list.Contains(user))//该用户在local experts候选项内
                        {
                            a_score += user_hub[user];
                        }                       
                    }                                       
                    
                    venue_authority[ven] = a_score;
                }

                //update users' hub score
                foreach (var user in user_list)
                {

                    double hub_score = 0;

                    foreach (var item in usercheckins[user])//遍历候选用户访问过的每个poi
                    {
                        if (venue_authority.ContainsKey(item.Key))
                        {
                            //hub = 用户访问过的所有该category的POIs的权威分之和
                            hub_score += venue_authority[item.Key];
                        }
                    }

                    user_hub[user] = hub_score;
                }
            }

            var sorted_user = from item in user_hub orderby item.Value descending select item;
            var result = new Dictionary<string, double>();           

            int counter = 0;

            foreach (var user in sorted_user)
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
        /// 以给定的Item列表为依据，计算两用户之间的相似度
        /// </summary>
        /// <param name="uid1"></param>
        /// <param name="uid2"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        public double similar_UCF(string uid1, string uid2,List<string> items)
        {
            //利用jaccard相似度计算
            int union = 0;
            int u1 = usercheckins[uid1].Count;
            int u2 = usercheckins[uid2].Count;
            foreach (var item in items)
            {
                //u1,u2都访问过该item
                if (usercheckins[uid1].ContainsKey(item) && usercheckins[uid2].ContainsKey(item))
                {
                    union++;
                }
            }

            double J = (union + 0.0) / (u1 + u2 - union);
            return J;
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

        #region analysis


        public void recall_precision_all(string filepath)
        {
            //导入推荐结果文件
            ds.Ini_analysis("recommendation_r_LAP_20160314_4800.csv");
            
        }

        #endregion
    }
}
