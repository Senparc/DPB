using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DPB.Models
{
    /// <summary>
    /// Manifest module
    /// </summary>
    public class Manifest
    {
        public List<Config> Configs { get; set; }
    }

    /// <summary>
    /// One config group
    /// </summary>
    public class Config
    {
        /// <summary>
        /// find the paths in this config group
        /// </summary>
        public List<string> Paths { get; set; }

    }


}
