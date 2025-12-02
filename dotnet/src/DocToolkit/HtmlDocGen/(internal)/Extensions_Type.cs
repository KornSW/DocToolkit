using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

internal static class ExtensionsForType {

  [EditorBrowsable(EditorBrowsableState.Always), Obsolete("Kommt aus aus kSystemExtensions")]
  public static bool TryParse(this Type targetType, string sourceText, ref object target) {
    try {

      if (targetType.IsEnum) {
        if (Enum.GetNames(targetType).Select(n => n.ToLower()).Contains(sourceText)) {
          target = Enum.Parse(targetType, sourceText, true);
          return true;
        }
      }

      // special fixes
      switch (targetType) {
        case var @case when @case == typeof(string): {
            target = sourceText;
            return true;
          }
        case var case1 when case1 == typeof(bool): {
            if (sourceText == "1") {
              sourceText = "true";
            }
            if (sourceText == "0") {
              sourceText = "false";
            }

            break;
          }
      }

      var flagMask = BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod;

      var parseMethod = targetType.GetMethods(flagMask).Where(m => m.Name == "TryParse" && m.GetParameters().Count() == 2 && m.ReturnType == typeof(bool) && m.GetParameters()[0].ParameterType == typeof(string) && m.GetParameters()[1].ParameterType == targetType.MakeByRefType() && m.GetParameters()[1].IsOut).FirstOrDefault();

      if (parseMethod == null) {
        return false;
      }

      else {
        object[] callParams = new object[] { sourceText, null };
        bool success = (bool)parseMethod.Invoke(null, callParams);
        if (success) {
          target = callParams[1];
        }
        return success;
      }
    }
    catch {
      return false;
    }
  }

  [EditorBrowsable(EditorBrowsableState.Always)]
  public static string FriendlyName(this Type extendee) {
    var sb = new StringBuilder();
    sb.Append(extendee.Namespace);
    sb.Append('.');
    Type[] genArgs = extendee.GetGenericArguments();
    if (genArgs != null && genArgs.Length > 0) {
      sb.Append(extendee.Name.Substring(0, extendee.Name.IndexOf('`')));
      bool first = true;
      sb.Append('(');
      foreach (var genArg in genArgs) {
        if (first) {
          first = false;
          sb.Append("Of ");
        }
        else {
          sb.Append(", ");
        }
        sb.Append(genArg.FriendlyName());
      }
      sb.Append(')');
    }
    else {
      sb.Append(extendee.Name);
    }
    return sb.ToString();
  }

  [EditorBrowsable(EditorBrowsableState.Always)]
  public static IEnumerable<PropertyInfo> WithIndexParams(this IEnumerable<PropertyInfo> extendee, int count = -1) {
    return from pi in extendee
           where pi.HasIndexParams(count)
           select pi;
  }

  //[EditorBrowsable(EditorBrowsableState.Always)]
  //public static string GetEnumName<TEnum>(this TEnum enumValue) where TEnum : struct {
  //  if (typeof(TEnum).IsEnum) {
  //    if (Enum.IsDefined(typeof(TEnum), enumValue)) {
  //      return Enum.GetName(typeof(TEnum), enumValue);
  //    }
  //    else {
  //      return null;
  //    }
  //  }
  //  else {
  //    return null;
  //  }
  //}

  //[EditorBrowsable(EditorBrowsableState.Always)]
  //public static bool IsList(this Type extendee) {
  //  var @type = extendee;
  //  if (extendee.IsGenericType) {
  //    type = type.GetGenericTypeDefinition();
  //  }
  //  return typeof(IList).IsAssignableFrom(type) || typeof(IList<>).IsAssignableFrom(type);
  //}

  [EditorBrowsable(EditorBrowsableState.Always)]
  public static bool HasIndexParams(this PropertyInfo extendee, int count = -1) {
    ParameterInfo[] p = extendee.GetIndexParameters();
    if (count == -1) {
      return p.Any();
    }
    int min = 0;
    int max = 0;
    foreach (var pi in p) {
      max += 1;
      if (!pi.IsOptional) {
        min += 1;
      }
    }
    return min <= count && count <= max;
  }

  ///// <summary>
  ///// Dynamically creates an List(Of ) type with the current type for the items.
  ///// The returned Type can be activated and casted to 'IList'
  ///// </summary>
  //[EditorBrowsable(EditorBrowsableState.Advanced)]
  //public static Type MakeListType(this Type itemType) {
  //  return typeof(List<>).MakeGenericType(itemType);
  //}


