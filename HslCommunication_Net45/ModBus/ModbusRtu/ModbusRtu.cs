﻿using HslCommunication.BasicFramework;
using HslCommunication.Serial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HslCommunication.Core;

namespace HslCommunication.ModBus
{
    /// <summary>
    /// Modbus-Rtu通讯协议的类库，多项式码0xA001
    /// </summary>
    /// <remarks>
    /// 本客户端支持的标准的modbus-tcp协议，内置的消息号会进行自增，地址格式采用富文本表示形式
    /// <note type="important">
    /// 地址共可以携带3个信息，最完整的表示方式"s=2;x=3;100"，对应的modbus报文是 02 03 00 64 00 01 的前四个字节，站号，功能码，起始地址，下面举例
    /// <list type="definition">
    /// <item>
    ///     <term>读取线圈</term>
    ///     <description>ReadCoil("100")表示读取线圈100的值，ReadCoil("s=2;100")表示读取站号为2，线圈地址为100的值</description>
    /// </item>
    /// <item>
    ///     <term>读取离散输入</term>
    ///     <description>ReadDiscrete("100")表示读取离散输入100的值，ReadDiscrete("s=2;100")表示读取站号为2，离散地址为100的值</description>
    /// </item>
    /// <item>
    ///     <term>读取寄存器</term>
    ///     <description>ReadInt16("100")表示读取寄存器100的值，ReadInt16("s=2;100")表示读取站号为2，寄存器100的值</description>
    /// </item>
    /// <item>
    ///     <term>读取输入寄存器</term>
    ///     <description>ReadInt16("x=4;100")表示读取输入寄存器100的值，ReadInt16("s=2;x=4;100")表示读取站号为2，输入寄存器100的值</description>
    /// </item>
    /// </list>
    /// 对于写入来说也是一致的
    /// <list type="definition">
    /// <item>
    ///     <term>写入线圈</term>
    ///     <description>WriteCoil("100",true)表示读取线圈100的值，WriteCoil("s=2;100",true)表示读取站号为2，线圈地址为100的值</description>
    /// </item>
    /// <item>
    ///     <term>写入寄存器</term>
    ///     <description>Write("100",(short)123)表示写寄存器100的值123，Write("s=2;100",(short)123)表示写入站号为2，寄存器100的值123</description>
    /// </item>
    /// </list>
    /// </note>
    /// </remarks>
    /// <example>
    /// 基本的用法请参照下面的代码示例，初始化部分的代码省略
    /// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Modbus\Modbus.cs" region="Example2" title="Modbus示例" />
    /// </example>
    public class ModbusRtu : SerialBase
    {
        #region Constructor

        /// <summary>
        /// 实例化一个MOdbus-Tcp协议的客户端对象
        /// </summary>
        public ModbusRtu( )
        {
            byteTransform = new ReverseWordTransform( );
        }


        /// <summary>
        /// 指定服务器地址，端口号，客户端自己的站号来初始化
        /// </summary>
        /// <param name="station">客户端自身的站号</param>
        public ModbusRtu( byte station = 0x01 )
        {
            byteTransform = new ReverseWordTransform( );
            this.station = station;
        }

        #endregion

        #region Private Member

        private byte station = ModbusInfo.ReadCoil;                  // 本客户端的站号
        private bool isAddressStartWithZero = true;                  // 线圈值的地址值是否从零开始
        private ReverseWordTransform byteTransform;                  // 数组转换规则

        #endregion

        #region Public Member


        /// <summary>
        /// 获取或设置起始的地址是否从0开始，默认为True
        /// </summary>
        /// <remarks>
        /// <note type="warning">因为有些设备的起始地址是从1开始的，就要设置本属性为<c>True</c></note>
        /// </remarks>
        public bool AddressStartWithZero
        {
            get { return isAddressStartWithZero; }
            set { isAddressStartWithZero = value; }
        }

        /// <summary>
        /// 获取或者重新修改服务器的默认站号信息
        /// </summary>
        /// <remarks>
        /// 当你调用 ReadCoil("100") 时，对应的站号就是本属性的值，当你调用 ReadCoil("s=2;100") 时，就忽略本属性的值，读写寄存器的时候同理
        /// </remarks>
        public byte Station
        {
            get { return station; }
            set { station = value; }
        }


        /// <summary>
        /// 多字节的数据是否高低位反转，常用于Int32,UInt32,float,double,Int64,UInt64类型读写
        /// </summary>
        /// <remarks>
        /// 对于Int32,UInt32,float,double,Int64,UInt64类型来说，存在多地址的电脑情况，需要和服务器进行匹配
        /// </remarks>
        public bool IsMultiWordReverse
        {
            get { return byteTransform.IsMultiWordReverse; }
            set { byteTransform.IsMultiWordReverse = value; }
        }

