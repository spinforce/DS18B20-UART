using System;
using System.IO.Ports;
using System.Collections;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;

namespace DS18B20UART_OW
{
    class DS18B20
    {

        enum Bit { HIGH, LOW};
        public enum Command:byte {Search=0xF0,ReadRom=0x33,MatchRom=0x55,ConvertT=0x44, ReadScratchpad=0xbe, SkipRom=0xcc, WriteScratchpad=0x4e }
        public enum TranferCounts:byte { ReadScratchpad=9, MatchRom=8, WriteScratchpad=3}
        SerialPort _sPort;
       
        public byte[] Buffer;
        private byte Bit_last_Conflict_pos;
        public byte Devices;  
       
        bool LastDevice;
       

 

        public DS18B20(string Port)
        {
            _sPort = new SerialPort();
            _sPort.ReadTimeout = 1000;
            _sPort.DataReceived += _sPort_DataReceived;

            //Command 1byte + Scratch 9byte oder Rom 8 byte 0-7 Command 8- Data
            //Ein Byte im Buffer entspricht einem Bit
            Buffer = new byte[8 * 10];
            _sPort.ReadBufferSize = 8 * 10;

            _sPort.PortName = Port;
            _sPort.DataBits = 8;
            _sPort.StopBits = StopBits.One;
            _sPort.Parity = Parity.None;
            _sPort.Open();

            Bit_last_Conflict_pos = 0;
            Devices = 0;
            LastDevice = false;

        }

        private void _sPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort s = (SerialPort)sender;
            //Console.WriteLine("Data da {0}",s.BytesToRead.ToString());
           // int offset = 0;
           // if (s.BytesToRead > 8) offset = 8;
           // _sPort.Read(Buffer, offset, s.BytesToRead);
           // tasklock = false;
           
        }

        ~DS18B20()
        {
            _sPort.Close();
        }

        public bool Reset()
        {

            _sPort.BaudRate = 9600;

           
            Buffer[0] = 0xF0;
            //WriteCommandToPort(1); 
            WriteToPort(1);

            if (Buffer[0] != 0xF0)
            {
               if(Devices < 1 ) Devices = 1;
                _sPort.BaudRate = 115200;              
                return true;
            }
            
            return false;
        }
        /// <summary>
        /// Sucht Bus nach Sensoren
        /// </summary>
        /// <param name="Address">Referenz auf Byte Array, gibt Adresse zurück</param>
        /// <returns>Letzter sensor = false</returns>
        //public bool Search(ref byte[] Address)
        public bool Search(ref DS18B20_Address Address)
        {
         
            if (LastDevice || Devices < 1 ) return true;

           
            ByteToBuffer(Command.Search);
            //Send Command
            //WriteCommandToPort(8);
            WriteToPort(8);
            System.Threading.Thread.Sleep(5);

            //Clear Data Buffer(+8) from last Conflict
            for (int ix = Bit_last_Conflict_pos + 1 + 8; ix < 8 * 8; ix++) Buffer[ix] = 0x0;

            for (byte BitsPos = 0; BitsPos < 8 * 8; BitsPos++)
            {

                ByteToBuffer(3); //0b00000011

                //Get Payload
                //WriteCommandToPort(2);
                WriteToPort(2);


                switch (BufferToBytes(0,1)[0])
                {
                    case 0:
                        //Konflikt 2 mögliche Pfade wähle erst 0 dann beim nächtsetn mal 1
                         if (Buffer[BitsPos + 8] == 0x00) Bit_last_Conflict_pos = BitsPos;
                        break;

                    case 1:
                        //Antwort Write 1
                        SetBufferDataBit(BitsPos, Bit.HIGH);
                        break;


                    case 2:
                        //Antwort Write 0
                        SetBufferDataBit(BitsPos, Bit.LOW);
                        break;

                    case 3:
                        //Fehler ToDo...
                        break;
                }
                //Im Datenbuffer ist das nächste Kommando enthalen
                Buffer[0] = Buffer[BitsPos + 8];
                //WriteCommandToPort(1);
                WriteToPort(1);
            }

            //aktuelles Ergebniss speichern
            //Address = BufferToBytes(8,8);
            Address = GetSensorAddress();

            //Pfad,Buffer für nächsten Durchlauf vorbereiten
            if (Buffer[Bit_last_Conflict_pos + 8] != 0xFF && Bit_last_Conflict_pos!=0)
            {
                SetBufferDataBit(Bit_last_Conflict_pos, Bit.HIGH);
                Devices++;
                LastDevice = false;
            }
            else LastDevice = true;

          
            return false;

        }


