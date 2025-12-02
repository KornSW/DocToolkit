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

namespace KornSW.DocToolkit.NewsPaperLayouting {

  /// <summary>
  /// Responsible for rendering HTML with inline CSS and data URLs for images.
  /// </summary>
  public sealed class StockNewsHtmlRenderer {
    private const string _IsoDateFormat = "yyyy-MM-dd";
    private const string _UiDateFormat = "dd.MM.yyyy";
    private const int _SidebarWidthPx = 320;

    /// <summary>
    /// Renders the complete HTML document from entries.
    /// </summary>
    /// <param name="entries">NewsEntry array</param>
    /// <param name="title">Document title (shown in header and HTML title)</param>
    /// <param name="today">Reference date for key-date highlighting</param>
    public string Render(NewsEntry[] entries, string title, DateTime today, string[] bullishTickers, string[] bearishTickers) {
      try {
        if (entries == null) {
          throw new ArgumentNullException("entries");
        }
        if (string.IsNullOrWhiteSpace(title)) {
          throw new ArgumentException("title must not be empty.");
        }


        EntryBucket bucket = this.SplitBuckets(entries);

        StringBuilder sb = new StringBuilder(200 * 1024);

        sb.Append("<!doctype html><html lang=\"de\"><head><meta charset=\"utf-8\"/>");
        sb.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"/>");
        sb.Append("<title>");
        sb.Append(HtmlEncode(title));
        sb.Append("</title>");
        sb.Append(this.RenderStyles());
        sb.Append("</head><body><div class=\"page\">");

        sb.Append("<header><h1>");
        sb.Append(HtmlEncode(title));
        sb.Append("</h1><p class=\"dim\">Die Kolumne die dein persönliches Portfolio berücksichtigt... (by T.Korn)</p></header>");

        sb.Append("<div class=\"layout\">");

        // MAIN
        sb.Append("<section class=\"main\">");

        // Majority 1 (Feature) – simple alternating width without ternary
        for (int i = 0; i < bucket.Majority1.Length; i = i + 1) {
          NewsEntry e = bucket.Majority1[i];
          string sizeClass = "m1 wide";
          int mod = i % 2;
          if (mod != 0) {
            sizeClass = "m1 half";
          }
          sb.Append(this.RenderMainArticle(e, sizeClass, today, bullishTickers, bearishTickers));
        }

        // Majority 2 (Standard)
        for (int i = 0; i < bucket.Majority2.Length; i = i + 1) {
          NewsEntry e = bucket.Majority2[i];
          sb.Append(this.RenderMainArticle(e, "m2", today, bullishTickers, bearishTickers));
        }

        sb.Append("</section>");

        // SIDEBAR: Majority 3
        sb.Append("<aside>");
        for (int i = 0; i < bucket.Majority3.Length; i = i + 1) {
          NewsEntry e = bucket.Majority3[i];
          sb.Append(this.RenderSidebarCard(e, today, bullishTickers, bearishTickers));
        }
        sb.Append("</aside>");

        sb.Append("</div></div></body></html>");

        return sb.ToString();
      }
      catch (Exception ex) {
        DevLogger.LogCritical(ex);
        throw;
      }
    }

    /// <summary>
    /// Splits entries into three buckets by Majority.
    /// </summary>
    private EntryBucket SplitBuckets(NewsEntry[] entries) {
      List<NewsEntry> m1 = new List<NewsEntry>();
      List<NewsEntry> m2 = new List<NewsEntry>();
      List<NewsEntry> m3 = new List<NewsEntry>();

      for (int i = 0; i < entries.Length; i = i + 1) {
        NewsEntry e = entries[i];
        if (e.Majority == 1) {
          m1.Add(e);
        }
        else if (e.Majority == 2) {
          m2.Add(e);
        }
        else {
          m3.Add(e);
        }
      }

      EntryBucket bucket = new EntryBucket();
      bucket.Majority1 = m1.ToArray();
      bucket.Majority2 = m2.ToArray();
      bucket.Majority3 = m3.ToArray();
      return bucket;
    }

