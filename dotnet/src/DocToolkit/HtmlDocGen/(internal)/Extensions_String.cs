using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace System {

  [Obsolete("Kommt aus aus kSystemExtensions")]
  internal static class ExtensionsForString {

    #region  Conversion 

    //[EditorBrowsable(EditorBrowsableState.Always)]
    //public static bool ToBoolean(this string value) {
    //  return value.ToInteger().ToBoolean();
    //}

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static byte ToByte(this string value) {
      byte result = 0;
      byte.TryParse(value, out result);
      return result;
    }

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static int ToInteger(this string value) {
      int result = 0;
      if (!int.TryParse(value, out result)) {
        switch (value.ToLower() ?? "") {
          case "true":
          case "wahr":
          case var @case when @case == (true.ToString().ToLower() ?? ""): {
              result = 1;
              break;
            }
          case "false":
          case "falsch":
          case var case1 when case1 == (false.ToString().ToLower() ?? ""): {
              result = 0;
              break;
            }
        }
      }
      return result;
    }

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static decimal ToDecimal(this string value) {
      decimal result = 0m;
      decimal.TryParse(value, out result);
      return result;
    }

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static byte[] ToBytes(this string value, Encoding enc = null) {
      if (enc == null) {
        enc = Encoding.Default;
      }
      return enc.GetBytes(value);
    }

    #endregion

    #region  Compare & Match 

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static bool MatchesMaskOrRegex(this string stringToEvaluate, string pattern, bool ignoreCasing = true) {
      if (pattern.StartsWith("^") && pattern.EndsWith("$")) {
        return stringToEvaluate.MatchesRegex(pattern, ignoreCasing);
      }
      else {
        return stringToEvaluate.MatchesWildcardMask(pattern, ignoreCasing);
      }
    }

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static bool MatchesRegex(this string stringToEvaluate, string pattern, bool ignoreCasing = true) {
      if (ignoreCasing) {
        return Regex.IsMatch(stringToEvaluate, pattern, RegexOptions.IgnoreCase);
      }
      else {
        return Regex.IsMatch(stringToEvaluate, pattern);
      }
    }

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static bool MatchesWildcardMask(this string stringToEvaluate, string pattern, bool ignoreCasing = true) {

      int indexOfDoubleDot = pattern.IndexOf("..", StringComparison.Ordinal);
      if (indexOfDoubleDot >= 0) {
        for (int i = indexOfDoubleDot, loopTo = pattern.Length - 1; i <= loopTo; i++) {
          if (!(pattern[i] == '.')) {
            return false;
          }
        }
      }

      string normalizedPatternString = Regex.Replace(pattern, @"\.+$", "");
      bool endsWithDot = !(normalizedPatternString.Length == pattern.Length);
      int endCharCount = 0;

      if (endsWithDot) {
        int lastNonWildcardPosition = normalizedPatternString.Length - 1;

        while (lastNonWildcardPosition >= 0) {
          char currentChar = normalizedPatternString[lastNonWildcardPosition];
          if (currentChar == '*') {
            endCharCount += short.MaxValue;
          }
          else if (currentChar == '?') {
            endCharCount += 1;
          }
          else {
            break;
          }
          lastNonWildcardPosition -= 1;
        }

        if (endCharCount > 0) {
          normalizedPatternString = normalizedPatternString.Substring(0, lastNonWildcardPosition + 1);
        }

      }

      bool endsWithWildcardDot = endCharCount > 0;
      bool endsWithDotWildcardDot = endsWithWildcardDot && normalizedPatternString.EndsWith(".");

      if (endsWithDotWildcardDot) {
        normalizedPatternString = normalizedPatternString.Substring(0, normalizedPatternString.Length - 1);
      }

      normalizedPatternString = Regex.Replace(normalizedPatternString, @"(?!^)(\.\*)+$", ".*");

      string escapedPatternString = Regex.Escape(normalizedPatternString);
      string prefix;
      string suffix;

      if (endsWithDotWildcardDot) {
        prefix = "^" + escapedPatternString;
        suffix = @"(\.[^.]{0," + endCharCount + "})?$";
      }
      else if (endsWithWildcardDot) {
        prefix = "^" + escapedPatternString;
        suffix = "[^.]{0," + endCharCount + "}$";
      }
      else {
        prefix = "^" + escapedPatternString;
        suffix = "$";
      }

      if (prefix.EndsWith(@"\.\*") && prefix.Length > 5) {
        prefix = prefix.Substring(0, prefix.Length - 4);
        suffix = Convert.ToString(@"(\..*)?") + suffix;
      }

      string expressionString = prefix.Replace(@"\*", ".*").Replace(@"\?", "[^.]?") + suffix;

      if (ignoreCasing) {
        return Regex.IsMatch(stringToEvaluate, expressionString, RegexOptions.IgnoreCase);
      }
      else {
        return Regex.IsMatch(stringToEvaluate, expressionString);
      }

    }

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static int CompareTo(this string strA, string strB, bool ignoreCasing) {
      return string.Compare(strA, strB, ignoreCasing);
    }

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static bool Equals(this string strA, string strB, bool ignoreCasing) {
      if (ignoreCasing) {
        return string.Equals(strA, strB, StringComparison.InvariantCultureIgnoreCase);
      }
      else {
        return string.Equals(strA, strB);
      }
    }

    // HAT BUG!!!
    // <Extension(), EditorBrowsable(EditorBrowsableState.Always), Obsolete("Use ")>
    // Public Function MatchesToWcPattern(stringToEvaluate As String, wildcardPattern As String, Optional wildcardChar As Char = "*"c, Optional ignoreCasing As Boolean = True) As Boolean
    // Dim patternParts As String()
    // Dim currentPosition As Integer = 0

    // If (ignoreCasing) Then
    // wildcardPattern = wildcardPattern.ToLower()
    // stringToEvaluate = stringToEvaluate.ToLower()
    // End If

    // patternParts = wildcardPattern.Split(wildcardChar)

    // If (String.IsNullOrEmpty(wildcardPattern)) Then
    // Return False
    // End If

    // If (Not String.IsNullOrEmpty(patternParts(0)) AndAlso Not stringToEvaluate.StartsWith(patternParts(0))) Then
    // Return False
    // End If

    // For Each part As String In patternParts
    // If (Not String.IsNullOrEmpty(part)) Then

    // Dim foundIndex As Integer
    // foundIndex = stringToEvaluate.Substring(currentPosition, stringToEvaluate.Length - currentPosition).IndexOf(part)
    // If (foundIndex >= 0) Then
    // currentPosition += foundIndex + part.Length
    // Else
    // Return False
    // End If

    // End If
    // Next

    // Return (currentPosition = stringToEvaluate.Length OrElse String.IsNullOrEmpty(patternParts(patternParts.Length - 1)))
    // End Function

    // <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    // Public Function MatchesToRegex(stringToEvaluate As String, regexPattern As String, Optional ignoreCasing As Boolean = True) As Boolean
    // If (ignoreCasing) Then
    // Return Regex.IsMatch(stringToEvaluate, regexPattern, RegexOptions.IgnoreCase)
    // Else
    // Return Regex.IsMatch(stringToEvaluate, regexPattern)
    // End If
    // End Function

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static IEnumerable<string> FilterByMaskOrRegex(this IEnumerable<string> value, string wildcardPattern, bool ignoreCasing = true) {
      return from item in value
             where item.MatchesMaskOrRegex(wildcardPattern, ignoreCasing)
             select item;
    }

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static IEnumerable<string> FilterByWildcardMask(this IEnumerable<string> value, string wildcardPattern, bool ignoreCasing = true) {
      return from item in value
             where item.MatchesWildcardMask(wildcardPattern, ignoreCasing)
             select item;
    }

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static IEnumerable<string> FilterByRegex(this IEnumerable<string> value, string regexPattern, bool ignoreCasing = true) {
      return from item in value
             where item.MatchesRegex(regexPattern, ignoreCasing)
             select item;
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static Regex CompileToRegex(this string regexPattern, bool ignoreCasing) {
      RegexOptions regexFlags;

      if (ignoreCasing) {
        regexFlags = RegexOptions.Compiled | RegexOptions.IgnoreCase;
      }
      else {
        regexFlags = RegexOptions.Compiled;
      }

      return new Regex(regexPattern, regexFlags);
    }

    // Private allCharsRegex As String = "[A-Za-z0-9ÄÖÜäöüß\]" & Regex.Escape("[)(}{,;.:_&!=-\/%""$+*?") & "]"
    // Private Function ConvertWildcardPatternToRegexPattern(wildCardPattern As String) As String

    // wildCardPattern = wildCardPattern.Replace("%", "ANY")
    // wildCardPattern = wildCardPattern.Replace("*", "ANY")
    // wildCardPattern = wildCardPattern.Replace("+", "ONE_OR_MORE")
    // wildCardPattern = wildCardPattern.Replace("?", "NONE_OR_ONE")

    // wildCardPattern = Regex.Escape(wildCardPattern).Replace("]", "\]") '<BUG-FIX

    // wildCardPattern = wildCardPattern.Replace("ANY", allCharsRegex & "*")
    // wildCardPattern = wildCardPattern.Replace("ONE_OR_MORE", allCharsRegex & "+")
    // wildCardPattern = wildCardPattern.Replace("NONE_OR_ONE", allCharsRegex & "?")

    // Return "^" & wildCardPattern & "$"
    // End Function




    // Private Const RegexCharsToTerminate As String = "?+:()[].\^$*"
    // Private Const RegexTerminatorChar As Char = "\"c
    // Private Const RegexSuffixString As String = "^("
    // Private Const RegexPrefixString As String = ")$"

    // <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    // Public Function MatchesWildcardPattern(input As String, wildcardPattern As String, Optional wildcardRegex As String = "([0-9]|[A_Z]|[a-z]|[ ])*", Optional wildcardChar As Char = "*"c) As Boolean
    // Dim wildcardPatternParts As String()
    // Dim regexPattern As String

    // If (Not wildcardPattern.Contains(wildcardChar)) Then
    // wildcardPattern = wildcardChar & wildcardPattern & wildcardChar
    // End If
    // wildcardPatternParts = wildcardPattern.Split(wildcardChar)

    // For Each regexCharToTerminate As Char In RegexCharsToTerminate
    // For i As Integer = 0 To (wildcardPatternParts.Length - 1)
    // wildcardPatternParts(i) = wildcardPatternParts(i).Replace(regexCharToTerminate, RegexTerminatorChar & regexCharToTerminate)
    // Next
    // Next
    // regexPattern = RegexSuffixString & String.Join(wildcardRegex, wildcardPatternParts) & RegexPrefixString

    // Return input.MatchesRegex(regexPattern)
    // End Function

    // <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
    // Public Function MatchesRegex(input As String, regexPattern As String) As Boolean
    // Return System.Text.RegularExpressions.Regex.IsMatch(input, regexPattern)
    // End Function

    #endregion

    #region  Cut & Trim 

    /// <summary>
    /// Determines whether the string instance ends with the specified suffix; otherwise, the suffix will be added to the string.
    /// </summary>
    /// <param name="extendee">The existing data type to be extended.</param>
    /// <param name="suffix">The suffix for the string instance.</param>
    /// <returns>A string instance including the suffix.</returns>
    /// <remarks></remarks>
    [EditorBrowsable(EditorBrowsableState.Always)]
    public static string EnsureEndsWith(this string extendee, string suffix) {
      if (!string.IsNullOrEmpty(extendee)) {
        if (extendee.EndsWith(suffix)) {
          return extendee;
        }
      }
      return extendee + suffix;
    }

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static string CutBeginUntil(this string extendee, string edge, bool removeEdge) {
      int edgeIndex = extendee.IndexOf(edge);
      if (edgeIndex < 0) {
        return extendee;
      }
      if (removeEdge) {
        return extendee.Substring(edgeIndex, extendee.Length - edgeIndex - edge.Length);
      }
      else {
        return extendee.Substring(edgeIndex, extendee.Length - edgeIndex);
      }
    }

    /// <summary>
    /// Appends an 'NewLine' string to the original String
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Always)]
    public static string NewLine(ref string extendee, int count = 1) {
      switch (count) {
        case var @case when @case < 1: {
            return extendee;
          }
        case 1: {
            return extendee + Environment.NewLine;
          }

        default: {
            string nl = Environment.NewLine;
            return extendee + Duplicate(ref nl, count);
          }
      }
    }

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static string Duplicate(ref string extendee, int count) {
      switch (count) {
        case var @case when @case < 1: {
            return string.Empty;
          }
        case 1: {
            return extendee;
          }
        case 2: {
            return extendee + extendee;
          }
        case 3: {
            return extendee + extendee + extendee;
          }

        default: {
            var sb = new StringBuilder();
            for (int i = 1, loopTo = count; i <= loopTo; i++)
              sb.Append(extendee);
            return sb.ToString();
          }
      }
    }

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static string CutEndUntil(this string extendee, string edge, bool removeEdge) {
      int edgeIndex = extendee.LastIndexOf(edge);
      if (edgeIndex < 0) {
        return extendee;
      }
      if (removeEdge) {
        return extendee.Substring(0, edgeIndex);
      }
      else {
        return extendee.Substring(0, edgeIndex + edge.Length);
      }
    }

    #endregion

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static string FixupLinebreaks(this string stringWithAnyLinebreaks) {
      return stringWithAnyLinebreaks.FixupLinebreaks(Environment.NewLine);
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static string FixupLinebreaks(this string stringWithAnyLinebreaks, string targetLinebreak) {
      var rdr = new StringReader(stringWithAnyLinebreaks);
      var sb = new StringBuilder(stringWithAnyLinebreaks.Length);
      bool first = true;
      foreach (var line in rdr.AllLines()) {
        if (first) {
          first = false;
        }
        else {
          sb.Append(targetLinebreak);
        }
        sb.Append(line);
      }
      return sb.ToString();
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static string BuildFormatString(this string stringWithPlaceholders, params string[] placeholderKeys) {
      var sb = new StringBuilder();
      sb.Append(stringWithPlaceholders);

      int index = 0;
      foreach (var phk in placeholderKeys) {
        sb.Replace(phk, "{" + index.ToString() + "}");
        index += 1;
      }

      return sb.ToString();
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static string InsertLinebreaks(this string extendee, int charcount) {
      var sb = new StringBuilder();
      foreach (var lineIt in extendee.Lines()) {
        string line = lineIt;
        while (line.Length > charcount) {
          sb.AppendLine(line.Substring(0, charcount));
          line = line.Substring(charcount);
        }
        sb.AppendLine(line);
      }
      return sb.ToString();
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static string FormatAsKey(this string extendee, int blockSize = 5) {
      var sb = new StringBuilder();
      var currentBlockSize = default(int);
      for (int i = 0, loopTo = extendee.Length - 1; i <= loopTo; i++) {
        if (currentBlockSize == blockSize) {
          sb.Append('-');
          currentBlockSize = 0;
        }
        sb.Append(extendee[i]);
        currentBlockSize += 1;
      }
      return sb.ToString().ToUpper();
    }

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static bool TryParse<TTargetType>(this string sourceText, ref TTargetType target) {
      object result = null;
      if (typeof(TTargetType).TryParse(sourceText, ref result)) {
        target = (TTargetType)result;
        return true;
      }
      else {
        return false;
      }
    }

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static bool TryParse(this string sourceText, Type targetType, ref object target) {
      return targetType.TryParse(sourceText, ref target);
    }

    /// <summary>
    /// If the source String is Empty or WhiteSpace, then Nothing will be returned. Otherwise the source String will be returned.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static string NothingIfEmpty(this string extendee) {
      if (string.IsNullOrWhiteSpace(extendee)) {
        return null;
      }
      else {
        return extendee;
      }
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static string SubStringBefore(this string extendee, string searchString) {
      int idx = extendee.IndexOf(searchString);
      if (idx >= 0) {
        return extendee.Substring(0, idx);
      }
      else {
        return extendee;
      }
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public static string SubStringAfter(this string extendee, string searchString) {
      int idx = extendee.IndexOf(searchString);
      if (idx >= 0) {
        return extendee.Substring(idx + 1, extendee.Length - idx - 1);
      }
      else {
        return string.Empty;
      }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="str"></param>
    /// <param name="length"></param>
    /// <param name="cutter">The cutter will be prepended at the left size of the returned string, when the stric was cutted. use this to apply some dots for example ("...my string")</param>
    [EditorBrowsable(EditorBrowsableState.Always)]
    public static string Right(this string str, int length, string cutter = null) {
      int overFlow = str.Length - length;
      if (overFlow > 0) {
        if (cutter == null) {
          return str.Substring(overFlow, str.Length - overFlow);
        }
        else {
          overFlow += cutter.Length;
          return cutter + str.Substring(overFlow, str.Length - overFlow);
        }
      }
      else {
        return str;
      }
    }

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static bool IsNullOrEmpty(this string anyString) {
      return string.IsNullOrEmpty(anyString);
    }

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static bool IsNullOrWhiteSpace(this string anyString) {
      return string.IsNullOrWhiteSpace(anyString);
    }

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static bool IsNotNullOrEmpty(this string anyString) {
      return !string.IsNullOrEmpty(anyString);
    }

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static bool IsNotNullOrWhiteSpace(this string anyString) {
      return !string.IsNullOrWhiteSpace(anyString);
    }

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static char[] ToCharArray(this string str) {
      return str.ToArray();
    }

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static string FormatWith(this string stringWithPlaceholders, params object[] values) {
      return string.Format(stringWithPlaceholders, values);
    }

    /* TODO ERROR: Skipped IfDirectiveTrivia
    #If NET461 Then
    *//* TODO ERROR: Skipped DisabledTextTrivia

        <Obsolete("use 'GetMD5HashAsHex()' instead!")>
        <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
        Public Function MD5(value As String) As String
          Using md5Provider As New System.Security.Cryptography.MD5CryptoServiceProvider

            Dim stringBytes As Byte()
            Dim hashBytes As Byte()
            Dim tmp As String = ""
            Dim sb As New StringBuilder

            stringBytes = Encoding.ASCII.GetBytes(value)
            hashBytes = md5Provider.ComputeHash(stringBytes)

            For i As Integer = 0 To hashBytes.Length - 1
              tmp = Microsoft.VisualBasic.Hex(hashBytes(i))
              If (tmp.Length = 1) Then
                sb.Append("0")
              End If
              sb.Append(tmp)
            Next

            Return sb.ToString().ToLower()
          End Using

        End Function

    *//* TODO ERROR: Skipped EndIfDirectiveTrivia
    #End If
    */
    /// <summary>
    /// a lower case hex representation of the MD5 hash for the given string (ASCII mode)
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Always)]
    public static string GetMD5HashAsHex(this string value) {
      using (var md5Provider = new MD5CryptoServiceProvider()) {

        byte[] stringBytes;
        byte[] hashBytes;
        string tmp = "";
        var sb = new StringBuilder();

        stringBytes = Encoding.ASCII.GetBytes(value);
        hashBytes = md5Provider.ComputeHash(stringBytes);
        return hashBytes.ToHexString();

      }

    }

    /// <summary>
    /// a ASCII representation of the MD5 hash for the given string (ASCII mode)
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Always)]
    public static string GetMD5Hash(this string value) {
      using (var md5Provider = new MD5CryptoServiceProvider()) {

        byte[] stringBytes;
        byte[] hashBytes;
        string tmp = "";
        var sb = new StringBuilder();

        stringBytes = Encoding.ASCII.GetBytes(value);
        hashBytes = md5Provider.ComputeHash(stringBytes);
        return Encoding.ASCII.GetString(hashBytes);
      }

    }

    /// <summary>
    /// a lower case hex representation of the SHA256 hash for the given string (ASCII mode)
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Always)]
    public static string GetSHA256HashAsHex(this string value) {
      using (var sha256Provider = SHA256.Create()) {

        byte[] stringBytes;
        byte[] hashBytes;
        string tmp = "";
        var sb = new StringBuilder();

        stringBytes = Encoding.ASCII.GetBytes(value);
        hashBytes = sha256Provider.ComputeHash(stringBytes);
        return hashBytes.ToHexString();

      }

    }

    #region  Multifield Manipulation 

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static string Join(this IEnumerable<string> value, string separator = "") {
      var sb = new StringBuilder();
      bool first = true;
      foreach (string s in value) {
        if (first) {
          first = false;
        }
        else {
          sb.Append(separator);
        }
        sb.Append(s);
      }
      return sb.ToString();
    }

//    [EditorBrowsable(EditorBrowsableState.Always)]
//    public static IEnumerable<string> AllToLower(this IEnumerable<string> value) {
//      return new Collections.Generic.EnumerableProxy<string, string>(value, s => s.ToLower());
//    }

//    [EditorBrowsable(EditorBrowsableState.Always)]
//    public static IEnumerable<string> AllToUpper(this IEnumerable<string> value) {
//      return new Collections.Generic.EnumerableProxy<string, string>(value, s => s.ToUpper());
//    }

//    [EditorBrowsable(EditorBrowsableState.Always)]
//    public static IEnumerable<string> TrimAll(this IEnumerable<string> value) {
//      return new Collections.Generic.EnumerableProxy<string, string>(value, s => s.Trim());
//    }

//    [EditorBrowsable(EditorBrowsableState.Always)]
//    public static IEnumerable<int> ToInteger(this IEnumerable<string> value) {
//      return value.
//#error Conversion error: Could not convert all type parameters, so they've been commented out. Inferred type may be different
//TransformTo/* <int> */(s => int.Parse(s));
//    }

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static IEnumerable<string> RemoveNullOrEmpty(this IEnumerable<string> value) {
      return from s in value
             where !string.IsNullOrEmpty(s)
             select s;
    }

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static IEnumerable<string> RemoveNullOrWhiteSpace(this IEnumerable<string> value) {
      return from s in value
             where !string.IsNullOrWhiteSpace(s)
             select s;
    }

    #endregion

    #region  Enumerable Lines 

    /// <summary>
    /// returns an ienumerable which allows to iterate over all lines of the string
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Always)]
    public static IEnumerable<string> Lines(this string extendee, bool keepEmptyLines = true) {
      return new StringLineEnumerable(ref extendee, keepEmptyLines);
    }

    #region  Enumerable 

    private class StringLineEnumerable : IEnumerable<string> {

      private string _FullString;
      private bool _KeepEmptyLines;

      public StringLineEnumerable(ref string basedOn, bool keepEmptyLines) {
        _FullString = basedOn;
        _KeepEmptyLines = keepEmptyLines;
      }

      private StringReader CreateNewStringReader() {
        return new StringReader(_FullString);
      }

      public IEnumerator<string> GetEnumerator() {
        return new StringLineEnumerator(this.CreateNewStringReader, _KeepEmptyLines);
      }

      public IEnumerator GetUntypedEnumerator() {
        return new StringLineEnumerator(this.CreateNewStringReader, _KeepEmptyLines);
      }

      IEnumerator IEnumerable.GetEnumerator() => this.GetUntypedEnumerator();

      #region  Enumerator 

      private class StringLineEnumerator : IEnumerator<string> {

        public delegate StringReader ReaderInitialisationMethod();

        private string _CurrentLine = null;
        private StringReader _Iterator = null;
        private ReaderInitialisationMethod _Initializer;
        private bool _KeepEmptyLines;

        public StringLineEnumerator(ReaderInitialisationMethod initializer, bool keepEmptyLines) {
          _Initializer = initializer;
          _KeepEmptyLines = keepEmptyLines;
        }

        public string Current {
          get {
            return _CurrentLine;
          }
        }

        private object UntypedCurrent {
          get {
            return _CurrentLine;
          }
        }

        object IEnumerator.Current { get => this.UntypedCurrent; }

        public bool MoveNext() {
          if (_Iterator == null) {
            this.Reset();
          }

          _CurrentLine = _Iterator.ReadLine();

          if (!_KeepEmptyLines) {
            while (_CurrentLine != null && string.IsNullOrWhiteSpace(_CurrentLine))
              _CurrentLine = _Iterator.ReadLine();
          }

          return _CurrentLine != null;
        }

        public void Reset() {
          _Iterator = _Initializer.Invoke();
          _CurrentLine = null;
        }

        #region  Dispose 

        [EditorBrowsable(EditorBrowsableState.Never)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _DisposedValue;

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        protected virtual void Dispose(bool disposing) {
          if (!_DisposedValue) {
            if (disposing) {
              // MANAGED
            }
            // UNMANAGED
          }
          _DisposedValue = true;
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public void Dispose() {
          this.Dispose(true);
          GC.SuppressFinalize(this);
        }

        #endregion

      }

      #endregion

    }

    #endregion

    #endregion

    #region  Split & Join 

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static IEnumerable<string> Split(this string extendee, string separator) {
      return new StringSplitEnumerable(ref extendee, ref separator);
    }

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static string[] SplitEscaped(this string extendee, string separator, bool keepBracketsInResult = false) {

      return extendee.SplitEscaped(separator, "\"", "\"", '\\', keepBracketsInResult);
    }

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static string[] SplitEscaped(this string extendee, string separator, string blockStartBracket, string blockEndBracket, char bracketEscapeChar = '\\', bool keepBracketsInResult = false) {

      int ubound = extendee.Length - 1;
      int currentIndex = 0;
      var completeTokens = new List<string>();
      bool escapingActive = false;
      bool currentlyInBrackets = false;
      var currentTokenSb = new StringBuilder();

      while (currentIndex <= ubound) {

        if (!escapingActive && extendee[currentIndex] == bracketEscapeChar) {
          escapingActive = true;
          // initiates a suspended write (only if escape was effective)
          currentIndex += 1;
        }
        else {

          if (!currentlyInBrackets) {

            if (currentIndex + blockStartBracket.Length - 1 <= ubound && (extendee.Substring(currentIndex, blockStartBracket.Length) ?? "") == (blockStartBracket ?? "")) {

              if (!escapingActive) {
                // BLOCK START
                currentlyInBrackets = true;
                if (keepBracketsInResult) {
                  currentTokenSb.Append(blockStartBracket);
                }
              }
              else {
                currentTokenSb.Append(blockStartBracket);
              }
              currentIndex += blockStartBracket.Length;
            }

            else if (currentIndex + separator.Length - 1 <= ubound && (extendee.Substring(currentIndex, separator.Length) ?? "") == (separator ?? "")) {

              if (!escapingActive) {
                // SPLIT
                completeTokens.Add(currentTokenSb.ToString());
                currentTokenSb.Clear();
              }
              else {
                currentTokenSb.Append(separator);
              }
              currentIndex += separator.Length;
            }

            else {

              if (escapingActive && !(extendee[currentIndex] == bracketEscapeChar)) {
                // RE-APPLY SUSPENDED ESCAPE CHAR, BECAUSE ESCAPING WAS NOT INTENDED
                currentTokenSb.Append(bracketEscapeChar);
              }
              // APPLY REGULAR CHAR
              currentTokenSb.Append(extendee[currentIndex]);
              currentIndex += 1;

            }
          }


          else if (currentIndex + blockEndBracket.Length - 1 <= ubound && (extendee.Substring(currentIndex, blockEndBracket.Length) ?? "") == (blockEndBracket ?? "")) { // IF CURRENTLY IN BRACKETS
            bool nextComesSeparator = currentIndex + separator.Length <= ubound && (extendee.Substring(currentIndex + 1, separator.Length) ?? "") == (separator ?? "");

            if (!escapingActive && (currentIndex == ubound || nextComesSeparator)) {
              // BLOCK END
              currentlyInBrackets = false;
              if (keepBracketsInResult) {
                currentTokenSb.Append(blockEndBracket);
              }
            }
            else {
              currentTokenSb.Append(blockEndBracket);
            }
            currentIndex += blockEndBracket.Length;
          }

          else {

            if (escapingActive && !(extendee[currentIndex] == bracketEscapeChar)) {
              // RE-APPLY SUSPENDED ESCAPE CHAR, BECAUSE ESCAPING WAS NOT INTENDED
              currentTokenSb.Append(bracketEscapeChar);
            }

            // APPLY REGULAR CHAR
            currentTokenSb.Append(extendee[currentIndex]);
            currentIndex += 1;


          }

          escapingActive = false;
        } // reset escaping flag
      }

      if (escapingActive) {
        // ESCAPING WAS NOT INTENDED (THE ESCAPE CHAR WAS THE LAST ONE)
        currentTokenSb.Append(bracketEscapeChar);
      }

      if (currentTokenSb.Length > 0) {
        // complete the last token
        completeTokens.Add(currentTokenSb.ToString());
      }

      return completeTokens.ToArray();
    }

    //[EditorBrowsable(EditorBrowsableState.Always)]
    //public static string JoinEscaped(this string[] extendee, string separator, bool generateBracketsAlsoIfNotNecessary = false) {

    //  return extendee.JoinEscaped(separator, "\"", "\"", '\\', generateBracketsAlsoIfNotNecessary);
    //}

    //[EditorBrowsable(EditorBrowsableState.Always)]
    //public static string JoinEscaped(this string[] extendee, string separator, string blockStartBracket, string blockEndBracket, char bracketEscapeChar = '\\', bool generateBracketsAlsoIfNotNecessary = false) {

    //  var sb = new StringBuilder();
    //  bool isFirst = true;

    //  foreach (var token in extendee) {

    //    if (isFirst) {
    //      isFirst = false;
    //    }
    //    else {
    //      sb.Append(separator);
    //    }
    //    bool writeBrackets = generateBracketsAlsoIfNotNecessary || token.Contains(separator);
    //    token = token.Replace(blockStartBracket, Conversions.ToString(bracketEscapeChar) + blockStartBracket);

    //    if (writeBrackets) {
    //      sb.Append(blockStartBracket);
    //      if ((blockStartBracket ?? "") != (blockEndBracket ?? "")) {
    //        token = token.Replace(blockEndBracket, Conversions.ToString(bracketEscapeChar) + blockEndBracket);
    //      }
    //    }

    //    sb.Append(token);

    //    if (writeBrackets) {
    //      sb.Append(blockEndBracket);
    //    }

    //  }

    //  return sb.ToString();
    //}

    #region  Enumerable 

    private class StringSplitEnumerable : IEnumerable<string> {

      private string _FullString;
      private string _Separator;

      public StringSplitEnumerable(ref string basedOn, ref string separator) {
        _FullString = basedOn;
        _Separator = separator;
      }

      public IEnumerator<string> GetEnumerator() {
        return new StringLineEnumerator(ref _FullString, _Separator);
      }

      public IEnumerator GetEnumeratorUntyped() {
        return new StringLineEnumerator(ref _FullString, _Separator);
      }

      IEnumerator IEnumerable.GetEnumerator() => this.GetEnumeratorUntyped();

      #region  Enumerator 

      private class StringLineEnumerator : IEnumerator<string> {

        private string _Separator;
        private int _SeparatorLength;
        private string _CurrentPart = null;
        private int _LastIndex = 0;
        private bool _EndReached = false;
        private string _FullString;

        public StringLineEnumerator(ref string basedOn, string separator) {
          _FullString = basedOn;
          _Separator = separator;
          _SeparatorLength = _Separator.Length;
        }

        public string Current {
          get {
            return _CurrentPart;
          }
        }

        private object UntypedCurrent {
          get {
            return _CurrentPart;
          }
        }

        object IEnumerator.Current { get => this.UntypedCurrent; }

        public bool MoveNext() {
          int foundIndex;

          if (_EndReached) {
            return false;
          }
          else {
            foundIndex = _FullString.IndexOf(_Separator, _LastIndex);
          }

          if (foundIndex < 0) {
            _EndReached = true;
          }
          else {
            _CurrentPart = _FullString.Substring(_LastIndex, foundIndex - _LastIndex);
            _LastIndex = foundIndex + _SeparatorLength;
          }

          return true;
        }

        public void Reset() {
          _LastIndex = 0;
          _EndReached = false;
          _CurrentPart = null;
        }

        #region  Dispose 

        [EditorBrowsable(EditorBrowsableState.Never)]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _DisposedValue;

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        protected virtual void Dispose(bool disposing) {
          if (!_DisposedValue) {
            if (disposing) {
              // MANAGED
            }
            // UNMANAGED
          }
          _DisposedValue = true;
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public void Dispose() {
          this.Dispose(true);
          GC.SuppressFinalize(this);
        }

        #endregion

      }

      #endregion

    }

    #endregion

    #endregion

    #region  Invoking Action(Of String)

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static void InvokeNewLine(this Action<string> target) {
      target.Invoke(Environment.NewLine);
    }

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static void InvokeNewLine(this Action<string> target, string @param) {
      target.Invoke(@param + Environment.NewLine);
    }

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static void InvokeNewLine(this Action<string> target, string format, params object[] values) {
      target.Invoke(string.Format(format, values) + Environment.NewLine);
    }

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static void Invoke(this Action<string> target, string format, params object[] values) {
      target.Invoke(string.Format(format, values));
    }

    #endregion

    ///// <summary>
    ///// Makes the string orderable by included numbers. instead of ["X1_K", "X10_K", "X2_K"] (wrong order) we will get ["X001_K", "X002_K", "X010_K"] (correct order) when specifing 'numberlength'=3.
    ///// </summary><param name="numberlength">should at least as long as the max possible length of numbers in the input string</param>
    //[EditorBrowsable(EditorBrowsableState.Advanced)]
    //public static string ToNumericOrderable(this string input, int numberlength = 7) {
    //  if (string.IsNullOrWhiteSpace(input)) {
    //    return input;
    //  }
    //  string[] separated = input.SeparateNumericBlocks();
    //  string numberLengthFormatstring = new string('0', numberlength);
    //  for (int i = 0, loopTo = separated.Length - 1; i <= loopTo; i++) {
    //    if (separated[i].Length > 0 && char.IsNumber(separated[i][0])) {
    //      separated[i] = int.Parse(separated[i]).ToString(numberLengthFormatstring);
    //    }
    //  }
    //  return string.Join("", separated);
    //}

    //[EditorBrowsable(EditorBrowsableState.Advanced)]
    //public static string[] SeparateNumericBlocks(this string input) {

    //  if (string.IsNullOrEmpty(input)) {
    //    return new[] { string.Empty };
    //  }

    //  var result = new string[1];
    //  int currentIndex = 0;
    //  bool currentlyAlpha = !char.IsNumber(input[0]);

    //  foreach (var c in input) {
    //    bool isAlpha = !char.IsNumber(c);
    //    if (!(currentlyAlpha == isAlpha)) {
    //      currentlyAlpha = isAlpha;
    //      currentIndex += 1;
    //      Array.Resize(ref result, currentIndex + 1);
    //    }
    //    result[currentIndex] += Conversions.ToString(c);
    //  }

    //  return result;
    //}

    //[EditorBrowsable(EditorBrowsableState.Always)]
    //public static string FirstToUpper(this string extendee) {
    //  if (string.IsNullOrWhiteSpace(extendee) || !char.IsLetter(extendee[0])) {
    //    return string.Empty;
    //  }
    //  if (char.IsLower(extendee[0])) {
    //    extendee = Conversions.ToString(char.ToUpper(extendee[0])) + extendee.Substring(1);
    //  }
    //  return extendee;
    //}

    //[EditorBrowsable(EditorBrowsableState.Always)]
    //public static string FirstToLower(this string extendee) {
    //  if (string.IsNullOrWhiteSpace(extendee) || !char.IsLetter(extendee[0])) {
    //    return string.Empty;
    //  }
    //  if (char.IsUpper(extendee[0])) {
    //    extendee = Conversions.ToString(char.ToLower(extendee[0])) + extendee.Substring(1);
    //  }
    //  return extendee;
    //}

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static string ToCamelCase(this string extendee) {
      if (string.IsNullOrWhiteSpace(extendee)) {
        return string.Empty;
      }
      var sb = new StringBuilder();
      int state = 0; // 0=outside,1=alphaword,2=numericword
      foreach (var ch in extendee) {
        switch (state) {
          case 0: {
              if (char.IsLetter(ch)) {
                sb.Append(char.ToUpper(ch));
                state = 1;
              }
              else if (char.IsDigit(ch)) {
                sb.Append(ch);
                state = 2;
              }

              break;
            }
          case 1: {
              if (char.IsDigit(ch)) {
                sb.Append(ch);
                state = 2;
              }
              else if (char.IsLetter(ch)) {
                sb.Append(char.ToLower(ch));
              }
              else {
                state = 0;
              }

              break;
            }
          case 2: {
              if (char.IsLetter(ch)) {
                sb.Append(char.ToUpper(ch));
                state = 1;
              }
              else if (char.IsDigit(ch)) {
                sb.Append(ch);
              }
              else {
                state = 0;
              }

              break;
            }
        }
      }
      return sb.ToString();
    }

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static string ToShortcutString(this string extendee) {
      if (string.IsNullOrWhiteSpace(extendee)) {
        return string.Empty;
      }
      var sb = new StringBuilder();
      int state = 0; // 0=outside,1=alphaword,2=numericword
      foreach (var ch in extendee) {
        switch (state) {
          case 0: {
              if (char.IsLetter(ch)) {
                sb.Append(char.ToUpper(ch));
                state = 1;
              }
              else if (char.IsDigit(ch)) {
                sb.Append(ch);
                state = 2;
              }

              break;
            }
          case 1: {
              if (char.IsDigit(ch)) {
                sb.Append(ch);
                state = 2;
              }
              else if (!char.IsLetter(ch)) {
                state = 0;
              }

              break;
            }
          case 2: {
              if (char.IsLetter(ch)) {
                sb.Append(char.ToUpper(ch));
                state = 1;
              }
              else if (!char.IsDigit(ch)) {
                state = 0;
              }

              break;
            }
        }
      }
      return sb.ToString();
    }

  }

}