using Sszn;
using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Drawing.Text;


namespace WindowsFormsApplication8
{
    public partial class Form1 : Form
    {
        #region 自定义
        //此处操作仅限本项目
        //因为SR9040最少单次触发50行数据，线宽6400个点
        int[] SR1Array = new int[50 * 6400]; 
        int[] SR2Array = new int[50 * 6400];

        //实例化镭射控制类
        SsznCamControl camControl1 = new SsznCamControl(0); 
        SsznCamControl camControl2 = new SsznCamControl(1); 

        private bool isDeviceConnected = false; //判断镭射连接状态
        private bool isStartProcess = false; //判断是否开始采集数据
        private bool isServerConnected = true; //判断Server是否连接

        private string logFilePath = "../../log.txt"; //log日志存储路径
        private string configPath = "../../Configure.txt"; //配置文件
        private string DataSavePath = "../../Result/Data.csv"; //数据结果保存路径

        //镭射的IP
        private string SR1IP; 
        private string SR2IP; 

        private readonly Series series; //Chart控件显示的值需要使用的一维数组Series

        double[] SubResultData = new double [10]; //最终显示和保存结果
        #endregion

        #region init
        public Form1()
        {
            InitializeComponent();

            InitializeChaart(); //初始化Chart控件所需操作在这个函数

            //Chart控件显示数据的Label和数据显示形式为折线图
            series = new Series
            {
                Name = "DataSeries",
                ChartType = SeriesChartType.Line
            };

            chart1.Series.Add(series); //将Series添加到Chart控件中
        }
        #endregion

        #region Load
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Load_1(object sender, EventArgs e)
        {
            //镭射的ShowInfo功能，当时不加就报错
            camControl1.ShowInfo1 += showInfo; 
            camControl2.ShowInfo1 += showInfo;

            ReadAndProcessConfig(); //读取配置文件里的参数
            ChangeLabelStatus(); //更新设备连接状态
        }
        #endregion

        #region 镭射打印日志功能，没有它就报错
        private void showInfo(string info1)
        {
            string Text = DateTime.Now.ToLongTimeString() + info1 + "\r\n";
            if (InfoText.InvokeRequired)
            {
                Action<string> actionDelegate = (x) => { InfoText.AppendText(Text); };
                InfoText.BeginInvoke(actionDelegate, Text);
            }
            else
            {
                InfoText.AppendText(Text);
            }
        }
        #endregion

        #region 默认界面显示内容
        /// <summary>
        /// 默认关闭连接状态
        /// </summary>
        private void ChangeLabelStatus()
        {
            //定义连接状态初始显示内容
            label1.BackColor = Color.Red;
            label3.BackColor = Color.Red;
            label2.Text = "SR1Temp:**℃";
            label2.BackColor = Color.Red;
            label4.Text = "SR2Temp:**℃";
            label4.BackColor = Color.Red;
            label6.Text = "SR1_Key：***Days";
            label6.BackColor = Color.Red;
            label10.Text = "SR2_Key：***Days";
            label10.BackColor = Color.Red;
        }
        #endregion

