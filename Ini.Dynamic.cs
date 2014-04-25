#region The MIT License (MIT)
//
// Copyright (c) 2013 Atif Aziz. All rights reserved.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
#endregion

namespace Gini
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Dynamic;
    using System.Globalization;
    using System.Linq;

    #endregion

    static partial class Ini
    {
        public static dynamic ParseObject(string ini)
        {
            return ParseObject(ini, null);
        }

        public static dynamic ParseObject(string ini, IEqualityComparer<string> comparer)
        {
            comparer = comparer ?? StringComparer.OrdinalIgnoreCase;
            var config = new Dictionary<string, Config<string>>(comparer);
            foreach (var section in from g in Parse(ini).GroupBy(e => e.Key ?? string.Empty, comparer)
                                     select KeyValuePair.Create(g.Key, g.SelectMany(e => e)))
            {
                var settings = new Dictionary<string, string>(comparer);
                foreach (var setting in section.Value)
                    settings[setting.Key] = setting.Value;
                config[section.Key] = new Config<string>(settings);
            }
            return new Config<Config<string>>(config);
        }

        public static dynamic ParseFlatObject(string ini, Func<string, string, string> keyMerger)
        {
            return ParseFlatObject(ini, keyMerger, null);
        }

        public static dynamic ParseFlatObject(string ini, Func<string, string, string> keyMerger, IEqualityComparer<string> comparer)
        {
            return new Config<string>(ParseFlatHash(ini, keyMerger, comparer));
        }

        sealed class Config<T> : DynamicObject
        {
            readonly IDictionary<string, T> _data;

            public Config(IDictionary<string, T> data)
            {
                Debug.Assert(data != null);
                _data = data;
            }

            public override bool TryGetMember(GetMemberBinder binder, out object result)
            {
                result = Find(binder.Name);
                return true;
            }

            public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
            {
                if (indexes == null) throw new ArgumentNullException("indexes");
                if (indexes.Length != 1) throw new ArgumentException("Too many indexes supplied.");
                var index = indexes[0];
                result = Find(index == null ? null : Convert.ToString(index, CultureInfo.InvariantCulture));
                return true;
            }
            
            object Find(string name)
            {
                T value;
                return _data.TryGetValue(name, out value) ? value : default(T);
            }
        }
    }
}