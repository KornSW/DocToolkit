
namespace HtmlDocGen {

  public class HtmlDocumentGenerator : DocumentGenerator {

    public HtmlDocumentGenerator(bool enableTokenizingV2 = false) {
      this.DocumentFormatProvider = new HtmlDocumentFormatProvider() { EnableTokenizingV2 = enableTokenizingV2 };
    }

    protected override string DefaultScope {
      get {
        return "htm";
      }
    }

  }
}