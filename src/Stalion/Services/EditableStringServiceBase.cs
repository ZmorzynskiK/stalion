using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stalion.Extensions;
using Stalion.Models;

namespace Stalion.Services
{
    /// <summary>
    /// This is a sample, base implementation for editable string service.
    /// You can use this as a base class for your service.
    /// </summary>
    public abstract class EditableStringServiceBase : IEditableStringService
    {
        protected const string CACHE_PER_LANG_CONTEXT_KEY = "Stalion.Lang-Context-{0}-{1}";

        public abstract string Name { get; }

        protected virtual string getCacheContextKey(string languageCode, string context)
        {
            return string.Format(CACHE_PER_LANG_CONTEXT_KEY, languageCode, context).ToLowerInvariant();
        }

        protected abstract Services.IEditableStringCacheService cacheService { get; }
        
        protected abstract IQueryable<Storage.IPersistentEditableString> getDbQuery();
        protected abstract void storeNewPersistentString(int key, string context, int? index, string value, string originalValue, string language);
        protected abstract void updatePersistentString(Storage.IPersistentEditableString obj);

        protected abstract IList<Models.EditableString> toEditableStringList(IEnumerable<Storage.IPersistentEditableString> src);

        protected abstract void beginDbTransaction();
        protected abstract void commitDbTransaction();
        protected abstract void rollbackDbTransaction();
        protected abstract void disposeDbTransaction();

        protected abstract void logError(string errorStr, Exception ex);

        public virtual EditableString GetString(string context, string t, int? idx)
        {
            var curUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
            var langCode = curUICulture.TwoLetterISOLanguageName;
            // we are caching by contexts and language
            string cacheKey = getCacheContextKey(langCode, context);
            var stringsInContext = cacheService.Get(cacheKey, -1,
                () =>
                {
                    var inContext = getDbQuery().Where(x => x.ESContext == context && x.ESLanguageCode == langCode).ToList();
                    return toEditableStringList(inContext).ToDictionary(x => x.Key);
                });

            int key = EditableString.GetKey(context, t, idx);
            // find key in our (possibly cached) list
            EditableString found = null;
            if(stringsInContext.TryGetValue(key, out found))
                return found;
            return null;

        }

        public abstract Storage.IPersistentEditableString GetById(object id);

        public virtual IList<Storage.IPersistentEditableString> Search(int? searchKey, string searchContext, string searchValue, string languageCode, int pageIndex, int pageSize, out int total)
        {
            var q = getDbQuery();

            if(searchKey != null)
                q = q.Where(x => x.ESKey == searchKey.Value);
            if(!string.IsNullOrWhiteSpace(searchContext))
                q = q.Where(x => x.ESContext.Contains(searchContext));
            if(!string.IsNullOrWhiteSpace(searchValue))
                q = q.Where(x => x.ESValue.Contains(searchValue));
            if(!string.IsNullOrWhiteSpace(languageCode))
                q = q.Where(x => x.ESLanguageCode == languageCode);

            total = q.Count();
            var list = q.Skip(pageIndex * pageSize).Take(pageSize).ToList();
            return list;

        }

        public virtual int StoreEditableStrings(string languageCode, EditableStringDictionary source)
        {
            var allStrings = source.SelectMany(x => x.Value);
            int added = 0;
            
            try
            {
                beginDbTransaction();
                
                var allData = getDbQuery().Where(x => x.ESLanguageCode == languageCode).ToList();

                foreach(var s in allStrings)
                {
                    var obj = allData.FirstOrDefault(x => x.ESKey == s.Key);
                    if(obj != null)
                        continue; // ignore when exists

                    storeNewPersistentString(s.Key, s.Context, s.Index, s.Value, s.Value, languageCode);
                    added++;
                }

                commitDbTransaction();
            }
            catch(Exception ex)
            {
                logError("Error storing new ES data", ex);
                rollbackDbTransaction();
            }
            finally
            {
                disposeDbTransaction();
            }

            if(added > 0)
                cacheService.RemoveByPattern(CACHE_PER_LANG_CONTEXT_KEY);

            return added;

        }

        public virtual int TryTranslateAutomatic(string languageCode)
        {
            int updatedCount = 0;
            try
            {
                beginDbTransaction();

                var existingData = getDbQuery().Where(x => x.ESLanguageCode == languageCode).ToList();
                // group by original value so that we will have all the same original values
                var groupedByOrgValue = existingData.GroupBy(x => x.ESOriginalValue, StringComparer.Ordinal);
                foreach(var grouped in groupedByOrgValue)
                {
                    string orgValue = grouped.Key;
                    // now group all items with the same OrgValue by current value
                    var groupedByCurrentValue = grouped.GroupBy(x => x.ESValue, StringComparer.Ordinal);
                    // if we only have one group that means that all current values are the same for given original value -> we have nothing to do
                    if(groupedByCurrentValue.Count() <= 1)
                        continue;
                    // if we have several different current values for given original value then get those strings
                    // that are currently not the same as original - they are to be translated
                    var toBeTranslatedGroup = groupedByCurrentValue.Where(x => x.Key.Equals(orgValue, StringComparison.Ordinal));
                    // get also those which are currently NOT the same as original value - those are source
                    var sourceTransaltions = groupedByCurrentValue.Where(x => !x.Key.Equals(orgValue, StringComparison.Ordinal));
                    // check if we have exactly ONE source transaltion: if we have many then we wouldn't know which one to use
                    if(sourceTransaltions.Count() != 1)
                        continue;
                    var sourceCurrVal = sourceTransaltions.First().Key;
                    // now update objects to be translated with current source value
                    foreach(var toBeTranslatedList in toBeTranslatedGroup)
                    {
                        foreach(var toBeTranslated in toBeTranslatedList)
                        {
                            toBeTranslated.ESValue = sourceCurrVal;
                            updatePersistentString(toBeTranslated);
                            updatedCount++;
                        }
                    }
                }

                commitDbTransaction();
            }
            catch(Exception ex)
            {
                logError("Error updating ES automatic translation", ex);
                rollbackDbTransaction();
                updatedCount = -1;
            }
            finally
            {
                disposeDbTransaction();
            }
            // remove conext from cache
            if(updatedCount > 0)
                cacheService.RemoveByPattern(CACHE_PER_LANG_CONTEXT_KEY);

            return updatedCount;

        }

