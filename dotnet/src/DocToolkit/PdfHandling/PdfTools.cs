using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Drawing;
using Logging.SmartStandards;
using Logging.SmartStandards.CopyForDocToolkit;

namespace KornSW.DocToolkit.PdfHandling {

  /// <summary>
  /// Helper class for executing wkhtmltopdf and related PDF tools.
  /// Encapsulates process execution, timeout handling and Windows scaling corrections.
  /// </summary>
  public class PdfTools {

    #region Path Configuration

    private static string _BinariesDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

    private PdfTools() {
    }

    /// <summary>
    /// Gets or sets the directory where wkhtmltopdf.exe is located.
    /// </summary>
    public static string BinariesDir {
      get { return _BinariesDir; }
      set { _BinariesDir = value; }
    }

    /// <summary>
    /// Timeout in minutes for external executable processes.
    /// </summary>
    public static int ExecutableTimeoutMinutes { get; set; } = 5;

    /// <summary>
    /// Full path to wkhtmltopdf.exe inside BinariesDir.
    /// </summary>
    private static string WkhtmlToPdfExecutablePath {
      get {
        return Path.Combine(_BinariesDir, "(Runtimes)", "wkhtmltopdf.exe");
      }
    }

    #endregion

    /// <summary>
    /// Runs wkhtmltopdf to convert an HTML file to PDF with given margins and scaling correction.
    /// </summary>
    /// <param name="pdfOutputFile">Target PDF file path.</param>
    /// <param name="topMargin">Top margin in millimeters.</param>
    /// <param name="bottomMargin">Bottom margin in millimeters.</param>
    /// <param name="htmlInputFile">Source HTML file path.</param>
    /// <returns>Exit code of wkhtmltopdf process.</returns>
    public static int RunWkHtmlToPdf(string htmlInputFile, string pdfOutputFile, int topMargin = 20, int bottomMargin = 20) {
      try {
        StringBuilder outputReceiver = new StringBuilder();
        int processExitCode;
        StringBuilder args = new StringBuilder();

        // Build argument list
        //args.Append("-L 0");
        //args.Append(" -R 0");
        args.Append("-L 20");
        args.Append(" -R 20");

        if (bottomMargin >= 0) {
          args.AppendFormat(" -B {0}", bottomMargin);
        }
        if (topMargin >= 0) {
          args.AppendFormat(" -T {0}", topMargin);
        }

        args.Append(" --disable-smart-shrinking");

        // Bugfix for scaling issues on high DPI displays
        decimal scaling = GetWindowsScaling();
        if (scaling != 1m) {
          args.AppendFormat(" --zoom {0:0.0}", scaling);
        }

        if (htmlInputFile.Contains(" ")) {
          args.AppendFormat(" \"{0}\"", htmlInputFile);
        }
        else {
          args.AppendFormat(" {0}", htmlInputFile);
        }

        if (pdfOutputFile.Contains(" ")) {
          args.AppendFormat(" \"{0}\"", pdfOutputFile);
        }
        else {
          args.AppendFormat(" {0}", pdfOutputFile);
        }

        string commandLineArguments = args.ToString();
        processExitCode = RunCommandLineExe(WkhtmlToPdfExecutablePath, commandLineArguments, (string s) => outputReceiver.Append(s));

        switch (processExitCode) {
          case 0:
            // success
            break;
          case 1:
            // also ok in some wkhtmltopdf versions
            break;
          default:
            if (outputReceiver.Length < 2) {
              throw new ApplicationException(string.Format("Process returned exit code {0}!", processExitCode));
            }
            else {
              throw new ApplicationException(string.Format("Process returned exit code {0}! Console output was: {1}", processExitCode, outputReceiver));
            }
        }

        return processExitCode;
      }
      catch (Exception ex) {
        DevLogger.LogError(ex);
        throw new ApplicationException(string.Format("Error executing '{0}': {1}", Path.GetFileName(WkhtmlToPdfExecutablePath), ex.Message), ex);
      }
    }

    #region Helper Methods

    /// <summary>
    /// Executes a command-line executable and captures standard output until process exits or timeout occurs.
    /// </summary>
    /// <param name="executable">Full path to executable.</param>
    /// <param name="arguments">Command-line arguments.</param>
    /// <param name="stdOutReceiver">Callback to receive standard output lines.</param>
    /// <returns>Exit code of the process.</returns>
    public static int RunCommandLineExe(string executable, string arguments, Action<string> stdOutReceiver) {
      ProcessStartInfo startInfo = new ProcessStartInfo();
      startInfo.UseShellExecute = false;
      startInfo.RedirectStandardOutput = true;
      startInfo.FileName = executable;
      startInfo.Arguments = arguments;
      startInfo.WindowStyle = ProcessWindowStyle.Hidden;
      startInfo.CreateNoWindow = true;

      int timeoutMinutes = ExecutableTimeoutMinutes;
      if (timeoutMinutes < 1) {
        timeoutMinutes = 0;
      }

      DateTime timeoutDeadline = DateTime.Now.AddMinutes(timeoutMinutes);
      Process processHandle = Process.Start(startInfo);

      while (!processHandle.HasExited) {
        if (processHandle.StandardOutput.Peek() > 0) {
          stdOutReceiver.Invoke(processHandle.StandardOutput.ReadToEnd());
        }
        else {
          Thread.Sleep(10);
        }

        if (DateTime.Now > timeoutDeadline) {
          try {
            processHandle.Kill();
          }
          catch {
          }
          throw new TimeoutException(string.Format("Timeout of {0} minutes has been reached", timeoutMinutes));
        }

        processHandle.Refresh();
      }

      return processHandle.ExitCode;
    }

    // Constants for GetDeviceCaps
    private const int LOGPIXELSX = 88;
    private const int LOGPIXELSY = 90;
    private const int ASPECTX = 40;
    private const int SCALINGFACTORX = 114;
    private const int SCALINGFACTORY = 115;
    private const int VERTRES = 10;
    private const int DESKTOPVERTRES = 117;

    /// <summary>
    /// Determines the Windows display scaling factor (DPI scaling).
    /// Returns 1.0 on standard DPI systems.
    /// </summary>
    /// <returns>Scaling factor (e.g., 1.25, 1.5).</returns>
    private static decimal GetWindowsScaling() {
      decimal result = 1m;
      IntPtr desktopHandle = IntPtr.Zero;

      try {
        desktopHandle = GetDC(IntPtr.Zero);
        int logicalScreenHeight = GetDeviceCaps(desktopHandle, VERTRES);
        int physicalScreenHeight = GetDeviceCaps(desktopHandle, DESKTOPVERTRES);
        result = Convert.ToDecimal((float)physicalScreenHeight / (float)logicalScreenHeight);
      }
      catch (Exception ex) {
        DevLogger.LogError(ex);
      }
      finally {
        try {
          ReleaseDC(IntPtr.Zero, desktopHandle);
        }
        catch {
        }
      }

      return result;
    }

    [DllImport("gdi32.dll")]
    private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    #endregion
  }

}