  [EditorBrowsable(EditorBrowsableState.Always)]
  public static IEnumerable<PropertyInfo> GetDefaultProperties(this Type extendee) {
    return extendee.GetDefaultMembers().OfType<PropertyInfo>();
  }

  [EditorBrowsable(EditorBrowsableState.Always)]
  public static bool IsDefaultProperty(this PropertyInfo extendee) {
    return extendee.DeclaringType.GetDefaultProperties().Contains(extendee);
  }

  //[EditorBrowsable(EditorBrowsableState.Always)]
  //public static object GetValueFrom(this Type extendee, object instance, params string[] propertyPath) {
  //  return extendee.GetValueFrom(instance, string.Join(".", propertyPath));
  //}

  [EditorBrowsable(EditorBrowsableState.Always)]
  public static object GetValueFrom(this Type extendee, object instance, string memberPath) {
    if (memberPath.Length < 1) {
      return null;
    }
    if (!extendee.IsSoftAssignableFrom(instance.GetType())) {
      return null;
    }


    // multiples are definitly defaultprop accessors
    memberPath = memberPath.Replace(")(", ").(");

    string currentPart;
    if (memberPath.Contains(".")) {
      currentPart = memberPath.Substring(0, memberPath.IndexOf("."));
    }
    else {
      currentPart = memberPath;
    }

    string[] args = Array.Empty<string>();
    if (currentPart.Contains("(")) {
      int idx = currentPart.IndexOf("(");
      args = currentPart.Substring(idx + 1, currentPart.Length - idx - 2).Split(',');
      currentPart = currentPart.Substring(0, idx);
    }

    PropertyInfo foundProp = null;
    var typedArgs = new List<object>();
    foreach (var prop in extendee.GetProperties().WithIndexParams(args.Length)) {
      bool isCompatible;
      if (string.IsNullOrWhiteSpace(currentPart)) {
        isCompatible = prop.IsDefaultProperty();
      }
      else {
        isCompatible = (prop.Name.ToLower() ?? "") == (currentPart.ToLower() ?? "");
      }
      if (isCompatible) {
        typedArgs.Clear();
        for (int i = 0, loopTo = args.Length - 1; i <= loopTo; i++) {
          object parsedArg = null;
          if (prop.GetIndexParameters()[i].ParameterType.TryParse(args[i], ref parsedArg)) {
            typedArgs.Add(parsedArg);
          }
          else {
            isCompatible = false;
            break;
          }
        }
      }
      if (isCompatible && prop.CanRead) {
        foundProp = prop;
        break;
      }
    }

    object subObj;
    Type subType;

    // the default property was used, we need to retry this in two steps
    if (foundProp == null) {

      if (!string.IsNullOrEmpty(currentPart) && currentPart.Length < memberPath.Length && !memberPath.StartsWith(currentPart + ".")) { // if not already using the default property
        return extendee.GetValueFrom(instance, memberPath.Insert(currentPart.Length, "."));
      }

      else if (extendee.IsArray && extendee.GetArrayRank() == args.Length) {
        var typedArrayIndices = new List<int>();
        for (int i = 0, loopTo1 = args.Length - 1; i <= loopTo1; i++) {
          int parsedArg = 0;
          if (int.TryParse(args[i], out parsedArg)) {
            typedArrayIndices.Add(parsedArg);
          }
          else {
            return false;
          }
        }
        subObj = ((Array)instance).GetValue(typedArrayIndices.ToArray());
        subType = extendee.GetElementType();
      }

      else if (typeof(IEnumerable).IsSoftAssignableFrom(extendee) && args.Length == 1) {
        int index;
        if (int.TryParse(args[0], out index)) {
         
          subObj = ((IEnumerable)instance).OfType<object>().Skip(index).First();

          if (extendee.GenericTypeArguments.Any()) {
            subType = extendee.GenericTypeArguments.First();
          }
          else {
            subType = subObj.GetType();
          }
        }

        else {
          return null;
        }
      }

      else {
        return null;
      }
    }

    else {
      if (args.Length > 0) {
        subObj = foundProp.GetValue(instance, typedArgs.ToArray());
      }
      else {
        subObj = foundProp.GetValue(instance, (object[])null);
      }
      subType = foundProp.PropertyType;
    }