        private void SetBufferDataBit(byte pos, Bit b)
        {
            if (b == Bit.HIGH) Buffer[pos+8] = 0xFF;
            else Buffer[pos+8] = 0x00;
        }


        public byte[] Transfer(Command command)
        {
            return Transfer(command, (byte)0);
        }

        public byte[] Transfer(Command command, TranferCounts count)
        {
            return Transfer(command, (byte)count);
        }



        public byte[] Transfer(Command command, byte count)
        {
           
          
           if(command == Command.ReadScratchpad || command == Command.ReadRom)
                    for (int ix = 0; ix < 8 * (count+1); ix++) Buffer[ix] = 0xFF;

            ByteToBuffer(command);
            //Send Command
            //WriteCommandToPort(8);
            WriteToPort(8);

            System.Threading.Thread.Sleep(5);

            //if (count > 0) WriteDataToPort((byte)(count * 8));
            if (count > 0) WriteToPort((byte)(count * 8));

            //return BufferToBytes(0,(byte)(count + 1));
           
            return BufferToBytes(8, count);
        }

 
        private void WriteToPort(byte count)
        {


            //int sleep = 5;
            int offset = 0;
            _sPort.DiscardOutBuffer();

            //Command oder Daten? Größer 8  Daten
            if (count > 8) { offset = 8; }//sleep = 11; }
            _sPort.ReceivedBytesThreshold = count;
            _sPort.BaseStream.Write(Buffer, offset, count);
           // Console.WriteLine("DEBUG Warte auf {0} Bytes", count.ToString());
            //System.Threading.Thread.Sleep(sleep);


            DateTime d = DateTime.Now;
            DateTime max = d.AddMilliseconds(100);

            while (_sPort.BytesToRead != count && d < max)
            {
                d = DateTime.Now;
                System.Threading.Thread.Sleep(1);
            }

            if (d < max)
            {
                if (_sPort.BytesToRead == count)
                {
                    //  Console.WriteLine("DEBUG Lese {0} Bytes im Buffer", _sPort.BytesToRead.ToString());
                    //System.Threading.Thread.Sleep(1);
                    _sPort.Read(Buffer, offset, _sPort.BytesToRead);
                }
                else Console.WriteLine("DEBUG Lesebuffer ungleich der erwarteten Bytes");
            }
            else
            {
                Console.WriteLine("DEBUG TimeOut bei WriteToPort");
            }

        }


 

        /// <summary>
        /// Verteilt Bits eines Byte an angegebener Position in den Buffer.
        /// z.B. Byte = 0xf0 Buffer = {0x00,0x00,0x00,0x00,0xff,0xff,0xff,0xff} LSB beginnend. 
        /// Pos 0 = Command 
        /// </summary>
        /// <param name="b">Byte</param>
        /// <param name="pos">Byte Position im Buffer</param>
        private void ByteToBuffer(byte b, byte pos = 0)
        {
            byte rol = 0x1;

            for (int ix = pos * 8; ix < pos * 8 + 8; ix++)
            {
                if ((b & rol) == 0) Buffer[ix] = 0;
                else Buffer[ix] = 0xff;
                rol <<= 1;
            }
        }
        private void ByteToBuffer(Command c)
        {
            ByteToBuffer((byte)c, 0);
        }

        /// <summary>
        /// Setzt die Bytes entsprechent Bits im Buffer wieder zu einem Byte Array zusammen.
        /// z.B.: count=2 Buffer an Pos = {0x00,0x00,0x00,0x00,0xff,0xff,0xff,0xff, 0xff,0x00,0x00,0x00,0xff,0xff,0xff,0x00}
        /// Ergibt {0xf0,0x71}
        /// </summary>
        /// <param name="offset">Postition im Buffer</param>
        /// <param name="count">Anzahl Bit-Bytes</param>
        /// <returns></returns>
        private byte[] BufferToBytes(byte offset, byte count)
        {

            byte[] bytes;
            bytes = new byte[count];
            int rBufferPointer = offset;


            for (int iy = 0; iy < count; iy++)
            {
                byte rol = 1;
                byte b = 0;

                for (int ix = 0; ix < 8; ix++)
                {
                    if (Buffer[rBufferPointer] == 0xFF) b |= rol;                    
                    else Buffer[rBufferPointer] = 0;

                    rBufferPointer++;
                    rol <<= 1;
                }
                bytes[iy] = b;
            }
            return bytes;
        }

        public DS18B20_Address GetSensorAddress()
        {
            byte[] b = BufferToBytes(8, 8);
            Array.Reverse(b);//Ist verkehrt im Buffer
            return new DS18B20_Address(b);
        }

