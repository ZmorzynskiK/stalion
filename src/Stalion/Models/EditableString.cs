﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stalion.Models
{
    /// <summary>
    /// Represents a string that is editable.
    /// </summary>
    public class EditableString
    {
        /// <summary>
        /// Arbitrary Id type
        /// </summary>
        public object Id { get; set; }
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

        public EditableString(object id, int key, string context, int? index, string value, string languageCode)
        {
            Id = id;
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


        public static int GetKey(string context, string val, int? idx)
        {
            // for now just return hash code of the string
            string t = (context + "_" + val).ToLowerInvariant();
            if(idx != null)
                return (t + "_" + idx).GetHashCode();
            return t.GetHashCode();
        }

    }
}