        #region 显示获取到的数组
        private void InitializeChaart()
        {
            chart1.ChartAreas[0].AxisX.Title = "PointsNum"; //X轴名称
            chart1.ChartAreas[0].AxisY.Title = "Z Value"; //Y轴名称
            chart1.ChartAreas[0].AxisY.Maximum = Convert.ToDouble(textBox2.Text.ToString()); //Y轴刻度最大值，注释掉即自适应
            chart1.ChartAreas[0].AxisY.Minimum = Convert.ToDouble(textBox3.Text.ToString()); //Y轴刻度最小值，注释掉即自适应
            chart1.ChartAreas[0].BackColor = Color.FromArgb(200, 200, 200); //Chart背景颜色设置，当前仅示例
            chart1.ChartAreas[0].BackSecondaryColor = Color.White;
            chart1.ChartAreas[0].BackGradientStyle = GradientStyle.TopBottom;
        }
        /// <summary>
        /// 显示镭射计算结果数据图表
        /// </summary>
        private async Task DisplayChart()
        {
            series.Points.Clear();

            if (chart1.InvokeRequired)
            {
                Action safeChart = delegate { DisplayChart(); };
                chart1.Invoke(safeChart);
            }
            else
            {
                for (int i = 0; i < SubResultData.Length; i++)
                {
                    series.Points.AddXY(i, SubResultData[i]);
                }
                chart1.Legends.Clear(); //清除右上角label图例
                series.Color = Color.FromArgb(255, 0, 255);

                //添加最大值和最小值标记
                //值是根据配置文档中设置的而来
                double maxValue = Convert.ToDouble(textBox2.Text.ToString()); 
                double minValue = Convert.ToDouble(textBox3.Text.ToString());

                StripLine maxStripLine = new StripLine();
                maxStripLine.Text = string.Format("Z Max Value：{0:F}", maxValue);
                //maxStripLine.Interval = 0;
                maxStripLine.StripWidth = (chart1.ChartAreas[0].AxisY.Maximum - chart1.ChartAreas[0].AxisY.Minimum) * 0.005f; //上限提示线的宽度
                maxStripLine.BackColor = Color.Red; //上限提示线的颜色
                maxStripLine.IntervalOffset = maxValue;
                chart1.ChartAreas[0].AxisY.StripLines.Add(maxStripLine);

                StripLine minStripLine = new StripLine();
                minStripLine.Text = string.Format("Z Min Value：{0:F}", minValue);
                //minStripLine.Interval = 0;
                minStripLine.StripWidth = (chart1.ChartAreas[0].AxisY.Maximum - chart1.ChartAreas[0].AxisY.Minimum) * 0.005f; //下限提示线的宽度
                minStripLine.BackColor = Color.Red; //下限提示线的颜色
                minStripLine.IntervalOffset = minValue;
                chart1.ChartAreas[0].AxisY.StripLines.Add(minStripLine);

                chart1.Invalidate();
                Controls.Add(chart1);
                WriteTextSafe("Data Display Success!");

                //将text文本框最大化,没地方放，先扔这
                InfoText.Multiline = true;
                InfoText.Dock = DockStyle.Fill;
                await Task.Delay(0);
            }
        }
        #endregion

        #region 连续触发模式获取数据
        /// <summary>
        /// 连续触发获取数据
        /// </summary>
        private async Task  StartProcessData()
        {
                WriteTextSafe("Start Process Data...");

                int errT1 = camControl1.softTrigOne();
                WriteTextSafe("\r\n" + (errT1 == 0 ? "softTrigOne SR1 Finish." : "softTrigOne Err.")); //C#的条件表达式
                SR1Array = camControl1.HeightData[0];

                await Task.Delay(0);
        }

        private async Task StartProcessData2()
        {
            WriteTextSafe("Start Process Data...");

            int errT2 = camControl2.softTrigOne_2();
            WriteTextSafe("\r\n" + (errT2 == 0 ? "softTrigOne SR2 Finish." : "softTrigOne Err.")); //C#的条件表达式
            SR2Array = camControl2.HeightData[0];

            await Task.Delay(0);
        }
        #endregion

        #region 计算高度差
        /// <summary>
        /// 计算上下镭射高度差值
        /// </summary>
        private void measureArray()
        {
            //方法一 共采集50行数据，只取第一行数据出来计算，每两百个点计算一个均值作为产品厚度值

            //int[] SR1NewArray = new int[2400];
            //Array.Copy(SR1Array, 1999, SR1NewArray, 0, 2400);

            //int[] SR2NewArray = new int[4400];
            //Array.Copy(SR2Array, 1999, SR2NewArray, 0, 2400);

            //int selectIndex = 0;
            //for (int i = 0; i < 2400; i += 200)
            //{
            //    SubResultData[selectIndex] = (SR1NewArray[i] - SR2NewArray[i]) * 0.00001;
            //    selectIndex++;
            //}

            //方法二

            //取几行采样数据算均值，如果单条线宽的Z值为X方向，那么它就是Y方向取几列来算均值
            //不敢用10列来算均值，数据太好了，吓人
            int getLineNum = 5;

            //线宽6400，取多少个参与计算决定数组长度
            int[] SR1NewArray = new int[4000];
            int[] SR2NewArray = new int[4000];
            double[] tmpRes = new double[4000];

            //根据getLineNum控制循环次数
            for (int j = 0; j < getLineNum; j++)
            {
                //此处操作仅限本项目
                //因为本项目标定导致上下镭射线挫了一点，原本头和尾各裁1200个点，只能少取400，所以从799开始裁
                //         原数组    从哪开始取      Copy到哪        Copy多长
                Array.Copy(SR1Array, 799 + j*6400, SR1NewArray, 0, 4000);
                Array.Copy(SR2Array, 799 + j*6400, SR2NewArray, 0, 4000);

                tmpRes[0] = SR1NewArray[0] - SR2NewArray[0];

                //计算累加和
                for (int i = 1; i < 4000; i++)
                {
                    tmpRes[i] = tmpRes[i - 1] + (SR1NewArray[i] - SR2NewArray[i]);
                }

                int selectIndex = 0;

                //每行采样数据中，每400个点计算一个均值，共 getLineNum 行的均值累加和
                for (int i = 0; i < 4000; i += 400)
                {
                    SubResultData[selectIndex] +=  (tmpRes[i + 399] - tmpRes[i]) * 0.0025 * 0.00001;
                    selectIndex++;
                }
            }

            //数组中为五行数据的累加和，计算getLineNum行的数据的均值所以需 /= getLineNum
            for (int i = 0; i <10 ; i++)
            {
                SubResultData[i] /= (double)getLineNum;
            }
        }
        #endregion