    if (memberPath.Contains(".")) {
      int idx = memberPath.IndexOf(".") + 1;
      return subType.GetValueFrom(subObj, memberPath.Substring(idx, memberPath.Length - idx));
    }
    else {
      return subObj;
    }

  }

  /// <summary>
  /// Returns the name of a type, defined by the DisplayNameAttribute. If no DisplayNameAttribute is defined, it returns the name of the type.
  /// </summary>
  /// <param name="extendee">The existing type to be extended.</param>
  [EditorBrowsable(EditorBrowsableState.Always)]
  public static string DisplayName(this Type extendee) {
    if (extendee.IsDefined(typeof(DisplayNameAttribute), false)) {
      DisplayNameAttribute attribute = (DisplayNameAttribute)extendee.GetCustomAttributes(typeof(DisplayNameAttribute), false)[0];

      return attribute.DisplayName;
    }

    return extendee.Name;
  }

  ///// <summary>
  ///// Returns wether the given type is a collection or not.
  ///// </summary>
  ///// <param name="extendee">The existing type to be extended.</param>
  //[EditorBrowsable(EditorBrowsableState.Always)]
  //public static bool IsCollection(this Type extendee) {
  //  if (extendee == null || ReferenceEquals(extendee, typeof(string))) {
  //    return false;
  //  }

  //  return typeof(IEnumerable).IsSoftAssignableFrom(extendee);
  //}

  //[EditorBrowsable(EditorBrowsableState.Always)]
  //public static bool IsParameterlessInstantiable(this Type extendee) {

  //  if (extendee.IsPrimitive) {
  //    return true;
  //  }

  //  if (!extendee.IsClass || extendee.IsAbstract) {
  //    return false;
  //  }

  //  if (extendee.GetConstructor(BindingFlags.CreateInstance | BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null) == null) {
  //    return false; // no parameterless constructor
  //  }

  //  return true;
  //}

  /// <returns>True, if the given type is a atomic type, otherwise false.</returns>
  [EditorBrowsable(EditorBrowsableState.Always)]
  public static bool IsAtomic(this Type extendee) {
    if (extendee.IsPrimitive) {
      return true;
    }
    if (extendee.IsEnum) {
      return true;
    }
    switch (extendee) {
      case var @case when @case == typeof(int):
      case var case1 when case1 == typeof(short):
      case var case2 when case2 == typeof(int): {
          break;
        }
      case var case3 when case3 == typeof(uint):
      case var case4 when case4 == typeof(ushort):
      case var case5 when case5 == typeof(uint): {
          break;
        }
      case var case6 when case6 == typeof(long):
      case var case7 when case7 == typeof(long): {
          break;
        }
      case var case8 when case8 == typeof(ulong):
      case var case9 when case9 == typeof(ulong): {
          break;
        }
      case var case10 when case10 == typeof(decimal):
      case var case11 when case11 == typeof(decimal): {
          break;
        }
      case var case12 when case12 == typeof(float):
      case var case13 when case13 == typeof(float): {
          break;
        }
      case var case14 when case14 == typeof(double):
      case var case15 when case15 == typeof(double): {
          break;
        }
      case var case16 when case16 == typeof(bool):
      case var case17 when case17 == typeof(bool): {
          break;
        }
      case var case18 when case18 == typeof(byte):
      case var case19 when case19 == typeof(byte): {
          break;
        }
      case var case20 when case20 == typeof(string):
      case var case21 when case21 == typeof(string): {
          break;
        }
      case var case22 when case22 == typeof(DateTime): {
          break;
        }
      case var case23 when case23 == typeof(Version): {
          break;
        }
      case var case24 when case24 == typeof(Guid): {
          break;
        }

      default: {
          return false;
        }
    }
    return true;
  }

  /// <summary>
  /// Returns wether the given type is a nullable type or not.
  /// </summary>
  /// <param name="extendee">The existing type to be extended.</param>
  /// <returns>True, if the given type is a nullable type, otherwise false.</returns>
  [EditorBrowsable(EditorBrowsableState.Always)]
  public static bool IsNullableType(this Type extendee) {
    if (extendee.IsGenericType && ReferenceEquals(extendee.GetGenericTypeDefinition(), typeof(object))) {
      return true;
    }

    return false;
  }

