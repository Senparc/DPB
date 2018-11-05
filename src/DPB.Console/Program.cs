using DPB.Models;
using Senparc.CO2NET.Extensions;
using Senparc.CO2NET.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DPB.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            //测试项目
            var json = @"{
""Paths"":[{
    ""Files"":[],
    ""KeepFileConiditions"":[],
    ""KeepContentConiditions"":[],
    ""ReplaceContents"":[
        {""XmlContent"":{""TagName"":""<TargetFrameworks>"",""ReplaceContent"":""this is the new content""}}
    ],
}]
}";

            var dpbManifest = SerializerHelper.GetObject<Manifest>(json);
            System. Console.WriteLine(dpbManifest.ToJson());

            System.Console.ReadLine();
        }
    }
}