        #region 获取镭射的温度，Key剩余时间——暂时调不出来，预留功能
        /// <summary>
        /// 获取镭射的温度和Key剩余时间
        /// </summary>
        /// <returns></returns>
        private async Task GetSR1Para()
        {
            //接收镭射剩余使用天数变量
            IntPtr SR1RemainDay = new IntPtr();

            //接收镭射当前温度变量
            int tempA = 99;
            int tempB = 99;

            while (label1.BackColor == Color.Green)
            {
                SR7LinkFunc.SR7IF_GetLicenseKey(0, SR1RemainDay); //获取镭射剩余天数
                label6.Text = string.Format("SR1_Key:{0}Days", SR1RemainDay.ToString());

                SR7LinkFunc.SR7IF_GetCameraTemperature(0, ref tempA, ref tempB); //获取镭射当前温度
                label2.Text = string.Format("SR1Temp:{0}℃", (tempA / 100).ToString());

                await Task.Delay(999); //控制刷新周期
            }
        }

        private async Task GetSR2Para()
        {
            //接收镭射剩余使用天数变量
            IntPtr SR2RemainDay = new IntPtr();

            //接收镭射当前温度变量
            int tempA = 99;
            int tempB = 99;

            while (label3.BackColor == Color.Green)
            {
                SR7LinkFunc.SR7IF_GetLicenseKey(1, SR2RemainDay); //获取镭射剩余天数
                label10.Text = string.Format("SR2_Key:{0}Days", SR2RemainDay.ToString());

                SR7LinkFunc.SR7IF_GetCameraTemperature(1, ref tempA, ref tempB); //获取镭射当前温度
                label4.Text = string.Format("SR2Temp:{0}℃", (tempA / 100).ToString());

                await Task.Delay(999); //控制刷新周期
            }
        }
        #endregion

        #region 双镭射连接
        /// <summary>
        /// 连接镭射，更新连接状态
        /// </summary>
        private void ConnectedDevice()
        {
            //连接双镭射
            int SR1Status = camControl1.connect(SR1IP);
            int SR2Status = camControl2.connect_2(SR2IP);

            //连接成功更新状态
            if (SR1Status == 0)
            {
                WriteTextSafe("SR1 Connected Success");
                label1.BackColor = Color.Green;
                label2.BackColor = Color.Green;
                label6.BackColor = Color.Green;

                //初始化一次回调
                camControl1.DataOnetimeCallBack();

                Task getSR1Para = GetSR1Para(); //更新温度和Key剩余时间
            }
            else
            {
                WriteTextSafe("SR1 Connected Faile");
            }

            //连接成功更新状态
            if (SR2Status == 0)
            {
                WriteTextSafe("SR2 Connected Success");
                label3.BackColor = Color.Green;
                label4.BackColor = Color.Green;
                label10.BackColor = Color.Green;

                //一次回调
                camControl2.DataOnetimeCallBack_2();

                Task getSR2Para = GetSR2Para(); //更新温度和Key剩余时间
            }
            else
            {
                WriteTextSafe("SR2 Connected Faile");
            }

            //更新按钮显示内容和颜色
            if (SR1Status == 0 && SR2Status == 0 && isServerConnected == true)
            {
                isDeviceConnected = true; //连接成功设置连接状态为true
                button1.Text = "On";
                button1.BackColor = Color.Green;
            }
            else
            {
                Disconnected();
            }
        }
        #endregion

        #region 双镭射断开
        /// <summary>
        /// 关闭所有连接，恢复默认设备连接状态
        /// </summary>
        private void Disconnected()
        {
            //断开镭射连接
            camControl1.disconnect();
            camControl2.disconnect();

            ChangeLabelStatus(); //更新连接状态
            isDeviceConnected = false; //断开连接后设置连接状态为false
            button1.Text = "Off";
            button1.BackColor = Color.Red;
        }
        #endregion

        #region On/Off按钮切换连接状态
        /// <summary>
        /// 点击按钮切换所有设备连接状态
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            if (isDeviceConnected)
            {
                Disconnected(); //断开设备连接
            }
            else
            {
                ConnectedDevice(); //连接设备
            }
        }
        #endregion

