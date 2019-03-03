using Senparc.CO2NET.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DPB.Models
{
    internal class FileWrap
    {
        public string SourceFilePath { get; set; }
        public string DestFilePath { get; set; }
        public string FileContent
        {
            get
            {
                TryLoadStream();
                FileContentStream.Seek(0, SeekOrigin.Begin);
                var sr = new StreamReader(FileContentStream);
                var str = sr.ReadToEnd();//save file content to memory cache (option)
                FileContentStream.Seek(0, SeekOrigin.Begin);
                return str;
            }
            set
            {
                FileContentStream?.Dispose();
                FileContentStream = new MemoryStream();
                var sw = new StreamWriter(FileContentStream);
                sw.Write(value);
                sw.Flush();
                FileContentStream.Flush();
                FileContentStream.Seek(0, SeekOrigin.Begin);
            }
        }

        public Stream FileContentStream { get; private set; }


        /// <summary>
        /// try to load FileContentStream while FileContentStream is null or empty
        /// </summary>
        public void TryLoadStream()
        {
            if (FileContentStream == null)
            {
                FileContentStream = new MemoryStream();
                using (var fs = new FileStream(SourceFilePath, FileMode.Open))
                {
                    fs.CopyTo(FileContentStream);
                    fs.Flush();
                    FileContentStream.Flush();
                    FileContentStream.Seek(0, SeekOrigin.Begin);
                }
            }
        }
    }
}
