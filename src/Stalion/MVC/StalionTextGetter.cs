using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Stalion.MVC
{
    public delegate IHtmlString EditableTextGetter(string t, int? idx = null, params object[] args);
    public delegate string EditableTextRawGetter(string t, int? idx = null, params object[] args);

}
