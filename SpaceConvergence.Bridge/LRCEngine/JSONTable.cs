using System.Collections;
using System.Collections.Generic;
using System;
using Microsoft.Xna.Framework;

namespace LRCEngine
{
    public class JSONArray
    {
        System.Object[] array;

        public static JSONArray empty = new JSONArray(new System.Object[] { });

        public JSONArray(System.Object[] inArray)
        {
            array = inArray;
        }

        public JSONArray(IEnumerable inArray)
        {
            System.Object unused = null;
            System.Object unused2 = null;
            int count = 0;
            foreach (System.Object obj in inArray)
            {
                unused = obj; // suppress warnings about unused obj
                ++count;
            }
            unused2 = unused;
            unused = unused2;

            array = new System.Object[count];
            int idx = 0;
            foreach (System.Object obj in inArray)
            {
                array[idx] = obj;
                ++idx;
            }
        }

        public System.Object this[int key]
        {
            get { return array[key]; }
        }

        public int Length
        {
            get { return array.Length; }
        }

        public void AddToSet(_Dictionary<string, bool> theSet)
        {
            foreach (string s in array)
            {
                theSet[s] = true;
            }
        }

        public JSONArray_JSONTables asJSONTables()
        {
            return new JSONArray_JSONTables(array.GetEnumerator());
        }

        public JSONArray_JSONArrays asJSONArrays()
        {
            return new JSONArray_JSONArrays(array.GetEnumerator());
        }

        public JSONArrayEnumerator<String> asStrings()
        {
            return new JSONArrayEnumerator<String>(array.GetEnumerator());
        }

        public Microsoft.Xna.Framework.Point toPoint()
        {
            return new Microsoft.Xna.Framework.Point(this.getInt(0), this.getInt(1));
        }

        public Microsoft.Xna.Framework.Vector2 toVector2()
        {
            return new Microsoft.Xna.Framework.Vector2(this.getFloat(0), this.getFloat(1));
        }

        public Microsoft.Xna.Framework.Vector3 toVector3()
        {
            return new Microsoft.Xna.Framework.Vector3(this.getFloat(0), this.getFloat(1), this.getFloat(2));
        }

        public System.Object getProperty(int idx)
        {
            return array[idx];
        }

        public int getInt(int idx)
        {
            return (int)(double)array[idx];
        }

        public float getFloat(int idx)
        {
            return (float)(double)array[idx];
        }

        public double getDouble(int idx)
        {
            return (double)array[idx];
        }

        public string getString(int idx)
        {
            return (string)array[idx];
        }

        public string getString(int idx, string defaultValue)
        {
            if (array.Length > idx)
                return (string)array[idx];
            else
                return defaultValue;
        }

        public bool getBool(int idx)
        {
            return (bool)array[idx];
        }

        public JSONArray getArray(int idx)
        {
            return new JSONArray((System.Object[])array[idx]);
        }

        public JSONTable getJSON(int idx)
        {
            return new JSONTable((_Dictionary<string, System.Object>)array[idx]);
        }

        public string[] toStringArray()
        {
            string[] result = new string[array.Length];
            for (int Idx = 0; Idx < array.Length; ++Idx)
            {
                result[Idx] = (string)array[Idx];
            }

            return result;
        }

        public override string ToString()
        {
            string result = "[ ";
            foreach (System.Object val in array)
            {
                if (val.GetType() == typeof(string))
                {
                    result += "\"" + val + "\", ";
                }
                else
                {
                    result += "" + val + ", ";
                }
            }
            result += " ]";
            return result;
        }
    }

    public class JSONArray_JSONTables : JSONArrayEnumerator<JSONTable>
    {
        public JSONArray_JSONTables(IEnumerator aBaseEnumerator) : base(aBaseEnumerator)
        {
        }

        public override JSONTable Current
        {
            get { return new JSONTable((_Dictionary<string, System.Object>)baseEnumerator.Current); }
        }
    }

    public class JSONArray_JSONArrays : JSONArrayEnumerator<JSONArray>
    {
        public JSONArray_JSONArrays(IEnumerator aBaseEnumerator) : base(aBaseEnumerator)
        {
        }

