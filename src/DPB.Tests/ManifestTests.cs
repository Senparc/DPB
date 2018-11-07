using System;
using System.Collections.Generic;
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

            //keep content Condition - while all the code blocks in *.cs files with keywrod mark: PDBMARK MP
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


            LetsGo letsGo = new LetsGo(manifest);
            letsGo.Build();
        }
    }
}
