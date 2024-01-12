# 双镭射对射测厚框架-开发手册

## 前言

本人也是第一次写Winform和C#，部分代码块没有很好的做到面向对象，部分代码块的逻辑可能也不是最佳写法，不足的地方希望可以多多指导

Data：2024/1/9

Author：Siyuan Zhang

Mail：15600165736@163.com

Describe：代码均为本人手写，仅供学习，如商用概不负责。最后说一句：开源万岁

## Winform功能介绍（图片看不了就看Typora图片文件夹）

![Winform界面](C:\Users\15600\Desktop\双镭射对射测厚框架V1.1\WindowsFormsApplication8\Typora图片\Winform界面.png)

1、数据显示区域：

采集完计算出结果后数据以折线图的形式显示

2、参数：

SampInter（Sample Interval）：采集间隔（单位ms）

Z MaxValue：显示公差/最大允许波动范围的最大值

Z MinValue：显示公差/最小允许波动范围的最小值

修改代码目录下Configure.txt文件设置参数，支持不停机更新，更新Configure.txt文件后，重新点击一下Start按钮即可生效

3、On/Off：连接/关闭镭射按钮，初次点击连接设备，显示绿色On，再次点击断开连接，显示红色Off

4、Start：开始/停止采集数据按钮，初次点击，镭射开始采集数据并计算结果，并将结果保存和现实在图标区域中

5、日志：所有文字输出、日志和报错都显示在此文本框内，所有内容也会保存在代码目录下Log.txt中

6、状态：显示镭射的连接状态、温度和License剩余使用时间，红色为断开，绿色为连接状态（Server_Connect功能未添加）

## 代码简述准备工作（图片看不了就看Typora图片文件夹）

本代码使用深视的SR 9040，需将下图文件添加在所示位置，并将SR7Link.dll和ImageConvert.dl添加到bin/Debug下

![代码简述](C:\Users\15600\Desktop\双镭射对射测厚框架V1.0\WindowsFormsApplication8\Typora图片\代码简述.png)

## 代码详解

On/Off
通过一个按钮控制设备的连接/关闭功能，通过判断 SRStatus 的值来判断是否连接成功，0为成功，非0为失败，同步更新设备连接状态颜色

```C#
#region 双镭射连接
/// <summary>
/// 连接镭射，更新连接状态
/// </summary>
private void ConnectedDevice()
{
    int SR1Status = camControl1.connect(SR1IP);
    int SR2Status = camControl2.connect_2(SR2IP);

    if (SR1Status == 0)
    {
        WriteTextSafe("SR1 Connected Success");
        label1.BackColor = Color.Green;
        label2.BackColor = Color.Green;
        label6.BackColor = Color.Green;

        //初始化一次回调
        camControl1.DataOnetimeCallBack();

        Task getSR1Para = GetSR1Para();
    }
    else
    {
        WriteTextSafe("SR1 Connected Faile");
    }

    if (SR2Status == 0)
    {
        WriteTextSafe("SR2 Connected Success");
        label3.BackColor = Color.Green;
        label4.BackColor = Color.Green;
        label10.BackColor = Color.Green;

        //一次回调
        camControl2.DataOnetimeCallBack_2();

        Task getSR2Para = GetSR2Para();
    }
    else
    {
        WriteTextSafe("SR2 Connected Faile");
    }

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
```

Start

通过判断 isStartProcess 的BOOL值来判断当前是开始采集数据还是停止采集，默认为False；如果第一次点击 Start 按钮则执行 else 语句循环执行采集数据、计算结果、保存数据和显示数据。此处使用 Task 来控制设备同时采集数据，等待设备采集完数据后开始调用计算函数 measureArray()，此时并没有使用Task，因为保存结果和显示结果数据需要等待计算出结果保存在数组中才能往下进行

```C#
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

                    await Task.WhenAll(SR1StartProc, SR2StartProc);

                    measureArray();

                    Task writeData = WriteArrayToCSV();
                    Task displayData = DisplayChart();

                    await Task.WhenAll(writeData, displayData);
                    await Task.Delay(Convert.ToInt16(textBox1.Text.ToString()));

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
            WriteTextSafe("镭射连上了嘛你就开始？");
        }
    }
}
#endregion
```

measureArray

由于当前使用深视SR9040镭射连续触发模式采集数据，每次最少采集50行数据，镭射线宽6400个点，但是数据量太大，所以仅取了第一个6400个点的深度值出来进行计算；为了降低计算量，6400个点开头和结尾的2000个点舍弃，中间的2400个点每200个点取出一个参与计算，最终输出12个点结果

```C#
#region 计算高度差
/// <summary>
/// 计算上下镭射高度差值
/// </summary>
private void measureArray()
{
    int[] SR1NewArray = new int[2400]; 
    Array.Copy(SR1Array, 1999, SR1NewArray, 0, 2400);

    int[] SR2NewArray = new int[4400];
    Array.Copy(SR2Array, 1999, SR2NewArray, 0, 2400);

    int selectIndex = 0;
    for (int i = 0; i < 2400; i+= 200)
    {
        SubResultData[selectIndex] = (SR1NewArray[i] - SR2NewArray[i]) * 0.00001;
        selectIndex++;
    }
}
#endregion
```

DisplayChart

此处使用Winform控件中的Chart显示折线图数据，为了让折线图显示在图表的最中间区域，根据数据结果的最大最小值计算出Y轴的最大刻度和最小刻度，根据从Configure.txt中读取的允许波动范围最大最小值以横线的形式显示在图表中(Chart图表背景颜色自定义功能也预留出来可自定义)

```C#
#region 显示获取到的数组
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

        //获取客户允许最大、最小波动范围
        double maxValue = Convert.ToDouble(textBox2.Text.ToString());
        double minValue = Convert.ToDouble(textBox3.Text.ToString());

        //添加最大值和最小值标记
        StripLine maxStripLine = new StripLine();
        maxStripLine.Text = string.Format("Z Max Value：{0:F}", maxValue);
        //maxStripLine.Interval = 0;
        //根据数据波动范围将折线图设置坐标轴Y方向最大值
        maxStripLine.StripWidth = (chart1.ChartAreas[0].AxisY.Maximum - chart1.ChartAreas[0].AxisY.Minimum) * 0.01f;
        maxStripLine.BackColor = Color.Red;
        maxStripLine.IntervalOffset = maxValue;
        chart1.ChartAreas[0].AxisY.StripLines.Add(maxStripLine);

        StripLine minStripLine = new StripLine();
        minStripLine.Text = string.Format("Z Min Value：{0:F}", minValue);
        //minStripLine.Interval = 0;
        //根据数据波动范围将折线图设置坐标轴Y方向最小值
        minStripLine.StripWidth = (chart1.ChartAreas[0].AxisY.Maximum - chart1.ChartAreas[0].AxisY.Minimum) * 0.01f; 
        minStripLine.BackColor = Color.Red;
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
```

WriteTextSafe

打印日志功能只需要将需要打印在文本框中的string类型变量作为实参传给函数即可

```C#
#region 打印日志就调它
/// <summary>
/// 打印日志
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
```

# 更新记录

## 2024/1/12

1. 更新计算高度差值的算法
2. 添加部分代码注释

```C#
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
    int getLineNum = 5;//取几行采样数据算均值

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
```



