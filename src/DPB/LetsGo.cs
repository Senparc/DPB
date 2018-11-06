using DPB.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DPB
{
    public class LetsGo
    {
        public Manifest Manifest { get; set; }

        const string BEGIN_MARK = "PDBMARK ";
        const string END_MARK = "PDBMARKEND";

        public LetsGo(Manifest manifest)
        {
            Manifest = manifest;
        }

        public void Build()
        {

            if (!Directory.Exists(Manifest.OutputDir))
            {
                Directory.CreateDirectory(Manifest.OutputDir);
            }

            var fullSourceRoot = Path.Combine(Directory.GetCurrentDirectory(), Manifest.SourceDir);
            var fullOutputRoot = Path.Combine(Directory.GetCurrentDirectory(), Manifest.OutputDir);

            foreach (var item in Manifest.Paths)
            {
                var files = item.Files.SelectMany(f => Directory.GetFiles(fullSourceRoot, f, SearchOption.AllDirectories)).ToList();
                foreach (var file in files)
                {
                    var newContent = new StringBuilder();
                    using (var fs = new FileStream(file, FileMode.Open))
                    {
                        var sr = new StreamReader(fs, Encoding.UTF8);

                        var fileContent = sr.ReadToEnd();
                        if (fileContent.Contains(BEGIN_MARK))
                        {
                            var lines = fileContent.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
                            var keep = true;
                            var removeBlockCount = 0;
                            var i = 0;
                            foreach (var line in lines)
                            {
                                i++;
                                if (keep)
                                {
                                    if (line.Contains(BEGIN_MARK))
                                    {
                                        //begin to check Conditions
                                        if (!item.KeepContentConiditions.Any(z => line.Contains(z)))
                                        {
                                            //drop content
                                            keep = false;
                                            removeBlockCount++;
                                            continue;
                                        }
                                    }

                                    //keep
                                    newContent.Append(line);
                                    if (i != lines.Count())
                                    {
                                        newContent.Append(Environment.NewLine);   //not last Item
                                    }
                                }
                                else
                                {
                                    //not keep, waiting the end mark
                                    if (line.Contains(END_MARK))
                                    {
                                        keep = true;
                                    }
                                }
                            }
                            sr.Dispose();
                        }

                        //save the file to OutputDir
                        var newFile = file.Replace(fullSourceRoot, fullOutputRoot);
                        using (var nweFs = new FileStream(newFile, FileMode.OpenOrCreate))
                        {
                            var sw = new StreamWriter(nweFs, Encoding.UTF8);
                            sw.Write(newContent.ToString());
                            sw.Flush();
                            nweFs.Flush(true);
                        }
                    }
                }
            }
        }
    }
}