  [EditorBrowsable(EditorBrowsableState.Always)]
  public static bool IsSoftAssignableFrom(this Type extendee, Type c) {
    return extendee.IsAssignableFrom(c);
  }

  [EditorBrowsable(EditorBrowsableState.Always)]
  public static bool IsSoftAssignableTo(this Type extendee, Type c) {
    return c.IsSoftAssignableFrom(extendee);
  }

  [EditorBrowsable(EditorBrowsableState.Always)]
  public static bool IsAssignableTo(this Type extendee, Type c) {
    return c.IsAssignableFrom(extendee);
  }


  /// <summary> Returns the first matching Method of the given attribute type. </summary>
  /// <param name="extendee"> The type to be searched in. </param>
  /// <typeparam name="TAttribute"> The desired attribute. </typeparam>
  /// <returns> The methodInfo of the found method or nothing. </returns>
  [EditorBrowsable(EditorBrowsableState.Always)]
  public static MethodInfo GetMethod<TAttribute>(this Type extendee) {
    if (extendee == null) {
      return null;
    }

    return (from n in extendee.GetMethods()
            where n.GetCustomAttributes(typeof(TAttribute), true).Length > 0
            select n).FirstOrDefault();
  }

  [EditorBrowsable(EditorBrowsableState.Always)]
  public static object Activate(this Type typeToActivate) {
    return typeToActivate.Activate(null);
  }

  [EditorBrowsable(EditorBrowsableState.Always)]
  public static object Activate(this Type typeToActivate, params object[] args) {
    if (typeToActivate.IsArray) {
      Array target;
      if (args != null && args.Length > 0) {
        target = Array.CreateInstance(typeToActivate.GetItemType(), args.Length);
        for (int i = 0, loopTo = target.Length - 1; i <= loopTo; i++)
          target.SetValue(args[i], i);
      }
      else {
        target = Array.CreateInstance(typeToActivate.GetItemType(), 0);
      }
      return target;
    }
    else if (args != null && args.Length > 0) {
      return Activator.CreateInstance(typeToActivate, args);
    }
    else if (typeToActivate.HasParameterlessConstructor()) {
      return Activator.CreateInstance(typeToActivate);
    }
    else if (typeToActivate == typeof(string)) {
      return string.Empty;
    }
    else {
      object newInst = null;
      typeToActivate.TryParse(string.Empty, ref newInst);
      return newInst;
    }
  }

  //[EditorBrowsable(EditorBrowsableState.Always)]
  //public static TCastTargetType Activate<TCastTargetType>(this Type typeToActivate) {
  //  return (TCastTargetType)typeToActivate.Activate();
  //}

  [EditorBrowsable(EditorBrowsableState.Always)]
  public static TCastTargetType Activate<TCastTargetType>(this Type typeToActivate, params object[] args) {
    return (TCastTargetType)typeToActivate.Activate(args);
  }

  [EditorBrowsable(EditorBrowsableState.Always)]
  public static bool HasParameterlessConstructor(this Type t) {
    if (!t.IsClass || t.IsAbstract) {
      return false;
    }
    foreach (var c in t.GetConstructors()) {
      if (c.IsPublic && c.GetParameters().Length < 1) {
        return true;
      }
    }
    return false;
  }

  [EditorBrowsable(EditorBrowsableState.Always)]
  public static bool HasCustomAttribute<T>(this Type extendee, Predicate<T> checkCriteria) {

    T attribute = (T)extendee.GetCustomAttributes(typeof(T), true).FirstOrDefault();
    if (attribute == null) {
      return false;
    }

    return checkCriteria.Invoke(attribute);
  }

  [EditorBrowsable(EditorBrowsableState.Always)]
  public static bool HasCustomAttribute<T>(this Type extendee) {
    T attribute = (T)extendee.GetCustomAttributes(typeof(T), true).FirstOrDefault();
    return attribute != null;
  }

  [EditorBrowsable(EditorBrowsableState.Always)]
  public static T CustomAttribute<T>(this Type extendee) {
    T attribute = (T)extendee.GetCustomAttributes(typeof(T), true).FirstOrDefault();
    return attribute;
  }

  // <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
  // Public Function SearchExtensionMethods(t As Type, includedAssemblyNamePattern As String) As MethodInfo()

  // Dim types As New List(Of Type)()

