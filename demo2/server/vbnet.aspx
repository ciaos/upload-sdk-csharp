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
            Dim UL = New HuaweiDbankCloud("xxxxx", "xxxxxxxxxxxxxxxxxxxxxxxxx", "xxx")
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
