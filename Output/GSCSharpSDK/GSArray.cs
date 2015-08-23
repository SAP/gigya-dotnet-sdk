/*
 * Copyright (C) 2011 Gigya, Inc.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace Gigya.Socialize.SDK
{
    [Serializable]
    public class GSArray : IEnumerable
    {
        private const string NO_INDEX_EX = "GSArray does not contain a value at index ";
        private JSONArray _array = new JSONArray();

        #region Constructors

        public GSArray() { }

        public GSArray(string json) : this(new JSONArray(json)) { }

        internal GSArray(JSONArray jsonArray)
        {
            for (int i = 0; i < jsonArray.Count; i++)
            {
                object value = jsonArray[i];
                if (value is JSONObject)
                {
                    GSObject obj = new GSObject((JSONObject)value);
                    _array.Add(obj);
                }
                else if (value is JSONArray)
                {
                    GSArray arr = new GSArray((JSONArray)value);
                    _array.Add(arr);
                }
                else
                {
                    _array.Add(value);
                }
            }
        }

        #endregion


        #region - ADDS -

        public void Add(string val) { _array.Add(val); }
        public void Add(bool val) { _array.Add(val); }
        public void Add(int val) { _array.Add(val); }
        public void Add(long val) { _array.Add(val); }
        public void Add(GSObject val) { _array.Add(val); }
        public void Add(GSArray val) { _array.Add(val); }
        public void Add(double val) { _array.Add(val); }
        internal void Add(object val) { _array.Add(val); }
        #endregion


        #region - GETS -
        public string GetString(int index)
        {
            object obj = _array[index];
            if (obj == null)
                return null;
            else
                return obj.ToString();
        }

        public bool GetBool(int index)
        {
            object obj = _array[index];
            if (obj == null)
                throw new NullReferenceException(NO_INDEX_EX + index);

            if (obj is bool)
            {
                return (bool)obj;
            }
            else
            {
                string st = obj.ToString().ToLower();
                return st == "true" || st == "1";
            }
        }

        public int GetInt(int index)
        {
            object obj = _array[index];
            if (obj == null)
                throw new NullReferenceException(NO_INDEX_EX + index);

            if (obj is int)
            {
                return (int)obj;
            }
            else
            {
                return int.Parse(this.GetString(index));
            }
        }

        public long GetLong(int index)
        {
            object obj = _array[index];
            if (obj == null)
                throw new NullReferenceException(NO_INDEX_EX + index);

            if (obj is long)
            {
                return (long)obj;
            }
            else
            {
                return long.Parse(this.GetString(index));
            }
        }

        public double GetDouble(int index)
        {
            object obj = _array[index];
            if (obj == null)
                throw new NullReferenceException(NO_INDEX_EX + index);

            if (obj is double || obj is decimal)
            {
                return (double)obj;
            }
            else
            {
                return double.Parse(this.GetString(index));
            }
        }

        public T GetObject<T>(int index) where T : class,new()
        {
            GSObject obj = GetObject(index);
            return obj.Cast<T>();
        }

        public GSObject GetObject(int index)
        {
            object obj = _array[index];
            if (obj == null)
                return null;
            else
                return (GSObject)obj;
        }

        public IEnumerable<T> GetArray<T>(int index) where T : class,new()
        {
            GSArray array = GetArray(index);
            return array.Cast<T>();
        }

        public GSArray GetArray(int index)
        {
            object obj = _array[index];
            if (obj == null)
                return null;
            else
                return (GSArray)obj;
        }
        #endregion


        internal JSONArray ToJsonArray()
        {
            JSONArray ret = new JSONArray();
            foreach (var item in this._array)
            {
                if (item is GSObject)
                {
                    ret.Add(((GSObject)item).ToJsonObject());
                }
                else if (item is GSArray)
                {
                    ret.Add(((GSArray)item).ToJsonArray());
                }
                else
                {
                    ret.Add(item);
                }
            }
            return ret;
        }


        public int Length
        {
            get { return this._array.Count; }
        }

        public int Count
        {
            get { return this._array.Count; }
        }

        public override string ToString()
        {
            return this.ToJsonArray().ToString();
        }

        public IEnumerator GetEnumerator()
        {
            for (int i = 0; i < this._array.Count; i++)
            {
                yield return this._array[i];
            }
        }

        public object this[int i]
        {
            get
            {
                return this._array[i];
            }
            set
            {
                this._array[i] = value;
            }
        }

        protected object[] Array { get { return _array.ToObjectArray(); } }


        internal IEnumerable<T> Get<T>(string[] path, int pos, bool attempt_conversion)
        {
            string key = path[pos];
            int index;

            // The key is an index; otherwise ignore
            if (key[0] == '[' && key[key.Length - 1] == ']')
            {

                // The key refers to all items ("[*]")
                if (key.Length == 3 && key[1] == '*')
                    foreach (var item in _array)
                        foreach (var value in GSObject.Get<T>(path, pos, item, attempt_conversion))
                            yield return value;

                // The key is a specific index (e.g. "[2]") and within the array bounds
                else if (int.TryParse(key.Substring(1, key.Length - 2), out index) && index < Length)
                    foreach (var value in GSObject.Get<T>(path, pos, _array[index], attempt_conversion))
                        yield return value;
            }
        }

        public IEnumerable<T> Cast<T>() where T : class,new()
        {
            foreach (object item in this)
            {
                yield return (T)item;
            }
        }

        internal IEnumerable Cast(Type memberType)
        {

            IList list = (IList)Activator.CreateInstance(memberType);

            Type[] listGenericTypes = memberType.GetGenericArguments();
            Type listGenericType = listGenericTypes[0];

            foreach (object item in this)
            {
                object itemToAdd;
                if (null == listGenericType)
                    itemToAdd = item;
                else
                {
                    if (item is GSObject)
                    {
                        itemToAdd = (item as GSObject).Cast(listGenericType);
                    }
                    else if (item is GSArray)
                    {
                        itemToAdd = (item as GSArray).Cast(listGenericType);
                    }
                    else
                    {
                        itemToAdd = Convert.ChangeType(item, listGenericType);
                    }
                }

                list.Add(itemToAdd);
            }
            return list;
        }
    }

    [Serializable]
    internal class JSONArray : List<object>
    {
        public JSONArray() { }

        public JSONArray(string json) : this(Deserialize(json)) { }

        public JSONArray(object[] array)
        {
            if (array != null)
            {
                foreach (var item in array)
                {
                    if (item is Dictionary<string, object>)
                    {
                        this.Add(new JSONObject((Dictionary<string, object>)item));
                    }
                    else if (item is object[])
                    {
                        this.Add(new JSONArray((object[])item));
                    }
                    else
                        this.Add(item);
                }
            }
        }

        static object[] Deserialize(string json)
        {
            JavaScriptSerializer ds = new JavaScriptSerializer();
            ds.MaxJsonLength = 50 * 1024 * 1024;
            return (object[])ds.DeserializeObject(json);
        }

        internal object[] ToObjectArray()
        {
            List<object> jsonObj = new List<object>();
            foreach (var item in this)
            {
                if (item is JSONObject)
                {
                    jsonObj.Add(((JSONObject)item).ToSortedDictionary());
                }
                else if (item is JSONArray)
                {
                    jsonObj.Add(((JSONArray)item).ToObjectArray());
                }
                else
                {
                    jsonObj.Add(item);
                }
            }
            return jsonObj.ToArray();
        }

        public override string ToString()
        {
            object[] obj = this.ToObjectArray();
            string ret = new JavaScriptSerializer().Serialize(obj);
            return ret;
        }
    }
}