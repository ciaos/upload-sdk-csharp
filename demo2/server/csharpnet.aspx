<%@ Import Namespace="UploadLibrary" %>
<%@ Page Language="C#" AutoEventWireup="true" %>

<html>
<head>
    <title></title>
</head>
<body>
    <div>
    <%
        try
        {
            HuaweiDbankCloud HDC = new HuaweiDbankCloud("xxxxx", "xxxxxxxxxxxxxxxxxxxxxxxxx", "xxx");
            string host = "", key = "", ts= "";
            HDC.GetHostAndKeyAndTs(Request.ServerVariables["REMOTE_ADDR"], ref host, ref key, ref ts);
            Response.Write(host+ " " + key + " " + ts);
        }
        catch (Exception)
        {
            Response.Write("error");
        }
    %>
    </div>
</body>
</html>
