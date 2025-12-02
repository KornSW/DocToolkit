using System;
using System.IO;

namespace HtmlDocGen {

  public abstract class DocumentBlock {

    public DocumentBlock() {
    }

    private DocumentDataSource _DataSource = new DocumentDataSource();

    public DocumentDataSource DataSource {
      get {
        return _DataSource;
      }
    }

    protected virtual bool IsVisible(Func<string, object> dataSourceGetter) {
      return true;
    }

    public abstract void WriteOutputTo(StreamWriter target, IDocumentFormatProvider provider, Func<string, object> dataSourceGetter, string scope);

  }
}