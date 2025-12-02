using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace HtmlDocGen {

  public abstract class TemplateBlock : DocumentBlock {

    #region  Childs 

    private Dictionary<string, List<DocumentBlock>> _Childs = new Dictionary<string, List<DocumentBlock>>();

    [Obsolete("Please specify the areaId via index param: Childs('myAreaId')")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public virtual IList<DocumentBlock> Childs {
      get {
        return this.get_Childs(string.Empty);
      }
    }

    public IList<DocumentBlock> get_Childs(string areaId) {
      areaId = areaId.Trim().ToLower();
      if (!_Childs.ContainsKey(areaId)) {
        _Childs.Add(areaId, new List<DocumentBlock>());
      }
      return _Childs[areaId];
    }

    protected string[] GetRegisteredAreaIds() {
      return _Childs.Keys.ToArray();
    }

    #endregion

    protected virtual Stream OpenTemplateStream(string scope, IDocumentFormatProvider formatProvider) {
      return formatProvider.OpenTemplateStream(scope, this.GetType());
    }


    #region  Generate 

    protected virtual void PickSources(Func<string, object> dataSourceGetter) {
    }

    public override void WriteOutputTo(StreamWriter target, IDocumentFormatProvider provider, Func<string, object> dataSourceGetter, string scope) {

      object wrappeddataSourceGetter(string name) {
        string targetName = name.SubStringBefore(".");
        string memberName = name.SubStringAfter(".");
        object dataSource;

        // new way with named areas
        foreach (var areaId in this.GetRegisteredAreaIds()) {
          if ((areaId.ToLower() ?? "") == (targetName.ToLower() ?? "")) {
            return this.get_Childs(targetName);
          }
        }

        // obsolete way
        if (targetName.ToLower() == "subparts") {
          return this.get_Childs(memberName);
        }

        else if (targetName.ToLower() == "me") {
          dataSource = this;
        }
        else {
          // targetName seems to be a class-name
          dataSource = this.DataSource.GetSource(targetName);
        }

        if (dataSource == null) {
          if (dataSourceGetter != null) {
            return dataSourceGetter.Invoke(name);
          }
          else {
            return null;
          }
        }
        else if (string.IsNullOrEmpty(memberName)) {
          return dataSource;
        }
        else {
          return dataSource.GetType().GetValueFrom(dataSource, memberName);
        }

      };

      this.PickSources(dataSourceGetter);

      if (this.IsVisible(wrappeddataSourceGetter)) {
        provider.WriteTemplateToStream(scope, target, wrappeddataSourceGetter, this.GetType(), this.OpenTemplateStream(scope, provider));
      }

    }

    #endregion

  }
}