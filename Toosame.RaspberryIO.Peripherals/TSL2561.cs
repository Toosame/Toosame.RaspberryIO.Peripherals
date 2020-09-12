using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using Toosame.RaspberryIO.Peripherals.Exceptions;

using Unosquare.WiringPi.Native;

namespace Toosame.RaspberryIO.Peripherals
{
    public class TSL2561
    {
        public const int I2C_ADDR_0x39 = 0x39;

        private const int COMMAND_CMD = 0x80;
        private const int TRANSACTION = 0x40;
        private const int TRANSACTION_SPECIAL = 0X60;

        private const int CONTROL = 0x00;
        private const int TIMING = 0x01;
        private const int INTERRUPT = 0X02;
        private const int THLLOW = 0x03;
        private const int THLHIGH = 0X04;
        private const int THHLOW = 0x05;
        private const int THHHIGH = 0X06;
        private const int ANALOG = 0X07;

        private const int ID = 0X12;
        private const int DATA0LOW = 0X14;
        private const int DATA0HIGH = 0X15;
        private const int DATA1LOW = 0X16;
        private const int DATA1HIGH = 0X17;
        private const int ADC_EN = 0X02;
        private const int CONTROL_POWERON = 0x01;
        private const int CONTROL_POWEROFF = 0x00;
        private const int INTR_TEST_MODE = 0X30;
        private const int INTR_INTER_MODE = 0X18;

        private const int INTEGRATIONTIME_Manual = 0x00;
        private const int INTEGRATIONTIME_2Z7MS = 0xFF;
        private const int INTEGRATIONTIME_5Z4MS = 0xFE;
        private const int INTEGRATIONTIME_51Z3MS = 0xED;
        private const int INTEGRATIONTIME_100MS = 0xDB;
        private const int INTEGRATIONTIME_200MS = 0xB6;
        private const int INTEGRATIONTIME_400MS = 0x6C;
        private const int INTEGRATIONTIME_688MS = 0x01;

        private const int GAIN_1X = 0x00;
        private const int GAIN_8X = 0x01;
        private const int GAIN_16X = 0x02;
        private const int GAIN_111X = 0x03;

        private const int K1C = 0x009A;
        private const int B1C = 0x2148;
        private const int M1C = 0x3d71;

        private const int K2C = 0x00c3;// # 0.38 * 2^RATIO_SCALE
        private const int B2C = 0x2a37;// # 0.1649 * 2^LUX_SCALE
        private const int M2C = 0x5b30;// # 0.3562 * 2^LUX_SCALE

        private const int K3C = 0x00e6;// # 0.45 * 2^RATIO_SCALE
        private const int B3C = 0x18ef;// # 0.0974 * 2^LUX_SCALE
        private const int M3C = 0x2db9;// # 0.1786 * 2^LUX_SCALE

        private const int K4C = 0x0114;// # 0.54 * 2^RATIO_SCALE
        private const int B4C = 0x0fdf;// # 0.062 * 2^LUX_SCALE
        private const int M4C = 0x199a;// # 0.10 * 2^LUX_SCALE

        private const int K5C = 0x0114;// # 0.54 * 2^RATIO_SCALE
        private const int B5C = 0x0000;// # 0.00000 * 2^LUX_SCALE
        private const int M5C = 0x0000;// # 0.00000 * 2^LUX_SCALE

        private const int CH0GAIN128X = 7;// # 128X gain scalar for Ch0
        private const int CH1GAIN128X = 115;// # 128X gain scalar for Ch1

        private const int NOM_INTEG_CYCLE = 148;

        private const int CH_SCALE = 16;

        private const int LUX_SCALE = 16;// # scale by 2^16
        private const int RATIO_SCALE = 9;//  # scale ratio by 2^9

        private int _address;
        private int _fd;

        public TSL2561(int address = I2C_ADDR_0x39)
        {
            _address = address;

            if (WiringPi.WiringPiSetupGpio() < 0)
                throw new DeviceLibInitException("set wiringPi lib failed");

            _fd = WiringPi.WiringPiI2CSetup(_address);

            Init();
        }

        public int ReadId()
        {
            return IIC_Read(COMMAND_CMD | TRANSACTION | ID);
        }

