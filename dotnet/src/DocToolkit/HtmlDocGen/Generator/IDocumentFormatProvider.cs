using System;
using System.Drawing;
using System.IO;

namespace HtmlDocGen {

  public interface IDocumentFormatProvider {

    Stream OpenTemplateStream(string scope, Type forTarget);

    string TemplateDirectory { get; set; }

    void WriteTemplateToStream(string scope, StreamWriter target, Func<string, object> dataSourceGetter, Type forTarget, Stream templateStream);
    void WriteStreamToStream(string scope, StreamWriter target, Func<string, object> dataSourceGetter, Stream source, bool resolvePlaceholders);
    void WriteTextToStream(string scope, StreamWriter target, Func<string, object> dataSourceGetter, string text);
    void WriteTextToStream(string scope, StreamWriter target, Func<string, object> dataSourceGetter, string formatstring, params object[] args);

#if NET46
     void WriteImageToStream(string scope, StreamWriter target, Func<string, object> dataSourceGetter, Image source);
#else
    [Obsolete("Images only supported in .NET Framework 4.x versions", true)]
    void WriteImageToStream(string scope, StreamWriter target, Func<string, object> dataSourceGetter, object source);

#endif

    void WritePagebreakToStream(string scope, StreamWriter target);

    void WriteObjectToStream(string scope, StreamWriter target, Func<string, object> dataSourceGetter, object obj, bool isTemplateContent, string formatString = null);

    void WriteDocumentStartToStream(string scope, StreamWriter target, Func<string, object> dataSourceGetter);
    void WriteDocumentEndToStream(string scope, StreamWriter target, Func<string, object> dataSourceGetter);

  }
}