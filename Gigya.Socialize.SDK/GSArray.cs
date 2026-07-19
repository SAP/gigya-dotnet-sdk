/*
 * Copyright (C) 2024 SAP SE
 * Modern .NET 9 SDK - GSArray
 */

using System.Collections;
using System.Globalization;
using System.Text.Json;

namespace Gigya.Socialize.SDK;

/// <summary>
/// Array container for collections of values.
/// Can hold: string, boolean, int, long, double, GSObject, GSArray.
/// </summary>
[Serializable]
public class GSArray : IEnumerable
{
    private const string NoIndexException = "GSArray does not contain a value at index ";
    private readonly List<object?> _array = new();

    #region Constructors

    /// <summary>
    /// Default constructor.
    /// </summary>
    public GSArray()
    {
    }

    /// <summary>
    /// Construct a GSArray from a JSON string.
    /// </summary>
    /// <param name="json">The JSON formatted array string.</param>
    public GSArray(string json)
    {
        var elements = JsonSerializer.Deserialize<JsonElement[]>(json);
        if (elements == null) return;

        foreach (var element in elements)
        {
            _array.Add(ConvertJsonElement(element));
        }
    }

    private static object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when element.TryGetInt32(out var i) => i,
            JsonValueKind.Number when element.TryGetInt64(out var l) => l,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Object => CreateGSObjectFromElement(element),
            JsonValueKind.Array => CreateGSArrayFromElement(element),
            _ => element.GetRawText()
        };
    }

    private static GSObject CreateGSObjectFromElement(JsonElement element)
    {
        var json = element.GetRawText();
        return new GSObject(json);
    }

    private static GSArray CreateGSArrayFromElement(JsonElement element)
    {
        var arr = new GSArray();
        foreach (var item in element.EnumerateArray())
        {
            arr._array.Add(ConvertJsonElement(item));
        }
        return arr;
    }

    #endregion

    #region Add Methods

    /// <summary>
    /// Adds a string value to the array.
    /// </summary>
    public void Add(string? val) => _array.Add(val);

    /// <summary>
    /// Adds a bool value to the array.
    /// </summary>
    public void Add(bool val) => _array.Add(val);

    /// <summary>
    /// Adds an int value to the array.
    /// </summary>
    public void Add(int val) => _array.Add(val);

    /// <summary>
    /// Adds a long value to the array.
    /// </summary>
    public void Add(long val) => _array.Add(val);

    /// <summary>
    /// Adds a double value to the array.
    /// </summary>
    public void Add(double val) => _array.Add(val);

    /// <summary>
    /// Adds a GSObject value to the array.
    /// </summary>
    public void Add(GSObject? val) => _array.Add(val);

    /// <summary>
    /// Adds a GSArray value to the array.
    /// </summary>
    public void Add(GSArray? val) => _array.Add(val);

    /// <summary>
    /// Internal method to add any object value.
    /// </summary>
    internal void AddInternal(object? val) => _array.Add(val);

    #endregion

    #region Get Methods

    /// <summary>
    /// Returns the string value at the specified index.
    /// </summary>
    /// <param name="index">The index of the element to return.</param>
    /// <returns>The string value, or null if the element is null.</returns>
    public string? GetString(int index)
    {
        var obj = _array[index];
        return obj?.ToString();
    }

    /// <summary>
    /// Returns the string value at the specified index, or the default value if out of range.
    /// </summary>
    /// <param name="index">The index of the element to return.</param>
    /// <param name="defaultValue">The default value to return if index is out of range.</param>
    /// <returns>The string value, or the default value.</returns>
    public string? GetString(int index, string? defaultValue)
    {
        if (index < 0 || index >= _array.Count)
            return defaultValue;
        var obj = _array[index];
        return obj?.ToString() ?? defaultValue;
    }

    /// <summary>
    /// Returns the bool value at the specified index.
    /// </summary>
    /// <param name="index">The index of the element to return.</param>
    /// <returns>The bool value.</returns>
    /// <exception cref="NullReferenceException">Thrown if the element is null.</exception>
    public bool GetBool(int index)
    {
        var obj = _array[index];
        if (obj == null)
            throw new NullReferenceException(NoIndexException + index);

        if (obj is bool b)
            return b;

        var str = obj.ToString()?.ToLowerInvariant();
        return str == "true" || str == "1";
    }

    /// <summary>
    /// Returns the bool value at the specified index, or the default value if out of range.
    /// </summary>
    /// <param name="index">The index of the element to return.</param>
    /// <param name="defaultValue">The default value to return if index is out of range.</param>
    /// <returns>The bool value, or the default value.</returns>
    public bool GetBool(int index, bool defaultValue)
    {
        if (index < 0 || index >= _array.Count)
            return defaultValue;
        try
        {
            return GetBool(index);
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Returns the int value at the specified index.
    /// </summary>
    /// <param name="index">The index of the element to return.</param>
    /// <returns>The int value.</returns>
    /// <exception cref="NullReferenceException">Thrown if the element is null.</exception>
    public int GetInt(int index)
    {
        var obj = _array[index];
        if (obj == null)
            throw new NullReferenceException(NoIndexException + index);

        if (obj is int i)
            return i;

        // Use InvariantCulture for consistent parsing across locales
        return int.Parse(GetString(index)!, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Returns the int value at the specified index, or the default value if out of range.
    /// </summary>
    /// <param name="index">The index of the element to return.</param>
    /// <param name="defaultValue">The default value to return if index is out of range.</param>
    /// <returns>The int value, or the default value.</returns>
    public int GetInt(int index, int defaultValue)
    {
        if (index < 0 || index >= _array.Count)
            return defaultValue;
        try
        {
            return GetInt(index);
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Returns the long value at the specified index.
    /// </summary>
    /// <param name="index">The index of the element to return.</param>
    /// <returns>The long value.</returns>
    /// <exception cref="NullReferenceException">Thrown if the element is null.</exception>
    public long GetLong(int index)
    {
        var obj = _array[index];
        if (obj == null)
            throw new NullReferenceException(NoIndexException + index);

        if (obj is long l)
            return l;

        // Use InvariantCulture for consistent parsing across locales
        return long.Parse(GetString(index)!, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Returns the long value at the specified index, or the default value if out of range.
    /// </summary>
    /// <param name="index">The index of the element to return.</param>
    /// <param name="defaultValue">The default value to return if index is out of range.</param>
    /// <returns>The long value, or the default value.</returns>
    public long GetLong(int index, long defaultValue)
    {
        if (index < 0 || index >= _array.Count)
            return defaultValue;
        try
        {
            return GetLong(index);
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Returns the double value at the specified index.
    /// </summary>
    /// <param name="index">The index of the element to return.</param>
    /// <returns>The double value.</returns>
    /// <exception cref="NullReferenceException">Thrown if the element is null.</exception>
    public double GetDouble(int index)
    {
        var obj = _array[index];
        if (obj == null)
            throw new NullReferenceException(NoIndexException + index);

        if (obj is double d)
            return d;
        if (obj is decimal dec)
            return (double)dec;

        // Use InvariantCulture for consistent parsing across locales
        return double.Parse(GetString(index)!, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Returns the double value at the specified index, or the default value if out of range.
    /// </summary>
    /// <param name="index">The index of the element to return.</param>
    /// <param name="defaultValue">The default value to return if index is out of range.</param>
    /// <returns>The double value, or the default value.</returns>
    public double GetDouble(int index, double defaultValue)
    {
        if (index < 0 || index >= _array.Count)
            return defaultValue;
        try
        {
            return GetDouble(index);
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Returns the GSObject value at the specified index.
    /// </summary>
    /// <param name="index">The index of the element to return.</param>
    /// <returns>The GSObject value, or null if the element is null.</returns>
    public GSObject? GetObject(int index)
    {
        var obj = _array[index];
        return obj as GSObject;
    }

    /// <summary>
    /// Returns the GSObject value at the specified index, or the default value if out of range.
    /// </summary>
    /// <param name="index">The index of the element to return.</param>
    /// <param name="defaultValue">The default value to return if index is out of range.</param>
    /// <returns>The GSObject value, or the default value.</returns>
    public GSObject? GetObject(int index, GSObject? defaultValue)
    {
        if (index < 0 || index >= _array.Count)
            return defaultValue;
        var obj = _array[index];
        return obj as GSObject ?? defaultValue;
    }

    /// <summary>
    /// Returns the typed object value at the specified index.
    /// </summary>
    /// <typeparam name="T">The type to cast to.</typeparam>
    /// <param name="index">The index of the element to return.</param>
    /// <returns>The typed object.</returns>
    public T? GetObject<T>(int index) where T : class, new()
    {
        var obj = GetObject(index);
        return obj?.Cast<T>();
    }

    /// <summary>
    /// Returns the GSArray value at the specified index.
    /// </summary>
    /// <param name="index">The index of the element to return.</param>
    /// <returns>The GSArray value, or null if the element is null.</returns>
    public GSArray? GetArray(int index)
    {
        var obj = _array[index];
        return obj as GSArray;
    }

    /// <summary>
    /// Returns the GSArray value at the specified index, or the default value if out of range.
    /// </summary>
    /// <param name="index">The index of the element to return.</param>
    /// <param name="defaultValue">The default value to return if index is out of range.</param>
    /// <returns>The GSArray value, or the default value.</returns>
    public GSArray? GetArray(int index, GSArray? defaultValue)
    {
        if (index < 0 || index >= _array.Count)
            return defaultValue;
        var obj = _array[index];
        return obj as GSArray ?? defaultValue;
    }

    /// <summary>
    /// Returns the typed array at the specified index.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="index">The index of the element to return.</param>
    /// <returns>An enumerable of typed objects.</returns>
    public IEnumerable<T> GetArray<T>(int index) where T : class, new()
    {
        var array = GetArray(index);
        return array?.Cast<T>() ?? Enumerable.Empty<T>();
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the number of elements in the array.
    /// </summary>
    public int Length => _array.Count;

    /// <summary>
    /// Gets the number of elements in the array.
    /// </summary>
    public int Count => _array.Count;

    /// <summary>
    /// Gets or sets the element at the specified index.
    /// </summary>
    /// <param name="i">The index of the element.</param>
    /// <returns>The element at the specified index.</returns>
    public object? this[int i]
    {
        get => _array[i];
        set => _array[i] = value;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Returns an enumerator that iterates through the array.
    /// </summary>
    public IEnumerator GetEnumerator()
    {
        for (int i = 0; i < _array.Count; i++)
        {
            yield return _array[i];
        }
    }

    /// <summary>
    /// Returns the array's content as a JSON string.
    /// </summary>
    public override string ToString()
    {
        return ToJsonString();
    }

    /// <summary>
    /// Returns the array's content as a JSON string.
    /// </summary>
    public string ToJsonString()
    {
        var arr = ToSerializableArray();
        return JsonSerializer.Serialize(arr);
    }

    /// <summary>
    /// Casts all elements to the specified type.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <returns>An enumerable of typed objects.</returns>
    public IEnumerable<T> Cast<T>() where T : class, new()
    {
        foreach (var item in this)
        {
            if (item is GSObject gsObj)
                yield return gsObj.Cast<T>();
            else if (item is T typedItem)
                yield return typedItem;
        }
    }

    #endregion

    #region Internal Methods

    internal object?[] ToSerializableArray()
    {
        var result = new List<object?>();
        foreach (var item in _array)
        {
            result.Add(item switch
            {
                GSObject gsObj => gsObj.ToSerializableDictionary(),
                GSArray gsArr => gsArr.ToSerializableArray(),
                _ => item
            });
        }
        return result.ToArray();
    }

    internal IEnumerable<T> GetInternal<T>(string[] path, int pos, bool attemptConversion)
    {
        var key = path[pos];

        // The key is an index; otherwise ignore
        if (key.Length >= 2 && key[0] == '[' && key[^1] == ']')
        {
            var indexStr = key[1..^1];

            // The key refers to all items ("[*]")
            if (indexStr == "*")
            {
                foreach (var item in _array)
                {
                    foreach (var value in GSObject.GetFromValue<T>(path, pos, item, attemptConversion))
                        yield return value;
                }
            }
            // The key is a specific index (e.g. "[2]") and within the array bounds
            else if (int.TryParse(indexStr, out var index) && index < Length)
            {
                foreach (var value in GSObject.GetFromValue<T>(path, pos, _array[index], attemptConversion))
                    yield return value;
            }
        }
    }

    internal IEnumerable CastInternal(Type memberType)
    {
        var list = (IList)Activator.CreateInstance(memberType)!;
        var genericArgs = memberType.GetGenericArguments();
        var elementType = genericArgs.Length > 0 ? genericArgs[0] : typeof(object);

        foreach (var item in this)
        {
            object? itemToAdd;
            
            if (item is GSObject gsObj)
            {
                itemToAdd = gsObj.Cast(elementType);
            }
            else if (item is GSArray gsArr)
            {
                itemToAdd = gsArr.CastInternal(elementType);
            }
            else if (item != null && elementType != typeof(object))
            {
                try
                {
                    itemToAdd = Convert.ChangeType(item, elementType);
                }
                catch
                {
                    itemToAdd = item;
                }
            }
            else
            {
                itemToAdd = item;
            }

            list.Add(itemToAdd);
        }

        return list;
    }

    #endregion
}