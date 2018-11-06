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
            manifest.Paths.Add(new PathConfig()
            {
                Files = new List<string>() { "*.cs" },
                KeepContentConiditions = new List<string>() { "MP" }
            });

            manifest.Paths.Add(new PathConfig()
            {
                Files = new List<string>() { "*.txt" },
                KeepFileConiditions = new List<string>() { "Keep" },
            });


            LetsGo letsGo = new LetsGo(manifest);
            letsGo.Build();
        }
    }
}