  // Dim assemblies = From anyLoadedAssembly In AppDomain.CurrentDomain.GetAssemblies()
  // Where anyLoadedAssembly.FullName.MatchesWildcardMask(includedAssemblyNamePattern)
  // Select anyLoadedAssembly

  // For Each item As Assembly In assemblies
  // types.AddRange(item.GetTypes())
  // Next

  // Dim query = From type In types
  // Where type.IsSealed AndAlso Not type.IsGenericType AndAlso Not type.IsNested
  // From method In type.GetMethods(BindingFlags.[Static] Or BindingFlags.[Public] Or BindingFlags.NonPublic)
  // Where method.IsDefined(GetType(ExtensionAttribute), False)
  // Where method.ReturnType IsNot GetType(Void)
  // Where method.GetParameters.Any() AndAlso method.GetParameters().Count = 1 AndAlso method.GetParameters()(0).ParameterType = t
  // Select method

  // Return query.ToArray()
  // End Function

  [EditorBrowsable(EditorBrowsableState.Always)]
  public static bool HasDefaultConstructor(this Type extendee) {
    return extendee.IsPrimitive || extendee.GetConstructor(BindingFlags.CreateInstance | BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null) != null;
  }

  [EditorBrowsable(EditorBrowsableState.Advanced)]
  public static bool HasGenericConstraint(this Type extendee, GenericParameterAttributes constraint) {
    return (extendee.GenericParameterAttributes & constraint) == constraint;
  }

  [EditorBrowsable(EditorBrowsableState.Advanced)]
  public static bool HasGenericConstraintNew(this Type extendee) {
    return extendee.HasGenericConstraint(GenericParameterAttributes.DefaultConstructorConstraint);
  }

  [EditorBrowsable(EditorBrowsableState.Advanced)]
  public static bool HasGenericConstraintClass(this Type extendee) {
    return extendee.HasGenericConstraint(GenericParameterAttributes.ReferenceTypeConstraint);
  }

  //[EditorBrowsable(EditorBrowsableState.Always)]
  //public static IEnumerable<MethodInfo> GetExtensionMethods(this Type extendee) {
  //  // we need caching, so lets redirect...
  //  return System.Reflection.ExtensionMethods.OfType(extendee);
  //}

  [EditorBrowsable(EditorBrowsableState.Always)]
  public static TypeConverter GetConverter(this Type instance) {
    var t = instance.GetType();
    try {
      if (t.HasCustomAttribute<TypeConverterAttribute>()) {
        var a = t.GetCustomAttribute<TypeConverterAttribute>(true);
        var converterType = Type.GetType(a.ConverterTypeName, false);
        if (converterType != null) {
          return converterType.Activate<TypeConverter>(new[] { t });
        }
      }
    }
    catch {
    }
    return null;
  }

  [EditorBrowsable(EditorBrowsableState.Advanced)]
  public static Type GetNonGenericBaseType(this Type extendee) {
    // actual the only indicator to identify generic parameter types
    if (extendee.FullName == null) {
      return extendee.BaseType.GetNonGenericBaseType();
    }
    else {
      return extendee;
    }
  }

  [EditorBrowsable(EditorBrowsableState.Advanced)]
  public static Type GetNonDynamicBaseType(this Type extendee) {
    if (extendee.Assembly.IsDynamic) {
      if (extendee.BaseType == null) {
        return null;
      }
      else {
        return extendee.BaseType.GetNonDynamicBaseType();
      }
    }
    else {
      return extendee;
    }
  }

  // <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
  // Public Function IsDictionary(t As Type) As Boolean
  // Dim dictType As Type = GetType(Dictionary(Of ,))
  // Return (t = dictType OrElse t.HasGenericBase(dictType))
  // End Function

  [EditorBrowsable(EditorBrowsableState.Always)]
  public static bool HasGenericBase<TBase>(this Type t) {
    return t.HasGenericBase(typeof(TBase));
  }

  [EditorBrowsable(EditorBrowsableState.Always)]
  public static bool HasGenericBase(this Type t, Type @base) {
    if (t.IsGenericType) {
      if (t.GetGenericTypeDefinition() == @base) {
        return true;
      }
    }
    return false;
  }

