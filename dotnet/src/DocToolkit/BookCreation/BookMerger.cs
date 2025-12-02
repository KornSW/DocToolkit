using Logging.SmartStandards.CopyForDocToolkit;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace KornSW.DocToolkit.BookCreation {

  /// <summary>
  /// Provides utilities to build a single printable HTML document from multiple input files:
  /// - Supports .md, .txt, .png, .jpg
  /// - Each input starts on a dedicated 'page' with print page-breaks
  /// - All images are embedded as Base64 data URIs (including images referenced inside Markdown)
  /// - Generates a unified cover page (title centered, author), optionally with a background image
  /// - If an input image file is named exactly 'title' (case-insensitive, any supported extension),
  ///   it will be placed above the cover title
  /// </summary>
  public static class BookMerger {
    private const string _Css = @"/* Unified print-friendly CSS with child-friendly typography */
:root {
    --page-max-width: 900px;
    --content-padding: 28px;
    --text-color: #1b1f24;
    --muted-color: #5a6675;
    --accent-color: #3b82f6; /* light blue tone for headings */
    --border-color: #e5e7eb;
    --code-bg: #f6f8fa;
    --heading-font: 'Comic Neue', 'Segoe Print', 'Comic Sans MS', system-ui, sans-serif;
    --body-font: 'Century Gothic', system-ui, sans-serif;
}

* {
    box-sizing: border-box;
}

html, body {
    margin: 0;
    padding: 0;
    color: #1b1f24;
    font-family: 'Century Gothic';
    line-height: 1.6;
    font-size: 18px; /* slightly larger text */
    -webkit-print-color-adjust: exact;
    print-color-adjust: exact;
}

.wrapper {
    width: 100%;
    display: block;
}

.page {
    max-width: var(--page-max-width);
    margin: 0 auto;
    padding: var(--content-padding);
    background: #ffffff;
}

.page + .page {
    /* border-top: 1px solid var(--border-color); */
}

/* Page breaks for print */
    .page {
        break-after: page;
        page-break-after: always;
    }
    .page:last-child {
        break-after: auto;
        page-break-after: auto;
    }

/* Cover page */
.cover {
    position: relative;
    text-align: center;
    padding-top: 12vh;
    padding-bottom: 12vh;
}
.cover__bg {
    position: absolute;
    inset: 0;
    background-position: center center;
    background-repeat: no-repeat;
    background-size: cover;
    opacity: 0.12;
    z-index: 0;
}
.cover__content {
    position: relative;
    z-index: 1;
}
.cover__image {
    display: block;
    margin: 0 auto 28px auto;
    max-width: 60%;
    height: auto;
}
.cover__title {
    font-family: 'Comic Sans MS';
    font-size: 48px;
    font-weight: 800;
    letter-spacing: 0.5px;
    margin: 0 0 8px 0;
    color: #3b82f6;
}
.cover__author {
    font-size: 20px;
    color: var(--muted-color);
    margin: 0;
}

/* Typography */
h1, h2, h3, h4, h5, h6 {
    font-family: 'Comic Sans MS';
    line-height: 1.3;
    margin: 1.4em 0 0.6em 0;
    color: var(--accent-color);
}
h1 { font-size: 36px; }
h2 { font-size: 30px; }
h3 { font-size: 26px; }
h4 { font-size: 22px; }
h5 { font-size: 20px; }
h6 { font-size: 18px; color: #3b82f6; }

p {
    margin: 0 0 1em 0;
}

a {
    color: #2563eb;
    text-decoration: none;
    border-bottom: 1px dashed rgba(37,99,235,0.4);
}

blockquote {
    margin: 1em 0;
    padding: 0.75em 1em;
    border-left: 4px solid #3b82f6;
    background: #f0f7ff;
    color: #334155;
    border-radius: 4px;
}

hr {
    border: none;
    border-top: 2px dotted var(--border-color);
    margin: 2em 0;
}

/* Lists */
ul, ol {
    margin: 0 0 1em 1.4em;
}
li + li {
    margin-top: 0.3em;
}

/* Code */
pre, code, kbd, samp {
    font-family: 'Courier New', monospace;
}
code {
    background: var(--code-bg);
    padding: 0.2em 0.4em;
    border: 1px solid #eaecef;
    border-radius: 4px;
    font-size: 0.9em;
}
pre {
    background: var(--code-bg);
    border: 1px solid #eaecef;
    border-radius: 8px;
    padding: 14px;
    overflow: auto;
}
pre code {
    background: transparent;
    border: none;
    padding: 0;
}

/* Tables */
table {
    border-collapse: collapse;
    width: 100%;
    margin: 1em 0;
}
th, td {
    border: 1px solid var(--border-color);
    padding: 10px 12px;
    text-align: left;
}
thead th {
    background: #f3f8ff;
}

/* Images */
img {
    max-width: 100%;
    height: auto;
    display: block;
    margin: 0.6em auto;
    border-radius: 6px;
}

/* Section header (filename display) */
.section-header {
    font-size: 14px;
    color: var(--muted-color);
    border-bottom: 1px dashed var(--border-color);
    padding-bottom: 8px;
    margin-bottom: 18px;
    text-align: right;
    font-style: italic;
}";


    private const string _CssALT2 = @"/* Unified print-friendly CSS with child-friendly typography */
:root {
    --page-max-width: 900px;
    --content-padding: 28px;
    --text-color: #1b1f24;
    --muted-color: #5a6675;
    --accent-color: #3b82f6; /* light blue tone for headings */
    --border-color: #e5e7eb;
    --code-bg: #f6f8fa;
    --heading-font: 'Comic Neue', 'Segoe Print', 'Comic Sans MS', system-ui, sans-serif;
    --body-font: 'Century Gothic', system-ui, sans-serif;
}

* {
    box-sizing: border-box;
}

html, body {
    margin: 0;
    padding: 0;
    color: var(--text-color);
    font-family: var(--body-font);
    line-height: 1.6;
    font-size: 18px; /* slightly larger text */
    -webkit-print-color-adjust: exact;
    print-color-adjust: exact;
}

.wrapper {
    width: 100%;
    display: block;
}

.page {
    max-width: var(--page-max-width);
    margin: 0 auto;
    padding: var(--content-padding);
    background: #ffffff;
}

.page + .page {
    /* border-top: 1px solid var(--border-color); */
}

/* Page breaks for print */
@media print {
    .page {
        break-after: page;
        page-break-after: always;
    }
    .page:last-child {
        break-after: auto;
        page-break-after: auto;
    }
}

/* Cover page */
.cover {
    position: relative;
    text-align: center;
    padding-top: 12vh;
    padding-bottom: 12vh;
}
.cover__bg {
    position: absolute;
    inset: 0;
    background-position: center center;
    background-repeat: no-repeat;
    background-size: cover;
    opacity: 0.12;
    z-index: 0;
}
.cover__content {
    position: relative;
    z-index: 1;
}
.cover__image {
    display: block;
    margin: 0 auto 28px auto;
    max-width: 60%;
    height: auto;
}
.cover__title {
    font-family: var(--heading-font);
    font-size: 48px;
    font-weight: 800;
    letter-spacing: 0.5px;
    margin: 0 0 8px 0;
    color: var(--accent-color);
}
.cover__author {
    font-size: 20px;
    color: var(--muted-color);
    margin: 0;
}

/* Typography */
h1, h2, h3, h4, h5, h6 {
    font-family: var(--heading-font);
    line-height: 1.3;
    margin: 1.4em 0 0.6em 0;
    color: var(--accent-color);
}
h1 { font-size: 36px; }
h2 { font-size: 30px; }
h3 { font-size: 26px; }
h4 { font-size: 22px; }
h5 { font-size: 20px; }
h6 { font-size: 18px; color: var(--muted-color); }

p {
    margin: 0 0 1em 0;
}

a {
    color: #2563eb;
    text-decoration: none;
    border-bottom: 1px dashed rgba(37,99,235,0.4);
}

blockquote {
    margin: 1em 0;
    padding: 0.75em 1em;
    border-left: 4px solid var(--accent-color);
    background: #f0f7ff;
    color: #334155;
    border-radius: 4px;
}

hr {
    border: none;
    border-top: 2px dotted var(--border-color);
    margin: 2em 0;
}

/* Lists */
ul, ol {
    margin: 0 0 1em 1.4em;
}
li + li {
    margin-top: 0.3em;
}

/* Code */
pre, code, kbd, samp {
    font-family: 'Courier New', monospace;
}
code {
    background: var(--code-bg);
    padding: 0.2em 0.4em;
    border: 1px solid #eaecef;
    border-radius: 4px;
    font-size: 0.9em;
}
pre {
    background: var(--code-bg);
    border: 1px solid #eaecef;
    border-radius: 8px;
    padding: 14px;
    overflow: auto;
}
pre code {
    background: transparent;
    border: none;
    padding: 0;
}

/* Tables */
table {
    border-collapse: collapse;
    width: 100%;
    margin: 1em 0;
}
th, td {
    border: 1px solid var(--border-color);
    padding: 10px 12px;
    text-align: left;
}
thead th {
    background: #f3f8ff;
}

/* Images */
img {
    max-width: 100%;
    height: auto;
    display: block;
    margin: 0.6em auto;
    border-radius: 6px;
}

/* Section header (filename display) */
.section-header {
    font-size: 14px;
    color: var(--muted-color);
    border-bottom: 1px dashed var(--border-color);
    padding-bottom: 8px;
    margin-bottom: 18px;
    text-align: right;
    font-style: italic;
}";

    private const string _CssALT = @"/* Unified print-friendly CSS (screen + print) */
:root {
    --page-max-width: 900px;
    --content-padding: 28px;
    --text-color: #1b1f24;
    --muted-color: #5a6675;
    --accent-color: #0f62fe;
    --border-color: #e5e7eb;
    --code-bg: #f6f8fa;
    --heading-font: ui-sans-serif, -apple-system, Segoe UI, Roboto, Helvetica, Arial, 'Apple Color Emoji', 'Segoe UI Emoji';
    --body-font: ui-sans-serif, -apple-system, Segoe UI, Roboto, Helvetica, Arial, 'Apple Color Emoji', 'Segoe UI Emoji';
}

* {
    box-sizing: border-box;
}

html, body {
    margin: 0;
    padding: 0;
    color: var(--text-color);
    font-family: var(--body-font);
    line-height: 1.55;
    -webkit-print-color-adjust: exact;
    print-color-adjust: exact;
}

.wrapper {
    width: 100%;
    display: block;
}

.page {
    max-width: var(--page-max-width);
    margin: 0 auto;
    padding: var(--content-padding);
    background: #ffffff;
}

.page + .page {
    border-top: 1px solid var(--border-color);
}

/* Page breaks for print */
@media print {
    .page {
        break-after: page;
        page-break-after: always;
    }
    .page:last-child {
        break-after: auto;
        page-break-after: auto;
    }
}

/* Cover page */
.cover {
    position: relative;
    text-align: center;
    padding-top: 12vh;
    padding-bottom: 12vh;
}
.cover__bg {
    position: absolute;
    inset: 0;
    background-position: center center;
    background-repeat: no-repeat;
    background-size: cover;
    opacity: 0.12;
    z-index: 0;
}
.cover__content {
    position: relative;
    z-index: 1;
}
.cover__image {
    display: block;
    margin: 0 auto 28px auto;
    max-width: 60%;
    height: auto;
}
.cover__title {
    font-family: var(--heading-font);
    font-size: 42px;
    font-weight: 800;
    letter-spacing: 0.2px;
    margin: 0 0 6px 0;
}
.cover__author {
    font-size: 16px;
    color: var(--muted-color);
    margin: 0;
}

/* Typography */
h1, h2, h3, h4, h5, h6 {
    font-family: var(--heading-font);
    line-height: 1.25;
    margin: 1.5em 0 0.6em 0;
}
h1 { font-size: 32px; }
h2 { font-size: 26px; }
h3 { font-size: 22px; }
h4 { font-size: 18px; }
h5 { font-size: 16px; }
h6 { font-size: 14px; color: var(--muted-color); }

p {
    margin: 0 0 0.9em 0;
}

a {
    color: var(--accent-color);
    text-decoration: none;
    border-bottom: 1px solid rgba(15,98,254,0.25);
}

blockquote {
    margin: 1em 0;
    padding: 0.75em 1em;
    border-left: 4px solid var(--border-color);
    background: #fafafa;
    color: #374151;
}

hr {
    border: none;
    border-top: 1px solid var(--border-color);
    margin: 2em 0;
}

/* Lists */
ul, ol {
    margin: 0 0 1em 1.4em;
}
li + li {
    margin-top: 0.25em;
}

/* Code */
pre, code, kbd, samp {
    font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, 'Liberation Mono', monospace;
}
code {
    background: var(--code-bg);
    padding: 0.15em 0.35em;
    border: 1px solid #eaecef;
    border-radius: 4px;
}
pre {
    background: var(--code-bg);
    border: 1px solid #eaecef;
    border-radius: 8px;
    padding: 14px;
    overflow: auto;
}
pre code {
    background: transparent;
    border: none;
    padding: 0;
}

/* Tables */
table {
    border-collapse: collapse;
    width: 100%;
    margin: 1em 0;
}
th, td {
    border: 1px solid var(--border-color);
    padding: 8px 10px;
    text-align: left;
}
thead th {
    background: #f9fafb;
}

/* Images */
img {
    max-width: 100%;
    height: auto;
    display: block;
}

/* Page header per section (optional, generated from filename) */
.section-header {
    font-size: 13px;
    color: var(--muted-color);
    border-bottom: 1px solid var(--border-color);
    padding-bottom: 8px;
    margin-bottom: 18px;
}";

    /// <summary>
    /// Builds a single HTML document string from multiple inputs with a unified style and print page breaks.
    /// </summary>
    /// <param name="inputPaths">Array of file paths (.md, .txt, .png, .jpg)</param>
    /// <param name="documentTitle">Document title for the cover page</param>
    /// <param name="author">Author line for the cover page</param>
    /// <param name="optionalCoverBackgroundImagePath">Optional background image path for the cover page (embedded as Base64)</param>
    /// <returns>Complete HTML as string</returns>
    public static string BuildPrintableHtml(
      string[] inputPaths, string documentTitle, string author, string optionalCoverBackgroundImagePath
    ) {

      if (inputPaths == null) {
        throw new ArgumentNullException("inputPaths");
      }

      StringBuilder html = new StringBuilder();
      html.AppendLine("<!DOCTYPE html>");
      html.AppendLine("<html lang=\"en\">");
      html.AppendLine("<head>");
      html.AppendLine("<meta charset=\"utf-8\"/>");
      html.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"/>");

      html.Append("<title>");
      html.Append(EscapeHtml(documentTitle));
      html.AppendLine("</title>");

      html.AppendLine("<style>");
      html.AppendLine(_Css);
      html.AppendLine("</style>");
      html.AppendLine("</head>");
      html.AppendLine("<body>");
      html.AppendLine("<div class=\"wrapper\">");

      string coverImageDataUri = FindTitleImageDataUri(inputPaths);
      string coverBackgroundDataUri = EncodeOptionalBackground(optionalCoverBackgroundImagePath);

      html.AppendLine(GenerateCoverHtml(documentTitle, author, coverImageDataUri, coverBackgroundDataUri));

      for (int i = 0; i < inputPaths.Length; i++) {
        string path = inputPaths[i];
        if (path == null || Path.GetFileNameWithoutExtension(path).Contains("title")) {
          continue;
        }

        string sectionHtml = ConvertSinglePathToSection(path);
        if (sectionHtml.Length > 0) {
          html.Append(sectionHtml);
        }

      }

      html.AppendLine("</div>");
      html.AppendLine("</body>");
      html.AppendLine("</html>");

      return html.ToString();
    }

    /// <summary>
    /// Writes the generated HTML string to a file path using UTF-8 without BOM.
    /// </summary>
    /// <param name="html">HTML content</param>
    /// <param name="outputPath">Output file path</param>
    public static void SaveHtmlToFile(string html, string outputPath) {
      if (html == null) {
        throw new ArgumentNullException("html");
      }

      if (outputPath == null) {
        throw new ArgumentNullException("outputPath");
      }

      try {
        byte[] bytes = new UTF8Encoding(false).GetBytes(html);
        File.WriteAllBytes(outputPath, bytes);
      }
      catch (UnauthorizedAccessException ex) {
        DevLogger.LogError(ex);
        throw;
      }
      catch (IOException ex) {
        DevLogger.LogError(ex);
        throw;
      }
    }

    /// <summary>
    /// Generates the cover page HTML block with optional image (above title) and optional background.
    /// </summary>
    /// <param name="title">Title text</param>
    /// <param name="author">Author text</param>
    /// <param name="coverImageDataUri">Optional image Data URI placed above title</param>
    /// <param name="coverBackgroundDataUri">Optional background image Data URI for the cover page</param>
    /// <returns>HTML for the cover section</returns>
    private static string GenerateCoverHtml(string title, string author, string coverImageDataUri, string coverBackgroundDataUri) {
      StringBuilder sb = new StringBuilder();
      sb.AppendLine("<section class=\"page cover\">");

      if (coverBackgroundDataUri != null && coverBackgroundDataUri.Length > 0) {
        sb.Append("<div class=\"cover__bg\" style=\"background-image:url('");
        sb.Append(coverBackgroundDataUri);
        sb.AppendLine("');\"></div>");
      }

      sb.AppendLine("<div class=\"cover__content\">");

      if (coverImageDataUri != null && coverImageDataUri.Length > 0) {
        sb.Append("<img class=\"cover__image\" alt=\"Cover\" src=\"");
        sb.Append(coverImageDataUri);
        sb.AppendLine("\"/>");
      }

      sb.Append("<h1 class=\"cover__title\">");
      sb.Append(EscapeHtml(title));
      sb.AppendLine("</h1>");

      if (author != null && author.Length > 0) {
        sb.Append("<p class=\"cover__author\">");
        sb.Append(EscapeHtml(author));
        sb.AppendLine("</p>");
      }

      sb.AppendLine("</div>");
      sb.AppendLine("</section>");
      return sb.ToString();
    }

    /// <summary>
    /// Converts a single input file to a page section with a header and content.
    /// </summary>
    /// <param name="path">Input file path</param>
    /// <returns>HTML for the section, or empty string on handled failure</returns>
    private static string ConvertSinglePathToSection(string path) {
      StringBuilder section = new StringBuilder();

      try {
        if (!File.Exists(path)) {
          DevLogger.LogTrace(0, 99999, "File not found, skipping: " + path);
          return string.Empty;
        }

        string ext = Path.GetExtension(path).ToLowerInvariant();
        string fileNameOnly = Path.GetFileName(path);

        section.AppendLine("<section class=\"page\">");
        //section.Append("<div class=\"section-header\">");
        //section.Append(EscapeHtml(fileNameOnly));
        //section.AppendLine("</div>");

        if (ext == ".md") {
          string markdown = File.ReadAllText(path, Encoding.UTF8);
          string baseDir = Path.GetDirectoryName(path);
          string htmlFromMd = ConvertMarkdownToHtml(markdown, baseDir);
          section.AppendLine(htmlFromMd);
        }
        else if (ext == ".txt") {
          string text = File.ReadAllText(path, Encoding.UTF8);
          section.Append("<pre>");
          section.Append(EscapeHtml(text));
          section.AppendLine("</pre>");
        }
        else if (ext == ".png" || ext == ".jpg" || ext == ".jpeg") {
          string dataUri = EncodeImageToDataUri(path);
          if (dataUri.Length > 0) {
            section.Append("<img alt=\"");
            section.Append(EscapeHtml(fileNameOnly));
            section.Append("\" src=\"");
            section.Append(dataUri);
            section.AppendLine("\"/>");
          }
        }
        else {
          DevLogger.LogTrace(0, 99999, "Unsupported extension, skipping: " + path);
        }

        section.AppendLine("</section>");
      }
      catch (UnauthorizedAccessException ex) {
        DevLogger.LogError(ex);
        return string.Empty;
      }
      catch (IOException ex) {
        DevLogger.LogError(ex);
        return string.Empty;
      }

      return section.ToString();
    }

    /// <summary>
    /// Converts a subset of Markdown to HTML, embedding referenced images as Base64 data URIs.
    /// Supported (essentials): headings, paragraphs, bold/italic, inline code, code fences, lists, links, images, blockquotes, hr.
    /// </summary>
    /// <param name="markdown">Markdown source</param>
    /// <param name="baseDirectory">Base directory for resolving relative image paths</param>
    /// <returns>HTML fragment</returns>
    private static string ConvertMarkdownToHtml(string markdown, string baseDirectory) {
      if (markdown == null) {
        return string.Empty;
      }

      // Normalize line endings
      string md = markdown.Replace("\r\n", "\n").Replace("\r", "\n");

      // Code fences (```lang ... ```)
      StringBuilder output = new StringBuilder();
      string[] lines = md.Split('\n');

      bool inCode = false;
      StringBuilder codeBuffer = new StringBuilder();

      // Simple stateful parsing
      for (int i = 0; i < lines.Length; i++) {
        string line = lines[i];

        if (IsFenceStartOrEnd(line)) {
          if (inCode) {
            // closing fence
            output.AppendLine("<pre><code>");
            output.Append(EscapeHtml(codeBuffer.ToString()));
            output.AppendLine("</code></pre>");
            codeBuffer.Clear();
            inCode = false;
          }
          else {
            // opening fence
            inCode = true;
            codeBuffer.Clear();
          }
          continue;
        }

        if (inCode) {
          codeBuffer.AppendLine(line);
          continue;
        }

        // Horizontal rule
        if (Regex.IsMatch(line, @"^\s*([-*_])\1\1(\1+)?\s*$")) {
          output.AppendLine("<hr/>");
          continue;
        }

        // Blockquote
        if (line.StartsWith(">")) {
          string quoteText = line.Substring(1).TrimStart();
          output.Append("<blockquote>");
          output.Append(ParseInlineMarkdown(quoteText, baseDirectory));
          output.AppendLine("</blockquote>");
          continue;
        }

        // Headings
        Match headingMatch = Regex.Match(line, @"^(#{1,6})\s+(.*)$");
        if (headingMatch.Success) {
          int level = headingMatch.Groups[1].Value.Length;
          string content = headingMatch.Groups[2].Value.Trim();
          output.Append("<h");
          output.Append(level.ToString());
          output.Append(">");
          output.Append(ParseInlineMarkdown(content, baseDirectory));
          output.Append("</h");
          output.Append(level.ToString());
          output.AppendLine(">");
          continue;
        }

        // Unordered list item
        if (Regex.IsMatch(line, @"^\s*[-+*]\s+")) {
          int startIndex = i;
          StringBuilder listBuilder = new StringBuilder();
          listBuilder.AppendLine("<ul>");
          while (i < lines.Length && Regex.IsMatch(lines[i], @"^\s*[-+*]\s+")) {
            string itemContent = Regex.Replace(lines[i], @"^\s*[-+*]\s+", "");
            listBuilder.Append("<li>");
            listBuilder.Append(ParseInlineMarkdown(itemContent, baseDirectory));
            listBuilder.AppendLine("</li>");
            i++;
          }
          listBuilder.AppendLine("</ul>");
          i = i - 1;
          output.Append(listBuilder.ToString());
          continue;
        }

        // Ordered list item
        if (Regex.IsMatch(line, @"^\s*\d+\.\s+")) {
          int startIndex = i;
          StringBuilder listBuilder = new StringBuilder();
          listBuilder.AppendLine("<ol>");
          while (i < lines.Length && Regex.IsMatch(lines[i], @"^\s*\d+\.\s+")) {
            string itemContent = Regex.Replace(lines[i], @"^\s*\d+\.\s+", "");
            listBuilder.Append("<li>");
            listBuilder.Append(ParseInlineMarkdown(itemContent, baseDirectory));
            listBuilder.AppendLine("</li>");
            i++;
          }
          listBuilder.AppendLine("</ol>");
          i = i - 1;
          output.Append(listBuilder.ToString());
          continue;
        }

        // Empty line = paragraph break
        if (line.Trim().Length == 0) {
          output.AppendLine("");
          continue;
        }

        // Paragraph
        output.Append("<p>");
        output.Append(ParseInlineMarkdown(line, baseDirectory));
        output.AppendLine("</p>");
      }

      return output.ToString();
    }

    /// <summary>
    /// Parses inline Markdown (bold/italic/code, links, images) and embeds images as Base64 data URIs.
    /// </summary>
    /// <param name="text">Inline markdown text</param>
    /// <param name="baseDirectory">Base directory for resolving relative image paths</param>
    /// <returns>HTML fragment</returns>
    private static string ParseInlineMarkdown(string text, string baseDirectory) {
      if (text == null) {
        return string.Empty;
      }

      string result = text;

      // Images: ![alt](path)
      result = Regex.Replace(
          result,
          @"!\[(.*?)\]\((.*?)\)",
          delegate (Match m) {
            string alt = m.Groups[1].Value;
            string url = m.Groups[2].Value;

            string dataUri = TryResolveImageToDataUri(url, baseDirectory);
            if (dataUri.Length > 0) {
              StringBuilder tag = new StringBuilder();
              tag.Append("<img alt=\"");
              tag.Append(EscapeHtml(alt));
              tag.Append("\" src=\"");
              tag.Append(dataUri);
              tag.Append("\"/>");
              return tag.ToString();
            }
            else {
              // Fallback: leave original (escaped) alt, keep src as-is
              StringBuilder tag = new StringBuilder();
              tag.Append("<img alt=\"");
              tag.Append(EscapeHtml(alt));
              tag.Append("\" src=\"");
              tag.Append(EscapeHtml(url));
              tag.Append("\"/>");
              return tag.ToString();
            }
          }
      );

      // Links: [text](url)
      result = Regex.Replace(
          result,
          @"\[(.*?)\]\((.*?)\)",
          delegate (Match m) {
            string label = m.Groups[1].Value;
            string url = m.Groups[2].Value;
            StringBuilder tag = new StringBuilder();
            tag.Append("<a href=\"");
            tag.Append(EscapeHtml(url));
            tag.Append("\">");
            tag.Append(EscapeHtml(label));
            tag.Append("</a>");
            return tag.ToString();
          }
      );

      // Bold: **text**
      result = Regex.Replace(result, @"\*\*(.+?)\*\*", "<strong>$1</strong>");

      // Italic: *text*
      result = Regex.Replace(result, @"\*(.+?)\*", "<em>$1</em>");

      // Inline code: `code`
      result = Regex.Replace(
          result,
          @"`([^`]+)`",
          delegate (Match m) {
            string code = m.Groups[1].Value;
            StringBuilder tag = new StringBuilder();
            tag.Append("<code>");
            tag.Append(EscapeHtml(code));
            tag.Append("</code>");
            return tag.ToString();
          }
      );

      return result;
    }

    /// <summary>
    /// Returns Base64 data URI for a supported image file, or empty string if unavailable.
    /// </summary>
    /// <param name="path">Image file path</param>
    /// <returns>data:image/...;base64,... or empty</returns>
    private static string EncodeImageToDataUri(string path) {
      try {
        if (path == null) {
          return string.Empty;
        }

        if (!File.Exists(path)) {
          return string.Empty;
        }

        byte[] bytes = File.ReadAllBytes(path);
        string base64 = Convert.ToBase64String(bytes);

        string ext = Path.GetExtension(path).ToLowerInvariant();
        string mime = "image/png";

        if (ext == ".png") {
          mime = "image/png";
        }
        else if (ext == ".jpg" || ext == ".jpeg") {
          mime = "image/jpeg";
        }
        else {
          // Default to octet-stream for safety
          mime = "application/octet-stream";
        }

        StringBuilder data = new StringBuilder();
        data.Append("data:");
        data.Append(mime);
        data.Append(";base64,");
        data.Append(base64);
        return data.ToString();
      }
      catch (UnauthorizedAccessException ex) {
        DevLogger.LogError(ex);
        return string.Empty;
      }
      catch (IOException ex) {
        DevLogger.LogError(ex);
        return string.Empty;
      }
    }

    /// <summary>
    /// If a file named exactly 'title' (case-insensitive) with supported image extension is present in inputs,
    /// returns its Base64 data URI. Otherwise returns empty string.
    /// </summary>
    /// <param name="inputPaths">All input paths</param>
    /// <returns>data URI or empty string</returns>
    private static string FindTitleImageDataUri(string[] inputPaths) {
      if (inputPaths == null || inputPaths.Length < 1) {
        return string.Empty;
      }


      //for (int i = 0; i < inputPaths.Length; i++) {
      //  string p = inputPaths[i];
      //  if (p == null) {
      //    continue;
      //  }

      var p = inputPaths[0];

      string name = Path.GetFileNameWithoutExtension(p);
      string ext = Path.GetExtension(p).ToLowerInvariant();
      if (name != null && name.Length > 0) {
        if (name.Contains("title")) {
          if (ext == ".png" || ext == ".jpg" || ext == ".jpeg") {
            string dataUri = EncodeImageToDataUri(p);
            if (dataUri.Length > 0) {
              return dataUri;
            }

          }
        }
      }
      // }

      return string.Empty;
    }

    /// <summary>
    /// Tries to convert a relative or absolute URL/path in Markdown image syntax to a Base64 data URI.
    /// </summary>
    /// <param name="urlOrPath">Markdown image target</param>
    /// <param name="baseDirectory">Base directory of the .md file</param>
    /// <returns>data URI or empty string</returns>
    private static string TryResolveImageToDataUri(string urlOrPath, string baseDirectory) {
      if (urlOrPath == null) {
        return string.Empty;
      }

      // Only embed local files; skip http/https
      if (urlOrPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || urlOrPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || urlOrPath.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) {
        return string.Empty;
      }

      string candidate = urlOrPath;

      if (!Path.IsPathRooted(candidate)) {
        if (baseDirectory != null && baseDirectory.Length > 0) {
          candidate = Path.Combine(baseDirectory, candidate);
        }
      }

      string normalized;
      try {
        normalized = Path.GetFullPath(candidate);
      }
      catch (Exception) {
        return string.Empty;
      }

      return EncodeImageToDataUri(normalized);
    }

    /// <summary>
    /// Encodes optional cover background, returning empty string if path == null/empty or unreadable.
    /// </summary>
    /// <param name="backgroundPath">Path to background image</param>
    /// <returns>data URI or empty</returns>
    private static string EncodeOptionalBackground(string backgroundPath) {
      if (backgroundPath == null) {
        return string.Empty;
      }

      if (backgroundPath.Length == 0) {
        return string.Empty;
      }

      return EncodeImageToDataUri(backgroundPath);
    }

    /// <summary>
    /// Escapes HTML special characters.
    /// </summary>
    /// <param name="text">Raw text</param>
    /// <returns>Escaped text</returns>
    private static string EscapeHtml(string text) {
      if (text == null) {
        return string.Empty;
      }

      StringBuilder sb = new StringBuilder(text.Length);
      for (int i = 0; i < text.Length; i++) {
        char c = text[i];
        if (c == '&') {
          sb.Append("&amp;");
        }
        else if (c == '<') {
          sb.Append("&lt;");
        }
        else if (c == '>') {
          sb.Append("&gt;");
        }
        else if (c == '"') {
          sb.Append("&quot;");
        }
        else if (c == '\'') {
          sb.Append("&#39;");
        }
        else {
          sb.Append(c);
        }
      }
      return sb.ToString();
    }

    /// <summary>
    /// Detects a Markdown code fence line (```).
    /// </summary>
    /// <param name="line">Input line</param>
    /// <returns>True if it's a fence delimiter</returns>
    private static bool IsFenceStartOrEnd(string line) {
      if (line == null) {
        return false;
      }

      if (Regex.IsMatch(line, @"^\s*```")) {
        return true;
      }

      return false;
    }
  }

}