        #region Start按钮控制采集
        /// <summary>
        /// 点击按钮切换是否开始采集
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button2_Click(object sender, EventArgs e)
        {
                if (isStartProcess)
                {
                    //断开
                    isStartProcess = false;
                    button2.BackColor = Color.Red;
                    WriteTextSafe("Stop Process Data...");
                }
                else
                {
                    isStartProcess = true;

                    if (button1.BackColor == Color.Green)
                    {
                        try
                        {
                            while (true)
                            {
                                Task SR1StartProc = StartProcessData();
                                Task SR2StartProc = StartProcessData2();

                                WriteTextSafe("Start Get Data...");
                                
                                button2.BackColor = Color.Green;
                                ReadAndProcessConfig(); //读取配置文件并应用

                                await Task.WhenAll(SR1StartProc, SR2StartProc); //等待双镭射都采集完数据

                                measureArray();

                                Task writeData = WriteArrayToCSV();
                                Task displayData = DisplayChart();
                                
                                await Task.WhenAll(writeData, displayData); //等待保存数据和显示图表完成，胆子大的可以不等，计算完直接开始下一轮采集
                                await Task.Delay(Convert.ToInt16(textBox1.Text.ToString())); //等待配置参数中设置的Interval采集间隔，单位ms

                                if (isStartProcess == false)
                                {
                                    break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteTextSafe(ex.Message);
                        }
                    }
                    else
                    {
                        WriteTextSafe("镭射连上了嘛你就开始？"); //等待大聪明触发这个功能
                    }
                }
        }
        #endregion

        #region 不停机应用参数设定 
        /// <summary>
        /// 读取并应用配置文件 ———— 每次更新参数就要重开程序，烦死了，现在舒服了
        /// </summary>
        private void ReadAndProcessConfig()
        {
            try 
	        {
		        using (StreamReader reader = new StreamReader(configPath))
                {
                    List<string> valueList = new List<string>();
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] parts = line.Split('=');
                        valueList.Add(parts[1].Trim());
                    }

                    for (int i = 0; i < valueList.Count; i++)
                    {
                        if (i==0)
                        {
                            SR1IP = valueList[i];
                        }
                        else if (i == 1)
                        {
                            SR2IP = valueList[i];
                        }
                        else if (i == 2)
                        {
                            textBox1.Text = valueList[i];
                        }
                        else if (i == 3)
                        {
                            textBox2.Text = valueList[i];
                        }
                        else if (i == 4)
                        {
                            textBox3.Text = valueList[i];
                        }
                    }
                    WriteTextSafe("配置文件读取应用成功！");
                }
	        }
	        catch (Exception)
	        {
		        WriteTextSafe("配置文件异常，请查看Configure.txt");
	        }
        }
        #endregion

        #region 打印日志就调它
        /// <summary>
        /// 打印日志 ———— 简单强大的显示日志功能
        /// </summary>
        /// <param name="text">文本显示内容</param>
        public void WriteTextSafe(string text)
        {
            if (InfoText.InvokeRequired)
            {
                Action safeWrite = delegate { WriteTextSafe(text); };
                InfoText.Invoke(safeWrite);
            }
            else
            {
                InfoText.AppendText(DateTime.Now.ToString() + text + Environment.NewLine);
                if (!File.Exists(logFilePath))
                {
                    using (StreamWriter writer = File.CreateText(logFilePath))
                    { 

                    }
                }
                using (StreamWriter writer = new StreamWriter(logFilePath,true))
                {
                    writer.WriteLine(DateTime.Now.ToString() + "_" + text + Environment.NewLine);
                }
                InfoText.SelectionStart = InfoText.Text.Length;
                InfoText.ScrollToCaret();
            }
        }
        #endregion

        #region 向csv写数据就调它 ———— 此处有个小bug：如果没有Result文件夹会报错，懒得改
        /// <summary>
        /// 写数据
        /// </summary>
        /// <param name="array"></param>
        private async Task WriteArrayToCSV()
        {
            bool fileExists = File.Exists(DataSavePath);

            using (StreamWriter writer = new StreamWriter(DataSavePath,append:true))
            {
                if (!fileExists)
                {
                    writer.WriteLine(""); //写入表头
                }

                for (int i = 0; i < SubResultData.Length; i++)
                {
                    writer.Write(SubResultData[i]);

                    if (i < SubResultData.Length - 1)
                    {
                        writer.Write(",");
                    }
                }
                writer.WriteLine();
                writer.Close(); //写完就关闭文档好习惯！防止一直占用文件，烦
            }

            WriteTextSafe("Data Save Success!");
            await Task.Delay(0);
        }
        #endregion
    }
}
