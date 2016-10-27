using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecSys
{
    /// <summary>
    /// 用于限定一个center的空间范围，有左上角和右下角坐标组成
    /// </summary>
    public class rectangle
    {
        public double upperLeft_x, upperLeft_y;//左上角
        public double lowerRight_x, lowerRight_y;//右下角

        public rectangle(double ul_x, double ul_y, double lr_x, double lr_y)
        {
            this.upperLeft_x = ul_x;
            this.upperLeft_y = ul_y;
            this.lowerRight_x = lr_x;
            this.lowerRight_y = lr_y;
        }

        public Boolean contains(double longi, double lati)
        {
            if ((this.upperLeft_x) <= longi && (this.upperLeft_y >= lati)
                && (this.lowerRight_x >= longi) && (this.lowerRight_y <= lati))//包含边界
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// using given location to update this range
        /// </summary>
        /// <param name="longi"></param>
        /// <param name="lati"></param>
        public void update(double longi, double lati)
        {
            upperLeft_x = (upperLeft_x < longi ? upperLeft_x : longi);
            upperLeft_y = (upperLeft_y > lati ? upperLeft_y : lati);
            lowerRight_x = (lowerRight_x > longi ? lowerRight_x : longi);
            lowerRight_y = (lowerRight_y < lati ? lowerRight_y : lati);
        }
    }
}
