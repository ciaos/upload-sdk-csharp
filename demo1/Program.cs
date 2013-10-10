using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UploadLibrary;

namespace UploadTest
{
	class Program
	{
		static void Main(string[] args)
		{
			//场景一，服务器端或者客户端直接上传文件到存储平台
			HuaweiDbankCloud HDC = new HuaweiDbankCloud(APPID, APPSECRET, APPNAME);
			Console.WriteLine(HDC.Upload("/dl/APPNAME/chat.rar", "E://chat.rar"));
		}
	}
}