  /// <summary>
  /// This is the same like 'GetElementType', but it also works on every IEnumerable
  /// (not only on arrays)
  /// </summary>
  [EditorBrowsable(EditorBrowsableState.Always)]
  public static Type GetItemType(this Type enumerableType) {

    if (enumerableType.IsArray) {
      return enumerableType.GetElementType();
    }

    if (typeof(IEnumerable).IsAssignableFrom(enumerableType)) {
      var targetType = typeof(IEnumerable<>);
      var ienumerableInterfaceType = (from t in enumerableType.GetInterfaces()
                                      where t.HasGenericBase(targetType)
                                      select t).FirstOrDefault();

      if (ienumerableInterfaceType != null) {
        return ienumerableInterfaceType.GetGenericArguments().First();
      }
      else {
        return typeof(object);
      }
    }

    return null;
  }

  [EditorBrowsable(EditorBrowsableState.Always)]
  public static PropertyInfo FindProperty(this Type extendee, string name) {
    if (!name.Contains(".")) {
      return extendee.GetBindableProperty(name);
    }

    var fields = System.ExtensionsForString.Split(name, "[.]");

    foreach (var @field in fields) {
      var propertyInfo = extendee.GetBindableProperty(@field);

      if (propertyInfo != null) {
        return propertyInfo.PropertyType.FindProperty(name.Remove(0, @field.Length + 1));
      }
    }

    return null;
  }

  [EditorBrowsable(EditorBrowsableState.Always)]
  public static PropertyInfo[] GetBindableProperties(this Type extendee) {
    return extendee.GetProperties(BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public).Where(p => p.GetIndexParameters().Count() == 0).ToArray();
  }

  [EditorBrowsable(EditorBrowsableState.Always)]
  public static PropertyInfo GetBindableProperty(this Type extendee, string name) {
    return extendee.GetBindableProperties().Where(p => (p.Name ?? "") == (name ?? "")).SingleOrDefault();
  }

  [EditorBrowsable(EditorBrowsableState.Always)]
  public static string GetNameWithNamespace(this Type extendee, bool lowerCase = false) {
    if (string.IsNullOrEmpty(extendee.Namespace)) {
      if (lowerCase) {
        return extendee.Name.ToLower();
      }
      else {
        return extendee.Name;
      }
    }
    else if (lowerCase) {
      return (extendee.Namespace + "." + extendee.Name).ToLower();
    }
    else {
      return extendee.Namespace + "." + extendee.Name;
    }
  }

  [EditorBrowsable(EditorBrowsableState.Always)]
  public static string GetDisplayName(this Type extendee) {
    var displayNameAttr = extendee.GetCustomAttributes().OfType<DisplayNameAttribute>().FirstOrDefault();
    if (displayNameAttr == null) {
      return extendee.Name;
    }
    else {
      return displayNameAttr.DisplayName;
    }
  }

  /// <summary>
  /// Only the Properties which are: readable, writable, parameterless, public and atomic types | arrays of atomic types
  /// </summary>
  [EditorBrowsable(EditorBrowsableState.Always)]
  public static IEnumerable<PropertyInfo> GetPrimitiveProperties(this Type extendee) {
    return extendee.GetProperties().Where(p => p.CanWrite && p.CanRead && !p.HasIndexParams() && p.PropertyType.IsAtomic());
  }

  // <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
  // Public Function GetWritingMethods(extendee As Type) As IEnumerable(Of MethodInfo)
  // Dim allMethods As IEnumerable(Of MethodInfo)

  // allMethods = extendee.GetMethods(BindingFlags.SetProperty Or BindingFlags.InvokeMethod Or BindingFlags.Public).
  // Where(Function(m) m.GetParameters().Count = 1)

  // allMethods = allMethods.Union(extendee.GetExtensionMethods.
  // Where(Function(m) m.GetParameters().Count = 2))

  // Return allMethods
  // End Function

  // <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
  // Public Function GetReadingMethods(extendee As Type) As IEnumerable(Of MethodInfo)
  // Dim allMethods As IEnumerable(Of MethodInfo)

  // allMethods = extendee.GetMethods(BindingFlags.GetProperty Or BindingFlags.InvokeMethod Or BindingFlags.Public).
  // Where(Function(m) m.GetParameters().Count = 0 AndAlso m.ReturnType IsNot GetType(Void))

  // allMethods = allMethods.Union(extendee.GetExtensionMethods.
  // Where(Function(m) m.GetParameters().Count = 1 AndAlso m.ReturnType IsNot GetType(Void)))

  // Return allMethods
  // End Function

}