        public override JSONArray Current
        {
            get { return new JSONArray((System.Object[])baseEnumerator.Current); }
        }
    }

    public class JSONArrayEnumerator<T> : IEnumerator<T>
    {
        protected IEnumerator baseEnumerator;

        public JSONArrayEnumerator(IEnumerator aBaseEnumerator)
        {
            baseEnumerator = aBaseEnumerator;
        }

        public IEnumerator GetEnumerator()
        {
            return this;
        }

        public bool MoveNext()
        {
            return baseEnumerator.MoveNext();
        }

        public void Reset()
        {
            baseEnumerator.Reset();
        }

        void IDisposable.Dispose()
        {
        }

        public virtual T Current
        {
            get { return (T)baseEnumerator.Current; }
        }

        object IEnumerator.Current { get { return Current; } }
    }

    public class JSONTable
    {
        _Dictionary<string, System.Object> dictionary;

        public JSONTable()
        {
            dictionary = new _Dictionary<string, System.Object>();
        }

        public JSONTable(_Dictionary<string, System.Object> inDictionary)
        {
            dictionary = inDictionary;
        }

        public JSONTable(string filename)
        {
            var request = new Bridge.Html5.XMLHttpRequest();
            request.Open("GET", filename, false);
            request.Send((string)null);
            int idx = 0;
            dictionary = (_Dictionary<string, System.Object>)parseValue(request.ResponseText, ref idx);
        }

        public ICollection<string> Keys
        {
            get { return dictionary.Keys; }
        }

        public bool hasKey(string name)
        {
            return dictionary.ContainsKey(name);
        }

        public System.Object getProperty(string name)
        {
            return dictionary[name];
        }

        public System.Object getProperty(string name, System.Object defaultValue)
        {
            if (dictionary.ContainsKey(name))
                return dictionary[name];
            else
                return defaultValue;
        }

        public int getInt(string name, int defaultValue)
        {
            if (dictionary.ContainsKey(name))
            {
                return (int)(double)dictionary[name]; // values are stored in the dictionary as doubles
            }
            else
            {
                return defaultValue;
            }
        }

        public float getFloat(string name, float defaultValue)
        {
            if (dictionary.ContainsKey(name))
            {
                return (float)(double)dictionary[name]; // values are stored in the dictionary as doubles
            }
            else
            {
                return defaultValue;
            }
        }

        public double getDouble(string name, double defaultValue)
        {
            if (dictionary.ContainsKey(name))
            {
                return (double)dictionary[name];
            }
            else
            {
                return defaultValue;
            }
        }

        public string getString(string name, string defaultValue)
        {
            if (dictionary.ContainsKey(name))
            {
                return (string)dictionary[name];
            }
            else
            {
                return defaultValue;
            }
        }

        public bool getBool(string name, bool defaultValue)
        {
            if (dictionary.ContainsKey(name))
            {
                return (bool)dictionary[name];
            }
            else
            {
                return defaultValue;
            }
        }

        public JSONArray getArray(string name, JSONArray defaultValue)
        {
            if (dictionary.ContainsKey(name))
            {
                return new JSONArray((System.Object[])dictionary[name]);
            }
            else
            {
                return defaultValue;
            }
        }

        public JSONTable getJSON(string name, JSONTable defaultValue)
        {
            if (dictionary.ContainsKey(name))
            {
                return new JSONTable((_Dictionary<string, System.Object>)dictionary[name]);
            }
            else
            {
                return defaultValue;
            }
        }

        public static void LogError(string error)
        {
            throw new ArgumentException(error);
        }

        public int getInt(string name)
        {
            if (!dictionary.ContainsKey(name))
                LogError("Table has no int called " + name);
            return (int)(double)dictionary[name];
        }

        public float getFloat(string name)
        {
            if (!dictionary.ContainsKey(name))
                LogError("Table has no float called " + name);
            return (float)(double)dictionary[name];
        }

        public double getDouble(string name)
        {
            if (!dictionary.ContainsKey(name))
                LogError("Table has no double called " + name);
            return (double)dictionary[name];
        }

