using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Reflection;

using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swpublished;
using SolidWorks.Interop.swconst;
using SolidWorksTools;
using SolidWorksTools.File;
using System.Collections.Generic;
using System.Diagnostics;

using System.Windows.Forms;
using System.IO;
using System.Linq;

using Microsoft.VisualBasic;

using System.Text.RegularExpressions;

namespace 钣金CustomCommand
{
    /// <summary>
    /// Summary description for 钣金CustomCommand.
    /// </summary>
    [Guid("6248fff8-61fa-4958-951b-bb515c444ee1"), ComVisible(true)]
    [SwAddin(
         Description = "钣金CustomCommand description",
         Title = "钣金CustomCommand",
         LoadAtStartup = true
         )]
    public class SwAddin : ISwAddin
    {
        #region Local Variables
        ISldWorks iSwApp = null;
        ICommandManager iCmdMgr = null;
        int addinID = 0;
        BitmapHandler iBmp;

        public static string direct;
        public const int mainCmdGroupID = 5;
        public const int mainItemID1 = 0;
        public const int mainItemID2 = 1;
        public const int mainItemID3 = 2;
        public const int flyoutGroupID = 91;

        #region Event Handler Variables
        Hashtable openDocs = new Hashtable();
        SolidWorks.Interop.sldworks.SldWorks SwEventPtr = null;
        #endregion

        // Public Properties
        public ISldWorks SwApp
        {
            get { return iSwApp; }
        }
        public ICommandManager CmdMgr
        {
            get { return iCmdMgr; }
        }

        public Hashtable OpenDocs
        {
            get { return openDocs; }
        }

        #endregion

        #region SolidWorks Registration
        [ComRegisterFunctionAttribute]
        public static void RegisterFunction(Type t)
        {
            #region Get Custom Attribute: SwAddinAttribute
            SwAddinAttribute SWattr = null;
            Type type = typeof(SwAddin);

            foreach (System.Attribute attr in type.GetCustomAttributes(false))
            {
                if (attr is SwAddinAttribute)
                {
                    SWattr = attr as SwAddinAttribute;
                    break;
                }
            }

            #endregion

            try
            {
                Microsoft.Win32.RegistryKey hklm = Microsoft.Win32.Registry.LocalMachine;
                Microsoft.Win32.RegistryKey hkcu = Microsoft.Win32.Registry.CurrentUser;

                string keyname = "SOFTWARE\\SolidWorks\\Addins\\{" + t.GUID.ToString() + "}";
                Microsoft.Win32.RegistryKey addinkey = hklm.CreateSubKey(keyname);
                addinkey.SetValue(null, 0);

                addinkey.SetValue("Description", SWattr.Description);
                addinkey.SetValue("Title", SWattr.Title);

                keyname = "Software\\SolidWorks\\AddInsStartup\\{" + t.GUID.ToString() + "}";
                addinkey = hkcu.CreateSubKey(keyname);
                addinkey.SetValue(null, Convert.ToInt32(SWattr.LoadAtStartup), Microsoft.Win32.RegistryValueKind.DWord);
            }
            catch (System.NullReferenceException nl)
            {
                Console.WriteLine("There was a problem registering this dll: SWattr is null. \n\"" + nl.Message + "\"");
                System.Windows.Forms.MessageBox.Show("There was a problem registering this dll: SWattr is null.\n\"" + nl.Message + "\"");
            }

            catch (System.Exception e)
            {
                Console.WriteLine(e.Message);

                System.Windows.Forms.MessageBox.Show("There was a problem registering the function: \n\"" + e.Message + "\"");
            }
        }

        [ComUnregisterFunctionAttribute]
        public static void UnregisterFunction(Type t)
        {
            try
            {
                Microsoft.Win32.RegistryKey hklm = Microsoft.Win32.Registry.LocalMachine;
                Microsoft.Win32.RegistryKey hkcu = Microsoft.Win32.Registry.CurrentUser;

                string keyname = "SOFTWARE\\SolidWorks\\Addins\\{" + t.GUID.ToString() + "}";
                hklm.DeleteSubKey(keyname);

                keyname = "Software\\SolidWorks\\AddInsStartup\\{" + t.GUID.ToString() + "}";
                hkcu.DeleteSubKey(keyname);
            }
            catch (System.NullReferenceException nl)
            {
                Console.WriteLine("There was a problem unregistering this dll: " + nl.Message);
                System.Windows.Forms.MessageBox.Show("There was a problem unregistering this dll: \n\"" + nl.Message + "\"");
            }
            catch (System.Exception e)
            {
                Console.WriteLine("There was a problem unregistering this dll: " + e.Message);
                System.Windows.Forms.MessageBox.Show("There was a problem unregistering this dll: \n\"" + e.Message + "\"");
            }
        }

        #endregion

        #region ISwAddin Implementation
        public SwAddin()
        {
        }

        public bool ConnectToSW(object ThisSW, int cookie)
        {
            iSwApp = (ISldWorks)ThisSW;
            addinID = cookie;

            direct = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            //Setup callbacks
            iSwApp.SetAddinCallbackInfo(0, this, addinID);

            #region Setup the Command Manager
            iCmdMgr = iSwApp.GetCommandManager(cookie);
            AddCommandMgr();
            #endregion

            #region Setup the Event Handlers
            SwEventPtr = (SolidWorks.Interop.sldworks.SldWorks)iSwApp;
            openDocs = new Hashtable();
            #endregion

            return true;
        }

        public bool DisconnectFromSW()
        {
            RemoveCommandMgr();

            System.Runtime.InteropServices.Marshal.ReleaseComObject(iCmdMgr);
            iCmdMgr = null;
            System.Runtime.InteropServices.Marshal.ReleaseComObject(iSwApp);
            iSwApp = null;
            //The addin _must_ call GC.Collect() here in order to retrieve all managed code pointers 
            GC.Collect();
            GC.WaitForPendingFinalizers();

            GC.Collect();
            GC.WaitForPendingFinalizers();

            return true;
        }
        #endregion

        #region UI Methods
        public void AddCommandMgr()
        {
            ICommandGroup cmdGroup;

            cmdGroup = iCmdMgr.CreateCommandGroup2(mainCmdGroupID, "钣金CustomCommand", "钣金CustomCommand", "", -1, true, 0);
            cmdGroup.LargeIconList = direct + "\\ToolbarLarge.bmp";
            cmdGroup.SmallIconList = direct + "\\ToolbarSmall.bmp";
            cmdGroup.LargeMainIcon = direct + "\\MainIconLarge.bmp";
            cmdGroup.SmallMainIcon = direct + "\\MainIconSmall.bmp";

            int menuToolbarOption = (int)(swCommandItemType_e.swMenuItem | swCommandItemType_e.swToolbarItem);
            cmdGroup.AddCommandItem2("钣金CustomCommand", -1, "钣金CustomCommand", "钣金CustomCommand", 0, "钣金CustomCommand", "", mainItemID1, menuToolbarOption);

            cmdGroup.HasToolbar = false;
            cmdGroup.HasMenu = true;
            cmdGroup.Activate();


        }

        public void RemoveCommandMgr()
        {
            iCmdMgr.RemoveCommandGroup(mainCmdGroupID);
            iCmdMgr.RemoveFlyoutGroup(flyoutGroupID);
        }

        #endregion

        #region UI Callbacks

