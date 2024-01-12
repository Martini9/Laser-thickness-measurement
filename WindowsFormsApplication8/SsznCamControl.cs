using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsFormsApplication8;
namespace Sszn
{
    public class SsznCamControl
    {
        #region 字段

        private int m_ID;//控制器序号
        private int m_Connected = 0;    //控制器连接标志   m_Connected=1连接
        private int m_bSnap = 0;//批处理开始标志 m_bSnap=1Start
        private int m_BatchFinish = 0;//记录扫描图像是否完成 0 unfinish 1 finish callback

        private string m_IP;//控制器IP地址
        private int m_DataCallBackMode = 0;  //数据回调模式

        private int m_Height = 0;//扫描行数
        private int m_Width = 0;//图像宽度
        private double m_xInterval;//点间距

        private int[][] m_HeightData = new int[2][] { null, null };       //高度数据缓存
        private byte[][] m_GrayData = new byte[2][] { null, null };       //灰度数据缓存
        private int[][] m_EncoderData = new int[2][] { null, null };      //编码器数据缓存

        private SR7IF_BatchOneTimeCallBack batchOneTimeCallBack; //一次回调

        private int m_cameraNum=1;//3D头个数

        private uint m_BatchRollProfilePoint = 0;    //无限循环采集行数（0：无终止循环。≥15000：设定终止行数，其他无效）
        private uint m_LoopReflushCount = 2000;//无限循环模式刷新行频
        private uint m_LoopProfilePoint = 20000;//无限循环模式显示行数
        #endregion

        #region 属性
        public uint LoopProfilePoint
        {
            get
            {
                return m_LoopProfilePoint;
            }
            set
            {
                m_LoopProfilePoint = value;
            }
        }
        public uint BatchRollProfilePoint
        {
            get
            {
                return m_BatchRollProfilePoint;
            }
            set
            {
                m_BatchRollProfilePoint = value;
            }
        }
        public uint LoopReflushCount
        {
            get
            {
                return m_LoopReflushCount;
            }
            set
            {
                m_LoopReflushCount = value;
            }
        }
        public int cameraNum
        {
            get
            {
               return m_cameraNum;
            }
            set
            {
                m_cameraNum = value;
            }
        }
        public int ID
        {
            get
            {
                return m_ID;
            }
            set
            {
                m_ID = value;
            }
        }
        public int Connected
        {
            get
            {
                return m_Connected;
            }
            set
            {
                m_Connected = value;
            }
        }
        public int bSnap
        {
            get
            {
                return m_bSnap;
            }
        }

        public int BatchFinish
        {
            get
            {
                return m_BatchFinish;
            }
        }
        public int DataCallBackMode
        {
            get
            {
                return m_DataCallBackMode;
            }
            set
            {
                m_DataCallBackMode = value;
            }
        }

        public int Height
        {
            get
            {
                return m_Height;
            }
            set
            {
                m_Height = value;
            }
        }
        public int Width
        {
            get
            {
                return m_Width;
            }
            set
            {
                m_Width = value;
            }
        }

        public double xInterval
        {
            get
            {
                return m_xInterval;
            }
            set
            {
                m_xInterval = value;
            }
        }
        public int[][] HeightData
        {
            get
            {
                return m_HeightData;
            }
        }
        public byte[][] GrayData
        {
            get
            {
                return m_GrayData;
            }
        }

        public int[][] EncoderData
        {
            get
            {
                return m_EncoderData;
            }
        }
        #endregion

        #region 回调接口
        public delegate void RefreshImageData(bool topmost);//图像数据刷新回调
        public event RefreshImageData _ShowImage;//图像数据刷新回调

        public event RefreshImageData _ShowImageLoop;//无线循环模式下图像刷新

        public delegate void ShowInfo(string info1);//控制器运行信息委托事件
        public event ShowInfo ShowInfo1;//   控制器运行信息委托事件             
        public event ShowInfo ShowNum1;//   无线循环模式下显示行数 
        #endregion

        #region  构造函数
        /// <summary>
        /// 控制器创建
        /// </summary>
        /// <param name="_id">控制器序号</param>
        public SsznCamControl(int _id)
        {
            m_ID = _id;

        }

        #endregion

        #region 控制器操作方法函数

        /// <summary>
        /// 获取在线设备的IP地址列表
        /// </summary>
        /// <param name="DeviceCount">查找到控制器数量</param>
        /// <param name="IpAddressList1">IP地址列表: IpAddressList1[0]代表第一个IP,IpAddressList1[0]的长度为4,对应IP地址的四个数字</param>
        /// <returns></returns>
        public int GetIPAddressList(out int DeviceCount, out byte[][] IpAddressList1)
        {
            DeviceCount = 0;
            IpAddressList1 = null;
            int[] readNum = new int[1];
            try
            {
                using (PinnedObject pin = new PinnedObject(readNum))       //内存自动释放接口
                {
                    IntPtr str_Setting = SR7LinkFunc.SR7IF_SearchOnline(pin.Pointer, 1000);

                    if (readNum[0] > 0)//返回值为NULL
                    {
                        DeviceCount = readNum[0];
                        IpAddressList1 = new byte[DeviceCount][];

                        for (int i = 0; i < readNum[0]; i++)
                        {
                            byte[] ip = new byte[4];
                            Marshal.Copy(str_Setting+4*i, ip,  0, 4);
                            IpAddressList1[i] = ip;
                        }
                    }
                }

                return 0;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return -1;
            }
        }


        /// <summary>
        /// 连接控制器
        /// </summary>
        /// <param name="ip">sample 192.168.0.10</param>
        /// <returns></returns>
        public int connect(string _IP)
        {
            try
            {
                this.m_IP = _IP;
                SR7IF_ETHERNET_CONFIG _ethernetConfig;

                string[] ipTmp = m_IP.Split('.');
                if (ipTmp.Length != 4)
                {
                    ShowInfo1("控制器" + m_ID.ToString() + "IP设置错误.");
                    return -2;
                }

                _ethernetConfig.abyIpAddress = new Byte[]
                {
                    Convert.ToByte(ipTmp[0]),
                    Convert.ToByte(ipTmp[1]),
                    Convert.ToByte(ipTmp[2]),
                    Convert.ToByte(ipTmp[3])
                };
                int errO = SR7LinkFunc.SR7IF_EthernetOpen((int)m_ID, ref _ethernetConfig);

                if (errO != 0)
                {
                    ShowInfo1("控制器" + m_ID.ToString() + "连接失败，返回值:"+errO.ToString());
                    return errO;
                }
                else
                {
                    m_Connected = 1;//连接成功
                    m_xInterval = SR7LinkFunc.SR7IF_ProfileData_XPitch(m_ID, new IntPtr());
                    m_Width = SR7LinkFunc.SR7IF_ProfileDataWidth(m_ID, new IntPtr());
                    m_Height= SR7LinkFunc.SR7IF_ProfilePointSetCount(m_ID, new IntPtr());
                    m_cameraNum = getCamBOnline();
                    IntiData(m_Height, m_Width);

                    ShowInfo1("控制器" + m_ID.ToString() + "连接成功.");
                }

                if (m_DataCallBackMode == 0)
                {
                    //注册取图完成回调函数
                    batchOneTimeCallBack = new SR7IF_BatchOneTimeCallBack(BatchOneTimeCallBack);
                    int reT = SR7LinkFunc.SR7IF_SetBatchOneTimeDataHandler((int)m_ID, batchOneTimeCallBack);
                    if (reT != 0)
                    {
                        return reT;
                    }
                }

            }
            catch (Exception e)
            {
                ShowInfo1("控制器" + m_ID.ToString() + "连接失败." + e.ToString());
                return -1;
            }

            return 0;

        }

        public int connect_2(string _IP)
        {
            try
            {
                this.m_IP = _IP;
                SR7IF_ETHERNET_CONFIG _ethernetConfig;

                string[] ipTmp = m_IP.Split('.');
                if (ipTmp.Length != 4)
                {
                    ShowInfo1("控制器" + m_ID.ToString() + "IP设置错误.");
                    return -2;
                }

                _ethernetConfig.abyIpAddress = new Byte[]
                {
                    Convert.ToByte(ipTmp[0]),
                    Convert.ToByte(ipTmp[1]),
                    Convert.ToByte(ipTmp[2]),
                    Convert.ToByte(ipTmp[3])
                };
                int errO = SR7LinkFunc.SR7IF_EthernetOpen((int)m_ID, ref _ethernetConfig);

                if (errO != 0)
                {
                    ShowInfo1("控制器" + m_ID.ToString() + "连接失败，返回值:" + errO.ToString());
                    return errO;
                }
                else
                {
                    m_Connected = 1;//连接成功
                    m_xInterval = SR7LinkFunc.SR7IF_ProfileData_XPitch(m_ID, new IntPtr());
                    m_Width = SR7LinkFunc.SR7IF_ProfileDataWidth(m_ID, new IntPtr());
                    m_Height = SR7LinkFunc.SR7IF_ProfilePointSetCount(m_ID, new IntPtr());
                    m_cameraNum = getCamBOnline();
                    IntiData(m_Height, m_Width);

                    ShowInfo1("控制器" + m_ID.ToString() + "连接成功.");
                }

                if (m_DataCallBackMode == 0)
                {
                    //注册取图完成回调函数
                    batchOneTimeCallBack = new SR7IF_BatchOneTimeCallBack(BatchOneTimeCallBack);
                    int reT = SR7LinkFunc.SR7IF_SetBatchOneTimeDataHandler((int)m_ID, batchOneTimeCallBack);
                    if (reT != 0)
                    {
                        return reT;
                    }
                }

            }
            catch (Exception e)
            {
                ShowInfo1("控制器" + m_ID.ToString() + "连接失败." + e.ToString());
                return -1;
            }

            return 0;

        }


