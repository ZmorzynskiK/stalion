using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stalion.Services
{
    public interface IEditableStringService
    {
        string Name { get; }

        Models.EditableString GetString(string context, string t, int? idx);
        Models.EditableString GetById(long id);
        bool Update(long id, string languageCode, string value);
        int StoreEditableStrings(string languageCode, Models.EditableStringDictionary source);

        IList<Models.EditableString> Search(int? searchKey, string searchContext, string searchValue, string languageCode, int pageIndex, int pageSize, out int total);

        string ExportToCSV(string languageCode, bool getOnlyTheSameAsOriginalValues);
        int UpdateFromCSV(string languageCode, string csv, out IList<int> errorLines);
        int TryTranslateAutomatic(string languageCode);
    }
}
