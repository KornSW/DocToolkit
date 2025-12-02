using System;
using System.Collections;
using System.IO;

namespace HtmlDocGen {

  public delegate object DataSourceGetterMethod(string typeNameAndProperty);

  public class MultipleDocumentBlocks<TNode> : DocumentBlock where TNode : TemplateBlock, new() {

    private Func<DataSourceGetterMethod, IEnumerable> _Selector;
    public MultipleDocumentBlocks(IEnumerable iteratableDataSource) {
      _Selector = new Func<DataSourceGetterMethod, IEnumerable>(ds => iteratableDataSource);
    }

    public MultipleDocumentBlocks(Func<DataSourceGetterMethod, IEnumerable> selector) {
      _Selector = selector;
    }

    public override void WriteOutputTo(StreamWriter target, IDocumentFormatProvider provider, Func<string, object> dataSourceGetter, string scope) {

      object wrappeddataSourceGetter(string name) {
        string className = name.SubStringBefore(".");
        string memberName = name.SubStringAfter(".");

        var dataSource = this.DataSource.GetSource(className);
        if (dataSource == null) {
          if (dataSourceGetter != null) {
            return dataSourceGetter.Invoke(name);
          }
          else {
            return null;
          }
        }
        else if (string.IsNullOrEmpty(memberName)) {
          return dataSource;
        }
        else {
          return dataSource.GetType().GetValueFrom(dataSource, memberName);
        }

      };

      if (this.IsVisible(wrappeddataSourceGetter)) {

        var currentNode = new TNode();
        IEnumerable source;
        source = _Selector.Invoke(wrappeddataSourceGetter);

        foreach (object iteratingDataSource in source) {
          currentNode.DataSource.SetSource(iteratingDataSource);
          currentNode.WriteOutputTo(target, provider, wrappeddataSourceGetter, scope);
        }

      }

    }

  }
}