        /// <summary>
        /// 连接断开
        /// </summary>
        /// <returns></returns>
        public int disconnect()
        {
            if (m_Connected == 1)
            {
                try
                {
                    //建立连接 SR7IF_CommClose 断开连接
                    int errC = SR7LinkFunc.SR7IF_CommClose(m_ID);
                    if (0 != errC)
                    {
                        ShowInfo1("控制器" + m_ID.ToString() + "断开失败,返回值："+ errC.ToString());
                        return errC;
                    }
                    else
                    {
                        m_Connected = 0;
                        ShowInfo1("控制器" + m_ID.ToString() + "断开成功");
                    }
                }
                catch (Exception ex)
                {
                    ShowInfo1("控制器" + m_ID.ToString()+ "断开失败" + ex.Message);
                    return -1;
                }
            }
            else
            {
                ShowInfo1("控制器" + m_ID.ToString()+"控制器未连接");
            }

            return 0;

        }

        /// <summary>
        /// 初始化m_HeightData，m_GrayData，m_EncoderData
        /// </summary>
        /// <param name="Height"></param>
        /// <param name="Width"></param>
        /// <returns></returns>
        public int IntiData(int Height, int Width)
        {
            try
            {

                for (int index = 0; index < m_cameraNum; index++)
                {
                    //修改
                    m_HeightData[index] = new int[Height * Width];
                    m_GrayData[index] = new byte[Height * Width];
                    m_EncoderData[index] = new int[Height];

                    for (int i = 0; i < m_HeightData[index].Length; i++)
                    {
                        m_HeightData[index][i] = -1000000000;
                    }

                    for (int i = 0; i < m_GrayData[index].Length; i++)
                    {
                        m_GrayData[index][i] = byte.MinValue;
                    }

                    for (int i = 0; i < m_EncoderData[index].Length; i++)
                    {
                        m_EncoderData[index][i] = int.MinValue;
                    }

                }
                return 0;
            }
            catch (Exception e)
            {
                ShowInfo1("控制器" + m_ID.ToString() + "内存申请失败 " + e.ToString());
                return -1;
            }
        }


        /// <summary>
        /// 一次回调模式下：开始批处理，即接收软触发,也接收硬触发
        /// </summary>
        /// <returns></returns>
        public int startOneTimeSnap()
        {
            try
            {

                int errS = SR7LinkFunc.SR7IF_StartMeasureWithCallback(m_ID, 1);
                if (errS != 0)
                {
                    ShowInfo1("控制器" + m_ID.ToString()+ "回调模式开始批处理失败,返回值："+errS.ToString());
                    return errS;
                }
                else
                {
                    ShowInfo1("控制器" + m_ID.ToString() + "回调模式开始批处理成功");
                    m_bSnap = 1;//批处理开启成功
                }

            }
            catch (Exception ex)
            {
                ShowInfo1("控制器" + m_ID.ToString()+ "回调模式开始批处理失败" + ex.Message);
                return -1;
            }

            return 0;

        }

        /// <summary>
        /// 停止批处理
        /// </summary>
        /// <returns></returns>
        public int StopMeasure()
        {
            if (m_bSnap == 1)
            {
                try
                {
                    int errS = SR7LinkFunc.SR7IF_StopMeasure(m_ID);

                    if (errS != 0)
                    {
                        ShowInfo1("控制器" + m_ID.ToString()+"停止批处理 失败,返回值："+errS.ToString());
                        return errS;
                    }
                    else
                    {
                        ShowInfo1("控制器" + m_ID.ToString() + "停止批处理 成功");
                        m_bSnap = 0;
                    }
                }
                catch (Exception ex)
                {
                    ShowInfo1("控制器" + m_ID.ToString()+ "停止批处理 失败." + ex.Message);
                    return -1;
                }
            }


            return 0;
        }

        /// <summary>
        /// 一次回调模式下软触发一次
        /// </summary>
        /// <returns></returns>
        public int softTrigOne()
        {
            int errT = SR7LinkFunc.SR7IF_TriggerOneBatch(m_ID);

            if (errT != 0)
            {
                ShowInfo1("控制器" + m_ID.ToString()+"软触发失败,返回值："+errT.ToString());
                return errT;
            }
            else
            {
                ShowInfo1("控制器1" + m_ID.ToString() + "软触发完成" + errT.ToString());
                // GrayDataSave();
            }
            m_BatchFinish = 0;
            return 0;
        }

        public int softTrigOne_2()
        {
            int errT = SR7LinkFunc.SR7IF_TriggerOneBatch(m_ID);

            if (errT != 0)
            {
                ShowInfo1("控制器" + m_ID.ToString() + "软触发失败,返回值：" + errT.ToString());
                return errT;
            }
            else
            {
                ShowInfo1("控制器2" + m_ID.ToString() + "软触发完成" + errT.ToString());
                // GrayDataSave();
            }
            m_BatchFinish = 0;
            return 0;
        }

        public void GrayDataSave()
        {
            try
            {
                if (this .GrayData[0] == null)
                {
                    return;
                }
                string table_file = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + "\\" + System.DateTime.Now.ToString("yyyy-MM-dd");
                if (!Directory.Exists(table_file))
                {
                    Directory.CreateDirectory(table_file); //创建桌面文件夹
                }

                string save_file = table_file + "\\" + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ".bmp";
                //GrayDataSaveDlg.Filter = "图像文件 (*.bmp)|*.bmp";

                //if (GrayDataSaveDlg.ShowDialog() == DialogResult.OK)
                {
                    //String fileEx = System.IO.Path.GetExtension(GrayDataSaveDlg.FileName);    //获取扩展名   

                    //if (fileEx == ".bmp")
                    {

                        int imgW = this.Width;
                        int imgH = this.Height;

                        Bitmap tmpBmp = null;
                        this.data2Bmp2(this.GrayData[0], imgW, imgH, ref tmpBmp);

                        tmpBmp.Save(save_file, System.Drawing.Imaging.ImageFormat.Bmp);

                        //  InfoText.AppendText("保存完成！");
                    }
                }
            }
            catch (Exception ex)
            {
             
                return;

            }

            return;
        }


        /// <summary>
        /// 一次回调模式获取图像，仅需调用一次
        /// </summary>
        /// <param name="_DataCallBackMode"></param>
        public int DataOnetimeCallBack()
        {

            //一次回调
            int err = startOneTimeSnap();
            if (err == 0)
            {
                GrayDataSave();
                //开启图像显示线程
                //Thread tmpThread = new Thread(ImageDisplayFunc);
                //tmpThread.Start();
                m_DataCallBackMode = 0;
                return 0;
            }
            else
            {
                return -1;
            }


        }

        public int DataOnetimeCallBack_2()
        {

            //一次回调
            int err = startOneTimeSnap();
            if (err == 0)
            {
                GrayDataSave();
                //开启图像显示线程
                //Thread tmpThread = new Thread(ImageDisplayFunc);
                //tmpThread.Start();
                m_DataCallBackMode = 0;
                return 0;
            }
            else
            {
                return -1;
            }


        }

        /// <summary>
        /// 阻塞模式获取图像，每获取一次图像需运行一次
        /// </summary>
        /// <param name="IOTiger">0软触发，1硬触发</param>
        /// <returns></returns>
        public int DataBlockReceived(int IOTiger)
        {
            //阻塞
            m_DataCallBackMode = 1;
            int err = startBlock(1);
            if (err == 0)
            {
                _ShowImage(true);
                GrayDataSave();
                return 0;
            }
            else
            {
                return -1;
            }

        }

        /// <summary>
        /// 无限循环模式下获取图像
        /// </summary>
        public int DataLoopReceived()
        {
            m_bSnap = 1;
            m_DataCallBackMode = 2;
            (new Thread(ThreadLoop)).Start();   //启动线程
            return 0;
        }