        public void SetSensorAddress(DS18B20_Address Address)
        {
            byte[] b = Address;
            Array.Reverse(b); //Muss umgekehrt in den Buffer;
            for (byte ix = 0; ix < 8; ix++) ByteToBuffer(b[ix], (byte)(ix + 1));
        }
		
		public void SetSensorAddress(string stringAD)
        {
            //DS18B20_Address Ad = 
            byte[] b = new DS18B20_Address(stringAD); 
            Array.Reverse(b); //Muss umgekehrt in den Buffer;
            for (byte ix = 0; ix < 8; ix++) ByteToBuffer(b[ix], (byte)(ix + 1));
        }

        public void Scratchpad2Buffer(byte[] sp)
        {
            for (byte ix = 0; ix < 3; ix++) ByteToBuffer(sp[ix], (byte)(ix + 1));
        }

        public static byte CRC8(byte[] check)
        {
            int crc = 0;            

            for (int x = check.Length-1; x > 0; x--)
            {
                int inbyte = check[x];

                for (byte i = 0; i<8; i++)
                {
                   int mix = (crc ^ inbyte) & 0x01;
                    crc >>= 1;
                    if (mix!=0) crc ^= 0x8C;
                    inbyte >>= 1;
                }
            }
            return (byte)crc;
        }

    }

    class DS18B20_Address
    {
        public enum Device : byte { Device18B20 = 0x28 }

        public byte CRC;
        public byte[] SerialNumber = new byte[6];
        public Device FamilyCode;
        private string _address;
        public string Address
        {
            get
            {
                _address = String.Format("{0:x2}:{1:x2}:{2:x2}:{3:x2}:{4:x2}:{5:x2}:{6:x2}:{7:x2}", CRC, SerialNumber[0], SerialNumber[1], SerialNumber[2], SerialNumber[3], SerialNumber[4], SerialNumber[5], (byte)FamilyCode);
                return _address;
            }
            set
            {
                Regex rx = new Regex(@"^[0-9a-f].:[0-9a-f].:[0-9a-f].:[0-9a-f].:[0-9a-f].:[0-9a-f].:[0-9a-f].:[0-9a-f].$", RegexOptions.IgnoreCase);

                if (rx.IsMatch(value)) _address = value;
                else _address = "00:00:00:00:00:00:00:00";

                byte[] temp = new byte[8];

                string[] t = _address.Split(':');

                for (int ix = 0; ix < 8; ix++) temp[ix] = byte.Parse(t[ix],System.Globalization.NumberStyles.HexNumber);
                DeSerialize(temp);
            }
        }
        public bool CheckCRC()
        {
            return (DS18B20.CRC8(this) == CRC);
        }

        public DS18B20_Address()
        {
            Address = "00:00:00:00:00:00:00:00";
        }
        public DS18B20_Address(DS18B20_Address ad)
        {
            CRC = ad.CRC;
            SerialNumber = (byte[])ad.SerialNumber.Clone();
            FamilyCode = ad.FamilyCode;
        }
        public DS18B20_Address(string ad)
        {
            Address = ad;
        }
        public DS18B20_Address(byte[] b)
        {
            DeSerialize(b);
        }

        public void DeSerialize(byte[] bytes)
        {
            BinaryReader br = new BinaryReader(new MemoryStream(bytes));
            CRC = br.ReadByte();
            SerialNumber = br.ReadBytes(6);
            FamilyCode = (Device)br.ReadByte();

        }

        public static implicit operator byte[] (DS18B20_Address ad)
        {
            MemoryStream ms = new MemoryStream(8);
            BinaryWriter bw = new BinaryWriter(ms);

            bw.Write(ad.CRC);
            bw.Write(ad.SerialNumber);
            bw.Write((byte)ad.FamilyCode);

            return ms.GetBuffer();

        }

        public override string ToString()
        {
            return Address;
        }

        public string ToString(string format)
        {
            String tmp = string.Empty;
            //if (formatProvider == null) formatProvider = System.Globalization.CultureInfo.CurrentCulture;
            switch (format)
            {
                case "SN":
                    tmp = String.Format("{0:x2}:{1:x2}:{2:x2}:{3:x2}:{4:x2}:{5:x2}", SerialNumber[0], SerialNumber[1], SerialNumber[2], SerialNumber[3], SerialNumber[4], SerialNumber[5]);
                    break;
                case "DEV":
                    switch(FamilyCode)
                    {
                        case Device.Device18B20:
                            tmp = String.Format("18B20");
                            break;
                    }                
                
                break;

                default:
                    tmp = Address;
                    break;
            }
            return tmp;

        }

  
    }


   
}
