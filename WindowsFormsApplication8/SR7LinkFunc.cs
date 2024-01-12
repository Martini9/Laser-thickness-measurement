//----------------------------------------------------------------------------- 
// <copyright file="SR7LinkFunc.cs" company="SSZN">
//	 Copyright (c) 2017 SSZN.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using WindowsFormsApplication8;
namespace Sszn
{
    #region SR7LinkFunc

    #region Structure
    /// <summary>
    /// IP 结构体
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct SR7IF_ETHERNET_CONFIG
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] abyIpAddress;
    }

    public struct SR_TARGET_SETTING
    {
        public byte byType;
        public byte byCategory;
        public byte byItem;
        public byte reserve;
        public byte byTarget1;
        public byte byTarget2;
        public byte byTarget3;
        public byte byTarget4;
    };

    public struct SR7IF_STR_CALLBACK_INFO
    {
        public int xPoints;                //x方向数据数量
        public int BatchPoints;            //批处理数量
        public int BatchTimes;             //批处理次数

        public double xPixth;              //x方向点间距
        public int startEncoder;           //批处理开始编码器值
        public int HeadNumber;             //相机头数量
        public int returnStatus;           //0:正常批处理
    }
    #endregion

    #region Method
    /// <summary>
    /// 回调函数-高速数据通信的回调函数接口.
    /// </summary>
    /// <param name="buffer"></param>      指向储存概要数据的缓冲区的指针.
    /// <param name="size"></param>        每个单元(行)的字节数量.
    /// <param name="count"></param>       存储在pBuffer中的内存的单元数量.
    /// <param name="notify"></param>      中断或批量结束等中断的通知.
    /// <param name="user"></param>        用户自定义信息.
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void HighSpeedDataCallBack(IntPtr buffer, uint size, uint count, uint notify, uint user);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="info"></param>
    /// <param name="DataObj"></param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void SR7IF_BatchOneTimeCallBack(IntPtr info, IntPtr DataObj);
    /// <summary>
    /// Function definitions   接口函数定义
    /// </summary>
    internal class SR7LinkFunc
    {
        internal static UInt32 ProgramSettingSize
        {
            get { return 10932; }
        }

        ///<summary>
        /// 通信连接------与相机连接
        /// </summary>
        /// <param name="lDeviceId"></param>          设备ID号，范围为0-3
        /// <param name="pEthernetConfig"></param>   （网口）通信设定
        /// <returns></returns>                       0：成功; 小于0：失败
        /// <remarks></remarks>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_EthernetOpen(int lDeviceId, ref SR7IF_ETHERNET_CONFIG pEthernetConfig);


        /// <summary>
        /// 断开与相机的连接
        /// </summary>
        /// <param name="lDeviceId"></param>      设备ID号，范围为0-3
        /// <returns></returns>                   0：成功; 小于0：失败
        /// <remarks></remarks>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_CommClose(int lDeviceId);

        /// <summary>
        /// 开始批处理,立即执行批处理程序
        /// </summary>
        /// <param name="lDeviceId"></param>       设备ID号，范围为0-3
        /// <param name="Timeout"></param>         非循环获取时，超时时间(单位ms);循环模式该参数可设置为-1
        /// <returns></returns>                    0：成功; 小于0：失败
        /// <remarks></remarks>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_StartMeasure(int lDeviceId, int Timeout);

        /// <summary>
        /// 开始批处理,硬件IO触发开始批处理，具体查看硬件手册
        /// </summary>
        /// <param name="lDeviceId"></param>     设备ID号，范围为0-3
        /// <param name="Timeout"></param>       非循环获取时,超时时间(单位ms);循环模式该参数可设置为-1
        /// <param name="restart"></param>       预留，设为0
        /// <returns></returns>                  0：成功; 小于0：失败
        /// <remarks></remarks>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_StartIOTriggerMeasure(int lDeviceId, int Timeout, int restart);

        /// <summary>
        /// 停止批处理---停止扫描
        /// </summary>
        /// <param name="lDeviceId"></param>     设备ID号，范围为0-3
        /// <returns></returns>                  0：成功; 小于0：失败
        /// <remarks></remarks>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_StopMeasure(int lDeviceId);

        /// <summary>
        /// 阻塞方式获取数据---等待数据接收完成
        /// </summary>
        /// <param name="lDeviceId"></param>     设备ID号，范围为0-3
        /// <param name="DataObj"></param>       返回数据指针
        /// <returns></returns>                  0：成功; 小于0：失败
        /// <remarks></remarks>
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_ReceiveData(int lDeviceId, IntPtr DataObj);


        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetCurrentEncoder(int lDeviceId, IntPtr DataObj);

        /// <summary>
        /// 获取当前批处理设定行数
        /// </summary>
        /// <param name="lDeviceId">设备号0-3</param>
        /// <param name="DataObj">预留设置为null</param>
        /// <returns></returns>  返回实际批处理行数
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_ProfilePointSetCount(int lDeviceId, IntPtr DataObj);


        /// <summary>
        /// 获取批处理实际获取行数
        /// </summary>
        /// <param name="lDeviceId"></param>      设备ID号，范围为0-3
        /// <param name="DataObj"></param>        预留，设置为NULL
        /// <returns></returns>                   返回实际批处理行数
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_ProfilePointCount(int lDeviceId, IntPtr DataObj);

        /// <summary>
        /// 获取数据宽度
        /// </summary>
        /// <param name="lDeviceId"></param>      设备ID号，范围为0-3
        /// <param name="DataObj"></param>        预留，设置为NULL
        /// <returns></returns>                   返回数据宽度(单位像素)
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_ProfileDataWidth(int lDeviceId, IntPtr DataObj);

        /// <summary>
        /// 获取数据x方向间距
        /// </summary>
        /// <param name="lDeviceId"></param>      设备ID号，范围为0-3
        /// <param name="DataObj"></param>        预留，设置为NULL
        /// <returns></returns>                   返回数据x方向间距(mm)
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern double SR7IF_ProfileData_XPitch(int lDeviceId, IntPtr DataObj);

        /// <summary>
        /// 获取编码器值
        /// </summary>
        /// <param name="lDeviceId"></param>       设备ID号，范围为0-3
        /// <param name="DataObj"></param>         预留，设置为NULL
        /// <param name="Encoder"></param>         返回数据指针
        /// <returns></returns>                    0：成功; 小于0：失败
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetEncoder(int lDeviceId, IntPtr DataObj, IntPtr Encoder);

        /// <summary>
        /// 非阻塞方式获取编码器值
        /// </summary>
        /// <param name="lDeviceId"></param>       设备ID号，范围为0-3
        /// <param name="DataObj"></param>         预留，设置为NULL
        /// <param name="Encoder"></param>         返回数据指针
        /// <param name="GetCnt"></param>          获取数据长度
        /// <returns></returns>                    >=0: 实际返回的数据长度   小于0: 获取失败
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetEncoderContiune(int lDeviceId, IntPtr DataObj, IntPtr Encoder, int GetCnt);

        /// <summary>
        /// 阻塞方式获取轮廓数据
        /// </summary>
        /// <param name="lDeviceId"></param>       设备ID号，范围为0-3
        /// <param name="DataObj"></param>         预留，设置为NULL
        /// <param name="Profile"></param>         返回数据指针
        /// <returns></returns>                    0：成功; 小于0：失败
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetProfileData(int lDeviceId, IntPtr DataObj, IntPtr Profile);

        /// <summary>
        /// 非阻塞方式获取轮廓数据
        /// </summary>
        /// <param name="lDeviceId"></param>        设备ID号，范围为0-3
        /// <param name="DataObj"></param>          预留，设置为NULL
        /// <param name="Profile"></param>          返回数据指针
        /// <param name="GetCnt"></param>           获取数据长度
        /// <returns></returns>                     >=0: 实际返回的数据长度   小于0: 获取失败
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetProfileContiuneData(int lDeviceId, IntPtr DataObj, IntPtr Profile, int GetCnt);

        /// <summary>
        /// 无终止循环获取数据
        /// </summary>
        /// <param name="lDeviceId"></param>         设备ID号，范围为0-3
        /// <param name="DataObj"></param>           预留，设置为NULL
        /// <param name="Profile"></param>           返回轮廓数据指针
        /// <param name="Intensity"></param>         返回亮度数据指针
        /// <param name="Encoder"></param>           返回编码器数据指针
        /// <param name="FrameId"></param>           返回帧编号数据指针
        /// <param name="FrameLoss"></param>         返回批处理过快掉帧数量数据指针
        /// <param name="GetCnt"></param>            获取数据长度
        /// <returns></returns>                      >=0: 实际返回的数据长度   小于0: 获取失败
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetBatchRollData(int lDeviceId,
            IntPtr DataObj,
            IntPtr Profile,
            IntPtr Intensity,
            IntPtr Encoder,
            IntPtr FrameId,
            IntPtr FrameLoss,
            int GetCnt);


        /// <summary>
        /// 无终止循环设定终止行数（该设置断电不保存）
        /// </summary>
        /// <param name="lDeviceId"></param>         设备ID号，范围为0-3
        /// <param name="DataObj"></param>           预留，设置为NULL
        /// <param name="points"></param>            设定行数，范围（0：无终止循环。≥15000：设定终止行数，其他无效）
        /// <returns></returns>                      =0: 设置成功   小于0: 设置失败
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_SetBatchRollProfilePoint(int lDeviceId, IntPtr DataObj, uint points);

        /// <summary>
        /// 阻塞方式获取亮度数据
        /// </summary>
        /// <param name="lDeviceId"></param>        设备ID号，范围为0-3
        /// <param name="DataObj"></param>          预留，设置为NULL
        /// <param name="Intensity"></param>        返回数据指针
        /// <returns></returns>                     0：成功; 小于0：失败
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetIntensityData(int lDeviceId, IntPtr DataObj, IntPtr Intensity);

        /// <summary>
        /// 非阻塞获取亮度数据
        /// </summary>
        /// <param name="lDeviceId"></param>        设备ID号，范围为0-3
        /// <param name="DataObj"></param>          预留，设置为NULL
        /// <param name="Intensity"></param>        返回数据指针
        /// <param name="GetCnt"></param>           获取数据长度
        /// <returns></returns>                     >=0: 实际返回的数据长度   小于0: 获取失败
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetIntensityContiuneData(int lDeviceId, IntPtr DataObj, IntPtr Intensity, int GetCnt);

        /// <summary>
        /// 无终止循环获取数据异常计算值
        /// </summary>
        /// <param name="lDeviceId"></param>        设备ID号，范围为0-3.
        /// <param name="EthErrCnt"></param>        返回网络传输导致错误的数量
        /// <param name="UserErrCnt"></param>       返回用户获取导致错误的数量
        /// <returns></returns>                     0：成功; 小于0：失败
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetBatchRollError(int lDeviceId, IntPtr EthErrCnt, IntPtr UserErrCnt);

        /// <summary>
        /// 获取系统错误信息.
        /// </summary>
        /// <param name="lDeviceId"></param>     设备ID号，范围为0-3.
        /// <param name="EthErrCnt"></param>     返回错误码数量.
        /// <param name="UserErrCnt"></param>    返回错误码指针.
        /// <returns></returns>                  0：成功; 小于0：失败
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetError(int lDeviceId, IntPtr pbyErrCnt, IntPtr pwErrCode);

        /// <summary>
        /// 初始化以太网高速数据通信.
        /// </summary>
        /// <param name="lDeviceId"></param>              设备ID号，范围为0-3.
        /// <param name="pEthernetConfig"></param>        Ethernet 通信设定.
        /// <param name="wHighSpeedPortNo"></param>       Ethernet 通信端口设定.
        /// <param name="pCallBack"></param>              高速通信中数据接收的回调函数.
        /// <param name="dwProfileCnt"></param>           回调函数被调用的频率. 范围1-256
        /// <param name="dwThreadId"></param>             线程号.
        /// <returns></returns>                           0：成功; 小于0：失败
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_HighSpeedDataEthernetCommunicationInitalize(int lDeviceId,
            ref SR7IF_ETHERNET_CONFIG pEthernetConfig,
            int wHighSpeedPortNo,
            HighSpeedDataCallBack pCallBack,
            uint dwProfileCnt,
            uint dwThreadId);

        /// <summary>
        /// 获取库版本号.
        /// </summary>
        /// <returns></returns>       返回版本信息.
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr SR7IF_GetVersion();

        /// <summary>
        /// 获取相机型号
        /// </summary>
        /// <param name="lDeviceId"></param>   设备ID号，范围为0-3.
        /// <returns></returns>                0：成功; 小于0：失败
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr SR7IF_GetModels(int lDeviceId);

        /// <summary>
        /// 获取相机头序列号
        /// </summary>
        /// <param name="lDeviceId"></param>   设备ID号，范围为0-3.
        /// <param name="Head"></param>        0:相机头A 1:相机头B
        /// <returns></returns>                成功:返回相机头序列号,失败：相机头不存在或参数错误
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr SR7IF_GetHeaderSerial(int lDeviceId, int Head);

        /// <summary>
        /// 获取传感头B是否在线
        /// </summary>
        /// <param name="lDeviceId"></param>      设备ID号，范围为0-3.
        /// <returns></returns>                   0：在线; 小于0：不在线
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetOnlineCameraB(int lDeviceId);

        /// <summary>
        /// 切换相机配置的参数.
        /// </summary>
        /// <param name="lDeviceId"></param>     设备ID号，范围为0-3.
        /// <param name="No"></param>            任务参数列表编号 0 - 63.
        /// <returns></returns>                  0：成功; 小于0：失败
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_SwitchProgram(int lDeviceId, int No);

        /// <summary>
        /// 设置输出端口电平.
        /// </summary>
        /// <param name="lDeviceId"></param>   设备ID号，范围为0-3.
        /// <param name="Port"></param>        输出端口号，范围为0-7.
        /// <param name="Level"></param>       输出电平值.
        /// <returns></returns>                0：成功; 小于0：失败
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_SetOutputPortLevel(uint lDeviceId, uint Port, bool Level);

        /// <summary>
        /// 读取输入端口电平
        /// </summary>
        /// <param name="lDeviceId"></param> 设备ID号，范围为0-3.
        /// <param name="Port"></param>      输入端口号，范围为0-7.
        /// <param name="Level"></param>     读取输入电平.
        /// <returns></returns>              0：成功; 小于0：失败
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetInputPortLevel(uint lDeviceId, uint Port, IntPtr Level);

        /// <summary>
        /// 参数设定
        /// </summary> 
        /// <param name="lDeviceId"></param>    设备ID号，范围为0-3.
        /// <param name="Depth"></param>        设置的值的级别.
        /// <param name="Type"></param>         设置类型.
        /// <param name="Category"></param>     设置种类.
        /// <param name="Item"></param>         设置项目.
        /// <param name="Target"></param>       根据发送 / 接收的设定，可能需要进行相应的指定。无需设定时，指定为 0。
        /// <param name="pData"></param>        设置数据.
        /// <param name="DataSize"></param>     设置数据的长度.
        /// <returns></returns>                 0：成功; 小于0：失败
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_SetSetting(uint lDeviceId, int Depth, int Type, int Category, int Item, int[] Target, IntPtr pData, int DataSize);

        /// <summary>
        /// 参数获取
        /// </summary> 
        /// <param name="lDeviceId"></param>    设备ID号，范围为0-3.
        /// <param name="Type"></param>         设置类型.
        /// <param name="Category"></param>     设置种类.
        /// <param name="Item"></param>         设置项目.
        /// <param name="Target"></param>       根据发送 / 接收的设定，可能需要进行相应的指定。无需设定时，指定为 0。
        /// <param name="pData"></param>        设置数据.
        /// <param name="DataSize"></param>     设置数据的长度.
        /// <returns></returns>                 0：成功; 小于0：失败
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetSetting(uint lDeviceId, int Type, int Category, int Item, int[] Target, IntPtr pData, int DataSize);

        /// <summary>
        /// 获取当前一条轮廓（非批处理下）
        /// </summary>
        /// <param name="lDeviceId"></param>         设备ID号，范围为0-3.
        /// <param name="pProfileData"></param>      返回轮廓的指针.
        /// <param name="pEncoder"></param>          返回编码器的指针.
        /// <returns></returns>                      0：成功; 小于0：失败
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetSingleProfile(uint lDeviceId, IntPtr pProfileData, IntPtr pEncoder);

        /// <summary>
        /// 将导出的参数导入到系统中.
        /// </summary>
        /// <param name="lDeviceId"></param>          设备ID号，范围为0-3.
        /// <param name="pSettingdata"></param>       导入参数表指针.
        /// <param name="size"></param>               导入参数表的大小.
        /// <returns></returns>                       0：成功; 小于0：失败
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_LoadParameters(uint lDeviceId, IntPtr pSettingdata, UInt32 size);
        /// <summary>
        /// 返回产品剩余天数
        /// </summary>
        /// <param name="lDeviceId"></param>
        /// <param name="RemainDay"></param>    返回剩余天数
        /// <returns></returns>                  0：成功; 小于0：失败
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_GetLicenseKey(uint lDeviceId, IntPtr RemainDay);
        /// <summary>
        /// 将系统参数导出，注意只导出当前任务的参数.
        /// </summary>
        /// <param name="lDeviceId"></param>          设备ID号，范围为0-3.
        /// <param name="size"></param>               返回参数表的大小.
        /// <returns></returns>                       NULL:失败. 其他:成功
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr SR7IF_ExportParameters(int lDeviceId, IntPtr size);

        /// <summary>
        /// 3D显示
        /// </summary>
        /// <param name="_BatchData"></param>               批处理数据
        /// <param name="x_true_step"></param>              x方向间矩/mm
        /// <param name="y_true_step"></param>              y方向间距/mm
        /// <param name="x_Point_num"></param>              x方向数据个数
        /// <param name="y_batchPoint_num"></param>         批处理行数
        /// <param name="z_scale"></param>                  z方向缩放系数
        /// <param name="Ho"></param>                       z方向最大值
        /// <param name="Lo"></param>                       z方向最小值
        [DllImport("SR3dexe.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void SR_3D_EXE_Show(IntPtr _BatchData,
                                double x_true_step,
                                double y_true_step,
                                int x_Point_num,
                                int y_batchPoint_num,
                                double z_scale,
                                double Ho,
                                double Lo
                                );


        /// <summary>
        /// 设置回调函数,建议获取数据后另外开启线程进行处理(批处理一次回调一次）
        /// </summary>
        /// <param name="lDeviceId"></param>   设备ID号，范围为0-3.
        /// <param name="size"></param>        回调函数.
        /// <returns></returns>                 0：成功; 小于0：失败
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_SetBatchOneTimeDataHandler(int lDeviceId, SR7IF_BatchOneTimeCallBack CallFunc);

        /// <summary>
        ///  开始批处理 （批处理一次, 回调一次)
        /// </summary>
        /// <param name="lDeviceId"></param>        设备ID号，范围为0-3.
        /// <param name="ImmediateBatch"></param>    0:立即开始批处理  1:等待外部开始批处理.
        /// <returns></returns>                       0：成功; 小于0：失败
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_StartMeasureWithCallback(int lDeviceId, int ImmediateBatch);

        /// <summary>
        /// 批处理软件触发开始（批处理一次回调一次）
        /// </summary>
        /// <param name="lDeviceId"></param>   设备ID号，范围为0-3.
        /// <returns></returns>                 0：成功; 小于0：失败
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern int SR7IF_TriggerOneBatch(int lDeviceId);

        /// <summary>
        /// 批处理轮廓获取（批处理一次回调一次）
        /// </summary>
        /// <param name="DataObj"></param>     设备ID号，范围为0-3.
        /// <param name="Head"></param>         0：相机头A  1：相机头B
        /// <returns></returns>                 返回数据指针 null:无数据或相应头不存在
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr SR7IF_GetBatchProfilePoint(IntPtr DataObj, int Head);

        /// <summary>
        ///  批处理亮度获取（批处理一次回调一次）
        /// </summary>
        /// <param name="DataObj"></param>   设备ID号，范围为0-3.
        /// <param name="Head"></param>    0：相机头A  1：相机头B
        /// <returns></returns>                返回数据指针 null:无数据或相应头不存在
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr SR7IF_GetBatchIntensityPoint(IntPtr DataObj, int Head);

        /// <summary>
        /// 批处理编码器获取（批处理一次回调一次）
        /// </summary>
        /// <param name="DataObj"></param>  设备ID号，范围为0-3.
        /// <param name="Head"></param>     0：相机头A  1：相机头B
        /// <returns></returns>             返回数据指针 null:无数据或相应头不存在
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr SR7IF_GetBatchEncoderPoint(IntPtr DataObj, int Head);

        /// <summary>
        /// 查找在线设备
        /// </summary>
        /// <param name="ReadNum">搜索到的设备个数</param>
        /// <param name="timeOut">搜索超时时间</param>
        /// <returns></returns>  返回已搜索到的设备的 IP 地址指针
        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]

        internal static extern IntPtr SR7IF_SearchOnline(IntPtr ReadNum, int timeOut);


        [DllImport("SR7Link.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr SR7IF_GetCameraTemperature( uint DeviceId ,ref int tempA, ref int tempB);
    }

    #endregion
    #endregion


    #region PinnedObject

    public sealed class PinnedObject : IDisposable
    {
        #region Field

        private GCHandle _Handle;      // Garbage collector handle

        #endregion

        #region Property

        /// <summary>
        /// Get the address.
        /// </summary>
        public IntPtr Pointer
        {
            // Get the leading address of the current object that is pinned.
            get { return _Handle.AddrOfPinnedObject(); }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="target">Target to protect from the garbage collector</param>
        public PinnedObject(object target)
        {
            // Pin the target to protect it from the garbage collector.
            _Handle = GCHandle.Alloc(target, GCHandleType.Pinned);
        }

        #endregion

        #region Interface
        /// <summary>
        /// Interface
        /// </summary>
        public void Dispose()
        {
            _Handle.Free();
            _Handle = new GCHandle();
        }

        #endregion

    }
    #endregion

    #region   Enum

    /*******接口函数返回值**********/
    enum SR7IF_ERROR
    {
        SR7IF_ERROR_NOT_FOUND = (-999),                  // Item is not found.
        SR7IF_ERROR_COMMAND = (-998),                  // Command not recognized.
        SR7IF_ERROR_PARAMETER = (-997),                  // Parameter is invalid.
        SR7IF_ERROR_UNIMPLEMENTED = (-996),                  // Feature not implemented.
        SR7IF_ERROR_HANDLE = (-995),                  // Handle is invalid.
        SR7IF_ERROR_MEMORY = (-994),                  // Out of memory.
        SR7IF_ERROR_TIMEOUT = (-993),                  // Action timed out.
        SR7IF_ERROR_DATABUFFER = (-992),                  // Buffer not large enough for data.
        SR7IF_ERROR_STREAM = (-991),                  // Error in stream.
        SR7IF_ERROR_CLOSED = (-990),                  // Resource is no longer avaiable.
        SR7IF_ERROR_VERSION = (-989),                  // Invalid version number.
        SR7IF_ERROR_ABORT = (-988),                  // Operation aborted.
        SR7IF_ERROR_ALREADY_EXISTS = (-987),                  // Conflicts with existing item.
        SR7IF_ERROR_FRAME_LOSS = (-986),                  // Loss of frame.
        SR7IF_ERROR_ROLL_DATA_OVERFLOW = (-985),                  // Continue mode Data overflow.
        SR7IF_ERROR_ROLL_BUSY = (-984),                  // Read Busy.
        SR7IF_ERROR_MODE = (-983),                  // Err mode.
        SR7IF_ERROR_CAMERA_NOT_ONLINE = (-982),                  // Camera not online.
        SR7IF_ERROR = (-1),                    // General error.
        SR7IF_OK = (0),                     // Operation successful.
        SR7IF_NORMAL_STOP = (-100)                //A normal stop caused by external IO or other causes

    }

    //SetSetting GetSetting
    enum SR7IF_SETTING_ITEM
    {

        TRIG_MODE = 0x0001,//触发模式
        SAMPLED_CYCLE = 0x0002,//采样周期
        BATCH_ON_OFF = 0x0003,//批处理开关
        ENCODER_TYPE = 0x0007,//编码器类型

        REFINING_POINTS = 0x0009,//细化点数
        BATCH_POINT = 0x000A,//批处理点数

        CYCLICAL_PATTERN = 0x0010,//循环模式 0 关闭 1 打开
        Z_MEASURING_RANGE = 0x0103,//Z方向测量范围

        SENSITIVITY = 0x0105,//感光灵敏度
        EXP_TIME = 0x0106,//曝光时间
        LIGHT_CONTROL = 0x010B,//光亮控制 
        LIGHT_MAX = 0x010C,//激光亮度上限
        LIGHT_MIN = 0x010D,//激光亮度下限
        PEAK_SENSITIVITY = 0x010F,//峰值灵敏度
        PEAK_SELECT = 0x0111,  //峰值选择

        X_SAMPLING = 0x0202,   //X轴压缩设定

        FILTER_X_MEDIAN = 0x020A,  //X轴中位数滤波
        FILTER_X_SMOOTH = 0x020B,  //X轴平滑滤波
        FILTER_Y_MEDIAN = 0x020C,  //Y轴中位数滤波
        FILTER_Y_SMOOTH = 0x020D,  //Y轴平滑滤波

        CHANGE_3D_25D = 0x3000,    //3D/2.5D切换 2.5模式下X轴压缩设定为 自动变更默认值.

        X_PIXEL = 0x3001,          //X数据宽度(单位像素)
        X_PITCH = 0x3002      //X Resolution

    }

    //触发模式
    enum SR7IF_TRIG_MODE
    {
        CONTINUE = 0,
        EXT_TRIGGER = 1,
        ENCODER = 2,
    }

    //采集周期
    //100 200 400 600 1000 1500 2000 2500

    //批处理开关
    enum SR7IF_BATCH_ON_OFF
    {
        OFF = 0,
        ON = 1
    }

    //编码器类型
    enum SR7IF_ENCODER_TYPE
    {
        E_1_1 = 0,//0：1 相 1 递增；
        E_2_1 = 1,//1：2 相 1 递增；
        E_2_2 = 2,//2：2 相 2 递增；
        E_2_4 = 3 //3：2 相 4 递增；
    }

    //细化点数   1 -- 
    //批处理点数 1 -- 

    //循环模式 0 关闭 1 打开
    enum SR7IF_CYCLICAL_PATTERN
    {
        CLOSE = 0,
        OPEN = 1
    }

    //Z方向测量范围 注：只支持SR8020/SR8060
    enum SR7IF_Z_MEASURING_RANGE
    {
        Z840 = 0,
        Z768 = 1,
        Z512 = 2,
        Z384 = 3,
        Z256 = 4,
        Z192 = 5,
        Z128 = 6,
        Z96 = 7,
        Z64 = 8,
        Z48 = 9,
        Z32 = 10
    };

    //感光灵敏度
    enum SR7IF_SENSITIVITY
    {
        HIGH = 0,
        HIGH_RANGE_1 = 1,
        HIGH_RANGE_2 = 2,
        HIGH_RANGE_3 = 3,
        HIGH_RANGE_4 = 4,
        CUSTOMIZATION = 5
    }

    //曝光时间
    enum SR7IF_EXP_TIME
    {
        T10US = 0,
        T15US = 1,
        T30US = 2,
        T60US = 3,
        T120US = 4,
        T240US = 5,
        T480US = 6,
        T960US = 7,
        T1920US = 8,
        T2400US = 9,
        T4900US = 10,
        T9800US = 11,

    }

    //光亮控制 
    enum SR7IF_LIGHT_CONTROL
    {
        AUTO = 0,
        MAN = 1
    }

    //激光亮度上限 0-99
    //激光亮度下限 0-99

    //峰值灵敏度
    enum SR7IF_PEAK_SENSITIVITY
    {
        N_1 = 1,
        N_2 = 2,
        N_3 = 3,
        N_4 = 4,
        N_5 = 5
    }

    //峰值选择
    enum SR7IF_PEAK_SELECT
    {
        STANDARD = 0,
        NEAR = 1,
        FAR = 2,
        BE_NULL = 3,
        CONTINUE = 4,
        GLUE = 5

    }

    //X轴压缩设定 注：2.5D 模式下不能设置
    enum SR7IF_X_SAMPLING
    {
        OFF = 1,
        X2 = 2,
        X4 = 4,
        X8 = 8,
        X16 = 16
    }

    //X轴中位数滤波
    enum SR7IF_FILTER_X_MEDIAN
    {
        OFF = 1,
        N3 = 3,
        N5 = 5,
        N7 = 7,
        N9 = 9
    }

    //Y轴中位数滤波
    enum SR7IF_FILTER_Y_MEDIAN
    {
        OFF = 1,
        N3 = 3,
        N5 = 5,
        N7 = 7,
        N9 = 9
    }

    //X轴平滑滤波
    enum SR7IF_FILTER_X_SMOOTH
    {

        N1 = 1,
        N2 = 2,
        N4 = 4,
        N8 = 8,
        N16 = 16,
        N32 = 32,
        N64 = 64

    }

    //Y轴平滑滤波
    enum SR7IF_FILTER_Y_SMOOTH
    {

        N1 = 1,
        N2 = 2,
        N4 = 4,
        N8 = 8,
        N16 = 16,
        N32 = 32,
        N64 = 64,
        N128 = 128,
        N256 = 256

    }

    //3D/2.5D切换 2.5模式下X轴压缩设定为 自动变更默认值.
    enum SR7IF_CHANGE_3D_25D
    {
        T3D = 0,
        T25D = 1
    }
    #endregion


    #region EcdClass
    public struct BATCH_INFO
    {
        public uint version;
        public int width;
        public int height;
        public double xInterval;
        public double yInterval;
    };

    public struct PointCloudHead
    {
        public int _height; //点云行数
        public int _width; //点云列数
        public double _xInterval; //点云列间距
        public double _yInterval; //点云行间距
    };



    public class EcdClass
    {

        public static void readEcd(string file, ref Int32[] data, ref PointCloudHead pcHead)
        {
            if (!File.Exists(file))
            {
                return;
            }

            FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read);
            BinaryReader br = new BinaryReader(fs);

            BATCH_INFO head = new BATCH_INFO();
            Int32 reserveU = 0;

            head.version = br.ReadUInt32();
            head.width = br.ReadInt32();
            head.height = br.ReadInt32();
            if (head.version == 2)
            {
                head.xInterval = Math.Round(br.ReadDouble(), 3);
                head.yInterval = Math.Round(br.ReadDouble(), 3);
                reserveU = br.ReadInt32();
            }
            else
            {
                reserveU = br.ReadInt32();  //无效位，为了数据对齐
                head.xInterval = Math.Round(br.ReadDouble(), 3);
                head.yInterval = Math.Round(br.ReadDouble(), 3);
            }
            br.ReadBytes(2552 * 4);   //文件头共10240字节
            data = new int[head.width * head.height];
            for (int i = 0; i < head.width * head.height; i++)
            {
                data[i] = br.ReadInt32();
            }
            pcHead._width = head.width;
            pcHead._height = head.height;
            pcHead._xInterval = head.xInterval;
            pcHead._yInterval = head.yInterval;

            Trace.WriteLine(head.version);
            Trace.WriteLine(pcHead._width);
            Trace.WriteLine(pcHead._height);
            Trace.WriteLine(pcHead._xInterval);
            Trace.WriteLine(pcHead._yInterval);

        }

        public static void writeEcd(string file, Int32[] data, PointCloudHead pcHead)
        {

            FileStream fs = new FileStream(file, FileMode.Create, FileAccess.Write);
            BinaryWriter bw = new BinaryWriter(fs);
            BATCH_INFO head = new BATCH_INFO();

            head.version = 2;
            head.width = pcHead._width;
            head.height = pcHead._height;
            head.xInterval = pcHead._xInterval;
            head.yInterval = pcHead._yInterval;

            bw.Write(head.version);
            bw.Write(head.width);
            bw.Write(head.height);
            bw.Write(head.xInterval);
            bw.Write(head.yInterval);

            byte[] buff = new byte[2553 * 4];

            bw.Write(buff, 0, buff.Length);
            for (int i = 0; i < data.Length; i++)
            {
                bw.Write(data[i]);
            }
            fs.Flush();
            fs.Close();
            bw.Close();

        }

        public static int WritePcd(string file, Int32[] batchData, PointCloudHead pcHead)
        {
            using (PinnedObject data = new PinnedObject(batchData))
            {
                IntPtr ptr = Marshal.StringToHGlobalAnsi(file);
                SavePcd(ptr, data.Pointer, pcHead);
                Marshal.FreeHGlobal(ptr);
            }
            return 0;
        }
        public static int writePly(string file, Int32[] batchData, PointCloudHead pcHead)
        {
            using (PinnedObject data = new PinnedObject(batchData))
            {
                IntPtr ptr = Marshal.StringToHGlobalAnsi(file);
                SavePly(ptr, data.Pointer, pcHead);
                Marshal.FreeHGlobal(ptr);
            }
            return 0;
        }
        public static double writeTif(string file, Int32[] batchData, PointCloudHead pcHead, double HeightMax, double HeightMin)
        {
            double ret;
            using (PinnedObject data = new PinnedObject(batchData))
            {
                IntPtr ptr = Marshal.StringToHGlobalAnsi(file);
                ret = Save16Tif(ptr, data.Pointer, pcHead, HeightMax, HeightMin);
                //Save32Tif(ptr, data.Pointer, pcHead);
                Marshal.FreeHGlobal(ptr);
            }
            return ret;
        }

        /// <summary>
        /// 保存PCD文件
        /// </summary>
        /// <param name="path"></param>            保存路径
        /// <param name="data"></param>            高度数据 int数组
        /// <param name="head"></param>            头文件图像长宽 xy间距
        /// <returns></returns>                    0：成功; 小于0：失败
        [DllImport("ImageConvert.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SavePcd(IntPtr path, IntPtr data, PointCloudHead head);

        /// <summary>
        /// 保存PCD文件
        /// </summary>
        /// <param name="path"></param>            保存路径
        /// <param name="data"></param>            高度数据 int数组
        /// <param name="head"></param>            头文件图像长宽 xy间距
        /// <returns></returns>                    0：成功; 小于0：失败
        [DllImport("ImageConvert.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SavePly(IntPtr path, IntPtr data, PointCloudHead head);

        /// <summary>
        /// 保存PCD文件
        /// </summary>
        /// <param name="path"></param>            保存路径
        /// <param name="data"></param>            高度数据 int数组
        /// <param name="head"></param>            头文件图像长宽 xy间距
        /// <returns></returns>                    0：成功; 小于0：失败
        [DllImport("ImageConvert.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool Save32Tif(IntPtr path, IntPtr data, PointCloudHead head);

        /// <summary>
        /// 保存PCD文件
        /// </summary>
        /// <param name="path"></param>            保存路径
        /// <param name="data"></param>            高度数据 int数组
        /// <param name="head"></param>            头文件图像长宽 xy间距
        /// <returns></returns>                    0：成功; 小于0：失败
        [DllImport("ImageConvert.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern double Save16Tif(IntPtr path, IntPtr data, PointCloudHead head, double HeightMax, double HeightMin);

    }


    #endregion

    #region ColorMap
    public enum SHOW_COLOR_MAP
    {
        GRAY = 0,
        SSZN_COLOR = 1,
        RRAINTABLE = 2,
        IRON = 3
    }

    /// <summary>
    /// 伪彩色图像构造器
    /// </summary>
    public class ColorMap
    {
        public static int SetLut(SHOW_COLOR_MAP lut)
        {

            try
            {
                switch (lut)
                {
                    case SHOW_COLOR_MAP.GRAY:
                        for (int i = 0; i < 256; i++)
                        {
                            nowTable[i, 0] = (byte)i;
                            nowTable[i, 1] = (byte)i;
                            nowTable[i, 2] = (byte)i;
                        }

                        break;
                    case SHOW_COLOR_MAP.IRON:
                        for (int i = 0; i < 256; i++)
                        {
                            nowTable[i, 0] = (byte)ironTable[i / 2, 0];
                            nowTable[i, 1] = (byte)ironTable[i / 2, 1];
                            nowTable[i, 2] = (byte)ironTable[i / 2, 2];
                        }

                        break;
                    case SHOW_COLOR_MAP.RRAINTABLE:
                        for (int i = 0; i < 256; i++)
                        {
                            nowTable[i, 0] = (byte)rainTable[i / 2, 0];
                            nowTable[i, 1] = (byte)rainTable[i / 2, 1];
                            nowTable[i, 2] = (byte)rainTable[i / 2, 2];
                        }

                        break;
                    case SHOW_COLOR_MAP.SSZN_COLOR:
                        for (int i = 0; i < 256; i++)
                        {

                            nowTable[i, 0] = (byte)(ColorMap.SSZNColor[i] & 0x000000ff);
                            nowTable[i, 1] = (byte)(byte)((ColorMap.SSZNColor[i] & 0x0000ff00) >> 8);
                            nowTable[i, 2] = (byte)((ColorMap.SSZNColor[i] & 0x00ff0000) >> 16);

                        }

                        break;

                }

            }
            catch (Exception ex)
            {

                Trace.WriteLine(ex.Message);
                return -1;
            }

            return 0;
        }

        #region Now

        public static byte[,] nowTable = new byte[256, 3];
        //{    { 0,0,0}, { 1,1,1}, { 2,2,2}, { 3,3,3}, { 4,4,4}, { 5,5,5}, { 6,6,6}, { 7,7,7},};

        #endregion Now

        // 铁红色带映射表
        #region iron
        public static byte[,] ironTable = new byte[128, 3] {
                {0,   0,  0},
                {0,   0,  0},
                {0,   0,  36},
                {0,   0,  51},
                {0,   0,  66},
                {0,   0,  81},
                {2,   0,  90},
                {4,   0,  99},
                {7,   0, 106},
                {11,   0, 115},
                {14,   0, 119},
                {20,   0, 123},
                {27,   0, 128},
                {33,   0, 133},
                {41,   0, 137},
                {48,   0, 140},
                {55,   0, 143},
                {61,   0, 146},
                {66,   0, 149},
                {72,   0, 150},
                {78,   0, 151},
                {84,   0, 152},
                {91,   0, 153},
                {97,   0, 155},
                {104,   0, 155},
                {110,   0, 156},
                {115,   0, 157},
                {122,   0, 157},
                {128,   0, 157},
                {134,   0, 157},
                {139,   0, 157},
                {146,   0, 156},
                {152,   0, 155},
                {157,   0, 155},
                {162,   0, 155},
                {167,   0, 154},
                {171,   0, 153},
                {175,   1, 152},
                {178,   1, 151},
                {182,   2, 149},
                {185,   4, 149},
                {188,   5, 147},
                {191,   6, 146},
                {193,   8, 144},
                {195,  11, 142},
                {198,  13, 139},
                {201,  17, 135},
                {203,  20, 132},
                {206,  23, 127},
                {208,  26, 121},
                {210,  29, 116},
                {212,  33, 111},
                {214,  37, 103},
                {217,  41,  97},
                {219,  46,  89},
                {221,  49,  78},
                {223,  53,  66},
                {224,  56,  54},
                {226,  60,  42},
                {228,  64,  30},
                {229,  68,  25},
                {231,  72,  20},
                {232,  76,  16},
                {234,  78,  12},
                {235,  82,  10},
                {236,  86,   8},
                {237,  90,   7},
                {238,  93,   5},
                {239,  96,   4},
                {240, 100,   3},
                {241, 103,   3},
                {241, 106,   2},
                {242, 109,   1},
                {243, 113,   1},
                {244, 116,   0},
                {244, 120,   0},
                {245, 125,   0},
                {246, 129,   0},
                {247, 133,   0},
                {248, 136,   0},
                {248, 139,   0},
                {249, 142,   0},
                {249, 145,   0},
                {250, 149,   0},
                {251, 154,   0},
                {252, 159,   0},
                {253, 163,   0},
                {253, 168,   0},
                {253, 172,   0},
                {254, 176,   0},
                {254, 179,   0},
                {254, 184,   0},
                {254, 187,   0},
                {254, 191,   0},
                {254, 195,   0},
                {254, 199,   0},
                {254, 202,   1},
                {254, 205,   2},
                {254, 208,   5},
                {254, 212,   9},
                {254, 216,  12},
                {255, 219,  15},
                {255, 221,  23},
                {255, 224,  32},
                {255, 227,  39},
                {255, 229,  50},
                {255, 232,  63},
                {255, 235,  75},
                {255, 238,  88},
                {255, 239, 102},
                {255, 241, 116},
                {255, 242, 134},
                {255, 244, 149},
                {255, 245, 164},
                {255, 247, 179},
                {255, 248, 192},
                {255, 249, 203},
                {255, 251, 216},
                {255, 253, 228},
                {255, 254, 239},
                {255, 255, 249},
                {255, 255, 249},
                {255, 255, 249},
                {255, 255, 249},
                {255, 255, 249},
                {255, 255, 249},
                {255, 255, 249},
                {255, 255, 249} };

        #endregion iron

        // 彩虹色带映射表
        #region rain
        public static byte[,] rainTable = new byte[128, 3]
        {
            {0,   0,   0},
            {0,   0,   0},
            {15,   0,  15},
            {31,   0,  31},
            {47,   0,  47},
            {63,   0,  63},
            {79,   0,  79},
            {95,   0,  95},
            {111,   0, 111},
            {127,   0, 127},
            {143,   0, 143},
            {159,   0, 159},
            {175,   0, 175},
            {191,   0, 191},
            {207,   0, 207},
            {223,   0, 223},
            {239,   0, 239},
            {255,   0, 255},
            {239,   0, 250},
            {223,   0, 245},
            {207,   0, 240},
            {191,   0, 236},
            {175,   0, 231},
            {159,   0, 226},
            {143,   0, 222},
            {127,   0, 217},
            {111,   0, 212},
            {95,   0, 208},
            {79,   0, 203},
            {63,   0, 198},
            {47,   0, 194},
            {31,   0, 189},
            {15,   0, 184},
            {0,   0, 180},
            {0,  15, 184},
            {0,  31, 189},
            {0,  47, 194},
            {0,  63, 198},
            {0,  79, 203},
            {0,  95, 208},
            {0, 111, 212},
            {0, 127, 217},
            {0, 143, 222},
            {0, 159, 226},
            {0, 175, 231},
            {0, 191, 236},
            {0, 207, 240},
            {0, 223, 245},
            {0, 239, 250},
            {0, 255, 255},
            {0, 245, 239},
            {0, 236, 223},
            {0, 227, 207},
            {0, 218, 191},
            {0, 209, 175},
            {0, 200, 159},
            {0, 191, 143},
            {0, 182, 127},
            {0, 173, 111},
            {0, 164,  95},
            {0, 155,  79},
            {0, 146,  63},
            {0, 137,  47},
            {0, 128,  31},
            {0, 119,  15},
            {0, 110,   0},
            {15, 118,   0},
            {30, 127,   0},
            {45, 135,   0},
            {60, 144,   0},
            {75, 152,   0},
            {90, 161,   0},
            {105, 169,  0},
            {120, 178,  0},
            {135, 186,  0},
            {150, 195,  0},
            {165, 203,  0},
            {180, 212,  0},
            {195, 220,  0},
            {210, 229,  0},
            {225, 237,  0},
            {240, 246,  0},
            {255, 255,  0},
            {251, 240,  0},
            {248, 225,  0},
            {245, 210,  0},
            {242, 195,  0},
            {238, 180,  0},
            {235, 165,  0},
            {232, 150,  0},
            {229, 135,  0},
            {225, 120,  0},
            {222, 105,  0},
            {219,  90,  0},
            {216,  75,  0},
            {212,  60,  0},
            {209,  45,  0},
            {206,  30,  0},
            {203,  15,  0},
            {200,   0,  0},
            {202,  11,  11},
            {205,  23,  23},
            {207,  34,  34},
            {210,  46,  46},
            {212,  57,  57},
            {215,  69,  69},
            {217,  81,  81},
            {220,  92,  92},
            {222, 104, 104},
            {225, 115, 115},
            {227, 127, 127},
            {230, 139, 139},
            {232, 150, 150},
            {235, 162, 162},
            {237, 173, 173},
            {240, 185, 185},
            {242, 197, 197},
            {245, 208, 208},
            {247, 220, 220},
            {250, 231, 231},
            {252, 243, 243},
            {252, 243, 243},
            {252, 243, 243},
            {252, 243, 243},
            {252, 243, 243},
            {252, 243, 243},
            {252, 243, 243},
            {252, 243, 243}
        };

        #endregion rain

        #region SSZNColor
        public static UInt32[] SSZNColor = new UInt32[256]{
  0x00000000, 0x000004ff, 0x000008ff, 0x00000dff, 0x000011ff, 0x000015ff, 0x000019ff, 0x00001eff,
    0x000022ff, 0x000026ff, 0x00002aff, 0x00002fff, 0x000033ff, 0x000037ff, 0x00003cff, 0x000040ff,
    0x000044ff, 0x000048ff, 0x00004dff, 0x000051ff, 0x000055ff, 0x000059ff, 0x00005eff, 0x000062ff,
    0x000066ff, 0x00006aff, 0x00006fff, 0x000073ff, 0x000077ff, 0x00007bff, 0x000080ff, 0x000084ff,
    0x000088ff, 0x00008cff, 0x000091ff, 0x000095ff, 0x000099ff, 0x00009dff, 0x0000a2ff, 0x0000a6ff,
    0x0000aaff, 0x0000aeff, 0x0000b3ff, 0x0000b7ff, 0x0000bbff, 0x0000bfff, 0x0000c4ff, 0x0000c8ff,
    0x0000ccff, 0x0000d0ff, 0x0000d5ff, 0x0000d9ff, 0x0000ddff, 0x0000e1ff, 0x0000e6ff, 0x0000eaff,
    0x0000eeff, 0x0000f2ff, 0x0000f7ff, 0x0000fbff, 0x0000ffff, 0x0000fffb, 0x0000fff6, 0x0000fff2,
    0x0000ffee, 0x0000ffea, 0x0000ffe5, 0x0000ffe1, 0x0000ffdd, 0x0000ffd9, 0x0000ffd4, 0x0000ffd0,
    0x0000ffcc, 0x0000ffc8, 0x0000ffc3, 0x0000ffbf, 0x0000ffbb, 0x0000ffb7, 0x0000ffb3, 0x0000ffae,
    0x0000ffaa, 0x0000ffa6, 0x0000ffa2, 0x0000ff9d, 0x0000ff99, 0x0000ff95, 0x0000ff91, 0x0000ff8c,
    0x0000ff88, 0x0000ff84, 0x0000ff80, 0x0000ff7b, 0x0000ff77, 0x0000ff73, 0x0000ff6f, 0x0000ff6a,
    0x0000ff66, 0x0000ff62, 0x0000ff5e, 0x0000ff59, 0x0000ff55, 0x0000ff51, 0x0000ff4d, 0x0000ff48,
    0x0000ff44, 0x0000ff40, 0x0000ff3c, 0x0000ff37, 0x0000ff33, 0x0000ff2f, 0x0000ff2b, 0x0000ff26,
    0x0000ff22, 0x0000ff1e, 0x0000ff1a, 0x0000ff15, 0x0000ff11, 0x0000ff0d, 0x0000ff09, 0x0000ff04,
    0x0000ff00, 0x0004ff00, 0x0008ff00, 0x000dff00, 0x0011ff00, 0x0015ff00, 0x001aff00, 0x001eff00,
    0x0022ff00, 0x0026ff00, 0x002aff00, 0x002fff00, 0x0033ff00, 0x0037ff00, 0x003cff00, 0x0040ff00,
    0x0044ff00, 0x0048ff00, 0x004cff00, 0x0051ff00, 0x0055ff00, 0x0059ff00, 0x005eff00, 0x0062ff00,
    0x0066ff00, 0x006aff00, 0x006eff00, 0x0073ff00, 0x0077ff00, 0x007bff00, 0x0080ff00, 0x0084ff00,
    0x0088ff00, 0x008cff00, 0x0091ff00, 0x0095ff00, 0x0099ff00, 0x009dff00, 0x00a2ff00, 0x00a6ff00,
    0x00aaff00, 0x00aeff00, 0x00b3ff00, 0x00b7ff00, 0x00bbff00, 0x00bfff00, 0x00c3ff00, 0x00c8ff00,
    0x00ccff00, 0x00d0ff00, 0x00d5ff00, 0x00d9ff00, 0x00ddff00, 0x00e1ff00, 0x00e5ff00, 0x00eaff00,
    0x00eeff00, 0x00f2ff00, 0x00f7ff00, 0x00fbff00, 0x00ffff00, 0x00fffb00, 0x00fff700, 0x00fff200,
    0x00ffee00, 0x00ffea00, 0x00ffe500, 0x00ffe100, 0x00ffdd00, 0x00ffd900, 0x00ffd500, 0x00ffd000,
    0x00ffcc00, 0x00ffc800, 0x00ffc300, 0x00ffbf00, 0x00ffbb00, 0x00ffb700, 0x00ffb300, 0x00ffae00,
    0x00ffaa00, 0x00ffa600, 0x00ffa200, 0x00ff9d00, 0x00ff9900, 0x00ff9500, 0x00ff9100, 0x00ff8c00,
    0x00ff8800, 0x00ff8400, 0x00ff8000, 0x00ff7b00, 0x00ff7700, 0x00ff7300, 0x00ff6e00, 0x00ff6a00,
    0x00ff6600, 0x00ff6200, 0x00ff5e00, 0x00ff5900, 0x00ff5500, 0x00ff5100, 0x00ff4c00, 0x00ff4800,
    0x00ff4400, 0x00ff4000, 0x00ff3c00, 0x00ff3700, 0x00ff3300, 0x00ff2f00, 0x00ff2a00, 0x00ff2600,
    0x00ff2200, 0x00ff1e00, 0x00ff1a00, 0x00ff1500, 0x00ff1100, 0x00ff0d00, 0x00ff0800, 0x00ff0400,
    0x00ff0000, 0x00ff0004, 0x00ff0008, 0x00ff000d, 0x00ff0011, 0x00ff0015, 0x00ff0019, 0x00ff001e,
    0x00ff0022, 0x00ff0026, 0x00ff002b, 0x00ff002f, 0x00ff0033, 0x00ff0037, 0x00ff003c, 0x00ff0040 };

        #endregion SSZNColor

    }
    #endregion
}
