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
using Newtonsoft.Json;

namespace DPB
{
    public class LetsGo
    {
        public Manifest Manifest { get; set; }

        const string BEGIN_MARK_PERFIX = "PDBMARK ";
        const string END_MARK = "PDBMARK_END";
        const string FILE_MARK_PREFIX = "PDBMARK_FILE ";

        /// <summary>
        /// Find nodes from xml by tag name and relpace with specified value
        /// </summary>
        /// <param name="parentNode"></param>
        /// <param name="tagName"></param>
        /// <param name="result"></param>
        private void ReplaceXmlElements(XElement parentNode, XmlContent xmlContent)
        {
            if (parentNode == null)
            {
                return;
            }

            foreach (var item in parentNode.Elements())
            {
                if (item.Name == xmlContent.TagName)
                {
                    item.Value = xmlContent.ReplaceContent;
                }
                else
                {
                    ReplaceXmlElements(item, xmlContent);
                }
            }
        }

        /// <summary>
        /// Find nodes from json by key name and relpace with specified value
        /// </summary>
        /// <param name="node"></param>
        /// <param name="jsonContent"></param>
        private void ReplaceJsonNodes(IDictionary<string, object> node, JsonContent jsonContent)
        {
            if (node == null)
            {
                return;
            }

            var keys = node.Keys.ToArray();

            for (int i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                if (key == jsonContent.KeyName)
                {
                    node[key] = jsonContent.ReplaceContent;
                }
                else if (node[key] is IDictionary<string, object>)
                {
                    ReplaceJsonNodes(node[key] as IDictionary<string, object>, jsonContent);
                }
            }
        }

        /// <summary>
        /// LetsGo
        /// </summary>
        /// <param name="manifest">manifest entity</param>
        public LetsGo(Manifest manifest)
        {
            Manifest = manifest;
        }

        /// <summary>
        /// LetsGo
        /// </summary>
        /// <param name="manifestJson">manifest json file</param>
        public LetsGo(string manifestJson)
        {
            Manifest = SerializerHelper.GetObject<Manifest>(manifestJson);
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
                                    ReplaceXmlElements(xml.Root, replaceContent.XmlContent);
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
                                    json = serializer.Deserialize(fileContent, typeof(object));
                                    ReplaceJsonNodes(json, replaceContent.JsonContent);
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
                            fileContent = JsonConvert.SerializeObject(json);
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
