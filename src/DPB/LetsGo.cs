using DPB.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DPB
{
    public class LetsGo
    {
        public Manifest Manifest { get; set; }

        public LetsGo(Manifest manifest)
        {
            Manifest = manifest;
        }

        public void Build()
        {

        }
    }
}
