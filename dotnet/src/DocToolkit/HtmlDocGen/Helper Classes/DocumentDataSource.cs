using System;
using System.Collections.Generic;

namespace HtmlDocGen {

  public class DocumentDataSource {

    public DocumentDataSource() {
    }

    private Dictionary<string, object> _Local = new Dictionary<string, object>();
    public void SetSource(object source) {
      string typeKey = source.GetType().Name;
      if (_Local.ContainsKey(typeKey)) {
        _Local[typeKey] = source;
      }
      else {
        _Local.Add(typeKey, source);
      }

    }

    public void SetSource<T>(T source) {
      string typeKey = typeof(T).Name;
      if (_Local.ContainsKey(typeKey)) {
        _Local[typeKey] = source;
      }
      else {
        _Local.Add(typeKey, source);
      }
    }

    public T GetSource<T>() {
      return (T)this.GetSource(typeof(T));
    }

    public void CopyTo(DocumentDataSource target) {
      foreach (var localKey in _Local.Keys) {
        if (target._Local.ContainsKey(localKey)) {
          target._Local[localKey] = _Local[localKey];
        }
        else {
          target._Local.Add(localKey, _Local[localKey]);
        }
      }
    }

    public T GetOrAddSource<T>() where T : new() {
      var instance = this.GetSource<T>();
      if (instance == null) {
        instance = new T();
        this.SetSource(instance);
      }
      return instance;
    }

    public object GetSource(Type ofType) {
      return this.GetSource(ofType.Name);
    }

    public object GetSource(string ofTypeName) {
      if (_Local.ContainsKey(ofTypeName)) {
        return _Local[ofTypeName];
      }
      else {
        return null;
      }
    }

  }
}