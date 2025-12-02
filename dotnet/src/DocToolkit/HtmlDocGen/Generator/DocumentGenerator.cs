using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HtmlDocGen {

  public class DocumentGenerator {

    public DocumentGenerator() {
    }

    public void GenerateDocument(StreamWriter target, params DocumentBlock[] rootNodes) {
      this.GenerateDocument(target, rootNodes.ToList());
    }

    protected virtual string DefaultScope {
      get {
        return null;
      }
    }

    public void GenerateDocument(StreamWriter target, IEnumerable<DocumentBlock> rootNodes, string scope = null) {

      if (scope == null) {
        scope = this.DefaultScope;
      }

      this.DocumentFormatProvider.WriteDocumentStartToStream(scope, target, this.ResolveGeneratorSpecificPlaceholders);
      foreach (var node in rootNodes)
        node.WriteOutputTo(target, this.DocumentFormatProvider, this.ResolveGeneratorSpecificPlaceholders, scope);

      this.DocumentFormatProvider.WriteDocumentEndToStream(scope, target, this.ResolveGeneratorSpecificPlaceholders);

      target.Flush();

    }

    private object ResolveGeneratorSpecificPlaceholders(string name) {

      switch (name ?? "") {

        case "Generator.FormatProviderType": {
            return this.DocumentFormatProvider.GetType().Name;
          }

        case "DateTime.Now": {
            return DateTime.Now;
          }

        case "Environment.MachineName": {
            return Environment.MachineName;
          }

        case "Environment.UserName": {
            return Environment.UserName;
          }

        case "Guid.NewGuid": {
            return Guid.NewGuid().ToString();
          }

        default: {
            string typeName = name.SubStringBefore(".");
            var t = Type.GetType(typeName, false);
            if (t != null) {
              return t.GetValueFrom(null, name.Substring(typeName.Length + 1, name.Length - typeName.Length - 1));
            }

            break;
          }

      }

      return null;
    }

    public IDocumentFormatProvider DocumentFormatProvider { get; set; }

  }
}