        /// <summary>
        /// 阻塞模式取图
        /// </summary>
        /// <param name="ioTrig">0软触发立即开始批处理，1 IO触发开始批处理</param>
        /// <returns></returns>
        public int startBlock(int ioTrig)
        {
            try
            {              
                int errS = (ioTrig == 0) ?
                SR7LinkFunc.SR7IF_StartMeasure(m_ID, 20000)
                :
                SR7LinkFunc.SR7IF_StartIOTriggerMeasure(m_ID, 20000, 0);
                if (errS != 0)
                {
                    ShowInfo1("控制器" + m_ID + "批处理开启失败.返回值：" + errS);
                    return errS; 
                }
                else
                {
                    ShowInfo1("控制器" + m_ID + "批处理开启成功");
                }

                // 接收数据
                IntPtr DataObject = new IntPtr();
                int errR = SR7LinkFunc.SR7IF_ReceiveData(m_ID, DataObject);


                if (errR != 0)
                {
                    ShowInfo1("控制器"+m_ID+"阻塞模式获取数据失败，返回值："+errR);
                    SR7LinkFunc.SR7IF_StopMeasure(m_ID);
                    return errR;
                }

                int iCamB = SR7LinkFunc.SR7IF_GetOnlineCameraB(m_ID);
                if(iCamB==1)
                {
                    m_Height = SR7LinkFunc.SR7IF_ProfilePointSetCount(m_ID, new IntPtr());
                    int[] _BatchData = new int[m_Height * m_Width];   //高度数据缓存
                    byte[] _GrayData = new byte[m_Height * m_Width];  //灰度数据缓存


                    using (PinnedObject pin = new PinnedObject(_BatchData))//内存自动释放接口
                    {
                        int Rc = SR7LinkFunc.SR7IF_GetProfileData(m_ID, new IntPtr(), pin.Pointer);   // pin.Pointer 获取高度数据缓存地址
                        ShowInfo1("高度数据获取" + ((Rc == 0) ? "成功" : "失败"));
                    }

                    // 获取亮度数据
                    using (PinnedObject pin = new PinnedObject(_GrayData))       //内存自动释放接口
                    {
                        int Rc = SR7LinkFunc.SR7IF_GetIntensityData(m_ID, new IntPtr(), pin.Pointer);
                        ShowInfo1("灰度数据获取" + ((Rc == 0) ? "成功" : "失败"));
                    }

                    //获取编码器值
                    using (PinnedObject pin = new PinnedObject(m_EncoderData[0]))       //内存自动释放接口
                    {
                        int Rc = SR7LinkFunc.SR7IF_GetEncoder(m_ID, new IntPtr(), pin.Pointer);
                        ShowInfo1("编码器数据获取" + ((Rc == 0) ? "成功" : "失败"));
                    }

                    for (int i = 0; i < m_Height; i++)
                    {
                        Array.Copy(_BatchData, (2 * i + 1) * m_Width / 2,
                                   m_HeightData[1], i * m_Width / 2, m_Width / 2);
                        Array.Copy(_BatchData, (2 * i) * m_Width / 2,
                                   m_HeightData[0], i * m_Width / 2, m_Width / 2);
                    }

                    for (int i = 0; i < m_Height; i++)
                    {
                        Array.Copy(_GrayData, (2 * i + 1) * m_Width / 2,
                                   m_GrayData[1], i * m_Width / 2, m_Width / 2);
                        Array.Copy(_GrayData, (2 * i) * m_Width / 2,
                                   m_GrayData[0], i * m_Width / 2, m_Width / 2);
                    }
                }
                else
                {
                    using (PinnedObject pin = new PinnedObject(m_HeightData[0]))//内存自动释放接口
                    {
                        int Rc = SR7LinkFunc.SR7IF_GetProfileData(m_ID, new IntPtr(), pin.Pointer);   // pin.Pointer 获取高度数据缓存地址
                        ShowInfo1("高度数据获取" + ((Rc == 0) ? "成功" : "失败"));
                    }

                    // 获取亮度数据
                    using (PinnedObject pin = new PinnedObject(m_GrayData[0]))       //内存自动释放接口
                    {
                        int Rc = SR7LinkFunc.SR7IF_GetIntensityData(m_ID, new IntPtr(), pin.Pointer);
                        ShowInfo1("灰度数据获取" + ((Rc == 0) ? "成功" : "失败"));
                    }
           
                    //获取编码器值
                    using (PinnedObject pin = new PinnedObject(m_EncoderData[0]))       //内存自动释放接口
                    {
                        int Rc = SR7LinkFunc.SR7IF_GetEncoder(m_ID, new IntPtr(), pin.Pointer);
                        ShowInfo1("编码器值获取" + ((Rc == 0) ? "成功" : "失败"));
                    }
                }

                
                //获取数据图像显示回调
                _ShowImage(true);

            }
            catch (Exception ex)
            {
                ShowInfo1( "控制器"+m_ID+ "阻塞模式获取数据失败" + ex.Message);
                return -1;
            }

            return 0;

        }

        /// <summary>
        /// 无限循环模式获取图像线程
        /// </summary>
   
