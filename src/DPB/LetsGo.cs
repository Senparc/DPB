using DPB.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using Senparc.CO2NET.Trace;
using Senparc.CO2NET.Helpers;
using Newtonsoft.Json;
using Senparc.CO2NET.Extensions;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;

namespace DPB
{
    public partial class LetsGo
    {
        public Manifest Manifest { get; set; }

        const string BEGIN_MARK_PERFIX = "DPBMARK ";
        const string END_MARK = "DPBMARK_END";
        const string FILE_MARK_PREFIX = "DPBMARK_FILE ";

        public List<string> Records { get; set; } = new List<string>();

        private Action<string> _recordAction = null;

        /// <summary>
        /// files memory Cache
        /// </summary>
        private Dictionary<string, FileWrap> FilesCache = new Dictionary<string, FileWrap>();

        public int AllFilesCount { get; set; }
        private int finishedFilesCount;
        public int FinishedFilesCount
        {
            get => finishedFilesCount;
            set
            {
                finishedFilesCount++;
                FinishPercentAction?.Invoke(Task.Factory.StartNew(() => AllFilesCount == 0 ? 0 : ((finishedFilesCount / AllFilesCount)) * 100));
            }
        }


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
        private void ReplaceJsonNodes(JToken node, JsonContent jsonContent)
        {
            if (node is JProperty)
            {
                var key = ((JProperty)node).Name;
                if (key == jsonContent.KeyName)
                {
                    Record($"json node <{jsonContent.KeyName}> changed vale from [{((JProperty)node).Value}] to [{jsonContent.ReplaceContent}]");
                    ((JProperty)node).Value = jsonContent.ReplaceContent;
                }

            }

            if (node.Children().Count() > 0)
            {
                for (int i = 0; i < node.Children().Count(); i++)
                {
                    ReplaceJsonNodes(node.Children().Skip(i).Take(1).First(), jsonContent);
                }
            }
        }


        ///// <summary>
        ///// Copy all files in the sourceDir to outputDir
        ///// </summary>
        ///// <param name="sourceDir"></param>
        ///// <param name="outputDir"></param>
        //public void CopyDirectory(string sourceDir, string outputDir)
        //{
        //    try
        //    {
        //        DirectoryInfo dir = new DirectoryInfo(sourceDir);
        //        FileSystemInfo[] fileinfoArr = dir.GetFileSystemInfos();

        //        Parallel.ForEach(fileinfoArr, /*new ParallelOptions() { MaxDegreeOfParallelism = 30 },*/ fileInfo =>
        //         {
        //             try
        //             {
        //                 if (fileInfo is DirectoryInfo)
        //                 {
        //                     if (!Directory.Exists(Path.Combine(outputDir, fileInfo.Name)))
        //                     {
        //                         Directory.CreateDirectory(Path.Combine(outputDir, fileInfo.Name));
        //                     }
        //                     CopyDirectory(fileInfo.FullName, Path.Combine(outputDir, fileInfo.Name));
        //                 }
        //                 else
        //                 {
        //                     var newFile = Path.Combine(outputDir, fileInfo.Name);
        //                     File.Copy(fileInfo.FullName, newFile, true);
        //                     Record($"file copy from {fileInfo.FullName} to {newFile}");
        //                 }
        //             }
        //             catch (Exception)
        //             {

        //             }


        //         });

        //        //foreach (FileSystemInfo fileInfo in fileinfoArr)
        //        //{

        //        //}
        //    }
        //    catch (Exception ex)
        //    {
        //        Record($"file copy error: {ex}");
        //    }
        //}


