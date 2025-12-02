
namespace HtmlDocGen {

  public class Content {

    private object[] _Data;

    public Content(params object[] data) {
      _Data = data;
    }

    public object Data {
      get {
        return _Data[0];
      }
    }

    public object get_Data(int index) {
      return _Data[index];
    }

  }
}