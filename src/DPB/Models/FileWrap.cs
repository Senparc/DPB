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
        public string FileContent { get; set; }

        /// <summary>
        /// try load FileContent while FileContent is null or empty
        /// </summary>
        public void TryLoadFileContent()
        {
            if (FileContent.IsNullOrEmpty())
            {
                using (var fs = new FileStream(SourceFilePath, FileMode.Open))
                {
                    using (var sr = new StreamReader(fs))
                    {
                        FileContent = sr.ReadToEnd();//save file content to memory cache (option)
                    }
                }
            }
        }
    }
}
