using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DeviceFunctionDriver
{
    public class Qworks
    {

        public static int MaxDevsSum = 32;

        public delegate void FuncIntHandler(IntPtr pData);
        public struct S_SensorInfo
        {
            double F0VCCINT;
            double F0VCCAUX;
            double F0Temperature;
            double F1VCCINT;
            double F1VCCAUX;
            double F1Temperature;
            double F1VCCINTMAX;
            double F1VCCAUXMAX;
            double F1TemperatureMax;
            double QGFVoltage12;
            double QGFCurrent12;
            double QGFVoltage5;
            double QGFCurrent5;
            double QGFTemperature0;
            double QGFTemperature1;
            double QGFTemperature2;
            double QGFTemperature3;
        };

        /****************************************************************************
         * Function:    Qworks_Version
         * Description: 获取Qworks版本信息
         * Input:       无
         * Output:      Version: 版本信息
         * Return:      参考错误列表EnumError
         * Other:       无
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_Version", CharSet = CharSet.Auto , CallingConvention = CallingConvention.Cdecl)]
        public static extern int Version(ref uint version);

        /****************************************************************************
         * Function:    Qworks_PCIInit
         * Description: 初始化pci版本的Qworks,目前已经不用
         * Input:       DeviceID:DeviceID
         *				VendorID:VendorID
         * Output:      DevSum:板卡数量一个bit代表一个板卡最多32个
         * Return:      参考错误列表EnumError
         * Other:       无
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_PCIInit", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PCIInit(uint DeviceID, uint VendorID, ref uint DevSum);


        /****************************************************************************
         * Function:    Qworks_PcieInit(不应再使用，应该用Qworks_Initialize)
         * Description: 初始化Qworks,接口类型为pci
         * Input:       DeviceID:DeviceID
         *				VendorID:VendorID
         * Output:      DevSum:查找到板卡数量1个bit代表一个板卡最多16个
         * Return:      参考错误列表EnumError
         * Other:       无
          ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_PcieInit", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PcieInit(uint DeviceID, uint VendorID, ref uint DevSum);

        /****************************************************************************
         * Function:    Qworks_TcpInit
         * Description: 初始化Qworks,接口类型为tcp
         * Input:       IPAddr: ip地址最多16个
                        CountSum: 之前已经成功初始化的设备数
         * Output:      DevSum: 查找到板卡数量1个bit代表一个板卡最多16个
         * Return:      参考错误列表EnumError
         * Other:       无
        ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_TcpInit", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int TcpInit(byte[,] IPAddr, uint CountSum);

        /****************************************************************************
         * Function:    Qworks_IpConfig
         * Description: 初始化Qworks,接口类型为tcp
         * Input:       IPAddr: ip地址最多16个
         * Output:      DevSum: 查找到板卡数量1个bit代表一个板卡最多16个
         * Return:      参考错误列表EnumError
         * Other:       无
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_IpConfig", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int IpConfigFunc(byte[] IPAddr, uint DevNum);


        /****************************************************************************
         * Function:    Qworks_Initialize
         * Description: 初始化Qworks
         * Input:       无
         * Output:      DevSum: 查找到板卡数量1个bit代表一个板卡最多16个
         * Return:      参考错误列表EnumError
         * Other:       无
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_Initialize", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Initialize(ref uint DevSum);

        /****************************************************************************
         * Function:    Qworks_Close
         * Description: 中断Qworks链接
         * Input:       无
         * Output:      无
         * Return:      参考错误列表EnumError
         * Other:       无
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_Close", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Close();

        /****************************************************************************
         * Function:    Qworks_Progress
         * Description: 获取板卡类型
         * Input:       DevNum: 板卡号
         * Output:      无
         * Return:      当前操作进度
         *              Qworks_QNFErase
         *              Qworks_QNFRead
         *              Qworks_QNFWrite
         *              Qworks_QNFNorRead
         *              Qworks_QNFNorWrite
         * Other:       无
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_Progress", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Progress(uint DevNum);


        /****************************************************************************
         * Function:    Qworks_ScanOpenDevice
         * Description: 打开pci/pci-e设备
         * Input:       DeviceID: Device ID
         *				VendorID: Vendor ID
         * Output:      Devs: 仪器句柄
         *				CardSum: 查找到板卡数量1个bit代表一个板卡最多32个
         * Return:      参考错误列表EnumError
         * Other:       无
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_ScanOpenDevice", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ScanOpenDevice(uint DeviceID, uint VendorID, ref IntPtr[] Devs, ref uint DevSum);


        /****************************************************************************
         * Function:    Qworks_PciReadReg32
         * Description: 从pci/pci-e某个地址中读取32bit数据
         * Input:       Devs: 仪器句柄
         *				AddrSpace: 基地址
         *              OffSet: 偏移地址
         * Output:      *Val: 读取pci/pci-e某地址的值
         * Return:      参考错误列表EnumError
         * Other:       无
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_PciReadReg32", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PciReadReg32(IntPtr Devs, uint AddrSpace, uint OffSet, ref uint Val);

        /****************************************************************************
         * Function:    Qworks_PciWriteReg32
         * Description: 向pci/pci-e某个地址中写入32bit数据
         * Input:       Devs: 仪器句柄
         *				AddrSpace: 基地址
         *              OffSet: 偏移地址
         *              Val: 写入pci/pci-e某地址的值
         * Output:      无
         * Return:      参考错误列表EnumError
         * Other:       无
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_PciWriteReg32", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PciWriteReg32(IntPtr Devs, uint AddrSpace, uint OffSet, uint Val);


        /****************************************************************************
         * Function:    Qworks_PciReadAddrBlock
         * Description: 从pci/pci-e某个地址中读取固定长度的数据
         * Input:       Devs: 仪器句柄
         *				AddrSpace: 基地址
         *              OffSet: 偏移地址
         *              Bytes: 读取数据的长度
         * Output:      *pData: 数据指针
         * Return:      参考错误列表EnumError
         * Other:       无
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_PciReadAddrBlock", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PciReadAddrBlock(IntPtr Devs, uint AddrSpace, uint OffSet, uint Bytes, string pData);


        /****************************************************************************
         * Function:    Qworks_PciWriteAddrBlock
         * Description: 向pci/pci-e某个地址中写入固定长度的数据
         * Input:       Devs: 仪器句柄
         *				AddrSpace: 基地址
         *              OffSet: 偏移地址
         *              Bytes: 读取数据的长度
         *              *pData: 数据指针
         * Output:      无
         * Return:      参考错误列表EnumError
         * Other:       无
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_PciWriteAddrBlock", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PciWriteAddrBlock(IntPtr Devs, uint AddrSpace, uint OffSet, uint Bytes, string pData);


        /****************************************************************************
         * Function:    Qworks_PciReadRegRoot
         * Description: 从pci/pci-e某个地址中读取32bit数据
         * Input:       DevNum: 板卡号
         *				AddrSpace: 基地址
         *              OffSet: 偏移地址
         * Output:      *Val: 读取pci/pci-e某地址的值
         * Return:      参考错误列表EnumError
         * Other:       无
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_PciReadRegRoot", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PciReadRegRoot(uint DevNum, uint AddrSpace, uint OffSet, ref uint Val);


        /****************************************************************************
         * Function:    Qworks_PciWriteRegRoot
         * Description: 向pci/pci-e某个地址中写入32bit数据
         * Input:       DevNum: 板卡号
         *				AddrSpace: 基地址
         *              OffSet: 偏移地址
         *              Val: 写入pci/pci-e某地址的值
         * Output:      无
         * Return:      参考错误列表EnumError
         * Other:       无
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_PciWriteRegRoot", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PciWriteRegRoot(uint DevNum, uint AddrSpace, uint OffSet, uint Val);

        /****************************************************************************
         * Function:    Qworks_SysReset
         * Description: 板卡复位
         * Input:       DevNum: 板卡号
         * Output:      无
         * Return:      参考错误列表EnumError
         * Other:       无
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_SysReset", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SysReset(uint DevNum);


        /****************************************************************************
        * Function:    Qworks_SetBoardInfo
        * Description: 
        ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_SetBoardInfo", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SetBoardInfo(ref uint data, uint Type, uint DevNum);


        /****************************************************************************
        * Function:    Qworks_GetBoardInfo
        * Description: 
        ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_GetBoardInfo", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetBoardInfo(ref uint data, uint Type, uint DevNum);


        /****************************************************************************
       * Function:    Qworks_F0ReadReg
       * Description: 
       ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_F0ReadReg", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int F0ReadReg(ref uint RegData, uint RegNum, uint DevNum);

        /****************************************************************************
       * Function:    Qworks_F0WriteReg
       * Description: 
       ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_F0WriteReg", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int F0WriteReg(uint RegData, uint RegNum, uint DevNum);


        /****************************************************************************
         * Function:    Qworks_F0Info
         * Description: 获取FPGA0信息
         * Input:       Cmd: 获取信息命令 参考EnumF0Info
         *				DevNum: 板卡号
         * Output:      *Info: FPGA0信息
         * Return:      参考错误列表EnumError
         * Other:       无
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_F0Info", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int F0Info(ref uint Info, uint Cmd, uint DevNum);


        /****************************************************************************
         * Function:    Qworks_SensorInfo
         * Description: 获取SensorInfo信息
    
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_SensorInfo", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SensorInfo(ref S_SensorInfo Info, uint DevNum);


        /****************************************************************************
         * Function:    Qworks_F1Info
         * Description: 获取FPGA1信息
         * Input:       Cmd: 获取信息命令 参考EnumF0Info
         *				DevNum: 板卡号
         * Output:      *Info: FPGA0信息
         * Return:      参考错误列表EnumError
         * Other:       无
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_F1Info", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int F1Info(ref uint Info, uint Cmd, uint DevNum);


        /****************************************************************************
         * Function:    Qworks_F1ReadReg
         * Description: 读FPGA1某一个寄存器
         * Input:       RegNum: 寄存器编号 (0~31)
         *				DevNum: 板卡号
         * Output:      *RegData: 寄存器值
         * Return:      参考错误列表EnumError
         * Other:       无
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_F1ReadReg", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int F1ReadReg(ref uint RegData, uint RegNum, uint DevNum);


        /****************************************************************************
         * Function:    Qworks_F1ReadAllReg
         * Description: 读FPGA1所有寄存器
         * Input:       DevNum: 板卡号
         * Output:      RegData: 寄存器值 大小128byte
         * Return:      参考错误列表EnumError
         * Other:       无
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_F1ReadAllReg", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int F1ReadAllReg(uint[] RegData, uint DevNum);


        /****************************************************************************
         * Function:    Qworks_F1WriteReg
         * Description: 写入FPGA1某一个寄存器
         * Input:       RegData: 寄存器值
         *              RegNum: 寄存器编号 (0~31)
         *				DevNum: 板卡号
         * Output:      无
         * Return:      参考错误列表EnumError
         * Other:       无
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_F1WriteReg", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int F1WriteReg(uint RegData, uint RegNum, uint DevNum);


        /****************************************************************************
         * Function:    Qworks_F1WriteAllReg
         * Description: 读FPGA1所有寄存器
         * Input:       DevNum: 板卡号
         * Output:      RegData: 寄存器值 大小128byte
         * Return:      参考错误列表EnumError
         * Other:       无
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_F1WriteAllReg", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int F1WriteAllReg(uint[] RegData, uint DevNum);

        /****************************************************************************
         * Function:    Qworks_F1RamSize
         * Description: 获取F1Ram大小
         * Input:       DevNum: 板卡号
         * Output:      *Bytes: F1Ram大小 byte
         * Return:      参考错误列表EnumError
         * Other:       无
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_F1RamSize", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int F1RamSize(ref uint Bytes, uint DevNum);

        /****************************************************************************
         * Function:    Qworks_F1ReadRam
         * Description: 读取F1Ram中数据
         * Input:       DevNum: 板卡号
         *              Bytes: 读取数据大小(8192Byte整数倍)
         * Output:      *pData: 数据
         *              *SuccessBytes: 实际读取数大小
         *              *Prog: 当前读取进度
         * Return:      参考错误列表EnumError
         * Other:       无
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_F1ReadRam", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int F1ReadRam(string pData, uint Bytes, uint DevNum, ref uint SuccessBytes, ref uint Prog);


        /****************************************************************************
         * Function:    Qworks_F1ReadRamAddr
         * Description: 读取F1Ram中数据
         * Input:       DevNum: 板卡号
         *              Addr: 写入的数据地址(8192Byte整数倍)
         *              Bytes: 写入数据大小(8192Byte整数倍)
         * Output:      *pData: 数据
         *              *SuccessBytes: 实际写入数大小
         *              *Prog: 当前读取进度
         * Return:      参考错误列表EnumError
         * Other:       无
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_F1ReadRamAddr", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int F1ReadRamAddr(string pData, uint Addr, uint Bytes, uint DevNum, ref uint SuccessBytes, ref uint Prog);


        /****************************************************************************
         * Function:    Qworks_F1ReadRamFile
         * Description: 读取F1Ram中数据并保存为文件
         * Input:       DevNum: 板卡号
         *              Bytes: 读取数据大小(8192Byte整数倍)
         *              *FilePath: 存储文件路径
         * Output:      *SuccessBytes: 实际读取数大小
         *              *Prog: 当前读取进度
         * Return:      参考错误列表EnumError
         * Other:       无
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_F1ReadRamFile", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int F1ReadRamFile(string FilePath, uint Bytes, uint DevNum, ref uint SuccessBytes, ref uint Prog);

        /****************************************************************************
         * Function:    Qworks_F1WriteRam
         * Description: 将数据写入F1Ram
         * Input:       DevNum: 板卡号
         *              Bytes: 写入数据大小(8192Byte整数倍)
         *              *pData: 数据
         * Output:      *SuccessBytes: 实际写入数大小
         *              *Prog: 当前读取进度
         * Return:      参考错误列表EnumError
         * Other:       无
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_F1WriteRam", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int F1WriteRam(string pData, uint Bytes, uint DevNum, ref uint SuccessBytes, ref uint Prog);


        /****************************************************************************
         * Function:    Qworks_F1WriteRamAddr
         * Description: 将数据写入F1Ram
         * Input:       DevNum: 板卡号
         *              Addr: 写入的数据地址(8192Byte整数倍)
         *              Bytes: 写入数据大小(8192Byte整数倍)
         *              *pData: 数据
         * Output:      *SuccessBytes: 实际写入数大小
         *              *Prog: 当前读取进度
         * Return:      参考错误列表EnumError
         * Other:       无
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_F1WriteRamAddr", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int F1WriteRamAddr(string pData, uint Addr, uint Bytes, uint DevNum, ref uint SuccessBytes, ref uint Prog);


        /****************************************************************************
         * Function:    Qworks_F1WriteRamFile
         * Description: 将文件中数据写入F1Ram
         * Input:       DevNum: 板卡号
         *              Bytes: 写入数据大小(8192Byte整数倍)
         *              *pData: 数据
         * Output:      *SuccessBytes: 实际写入数大小
         *              *Prog: 当前读取进度
         * Return:      参考错误列表EnumError
         * Other:       无
        ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_F1WriteRamFile", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int F1WriteRamFile(string FilePath, uint Bytes, uint DevNum, ref uint SuccessBytes, ref uint Prog);


        /****************************************************************************
         * Function:    Qworks_F1Ram2Size
         * Description: 获取F1Ram2大小
         * Input:       DevNum: 板卡号
         * Output:      *Bytes: F1Ram2大小 byte
         * Return:      参考错误列表EnumError
         * Other:       无
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_F1Ram2Size", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int F1Ram2Size(ref uint Bytes, uint DevNum);


        /****************************************************************************
         * Function:    Qworks_F1ReadRa2mAddr
         * Description: 读取F1Ram2中数据
         * Input:       DevNum: 板卡号
         *              Addr: 写入的数据地址(8192Byte整数倍)
         *              Bytes: 写入数据大小(8192Byte整数倍)
         * Output:      *pData: 数据
         *              *SuccessBytes: 实际写入数大小
         *              *Prog: 当前读取进度
         * Return:      参考错误列表EnumError
         * Other:       无
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_F1ReadRam2Addr", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int F1ReadRam2Addr(string pData, uint Addr, uint Bytes, uint DevNum, ref uint SuccessBytes, ref uint Prog);

        /****************************************************************************
         * Function:    Qworks_F1WriteRam2Addr
         * Description: 将数据写入F1Ram2
         * Input:       DevNum: 板卡号
         *              Addr: 写入的数据地址(8192Byte整数倍)
         *              Bytes: 写入数据大小(8192Byte整数倍)
         *              *pData: 数据
         * Output:      *SuccessBytes: 实际写入数大小
         *              *Prog: 当前读取进度
         * Return:      参考错误列表EnumError
         * Other:       无
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_F1WriteRam2Addr", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int F1WriteRam2Addr(string pData, uint Addr, uint Bytes, uint DevNum, ref uint SuccessBytes, ref uint Prog);

        /****************************************************************************
         * Function:    Qworks_F1BitLoad
         * Description: 将bit文件加载至FPGA1
         * Input:       DevNum: 板卡号
         *              *FilePath: 文件路径
         * Output:      *Prog: 当前读取进度
         * Return:      参考错误列表EnumError
         * Other:       无
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_F1BitLoad", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int F1BitLoad(string FilePath, uint DevNum, ref uint Prog);


        /****************************************************************************
         * Function:    Qworks_F1BitSolidification
         * Description: 将bit文件烧写至FPGA1
         * Input:       DevNum: 板卡号
         *              *FilePath: 文件路径
         *              AddrSpace: 文件存储位置
         * Output:      *Prog: 当前读取进度
         * Return:      参考错误列表EnumError
         * Other:       无
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_F1BitSolidification", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int F1BitSolidification(string FilePath, uint AddrSpace, uint DevNum, ref uint Prog);


        /****************************************************************************
         * Function:    Qworks_F1FlashBitLoad
         * Description: 将已经烧写完的bit文件加载至FPGA1
         * Input:       DevNum: 板卡号
         *              AddrSpace: 文件存储位置
         * Output:      无
         * Return:      参考错误列表EnumError
         * Other:       无
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_F1FlashBitLoad", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int F1FlashBitLoad(uint AddrSpace, uint DevNum);

        /****************************************************************************
         * Function:    Qworks_F1bitDefaultLoad
         * Description: 设置上电默认从某个存储位置将已经烧写完的bit文件加载至FPGA1
         * Input:       DevNum: 板卡号
         * AddrSpace:   文件存储位置
         * Output:      无
         * Return:      参考错误列表EnumError
         * Other:       无
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_F1bitDefaultLoad", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int F1bitDefaultLoad(uint AddrSpace, uint DevNum);


        /****************************************************************************
         * Function:    Qworks_F1IRQEnable
         * Description: 接收FPGA发出的中断
         * Input:       DevNum: 板卡号
         *              FuncIntHandler: 函数头(需要用QWORKSCALLFUNC修饰)
         * Output:      无
         * Return:      参考错误列表EnumError
         * Other:       无
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_F1IRQEnable", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int F1IRQEnable(FuncIntHandler IntHandler, IntPtr pdata, uint DevNum);


        /****************************************************************************
         * Function:    Qworks_F1IRQDisable
         * Description: 停止FPGA发出的中断
         * Input:       DevNum: 板卡号
         * Output:      无
         * Return:      参考错误列表EnumError
         * Other:       无
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_F1IRQDisable", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int F1IRQDisable(uint DevNum);


        /****************************************************************************
         * Function:    Qworks_F1PulseEnable
         * Description: FPGA开始发送脉冲
         * Input:       DevNum: 板卡号
         * Output:      无
         * Return:      参考错误列表EnumError
         * Other:       无
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_F1PulseEnable", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int F1PulseEnable(uint DevNum);

        /****************************************************************************
         * Function:    Qworks_F1PulseDisable
         * Description: FPGA停止发送脉冲
         * Input:       DevNum: 板卡号
         * Output:      无
         * Return:      参考错误列表EnumError
         * Other:       无
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_F1PulseDisable", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int F1PulseDisable(uint DevNum);


        /****************************************************************************
         * Function:    Qworks_GridQDRRead
         * Description: 读取QDR中数据
         * Input:       DevNum: 板卡号
         *              QDRType: QDR类型，参考EnumQDRType
         * Output:      *pData: 读取数据
         *              *Prog: 当前读取进度
         * Return:      参考错误列表EnumError
         * Other:       无
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_GridQDRRead", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GridQDRRead(string pData, uint Addr, uint Count, uint DevNum, ref uint Prog);

        /****************************************************************************
         * Function:    Qworks_GridQDRReadFile
         * Description: 读取QDR中数据并保存文件
         * Input:       DevNum: 板卡号
         *              *FilePath: 读取数据存储路径
         *              QDRType: QDR类型，参考EnumQDRType
         * Output:      *Prog: 当前读取进度
         * Return:      参考错误列表EnumError
         * Other:       无
        ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_GridQDRReadFile", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GridQDRReadFile(string FilePath, uint QDRType, uint DevNum, ref uint Prog);


        /****************************************************************************
         * Function:    Qworks_GridQDRWrite
         * Description: 将数据写入QDR
         * Input:       DevNum: 板卡号
         *              *pData: 数据  (大小为9MB或18MB)
         *              QDRType: QDR类型，参考EnumQDRType
         * Output:      *Prog: 当前写入进度
         * Return:      参考错误列表EnumError
         * Other:       无
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_GridQDRWrite", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GridQDRWrite(string pData, uint Addr, uint Count, uint DevNum, ref uint Prog);

        /****************************************************************************
         * Function:    Qworks_GridQDRWriteFile
         * Description: 将文件中数据写入QDR
         * Input:       DevNum: 板卡号
         *              *FilePath: 数据文件路径  (大小为9MB或18MB)
         *              QDRType: QDR类型，参考EnumQDRType
         * Output:      *Prog: 当前写入进度
         * Return:      参考错误列表EnumError
         * Other:       无
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_GridQDRWriteFile", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GridQDRWriteFile(string FilePath, uint QDRType, uint DevNum, ref uint Prog);


        /****************************************************************************
         * Function:    Qworks_GridSLCErase
         * Description: 擦除SLC
         * Input:       DevNum: 板卡号
         *              StartBlock: 起始块地址
         *              StopBlock: 结束块地址
         * Output:      无
         * Return:      参考错误列表EnumError
         * Other:       SLC一块为16MB,2000块
         * ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_GridSLCErase", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GridSLCErase(uint StartBlock, uint StopBlock, uint DevNum, ref uint Prog);

        /****************************************************************************
         * Function:    Qworks_GridSLCRead
         * Description: 读取SLC中数据
         * Input:       DevNum: 板卡号
         *              StartBlock: 起始块地址
         *              StopBlock: 结束块地址
         * Output:      *pData: 数据
         *              *Prog: 当前读取进度
         * Return:      参考错误列表EnumError
         * Other:       SLC一块为16MB,2000块
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_GridSLCRead", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GridSLCRead(string pData, uint StartBlock, uint StopBlock, uint DevNum, ref uint Prog);


        /****************************************************************************
         * Function:    Qworks_GridSLCReadFile
         * Description: 读取SLC中数据并保存文件
         * Input:       DevNum: 板卡号
         *              StartBlock: 起始块地址
         *              StopBlock: 结束块地址
         * Output:      *pData: 数据
         *              *Prog: 当前读取进度
         * Return:      参考错误列表EnumError
         * Other:       SLC一块为16MB,2000块
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_GridSLCReadFile", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GridSLCReadFile(string FilePath, uint StartBlock, uint StopBlock, uint DevNum, ref uint Prog);


        /****************************************************************************
         * Function:    Qworks_GridSLCWrite
         * Description: 向SLC中写入数据
         * Input:       DevNum: 板卡号
         *              StartBlock: 起始块地址
         *              StopBlock: 结束块地址
         *              *pData: 数据
         * Output:      *Prog: 当前读取进度
         * Return:      参考错误列表EnumError
         * Other:       SLC一块为16MB,2000块
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_GridSLCWrite", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GridSLCWrite(string pData, uint StartBlock, uint StopBlock, uint DevNum, ref uint Prog);


        /****************************************************************************
         * Function:    Qworks_GridSLCWriteFile
         * Description: 将文件内容写入SLC
         * Input:       DevNum: 板卡号
         *              StartBlock: 起始块地址
         *              StopBlock: 结束块地址
         *       		*FilePath: 文件路径
         * Output:		*Prog: 当前读取进度
         * Return:      参考错误列表EnumError
         * Other:       SLC一块为16MB,2000块
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_GridSLCWriteFile", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GridSLCWriteFile(string FilePath, uint StartBlock, uint StopBlock, uint DevNum, ref uint Prog);

        /****************************************************************************
         * Function:    Qworks_GridSLC128Erase
         * Description: 擦除SLC
         * Input:       DevNum: 板卡号
         *              StartBlock: 起始块地址
         *              StopBlock: 结束块地址
         * Output:      无
         * Return:      参考错误列表EnumError
         * Other:       SLC一块为128MB,1024块
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_GridSLC128EraseBlock", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GridSLC128EraseBlock(uint StartBlock, uint StopBlock, uint DevNum, ref uint Prog);


        /****************************************************************************
         * Function:    Qworks_GridSLC128Read
         * Description: 读取SLC中数据
         * Input:       DevNum: 板卡号
         *              StartBlock: 起始块地址
         *              StopBlock: 结束块地址
         * Output:      *pData: 数据
         *              *Prog: 当前读取进度
         * Return:      参考错误列表EnumError
         * Other:       SLC一块为128MB,1024块
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_GridSLC128ReadBlock", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GridSLC128ReadBlock(string pData, uint StartBlock, uint StopBlock, uint DevNum, ref uint Prog);


        /****************************************************************************
         * Function:    Qworks_GridSLC128Write
         * Description: 向SLC中写入数据
         * Input:       DevNum: 板卡号
         *              StartBlock: 起始块地址
         *              StopBlock: 结束块地址
         *              *pData: 数据
         * Output:      *Prog: 当前读取进度
         * Return:      参考错误列表EnumError
         * Other:       SLC一块为128MB,1024块
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_GridSLC128WriteBlock", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GridSLC128WriteBlock(string pData, uint StartBlock, uint StopBlock, uint DevNum, ref uint Prog);

        /****************************************************************************
         * Function:    Qworks_GridSD4GRead
         * Description: 读取SD4G中数据
         * Input:       DevNum: 板卡号
         *              Addr:   RAM地址(byte)
         *              Bytes:  数据长度(byte)
         * Output:      *pData: 数据
         *              *Prog:  当前读取进度
         * Return:      参考错误列表EnumError
         * Other:
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_GridSD4GRead", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GridSD4GRead(string pData, uint Addr, uint Bytes, uint DevNum, ref uint Prog);

        /****************************************************************************
         * Function:    Qworks_GridSD4GReadFile 
         * Description: 读取SD4G中数据并保存为文件
         * Input:       DevNum: 板卡号
         *              Addr:   RAM地址(byte)
         *              Bytes:  数据长度(byte)
         *              *FilePath: 文件存储路径
         * Output:      *Prog: 当前读取进度
         * Return:      参考错误列表EnumError
         * Other:
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_GridSD4GReadFile", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GridSD4GReadFile(string FilePath, uint Addr, uint Bytes, uint DevNum, ref uint Prog);


        /****************************************************************************
         * Function:    Qworks_GridSD4GWrite
         * Description: 向SD4G中写入数据
         * Input:       DevNum: 板卡号
         *              Addr:   RAM地址(byte)
         *              Bytes:  数据长度(byte)
         *              *pData: 数据
         * Output:      *Prog: 当前读取进度
         * Return:      参考错误列表EnumError
         * Other:       SD4G共32页，一页为128MB
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_GridSD4GWrite", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GridSD4GWrite(string pData, uint Addr, uint Bytes, uint DevNum, ref uint Prog);


        /****************************************************************************
         * Function:    Qworks_GridSD4GWriteFile
         * Description: 将文件内容写入SD4G
         * Input:       DevNum: 板卡号
         *              Addr:   RAM地址(byte)
         *              Bytes:  数据长度(byte)
         *       		*FilePath: 文件路径
         * Output:      *Prog: 当前读取进度
         * Return:      参考错误列表EnumError
         * Other:       SD4G共32页，一页为128MB
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_GridSD4GWriteFile", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GridSD4GWriteFile(string FilePath, uint Addr, uint Bytes, uint DevNum, ref uint Prog);

        /****************************************************************************
         * Function:    Qworks_StackEyeValueRead
         * Description: 读取QStack中眼值(传说中的窗口值)
         * Input:       DevNum: 板卡号
         *              Switch: 子卡选择,左侧还是右侧,参考EnumBoardType
         * Output:		*EyeValue0: 眼值0
         *              *EyeValue1: 眼值1
         *              *EyeValue2: 眼值2
         *              *EyeValue3: 眼值3
         * Return:      参考错误列表EnumError
         * Other:
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_StackEyeValueRead", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int StackEyeValueRead(uint Switch, ref byte EyeValue0, ref byte EyeValue1, ref byte EyeValue2, ref byte EyeValue3, uint DevNum);


        /****************************************************************************
         * Function:    Qworks_StackEyeValueWrite
         * Description: 向QStack写入眼值(传说中的窗口值)
         * Input:       DevNum: 板卡号
         *              Switch: 子卡选择,左侧还是右侧,参考EnumBoardType
         *              EyeValue0: 眼值0
         *              EyeValue1: 眼值1
         *              EyeValue2: 眼值2
         *              EyeValue3: 眼值3
         * Output:		无
         * Return:      参考错误列表EnumError
         * Other:
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_StackEyeValueWrite", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int StackEyeValueWrite(uint Switch, byte EyeValue0, byte EyeValue1, byte EyeValue2, byte EyeValue3, uint DevNum);


        /****************************************************************************
         * Function:    Qworks_QNFState
         * Description: QNF当前状态
         * Input:       Handle
         * Output:      Null
         * Return:      Please see ErrorList
         * Other:       Null
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_QNFState", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int QNFState(uint DevNum);

        /****************************************************************************
         * Function:    Qworks_QNFInfoState
         * Description: QNF当前状态
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_QNFInfoState", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int QNFInfoState(uint DevNum, ref uint Version, ref double Voltage5, ref double Voltage12, ref double VCCINT, ref double VCCINTMAX, ref double Fpga0Temperature, ref double Fpga0TemperatureMax, ref double Fpga1Temperature,
                        ref double Fpga1TemperatureMax, ref double Temperature0, ref double Temperature1, ref double Temperature2, ref double Temperature3, ref uint Aurora, ref uint LinkType, ref uint QnfType);


        /****************************************************************************
        * Function:    Qworks_QNFInfoQnfRead
        * Description: QNF当前状态
        ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_QNFInfoQnfRead", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int QNFInfoQnfRead(uint DevNum, ref uint CardType, ref uint Revision, ref uint ID, ref uint Year, ref uint Week);


        /****************************************************************************
        * Function:    Qworks_QNFInfoQnfWrite
        * Description: QNF当前状态
        ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_QNFInfoQnfWrite", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int QNFInfoQnfWrite(uint DevNum, uint CardType, uint Revision, uint ID, uint Year, uint Week);


        /****************************************************************************
         * Function:    Qworks_QNFErase
         * Description: QNF擦除存储板数据
         * Input:       Handle
         *              BlockStart
         *              BlockCount
         * Output:      Null
         * Return:      Please see ErrorList
         * Other:       通过Qworks_Progress(DevNum)获得进度(0~100)
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_QNFErase", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int QNFErase(uint DevNum, uint BlockStart, uint BlockCount);


        /****************************************************************************
         * Function:    Qworks_QNFStop
         * Description: QNF停止当前可以停止的操作
         * Input:       Handle
         * Output:      Null
         * Return:      Please see ErrorList
         * Other:       Null
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_QNFStop", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int QNFStop(uint DevNum);


        /****************************************************************************
         * Function:    Qworks_QNFReadFile
         * Description: 读取QNF中数据
         * Input:       Handle
         *              PageStart
         *              PageCount
         *              FilePath
         * Output:      Null
         * Return:      Please see ErrorList
         * Other:       通过Qworks_Progress(DevNum)获得进度(0~100)
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_QNFReadFile", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int QNFReadFile(uint DevNum, uint PageStart, uint PageCount, string FilePath);


        /****************************************************************************
         * Function:    Qworks_QNFWriteFile
         * Description: 向QNF中写入数据
         * Input:       Handle
         *              PageStart
         *              PageCount
         *              FilePath
         * Output:      Null
         * Return:      Please see ErrorList
         * Other:       通过Qworks_Progress(DevNum)获得进度(0~100)
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_QNFWriteFile", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int QNFWriteFile(uint DevNum, uint PageStart, uint PageCount, string FilePath);


        /****************************************************************************
         * Function:    Qworks_QNFGTXSend
         * Description: QNF通过GTX发送数据
         * Input:       Handle
         *              PageStart
         *              PageCount
         *              Length
         * Output:      Null
         * Return:      Please see ErrorList
         * Other:       Null
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_QNFGTXSend", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int QNFGTXSend(uint DevNum, uint PageStart, uint PageCount, uint Length);


        /****************************************************************************
         * Function:    Qworks_QNFGTXSendLoop
         * Description: QNF通过GTX发送数据
         * Input:       Handle
         *              PageStart
         *              PageCount
         *              Length
         * Output:      Null
         * Return:      Please see ErrorList
         * Other:       Null
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_QNFGTXSendLoop", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int QNFGTXSendLoop(uint DevNum, uint PageStart, uint PageCount, uint Length);


        /****************************************************************************
         * Function:    Qworks_QNFGTXRecv
         * Description: QNF通过GTX接收数据
         * Input:       Handle
         *              PageStart
         *              PageCount
         *              Length
         * Output:      Null
         * Return:      Please see ErrorList
         * Other:       Null
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_QNFGTXRecv", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int QNFGTXRecv(uint DevNum, uint PageStart, uint PageCount, uint Length);

        /****************************************************************************
         * Function:    Qworks_QNF4FCSend
         * Description: QNF通过4FC发送数据
         * Input:       Handle
         *              PageStart
         *              PageCount
         *              Length
         * Output:      Null
         * Return:      Please see ErrorList
         * Other:       Null
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_QNF4FCSend", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int QNF4FCSend(uint DevNum, uint PageStart, uint PageCount, uint Length);


        /****************************************************************************
         * Function:    Qworks_QNF4FCRecv
         * Description: QNF通过4FC接收数据
         * Input:       Handle
         *              PageStart
         *              PageCount
         *              Length
         * Output:      Null
         * Return:      Please see ErrorList
         * Other:       Null
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_QNF4FCRecv", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int QNF4FCRecv(uint DevNum, uint PageStart, uint PageCount, uint Length);


        /****************************************************************************
         * Function:    Qworks_QNFNorRead
         * Description: 从QNF中NorFlash读取数据
         * Input:       Handle
         *              Addr
         *              Count
         * Output:      pBuffer
         * Return:      Please see ErrorList
         * Other:       通过Qworks_Progress(DevNum)获得进度(0~100)
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_QNFNorRead", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int QNFNorRead(uint DevNum, uint Addr, uint Count, string pBuffer);


        /****************************************************************************
         * Function:    Qworks_QNFNorWrite
         * Description: 向QNF中NorFlash写入数据
         * Input:       Handle
         *              Addr  (128KB)
         *              Count (128KB)
         *              pBuffer
         * Output:      Null
         * Return:      Please see ErrorList
         * Other:       通过Qworks_Progress(DevNum)获得进度(0~100)
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_QNFNorWrite", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int QNFNorWrite(uint DevNum, uint Addr, uint Count, string pBuffer);

        /****************************************************************************
         * Function:    Qworks_QNFErrorCode
         * Description: 存储板接收，误码数
         * Input:       DevNum
         * Output:      Null
         * Return:      Please see ErrorList
         * Other:       Null
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_QNFErrorCode", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int QNFErrorCode(uint DevNum);


        /****************************************************************************
         * Function:    Qworks_QNFRecvState
         * Description: 存储板接收异常状态及异常状态时已经存储的数据个数
         * Input:       DevNum
         * Output:      Null
         * Return:      Please see ErrorList
         * Other:       Null
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_QNFRecvState", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int QNFRecvState(uint DevNum);


        /****************************************************************************
         * Function:    Qworks_QNFSendState
         * Description: 存储板发送异常状态
         * Input:       DevNum
         * Output:      Null
         * Return:      Please see ErrorList
         * Other:       Null
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_QNFSendState", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int QNFSendState(uint DevNum);


        /****************************************************************************
         * Function:    Qworks_QNFRecvPage
         * Description: 存储板当前写入总页数
         * Input:       DevNum
         * Output:      Null
         * Return:      Please see ErrorList
         * Other:       Null
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_QNFRecvPage", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int QNFRecvPage(uint DevNum);


        /****************************************************************************
         * Function:    Qworks_QNFLink
         * Description: 回放与采集相关，存储板内部连接与存储板与DRFM连接是否正常
         * Input:       DevNum
         * Output:      Null
         * Return:      Please see ErrorList
         * Other:       Null
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_QNFLink", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int QNFLink(uint DevNum);

        /****************************************************************************
         * Function:    Qworks_ClearIP
         * Description: Delete all IPs to Scan
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_ClearIP", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ClearIP();


        /****************************************************************************
         * Function:    Qworks_AddIP
         * Description:
         * Input:       zero terminated string of a IP address, such as "192.168.0.1"
         * Return:      if success,return the index(base 1) of added IP's, same IP will
         *              get same index in a session.
         *              return -1 for record is full.
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_AddIP", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int AddIP(string ip);


        /****************************************************************************
         * Function:    Qworks_GridSD8GRead
         * Description: 读SD8G至内存
         * Input:       pData 接收数据的buffer地址
         *              Addr_8kB 以8k Byte（8*1024）为单位的起始地址
         *              Len_8kB  以8k Byte（8*1024）为单位的数据长度，最大不超过(128*1024),对应1GB
         *              DevNum 设备号
         *              Prog 用于反馈进度的整数，返回值0~100
         * Return:      Please see ErrorList
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_GridSD8GRead", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GridSD8GRead(string pData, uint Addr_8kB, uint Len_8kB, uint DevNum, ref uint Prog);

        /****************************************************************************
         * Function:    Qworks_GridSD8GWrite
         * Description: 读SD8G至内存
         * Input:       pData 待写数据的buffer地址
         *              Addr_8kB 以8k Byte（8*1024）为单位的起始地址
         *              Len_8kB  以8k Byte（8*1024）为单位的数据长度，最大不超过(128*1024),对应1GB
         *              DevNum 设备号
         *              Prog 用于反馈进度的整数，返回值0~100
         * Return:      Please see ErrorList
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_GridSD8GWrite", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int GridSD8GWrite(string pData, uint Addr_8kB, uint Len_8kB, uint DevNum, ref uint Prog);


        /****************************************************************************
         * Function:    Qworks_QNFRead
         * Description: 读QNF数据至内存
         * Input:       DevNum 设备号
                        PageStart 起始地址页
                        PageCount 页数
                        buffer    接受数据的缓存地址
         * Output:      Null
         * Return:      Please see ErrorList
         * Other:       通过Qworks_Progress(DevNum)获得进度(0~100)
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_QNFRead", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int QNFRead(uint DevNum, uint PageStart, uint PageCount, string buffer);


        /****************************************************************************
         * Function:    Qworks_QNFWrite
         * Description: 写内存数据至QNF
         * Input:       DevNum 设备号
                        PageStart 起始地址页
                        PageCount 页数
                        buffer    待写数据的缓存地址
         * Output:      Null
         * Return:      Please see ErrorList
         * Other:       通过Qworks_Progress(DevNum)获得进度(0~100)
         ****************************************************************************/
        [DllImport("QWorks.dll", EntryPoint = "Qworks_QNFWrite", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Cdecl)]
        public static extern int QNFWrite(uint DevNum, uint PageStart, uint PageCount, string buffer);

    }
}
