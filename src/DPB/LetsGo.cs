using DPB.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Senparc.CO2NET.Trace;
using Senparc.CO2NET.Helpers;
using System.Web.Script.Serialization;

namespace DPB
{
    public class LetsGo
    {
        public Manifest Manifest { get; set; }

        const string BEGIN_MARK_PERFIX = "PDBMARK ";
        const string END_MARK = "PDBMARK_END";
        const string FILE_MARK_PREFIX = "PDBMARK_FILE ";

        /// <summary>
        /// Find nodes from xml by tag name
        /// </summary>
        /// <param name="parentNode"></param>
        /// <param name="tagName"></param>
        /// <param name="result"></param>
        private void FindElements(XElement parentNode, string tagName, List<XElement> result)
        {
            foreach (var item in parentNode.Elements())
            {
                if (item.Name == tagName)
                {
                    result.Add(item);
                }
                else
                {
                    FindElements(item, tagName, result);
                }
            }
        }

        public LetsGo(Manifest manifest)
        {
            Manifest = manifest;
        }

        public void Build(bool cleanOutputDir = true)
        {
            var fullSourceRoot = Path.Combine(Directory.GetCurrentDirectory(), Manifest.SourceDir);
            var fullOutputRoot = Path.Combine(Directory.GetCurrentDirectory(), Manifest.OutputDir);

            if (cleanOutputDir)
            {
                try
                {
                    Directory.Delete(fullOutputRoot, true);
                }
                catch (Exception ex)
                {
                    SenparcTrace.BaseExceptionLog(ex);
                }
            }

            if (!Directory.Exists(fullOutputRoot))
            {
                Directory.CreateDirectory(fullOutputRoot);
            }

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


                        XDocument xml = null;
                        dynamic json = null;
                        foreach (var replaceContent in item.ReplaceContents)
                        {
                            #region Xml

                            if (replaceContent.XmlContent != null)
                            {
                                try
                                {
                                    xml = xml ?? XDocument.Parse(fileContent);
                                    var xmlNodeList = new List<XElement>();
                                    FindElements(xml.Root, replaceContent.XmlContent.TagName, xmlNodeList);
                                    xmlNodeList.ForEach(z => z.Value = replaceContent.XmlContent.ReplaceContent);
                                }
                                catch (Exception ex)
                                {
                                    SenparcTrace.SendCustomLog("Xml file format wrong", ex.Message);
                                }
                            }
                            #endregion

                            #region Json
                            else if (replaceContent.JsonContent != null)
                            {
                                try
                                {
                                    var serializer = new JavaScriptSerializer();
                                    dynamic obj = serializer.Deserialize(fileContent, typeof(object));
                                    if (obj!=null)
                                    {

                                    }
                                }
                                catch (Exception ex)
                                {
                                    SenparcTrace.SendCustomLog("Json file format wrong", ex.Message);
                                }
                            }

                            #endregion
                        }

                        if (xml != null)
                        {
                            fileContent = xml.ToString();
                        }
                        else if (json != null)
                        {
                            fileContent = json.ToJson();
                        }






                        #region File Mark

                        if (fileContent.Contains(FILE_MARK_PREFIX))
                        {
                            //judgement whether this file can keep
                            var regex = new Regex($@"{FILE_MARK_PREFIX}(?<kw>[^\r\n ,]*)");
                            var match = regex.Match(fileContent);
                            if (match.Success && !item.KeepFileConiditions.Any(z => z == match.Groups["kw"].Value))
                            {
                                //remove this file
                                continue;
                            }
                        }

                        #endregion

                        #region Content Mark

                        if (fileContent.Contains(BEGIN_MARK_PERFIX))
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
                                    if (line.Contains(BEGIN_MARK_PERFIX))
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
                        else
                        {
                            newContent.Append(fileContent);
                        }

                        #endregion

                        //save the file to OutputDir
                        var newFile = file.Replace(fullSourceRoot, fullOutputRoot);
                        if (File.Exists(newFile))
                        {
                            File.Delete(newFile);
                        }

                        var newDir = Path.GetDirectoryName(newFile);
                        if (!Directory.Exists(newDir))
                        {
                            Directory.CreateDirectory(newDir);
                        }

                        using (var nweFs = new FileStream(newFile, FileMode.Create))
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
