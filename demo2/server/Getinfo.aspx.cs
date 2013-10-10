using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text;
using System.IO;
using UploadLibrary;


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
			Response.Write(host+ " " + key + " " + ts);
		}
		catch (Exception)
		{
			Response.Write("error");
		}
	}
	#endregion
}
