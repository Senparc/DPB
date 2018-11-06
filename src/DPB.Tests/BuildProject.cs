using DPB.Models;

namespace DPB.Tests
{
    public class BuildProject
    {
        public void Build()
        {
            Manifest manifest = new Manifest();
            manifest.SourceDir = "..\\..\\SourceDir";//or absolute address: e:\ThisProject\src
            manifest.OutputDir = "..\\..\\OutputDir";//or absolute address: e:\ThisProject\Output
        }
    }
}
