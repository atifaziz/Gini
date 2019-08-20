namespace Gini.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;

    public class Tests
    {
        [Test]
        public void Parse()
        {
            const string tab = "\t";
            const string space = " ";

            var ini = $@"
                key1=value1
                key2 = value2{space}

                [ empty1 ]

                [empty2]

                [] ; comment next to section name is allowed
                [] # comment next to section name is allowed

                key3=value3

                # next two key-value pairs are commented-out
                # key4=value4
                # key5=value5

                [section3]
                foo = bar baz qux{tab} {tab}
                empty_key_1=
                empty_key_2={space}
                foo.bar=baz
                foo-bar=baz

                [key-less]
                =foo
                =bar
                =baz

                [value-less]
                foo =
                bar = {tab}
                baz = {space}

                [empty]
                =
                = {tab}
                = {space}
                ";

            var sections =
                Ini.Parse(ini);

            using (var se = sections.GetEnumerator())
            {
                var section = se.Read();
                Assert.That(section.Key, Is.Null);
                Assert.That(section, Is.EqualTo(new[]
                {
                    KeyValuePair.Create("key1", "value1"),
                    KeyValuePair.Create("key2", "value2"),
                    KeyValuePair.Create("key3", "value3"),
                }));

                section = se.Read();
                Assert.That(section.Key, Is.EqualTo("section3"));
                Assert.That(section, Is.EqualTo(new[]
                {
                    KeyValuePair.Create("foo", "bar baz qux"),
                    KeyValuePair.Create("empty_key_1", (string)null),
                    KeyValuePair.Create("empty_key_2", (string)null),
                    KeyValuePair.Create("foo.bar", "baz"),
                    KeyValuePair.Create("foo-bar", "baz"),
                }));

                section = se.Read();
                Assert.That(section.Key, Is.EqualTo("key-less"));
                Assert.That(section, Is.EqualTo(from v in new[] { "foo", "bar", "baz" }
                                                select KeyValuePair.Create((string)null, v)));

                section = se.Read();
                Assert.That(section, Is.EqualTo(from k in new[] { "foo", "bar", "baz" }
                                                select KeyValuePair.Create(k, (string)null)));

                Assert.That(se.MoveNext(), Is.False);
            }
        }

        [TestCase("# comment\r[section]\nfoo=bar")]
        [TestCase("; comment\r[section]\nfoo=bar")]
        [TestCase("# comment\n[section]\nfoo=bar")]
        [TestCase("; comment\n[section]\nfoo=bar")]
        public void CommentTermination(string ini)
        {
            var section = Ini.Parse(ini).Single();

            Assert.That(section.Key, Is.EqualTo("section"));
            Assert.That(section, Is.EqualTo(new[] { KeyValuePair.Create("foo", "bar") }));
        }

        [TestCase("[section]\nfoo=bar\r")]
        [TestCase("[section]\nfoo=bar\n")]
        public void ValueTermination(string ini)
        {
            var section = Ini.Parse(ini).Single();

            Assert.That(section.Key, Is.EqualTo("section"));
            Assert.That(section, Is.EqualTo(new[] { KeyValuePair.Create("foo", "bar") }));
        }

        [TestCase("[section]\nkey="        , "section", "key", null   )]
        [TestCase("[section]\nkey=value"   , "section", "key", "value")]
        [TestCase("[section]\nkey=value "  , "section", "key", "value")]
        [TestCase("[section]\nkey=value \t", "section", "key", "value")]
        public void Termination(string ini, string sectionName, string key, string value)
        {
            var section = Ini.Parse(ini).Single();

            Assert.That(section.Key, Is.EqualTo(sectionName));
            Assert.That(section, Is.EqualTo(new[] { KeyValuePair.Create(key, value) }));
        }

        [TestCase("")]
        [TestCase(" \t ")]
        [TestCase("\r")]
        [TestCase("\r\n")]
        [TestCase("\n")]
        [TestCase("\r\n ; comment 1 \r # comment 2 \n")]
        [TestCase("# comment")]
        [TestCase("; comment")]
        [TestCase("\r")]
        [TestCase("[]")]
        [TestCase("[foo]")]
        public void ParseEmpty(string ini)
        {
            Assert.That(Ini.Parse(ini), Is.Empty);
        }

        [TestCase("["  , "Syntax error (at 1:2) parsing INI format. Expected section name.")]
        [TestCase("[ " , "Syntax error (at 1:3) parsing INI format. Expected section name.")]
        [TestCase("[\t", "Syntax error (at 1:3) parsing INI format. Expected section name.")]
        [TestCase("[\r", "Syntax error (at 1:2) parsing INI format. Expected section name.")]
        [TestCase("[\n", "Syntax error (at 1:2) parsing INI format. Expected section name.")]

        [TestCase("[foo"       , "Syntax error (at 1:5) parsing INI format. Expected ']'.")]
        [TestCase("[foo "      , "Syntax error (at 1:6) parsing INI format. Expected ']'.")]
        [TestCase("[foo\t"     , "Syntax error (at 1:6) parsing INI format. Expected ']'.")]
        [TestCase("[foo bar"   , "Syntax error (at 1:6) parsing INI format. Expected ']'.")]
        [TestCase("[foo\r"     , "Syntax error (at 1:5) parsing INI format. Expected ']'.")]
        [TestCase("[foo\n"     , "Syntax error (at 1:5) parsing INI format. Expected ']'.")]
        [TestCase("[foo.bar.]" , "Syntax error (at 1:9) parsing INI format. Expected ']'.")]
        [TestCase("[foo.bat.\t", "Syntax error (at 1:9) parsing INI format. Expected ']'.")]
        [TestCase("[foo.bar. " , "Syntax error (at 1:9) parsing INI format. Expected ']'.")]
        [TestCase("[foo.bar-]" , "Syntax error (at 1:9) parsing INI format. Expected ']'.")]
        [TestCase("[foo.bat-\t", "Syntax error (at 1:9) parsing INI format. Expected ']'.")]
        [TestCase("[foo.bar- " , "Syntax error (at 1:9) parsing INI format. Expected ']'.")]

        [TestCase("foo"       , "Syntax error (at 1:4) parsing INI format. Expected '='.")]
        [TestCase("foo "      , "Syntax error (at 1:5) parsing INI format. Expected '='.")]
        [TestCase("foo\t"     , "Syntax error (at 1:5) parsing INI format. Expected '='.")]
        [TestCase("foo\n"     , "Syntax error (at 1:4) parsing INI format. Expected '='.")]
        [TestCase("foo\r"     , "Syntax error (at 1:4) parsing INI format. Expected '='.")]
        [TestCase("foo?"      , "Syntax error (at 1:4) parsing INI format. Expected '='.")]
        [TestCase("foo bar"   , "Syntax error (at 1:5) parsing INI format. Expected '='.")]
        [TestCase("foo> bar"  , "Syntax error (at 1:4) parsing INI format. Expected '='.")]
        [TestCase("foo > bar" , "Syntax error (at 1:5) parsing INI format. Expected '='.")]
        [TestCase("foo.bar.=" , "Syntax error (at 1:8) parsing INI format. Expected '='.")]
        [TestCase("foo.bar.\t", "Syntax error (at 1:8) parsing INI format. Expected '='.")]
        [TestCase("foo.bar. " , "Syntax error (at 1:8) parsing INI format. Expected '='.")]
        [TestCase("foo-bar-=" , "Syntax error (at 1:8) parsing INI format. Expected '='.")]
        [TestCase("foo-bar-\t", "Syntax error (at 1:8) parsing INI format. Expected '='.")]
        [TestCase("foo-bar- " , "Syntax error (at 1:8) parsing INI format. Expected '='.")]

        [TestCase("?", "Syntax error (at 1:1) parsing INI format. Expected section, key or comment.")]

        [TestCase("[]foo", "Syntax error (at 1:3) parsing INI format. Expected comment or white space.")]

        [TestCase("\r["      , "Syntax error (at 2:2) parsing INI format. Expected section name.")]
        [TestCase("\r\r["    , "Syntax error (at 3:2) parsing INI format. Expected section name.")]
        [TestCase("\n\r["    , "Syntax error (at 3:2) parsing INI format. Expected section name.")]
        [TestCase("\r\n\r["  , "Syntax error (at 3:2) parsing INI format. Expected section name.")]
        [TestCase("\r\r\n["  , "Syntax error (at 3:2) parsing INI format. Expected section name.")]
        [TestCase("\r\r\n\r[", "Syntax error (at 4:2) parsing INI format. Expected section name.")]

        [TestCase("[0", "Syntax error (at 1:2) parsing INI format. Expected section name.")]
        [TestCase("[1", "Syntax error (at 1:2) parsing INI format. Expected section name.")]
        [TestCase("[2", "Syntax error (at 1:2) parsing INI format. Expected section name.")]
        [TestCase("[3", "Syntax error (at 1:2) parsing INI format. Expected section name.")]
        [TestCase("[4", "Syntax error (at 1:2) parsing INI format. Expected section name.")]
        [TestCase("[5", "Syntax error (at 1:2) parsing INI format. Expected section name.")]
        [TestCase("[6", "Syntax error (at 1:2) parsing INI format. Expected section name.")]
        [TestCase("[7", "Syntax error (at 1:2) parsing INI format. Expected section name.")]
        [TestCase("[8", "Syntax error (at 1:2) parsing INI format. Expected section name.")]
        [TestCase("[9", "Syntax error (at 1:2) parsing INI format. Expected section name.")]

        [TestCase("0", "Syntax error (at 1:1) parsing INI format. Expected section, key or comment.")]
        [TestCase("1", "Syntax error (at 1:1) parsing INI format. Expected section, key or comment.")]
        [TestCase("2", "Syntax error (at 1:1) parsing INI format. Expected section, key or comment.")]
        [TestCase("3", "Syntax error (at 1:1) parsing INI format. Expected section, key or comment.")]
        [TestCase("4", "Syntax error (at 1:1) parsing INI format. Expected section, key or comment.")]
        [TestCase("5", "Syntax error (at 1:1) parsing INI format. Expected section, key or comment.")]
        [TestCase("6", "Syntax error (at 1:1) parsing INI format. Expected section, key or comment.")]
        [TestCase("7", "Syntax error (at 1:1) parsing INI format. Expected section, key or comment.")]
        [TestCase("8", "Syntax error (at 1:1) parsing INI format. Expected section, key or comment.")]
        [TestCase("9", "Syntax error (at 1:1) parsing INI format. Expected section, key or comment.")]

        [TestCase("[foo." , "Syntax error (at 1:5) parsing INI format. Expected ']'.")]
        [TestCase("[foo-" , "Syntax error (at 1:5) parsing INI format. Expected ']'.")]

        [TestCase("foo."  , "Syntax error (at 1:4) parsing INI format. Expected '='.")]
        [TestCase("foo-"  , "Syntax error (at 1:4) parsing INI format. Expected '='.")]
        [TestCase("foo.=" , "Syntax error (at 1:4) parsing INI format. Expected '='.")]
        [TestCase("foo-=" , "Syntax error (at 1:4) parsing INI format. Expected '='.")]

        [TestCase("[.."   , "Syntax error (at 1:3) parsing INI format. Expected non-punctuation.")]
        [TestCase("[--"   , "Syntax error (at 1:3) parsing INI format. Expected non-punctuation.")]
        [TestCase("[__"   , "Syntax error (at 1:3) parsing INI format. Expected non-punctuation.")]
        [TestCase("[foo..", "Syntax error (at 1:6) parsing INI format. Expected non-punctuation.")]
        [TestCase("[foo--", "Syntax error (at 1:6) parsing INI format. Expected non-punctuation.")]
        [TestCase("[foo__", "Syntax error (at 1:6) parsing INI format. Expected non-punctuation.")]

        [TestCase(".."   , "Syntax error (at 1:2) parsing INI format. Expected non-punctuation.")]
        [TestCase("--"   , "Syntax error (at 1:2) parsing INI format. Expected non-punctuation.")]
        [TestCase("__"   , "Syntax error (at 1:2) parsing INI format. Expected non-punctuation.")]
        [TestCase("foo..", "Syntax error (at 1:5) parsing INI format. Expected non-punctuation.")]
        [TestCase("foo--", "Syntax error (at 1:5) parsing INI format. Expected non-punctuation.")]
        [TestCase("foo__", "Syntax error (at 1:5) parsing INI format. Expected non-punctuation.")]

        public void SyntaxError(string ini, string errorMessage)
        {
            var e = Assert.Throws<FormatException>(() =>
                Ini.Parse(ini).GetEnumerator().MoveNext());
            Assert.That(e.Message, Is.EqualTo(errorMessage));
        }
    }
}
