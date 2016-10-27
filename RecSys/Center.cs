using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecSys
{
    class Center
    {
        public string c_id;
        public rectangle sp;//该center的空间范围
        public List<string> poi_list;//中心选择时属于该center的POI
        //public Dictionary<string, List<string>> cate_poi;//用户在该中心访问过的 类型，POI集合        

        public Center(string id, rectangle sp, List<string> poi_list)
        {
            this.c_id = id;
            this.sp = sp;
            this.poi_list = poi_list;
        }

        /// <summary>
        /// check whether the given location is in this center's region
        /// </summary>
        /// <param name="longi"></param>
        /// <param name="lati"></param>
        /// <returns></returns>
        public Boolean contains(double longi,double lati)
        {
            if (this.sp.contains(longi, lati))
            {
                return true;
            }
            return false;
        }

    }
}