        public string getString(string name)
        {
            if (!dictionary.ContainsKey(name))
                LogError("Table has no string called " + name);
            return (string)dictionary[name];
        }

        public Vector2 getVector2(string name)
        {
            JSONArray array = getArray(name);
            if (array.Length != 2)
                LogError("getVector2 - array length is " + array.Length);
            return array.toVector2();
        }

        public bool getBool(string name)
        {
            if (!dictionary.ContainsKey(name))
                LogError("Table has no bool called " + name);
            return (bool)dictionary[name];
        }

        public JSONArray getArray(string name)
        {
            if (!dictionary.ContainsKey(name))
                LogError("Table has no array called " + name);
            return new JSONArray((System.Object[])dictionary[name]);
        }

        public JSONTable getJSON(string name)
        {
            if (!dictionary.ContainsKey(name))
                LogError("Table has no subtable called " + name);
            return new JSONTable((_Dictionary<string, System.Object>)dictionary[name]);
        }

        static System.Object parseValue(string json, ref int idx)
        {
            SkipWhitespace(json, ref idx);
            if (json[idx] == '{')
            {
                _Dictionary<string, System.Object> result = new _Dictionary<string, System.Object>();

                while (true)
                {
                    ++idx;
                    SkipWhitespace(json, ref idx);

                    // permit trailing commas - {"foo":"bar" , } is legal
                    if (json[idx] == '}')
                    {
                        ++idx;
                        return result;
                    }

                    string key = (string)parseValue(json, ref idx);
                    SkipWhitespace(json, ref idx);

                    if (json[idx] != ':')
                    {
                        ReportError(json, idx, "Invalid keyvalue separator: " + json[idx] + "!");
                        return null;
                    }

                    ++idx;
                    System.Object value = parseValue(json, ref idx);
                    result[key] = value;

                    SkipWhitespace(json, ref idx);

                    if (json[idx] == '}')
                    {
                        ++idx;
                        return result;
                    }
                    else if (json[idx] != ',')
                    {
                        //ReportError(json, idx, "Expected a comma, got: " + json[idx] + "");
                        //return null;
                        // permit missing commas - {"foo":1 "bar":1 } is legal
                        idx--;
                    }
                }
            }
            else if (json[idx] == '[')
            {
                List<System.Object> values = new List<System.Object>();
                ++idx;

                while (true)
                {
                    SkipWhitespace(json, ref idx);

                    if (json[idx] == ']')
                    {
                        ++idx;
                        return values.ToArray();
                    }

                    System.Object value = parseValue(json, ref idx);
                    SkipWhitespace(json, ref idx);

                    values.Add(value);

                    SkipWhitespace(json, ref idx);

                    if (json[idx] == ',')
                    {
                        ++idx;
                    }
                    else if (json[idx] != ']')
                    {
                        ReportError(json, idx, "Expected a comma, got: " + json[idx] + "");
                        return null;
                    }
                }
            }
            else if (json[idx] == '"')
            {
                ++idx;
                String stringSoFar = "";
                int startIdx = idx;
                while (json[idx] != '"')
                {
                    if (json[idx] == '\\')
                    {
                        stringSoFar += json.Substring(startIdx, idx - startIdx);
                        idx++;
                        if (json[idx] == 'n')
                        {
                            stringSoFar += '\n';
                        }
                        else
                        {
                            stringSoFar += json[idx];
                        }
                        startIdx = idx + 1;
                    }
                    ++idx;
                }
                ++idx;
                return stringSoFar + json.Substring(startIdx, idx - startIdx - 1);
            }
            else if (json[idx] == '-' || json[idx] >= '0' && json[idx] <= '9')
            {
                bool negate = (json[idx] == '-');
                if (negate)
                {
                    ++idx;
                }

                int numberSoFar = 0;
                do
                {
                    numberSoFar = numberSoFar * 10 + json[idx] - '0';
                    ++idx;
                }
                while (json[idx] >= '0' && json[idx] <= '9');

                double result;

                if (json[idx] == '.')
                {
                    // floating point
                    ++idx;

                    int fractionSoFar = 0;
                    double divisor = 1.0f;
                    do
                    {
                        fractionSoFar = fractionSoFar * 10 + json[idx] - '0';
                        divisor *= 10.0f;
                        ++idx;
                    }
                    while (json[idx] >= '0' && json[idx] <= '9');

                    result = numberSoFar + fractionSoFar / divisor;
                }
                else
                {
                    result = numberSoFar;
                }

                if (negate)
                    return -result;
                else
                    return result;
            }
            else if (json[idx] >= 'a' && json[idx] <= 'z')
            {
                int startIdx = idx;
                do
                {
                    ++idx;
                }
                while (json[idx] >= 'a' && json[idx] <= 'z');

                string keyword = json.Substring(startIdx, idx - startIdx);
                if (keyword == "false")
                {
                    return false;
                }
                else if (keyword == "true")
                {
                    return true;
                }
                else
                {
                    ReportError(json, idx, "Invalid json keyword: " + keyword + "!");
                    return null;
                }
            }
            else
            {
                ReportError(json, idx, "Invalid symbol: '" + json[idx] + "'");
                return null;
            }
        }