    /// <summary>
    /// Renders a main-section article (Majority 1 or 2).
    /// </summary>
    private string RenderMainArticle(NewsEntry e, string sizeClass, DateTime today, string[] bullishTickers, string[] bearishTickers) {
      StringBuilder sb = new StringBuilder(16 * 1024);

      sb.Append("<article class=\"");
      sb.Append(HtmlEncode(sizeClass));
      sb.Append("\">");

      string dataUrl = string.Empty;
      foreach (var imgUrl in e.ImageUrls) {
        dataUrl = ImageDownloader.TryDownloadAsDataUrl(imgUrl);
        if (!string.IsNullOrEmpty(dataUrl)) {
          sb.Append("<img class=\"thumb\" src=\"");
          sb.Append(dataUrl);
          sb.Append("\" alt=\"BILD KONNTE NICHT GELADEN WERDEN :-(\"/>");
          break;
        }
      }

      sb.Append("<div class=\"pad\">");
      sb.Append("<h2>");
      sb.Append(HtmlEncode(e.Headline));
      sb.Append(" <span class=\"ticker\">");
      sb.Append(HtmlEncode(e.RelatedTicker));
      sb.Append("</span></h2>");

      sb.Append("<div class=\"content\">");
      sb.Append(ConvertMarkdownBoldToHtml(HtmlEncode(e.Content)));
      sb.Append("</div>");

      if (bullishTickers.Contains(e.RelatedTicker)) {
        sb.Append("<div class=\"recom\"><strong>Empfehlung</strong> für deine Positionen (Bullish): <i>");
        sb.Append(ConvertMarkdownBoldToHtml(HtmlEncode(e.RecommendedActionsBullish)));
        sb.Append("</i></div>");
      }
      else if (bearishTickers.Contains(e.RelatedTicker)) {
        sb.Append("<div class=\"recom\"><strong>Empfehlung</strong> für deine Positionen (Bearish): <i>");
        sb.Append(ConvertMarkdownBoldToHtml(HtmlEncode(e.RecommendedActionsBearish)));
        sb.Append("</i></div>");
      }
      else {
        sb.Append("<div class=\"recom\"><strong>Empfehlung</strong> für dich (neue Trades): <i>");
        sb.Append(ConvertMarkdownBoldToHtml(HtmlEncode(e.RecommendedActionsEntry)));
        sb.Append("</i></div>");
      }

      sb.Append(this.RenderMetaBlock(e, today));

      sb.Append("</div></article>");

      return sb.ToString();
    }

    /// <summary>
    /// Renders a sidebar card (Majority 3).
    /// </summary>
    private string RenderSidebarCard(NewsEntry e, DateTime today, string[] bullishTickers, string[] bearishTickers) {
      StringBuilder sb = new StringBuilder(12 * 1024);

      sb.Append("<div class=\"m3-card\">");

      string dataUrl = string.Empty;
      foreach (var imgUrl in e.ImageUrls) {
        dataUrl = ImageDownloader.TryDownloadAsDataUrl(imgUrl);
        if (!string.IsNullOrEmpty(dataUrl)) {
          sb.Append("<img class=\"thumb\" src=\"");
          sb.Append(dataUrl);
          sb.Append("\" alt=\"BILD KONNTE NICHT GELADEN WERDEN :-(\"/>");
          break;
        }
      }


      sb.Append("<div class=\"pad\">");
      sb.Append("<h3>");
      sb.Append(HtmlEncode(e.Headline));
      sb.Append(" <span class=\"ticker\">");
      sb.Append(HtmlEncode(e.RelatedTicker));
      sb.Append("</span></h3>");

      sb.Append("<div class=\"content\">");
      sb.Append(ConvertMarkdownBoldToHtml(HtmlEncode(e.Content)));
      sb.Append("</div>");


      if (bullishTickers.Contains(e.RelatedTicker) && !bearishTickers.Contains(e.RelatedTicker) && !string.IsNullOrWhiteSpace(e.RecommendedActionsBullish)) {
        sb.Append("<div class=\"recom\"><strong>Empfehlung</strong> für dich (also Bullish): <i>");
        sb.Append(ConvertMarkdownBoldToHtml(HtmlEncode(e.RecommendedActionsBullish)));
        sb.Append("</i></div>");
      }
      if (bearishTickers.Contains(e.RelatedTicker) && !bullishTickers.Contains(e.RelatedTicker) && !string.IsNullOrWhiteSpace(e.RecommendedActionsBearish)) {
        sb.Append("<div class=\"recom\"><strong>Empfehlung</strong> für dich (also Bearish): <i>");
        sb.Append(ConvertMarkdownBoldToHtml(HtmlEncode(e.RecommendedActionsBearish)));
        sb.Append("</i></div>");
      }

      sb.Append(this.RenderMetaBlock(e, today));

      sb.Append("</div></div>");

      return sb.ToString();
    }

