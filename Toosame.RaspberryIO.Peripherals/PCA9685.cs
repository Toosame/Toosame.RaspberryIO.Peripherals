using System;
using System.Threading;

using Toosame.RaspberryIO.Peripherals.Exceptions;

using Unosquare.WiringPi.Native;

namespace Toosame.RaspberryIO.Peripherals
{
    public class PCA9685
    {
        public const int PWM_I2C_Addr = 0x40;
        public const int PWM_I2C_Hz = 50;

        private const byte __SUBADR1 = 0x02;
        private const byte __SUBADR2 = 0x03;
        private const byte __SUBADR3 = 0x04;
        private const byte __MODE1 = 0x00;
        private const byte __MODE2 = 0x01;
        private const byte __PRESCALE = 0xFE;
        private const byte __LED0_ON_L = 0x06;
        private const byte __LED0_ON_H = 0x07;
        private const byte __LED0_OFF_L = 0x08;
        private const byte __LED0_OFF_H = 0x09;
        private const byte __ALLLED_ON_L = 0xFA;
        private const byte __ALLLED_ON_H = 0xFB;
        private const byte __ALLLED_OFF_L = 0xFC;
        private const byte __ALLLED_OFF_H = 0xFD;

        private int _address;
        private readonly float _freq;

        private readonly int _fd;

        public PCA9685(int address = PWM_I2C_Addr, float freq = PWM_I2C_Hz)
        {
            _address = address;
            _freq = freq;

            if (WiringPi.WiringPiSetupGpio() < 0)
                throw new DeviceLibInitException("set wiringPi lib failed");

            _fd = WiringPi.WiringPiI2CSetup(_address);

            Init();
        }

        private void Init()
        {
            float prescaleval;
            int oldmode;

            IIC_Write(__MODE1, 0x00);
            prescaleval = 25000000;
            prescaleval /= 4096.0f;
            prescaleval /= _freq;
            prescaleval -= 1.0f;

            prescaleval += 0.5f;

            oldmode = IIC_Read(__MODE1);
            IIC_Write(__MODE1, (oldmode & 0x7F) | 0x10);
            IIC_Write(__PRESCALE, Convert.ToInt32(prescaleval));
            IIC_Write(__MODE1, oldmode);
            Thread.Sleep(1600);
            IIC_Write(__MODE1, oldmode | 0x80);
            IIC_Write(__MODE2, 0x04);
        }

        /// <summary>
        /// Rotation Angle
        /// </summary>
        /// <param name="channel">range: 0 - 1</param>
        /// <param name="angle">
        /// <list>
        /// <item>
        /// <term>channel 0</term>
        /// <description>0° ~ 180°</description>
        /// </item>
        /// <item>
        /// <term>channel 1</term>
        /// <description>70° ~ 180°</description>
        /// </item>
        /// </list>
        /// </param>
        public void SetRotationAngle(int channel, int angle)
        {
            if (angle >= 0 && angle <= 180)
                SetServoPulse(channel, angle * (2000 / 180) + 501);
        }

        private void SetServoPulse(int channel, int pulse)
        {
            SetPWM(channel, 0, pulse * 4096 / 20000);
        }

        private void SetPWM(int channel, int on, int off)
        {
            IIC_Write(__LED0_ON_L + 4 * channel, on & 0xFF);
            IIC_Write(__LED0_ON_H + 4 * channel, on >> 8);
            IIC_Write(__LED0_OFF_L + 4 * channel, off & 0xFF);
            IIC_Write(__LED0_OFF_H + 4 * channel, off >> 8);
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
