# DS18B20-UART
DS18B20 an PC Com Port C#

Kleine Inspiration OnWire DS18B20 Temperatur Sensor an Serieller Schnitstelle zu betreiben. 

Es wird ein USB2Seriell Konverter benötigt am besten mit +5V,GND,RX,TX (4 Pins) und eine kleine Schaltung um den TTL TX in einen Open Kollektor umzuwandeln. Der RX geht direkt an den DS Pin. Der UART verhaltet sich dann wie ein Loobback. (RX,TX kurz geschlossen)

Basis für die low level Kommunikation ist das APP214 von MAXIM
https://www.maximintegrated.com/en/app-notes/index.mvp/id/214

Die Mos Bausteine im Figure 2a. des Discrete open-drain buffer können auch gegen normale Transistoren z.B BC548 ersetzt werden.
In der Program.cs ist natürlich der entsprechende COM Port anzupassen.

Die Search Funktion ist komplett von mir geschrieben. Sie baut auf keiner der üblichen Anleitungen im Internet auf. Alle von mir getestetetn Sensoren haben zumindest funktioniert.

Viel Spaß
