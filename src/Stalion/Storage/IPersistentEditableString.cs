using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stalion.Storage
{
    /// <summary>
    /// This interface represents a database entity. Used mostly with EditableStringServiceBase.
    /// </summary>
    public interface IPersistentEditableString
    {
        int ESKey { get; set; }
        string ESContext { get; set; }
        string ESValue { get; set; }
        string ESOriginalValue { get; set; }
        int? ESIndex { get; set; }
        string ESLanguageCode { get; set; }
    }
}