        public void SetInterruptThreshold(int min, int max)
        {
            int dataLLow = min % 256;
            int dataLHigh = min / 256;
            IIC_Write(COMMAND_CMD | THLLOW, dataLLow);
            IIC_Write(COMMAND_CMD | THLHIGH, dataLHigh);

            int dataHLow = max % 256;
            int dataHHigh = max / 256;
            IIC_Write(COMMAND_CMD | THHLOW, dataHLow);
            IIC_Write(COMMAND_CMD | THHHIGH, dataHHigh);
        }

        public int ReadChannel(int channel)
        {
            int dataLow = 0, dataHigh = 0;
            if (channel == 0)
            {
                dataLow = IIC_Read(COMMAND_CMD | TRANSACTION | DATA0LOW);
                dataHigh = IIC_Read(COMMAND_CMD | TRANSACTION | DATA0HIGH);
            }
            else if (channel == 1)
            {
                dataLow = IIC_Read(COMMAND_CMD | TRANSACTION | DATA1LOW);
                dataHigh = IIC_Read(COMMAND_CMD | TRANSACTION | DATA1HIGH);
            }

            return 256 * dataHigh + dataLow;
        }

        public int GetLux(int iGain, int tIntCycles)
        {
            int chScale0;
            if (tIntCycles == NOM_INTEG_CYCLE)
                chScale0 = (1 << (CH_SCALE));
            else// if (tIntCycles != NOM_INTEG_CYCLE)
                chScale0 = (NOM_INTEG_CYCLE << CH_SCALE) / tIntCycles;

            int chScale1 = 0;
            if (iGain == 0)
            {
                chScale1 = chScale0;
            }
            else if (iGain == 1)
            {
                chScale0 = chScale0 >> 3; // Scale/multiply value by 1/8
                chScale1 = chScale0;
            }
            else if (iGain == 2)
            {
                chScale0 = chScale0 >> 4; // Scale/multiply value by 1/16
                chScale1 = chScale0;
            }
            else if (iGain == 3)
            {
                chScale1 = Convert.ToInt32(chScale0 / CH1GAIN128X);
                chScale0 = Convert.ToInt32(chScale0 / CH0GAIN128X);
            }


            int Channel_0 = ReadChannel(0);
            int Channel_1 = ReadChannel(1);
            int channel0 = (Channel_0 * chScale0) >> CH_SCALE;
            int channel1 = (Channel_1 * chScale1) >> CH_SCALE;

            int ratio1 = 0;
            if (channel0 != 0)
                ratio1 = Convert.ToInt32((channel1 << (RATIO_SCALE + 1)) / channel0);
            int ratio = (ratio1 + 1) >> 1;

            int b = 0, m = 0;
            if ((ratio >= 0X00) || (ratio <= K1C))
            {
                b = B1C;
                m = M1C;
            }
            else if (ratio <= K2C)
            {
                b = B2C;
                m = M2C;
            }
            else if (ratio <= K3C)
            {
                b = B3C;
                m = M3C;
            }
            else if (ratio <= K4C)
            {
                b = B4C;
                m = M4C;
            }
            else if (ratio > K5C)
            {
                b = B5C;
                m = M5C;
            }

            int temp = ((channel0 * b) - (channel1 * m));
            temp += (1 << (LUX_SCALE - 1));

            return temp >> LUX_SCALE;
        }

        private void Init()
        {
            IIC_Write(COMMAND_CMD | CONTROL, CONTROL_POWERON);
            Thread.Sleep(1600);
            IIC_Write(COMMAND_CMD | TIMING, INTEGRATIONTIME_400MS);
            IIC_Write(COMMAND_CMD | CONTROL, ADC_EN | CONTROL_POWERON);
            IIC_Write(COMMAND_CMD | INTERRUPT, INTR_INTER_MODE);
            IIC_Write(COMMAND_CMD | ANALOG, GAIN_16X);
        }

        private void IIC_Write(int add_, int data_)
        {
            WiringPi.WiringPiI2CWriteReg8(_fd, add_, data_);
        }

        private int IIC_Read(int add_)
        {
            return WiringPi.WiringPiI2CReadReg8(_fd, add_);
        }
    }
}