    // ... Namespaces etc. unverändert ...

    private string RenderMetaBlock(NewsEntry e, DateTime today) {
      StringBuilder sb = new StringBuilder(4 * 1024);

      sb.Append("<div class=\"meta\">");

      // Ratings
      sb.Append("<div class=\"ratings\"><strong>Ratings:</strong> ");
      if (e.Ratings.Count == 0) {
        sb.Append("n/a");
      }
      else {
        bool first = true;
        foreach (KeyValuePair<string, int> kv in e.Ratings) {
          if (!first) {
            sb.Append("; ");
          }

          string label = RatingsHelper.MapToLabel(kv.Value);
          string color = RatingsHelper.MapToColor(kv.Value);
          bool strong = RatingsHelper.IsStrong(kv.Value);

          sb.Append(HtmlEncode(kv.Key));
          sb.Append(": ");

          if (strong) {
            sb.Append("<strong>");
            sb.Append("<span style=\"color:");
            sb.Append(HtmlEncode(color));
            sb.Append("\">");
            sb.Append(HtmlEncode(label));
            sb.Append("</span>");
            sb.Append("</strong>");
          }
          else {
            sb.Append("<span style=\"color:");
            sb.Append(HtmlEncode(color));
            sb.Append("\">");
            sb.Append(HtmlEncode(label));
            sb.Append("</span>");
          }

          first = false;
        }
      }

      // Zielkurs
      sb.Append(" <span class=\"target\">| Zielkurs: <i><b>");
      if (e.TargetCourseFrom > 0 && e.TargetCourseTo > 0) {
        sb.Append(e.TargetCourseFrom.ToString(CultureInfo.InvariantCulture));
        sb.Append("</b> – <b>");
        sb.Append(e.TargetCourseTo.ToString(CultureInfo.InvariantCulture));
      }
      else {
        sb.Append("n/a");
      }
      sb.Append("</b></i></span></div>");

      // Key dates (nur datum blau + ggf. fett)
      sb.Append("<div class=\"keydates\"><strong>Key-Dates:</strong> ");
      string kd = RenderKeyDates(e.KeyDates, today);
      if (string.IsNullOrWhiteSpace(kd)) {
        sb.Append("n/a");
      }
      else {
        sb.Append(kd);
      }
      sb.Append("</div>");

      // Sources unverändert
      sb.Append("<div class=\"sources\">Quellen: ");
      string sources = RenderSources(e.OriginUrls);
      if (string.IsNullOrWhiteSpace(sources)) {
        sb.Append("n/a");
      }
      else {
        sb.Append(sources);
      }
      sb.Append("</div>");

      sb.Append("</div>");

      return sb.ToString();
    }

    private string RenderKeyDates(Dictionary<string, string> keyDates, DateTime today) {
      if (keyDates == null || keyDates.Count == 0) {
        return string.Empty;
      }

      DateTime nextWeekFriday = ComputeFridayOfComingWeek(today);
      StringBuilder sb = new StringBuilder(512);

      bool first = true;
      foreach (KeyValuePair<string, string> kv in keyDates) {
        if (!first) {
          sb.Append("; ");
        }

        string label = kv.Key;
        string val = kv.Value;

        DateTime parsed;
        bool parsedOk = TryParseIsoDate(val, out parsed);

        sb.Append(HtmlEncode(label));
        sb.Append(" ");

        if (parsedOk) {
          bool within = IsWithinWindow(parsed, today, nextWeekFriday);
          if (within) {
            sb.Append("<strong><span style=\"color:#0b3d91\">");
            sb.Append(parsed.ToString(_UiDateFormat, CultureInfo.InvariantCulture));
            sb.Append("</span></strong>");
          }
          else {
            sb.Append("<span style=\"color:#0b3d91\">");
            sb.Append(parsed.ToString(_UiDateFormat, CultureInfo.InvariantCulture));
            sb.Append("</span>");
          }
        }
        else {
          sb.Append("<span style=\"color:#0b3d91\">");
          sb.Append(HtmlEncode(val));
          sb.Append("</span>");
        }

        first = false;
      }

      return sb.ToString();
    }