        /// <summary>
        /// Copy all files in the sourceDir to outputDir to memory
        /// </summary>
        /// <param name="sourceDir"></param>
        /// <param name="outputDir"></param>
        public void ScanFile(string sourceDir, string outputDir)
        {
            try
            {
                DirectoryInfo dir = new DirectoryInfo(sourceDir);
                FileSystemInfo[] fileinfoArr = dir.GetFileSystemInfos();

                foreach (var fileInfo in fileinfoArr)
                {
                    try
                    {
                        //Record($"[in memory] scan file {fileInfo.FullName}");
                        if (fileInfo is DirectoryInfo)
                        {
                            ScanFile(fileInfo.FullName, Path.Combine(outputDir, fileInfo.Name));
                        }
                        else
                        {
                            var newFile = Path.Combine(outputDir, fileInfo.Name);
                            FilesCache[fileInfo.FullName] = new FileWrap()
                            {
                                SourceFilePath = fileInfo.FullName,
                                DestFilePath = newFile,
                            };

                            Record($"[in memory] file add from {fileInfo.FullName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Record($@"[in memory] file add error {ex.Message}
{ex.StackTrace}");
                    }
                };

                //foreach (FileSystemInfo fileInfo in fileinfoArr)
                //{

                //}
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
            _recordAction?.Invoke(message);
            Console.WriteLine(message);
        }

        /// <summary>
        /// LetsGo
        /// </summary>
        /// <param name="manifest">manifest entity</param>
        /// <param name="recordAction">run after each record</param>
        public LetsGo(Manifest manifest, Action<string> recordAction = null)
        {
            Manifest = manifest;
            _recordAction = recordAction;
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
        /// How much percent finished
        /// </summary>
        public Action<Task<int>> FinishPercentAction = null;


        /// <summary>
        /// Build a new project from source
        /// </summary>
        /// <param name="cleanOutputDir"></param>
        public void Build(bool cleanOutputDir = true)
        {
            var startTime = DateTime.Now;
            try
            {

                Records.Clear();
                Record($"---- DBP Build begin at {startTime.ToString()}  ----");

                //var fullSourceRoot = Path.Combine(Directory.GetCurrentDirectory(), Manifest.SourceDir);
                //var fullOutputRoot = Path.Combine(Directory.GetCurrentDirectory(), Manifest.OutputDir);

                if (cleanOutputDir)
                {
                    try
                    {
                        if (Directory.Exists(Manifest.AbsoluteOutputDir))
                        {
                            Directory.Delete(Manifest.AbsoluteOutputDir, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        SenparcTrace.BaseExceptionLog(ex);
                    }
                }

                if (!Directory.Exists(Manifest.AbsoluteOutputDir))
                {
                    Directory.CreateDirectory(Manifest.AbsoluteOutputDir);
                }

                //Scan and save all file path to memory
                Record($"===== start scan all files  =====");
                ScanFile(Manifest.AbsoluteSourceDir, Manifest.AbsoluteOutputDir);
                Record($"---- end sacn all files  ----");
                Record($"---- {(DateTime.Now - startTime).TotalSeconds} seconds  ----");

                int groupIndex = 0;

                //var filesGroup = new List<List<string>>();
                //foreach (var configGroup in Manifest.ConfigGroup)
                //{
                //    var omitFiles = configGroup.OmitFiles.SelectMany(f => Directory.GetFiles(Manifest.AbsoluteOutputDir, f, SearchOption.AllDirectories)).ToList();
                //    var files = configGroup.Files.SelectMany(f => Directory.GetFiles(Manifest.AbsoluteOutputDir, f, SearchOption.AllDirectories))
                //                    .Where(f => !omitFiles.Contains(f)).ToList();
                //    filesGroup.Add(files);
                //    AllFilesCount += files.Count;
                //}

                AllFilesCount = Manifest.ConfigGroup.Sum(z => z.Files.Count());

                foreach (var configGroup in Manifest.ConfigGroup)
                {
                    groupIndex++;

                    Record($"config group: {groupIndex}");

                    //select files my file condition (or search pattern)
                    Func<string, IEnumerable<string>> searchFileFunc = (fileCondition) =>
                    {
                        string fileNamePattern = null;
                        var exactMatchFileName = false;
                        if (fileCondition != null)
                        {
                            if (fileCondition.Contains("*"))
                            {
                                fileNamePattern = $"({fileCondition.Replace("*", ".*")})";

                                if (fileCondition.Last() != '*')
                                {
                                    fileNamePattern += "$";//the file name ends by fileCondition
                                }

                                if (fileCondition.First() != '*')
                                {
                                    fileNamePattern = "^" + fileNamePattern;//the file name starts by fileCondition
                                }
                            }
                            else
                            {
                                fileNamePattern = fileCondition;
                                exactMatchFileName = true;//not wildcard character, exact match file name
                            }
                        }

                        var result = FilesCache.Keys.Where(z =>
                                    exactMatchFileName
                                        ? ((fileCondition.Contains("/") || fileCondition.Contains("\\"))
                                            ? z.EndsWith(fileCondition)                                 // contains path charter
                                            : z.Split(new[] { '/', '\\' }).Last() == fileCondition)     // whole file name without path charter
                                        : (fileNamePattern == null
                                            ? true                                                      // no fileNamePattern supported, all files allow
                                            : Regex.IsMatch(z.Split(new[] { '/', '\\' }).Last(), fileNamePattern, RegexOptions.IgnoreCase))); // fileNamePattern supported                                    );
                        return result;
                    };

                    var omitFiles = configGroup.OmitChangeFiles.SelectMany(searchFileFunc).ToList();
                    //var files = configGroup.Files.SelectMany(searchFileFunc).Where(f => !omitFiles.Contains(f)).ToList();
                    var files = configGroup.Files.SelectMany(searchFileFunc).ToList();
                    //files.AddRange(omitFiles);//add omit change files
                    //files = files.Distinct().ToList();// distinct

                    #region Remove Files

                    if (configGroup.RemoveFiles)
                    {
                        Record($"[in memory] remove files:");
                        foreach (var file in files.ToList())
                        {
                            Record($"[in memory] try to remove file: {file}");
                            FilesCache.Remove(file);
                            files.Remove(file);
                            FinishedFilesCount++;
                        }
                        continue;
                    }

                    #endregion

                    #region Remove Directories

                    foreach (var dir in configGroup.RemoveDictionaries)
                    {
                        var dirPath = Path.Combine(Manifest.AbsoluteOutputDir, dir);
                        Record($"[in memory] tobe remove directory: {dirPath}");

                        var removeDirFiles = FilesCache.Keys
                            .Where(z => z.Contains($"\\{dir}\\") || z.Contains($"/{dir}/")).ToList();

                        foreach (var file in removeDirFiles)
                        {
                            Record($"[in memory] remove directory: {dirPath}");
                            FilesCache.Remove(file);
                            files.Remove(file);
                        }
                    }

                    #endregion

                    foreach (var file in files)
                    {
                        try
                        {
                            Record($"[in memory] dynamic file: {file}");

                            //string fileContent = null;

                            var fileWrap = FilesCache[file];

                            if (!omitFiles.Contains(file))
                            {

                                #region File Mark

                                if (fileWrap.FileContent.Contains(FILE_MARK_PREFIX))
                                {
                                    //judgement whether this file can keep
                                    var regex = new Regex($@"{FILE_MARK_PREFIX}(?<kw>[^\r\n \*,]*)");
                                    var match = regex.Match(fileWrap.FileContent);
                                    if (match.Success && !configGroup.KeepFileConiditions.Any(z => z == match.Groups["kw"].Value))
                                    {
                                        //remove this file
                                        Record($"[in memory] remove this file");
                                        try
                                        {
                                            FilesCache.Remove(file);
                                            Record($"[in memory] removed file: {file}");
                                        }
                                        catch (Exception ex)
                                        {
                                            Record($"[in memory] delete file error:{ex}");
                                        }
                                        continue;
                                    }
                                }

                                #endregion

                                #region ReplaceContents

                                XDocument xml = null;
                                dynamic json = null;
                                foreach (var replaceContent in configGroup.ReplaceContents)
                                {
                                    #region String

                                    if (replaceContent.StringContent != null)
                                    {
                                        Record($"Replace String \"{replaceContent.StringContent.String}\" by \"{replaceContent.StringContent.ReplaceContent}\"");
                                        fileWrap.FileContent = fileWrap.FileContent.Replace(replaceContent.StringContent.String, replaceContent.StringContent.ReplaceContent);
                                    }

                                    #endregion

                                    #region Regex

                                    else if (replaceContent.RegexContent != null)
                                    {
                                        Record($"Regex Replace String \"{replaceContent.RegexContent.Pattern}\" by \"{replaceContent.RegexContent.ReplaceContent}\"");
                                        fileWrap.FileContent = Regex.Replace(fileWrap.FileContent, replaceContent.RegexContent.Pattern, replaceContent.RegexContent.ReplaceContent, replaceContent.RegexContent.RegexOptions);
                                    }

                                    #endregion

                                    #region Xml

                                    else if (replaceContent.XmlContent != null)
                                    {
                                        try
                                        {
                                            xml = xml ?? XDocument.Parse(fileWrap.FileContent);
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
                                            //var serializer = new JavaScriptSerializer();
                                            json = fileWrap.FileContent.GetObject<dynamic>(); //serializer.Deserialize(fileContent, typeof(object));
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
                                    fileWrap.FileContent = xml.ToString();
                                    Record($"Xml file changed.");
                                }
                                else if (json != null)
                                {
                                    fileWrap.FileContent = JsonConvert.SerializeObject(json, Formatting.Indented);
                                    Record($"Json file changed.");
                                }


                                #endregion

                                #region Custom Functions

                                if (configGroup.CustomFunc != null)
                                {
                                    fileWrap.FileContent = configGroup.CustomFunc(file, fileWrap.FileContent);
                                    Record($"Custom Function");
                                }


                                #region Content Mark

                                if (configGroup.KeepContentConiditions.Count > 0 && fileWrap.FileContent.Contains(BEGIN_MARK_PERFIX))
                                {
                                    var newContent = new StringBuilder();

                                    var lines = fileWrap.FileContent.Split(new string[] { Environment.NewLine, "\r", "\n" }, StringSplitOptions.None);
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
                                                    removeBlockCount++;

                                                    if (line.Contains(END_MARK))
                                                    {
                                                        //just remove this line
                                                        keep = true;
                                                    }
                                                    else
                                                    {
                                                        keep = false;
                                                    }

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
                                        fileWrap.FileContent = newContent.ToString();
                                    }
                                }
                                else
                                {
                                    //newContent.Append(fileWrap.FileContent);
                                }

                                Record("[in memory] File Size:" + fileWrap.FileContent.Length);

                                #endregion


                                #endregion
                            }
                            else
                            {
                                //newContent.Append(fileWrap.FileContent);// not change anything
                            }

                            Record($"[in memory] complete file processing: {file}");

                            FinishedFilesCount++;
                        }
                        catch (Exception ex)
                        {
                            Record($@"error[{file}]: {ex.Message}
{ex.StackTrace}");
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                Record($@"build error: {ex.Message}
{ex.StackTrace}");
            }
            finally
            {


            }

            try
            {
                #region save new files
                foreach (var fileWrap in FilesCache.Values.ToList())
                {
                    try
                    {
                        var dir = fileWrap.DestFilePath.Substring(0, fileWrap.DestFilePath.LastIndexOf(Path.DirectorySeparatorChar));
                        if (!Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }

                        //save the file to OutputDir
                        var openMode = File.Exists(fileWrap.DestFilePath) ? FileMode.Truncate : FileMode.Create;
                        using (var fs = new FileStream(fileWrap.DestFilePath, openMode))
                        {
                            fileWrap.TryLoadStream();//try load content stream
                            fileWrap.FileContentStream.CopyTo(fs);
                            fs.Flush(true);
                            fileWrap.FileContentStream.Dispose();//close the stream
                            Record($"modified and saved a new file: {fileWrap.DestFilePath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Record($@"saved a file error: {fileWrap.DestFilePath}
{ex.Message}
{ex.StackTrace}");
                    }

                }
                #endregion

                #region save manifest and log files

                var manifestFileName = Path.Combine(Manifest.AbsoluteOutputDir, "manifest.config");
                using (var logFs = new FileStream(manifestFileName, FileMode.Create))
                {
                    var sw = new StreamWriter(logFs, Encoding.UTF8);
                    sw.Write(Manifest.ToJson());
                    sw.Flush();
                    logFs.Flush(true);
                }
                Record($"saved manifest file: {manifestFileName}");


                var logFileName = Path.Combine(Manifest.AbsoluteOutputDir, "DPB.log");
                Record($"saved log file: {logFileName}");
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

                #endregion
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                //TODO:recore out side
            }
        }

    }
}
