using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;

namespace HtmlDocGen {

  namespace My.Resources {

    internal class MyEmbeddedFileRepository : EmbeddedFileRepository {

      public MyEmbeddedFileRepository() : base(Assembly.GetExecutingAssembly()) {
      }

    }

    #region  .My-Extension 

    internal static class EmbeddedFilesMyExtension {

      private static MyEmbeddedFileRepository _EmbeddedFiles = null;

      public static MyEmbeddedFileRepository EmbeddedFiles {
        get {
          if (_EmbeddedFiles == null) {
            _EmbeddedFiles = new MyEmbeddedFileRepository();
          }
          return _EmbeddedFiles;
        }
      }

    }

    #endregion

  }

  internal class EmbeddedFileRepository {

    #region  Declarations & Constructor 

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private IEnumerable<EmbeddedFile> _Files = null;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private Assembly _Assembly;

    public EmbeddedFileRepository(string assemblyFilename) : this(Assembly.LoadFile(assemblyFilename)) {
    }

    public EmbeddedFileRepository(Assembly @assembly) {
      _Assembly = assembly;
    }

    #endregion

    #region  Enumeration (Shared) 

    private static IEnumerable<EmbeddedFile> EnumerateResourceFilesFrom(Assembly sourceAssembly) {
      var files = new List<EmbeddedFile>();
      string ns = sourceAssembly.GetDefaultNamespace();

      foreach (string fullFileName in sourceAssembly.GetManifestResourceNames()) {
        if (!fullFileName.ToLower().EndsWith(".resources") && !fullFileName.ToLower().EndsWith(".resx")) {
          files.Add(new EmbeddedFile(fullFileName, sourceAssembly, ns));
        }
      }

      return files;
    }

    #endregion

    #region  Repository Access 

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public IEnumerable<EmbeddedFile> Files {
      get {
        if (_Files == null) {
          _Files = EnumerateResourceFilesFrom(_Assembly);
        }

        return _Files;
      }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public EmbeddedFile this[string name] {
      get {
        EmbeddedFile foundFile = null;

        foundFile = (from f in this.Files
                     where f.Name.ToLower() == name.ToLower()
                     select f).FirstOrDefault();
        if (foundFile != null) {
          return foundFile;
        }

        foundFile = (from f in this.Files
                     where f.FullName.ToLower() == name.ToLower()
                     select f).SingleOrDefault();
        return foundFile;
      }
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public bool Contains(string name) {
      if ((from f in this.Files
           where f.Name.ToLower() == name.ToLower()
           select f).FirstOrDefault() != null) {
        return true;
      }
      else {
        return (from f in this.Files
                where f.FullName.ToLower() == name.ToLower()
                select f).FirstOrDefault() != null;
      }
    }

    #endregion

  }

  [DebuggerDisplay("EmbeddedFile ({Name})")]
  internal class EmbeddedFile {

    #region  Declarations & Constructor 

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string _FullName;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private Assembly _ContainingAssembly;
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string _AssemblyDefaultNamespace;

    public EmbeddedFile(string fullName, Assembly containingAssembly, string assemblyDefaultNamespace) {

      _FullName = fullName;
      _ContainingAssembly = containingAssembly;
      _AssemblyDefaultNamespace = assemblyDefaultNamespace;

      if (!_AssemblyDefaultNamespace.EndsWith(".")) {
        _AssemblyDefaultNamespace = _AssemblyDefaultNamespace + ".";
      }

    }

    #endregion

    #region  Properties 

    public string Name {
      get {
        return _FullName.Substring(_AssemblyDefaultNamespace.Length, _FullName.Length - _AssemblyDefaultNamespace.Length);
      }
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public string FullName {
      get {
        return _FullName;
      }
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public override string ToString() {
      return this.FullName;
    }

    #endregion

    #region  Content Access 

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public Stream OpenStream() {
      return _ContainingAssembly.GetManifestResourceStream(_FullName);
    }

    public void ExtractToFilesystem(string targetPath) {

      if (string.IsNullOrEmpty(Path.GetFileName(targetPath))) {
        targetPath = targetPath + Path.DirectorySeparatorChar + this.Name;
      }

      using (var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write)) {

        using (var resStream = this.OpenStream()) {
          resStream.CopyTo(fileStream);
        }

        fileStream.Close();
      }

    }

    public string ReadText() {
      string buffer;

      using (var resStream = this.OpenStream()) {

        using (var resStreamReader = new StreamReader(resStream, true)) {
          buffer = resStreamReader.ReadToEnd();
        }

        resStream.Close();
      }

      return buffer;
    }

#if NET46
    public Image ReadImage() {
      Image clonedInstance;

      using (var resStream = this.OpenStream()) {

        using (var buffer = Image.FromStream(resStream)) {

          // if we dont do this, the image gets lost when closing the stream
          clonedInstance = new Bitmap(buffer.Width, buffer.Height);
          using (var gfxForCloning = Graphics.FromImage(clonedInstance)) {
            gfxForCloning.DrawImage(buffer, new Point(0, 0));
          }

        }

        resStream.Close();
      }

      return clonedInstance;
    }
#else
    [Obsolete("Images only supported in .NET Framework 4.x versions", true)]
    public object ReadImage() {
      //TODO: Image-Support reparieren
      throw new NotSupportedException("Images only supported in .NET Framework 4.x versions");
    }
#endif

    public byte[] ReadBinary() {
      byte[] buffer;

      using (var resStream = this.OpenStream()) {
        buffer = new byte[(int)(resStream.Length - 1L) + 1];
        resStream.Read(buffer, 0, (int)resStream.Length);
        resStream.Close();
      }

      return buffer;
    }

    #endregion

  }

  #region  Publishing via Extension 

  internal static class ExtenstionsForReflectionAssembly {

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static EmbeddedFileRepository EmbeddedFiles(this Assembly @assembly) {
      return new EmbeddedFileRepository(assembly);
    }

  }
}

#endregion
