using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Verse;

namespace Exosuit
{
    internal class XmlBackCompat: PatchOperation
    {

        public List<string> targetClassList;
        public List<string> classReplacedList;
        public Dictionary<string, string> ReplacePairs => Enumerable.Range(0, targetClassList.Count).ToDictionary(i => targetClassList[i],
                                                i => classReplacedList[i]);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ProcessNode(XmlNode n)
        {

            if (n.Value != null && ReplacePairs.TryGetValue(n.Value, out var v))
            {
                n.Value = v;
            }
        }
        protected override bool ApplyWorker(XmlDocument xml)
        {
            try
            {
                Log.Message(string.Join(','+Environment.NewLine, ReplacePairs.Select(kvp => $"\"{kvp.Key}\" => {kvp.Value}")));
            }
            catch (Exception ex)
            {

                Log.Error(ex.StackTrace);
            }


            ProcessNode(xml);
            if (xml.Attributes != null)
            {
                foreach (var attr in xml.Attributes)
                {
                    if (attr is XmlNode n)
                    {
                        NodeProcessor(n);
                    }
                }
            }
            if (xml.ChildNodes != null)
            {
                Parallel.ForEach(xml.ChildNodes.Cast<XmlNode>(),NodeProcessor);
            }
            return true;
        }
        private void NodeProcessor(XmlNode inNode)
        {
            ProcessNode(inNode);
            if (inNode.Attributes != null)
            {
                foreach (var attr in inNode.Attributes)
                {
                    if (attr is XmlNode n)
                    {
                        NodeProcessor(n);
                    }
                }
            }
            if (inNode.ChildNodes != null) { 
                foreach (XmlNode node in inNode.ChildNodes)
                {
                    NodeProcessor(node);
                }
            }
        }
    }
}
