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
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;

    #endregion

    static partial class Ini
    {
        static class Parser
        {
            enum State
            {
                NewLine,
                Cr,
                Comment,
                ScanSectionName,
                SectionName,
                ScanSectionClose,
                SectionClosure,
                Key,
                ScanEqual,
                ScanValue,
                Value,
                ValueWhiteSpace,
            }

            static bool IsNamePunctuation(char ch) =>
                ch == '.' || ch == '-' || ch == '_';

            static bool IsNameStart(char ch) =>
                char.IsLetter(ch) || IsNamePunctuation(ch);

            static bool IsNameMid(char ch) =>
                char.IsLetterOrDigit(ch) || IsNamePunctuation(ch);

            static bool IsNameEnd(char ch) =>
                char.IsLetterOrDigit(ch);

            static class Expectation
            {
                public const string Equal               = "Expected '='.";
                public const string RightBracket        = "Expected ']'.";
                public const string SectionName         = "Expected section name.";
                public const string SectionKeyOrComment = "Expected section, key or comment.";
                public const string CommentOrWhiteSpace = "Expected comment or white space.";
                public const string NonPunctuation      = "Expected non-punctuation.";
            }

            // ReSharper disable once MemberHidesStaticFromOuterClass

            public static IEnumerable<T> Parse<T>(string ini, Func<string, string, string, T> selector)
            {
                var state = State.NewLine;
                var line = 1;
                var col = 1;
                var si = 0;
                var vtsi = -1; // value tail space index
                var section = (string)null;
                var key = (string)null;

                string Text(int end) =>
                    end == si ? null : ini.Substring(si, end - si);

                var i = 0;

                bool IsDuplicatePunctuation(char ch)
                {
                    Debug.Assert(i > si);
                    return ch == ini[i - 1] && IsNamePunctuation(ch);
                }

                for (; i < ini.Length; i++)
                {
                    var ch = ini[i];
                    restart:
                    switch (state)
                    {
                        case State.NewLine:
                        {
                            switch (ch)
                            {
                                case ' ':
                                case '\t':
                                    break;
                                case '[':
                                    state = State.ScanSectionName; break;
                                case '#':
                                case ';':
                                    state = State.Comment;
                                    break;
                                case '\n':
                                    line++; col = 0;
                                    break;
                                case '\r':
                                    state = State.Cr;
                                    break;
                                case char c when IsNameStart(c):
                                    si = i;
                                    state = State.Key;
                                    break;
                                case '=':
                                    key = null;
                                    state = State.ScanValue;
                                    break;
                                default:
                                    throw SyntaxError(Expectation.SectionKeyOrComment);
                            }
                            break;
                        }
                        case State.Cr:
                        {
                            switch (ch)
                            {
                                case '\r':
                                    line++; col = 0;
                                    break;
                                case '\n':
                                    state = State.NewLine;
                                    goto restart;
                                default:
                                    line++; col = 1;
                                    state = State.NewLine;
                                    goto restart;
                            }
                            break;
                        }
                        case State.Comment:
                        {
                            switch (ch)
                            {
                                case '\r':
                                    state = State.Cr;
                                    break;
                                case '\n':
                                    state = State.NewLine;
                                    goto restart;
                            }
                            break;
                        }
                        case State.ScanSectionName:
                        {
                            switch (ch)
                            {
                                case ' ':
                                case '\t':
                                    break;
                                case ']':
                                    section = null;
                                    state = State.SectionClosure;
                                    break;
                                case char c when IsNameStart(c):
                                    si = i;
                                    state = State.SectionName;
                                    break;
                                default:
                                    throw SyntaxError(Expectation.SectionName);
                            }
                            break;
                        }
                        case State.SectionName:
                        {
                            switch (ch)
                            {
                                case '\t':
                                case ' ':
                                case ']':
                                    if (!IsNameEnd(ini[i - 1]))
                                        throw SyntaxError(Expectation.RightBracket, offset: -1);
                                    section = Text(i);
                                    state = State.ScanSectionClose;
                                    goto restart;
                                case char c when IsNameMid(c):
                                    if (IsDuplicatePunctuation(c))
                                        throw SyntaxError(Expectation.NonPunctuation);
                                    break;
                                default:
                                    throw SyntaxError(Expectation.RightBracket);
                            }
                            break;
                        }
                        case State.ScanSectionClose:
                        {
                            switch (ch)
                            {
                                case ' ':
                                case '\t':
                                    break;
                                case ']':
                                    state = State.SectionClosure;
                                    break;
                                default:
                                    throw SyntaxError(Expectation.RightBracket);
                            }
                            break;
                        }
                        case State.SectionClosure:
                        {
                            switch (ch)
                            {
                                case ' ':
                                case '\t':
                                    break;
                                case '\r':
                                    state = State.Cr;
                                    break;
                                case '\n':
                                    state = State.NewLine;
                                    goto restart;
                                case '#':
                                case ';':
                                    state = State.Comment;
                                    break;
                                default:
                                    throw SyntaxError(Expectation.CommentOrWhiteSpace);
                            }
                            break;
                        }
                        case State.Key:
                        {
                            switch (ch)
                            {
                                case '\t':
                                case ' ':
                                case '=':
                                    if (!IsNameEnd(ini[i - 1]))
                                        throw SyntaxError(Expectation.Equal, offset: -1);
                                    key = Text(i);
                                    state = State.ScanEqual;
                                    goto restart;
                                case char c when IsNameMid(c):
                                    if (IsDuplicatePunctuation(c))
                                        throw SyntaxError(Expectation.NonPunctuation);
                                    break;
                                default:
                                    throw SyntaxError(Expectation.Equal);
                            }
                            break;
                        }
                        case State.ScanEqual:
                        {
                            switch (ch)
                            {
                                case ' ':
                                case '\t':
                                    break;
                                case '=':
                                    state = State.ScanValue;
                                    break;
                                default:
                                    throw SyntaxError(Expectation.Equal);
                            }
                            break;
                        }
                        case State.ScanValue:
                        {
                            switch (ch)
                            {
                                case ' ':
                                case '\t':
                                    break;
                                default:
                                    si = i;
                                    state = State.Value;
                                    if (ch == '\r' || ch == '\n')
                                        goto restart;
                                    break;
                            }
                            break;
                        }
                        case State.Value:
                        {
                            switch (ch)
                            {
                                case ' ':
                                case '\t':
                                    vtsi = i;
                                    state = State.ValueWhiteSpace;
                                    break;
                                case '\r':
                                {
                                    if (Token(Text(i), out var t))
                                        yield return t;
                                    state = State.Cr;
                                    break;
                                }
                                case '\n':
                                {
                                    if (Token(Text(i), out var t))
                                        yield return t;
                                    state = State.NewLine;
                                    goto restart;
                                }
                            }
                            break;
                        }
                        case State.ValueWhiteSpace:
                        {
                            switch (ch)
                            {
                                case ' ':
                                case '\t':
                                    break;
                                case '\r':
                                case '\n':
                                    if (Token(Text(vtsi), out var t))
                                        yield return t;
                                    state = State.NewLine;
                                    goto restart;
                                default:
                                    vtsi = -1;
                                    state = State.Value;
                                    break;
                            }
                            break;
                        }
                    }

                    col++;
                }

                switch (state)
                {
                    case State.ScanSectionName:
                        throw SyntaxError(Expectation.SectionName);
                    case State.SectionName:
                        throw SyntaxError(Expectation.RightBracket, offset: IsNameEnd(ini[i - 1]) ? 0 : -1);
                    case State.ScanSectionClose:
                        throw SyntaxError(Expectation.RightBracket);
                    case State.Key:
                        throw SyntaxError(Expectation.Equal, offset: IsNameEnd(ini[i - 1]) ? 0 : -1);
                    case State.ScanEqual:
                        throw SyntaxError(Expectation.Equal);
                    case State.ScanValue:
                    {
                        if (Token(null, out var t))
                            yield return t;
                        break;
                    }
                    case State.Value:
                    {
                        if (Token(Text(i), out var t))
                            yield return t;
                        break;
                    }
                    case State.ValueWhiteSpace:
                    {
                        if (Token(Text(vtsi), out var t))
                            yield return t;
                        break;
                    }
                    case State.Comment:
                    case State.Cr:
                    case State.NewLine:
                    case State.SectionClosure:
                        break;
                    default:
                        throw new Exception($"Internal implementation error ({state}).");
                }

                bool Token(string v, out T token)
                {
                    if (key is null && v is null)
                    {
                        token = default;
                        return false;
                    }

                    token = selector(section, key, v);
                    return true;
                }

                Exception SyntaxError(string expectation, int offset = 0) =>
                    new FormatException($"Syntax error (at {line}:{col + offset}) parsing INI format.{Concat(" ", expectation)}");
            }
        }

        static string Concat(string s1, string s2)
            => s1 != null && s2 != null ? s1 + s2 : null;

        public static IEnumerable<IGrouping<string, KeyValuePair<string, string>>> Parse(string ini) =>
            Parse(ini, KeyValuePair.Create);

        public static IEnumerable<IGrouping<string, T>> Parse<T>(string ini, Func<string, string, T> settingSelector) =>
            Parse(ini, (_, k, v) => settingSelector(k, v));

        public static IEnumerable<IGrouping<string, T>> Parse<T>(string ini, Func<string, string, string, T> settingSelector)
        {
            if (settingSelector == null) throw new ArgumentNullException(nameof(settingSelector));

            if (string.IsNullOrWhiteSpace(ini))
                return Enumerable.Empty<IGrouping<string, T>>();

            return Parser.Parse(ini, (s, k, v) => KeyValuePair.Create(s, settingSelector(s, k, v)))
                         .GroupAdjacent(e => e.Key, e => e.Value);
        }

        public static IDictionary<string, IDictionary<string, string>> ParseHash(string ini) =>
            ParseHash(ini, null);

        public static IDictionary<string, IDictionary<string, string>> ParseHash(string ini, IEqualityComparer<string> comparer)
        {
            comparer = comparer ?? StringComparer.OrdinalIgnoreCase;
            var sections = Parse(ini);
            return sections.GroupBy(g => g.Key ?? string.Empty, comparer)
                           .ToDictionary(g => g.Key,
                                         g => (IDictionary<string, string>) g.SelectMany(e => e)
                                                                             .GroupBy(e => e.Key, comparer)
                                                                             .ToDictionary(e => e.Key, e => e.Last().Value, comparer),
                                         comparer);
        }

        public static IDictionary<string, string> ParseFlatHash(string ini, Func<string, string, string> keyMerger) =>
            ParseFlatHash(ini, keyMerger, null);

        public static IDictionary<string, string> ParseFlatHash(string ini, Func<string, string, string> keyMerger, IEqualityComparer<string> comparer)
        {
            if (keyMerger == null) throw new ArgumentNullException(nameof(keyMerger));

            var settings = new Dictionary<string, string>(comparer ?? StringComparer.OrdinalIgnoreCase);
            foreach (var setting in from section in Parse(ini)
                                    from setting in section
                                    select KeyValuePair.Create(keyMerger(section.Key, setting.Key), setting.Value))
            {
                settings[setting.Key] = setting.Value;
            }
            return settings;
        }

        static class KeyValuePair
        {
            public static KeyValuePair<TKey, TValue> Create<TKey, TValue>(TKey key, TValue value) =>
                new KeyValuePair<TKey, TValue>(key, value);
        }

        #region MoreLINQ

        // MoreLINQ - Extensions to LINQ to Objects
        // Copyright (c) 2012 Atif Aziz. All rights reserved.
        //
        // Licensed under the Apache License, Version 2.0 (the "License");
        // you may not use this file except in compliance with the License.
        // You may obtain a copy of the License at
        //
        //     http://www.apache.org/licenses/LICENSE-2.0
        //
        // Unless required by applicable law or agreed to in writing, software
        // distributed under the License is distributed on an "AS IS" BASIS,
        // WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
        // See the License for the specific language governing permissions and
        // limitations under the License.

        static IEnumerable<IGrouping<TKey, TSource>> GroupAdjacent<TSource, TKey>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey> comparer)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

            return GroupAdjacent(source, keySelector, e => e, comparer);
        }

        static IEnumerable<IGrouping<TKey, TElement>> GroupAdjacent<TSource, TKey, TElement>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector)
        {
            return GroupAdjacent(source, keySelector, elementSelector, null);
        }

        static IEnumerable<IGrouping<TKey, TElement>> GroupAdjacent<TSource, TKey, TElement>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            IEqualityComparer<TKey> comparer)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            if (elementSelector == null) throw new ArgumentNullException(nameof(elementSelector));

            return GroupAdjacentImpl(source, keySelector, elementSelector,
                                     comparer ?? EqualityComparer<TKey>.Default);
        }

        static IEnumerable<IGrouping<TKey, TElement>> GroupAdjacentImpl<TSource, TKey, TElement>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            IEqualityComparer<TKey> comparer)
        {
            Debug.Assert(source != null);
            Debug.Assert(keySelector != null);
            Debug.Assert(elementSelector != null);
            Debug.Assert(comparer != null);

            using (var iterator = source.GetEnumerator())
            {
                var group = default(TKey);
                var members = (List<TElement>) null;

                while (iterator.MoveNext())
                {
                    var key = keySelector(iterator.Current);
                    var element = elementSelector(iterator.Current);
                    if (members != null && comparer.Equals(group, key))
                    {
                        members.Add(element);
                    }
                    else
                    {
                        if (members != null)
                            yield return CreateGroupAdjacentGrouping(group, members);
                        group = key;
                        members = new List<TElement> { element };
                    }
                }

                if (members != null)
                    yield return CreateGroupAdjacentGrouping(group, members);
            }
        }

        static Grouping<TKey, TElement> CreateGroupAdjacentGrouping<TKey, TElement>(TKey key, IList<TElement> members)
        {
            Debug.Assert(members != null);
            return new Grouping<TKey, TElement>(key, members.IsReadOnly ? members : new ReadOnlyCollection<TElement>(members));
        }

        sealed class Grouping<TKey, TElement> : IGrouping<TKey, TElement>
        {
            readonly IEnumerable<TElement> _members;

            public Grouping(TKey key, IEnumerable<TElement> members)
            {
                Debug.Assert(members != null);
                Key = key;
                _members = members;
            }

            public TKey Key { get; }

            public IEnumerator<TElement> GetEnumerator() { return _members.GetEnumerator(); }
            IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        }

        #endregion
    }
}
