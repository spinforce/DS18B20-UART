//#define MATCH_ROM

using System;
using System.Collections;
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



        static DS18B20 sensor = new DS18B20("COM27");

        


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

#if !MATCH_ROM
                //Skip Rom
                StartConvertion(true);
#endif

                for (byte iy = 0; iy < Sensors.Count; iy++)
                {
                    //Sensor Adresse in Buffer laden
                    for (byte ix = 0; ix < 8; ix++) sensor.ByteToBuffer(((byte[])Sensors[iy])[ix], (byte)(ix + 1));
                    //for (byte ix = 0; ix < 8; ix++) sensor.ByteToBuffer(((byte[])Sensors[iy])[ix], (byte)(ix + 1));

#if MATCH_ROM
                    //Ohne SkipRom, Match ROM
                    StartConvertion(false);
#endif

                    ReadScratchPad();

                }
                Console.WriteLine("Nächste Messung Taste drücken");
                Console.ReadKey();
                //System.Threading.Thread.Sleep(5000);
            }

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




           
            if (SkipRom)
            {
                Console.WriteLine("Skip ROM, alle Sensoren messen zusammen ");
                sensor.Transfer(DS18B20.Command.SkipRom);
            }
            else
            {
                //Select Sensor
                Console.Write("Match ROM jeder Sensor misst Separat. Select Sensor ");
                Address = sensor.BufferToBytes(8, 8);
                PrintBytes(Address);

                //Transfer Command und Sensor Adresse 8 Bytes
                sensor.Transfer(DS18B20.Command.MatchRom, 8);
            }

            //Start Temp Convertion nur Command
            Console.WriteLine("Start Convertion\r\n");
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
            Console.WriteLine("Read Scratchpad");
            Console.Write("Sensor Address:\t");

            PrintBytes(sensor.BufferToBytes(8, 8));

            //Transfer Command und Sensor Adresse 8 Bytes
            sensor.Transfer(DS18B20.Command.MatchRom,8);
            bytes = sensor.Transfer(DS18B20.Command.ReadScratchpad, 9);

            Console.Write("Scratchpad:\t");
            PrintBytes(bytes);



            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Temp: {0}°C\r\n", sensor.GetTemp());
            Console.ResetColor();
        }

        static void PrintBytes(byte[] b)
        {
            for (byte ix = 0; ix < b.Length - 1; ix++) Console.Write("{0:x2}:", b[ix]);
            Console.WriteLine("{0:x2}", b[b.Length - 1]);

        }
    } 

}
