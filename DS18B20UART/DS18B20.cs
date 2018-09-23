using System;
using System.IO.Ports;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;




namespace DS18B20UART_OW
{
    class DS18B20
    {

        enum Bit { HIGH, LOW};
        public enum Command {Search=0xF0,ReadRom=0x33,MatchRom=0x55,ConvertT=0x44, ReadScratchpad=0xbe, SkipRom=0xcc }

        SerialPort _sPort;

        public byte[] Buffer;
        private byte Bit_last_Conflict_pos;
        private const double Temp12 = 0.0625;
        private const double Temp11 = 0.125;
        private const double Temp10 = 0.25;
        private const double Temp9 = 0.5;
        public byte Devices;  
       
        bool LastDevice;

 

        public DS18B20(string Port)
        {
            _sPort = new SerialPort();

            //Command 1byte + Scratch 9byte oder Rom 8 byte 0-7 Command 8- Data
            //Ein Byte im Buffer entspricht einem Bit
            Buffer = new byte[8 * 10];

            _sPort.PortName = Port;
            _sPort.DataBits = 8;
            _sPort.StopBits = StopBits.One;
            _sPort.Parity = Parity.None;
            _sPort.Open();

            Bit_last_Conflict_pos = 0;
            Devices = 0;
            LastDevice = false;

        }

        ~DS18B20()
        {
            _sPort.Close();
        }

        public bool Reset()
        {

            _sPort.BaudRate = 9600;
            Buffer[0] = 0xF0;
            WriteCommandToPort(1); 

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
       public bool Search(ref byte[] Address)
        {
            if (LastDevice || Devices < 1 ) return true;

            ByteToBuffer(Command.Search, 0);
            //Send Command
            WriteCommandToPort(8);

            System.Threading.Thread.Sleep(5);

            //Clear Data Buffer(+8) from last Conflict
            for (int ix = Bit_last_Conflict_pos + 1 + 8; ix < 8 * 8; ix++) Buffer[ix] = 0x0;

            for (byte BitsPos = 0; BitsPos < 8 * 8; BitsPos++)
            {

                ByteToBuffer(3, 0); //0b00000011

                //Get Payload
                WriteCommandToPort(2);


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
                WriteCommandToPort(1);

            }

            //aktuelles Ergebniss speichern
            Address = BufferToBytes(8,8);

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
            return Transfer(command, 0);
        }

        public byte[] Transfer(Command command, byte count)
        {
          
           if(command == Command.ReadScratchpad || command == Command.ReadRom)
                    for (int ix = 0; ix < 8 * (count+1); ix++) Buffer[ix] = 0xFF;

            ByteToBuffer(command, 0);
            //Send Command
            WriteCommandToPort(8);

            System.Threading.Thread.Sleep(5);

            if (count > 0) WriteDataToPort((byte)(count * 8));
 
            //return BufferToBytes(0,(byte)(count + 1));
            return BufferToBytes(8, count);

        }

        /// <summary>
        /// Schreibe # Bits from Databuffer zum Port
        /// </summary>
        /// <param name="count">Nr. Bits to Write</param>
        private void WriteDataToPort(byte count)
        {

            _sPort.Write(Buffer, 8, count);
            while (_sPort.BytesToRead != count) System.Threading.Thread.Sleep(1);
            _sPort.Read(Buffer, 8, count);

        }


        /// <summary>
        /// Schreibe # Bits vom Commandbuffer zum Port
        /// </summary>
        /// <param name="count">Nr. Bits to Write</param>
        private void WriteCommandToPort(byte count)
        {
            _sPort.Write(Buffer, 0, count);
            while (_sPort.BytesToRead != count) System.Threading.Thread.Sleep(1);
            _sPort.Read(Buffer, 0, count);
        }

        /// <summary>
        /// Verteilt Bits eines Byte an angegebener Position in den Buffer.
        /// z.B. Byte = 0xf0 Buffer = {0x00,0x00,0x00,0x00,0xff,0xff,0xff,0xff} LSB beginnend. 
        /// Pos 0 = Command 
        /// </summary>
        /// <param name="b">Byte</param>
        /// <param name="pos">Byte Position im Buffer</param>
        public void ByteToBuffer(byte b, byte pos)
        {
            byte rol = 0x1;

            for (int ix = pos * 8; ix < pos * 8 + 8; ix++)
            {
                if ((b & rol) == 0) Buffer[ix] = 0;
                else Buffer[ix] = 0xff;
                rol <<= 1;
            }
        }
        private void ByteToBuffer(Command c, byte pos)
        {
            ByteToBuffer((byte)c, pos);
        }

        /// <summary>
        /// Setzt die Bytes entsprechent Bits im Buffer wieder zu einem Byte Array zusammen.
        /// z.B.: count=2 Buffer an Pos = {0x00,0x00,0x00,0x00,0xff,0xff,0xff,0xff, 0xff,0x00,0x00,0x00,0xff,0xff,0xff,0x00}
        /// Ergibt {0xf0,0x71}
        /// </summary>
        /// <param name="offset">Postition im Buffer</param>
        /// <param name="count">Anzahl Bit-Bytes</param>
        /// <returns></returns>
        public byte[] BufferToBytes(byte offset, byte count)
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

        public double GetTemp()
        {

            double t_result;
            byte[] bytes = BufferToBytes(8, 2);
            int t = (bytes[1] << 8) | bytes[0];
            if ((t & 0xf800) != 0) t_result = -(double)~(t - 1);
            else t_result = t;
            t_result *= Temp12;

            return t_result;

        }
    }
}
