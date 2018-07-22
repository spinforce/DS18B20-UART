using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using DS18B20UART_OW;


namespace DS18B20UART
{
    class Program
    {
        //static byte[] mySensor = { 0x28, 0x95, 0xa8, 0x27, 0x00, 0x00, 0x80, 0x33 };
        //static byte[] mySensor = { 0x28, 0x3a, 0xf9, 0x27, 0x00, 0x00, 0x80, 0x13 };
       // static byte[] mySensor = { 0x28, 0x77, 0x99, 0x27, 0x0, 0x0, 0x80, 0x3d }; 


        static byte[] bytes;
        static byte[] Address = new byte[8];

        static ArrayList Sensors = new ArrayList(8);



        static DS18B20 sensor = new DS18B20("COM16");

        


        static void Main(string[] args)
        {


            //********************* Ist wer da ? ********************************
            Console.Clear();

            if (!sensor.Reset())
            {
                Console.WriteLine("Nix Sensor\r\n");
                return;
            }

            Console.WriteLine("Sensor da :-)\r\n");




            while (!sensor.Search(ref Address))
            {

                Console.Write("Sensor ");

                PrintBytes(Address);

                Sensors.Add(Address);

                sensor.Reset();

            }

            Console.WriteLine("{0} Sensor(en) gefunden", sensor.Devices);
            System.Threading.Thread.Sleep(2000);


            //********************* Nur ein Sensor am Bus. Wer da ist da ? ********************************

            /*
                        bytes = ow.Transfer(0x33, 8, OnWire.TransferDirection.Read);
                        

                        Console.Write("Memory:");
                        for (byte ix = 1; ix < bytes.Length - 1; ix++) Console.Write("{0:x2}:", bytes[ix]);
                        Console.WriteLine("{0:x2}\r\n", bytes[bytes.Length - 1]);

    */



            while (true)
            {
                Console.Clear();

                //Skip Rom
                StartConvertion(true);

                for (byte iy = 0; iy < Sensors.Count; iy++)
                {
                    for (byte ix = 0; ix < 8; ix++) sensor.ByteToBuffer(((byte[])Sensors[iy])[ix], (byte)(ix + 1));

                    //Ohne SkipRom
                   // StartConvertion(false);
                    ReadScratchPad();

                }
                System.Threading.Thread.Sleep(5000);
            }

        }

        static void PrintBytes(byte[] b)
        {
            for (byte ix = 0; ix < b.Length - 1; ix++) Console.Write("{0:x2}:", b[ix]);
            Console.WriteLine("{0:x2}", b[b.Length - 1]);

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="SkipRom">true SkipROM oder nicht</param>
        static void StartConvertion(bool SkipRom)
        {
            //********************* Start Convertion ********************************

            Console.WriteLine("Reset Bus");
            if (!sensor.Reset())
            {
                Console.WriteLine("Nix Sensor\r\n");
                return;
            }


            //Select Sensor
            Console.Write("Select Sensor ");

            Address = sensor.BufferToBytes(8, 8);
            PrintBytes(Address);


            //Transfer Command und Sensor Adresse 8 Bytes
            if (SkipRom) sensor.Transfer(DS18B20.Command.SkipRom);
            else sensor.Transfer(DS18B20.Command.MatchRom, 8);


            //Start Temp Convertion nur Command
            Console.WriteLine("Start Convertion");
            sensor.Transfer(DS18B20.Command.ConvertT);

            //Datenblatt max Convertion Time
            System.Threading.Thread.Sleep(800);

        }

        static void ReadScratchPad()
        {

            //********************* Read Scratchpad ********************************
            Console.WriteLine("Reset Bus");
            if (!sensor.Reset())
            {
                Console.WriteLine("Nix Sensor\r\n");
                return;
            }

            //Select Sensor
            Console.WriteLine("Select Sensor wie vorher");
            sensor.Transfer(DS18B20.Command.MatchRom, 8);

            Console.WriteLine("Read Scratchpad");
            bytes = sensor.Transfer(DS18B20.Command.ReadScratchpad, 9);


            Console.Write("Scratchpad ");
            PrintBytes(bytes);



            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Temp: {0}°C\r\n", sensor.GetTemp());
            Console.ResetColor();
        }
    }

}