        private void ThreadLoop()
        {

            int _curDevId = m_ID;

            IntPtr DataObj = new IntPtr();

            //设置无限循环终止行数，>= 15000生效
            if (m_BatchRollProfilePoint >= 15000)
            {
                int Ret = SR7LinkFunc.SR7IF_SetBatchRollProfilePoint(_curDevId, DataObj, m_BatchRollProfilePoint);
                if (Ret != 0)
                {
                    ShowInfo1("控制器" + m_ID + "无限循环终止行数设置 失败,返回值：" + Ret.ToString());
                    return;
                }
                else
                {
                    ShowInfo1("控制器" + m_ID + "无限循环终止行数设置 成功");
                }
            }
            
            if (m_BatchRollProfilePoint>= m_LoopProfilePoint)
            {
                IntiData((int)m_BatchRollProfilePoint, m_Width);//设置数据缓存
            }
            else
            {
                IntiData((int)m_LoopProfilePoint, m_Width);//设置数据缓存
            }

            // 开始批处理
            int Rc = -1;

            if (2 == 1)
                Rc = SR7LinkFunc.SR7IF_StartIOTriggerMeasure(_curDevId, 20000, 0);
            else
                Rc = SR7LinkFunc.SR7IF_StartMeasure(_curDevId, 20000);
            if (Rc != 0)
            {
                ShowInfo1("控制器" + m_ID + "批处理操作 失败,返回值：" + Rc.ToString());
                return;
            }
            else
            {
                ShowInfo1("控制器" + m_ID + "批处理操作 成功");
            }
          
            int LoopOneCount = 500;//调用一次 SR7IF_GetBatchRollData 接口返回的最大批处理点数
            int[] TmpHeightData = new int[2 * LoopOneCount * m_Width];       //当前批次高度数据缓存
            byte[] TmpGrayData = new byte[2 * LoopOneCount * m_Width];       //当前批次灰度数据缓存
            uint[] FrameLoss = new uint[2 * LoopOneCount];                       //批处理过快掉帧数量数据缓存
            long[] FrameId = new long[2 * LoopOneCount];                         //帧编号数据缓存
            uint[] Encoder = new uint[2 * LoopOneCount];                         //编码器数据缓存

            Int32 curNo = 0;                    //当前批处理编号
            int m_LastReflushCount = 0;         //距离上次图像刷新缓存行数
            long OverFlowStartId = 0;                                //溢出起始帧号
            uint FrameLossID = 0;                                    //丢帧数 
            uint EncoderID = 0;                                      //编码器值

            while (m_bSnap == 1)
            {
                //接收数据---当前批次高度数据、灰度数据、编码器数据、帧编号、掉帧数量数据
                using (PinnedObject pin_height = new PinnedObject(TmpHeightData))       //内存自动释放接
                {
                    using (PinnedObject pin_gray = new PinnedObject(TmpGrayData))
                    {
                        using (PinnedObject pin_encoder = new PinnedObject(Encoder))
                        {
                            using (PinnedObject pin_frameloss = new PinnedObject(FrameLoss))
                            {
                                using (PinnedObject pin_frameid = new PinnedObject(FrameId))
                                {
                                    int curBPt = SR7LinkFunc.SR7IF_GetBatchRollData(_curDevId,
                                        DataObj,
                                        pin_height.Pointer,
                                        pin_gray.Pointer,
                                        pin_encoder.Pointer,
                                        pin_frameid.Pointer,
                                        pin_frameloss.Pointer,
                                        LoopOneCount);


                                    if (curBPt == 0)
                                    {
                                        continue;
                                    }
                                    #region Err Proc 
                                    if (curBPt < 0)
                                    {
                                        if (curBPt == (int)SR7IF_ERROR.SR7IF_ERROR_MODE)
                                        {
                                            SR7LinkFunc.SR7IF_StopMeasure(_curDevId);
                                            ShowInfo1("控制器" + m_ID.ToString() + "当前为非循环模式,返回值：" + curBPt.ToString());
                                            break;
                                        }
                                        else if (curBPt == (int)SR7IF_ERROR.SR7IF_NORMAL_STOP)
                                        {
                                            m_bSnap = 0;
                                            SR7LinkFunc.SR7IF_StopMeasure(_curDevId);
                                            ShowInfo1("控制器" + m_ID.ToString() + "外部IO或其他因素导致批处理正常终止，返回值：" + curBPt.ToString());
                                            break;
                                        }
                                        else
                                        {
                                            //获取错误码
                                            int[] EthErrCnt = new int[1];
                                            int[] UserErrCnt = new int[1];
                                            using (PinnedObject pin_EthErrCnt = new PinnedObject(EthErrCnt))
                                            {
                                                using (PinnedObject pin_UserErrCnt = new PinnedObject(UserErrCnt))
                                                {
                                                    SR7LinkFunc.SR7IF_GetBatchRollError(_curDevId, pin_EthErrCnt.Pointer, pin_UserErrCnt.Pointer);
                                                }
                                            }
                                            if (curBPt == (int)SR7IF_ERROR.SR7IF_ERROR_ROLL_DATA_OVERFLOW)
                                            {
                                                String Err1 = "数据获取过慢，数据覆盖.网络原因溢出量：" + EthErrCnt[0].ToString();
                                                String Err2 = "，用户原因溢出量：" + UserErrCnt[0].ToString();
                                                String Err3 = "，溢出起始帧号：" + OverFlowStartId.ToString();
                                                String Err4 = "，丢帧数：" + FrameLossID.ToString();
                                                String Err5 = "，编码器值：" + EncoderID.ToString();
                                                String tmt = Err1 + Err2 + Err3 + Err4 + Err5 + "\r\n";
                                                String str_Err = tmt;
                                                ShowInfo1("控制器" + m_ID.ToString() + str_Err + ",返回值：" + curBPt.ToString());
                                            }
                                            else if (curBPt == (int)SR7IF_ERROR.SR7IF_ERROR_ROLL_BUSY)
                                            {
                                                String str_Err = "Busy" + "\r\n";
                                                ShowInfo1("控制器" + m_ID.ToString() + str_Err + ",返回值：" + curBPt.ToString());
                                            }
                                            EthErrCnt = null;
                                            UserErrCnt = null;
                                            GC.Collect();
                                            break;
                                        }

                                    }

                                    #endregion Err Proc

                                    int TmpID = curBPt - 1;
                                    OverFlowStartId = FrameId[TmpID];
                                    FrameLossID = FrameLoss[TmpID];
                                    EncoderID = Encoder[TmpID];
                                    int b_camBOnline = 2;
                                    if (b_camBOnline == 2)
                                    {
                                        FrameLossID = FrameLoss[TmpID * 2];  //双相机
                                        EncoderID = Encoder[TmpID * 2];      //双相机
                                    }
                                    if ((curNo + curBPt) > m_BatchRollProfilePoint)
                                    {
                                        curBPt -= (int)(m_BatchRollProfilePoint - curNo);
                                    }
                                    
                                    if (cameraNum==1)
                                    {
                                        Trace.WriteLine(string.Format("{0}", (m_LoopProfilePoint - curBPt)));
                                        Array.Copy(HeightData[0], curBPt * m_Width, HeightData[0], curNo* m_Width, (curBPt) * m_Width);
                                        //获取数据放到HeightData[0]
                                        Array.Copy(TmpHeightData, 0, HeightData[0], (m_LoopProfilePoint- curBPt) * m_Width, curBPt * m_Width);                                        
                                        
                                    }
                                    else
                                    {
                                        int[] TmpHeightDataA = new int[curBPt * m_Width / 2];
                                        int[] TmpHeightDataB = new int[curBPt * m_Width / 2];
                                        //拆分获取的数据分为AB头数据
                                        for (int i=0;i< curBPt;i++)
                                        {
                                            Array.Copy(TmpHeightData, i * m_Width, TmpHeightDataA, i * m_Width / 2, m_Width / 2);
                                            Array.Copy(TmpHeightData, m_Width/2+i * m_Width, TmpHeightDataB, i * m_Width / 2, m_Width / 2);
                                        }
                                        Array.Copy(TmpHeightDataA, 0, HeightData[0], curNo * m_Width/2, curBPt * m_Width/2);
                                        Array.Copy(TmpHeightDataB, 0, HeightData[0], curNo * m_Width-2, curBPt * m_Width/2);
                                    }


                                    //累计行数
                                    curNo += curBPt;
                                    m_LastReflushCount += curBPt;
                                    ShowNum1(curNo.ToString());
                                    //满足条件结束循环
                                    if (m_BatchRollProfilePoint != 0 && curNo >= m_BatchRollProfilePoint)
                                    {
                                        StopMeasure();
                                        m_bSnap = 0;
                                        _ShowImageLoop(true);//循环结束回调
                                    }
                                    //图像间隔刷新
                                    else if(m_LastReflushCount>= m_LoopReflushCount)
                                    {
                                        m_LastReflushCount = 0;
                                        _ShowImageLoop(true);
                                    }                                                                   
                                }
                            }
                        }
                    }
                }
                Thread.Sleep(100);
            }

            //内存释放
            TmpHeightData = null;
            TmpGrayData = null;
            FrameLoss = null;
            Encoder = null;
            FrameId = null;
            GC.Collect();
        }

        /// <summary>
        /// 获取当前控制器相机在线个数
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        public int getCamBOnline()
        {
            int  num = (SR7LinkFunc.SR7IF_GetOnlineCameraB(m_ID) == 0) ? 2 : 1;
            return num;
        }


        /// <summary>
        /// 一次回调模式下图像获取线程
        /// </summary>
        private void ImageDisplayFunc()
        {
            try
            {

                int camNum = getCamBOnline();
                while (m_bSnap == 1)
                {
                    if (m_BatchFinish == 1)
                    {
                        m_Height = SR7LinkFunc.SR7IF_ProfilePointSetCount(m_ID, new IntPtr());
                        m_BatchFinish = 0;
                        GrayDataSave();
                        //获取数据图像显示回调
                        _ShowImage(true);
                    }
                    Thread.Sleep(100);
                }

            }
            catch (Exception ex)
            {
                return;
            }

        }


        /// <summary>
        /// 一次回调函数
        /// </summary>
        /// <param name="info"></param>
        /// <param name="data"></param>
        public void BatchOneTimeCallBack(IntPtr info, IntPtr data)
        {

            ShowInfo1("BatchOneTimeCallBack!");

            SR7IF_STR_CALLBACK_INFO coninfo = new SR7IF_STR_CALLBACK_INFO();
            coninfo = (SR7IF_STR_CALLBACK_INFO)Marshal.PtrToStructure(info, typeof(SR7IF_STR_CALLBACK_INFO));

            if (coninfo.returnStatus == -100)
                return ;

            int mBatchPoint = coninfo.BatchPoints;
            int mBatchWidth = coninfo.xPoints;

            IntPtr[] mTmpData = new IntPtr[2];
            IntPtr[] mTmpGraydata = new IntPtr[2];
            IntPtr[] mTmpEncoderdata = new IntPtr[2];

            for (int index = 0; index < coninfo.HeadNumber; index++)
            {
                mTmpData[index] = SR7LinkFunc.SR7IF_GetBatchProfilePoint(data, index);
                mTmpGraydata[index] = SR7LinkFunc.SR7IF_GetBatchIntensityPoint(data, index);
                mTmpEncoderdata[index] = SR7LinkFunc.SR7IF_GetBatchEncoderPoint(data, index);

                if (mTmpData[index] != IntPtr.Zero)
                {
                    Marshal.Copy(mTmpData[index], m_HeightData[index], 0, mBatchPoint * mBatchWidth);
                }
                else
                {
                    ShowInfo1("内存不足,相机头A高度数据获取失败");
                }

                if (mTmpGraydata[index] != IntPtr.Zero)
                {
                    Marshal.Copy(mTmpGraydata[index], m_GrayData[index], 0, mBatchPoint * mBatchWidth);
                }
                else
                {
                    ShowInfo1("内存不足,相机头A灰度数据获取失败");
                }

                if (mTmpEncoderdata[index] != IntPtr.Zero)
                {
                    Marshal.Copy(mTmpEncoderdata[index], m_EncoderData[index], 0, mBatchPoint);
                }
                else
                {
                    ShowInfo1("内存不足,相机头A编码器获取失败");
                }

            }

            GC.Collect();
            m_BatchFinish = 1;


        }

