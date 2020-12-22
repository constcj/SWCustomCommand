using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace 钣金CustomCommand
{
    class Relation
    {
        /// <summary>
        /// 异常
        /// </summary>
        /// <param name="e"></param>
        public static void Exp(System.Exception e)
        {
            FileStream fs = null;
            StreamWriter sw = null;
            if (!File.Exists("c:\\exptlog.txt"))
            {
                fs = new FileStream("c:\\exptlog.txt", FileMode.Create, FileAccess.Write);
                sw = new StreamWriter(fs, UnicodeEncoding.GetEncoding("GB2312"));
                long fl = fs.Length;
                fs.Seek(fl, SeekOrigin.End);
            }
            else
            {
                fs = new FileStream("c:\\exptlog.txt", FileMode.Open, FileAccess.Write);
                sw = new StreamWriter(fs, UnicodeEncoding.GetEncoding("GB2312"));
                long fl = fs.Length;
                fs.Seek(fl, SeekOrigin.Begin);

            }
            sw.WriteLine(DateTime.Now.ToString() + ":" + e.ToString());//开始写入值
            sw.WriteLine(e.Message + e.StackTrace);
            sw.WriteLine();
            sw.Close();
            fs.Close();
        }
    }
}
