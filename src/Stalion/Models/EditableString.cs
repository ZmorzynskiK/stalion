using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stalion.Models
{
    /// <summary>
    /// Represents a string that is editable. This entity is NOT directly taken from database.
    /// </summary>
    public class EditableString
    {
        /// <summary>
        ///  This is a "key" corresponding to a particular string. Most likely this will be a hashcode so this isn't necessary unique.
        /// </summary>
        public int Key { get; set; }
        public string Value { get; set; }
        public string Context { get; set; }
        public string ContextWithIndex { get { return (Index == null ? Context : Context + " [" + Index + "]"); } }
        public int? Index { get; set; }
        public string LanguageCode { get; set; }

        public EditableString(int key, string context, int? index, string value)
        {
            Context = context;
            Key = key;
            Value = value;
            Index = index;
        }

        public EditableString(int key, string context, int? index, string value, string languageCode)
        {
            Context = context;
            Key = key;
            Value = value;
            Index = index;
            LanguageCode = languageCode;
        }

        public override bool Equals(object obj)
        {
            if(obj == this) return true;
            var es = obj as EditableString;
            if(es == null) return false;

            return es.Key == this.Key;
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode();
        }

        public static int GetHashString(string text)
        {
            if(string.IsNullOrWhiteSpace(text))
                return 0;

            unchecked
            {
                int hash = 23;
                foreach(char c in text)
                {
                    hash = hash * 31 + c;
                }
                return hash;
            }
        }

        public static int GetKey(string context, string val, int? idx)
        {
            // for now just return hash code of the string
            string t = (context + "_" + val).ToLowerInvariant();
            if(idx != null)
                return GetHashString(t + "_" + idx);
            return GetHashString(t);
        }

    }

    public class EditableStringDictionary : Dictionary<string, IList<EditableString>>
    {
        public EditableStringDictionary()
            : base(StringComparer.OrdinalIgnoreCase)
        {
        }
    }

}
