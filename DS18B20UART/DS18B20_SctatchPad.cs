using System;
using System.IO;


namespace DS18B20UART_OW
{

    class DS18B20_SctatchPad
    {
        public enum Resolution : byte { Bits09 = 0x0f, Bits10 = 0x1f, Bits11 = 0x3f, Bits12 = 0x7f }

        public byte Temperature_LSB;
        public byte Temperature_MSB;
        public byte THRegister;
        public byte TLRegister;
        public Resolution ConfigRegister;
        public byte Reserved1, Reserved2, Reserved3;
        public byte CRC;


        const double Temp12 = 0.0625;
        const double Temp11 = 0.125;
        const double Temp10 = 0.25;
        const double Temp9 = 0.5;

        public DS18B20_SctatchPad()
        {          
        }

        public DS18B20_SctatchPad(byte[] b)
        {
            DeSerialize(b);
        }

        public Byte[] SerializeForWrite()
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            bw.Write(THRegister);
            bw.Write(TLRegister);
            bw.Write((byte)ConfigRegister);
            return ms.ToArray();

        }
        public static implicit operator byte[] (DS18B20_SctatchPad sp)
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);

            bw.Write(sp.CRC);
            bw.Write(sp.Reserved3);
            bw.Write(sp.Reserved2);
            bw.Write(sp.Reserved1);
            bw.Write((byte)sp.ConfigRegister);
            bw.Write(sp.TLRegister);
            bw.Write(sp.THRegister);
            bw.Write(sp.Temperature_MSB);
            bw.Write(sp.Temperature_LSB);

            return ms.ToArray();
        }

        public static implicit operator DS18B20_SctatchPad(byte[] b)
        {
            return new DS18B20_SctatchPad(b);
        }

        private void DeSerialize(byte[] array)
        {
            BinaryReader br = new BinaryReader(new MemoryStream(array,0,9));

            Temperature_LSB = br.ReadByte();
            Temperature_MSB = br.ReadByte();
            THRegister = br.ReadByte();
            TLRegister = br.ReadByte();
            ConfigRegister = (Resolution)br.ReadByte();
            Reserved1 = br.ReadByte();
            Reserved2 = br.ReadByte();
            Reserved3 = br.ReadByte();
            CRC = br.ReadByte();
        }

        public override string ToString()
        {
            return String.Format("{8:x2}:{7:x2}:{6:x2}:{5:x2}:{4:x2}:{3:x2}:{2:x2}:{1:x2}:{0:x2}",
                Temperature_LSB, Temperature_MSB, THRegister, TLRegister, (byte)ConfigRegister, Reserved1, Reserved2, Reserved3, CRC);

        }

        public bool CheckCRC()
        {
            return (DS18B20.CRC8(this) == CRC);
        }
        public double GetAlarmTemp()
        {
            return _GetTemp((THRegister << 8) | TLRegister);
        }

        public double GetTemp()
        {
            return _GetTemp((Temperature_MSB << 8) | Temperature_LSB);
        }

        public double _GetTemp(int t)
        {

            double t_result;
            

            //if ((t & 0xf800) != 0) t_result = -(double)~(t - 1);
            if ((t & 0xf800) != 0) t = -(~(t - 1));



            switch (ConfigRegister)
            {

                case Resolution.Bits09:
                    t >>= 3;
                    t_result = t;
                    t_result *= Temp9;
                    break;

                case Resolution.Bits10:
                    t >>= 2;
                    t_result = t;
                    t_result *= Temp10;
                    break;

                case Resolution.Bits11:
                    t >>= 1;
                    t_result = t;
                    t_result *= Temp11;
                    break;

                default:
                    t_result = t;
                    t_result *= Temp12;
                    break;
            }

            //Geht auch one case und shiften und nur mal Temp12. Also nur der Teil in default.
            //Ist nur zur übersicht.

            return t_result;

        }
		
		
		
		public string ToString(string format)
        {    
            String tmp = string.Empty;
            //if (formatProvider == null) formatProvider = System.Globalization.CultureInfo.CurrentCulture;
            switch (format)
            {
                case "Resolution":

                    switch (ConfigRegister)
                    {

                        case Resolution.Bits09:
                            tmp = "9BIT";
                            break;

                        case Resolution.Bits10:
                            tmp = "10BIT";
                            break;

                        case Resolution.Bits11:
                            tmp = "11BIT";
                            break;

                        case Resolution.Bits12:
                            tmp = "12BIT";
                            break;
                    }

                    break;

            }

            return tmp;

        }
    }



}