        public void BatchOneTimeCallBack_2(IntPtr info, IntPtr data)
        {

            ShowInfo1("BatchOneTimeCallBack!");

            SR7IF_STR_CALLBACK_INFO coninfo = new SR7IF_STR_CALLBACK_INFO();
            coninfo = (SR7IF_STR_CALLBACK_INFO)Marshal.PtrToStructure(info, typeof(SR7IF_STR_CALLBACK_INFO));

            if (coninfo.returnStatus == -100)
                return;

            int mBatchPoint = coninfo.BatchPoints;
            int mBatchWidth = coninfo.xPoints;

            IntPtr[] mTmpData = new IntPtr[2];
            IntPtr[] mTmpGraydata = new IntPtr[2];
            IntPtr[] mTmpEncoderdata = new IntPtr[2];

            for (int index = 0; index < coninfo.HeadNumber; index++)
            {
                mTmpData[index] = SR7LinkFunc.SR7IF_GetBatchProfilePoint(data, index);
                mTmpGraydata[index] = SR7LinkFunc.SR7IF_GetBatchIntensityPoint(data, index);
                mTmpEncoderdata[index] = SR7LinkFunc.SR7IF_GetBatchEncoderPoint(data, index);

                if (mTmpData[index] != IntPtr.Zero)
                {
                    Marshal.Copy(mTmpData[index], m_HeightData[index], 0, mBatchPoint * mBatchWidth);
                }
                else
                {
                    ShowInfo1("内存不足,相机头A高度数据获取失败");
                }

                if (mTmpGraydata[index] != IntPtr.Zero)
                {
                    Marshal.Copy(mTmpGraydata[index], m_GrayData[index], 0, mBatchPoint * mBatchWidth);
                }
                else
                {
                    ShowInfo1("内存不足,相机头A灰度数据获取失败");
                }

                if (mTmpEncoderdata[index] != IntPtr.Zero)
                {
                    Marshal.Copy(mTmpEncoderdata[index], m_EncoderData[index], 0, mBatchPoint);
                }
                else
                {
                    ShowInfo1("内存不足,相机头A编码器获取失败");
                }

            }

            GC.Collect();
            m_BatchFinish = 1;


        }
        /// <summary>
        /// 获取一条轮廓
        /// </summary>
        /// <param name="ProfData">轮廓数据</param>
        /// <param name="EncoderData">编码器数据</param>
        /// <returns></returns>
        public int GetProfile(out int[] ProfData,out uint [] EncoderData)
        {
            int m_EncoderNum=getCamBOnline();

            EncoderData = new uint[m_EncoderNum];

            ProfData = new int[m_Width];

            using (PinnedObject pin_Profile = new PinnedObject(ProfData))
            {
                using (PinnedObject pin_Encoder = new PinnedObject(EncoderData))
                {
                    int Rc = SR7LinkFunc.SR7IF_GetSingleProfile((uint)m_ID, pin_Profile.Pointer, pin_Encoder.Pointer);

                    if (Rc != 0)
                    {
                        ShowInfo1("控制器" + m_ID.ToString() + "获取当前一条轮廓失败，返回值:" + Rc.ToString());
                        return -1;
                    }
                    else
                    {
                        return 0;
                    }
                }

            }

        }

        public int GetProfile_2(out int[] ProfData, out uint[] EncoderData)
        {
            int m_EncoderNum = getCamBOnline();

            EncoderData = new uint[m_EncoderNum];

            ProfData = new int[m_Width];

            using (PinnedObject pin_Profile = new PinnedObject(ProfData))
            {
                using (PinnedObject pin_Encoder = new PinnedObject(EncoderData))
                {
                    int Rc = SR7LinkFunc.SR7IF_GetSingleProfile((uint)m_ID, pin_Profile.Pointer, pin_Encoder.Pointer);

                    if (Rc != 0)
                    {
                        ShowInfo1("控制器" + m_ID.ToString() + "获取当前一条轮廓失败，返回值:" + Rc.ToString());
                        return -1;
                    }
                    else
                    {
                        return 0;
                    }
                }

            }

        }
        #endregion

        #region 参数设置方法
        /// <summary>
        /// 设置参数值
        /// </summary>
        /// <param name="iAB">指定修改目标，参数需要区分相机AB时按照相机A为0，B为1区分，否则默认设置0</param>
        /// <param name="configNum">配方编号</param>
        /// <param name="SupportItem">参数项枚举</param>
        /// <param name="num">设置数据</param>
        /// <returns></returns>
        public int setParam(int iAB, int configNum, int SupportItem, int num)
        {
            try
            {
                int depth = 0x02;//1 掉电不保存;2 掉电保存

                int Category = 0x00;//不同页面 0 1 2 
                int Item = 0x01;
                int[] tar = new int[4] { iAB, 0, 0, 0 };

                int DataSize = 0;
                int errT = TransCategory(SupportItem, out Category, out Item, out DataSize);

                if (errT != 0)
                {
                    ShowInfo1("控制器" + m_ID.ToString() + "TransCategory Err.");
                    return -1;
                }

                byte[] pData = null;
                int errN = TransNum(num, DataSize, ref pData);
                if (errN != 0)
                {
                    ShowInfo1 ("控制器" + m_ID.ToString() + "TransNum Err" );
                    return -1;
                }

                using (PinnedObject pin = new PinnedObject(pData))
                {
                    int errS = SR7LinkFunc.SR7IF_SetSetting((uint)m_ID, depth, 15 + configNum,
                        Category, Item, tar, pin.Pointer, DataSize);
                    if (errS != 0)
                    {
                        ShowInfo1("控制器" + m_ID.ToString() + "设置参数错误，返回值：" + errS.ToString());
                        return -1;
                    }
                    else
                    {
                        ShowInfo1("控制器" + m_ID.ToString() + "设置参数成功");
                    }

                }

            }
            catch (Exception ex)
            {
                ShowInfo1("控制器" + m_ID.ToString() + "setParam Err. " + ex.Message);
                return -1;
            }

            return 0;

        }


        /// <summary>
        /// 参数设置，具体参考通讯库参考手册
        /// </summary>
        /// <param name="_DeviceId">控制器ID</param>
        /// <param name="Depth">参数是否断电保存，0x01断电不保存，0x02断电保存</param>
        /// <param name="FormulaNum">-1：返回当前配方的设定；16 至 79：配方 0 至配方 63 中的哪一项设定</param>
        /// <param name="Category">指定参数页，0：触发设定页面；1：拍摄设定页面；2：轮廓设定页面</param>
        /// <param name="Item">指定发送 / 接收 Category 指定项目中的哪一项设定</param>
        /// <param name="Target">指定修改目标，参数需要区分相机AB时按照相机A为0，B为1区分，否则默认设置0</param>
        /// <param name="pData">写入的具体数据</param>
        /// <param name="DataSize">写入数据的长度</param>
        /// <returns></returns>
        public int SetParams( int _DeviceId, int Depth, int FormulaNum, int Category, int Item,int Target, PinnedObject pData, int DataSize)
        {
            int[] tar = new int[4] { Target, 0, 0, 0 };
            int errS = SR7LinkFunc.SR7IF_SetSetting((uint)_DeviceId, Depth, FormulaNum,Category, Item, tar, pData.Pointer, DataSize);
            if (errS != 0)
            {
                ShowInfo1("控制器" + _DeviceId.ToString() + "设置参数失败，返回值：" + errS.ToString());
                return -1;

            }
            else
            {
                ShowInfo1("控制器" + _DeviceId.ToString() + "设置参数成功，返回值：" + errS.ToString());
                return 0;
            }
            
        }

