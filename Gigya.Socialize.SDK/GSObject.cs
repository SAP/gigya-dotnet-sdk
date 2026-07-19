/*
 * Copyright (C) 2024 SAP SE
 * Modern .NET 9 SDK - GSObject
 */

using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Web;

namespace Gigya.Socialize.SDK;

/// <summary>  
/// Used for passing parameters when issuing requests e.g. GSRequest.Send
/// As well as returning response data e.g. GSResponse.GetData
/// The dictionary can hold the following types: string, boolean, int, long, Array of GSObjects, GSObject    
/// </summary>
[Serializable]
public class GSObject
{
    // Using StringComparer.Ordinal to ensure alphabetic order of keys
    // (Important when calculating base string for OAuth1 signatures)
    private readonly SortedDictionary<string, object?> _map = new(StringComparer.Ordinal);
    private static readonly Dictionary<Type, List<MemberInfo>> TypeCache = new();
    private static readonly object TypeCacheLock = new();

    #region Constructors

    /// <summary>
    /// Default constructor.
    /// </summary>
    public GSObject()
    {
    }

    /// <summary>
    /// Construct a GSObject from a JSON string, anonymous type, or any other class via JSON serialization.
    /// </summary>
    /// <param name="obj">The source object (JSON string or object to serialize).</param>
    public GSObject(object obj)
    {
        if (obj is string jsonString)
        {
            ConstructFromJsonString(jsonString);
        }
        else if (obj != null)
        {
            ConstructFromTypedClass(obj);
        }
    }

    /// <summary>
    /// Construct a GSObject from a JSON string.
    /// </summary>
    /// <param name="json">The JSON formatted string.</param>
    /// <exception cref="JsonException">Thrown if unable to parse JSON.</exception>
    public GSObject(string json)
    {
        ConstructFromJsonString(json);
    }