        /// <summary>
        /// 字符串数据是否按照字来反转
        /// </summary>
        /// <remarks>
        /// 字符串按照2个字节的排列进行颠倒，根据实际情况进行设置
        /// </remarks>
        public bool IsStringReverse
        {
            get { return byteTransform.IsStringReverse; }
            set { byteTransform.IsStringReverse = value; }
        }

        /// <summary>
        /// 获取本对象的数据转换辅助对象
        /// </summary>
        /// <remarks>
        /// 本转换对象只能获取，无法设置。
        /// </remarks>
        public ReverseWordTransform ByteTransform
        {
            get { return byteTransform; }
        }

        #endregion

        #region Build Command


        /// <summary>
        /// 生成一个读取线圈的指令头
        /// </summary>
        /// <param name="address">地址</param>
        /// <param name="count">长度</param>
        /// <returns>携带有命令字节</returns>
        private OperateResult<byte[]> BuildReadCoilCommand( string address, ushort count )
        {
            OperateResult<ModbusAddress> analysis = ModbusInfo.AnalysisReadAddress( address, isAddressStartWithZero );
            if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );
            
            // 生成最终tcp指令
            byte[] buffer = ModbusInfo.PackCommandToRtu( analysis.Content.CreateReadCoils( station, count ) );
            return OperateResult.CreateSuccessResult( buffer );
        }

        /// <summary>
        /// 生成一个读取离散信息的指令头
        /// </summary>
        /// <param name="address">地址</param>
        /// <param name="length">长度</param>
        /// <returns>携带有命令字节</returns>
        private OperateResult<byte[]> BuildReadDiscreteCommand( string address, ushort length )
        {
            OperateResult<ModbusAddress> analysis = ModbusInfo.AnalysisReadAddress( address, isAddressStartWithZero );
            if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );
            
