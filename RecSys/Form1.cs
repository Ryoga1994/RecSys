using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RecSys
{

 

    public partial class Form1 : Form
    {
        MGM_LPA ini = new MGM_LPA();
        User_CF ucf = new User_CF();
        MGM_UCF mgm_ucf = new MGM_UCF();
        Optics_MGM_UCF optics_m_ucf = new Optics_MGM_UCF();
        PL_UCF pl_ucf = new PL_UCF();
        Exp_UCF exp_ucf = new Exp_UCF();
        CPL_UCF cpl_ucf = new CPL_UCF();
        PL pl = new PL();
        MGM mgm = new MGM();
        L_PL_UCF l_pl_ucf = new L_PL_UCF();
        L_MGM_UCF l_mgm_ucf = new L_MGM_UCF();

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!ini.ds.isInitialized())
            {
                ini.Initial();
            }

            double r = Convert.ToDouble(textBox1.Text);
            double d = Convert.ToDouble(textBox2.Text);
            double theta = Convert.ToDouble(textBox3.Text);
            var dic = ini.coverage1_all(r,d,theta);

            //show dictionary to DataTable

            //show dictionary to DataTable
            DataTable dt = new DataTable();
            dt.Columns.Add("uid");
            dt.Columns.Add("train_poi_cov");
            dt.Columns.Add("test_poi_cov");
            dt.Columns.Add("train_checkin_cov");
            dt.Columns.Add("test_checkin_cov");

            foreach (var item in dic)
            {
                DataRow dr = dt.NewRow();

                dr["uid"] = item.Key;
                dr["train_poi_cov"] = item.Value.Item1;
                dr["test_poi_cov"] = item.Value.Item2;
                dr["train_checkin_cov"] = item.Value.Item3;
                dr["test_checkin_cov"] = item.Value.Item4;

                dt.Rows.Add(dr);
            }

            dataGridView1.DataSource = dt;

            DataTableToCSV(dt, "D:/dissertation/data/result/coverage_"+r+"_"+d+"_"+theta+".csv");
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

        private void button2_Click(object sender, EventArgs e)
        {
            if (!ini.ds.isInitialized())
            {
                
                ini.Initial();

            }


            double p = Convert.ToDouble(textBox1.Text);
            double d = Convert.ToDouble(textBox2.Text);
            double theta = Convert.ToDouble(textBox3.Text);
            var dic = ini.coverage2_all(p, d, theta);

            //show dictionary to DataTable
            DataTable dt = new DataTable();
            dt.Columns.Add("uid");
            dt.Columns.Add("train_poi_cov");
            dt.Columns.Add("test_poi_cov");
            dt.Columns.Add("train_checkin_cov");
            dt.Columns.Add("test_checkin_cov");

            foreach (var item in dic)
            {
                DataRow dr = dt.NewRow();

                dr["uid"] = item.Key;
                dr["train_poi_cov"] = item.Value.Item1;
                dr["test_poi_cov"] = item.Value.Item2;
                dr["train_checkin_cov"] = item.Value.Item3;
                dr["test_checkin_cov"] = item.Value.Item4;

                dt.Rows.Add(dr);
            }

            dataGridView1.DataSource = dt;

            DataTableToCSV(dt, "D:/dissertation/data/result/coverage_" + p + "_" + d + "_" + theta + ".csv");
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            string uid = textBox4.Text;

            double p = Convert.ToDouble(textBox1.Text);
            double d = Convert.ToDouble(textBox2.Text);
            double theta = Convert.ToDouble(textBox3.Text);

            ini.coverage2(uid, p, d, theta);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (!ini.ds.isInitialized())
            {

                ini.Initial();

            }

            //double p = Convert.ToDouble(textBox1.Text);
            double d = Convert.ToDouble(textBox2.Text);
            double theta = Convert.ToDouble(textBox3.Text);
            var dic = ini.coverage3_all(d, theta);

            //show dictionary to DataTable
            DataTable dt = new DataTable();
            dt.Columns.Add("uid");
            dt.Columns.Add("train_poi_cov");
            dt.Columns.Add("test_poi_cov");
            dt.Columns.Add("train_checkin_cov");
            dt.Columns.Add("test_checkin_cov");

            foreach (var item in dic)
            {
                DataRow dr = dt.NewRow();

                dr["uid"] = item.Key;
                dr["train_poi_cov"] = item.Value.Item1;
                dr["test_poi_cov"] = item.Value.Item2;
                dr["train_checkin_cov"] = item.Value.Item3;
                dr["test_checkin_cov"] = item.Value.Item4;

                dt.Rows.Add(dr);
            }

            dataGridView1.DataSource = dt;

            DataTableToCSV(dt, "D:/dissertation/data/result/coverage3_" + d + "_" + theta + ".csv");
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (!ini.ds.isInitialized())
            {
                ini.Initial();
            }

            double r = Convert.ToDouble(textBox1.Text);
            double d = Convert.ToDouble(textBox2.Text);
            double theta = Convert.ToDouble(textBox3.Text);
            int n = Convert.ToInt32(textBox4.Text);
            ini.recommend_all(r, d, theta, n);

            //show dictionary to DataTable
            //DataTable dt = new DataTable();
            //dt.Columns.Add("uid");
            //dt.Columns.Add("poiid");
            //dt.Columns.Add("interest");

            //int user_count = 0;

            //foreach (var user in dic)
            //{
            //    foreach (var item in dic[user.Key])
            //    {
            //        DataRow dr = dt.NewRow();

            //        dr["uid"] = user.Key;
            //        dr["poiid"] = item.Key;
            //        dr["interest"] = item.Value;

            //        dt.Rows.Add(dr);                   
            //    }

            //    user_count++;
            //    if ((user_count % 100) == 0)
            //    {
            //        DataTableToCSV(dt, "D:/dissertation/data/result/recommendation_MGM_LAP_20160314_" + user_count + ".csv");
            //    }

            //}

        }

        private void button6_Click(object sender, EventArgs e)
        {
            ini.ds.Ini_analysis("recommendation_r_LAP_20160314_4800.csv");
            dataGridView1.DataSource = ini.ds.Recall_Precision_all("MGM_LAP_recall_cov_20.csv");

        }

       

        private void userCFToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            
            ucf.Initial("usercheckins_train_20160321_0.1.csv", "usercheckins_test_20160321_0.1.csv");

            //double r = Convert.ToDouble(textBox1.Text);
            //double d = Convert.ToDouble(textBox2.Text);
            //double theta = Convert.ToDouble(textBox3.Text);
            int n = Convert.ToInt32(textBox4.Text);
            ucf.recommend_all(n);//推荐项数目
        }

        private void analysisToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ucf.ds.Ini_analysis("recommendation_MGM_UCF_20160325_5_0.5_0.5.csv");
            dataGridView1.DataSource = ucf.ds.Recall_Precision_all("MGM_UCF_recall_cov_5.csv");
        }

        private void mGMUCFToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            
            mgm_ucf.Initial("usercheckins_train_20160325_0.1.csv", "usercheckins_test_20160325_0.1.csv");
            //double r = Convert.ToDouble(textBox1.Text);
            //double d = Convert.ToDouble(textBox2.Text);
            //double theta = Convert.ToDouble(textBox3.Text);
            //int n = Convert.ToInt32(textBox4.Text);
            //参数可变
            mgm_ucf.recommend_all(5, 0.5, 0.5, 15, 0.02);//推荐项数目

            //for (int i = 0; i <= 10; i++)
            //{
            //    double a = i / (10 + 0.0);
            //    double b = (10 - i) / (10 + 0.0);
            //    mgm_ucf.recommend_all(50, a, b, 15, 0.02);
            //}

        }

        private void analysusToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mgm_ucf.ds.Ini_analysis("recommendation_MGM_UCF_20160320_50.csv");
            dataGridView1.DataSource = mgm_ucf.ds.Recall_Precision_all("MGM_UCF_recall_cov.csv");
        }

        private void opticsToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            optics_m_ucf.Initial("usercheckins_train_20160314.csv", "usercheckins_test_20160314.csv");
            var pois =  optics_m_ucf.ds.clean_poi.Keys.ToList<string>();

            optics_m_ucf.Optics_poi(pois, 0.2, 10);
        }

        private void powerLawToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void powerLawToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            pl_ucf.Initial("usercheckins_train_20160321_0.1.csv", "usercheckins_test_20160321_0.1.csv");
            //参数可变

            //int n = Convert.ToInt32(textBox4.Text);

            pl_ucf.recommend_all(5, 0.5, 0.5);//推荐项数目
            //pl_ucf.recommend_all(10, 0, 1);
            //pl_ucf.recommend_all(15, 0, 1);
            //pl_ucf.recommend_all(20, 0.5, 0.5);
            //pl_ucf.recommend_all(25, 0, 1);
            //pl_ucf.recommend_all(30, 0, 1);
            //pl_ucf.recommend_all(35, 0, 1);
            //pl_ucf.recommend_all(40, 0, 1);
            //pl_ucf.recommend_all(45, 0, 1);
            //pl_ucf.recommend_all(50, 0.5, 0.5);

            //for (int i = 0; i <= 10; i++)
            //{
            //    double a = i / (10 + 0.0);
            //    double b = (10 - i) / (10 + 0.0);
            //    pl_ucf.recommend_all(50, a, b);
            //}
        }

        private void analysisToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            int n = Convert.ToInt32(textBox4.Text);//确定分析的目标文件
            pl_ucf.ds.Ini_analysis("recommendation_PL_UCF_20160320_5_0.5_0.5.csv");
            dataGridView1.DataSource = pl_ucf.ds.Recall_Precision_all("PL_UCF_recall_cov_" + n + ".csv");
        }

        private void eXPUCFToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            exp_ucf.Initial("usercheckins_train_20160314.csv", "usercheckins_test_20160314.csv");
            //参数可变

            int n = Convert.ToInt32(textBox4.Text);

            exp_ucf.recommend_all(n,0.5,0.5);
        }

        private void analysisToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            int n = Convert.ToInt32(textBox4.Text);//确定分析的目标文件
            exp_ucf.ds.Ini_analysis("recommendation_EXP_UCF_20160317_" + n + ".csv");
            dataGridView1.DataSource = exp_ucf.ds.Recall_Precision_all("EXP_UCF_recall_cov_" + n + ".csv");
        }

        private void cPLUCFToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            cpl_ucf.Initial("usercheckins_train_20160314.csv", "usercheckins_test_20160314.csv");
            //参数可变

            int n = Convert.ToInt32(textBox4.Text);

            cpl_ucf.recommend_all(n, 0.05, 0.95, 15, 0.02);
        }

        private void analysisToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            int n = Convert.ToInt32(textBox4.Text);//确定分析的目标文件
            cpl_ucf.ds.Ini_analysis("recommendation_CPL_UCF_20160318_" + n + ".csv");
            dataGridView1.DataSource = cpl_ucf.ds.Recall_Precision_all("CPL_UCF_recall_cov_" + n + ".csv");
        }

        private void pLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pl.Initial("usercheckins_train_20160314.csv", "usercheckins_test_20160314.csv");
            //参数可变

            //int n = Convert.ToInt32(textBox4.Text);

            //pl.recommend_all(5);
            //pl.recommend_all(10);
            //pl.recommend_all(15);
            //pl.recommend_all(20);
            //pl.recommend_all(25);
            //pl.recommend_all(30);
            //pl.recommend_all(35);
            //pl.recommend_all(40);
            //pl.recommend_all(45);
            pl.recommend_all(50);
        }

        private void analysis2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int n = Convert.ToInt32(textBox4.Text);//确定分析的目标文件
            cpl_ucf.ds.Ini_analysis("recommendation_PL_20160319_" + n + ".csv");
            dataGridView1.DataSource = cpl_ucf.ds.Recall_Precision_all("PL_recall_cov_" + n + ".csv");
        }

        private void multiGaussianToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mgm.Initial("usercheckins_train_20160321_0.5.csv", "usercheckins_test_20160321_0.5.csv");
            //参数可变

            int n = Convert.ToInt32(textBox4.Text);

            mgm.recommend_all(n, 15, 0.02);
        }

        private void analysis2ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            int n = Convert.ToInt32(textBox4.Text);//确定分析的目标文件
            mgm.ds.Ini_analysis("recommendation_MGM_20160320_" + n + ".csv");
            dataGridView1.DataSource = mgm.ds.Recall_Precision_all("MGM_recall_cov_" + n + ".csv");
        }

        private void lPLUCFToolStripMenuItem_Click(object sender, EventArgs e)
        {
            l_pl_ucf.Initial("usercheckins_train_20160321_0.1.csv", "usercheckins_test_20160321_0.1.csv");
            //参数可变

            //int n = Convert.ToInt32(textBox4.Text);

            l_pl_ucf.recommend_all4(5, 0.5, 0.5);
            ////建立数据库连接
            //string filepath = "D:\\dissertation\\data\\MGM\\database20160309.accdb";
            //string strdb = "Provider=Microsoft.ACE.OLEDB.12.0;Data source=" + filepath;
            //OleDbConnection con = new OleDbConnection(strdb);
            //con.Open();//打开数据库

            //l_pl_ucf.recommend_all2(n, con);

            //con.Close(); //关闭数据库
        }

        private void analysis3ToolStripMenuItem_Click(object sender, EventArgs e)
        {

            DataTable dt = new DataTable();

            dt.Columns.Add("a");//参数a
            dt.Columns.Add("b");
            dt.Columns.Add("Recall");
            dt.Columns.Add("Precision");

            //int n = Convert.ToInt32(textBox4.Text);//确定分析的目标文件
            for (int i = 0; i < 10; i++)
            {
                double a = i / (10 + 0.0);
                double b = (10 - i) / (10 + 0.0);
                //导入指定参数组合的推荐结果
                l_pl_ucf.ds.Ini_ana_DB("L_PL_UCF", a, b);

                var re = l_pl_ucf.ds.Recall_Precision_all2();

                DataRow dr = dt.NewRow();
                dr["a"] = a;
                dr["b"] = b;
                dr["Recall"] = re.Item1;
                dr["Precision"] = re.Item2;

                dt.Rows.Add(dr);
            }
            dataGridView1.DataSource = dt;                   
        }

        //调用完全推荐文件（n=50），从中获得前n个项的推荐结果
        private void aNAToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //int n = Convert.ToInt32(textBox4.Text);//指定需返回的前n个推荐项


            DataTable dt = new DataTable();

            dt.Columns.Add("n");//参数a
            dt.Columns.Add("Recall");
            dt.Columns.Add("Precision");

            for (int i = 5; i <= 50; i += 5)
            {
                l_pl_ucf.ds.Ini_analysis("recommendation_L_PL_UCF_20160320_50.csv");
                l_pl_ucf.ds.rec = l_pl_ucf.ds.Rec_Splitter(i);

                var re = l_pl_ucf.ds.Recall_Precision_all2();

                DataRow dr = dt.NewRow();
                dr["n"] = i;
                dr["Recall"] = re.Item1;
                dr["Precision"] = re.Item2;

                dt.Rows.Add(dr);

            }

            dataGridView1.DataSource = dt;
                        
            //dataGridView1.DataSource = l_pl_ucf.ds.Recall_Precision_all("L_PL_UCF_recall_cov_" + n + ".csv");


        }

        private void aNA1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //int n = Convert.ToInt32(textBox4.Text);//指定需返回的前n个推荐项

            //导入完全推荐文件
            //pl_ucf.ds.Ini_analysis("recommendation_PL_UCF_20160320_50.csv");

            ////抽取rec中每个用户的前n个推荐结果，并更新rec
            //pl_ucf.ds.rec = pl_ucf.ds.Rec_Splitter(n);
            //dataGridView1.DataSource = pl_ucf.ds.Recall_Precision_all("PL_UCF_recall_cov_" + n + ".csv");

            //-------------批处理

            DataTable dt = new DataTable();

            //dt.Columns.Add("a");//参数a
            dt.Columns.Add("n");//推荐数目n
            dt.Columns.Add("Recall");
            dt.Columns.Add("Precision");

            //抽取rec中每个用户的前n个推荐结果，并更新rec
            for (int i = 5; i <= 50; i += 5)
            {
                //初始化分析环境
                pl_ucf.ds.Ini_analysis("recommendation_PL_UCF_20160320_50_1_0.csv");

                pl_ucf.ds.rec = pl_ucf.ds.Rec_Splitter(i);
                //返回对应条件下的均值
                var re = pl_ucf.ds.Recall_Precision_all2();

                DataRow dr = dt.NewRow();
                dr["n"] = i;
                dr["Recall"] = re.Item1;
                dr["Precision"] = re.Item2;

                dt.Rows.Add(dr);
            }

            dataGridView1.DataSource = dt;

            //-------end 批处理
        }

        private void lMGMUCFToolStripMenuItem_Click(object sender, EventArgs e)
        {
            l_mgm_ucf.Initial("usercheckins_train_20160321_0.1.csv", "usercheckins_test_20160321_0.1.csv");
            //参数可变

            //int n = Convert.ToInt32(textBox4.Text);
            //l_mgm_ucf.recommend_all(n, 0, 1, 15, 0.02);

            //链接数据库
            //string filepath = "D:\\dissertation\\data\\MGM\\database20160309.accdb";
            //string strdb = "Provider=Microsoft.ACE.OLEDB.12.0;Data source=" + filepath;
            //OleDbConnection con = new OleDbConnection(strdb);
            //con.Open();//打开数据库

            l_mgm_ucf.recommend_all(5, 0.5, 0.5, 15, 0.02);


        }

        private void aNA3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int n = Convert.ToInt32(textBox4.Text);//指定需返回的前n个推荐项

            //导入完全推荐文件
            l_mgm_ucf.ds.Ini_analysis("recommendation_L_MGM_UCF_20160320_50.csv");

            //抽取rec中每个用户的前n个推荐结果，并更新rec
            l_mgm_ucf.ds.rec = l_mgm_ucf.ds.Rec_Splitter(n);
            dataGridView1.DataSource = l_mgm_ucf.ds.Recall_Precision_all("L_MGM_UCF_recall_cov_" + n + ".csv");
        }

        private void aNAToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            int n = Convert.ToInt32(textBox4.Text);//指定需返回的前n个推荐项

            //导入完全推荐文件
            ucf.ds.Ini_analysis("recommendation_UCF_20160320_50.csv");

            //抽取rec中每个用户的前n个推荐结果，并更新rec
            ucf.ds.rec = ucf.ds.Rec_Splitter(n);
            dataGridView1.DataSource = ucf.ds.Recall_Precision_cold("UCF_recall_cov_" + n + ".csv",9);
        }

        private void aNA1ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            //int n = Convert.ToInt32(textBox4.Text);//指定需返回的前n个推荐项

            //导入完全推荐文件
            //mgm_ucf.ds.Ini_analysis("recommendation_MGM_UCF_20160320_50_0.2_0.8.csv");


            //-------------批处理

            DataTable dt = new DataTable();

            //dt.Columns.Add("a");//参数a
            dt.Columns.Add("n");//推荐数目n
            dt.Columns.Add("Recall");
            dt.Columns.Add("Precision");

            //抽取rec中每个用户的前n个推荐结果，并更新rec
            for (int i = 5; i <= 50; i += 5)
            {
                //初始化分析环境
                mgm_ucf.ds.Ini_analysis("recommendation_MGM_UCF_20160320_50.csv");

                mgm_ucf.ds.rec = mgm_ucf.ds.Rec_Splitter(i);
                //返回对应条件下的均值
                var re =  mgm_ucf.ds.Recall_Precision_all2();

                DataRow dr = dt.NewRow();
                dr["n"] = i;
                dr["Recall"] = re.Item1;
                dr["Precision"] = re.Item2;

                dt.Rows.Add(dr); 
            }

            dataGridView1.DataSource = dt;

            //-------end 批处理

            //mgm_ucf.ds.rec = mgm_ucf.ds.Rec_Splitter(n);
            //dataGridView1.DataSource = mgm_ucf.ds.Recall_Precision_all("MGM_UCF_recall_cov_" + n + ".csv");
        }

        private void aNA2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //int n = Convert.ToInt32(textBox4.Text);//指定需返回的前n个推荐项

            //导入完全推荐文件
            //mgm.ds.Ini_analysis("recommendation_MGM_20160320_50.csv");

            ////抽取rec中每个用户的前n个推荐结果，并更新rec
            //mgm.ds.rec = mgm.ds.Rec_Splitter(n);
            //dataGridView1.DataSource = mgm.ds.Recall_Precision_all("MGM_recall_cov_" + n + ".csv");

            //---------批处理
            DataTable dt = new DataTable();

            //dt.Columns.Add("a");//参数a
            dt.Columns.Add("n");//推荐数目n
            dt.Columns.Add("Recall");
            dt.Columns.Add("Precision");

            //抽取rec中每个用户的前n个推荐结果，并更新rec
            for (int i = 5; i <= 50; i += 5)
            {
                //初始化分析环境
                mgm.ds.Ini_analysis("recommendation_L_MGM_UCF_20160320_50.csv");

                mgm.ds.rec = mgm.ds.Rec_Splitter(i);
                //返回对应条件下的均值
                var re = mgm.ds.Recall_Precision_all2();

                DataRow dr = dt.NewRow();
                dr["n"] = i;
                dr["Recall"] = re.Item1;
                dr["Precision"] = re.Item2;

                dt.Rows.Add(dr);
            }

            dataGridView1.DataSource = dt;

            //-------end 批处理

        }

        private void aNA2ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            int n = Convert.ToInt32(textBox4.Text);//指定需返回的前n个推荐项

            //导入完全推荐文件
            pl.ds.Ini_analysis("recommendation_PL_20160320_50.csv");

            //抽取rec中每个用户的前n个推荐结果，并更新rec
            pl.ds.rec = pl.ds.Rec_Splitter(n);
            dataGridView1.DataSource = pl.ds.Recall_Precision_all("PL_recall_cov_" + n + ".csv");



        }

        //用于生成不同比例的分割数据集
        private void button7_Click(object sender, EventArgs e)
        {
            DataSplitter ds = new DataSplitter();
            ds.Initial(0.1);
        }



     
        private void coldUsersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //计算cold start user 的Recall和Precision
            //抽取rec中每个用户的前n个推荐结果，并更新rec

            DataTable dt = new DataTable();
            dt.Columns.Add("n");
            dt.Columns.Add("Recall");
            dt.Columns.Add("Precision");

            for (int i = 5; i <= 50; i += 5)
            {
                //初始化分析环境
                ucf.ds.Ini_analysis("recommendation_MGM_UCF_20160320_50.csv");

                ucf.ds.rec = ucf.ds.Rec_Splitter(i);
                //返回对应条件下的均值
                var re = ucf.ds.Recall_Precision_all2_cold(9);

                DataRow dr = dt.NewRow();
                dr["n"] = i;
                dr["Recall"] = re.Item1;
                dr["Precision"] = re.Item2;

                dt.Rows.Add(dr);
            }

            dataGridView1.DataSource = dt;

            //-------end 批处理
        }

        private void coldUsersToolStripMenuItem1_Click(object sender, EventArgs e)
        {

        }

        private void analysis3ToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            DataTable dt = new DataTable();

            dt.Columns.Add("a");//参数a
            dt.Columns.Add("b");
            dt.Columns.Add("Recall");
            dt.Columns.Add("Precision");

            //int n = Convert.ToInt32(textBox4.Text);//确定分析的目标文件
            for (int i = 0; i < 10; i++)
            {
                double a = i / (10 + 0.0);
                double b = (10 - i) / (10 + 0.0);
                //导入指定参数组合的推荐结果
                l_pl_ucf.ds.Ini_ana_DB("Rec_L_MGM_UCF", a, b);

                var re = l_pl_ucf.ds.Recall_Precision_all2();

                DataRow dr = dt.NewRow();
                dr["a"] = a;
                dr["b"] = b;
                dr["Recall"] = re.Item1;
                dr["Precision"] = re.Item2;

                dt.Rows.Add(dr);
            }
            dataGridView1.DataSource = dt;

        }

        private void lMGMUCF误差测试ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            l_mgm_ucf.Initial("usercheckins_train_20160314.csv", "usercheckins_test_20160314.csv");

            //针对推荐结果不一致的uid进行误差测试

            string uid = "1872459151";
            
            dataGridView1.DataSource = l_mgm_ucf.combine_rec3(uid, 5, 0.9,0.1, 15, 0.02); 
        }

        private void mGM测试ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mgm.Initial("usercheckins_train_20160314.csv", "usercheckins_test_20160314.csv");

            string uid = "1862496484";
            dataGridView1.DataSource =  mgm.predict_gauss3(uid, 5,15,0.02);
        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }
    }
}