        static void SkipWhitespace(string text, ref int idx)
        {
            if (text.Length <= idx)
            {
                return;
            }

            char c = text[idx];
            while (c == ' ' || c == '\t' || c == '\r' || c == '\n')
            {
                ++idx;
                c = text[idx];
            }

            if (c == '/')
            {
                if (text[idx + 1] == '/')
                {
                    // comment
                    ++idx; // to the /
                    do
                    {
                        ++idx; // to the character after the /
                        c = text[idx];
                    }
                    while (idx < text.Length && c != '\n');
                }
                else if (text[idx + 1] == '*')
                {
                    /* comment */
                    int startIdx = idx;
                    ++idx; // to the *
                    do
                    {
                        ++idx; // to the character after the *
                        c = text[idx];
                    }
                    while (idx < text.Length && (c != '*' || text[idx + 1] != '/'));
                    if (idx == text.Length)
                    {
                        ReportError(text, startIdx, "Unterminated /* comment");
                    }
                    else
                    {
                        idx += 2; // to the character after the */
                    }
                }

                SkipWhitespace(text, ref idx);
            }
        }

        public static String parseCommandWord(string text, ref int idx)
        {
            SkipWhitespace(text, ref idx);
            if ((text[idx] >= 'a' && text[idx] <= 'z') || (text[idx] >= 'A' && text[idx] <= 'Z'))
            {
                int startIdx = idx;
                do
                {
                    ++idx;
                }
                while (idx < text.Length && ((text[idx] >= 'a' && text[idx] <= 'z') || (text[idx] >= 'A' && text[idx] <= 'Z')));

                string word = text.Substring(startIdx, idx - startIdx);
                if (word == "true" || word == "false")
                {
                    return null; // can't handle keywords here
                }
                return word;
            }
            else
            {
                return null;
            }
        }

        static void ReportError(string json, int errorAt, string message)
        {
            int lineNumber = 1;
            int lineStartIdx = 0;
            for (int idx = 0; idx <= errorAt; ++idx)
            {
                if (json[idx] == '\n')
                {
                    ++lineNumber;
                    lineStartIdx = idx + 1;
                }
            }

            string lineText = "";
            for (int endIdx = errorAt + 1; endIdx < json.Length; ++endIdx)
            {
                if (json[endIdx] == '\n' || json[endIdx] == '\r')
                {
                    lineText = json.Substring(lineStartIdx, endIdx - lineStartIdx);
                    break;
                }
            }

            LogError("JSON error at line " + lineNumber + " - " + lineText + "\n" + message);
        }

        public override string ToString()
        {
            string result = "{ ";
            foreach (string key in dictionary.Keys)
            {
                System.Object val = dictionary[key];
                if (val.GetType() == typeof(string))
                {
                    result += "\"" + key + "\":\"" + val + "\",\n";
                }
                else
                {
                    result += "\"" + key + "\":" + val + ",\n";
                }
            }
            result += "}";
            return result;
        }

        public void Add(string key, System.Object val)
        {
            dictionary[key] = val;
        }
    }
}