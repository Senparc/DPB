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
            Manifest manifest = new Manifest();
            manifest.SourceDir = "..\\..\\SourceDir";
            manifest.OutputDir = "..\\..\\OutputDir";

            //keep content Condition - MP
            manifest.Paths.Add(new PathConfig()
            {
                Files = new List<string>() { "*.cs" },
                KeepContentConiditions = new List<string>() { "MP" }
            });

            //keep files Condition - Keep
            manifest.Paths.Add(new PathConfig()
            {
                Files = new List<string>() { "*.txt" },
                KeepFileConiditions = new List<string>() { "Keep" },
            });

            //change xml nodes' value
            var pathConfigXml = new PathConfig()
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

            //change jaon nodes' value
            var pathConfigJson = new PathConfig()
            {
                Files = new List<string>() { "*.json" }
            };

            pathConfigXml.ReplaceContents.Add(new ReplaceContent()
            {
                JsonContent = new JsonContent()
                {
                    KeyName = "Version",
                    ReplaceContent = "6.6.6.6"
                }
            });

            manifest.Paths.Add(pathConfigXml);


            LetsGo letsGo = new LetsGo(manifest);
            letsGo.Build();
        }
    }
}
