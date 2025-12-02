using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace System.Reflection {

  [Obsolete("Kommt aus aus kSystemExtensions")]
  internal static class ExtensionsForAssembly {

    //[EditorBrowsable(EditorBrowsableState.Always)]
    //public static bool IsDirectReferencedBy(this Assembly targetAssembly, Assembly sourceAssembly) {
    //  if ((targetAssembly.FullName ?? "") == (sourceAssembly.FullName ?? "")) {
    //    return true;
    //  }
    //  return sourceAssembly.GetReferencedAssemblies().Where(a => (a.FullName ?? "") == (targetAssembly.FullName ?? "")).Any();
    //}

    //[EditorBrowsable(EditorBrowsableState.Always)]
    //public static bool KnowsAssembly(this Assembly sourceAssembly, Assembly targetAssembly) {
    //  return sourceAssembly.KnowsAssembly(targetAssembly.GetName());
    //}

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static bool KnowsAssembly(this Assembly sourceAssembly, AssemblyName targetAssemblyName) {
      string sourceAssemblyName = sourceAssembly.GetName().FullName;
      if ((sourceAssemblyName ?? "") == (targetAssemblyName.FullName ?? "")) {
        return true;
      }

      AssemblyName[] referencedAssemblies = sourceAssembly.GetReferencedAssemblies();

      // soft search
      foreach (var assName in referencedAssemblies) {
        if ((sourceAssemblyName ?? "") == (assName.FullName ?? "")) {
          return true;
        }
      }

      // deep search
      foreach (var assName in referencedAssemblies) {
        var ass = Assembly.Load(assName);
        if (ass.KnowsAssembly(targetAssemblyName)) {
          return true;
        }
      }

      return false;
    }

    //[EditorBrowsable(EditorBrowsableState.Always)]
    //public static bool IsSystemAssembly(this Assembly extendee) {

    //  if (extendee.FullName.StartsWith("Microsoft")) {
    //    return true;
    //  }
    //  if (extendee.FullName.StartsWith("System.")) {
    //    return true;
    //  }

    //  if (extendee.IsDefined(typeof(AssemblyCompanyAttribute), true)) {
    //    AssemblyCompanyAttribute companyAttribute = (AssemblyCompanyAttribute)extendee.GetCustomAttributes(typeof(AssemblyCompanyAttribute), true)[0];

    //    if (companyAttribute.Company.ToLower().Contains("microsoft")) {
    //      return true;
    //    }
    //  }

    //  return false;
    //}

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static bool IsDotNetAssembly(this FileInfo assemblyFile) {
      try {
        var a = Assembly.LoadFrom(assemblyFile.FullName);
        return a != null;
      }
      catch (BadImageFormatException ex) {
        return false;
      }
    }

    ///// <summary>
    ///// Generates an unique Fingerprint for an Assembly using the PublicKeyToken
    ///// </summary>
    //[EditorBrowsable(EditorBrowsableState.Always)]
    //public static SecureString GetFingerPrint(this Assembly @assembly) {
    //  var fingerPrint = new SecureString();
    //  try {
    //    byte[] pk = assembly.GetName().GetPublicKeyToken();
    //    if (pk != null) {
    //      fingerPrint.AppendBytes(pk);
    //      return fingerPrint;
    //    }
    //  }
    //  catch {
    //  }
    //  fingerPrint.AppendBytes(Encoding.ASCII.GetBytes(assembly.GetName().Name));
    //  return fingerPrint;
    //}


    [EditorBrowsable(EditorBrowsableState.Always)]
    public static T GetAssemblyInfo<T>(this Assembly sourceInstance) where T : Attribute {
      return sourceInstance.GetCustomAttributes(true).OfType<T>().FirstOrDefault();
    }

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static string GetAssemblyCompany(this Assembly sourceInstance) {
      var attrib = sourceInstance.GetAssemblyInfo<AssemblyCompanyAttribute>();
      if (attrib is null) {
        return string.Empty;
      }
      else {
        return attrib.Company;
      }
    }

    //[EditorBrowsable(EditorBrowsableState.Always)]
    //public static string GetAssemblyTitle(this Assembly sourceInstance) {
    //  var attrib = sourceInstance.GetAssemblyInfo<AssemblyTitleAttribute>();
    //  if (attrib is null) {
    //    return string.Empty;
    //  }
    //  else {
    //    return attrib.Title;
    //  }
    //}

    //[EditorBrowsable(EditorBrowsableState.Always)]
    //public static string GetAssemblyDescription(this Assembly sourceInstance) {
    //  var attrib = sourceInstance.GetAssemblyInfo<AssemblyDescriptionAttribute>();
    //  if (attrib is null) {
    //    return string.Empty;
    //  }
    //  else {
    //    return attrib.Description;
    //  }
    //}

    //[EditorBrowsable(EditorBrowsableState.Always)]
    //public static string GetAssemblyProduct(this Assembly sourceInstance) {
    //  var attrib = sourceInstance.GetAssemblyInfo<AssemblyProductAttribute>();
    //  if (attrib is null) {
    //    return string.Empty;
    //  }
    //  else {
    //    return attrib.Product;
    //  }
    //}

    //[EditorBrowsable(EditorBrowsableState.Always)]
    //public static string GetAssemblyCopyright(this Assembly sourceInstance) {
    //  var attrib = sourceInstance.GetAssemblyInfo<AssemblyCopyrightAttribute>();
    //  if (attrib is null) {
    //    return string.Empty;
    //  }
    //  else {
    //    return attrib.Copyright;
    //  }
    //}

    //[EditorBrowsable(EditorBrowsableState.Always)]
    //public static string GetAssemblyTrademark(this Assembly sourceInstance) {
    //  var attrib = sourceInstance.GetAssemblyInfo<AssemblyTrademarkAttribute>();
    //  if (attrib is null) {
    //    return string.Empty;
    //  }
    //  else {
    //    return attrib.Trademark;
    //  }
    //}

    //[EditorBrowsable(EditorBrowsableState.Always)]
    //public static Guid GetAssemblyComGuid(this Assembly sourceInstance) {
    //  var attrib = sourceInstance.GetAssemblyInfo<GuidAttribute>();
    //  if (attrib is null) {
    //    return Guid.Empty;
    //  }
    //  else {
    //    return Guid.Parse(attrib.Value);
    //  }
    //}

    //[EditorBrowsable(EditorBrowsableState.Always)]
    //public static Version GetAssemblyVersion(this Assembly sourceInstance) {
    //  var attrib = sourceInstance.GetAssemblyInfo<AssemblyVersionAttribute>();
    //  if (attrib is null) {
    //    return new Version(0, 0, 0, 0);
    //  }
    //  else {
    //    return Version.Parse(attrib.Version);
    //  }
    //}

    //[EditorBrowsable(EditorBrowsableState.Always)]
    //public static Version GetAssemblyFileVersion(this Assembly sourceInstance) {
    //  var attrib = sourceInstance.GetAssemblyInfo<AssemblyFileVersionAttribute>();
    //  if (attrib is null) {
    //    return new Version(0, 0, 0, 0);
    //  }
    //  else {
    //    return Version.Parse(attrib.Version);
    //  }
    //}

    //[EditorBrowsable(EditorBrowsableState.Always)]
    //public static Version GetAssemblyInformationalVersion(this Assembly sourceInstance) {
    //  var attrib = sourceInstance.GetAssemblyInfo<AssemblyInformationalVersionAttribute>();
    //  if (attrib is null) {
    //    return new Version(0, 0, 0, 0);
    //  }
    //  else {
    //    return Version.Parse(attrib.InformationalVersion);
    //  }
    //}

    //[EditorBrowsable(EditorBrowsableState.Always)]
    //public static bool IsInMemory(this Assembly sourceInstance) {
    //  return sourceInstance.IsDynamic;
    //}

    //[EditorBrowsable(EditorBrowsableState.Always)]
    //public static Resources.ResourceManager GetResourceManager(this Assembly sourceInstance) {
    //  return new Resources.ResourceManager("Resources", sourceInstance);
    //}

    [EditorBrowsable(EditorBrowsableState.Always)]
    public static string GetDefaultNamespace(this Assembly @assembly) {
      var referenceType = (from t in @assembly.GetTypesAccessable()
                           where t.FullName.Contains(".My.")
                           select t).FirstOrDefault();
      if (referenceType is null) {
        return string.Empty;
      }
      else {
        return referenceType.FullName.Substring(0, referenceType.FullName.IndexOf(".My."));
      }
    }


    /// <summary>
    /// A really important secret is that the common method "GetTypes()" will fail
    /// if the assembly contains one or more types with broken references (to non existing assemblies).
    /// This method 'GetTypesAccessable()' will handle this problem and return at least those types,
    /// which could be loaded!
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Always)]
    public static Type[] GetTypesAccessable(this Assembly @assembly) {
      return assembly.GetTypesAccessable(ex => Trace.TraceWarning(ex.Message));
    }

    /// <summary>
    /// A really important secret is that the common method "GetTypes()" will fail
    /// if the assembly contains one or more types with broken references (to non existing assemblies).
    /// This method 'GetTypesAccessable()' will handle this problem and return at least those types,
    /// which could be loaded!
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Always)]
    public static Type[] GetTypesAccessable(this Assembly @assembly, Action<Exception> loaderExceptionHandler) {
      try {
        return assembly.GetTypes();
      }
      catch (ReflectionTypeLoadException ex) {
        foreach (var le in ex.LoaderExceptions)
          loaderExceptionHandler.Invoke(le);
        // This ugly workarround is the only way to get the types from a asembly
        // which contains one or more types with broken references f.e:
        // [Type1] good!
        // [Type2] good!
        // [Type3] INHERITS [<non-exitings-assembly>.BaseType3] bad!
        return ex.Types.Where(t => t != null).ToArray();
      }
    }

  }

}