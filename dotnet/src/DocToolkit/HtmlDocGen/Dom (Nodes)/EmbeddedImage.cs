using System;
using System.Drawing;
using System.IO;

namespace HtmlDocGen {

#if NET46

  public abstract class EmbeddedImage : DocumentBlock {

    protected abstract Image GenerateImage(Func<string, object> dataSourceGetter);

    public override void WriteOutputTo(StreamWriter target, IDocumentFormatProvider provider, Func<string, object> dataSourceGetter, string scope) {
      provider.WriteImageToStream(scope, target, dataSourceGetter, this.GenerateImage(dataSourceGetter));
    }

  }

  public class StaticEmbeddedImage : EmbeddedImage {

    private Image _Image;

    public StaticEmbeddedImage(Image image) {
      _Image = image;
    }

    protected override Image GenerateImage(Func<string, object> dataSourceGetter) {
      return _Image;
    }

  }


#else

  //TODO: Image-Support reparieren
  [Obsolete("Images only supported in .NET Framework 4.x versions", true)]
  public abstract class EmbeddedImage : DocumentBlock {

    public override void WriteOutputTo(StreamWriter target, IDocumentFormatProvider provider, Func<string, object> dataSourceGetter, string scope) {

      throw new NotSupportedException("Images only supported in .NET Framework 4.x versions");

    }

  }

  //TODO: Image-Support reparieren
  [Obsolete("Images only supported in .NET Framework 4.x versions", true)]
  public class StaticEmbeddedImage : EmbeddedImage {

    public StaticEmbeddedImage(object image) {
    }

  }

#endif

}