using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UploadLibrary;

namespace UploadTest
{
	class MyProgress : IProgressListener
	{
		void IProgressListener.onProgress(long curBytePos, long totalBytes)
		{
			Console.WriteLine(curBytePos + "/" + totalBytes);
		}
	}
	class Program
	{
		static void Main(string[] args)
		{
			//场景一，服务器端或者客户端直接上传文件到存储平台
			HuaweiDbankCloud HDC = new HuaweiDbankCloud(APPID, APPSECRET, APPNAME);
			Console.WriteLine(HDC.Upload("/dl/APPNAME/chat.rar", "E://chat.rar"));
			MyProgress progress = new MyProgress();

			//场景二，设置回调参数
			Thread th = new Thread(new ThreadStart(delegate()
						{
						Console.WriteLine(HDC.Upload("/dl/APPNAME/chat.rar", "E://chat.rar", progress));
						}
						)
					);
			th.Start();
			Console.WriteLine("OK");
		}
	}
}

