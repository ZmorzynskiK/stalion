using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Stalion.MVC
{
    public abstract class StalionWebViewPage<TModel> : WebViewPage<TModel>
    {
        private EditableTextGetter _textGetter;
        private EditableTextRawGetter _rawGetter;

        private Services.IEditableStringService _stringService;
        
        private void ensureServices()
        {
            if(_stringService == null)
                // get storage provider
                _stringService = DependencyResolver.Current.GetService<Services.IEditableStringService>();
        }

        public EditableTextGetter S
        {
            get
            {
                if(_textGetter == null)
                {
                    ensureServices();
                    string vp = VirtualPath;
                    _textGetter = (t, idx, args) => getHtmlString(vp, t, idx, args);
                }
                return _textGetter;
            }
        }

        public EditableTextRawGetter R
        {
            get
            {
                if(_rawGetter == null)
                {
                    ensureServices();
                    string vp = VirtualPath;
                    _rawGetter = (t, idx, args) => getRawString(vp, t, idx, args);
                }
                return _rawGetter;
            }
        }

        private IHtmlString getHtmlString(string context, string t, int? idx, params object[] args)
        {
            var found = _stringService.GetString(context, t, idx);
            string v = t;
            if(found != null)
                v = found.Value;
            return MvcHtmlString.Create(string.Format(v, args));
        }

        private string getRawString(string context, string t, int? idx, params object[] args)
        {
            var found = _stringService.GetString(context, t, idx);
            if(found != null)
                return string.Format(found.Value, args);
            return string.Format(t, args);
        }

    }
}
