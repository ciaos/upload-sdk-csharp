Upload SDK .Net
=====================
华为云加速上传SDK使用指南
* * *

A . 概述
-----------
该SDK适用于.Net Framework（>=3.5），开发环境建议选择Visual Studio 2010。

B . 应用场景
-----------
> 1. 将文件直接上传到一个专业云存储平台
> 2. 对公司的数据文档自动备份到云端，防止丢失
> 3. 实现实时共享，上传一张照片/视频，把url告诉小伙伴们就可以直接下载

> 4. 又或者想让小伙伴们能有个云存储空间，告诉他们一个上传IP，临时密钥（不要泄漏自己的密钥），他们可以用来上传分享文件了

C . 使用说明
----------
### C1 . 前期准备 ###
	制作上传依赖库UploadLibrary.dll：
	1. 下载HuaweiDbankCloud.cs代码存到本地；
	2. 开始菜单->Visual Studio Tools->Visual Studio命令提示(2010)，打开此窗口；
	3. 切换到HuaweiDbankCloud.cs代码所在目录(如执行 cd c:\test\)
	4. 执行命令csc /out:UploadLibrary.dll /t:library HuaweiDbankCloud.cs生成动态链接库

### C2 . 场景1/2/3 ###
	1. 申请开发者账号，获取应用ID(APPID)，应用密钥(APPSECRET)，应用名称(APPNAME)
	2. 在项目中添加引用UploadLibrary.dll文件
	3. 初始化上传操作对象，调用Upload函数上传文件，其中第一个参数为云存储的存储全路径(前缀 /dl/APPNAME 必选)，第二个参数为本地文件路径

交互流程如下

1. 客户端(服务器)直接上传至 距自己最近速度最快的云存储服务器
2. 上传成功后客户端进行后续业务操作

![](http://zl.hwpan.com/u12134807/demo1.png)

代码示例如下（详见[demo1](https://github.com/ciaos/upload-sdk-csharp/blob/master/demo1/Program.cs)）

```csharp
using UploadLibrary;

//初始化对象，上传单个文件，成功返回true，失败返回false
HuaweiDbankCloud HDC = new HuaweiDbankCloud(APPID, APPSECRET, APPNAME);
Console.WriteLine(HDC.Upload("/dl/APPNAME/chat.rar", "E://chat.rar"));

```

### C3 . 场景4 (业务服务器端)###
	1. 申请开发者账号，获取应用ID(APPID)，应用密钥(APPSECRET)，应用名称(APPNAME)
	2. 将UploadLibrary.dll放在网站中根目录bin文件夹下（可能需要重启服务器）
	3. 初始化上传操作对象，调用GetHostAndKeyAndTs函数获取距离用户最近的上传IP，并分配给用户密钥及时间戳用于上传

交互流程如下：

1. 用户访问业务服务器
2. 业务服务器访问华为云存储接口获取距离用户最近最快的上传IP
3. 业务服务器生成临时密钥和时间戳返回给用户用于上传

![](http://zl.hwpan.com/u12134807/demo2.png)

代码示例如下（详见[demo2/server](https://github.com/ciaos/upload-sdk-csharp/blob/master/demo2/server/)）

```vb.net
<%@ Import Namespace="UploadLibrary" %>
<%@ Page Language="VB" AutoEventWireup="false" %>

<html>
<head>
    <title></title>
</head>
<body>
    <div>
    <%
        Try
            Dim UL = New HuaweiDbankCloud(APPID, APPSECRET, APPNAME)
            Dim Host, Key, Ts As String
            Host = ""
            Key  = ""
            Ts   = ""
            Dim Remote_Addr As String
            Remote_Addr = Request.ServerVariables("REMOTE_ADDR")
            UL.GetHostAndKeyAndTs(Remote_Addr, Host, Key, Ts)
            Response.Write(Host + " " + Key + " " + Ts)
        Catch ex As Exception
            Response.Write("error")
        End Try    
    %>
    </div>
</body>
</html>
```

### C4 . 场景4 (业务客户端)###
	1. 在项目中添加引用UploadLibrary.dll文件
	2. 访问业务服务器端获取上传IP，密钥等信息
	3. 初始化上传操作对象，调用Upload函数上传文件，其中第四个参数为云存储的存储全路径(前缀 /dl/APPNAME 必选)，第五个参数为本地文件路径

交互流程如下：

1. 客户端访问业务服务器获取上传IP，密钥等信息
2. 客户端使用密钥信息上传文件到云存储服务器
3. 客户端可以通过指定回调url通知业务服务器是否上传完成（可选）

代码示例如下（详见[demo2/client](https://github.com/ciaos/upload-sdk-csharp/blob/master/demo2/client/)）

```csharp
using UploadLibrary;

//步骤一：访问业务服务器获取上传IP，临时密钥，时间戳信息
//host , key , ts = http.Get("http://server/demo.aspx")

//步骤二：初始化操作对象，上传文件，传入回调url通知业务服务器上传完毕(这一步可选)
HuaweiDbankCloud HDCClient = new HuaweiDbankCloud();
Console.WriteLine(HDCClient.ClientUpload(host, key, ts, "/dl/"+APPNAME+"/chat.rar", "E://chat.rar"));
Console.WriteLine(HDCClient.ClientUpload(host, key, ts, "/dl/"+APPNAME+"/chat.rar", "E://chat.rar","http://server/callback.aspx","Upload OK"));
```

-------------

如有疑问 [@littley](http://weibo.com/littley)

2013/10/10 14:39:16 

