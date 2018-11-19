using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DPB.Models
{
    /// <summary>
    /// Manifest module
    /// </summary>
    public class Manifest
    {
        private string _sourceDir;
        private string _outputDir;

        /// <summary>
        /// Source directory
        /// </summary>
        public string SourceDir
        {
            get => _sourceDir;
            set
            {
                _sourceDir = value;
                if (Directory.GetLogicalDrives().ToList().Any(z => _sourceDir.ToUpper().StartsWith(z.ToUpper())))
                {
                    AbsoluteSourceDir = _sourceDir;
                }
                else
                {
                    AbsoluteSourceDir = Path.Combine(Directory.GetCurrentDirectory(), _sourceDir);
                }
            }
        }
        /// <summary>
        /// Absolute source directory
        /// </summary>
        public string AbsoluteSourceDir { get; private set; }

        /// <summary>
        /// Output target directory
        /// </summary>
        public string OutputDir
        {
            get => _outputDir; set
            {
                _outputDir = value;
                if (Directory.GetLogicalDrives().ToList().Any(z => _outputDir.ToUpper().StartsWith(z.ToUpper())))
                {
                    AbsoluteOutputDir = _outputDir;
                }
                else
                {
                    AbsoluteOutputDir = Path.Combine(Directory.GetCurrentDirectory(), _outputDir);
                }
            }
        }
        /// <summary>
        /// Absolute output target directory
        /// </summary>
        public string AbsoluteOutputDir { get; private set; }
        /// <summary>
        /// find the paths in this config group
        /// </summary>
        public List<GroupConfig> ConfigGroup { get; set; } = new List<GroupConfig>();

        /// <summary>
        /// Manifest
        /// </summary>
        /// <param name="sourceDir">Source directory</param>
        /// <param name="outputDir">Output target directory</param>
        public Manifest(string sourceDir, string outputDir)
        {
            SourceDir = sourceDir;
            OutputDir = outputDir;
        }
    }

    public class GroupConfig
    {

        /// <summary>
        /// partten to find the files in this config group, ex. *.cs
        /// </summary>
        public List<string> Files { get; set; } = new List<string>();
        /// <summary>
        /// omit files in Files result
        /// </summary>
        public List<string> OmitFiles { get; set; } = new List<string>();
        /// <summary>
        /// remove all files in Files but OmitFiles
        /// </summary>
        public bool RemoveFiles { get; set; }
        /// <summary>
        /// remove dictionaries(relative address)
        /// </summary>
        public List<string> RemoveDictionaries { get; set; } = new List<string>();

        /// <summary>
        /// while meet one of the conditions, the files will not be deleted
        /// </summary>
        public List<string> KeepFileConiditions { get; set; } = new List<string>();
        /// <summary>
        /// while meet one of the conditions, the code block in file will be retained. Otherwise the code block will be removed.
        /// <para><code>//DPB Keep,this,BLOCK</code></para>
        /// <para><code>this is the block</code></para>
        /// <para><code>for any condition: 'Keep', 'this' or 'BLOCK' (case insensitive) can be retained</code></para>
        /// <para><code>until the following line</code></para>
        /// <para><code>//DPB END</code></para>
        /// <para>PS: DPB at the begin and DPB END at the and must use UPPERCASE, // is not optional, just for many languages to support the annotations</para>
        /// </summary>
        public List<string> KeepContentConiditions { get; set; } = new List<string>();
        /// <summary>
        /// The content to replace
        /// </summary>
        public List<ReplaceContent> ReplaceContents { get; set; } = new List<ReplaceContent>();

        /// <summary>
        /// Custom functions for file content, input is file content ,outpit is new file content
        /// </summary>
        [JsonIgnore]
        public Func<string, string> CustomFunc { get; set; }
    }

    /// <summary>
    /// Replace content config
    /// </summary>
    public class ReplaceContent
    {
        /// <summary>
        /// String config
        /// </summary>
        public StringContent StringContent { get; set; }
        /// <summary>
        /// Regex config
        /// </summary>
        public RegexContent RegexContent { get; set; }
        /// <summary>
        /// Xml node config (only for xml format file)
        /// </summary>
        public XmlContent XmlContent { get; set; }
        /// <summary>
        /// Json node config (only for json file)
        /// </summary>
        public JsonContent JsonContent { get; set; }
    }


    /// <summary>
    /// String config
    /// </summary>
    public class StringContent
    {
        /// <summary>
        /// String
        /// </summary>
        public string String { get; set; }
        ///// <summary>
        ///// Is CaseSensitive, default: false
        ///// </summary>
        //public bool CaseSensitive { get; set; } = false;
        /// <summary>
        /// While condition string meets, this node's content will be replaced with new value
        /// </summary>
        public string ReplaceContent { get; set; }
    }

    /// <summary>
    /// Regex config
    /// </summary>
    public class RegexContent
    {
        /// <summary>
        /// Regex pattern
        /// </summary>
        public string Pattern { get; set; }
        /// <summary>
        /// RegexOptions
        /// </summary>
        public RegexOptions RegexOptions { get; set; }
        /// <summary>
        /// While condition string meets, this node's content will be replaced with new value
        /// </summary>
        public string ReplaceContent { get; set; }
    }

    /// <summary>
    /// Xml node config (only for xml format file)
    /// </summary>
    public class XmlContent
    {
        /// <summary>
        /// Tag name for xml
        /// </summary>
        public string TagName { get; set; }
        /// <summary>
        /// While condition string meets, this node's content will be replaced with new value
        /// </summary>
        public string ReplaceContent { get; set; }
    }

    /// <summary>
    /// Json node config (only for json file)
    /// </summary>
    public class JsonContent
    {
        /// <summary>
        /// Json key name
        /// </summary>
        public string KeyName { get; set; }
        /// <summary>
        /// While condition string meets, this node's content will be replaced with new value
        /// </summary>
        public string ReplaceContent { get; set; }
    }



    //public class TextContent
    //{
    //    /// <summary>
    //    /// Json key name
    //    /// </summary>
    //    public string KeyName { get; set; }
    //    /// <summary>
    //    /// While condition string meets, this node's content will be replaced with new value
    //    /// </summary>
    //    public string ReplaceContent { get; set; }
    //}
}
