using System;
using System.ComponentModel;
using System.Text;

namespace System {

  [Obsolete("Kommt aus aus kSystemExtensions")]
  internal static class ExtensionsForByte {

    //[EditorBrowsable(EditorBrowsableState.Always)]
    //public static long ToLong(this byte value) {
    //  return Convert.ToInt64(value);
    //}

    //[EditorBrowsable(EditorBrowsableState.Always)]
    //public static double ToDouble(this byte value) {
    //  return Convert.ToDouble(value);
    //}

    //[EditorBrowsable(EditorBrowsableState.Always)]
    //public static int ToInteger(this byte value) {
    //  return Convert.ToInt32(value);
    //}

    //[EditorBrowsable(EditorBrowsableState.Always)]
    //public static string ToString(this byte value, int minimumLength, char fillChar = '0') {
    //  return value.ToInteger().ToString(minimumLength, fillChar);
    //}

    //[EditorBrowsable(EditorBrowsableState.Always)]
    //public static char ToChar(this byte value) {
    //  return Encoding.ASCII.GetChars(new byte[] { value })[0];
    //}

    //[EditorBrowsable(EditorBrowsableState.Always)]
    //public static string ToString(this byte[] value, Encoding enc = null) {
    //  if (enc is null) {
    //    enc = Encoding.Default;
    //  }
    //  return enc.GetString(value);
    //}

    // <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    // Public Function EncodeHex(value As Byte) As String
    // '##################################
    // '  TODO: IMPLEMENT THIS METHOD !!!
    // Throw New NotImplementedException()
    // '##################################
    // End Function

    // <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    // Public Function EncodeHex(value As Byte()) As String
    // '##################################
    // '  TODO: IMPLEMENT THIS METHOD !!!
    // Throw New NotImplementedException()
    // '##################################
    // End Function

    // <Extension(), EditorBrowsable(EditorBrowsableState.Never), Obsolete("Use ToBase64String", True)>
    // Public Function EncodeB64(value As Byte) As String
    // Return Convert.ToBase64String({value})
    // End Function

    //[EditorBrowsable(EditorBrowsableState.Always)]
    //public static char[] ToCharArray(this byte[] value, Encoding encoding) {
    //  return encoding.GetChars(value);
    //}

    //[EditorBrowsable(EditorBrowsableState.Always)]
    //public static char[] EncodeBase64(this byte[] value) {
    //  return Convert.ToBase64String(value).ToCharArray();
    //}

    //[EditorBrowsable(EditorBrowsableState.Always)]
    //public static char[] EncodeHex(this byte[] value, bool upperCase = true) {
    //  return value.ToHexString(upperCase).ToCharArray();
    //}

    /// <summary>
    /// returns a lower hex string
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Always)]
    public static string ToHexString(this byte[] value, bool upperCase = false) {
      var sb = new StringBuilder();
      foreach (var b in value)
        sb.Append(b.ToString("x2"));
      if (upperCase) {
        return sb.ToString().ToUpper();
      }
      else {
        return sb.ToString();
      }
    }

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static string ToBase64String(this byte[] bytes) {
      return Convert.ToBase64String(bytes);
    }

    // <Extension(), EditorBrowsable(EditorBrowsableState.Never), Obsolete("Use ToBase64String", True)>
    // Public Function EncodeB64(value As Byte()) As String
    // Return Convert.ToBase64String(value)
    // End Function

  }

}