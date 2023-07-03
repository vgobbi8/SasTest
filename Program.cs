// See https://aka.ms/new-console-template for more information
using SasTest;

Console.WriteLine("Hello, World!");

var obj = new UdpClientExample();

var tReceive = new Thread(obj.ReceiveThread);
tReceive.Start();

obj.PerformFullTest();