            // 生成最终tcp指令
            byte[] buffer = ModbusInfo.PackCommandToRtu( analysis.Content.CreateReadDiscrete( station, length ) );
            return OperateResult.CreateSuccessResult( buffer );
        }




        /// <summary>
        /// 生成一个读取寄存器的指令头
        /// </summary>
        /// <param name="address">地址</param>
        /// <param name="length">长度</param>
        /// <returns>携带有命令字节</returns>
        private OperateResult<byte[]> BuildReadRegisterCommand( string address, ushort length )
        {
            OperateResult<ModbusAddress> analysis = ModbusInfo.AnalysisReadAddress( address, isAddressStartWithZero );
            if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );
            
            // 生成最终tcp指令
            byte[] buffer = ModbusInfo.PackCommandToRtu( analysis.Content.CreateReadRegister( station, length ) );
            return OperateResult.CreateSuccessResult( buffer );
        }



        /// <summary>
        /// 生成一个读取寄存器的指令头
        /// </summary>
        /// <param name="address">地址</param>
        /// <param name="length">长度</param>
        /// <returns>携带有命令字节</returns>
        private OperateResult<byte[]> BuildReadRegisterCommand( ModbusAddress address, ushort length )
        {
            // 生成最终tcp指令
            byte[] buffer = ModbusInfo.PackCommandToRtu( address.CreateReadRegister( station, length ) );
            return OperateResult.CreateSuccessResult( buffer );
        }



        private OperateResult<byte[]> BuildWriteOneCoilCommand( string address, bool value )
        {
            OperateResult<ModbusAddress> analysis = ModbusInfo.AnalysisReadAddress( address, isAddressStartWithZero );
            if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );
            
            // 生成最终tcp指令
            byte[] buffer = ModbusInfo.PackCommandToRtu( analysis.Content.CreateWriteOneCoil( station, value ) );
            return OperateResult.CreateSuccessResult( buffer );
        }





        private OperateResult<byte[]> BuildWriteOneRegisterCommand( string address, byte[] data )
        {
            OperateResult<ModbusAddress> analysis = ModbusInfo.AnalysisReadAddress( address, isAddressStartWithZero );
            if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );
            
            // 生成最终tcp指令
            byte[] buffer = ModbusInfo.PackCommandToRtu( analysis.Content.CreateWriteOneRegister( station, data ) );
            return OperateResult.CreateSuccessResult( buffer );
        }



        private OperateResult<byte[]> BuildWriteCoilCommand( string address, bool[] values )
        {
            OperateResult<ModbusAddress> analysis = ModbusInfo.AnalysisReadAddress( address, isAddressStartWithZero );
            if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );
            
            // 生成最终tcp指令
            byte[] buffer = ModbusInfo.PackCommandToRtu( analysis.Content.CreateWriteCoil( station, values ) );
            return OperateResult.CreateSuccessResult( buffer );
        }


        private OperateResult<byte[]> BuildWriteRegisterCommand( string address, byte[] values )
        {
            OperateResult<ModbusAddress> analysis = ModbusInfo.AnalysisReadAddress( address, isAddressStartWithZero );
            if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );
            
            // 生成最终tcp指令
            byte[] buffer = ModbusInfo.PackCommandToRtu( analysis.Content.CreateWriteRegister( station, values ) );
            return OperateResult.CreateSuccessResult( buffer );
        }



        #endregion
        
        #region Core Interative
        

        /// <summary>
        /// 检查当前的Modbus-Tcp响应是否是正确的
        /// </summary>
        /// <param name="send">发送的数据信息</param>
        /// <returns>带是否成功的结果数据</returns>
        private OperateResult<byte[]> CheckModbusTcpResponse( byte[] send )
        {
            OperateResult<byte[]> result = ReadBase( send );
            if (!result.IsSuccess) return result;

            if(result.Content.Length < 5)
            {
                return new OperateResult<byte[]>( )
                {
                    IsSuccess = false,
                    Message = "接收数据长度不能小于5",
                };
            }

            if(!SoftCRC16.CheckCRC16(result.Content))
            {
                return new OperateResult<byte[]>( )
                {
                    IsSuccess = false,
                    Message = "CRC校验失败",
                };
            }

            if ((send[1] + 0x80) == result.Content[1])
            {
                // 发生了错误
                return new OperateResult<byte[]>( )
                {
                    IsSuccess = false,
                    Message = ModbusInfo.GetDescriptionByErrorCode( result.Content[2] ),
                    ErrorCode = result.Content[2],
                };
            }
            else
            {
                // 移除CRC校验
                byte[] buffer = new byte[result.Content.Length - 2];
                Array.Copy( result.Content, 0, buffer, 0, buffer.Length );
                return OperateResult.CreateSuccessResult( buffer );
            }
        }

        #endregion

        #region Protect Override

        /// <summary>
        /// 检查当前接收的字节数据是否正确的
        /// </summary>
        /// <param name="rBytes"></param>
        /// <returns></returns>
        protected override bool CheckReceiveBytes( byte[] rBytes )
        {
            return SoftCRC16.CheckCRC16( rBytes );
        }

        #endregion

        #region Customer Support

        /// <summary>
        /// 读取自定义的数据类型，只针对寄存器而言，需要规定了写入和解析规则
        /// </summary>
        /// <typeparam name="T">类型名称</typeparam>
        /// <param name="address">起始地址</param>
        /// <returns>带是否成功的特定类型的对象</returns>
        public OperateResult<T> ReadCustomer<T>( string address ) where T : IDataTransfer, new()
        {
            OperateResult<T> result = new OperateResult<T>( );
            T Content = new T( );
            OperateResult<byte[]> read = Read( address, Content.ReadCount );
            if (read.IsSuccess)
            {
                Content.ParseSource( read.Content );
                result.Content = Content;
                result.IsSuccess = true;
            }
            else
            {
                result.ErrorCode = read.ErrorCode;
                result.Message = read.Message;
            }
            return result;
        }

        /// <summary>
        /// 写入自定义的数据类型到寄存器去，只要规定了生成字节的方法即可
        /// </summary>
        /// <typeparam name="T">自定义类型</typeparam>
        /// <param name="address">起始地址</param>
        /// <param name="data">实例对象</param>
        /// <returns>是否成功</returns>
        public OperateResult WriteCustomer<T>( string address, T data ) where T : IDataTransfer, new()
        {
            return Write( address, data.ToSource( ) );
        }


        #endregion

        #region Read Support

        /// <summary>
        /// 读取服务器的数据，需要指定不同的功能码
        /// </summary>
        /// <param name="code">指令</param>
        /// <param name="address">地址</param>
        /// <param name="length">长度</param>
        /// <returns></returns>
        private OperateResult<byte[]> ReadModBusBase( byte code, string address, ushort length )
        {
            OperateResult<byte[]> command = null;
            switch (code)
            {
                case ModbusInfo.ReadCoil:
                    {
                        command = BuildReadCoilCommand( address, length );
                        break;
                    }
                case ModbusInfo.ReadDiscrete:
                    {
                        command = BuildReadDiscreteCommand( address, length );
                        break;
                    }
                case ModbusInfo.ReadRegister:
                    {
                        command = BuildReadRegisterCommand( address, length );
                        break;
                    }
                default: command = new OperateResult<byte[]>( ) { Message = StringResources.ModbusTcpFunctionCodeNotSupport }; break;
            }
            if (!command.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( command );

            OperateResult<byte[]> resultBytes = CheckModbusTcpResponse( command.Content );
            if (resultBytes.IsSuccess)
            {
                // 二次数据处理
                if (resultBytes.Content?.Length >= 3)
                {
                    byte[] buffer = new byte[resultBytes.Content.Length - 3];
                    Array.Copy( resultBytes.Content, 3, buffer, 0, buffer.Length );
                    resultBytes.Content = buffer;
                }
            }
            return resultBytes;
        }

        /// <summary>
        /// 读取服务器的数据，需要指定不同的功能码
        /// </summary>
        /// <param name="address">地址</param>
        /// <param name="length">长度</param>
        /// <returns></returns>
        private OperateResult<byte[]> ReadModBusBase( ModbusAddress address, ushort length )
        {
            OperateResult<byte[]> command = BuildReadRegisterCommand( address, length );
            if (!command.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( command );

            OperateResult<byte[]> resultBytes = CheckModbusTcpResponse( command.Content );
            if (resultBytes.IsSuccess)
            {
                // 二次数据处理
                if (resultBytes.Content?.Length >= 3)
                {
                    byte[] buffer = new byte[resultBytes.Content.Length - 3];
                    Array.Copy( resultBytes.Content, 3, buffer, 0, buffer.Length );
                    resultBytes.Content = buffer;
                }
            }
            return resultBytes;
        }


        /// <summary>
        /// 读取线圈，需要指定起始地址
        /// </summary>
        /// <param name="address">起始地址，格式为"1234"</param>
        /// <returns>带有成功标志的bool对象</returns>
        public OperateResult<bool> ReadCoil( string address )
        {
            var read = ReadModBusBase( ModbusInfo.ReadCoil, address, 1 );
            if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool>( read );

            return ByteTransformHelper.GetBoolResultFromBytes( read, byteTransform );
        }






        /// <summary>
        /// 批量的读取线圈，需要指定起始地址，读取长度
        /// </summary>
        /// <param name="address">起始地址，格式为"1234"</param>
        /// <param name="length">读取长度</param>
        /// <returns>带有成功标志的bool数组对象</returns>
        public OperateResult<bool[]> ReadCoil( string address, ushort length )
        {
            var read = ReadModBusBase( ModbusInfo.ReadCoil, address, length );
            if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

            return OperateResult.CreateSuccessResult( SoftBasic.ByteToBoolArray( read.Content, length ) );
        }




        /// <summary>
        /// 读取输入线圈，需要指定起始地址
        /// </summary>
        /// <param name="address">起始地址，格式为"1234"</param>
        /// <returns>带有成功标志的bool对象</returns>
        public OperateResult<bool> ReadDiscrete( string address )
        {
            var read = ReadModBusBase( ModbusInfo.ReadDiscrete, address, 1 );
            if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool>( read );

            return ByteTransformHelper.GetBoolResultFromBytes( read, byteTransform );
        }






        /// <summary>
        /// 批量的读取输入点，需要指定起始地址，读取长度
        /// </summary>
        /// <param name="address">起始地址，格式为"1234"</param>
        /// <param name="length">读取长度</param>
        /// <returns>带有成功标志的bool数组对象</returns>
        public OperateResult<bool[]> ReadDiscrete( string address, ushort length )
        {
            var read = ReadModBusBase( ModbusInfo.ReadDiscrete, address, length );
            if (!read.IsSuccess) return OperateResult.CreateFailedResult<bool[]>( read );

            return OperateResult.CreateSuccessResult( SoftBasic.ByteToBoolArray( read.Content, length ) );
        }



        /// <summary>
        /// 从Modbus服务器批量读取寄存器的信息，需要指定起始地址，读取长度
        /// </summary>
        /// <param name="address">起始地址，格式为"1234"，或者是带功能码格式x=3;1234</param>
        /// <param name="length">读取的数量</param>
        /// <returns>带有成功标志的字节信息</returns>
        /// <example>
        /// 此处演示批量读取的示例
        /// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Modbus\Modbus.cs" region="ReadExample2" title="Read示例" />
        /// </example>
        public OperateResult<byte[]> Read( string address, ushort length )
        {
            OperateResult<ModbusAddress> analysis = ModbusInfo.AnalysisReadAddress( address, isAddressStartWithZero );
            if (!analysis.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( analysis );

            List<byte> lists = new List<byte>( );
            ushort alreadyFinished = 0;
            while (alreadyFinished < length)
            {
                ushort lengthTmp = (ushort)Math.Min( (length - alreadyFinished), 120 );
                OperateResult<byte[]> read = ReadModBusBase( analysis.Content.AddressAdd( alreadyFinished ), lengthTmp );
                if (!read.IsSuccess) return OperateResult.CreateFailedResult<byte[]>( read );

                lists.AddRange( read.Content );
                alreadyFinished += lengthTmp;
            }
            return OperateResult.CreateSuccessResult( lists.ToArray( ) );
        }


        /// <summary>
        /// 读取指定地址的short数据
        /// </summary>
        /// <param name="address">起始地址，格式为"1234"</param>
        /// <returns>带有成功标志的short数据</returns>
        public OperateResult<short> ReadInt16( string address )
        {
            return ByteTransformHelper.GetInt16ResultFromBytes( Read( address, 1 ), byteTransform );
        }

        /// <summary>
        /// 读取指定地址的short数组
        /// </summary>
        /// <param name="address">起始地址，格式为"1234"</param>
        /// <param name="length">数组长度</param>
        /// <returns>带有成功标志的short数据</returns>
        public OperateResult<short[]> ReadInt16( string address, ushort length )
        {
            OperateResult<byte[]> read = Read( address, length );
            if (!read.IsSuccess) return OperateResult.CreateFailedResult<short[]>( read );
            return OperateResult.CreateSuccessResult( byteTransform.TransInt16( read.Content, 0, length ) );
        }

        /// <summary>
        /// 读取指定地址的ushort数据
        /// </summary>
        /// <param name="address">起始地址，格式为"1234"</param>
        /// <returns>带有成功标志的ushort数据</returns>
        public OperateResult<ushort> ReadUInt16( string address )
        {
            return ByteTransformHelper.GetUInt16ResultFromBytes( Read( address, 1 ), byteTransform );
        }

        /// <summary>
        /// 读取指定地址的ushort数组
        /// </summary>
        /// <param name="address">起始地址，格式为"1234"</param>
        /// <param name="length">数组长度</param>
        /// <returns>带有成功标志的ushort数据</returns>
        public OperateResult<ushort[]> ReadUInt16( string address, ushort length )
        {
            OperateResult<byte[]> read = Read( address, length );
            if (!read.IsSuccess) return OperateResult.CreateFailedResult<ushort[]>( read );
            return OperateResult.CreateSuccessResult( byteTransform.TransUInt16( read.Content, 0, length ) );
        }

        /// <summary>
        /// 读取指定地址的int数据
        /// </summary>
        /// <param name="address">起始地址，格式为"1234"</param>
        /// <returns>带有成功标志的int数据</returns>
        public OperateResult<int> ReadInt32( string address )
        {
            return ByteTransformHelper.GetInt32ResultFromBytes( Read( address, 2 ), byteTransform );
        }

        /// <summary>
        /// 读取指定地址的int数组
        /// </summary>
        /// <param name="address">起始地址，格式为"1234"</param>
        /// <param name="length">数组的长度</param>
        /// <returns>带有成功标志的int数据</returns>
        public OperateResult<int[]> ReadInt32( string address, ushort length )
        {
            OperateResult<byte[]> read = Read( address, (ushort)(length * 2) );
            if (!read.IsSuccess) return OperateResult.CreateFailedResult<int[]>( read );
            return OperateResult.CreateSuccessResult( byteTransform.TransInt32( read.Content, 0, length ) );
        }

        /// <summary>
        /// 读取指定地址的uint数据
        /// </summary>
        /// <param name="address">起始地址，格式为"1234"</param>
        /// <returns>带有成功标志的uint数据</returns>
        public OperateResult<uint> ReadUInt32( string address )
        {
            return ByteTransformHelper.GetUInt32ResultFromBytes( Read( address, 2 ), byteTransform );
        }

        /// <summary>
        /// 读取指定地址的uint数组
        /// </summary>
        /// <param name="address">起始地址，格式为"1234"</param>
        /// <param name="length">数组长度</param>
        /// <returns>带有成功标志的uint数据</returns>
        public OperateResult<uint[]> ReadUInt32( string address, ushort length )
        {
            OperateResult<byte[]> read = Read( address, (ushort)(length * 2) );
            if (!read.IsSuccess) return OperateResult.CreateFailedResult<uint[]>( read );
            return OperateResult.CreateSuccessResult( byteTransform.TransUInt32( read.Content, 0, length ) );
        }

        /// <summary>
        /// 读取指定地址的float数据
        /// </summary>
        /// <param name="address">起始地址，格式为"1234"</param>
        /// <returns>带有成功标志的float数据</returns>
        public OperateResult<float> ReadFloat( string address )
        {
            return ByteTransformHelper.GetSingleResultFromBytes( Read( address, 2 ), byteTransform );
        }

        /// <summary>
        /// 读取指定地址的float数组
        /// </summary>
        /// <param name="address">起始地址，格式为"1234"</param>
        /// <param name="length">数组长度</param>
        /// <returns>带有成功标志的float数据</returns>
        public OperateResult<float[]> ReadFloat( string address, ushort length )
        {
            OperateResult<byte[]> read = Read( address, (ushort)(length * 2) );
            if (!read.IsSuccess) return OperateResult.CreateFailedResult<float[]>( read );
            return OperateResult.CreateSuccessResult( byteTransform.TransSingle( read.Content, 0, length ) );
        }

        /// <summary>
        /// 读取指定地址的long数据
        /// </summary>
        /// <param name="address">起始地址，格式为"1234"</param>
        /// <returns>带有成功标志的long数据</returns>
        public OperateResult<long> ReadInt64( string address )
        {
            return ByteTransformHelper.GetInt64ResultFromBytes( Read( address, 4 ), byteTransform );
        }

        /// <summary>
        /// 读取指定地址的long数组
        /// </summary>
        /// <param name="address">起始地址，格式为"1234"</param>
        /// <param name="length">数组长度</param>
        /// <returns>带有成功标志的long数据</returns>
        public OperateResult<long[]> ReadInt64( string address, ushort length )
        {
            OperateResult<byte[]> read = Read( address, (ushort)(length * 4) );
            if (!read.IsSuccess) return OperateResult.CreateFailedResult<long[]>( read );
            return OperateResult.CreateSuccessResult( byteTransform.TransInt64( read.Content, 0, length ) );
        }

        /// <summary>
        /// 读取指定地址的ulong数据
        /// </summary>
        /// <param name="address">起始地址，格式为"1234"</param>
        /// <returns>带有成功标志的ulong数据</returns>
        public OperateResult<ulong> ReadUInt64( string address )
        {
            return ByteTransformHelper.GetUInt64ResultFromBytes( Read( address, 4 ), byteTransform );
        }

        /// <summary>
        /// 读取指定地址的ulong数组
        /// </summary>
        /// <param name="address">起始地址，格式为"1234"</param>
        /// <param name="length">数组长度</param>
        /// <returns>带有成功标志的ulong数据</returns>
        public OperateResult<ulong[]> ReadUInt64( string address, ushort length )
        {
            OperateResult<byte[]> read = Read( address, (ushort)(length * 4) );
            if (!read.IsSuccess) return OperateResult.CreateFailedResult<ulong[]>( read );
            return OperateResult.CreateSuccessResult( byteTransform.TransUInt64( read.Content, 0, length ) );
        }

        /// <summary>
        /// 读取指定地址的double数据
        /// </summary>
        /// <param name="address">起始地址，格式为"1234"</param>
        /// <returns>带有成功标志的double数据</returns>
        public OperateResult<double> ReadDouble( string address )
        {
            return ByteTransformHelper.GetDoubleResultFromBytes( Read( address, 4 ), byteTransform );
        }

        /// <summary>
        /// 读取指定地址的double数组
        /// </summary>
        /// <param name="address">起始地址，格式为"1234"</param>
        /// <param name="length">数组长度</param>
        /// <returns>带有成功标志的double数据</returns>
        public OperateResult<double[]> ReadDouble( string address, ushort length )
        {
            OperateResult<byte[]> read = Read( address, (ushort)(length * 4) );
            if (!read.IsSuccess) return OperateResult.CreateFailedResult<double[]>( read );
            return OperateResult.CreateSuccessResult( byteTransform.TransDouble( read.Content, 0, length ) );
        }

        /// <summary>
        /// 读取地址地址的String数据，字符串编码为ASCII
        /// </summary>
        /// <param name="address">起始地址，格式为"1234"</param>
        /// <param name="length">字符串长度</param>
        /// <returns>带有成功标志的string数据</returns>
        public OperateResult<string> ReadString( string address, ushort length )
        {
            return ByteTransformHelper.GetStringResultFromBytes( Read( address, length ), byteTransform );
        }



        #endregion

        #region Write One Register



        /// <summary>
        /// 写一个寄存器数据
        /// </summary>
        /// <param name="address">起始地址</param>
        /// <param name="high">高位</param>
        /// <param name="low">地位</param>
        /// <returns>返回写入结果</returns>
        public OperateResult WriteOneRegister( string address, byte high, byte low )
        {
            OperateResult<byte[]> command = BuildWriteOneRegisterCommand( address, new byte[] { high, low } );
            if (!command.IsSuccess)
            {
                return command;
            }

            return CheckModbusTcpResponse( command.Content );
        }

        /// <summary>
        /// 写一个寄存器数据
        /// </summary>
        /// <param name="address">起始地址</param>
        /// <param name="value">写入值</param>
        /// <returns>返回写入结果</returns>
        public OperateResult WriteOneRegister( string address, short value )
        {
            byte[] buffer = BitConverter.GetBytes( value );
            return WriteOneRegister( address, buffer[1], buffer[0] );
        }

        /// <summary>
        /// 写一个寄存器数据
        /// </summary>
        /// <param name="address">起始地址</param>
        /// <param name="value">写入值</param>
        /// <returns>返回写入结果</returns>
        public OperateResult WriteOneRegister( string address, ushort value )
        {
            byte[] buffer = BitConverter.GetBytes( value );
            return WriteOneRegister( address, buffer[1], buffer[0] );
        }



        #endregion

        #region Write Base


        /// <summary>
        /// 将数据写入到Modbus的寄存器上去，需要指定起始地址和数据内容
        /// </summary>
        /// <param name="address">起始地址，格式为"1234"</param>
        /// <param name="value">写入的数据，长度根据data的长度来指示</param>
        /// <returns>返回写入结果</returns>
        /// <remarks>
        /// 富地址格式，支持携带站号信息，功能码信息，具体参照类的示例代码
        /// </remarks>
        /// <example>
        /// 此处演示批量写入的示例
        /// <code lang="cs" source="HslCommunication_Net45.Test\Documentation\Samples\Modbus\Modbus.cs" region="WriteExample2" title="Write示例" />
        /// </example>
        public OperateResult Write( string address, byte[] value )
        {
            OperateResult<byte[]> command = BuildWriteRegisterCommand( address, value );
            if (!command.IsSuccess)
            {
                return command;
            }

            return CheckModbusTcpResponse( command.Content );
        }


        #endregion

        #region Write Coil

        /// <summary>
        /// 写一个线圈信息，指定是否通断
        /// </summary>
        /// <param name="address">起始地址</param>
        /// <param name="value">写入值</param>
        /// <returns>返回写入结果</returns>
        public OperateResult WriteCoil( string address, bool value )
        {
            OperateResult<byte[]> command = BuildWriteOneCoilCommand( address, value );
            if (!command.IsSuccess)
            {
                return command;
            }

            return CheckModbusTcpResponse( command.Content );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address">起始地址</param>
        /// <param name="values">写入值</param>
        /// <returns>返回写入结果</returns>
        public OperateResult WriteCoil( string address, bool[] values )
        {
            OperateResult<byte[]> command = BuildWriteCoilCommand( address, values );
            if (!command.IsSuccess)
            {
                return command;
            }

            return CheckModbusTcpResponse( command.Content );
        }


        #endregion

        #region Write String

        /// <summary>
        /// 向寄存器中写入字符串，编码格式为ASCII
        /// </summary>
        /// <param name="address">要写入的数据地址</param>
        /// <param name="value">要写入的实际数据</param>
        /// <returns>返回写入结果</returns>
        public OperateResult Write( string address, string value )
        {
            byte[] temp = byteTransform.TransByte( value, Encoding.ASCII );
            temp = SoftBasic.ArrayExpandToLengthEven( temp );
            return Write( address, temp );
        }

        /// <summary>
        /// 向寄存器中写入指定长度的字符串,超出截断，不够补0，编码格式为ASCII
        /// </summary>
        /// <param name="address">要写入的数据地址</param>
        /// <param name="value">要写入的实际数据</param>
        /// <param name="length">指定的字符串长度，必须大于0</param>
        /// <returns>返回写入结果</returns>
        public OperateResult Write( string address, string value, int length )
        {
            byte[] temp = byteTransform.TransByte( value, Encoding.ASCII );
            temp = SoftBasic.ArrayExpandToLength( temp, length );
            return Write( address, temp );
        }

        /// <summary>
        /// 向寄存器中写入字符串，编码格式为Unicode
        /// </summary>
        /// <param name="address">要写入的数据地址</param>
        /// <param name="value">要写入的实际数据</param>
        /// <returns>返回写入结果</returns>
        public OperateResult WriteUnicodeString( string address, string value )
        {
            byte[] temp = byteTransform.TransByte( value, Encoding.Unicode );
            return Write( address, temp );
        }

        /// <summary>
        /// 向寄存器中写入指定长度的字符串,超出截断，不够补0，编码格式为Unicode
        /// </summary>
        /// <param name="address">要写入的数据地址</param>
        /// <param name="value">要写入的实际数据</param>
        /// <param name="length">指定的字符串长度，必须大于0</param>
        /// <returns>返回写入结果</returns>
        public OperateResult WriteUnicodeString( string address, string value, int length )
        {
            byte[] temp = byteTransform.TransByte( value, Encoding.Unicode );
            temp = SoftBasic.ArrayExpandToLength( temp, length * 2 );
            return Write( address, temp );
        }
        
        #endregion

        #region Write bool[]

        /// <summary>
        /// 向寄存器中写入bool数组，返回值说明，比如你写入M100,那么data[0]对应M100.0
        /// </summary>
        /// <param name="address">要写入的数据地址</param>
        /// <param name="values">要写入的实际数据，长度为8的倍数</param>
        /// <returns>返回写入结果</returns>
        public OperateResult Write( string address, bool[] values )
        {
            return Write( address, BasicFramework.SoftBasic.BoolArrayToByte( values ) );
        }


        #endregion

        #region Write Short

        /// <summary>
        /// 向寄存器中写入short数组，返回值说明
        /// </summary>
        /// <param name="address">要写入的数据地址</param>
        /// <param name="values">要写入的实际数据</param>
        /// <returns>返回写入结果</returns>
        public OperateResult Write( string address, short[] values )
        {
            return Write( address, byteTransform.TransByte( values ) );
        }

        /// <summary>
        /// 向寄存器中写入short数据，返回值说明
        /// </summary>
        /// <param name="address">要写入的数据地址</param>
        /// <param name="value">要写入的实际数据</param>
        /// <returns>返回写入结果</returns>
        public OperateResult Write( string address, short value )
        {
            return Write( address, new short[] { value } );
        }

        #endregion

        #region Write UShort


        /// <summary>
        /// 向寄存器中写入ushort数组，返回值说明
        /// </summary>
        /// <param name="address">要写入的数据地址</param>
        /// <param name="values">要写入的实际数据</param>
        /// <returns>返回写入结果</returns>
        public OperateResult Write( string address, ushort[] values )
        {
            return Write( address, byteTransform.TransByte( values ) );
        }


        /// <summary>
        /// 向寄存器中写入ushort数据，返回值说明
        /// </summary>
        /// <param name="address">要写入的数据地址</param>
        /// <param name="value">要写入的实际数据</param>
        /// <returns>返回写入结果</returns>
        public OperateResult Write( string address, ushort value )
        {
            return Write( address, new ushort[] { value } );
        }


        #endregion

        #region Write Int

        /// <summary>
        /// 向寄存器中写入int数组，返回值说明
        /// </summary>
        /// <param name="address">要写入的数据地址</param>
        /// <param name="values">要写入的实际数据</param>
        /// <returns>返回写入结果</returns>
        public OperateResult Write( string address, int[] values )
        {
            return Write( address, byteTransform.TransByte( values ) );
        }

        /// <summary>
        /// 向寄存器中写入int数据，返回值说明
        /// </summary>
        /// <param name="address">要写入的数据地址</param>
        /// <param name="value">要写入的实际数据</param>
        /// <returns>返回写入结果</returns>
        public OperateResult Write( string address, int value )
        {
            return Write( address, new int[] { value } );
        }

        #endregion

        #region Write UInt

        /// <summary>
        /// 向寄存器中写入uint数组，返回值说明
        /// </summary>
        /// <param name="address">要写入的数据地址</param>
        /// <param name="values">要写入的实际数据</param>
        /// <returns>返回写入结果</returns>
        public OperateResult Write( string address, uint[] values )
        {
            return Write( address, byteTransform.TransByte( values ) );
        }

        /// <summary>
        /// 向寄存器中写入uint数据，返回值说明
        /// </summary>
        /// <param name="address">要写入的数据地址</param>
        /// <param name="value">要写入的实际数据</param>
        /// <returns>返回写入结果</returns>
        public OperateResult Write( string address, uint value )
        {
            return Write( address, new uint[] { value } );
        }

        #endregion

        #region Write Float

        /// <summary>
        /// 向寄存器中写入float数组，返回值说明
        /// </summary>
        /// <param name="address">要写入的数据地址</param>
        /// <param name="values">要写入的实际数据</param>
        /// <returns>返回写入结果</returns>
        public OperateResult Write( string address, float[] values )
        {
            return Write( address, byteTransform.TransByte( values ) );
        }

        /// <summary>
        /// 向寄存器中写入float数据，返回值说明
        /// </summary>
        /// <param name="address">要写入的数据地址</param>
        /// <param name="value">要写入的实际数据</param>
        /// <returns>返回写入结果</returns>
        public OperateResult Write( string address, float value )
        {
            return Write( address, new float[] { value } );
        }


        #endregion

        #region Write Long

        /// <summary>
        /// 向寄存器中写入long数组，返回值说明
        /// </summary>
        /// <param name="address">要写入的数据地址</param>
        /// <param name="values">要写入的实际数据</param>
        /// <returns>返回写入结果</returns>
        public OperateResult Write( string address, long[] values )
        {
            return Write( address, byteTransform.TransByte( values ) );
        }

        /// <summary>
        /// 向寄存器中写入long数据，返回值说明
        /// </summary>
        /// <param name="address">要写入的数据地址</param>
        /// <param name="value">要写入的实际数据</param>
        /// <returns>返回写入结果</returns>
        public OperateResult Write( string address, long value )
        {
            return Write( address, new long[] { value } );
        }

        #endregion

        #region Write ULong

        /// <summary>
        /// 向寄存器中写入ulong数组，返回值说明
        /// </summary>
        /// <param name="address">要写入的数据地址</param>
        /// <param name="values">要写入的实际数据</param>
        /// <returns>返回写入结果</returns>
        public OperateResult Write( string address, ulong[] values )
        {
            return Write( address, byteTransform.TransByte( values ) );
        }

        /// <summary>
        /// 向寄存器中写入ulong数据，返回值说明
        /// </summary>
        /// <param name="address">要写入的数据地址</param>
        /// <param name="value">要写入的实际数据</param>
        /// <returns>返回写入结果</returns>
        public OperateResult Write( string address, ulong value )
        {
            return Write( address, new ulong[] { value } );
        }

        #endregion

        #region Write Double

        /// <summary>
        /// 向寄存器中写入double数组，返回值说明
        /// </summary>
        /// <param name="address">要写入的数据地址</param>
        /// <param name="values">要写入的实际数据</param>
        /// <returns>返回写入结果</returns>
        public OperateResult Write( string address, double[] values )
        {
            return Write( address, byteTransform.TransByte( values ) );
        }

        /// <summary>
        /// 向寄存器中写入double数据，返回值说明
        /// </summary>
        /// <param name="address">要写入的数据地址</param>
        /// <param name="value">要写入的实际数据</param>
        /// <returns>返回写入结果</returns>
        public OperateResult Write( string address, double value )
        {
            return Write( address, new double[] { value } );
        }

        #endregion

        #region Object Override

        /// <summary>
        /// 获取当前对象的字符串标识形式
        /// </summary>
        /// <returns>字符串信息</returns>
        public override string ToString( )
        {
            return "ModbusRtuNet";
        }

        #endregion



    }
}
