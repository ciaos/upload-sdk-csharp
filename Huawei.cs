using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Net;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace UploadLibrary
{
    #region SDK工具类
    public class HuaweiDbankCloud
    {
        #region 私有属性
        private String _AppID, _AppSecret, _AppName;
        private String _UploadHost;

        protected const String _RestURI = "http://api.dbank.com/rest.php";
        #endregion

        #region 构造函数
        /**
         * appId,appSecret,appName为存储平台分配的应用ID，应用密钥，应用名称
         * 注：场景一
         */
        public HuaweiDbankCloud(String appId, String appSecret, String appName)
        {
            this._AppID = appId;
            this._AppSecret = appSecret;
            this._AppName = appName;

            this._UploadHost = null;
        }
        /*
         * 注：场景二（客户端）
         */
        public HuaweiDbankCloud()
        {
            this._AppID = "";
            this._AppSecret = "";
            this._AppName = "";

            this._UploadHost = null;
        }
        #endregion

        private static readonly object fsLock = new object();

        #region 公共方法
        /**
         * 场景一
         * 
         * 功能：从服务器或客户端直接请求上传主机地址并上传文件
         * Uri:             "/dl/$_AppName/abc/test.dat"
         * LocalFile:       "C:\abc\test.dat"
         * CallbackURL:     "http://mydomain.com/callback.php"
         * CallbackStatus:  "Tag:from dbank storage"
         */
        public bool Upload(String Uri, String LocalFile)
        {
            if (_UploadHost == null)
            {
                if ((_UploadHost = GetUploadHost(null)) == null) { return false; }
            }
            _UploadHost = "14.17.110.140";
            return UploadJob(Uri, LocalFile, null, null, null);
        }
        public bool Upload(String Uri, String LocalFile, String CallbackURL, String CallbackStatus)
        {
            if (_UploadHost == null)
            {
                if ((_UploadHost = GetUploadHost(null)) == null) { return false; }
            }

            return UploadJob(Uri, LocalFile, null, CallbackURL, CallbackStatus);
        }

        /**
         * 场景二 ~ 第三方应用服务器端
         * 
         * 功能：获取客户端IP对应的上传IP,并项客户端返回临时密钥
         * ClientIp:        客户端IP
         * Host:            存放上传主机IP
         * Key:             获取上传临时密钥
         */
        public bool GetHostAndKeyAndTs(String ClientIp, ref String Host, ref String Key, ref String Ts)
        {
            if ((Host = GetUploadHost(ClientIp)) == null) { return false; }

            DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1, 0, 0, 0, 0));
            DateTime nowTime = DateTime.Now;
            long unixTime = (long)Math.Round((nowTime - startTime).TotalSeconds, MidpointRounding.AwayFromZero);
            Ts = unixTime.ToString();
            Key = bash64_hash_hmac(Ts, this._AppSecret, false);

            return true;
        }

        /**
         * 场景二 ~ 第三方应用客户端
         * 
         * 功能：上传文件到存储平台
         * Host:            从第三方应用服务器获取到的上传主机IP
         * Uri:             "/dl/$_AppName/abc/test.dat"
         * LocalFile:       "C:\abc\test.dat"
         */
        public bool ClientUpload(String Host, String TempSecret, String Ts, String Uri, String LocalFile)
        {
            _UploadHost = Host;
            _AppSecret = TempSecret;
            return UploadJob(Uri, LocalFile, Ts, null, null);
        }
        public bool ClientUpload(String Host, String TempSecret, String Ts, String Uri, String LocalFile, String CallbackURL, String CallbackStatus)
        {
            _UploadHost = Host;
            _AppSecret = TempSecret;
            return UploadJob(Uri, LocalFile, Ts, CallbackURL, CallbackStatus);
        }

        #endregion

        #region 保护方法
        //获取上传主机地址
        protected String GetUploadHost(String ClientIp)
        {
            SortedDictionary<String, String> dt = new SortedDictionary<String, String>();
            DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1, 0, 0, 0, 0));
            DateTime nowTime = DateTime.Now;
            long unixTime = (long)Math.Round((nowTime - startTime).TotalSeconds, MidpointRounding.AwayFromZero);
            dt.Add("nsp_app", this._AppID);
            dt.Add("nsp_fmt", "JSON");
            dt.Add("nsp_ver", "1.0");
            dt.Add("nsp_svc", "nsp.ping.getupsrvip");
            dt.Add("nsp_ts", unixTime.ToString());
            if (ClientIp != null)
            {
                dt.Add("client_ip", ClientIp);
            }
            string postData = getPostData(this._AppSecret, dt);
            String res = null;
            try
            {
                res = HuaweiDbankCloudHelper._Request(_RestURI, postData);
            }
            catch (Exception ex)
            {
                NSPLog.log(LogMsgType.NSP_LOG_ERR, "获取上传ip错误 " + ex.ToString());
                return null;
            }

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            Dictionary<string, object> json = (Dictionary<string, object>)serializer.DeserializeObject(res);
            object uploadhost = null;
            try
            {
                json.TryGetValue("ip", out uploadhost);
                if (uploadhost == null)
                {
                    NSPLog.log(LogMsgType.NSP_LOG_ERR, "解析上传ip错误 " + res);
                    return null;
                }
                NSPLog.log(LogMsgType.NSP_LOG_NOTICE, "获取到上传IP " + uploadhost.ToString());
                return uploadhost.ToString();
            }
            catch (Exception)
            {
                NSPLog.log(LogMsgType.NSP_LOG_ERR, "TryGetValue上传ip错误 " + res);
                return null;
            }
        }

        //上传
        protected bool UploadJob(String Uri, String LocalFile, String Ts, String CallbackURL, String CallbackStatus)
        {
            int trytimes = 3;//上传尝试3次

            string filename = Path.GetFileName(LocalFile);
            FileInfo fileinfo = new FileInfo(LocalFile);
            SortedDictionary<String, String> dt = new SortedDictionary<String, String>();
            try
            {
                dt.Add("nsp-file-size", fileinfo.Length.ToString());
            }
            catch (Exception)
            {
                NSPLog.log(LogMsgType.NSP_LOG_ERR, "上传文件有错");
                return false;
            }
            if (CallbackURL != null && CallbackStatus != null)
            {
                dt.Add("nsp-callback", CallbackURL);
                dt.Add("nsp-callback-status", CallbackStatus);
            }
            if (Ts != null)
            {
                dt.Add("nsp-ts", Ts);
            }

            String md5 = HuaweiDbankCloudTool.GetMD5Hash(LocalFile).ToLower();
            dt.Add("nsp-file-md5", md5);

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            //计算文件分片md5
            FileStream aFile = File.OpenRead(LocalFile);
            long start, end;
            String[] md5Arr = new String[2];
            for (int i = 1; i <= 2; i++) {
                
                start = ((long)CRC32Cls.GetCRC32Str(md5 + i.ToString())) % fileinfo.Length;
                end = (start + 1024 * 1024 > fileinfo.Length -1) ? fileinfo.Length - 1 : start + 1024 * 1024;
                byte[] tData = new byte[end - start + 1];
                lock (fsLock)
                {
                    aFile.Seek(start, SeekOrigin.Begin);
                    aFile.Read(tData, 0, (int)(end - start + 1));
                }
                MD5 md5Hasher = MD5.Create();
                byte[] data = md5Hasher.ComputeHash(tData);
                sbyte[] sData = (sbyte[])(Array)data;
                StringBuilder sBuilder = new StringBuilder();
                for (int j = 0; j < sData.Length; j++)
                {
                    sBuilder.Append(sData[j].ToString("x2"));
                }
                String aaa = sBuilder.ToString();
                md5Arr[i - 1] = sBuilder.ToString();
            }
            aFile.Close();
            dt.Add("nsp-content-md5",serializer.Serialize(md5Arr));
            return false;
            //上传初始化操作
            string signatureString = getSignatureString("PUT", Uri + "?init", dt);
            string nsp_sig = bash64_hash_hmac(signatureString, _AppSecret, true);

            dt["nsp-sig"] = nsp_sig;
            Dictionary<String,String>Ret = HuaweiDbankCloudHelper._RequestUpload("http://" + this._UploadHost + Uri + "?init", LocalFile, dt, UploadState.INIT);
            //上传操作
            Dictionary<string, object> json;
            object upload_status = null;
            while(trytimes -- > 0){
                if(Ret["StatusCode"] == HttpStatusCode.OK.ToString())
                { //飞速上传成功
                    return true;
                }
                else if(Ret["StatusCode"] == HttpStatusCode.Created.ToString())
                {
                    json = (Dictionary<string, object>)serializer.DeserializeObject(Ret["Body"]);
                    try
                    {
                        json.TryGetValue("upload_status", out upload_status);
                        if (upload_status == null)
                        {
                            NSPLog.log(LogMsgType.NSP_LOG_ERR, "init上传返回内容错误 " + Ret["Body"]);
                            break;
                        }
                    }
                    catch (Exception)
                    {
                        NSPLog.log(LogMsgType.NSP_LOG_ERR, "init上传返回内容错误 " + Ret["Body"]);
                        break;
                    }
                    //upload range
                }
                else if (Ret["StatusCode"] == HttpStatusCode.Unauthorized.ToString()) { 
                    break;
                }
                else //其它异常时，寻找断点
                {
                    dt.Remove("nsp-sig");
                    dt.Remove("nsp-content-md5");
                    dt.Remove("nsp-content-range");
                    signatureString = getSignatureString("PUT", Uri + "?resume", dt);
                    nsp_sig = bash64_hash_hmac(signatureString, _AppSecret, true);
                    dt["nsp-sig"] = nsp_sig;
                    Ret = HuaweiDbankCloudHelper._RequestUpload("http://" + this._UploadHost + Uri + "?resume", LocalFile, dt, UploadState.RESUME);

                    continue;
                }

                dt.Remove("nsp-sig");
                dt.Remove("nsp-content-md5");
                dt.Remove("nsp-content-range");
                if ((int)upload_status == 1) //上传指定分片
                {
                    object[] range = (object[])((object[])json["completed_range"])[0];
                    long s = long.Parse(range[0].ToString());
                    if (s == 0)
                    {
                        s = long.Parse(range[1].ToString());
                        dt.Add("nsp-content-range", (s + 1).ToString() + "-" + (fileinfo.Length - 1).ToString() + "/" + fileinfo.Length.ToString());
                    }
                }

                signatureString = getSignatureString("PUT", Uri, dt);
                dt["nsp-sig"] = bash64_hash_hmac(signatureString, _AppSecret, true);
                //上传文件
                Ret = HuaweiDbankCloudHelper._RequestUpload("http://" + this._UploadHost + Uri, LocalFile, dt, UploadState.ACTION);
            }
            return false;
        }
        #endregion

        #region 私有方法
        /**
         * 计算字符串MD5值
         */
        private string getMd5Hash(string input)
        {
            MD5 md5Hasher = MD5.Create();

            byte[] data = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(input));

            StringBuilder sBuilder = new StringBuilder();

            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }
        /**
          * 计算post内容
          */
        private string getPostData(string secret, SortedDictionary<string, string> dics)
        {
            String data = "";
            String md5str = new String(secret.ToCharArray());
            HttpUtility coder = new HttpUtility();

            foreach (KeyValuePair<string, string> kv in dics)
            {
                string k = kv.Key.ToString();
                string v = kv.Value.ToString();

                byte[] unicodebytes = Encoding.GetEncoding("gb2312").GetBytes(v);
                byte[] asciibytes = Encoding.Convert(Encoding.GetEncoding("gb2312"), Encoding.UTF8, unicodebytes);

                try
                {
                    data += Encoding.UTF8.GetString(Encoding.GetEncoding("gb2312").GetBytes(k.ToCharArray())) + "=";
                    data += HttpUtility.UrlEncode(v, Encoding.UTF8) + "&";
                }
                catch (Exception err)
                {
                    throw err;
                }

                md5str += k;
                md5str += v;
            }
            string key = getMd5Hash(md5str).ToUpper();
            data += "nsp_key=" + key;
            return data;
        }

        private String getNSPKey(string secret, SortedDictionary<string, string> dics)
        {
            String md5str = new String(secret.ToCharArray());

            foreach (KeyValuePair<string, string> kv in dics)
            {
                md5str += kv.Key.ToString();
                md5str += kv.Value.ToString();
            }
            string key = getMd5Hash(md5str).ToUpper();
            return key;
        }


        private string getSignatureString(String Method, String Uri, SortedDictionary<String, String> dt)
        {
            string signatureString = Method + "&" + HttpUtility.UrlEncode(Uri, Encoding.UTF8);

            string headParams = "";
            foreach (KeyValuePair<string, string> kv in dt)
            {
                string k = kv.Key.ToString();
                string v = kv.Value.ToString();
                if (headParams == "")
                {
                    headParams = k + "=" + v;
                }
                else
                {
                    headParams += "&" + k + "=" + v;
                }
            }
            if (headParams != "")
            {
                signatureString += "&" + HttpUtility.UrlEncode(headParams, Encoding.UTF8);
            }

            return Regex.Replace(signatureString, "(%[0-9a-f][0-9a-f])", c => c.Value.ToUpper());
        }

        private String bash64_hash_hmac(string signatureString, string secretKey, bool raw_output)
        {
            var enc = Encoding.ASCII;
            HMACSHA1 hmac = new HMACSHA1(enc.GetBytes(secretKey));
            hmac.Initialize();

            byte[] buffer = enc.GetBytes(signatureString);
            if (raw_output)
            {
                return System.Convert.ToBase64String(hmac.ComputeHash(buffer));
            }
            else
            {
                return BitConverter.ToString(hmac.ComputeHash(buffer)).Replace("-", "").ToLower();
            }
        }

        #endregion
    }
    #endregion
    //上传状态
    enum UploadState
    {
        INIT = 0,   //飞速上传，或者检查文件断点位置
        RESUME = 1, //检查断点位置
        ACTION = 2  //上传操作
    }

    #region 错误日志工具类
    enum LogMsgType
    {
        NSP_LOG_ERR = 0,
        NSP_LOG_NOTICE = 1,
        NSP_LOG_CLOSE = 2
    }
    class NSPLog
    {
        //日志文件名
        static string logname = "nsp_sdk.log";

        static LogMsgType loglevel = LogMsgType.NSP_LOG_ERR;

        #region 记录日志
        public static void log(LogMsgType logtype, string logmsg)
        {
            if (logtype < loglevel){return;}
            try
            {
                DateTime nowTime = DateTime.Now;

                FileStream fs = new FileStream(logname, FileMode.Append | FileMode.OpenOrCreate);
                StreamWriter sw = new StreamWriter(fs);

                string type = null;

                if (logtype.Equals(LogMsgType.NSP_LOG_ERR))
                {
                    type = "[error]";
                }
                else if (logtype.Equals(LogMsgType.NSP_LOG_NOTICE))
                {
                    type = "[notice]";
                }
                else
                {
                    type = "[unknown]";
                }

                sw.WriteLine(nowTime.ToString() + " # " + type + " # " + logmsg);

                sw.Flush();
                sw.Close();
                fs.Close();
            }
            catch (Exception)
            {
                throw new Exception("记录log日志出错");
            }
        }
        #endregion
    }
    #endregion

    #region 网络请求工具类
    class HuaweiDbankCloudHelper {
        public static String _Request(string httpUrl, string postData)
        {
            string Result;
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(httpUrl);
                req.Method = "POST";
                req.ContentType = "application/x-www-form-urlencoded";
                req.Headers.Add("Accept-Encoding", "gzip");
                Stream postStream = req.GetRequestStream();
                byte[] byteArray = Encoding.UTF8.GetBytes(postData);
                postStream.Write(byteArray, 0, byteArray.Length);
                
                HttpWebResponse res = (HttpWebResponse)req.GetResponse();

                NSPLog.log(LogMsgType.NSP_LOG_NOTICE, "发起请求 " + httpUrl + "?" + postData);

                Stream smRes = res.GetResponseStream();
                {
                    if ("gzip" == res.ContentEncoding.ToLower())
                    {
                        smRes = new GZipStream(smRes, CompressionMode.Decompress);
                    }
                    StreamReader sr = new StreamReader(smRes, System.Text.Encoding.UTF8);
                    Result = sr.ReadToEnd();
                    sr.Close();

                    NSPLog.log(LogMsgType.NSP_LOG_NOTICE, "获取响应 " + Result);
                }
                if (res != null)
                {
                    res.Close();
                }
                if (smRes != null)
                {
                    smRes.Close();
                }
                if (res.StatusCode != HttpStatusCode.OK)
                {
                    NSPLog.log(LogMsgType.NSP_LOG_ERR, "响应状态码 " + res.StatusCode);
                    throw new WebException();
                }
            }
            catch (Exception ex) {
                NSPLog.log(LogMsgType.NSP_LOG_ERR, "请求网络错误");
                return ex.ToString();
            }
            return Result;
        }

        public static Dictionary<String,String> _RequestUpload(String uploadUrl, String localFile, SortedDictionary<String, String> headers, UploadState state)
        {
            Dictionary<String,String> Ret = new Dictionary<String,String>();
            string Result;
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(uploadUrl);
                req.Method = "PUT";
                req.ContentType = "application/x-www-form-urlencoded";
                Stream postStream = req.GetRequestStream();

                foreach (KeyValuePair<string, string> kv in headers)
                {
                    req.Headers.Add(kv.Key.ToString(), kv.Value.ToString());
                }
                req.UserAgent=".Net SDK/1.0.0";

                if (state == UploadState.ACTION)
                {
                    FileStream fileStream;
                    try
                    {
                        fileStream = new FileStream(localFile, FileMode.Open, FileAccess.Read);
                    }
                    catch (Exception)
                    {
                        NSPLog.log(LogMsgType.NSP_LOG_ERR, "无法打开文件 " + localFile);
                        return null;
                    }

                    long pos = 0;
                    if(headers.ContainsKey("nsp-content-range")){
                        pos = long.Parse(headers["nsp-content-range"].Split('-')[0]);
                    }

                    if (pos > 0) {
                        fileStream.Seek(pos, SeekOrigin.Begin);
                    }

                    byte[] buffer = new Byte[checked((uint)Math.Min(4096, (int)fileStream.Length))];
                    int bytesRead = 0;
                    while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        postStream.Write(buffer, 0, bytesRead);
                    }
                }
                NSPLog.log(LogMsgType.NSP_LOG_NOTICE, "发起请求 " + uploadUrl);

                HttpWebResponse res = (HttpWebResponse)req.GetResponse();

                Stream smRes = res.GetResponseStream();
                {
                    StreamReader sr = new StreamReader(smRes, System.Text.Encoding.UTF8);
                    Result = sr.ReadToEnd();
                    sr.Close();

                    NSPLog.log(LogMsgType.NSP_LOG_NOTICE, "获取响应 " + Result);
                }
                if (res != null)
                {
                    res.Close();
                }
                if (smRes != null)
                {
                    smRes.Close();
                }
                if (res.StatusCode != HttpStatusCode.OK)
                {
                    NSPLog.log(LogMsgType.NSP_LOG_ERR, "响应状态码 " + res.StatusCode);
                }
                Ret.Add("StatusCode", res.StatusCode.ToString());
                Ret.Add("Body", Result);
            }
            catch (Exception ex)
            {
                NSPLog.log(LogMsgType.NSP_LOG_ERR, "请求网络错误");
                Ret.Add("StatusCode","0");
                Ret.Add("Body","请求网络错误 " + ex.ToString());
                return Ret;
            }
            
            return Ret;
        }
    }
    #endregion

    #region 其他工具类
    class HuaweiDbankCloudTool {
        public static string GetMD5Hash(string pathName)
        {
            string ret = "";
            string strHashData = "";

            byte[] bytes;
            FileStream fs = null;

            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();

            try
            {
                fs = new FileStream(pathName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                bytes = md5.ComputeHash(fs);
                fs.Close();
                strHashData = System.BitConverter.ToString(bytes);

                strHashData = strHashData.Replace("-", "");
                ret = strHashData;
            }
            catch (System.Exception ex)
            {
                NSPLog.log(LogMsgType.NSP_LOG_ERR, "计算md5错误 " + ex.Message);
            }
            return ret;
        }
    }

    class CRC32Cls
    {
        static protected ulong[] Crc32Table;
        //生成CRC32码表
        static public void GetCRC32Table()
        {
            ulong Crc;
            Crc32Table = new ulong[256];
            int i, j;
            for (i = 0; i < 256; i++)
            {
                Crc = (ulong)i;
                for (j = 8; j > 0; j--)
                {
                    if ((Crc & 1) == 1)
                        Crc = (Crc >> 1) ^ 0xEDB88320;
                    else
                        Crc >>= 1;
                }
                Crc32Table[i] = Crc;
            }
        }
        //获取字符串的CRC32校验值
        static public ulong GetCRC32Str(string sInputString)
        {
            //生成码表
            GetCRC32Table();
            byte[] buffer = System.Text.ASCIIEncoding.ASCII.GetBytes(sInputString); ulong value = 0xffffffff;

            int len = buffer.Length;
            for (int i = 0; i < len; i++)
            {
                value = (value >> 8) ^ Crc32Table[(value & 0xFF) ^ buffer[i]];
            }
            return value ^ 0xffffffff;
        }
    }
    #endregion
}

