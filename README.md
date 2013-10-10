Upload SDK CSharp
=====================
封装了华为存储平台上传接口
* * *

应用场景
-----------
> 1. 想将文件上传到一个专业云存储平台
> 2. 对公司的数据文档自动备份到云端，防止丢失
> 3. 实现实时共享，上传一张照片/视频，把url告诉小伙伴们就可以直接下载

> 4. 又或者想让小伙伴们能有个云存储空间，告诉他们一个上传IP，临时密钥（不要泄漏自己的密钥），他们可以用来上传分享文件了

使用方法
----------

*	应用场景1/2/3
<pre><code>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UploadLibrary;//记着引用这个类库
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
</code></pre>

*   应用场景4（服务器端，自己使用）
<pre><code>
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text;
using System.IO;
using UploadLibrary;//记着引用这个类库
public partial class Getinfo : System.Web.UI.Page
{
	#region 配置部分

	//平台申请的APPID与APPSECRET和APPNAME
	//
	private string APPID		= ;
	private string APPSECRET	= ;
	private string APPNAME	  = ;

	#endregion

	#region 获取上传IP，密钥以及时间戳
	protected void Page_Load(object sender, EventArgs e)
	{
		try
		{
			HuaweiDbankCloud HDC = new HuaweiDbankCloud(APPID, APPSECRET, APPNAME);
			string host = "", key = "", ts= "";
			HDC.GetHostAndKeyAndTs(Request.ServerVariables["REMOTE_ADDR"], ref host, ref key, ref ts);
			//把上传IP和密钥告诉别人
			Response.Write(host+ " " + key + " " + ts);
		}
		catch (Exception)
		{
			Response.Write("error");
		}
	}
	#endregion
}
</code></pre>

*   应用场景4（客户端，小伙伴们使用）
<pre><code>
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UploadLibrary;//记着引用这个类库
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
</code></pre>

Weibo Account
-------------

Have a question? [@littley](http://weibo.com/littley)

2013/10/10 14:39:16 