    /// <summary>
    /// Renders sources as links with domain-only labels.
    /// </summary>
    private string RenderSources(string[] originUrls) {
      if (originUrls == null || originUrls.Length == 0) {
        return string.Empty;
      }

      StringBuilder sb = new StringBuilder(512);
      bool first = true;

      for (int i = 0; i < originUrls.Length; i = i + 1) {
        string url = originUrls[i];
        if (string.IsNullOrWhiteSpace(url)) {
          continue;
        }

        Uri u = UrlHelper.TryMakeUri(url);
        string label = UrlHelper.ExtractDomainLabel(u);

        if (!first) {
          sb.Append(", ");
        }

        sb.Append("<a href=\"");
        sb.Append(HtmlEncode(url));
        sb.Append("\" target=\"_blank\" rel=\"noopener\">");
        sb.Append(HtmlEncode(label));
        sb.Append("</a>");

        first = false;
      }

      return sb.ToString();
    }

    /// <summary>
    /// HTML-escapes a string using WebUtility.HtmlEncode.
    /// </summary>
    private static string HtmlEncode(string text) {
      if (text == null) {
        return string.Empty;
      }
      return System.Net.WebUtility.HtmlEncode(text);
    }

    /// <summary>
    /// Converts **bold** markdown into <strong>bold</strong>
    /// </summary>
    private static string ConvertMarkdownBoldToHtml(string encoded) {
      if (string.IsNullOrEmpty(encoded)) {
        return string.Empty;
      }

      Regex re = new Regex(@"\*\*(.+?)\*\*", RegexOptions.Singleline);
      string result = re.replace(encoded, new MatchEvaluator(delegate (Match m) {
        string inner = m.Groups[1].Value;
        return "<b>" + inner + "</b>";
      }));

      return result;
    }

    /// <summary>
    /// Calculates the Friday of the coming week relative to a given date.
    /// </summary>
    private static DateTime ComputeFridayOfComingWeek(DateTime today) {
      int daysUntilNextMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
      if (daysUntilNextMonday == 0) {
        daysUntilNextMonday = 7;
      }
      DateTime nextMonday = today.AddDays(daysUntilNextMonday);
      DateTime nextFriday = nextMonday.AddDays(4);
      return nextFriday.Date;
    }

    /// <summary>
    /// Returns true if 'date' lies in [start ... end] inclusive.
    /// </summary>
    private static bool IsWithinWindow(DateTime date, DateTime start, DateTime end) {
      if (date.Date < start.Date) {
        return false;
      }
      if (date.Date > end.Date) {
        return false;
      }
      return true;
    }

    /// <summary>
    /// Tries to parse ISO date (yyyy-MM-dd).
    /// </summary>
    private static bool TryParseIsoDate(string s, out DateTime dt) {
      return DateTime.TryParseExact(s, _IsoDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dt);
    }

