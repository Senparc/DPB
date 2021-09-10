using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
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
                OmitChangeFiles = new List<string>() { "*-Keep.cs" },
                KeepContentConiditions = new List<string>() { "MP" }
            });

            //keep files Condition - Keep
            manifest.ConfigGroup.Add(new GroupConfig()
            {
                Files = new List<string>() { "*.txt" },
                KeepFileConiditions = new List<string>() { "Keep" },
                 //KeepContentConiditions= new List<string>() { "o","k"}
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
                OmitChangeFiles = new List<string>() { "FileRemoveOmit.txt" },
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

            //.net core file test
            manifest.ConfigGroup.Add(new GroupConfig()
            {
                Files = new List<string>() { "Startup.cs.txt" },
                KeepContentConiditions = new List<string>() { "MP", "Redis" }
            });

            //custom functions
            manifest.ConfigGroup.Add(new GroupConfig()
            {
                Files = new List<string>() { "CustomFunctionFile1.txt" },
                CustomFunc = (fileName, fileContent) => fileContent.ToUpper() + $"{Environment.NewLine}FileName:{fileName} - {DateTime.Now}"// all letters ToUpper(), or do anythiny you like
            });

            manifest.ConfigGroup.Add(new GroupConfig()
            {
                Files = new List<string>() { "CustomFunctionFile2-net45-csproj.xml" },
                KeepContentConiditions = new List<string>() { "MP", "Redis" },
                CustomFunc = (fileName, fileContent) =>
                {
                    XDocument d = XDocument.Parse(fileContent);
                    XNamespace dc = d.Root.Name.Namespace;
                    var xmlNamespace = dc.ToString();

                    d.Root.Elements(dc + "ItemGroup").ToList()
                            .ForEach(z => z.Elements(dc + "ProjectReference")
                                           .Where(el => !el.ToString().Contains("CommonService"))
                                           .Remove());

                    //add each nuget packages
                    var newItemGroup = new XElement(dc + "ItemGroup");
                    d.Root.Add(newItemGroup);

                    var newElement = new XElement(dc + "PackageReference");
                    newElement.Add(new XAttribute("Include", "NEW_PACKAGE"));
                    newElement.Add(new XAttribute("Version", "NEW_PACKAGE_VERSION"));
                    newItemGroup.Add(newElement);
                    return d.ToString();
                }
            });

            manifest.ConfigGroup.Add(new GroupConfig()
            {
                Files = new List<string>() { "KeepPartsOfContent.txt" },
                KeepFileConiditions = new List<string>() { "KeepPartsOfContent" },
                KeepContentConiditions = new List<string>() { "KeepPartsOfContent" },
            });

            LetsGo letsGo = new LetsGo(manifest);
            letsGo.Build();
        }
    }
}
