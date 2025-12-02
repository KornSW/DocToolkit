#if NET46

using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing.Imaging;

namespace System.Drawing {
    
   [Obsolete("Kommt aus aus kSystemExtensions")]
   internal static class ExtensionsForImage {

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static Icon ToIcon(this byte[] bytes) {
      var ms = new MemoryStream(bytes);
      return new Icon(ms);
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static byte[] GetBytes(this Image image) {
      var ms = new MemoryStream();
      image.Save(ms, ImageFormat.Png);
      return ms.ToArray();
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static string ToBase64String(this Image image) {
      return Convert.ToBase64String(image.GetBytes());
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static byte[] GetBytes(this Image image, ImageFormat format) {
      if (image is null) {
        return null;
      }
      var ms = new MemoryStream();
      image.Save(ms, format);
      return ms.ToArray();
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static string GetBase64ContentString(this Image image) {
      return image.GetBase64ContentString(ImageFormat.Png);
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static string GetBase64ContentString(this Image image, ImageFormat format) {
      var sb = new StringBuilder();
      sb.Append("data:");
      sb.Append(format.GetMimeName());
      sb.Append(";base64,");
      sb.Append(image.GetBytes().ToBase64String());
      return sb.ToString();
    }

    public enum VerticalAlignment : int {
      Top,
      Middle,
      Bottom
    }

    public enum HorizontalAlignment : int {
      Left,
      Middle,
      Right
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static Image ToImage(this string text, Font font, Color foreColor) {
      return text.ToImage(font, foreColor, Color.Transparent);
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static Image ToImage(this string text, Font font, Color foreColor, Color backColor) {
      if (string.IsNullOrWhiteSpace(text)) {
        return new Bitmap(font.Height, 1);
      }
      Bitmap img;
      using (Graphics g = Graphics.FromImage(new Bitmap(font.Height, font.Height))) {
        {
          var withBlock = g.MeasureString(text, font);
          img = new Bitmap((int)withBlock.Width + 1, (int)withBlock.Height + 1);
        }
      }
      var brush = new SolidBrush(foreColor);
      using (Graphics g = Graphics.FromImage(img)) {
        if (!(backColor == Color.Transparent)) {
          g.FillRectangle(new SolidBrush(backColor), 0, 0, img.Width, img.Height);
        }

        g.DrawString(text, font, brush, 0, 0);
      }
      return img;
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static string GetMimeName(this ImageFormat format) {
      return "image/" + format.ToString();
    }

    // <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    // Public Function CalulateChildOffset(sourceInstance As Rectangle, childRect As Rectangle, hAlign As HorizontalAlignment, vAlign As VerticalAlignment) As Point
    // Throw New NotImplementedException
    // End Function

    // <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    // Public Function CalulateChildOffset(sourceInstance As Size, childRect As Rectangle, hAlign As HorizontalAlignment, vAlign As VerticalAlignment) As Point
    // Throw New NotImplementedException
    // End Function

    // <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    // Public Function CalulateChildOffset(sourceInstance As Rectangle, childSize As Size, hAlign As HorizontalAlignment, vAlign As VerticalAlignment) As Point
    // Throw New NotImplementedException
    // End Function

    // <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    // Public Function CalulateChildOffset(sourceInstance As Size, childSize As Size, hAlign As HorizontalAlignment, vAlign As VerticalAlignment) As Point
    // Throw New NotImplementedException
    // End Function

    // <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    // Public Function CalulateChildOffset(sourceInstance As Rectangle, childRect As Rectangle, percentFromLeft As Double, percentFromTop As Double) As Point
    // Throw New NotImplementedException
    // End Function

    // <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    // Public Function CalulateChildOffset(sourceInstance As Size, childRect As Rectangle, percentFromLeft As Double, percentFromTop As Double) As Point
    // Throw New NotImplementedException
    // End Function

    // <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    // Public Function CalulateChildOffset(sourceInstance As Rectangle, childSize As Size, percentFromLeft As Double, percentFromTop As Double) As Point
    // Throw New NotImplementedException
    // End Function

    // <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    // Public Function CalulateChildOffset(sourceInstance As Size, childSize As Size, percentFromLeft As Double, percentFromTop As Double) As Point
    // Throw New NotImplementedException
    // End Function

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static SizeF CalculateStringSize(this Font f, string text) {
      if (_G is null) {
        _G = Graphics.FromImage(new Bitmap(10, 10));
      }
      return _G.MeasureString(text, f);
    }
    private static Graphics _G = default;

    #region  for Image Types 

    //[EditorBrowsable(EditorBrowsableState.Always)]
    //public static Icon ToIcon(this Image sourceImage) {
    //  if (sourceImage is null) {
    //    return default;
    //  }
    //  var bmp = new Bitmap(sourceImage);
    //  return Icon.FromHandle(bmp.GetHicon);
    //}

    // <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    // Public Function ToPngImage(sourceIcon As Icon) As Image
    // If (sourceIcon Is Nothing) Then
    // Return Nothing
    // End If

    // Throw New NotImplementedException

    // End Function

    #region  Image Modifications 

    public enum RotationAngle : int {
      Angle90Left = 270,
      Angle90Right = 90,
      Angle180 = 180,
      Angle270Left = 90,
      Angle270Right = 270
    }

    public enum FlippingMode : int {
      Horizontal = 1,
      Vertical = 2
    }

    // <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    // Public Function GetRotatedVersion(sourceInstance As System.Drawing.Image, angle As RotationAngle) As System.Drawing.Image
    // Throw New NotImplementedException
    // End Function

    // <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    // Public Function GetFilppedVersion(sourceInstance As System.Drawing.Image, mode As FlippingMode) As System.Drawing.Image
    // Throw New NotImplementedException
    // End Function

    // <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    // Public Function GetDisabledVersion(sourceInstance As System.Drawing.Image) As System.Drawing.Image
    // Throw New NotImplementedException
    // End Function

    // <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    // Public Function GetOverlayedVersion(sourceInstance As System.Drawing.Image, overlayImage As System.Drawing.Image) As System.Drawing.Image
    // Throw New NotImplementedException
    // End Function

    // <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    // Public Function GetTransparentVersion(sourceInstance As System.Drawing.Image, transparencyColor As System.Drawing.Color) As System.Drawing.Image
    // Throw New NotImplementedException
    // End Function

    // <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    // Public Function GetOverlayedVersion(sourceInstance As System.Drawing.Image, overlayImage As System.Drawing.Image, overlayPosition As System.Drawing.Rectangle) As System.Drawing.Image
    // Throw New NotImplementedException
    // End Function

    // <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    // Public Function GetResizedVersion(sourceInstance As System.Drawing.Image, maxWidth As Integer, maxHeight As Integer) As System.Drawing.Image
    // Throw New NotImplementedException
    // End Function

    // <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    // Public Function GetResizedVersion(sourceInstance As System.Drawing.Image, maxWidth As Integer, maxHeight As Integer, minWidth As Integer, minHeight As Integer) As System.Drawing.Image
    // Throw New NotImplementedException
    // End Function

    #endregion

    #region  Export To File 

    // <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    // Public Sub SaveJpgFile(sourceInstance As System.Drawing.Image, fileName As String, qualityPercent As Integer)
    // Throw New NotImplementedException
    // End Sub

    // <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    // Public Sub SavePngFile(sourceInstance As System.Drawing.Image, fileName As String, qualityPercent As Integer)
    // Throw New NotImplementedException
    // End Sub

    // <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    // Public Sub SaveGifFile(sourceInstance As System.Drawing.Image, fileName As String, qualityPercent As Integer)
    // Throw New NotImplementedException
    // End Sub

    // <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    // Public Sub SaveBmpFile(sourceInstance As System.Drawing.Image, fileName As String)
    // Throw New NotImplementedException
    // End Sub

    // <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    // Public Sub SaveIcoFile(sourceInstance As System.Drawing.Image, fileName As String, qualityPercent As Integer)
    // Throw New NotImplementedException
    // End Sub

    #endregion

    // <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    // Public Function ToByteArray(sourceInstance As System.Drawing.Image) As Byte()
    // Throw New NotImplementedException
    // End Function

    // <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    // Public Sub LoadEmbeddedFile(ByRef sourceInstance As System.Drawing.Image, assembly As System.Reflection.Assembly, defaultNamespace As String, fileName As String)
    // Throw New NotImplementedException
    // End Sub

    #endregion

  }

}


#endif