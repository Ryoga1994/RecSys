using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecSys
{
    class DataSplitter
    {
        //class to input data and split into train dataset and test dataset
        private Boolean initialized = false;//indicate whether data has been loaded and splittedb
        public Boolean isRetrieved = false;
        public Boolean have_ini_analysis = false;//表示系统是否导入推荐结果文件
        public splittedData splitted_usercheckin;
        public Dictionary<string, List<string>> poi_user;
        public Dictionary<string, Tuple<double, double, string>> clean_poi;//train集中包含的POI数目
        public Dictionary<string, Dictionary<string, double>> rec;

        /// <summary>
        /// 初始化算法环境，导入usercheckin数据，POI数据
        /// </summary>
        /// <param name="p">测试集Test中的数据比例</param>
        public void Initial(double p)
        {
            splitted_usercheckin = splitter(p);//初始化usercheckins的train集/test集
            clean_poi = get_clean_poi();//生成clean_poi同时生成物品-用户倒排表poi_user
            initialized = true;
        }

        /// <summary>
        /// 恢复已经随机生成的train集，test集, 基于train集生成poi表，poi_user倒排表
        /// </summary>
        /// <param name="train">train集文件名</param>
        /// <param name="test">test集文件名</param>
        public void Ini_retrieve(string train,string test)
        {
            splitted_usercheckin = retrieve_usercheckin(train, test);
            clean_poi = get_clean_poi();
            isRetrieved = true;
        }

        /// <summary>
        /// 初始化实验分析环境（推荐结果）
        /// </summary>
        /// <param name="fileName"></param>
        public void Ini_analysis(string fileName)
        {
            if (splitted_usercheckin == null)
            {
                splitted_usercheckin = retrieve_usercheckin("usercheckins_train_20160325_0.1.csv", "usercheckins_test_20160325_0.1.csv");
            }
            rec = load_recommend_file(fileName);
            have_ini_analysis = true;
        }

        public void Ini_ana_DB(string table, double a, double b)
        {
            if (splitted_usercheckin == null)
            {
                splitted_usercheckin = retrieve_usercheckin("usercheckins_train_20160314.csv", "usercheckins_test_20160314.csv");
            }
            rec = Rec_DB(table, a, b);
            have_ini_analysis = true;
        }


        //getter : check whether the data has been loaded and splitted
        public Boolean isInitialized()
        {
            return this.initialized;
        }

        public splittedData getSplittedData()
        {
            return this.splitted_usercheckin;
        }

        #region load data
        /// <summary>
        /// load usercheckin dataset from access database
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>Dictionary(uid, Dictionary(poiid,checkinNum))</returns>
        public Dictionary<string, Dictionary<string, int>> getUsercheckins2()
        {

            Dictionary<string, Dictionary<string, int>> user_checkins = new Dictionary<string, Dictionary<string, int>>();
            

            //建立数据库连接
            string filepath = "D:\\dissertation\\data\\MGM\\database20160309.accdb";
            string strdb = "Provider=Microsoft.ACE.OLEDB.12.0;Data source=" + filepath;
            OleDbConnection con = new OleDbConnection(strdb);
            con.Open();//打开数据库

            //创建command对象并保存sql查询语句
            string strsql = "select * from Rd_usercheckins_15_16";

            OleDbCommand testCommand = con.CreateCommand();
            testCommand.CommandText = strsql;

            OleDbDataReader testReader = testCommand.ExecuteReader();

            while (testReader.Read())
            {
                string uid = (string)testReader["uid"];
                string poiid = Convert.ToString(testReader["poiid"]);

                if (!user_checkins.ContainsKey(uid))
                {
                    user_checkins.Add(uid, new Dictionary<string, int>());
                }
                if (!user_checkins[uid].ContainsKey(poiid))
                {
                    user_checkins[uid].Add(poiid, 0);
                }

                user_checkins[uid][poiid]++;

                
            }

            testReader.Close();//关闭Reader对象
            con.Close();//关闭数据库

            return user_checkins;

        }

        /// <summary>
        /// get POICategory Data with Category2 labeled each of them from access database
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>dic(poiid,title,address,longi,lati,cate2)</returns>
        public Dictionary<string, Tuple<double, double, string>> getPOIcategory2()
        {

            // poiid, title, address, longitude, latitude, category2
            var poicategory = new Dictionary<string, Tuple<double, double, string>>();

            //建立数据库连接
            string filepath = "D:\\dissertation\\data\\MGM\\database20160309.accdb";
            string strdb = "Provider=Microsoft.ACE.OLEDB.12.0;Data source=" + filepath;
            OleDbConnection con = new OleDbConnection(strdb);
            con.Open();//打开数据库

            //创建command对象并保存sql查询语句
            string strsql = "select * from Rd_poicategory_20160312";

            OleDbCommand testCommand = con.CreateCommand();
            testCommand.CommandText = strsql;

            OleDbDataReader testReader = testCommand.ExecuteReader();

            while (testReader.Read())
            {

                string poiid = Convert.ToString(testReader["poiid"]);
                //string title = Convert.ToString(testReader["title"]);
                //string address = Convert.ToString(testReader["address"]);
                double longi = Convert.ToDouble(testReader["longitude"]);
                double lati = Convert.ToDouble(testReader["latitude"]);
                string cate2 = Convert.ToString(testReader["category2"]);

                poicategory.Add(poiid, new Tuple<double, double, string>(longi, lati, cate2));
            }

            testReader.Close();//关闭Reader对象
            con.Close();//关闭数据库

            return poicategory;
        }

        #endregion

        /// <summary>
        /// get cold start users
        /// </summary>
        /// <param name="max_poi">cold start user可能访问的最大POI数目</param>
        /// <returns>list of uid</returns>
        public List<string> get_cold_user(int max_poi)
        {
            //var user_checkins = getUsercheckins2();

            var user_list = new List<string>();

            foreach (var user in splitted_usercheckin.train)//遍历train集
            {
                if (user.Value.Count <= max_poi)//用户在train集中签到过的POI数目≤阈值
                {
                    user_list.Add(user.Key);
                }
            }

            return user_list;

        }

        /// <summary>
        /// sub class used to return training set and test set at same time
        /// due to random select method, training set and test set should be recorded at same time
        /// </summary>
        public class splittedData
        {
            public Dictionary<string, Dictionary<string, int>> train;
            public Dictionary<string, Dictionary<string, int>> test;
        }

        /// <summary>
        /// randomly split dataset into two part and return both training set and test set
        /// </summary>
        /// <param name="percent">indicate ratio of usercheckins has been marked off</param>
        /// <returns></returns>
        public splittedData splitter(double p)
        {           

            var user_checkins = getUsercheckins2();
            var user_list = user_checkins.Keys.ToArray<string>();
            
            splittedData sd = new splittedData();

            

            System.Random rd = new System.Random();

            var test = new Dictionary<string, Dictionary<string, int>>();


            foreach (var user in user_list)
            {
                //仅选择签到地点数>10的用户
                if (user_checkins[user].Count < 10)
                {
                    user_checkins.Remove(user);
                    continue;
                }

                int num_test = Convert.ToInt32(p * (user_checkins[user].Count() + 0.0)); //number of item in test set
                //获取该用户访问过的所有poiid
                string[] keys = user_checkins[user].Keys.ToArray<string>();

                test.Add(user, new Dictionary<string, int>());

                while(test[user].Count() < num_test)//测试集中数目未达到比例要求时，重复循环
                {
                    string randomKey = keys[rd.Next(0, keys.Length)];

                    if (!test[user].ContainsKey(randomKey))
                    {
                        test[user].Add(randomKey, user_checkins[user][randomKey]);
                        user_checkins[user].Remove(randomKey);//remove test item from train set                      
                    }
                }
            }
            sd.train = user_checkins;
            sd.test = test;
            checkins2csv(sd.train, "result/usercheckins_train_20160325_0.1.csv");
            checkins2csv(test, "result/usercheckins_test_20160325_0.1.csv");

            return sd;
        }

        public void checkins2csv(Dictionary<string, Dictionary<string, int>> checkin, string filename)
        {
            DataTable dt = new DataTable();

            dt.Columns.Add("uid");
            dt.Columns.Add("poiid");
            dt.Columns.Add("checkinNum");

            foreach (var user in checkin)
            {
                foreach (var item in checkin[user.Key])
                {
                    DataRow dr = dt.NewRow();
                    dr["uid"] = user.Key;
                    dr["poiid"] = item.Key;
                    dr["checkinNum"] = item.Value;

                    dt.Rows.Add(dr);
                }
            }
            DataTableToCSV(dt, "D:/dissertation/data/" + filename);

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

        public Dictionary<string, Tuple<double, double, string>> get_clean_poi()
        {
            var poi = getPOIcategory2();
            //poi倒排表仅根据train集构建，只有train中出现过的POI才会被推荐给用户
            var user_checkin = getSplittedData().train;
            poi_user = new Dictionary<string, List<string>>();//建立用户物品倒排表

            var clean_poi = new Dictionary<string, Tuple<double, double, string>>();

            DataTable dt = new DataTable();
            dt.Columns.Add("poiid");
            dt.Columns.Add("longitude");
            dt.Columns.Add("latitude");
            dt.Columns.Add("category2");

            foreach (var user in user_checkin)//遍历整个train集
            {
                //if (user.Value.Count < 10)
                //{
                //    continue;
                //}

                foreach (var item in user_checkin[user.Key])
                {
                    if (!clean_poi.ContainsKey(item.Key))
                    {
                        clean_poi.Add(item.Key, poi[item.Key]);

                        DataRow dr = dt.NewRow();
                        dr["poiid"] = item.Key;
                        dr["longitude"] = poi[item.Key].Item1;
                        dr["latitude"] = poi[item.Key].Item2;
                        dr["category2"] = poi[item.Key].Item3;

                        dt.Rows.Add(dr);

                        poi_user.Add(item.Key, new List<string>());                       
                    }
                    //物品 - 用户倒排表
                    poi_user[item.Key].Add(user.Key);
                }
            }
            DataTableToCSV(dt, "D:/dissertation/data/result/clean_poi_20160321.csv");

            return clean_poi;
        }

        public Dictionary<string, Dictionary<string, double>> load_recommend_file(string fileName)
        {

            string[] text = System.IO.File.ReadLines(@"D:\dissertation\data\result\" + fileName, Encoding.Default).ToArray();

            var recommendation = new Dictionary<string, Dictionary<string, double>>();

            int counter = 0;
            //DataTable dt = new DataTable();

            //dt.Columns.Add("uid");
            //dt.Columns.Add("poiid");
            //dt.Columns.Add("interest");

            foreach (string s in text)
            {
                if (counter == 0)//首行标题
                {

                }
                else
                {
                    string uid = s.Split(',')[0];
                    string poiid = s.Split(',')[1];
                    double interest = Convert.ToDouble(s.Split(',')[2]);

                    //append into recommendation dictionary
                    if (!recommendation.ContainsKey(uid))
                    {
                        recommendation.Add(uid, new Dictionary<string, double>());
                    }
                    recommendation[uid].Add(poiid, interest);

                    //DataRow dr = dt.NewRow();
                    //dr["uid"] = uid;
                    //dr["poiid"] = poiid;
                    //dr["interest"] = interest;

                    //dt.Rows.Add(dr);
                }

                counter++;
            }

            //dataGridView1.DataSource = dt;

            return recommendation;
        }

        /// <summary>
        /// calculate Recall for the given user
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public Tuple<double,double> Recall_Precision(string uid)
        {
            //get common items
            //List<string> commonItemsList = new List<string>();
            int common = 0;

            //if (!splitted_usercheckin.test.ContainsKey(uid))//测试集中不包含对应用户（impossible = =）
            //{
            //    return null;
            //}

            foreach (var T_item in splitted_usercheckin.test[uid])//遍历test集中用户访问过的每个POI
            {
                if (rec[uid].ContainsKey(T_item.Key))//当前POI在推荐结果里
                {
                    common++;
                }
            }

            double recall = (common + 0.0) / splitted_usercheckin.test[uid].Count;
            double precision = (common + 0.0) / rec[uid].Count();

            var result = new Tuple<double, double>(recall, precision);
            return result;
        }

        public DataTable Recall_Precision_all(string fileName)
        {
            DataTable dt = new DataTable();

            dt.Columns.Add("uid");
            dt.Columns.Add("recall");
            dt.Columns.Add("precision");

            foreach (var user in rec)
            {
                var result = Recall_Precision(user.Key);

                DataRow dr = dt.NewRow();

                dr["uid"] = user.Key;
                dr["recall"] = result.Item1;
                dr["precision"] = result.Item2;

                dt.Rows.Add(dr);
            }
            DataTableToCSV(dt, "D:/dissertation/data/result/"+fileName);
            return dt;
        }

        /// <summary>
        /// 计算cold start user的Recall和Precision
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public DataTable Recall_Precision_cold(string fileName, int max_p)
        {
            //获取cold start user 列表
            var cold_user = get_cold_user(max_p);

            DataTable dt = new DataTable();

            dt.Columns.Add("uid");
            dt.Columns.Add("recall");
            dt.Columns.Add("precision");

            foreach (var user in cold_user)
            {
                var result = Recall_Precision(user);

                DataRow dr = dt.NewRow();

                dr["uid"] = user;
                dr["recall"] = result.Item1;
                dr["precision"] = result.Item2;

                dt.Rows.Add(dr);
            }
            DataTableToCSV(dt, "D:/dissertation/data/result/" + fileName);
            return dt;
        }

        /// <summary>
        /// 仅获取Recall，Precision的均值
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>（Recall，Precision）</returns>
        public Tuple<double,double> Recall_Precision_all2()
        {
            List<double> Recall = new List<double>();
            List<double> Precision = new List<double>();

            foreach (var user in rec)
            {
                var result = Recall_Precision(user.Key);

                Recall.Add(result.Item1);
                Precision.Add(result.Item2);

            }

            //取Recall 和Precision的均值
            double ave_r = Recall.Average();
            double ave_p = Precision.Average();

            var res = new Tuple<double, double>(ave_r, ave_p);
            return res;
        }



        /// <summary>
        /// 仅获取cold start users 的 Recall，Precision的均值
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>（Recall，Precision）</returns>
        public Tuple<double, double> Recall_Precision_all2_cold(int max_p)
        {
            var cold_users = get_cold_user(max_p);

            List<double> Recall = new List<double>();
            List<double> Precision = new List<double>();

            foreach (var user in cold_users)//计算cold user的精确度和召回率
            {
                var result = Recall_Precision(user);

                Recall.Add(result.Item1);
                Precision.Add(result.Item2);

            }

            //取Recall 和Precision的均值
            double ave_r = Recall.Average();
            double ave_p = Precision.Average();

            var res = new Tuple<double, double>(ave_r, ave_p);
            return res;
        }

        public splittedData retrieve_usercheckin(string train, string test)
        {
            splittedData sd = new splittedData();
            sd.train = load_checkin_CSV(train);
            sd.test = load_checkin_CSV(test);

            return sd;
        }

        public Dictionary<string, Dictionary<string, int>> load_checkin_CSV(string fileName)
        {
            string[] text = System.IO.File.ReadLines(@"D:\dissertation\data\result\" + fileName, Encoding.Default).ToArray();

            var dic = new Dictionary<string, Dictionary<string, int>>();

            int counter = 0;

            foreach (string s in text)
            {
                if (counter == 0)//首行
                {

                }
                else
                {
                    string uid = s.Split(',')[0];
                    string poiid = s.Split(',')[1];
                    int checkinNum = Convert.ToInt32(s.Split(',')[2]);

                    //append into recommendation dictionary
                    if (!dic.ContainsKey(uid))
                    {
                        dic.Add(uid, new Dictionary<string, int>());
                    }
                    dic[uid].Add(poiid, checkinNum);

                    //DataRow dr = dt.NewRow();
                    //dr["uid"] = uid;
                    //dr["poiid"] = poiid;
                    //dr["interest"] = interest;

                    //dt.Rows.Add(dr);
                }

                counter++;
            }

            //dataGridView1.DataSource = dt;

            return dic;
        }

        /// <summary>
        /// 从推荐结果Rec中 获取每个用户的前n个推荐结果
        /// </summary>
        /// <param name="n">为每个用户返回的推荐项数目</param>
        /// <returns>以dictionary(uid,dictionary(poiid,interest)),
        /// 返回推荐文件中所有用户的前n个推荐结果</returns>
        public Dictionary<string,Dictionary<string,double>> Rec_Splitter(int n)
        {

            //用于存储每个用户的前n个推荐结果
            var result = new Dictionary<string, Dictionary<string, double>>();

            //为每个用户取出Top n个推荐结果
            foreach (var user in rec)
            {
                result.Add(user.Key, new Dictionary<string, double>());

                int count = 0;//用于记录结果集中的推荐项数目

                //从推荐项中取出对应用户的推荐结果，按interest降序排序
                var temp = from item in rec[user.Key] orderby item.Value descending select item;

                //取前n个推荐结果
                foreach (var item in temp)
                {
                    if (count >= n)
                    {
                        break;
                    }

                    result[user.Key].Add(item.Key, item.Value);
                    //var t = result[user.Key].Count;
                    count++;
                }
            }

            return result;
        }

        /// <summary>
        /// 从数据库指定表中读取指定参数组合的推荐结果
        /// </summary>
        /// <param name="tableName">数据库中的表名称</param>
        /// <param name="a">比例系数alpha（User-CF）</param>
        /// <param name="b">比例系数beta</param>
        /// <returns></returns>
        public Dictionary<string, Dictionary<string, double>> Rec_DB(string tableName,double a,double b)
        {
            var rec = new Dictionary<string, Dictionary<string, double>>();

            //建立数据库连接
            string filepath = "D:\\dissertation\\data\\MGM\\database20160309.accdb";
            string strdb = "Provider=Microsoft.ACE.OLEDB.12.0;Data source=" + filepath;
            OleDbConnection con = new OleDbConnection(strdb);
            con.Open();//打开数据库

            //创建command对象并保存sql查询语句
            string strsql = "select * from " + tableName + " where alpha = " + a + " and beta = " + b + "";

            OleDbCommand testCommand = con.CreateCommand();
            testCommand.CommandText = strsql;

            OleDbDataReader testReader = testCommand.ExecuteReader();

            int cont = 0;

            while (testReader.Read())
            {
                string uid = Convert.ToString( testReader["uid"]);
                string poiid = Convert.ToString(testReader["poiid"]);
                double interest = Convert.ToDouble(testReader["interest"]);

                if (!rec.ContainsKey(uid))
                {
                    rec.Add(uid, new Dictionary<string, double>());
                }
                rec[uid].Add(poiid, interest);

                cont++;
            }

            testReader.Close();//关闭Reader对象
            con.Close();//关闭数据库

            return rec;

        }
    }
}