        /// <summary>
        /// 获取参数值
        /// </summary>
        /// <param name="iAB">指定修改目标，参数需要区分相机AB时按照相机A为0，B为1区分，否则默认设置0</param>
        /// <param name="configNum">配方编号</param>
        /// <param name="SupportItem">枚举值</param>
        /// <param name="num">返回参数值</param>
        /// <returns></returns>
        public int getParam(int iAB, int configNum, int SupportItem, out int num)
        {
            num = 0;

            try
            {

                if (SupportItem <= 0x3000)
                {

                    int Category = 0x00;//不同页面 0 1 2 
                    int Item = 0x01;
                    int[] tar = new int[4] { iAB, 0, 0, 0 };

                    int DataSize = 1;

                    int errT = TransCategory(SupportItem, out Category, out Item, out DataSize);

                    if (0 != errT)
                    {
                        ShowInfo1("TransCategory Err.");
                        return errT;
                    }

                    byte[] pData = new byte[DataSize];

                    using (PinnedObject pin = new PinnedObject(pData))
                    {

                        int errG = SR7LinkFunc.SR7IF_GetSetting((uint)m_ID, 15 + configNum,
                             Category, Item, tar, pin.Pointer, DataSize);

                        if (0 != errG)
                        {
                            ShowInfo1("控制器"+m_ID.ToString()+"获取参数失败，返回值："+errG.ToString());
                            return errG;
                        }

                        StringBuilder sb = new StringBuilder();

                        //Get Data
                        for (int i = 0; i < pData.Length; i++)
                        {
                            num += pData[i] * (int)Math.Pow(256, i);
                        }

                    }

                }
                else if (SupportItem <= 0x4000 && SupportItem > 0x3000)
                {
                    switch (SupportItem)
                    {
                        //X数据宽度(单位像素)
                        case (int)SR7IF_SETTING_ITEM.X_PIXEL:
                            int camNum = SR7LinkFunc.SR7IF_GetOnlineCameraB(m_ID) == 0 ? 2 : 1;
                            num = SR7LinkFunc.SR7IF_ProfileDataWidth(m_ID, new IntPtr()) / camNum;

                            break;
                        default:
                            return -3;
                    }

                }
            }
            catch (Exception ex)
            {
                
                ShowInfo1("getParam Err." + ex.Message);
                return -1;
            }

            return 0;

        }


        /// <summary>
        /// 返回指定端口0 - 7 输入电平，0/False 低 1/True 高
        /// </summary>
        /// <param name="port"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public int GetIo(uint port, out bool level)
        {
            level = false;

            try
            {
                using (PinnedObject pin = new PinnedObject(level))
                {
                    int errG = SR7LinkFunc.SR7IF_GetInputPortLevel((uint)m_ID, port, pin.Pointer);

                    if (0 != errG)
                    {
                        return errG;
                    }
                }
            }
            catch (Exception ex)
            {
                ShowInfo1("GetIo:" + ex.Message);
                return -1;
            }

            return 0;

        }
        /// <summary>
        /// 设置IO开关
        /// </summary>
        /// <param name="port"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public int SetIo(uint port, bool level)
        {
            try
            {
                int errS = SR7LinkFunc.SR7IF_SetOutputPortLevel((uint)m_ID, port, level);

                if (0 != errS)
                {
                    return errS;
                }
            }
            catch (Exception ex)
            {
                ShowInfo1("SetIo:" + ex.Message);
                return -1;
            }

            return 0;

        }

       

        private int TransNum(int num, int DataSize, ref byte[] byteNum)
        {
            try
            {
                byteNum = new byte[DataSize];

                for (int i = 0; i < DataSize; i++)
                {
                    byteNum[i] = (byte)((num >> (i * 8)) & 0xFF);
                }
            }
            catch (Exception ex)
            {
                
                ShowInfo1("TransNum Err. " + ex.Message);
                return -1;
            }

            return 0;

        }

        private int TransCategory(int SupportItem, out int Category, out int Item, out int DataSize)
        {
            Category = SupportItem / 256;
            Item = SupportItem % 256;
            DataSize = 1;

            try
            {

                switch (SupportItem)
                {
                    //触发模式
                    case (int)SR7IF_SETTING_ITEM.TRIG_MODE:
                        DataSize = 1;
                        break;

                    //采样周期
                    case (int)SR7IF_SETTING_ITEM.SAMPLED_CYCLE:
                        DataSize = 4;
                        break;

                    //批处理开关
                    case (int)SR7IF_SETTING_ITEM.BATCH_ON_OFF:
                        DataSize = 1;
                        break;

                    //编码器类型
                    case (int)SR7IF_SETTING_ITEM.ENCODER_TYPE:
                        DataSize = 1;
                        break;

                    //细化点数
                    case (int)SR7IF_SETTING_ITEM.REFINING_POINTS:
                        DataSize = 2;
                        break;

                    //批处理点数
                    case (int)SR7IF_SETTING_ITEM.BATCH_POINT:

                        DataSize = 2;
                        break;

                    case (int)SR7IF_SETTING_ITEM.CYCLICAL_PATTERN:
                        DataSize = 1;
                        break;

                    case (int)SR7IF_SETTING_ITEM.Z_MEASURING_RANGE://????
                        DataSize = 1;
                        break;

                    //感光灵敏度
                    case (int)SR7IF_SETTING_ITEM.SENSITIVITY:
                        DataSize = 1;
                        break;

                    //曝光时间
                    case (int)SR7IF_SETTING_ITEM.EXP_TIME:
                        DataSize = 1;
                        break;

                    //光亮控制
                    case (int)SR7IF_SETTING_ITEM.LIGHT_CONTROL:
                        DataSize = 1;
                        break;

                    //激光亮度上限
                    case (int)SR7IF_SETTING_ITEM.LIGHT_MAX:
                        DataSize = 1;
                        break;

                    //激光亮度下限
                    case (int)SR7IF_SETTING_ITEM.LIGHT_MIN:
                        DataSize = 1;
                        break;

                    //峰值灵敏度
                    case (int)SR7IF_SETTING_ITEM.PEAK_SENSITIVITY:
                        DataSize = 1;
                        break;

                    //峰值选择
                    case (int)SR7IF_SETTING_ITEM.PEAK_SELECT:
                        DataSize = 1;
                        break;

                    //X轴压缩设定
                    case (int)SR7IF_SETTING_ITEM.X_SAMPLING:
                        DataSize = 1;
                        break;

                    //X轴中位数滤波
                    case (int)SR7IF_SETTING_ITEM.FILTER_X_MEDIAN:
                        DataSize = 1;
                        break;

                    //Y轴中位数滤波
                    case (int)SR7IF_SETTING_ITEM.FILTER_Y_MEDIAN:
                        DataSize = 1;
                        break;

                    //X轴平滑滤波
                    case (int)SR7IF_SETTING_ITEM.FILTER_X_SMOOTH:
                        DataSize = 1;
                        break;

                    //Y轴平滑滤波
                    case (int)SR7IF_SETTING_ITEM.FILTER_Y_SMOOTH:
                        DataSize = 2;
                        break;

                    //3D/2.5D模式切换
                    case (int)SR7IF_SETTING_ITEM.CHANGE_3D_25D:
                        DataSize = 1;
                        break;

                    default:
                        Item = 0;
                        DataSize = 1;
                        return -1;

                }
            }
            catch (Exception ex)
            {
               
                ShowInfo1( "TransCategory Err. " + ex.Message);
                return -2;
            }

            return 0;

        }

        public int getCurEncoderValue(out UInt32 encodeVal)
        {
            int err = 0;
            encodeVal = 0;

            UInt32[] pData = new UInt32[1] { 0 };

            using (PinnedObject pin = new PinnedObject(pData))
            {
                err = SR7LinkFunc.SR7IF_GetCurrentEncoder(m_ID, pin.Pointer);
            }

            //ShowInfo1(pData[0]);

            

            encodeVal = pData[0];

            return 0;

        }
        #endregion

        #region 数据图像转换的方法
        /// <summary>
        /// 高度数据转换为图像
        /// </summary>
        /// <param name="_BatchData"> 高度数据</param>    
        /// <param name="img_w">图像宽度</param>          
        /// <param name="img_h">图像高度</param>          
        /// <param name="e_Color">显示图像颜色</param>
        /// <param name="limit_min">高度限制下限（单位mm）</param>
        /// <param name="limit_max">高度限制上限（单位mm）</param>
        /// <returns></returns>
        public Bitmap BatchDataShow(int[] _BatchData, int img_w, int img_h, SHOW_COLOR_MAP e_Color, double limit_min, double limit_max)
        {
            
            byte[] dataByte = new byte[img_w * img_h];

            int InMin;
            int InMax;
            MinMaxRange(_BatchData, limit_min, limit_max , out InMin, out InMax);

            int errT = TransImg(ref _BatchData, ref dataByte, InMin, InMax, 0, 255, img_w, img_h);
           

            Bitmap tmpBmp = null;

            int errB = data2Bmp(dataByte, img_w, img_h, ref tmpBmp, e_Color);

           
            GC.Collect();
            return tmpBmp;

        }


