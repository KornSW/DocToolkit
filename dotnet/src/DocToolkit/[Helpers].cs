using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Web; // For Url decoding only (safe across platforms in .NET Framework); if targeting .NET Core, remove and rely on System.Net.WebUtility
using Newtonsoft.Json;
using System.Reflection;
using System.Diagnostics;
using Logging.SmartStandards.CopyForDocToolkit;

namespace KornSW.DocToolkit {

  /// <summary>
  /// Downloads images synchronously and returns a data URL. Returns empty string on failure.
  /// </summary>
  public static class ImageDownloader {
    /// <summary>
    /// Attempts to download a URL and convert to data URL.
    /// </summary>
    public static string TryDownloadAsDataUrl(string url) {

      try {
        if (string.IsNullOrWhiteSpace(url)) {
          return string.Empty;
        }

        if (url.StartsWith("data:")) {
          return url;
        }

        url = url.Replace(Environment.NewLine, "");

        Uri uri = UrlHelper.TryMakeUri(url);
        if (uri == null) {
          return string.Empty;
        }

        if (url.EndsWith(".png", StringComparison.CurrentCultureIgnoreCase)) {
        }
        else if (url.EndsWith(".jpg", StringComparison.CurrentCultureIgnoreCase)) {
        }
        else if (url.EndsWith(".jpeg", StringComparison.CurrentCultureIgnoreCase)) {
        }
        else if (url.EndsWith(".svg", StringComparison.CurrentCultureIgnoreCase)) {
        }
        else {
          DevLogger.LogTrace(0, 0, "SkippingiInvalid Image-URL: " + url);
          return string.Empty;
        }

        HttpWebRequest req = (HttpWebRequest)WebRequest.Create(uri);
        req.Method = "GET";
        req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";
        req.Timeout = 15000;
        req.AllowAutoRedirect = false;
        req.KeepAlive = true;
        req.Proxy = null;

        DevLogger.LogTrace(0, 0, "Downloading Image-URL: " + url);

        using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse()) {
          if (resp.StatusCode != HttpStatusCode.OK) {
            return string.Empty;
          }

          string contentType = resp.ContentType;
          if (string.IsNullOrWhiteSpace(contentType)) {
            contentType = "application/octet-stream";
          }

          using (Stream stream = resp.GetResponseStream()) {
            if (stream == null) {
              return string.Empty;
            }

            using (MemoryStream ms = new MemoryStream()) {
              byte[] buffer = new byte[16 * 1024];
              int read = 0;
              do {
                read = stream.Read(buffer, 0, buffer.Length);
                if (read > 0) {
                  ms.Write(buffer, 0, read);
                }
              }
              while (read > 0);

              byte[] bytes = ms.ToArray();
              string b64 = Convert.ToBase64String(bytes);
              string dataUrl = "data:" + contentType + ";base64," + b64;
              return dataUrl;
            }
          }
        }
      }
      catch (Exception ex) {
        DevLogger.LogCritical(ex);
        return string.Empty;
      }
    }
  }

  /// <summary>
  /// URL utilities for domain extraction and Uri parsing.
  /// </summary>
  public static class UrlHelper {
    /// <summary>
    /// Creates a Uri instance or returns null on failure.
    /// </summary>
    public static Uri TryMakeUri(string url) {
      try {
        Uri u;
        bool ok = Uri.TryCreate(url, UriKind.Absolute, out u);
        if (!ok) {
          return null;
        }
        return u;
      }
      catch {
        return null;
      }
    }

    /// <summary>
    /// Extracts a human-friendly domain label (e.g., "finanzen.net") from a Uri.
    /// </summary>
    public static string ExtractDomainLabel(Uri uri) {
      if (uri == null) {
        return "source";
      }

      string host = uri.Host;
      if (string.IsNullOrWhiteSpace(host)) {
        return "source";
      }

      if (host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)) {
        return host.Substring(4);
      }

      return host;
    }
  }

  /// <summary>
  /// Regex helper for explicit MatchEvaluator without lambdas.
  /// </summary>
  internal static class RegexExtensions {
    /// <summary>
    /// Wrapper to allow MatchEvaluator with explicit delegate.
    /// </summary>
    public static string replace(this Regex regex, string input, MatchEvaluator evaluator) {
      return Regex.Replace(input, regex.ToString(), evaluator, regex.Options);
    }

  }

}
