using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace System.IO {

  internal static class ExtensionsForTextReader {

    [EditorBrowsable(EditorBrowsableState.Always), Obsolete("Kommt aus aus kSystemExtensions")]
    public static IEnumerable<string> AllLines(this TextReader source) {
      return GetLineIterator(source);
    }

    private static IEnumerable<string> GetLineIterator(TextReader source) {
      string currentLine;
      currentLine = source.ReadLine();
      while (currentLine != null) {
        yield return currentLine;
        currentLine = source.ReadLine();
      }
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static bool EndReached(this TextReader extendee) {
      return extendee.Peek() < 0;
    }

    //[EditorBrowsable(EditorBrowsableState.Advanced)]
    //public static char PeekChar(this TextReader extendee) {
    //  return Convert.ToChar(extendee.Peek());
    //}

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static char ReadChar(this TextReader extendee) {
      return Convert.ToChar(extendee.Read());
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static void SkipUntil(this TextReader extendee, string searchPattern) {
      int patternIndex = 0;
      char currentChar;

      while (!extendee.EndReached()) {
        currentChar = extendee.ReadChar();
        if (currentChar == searchPattern[patternIndex]) {
          patternIndex += 1;
          if (patternIndex == searchPattern.Length) {
            return;
          }
        }
        else {
          patternIndex = 0;
        }
      }

    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static string ReadUntil(this TextReader extendee, string searchPattern, bool removePattern) {
      var buffer = new StringBuilder();
      int patternIndex = 0;
      char currentChar;
      bool patternFound = false;

      while (!extendee.EndReached() && !patternFound) {
        currentChar = extendee.ReadChar();
        buffer.Append(currentChar);
        if (currentChar == searchPattern[patternIndex]) {
          patternIndex += 1;
          if (patternIndex == searchPattern.Length) {
            patternFound = true;
          }
        }
        else {
          patternIndex = 0;
        }
      }

      if (patternFound && removePattern) {
        return buffer.ToString(0, buffer.Length - searchPattern.Length);
      }
      else {
        return buffer.ToString();
      }

    }

    //[EditorBrowsable(EditorBrowsableState.Advanced)]
    //public static IEnumerable<string> Tokenize(this TextReader extendee, string tokenBegin, string tokenEnd, bool includeTokensOnly = false) {
    //  bool inToken = false;
    //  while (!extendee.EndReached()) {
    //    if (inToken) {
    //      yield return extendee.ReadUntil(tokenEnd, true);
    //      inToken = false;
    //    }
    //    else {
    //      yield return extendee.ReadUntil(tokenBegin, true);
    //      inToken = true;
    //    }
    //  }
    //}

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static void Tokenize(this TextReader extendee, string tokenBegin, string tokenEnd, Action<string> tokenReceiver, Action<string> outerRangeReceiver = null) {
      bool inToken = false;
      var tokens = new List<string>();
      while (!extendee.EndReached()) {
        if (inToken) {
          tokenReceiver.Invoke(extendee.ReadUntil(tokenEnd, true));
          inToken = false;
        }
        else {
          if (outerRangeReceiver != null) {
            outerRangeReceiver.Invoke(extendee.ReadUntil(tokenBegin, true));
          }
          else {
            extendee.SkipUntil(tokenBegin);
          }

          inToken = true;
        }
      }
    }

  }

}