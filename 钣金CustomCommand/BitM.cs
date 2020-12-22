using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace 钣金CustomCommand
{
    class BitM
    {      
        private Bitmap bi = null;
        private Graphics g = null;
        private double coox = 0;
        private float pee = 4;
        public BitM(double coox1)
        {
            coox = coox1;
            bi = new Bitmap(1000, 1000);
            g = Graphics.FromImage(bi);
        }
        /// <summary>
        /// 绘图
        /// </summary>
        /// <param name="g"></param>
        /// <param name="e"></param>
        /// <param name="p"></param>
        /// <param name="pe"></param>
        public void GetBitmap(List<CurveInfo>[] listCurveMid, double hou)
        { // bi.RotateFlip(RotateFlipType.RotateNoneFlipY);
            try
            {
                double interx=-1;
                double intery=-1;

                int list0 = -1;
                int list1 = -1;

               if (listCurveMid.Length==2)
                {
                    List<CurveInfo> listCurve0 = listCurveMid[0];
                    List<CurveInfo> listCurve1 = listCurveMid[1];

                    for (int i = 0; i < listCurve0.Count; i++)
                    {
                        if (listCurve0[i].r==0)
                        {
                            for (int j = 0; j < listCurve1.Count; j++)
                            {
                                if (listCurve1[j].r == 0)
                                {
                                    double dx12 = -listCurve1[j].endMathPoint[0] + listCurve1[j].startMathPoint[0];
                                    double dy12 = -listCurve1[j].endMathPoint[1] + listCurve1[j].startMathPoint[1];

                                    double dx23 = -listCurve0[i].endMathPoint[0] + listCurve0[i].startMathPoint[0];
                                    double dy23 = -listCurve0[i].endMathPoint[1] + listCurve0[i].startMathPoint[1];

                                    double lengh12 = Math.Sqrt(dx12 * dx12 + dy12 * dy12);
                                    double lengh23 = Math.Sqrt(dx23 * dx23 + dy23 * dy23);

                                    double dx1 = -listCurve1[j].endMathPoint[0]+ listCurve0[i].startMathPoint[0];
                                    double dy1 = -listCurve1[j].endMathPoint[1] + listCurve0[i].startMathPoint[1];

                                    double dx2 = -listCurve1[j].endMathPoint[0] + listCurve0[i].endMathPoint[0];
                                    double dy2 = -listCurve1[j].endMathPoint[1]+ listCurve0[i].endMathPoint[1];

                                    double lengh1 =(Math.Sqrt((dx1 * dy2 - dy1 * dx2)* (dx1 * dy2 - dy1 * dx2)))/ lengh23;                                   

                                    double dx11 = -listCurve1[j].startMathPoint[0]+ listCurve0[i].startMathPoint[0];
                                    double dy11 = -listCurve1[j].startMathPoint[1]+listCurve0[i].startMathPoint[1];

                                    double dx22 = -listCurve1[j].startMathPoint[0]+listCurve0[i].endMathPoint[0];
                                    double dy22 = -listCurve1[j].startMathPoint[1]+ listCurve0[i].endMathPoint[1];

                                    double lengh2 = (Math.Sqrt((dx11 * dy22 - dy11 * dx22) * (dx11 * dy22 - dy11 * dx22))) / lengh23;

                                    double dx111 = -listCurve0[i].endMathPoint[0] + listCurve1[j].startMathPoint[0];
                                    double dy111 = -listCurve0[i].endMathPoint[1] + listCurve1[j].startMathPoint[1];

                                    double dx222 = -listCurve0[i].endMathPoint[0] + listCurve1[j].endMathPoint[0];
                                    double dy222 = -listCurve0[i].endMathPoint[1] + listCurve1[j].endMathPoint[1];

                                    double lengh11 = (Math.Sqrt((dx111 * dy222 - dy111 * dx222) * (dx111 * dy222 - dy111 * dx222))) / lengh12;

                                    double dx1111 = -listCurve0[i].startMathPoint[0] + listCurve1[j].startMathPoint[0];
                                    double dy1111 = -listCurve0[i].startMathPoint[1] + listCurve1[j].startMathPoint[1];

                                    double dx2222 = -listCurve0[i].startMathPoint[0] + listCurve1[j].endMathPoint[0];
                                    double dy2222 = -listCurve0[i].startMathPoint[1] + listCurve1[j].endMathPoint[1];

                                    double lengh22 = (Math.Sqrt((dx1111 * dy2222 - dy1111 * dx2222) * (dx1111 * dy2222 - dy1111 * dx2222))) / lengh12;                                                              
                                    if (Math.Round(lengh1+ lengh2,3)== Math.Round(lengh12, 3) &&
                                        Math.Round(lengh11 + lengh22, 3) == Math.Round(lengh23, 3))
                                    {
                                        double dx = -listCurve1[j].endMathPoint[0] + listCurve1[j].startMathPoint[0];
                                        double dy = -listCurve1[j].endMathPoint[1] + listCurve1[j].startMathPoint[1];

                                        double idx = dx* lengh1/ lengh12;
                                        double idy = dy * lengh1 / lengh12;

                                        interx = listCurve1[j].endMathPoint[0] + idx;
                                        intery = listCurve1[j].endMathPoint[1] + idy;
                                        list0 = i;
                                        list1 = j;
                                    }
                                }
                            }
                        }                      
                    }

                }

                pee = Convert.ToSingle(5);

                Pen pe = new Pen(Color.Black, pee);

                for (int j = 0; j < listCurveMid.Length;j++)
                {
                    for (int i = 0; i < listCurveMid[j].Count; i++)
                    {
                        CurveInfo e = listCurveMid[j][i];
                        if (e.r == 0)
                        {
                            Point p1 = new Point((int)Math.Round(e.startMathPoint[0], 0), (int)Math.Round(e.startMathPoint[1], 0));
                            Point p2 = new Point((int)Math.Round(e.endMathPoint[0], 0), (int)Math.Round(e.endMathPoint[1], 0));

                            g.DrawLine(pe, p1, p2);

                            String drawString = Math.Round(e.length, 1).ToString();

                            // Create font and brush.
                            Font drawFont = new Font("仿宋", Convert.ToSingle(coox));
                            SolidBrush drawBrush = new SolidBrush(Color.Black);

                            StringFormat stringFormat = new StringFormat();
                            stringFormat.Alignment = StringAlignment.Center;
                            stringFormat.LineAlignment = StringAlignment.Center;
                            // Create point for upper-left corner of drawing.

                            double stax = e.endMathPoint[0] - e.startMathPoint[0];
                            double stay = e.endMathPoint[1] - e.startMathPoint[1];

                            float x = (float)((e.startMathPoint[0] + e.endMathPoint[0]) / 2);
                            float y = (float)((e.startMathPoint[1]  + e.endMathPoint[1]) / 2);

                            if (i == 0)
                            {
                                if ((Math.Round(listCurveMid[j][i].startMathPoint[0], 3) == Math.Round(listCurveMid[j][i + 2].startMathPoint[0], 3) &&
                                Math.Round(listCurveMid[j][i].startMathPoint[1], 3) == Math.Round(listCurveMid[j][i + 2].startMathPoint[1], 3)) ||
                                (Math.Round(listCurveMid[j][i].startMathPoint[0], 3) == Math.Round(listCurveMid[j][i + 2].endMathPoint[0], 3) &&
                                Math.Round(listCurveMid[j][i].startMathPoint[1], 3) == Math.Round(listCurveMid[j][i + 2].endMathPoint[1], 3)))
                                {
                                    x = (float)(e.endMathPoint[0]);
                                    y = (float)(e.endMathPoint[1]);
                                }
                                else if ((Math.Round(listCurveMid[j][i].endMathPoint[0], 3) == Math.Round(listCurveMid[j][i + 2].startMathPoint[0], 3) &&
                                    Math.Round(listCurveMid[j][i].endMathPoint[1], 3) == Math.Round(listCurveMid[j][i + 2].startMathPoint[1], 3)) ||
                                    (Math.Round(listCurveMid[j][i].endMathPoint[0], 3) == Math.Round(listCurveMid[j][i + 2].endMathPoint[0], 3) &&
                                    Math.Round(listCurveMid[j][i].endMathPoint[1], 3) == Math.Round(listCurveMid[j][i + 2].endMathPoint[1], 3)))
                                {
                                    x = (float)(e.startMathPoint[0]);
                                    y = (float)(e.startMathPoint[1]);
                                }
                            }
                            else if (i == listCurveMid[j].Count - 1)
                            {
                                if ((Math.Round(listCurveMid[j][i].startMathPoint[0], 3) == Math.Round(listCurveMid[j][i - 2].startMathPoint[0], 3) &&
                              Math.Round(listCurveMid[j][i].startMathPoint[1], 3) == Math.Round(listCurveMid[j][i - 2].startMathPoint[1], 3)) ||
                              (Math.Round(listCurveMid[j][i].startMathPoint[0], 3) == Math.Round(listCurveMid[j][i - 2].endMathPoint[0], 3) &&
                              Math.Round(listCurveMid[j][i].startMathPoint[1], 3) == Math.Round(listCurveMid[j][i - 2].endMathPoint[1], 3)))
                                {
                                    x = (float)(e.endMathPoint[0]);
                                    y = (float)(e.endMathPoint[1]);
                                }
                                else if ((Math.Round(listCurveMid[j][i].endMathPoint[0], 3) == Math.Round(listCurveMid[j][i - 2].startMathPoint[0], 3) &&
                                    Math.Round(listCurveMid[j][i].endMathPoint[1], 3) == Math.Round(listCurveMid[j][i - 2].startMathPoint[1], 3)) ||
                                    (Math.Round(listCurveMid[j][i].endMathPoint[0], 3) == Math.Round(listCurveMid[j][i - 2].endMathPoint[0], 3) &&
                                    Math.Round(listCurveMid[j][i].endMathPoint[1], 3) == Math.Round(listCurveMid[j][i - 2].endMathPoint[1], 3)))
                                {
                                    x = (float)(e.startMathPoint[0]);
                                    y = (float)(e.startMathPoint[1]);
                                }
                            }
                            else if ((list0 !=-1)&&((j == 0 && list0 == i) || (j == 1 && list1 == i)))
                            {
                                    double dx1 = interx-e.startMathPoint[0];
                                    double dy1 = intery - e.startMathPoint[1];

                                    double dx2 = interx - e.endMathPoint[0];
                                    double dy2 = intery - e.endMathPoint[1];

                                    double length1 = Math.Sqrt(dx1 * dx1 + dy1 * dy1);
                                    double length2 = Math.Sqrt(dx2 * dx2 + dy2 * dy2);

                                    if (length1>=length2)
                                    {
                                        x= (float)(interx-dx1/ 2);
                                        y = (float)(intery - dy1 / 2);
                                    }
                                    else
                                    {
                                        x = (float)(interx - dx2 / 2);
                                        y = (float)(intery - dy2 / 2);
                                    }
                            }
                                if (j == 0)
                            {
                                if (Math.Round(stay) == 0)
                                {
                                    stringFormat.LineAlignment = StringAlignment.Near;
                                }
                                else if (Math.Round(stax) == 0)
                                {
                                    stringFormat.Alignment = StringAlignment.Far;
                                }
                                else if (stay > 0)
                                {
                                    stringFormat.Alignment = StringAlignment.Far;
                                    stringFormat.LineAlignment = StringAlignment.Near;
                                }
                                else
                                {
                                    stringFormat.Alignment = StringAlignment.Far;
                                    stringFormat.LineAlignment = StringAlignment.Far;
                                }
                            }
                            else
                            {
                                if (Math.Round(stay) == 0)
                                {
                                    stringFormat.LineAlignment = StringAlignment.Far;
                                }
                                else if (Math.Round(stax) == 0)
                                {
                                    stringFormat.Alignment = StringAlignment.Near;
                                }
                                else if (stay > 0)
                                {
                                    stringFormat.Alignment = StringAlignment.Near;
                                    stringFormat.LineAlignment = StringAlignment.Far;
                                }
                                else
                                {
                                    stringFormat.Alignment = StringAlignment.Near;
                                    stringFormat.LineAlignment = StringAlignment.Near;
                                }
                            }

                            // Draw string to screen.
                            g.DrawString(drawString, drawFont, drawBrush, x, y, stringFormat);
                        }
                        else if (e.r != 0)
                        {
                            double total = Math.Round((e.length / e.r) * (180 / Math.PI), 1);

                            double stax = e.centerpoint[0] - e.startMathPoint[0];
                            double stay = e.centerpoint[1] - e.startMathPoint[1];

                            double endx = e.centerpoint[0] - e.endMathPoint[0];
                            double endy = e.centerpoint[1] - e.endMathPoint[1];

                            double vertx = endx + stax;
                            double verty = endy + stay;

                            double positionx = 0;
                            double positiony = 0;

                            String drawString = Math.Round(180 - total).ToString() + "°";

                            // Create font and brush.
                            Font drawFont = new Font("仿宋", Convert.ToSingle(coox));
                            SolidBrush drawBrush = new SolidBrush(Color.Black);

                            // Create point for upper-left corner of drawing.
                            float x = (float)(positionx);
                            float y = (float)(positiony);

                            if ((Math.Round(listCurveMid[j][i - 1].startMathPoint[0], 3) == Math.Round(listCurveMid[j][i + 1].startMathPoint[0], 3) &&
                               Math.Round(listCurveMid[j][i - 1].startMathPoint[1], 3) == Math.Round(listCurveMid[j][i + 1].startMathPoint[1], 3)) ||
                               (Math.Round(listCurveMid[j][i - 1].startMathPoint[0], 3) == Math.Round(listCurveMid[j][i + 1].endMathPoint[0], 3) &&
                               Math.Round(listCurveMid[j][i - 1].startMathPoint[1], 3) == Math.Round(listCurveMid[j][i + 1].endMathPoint[1], 3)))
                            {
                                x = (float)(listCurveMid[j][i - 1].startMathPoint[0]);
                                y = (float)(listCurveMid[j][i - 1].startMathPoint[1]);
                            }
                            else if ((Math.Round(listCurveMid[j][i - 1].endMathPoint[0], 3) == Math.Round(listCurveMid[j][i + 1].startMathPoint[0], 3) &&
                                Math.Round(listCurveMid[j][i - 1].endMathPoint[1], 3) == Math.Round(listCurveMid[j][i + 1].startMathPoint[1], 3)) ||
                                (Math.Round(listCurveMid[j][i - 1].endMathPoint[0], 3) == Math.Round(listCurveMid[j][i + 1].endMathPoint[0], 3) &&
                                Math.Round(listCurveMid[j][i - 1].endMathPoint[1], 3) == Math.Round(listCurveMid[j][i + 1].endMathPoint[1], 3)))
                            {
                                x = (float)(listCurveMid[j][i - 1].endMathPoint[0]);
                                y = (float)(listCurveMid[j][i - 1].endMathPoint[1]);
                            }

                            StringFormat stringFormat = new StringFormat();
                            stringFormat.Alignment = StringAlignment.Center;
                            stringFormat.LineAlignment = StringAlignment.Center;

                            if (vertx > 0 && verty > 0)
                            {
                                stringFormat.Alignment = StringAlignment.Far;
                                stringFormat.LineAlignment = StringAlignment.Far;
                            }
                            else if (vertx > 0 && verty < 0)
                            {
                                stringFormat.Alignment = StringAlignment.Far;
                                stringFormat.LineAlignment = StringAlignment.Near;
                            }
                            else if (vertx < 0 && verty < 0)
                            {
                                stringFormat.Alignment = StringAlignment.Near;
                                stringFormat.LineAlignment = StringAlignment.Near;
                            }
                            else if (vertx < 0 && verty > 0)
                            {
                                stringFormat.Alignment = StringAlignment.Near;
                                stringFormat.LineAlignment = StringAlignment.Far;
                            }
                            else if (vertx == 0 && verty > 0)
                            {
                                stringFormat.LineAlignment = StringAlignment.Far;
                            }
                            else if (vertx == 0 && verty < 0)
                            {
                                stringFormat.LineAlignment = StringAlignment.Near;
                            }
                            else if (vertx > 0 && verty == 0)
                            {
                                stringFormat.Alignment = StringAlignment.Far;
                            }
                            else if (vertx < 0 && verty == 0)
                            {
                                stringFormat.Alignment = StringAlignment.Near;
                            }

                            // Draw string to screen.
                            g.DrawString(drawString, drawFont, drawBrush, x, y, stringFormat);

                        }
                    }
                }
            }
            catch (System.Exception e1)
            {
                Relation.Exp(e1);
            }
            }
        public void SaveBit(String address)
        { bi.Save(address); }
        
    }
}
