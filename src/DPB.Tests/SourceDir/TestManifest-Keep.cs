
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DPB.Tests.SourceDir
{
    public class TestManifestKeep
    {
        public TestManifestKeep()
        {
            //DPBMARK MP
            var tip = "this line will stay here in OutputDir while Conditions have MP keyword.";
            //DPBMARK_END

            //DPBMARK NotStay
            var tip2 = "this line will STAY here, because this file is in the OmitFiles list.";
            //DPBMARK_END

            var tip_stay = "this line will be always stay here.";
        }
    }
}
