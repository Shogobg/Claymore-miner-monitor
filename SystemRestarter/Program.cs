using System;
using System.Threading;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace SystemRestarter
{
	class Program
	{
		public static IConfiguration Configuration { get; set; }
		static int timeUnderThreshold = 0;

		static int minimumThreshold;
		static int maxTimeUnderThreshold;

		private static BackgroundWorker task = null;

		static void Main(string[] args)
		{
			Configuration = ConfigurationHelper.GetConfig();
			Int32.TryParse(Configuration["minimumThreshold"], out minimumThreshold);
			Int32.TryParse(Configuration["maxTimeUnderThreshold"], out maxTimeUnderThreshold);

			task = new BackgroundWorker();
			task.DoWork += (s, args1) =>
			{
				while (true)
				{
					TakeAction(new object());
					Thread.Sleep(1000);
				}
			};
			task.RunWorkerAsync();

			//countDownTimer = new Timer(TakeAction, null, 0, 1000);
			Console.ReadKey(true);
		}

		private static void TakeAction(object o)
		{
			Console.Write(DateTime.Now.ToString("yy/MM/dd H:mm:ss: "));

			try
			{
				var res = Newtonsoft.Json.JsonConvert.DeserializeObject<ClaymoreResponse>(sendCommand("{\"id\":0,\"jsonrpc\":\"2.0\",\"method\":\"miner_getstat1\"}"));

				int currHashrate;
				Int32.TryParse(res.Result[2].Split(';')[0], out currHashrate);

				Console.WriteLine("Current hash rate:" + currHashrate);

				if (currHashrate < minimumThreshold)
				{
					Console.ForegroundColor = ConsoleColor.Yellow;
					Console.WriteLine("Miner malfunction! Time under threshold: " + timeUnderThreshold);
					Console.ResetColor();
					timeUnderThreshold++;
				}
				else
				{
					timeUnderThreshold = 0;
				}
			}
			catch(Exception e)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("Miner unreachable! Time under threshold: " + timeUnderThreshold);
				Console.ResetColor();
				timeUnderThreshold++;
			}

			if(timeUnderThreshold > maxTimeUnderThreshold)
			{
				Console.ForegroundColor = ConsoleColor.Cyan;
				Console.WriteLine("Trying to restart the operating system!");
				Console.ResetColor();


				ProcessStartInfo proc = new ProcessStartInfo();
				proc.FileName = "cmd";
				proc.WindowStyle = ProcessWindowStyle.Hidden;
				proc.Arguments = "/C shutdown " + "-f -r -t 5";
				Process.Start(proc);
			}
		}

		private static string sendCommand(string command)
		{
			TcpClient tcpclnt = new TcpClient();

			int port = 0;
			Int32.TryParse(Configuration["port"], out port);

			tcpclnt.Connect(Configuration["ip"], port);
			Stream stm = tcpclnt.GetStream();

			ASCIIEncoding asen = new ASCIIEncoding();
			byte[] ba = asen.GetBytes(command);
			stm.Write(ba, 0, ba.Length);
			byte[] bb = new byte[1000];
			int k = stm.Read(bb, 0, 1000);

			StringBuilder Response = new StringBuilder();

			for (int i = 0; i < k; i++)
				Response.Append(Convert.ToChar(bb[i]));

			tcpclnt.Close();

			return Response.ToString();
		}
	}
}