        public void 钣金CustomCommand()
        {
            try
            {
            int errors = 0;
            int warnings = 0;
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Multiselect = true;
            fileDialog.Title = "请选择文件";
            fileDialog.Filter = "所有文件(*.sldprt)|*.sldprt";
            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                string[] files = fileDialog.FileNames;

                double  coox = Convert.ToDouble(Interaction.InputBox("输入文字大小：", "文字大小输入", "", -1, -1));

                foreach (string file1 in files)
                {
                    BitM bitM = new BitM(coox);

                    ModelDoc2 part = (ModelDoc2)iSwApp.OpenDoc6(file1,1,0,null,errors,warnings);

            if (part is PartDoc)
            {
                FeatureManager featureManager = part.FeatureManager;

                SheetMetalFolder sheetMetalFolder = (SheetMetalFolder)featureManager.GetSheetMetalFolder();
                int sheetmetalcount = sheetMetalFolder.GetSheetMetalCount();
                if (sheetmetalcount > 0)
                {
                    Dictionary<int, string> dictionary1 = new Dictionary<int, string>();
                    int position = 0;
                    List<string> list1 = new List<string>();
                    List<string> list2 = new List<string>();
                    object[] fs = (object[])featureManager.GetFeatures(false);
                    for (int i = 0; i < fs.Length; i++)
                    {
                        Feature feature2 = (Feature)fs[i];
                        string typename = feature2.GetTypeName2();
                        list1.Add(typename);
                        list2.Add(feature2.Name);
                        if (Regex.IsMatch(typename, "EdgeFlange|SMMiteredFlange|SketchBend"))
                        {
                            dictionary1.Add(i, typename);
                        }
                        if (Regex.IsMatch(typename, "SMBaseFlange"))
                        {
                            position = i - 1;
                        }
                    }

                    Feature featureS = sheetMetalFolder.GetFeature();
                    SheetMetalFeatureData sheetMetalFeatureData1 = (SheetMetalFeatureData)featureS.GetDefinition();
                    double hou = Math.Round(sheetMetalFeatureData1.Thickness * 1000, 3);
                    double k = sheetMetalFeatureData1.KFactor;

                    List<string> marks = new List<string>();

                    foreach (int item in dictionary1.Keys)
                    {
                        if (dictionary1[item] == "EdgeFlange")
                        {
                            Feature feature3 = (Feature)fs[item];
                            EdgeFlangeFeatureData oneBendFeature2 = (EdgeFlangeFeatureData)feature3.GetDefinition();
                            oneBendFeature2.AccessSelections(part, null);
                            int co = oneBendFeature2.GetEdgeCount();
                            Edge edge = (Edge)oneBendFeature2.Edge;
                            Curve curve = (Curve)edge.GetCurve();
                            double[] dc = (double[])curve.LineParams;

                            dc[3] = Math.Round(dc[3], 5);
                            dc[4] = Math.Round(dc[4], 5);
                            dc[5] = Math.Round(dc[5], 5);

                            if (dc[3] != 0 && !marks.Contains("x"))
                                marks.Add("x");
                            else if (dc[4] != 0 && !marks.Contains("y"))
                                marks.Add("y");
                            else if (dc[5] != 0 && !marks.Contains("z"))
                                marks.Add("z");
                        }
                        else if (dictionary1[item] == "SMMiteredFlange")
                        {
                            Feature feature3 = (Feature)fs[item];
                            IMiterFlangeFeatureData oneBendFeature2 = (IMiterFlangeFeatureData)feature3.GetDefinition();
                            oneBendFeature2.AccessSelections(part, null);
                            int co = oneBendFeature2.IGetEdgesCount();
                            Object[] edges = (Object[])oneBendFeature2.Edges;
                            Edge edge = (Edge)edges[0];
                            Curve curve = (Curve)edge.GetCurve();
                            double[] dc = (double[])curve.LineParams;

                            dc[3] = Math.Round(dc[3], 5);
                            dc[4] = Math.Round(dc[4], 5);
                            dc[5] = Math.Round(dc[5], 5);

                            if (dc[3] != 0 && !marks.Contains("x"))
                                marks.Add("x");
                            else if (dc[4] != 0 && !marks.Contains("y"))
                                marks.Add("y");
                            else if (dc[5] != 0 && !marks.Contains("z"))
                                marks.Add("z");
                        }
                        else if (dictionary1[item] == "SketchBend")
                        {//得到草图
                         //SelectionMgr selectionMgr =(SelectionMgr)part.SelectionManager;
                                        MathUtility mathUtil = (MathUtility)iSwApp.GetMathUtility();

                                        Feature feature3 = (Feature)fs[position];
                            Sketch oneBendFeature2 = (Sketch)feature3.GetSpecificFeature2();
                                    part.Extension.SelectByID2(list2[position], "SKETCH", 0, 0, 0, false, 0, null, 0);
                                     int co = 0;                          
                                      
                         object face = oneBendFeature2.GetReferenceEntity(ref co);
                          //  Face face1 = (Face)face;
                            double[] edges = (double[])oneBendFeature2.GetLines2(1);
                            double[] line1star1 = new double[] { edges[6], edges[7], edges[8] };
                            double[] line1end1 = new double[] { edges[9], edges[10], edges[11] };
                            double[] line2star1 = new double[] { edges[18], edges[19], edges[20] };
                            double[] line2end1 = new double[] { edges[21], edges[22], edges[23] };

                            MathPoint star1 = (MathPoint)mathUtil.CreatePoint(line1star1);
                            MathPoint end1 = (MathPoint)mathUtil.CreatePoint(line1end1);
                            MathPoint star2 = (MathPoint)mathUtil.CreatePoint(line2star1);
                            MathPoint end2 = (MathPoint)mathUtil.CreatePoint(line2end1);

                            MathTransform MathTrans = oneBendFeature2.ModelToSketchTransform;
                            object MathTransre =MathTrans.Inverse();

                            star1 = (MathPoint)star1.MultiplyTransform(MathTransre);
                            end1 = (MathPoint)end1.MultiplyTransform(MathTransre);
                            star2 = (MathPoint)star2.MultiplyTransform(MathTransre);
                            end2 = (MathPoint)end2.MultiplyTransform(MathTransre);

                             double[] line1star = (double[])star1.ArrayData;
                             double[] line1end = (double[])end1.ArrayData;
                             double[] line2star = (double[])star2.ArrayData;
                             double[] line2end = (double[])end2.ArrayData;

                            double[] line1vex = new double[] { line1star[0] - line1end[0], line1star[1] - line1end[1], line1star[2] - line1end[2] };
                            double[] line2vex = new double[] { line2star[0] - line2end[0], line2star[1] - line2end[1], line2star[2] - line2end[2] };

                            double[] linevex = new double[3];
                            linevex[0] = line1vex[1] * line2vex[2] - line1vex[2] * line2vex[1];
                            linevex[1] = line1vex[2] * line2vex[0] - line1vex[0] * line2vex[2];
                            linevex[2] = line1vex[0] * line2vex[1] - line1vex[1] * line2vex[0];

                            if (Math.Round( linevex[0],6) != 0 && !marks.Contains("x"))
                                marks.Add("x");
                            else if (Math.Round(linevex[1], 6) != 0 && !marks.Contains("y"))
                                marks.Add("y");
                            else if (Math.Round(linevex[2],6) != 0 && !marks.Contains("z"))
                                marks.Add("z");
                           // part.InsertSketch2(false);
                        }
                    }
                    featureManager.EditRollback((int)swMoveRollbackBarTo_e.swMoveRollbackBarToEnd, "");
                    //Object[] features = featureManager.GetFeatures(true);

                    //foreach (Object item2 in features)
                    //{
                    //    Feature feature = (Feature)item2;
                    //    string featureName = feature.Name;
                    //}
                    //------------
                    PartDoc partDoc = (PartDoc)part;
                    object[] bodys = (object[])partDoc.GetBodies2(0, true);

                    foreach (object item in bodys)
                    {
                        Body2 body2Metal = (Body2)item;
                        int body2Ledges = body2Metal.GetEdgeCount();
                        object[] edges = (object[])body2Metal.GetEdges();

                        List<CurveInfo>[] listCurveMidLast =null;//通过k因子得到的中性层线段


                                    if (marks.Count == 3)
                                    {
                                        listCurveMidLast = new List<CurveInfo>[2];
                                    }
                                    else
                                    {
                                        listCurveMidLast = new List<CurveInfo>[marks.Count];
                                    }

                                    Boolean tranformt = false;

                                    if (marks.Count==2)
                                    {
                                        string tranfromcomb = marks[0] + marks[1];

                                        switch (tranfromcomb)
                                        {
                                            case "xy":
                                                tranformt = true;
                                                break;
                                            case "yx":
                                                tranformt = true;
                                                break;
                                            case "yz":
                                                tranformt = true;
                                                break;
                                            case "zy":
                                                tranformt = true;
                                                break;
                                            case "xz":
                                                tranformt = false;
                                                break;
                                            case "zx":
                                                tranformt = false;
                                                break;
                                        }
                                    }

                                    for (int i = 0; i < marks.Count; i++)
                        {
                            if (i > 1) break;                                 

                            List<CurveInfo>[] listCurve1 = new List<CurveInfo>[2];

                            listCurve1[0] = new List<CurveInfo>();//直线存储
                            listCurve1[1] = new List<CurveInfo>();//圆弧存储
                            
                         List<CurveInfo> listCurveMid = new List<CurveInfo>();
                            foreach (Object item1 in edges)
                            {
                                Edge edge = (Edge)item1;

                                CurveInfo curveInfo = new CurveInfo();
                                Curve curve = (Curve)edge.GetCurve();

                                CurveParamData curveParam = edge.GetCurveParams3();
                                int curveType = curveParam.CurveType;

                                double[] curveParams = (double[])edge.GetCurveParams2();

                                curveInfo.length = 1000 * curve.GetLength2(curveParams[6], curveParams[7]);

                                double[][] cor = null;

                                if (marks[i] == "x") cor = GetStartPoint(curveParams[1], curveParams[2], curveParams[4], curveParams[5]);
                                if (marks[i] == "y") cor = GetStartPoint(curveParams[0], curveParams[2], curveParams[3], curveParams[5]);
                                if (marks[i] == "z") cor = GetStartPoint(curveParams[0], curveParams[1], curveParams[3], curveParams[4]);

                                if (cor == null) continue;

                                curveInfo.startMathPoint = cor[0];
                                curveInfo.endMathPoint = cor[1];

                                            double ang = 0;
                                if (curveType == 3002)// 直线3001，圆3002，曲线3005；
                                {


                                    double[] lineParams = (double[])curve.CircleParams;
                                    curveInfo.r = lineParams[6] * 1000;
                                    ang = curveInfo.length / curveInfo.r;
                                    if (marks[i] == "x")
                                    {
                                        if (Math.Round(lineParams[3], 3) == 0) continue;
                                        curveInfo.centerpoint = new double[] { lineParams[1] * 1000, lineParams[2] * 1000 };
                                    }
                                    if (marks[i] == "y")
                                    {
                                        if (Math.Round(lineParams[4], 3) == 0) continue;
                                        curveInfo.centerpoint = new double[] { lineParams[0] * 1000, lineParams[2] * 1000 };
                                    }
                                    if (marks[i] == "z")
                                    {
                                        if (Math.Round(lineParams[5], 3) == 0) continue;
                                        curveInfo.centerpoint = new double[] { lineParams[0] * 1000, lineParams[1] * 1000 };
                                    }
                                }

                                if (curveType == 3001 && !listCurve1[0].Contains(curveInfo))
                                {
                                    listCurve1[0].Add(curveInfo);
                                }
                                else if ((curveType == 3002) && !listCurve1[1].Contains(curveInfo) && ang<Math.PI)
                                {
                                    listCurve1[1].Add(curveInfo);
                                }

                            }
                            //圆弧处理，即将连续的圆弧合为一段 
                            int index = 0;
                            CurveInfo curveInfoarc = new CurveInfo();
                            while (index < listCurve1[1].Count - 1)
                            {
                                for (int j = index + 1; j < listCurve1[1].Count; j++)
                                {
                                    if (Math.Round(listCurve1[1][j].centerpoint[0], 2) == Math.Round(listCurve1[1][index].centerpoint[0], 2) &&
                                        Math.Round(listCurve1[1][j].centerpoint[1], 2) == Math.Round(listCurve1[1][index].centerpoint[1], 2) &&
                                         (Math.Round(listCurve1[1][j].r, 2) == Math.Round(listCurve1[1][index].r, 2)))
                                    {
                                        if (Math.Round(listCurve1[1][j].startMathPoint[0], 2) == Math.Round(listCurve1[1][index].startMathPoint[0], 2) &&
                                        Math.Round(listCurve1[1][j].startMathPoint[1], 2) == Math.Round(listCurve1[1][index].startMathPoint[1], 2))
                                        {
                                            double dx1 = listCurve1[1][j].endMathPoint[0] - listCurve1[1][j].centerpoint[0];
                                            double dy1 = listCurve1[1][j].endMathPoint[1] - listCurve1[1][j].centerpoint[1];

                                            double dx2 = listCurve1[1][index].endMathPoint[0] - listCurve1[1][j].centerpoint[0];
                                            double dy2 = listCurve1[1][index].endMathPoint[1] - listCurve1[1][j].centerpoint[1];

                                            double angle = Math.Acos((dx1 * dx2 + dy1 * dy2) / (listCurve1[1][j].r * listCurve1[1][j].r));
                                            double angle1 = listCurve1[1][j].length / listCurve1[1][j].r;
                                            double angle2 = listCurve1[1][index].length / listCurve1[1][index].r;

                                            if (Math.Round(angle, 2) == Math.Round(angle2 - angle1, 2))
                                            {
                                                curveInfoarc = listCurve1[1][j];
                                                break;
                                            }
                                            else if (Math.Round(angle, 2) == Math.Round(angle1 - angle2, 2))
                                            {
                                                curveInfoarc = listCurve1[1][index];
                                                break;
                                            }
                                            else if (Math.Round(angle, 2) == Math.Round(angle1 + angle2, 2))
                                            {
                                                double[][] dcor = GetStartPoint(listCurve1[1][j].endMathPoint[0] / 1000, listCurve1[1][j].endMathPoint[1] / 1000,
                                                    listCurve1[1][index].endMathPoint[0] / 1000, listCurve1[1][index].endMathPoint[1] / 1000);
                                                listCurve1[1][j].startMathPoint = new double[] { dcor[0][0], dcor[0][1] };
                                                listCurve1[1][j].endMathPoint = new double[] { dcor[1][0], dcor[1][1] };
                                                listCurve1[1][j].length = listCurve1[1][j].length + listCurve1[1][index].length;
                                                curveInfoarc = listCurve1[1][index];
                                                break;
                                            }
                                        }
                                        else if (Math.Round(listCurve1[1][j].endMathPoint[0], 2) == Math.Round(listCurve1[1][index].endMathPoint[0], 2) &&
                                        Math.Round(listCurve1[1][j].endMathPoint[1], 2) == Math.Round(listCurve1[1][index].endMathPoint[1], 2))
                                        {
                                            double dx1 = listCurve1[1][j].startMathPoint[0] - listCurve1[1][j].centerpoint[0];
                                            double dy1 = listCurve1[1][j].startMathPoint[1] - listCurve1[1][j].centerpoint[1];

                                            double dx2 = listCurve1[1][index].startMathPoint[0] - listCurve1[1][j].centerpoint[0];
                                            double dy2 = listCurve1[1][index].startMathPoint[1] - listCurve1[1][j].centerpoint[1];

                                            double angle = Math.Acos((dx1 * dx2 + dy1 * dy2) / (listCurve1[1][j].r * listCurve1[1][j].r));
                                            double angle1 = listCurve1[1][j].length / listCurve1[1][j].r;
                                            double angle2 = listCurve1[1][index].length / listCurve1[1][index].r;

                                            if (Math.Round(angle, 2) == Math.Round(angle2 - angle1, 2))
                                            {
                                                curveInfoarc = listCurve1[1][j];
                                                break;
                                            }
                                            else if (Math.Round(angle, 2) == Math.Round(angle1 - angle2, 2))
                                            {
                                                curveInfoarc = listCurve1[1][index];
                                                break;
                                            }
                                            else if (Math.Round(angle, 2) == Math.Round(angle1 + angle2, 2))
                                            {
                                                double[][] dcor = GetStartPoint(listCurve1[1][j].startMathPoint[0] / 1000, listCurve1[1][j].startMathPoint[1] / 1000,
                                                    listCurve1[1][index].startMathPoint[0] / 1000, listCurve1[1][index].startMathPoint[1] / 1000);
                                                listCurve1[1][j].startMathPoint = new double[] { dcor[0][0], dcor[0][1] };
                                                listCurve1[1][j].endMathPoint = new double[] { dcor[1][0], dcor[1][1] };
                                                listCurve1[1][j].length = listCurve1[1][j].length + listCurve1[1][index].length;
                                                curveInfoarc = listCurve1[1][index];
                                                break;
                                            }
                                        }
                                        else if (Math.Round(listCurve1[1][j].endMathPoint[0], 2) == Math.Round(listCurve1[1][index].startMathPoint[0], 2) &&
                                        Math.Round(listCurve1[1][j].endMathPoint[1], 2) == Math.Round(listCurve1[1][index].startMathPoint[1], 2))
                                        {
                                            double dx1 = listCurve1[1][j].startMathPoint[0] - listCurve1[1][j].centerpoint[0];
                                            double dy1 = listCurve1[1][j].startMathPoint[1] - listCurve1[1][j].centerpoint[1];

                                            double dx2 = listCurve1[1][index].endMathPoint[0] - listCurve1[1][j].centerpoint[0];
                                            double dy2 = listCurve1[1][index].endMathPoint[1] - listCurve1[1][j].centerpoint[1];

                                            double angle = Math.Acos((dx1 * dx2 + dy1 * dy2) / (listCurve1[1][j].r * listCurve1[1][j].r));
                                            double angle1 = listCurve1[1][j].length / listCurve1[1][j].r;
                                            double angle2 = listCurve1[1][index].length / listCurve1[1][index].r;

                                            if (Math.Round(angle, 2) == Math.Round(angle2 - angle1, 2))
                                            {
                                                curveInfoarc = listCurve1[1][j];
                                                break;
                                            }
                                            else if (Math.Round(angle, 2) == Math.Round(angle1 - angle2, 2))
                                            {
                                                curveInfoarc = listCurve1[1][index];
                                                break;
                                            }
                                            else if (Math.Round(angle, 2) == Math.Round(angle1 + angle2, 2))
                                            {
                                                double[][] dcor = GetStartPoint(listCurve1[1][j].startMathPoint[0] / 1000, listCurve1[1][j].startMathPoint[1] / 1000,
                                                    listCurve1[1][index].endMathPoint[0] / 1000, listCurve1[1][index].endMathPoint[1] / 1000);
                                                listCurve1[1][j].startMathPoint = new double[] { dcor[0][0], dcor[0][1] };
                                                listCurve1[1][j].endMathPoint = new double[] { dcor[1][0], dcor[1][1] };
                                                listCurve1[1][j].length = listCurve1[1][j].length + listCurve1[1][index].length;
                                                curveInfoarc = listCurve1[1][index];
                                                break;
                                            }
                                        }
                                        else if (Math.Round(listCurve1[1][j].startMathPoint[0], 2) == Math.Round(listCurve1[1][index].endMathPoint[0], 2) &&
                                        Math.Round(listCurve1[1][j].startMathPoint[1], 2) == Math.Round(listCurve1[1][index].endMathPoint[1], 2))
                                        {
                                            double dx1 = listCurve1[1][j].endMathPoint[0] - listCurve1[1][j].centerpoint[0];
                                            double dy1 = listCurve1[1][j].endMathPoint[1] - listCurve1[1][j].centerpoint[1];

                                            double dx2 = listCurve1[1][index].startMathPoint[0] - listCurve1[1][j].centerpoint[0];
                                            double dy2 = listCurve1[1][index].startMathPoint[1] - listCurve1[1][j].centerpoint[1];

                                            double angle = Math.Acos((dx1 * dx2 + dy1 * dy2) / (listCurve1[1][j].r * listCurve1[1][j].r));
                                            double angle1 = listCurve1[1][j].length / listCurve1[1][j].r;
                                            double angle2 = listCurve1[1][index].length / listCurve1[1][index].r;

                                            if (Math.Round(angle, 2) == Math.Round(angle2 - angle1, 2))
                                            {
                                                curveInfoarc = listCurve1[1][j];
                                                break;
                                            }
                                            else if (Math.Round(angle, 2) == Math.Round(angle1 - angle2, 2))
                                            {
                                                curveInfoarc = listCurve1[1][index];
                                                break;
                                            }
                                            else if (Math.Round(angle, 2) == Math.Round(angle1 + angle2, 2))
                                            {
                                                double[][] dcor = GetStartPoint(listCurve1[1][j].endMathPoint[0] / 1000, listCurve1[1][j].endMathPoint[1] / 1000,
                                                    listCurve1[1][index].startMathPoint[0] / 1000, listCurve1[1][index].startMathPoint[1] / 1000);
                                                listCurve1[1][j].startMathPoint = new double[] { dcor[0][0], dcor[0][1] };
                                                listCurve1[1][j].endMathPoint = new double[] { dcor[1][0], dcor[1][1] };
                                                listCurve1[1][j].length = listCurve1[1][j].length + listCurve1[1][index].length;
                                                curveInfoarc = listCurve1[1][index];
                                                break;
                                            }
                                        }
                                    }
                                    if (j == listCurve1[1].Count - 1)
                                    {
                                        index += 1;
                                        curveInfoarc = new CurveInfo();
                                    }
                                }
                                if (listCurve1[1].Contains(curveInfoarc)) listCurve1[1].Remove(curveInfoarc);
                            }

                            index = 0;
                            int mid = 0;
                            //去除多余线段
                            while (index < listCurve1[0].Count)
                            {
                                CurveInfo curveInfoline = null;
                                for (int j = index; j < listCurve1[0].Count; j++)
                                {            
                                    if (j == listCurve1[0].Count - 1)
                                    {
                                        index = j + 1;
                                    }
                                    else
                                    {
                                        index = j;
                                    }

                                    if (FindInter(listCurve1[1], listCurve1[0][j], listCurve1[0], hou))
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        curveInfoline = listCurve1[0][j];
                                        break;
                                    }
                                }
                                if (curveInfoline != null)
                                {
                                    listCurve1[0].Remove(curveInfoline);
                                }
                            }

                            //直线处理，即将连续的直线合为一段
                            //  List<CurveInfo> listcordcopy = new List<CurveInfo>(listCurve1[0].ToArray());
                            index = 0;
                            mid = 0;
                            while (index < listCurve1[0].Count - 1)
                            {
                                for (int j = index + 1; j < listCurve1[0].Count; j++)
                                {
                                    double dx1 = listCurve1[0][j].endMathPoint[0] - listCurve1[0][index].startMathPoint[0];
                                    double dy1 = listCurve1[0][j].endMathPoint[1] - listCurve1[0][index].startMathPoint[1];

                                    double dx2 = listCurve1[0][j].endMathPoint[0] - listCurve1[0][index].endMathPoint[0];
                                    double dy2 = listCurve1[0][j].endMathPoint[1] - listCurve1[0][index].endMathPoint[1];

                                    double dx1y2 = Math.Round(dx1 * dy2, 2);
                                    double dx2y1 = Math.Round(dx2 * dy1, 2);

                                    double dx11 = listCurve1[0][j].startMathPoint[0] - listCurve1[0][index].startMathPoint[0];
                                    double dy11 = listCurve1[0][j].startMathPoint[1] - listCurve1[0][index].startMathPoint[1];

                                    double dx22 = listCurve1[0][j].startMathPoint[0] - listCurve1[0][index].endMathPoint[0];
                                    double dy22 = listCurve1[0][j].startMathPoint[1] - listCurve1[0][index].endMathPoint[1];

                                    double dx11y22 = Math.Round(dx11 * dy22, 2);
                                    double dx22y11 = Math.Round(dx22 * dy11, 2);

                                    if (dx1y2 == dx2y1 && dx11y22 == dx22y11)
                                    {
                                        double length = Math.Round(Math.Sqrt(dx11 * dx11 + dy11 * dy11) + Math.Sqrt(dx2 * dx2 + dy2 * dy2), 2);
                                        if (length <= Math.Round(listCurve1[0][index].length + listCurve1[0][j].length, 2))
                                        {
                                            CurveInfo curveInfoLine3 = new CurveInfo();
                                            if (Math.Round( listCurve1[0][index].startMathPoint[0],3) < Math.Round(listCurve1[0][j].startMathPoint[0],3))
                                            {
                                                curveInfoLine3.startMathPoint[0] = listCurve1[0][index].startMathPoint[0];
                                                curveInfoLine3.startMathPoint[1] = listCurve1[0][index].startMathPoint[1];

                                             }
                                            else if (Math.Round(listCurve1[0][index].startMathPoint[0],3) > Math.Round(listCurve1[0][j].startMathPoint[0],3))
                                            {
                                                curveInfoLine3.startMathPoint[0] = listCurve1[0][j].startMathPoint[0];
                                                curveInfoLine3.startMathPoint[1] = listCurve1[0][j].startMathPoint[1];
                                            }
                                            else
                                            {
                                                if (Math.Round(listCurve1[0][index].startMathPoint[1],3) < Math.Round(listCurve1[0][j].startMathPoint[1],3))
                                                {
                                                    curveInfoLine3.startMathPoint[0] = listCurve1[0][index].startMathPoint[0];
                                                    curveInfoLine3.startMathPoint[1] = listCurve1[0][index].startMathPoint[1];
                                                 }
                                                else
                                                {
                                                    curveInfoLine3.startMathPoint[0] = listCurve1[0][j].startMathPoint[0];
                                                    curveInfoLine3.startMathPoint[1] = listCurve1[0][j].startMathPoint[1];
                                                 }
                                            }

                                            if (Math.Round(listCurve1[0][index].endMathPoint[0],3) > Math.Round(listCurve1[0][j].endMathPoint[0],3))
                                            {
                                                curveInfoLine3.endMathPoint[0] = listCurve1[0][index].endMathPoint[0];
                                                            curveInfoLine3.endMathPoint[1] = listCurve1[0][index].endMathPoint[1];
                                                        }
                                            else if (Math.Round(listCurve1[0][index].endMathPoint[0],3) < Math.Round(listCurve1[0][j].endMathPoint[0],3))
                                            {
                                                curveInfoLine3.endMathPoint[0] = listCurve1[0][j].endMathPoint[0];
                                                            curveInfoLine3.endMathPoint[1] = listCurve1[0][j].endMathPoint[1];
                                                        }
                                            else
                                            {
                                                if (Math.Round(listCurve1[0][index].endMathPoint[1],3) > Math.Round(listCurve1[0][j].endMathPoint[1],3))
                                                {
                                                    curveInfoLine3.endMathPoint[0] = listCurve1[0][index].endMathPoint[0];
                                                                curveInfoLine3.endMathPoint[1] = listCurve1[0][index].endMathPoint[1];
                                                            }
                                                else
                                                {
                                                    curveInfoLine3.endMathPoint[0] = listCurve1[0][j].endMathPoint[0];
                                                    curveInfoLine3.endMathPoint[1] = listCurve1[0][j].endMathPoint[1];
                                                }
                                            }
                                            curveInfoLine3.length = Math.Sqrt((curveInfoLine3.startMathPoint[0] - curveInfoLine3.endMathPoint[0]) * (curveInfoLine3.startMathPoint[0] - curveInfoLine3.endMathPoint[0]) +
                                                (curveInfoLine3.startMathPoint[1] - curveInfoLine3.endMathPoint[1]) * (curveInfoLine3.startMathPoint[1] - curveInfoLine3.endMathPoint[1]));

                                            listCurve1[0].Remove(listCurve1[0][index]);
                                            listCurve1[0].Remove(listCurve1[0][j - 1]);
                                            listCurve1[0].Add(curveInfoLine3);
                                            mid += 1;
                                            break;
                                        }
                                    }
                                }
                                if (mid == 0)
                                {
                                    index += 1;
                                }
                                mid = 0;
                            }

                                        //去除宽度线条
                                        List<CurveInfo> listCurve2 = new List<CurveInfo>(listCurve1[0].ToArray());
                            foreach (CurveInfo item1 in listCurve2)
                            {
                                if (Math.Round(item1.length, 3) <= hou) listCurve1[0].Remove(item1);
                            }

                            CurveInfo curveInfosta = new CurveInfo();
                            //得到起始线条
                            for (int j = 0; j < listCurve1[0].Count; j++)
                            {
                                int cout = 0;
                                for (int m = 0; m < listCurve1[1].Count; m++)
                                {
                                    double dx1 = listCurve1[1][m].startMathPoint[0] - listCurve1[0][j].startMathPoint[0];
                                    double dy1 = listCurve1[1][m].startMathPoint[1] - listCurve1[0][j].startMathPoint[1];
                                    double dx2 = listCurve1[1][m].startMathPoint[0] - listCurve1[0][j].endMathPoint[0];
                                    double dy2 = listCurve1[1][m].startMathPoint[1] - listCurve1[0][j].endMathPoint[1];

                                    double dx11 = listCurve1[1][m].endMathPoint[0] - listCurve1[0][j].startMathPoint[0];
                                    double dy11 = listCurve1[1][m].endMathPoint[1] - listCurve1[0][j].startMathPoint[1];
                                    double dx22 = listCurve1[1][m].endMathPoint[0] - listCurve1[0][j].endMathPoint[0];
                                    double dy22 = listCurve1[1][m].endMathPoint[1] - listCurve1[0][j].endMathPoint[1];

                                    double dx1y2 = Math.Round(dx1 * dy2, 2);
                                    double dy1x2 = Math.Round(dy1 * dx2, 2);

                                    double dx11y22 = Math.Round(dx11 * dy22, 2);
                                    double dy11x22 = Math.Round(dy11 * dx22, 2);

                                    double length1 = Math.Sqrt(dx1 * dx1 + dy1 * dy1) + Math.Sqrt(dx2 * dx2 + dy2 * dy2);
                                    double length2 = Math.Sqrt(dx11 * dx11 + dy11 * dy11) + Math.Sqrt(dx22 * dx22 + dy22 * dy22);

                                    if (dx1y2 == dy1x2 && Math.Round(length1, 2) == Math.Round(listCurve1[0][j].length, 2))
                                    {
                                        cout += 1;
                                    }
                                    else if (dy11x22 == dx11y22 && Math.Round(length2, 2) == Math.Round(listCurve1[0][j].length, 2))
                                    {
                                        cout += 1;
                                    }
                                }
                                if (cout == 1)
                                {
                                    curveInfosta.startMathPoint = listCurve1[0][j].startMathPoint;
                                    curveInfosta.endMathPoint = listCurve1[0][j].endMathPoint;
                                    curveInfosta.length = listCurve1[0][j].length;
                                    break;
                                }
                            }

                            listCurve1[0].Remove(curveInfosta);
                            CurveInfo curveInfosta1 = new CurveInfo() { startMathPoint = curveInfosta.startMathPoint, endMathPoint = curveInfosta.endMathPoint, length = curveInfosta.length };
                            listCurveMid.Add(curveInfosta);
                            int kk = 0;

                            //得到按序排列的线段
                            do
                            {
                                kk = 0;
                                if (curveInfosta.r == 0)
                                {
                                    for (int m = 0; m < listCurve1[1].Count; m++)
                                    {
                                        double dx1 = listCurve1[1][m].startMathPoint[0] - curveInfosta.startMathPoint[0];
                                        double dy1 = listCurve1[1][m].startMathPoint[1] - curveInfosta.startMathPoint[1];
                                        double dx2 = listCurve1[1][m].startMathPoint[0] - curveInfosta.endMathPoint[0];
                                        double dy2 = listCurve1[1][m].startMathPoint[1] - curveInfosta.endMathPoint[1];

                                        double dx11 = listCurve1[1][m].endMathPoint[0] - curveInfosta.startMathPoint[0];
                                        double dy11 = listCurve1[1][m].endMathPoint[1] - curveInfosta.startMathPoint[1];
                                        double dx22 = listCurve1[1][m].endMathPoint[0] - curveInfosta.endMathPoint[0];
                                        double dy22 = listCurve1[1][m].endMathPoint[1] - curveInfosta.endMathPoint[1];

                                        double dx1y2 = Math.Round(dx1 * dy2, 2);
                                        double dy1x2 = Math.Round(dy1 * dx2, 2);

                                        double dx11y22 = Math.Round(dx11 * dy22, 2);
                                        double dy11x22 = Math.Round(dy11 * dx22, 2);

                                        double length1 = Math.Sqrt(dx1 * dx1 + dy1 * dy1) + Math.Sqrt(dx2 * dx2 + dy2 * dy2);
                                        double length2 = Math.Sqrt(dx11 * dx11 + dy11 * dy11) + Math.Sqrt(dx22 * dx22 + dy22 * dy22);

                                        if ((dy11x22 == dx11y22 && Math.Round(length2, 2) == Math.Round(curveInfosta.length, 2)) ||
                                            (dy1x2 == dx1y2 && Math.Round(length1, 2) == Math.Round(curveInfosta.length, 2)))
                                        {
                                            curveInfosta = new CurveInfo()
                                            {
                                                startMathPoint = listCurve1[1][m].startMathPoint,
                                                endMathPoint = listCurve1[1][m].endMathPoint,
                                                centerpoint = listCurve1[1][m].centerpoint,
                                                length = listCurve1[1][m].length,
                                                r = listCurve1[1][m].r
                                            };
                                            CurveInfo curveInfosta2 = new CurveInfo()
                                            {
                                                startMathPoint = listCurve1[1][m].startMathPoint,
                                                endMathPoint = listCurve1[1][m].endMathPoint,
                                                centerpoint = listCurve1[1][m].centerpoint,
                                                length = listCurve1[1][m].length,
                                                r = listCurve1[1][m].r
                                            };
                                            listCurveMid.Add(curveInfosta2);
                                            kk += 1;
                                            break;
                                        }
                                    }
                                    if (listCurve1[1].Contains(curveInfosta)) listCurve1[1].Remove(curveInfosta);
                                }
                                else
                                {
                                    for (int m = 0; m < listCurve1[0].Count; m++)
                                    {
                                        double dx1 = curveInfosta.startMathPoint[0] - listCurve1[0][m].startMathPoint[0];
                                        double dy1 = curveInfosta.startMathPoint[1] - listCurve1[0][m].startMathPoint[1];
                                        double dx2 = curveInfosta.startMathPoint[0] - listCurve1[0][m].endMathPoint[0];
                                        double dy2 = curveInfosta.startMathPoint[1] - listCurve1[0][m].endMathPoint[1];

                                        double dx11 = curveInfosta.endMathPoint[0] - listCurve1[0][m].startMathPoint[0];
                                        double dy11 = curveInfosta.endMathPoint[1] - listCurve1[0][m].startMathPoint[1];
                                        double dx22 = curveInfosta.endMathPoint[0] - listCurve1[0][m].endMathPoint[0];
                                        double dy22 = curveInfosta.endMathPoint[1] - listCurve1[0][m].endMathPoint[1];

                                        double dx1y2 = Math.Round(dx1 * dy2, 2);
                                        double dy1x2 = Math.Round(dy1 * dx2, 2);

                                        double dx11y22 = Math.Round(dx11 * dy22, 2);
                                        double dy11x22 = Math.Round(dy11 * dx22, 2);

                                        double length1 = Math.Sqrt(dx1 * dx1 + dy1 * dy1) + Math.Sqrt(dx2 * dx2 + dy2 * dy2);
                                        double length2 = Math.Sqrt(dx11 * dx11 + dy11 * dy11) + Math.Sqrt(dx22 * dx22 + dy22 * dy22);

                                        if ((dy11x22 == dx11y22 && Math.Round(length2, 2) == Math.Round(listCurve1[0][m].length, 2))
                                            || (dy1x2 == dx1y2 && Math.Round(length1, 2) == Math.Round(listCurve1[0][m].length, 2)))
                                        {
                                            curveInfosta = new CurveInfo()
                                            {
                                                startMathPoint = listCurve1[0][m].startMathPoint,
                                                endMathPoint = listCurve1[0][m].endMathPoint,
                                                length = listCurve1[0][m].length
                                            };
                                            CurveInfo curveInfosta2 = new CurveInfo()
                                            {
                                                startMathPoint = listCurve1[0][m].startMathPoint,
                                                endMathPoint = listCurve1[0][m].endMathPoint,
                                                length = listCurve1[0][m].length
                                            };
                                            listCurveMid.Add(curveInfosta2);
                                            kk += 1;
                                            break;
                                        }
                                    }
                                    if (listCurve1[0].Contains(curveInfosta)) listCurve1[0].Remove(curveInfosta);
                                }
                            } while (kk != 0);
                            kk = 1;
                            while (kk < listCurveMid.Count - 1)
                            {
                                if (listCurveMid[kk].r != 0)
                                {
                                    //第一段直线
                                    double dx1 = listCurveMid[kk].startMathPoint[0] - listCurveMid[kk - 1].startMathPoint[0];
                                    double dy1 = listCurveMid[kk].startMathPoint[1] - listCurveMid[kk - 1].startMathPoint[1];
                                    double dx2 = listCurveMid[kk].startMathPoint[0] - listCurveMid[kk - 1].endMathPoint[0];
                                    double dy2 = listCurveMid[kk].startMathPoint[1] - listCurveMid[kk - 1].endMathPoint[1];

                                    double dx11 = listCurveMid[kk].endMathPoint[0] - listCurveMid[kk - 1].startMathPoint[0];
                                    double dy11 = listCurveMid[kk].endMathPoint[1] - listCurveMid[kk - 1].startMathPoint[1];
                                    double dx22 = listCurveMid[kk].endMathPoint[0] - listCurveMid[kk - 1].endMathPoint[0];
                                    double dy22 = listCurveMid[kk].endMathPoint[1] - listCurveMid[kk - 1].endMathPoint[1];

                                    double dx1y2 = Math.Round(dx1 * dy2, 2);
                                    double dy1x2 = Math.Round(dy1 * dx2, 2);

                                    double dx11y22 = Math.Round(dx11 * dy22, 2);
                                    double dy11x22 = Math.Round(dy11 * dx22, 2);

                                    double dx1incre = 0;
                                    double dy1incre = 0;

                                    double intersectionx = 0;
                                    double intersectiony = 0;

                                    if (dx1y2 == dy1x2)
                                    {
                                        if ((Math.Round(dx1, 2) != 0 || Math.Round(dy1, 2) != 0) && (Math.Round(dx2, 2) != 0 || Math.Round(dy2, 2) != 0))
                                        {
                                            double length1 = Math.Sqrt(dx1 * dx1 + dy1 * dy1);
                                            double length2 = Math.Sqrt(dx2 * dx2 + dy2 * dy2);
                                            if (length1 > length2)
                                            {
                                                listCurveMid[kk - 1].endMathPoint = new double[] { listCurveMid[kk].startMathPoint[0], listCurveMid[kk].startMathPoint[1] };

                                                double length = 0;
                                                double r = listCurveMid[kk].r;
                                                if (listCurveMid[kk].r > hou) r = listCurveMid[kk].r - hou;

                                                if (Math.Round((listCurveMid[kk].length / listCurveMid[kk].r), 2) >= Math.Round(Math.PI / 2, 2))//圆弧增量长度
                                                {
                                                    length = r + hou;
                                                }
                                                else
                                                {
                                                    length = (r + hou) * Math.Sin((listCurveMid[kk].length / listCurveMid[kk].r));//圆弧增量长度
                                                }

                                                dx1incre = dx1 * length / length1;
                                                dy1incre = dy1 * length / length1;
                                                listCurveMid[kk - 1].endMathPoint[0] = listCurveMid[kk - 1].endMathPoint[0] + dx1incre;
                                                listCurveMid[kk - 1].endMathPoint[1] = listCurveMid[kk - 1].endMathPoint[1] + dy1incre;
                                                listCurveMid[kk - 1].length = Math.Sqrt((listCurveMid[kk - 1].endMathPoint[0] - listCurveMid[kk - 1].startMathPoint[0]) * (listCurveMid[kk - 1].endMathPoint[0] - listCurveMid[kk - 1].startMathPoint[0]) +
                                                    (listCurveMid[kk - 1].endMathPoint[1] - listCurveMid[kk - 1].startMathPoint[1]) * (listCurveMid[kk - 1].endMathPoint[1] - listCurveMid[kk - 1].startMathPoint[1]));
                                                intersectionx = listCurveMid[kk - 1].endMathPoint[0];
                                                intersectiony = listCurveMid[kk - 1].endMathPoint[1];
                                            }
                                            else
                                            {
                                                listCurveMid[kk - 1].startMathPoint = new double[] { listCurveMid[kk].startMathPoint[0], listCurveMid[kk].startMathPoint[1] };

                                                double length = 0;
                                                double r = listCurveMid[kk].r;
                                                if (listCurveMid[kk].r > hou) r = listCurveMid[kk].r - hou;

                                                if (Math.Round((listCurveMid[kk].length / listCurveMid[kk].r), 2) >= Math.Round(Math.PI / 2, 2))//圆弧增量长度
                                                {
                                                    length = r + hou;
                                                }
                                                else
                                                {
                                                    length = (r + hou) * Math.Sin((listCurveMid[kk].length / listCurveMid[kk].r));//圆弧增量长度
                                                }

                                                dx1incre = dx2 * length / length2;
                                                dy1incre = dy2 * length / length2;
                                                listCurveMid[kk - 1].startMathPoint[0] = listCurveMid[kk - 1].startMathPoint[0] + dx1incre;
                                                listCurveMid[kk - 1].startMathPoint[1] = listCurveMid[kk - 1].startMathPoint[1] + dy1incre;
                                                listCurveMid[kk - 1].length = Math.Sqrt((listCurveMid[kk - 1].endMathPoint[0] - listCurveMid[kk - 1].startMathPoint[0]) * (listCurveMid[kk - 1].endMathPoint[0] - listCurveMid[kk - 1].startMathPoint[0]) +
                                                    (listCurveMid[kk - 1].endMathPoint[1] - listCurveMid[kk - 1].startMathPoint[1]) * (listCurveMid[kk - 1].endMathPoint[1] - listCurveMid[kk - 1].startMathPoint[1]));
                                                intersectionx = listCurveMid[kk - 1].startMathPoint[0];
                                                intersectiony = listCurveMid[kk - 1].startMathPoint[1];
                                            }
                                        }
                                        else
                                        {
                                            double length1 = Math.Sqrt(dx1 * dx1 + dy1 * dy1);
                                            double length2 = Math.Sqrt(dx2 * dx2 + dy2 * dy2);
                                            if (Math.Round(length1, 2) == 0)
                                            {
                                                double length = 0;
                                                double r = listCurveMid[kk].r;
                                                if (listCurveMid[kk].r > hou) r = listCurveMid[kk].r - hou;

                                                if (Math.Round((listCurveMid[kk].length / listCurveMid[kk].r), 2) >= Math.Round(Math.PI / 2, 2))//圆弧增量长度
                                                {
                                                    length = r + hou;
                                                }
                                                else
                                                {
                                                    length = (r + hou) * Math.Sin((listCurveMid[kk].length / listCurveMid[kk].r));//圆弧增量长度
                                                }

                                                dx1incre = dx2 * length / length2;
                                                dy1incre = dy2 * length / length2;
                                                listCurveMid[kk - 1].startMathPoint[0] = listCurveMid[kk - 1].startMathPoint[0] + dx1incre;
                                                listCurveMid[kk - 1].startMathPoint[1] = listCurveMid[kk - 1].startMathPoint[1] + dy1incre;
                                                listCurveMid[kk - 1].length = Math.Sqrt((listCurveMid[kk - 1].endMathPoint[0] - listCurveMid[kk - 1].startMathPoint[0]) * (listCurveMid[kk - 1].endMathPoint[0] - listCurveMid[kk - 1].startMathPoint[0]) +
                                                    (listCurveMid[kk - 1].endMathPoint[1] - listCurveMid[kk - 1].startMathPoint[1]) * (listCurveMid[kk - 1].endMathPoint[1] - listCurveMid[kk - 1].startMathPoint[1]));
                                                intersectionx = listCurveMid[kk - 1].startMathPoint[0];
                                                intersectiony = listCurveMid[kk - 1].startMathPoint[1];
                                            }
                                            else
                                            {
                                                double length = 0;
                                                double r = listCurveMid[kk].r;
                                                if (listCurveMid[kk].r > hou) r = listCurveMid[kk].r - hou;

                                                if (Math.Round((listCurveMid[kk].length / listCurveMid[kk].r), 2) >= Math.Round(Math.PI / 2, 2))//圆弧增量长度
                                                {
                                                    length = r + hou;
                                                }
                                                else
                                                {
                                                    length = (r + hou) * Math.Sin((listCurveMid[kk].length / listCurveMid[kk].r));//圆弧增量长度
                                                }

                                                dx1incre = dx1 * length / length1;
                                                dy1incre = dy1 * length / length1;
                                                listCurveMid[kk - 1].endMathPoint[0] = listCurveMid[kk - 1].endMathPoint[0] + dx1incre;
                                                listCurveMid[kk - 1].endMathPoint[1] = listCurveMid[kk - 1].endMathPoint[1] + dy1incre;
                                                listCurveMid[kk - 1].length = Math.Sqrt((listCurveMid[kk - 1].endMathPoint[0] - listCurveMid[kk - 1].startMathPoint[0]) * (listCurveMid[kk - 1].endMathPoint[0] - listCurveMid[kk - 1].startMathPoint[0]) +
                                                    (listCurveMid[kk - 1].endMathPoint[1] - listCurveMid[kk - 1].startMathPoint[1]) * (listCurveMid[kk - 1].endMathPoint[1] - listCurveMid[kk - 1].startMathPoint[1]));
                                                intersectionx = listCurveMid[kk - 1].endMathPoint[0];
                                                intersectiony = listCurveMid[kk - 1].endMathPoint[1];
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if ((Math.Round(dx11, 2) != 0 || Math.Round(dy11, 2) != 0) && (Math.Round(dx22, 2) != 0 || Math.Round(dy22, 2) != 0))
                                        {
                                            double length1 = Math.Sqrt(dx11 * dx11 + dy11 * dy11);
                                            double length2 = Math.Sqrt(dx22 * dx22 + dy22 * dy22);
                                            if (length1 > length2)
                                            {
                                                listCurveMid[kk - 1].endMathPoint = new double[] { listCurveMid[kk].endMathPoint[0], listCurveMid[kk].endMathPoint[1] };

                                                double length = 0;
                                                double r = listCurveMid[kk].r;
                                                if (listCurveMid[kk].r > hou) r = listCurveMid[kk].r - hou;

                                                if (Math.Round((listCurveMid[kk].length / listCurveMid[kk].r), 2) >= Math.Round(Math.PI / 2, 2))//圆弧增量长度
                                                {
                                                    length = r + hou;
                                                }
                                                else
                                                {
                                                    length = (r + hou) * Math.Sin((listCurveMid[kk].length / listCurveMid[kk].r));//圆弧增量长度
                                                }

                                                dx1incre = dx11 * length / length1;
                                                dy1incre = dy11 * length / length1;
                                                listCurveMid[kk - 1].endMathPoint[0] = listCurveMid[kk - 1].endMathPoint[0] + dx1incre;
                                                listCurveMid[kk - 1].endMathPoint[1] = listCurveMid[kk - 1].endMathPoint[1] + dy1incre;
                                                listCurveMid[kk - 1].length = Math.Sqrt((listCurveMid[kk - 1].endMathPoint[0] - listCurveMid[kk - 1].startMathPoint[0]) * (listCurveMid[kk - 1].endMathPoint[0] - listCurveMid[kk - 1].startMathPoint[0]) +
                                                    (listCurveMid[kk - 1].endMathPoint[1] - listCurveMid[kk - 1].startMathPoint[1]) * (listCurveMid[kk - 1].endMathPoint[1] - listCurveMid[kk - 1].startMathPoint[1]));
                                                intersectionx = listCurveMid[kk - 1].endMathPoint[0];
                                                intersectiony = listCurveMid[kk - 1].endMathPoint[1];
                                            }
                                            else
                                            {
                                                listCurveMid[kk - 1].startMathPoint = new double[] { listCurveMid[kk].endMathPoint[0], listCurveMid[kk].endMathPoint[1] };

                                                double length = 0;
                                                double r = listCurveMid[kk].r;
                                                if (listCurveMid[kk].r > hou) r = listCurveMid[kk].r - hou;

                                                if (Math.Round((listCurveMid[kk].length / listCurveMid[kk].r), 2) >= Math.Round(Math.PI / 2, 2))//圆弧增量长度
                                                {
                                                    length = r + hou;
                                                }
                                                else
                                                {
                                                    length = (r + hou) * Math.Sin((listCurveMid[kk].length / listCurveMid[kk].r));//圆弧增量长度
                                                }

                                                dx1incre = dx22 * length / length2;
                                                dy1incre = dy22 * length / length2;
                                                listCurveMid[kk - 1].startMathPoint[0] = listCurveMid[kk - 1].startMathPoint[0] + dx1incre;
                                                listCurveMid[kk - 1].startMathPoint[1] = listCurveMid[kk - 1].startMathPoint[1] + dy1incre;
                                                listCurveMid[kk - 1].length = Math.Sqrt((listCurveMid[kk - 1].endMathPoint[0] - listCurveMid[kk - 1].startMathPoint[0]) * (listCurveMid[kk - 1].endMathPoint[0] - listCurveMid[kk - 1].startMathPoint[0]) +
                                                    (listCurveMid[kk - 1].endMathPoint[1] - listCurveMid[kk - 1].startMathPoint[1]) * (listCurveMid[kk - 1].endMathPoint[1] - listCurveMid[kk - 1].startMathPoint[1]));
                                                intersectionx = listCurveMid[kk - 1].startMathPoint[0];
                                                intersectiony = listCurveMid[kk - 1].startMathPoint[1];
                                            }
                                        }
                                        else
                                        {
                                            double length1 = Math.Sqrt(dx11 * dx11 + dy11 * dy11);
                                            double length2 = Math.Sqrt(dx22 * dx22 + dy22 * dy22);
                                            if (Math.Round(length1, 2) == 0)
                                            {
                                                double length = 0;
                                                double r = listCurveMid[kk].r;
                                                if (listCurveMid[kk].r > hou) r = listCurveMid[kk].r - hou;

                                                if (Math.Round((listCurveMid[kk].length / listCurveMid[kk].r), 2) >= Math.Round(Math.PI / 2, 2))//圆弧增量长度
                                                {
                                                    length = r + hou;
                                                }
                                                else
                                                {
                                                    length = (r + hou) * Math.Sin((listCurveMid[kk].length / listCurveMid[kk].r));//圆弧增量长度
                                                }

                                                dx1incre = dx22 * length / length2;
                                                dy1incre = dy22 * length / length2;
                                                listCurveMid[kk - 1].startMathPoint[0] = listCurveMid[kk - 1].startMathPoint[0] + dx1incre;
                                                listCurveMid[kk - 1].startMathPoint[1] = listCurveMid[kk - 1].startMathPoint[1] + dy1incre;
                                                listCurveMid[kk - 1].length = Math.Sqrt((listCurveMid[kk - 1].endMathPoint[0] - listCurveMid[kk - 1].startMathPoint[0]) * (listCurveMid[kk - 1].endMathPoint[0] - listCurveMid[kk - 1].startMathPoint[0]) +
                                                    (listCurveMid[kk - 1].endMathPoint[1] - listCurveMid[kk - 1].startMathPoint[1]) * (listCurveMid[kk - 1].endMathPoint[1] - listCurveMid[kk - 1].startMathPoint[1]));
                                                intersectionx = listCurveMid[kk - 1].startMathPoint[0];
                                                intersectiony = listCurveMid[kk - 1].startMathPoint[1];
                                            }
                                            else
                                            {
                                                double length = 0;
                                                double r = listCurveMid[kk].r;
                                                if (listCurveMid[kk].r > hou) r = listCurveMid[kk].r - hou;

                                                if (Math.Round((listCurveMid[kk].length / listCurveMid[kk].r), 2) >= Math.Round(Math.PI / 2, 2))//圆弧增量长度
                                                {
                                                    length = r + hou;
                                                }
                                                else
                                                {
                                                    length = (r + hou) * Math.Sin((listCurveMid[kk].length / listCurveMid[kk].r));//圆弧增量长度
                                                }

                                                dx1incre = dx11 * length / length1;
                                                dy1incre = dy11 * length / length1;
                                                listCurveMid[kk - 1].endMathPoint[0] = listCurveMid[kk - 1].endMathPoint[0] + dx1incre;
                                                listCurveMid[kk - 1].endMathPoint[1] = listCurveMid[kk - 1].endMathPoint[1] + dy1incre;
                                                listCurveMid[kk - 1].length = Math.Sqrt((listCurveMid[kk - 1].endMathPoint[0] - listCurveMid[kk - 1].startMathPoint[0]) * (listCurveMid[kk - 1].endMathPoint[0] - listCurveMid[kk - 1].startMathPoint[0]) +
                                                    (listCurveMid[kk - 1].endMathPoint[1] - listCurveMid[kk - 1].startMathPoint[1]) * (listCurveMid[kk - 1].endMathPoint[1] - listCurveMid[kk - 1].startMathPoint[1]));
                                                intersectionx = listCurveMid[kk - 1].endMathPoint[0];
                                                intersectiony = listCurveMid[kk - 1].endMathPoint[1];
                                            }
                                        }
                                    }

                                    //第二点直线
                                    dx1 = listCurveMid[kk].startMathPoint[0] - listCurveMid[kk + 1].startMathPoint[0];
                                    dy1 = listCurveMid[kk].startMathPoint[1] - listCurveMid[kk + 1].startMathPoint[1];
                                    dx2 = listCurveMid[kk].startMathPoint[0] - listCurveMid[kk + 1].endMathPoint[0];
                                    dy2 = listCurveMid[kk].startMathPoint[1] - listCurveMid[kk + 1].endMathPoint[1];

                                    dx11 = listCurveMid[kk].endMathPoint[0] - listCurveMid[kk + 1].startMathPoint[0];
                                    dy11 = listCurveMid[kk].endMathPoint[1] - listCurveMid[kk + 1].startMathPoint[1];
                                    dx22 = listCurveMid[kk].endMathPoint[0] - listCurveMid[kk + 1].endMathPoint[0];
                                    dy22 = listCurveMid[kk].endMathPoint[1] - listCurveMid[kk + 1].endMathPoint[1];

                                    dx1y2 = Math.Round(dx1 * dy2, 2);
                                    dy1x2 = Math.Round(dy1 * dx2, 2);

                                    dx11y22 = Math.Round(dx11 * dy22, 2);
                                    dy11x22 = Math.Round(dy11 * dx22, 2);

                                    double increintersectionx = 0;
                                    double increintersectiony = 0;

                                    if (dx1y2 == dy1x2)
                                    {
                                        if ((Math.Round(dx1, 2) != 0 || Math.Round(dy1, 2) != 0) && (Math.Round(dx2, 2) != 0 || Math.Round(dy2, 2) != 0))
                                        {
                                            double length1 = Math.Sqrt(dx1 * dx1 + dy1 * dy1);
                                            double length2 = Math.Sqrt(dx2 * dx2 + dy2 * dy2);
                                            if (length1 > length2)
                                            {
                                                listCurveMid[kk + 1].endMathPoint = new double[] { listCurveMid[kk].startMathPoint[0], listCurveMid[kk].startMathPoint[1] };

                                                double length = 0;
                                                double r = listCurveMid[kk].r;
                                                if (listCurveMid[kk].r > hou) r = listCurveMid[kk].r - hou;

                                                if (Math.Round((listCurveMid[kk].length / listCurveMid[kk].r), 2) >= Math.Round(Math.PI / 2, 2))//圆弧增量长度
                                                {
                                                    length = r + hou;
                                                }
                                                else
                                                {
                                                    length = (r + hou) * Math.Sin((listCurveMid[kk].length / listCurveMid[kk].r));//圆弧增量长度
                                                }

                                                dx1incre = dx1 * length / length1;
                                                dy1incre = dy1 * length / length1;
                                                listCurveMid[kk + 1].endMathPoint[0] = listCurveMid[kk + 1].endMathPoint[0] + dx1incre;
                                                listCurveMid[kk + 1].endMathPoint[1] = listCurveMid[kk + 1].endMathPoint[1] + dy1incre;
                                                listCurveMid[kk + 1].length = Math.Sqrt((listCurveMid[kk + 1].endMathPoint[0] - listCurveMid[kk + 1].startMathPoint[0]) * (listCurveMid[kk + 1].endMathPoint[0] - listCurveMid[kk + 1].startMathPoint[0]) +
                                                    (listCurveMid[kk + 1].endMathPoint[1] - listCurveMid[kk + 1].startMathPoint[1]) * (listCurveMid[kk + 1].endMathPoint[1] - listCurveMid[kk + 1].startMathPoint[1]));
                                                increintersectionx = listCurveMid[kk + 1].endMathPoint[0];
                                                increintersectiony = listCurveMid[kk + 1].endMathPoint[1];
                                            }
                                            else
                                            {
                                                listCurveMid[kk + 1].startMathPoint = new double[] { listCurveMid[kk].startMathPoint[0], listCurveMid[kk].startMathPoint[1] };

                                                double length = 0;
                                                double r = listCurveMid[kk].r;
                                                if (listCurveMid[kk].r > hou) r = listCurveMid[kk].r - hou;

                                                if (Math.Round((listCurveMid[kk].length / listCurveMid[kk].r), 2) >= Math.Round(Math.PI / 2, 2))//圆弧增量长度
                                                {
                                                    length = r + hou;
                                                }
                                                else
                                                {
                                                    length = (r + hou) * Math.Sin((listCurveMid[kk].length / listCurveMid[kk].r));//圆弧增量长度
                                                }

                                                dx1incre = dx2 * length / length2;
                                                dy1incre = dy2 * length / length2;
                                                listCurveMid[kk + 1].startMathPoint[0] = listCurveMid[kk + 1].startMathPoint[0] + dx1incre;
                                                listCurveMid[kk + 1].startMathPoint[1] = listCurveMid[kk + 1].startMathPoint[1] + dy1incre;
                                                listCurveMid[kk + 1].length = Math.Sqrt((listCurveMid[kk + 1].endMathPoint[0] - listCurveMid[kk + 1].startMathPoint[0]) * (listCurveMid[kk + 1].endMathPoint[0] - listCurveMid[kk + 1].startMathPoint[0]) +
                                                    (listCurveMid[kk + 1].endMathPoint[1] - listCurveMid[kk + 1].startMathPoint[1]) * (listCurveMid[kk + 1].endMathPoint[1] - listCurveMid[kk + 1].startMathPoint[1]));
                                                increintersectionx = listCurveMid[kk + 1].startMathPoint[0];
                                                increintersectiony = listCurveMid[kk + 1].startMathPoint[1];
                                            }
                                        }
                                        else
                                        {
                                            double length1 = Math.Sqrt(dx1 * dx1 + dy1 * dy1);
                                            double length2 = Math.Sqrt(dx2 * dx2 + dy2 * dy2);
                                            if (Math.Round(length1, 2) == 0)
                                            {
                                                double length = 0;
                                                double r = listCurveMid[kk].r;
                                                if (listCurveMid[kk].r > hou) r = listCurveMid[kk].r - hou;

                                                if (Math.Round((listCurveMid[kk].length / listCurveMid[kk].r), 2) >= Math.Round(Math.PI / 2, 2))//圆弧增量长度
                                                {
                                                    length = r + hou;
                                                }
                                                else
                                                {
                                                    length = (r + hou) * Math.Sin((listCurveMid[kk].length / listCurveMid[kk].r));//圆弧增量长度
                                                }

                                                dx1incre = dx2 * length / length2;
                                                dy1incre = dy2 * length / length2;
                                                listCurveMid[kk + 1].startMathPoint[0] = listCurveMid[kk + 1].startMathPoint[0] + dx1incre;
                                                listCurveMid[kk + 1].startMathPoint[1] = listCurveMid[kk + 1].startMathPoint[1] + dy1incre;
                                                listCurveMid[kk + 1].length = Math.Sqrt((listCurveMid[kk + 1].endMathPoint[0] - listCurveMid[kk + 1].startMathPoint[0]) * (listCurveMid[kk + 1].endMathPoint[0] - listCurveMid[kk + 1].startMathPoint[0]) +
                                                    (listCurveMid[kk + 1].endMathPoint[1] - listCurveMid[kk + 1].startMathPoint[1]) * (listCurveMid[kk + 1].endMathPoint[1] - listCurveMid[kk + 1].startMathPoint[1]));
                                                increintersectionx = listCurveMid[kk + 1].startMathPoint[0];
                                                increintersectiony = listCurveMid[kk + 1].startMathPoint[1];
                                            }
                                            else
                                            {
                                                double length = 0;
                                                double r = listCurveMid[kk].r;
                                                if (listCurveMid[kk].r > hou) r = listCurveMid[kk].r - hou;

                                                if (Math.Round((listCurveMid[kk].length / listCurveMid[kk].r), 2) >= Math.Round(Math.PI / 2, 2))//圆弧增量长度
                                                {
                                                    length = r + hou;
                                                }
                                                else
                                                {
                                                    length = (r + hou) * Math.Sin((listCurveMid[kk].length / listCurveMid[kk].r));//圆弧增量长度
                                                }

                                                dx1incre = dx1 * length / length1;
                                                dy1incre = dy1 * length / length1;
                                                listCurveMid[kk + 1].endMathPoint[0] = listCurveMid[kk + 1].endMathPoint[0] + dx1incre;
                                                listCurveMid[kk + 1].endMathPoint[1] = listCurveMid[kk + 1].endMathPoint[1] + dy1incre;
                                                listCurveMid[kk + 1].length = Math.Sqrt((listCurveMid[kk + 1].endMathPoint[0] - listCurveMid[kk + 1].startMathPoint[0]) * (listCurveMid[kk + 1].endMathPoint[0] - listCurveMid[kk + 1].startMathPoint[0]) +
                                                    (listCurveMid[kk + 1].endMathPoint[1] - listCurveMid[kk + 1].startMathPoint[1]) * (listCurveMid[kk + 1].endMathPoint[1] - listCurveMid[kk + 1].startMathPoint[1]));
                                                increintersectionx = listCurveMid[kk + 1].endMathPoint[0];
                                                increintersectiony = listCurveMid[kk + 1].endMathPoint[1];
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if ((Math.Round(dx11, 2) != 0 || Math.Round(dy11, 2) != 0) && (Math.Round(dx22, 2) != 0 || Math.Round(dy22, 2) != 0))
                                        {
                                            double length1 = Math.Sqrt(dx11 * dx11 + dy11 * dy11);
                                            double length2 = Math.Sqrt(dx22 * dx22 + dy22 * dy22);
                                            if (length1 > length2)
                                            {
                                                listCurveMid[kk + 1].endMathPoint = new double[] { listCurveMid[kk].endMathPoint[0], listCurveMid[kk].endMathPoint[1] };

                                                double length = 0;
                                                double r = listCurveMid[kk].r;
                                                if (listCurveMid[kk].r > hou) r = listCurveMid[kk].r - hou;

                                                if (Math.Round((listCurveMid[kk].length / listCurveMid[kk].r), 2) >= Math.Round(Math.PI / 2, 2))//圆弧增量长度
                                                {
                                                    length = r + hou;
                                                }
                                                else
                                                {
                                                    length = (r + hou) * Math.Sin((listCurveMid[kk].length / listCurveMid[kk].r));//圆弧增量长度
                                                }

                                                dx1incre = dx11 * length / length1;
                                                dy1incre = dy11 * length / length1;
                                                listCurveMid[kk + 1].endMathPoint[0] = listCurveMid[kk + 1].endMathPoint[0] + dx1incre;
                                                listCurveMid[kk + 1].endMathPoint[1] = listCurveMid[kk + 1].endMathPoint[1] + dy1incre;
                                                listCurveMid[kk + 1].length = Math.Sqrt((listCurveMid[kk + 1].endMathPoint[0] - listCurveMid[kk + 1].startMathPoint[0]) * (listCurveMid[kk + 1].endMathPoint[0] - listCurveMid[kk + 1].startMathPoint[0]) +
                                                    (listCurveMid[kk + 1].endMathPoint[1] - listCurveMid[kk + 1].startMathPoint[1]) * (listCurveMid[kk + 1].endMathPoint[1] - listCurveMid[kk + 1].startMathPoint[1]));
                                                increintersectionx = listCurveMid[kk + 1].endMathPoint[0];
                                                increintersectiony = listCurveMid[kk + 1].endMathPoint[1];
                                            }
                                            else
                                            {
                                                listCurveMid[kk + 1].startMathPoint = new double[] { listCurveMid[kk].endMathPoint[0], listCurveMid[kk].endMathPoint[1] };

                                                double length = 0;
                                                double r = listCurveMid[kk].r;
                                                if (listCurveMid[kk].r > hou) r = listCurveMid[kk].r - hou;

                                                if (Math.Round((listCurveMid[kk].length / listCurveMid[kk].r), 2) >= Math.Round(Math.PI / 2, 2))//圆弧增量长度
                                                {
                                                    length = r + hou;
                                                }
                                                else
                                                {
                                                    length = (r + hou) * Math.Sin((listCurveMid[kk].length / listCurveMid[kk].r));//圆弧增量长度
                                                }

                                                dx1incre = dx22 * length / length2;
                                                dy1incre = dy22 * length / length2;
                                                listCurveMid[kk + 1].startMathPoint[0] = listCurveMid[kk + 1].startMathPoint[0] + dx1incre;
                                                listCurveMid[kk + 1].startMathPoint[1] = listCurveMid[kk + 1].startMathPoint[1] + dy1incre;
                                                listCurveMid[kk + 1].length = Math.Sqrt((listCurveMid[kk + 1].endMathPoint[0] - listCurveMid[kk + 1].startMathPoint[0]) * (listCurveMid[kk + 1].endMathPoint[0] - listCurveMid[kk + 1].startMathPoint[0]) +
                                                    (listCurveMid[kk + 1].endMathPoint[1] - listCurveMid[kk + 1].startMathPoint[1]) * (listCurveMid[kk + 1].endMathPoint[1] - listCurveMid[kk + 1].startMathPoint[1]));
                                                increintersectionx = listCurveMid[kk + 1].startMathPoint[0];
                                                increintersectiony = listCurveMid[kk + 1].startMathPoint[1];
                                            }
                                        }
                                        else
                                        {
                                            double length1 = Math.Sqrt(dx11 * dx11 + dy11 * dy11);
                                            double length2 = Math.Sqrt(dx22 * dx22 + dy22 * dy22);
                                            if (Math.Round(length1, 2) == 0)
                                            {
                                                double length = 0;
                                                double r = listCurveMid[kk].r;
                                                if (listCurveMid[kk].r > hou) r = listCurveMid[kk].r - hou;

                                                if (Math.Round((listCurveMid[kk].length / listCurveMid[kk].r), 2) >= Math.Round(Math.PI / 2, 2))//圆弧增量长度
                                                {
                                                    length = r + hou;
                                                }
                                                else
                                                {
                                                    length = (r + hou) * Math.Sin((listCurveMid[kk].length / listCurveMid[kk].r));//圆弧增量长度
                                                }

                                                dx1incre = dx22 * length / length2;
                                                dy1incre = dy22 * length / length2;
                                                listCurveMid[kk + 1].startMathPoint[0] = listCurveMid[kk + 1].startMathPoint[0] + dx1incre;
                                                listCurveMid[kk + 1].startMathPoint[1] = listCurveMid[kk + 1].startMathPoint[1] + dy1incre;
                                                listCurveMid[kk + 1].length = Math.Sqrt((listCurveMid[kk + 1].endMathPoint[0] - listCurveMid[kk + 1].startMathPoint[0]) * (listCurveMid[kk + 1].endMathPoint[0] - listCurveMid[kk + 1].startMathPoint[0]) +
                                                    (listCurveMid[kk + 1].endMathPoint[1] - listCurveMid[kk + 1].startMathPoint[1]) * (listCurveMid[kk + 1].endMathPoint[1] - listCurveMid[kk + 1].startMathPoint[1]));
                                                increintersectionx = listCurveMid[kk + 1].startMathPoint[0];
                                                increintersectiony = listCurveMid[kk + 1].startMathPoint[1];
                                            }
                                            else
                                            {
                                                double length = 0;
                                                double r = listCurveMid[kk].r;
                                                if (listCurveMid[kk].r > hou) r = listCurveMid[kk].r - hou;

                                                if (Math.Round((listCurveMid[kk].length / listCurveMid[kk].r), 2) >= Math.Round(Math.PI / 2, 2))//圆弧增量长度
                                                {
                                                    length = r + hou;
                                                }
                                                else
                                                {
                                                    length = (r + hou) * Math.Sin((listCurveMid[kk].length / listCurveMid[kk].r));//圆弧增量长度
                                                }

                                                dx1incre = dx11 * length / length1;
                                                dy1incre = dy11 * length / length1;
                                                listCurveMid[kk + 1].endMathPoint[0] = listCurveMid[kk + 1].endMathPoint[0] + dx1incre;
                                                listCurveMid[kk + 1].endMathPoint[1] = listCurveMid[kk + 1].endMathPoint[1] + dy1incre;
                                                listCurveMid[kk + 1].length = Math.Sqrt((listCurveMid[kk + 1].endMathPoint[0] - listCurveMid[kk + 1].startMathPoint[0]) * (listCurveMid[kk + 1].endMathPoint[0] - listCurveMid[kk + 1].startMathPoint[0]) +
                                                    (listCurveMid[kk + 1].endMathPoint[1] - listCurveMid[kk + 1].startMathPoint[1]) * (listCurveMid[kk + 1].endMathPoint[1] - listCurveMid[kk + 1].startMathPoint[1]));
                                                increintersectionx = listCurveMid[kk + 1].endMathPoint[0];
                                                increintersectiony = listCurveMid[kk + 1].endMathPoint[1];
                                            }
                                        }
                                    }

                                    double verchangx = intersectionx - increintersectionx;
                                    double verchangy = intersectiony - increintersectiony;

                                    int j = kk + 1;
                                    while (j < listCurveMid.Count)
                                    {
                                        listCurveMid[j].startMathPoint[0] = listCurveMid[j].startMathPoint[0] + verchangx;
                                        listCurveMid[j].startMathPoint[1] = listCurveMid[j].startMathPoint[1] + verchangy;
                                        listCurveMid[j].endMathPoint[0] = listCurveMid[j].endMathPoint[0] + verchangx;
                                        listCurveMid[j].endMathPoint[1] = listCurveMid[j].endMathPoint[1] + verchangy;
                                        if (listCurveMid[j].r != 0)
                                        {
                                            listCurveMid[j].centerpoint[0] = listCurveMid[j].centerpoint[0] + verchangx;
                                            listCurveMid[j].centerpoint[1] = listCurveMid[j].centerpoint[1] + verchangy;
                                        }
                                        j += 1;
                                    }
                                }
                                kk += 1;
                            }

                            //根据不同线条集合组合方式旋转90度
                            if (i == 1 && tranformt)
                            {
                                for (int j = 0; j < listCurveMid.Count; j++)
                                {
                                    double x = listCurveMid[j].startMathPoint[0] * Math.Cos(Math.PI / 2) - listCurveMid[j].startMathPoint[1] * Math.Sin(Math.PI / 2);
                                    double y = listCurveMid[j].startMathPoint[0] * Math.Sin(Math.PI / 2) + listCurveMid[j].startMathPoint[1] * Math.Cos(Math.PI / 2);
                                    listCurveMid[j].startMathPoint[0] = x;
                                    listCurveMid[j].startMathPoint[1] = y;
                                    x = listCurveMid[j].endMathPoint[0] * Math.Cos(Math.PI / 2) - listCurveMid[j].endMathPoint[1] * Math.Sin(Math.PI / 2);
                                    y = listCurveMid[j].endMathPoint[0] * Math.Sin(Math.PI / 2) + listCurveMid[j].endMathPoint[1] * Math.Cos(Math.PI / 2);
                                    listCurveMid[j].endMathPoint[0] = x;
                                    listCurveMid[j].endMathPoint[1] = y;
                                    if (listCurveMid[j].r != 0)
                                    {
                                        x = listCurveMid[j].centerpoint[0] * Math.Cos(Math.PI / 2) - listCurveMid[j].centerpoint[1] * Math.Sin(Math.PI / 2);
                                        y = listCurveMid[j].centerpoint[0] * Math.Sin(Math.PI / 2) + listCurveMid[j].centerpoint[1] * Math.Cos(Math.PI / 2);
                                        listCurveMid[j].centerpoint[0] = x;
                                        listCurveMid[j].centerpoint[1] = y;
                                    }
                                }
                            }

                            #region "其他"
                            //直线处理，得到直线中性层线段
                            //foreach (Cord item1 in listcord)
                            //{
                            //    var lines = from line in listCurve1[0]
                            //                where (line.endMathPoint[0] == item1.x && line.endMathPoint[1] == item1.y) || (line.startMathPoint[0] == item1.x && line.startMathPoint[1] == item1.y)
                            //                select line;
                            //    if (lines.Count() > 1)
                            //    {
                            //        CurveInfo[] curveInfos = new CurveInfo[lines.Count()];

                            //        for (int j = 0; j < lines.Count(); j++)
                            //        {
                            //            curveInfos[j] = lines.ElementAt(j);
                            //        }
                            //        double[] dd11 = new double[2];
                            //        for (int j = 0; j < curveInfos.Count(); j++)
                            //        {
                            //            CurveInfo curveInfom = new CurveInfo();
                            //            for (int m = j + 1; m < curveInfos.Count(); m++)
                            //            {
                            //                double dd111 = Math.Round((curveInfos[j].endMathPoint[0] - curveInfos[j].startMathPoint[0]) * (curveInfos[m].endMathPoint[0] - curveInfos[m].startMathPoint[0]),3);
                            //                double dd222 = Math.Round((curveInfos[j].endMathPoint[1] - curveInfos[j].startMathPoint[1]) * (curveInfos[m].endMathPoint[1] - curveInfos[m].startMathPoint[1]),3);

                            //                if (dd111+dd222 == 0)
                            //                {                                              
                            //                    if (curveInfos[j].length == hou)
                            //                    {
                            //                        if (curveInfos[j].startMathPoint[0]==item1.x && curveInfos[j].startMathPoint[1] == item1.y)
                            //                        {
                            //                             dd11 = new double[2] { k*(curveInfos[j].endMathPoint[0]-curveInfos[j].startMathPoint[0]), k * (curveInfos[j].endMathPoint[1] - curveInfos[j].startMathPoint[1]) };
                            //                        }
                            //                        else
                            //                        {
                            //                            dd11 = new double[2] { k * (curveInfos[j].startMathPoint[0]- curveInfos[j].endMathPoint[0]), k * (curveInfos[j].startMathPoint[1]- curveInfos[j].endMathPoint[1])};
                            //                        }

                            //                        curveInfom.length = curveInfos[m].length;
                            //                        curveInfom.startMathPoint[0] = Math.Round(curveInfos[m].startMathPoint[0]+ dd11[0],3);
                            //                        curveInfom.startMathPoint[1] = Math.Round(curveInfos[m].startMathPoint[1] + dd11[1],3);
                            //                        curveInfom.endMathPoint[0] = Math.Round(curveInfos[m].endMathPoint[0] + dd11[0],3);
                            //                        curveInfom.endMathPoint[1] = Math.Round(curveInfos[m].endMathPoint[1] + dd11[1],3);

                            //                     }
                            //                    if (curveInfos[m].length == hou)
                            //                    {
                            //                        if (curveInfos[m].startMathPoint[0] == item1.x && curveInfos[m].startMathPoint[1] == item1.y)
                            //                        {
                            //                            dd11 = new double[2] { k * (curveInfos[m].endMathPoint[0] - curveInfos[m].startMathPoint[0]), k * (curveInfos[m].endMathPoint[1] - curveInfos[m].startMathPoint[1]) };
                            //                        }
                            //                        else
                            //                        {
                            //                            dd11 = new double[2] { k * (curveInfos[m].startMathPoint[0] - curveInfos[m].endMathPoint[0]), k * (curveInfos[m].startMathPoint[1] - curveInfos[m].endMathPoint[1]) };
                            //                        }

                            //                        curveInfom.length = curveInfos[j].length;
                            //                        curveInfom.startMathPoint[0] =Math.Round( curveInfos[j].startMathPoint[0] + dd11[0],3);
                            //                        curveInfom.startMathPoint[1] = Math.Round(curveInfos[j].startMathPoint[1] + dd11[1], 3);
                            //                        curveInfom.endMathPoint[0] = Math.Round(curveInfos[j].endMathPoint[0] + dd11[0], 3);
                            //                        curveInfom.endMathPoint[1] = Math.Round(curveInfos[j].endMathPoint[1] + dd11[1], 3);
                            //                    }
                            //                    if (!listCurveMid.Contains(curveInfom)) listCurveMid.Add(curveInfom);
                            //                }
                            //            }
                            //        }
                            //    }
                            //}

                            //圆弧处理，得到圆弧中性层线段
                            //for (int j = 0; j < listCurve1[1].Count(); j++)
                            //{
                            //    CurveInfo arcc = new CurveInfo();
                            //    for (int m = j+1; m < listCurve1[1].Count(); m++)
                            //    {
                            //        if (listCurve1[1][j].centerpoint[0] == listCurve1[1][m].centerpoint[0] && listCurve1[1][j].centerpoint[1] == listCurve1[1][m].centerpoint[1])
                            //        {
                            //            double dx1 = listCurve1[1][j].centerpoint[0] - listCurve1[1][j].startMathPoint[0];
                            //            double dy1 = listCurve1[1][j].centerpoint[1] - listCurve1[1][j].startMathPoint[1];

                            //            double dx2 = listCurve1[1][j].centerpoint[0] - listCurve1[1][m].startMathPoint[0];
                            //            double dy2 = listCurve1[1][j].centerpoint[1] - listCurve1[1][m].startMathPoint[1];

                            //            double rdx2 = Math.Round(dx1 * dy2,3);
                            //            double rdy2 = Math.Round(dx2 * dy1,3);

                            //            double dx11 = listCurve1[1][j].centerpoint[0] - listCurve1[1][j].endMathPoint[0];
                            //            double dy11 = listCurve1[1][j].centerpoint[1] - listCurve1[1][j].endMathPoint[1];

                            //            double dx22 = listCurve1[1][j].centerpoint[0] - listCurve1[1][m].endMathPoint[0];
                            //            double dy22 = listCurve1[1][j].centerpoint[1] - listCurve1[1][m].endMathPoint[1];

                            //            double rdx22 = Math.Round(dx11 * dy22,3);
                            //            double rdy22 = Math.Round(dx22 * dy11,3);
                            //            if (rdx2 == rdy2 && rdx22 == rdy22)
                            //            {
                            //                double rdx20 = Math.Round(listCurve1[1][m].r + hou,3);
                            //                double rdx21 = Math.Round(listCurve1[1][j].r + hou,3);
                            //                if (listCurve1[1][j].r == rdx20)
                            //                {
                            //                    dd1[0] = (listCurve1[1][j].startMathPoint[0] - listCurve1[1][m].startMathPoint[0]) * k;
                            //                    dd1[1] = (listCurve1[1][j].startMathPoint[1] - listCurve1[1][m].startMathPoint[1]) * k;

                            //                    dd2[0] = (listCurve1[1][j].endMathPoint[0] - listCurve1[1][m].endMathPoint[0]) * k;
                            //                    dd2[1] = (listCurve1[1][j].endMathPoint[1] - listCurve1[1][m].endMathPoint[1]) * k;

                            //                    arcc.centerpoint = listCurve1[1][j].centerpoint;
                            //                    arcc.startMathPoint = new double[2] { Math.Round(listCurve1[1][m].startMathPoint[0] + dd1[0],3), Math.Round(listCurve1[1][m].startMathPoint[1] + dd1[1],3) };
                            //                    arcc.endMathPoint = new double[2] { Math.Round(listCurve1[1][m].endMathPoint[0] + dd2[0],3), Math.Round(listCurve1[1][m].endMathPoint[1] + dd2[1],3) };
                            //                    arcc.r = listCurve1[1][m].r + Math.Round(Math.Sqrt(dd1[0] * dd1[0] + dd1[1] * dd1[1]),3);
                            //                    arcc.length = Math.Round((listCurve1[1][m].length / listCurve1[1][m].r) * arcc.r,3);

                            //                    if (!listCurveMid.Contains(arcc))
                            //                    {
                            //                        listCurveMid.Add(arcc);
                            //                        CurveInfo[] curveInfoss = GetLineCurveInfo(arcc);
                            //                        if (!listCurveMid.Contains(curveInfoss[0])) listCurveMid.Add(curveInfoss[0]);
                            //                        if (!listCurveMid.Contains(curveInfoss[1])) listCurveMid.Add(curveInfoss[1]);
                            //                    }
                            //                }
                            //                else if (listCurve1[1][m].r == rdx21)
                            //                {
                            //                    dd1[0] = (listCurve1[1][m].startMathPoint[0] - listCurve1[1][j].startMathPoint[0]) * k;
                            //                    dd1[1] = (listCurve1[1][m].startMathPoint[1] - listCurve1[1][j].startMathPoint[1]) * k;

                            //                    dd2[0] = (listCurve1[1][m].endMathPoint[0] - listCurve1[1][j].endMathPoint[0]) * k;
                            //                    dd2[1] = (listCurve1[1][m].endMathPoint[1] - listCurve1[1][j].endMathPoint[1]) * k;

                            //                    arcc.centerpoint = listCurve1[1][j].centerpoint;
                            //                    arcc.startMathPoint = new double[2] { Math.Round(listCurve1[1][j].startMathPoint[0] + dd1[0],3), Math.Round(listCurve1[1][j].startMathPoint[1] + dd1[1],3) };
                            //                    arcc.endMathPoint = new double[2] { Math.Round(listCurve1[1][j].endMathPoint[0] + dd2[0],3), Math.Round(listCurve1[1][j].endMathPoint[1] + dd2[1],3) };
                            //                    arcc.r = listCurve1[1][j].r + Math.Round(Math.Sqrt(dd1[0] * dd1[0] + dd1[1] * dd1[1]),3);
                            //                    arcc.length = Math.Round((listCurve1[1][j].length / listCurve1[1][j].r) * arcc.r,3);

                            //                    if (!listCurveMid.Contains(arcc))
                            //                    {
                            //                        listCurveMid.Add(arcc);
                            //                        CurveInfo[] curveInfoss = GetLineCurveInfo(arcc);
                            //                        if (!listCurveMid.Contains(curveInfoss[0])) listCurveMid.Add(curveInfoss[0]);
                            //                        if (!listCurveMid.Contains(curveInfoss[1])) listCurveMid.Add(curveInfoss[1]);
                            //                    }
                            //                }                                          
                            //            }

                            //        }
                            //    }
                            //}
                            //去掉多余线段
                            //for (int j = 0; j < listCurveMidCopy.Count; j++)
                            //{
                            //    if (listCurveMidCopy[j].r != 0) continue;

                            //    for (int m = 0; m < listCurveMidCopy.Count; m++)
                            //    {
                            //        if (listCurveMidCopy[m].r != 0) continue;

                            //        if (m != j)
                            //        {
                            //            double dx1 = listCurveMidCopy[j].startMathPoint[0] - listCurveMidCopy[m].startMathPoint[0];
                            //            double dy1 = listCurveMidCopy[j].startMathPoint[1] - listCurveMidCopy[m].startMathPoint[1];

                            //            double dx11 = listCurveMidCopy[j].startMathPoint[0] - listCurveMidCopy[m].endMathPoint[0];
                            //            double dy11 = listCurveMidCopy[j].startMathPoint[1] - listCurveMidCopy[m].endMathPoint[1];

                            //            double dx2 = listCurveMidCopy[j].endMathPoint[0] - listCurveMidCopy[m].startMathPoint[0];
                            //            double dy2 = listCurveMidCopy[j].endMathPoint[1] - listCurveMidCopy[m].startMathPoint[1];

                            //            double dx22 = listCurveMidCopy[j].endMathPoint[0] - listCurveMidCopy[m].endMathPoint[0];
                            //            double dy22 = listCurveMidCopy[j].endMathPoint[1] - listCurveMidCopy[m].endMathPoint[1];

                            //            if (Math.Round(dx1 * dy11,6) == Math.Round(dy1 * dx11,6) && Math.Round(dx2 * dy22,6) == Math.Round(dy2 * dx22,6))
                            //            {
                            //                recod += 1;
                            //            }
                            //        }
                            //    }
                            //    if (recod == 0 && listCurveMidCopy[j].r == 0)
                            //    {
                            //        listCurveMid.Remove(listCurveMidCopy[j]);
                            //    }
                            //    recod = 0;
                            //}
                            #endregion
                            Cord cordmin = new Cord() { x = listCurveMid[0].startMathPoint[0], y = listCurveMid[0].startMathPoint[1] };
                            Cord cordmax = new Cord() { x = listCurveMid[0].endMathPoint[0], y = listCurveMid[0].endMathPoint[1] };

                            ChangeValue(ref listCurveMid);

                             listCurveMidLast[i] = listCurveMid;

                                    }

                                    bitM.GetBitmap(listCurveMidLast, hou);

                                    string address = Path.GetDirectoryName(file1) + "\\" + Path.GetFileNameWithoutExtension(file1) + ".jpg";
                                    
                                    bitM.SaveBit(address);
                                                                   
                                }
                            }
            }
                       iSwApp.CloseDoc(Path.GetFileName(file1));
                }
    }
            }
            catch (System.Exception e1)
            {
                Relation.Exp(e1);
            }
        }
        /// <summary>
        /// 返回坐标数组，坐标值小的作为起始点，数组第一个为起始点，坐标值大的作为终止点,数组第二个为终止点
        /// </summary>
        /// <param name="c1">坐标点1横坐标</param>
        /// <param name="c2">坐标点1纵坐标</param>
        /// <param name="c3">坐标点2横坐标</param>
        /// <param name="c4">坐标点2纵坐标</param>
        /// <returns></returns>
        private double[][] GetStartPoint(double c1, double c2, double c3, double c4)
        {
            double[][] cor = new double[2][];
            double c11 = c1 * 1000;
            double c22 = c2 * 1000;
            double c33 = c3 * 1000;
            double c44 = c4 * 1000;

            cor[0] = new double[] { c11, c22 };
            cor[1] = new double[] { c33, c44 };

            if (Math.Round(c11, 3) > Math.Round(c33, 3))
            {
                cor[0][0] = c33;
                cor[0][1] = c44;

                cor[1][0] = c11;
                cor[1][1] = c22;
            }
            else if (Math.Round(c11, 3) == Math.Round(c33, 3))
            {
                if (Math.Round(c22, 3) > Math.Round(c44, 3))
                {
                    cor[0][0] = c33;
                    cor[0][1] = c44;

                    cor[1][0] = c11;
                    cor[1][1] = c22;
                }
                else if (Math.Round(c22, 3) == Math.Round(c44, 3))
                {
                    cor = null;
                }
            }
            return cor;
        }
        /// <summary>
        /// 整体平台
        /// </summary>
        /// <param name="curveInfo"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void ChangeCurve(ref CurveInfo curveInfo, double x, double y)
        {
            curveInfo.startMathPoint[0] = curveInfo.startMathPoint[0] + x;
            curveInfo.startMathPoint[1] = curveInfo.startMathPoint[1] + y;
            curveInfo.endMathPoint[0] = curveInfo.endMathPoint[0] + x;
            curveInfo.endMathPoint[1] = curveInfo.endMathPoint[1] + y;

            if (curveInfo.r != 0)
            {
                curveInfo.centerpoint[0] = curveInfo.centerpoint[0] + x;
                curveInfo.centerpoint[1] = curveInfo.centerpoint[1] + y;
            }
        }

        /// <summary>
        /// 两条线段是否平行，距离为某一固定值
        /// </summary>
        /// <param name="curveInfoItem">线段1</param>
        /// <param name="curveInfo">线段2</param>
        /// <param name="hou">距离</param>
        /// <returns></returns>
        private bool GetInstecLine(CurveInfo curveInfoItem, CurveInfo curveInfo, double hou)
        {

            double dx1 = curveInfoItem.startMathPoint[0] - curveInfoItem.endMathPoint[0];
            double dy1 = curveInfoItem.startMathPoint[1] - curveInfoItem.endMathPoint[1];

            double dx2 = curveInfo.startMathPoint[0] - curveInfo.endMathPoint[0];
            double dy2 = curveInfo.startMathPoint[1] - curveInfo.endMathPoint[1];

            double dx1y2 = Math.Round(dx1 * dy2, 2);
            double dy1x2 = Math.Round(dy1 * dx2, 2);

            double dx3 = curveInfo.startMathPoint[0] - curveInfoItem.endMathPoint[0];
            double dy3 = curveInfo.startMathPoint[1] - curveInfoItem.endMathPoint[1];

            double h = Math.Round(Math.Abs(dx3 * dy1 - dy3 * dx1) / curveInfoItem.length, 3);

            if (dx1y2 == dy1x2 && h == hou) return true;
            else return false;

        }

        /// <summary>
        /// 点是否在线段上
        /// </summary>
        /// <param name="listCurve1">圆弧集合</param>
        /// <param name="listCurve0">直线线段</param>
        /// <param name="listCurve0s">直线集合</param>
        /// <param name="hou">板厚</param>
        /// <returns></returns>
        private bool FindInter(List<CurveInfo> listCurve1, CurveInfo listCurve0, List<CurveInfo> listCurve0s,double hou)
        {
            List<CurveInfo> listCurve0ss =new List<CurveInfo>(listCurve0s.ToList());

            listCurve0ss.Remove(listCurve0);

            foreach (var item1 in listCurve1)
            {
            double dx1 = item1.endMathPoint[0] - listCurve0.startMathPoint[0];
            double dy1 = item1.endMathPoint[1] - listCurve0.startMathPoint[1];

            double dx2 = item1.endMathPoint[0] - listCurve0.endMathPoint[0];
            double dy2 = item1.endMathPoint[1] - listCurve0.endMathPoint[1];

            double dx1y2 = Math.Round(dx1 * dy2, 2);
            double dx2y1 = Math.Round(dx2 * dy1, 2);

            double dx11 = item1.startMathPoint[0] - listCurve0.startMathPoint[0];
            double dy11 = item1.startMathPoint[1] - listCurve0.startMathPoint[1];

            double dx22 = item1.startMathPoint[0] - listCurve0.endMathPoint[0];
            double dy22 = item1.startMathPoint[1] - listCurve0.endMathPoint[1];

            double dx11y22 = Math.Round(dx11 * dy22, 2);
            double dx22y11 = Math.Round(dx22 * dy11, 2);

            if (dx1y2 == dx2y1 || dx22y11 == dx11y22)
            {
                foreach (CurveInfo item in listCurve0ss)
                {
                    double sdx1 = item.startMathPoint[0] - listCurve0.startMathPoint[0];
                    double sdy1 = item.startMathPoint[1] - listCurve0.startMathPoint[1];

                    double sdx2 = item.endMathPoint[0] - listCurve0.endMathPoint[0];
                    double sdy2 = item.endMathPoint[1] - listCurve0.endMathPoint[1];

                    double lengt1 = Math.Sqrt(sdx1 * sdx1 + sdy1 * sdy1);
                    double lengt2 = Math.Sqrt(sdx2 * sdx2 + sdy2 * sdy2);

                    if (Math.Round( item.length,2)== Math.Round(listCurve0.length, 2) &&
                        Math.Round(lengt1, 2) == Math.Round(hou, 2) &&
                        Math.Round(lengt2, 2) == Math.Round(hou, 2))
                    {
                        return true;
                    }
                }
            }           
            }
            return false;
        }
        /// <summary>
        /// 改变点坐标
        /// </summary>
        /// <param name="listCurveMid">线条集合</param>
        /// <param name="cordmin">图片最小位置</param>
        /// <param name="cordmax">图片最大位置</param>
        private void ChangeValue(ref List<CurveInfo> listCurveMid)
        {
            Cord   cordmin = new Cord() { x = listCurveMid[0].startMathPoint[0], y = listCurveMid[0].startMathPoint[1] };
            Cord cordmax = new Cord() { x = listCurveMid[0].endMathPoint[0], y = listCurveMid[0].endMathPoint[1] };

            foreach (CurveInfo item2 in listCurveMid)
            {
                cordmin.x = item2.startMathPoint[0] < cordmin.x ? item2.startMathPoint[0] : cordmin.x;
                cordmin.x = item2.endMathPoint[0] < cordmin.x ? item2.endMathPoint[0] : cordmin.x;
                cordmin.y = item2.startMathPoint[1] < cordmin.y ? item2.startMathPoint[1] : cordmin.y;
                cordmin.y = item2.endMathPoint[1] < cordmin.y ? item2.endMathPoint[1] : cordmin.y;

                cordmax.x = item2.startMathPoint[0] > cordmax.x ? item2.startMathPoint[0] : cordmax.x;
                cordmax.x = item2.endMathPoint[0] > cordmax.x ? item2.endMathPoint[0] : cordmax.x;
                cordmax.y = item2.startMathPoint[1] > cordmax.y ? item2.startMathPoint[1] : cordmax.y;
                cordmax.y = item2.endMathPoint[1] > cordmax.y ? item2.endMathPoint[1] : cordmax.y;
            }

            double dirx = cordmax.x - cordmin.x;
            double diry = cordmax.y - cordmin.y;

            if (dirx > diry)
            {
                cordmin.y = cordmin.y - (dirx - diry) / 2;
                cordmax.y = cordmax.y + (dirx - diry) / 2;
            }
            else if (dirx < diry)
            {
                cordmin.x = cordmin.x - (diry - dirx) / 2;
                cordmax.x = cordmax.x + (diry - dirx) / 2;
            }

            dirx = cordmax.x - cordmin.x;
            diry = cordmax.y - cordmin.y;

            cordmin.x = Math.Round(cordmin.x - 0.2 * dirx, 3);
            cordmin.y = Math.Round(cordmin.y - 0.2 * diry, 3);

            cordmax.x = Math.Round(cordmax.x + 0.2 * dirx, 3);
            cordmax.y = Math.Round(cordmax.y + 0.2 * diry, 3);

          double  coo = cordmax.x - cordmin.x;
          double  bew = 1000 / coo;

            for (int j = 0; j < listCurveMid.Count; j++)
            {
                listCurveMid[j].startMathPoint[0] = (listCurveMid[j].startMathPoint[0] - cordmin.x)*bew;
                listCurveMid[j].startMathPoint[1] =(listCurveMid[j].startMathPoint[1] - cordmin.y)*bew;

                listCurveMid[j].endMathPoint[0] = (listCurveMid[j].endMathPoint[0] - cordmin.x)*bew;
                listCurveMid[j].endMathPoint[1] = (listCurveMid[j].endMathPoint[1] - cordmin.y)*bew;

                listCurveMid[j].length = listCurveMid[j].length;
                listCurveMid[j].lengthbew = listCurveMid[j].length * bew;

                if (listCurveMid[j].r != 0)
                {
                    listCurveMid[j].centerpoint[0] = (listCurveMid[j].centerpoint[0] - cordmin.x)*bew;
                    listCurveMid[j].centerpoint[1] = (listCurveMid[j].centerpoint[1] - cordmin.y)*bew;
                }
            }
        }
    }
        #endregion
        class CurveInfo
        {
            /// <summary>
            /// 线长
            /// </summary>
            public double length = 0;
            public double[] startMathPoint = new double[] { 0, 0 };
            public double[] endMathPoint = new double[] { 0, 0 };
            /// <summary>
            /// r不为0时线条类型为圆弧或者圆；
            /// </summary>
            public double r = 0;
            /// <summary>
            /// 圆心坐标
            /// </summary>
            public double[] centerpoint = new double[] { 0, 0 };
            /// <summary>
            /// x代表第一个坐标，y代表第二个坐标。
            /// </summary>
            public double lengthbew = 0;
            public override bool Equals(object obj)
            {
                CurveInfo curveInfoobj = (CurveInfo)obj;
                if (Math.Round(this.startMathPoint[0], 3) == Math.Round(curveInfoobj.startMathPoint[0], 3) && Math.Round(this.startMathPoint[1], 3) == Math.Round(curveInfoobj.startMathPoint[1], 3) &&
                    Math.Round(this.endMathPoint[0], 3) == Math.Round(curveInfoobj.endMathPoint[0], 3) && Math.Round(this.endMathPoint[1], 3) == Math.Round(curveInfoobj.endMathPoint[1], 3) &&
                   Math.Round(this.length, 3) == Math.Round(curveInfoobj.length, 3) && Math.Round(this.r, 3) == Math.Round(curveInfoobj.r, 3) &&
                     Math.Round(this.centerpoint[0], 3) == Math.Round(curveInfoobj.centerpoint[0], 3) && Math.Round(this.centerpoint[1], 3) == Math.Round(curveInfoobj.centerpoint[1], 3) &&
                      Math.Round(this.lengthbew, 3) == Math.Round(curveInfoobj.lengthbew, 3)
                    )
                {
                    return true;
                }
                else
                { return false; }
            }
            public override int GetHashCode()
            {
                return Math.Round(length, 3).GetHashCode() + Math.Round(startMathPoint[0], 3).GetHashCode() +
                    Math.Round(startMathPoint[1], 3).GetHashCode() + Math.Round(endMathPoint[0], 3).GetHashCode() + Math.Round(endMathPoint[1], 3).GetHashCode()
                    + Math.Round(r, 3).GetHashCode() + Math.Round(centerpoint[0], 3).GetHashCode() + Math.Round(centerpoint[1], 3).GetHashCode();
            }
        }
        /// <summary>
        /// 点坐标
        /// </summary>
        struct Cord
        {
            /// <summary>
            /// x代表第一个坐标，y代表第二个坐标。
            /// </summary>
            public double x, y;
            public override bool Equals(object obj)
            {
                Cord cord = (Cord)obj;
                if (Math.Round(this.x, 3) == Math.Round(cord.x, 3) && Math.Round(this.y, 3) == Math.Round(cord.y, 3))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            public override int GetHashCode()
            {
                return Math.Round(x, 3).GetHashCode() + Math.Round(y, 3).GetHashCode();
            }
        }
    }



