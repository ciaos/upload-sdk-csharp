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
			//场景二，客户端获取服务器信息后直接上传文件到存储平台
			
			//步骤一：访问服务器http://demo/Getinfo.aspx获取上传IP，临时密钥，时间戳信息
			...
			
			//步骤二：上传文件
			HuaweiDbankCloud HDCClient = new HuaweiDbankCloud();
			Console.WriteLine(HDCClient.ClientUpload(host, key, ts, "/dl/"+APPNAME+"/chat.rar", "E://chat.rar"));
		}
	}
}

