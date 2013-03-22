using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Lina.BlogEngine
{
    struct resinfo
    {
        /// <summary>
        /// 完整的原始路径
        /// </summary>
        public string orgurl;
        /// <summary>
        /// 原始文件名
        /// </summary>
        public string orgname;
        /// <summary>
        /// 原始的扩展文件名
        /// </summary>
        public string extname;
        /// <summary>
        /// 新文件名
        /// </summary>
        public string newname;
    }
    /// <summary>
    /// 获取（网页）内容中的远程资源
    /// </summary>
    public class RemoteResource
    {
        private int SeriesNum;
        private string FileNum;
        private string restype = ".gif|.jpg|.bmp|.png|.jpeg";
        private string _remoteurl;
        private string _localurl;
        private string _localpath;
        private string _content = "";
        private bool _rename;
        private bool bcomp = false;
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="Content">包含要获取远程资源的内容</param>
        /// <param name="LocalURLDirectory">要将文件保存到本地服务器的虚拟目录，用于替换原来的远程链接地址，如：http://www.xuey.com/remoteres,可以为空,也可以为../一个或多个。</param>
        /// <param name="LocalPhysicalDirectory">要将文件保存到本地服务器的磁盘路径，如：C:\Inetpub\wwwroot\remoteres,如果不存在可以创建</param>
        /// <param name="RemoteUrl">用于处理相对路径（如src="../images/xuey.gif"）的资源，如果为空，则只取完整路径的资源，以http(或https,ftp,rtsp,mms)://开头</param>
        /// <param name="RenameFile">是否要重命名资源文件，如为false则自动覆盖重名文件</param>
        public RemoteResource(string Content, string LocalURLDirectory, string LocalPhysicalDirectory, string RemoteUrl, bool RenameFile)
        {
            _content = Content;
            _localurl = LocalURLDirectory.Trim();
            _localpath = LocalPhysicalDirectory.Trim();
            if (RemoteUrl == null)
                _remoteurl = "";
            else
                _remoteurl = RemoteUrl.Trim();
            if (_remoteurl.Equals(""))
                bcomp = true;
            if (_localpath.Equals(""))
                throw new NullReferenceException("本地的物理路径不能为空!");
            _rename = RenameFile;
            SeriesNum = 1;
            FileNum = StringHelpher.CreateVildateString(6);
            _localpath = _localpath.Replace("/", "\\");
            _localurl = _localurl.Replace("\\", "/");
            _remoteurl = _remoteurl.Replace("\\", "/");
            _localpath = _localpath.TrimEnd('\\');
            _localurl = _localurl.TrimEnd('/');
            if (!Directory.Exists(_localpath))
                Directory.CreateDirectory(_localpath);
        }
        /// <summary>
        /// 要获取的资源文件扩展名，扩展名不要加点(.)，如{"gif","jpg","png"},默认的下载文件有gif,jpg,bmp,png
        /// </summary>
        public string[] FileExtends
        {
            set
            {
                restype = "";
                string[] flexs = value;
                for (int i = 0; i < flexs.Length; i++)
                {
                    if (i > 0)
                        restype += "|";
                    restype += "." + flexs[i].TrimStart('.');
                }
            }
        }
        /// <summary>
        /// 获取远程资源的路径
        /// </summary>
        private IList<resinfo> ObtainResURL()
        {
            IList<resinfo> list = new List<resinfo>();
            string pattern = "src\\s?=\\s?['\"]?(?<resurl>.+?(" + restype.Replace(".", "\\.") + "))";
            //string pattern = "[=\\(]['\"\\ ]??(?<resurl>[^<>\"]+?(" + restype.Replace(".","\\.") + "))";
            if (bcomp)
                pattern = @"(http|https|ftp|rtsp|mms)://\S+(" + restype.Replace(".", "\\.") + ")";
            Regex reg = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            Match m = reg.Match(_content);
            while (m.Success)
            {
                string url = "";
                if (bcomp)
                {
                    url = m.Value;
                }
                else
                {
                    url = m.Groups["resurl"].Value;
                }
                bool bsame = false;
                foreach (resinfo res in list)
                {
                    if (res.orgurl.Equals(url))
                    {
                        bsame = true;
                        break;
                    }
                }
                if (!bsame)
                {
                    #region 加入资源列表
                    string name = "";
                    string curl = url.Replace("\\", "/").Trim();
                    if (curl.IndexOf("/") >= 0)
                    {
                        name = curl.Substring(curl.LastIndexOf("/") + 1);
                    }
                    else
                    {
                        name = url;
                    }
                    int pos = name.LastIndexOf(".");
                    resinfo r;
                    r.orgurl = url;
                    r.orgname = name.Substring(0, pos);
                    r.extname = name.Substring(pos + 1);
                    //如果图片没有后缀则默认一个后缀
                    if (r.extname == null || r.extname.Equals(""))
                    {
                        r.extname = "gif";
                    }
                    r.newname = "";
                    list.Add(r);
                    #endregion 加入资源列表
                }
                m = m.NextMatch();
            }
            return list;
        }
        /// <summary>
        /// 保存远程图片并替换原文内容
        /// </summary>
        public void FetchResource()
        {
            IList<resinfo> list = ObtainResURL();
            if (!_localurl.Equals(""))
                _localurl += "/";
            WebClient client = new WebClient();
            foreach (resinfo r in list)
            {
               
                string url = r.orgurl;
                string newurl = "", newpath = "";
                Uri newUri = new Uri(url);
                if (_rename)
                {
                    #region 生成新文件名
                    string newname = FileNum + SeriesNum.ToString().PadLeft(3, '0') + "." + r.extname;
                    while (File.Exists(_localpath + "\\" + newname))
                    {
                        SeriesNum++;
                        newname = FileNum + SeriesNum.ToString().PadLeft(3, '0') + "." + r.extname;
                    }
                    newpath = _localpath + "\\" + newname;
                    newurl = _localurl + newname;
                    #endregion
                }
                else
                {
                    newurl = _localurl + r.orgname + "." + r.extname;
                    newpath = _localpath + "\\" + r.orgname + "." + r.extname;
                }

                #region 替换文件名
                if (DownloadFile(client, newUri, newpath))//成功下载
                {
                    Lina.Common.UpFileInfo UpFile = new Lina.Common.UpFileInfo();
                    UpFile.FileAddr = newurl;
                    UpFile.Author = "";
                    UpFile.FileID = "";
                    UpFile.OldName = r.orgname + "." + r.extname;
                    UpFile.Locked = false;
                    UpFile.SmallPicAddr = newurl;
                    string path = DraftComp.StoreFile(UpFile, true);
                    if (path.StartsWith("~"))
                        path = path.Substring(1);
                    _content = _content.Replace(r.orgurl, path);
                }
                //DownloadFileAsTask(newUri, newpath);
                //下载失败日志记录Lina.Common.Logger.Log(string str);
                
                #endregion 替换文件名
                SeriesNum++;
            }
            //释放
            client.Dispose();
        }

        static bool DownloadFile(WebClient client, Uri address, string newPath)
        {
            try
            {
                client.DownloadFile(address, newPath);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }


        static Task<string> DownloadFileAsTask(Uri address,string newPath)
        {
            TaskCompletionSource<string> tcs =
             new TaskCompletionSource<string>();
            WebClient client = new WebClient();
            client.DownloadStringCompleted += (sender, args) =>
            {
                if (args.Error != null) tcs.SetException(args.Error);
                else if (args.Cancelled) tcs.SetCanceled();
                else tcs.SetResult(args.Result);
            };
            client.DownloadFile(address, newPath);
            return tcs.Task;
        }

        /// <summary>
        /// 获取内容
        /// </summary>
        public string Content
        {
            get { return _content; }
        }

    }
}