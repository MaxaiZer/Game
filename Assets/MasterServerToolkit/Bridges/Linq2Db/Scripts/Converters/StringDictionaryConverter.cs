using LinqToDB.Common;
using MasterServerToolkit.Json;
using System.Collections.Generic;

namespace MasterServerToolkit.Bridges.Linq2Db
{
    public class StringDictionaryConverter : ValueConverter<Dictionary<string, string>, string>
    {
        public StringDictionaryConverter() : base(v => To(v), p => From(p), true) { }

        private static string To(Dictionary<string, string> v)
        {
            return v == null ? null : new MstJson(v).ToString();
        }

        private static Dictionary<string, string> From(string p)
        {
            return p == null ? null : new MstJson(p).ToDictionary();
        }
    }
}
