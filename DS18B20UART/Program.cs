//#define MATCH_ROM

using System;
using System.Collections;
using System.Collections.Generic;
using DS18B20UART_OW;




namespace DS18B20UART
{
    class Program
    {

       
           
        static DS18B20_SctatchPad ScratchPad = new DS18B20_SctatchPad();




        static void Main(string[] args)
        {

            Console.Clear();
           // DS18B20 sensor = new DS18B20("COM27");
            DS18B20 sensor = new DS18B20("COM2");

            DS18B20_Address SensorAddress = new DS18B20_Address();
            List<DS18B20_Address> SensorAddresses = new List<DS18B20_Address>(8);

            //********************* Ist wer da ? ********************************
            if (!sensor.Reset())
            {
                Console.WriteLine("Nix Sensor\r\n");
                return;
            }

            Console.WriteLine("Sensor da :-)\r\n");


           

            while (!sensor.Search(ref SensorAddress))
            {

                Console.Write("Sensor ");
  
                //PrintBytes(Address);
                Console.WriteLine("Adresse:\t{0}",SensorAddress.Address);
                Console.WriteLine("CRC:\t\t{0}", SensorAddress.CRC.ToString("x2"));
                Console.Write("CRC Check\t{0:x2} ", DS18B20.CRC8(SensorAddress));
               
                Console.WriteLine("{0}", SensorAddress.CheckCRC() ? "OK":"Fehler");
                Console.WriteLine("Seriennummer:\t{0}", SensorAddress.ToString("SN"));
                Console.WriteLine("Device Typ:\t{0}\r\n", SensorAddress.ToString("DEV"));
              

                SensorAddresses.Add(SensorAddress);

                
               
               

                sensor.Reset();
                
            }

            Console.WriteLine("{0} Sensor(en) gefunden", sensor.Devices);
            Console.WriteLine("Weiter Taste drücken");
            Console.ReadKey();

     





            //********************* Nur ein Sensor am Bus. Wer da ist da ? ********************************

            /*
                        bytes = ow.Transfer(0x33, 8, OnWire.TransferDirection.Read);
                        

                        Console.Write("Memory:");
                        for (byte ix = 1; ix < bytes.Length - 1; ix++) Console.Write("{0:x2}:", bytes[ix]);
                        Console.WriteLine("{0:x2}\r\n", bytes[bytes.Length - 1]);

    */


            //Beispiel
            //Ersten Sensor auf 12 Bit umstellen
            //MatchRom = Gezielt Sensor auswählen, Skiprom = alle Sensoren

            //Aktuelles Scratchpad auslesen

            //Sensor Adresse in Buffer laden
            sensor.SetSensorAddress(SensorAddresses[0]);
            sensor.Transfer(DS18B20.Command.MatchRom, DS18B20.TranferCounts.MatchRom);
            ScratchPad = sensor.Transfer(DS18B20.Command.ReadScratchpad, DS18B20.TranferCounts.ReadScratchpad);

            DS18B20_SctatchPad.Resolution r = DS18B20_SctatchPad.Resolution.Bits12;


            if (ScratchPad.ConfigRegister != r)
            {
                Console.WriteLine("Configregister auf gewünschte Auflösung umstellen");
                sensor.Reset();

                //Config Register auf 12Bit Setzen
                ScratchPad.ConfigRegister = r;

                //Sensor Adresse in Buffer laden

                sensor.SetSensorAddress(SensorAddresses[0]);
                sensor.Transfer(DS18B20.Command.MatchRom, DS18B20.TranferCounts.MatchRom);

                //Die 3 Bytes aus dem Scratchpad retour schreiben
                sensor.Scratchpad2Buffer(ScratchPad.SerializeForWrite());
                sensor.Transfer(DS18B20.Command.WriteScratchpad, DS18B20.TranferCounts.WriteScratchpad);
            }
            else Console.WriteLine("Configregister hat bereits die gewünschte Auflösung");

            System.Threading.Thread.Sleep(2000);

            while (true)
            {
                Console.Clear();

#if !MATCH_ROM
                //Skip Rom
                StartConvertion(true,ref sensor);
#endif

                for (byte iy = 0; iy < SensorAddresses.Count; iy++)
                {
                    //Sensor Adresse in Buffer laden
                   sensor.SetSensorAddress(SensorAddresses[iy]);

#if MATCH_ROM
                    //Ohne SkipRom, Match ROM
                    StartConvertion(false,ref sensor);
#endif

                    ReadScratchPad(ref sensor);

                }
                //Console.WriteLine("Nächste Messung Taste drücken");
                //Console.ReadKey();
                System.Threading.Thread.Sleep(5000);
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="SkipRom">true SkipROM oder nicht</param>
        static void StartConvertion(bool SkipRom,ref DS18B20 sensor)
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


                Console.WriteLine(sensor.GetSensorAddress());

                //Transfer Command und Sensor Adresse 8 Bytes
                sensor.Transfer(DS18B20.Command.MatchRom, DS18B20.TranferCounts.MatchRom);
            }

            //Start Temp Convertion nur Command
            Console.WriteLine("Start Convertion\r\n");
            sensor.Transfer(DS18B20.Command.ConvertT);

            //Datenblatt max Convertion Time
            System.Threading.Thread.Sleep(800);

        }

        static void ReadScratchPad(ref DS18B20 sensor)
        {

            //********************* Read Scratchpad ********************************
            Console.WriteLine("Reset Bus");
            if (!sensor.Reset())
            {
                Console.WriteLine("Nix Sensor\r\n");
                return;
            }

            //Select Sensor
            Console.WriteLine("Read Scratchpad für Sensor:\t{0}", sensor.GetSensorAddress());  

            //Transfer Command und Sensor Adresse 8 Bytes
            sensor.Transfer(DS18B20.Command.MatchRom, DS18B20.TranferCounts.MatchRom);
            ScratchPad=sensor.Transfer(DS18B20.Command.ReadScratchpad, DS18B20.TranferCounts.ReadScratchpad);

            Console.Write("Scratchpad:\t\t\t");
            //PrintBytes(ScratchPad.Serialize());
            Console.WriteLine(ScratchPad);

           
            Console.Write("Check CRC\t\t\t{0:x2} ", DS18B20.CRC8(ScratchPad));
            Console.WriteLine("{0}", ScratchPad.CheckCRC() ? "OK" : "Fehler");

            Console.WriteLine("Configregister:\t\t\t{0:x2}", (byte)ScratchPad.ConfigRegister);


            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Temp: {0:F4}°C\r\n", ScratchPad.GetTemp());
            Console.ResetColor();
        }     
        
    }

}
