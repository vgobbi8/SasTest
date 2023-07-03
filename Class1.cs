using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Reflection.Metadata;

namespace SasTest
{
    public class UdpClientExample
    {

        static string torque_csv_name = "torque.csv";
        static string rpm_csv_name = "rpm.csv";
        static string torque_output_csv_name = "torque_output.csv";
        static string rpm_output_csv_name = "rpm_output.csv";

        string torque_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, torque_csv_name);
        string rpm_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, rpm_csv_name);
        string torque_output_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, torque_output_csv_name);
        string rpm_output_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, rpm_output_csv_name);


        string log_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log_script.txt");

        bool bContinue = true;

        int errosTorque = 0;
        int errosRPM = 0;


        List<string> sentTorque = new List<string>();
        List<string> sentRPM = new List<string>();

        List<string> receivedTorque = new List<string>();
        List<string> receivedRPM = new List<string>();




        private StringBuilder sbLog = new StringBuilder();

        public void ReceiveThread()
        {
            bContinue = true;
            byte[] Message;
            IPEndPoint mTempIpEndPoint = new IPEndPoint(IPAddress.Loopback, 9001);
            IPEndPoint localpt = new IPEndPoint(IPAddress.Loopback, 9001);
            UdpClient mUdpClient = new UdpClient();
            mUdpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            mUdpClient.Client.Bind(localpt);
            mUdpClient.Client.ReceiveBufferSize = 24000;


            while (bContinue)
            {
                if (mUdpClient.Available > 0)
                {
                    Message = mUdpClient.Receive(ref mTempIpEndPoint);

                    string s = Encoding.UTF8.GetString(Message, 0, Message.Length);
                    HandleReceive(s);
                    Console.WriteLine($"Received: {s}");
                }
            }
        }

        private void HandleReceive(string data)
        {
            if (data.StartsWith("T"))
            {
                receivedTorque.Add(data);

            }
            else if (data.StartsWith("R"))
            {
                receivedRPM.Add(data);
            }
        }

        private List<MyCsvData> ReadCsvTorque()
        {

            using (var reader = new StreamReader(torque_path))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csv.Context.RegisterClassMap<TorqueMap>();
                var records = csv.GetRecords<MyCsvData>();
                return records.ToList();
            }

        }
        private List<MyCsvData> ReadCsvRPM()
        {

            using (var reader = new StreamReader(rpm_path))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csv.Context.RegisterClassMap<RPMMap>();
                var records = csv.GetRecords<MyCsvData>();
                return records.ToList();
            }
        }


        public async void PerformFullTest()
        {
            //Teste Torque
            var dadosTorque = ReadCsvTorque();

            sbLog.Clear();
            sentRPM.Clear();
            sentTorque.Clear();
            receivedRPM.Clear();
            receivedTorque.Clear();

            errosTorque = 0;
            var csvTorque = new StringBuilder();

            csvTorque.AppendLine("passo,Descrição,Resultado Esperado, Resultado Obtido,Status");
            csvTorque.AppendLine("1,Executar o Script Teste Torque, Script Teste Torque sendo executado,Script Teste Torque Sendo Executado, ");


            foreach (var item in dadosTorque)
            {
                //Envia para a porta
                var comando = $"T{item.Leitura}";
                await EnviaComando(comando);
                Log($"Enviando: {comando}");

                sentTorque.Add(comando);
                while (sentTorque.Count > receivedTorque.Count)
                {
                    //Aguarda receber todos
                }


                //Quando receber
                var resultado = receivedTorque.Last(); // [receivedTorque.Count - 1];
                Log($"Recebido: {resultado}");
                //Gets the value. The string is formated: T{value}
                var value = double.Parse(resultado.Replace("T", ""));
                if (value != item.Resultado)
                {
                    errosTorque++;
                }
                System.Threading.Thread.Sleep(100);
            }

            var passou = errosTorque == 0 ? "Passou" : "Falhou";

            csvTorque.AppendLine($"2,Ler o contador de erros do script Teste Torque,Contador de erros igual a 0,Contador de erros igual a {errosTorque},{passou}");

            csvTorque.AppendLine($"3,Final de execução,,,");
            File.WriteAllText(torque_output_path, csvTorque.ToString(), Encoding.UTF8);

            //Teste Torque
            var dadosRPM = ReadCsvRPM();
            errosRPM = 0;
            var csvRPM = new StringBuilder();

            csvRPM.AppendLine("passo,Descrição,Resultado Esperado, Resultado Obtido,Status");
            csvRPM.AppendLine("1,Executar o Script Teste RPM, Script Teste RPM sendo executado,Script Teste RPM Sendo Executado, ");


            foreach (var item in dadosRPM)
            {
                //Envia para a porta
                var comando = $"R{item.Leitura}";
                await EnviaComando(comando);
                Log($"Enviando: {comando}");
                sentRPM.Add(comando);
                while (sentRPM.Count > receivedRPM.Count)
                {
                    //Aguarda receber todos
                }
                //Quando receber
                var resultado = receivedRPM.Last();// [receivedRPM.Count - 1];
                Log($"Recebido: {resultado}");
                //Gets the value. The string is formated: T{value}
                var value = double.Parse(resultado.Replace("R", ""));
                if (value != item.Resultado)
                {
                    errosRPM++;
                }
                System.Threading.Thread.Sleep(100);
            }
            var passouRPM = errosRPM == 0 ? "Passou" : "Falhou";

            csvRPM.AppendLine($"2,Ler o contador de erros do script Teste Torque,Contador de erros igual a 0,Contador de erros igual a {errosTorque},{passou}");

            csvRPM.AppendLine($"3,Final de execução,,,");
            File.WriteAllText(rpm_output_path, csvRPM.ToString(),Encoding.UTF8);
            File.WriteAllText(log_path, sbLog.ToString());
        
        }

        private async Task EnviaComando(string comando)
        {
            // Specify the IP address and port
            string ipAddress = "127.0.0.1"; // Replace with the actual IP address
            int port = 9000; // Replace with the actual port number
            // Create a UDP client
            using (UdpClient udpClient = new UdpClient())
            {
                try
                {
                    // Encode the command as bytes
                    byte[] commandBytes = Encoding.ASCII.GetBytes(comando);

                    // Send the command to the specified IP address and port
                    await udpClient.SendAsync(commandBytes, commandBytes.Length, ipAddress, port);
             
                    Console.WriteLine($"Command '{comando}' sent successfully to {ipAddress}:{port}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error occurred while sending/receiving command: {e.Message}");
                }
            }
        }


        public void Log(string str)
        {
            sbLog.AppendLine(str);
        }




        public void Stop()
        {
            bContinue = false;
        }



        
    }

    public class Execucao
    {
        public int Passo { get; set; }
        public string Descricao { get; set; }
        public string ResultadoEsperado { get; set; }
        public string Status { get; set; }


        public Execucao()
        {
            Descricao = "Injetar o valor de Torque lido pelo sensor igual 10";

        }
    }

    public class MyCsvData
    {
        public float Leitura { get; set; }
        public float Resultado { get; set; }
    }


    public class TorqueMap : ClassMap<MyCsvData>
    {
        public TorqueMap()
        {
            Map(p => p.Leitura).Name("Leitura (Volts)");
            Map(p => p.Resultado).Name("Torque (%)");
        }
    }

    public class RPMMap : ClassMap<MyCsvData>
    {
        public RPMMap()
        {
            Map(p => p.Leitura).Name("Leitura (Hz)");
            Map(p => p.Resultado).Name("RPM (%)");
        }
    }
}