    private void ConstructFromJsonString(string json)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, options);
        
        if (dict == null) return;

        foreach (var kvp in dict)
        {
            _map[kvp.Key] = ConvertJsonElement(kvp.Value);
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
        var obj = new GSObject();
        foreach (var prop in element.EnumerateObject())
        {
            obj._map[prop.Name] = ConvertJsonElement(prop.Value);
        }
        return obj;
    }

    private static GSArray CreateGSArrayFromElement(JsonElement element)
    {
        var arr = new GSArray();
        foreach (var item in element.EnumerateArray())
        {
            arr.AddInternal(ConvertJsonElement(item));
        }
        return arr;
    }

    private void ConstructFromTypedClass(object obj)
    {
        var type = obj.GetType();
        
        // Special handling for Dictionary<string, object> and similar types
        if (obj is System.Collections.IDictionary dict)
        {
            foreach (System.Collections.DictionaryEntry entry in dict)
            {
                var key = entry.Key?.ToString();
                if (key == null) continue;
                
                var value = entry.Value;
                if (value == null)
                {
                    _map[key] = null;
                }
                else if (value.GetType().IsPrimitive || value is string)
                {
                    _map[key] = value;
                }
                else if (value is IEnumerable enumerable and not string and not System.Collections.IDictionary)
                {
                    var gsArr = new GSArray();
                    foreach (var item in enumerable)
                    {
                        if (item == null)
                        {
                            gsArr.AddInternal(null);
                        }
                        else if (item.GetType().IsPrimitive || item is string)
                        {
                            gsArr.AddInternal(item);
                        }
                        else
                        {
                            gsArr.Add(new GSObject(item));
                        }
                    }
                    _map[key] = gsArr;
                }
                else if (value.GetType().IsClass)
                {
                    _map[key] = new GSObject(value);
                }
            }
            return;
        }
        
        var members = GetTypeMembers(type);

        foreach (var member in members)
        {
            var value = GetMemberValue(obj, member);
            var memberName = member.Name;

            if (value == null)
            {
                _map[memberName] = null;
            }
            else if (value.GetType().IsPrimitive || value is string)
            {
                _map[memberName] = value;
            }
            else if (value is IEnumerable enumerable and not string)
            {
                var gsArr = new GSArray();
                foreach (var item in enumerable)
                {
                    if (item == null)
                    {
                        gsArr.AddInternal(null);
                    }
                    else if (item.GetType().IsPrimitive || item is string)
                    {
                        gsArr.AddInternal(item);
                    }
                    else
                    {
                        gsArr.Add(new GSObject(item));
                    }
                }
                _map[memberName] = gsArr;
            }
            else if (value.GetType().IsClass)
            {
                _map[memberName] = new GSObject(value);
            }
        }
    }

    #endregion

    #region Put Methods

    /// <summary>
    /// Associates the specified value with the specified key in this dictionary. 
    /// If the dictionary previously contained a mapping for the key, the old value is replaced by the specified value.
    /// </summary>
    /// <param name="key">Key with which the specified value is to be associated.</param>
    /// <param name="value">A string value to be associated with the specified key.</param>
    /// <returns>This GSObject for method chaining.</returns>
    public GSObject Put(string key, string? value)
    {
        if (key != null)
            _map[key] = value;
        return this;
    }

    /// <summary>
    /// Associates the specified value with the specified key in this dictionary. 
    /// If the dictionary previously contained a mapping for the key, the old value is replaced by the specified value.
    /// </summary>
    /// <param name="key">Key with which the specified value is to be associated.</param>
    /// <param name="value">An int value to be associated with the specified key.</param>
    /// <returns>This GSObject for method chaining.</returns>
    public GSObject Put(string key, int value)
    {
        if (key != null)
            _map[key] = value;
        return this;
    }

    /// <summary>
    /// Associates the specified value with the specified key in this dictionary. 
    /// If the dictionary previously contained a mapping for the key, the old value is replaced by the specified value.
    /// </summary>
    /// <param name="key">Key with which the specified value is to be associated.</param>
    /// <param name="value">A long value to be associated with the specified key.</param>
    /// <returns>This GSObject for method chaining.</returns>
    public GSObject Put(string key, long value)
    {
        if (key != null)
            _map[key] = value;
        return this;
    }

    /// <summary>
    /// Associates the specified value with the specified key in this dictionary. 
    /// If the dictionary previously contained a mapping for the key, the old value is replaced by the specified value.
    /// </summary>
    /// <param name="key">Key with which the specified value is to be associated.</param>
    /// <param name="value">A bool value to be associated with the specified key.</param>
    /// <returns>This GSObject for method chaining.</returns>
    public GSObject Put(string key, bool value)
    {
        if (key != null)
            _map[key] = value;
        return this;
    }

    /// <summary>
    /// Associates the specified value with the specified key in this dictionary. 
    /// If the dictionary previously contained a mapping for the key, the old value is replaced by the specified value.
    /// </summary>
    /// <param name="key">Key with which the specified value is to be associated.</param>
    /// <param name="value">A double value to be associated with the specified key.</param>
    /// <returns>This GSObject for method chaining.</returns>
    public GSObject Put(string key, double value)
    {
        if (key != null)
            _map[key] = value;
        return this;
    }

    /// <summary>
    /// Associates the specified value with the specified key in this dictionary. 
    /// If the dictionary previously contained a mapping for the key, the old value is replaced by the specified value.
    /// </summary>
    /// <param name="key">Key with which the specified value is to be associated.</param>
    /// <param name="value">A GSObject value to be associated with the specified key.</param>
    /// <returns>This GSObject for method chaining.</returns>
    public GSObject Put(string key, GSObject? value)
    {
        if (key != null)
            _map[key] = value;
        return this;
    }

    /// <summary>
    /// Associates the specified value with the specified key in this dictionary. 
    /// If the dictionary previously contained a mapping for the key, the old value is replaced by the specified value.
    /// </summary>
    /// <param name="key">Key with which the specified value is to be associated.</param>
    /// <param name="value">A GSArray value to be associated with the specified key.</param>
    /// <returns>This GSObject for method chaining.</returns>
    public GSObject Put(string key, GSArray? value)
    {
        if (key != null)
            _map[key] = value;
        return this;
    }

    /// <summary>
    /// Internal method to put any object value.
    /// </summary>
    internal void PutInternal(string key, object? value)
    {
        if (key != null)
            _map[key] = value;
    }

    #endregion

    #region Get Methods (with default)

    /// <summary>
    /// Returns the bool value to which the specified key is mapped, or the 
    /// defaultValue if this dictionary contains no mapping for the key.
    /// </summary>
    /// <param name="key">The key whose associated value is to be returned.</param>
    /// <param name="defaultValue">The bool value to be returned if this dictionary doesn't contain the specified key.</param>
    /// <returns>The bool value to which the specified key is mapped, or the defaultValue if 
    /// this dictionary contains no mapping for the key.</returns>
    public bool GetBool(string key, bool defaultValue)
    {
        try { return GetTypedValue<bool>(key, defaultValue, true); }
        catch { return defaultValue; }
    }

    /// <summary>
    /// Returns the nullable bool value to which the specified key is mapped, or the 
    /// defaultValue if this dictionary contains no mapping for the key.
    /// </summary>
    /// <param name="key">The key whose associated value is to be returned.</param>
    /// <param name="defaultValue">The bool value to be returned if this dictionary doesn't contain the specified key.</param>
    /// <returns>The bool value to which the specified key is mapped, or the defaultValue if 
    /// this dictionary contains no mapping for the key.</returns>
    public bool? GetBool(string key, bool? defaultValue)
    {
        try { return GetTypedValue<bool?>(key, defaultValue, true); }
        catch { return defaultValue; }
    }

    /// <summary>
    /// Returns the int value to which the specified key is mapped, or the 
    /// defaultValue if this dictionary contains no mapping for the key.
    /// </summary>
    /// <param name="key">The key whose associated value is to be returned.</param>
    /// <param name="defaultValue">The int value to be returned if this dictionary doesn't contain the specified key.</param>
    /// <returns>The int value to which the specified key is mapped, or the defaultValue if 
    /// this dictionary contains no mapping for the key.</returns>
    public int GetInt(string key, int defaultValue)
    {
        try { return GetTypedValue<int>(key, defaultValue, true); }
        catch { return defaultValue; }
    }

    /// <summary>
    /// Returns the nullable int value to which the specified key is mapped, or the 
    /// defaultValue if this dictionary contains no mapping for the key.
    /// </summary>
    /// <param name="key">The key whose associated value is to be returned.</param>
    /// <param name="defaultValue">The int value to be returned if this dictionary doesn't contain the specified key.</param>
    /// <returns>The int value to which the specified key is mapped, or the defaultValue if 
    /// this dictionary contains no mapping for the key.</returns>
    public int? GetInt(string key, int? defaultValue)
    {
        try { return GetTypedValue<int?>(key, defaultValue, true); }
        catch { return defaultValue; }
    }

    /// <summary>
    /// Returns the long value to which the specified key is mapped, or the defaultValue
    /// if this dictionary contains no mapping for the key.
    /// </summary>
    /// <param name="key">The key whose associated value is to be returned.</param>
    /// <param name="defaultValue">The long value to be returned if this dictionary doesn't contain the specified key.</param>
    /// <returns>The long value to which the specified key is mapped, or the defaultValue if this 
    /// dictionary contains no mapping for the key.</returns>
    public long GetLong(string key, long defaultValue)
    {
        try { return GetTypedValue<long>(key, defaultValue, true); }
        catch { return defaultValue; }
    }

    /// <summary>
    /// Returns the nullable long value to which the specified key is mapped, or the defaultValue
    /// if this dictionary contains no mapping for the key.
    /// </summary>
    /// <param name="key">The key whose associated value is to be returned.</param>
    /// <param name="defaultValue">The long value to be returned if this dictionary doesn't contain the specified key.</param>
    /// <returns>The long value to which the specified key is mapped, or the defaultValue if this 
    /// dictionary contains no mapping for the key.</returns>
    public long? GetLong(string key, long? defaultValue)
    {
        try { return GetTypedValue<long?>(key, defaultValue, true); }
        catch { return defaultValue; }
    }

    /// <summary>
    /// Returns the double value to which the specified key is mapped, or the defaultValue
    /// if this dictionary contains no mapping for the key.
    /// </summary>
    /// <param name="key">The key whose associated value is to be returned.</param>
    /// <param name="defaultValue">The double value to be returned if this dictionary doesn't contain the specified key.</param>
    /// <returns>The double value to which the specified key is mapped, or the defaultValue if this 
    /// dictionary contains no mapping for the key.</returns>
    public double GetDouble(string key, double defaultValue)
    {
        try { return GetTypedValue<double>(key, defaultValue, true); }
        catch { return defaultValue; }
    }

    /// <summary>
    /// Returns the nullable double value to which the specified key is mapped, or the defaultValue
    /// if this dictionary contains no mapping for the key.
    /// </summary>
    /// <param name="key">The key whose associated value is to be returned.</param>
    /// <param name="defaultValue">The double value to be returned if this dictionary doesn't contain the specified key.</param>
    /// <returns>The double value to which the specified key is mapped, or the defaultValue if this 
    /// dictionary contains no mapping for the key.</returns>
    public double? GetDouble(string key, double? defaultValue)
    {
        try { return GetTypedValue<double?>(key, defaultValue, true); }
        catch { return defaultValue; }
    }

    /// <summary>
    /// Returns the string value to which the specified key is mapped, or the defaultValue
    /// if this dictionary contains no mapping for the key.
    /// </summary>
    /// <param name="key">The key whose associated value is to be returned.</param>
    /// <param name="defaultValue">The string value to be returned if this dictionary doesn't contain the specified key.</param>
    /// <returns>The string value to which the specified key is mapped, or the defaultValue if this 
    /// dictionary contains no mapping for the key.</returns>
    public string? GetString(string key, string? defaultValue)
    {
        try { return GetTypedValue<string?>(key, defaultValue, true); }
        catch { return defaultValue; }
    }

    /// <summary>
    /// Returns the GSObject value to which the specified key is mapped, or the defaultValue
    /// if this dictionary contains no mapping for the key.
    /// </summary>
    /// <param name="key">The key whose associated value is to be returned.</param>
    /// <param name="defaultValue">The GSObject value to be returned if this dictionary doesn't contain the specified key.</param>
    /// <returns>The GSObject value to which the specified key is mapped, or the defaultValue if this 
    /// dictionary contains no mapping for the key.</returns>
    public GSObject? GetObject(string key, GSObject? defaultValue)
    {
        try { return GetTypedValue<GSObject?>(key, defaultValue, true); }
        catch { return defaultValue; }
    }

    /// <summary>
    /// Returns the GSArray value to which the specified key is mapped, or the defaultValue
    /// if this dictionary contains no mapping for the key.
    /// </summary>
    /// <param name="key">The key whose associated value is to be returned.</param>
    /// <param name="defaultValue">The GSArray value to be returned if this dictionary doesn't contain the specified key.</param>
    /// <returns>The GSArray value to which the specified key is mapped, or the defaultValue if this 
    /// dictionary contains no mapping for the key.</returns>
    public GSArray? GetArray(string key, GSArray? defaultValue)
    {
        try { return GetTypedValue<GSArray?>(key, defaultValue, true); }
        catch { return defaultValue; }
    }

    #endregion

    #region Get Methods (throwing)

    /// <summary>
    /// Returns the bool value to which the specified key is mapped. 
    /// </summary>
    /// <param name="key">The key whose associated value is to be returned.</param>
    /// <returns>The bool value to which the specified key is mapped.</returns>
    /// <exception cref="GSKeyNotFoundException">Thrown if the key is not found.</exception>
    /// <exception cref="FormatException">Thrown if the value cannot be parsed as bool.</exception>
    public bool GetBool(string key) => GetTypedValue<bool>(key, default, false);

    /// <summary>
    /// Returns the int value to which the specified key is mapped. 
    /// </summary>
    /// <param name="key">The key whose associated value is to be returned.</param>
    /// <returns>The int value to which the specified key is mapped.</returns>
    /// <exception cref="GSKeyNotFoundException">Thrown if the key is not found.</exception>
    /// <exception cref="FormatException">Thrown if the value cannot be parsed as int.</exception>
    public int GetInt(string key) => GetTypedValue<int>(key, default, false);

    /// <summary>
    /// Returns the long value to which the specified key is mapped. 
    /// </summary>
    /// <param name="key">The key whose associated value is to be returned.</param>
    /// <returns>The long value to which the specified key is mapped.</returns>
    /// <exception cref="GSKeyNotFoundException">Thrown if the key is not found.</exception>
    /// <exception cref="FormatException">Thrown if the value cannot be parsed as long.</exception>
    public long GetLong(string key) => GetTypedValue<long>(key, default, false);

    /// <summary>
    /// Returns the double value to which the specified key is mapped. 
    /// </summary>
    /// <param name="key">The key whose associated value is to be returned.</param>
    /// <returns>The double value to which the specified key is mapped.</returns>
    /// <exception cref="GSKeyNotFoundException">Thrown if the key is not found.</exception>
    /// <exception cref="FormatException">Thrown if the value cannot be parsed as double.</exception>
    public double GetDouble(string key) => GetTypedValue<double>(key, default, false);

    /// <summary>
    /// Returns the string value to which the specified key is mapped. 
    /// </summary>
    /// <param name="key">The key whose associated value is to be returned.</param>
    /// <returns>The string value to which the specified key is mapped.</returns>
    /// <exception cref="GSKeyNotFoundException">Thrown if the key is not found.</exception>
    public string GetString(string key) => GetTypedValue<string>(key, null!, false)!;

    /// <summary>
    /// Returns the GSObject value to which the specified key is mapped. 
    /// </summary>
    /// <param name="key">The key whose associated value is to be returned.</param>
    /// <returns>The GSObject value to which the specified key is mapped.</returns>
    /// <exception cref="GSKeyNotFoundException">Thrown if the key is not found.</exception>
    /// <exception cref="InvalidCastException">Thrown if the value cannot be cast to GSObject.</exception>
    public GSObject GetObject(string key) => GetTypedValue<GSObject>(key, null!, false)!;

    /// <summary>
    /// Returns the GSArray value to which the specified key is mapped. 
    /// </summary>
    /// <param name="key">The key whose associated value is to be returned.</param>
    /// <returns>The GSArray value to which the specified key is mapped.</returns>
    /// <exception cref="GSKeyNotFoundException">Thrown if the key is not found.</exception>
    /// <exception cref="InvalidCastException">Thrown if the value cannot be cast to GSArray.</exception>
    public GSArray GetArray(string key) => GetTypedValue<GSArray>(key, null!, false)!;

    #endregion

    #region Generic Get Methods

    /// <summary>
    /// Returns the typed object value to which the specified key is mapped.
    /// </summary>
    /// <typeparam name="T">The type to cast to.</typeparam>
    /// <param name="key">The key whose associated value is to be returned.</param>
    /// <returns>The typed object.</returns>
    public T? GetObject<T>(string key) where T : class, new()
    {
        var obj = GetObject(key, null);
        return obj?.Cast<T>();
    }

    /// <summary>
    /// Returns the typed array to which the specified key is mapped.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="key">The key whose associated value is to be returned.</param>
    /// <returns>An enumerable of typed objects.</returns>
    public IEnumerable<T> GetArray<T>(string key) where T : class, new()
    {
        var array = GetArray(key);
        return array.Cast<T>();
    }

    /// <summary>
    /// Gets one or more nested objects using a path expression.
    /// </summary>
    /// <typeparam name="T">The type of the object(s) to obtain.</typeparam>
    /// <param name="path">A dot-delimited path down the objects hierarchy.</param>
    /// <param name="attemptConversion">If true, attempt type conversions.</param>
    /// <returns>A list of values which match the path.</returns>
    public IEnumerable<T> Get<T>(string path, bool attemptConversion = false)
    {
        var tokens = TokenizePath(path);
        return GetInternal<T>(tokens, 0, attemptConversion);
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Returns true if this dictionary contains a mapping for the specified key.
    /// </summary>
    /// <param name="key">Key whose presence in this map is to be tested.</param>
    /// <returns>True if this map contains a mapping for the specified key.</returns>
    public bool ContainsKey(string key) => _map.ContainsKey(key);

    /// <summary>
    /// Parse parameters from URL into the dictionary.
    /// </summary>
    /// <param name="url">The URL string to parse.</param>
    public void ParseURL(string url)
    {
        try
        {
            var u = new Uri(url);
            ParseQuerystring(u.Query);
            ParseQuerystring(u.Fragment);
        }
        catch (UriFormatException)
        {
            // Ignore invalid URLs
        }
    }

    /// <summary>
    /// Parse parameters from query string.
    /// </summary>
    /// <param name="qs">The query string to parse.</param>
    public void ParseQuerystring(string? qs)
    {
        if (string.IsNullOrEmpty(qs)) return;

        if (qs.StartsWith("?")) qs = qs[1..];
        if (qs.StartsWith("#")) qs = qs[1..];

        var pairs = qs.Split('&');
        foreach (var parameter in pairs)
        {
            var indexOf = parameter.IndexOf('=');
            if (indexOf == -1) continue;
            
            var key = parameter[..indexOf];
            var value = parameter[(indexOf + 1)..];
            
            try
            {
                Put(key, HttpUtility.UrlDecode(value, Encoding.UTF8));
            }
            catch
            {
                // Ignore parsing errors
            }
        }
    }

    /// <summary>
    /// Removes the key (and its corresponding value) from this dictionary. 
    /// This method does nothing if the key is not in this dictionary.  
    /// </summary>
    /// <param name="key">The key that needs to be removed.</param>
    public void Remove(string key) => _map.Remove(key);

    /// <summary>
    /// Removes all of the entries from this dictionary. The dictionary will be empty after this call returns. 
    /// </summary>
    public void Clear() => _map.Clear();

    /// <summary>
    /// Returns a String array containing the keys in this dictionary. 
    /// </summary>
    /// <returns>A KeyCollection of the keys in this dictionary.</returns>
    public SortedDictionary<string, object?>.KeyCollection GetKeys() => _map.Keys;

    /// <summary>
    /// Gets or sets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the value to get or set.</param>
    /// <returns>The value associated with the specified key.</returns>
    public object? this[string key]
    {
        get => _map.TryGetValue(key, out var value) ? value : null;
        set => _map[key] = value;
    }

    /// <summary>
    /// Returns the dictionary's content as a JSON string. 
    /// </summary>
    /// <returns>The dictionary's content as a JSON string.</returns>
    public override string ToString() => ToJsonString();

    /// <summary>
    /// Returns the dictionary's content as a JSON string. 
    /// </summary>
    /// <returns>The dictionary's content as a JSON string.</returns>
    public string ToJsonString()
    {
        var dict = ToSerializableDictionary();
        return JsonSerializer.Serialize(dict);
    }

    /// <summary>
    /// Returns a deep clone of the current instance.
    /// </summary>
    /// <returns>A deep clone of this GSObject.</returns>
    public GSObject Clone()
    {
        var json = ToJsonString();
        return new GSObject(json);
    }

    /// <summary>
    /// Casts this GSObject to a typed object.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <returns>A new instance of T populated with data from this GSObject.</returns>
    public T Cast<T>() where T : class, new()
    {
        return (T)Cast(typeof(T));
    }

    #endregion

    #region Internal Methods

    internal object Cast(Type requestedType)
    {
        var instance = Activator.CreateInstance(requestedType)!;
        var members = GetTypeMembers(requestedType);

        foreach (var member in members)
        {
            _map.TryGetValue(member.Name, out var value);
            var memberType = GetMemberType(member);

            if (value == null)
            {
                SetMemberValue(instance, member, null);
                continue;
            }

            object? convertedValue = null;
            
            if (memberType.IsValueType || memberType == typeof(string))
            {
                var underlyingType = Nullable.GetUnderlyingType(memberType);
                var targetType = underlyingType ?? memberType;

                if (value.GetType() == targetType)
                {
                    convertedValue = value;
                }
                else if (value is IConvertible)
                {
                    try
                    {
                        convertedValue = Convert.ChangeType(value, targetType);
                    }
                    catch
                    {
                        // Keep null
                    }
                }
            }
            else if (value is GSObject gsObj)
            {
                convertedValue = gsObj.Cast(memberType);
            }
            else if (value is GSArray gsArr && memberType.GetInterfaces().Contains(typeof(IList)))
            {
                convertedValue = gsArr.CastInternal(memberType);
            }

            if (convertedValue != null)
            {
                SetMemberValue(instance, member, convertedValue);
            }
        }

        return instance;
    }

    internal IEnumerable<T> GetInternal<T>(string[] path, int pos, bool attemptConversion)
    {
        if (!_map.TryGetValue(path[pos], out var value))
            yield break;

        foreach (var result in GetFromValue<T>(path, pos, value, attemptConversion))
            yield return result;
    }

    internal static IEnumerable<T> GetFromValue<T>(string[] path, int pos, object? value, bool attemptConversion)
    {
        // End of the path -- return a value
        if (pos == path.Length - 1)
        {
            if (value is T typedValue)
            {
                yield return typedValue;
            }
            else if (value == null && default(T) == null)
            {
                yield return default!;
            }
            else if (attemptConversion && typeof(T) == typeof(string))
            {
                yield return (T)(object)value!.ToString()!;
            }
            else if (attemptConversion && value is IConvertible)
            {
                var baseType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
                if (baseType.GetInterfaces().Contains(typeof(IConvertible)))
                {
                    object? converted = null;
                    try { converted = Convert.ChangeType(value, baseType); }
                    catch { /* ignore */ }
                    if (converted != null)
                        yield return (T)converted;
                }
            }
        }
        else if (value is GSObject gsObj)
        {
            foreach (var result in gsObj.GetInternal<T>(path, pos + 1, attemptConversion))
                yield return result;
        }
        else if (value is GSArray gsArr)
        {
            foreach (var result in gsArr.GetInternal<T>(path, pos + 1, attemptConversion))
                yield return result;
        }
    }

    internal SortedDictionary<string, object?> ToSerializableDictionary()
    {
        var result = new SortedDictionary<string, object?>();
        foreach (var kvp in _map)
        {
            result[kvp.Key] = kvp.Value switch
            {
                GSObject gsObj => gsObj.ToSerializableDictionary(),
                GSArray gsArr => gsArr.ToSerializableArray(),
                _ => kvp.Value
            };
        }
        return result;
    }

    private T GetTypedValue<T>(string key, T defaultValue, bool useDefault)
    {
        if (_map.TryGetValue(key, out var val))
        {
            if (val == null) return (T)(object)null!;

            var targetType = typeof(T);
            
            if (targetType == typeof(string))
                return (T)(object)val.ToString()!;

            if (val.GetType() == targetType)
                return (T)val;

            if (val is string str)
            {
                var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
                
                // Use InvariantCulture for all numeric parsing to ensure consistent behavior across locales
                if (underlyingType == typeof(int)) return (T)(object)int.Parse(str, CultureInfo.InvariantCulture);
                if (underlyingType == typeof(long)) return (T)(object)long.Parse(str, CultureInfo.InvariantCulture);
                if (underlyingType == typeof(bool)) return (T)(object)bool.Parse(str);
                if (underlyingType == typeof(double)) return (T)(object)double.Parse(str, CultureInfo.InvariantCulture);
                if (underlyingType == typeof(decimal)) return (T)(object)decimal.Parse(str, CultureInfo.InvariantCulture);
            }

            return (T)val;
        }

        if (useDefault)
            return defaultValue;

        throw new GSKeyNotFoundException($"GSObject does not contain a value for key {key}");
    }

    private static string[] TokenizePath(string path)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();
        
        for (int i = 0; i < path.Length; i++)
        {
            char c = path[i];
            
            if (c == '.')
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
            }
            else if (c == '[')
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
                
                // Find the closing bracket
                int end = path.IndexOf(']', i);
                if (end > i)
                {
                    tokens.Add(path[i..(end + 1)]);
                    i = end;
                }
            }
            else
            {
                current.Append(c);
            }
        }
        
        if (current.Length > 0)
            tokens.Add(current.ToString());
        
        return tokens.ToArray();
    }

    #endregion

    #region Reflection Helpers

    private static List<MemberInfo> GetTypeMembers(Type type)
    {
        // Use proper locking to avoid race conditions
        lock (TypeCacheLock)
        {
            if (TypeCache.TryGetValue(type, out var cached))
                return cached;

            var members = new List<MemberInfo>();
            var isAnonymous = IsAnonymousType(type);

            if (isAnonymous)
            {
                members.AddRange(type.GetProperties().Cast<MemberInfo>());
            }
            else
            {
                var ignoreAttrType = typeof(IgnoreAttribute);
                
                var properties = type.GetProperties()
                    .Where(x => !x.IsDefined(ignoreAttrType, true))
                    .Where(x => x.CanRead && x.CanWrite);
                
                var fields = type.GetFields()
                    .Where(x => !x.IsDefined(ignoreAttrType, true));

                members.AddRange(fields.Cast<MemberInfo>());
                members.AddRange(properties.Cast<MemberInfo>());
            }

            TypeCache[type] = members;
            return members;
        }
    }

    private static bool IsAnonymousType(Type type)
    {
        return type.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Length > 0
               && type.FullName?.Contains("AnonymousType") == true;
    }

    private static object? GetMemberValue(object instance, MemberInfo member)
    {
        return member switch
        {
            FieldInfo field => field.GetValue(instance),
            PropertyInfo prop => prop.GetValue(instance),
            _ => null
        };
    }

    private static void SetMemberValue(object instance, MemberInfo member, object? value)
    {
        switch (member)
        {
            case FieldInfo field:
                field.SetValue(instance, value);
                break;
            case PropertyInfo prop:
                prop.SetValue(instance, value);
                break;
        }
    }

    private static Type GetMemberType(MemberInfo member)
    {
        return member switch
        {
            FieldInfo field => field.FieldType,
            PropertyInfo prop => prop.PropertyType,
            _ => typeof(object)
        };
    }

    #endregion

    #region Nested Types

    /// <summary>
    /// Attribute to mark properties or fields that should be ignored during serialization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class IgnoreAttribute : Attribute
    {
    }

    #endregion
}