using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace DropPointImportFromGLS
{
    public class XmlStripper
    {
        public XmlNode RemoveAllNamespaces(XmlNode documentElement)
        {
            var xmlnsPattern = "\\s+xmlns\\s*(:\\w)?\\s*=\\s*\\\"(?<url>[^\\\"]*)\\\"";
            var outerXml = documentElement.OuterXml;
            var matchCol = Regex.Matches(outerXml, xmlnsPattern);
            foreach (var match in matchCol)
                outerXml = outerXml.Replace(match.ToString(), "");

            var result = new XmlDocument();
            result.LoadXml(outerXml);

            return result;
        }

        public XmlNode Strip(XmlNode documentElement)
        {
            var namespaceManager = new XmlNamespaceManager(documentElement.OwnerDocument.NameTable);
            foreach (var nspace in namespaceManager.GetNamespacesInScope(XmlNamespaceScope.All))
            {
                namespaceManager.RemoveNamespace(nspace.Key, nspace.Value);
            }

            return documentElement;
        }
    }

}
