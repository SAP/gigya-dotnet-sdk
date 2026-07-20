/*
 * Copyright (C) 2024 SAP SE
 * Characterization tests for GSObject
 */

using Xunit;

namespace Gigya.Socialize.SDK.Tests;

/// <summary>
/// Characterization tests for GSObject to verify behavior matches legacy SDK.
/// </summary>
public class GSObjectTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_Default_CreatesEmptyObject()
    {
        var obj = new GSObject();
        
        Assert.Empty(obj.GetKeys());
    }

    [Fact]
    public void Constructor_FromJson_ParsesCorrectly()
    {
        var json = "{\"name\":\"John\",\"age\":30,\"active\":true}";
        var obj = new GSObject(json);
        
        Assert.Equal("John", obj.GetString("name", null));
        Assert.Equal(30, obj.GetInt("age", 0));
        Assert.True(obj.GetBool("active", false));
    }

    [Fact]
    public void Constructor_FromAnonymousObject_ParsesCorrectly()
    {
        var obj = new GSObject(new { name = "John", age = 30, active = true });
        
        Assert.Equal("John", obj.GetString("name", null));
        Assert.Equal(30, obj.GetInt("age", 0));
        Assert.True(obj.GetBool("active", false));
    }

    [Fact]
    public void Constructor_FromDictionary_ParsesCorrectly()
    {
        var dict = new Dictionary<string, object>
        {
            { "name", "John" },
            { "age", 30 },
            { "active", true }
        };
        var obj = new GSObject(dict);
        
        Assert.Equal("John", obj.GetString("name", null));
        Assert.Equal(30, obj.GetInt("age", 0));
        Assert.True(obj.GetBool("active", false));
    }

    #endregion

    #region Put/Get Tests

    [Fact]
    public void Put_String_CanBeRetrieved()
    {
        var obj = new GSObject();
        obj.Put("key", "value");
        
        Assert.Equal("value", obj.GetString("key", null));
    }

    [Fact]
    public void Put_Int_CanBeRetrieved()
    {
        var obj = new GSObject();
        obj.Put("key", 42);
        
        Assert.Equal(42, obj.GetInt("key", 0));
    }

    [Fact]
    public void Put_Long_CanBeRetrieved()
    {
        var obj = new GSObject();
        obj.Put("key", 9876543210L);
        
        Assert.Equal(9876543210L, obj.GetLong("key", 0));
    }

    [Fact]
    public void Put_Double_CanBeRetrieved()
    {
        var obj = new GSObject();
        obj.Put("key", 3.14159);
        
        Assert.Equal(3.14159, obj.GetDouble("key", 0), 5);
    }

    [Fact]
    public void Put_Bool_CanBeRetrieved()
    {
        var obj = new GSObject();
        obj.Put("key", true);
        
        Assert.True(obj.GetBool("key", false));
    }

    [Fact]
    public void Put_GSObject_CanBeRetrieved()
    {
        var obj = new GSObject();
        var nested = new GSObject();
        nested.Put("inner", "value");
        obj.Put("key", nested);
        
        var retrieved = obj.GetObject("key", null);
        Assert.NotNull(retrieved);
        Assert.Equal("value", retrieved!.GetString("inner", null));
    }

    [Fact]
    public void Put_GSArray_CanBeRetrieved()
    {
        var obj = new GSObject();
        var arr = new GSArray();
        arr.Add("item1");
        arr.Add("item2");
        obj.Put("key", arr);
        
        var retrieved = obj.GetArray("key", null);
        Assert.NotNull(retrieved);
        Assert.Equal(2, retrieved!.Length);
    }

    #endregion

    #region Default Value Tests

    [Fact]
    public void GetString_MissingKey_ReturnsDefault()
    {
        var obj = new GSObject();
        
        Assert.Equal("default", obj.GetString("missing", "default"));
    }

    [Fact]
    public void GetInt_MissingKey_ReturnsDefault()
    {
        var obj = new GSObject();
        
        Assert.Equal(99, obj.GetInt("missing", 99));
    }

    [Fact]
    public void GetLong_MissingKey_ReturnsDefault()
    {
        var obj = new GSObject();
        
        Assert.Equal(999L, obj.GetLong("missing", 999L));
    }

    [Fact]
    public void GetDouble_MissingKey_ReturnsDefault()
    {
        var obj = new GSObject();
        
        Assert.Equal(1.5, obj.GetDouble("missing", 1.5));
    }

    [Fact]
    public void GetBool_MissingKey_ReturnsDefault()
    {
        var obj = new GSObject();
        
        Assert.True(obj.GetBool("missing", true));
    }

    #endregion

    #region ContainsKey/Remove/Clear Tests

    [Fact]
    public void ContainsKey_ExistingKey_ReturnsTrue()
    {
        var obj = new GSObject();
        obj.Put("key", "value");
        
        Assert.True(obj.ContainsKey("key"));
    }

    [Fact]
    public void ContainsKey_MissingKey_ReturnsFalse()
    {
        var obj = new GSObject();
        
        Assert.False(obj.ContainsKey("missing"));
    }

    [Fact]
    public void Remove_ExistingKey_RemovesIt()
    {
        var obj = new GSObject();
        obj.Put("key", "value");
        obj.Remove("key");
        
        Assert.False(obj.ContainsKey("key"));
    }

    [Fact]
    public void Clear_RemovesAllKeys()
    {
        var obj = new GSObject();
        obj.Put("key1", "value1");
        obj.Put("key2", "value2");
        obj.Clear();
        
        Assert.Empty(obj.GetKeys());
    }

    #endregion

    #region Clone Tests

    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        var obj = new GSObject();
        obj.Put("key", "value");
        
        var clone = obj.Clone();
        clone.Put("key", "modified");
        
        Assert.Equal("value", obj.GetString("key", null));
        Assert.Equal("modified", clone.GetString("key", null));
    }

    #endregion

    #region ToJsonString Tests

    [Fact]
    public void ToJsonString_SimpleObject_ProducesValidJson()
    {
        var obj = new GSObject();
        obj.Put("name", "John");
        obj.Put("age", 30);
        
        var json = obj.ToJsonString();
        
        Assert.Contains("\"name\"", json);
        Assert.Contains("\"John\"", json);
        Assert.Contains("\"age\"", json);
        Assert.Contains("30", json);
    }

    [Fact]
    public void ToJsonString_NestedObject_ProducesValidJson()
    {
        var obj = new GSObject();
        var nested = new GSObject();
        nested.Put("inner", "value");
        obj.Put("outer", nested);
        
        var json = obj.ToJsonString();
        
        Assert.Contains("\"outer\"", json);
        Assert.Contains("\"inner\"", json);
        Assert.Contains("\"value\"", json);
    }

    #endregion

    #region Indexer Tests

    [Fact]
    public void Indexer_Get_ReturnsValue()
    {
        var obj = new GSObject();
        obj.Put("key", "value");
        
        Assert.Equal("value", obj["key"]);
    }

    [Fact]
    public void Indexer_Set_SetsValue()
    {
        var obj = new GSObject();
        obj["key"] = "value";
        
        Assert.Equal("value", obj.GetString("key", null));
    }

    #endregion

    #region Type Conversion Tests

    [Fact]
    public void GetInt_FromString_ConvertsCorrectly()
    {
        var obj = new GSObject("{\"value\":\"42\"}");
        
        Assert.Equal(42, obj.GetInt("value", 0));
    }

    [Fact]
    public void GetLong_FromString_ConvertsCorrectly()
    {
        var obj = new GSObject("{\"value\":\"9876543210\"}");
        
        Assert.Equal(9876543210L, obj.GetLong("value", 0));
    }

    [Fact]
    public void GetBool_FromString_ConvertsCorrectly()
    {
        var obj = new GSObject("{\"value\":\"true\"}");
        
        Assert.True(obj.GetBool("value", false));
    }

    #endregion

    #region Path Access Tests

    [Fact]
    public void Get_SimplePath_ReturnsValue()
    {
        var obj = new GSObject("{\"name\":\"John\"}");
        
        var results = obj.Get<string>("name").ToList();
        
        Assert.Single(results);
        Assert.Equal("John", results[0]);
    }

    [Fact]
    public void Get_NestedPath_ReturnsValue()
    {
        var obj = new GSObject("{\"user\":{\"name\":\"John\"}}");
        
        var results = obj.Get<string>("user.name").ToList();
        
        Assert.Single(results);
        Assert.Equal("John", results[0]);
    }

    [Fact]
    public void Get_ArrayPath_ReturnsValue()
    {
        var obj = new GSObject("{\"items\":[\"a\",\"b\",\"c\"]}");
        
        var results = obj.Get<string>("items[1]").ToList();
        
        Assert.Single(results);
        Assert.Equal("b", results[0]);
    }

    [Fact]
    public void Get_ArrayWildcard_ReturnsAllValues()
    {
        var obj = new GSObject("{\"items\":[\"a\",\"b\",\"c\"]}");
        
        var results = obj.Get<string>("items[*]").ToList();
        
        Assert.Equal(3, results.Count);
        Assert.Contains("a", results);
        Assert.Contains("b", results);
        Assert.Contains("c", results);
    }

    #endregion
}