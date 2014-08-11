#region License
// ConcurrentDictionary.cs
//
// Copyright (c) 2009 Jérémie "Garuma" Laval
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//
#endregion

namespace Provision.Providers.PortableMemoryCache.Mono
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;

    [DebuggerDisplay("Count={Count}")]
    public class ConcurrentDictionary<TKey, TValue> : IDictionary<TKey, TValue>,
      ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>,
      IDictionary, ICollection, IEnumerable
    {
        IEqualityComparer<TKey> comparer;

        SplitOrderedList<TKey, KeyValuePair<TKey, TValue>> internalDictionary;

        public ConcurrentDictionary()
            : this(EqualityComparer<TKey>.Default)
        {
        }

        public ConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection)
            : this(collection, EqualityComparer<TKey>.Default)
        {
        }

        public ConcurrentDictionary(IEqualityComparer<TKey> comparer)
        {
            this.comparer = comparer;
            this.internalDictionary = new SplitOrderedList<TKey, KeyValuePair<TKey, TValue>>(comparer);
        }

        public ConcurrentDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer)
            : this(comparer)
        {
            foreach (KeyValuePair<TKey, TValue> pair in collection)
                this.Add(pair.Key, pair.Value);
        }

        // Parameters unused
        public ConcurrentDictionary(int concurrencyLevel, int capacity)
            : this(EqualityComparer<TKey>.Default)
        {

        }

        public ConcurrentDictionary(int concurrencyLevel,
                                     IEnumerable<KeyValuePair<TKey, TValue>> collection,
                                     IEqualityComparer<TKey> comparer)
            : this(collection, comparer)
        {

        }

        // Parameters unused
        public ConcurrentDictionary(int concurrencyLevel, int capacity, IEqualityComparer<TKey> comparer)
            : this(comparer)
        {

        }

        void CheckKey(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException("key");
        }

        void Add(TKey key, TValue value)
        {
            while (!this.TryAdd(key, value)) ;
        }

        void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
        {
            this.Add(key, value);
        }

        public bool TryAdd(TKey key, TValue value)
        {
            this.CheckKey(key);
            return this.internalDictionary.Insert(this.Hash(key), key, Make(key, value));
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> pair)
        {
            this.Add(pair.Key, pair.Value);
        }

        public TValue AddOrUpdate(TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
        {
            this.CheckKey(key);
            if (addValueFactory == null)
                throw new ArgumentNullException("addValueFactory");
            if (updateValueFactory == null)
                throw new ArgumentNullException("updateValueFactory");
            return this.internalDictionary.InsertOrUpdate(this.Hash(key),
                                                      key,
                                                      () => Make(key, addValueFactory(key)),
                                                      (e) => Make(key, updateValueFactory(key, e.Value))).Value;
        }

        public TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
        {
            return this.AddOrUpdate(key, (_) => addValue, updateValueFactory);
        }

        TValue AddOrUpdate(TKey key, TValue addValue, TValue updateValue)
        {
            this.CheckKey(key);
            return this.internalDictionary.InsertOrUpdate(this.Hash(key),
                                                      key,
                                                      Make(key, addValue),
                                                      Make(key, updateValue)).Value;
        }

        TValue GetValue(TKey key)
        {
            TValue temp;
            if (!this.TryGetValue(key, out temp))
                throw new KeyNotFoundException(key.ToString());
            return temp;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            this.CheckKey(key);
            KeyValuePair<TKey, TValue> pair;
            bool result = this.internalDictionary.Find(this.Hash(key), key, out pair);
            value = pair.Value;

            return result;
        }

        public bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue)
        {
            this.CheckKey(key);
            return this.internalDictionary.CompareExchange(this.Hash(key), key, Make(key, newValue), (e) => e.Value.Equals(comparisonValue));
        }

        public TValue this[TKey key]
        {
            get
            {
                return this.GetValue(key);
            }
            set
            {
                this.AddOrUpdate(key, value, value);
            }
        }

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            this.CheckKey(key);
            return this.internalDictionary.InsertOrGet(this.Hash(key), key, Make(key, default(TValue)), () => Make(key, valueFactory(key))).Value;
        }

        public TValue GetOrAdd(TKey key, TValue value)
        {
            this.CheckKey(key);
            return this.internalDictionary.InsertOrGet(this.Hash(key), key, Make(key, value), null).Value;
        }

        public bool TryRemove(TKey key, out TValue value)
        {
            this.CheckKey(key);
            KeyValuePair<TKey, TValue> data;
            bool result = this.internalDictionary.Delete(this.Hash(key), key, out data);
            value = data.Value;
            return result;
        }

        bool Remove(TKey key)
        {
            TValue dummy;

            return this.TryRemove(key, out dummy);
        }

        bool IDictionary<TKey, TValue>.Remove(TKey key)
        {
            return this.Remove(key);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> pair)
        {
            return this.Remove(pair.Key);
        }

        public bool ContainsKey(TKey key)
        {
            this.CheckKey(key);
            KeyValuePair<TKey, TValue> dummy;
            return this.internalDictionary.Find(this.Hash(key), key, out dummy);
        }

        bool IDictionary.Contains(object key)
        {
            if (!(key is TKey))
                return false;

            return this.ContainsKey((TKey)key);
        }

        void IDictionary.Remove(object key)
        {
            if (!(key is TKey))
                return;

            this.Remove((TKey)key);
        }

        object IDictionary.this[object key]
        {
            get
            {
                TValue obj;
                if (key is TKey && this.TryGetValue((TKey)key, out obj))
                    return obj;
                return null;
            }
            set
            {
                if (!(key is TKey) || !(value is TValue))
                    throw new ArgumentException("key or value aren't of correct type");

                this[(TKey)key] = (TValue)value;
            }
        }

        void IDictionary.Add(object key, object value)
        {
            if (!(key is TKey) || !(value is TValue))
                throw new ArgumentException("key or value aren't of correct type");

            this.Add((TKey)key, (TValue)value);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> pair)
        {
            TValue value;
            if (!this.TryGetValue(pair.Key, out value))
                return false;

            return EqualityComparer<TValue>.Default.Equals(value, pair.Value);
        }

        public KeyValuePair<TKey, TValue>[] ToArray()
        {
            // This is most certainly not optimum but there is
            // not a lot of possibilities

            return new List<KeyValuePair<TKey, TValue>>(this).ToArray();
        }

        public void Clear()
        {
            // Pronk
            this.internalDictionary = new SplitOrderedList<TKey, KeyValuePair<TKey, TValue>>(this.comparer);
        }

        public int Count
        {
            get
            {
                return this.internalDictionary.Count;
            }
        }

        public bool IsEmpty
        {
            get
            {
                return this.Count == 0;
            }
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        bool IDictionary.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                return this.GetPart<TKey>((kvp) => kvp.Key);
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                return this.GetPart<TValue>((kvp) => kvp.Value);
            }
        }

        ICollection IDictionary.Keys
        {
            get
            {
                return (ICollection)this.Keys;
            }
        }

        ICollection IDictionary.Values
        {
            get
            {
                return (ICollection)this.Values;
            }
        }

        ICollection<T> GetPart<T>(Func<KeyValuePair<TKey, TValue>, T> extractor)
        {
            List<T> temp = new List<T>();

            foreach (KeyValuePair<TKey, TValue> kvp in this)
                temp.Add(extractor(kvp));

            return new ReadOnlyCollection<T>(temp);
        }

        void ICollection.CopyTo(Array array, int startIndex)
        {
            KeyValuePair<TKey, TValue>[] arr = array as KeyValuePair<TKey, TValue>[];
            if (arr == null)
                return;

            this.CopyTo(arr, startIndex, this.Count);
        }

        void CopyTo(KeyValuePair<TKey, TValue>[] array, int startIndex)
        {
            this.CopyTo(array, startIndex, this.Count);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int startIndex)
        {
            this.CopyTo(array, startIndex);
        }

        void CopyTo(KeyValuePair<TKey, TValue>[] array, int startIndex, int num)
        {
            foreach (var kvp in this)
            {
                array[startIndex++] = kvp;

                if (--num <= 0)
                    return;
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return this.GetEnumeratorInternal();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)this.GetEnumeratorInternal();
        }

        IEnumerator<KeyValuePair<TKey, TValue>> GetEnumeratorInternal()
        {
            return this.internalDictionary.GetEnumerator();
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return new ConcurrentDictionaryEnumerator(this.GetEnumeratorInternal());
        }

        class ConcurrentDictionaryEnumerator : IDictionaryEnumerator
        {
            IEnumerator<KeyValuePair<TKey, TValue>> internalEnum;

            public ConcurrentDictionaryEnumerator(IEnumerator<KeyValuePair<TKey, TValue>> internalEnum)
            {
                this.internalEnum = internalEnum;
            }

            public bool MoveNext()
            {
                return this.internalEnum.MoveNext();
            }

            public void Reset()
            {
                this.internalEnum.Reset();
            }

            public object Current
            {
                get
                {
                    return this.Entry;
                }
            }

            public DictionaryEntry Entry
            {
                get
                {
                    KeyValuePair<TKey, TValue> current = this.internalEnum.Current;
                    return new DictionaryEntry(current.Key, current.Value);
                }
            }

            public object Key
            {
                get
                {
                    return this.internalEnum.Current.Key;
                }
            }

            public object Value
            {
                get
                {
                    return this.internalEnum.Current.Value;
                }
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                return this;
            }
        }

        bool IDictionary.IsFixedSize
        {
            get
            {
                return false;
            }
        }

        bool ICollection.IsSynchronized
        {
            get { return true; }
        }

        static KeyValuePair<U, V> Make<U, V>(U key, V value)
        {
            return new KeyValuePair<U, V>(key, value);
        }

        uint Hash(TKey key)
        {
            return (uint)this.comparer.GetHashCode(key);
        }
    }
}