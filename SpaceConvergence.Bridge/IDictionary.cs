using Bridge;

namespace System.Collections.Generic
{
    [External]
    [Reflectable]
    [Convention(Target = ConventionTarget.Member, Member = ConventionMember.Method, Notation = Notation.LowerCamelCase)]
    [Name("System.Collections.Generic.IDictionary$2")]
    public interface _IDictionary<TKey, TValue> : ICollection<KeyValuePair<TKey, TValue>>, IBridgeClass
    {
        [AccessorsIndexer]
        TValue this[TKey key]
        {
            [Name("getItem")]
            get;
            [Name("setItem")]
            set;
        }

        ICollection<TKey> Keys
        {
            get;
        }

        ICollection<TValue> Values
        {
            get;
        }

        bool ContainsKey(TKey key);

        void Add(TKey key, TValue value);

        bool Remove(TKey key);

        bool TryGetValue(TKey key, out TValue value);
    }
}

namespace System.Collections
{

    using System.Collections.Generic;
    using Bridge;
    [External]
    [Unbox(true)]
    [Convention(Target = ConventionTarget.Member, Member = ConventionMember.Method, Notation = Notation.LowerCamelCase)]
    [Reflectable, Name("System.Collections.IDictionary")]
    public interface _IDictionary : ICollection, IEnumerable, IBridgeClass
    {
        bool IsFixedSize
        {
            get;
        }

        bool IsReadOnly
        {
            get;
        }

        object this[object key]
        {
            get;
            set;
        }

        ICollection Keys
        {
            get;
        }

        ICollection Values
        {
            get;
        }

        void Add(object key, object value);

        void Clear();

        bool Contains(object key);

        new IDictionaryEnumerator GetEnumerator();

        void Remove(object key);
    }

    [FileName("dictionary.js")]
    public interface IDictionaryEnumerator : IEnumerator
    {
        DictionaryEntry Entry
        {
            get;
        }

        object Key
        {
            get;
        }

        object Value
        {
            get;
        }
    }

    [Serializable]
    [FileName("dictionary.js")]
    public struct DictionaryEntry
    {
        private object _key;

        private object _value;

        public object Key
        {
            get
            {
                return this._key;
            }
            set
            {
                this._key = value;
            }
        }

        public object Value
        {
            get
            {
                return this._value;
            }
            set
            {
                this._value = value;
            }
        }

        public DictionaryEntry(object key, object value)
        {
            this._key = key;
            this._value = value;
        }
    }
}