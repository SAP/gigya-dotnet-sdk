/*
 * Copyright (C) 2024 SAP SE
 * Characterization tests for GSArray
 */

using Xunit;

namespace Gigya.Socialize.SDK.Tests;

/// <summary>
/// Characterization tests for GSArray to verify behavior matches legacy SDK.
/// </summary>
public class GSArrayTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_Default_CreatesEmptyArray()
    {
        var arr = new GSArray();
        
        Assert.Equal(0, arr.Length);
    }

    [Fact]
    public void Constructor_FromJson_ParsesCorrectly()
    {
        var json = "[\"a\",\"b\",\"c\"]";
        var arr = new GSArray(json);
        
        Assert.Equal(3, arr.Length);
        Assert.Equal("a", arr.GetString(0, null));
        Assert.Equal("b", arr.GetString(1, null));
        Assert.Equal("c", arr.GetString(2, null));
    }

    [Fact]
    public void Constructor_FromJsonWithMixedTypes_ParsesCorrectly()
    {
        var json = "[\"text\",42,true,3.14]";
        var arr = new GSArray(json);
        
        Assert.Equal(4, arr.Length);
        Assert.Equal("text", arr.GetString(0, null));
        Assert.Equal(42, arr.GetInt(1, 0));
        Assert.True(arr.GetBool(2, false));
        Assert.Equal(3.14, arr.GetDouble(3, 0), 2);
    }

    #endregion

    #region Add Tests

    [Fact]
    public void Add_String_IncreasesLength()
    {
        var arr = new GSArray();
        arr.Add("item");
        
        Assert.Equal(1, arr.Length);
        Assert.Equal("item", arr.GetString(0, null));
    }

    [Fact]
    public void Add_Int_IncreasesLength()
    {
        var arr = new GSArray();
        arr.Add(42);
        
        Assert.Equal(1, arr.Length);
        Assert.Equal(42, arr.GetInt(0, 0));
    }

    [Fact]
    public void Add_Long_IncreasesLength()
    {
        var arr = new GSArray();
        arr.Add(9876543210L);
        
        Assert.Equal(1, arr.Length);
        Assert.Equal(9876543210L, arr.GetLong(0, 0));
    }

    [Fact]
    public void Add_Double_IncreasesLength()
    {
        var arr = new GSArray();
        arr.Add(3.14159);
        
        Assert.Equal(1, arr.Length);
        Assert.Equal(3.14159, arr.GetDouble(0, 0), 5);
    }

    [Fact]
    public void Add_Bool_IncreasesLength()
    {
        var arr = new GSArray();
        arr.Add(true);
        
        Assert.Equal(1, arr.Length);
        Assert.True(arr.GetBool(0, false));
    }

    [Fact]
    public void Add_GSObject_IncreasesLength()
    {
        var arr = new GSArray();
        var obj = new GSObject();
        obj.Put("key", "value");
        arr.Add(obj);
        
        Assert.Equal(1, arr.Length);
        var retrieved = arr.GetObject(0, null);
        Assert.NotNull(retrieved);
        Assert.Equal("value", retrieved!.GetString("key", null));
    }

    [Fact]
    public void Add_GSArray_IncreasesLength()
    {
        var arr = new GSArray();
        var nested = new GSArray();
        nested.Add("nested");
        arr.Add(nested);
        
        Assert.Equal(1, arr.Length);
        var retrieved = arr.GetArray(0, null);
        Assert.NotNull(retrieved);
        Assert.Equal(1, retrieved!.Length);
    }

    [Fact]
    public void Add_MultipleItems_MaintainsOrder()
    {
        var arr = new GSArray();
        arr.Add("first");
        arr.Add("second");
        arr.Add("third");
        
        Assert.Equal(3, arr.Length);
        Assert.Equal("first", arr.GetString(0, null));
        Assert.Equal("second", arr.GetString(1, null));
        Assert.Equal("third", arr.GetString(2, null));
    }

    #endregion

    #region Get Tests with Default

    [Fact]
    public void GetString_OutOfRange_ReturnsDefault()
    {
        var arr = new GSArray();
        
        Assert.Equal("default", arr.GetString(0, "default"));
    }

    [Fact]
    public void GetInt_OutOfRange_ReturnsDefault()
    {
        var arr = new GSArray();
        
        Assert.Equal(99, arr.GetInt(0, 99));
    }

    [Fact]
    public void GetLong_OutOfRange_ReturnsDefault()
    {
        var arr = new GSArray();
        
        Assert.Equal(999L, arr.GetLong(0, 999L));
    }

    [Fact]
    public void GetDouble_OutOfRange_ReturnsDefault()
    {
        var arr = new GSArray();
        
        Assert.Equal(1.5, arr.GetDouble(0, 1.5));
    }

    [Fact]
    public void GetBool_OutOfRange_ReturnsDefault()
    {
        var arr = new GSArray();
        
        Assert.True(arr.GetBool(0, true));
    }

    #endregion

    #region Indexer Tests

    [Fact]
    public void Indexer_Get_ReturnsValue()
    {
        var arr = new GSArray();
        arr.Add("value");
        
        Assert.Equal("value", arr[0]);
    }

    [Fact]
    public void Indexer_Set_SetsValue()
    {
        var arr = new GSArray();
        arr.Add("placeholder");
        arr[0] = "newvalue";
        
        Assert.Equal("newvalue", arr.GetString(0, null));
    }

    #endregion

    #region ToJsonString Tests

    [Fact]
    public void ToJsonString_SimpleArray_ProducesValidJson()
    {
        var arr = new GSArray();
        arr.Add("a");
        arr.Add("b");
        arr.Add("c");
        
        var json = arr.ToJsonString();
        
        Assert.StartsWith("[", json);
        Assert.EndsWith("]", json);
        Assert.Contains("\"a\"", json);
        Assert.Contains("\"b\"", json);
        Assert.Contains("\"c\"", json);
    }

    [Fact]
    public void ToJsonString_MixedTypes_ProducesValidJson()
    {
        var arr = new GSArray();
        arr.Add("text");
        arr.Add(42);
        arr.Add(true);
        
        var json = arr.ToJsonString();
        
        Assert.Contains("\"text\"", json);
        Assert.Contains("42", json);
        Assert.Contains("true", json);
    }

    [Fact]
    public void ToJsonString_NestedObjects_ProducesValidJson()
    {
        var arr = new GSArray();
        var obj = new GSObject();
        obj.Put("key", "value");
        arr.Add(obj);
        
        var json = arr.ToJsonString();
        
        Assert.Contains("\"key\"", json);
        Assert.Contains("\"value\"", json);
    }

    #endregion

    #region Enumerable Tests

    [Fact]
    public void GetEnumerator_IteratesAllItems()
    {
        var arr = new GSArray();
        arr.Add("a");
        arr.Add("b");
        arr.Add("c");
        
        var items = new List<object?>();
        foreach (var item in arr)
        {
            items.Add(item);
        }
        
        Assert.Equal(3, items.Count);
        Assert.Equal("a", items[0]);
        Assert.Equal("b", items[1]);
        Assert.Equal("c", items[2]);
    }

    [Fact]
    public void Linq_WorksCorrectly()
    {
        var arr = new GSArray();
        arr.Add("apple");
        arr.Add("banana");
        arr.Add("cherry");
        
        var filtered = arr.Cast<object?>()
            .Where(x => x?.ToString()?.StartsWith("b") == true)
            .ToList();
        
        Assert.Single(filtered);
        Assert.Equal("banana", filtered[0]);
    }

    #endregion

    #region Type Conversion Tests

    [Fact]
    public void GetInt_FromString_ConvertsCorrectly()
    {
        var arr = new GSArray("[\"42\"]");
        
        Assert.Equal(42, arr.GetInt(0, 0));
    }

    [Fact]
    public void GetLong_FromString_ConvertsCorrectly()
    {
        var arr = new GSArray("[\"9876543210\"]");
        
        Assert.Equal(9876543210L, arr.GetLong(0, 0));
    }

    [Fact]
    public void GetBool_FromString_ConvertsCorrectly()
    {
        var arr = new GSArray("[\"true\"]");
        
        Assert.True(arr.GetBool(0, false));
    }

    #endregion
}