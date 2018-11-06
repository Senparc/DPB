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
        /// <summary>
        /// Source directory
        /// </summary>
        public string SourceDir { get; set; }
        /// <summary>
        /// Output target directory
        /// </summary>
        public string OutputDir { get; set; }
        /// <summary>
        /// find the paths in this config group
        /// </summary>
        public List<GroupConfig> ConfigGroup { get; set; } = new List<GroupConfig>();
    }

    public class GroupConfig
    {
        /// <summary>
        /// file the files in this config group
        /// </summary>
        public List<string> Files { get; set; } = new List<string>();
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
    }

    /// <summary>
    /// Replace content config
    /// </summary>
    public class ReplaceContent
    {
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
}
