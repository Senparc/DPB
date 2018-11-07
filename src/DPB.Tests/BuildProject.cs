using DPB.Models;

namespace DPB.Tests
{
    public class BuildProject
    {
        public void Build()
        {
            var sourceDir = "..\\..\\SourceDir";//or absolute address: e:\ThisProject\src
            var outputDir = "..\\..\\OutputDir";//or absolute address: e:\ThisProject\Output
            Manifest manifest = new Manifest(sourceDir,outputDir);
        }
    }
}