        public virtual bool Update(object id, string languageCode, string value)
        {
            Storage.IPersistentEditableString obj = null;
            // start transaction
            try
            {
                beginDbTransaction();

                obj = GetById(id);
                if(obj == null)
                {
                    // we can only update here
                    return false;
                }

                obj.ESValue = value;
                obj.ESLanguageCode = languageCode;
                updatePersistentString(obj);

                commitDbTransaction();
            }
            catch(Exception ex)
            {
                logError("Error updating ES", ex);
                rollbackDbTransaction();
                return false;
            }
            finally
            {
                disposeDbTransaction();
            }
            // remove conext from cache
            if(!string.IsNullOrWhiteSpace(obj.ESContext))
            {
                string cacheKey = getCacheContextKey(languageCode, obj.ESContext);
                cacheService.Remove(cacheKey);
            }
            return true;
        }

        public virtual int UpdateFromCSV(string languageCode, string csv, out IList<int> errorLines)
        {
            int updatedCount = 0;
            errorLines = new List<int>();
            try
            {
                beginDbTransaction();

                using(var sr = new System.IO.StringReader(csv))
                {
                    var existingData = getDbQuery().ToList();

                    // ignore header
                    sr.ReadLine();
                    int ln = 1;
                    string line = null;
                    while((line = sr.ReadLine()) != null)
                    {
                        var elems = line.Split(';');
                        if(elems.Length != 2 || string.IsNullOrWhiteSpace(elems[0]) || string.IsNullOrWhiteSpace(elems[1]))
                            errorLines.Add(ln);
                        else
                        {
                            // first is value to be stored
                            var val = elems[0].Trim('"');
                            // second is keys separated by ,
                            var keysList = elems[1].Trim('"');
                            if(string.IsNullOrWhiteSpace(val) || string.IsNullOrWhiteSpace(keysList))
                                errorLines.Add(ln);
                            else
                            {
                                var keys = keysList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                                foreach(var keyString in keys)
                                {
                                    int key = 0;
                                    if(int.TryParse(keyString, out key))
                                    {
                                        // now we have id and value, find this object in our list with the same language code first
                                        var foundInThisLanguage = existingData.FirstOrDefault(x => x.ESKey == key && x.ESLanguageCode == languageCode);
                                        if(foundInThisLanguage != null) //only update if we found text
                                        {
                                            foundInThisLanguage.ESValue = val;
                                            updatePersistentString(foundInThisLanguage);
                                            updatedCount++;
                                        }
                                        else
                                        {
                                            // if we didn't find this key in this language, then try to get from other language but with context not null
                                            var foundInAnyLanguage = existingData.FirstOrDefault(x => x.ESKey == key && x.ESContext != null);
                                            if(foundInAnyLanguage != null)
                                            {
                                                // create new entry for this language and copy data from this 
                                                storeNewPersistentString(key, foundInAnyLanguage.ESContext, foundInAnyLanguage.ESIndex, val, foundInAnyLanguage.ESOriginalValue, languageCode);
                                                updatedCount++;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        errorLines.Add(ln);
                                        break;
                                    }
                                }
                            }
                        }
                        ln++;
                    }
                }

                commitDbTransaction();
            }
            catch(Exception ex)
            {
                logError("Error updating ES list", ex);
                rollbackDbTransaction();
                updatedCount = -1;
            }
            finally
            {
                disposeDbTransaction();
            }
            // remove from cache
            if(updatedCount > 0)
                cacheService.RemoveByPattern(CACHE_PER_LANG_CONTEXT_KEY);

            return updatedCount;

        }

        public virtual string ExportToCSV(string languageCode, bool getOnlyTheSameAsOriginalValues)
        {
            // get all data for this language and group by value
            var allData = getDbQuery().Where(x => x.ESLanguageCode == languageCode).ToList();
            // check if we want to get only those items which are belived not to be translated (current value is the same as original value)
            if(getOnlyTheSameAsOriginalValues)
                allData = allData.Where(x => x.ESValue.Equals(x.ESOriginalValue, StringComparison.Ordinal)).ToList();
            var groupedByValue = allData.GroupBy(x => x.ESValue, StringComparer.Ordinal);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Text; Keys");
            foreach(var grouped in groupedByValue)
            {
                // get all ids with the same text
                var keys = string.Join(",", grouped.Select(x => x.ESKey.ToString()));
                // prepare value
                string val = grouped.Key.Replace('"', '\'').Replace(';', ',');
                // write csv
                sb.AppendFormat("\"{0}\";\"{1}\"", val, keys).AppendLine();
            }
            return sb.ToString();

        }

    }
}
