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

        public List<string> Records { get; set; } = new List<string>();

        /// <summary>
        /// Find nodes from xml by tag name and relpace with specified value
        /// </summary>
        /// <param name="parentNode"></param>
        /// <param name="xmlContent"></param>
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
                    Record($"xml node <{xmlContent.TagName}> changed vale from [{item.Value}] to [{xmlContent.ReplaceContent}]");
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
                    Record($"json node <{jsonContent.KeyName}> changed vale from [{node[key]}] to [{jsonContent.ReplaceContent}]");
                    node[key] = jsonContent.ReplaceContent;
                }
                else if (node[key] is IDictionary<string, object>)
                {
                    ReplaceJsonNodes(node[key] as IDictionary<string, object>, jsonContent);
                }
            }
        }

        /// <summary>
        /// Copy all files in the sourceDir to outputDir
        /// </summary>
        /// <param name="sourceDir"></param>
        /// <param name="outputDir"></param>
        public void CopyDirectory(string sourceDir, string outputDir)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(sourceDir);
                FileSystemInfo[] fileinfoArr = dir.GetFileSystemInfos();
                foreach (FileSystemInfo fileInfo in fileinfoArr)
                {
                    if (fileInfo is DirectoryInfo)
                    {
                        if (!Directory.Exists(Path.Combine(outputDir, fileInfo.Name)))
                        {
                            Directory.CreateDirectory(Path.Combine(outputDir, fileInfo.Name));
                        }
                        CopyDirectory(fileInfo.FullName, Path.Combine(outputDir, fileInfo.Name));
                    }
                    else
                    {
                        var newFile = Path.Combine(outputDir, fileInfo.Name);
                        File.Copy(fileInfo.FullName, newFile, true);
                        Record($"file copy from {fileInfo.FullName} to {newFile}");
                    }
                }
            }
            catch (Exception ex)
            {
                Record($"file copy error: {ex}");
            }
        }

        /// <summary>
        /// Record logs
        /// </summary>
        /// <param name="message"></param>
        private void Record(string message)
        {
            Records.Add($"{DateTime.Now.ToString()}\t{message}");
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

        /// <summary>
        /// Build a new project from source
        /// </summary>
        /// <param name="cleanOutputDir"></param>
        public void Build(bool cleanOutputDir = true)
        {
            var startTime = DateTime.Now;
            Records.Clear();
            Record($"---- DBP Build begin at {startTime.ToString()}  ----");

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

            //Copy all files
            Record($"===== start copy all files  =====");
            CopyDirectory(fullSourceRoot, fullOutputRoot);
            Record($"---- end copy all files  ----");
            Record($"---- {(DateTime.Now-startTime).TotalSeconds} seconds  ----");


            int groupIndex = 0;
            foreach (var configGroup in Manifest.ConfigGroup)
            {
                groupIndex++;

                Record($"config group: {groupIndex}");

                var files = configGroup.Files.SelectMany(f => Directory.GetFiles(fullOutputRoot, f, SearchOption.AllDirectories)).ToList();
                foreach (var file in files)
                {
                    Record($"file: {file}");

                    var newContent = new StringBuilder();
                    using (var fs = new FileStream(file, FileMode.Open))
                    {
                        var sr = new StreamReader(fs, Encoding.UTF8);

                        var fileContent = sr.ReadToEnd();


                        XDocument xml = null;
                        dynamic json = null;
                        foreach (var replaceContent in configGroup.ReplaceContents)
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
                                    Record($"Xml file format wrong");
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
                                    Record($"Json file format wrong");
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
                            if (match.Success && !configGroup.KeepFileConiditions.Any(z => z == match.Groups["kw"].Value))
                            {
                                //remove this file
                                Record($"remove this file");
                                try
                                {
                                    File.Delete(file);
                                }
                                catch (Exception ex)
                                {
                                    Record($"delete file error:{ex}");
                                }
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
                                        if (!configGroup.KeepContentConiditions.Any(z => line.Contains(z)))
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

                      
                    }

                    #region save new file

                    //save the file to OutputDir
                    using (var fs = new FileStream(file, FileMode.Truncate))
                    {
                        var sw = new StreamWriter(fs, Encoding.UTF8);
                        sw.Write(newContent.ToString());
                        sw.Flush();
                        fs.Flush(true);
                        Record($"modified and saved a new file: {file}");
                    }

                    #endregion
                }
            }

            var logFileName = Path.Combine(fullOutputRoot, "DPB.log");
            Record($"saved new file: {logFileName}");
            Record($"===== DPB Build Finished =====");
            Record($"---- Total time: {(DateTime.Now - startTime).TotalSeconds} seconds ----");

            using (var logFs = new FileStream(logFileName, FileMode.Create))
            {
                var sw = new StreamWriter(logFs, Encoding.UTF8);
                var logs = new StringBuilder();
                Records.ForEach(z => logs.AppendLine(z));
                sw.Write(logs.ToString());
                sw.Flush();
                logFs.Flush(true);
            }
        }
    }
}
