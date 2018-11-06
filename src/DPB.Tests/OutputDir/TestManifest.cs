
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DPB.Tests.SourceDir
{
    public class TestManifest
    {
        public TestManifest()
        {
            //PDBMARK MP
            var tip = "this line will stay here in OutputDir while Conditions have MP keyword.";
            //PDBMARK_END


            var tip_stay = "this line will be always stay here.";
        }
    }
}