        /// <summary>
        /// 灰度数据转换为图像
        /// </summary>
        /// <param name="_grayData"></param>
        /// <param name="img_w"></param>
        /// <param name="img_h"></param>
        /// <returns></returns>
        public Bitmap GrayDataShow(byte[] _grayData, int img_w, int img_h)
        {
            // 由于BMP图像对于行是倒置的，即图像显示的第一行是最后一行数据，所以要倒置
            //int errM = MirrorImg(ref _grayData, img_w, img_h);

            Bitmap tmpBmp = null;
           
            int errB = data2Bmp2(_grayData, img_w, img_h, ref tmpBmp);

            return tmpBmp;

            GC.Collect();

        }
        /// <summary>
        /// 一条轮廓数据转为图像
        /// </summary>
        /// <param name="ProfData">轮廓数据</param>
        /// <param name="_Width">显示的图像宽度</param>
        /// <param name="_Height">显示的图像高度</param>
        /// <param name="_DataWidth">轮廓数据宽度</param>
        /// <returns></returns>
        public Bitmap ProfileTobmp(int[] ProfData, int _Width, int _Height, int _DataWidth)
        {
            IntPtr DataObject = new IntPtr();
            //显示
            // 数据x方向间距(mm)
            //double m_XPitch = SR7LinkFunc.SR7IF_ProfileData_XPitch(ID, DataObject);
            double heightRange = 20.0;

            Bitmap tmpBmp = new Bitmap(_Width, _Height);
            Graphics g = Graphics.FromImage(tmpBmp);
            //Graphics g = pictureBox1.CreateGraphics();

            g.Clear(Color.Black);//清屏
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;              //消除锯齿  
                                                                                             //X方向比例
            double x_scale = Convert.ToDouble(_Width) / _DataWidth;
            //y方向比例
            double y_scale = Convert.ToDouble(_Height * 0.5) / heightRange;

            PointF currentPoint = new PointF(-100, -100);
            PointF currentPointNext = new PointF(-100, -100);
            for (int i = 1; i < _DataWidth; i++)
            {
                double Tmpy = ProfData[i] * 0.00001;
                double Tmpyy = ProfData[i - 1] * 0.00001;
                if (Tmpy > (-100) && Tmpyy > (-100))
                {
                    currentPoint.X = Convert.ToSingle(i * x_scale - 10);
                    currentPoint.Y = Convert.ToSingle(_Height - (Tmpyy + heightRange) * y_scale);

                    currentPointNext.X = Convert.ToSingle((i - 1) * x_scale - 10);
                    currentPointNext.Y = Convert.ToSingle(_Height - (Tmpyy + heightRange) * y_scale);

                    g.DrawLine(new Pen(Color.Red, 1), currentPointNext, currentPoint);
                }

            }

            return tmpBmp;
        }
        /// <summary>
        /// 高度数据转灰度数据
        /// </summary>
        /// <param name="dataInt"></param>
        /// <param name="dataByte"></param>
        /// <param name="InMin"></param>
        /// <param name="InMax"></param>
        /// <param name="OutMin"></param>
        /// <param name="OutMax"></param>
        /// <param name="img_w"></param>
        /// <param name="img_h"></param>
        /// <returns></returns>
        public int TransImg(ref int[] dataInt, ref byte[] dataByte, Int32 InMin, Int32 InMax, byte OutMin, byte OutMax, int img_w, int img_h)
        {
            try
            {
                // 颜色区间与高度区间比例
                double fScale = ((double)(OutMax - OutMin)) / ((double)(InMax - InMin));
                int dataCount = img_w * img_h;

                for (int i = 0; i < dataCount; i++)
                {
                    if (dataInt[i] < InMin)
                        dataByte[i] = OutMin;
                    else if (dataInt[i] > InMax)
                        dataByte[i] = OutMax;
                    else
                    {
                        dataByte[i] = Convert.ToByte((dataInt[i] - InMin) * fScale + OutMin);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return -1;
            }

            return 0;
        }
        /// <summary>
        /// 高度数据转换成灰度数据（转换成16位灰度图或者8位灰度图数据）
        /// </summary>
        /// <param name="dataInt">转换前图像数据</param>
        /// <param name="dataInt">转换后图像数据</param>
        /// <param name="InMin">数组最小值</param>
        /// <param name="InMax">数组最大值</param>
        /// <param name="OutMin">输出数组的最小值</param>
        /// <param name="OutMax">输出数组的最大值</param>
        /// <param name="img_w">图像数组宽度</param>
        /// <param name="img_h">图像数组高度</param>
        /// <returns></returns>
        public int TransImg1(ref int[] dataInt, out int[] dataOut, Int32 InMin, Int32 InMax, int OutMin, int OutMax, int img_w, int img_h)
        {
            try
            {
                // 颜色区间与高度区间比例
                double fScale = ((double)(OutMax - OutMin)) / ((double)(InMax - InMin));
                int dataCount = img_w * img_h;
                dataOut = new int[dataCount];
                for (int i = 0; i < dataCount; i++)
                {
                    if (dataInt[i] < InMin)
                        dataOut[i] = OutMin;
                    else if (dataInt[i] > InMax)
                        dataOut[i] = OutMax;
                    else
                    {
                        dataOut[i] = Convert.ToInt32((dataInt[i] - InMin) * fScale + OutMin);
                    }
                }
            }
            catch (Exception ex)
            {
                dataOut = null;
                MessageBox.Show(ex.Message);
                return -1;
            }

            return 0;
        }
        /// <summary>
        /// 获取无限循环高度图像
        /// </summary>
        /// <param name="_BatchData"></param>
        /// <param name="max_height"></param>
        /// <param name="min_height"></param>
        /// <param name="_ColorMax"></param>
        /// <param name="img_w"></param>
        /// <param name="img_h"></param>
        /// <param name="_xscale"></param>
        /// <param name="_yscale"></param>
        public Bitmap BatchDataLoopShow(int[] _BatchData, double max_height, double min_height, int _ColorMax, int img_w, int img_h, double _xscale, double _yscale)
        {
            if (img_w < 1600)
                _xscale = 1;

            //数据转换
            double fscale = Convert.ToDouble(_ColorMax) / (max_height - min_height);   //颜色区间与高度区间比例
            int imgH = Convert.ToInt32(img_h * _yscale);
            int imgW = Convert.ToInt32(img_w * _xscale);
            int TmpX = 0;
            int Tmppx = 0;
            int Tmpys = Convert.ToInt32(1.0 / _yscale);
            int Tmpxs = Convert.ToInt32(1.0 / _xscale);

            int TT = (imgW * 8 + 31) / 32;   //图像四字节对齐
            TT = TT * 4;

            int dwDataSize = TT * imgH;
            byte[] BatchImage = new byte[dwDataSize];

            for (int i = 0; i < imgH; i++)
            {
                TmpX = i * Tmpys * img_w;
                Tmppx = i * TT;
                for (int j = 0; j < imgW; j++)
                {
                    double Tmp = Convert.ToDouble(_BatchData[TmpX + j * Tmpxs]) * 0.00001;
                    if (Tmp < min_height)
                        BatchImage[Tmppx + j] = 0;
                    else if (Tmp > max_height)
                        BatchImage[Tmppx + j] = 0xff;
                    else
                    {
                        byte tmpt = Convert.ToByte((Tmp - min_height) * fscale);
                        BatchImage[Tmppx + j] = tmpt;
                    }
                }
            }

            Bitmap TmpBitmap = new Bitmap(imgW, imgH, PixelFormat.Format8bppIndexed);

            // 256 调色板
            ColorPalette monoPalette = TmpBitmap.Palette;
            Color[] entries = monoPalette.Entries;
            for (int i = 0; i < 256; i++)
                entries[i] = Color.FromArgb(i, i, i);

            TmpBitmap.Palette = monoPalette;

            Rectangle rect = new Rectangle(0, 0, TmpBitmap.Width, TmpBitmap.Height);
            System.Drawing.Imaging.BitmapData bmpData = TmpBitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
            int bytes = TT * TmpBitmap.Height;
            IntPtr ptr = bmpData.Scan0;
            System.Runtime.InteropServices.Marshal.Copy(BatchImage, 0, ptr, bytes);
            TmpBitmap.UnlockBits(bmpData);

            //pictureBox1.Image = TmpBitmap;
            return TmpBitmap;

        }

        /// <summary>
        /// 无限循环灰度图像显示
        /// </summary>
        /// <param name="_grayData"></param>
        /// <param name="img_w"></param>
        /// <param name="img_h"></param>
        /// <param name="_xscale"></param>
        /// <param name="_yscale"></param>
        public Bitmap GrayDataLoopShow(byte[] _grayData, int img_w, int img_h, double _xscale, double _yscale)
        {
            if (img_w < 1600)
                _xscale = 1;
            int imgH = Convert.ToInt32(img_h * _yscale);
            int imgW = Convert.ToInt32(img_w * _xscale);
            int TmpX = 0;
            int Tmppx = 0;
            int Tmpys = Convert.ToInt32(1.0 / _yscale);
            int Tmpxs = Convert.ToInt32(1.0 / _xscale);

            int TT = (imgW * 8 + 31) / 32;   //图像四字节对齐
            TT = TT * 4;

            int dwDataSize = TT * imgH;
            byte[] BatchImage = new byte[dwDataSize];

            for (int i = 0; i < imgH; i++)
            {
                TmpX = i * Tmpys * img_w;
                Tmppx = i * TT;
                for (int j = 0; j < imgW; j++)
                {
                    BatchImage[Tmppx + j] = _grayData[TmpX + j * Tmpxs];
                }
            }

            Bitmap TmpBitmap = new Bitmap(imgW, imgH, PixelFormat.Format8bppIndexed);

            // 256 调色板
            ColorPalette monoPalette = TmpBitmap.Palette;
            Color[] entries = monoPalette.Entries;
            for (int i = 0; i < 256; i++)
                entries[i] = Color.FromArgb(i, i, i);

            TmpBitmap.Palette = monoPalette;

            Rectangle rect = new Rectangle(0, 0, TmpBitmap.Width, TmpBitmap.Height);
            System.Drawing.Imaging.BitmapData bmpData = TmpBitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
            int bytes = TT * TmpBitmap.Height;   //图像每行字节数
            IntPtr ptr = bmpData.Scan0;
            System.Runtime.InteropServices.Marshal.Copy(BatchImage, 0, ptr, bytes);
            TmpBitmap.UnlockBits(bmpData);

            //pictureBox1.Image = TmpBitmap;
            return TmpBitmap;
            BatchImage = null;
            GC.Collect();
        }


        /// <summary>
        /// 获取高度区间范围的最大数据和最小数据
        /// </summary>
        /// <param name="array"></param>
        /// <param name="limit_min">高度下限（单位：mm）</param>
        /// <param name="limit_max">高度上限（单位：mm）</param>
        /// <param name="val_min"></param>
        /// <param name="val_max"></param>
        /// <returns></returns>
        public int MinMaxRange(Int32[] array, double limit_min, double limit_max, out Int32 val_min, out Int32 val_max)
        {
            val_min = Int32.MaxValue;
            val_max = Int32.MinValue;

            try
            {
                if (array == null)
                {
                    return -2;
                }

                foreach (int x in array)
                {
                    
                    if (x < val_min && x > Convert.ToInt32(limit_min * 100000)) val_min = x;
                    if (x > val_max && x < Convert.ToInt32(limit_max * 100000)) val_max = x;
                }

            }
            catch (Exception ex)
            {
                ShowInfo1(ex.Message);
                return -1;
            }

            return 0;
        }

        /// <summary>
        /// 获取高度区间范围的最大数据和最小数据
        /// </summary>
        /// <param name="array"></param>
        /// <param name="limit_min">限制高度下限（单位： mm）</param>
        /// <param name="limit_max">限制高度上限（单位： mm）</param>
        /// <param name="val_min"></param>
        /// <param name="val_max"></param>
        /// <returns></returns>
        public int MinMaxRange(Int32[] array, double limit_min, double limit_max, out double val_min, out double val_max)
        {
            val_min = 100000;
            val_max = -100000;

            try
            {
                if (array == null)
                {
                    return -2;
                }

                foreach (int x in array)
                {

                    if (x < val_min && x > ((int)(limit_min * 100000))) val_min = x;
                    if (x > val_max && x < ((int)(limit_max * 100000))) val_max = x;
                }

            }
            catch (Exception ex)
            {
                ShowInfo1(ex.Message);
                return -1;
            }

            return 0;
        }

        private int MirrorImg(ref byte[] data, int img_w, int img_h)
        {
            try
            {
                byte[] tmpData = new byte[img_w];

                for (int i = 0; i < img_h / 2; i++)
                {
                    Array.Copy(data, i * img_w, tmpData, 0, img_w);
                    Array.Copy(data, (img_h - i - 1) * img_w, data, i * img_w, img_w);
                    Array.Copy(tmpData, 0, data, (img_h - i - 1) * img_w, img_w);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return -1;
            }

            return 0;

        }

        private int ZoomImg(byte[] data, ref byte[] outData, Int32 img_w, Int32 img_h, Int32 out_img_w, Int32 out_img_h)
        {
            double zoom_x = (1.0 * img_w) / (1.0 * out_img_w);
            double zoom_y = (1.0 * img_h) / (1.0 * out_img_h);

            for (int h = 0; h < out_img_h; h++)
            {
                for (int w = 0; w < out_img_w; w++)
                {
                    int w_in = (int)((double)w * zoom_x);
                    int h_in = (int)((double)h * zoom_y);

                    outData[h * out_img_w + w] = data[w_in + h_in * img_w];
                }
            }

            return 0;

        }


        /// <summary>
        /// 深度数据转换成图像
        /// </summary>
        /// <param name="data"></param>
        /// <param name="img_w"></param>
        /// <param name="img_h"></param>
        /// <param name="tmpBmp"></param>
        /// <param name="e_Color"></param>
        /// <returns></returns>
        private int data2Bmp(byte[] data, int img_w, int img_h, ref Bitmap tmpBmp, SHOW_COLOR_MAP e_Color)
        {

            try
            {
                tmpBmp = new Bitmap(img_w, img_h, PixelFormat.Format8bppIndexed);
                // 256 调色板
                ColorPalette tmpPalette = tmpBmp.Palette;
                Color[] entries = tmpPalette.Entries;

                ColorMap.SetLut(e_Color);

                for (int i = 0; i < 256; i++)
                {
                    entries[i] = Color.FromArgb(ColorMap.nowTable[i, 2],
                                        ColorMap.nowTable[i, 1], ColorMap.nowTable[i, 0]);
                }

                tmpBmp.Palette = tmpPalette;

                BitmapData bmpData = tmpBmp.LockBits(
                    new Rectangle(0, 0, tmpBmp.Width, tmpBmp.Height),
                    ImageLockMode.WriteOnly,
                    PixelFormat.Format8bppIndexed);

                Marshal.Copy(data, 0, bmpData.Scan0, tmpBmp.Width * tmpBmp.Height);
                tmpBmp.UnlockBits(bmpData);

            }
            catch (Exception ex)
            {

                Trace.WriteLine(ex.Message);
                return -1;
            }
            return 0;
        }

        /// <summary>
        /// 数据转换成灰度图
        /// </summary>
        /// <param name="data"></param>
        /// <param name="img_w"></param>
        /// <param name="img_h"></param>
        /// <returns></returns>
        public int data2Bmp2(byte[] data, int img_w, int img_h, ref Bitmap tmpBmp)
        {

            try
            {
                tmpBmp = new Bitmap(img_w, img_h, PixelFormat.Format8bppIndexed);
                // 256 调色板
                ColorPalette monoPalette = tmpBmp.Palette;
                Color[] entries = monoPalette.Entries;
                for (int i = 0; i < 256; i++)
                    entries[i] = Color.FromArgb(i, i, i);

                tmpBmp.Palette = monoPalette;


                BitmapData bmpData = tmpBmp.LockBits(
                    new Rectangle(0, 0, tmpBmp.Width, tmpBmp.Height),
                    ImageLockMode.WriteOnly,
                    PixelFormat.Format8bppIndexed);

                Marshal.Copy(data, 0, bmpData.Scan0, tmpBmp.Width * tmpBmp.Height);
                tmpBmp.UnlockBits(bmpData);


            }
            catch (Exception ex)
            {

                Trace.WriteLine(ex.Message);
                return -1;
            }

            return 0;

        }



        //public void HeightDataSave()
        //{
        //    PointCloudHead pcHead = new PointCloudHead();

        //    pcHead._height = SR7LinkFunc.SR7IF_ProfilePointSetCount(camControl1.ID, new IntPtr());
        //    pcHead._width = SR7LinkFunc.SR7IF_ProfileDataWidth(camControl1.ID, new IntPtr());
        //    pcHead._xInterval = SR7LinkFunc.SR7IF_ProfileData_XPitch(camControl1.ID, new IntPtr());
        //    pcHead._yInterval = pcHead._xInterval;//行间距根据实际情况设置，默认与点间距一致

        //    int[] HeightData = new int[pcHead._height * pcHead._width];

        //    //数组整合
        //    for (int i = 0; i < pcHead._height; i++)
        //    {
        //        Array.Copy(camControl1.HeightData[0], i * pcHead._width / 2,
        //                           HeightData, 2 * i * pcHead._width / 2, pcHead._width / 2);
        //        Array.Copy(camControl1.HeightData[1], i * pcHead._width / 2,
        //                   HeightData, (2 * i + 1) * pcHead._width / 2, pcHead._width / 2);
        //    }
        //    HeightDataSaveDlg.Filter = "Ecd (*.ecd)|*.ecd";

        //    if (HeightDataSaveDlg.ShowDialog() == DialogResult.OK)
        //    {

        //        string str = System.IO.Path.GetFullPath(HeightDataSaveDlg.FileName);

        //        //获取文件的扩展名       
        //        string strExt = Path.GetExtension(HeightDataSaveDlg.FileName);

        //        EcdClass.writeEcd(str, HeightData, pcHead);

        //        InfoText.AppendText("\r\n" + "保存完成！");

        //    }

        ////}
        #endregion

    }
}