    /// <summary>
    /// Returns the CSS (layout preserved) with updated keydate color.
    /// </summary>
    private string RenderStyles() {
      StringBuilder css = new StringBuilder(8 * 1024);

      css.Append("<style>");
      css.Append(":root{--col-gap:24px;--side-width:");
      css.Append(_SidebarWidthPx.ToString(CultureInfo.InvariantCulture));
      css.Append("px;--muted:#666;--hair:#e6e6e6;}*{box-sizing:border-box}body{margin:0;font-family:ui-sans-serif,system-ui,-apple-system,Segoe UI,Roboto,Helvetica,Arial;color:#222;background:#fafafa;line-height:1.45}.page{max-width:1200px;margin:0 auto;padding:24px}header h1{margin:0 0 6px 0;font-size:28px;letter-spacing:.2px}header p{margin:0 0 24px 0;color:var(--muted);font-size:14px}");
      css.Append(".layout{display:grid;grid-template-columns:1fr var(--side-width);gap:var(--col-gap)}@media(max-width:1024px){.layout{grid-template-columns:1fr}aside{order:99}}");
      css.Append(".main{display:grid;grid-template-columns:repeat(12,1fr);gap:20px;align-items:start}");
      css.Append("article{background:#fff;border:1px solid var(--hair);border-radius:10px;overflow:clip;display:flex;flex-direction:column;min-height:100%}.thumb{width:100%;aspect-ratio:16/9;object-fit:cover;display:block;background:#ddd}.pad{padding:14px 16px 12px 16px}h2{font-weight:800;margin:2px 0 8px 0;line-height:1.15}.content{font-size:16px;margin-bottom:10px}");
      css.Append(".m1{grid-column:span 12}.m1 h2{font-size:28px}.m1 .content{font-size:18px}.m1.wide{grid-column:span 12}.m1.half{grid-column:span 8}");
      css.Append(".m2{grid-column:span 6}.m2 h2{font-size:22px}.m2 .content{font-size:16px}");
      css.Append("aside .m3-card{background:#fff;border:1px solid var(--hair);border-radius:10px;margin-bottom:14px;overflow:clip}aside .m3-card h3{font-size:16px;margin:6px 0 8px 0;line-height:1.2;font-weight:800}aside .m3-card .content{font-size:14px}aside .thumb{aspect-ratio:4/3}");
      css.Append(".meta{margin-top:6px;padding-top:10px;border-top:1px dashed var(--hair);font-size:13px;color:#333}.meta .target{color:#000}.keydates{font-size:13px;margin-top:4px}.sources{font-size:12px;color:var(--muted);margin-top:6px}.sources a{color:inherit;text-decoration:none;border-bottom:1px dotted #bbb}");
      css.Append(".recom{font-size:14px;color:#222;background:#f7f7f7;border-left:3px solid #ccc;padding:8px 10px;border-radius:6px;margin-top:8px}.ticker{display:inline-block;font-size:12px;font-weight:700;color:#444;background:#f0f0f0;padding:2px 6px;border-radius:6px;margin-left:6px}.dim{color:var(--muted)}strong{}");
      css.Append("</style>");

      return css.ToString();
    }
  }

  /// <summary>
  /// Maps numeric ratings to labels and colors.
  /// </summary>
  public static class RatingsHelper {
    /// <summary>
    /// Returns the textual label for a numeric rating.
    /// </summary>
    public static string MapToLabel(int value) {
      if (value <= -2) {
        return "Strong-SELL";
      }
      if (value == -1) {
        return "Sell";
      }
      if (value == 0) {
        return "Hold";
      }
      if (value == 1) {
        return "Buy";
      }
      return "Strong-BUY";
    }

    /// <summary>
    /// Returns a hex color string (#RRGGBB) for the numeric rating. >0 green, <0 red, 0 neutral.
    /// </summary>
    public static string MapToColor(int value) {
      if (value > 0) {
        return "#0a8f2f";
      }
      if (value < 0) {
        return "#c62828";
      }
      return "#333333";
    }

    /// <summary>
    /// True if the rating is a "Strong..." value (abs == 2).
    /// </summary>
    public static bool IsStrong(int value) {
      if (value <= -2) {
        return true;
      }
      if (value >= 2) {
        return true;
      }
      return false;
    }
  }

  /// <summary>
  /// JSON data model matching the provided schema.
  /// </summary>
  [DebuggerDisplay("[{RelatedTicker}] {Headline}")]
  public sealed class NewsEntry {
    [JsonProperty("Headline")]
    public string Headline { get; set; }

    [JsonProperty("RelatedTicker")]
    public string RelatedTicker { get; set; }

    [JsonProperty("Majority")]
    public int Majority { get; set; }

    [JsonProperty("ImageUrls")]
    public string[] ImageUrls { get; set; }

    [JsonProperty("Content")]
    public string Content { get; set; }

    [JsonProperty("OriginUrls")]
    public string[] OriginUrls { get; set; }

    [JsonProperty("Ratings")]
    public Dictionary<string, int> Ratings { get; set; }

    [JsonProperty("TargetCourseFrom")]
    public decimal TargetCourseFrom { get; set; }

    [JsonProperty("TargetCourseTo")]
    public decimal TargetCourseTo { get; set; }

    [JsonProperty("KeyDates")]
    public Dictionary<string, string> KeyDates { get; set; }


    //TODO: ABSTRAHIEREN-IN-ZUSATZSEKTION
    [JsonProperty("RecommendedActionsBullish")]
    public string RecommendedActionsBullish { get; set; }

    [JsonProperty("RecommendedActionsBearish")]
    public string RecommendedActionsBearish { get; set; }

    [JsonProperty("RecommendedActionsEntry")]
    public string RecommendedActionsEntry { get; set; }
  }

  /// <summary>
  /// Helper struct to keep buckets separated.
  /// </summary>
  public sealed class EntryBucket {
    public NewsEntry[] Majority1 { get; set; }
    public NewsEntry[] Majority2 { get; set; }
    public NewsEntry[] Majority3 { get; set; }
  }

}
