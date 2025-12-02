using System;
using System.Drawing;
using System.IO;

namespace HtmlDocGen {

  public class HtmlDocumentFormatProvider : IDocumentFormatProvider {

    public bool EnableTokenizingV2 { get; set; } = false;

#if NET46
    public void WriteImageToStream(string scope, StreamWriter target, Func<string, object> dataSourceGetter, Image source) {
      this.WriteObjectToStream(scope, target, dataSourceGetter, source, false);
    }
#else
    [Obsolete("Images only supported in .NET Framework 4.x versions", true)]
    public void WriteImageToStream(string scope, StreamWriter target, Func<string, object> dataSourceGetter, object source) {
      //TODO: Image-Support reparieren
      throw new NotSupportedException("Images only supported in .NET Framework 4.x versions");
    }
#endif

    public void WriteTemplateToStream(string scope, StreamWriter target, Func<string, object> dataSourceGetter, Type forTarget, Stream templateStream) {
      this.WriteStreamToStream(scope, target, dataSourceGetter, templateStream, true);
    }

    public void WriteStreamToStream(string scope, StreamWriter target, Func<string, object> dataSourceGetter, Stream source, bool resolvePlaceholders) {
      if (resolvePlaceholders) {
        using (var sr = new StreamReader(source)) {
          this.WriteTextToStream(scope, target, dataSourceGetter, sr);
        }
      }
      else {
        source.CopyTo(target.BaseStream);
      }
    }

    public void WriteTextToStream(string scope, StreamWriter target, Func<string, object> dataSourceGetter, string text) {
      using (var sr = new StringReader(text)) {
        this.WriteTextToStream(scope, target, dataSourceGetter, sr);
      }
    }

    public void WriteTextToStream(string scope, StreamWriter target, Func<string, object> dataSourceGetter, TextReader textReader) {

      void placeholderProcessingMethod(string placeHolderString) {

        object placeHolderObject;
        string formatString = placeHolderString.SubStringAfter(":").NothingIfEmpty();
        string objectName = placeHolderString.SubStringBefore(":");
        string newScope = scope;

        if (objectName.Contains("~")) {
          newScope = objectName.SubStringAfter("~");
          objectName = objectName.SubStringBefore("~");
        }

        placeHolderObject = dataSourceGetter.Invoke(objectName);
        this.WriteObjectToStream(newScope, target, dataSourceGetter, placeHolderObject, false, formatString);

      };

      void contentProcessingMethod(string placeHolderString) => this.WriteObjectToStream(scope, target, dataSourceGetter, placeHolderString, true);

      if (this.EnableTokenizingV2) {
        textReader.Tokenize("{{", "}}", placeholderProcessingMethod, contentProcessingMethod);
      }
      else {
        textReader.Tokenize("<!--{", "}-->", placeholderProcessingMethod, contentProcessingMethod);
      }

    }

    public void WriteObjectToStream(string scope, StreamWriter target, Func<string, object> dataSourceGetter, object obj, bool isTemplateContent, string formatString = null) {

      if (obj == null) { // NOTHING
        return;
      }

      else if (obj is string) { // STRING
        if (!string.IsNullOrEmpty((string)obj)) {
          if (isTemplateContent) {
            target.Write((string)obj);
          }
          else {
            target.Write(this.EscapeHtml((string)obj));
          }
        }
      }

      else if (typeof(DocumentBlock).IsAssignableFrom(obj.GetType())) { // SUB-DOCUMENT
        ((DocumentBlock)obj).WriteOutputTo(target, this, dataSourceGetter, scope);
      }
#if NET46
      else if (obj is Image) { // IMAGE
        target.Write("data:image/png;base64,");
        target.Write(((Image)obj).GetBytes(System.Drawing.Imaging.ImageFormat.Png).ToBase64String());
      }
#else
      else if (obj != null && obj.GetType().Name.EndsWith("Image")) { // IMAGE
        //TODO: Image-Support reparieren
        throw new NotSupportedException("Images only supported in .NET Framework 4.x versions");
      }
#endif

      else if (obj is Stream) { // STREAM (a datasource could inject a file)
        this.WriteStreamToStream(scope, target, dataSourceGetter, (Stream)obj, false);
      }

      else if (typeof(System.Collections.IEnumerable).IsAssignableFrom(obj.GetType())) {
        foreach (var item in (System.Collections.IEnumerable)obj) {
          if (item is DocumentBlock) {
            // wenn models properties vom typ DocumentBlock enthalten...
            // eigentlich ein sehr schlechtes design!!!
            ((DocumentBlock)item).WriteOutputTo(target, this, dataSourceGetter, scope);
          }
          else {
            this.WriteObjectToStream(scope, target, dataSourceGetter, item, isTemplateContent, formatString);
          }
        }
      }

      else { // OTHER TYPES (using ToString() with Format)
        string str;
        if (string.IsNullOrWhiteSpace(formatString)) {
          str = obj.ToString();
        }
        else {
          str = string.Format("{0:" + formatString + "}", obj);
        }
        if (isTemplateContent) {
          target.Write(str);
        }
        else {
          target.Write(this.EscapeHtml(str));
        }

      }

    }

    public void WritePagebreakToStream(string scope, StreamWriter target) {
      target.WriteLine("<p id=\"pagebreak\">");
    }

    private string EscapeHtml(string text) {
      return System.Web.HttpUtility.HtmlEncode(text).Replace(" ", "&nbsp;").Replace(Environment.NewLine, "<br />");
    }

    public void WriteTextToStream(string scope, StreamWriter target, Func<string, object> dataSourceGetter, string formatstring, params object[] args) {
      this.WriteTextToStream(scope, target, dataSourceGetter, string.Format(formatstring, args));
    }

    public string TemplateDirectory { get; set; }

    public Stream OpenTemplateStream(string scope, Type forTarget) {
      EmbeddedFile @file;
      string fileName;

      if (scope == null) {
        fileName = string.Format("{0}.Template.htm", forTarget.Name);
      }
      else {
        fileName = string.Format("{0}.Template.{1}", forTarget.Name, scope);
      }

      @file = forTarget.Assembly.EmbeddedFiles()[fileName];

      if (@file == null) {
        throw new ApplicationException("Template file '{1}' was not found as Embedded Resource in Assembly '{0}'".FormatWith(forTarget.Assembly.GetName().Name, fileName));
      }

      return @file.OpenStream();
    }

    public void WriteDocumentStartToStream(string scope, StreamWriter target, Func<string, object> dataSourceGetter) {
      // target.WriteLine("<html>")
      // target.WriteLine("<head>")
      // target.WriteLine("<style>")
      // 'Using s = Me.OpenTemplateStream("DefaultCssStyle.css")
      // '  target.WriteLine(s.ReadAllText())
      // 'End Using
      // target.WriteLine("</style>")
      // target.WriteLine("</head>")
      // target.WriteLine("<body>")
    }

    public void WriteDocumentEndToStream(string scope, StreamWriter target, Func<string, object> dataSourceGetter) {
      // target.WriteLine("</body>")
      // target.WriteLine("</html>")
    }

  }
}