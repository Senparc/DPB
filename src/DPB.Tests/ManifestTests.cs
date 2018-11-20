using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DPB.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Helpers;

namespace DPB.Tests
{
    [TestClass]
    public class ManifestTests
    {
        [TestMethod]
        public void JsonTest()
        {
            var json = @"{
""SourceDir"":"""",
""OutputDir"":"""",
""Paths"":[{
    ""Files"":[],
    ""KeepFileConiditions"":[],
    ""KeepContentConiditions"":[],
    ""ReplaceContents"":[
        {""XmlContent"":{""TagName"":""<TargetFrameworks>"",""ReplaceContent"":""this is the new content""}}
    ]
}]
}";

            //测试配置JSON读取
            var dpbManifest = SerializerHelper.GetObject<Manifest>(json);
            Console.WriteLine(dpbManifest.ToJson());
        }

        [TestMethod]
        public void BuildTest()
        {
            var sourceDir = "..\\..\\SourceDir";//or absolute address: e:\ThisProject\src
            var outputDir = "..\\..\\OutputDir";//or absolute address: e:\ThisProject\Output
            Manifest manifest = new Manifest(sourceDir, outputDir);

            //keep content Condition - while all the code blocks in *.cs files with keywrod mark: DPBMARK MP
            manifest.ConfigGroup.Add(new GroupConfig()
            {
                Files = new List<string>() { "*.cs" },
                OmitFiles = new List<string>() { "*-Keep.cs" },
                KeepContentConiditions = new List<string>() { "MP" }
            });

            //keep files Condition - Keep
            manifest.ConfigGroup.Add(new GroupConfig()
            {
                Files = new List<string>() { "*.txt" },
                KeepFileConiditions = new List<string>() { "Keep" },
            });

            //change certain string content

            manifest.ConfigGroup.Add(new GroupConfig()
            {
                Files = new List<string>() { "StringReplaceFile.txt", "RegexReplaceFile.txt" },
                ReplaceContents = new List<ReplaceContent>() {
                     new ReplaceContent(){
                             StringContent=new StringContent(){
                                 String="<This conent will be replaced by StringContent>",
                                  ReplaceContent="[This is new content, replaced by StringContent]"
                             }
                     },
                     new ReplaceContent(){
                          RegexContent = new RegexContent(){
                                  Pattern = @"\<[^\>]*\>",
                                  ReplaceContent="[This is new content, replaced by ReplaceContent]",
                                   RegexOptions = RegexOptions.IgnoreCase
                          }
                     }
                 }
            });

            //change xml nodes' value
            var pathConfigXml = new GroupConfig()
            {
                Files = new List<string>() { "*.xml" }
            };
            pathConfigXml.ReplaceContents.Add(new ReplaceContent()
            {
                XmlContent = new XmlContent()
                {
                    TagName = "ProjectName",
                    ReplaceContent = "This is the new value"
                }
            });
            pathConfigXml.ReplaceContents.Add(new ReplaceContent()
            {
                XmlContent = new XmlContent()
                {
                    TagName = "Count",
                    ReplaceContent = "666"
                }
            });
            manifest.ConfigGroup.Add(pathConfigXml);


            //change jaon nodes' value
            var pathConfigJson = new GroupConfig()
            {
                Files = new List<string>() { "*.json" }
            };

            pathConfigJson.ReplaceContents.Add(new ReplaceContent()
            {
                JsonContent = new JsonContent()
                {
                    KeyName = "version",
                    ReplaceContent = "6.6.6.6"
                }
            });
            manifest.ConfigGroup.Add(pathConfigJson);

            //remove file
            manifest.ConfigGroup.Add(new GroupConfig()
            {
                Files = new List<string>() { "FileRemove*.txt" },
                OmitFiles = new List<string>() { "FileRemoveOmit.txt" },
                RemoveFiles = true
            });

            //remove directories
            manifest.ConfigGroup.Add(new GroupConfig()
            {
                RemoveDictionaries = new List<string>() {
                    "ChildrenDirectoriesWillBeRemoved\\Remove1",
                    "ChildrenDirectoriesWillBeRemoved\\Remove2"
              }
            });

            //custom functions
            manifest.ConfigGroup.Add(new GroupConfig()
            {
                Files = new List<string>() { "CustomFunctionFile*.txt" },
                CustomFunc = fileContent => fileContent.ToUpper()// all letters ToUpper(), or do anythiny you like
            });

            LetsGo letsGo = new LetsGo(manifest);
            letsGo.Build();
        }
    }
}
