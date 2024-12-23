using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace TeXnically
{
    public class LatexTools
    {
        public static string TagSvgWithLatex(string tag, string svg)
        {
            // If string is empty, return an empty SVG
            if (String.IsNullOrWhiteSpace(svg))
                return "";
            
            // Load the SVG content
            XmlDocument doc = new XmlDocument();
            try 
            { 
                doc.LoadXml(svg);
            }
            catch { throw new Exception("The output SVG doesn't appear to be valid XML, and can't be parsed."); }

            if (!doc.HasChildNodes)
                throw new Exception("The output SVG has no child nodes, and can't be parsed.");
            else
            {
                foreach (XmlNode nd in doc.LastChild.ChildNodes)
                {
                    // Tag any g nodes with an id that matches the LaTeX content
                    if (nd.Name == "g")
                    {
                        XmlAttribute newAttr = doc.CreateAttribute("id");
                        newAttr.Value = tag;
                        if (nd.Attributes != null)
                            nd.Attributes.Append(newAttr);
                    }
                }

                // Return the modified document
                return doc.InnerXml.Replace("currentColor", "black"); // MS Office often struggles with this fill/stroke definition
            }
        }

        public static string? ReadLatexSvg(string svg)
        {
            // Load the XML
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.LoadXml(svg);
            }
            catch
            {
                throw new Exception("The input doesn't appear to be valid XML, and can't be parsed.");
            }

            if (!doc.HasChildNodes)
                throw new Exception("The input SVG has no child nodes, and can't be parsed.");
            else
            {
                foreach (XmlNode nd in doc.LastChild.ChildNodes)
                {
                    if (nd.Name == "g")
                    {
                        if (nd.Attributes != null)
                        {
                            if(nd.Attributes["id"] !=null)
                                return nd.Attributes["id"].Value;
                        }
                    }
                }

                return null;
            }
        }
    }
}
