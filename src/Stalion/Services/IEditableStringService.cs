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
        bool Update(object id, string languageCode, string value);
        bool Create(string context, int? idx, string langCode, string value, string orgValue);
        int StoreEditableStrings(string languageCode, Models.EditableStringDictionary source);
        int RecalcKeys();
        bool Delete(object id);

        Storage.IPersistentEditableString GetById(object id);
        IList<Storage.IPersistentEditableString> Search(int? searchKey, string searchContext, string searchValue, string languageCode, int pageIndex, int pageSize, out int total);

        string ExportToCSV(string languageCode, bool getOnlyTheSameAsOriginalValues);
        int UpdateFromCSV(string languageCode, string csv, out IList<int> errorLines);
        int TryTranslateAutomatic(string languageCode);

        int SetAllDataFromCsv(string languageCode, string csv, out IList<int> errorLines);
        string GetAllDataToCsv(string languageCode);
    }
}
