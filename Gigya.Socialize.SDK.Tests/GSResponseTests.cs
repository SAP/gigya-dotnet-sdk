/*
 * Copyright (C) 2024 SAP SE
 * Characterization tests for GSResponse
 */

using Xunit;

namespace Gigya.Socialize.SDK.Tests;

/// <summary>
/// Characterization tests for GSResponse to verify behavior matches legacy SDK.
/// </summary>
public class GSResponseTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_FromJsonResponse_ParsesCorrectly()
    {
        var json = "{\"errorCode\":0,\"errorMessage\":\"OK\",\"data\":{\"name\":\"John\"}}";
        var response = new GSResponse("test.method", json, null);
        
        Assert.Equal(0, response.GetErrorCode());
        Assert.Equal("OK", response.GetErrorMessage());
    }

    [Fact]
    public void Constructor_FromErrorCode_CreatesErrorResponse()
    {
        var response = new GSResponse("test.method", null, 400002, null);
        
        Assert.Equal(400002, response.GetErrorCode());
        Assert.Equal("Required parameter is missing", response.GetErrorMessage());
    }

    [Fact]
    public void Constructor_FromErrorCodeWithMessage_UsesProvidedMessage()
    {
        var response = new GSResponse("test.method", null, 500, "Custom error", null);
        
        Assert.Equal(500, response.GetErrorCode());
        Assert.Equal("Custom error", response.GetErrorMessage());
    }

    [Fact]
    public void Constructor_WithHeaders_StoresHeaders()
    {
        var headers = new Dictionary<string, string>
        {
            { "Content-Type", "application/json" },
            { "X-Custom", "value" }
        };
        var json = "{\"errorCode\":0}";
        var response = new GSResponse("test.method", headers, json, null);
        
        var responseHeaders = response.GetHeaders();
        Assert.Equal("application/json", responseHeaders["Content-Type"]);
        Assert.Equal("value", responseHeaders["X-Custom"]);
    }

    #endregion

    #region GetErrorCode/GetErrorMessage Tests

    [Fact]
    public void GetErrorCode_SuccessResponse_ReturnsZero()
    {
        var json = "{\"errorCode\":0}";
        var response = new GSResponse("test.method", json, null);
        
        Assert.Equal(0, response.GetErrorCode());
    }

    [Fact]
    public void GetErrorCode_ErrorResponse_ReturnsErrorCode()
    {
        var json = "{\"errorCode\":403005,\"errorMessage\":\"Unauthorized\"}";
        var response = new GSResponse("test.method", json, null);
        
        Assert.Equal(403005, response.GetErrorCode());
    }

    [Fact]
    public void GetErrorMessage_ErrorResponse_ReturnsMessage()
    {
        var json = "{\"errorCode\":403005,\"errorMessage\":\"Unauthorized access\"}";
        var response = new GSResponse("test.method", json, null);
        
        Assert.Equal("Unauthorized access", response.GetErrorMessage());
    }

    #endregion

    #region GetData Tests

    [Fact]
    public void GetData_ValidJson_ReturnsGSObject()
    {
        var json = "{\"errorCode\":0,\"name\":\"John\",\"age\":30}";
        var response = new GSResponse("test.method", json, null);
        
        var data = response.GetData();
        Assert.NotNull(data);
        Assert.Equal("John", data!.GetString("name", null));
        Assert.Equal(30, data.GetInt("age", 0));
    }

    [Fact]
    public void GetData_EmptyResponse_ReturnsNull()
    {
        var response = new GSResponse("test.method", "", null);
        
        Assert.Null(response.GetData());
    }

    #endregion

    #region GetResponseText Tests

    [Fact]
    public void GetResponseText_ReturnsOriginalText()
    {
        var json = "{\"errorCode\":0,\"data\":\"test\"}";
        var response = new GSResponse("test.method", json, null);
        
        Assert.Equal(json, response.GetResponseText());
    }

    [Fact]
    public void GetResponseText_TrimsWhitespace()
    {
        var json = "  {\"errorCode\":0}  ";
        var response = new GSResponse("test.method", json, null);
        
        Assert.Equal("{\"errorCode\":0}", response.GetResponseText());
    }

    #endregion

    #region Get Value Methods Tests

    [Fact]
    public void GetString_ExistingKey_ReturnsValue()
    {
        var json = "{\"errorCode\":0,\"name\":\"John\"}";
        var response = new GSResponse("test.method", json, null);
        
        Assert.Equal("John", response.GetString("name", null));
    }

    [Fact]
    public void GetString_MissingKey_ReturnsDefault()
    {
        var json = "{\"errorCode\":0}";
        var response = new GSResponse("test.method", json, null);
        
        Assert.Equal("default", response.GetString("missing", "default"));
    }

    [Fact]
    public void GetInt_ExistingKey_ReturnsValue()
    {
        var json = "{\"errorCode\":0,\"count\":42}";
        var response = new GSResponse("test.method", json, null);
        
        Assert.Equal(42, response.GetInt("count", 0));
    }

    [Fact]
    public void GetLong_ExistingKey_ReturnsValue()
    {
        var json = "{\"errorCode\":0,\"bigNumber\":9876543210}";
        var response = new GSResponse("test.method", json, null);
        
        Assert.Equal(9876543210L, response.GetLong("bigNumber", 0));
    }

    [Fact]
    public void GetDouble_ExistingKey_ReturnsValue()
    {
        var json = "{\"errorCode\":0,\"price\":19.99}";
        var response = new GSResponse("test.method", json, null);
        
        Assert.Equal(19.99, response.GetDouble("price", 0), 2);
    }

    [Fact]
    public void GetBool_ExistingKey_ReturnsValue()
    {
        var json = "{\"errorCode\":0,\"active\":true}";
        var response = new GSResponse("test.method", json, null);
        
        Assert.True(response.GetBool("active", false));
    }

    [Fact]
    public void GetObject_ExistingKey_ReturnsGSObject()
    {
        var json = "{\"errorCode\":0,\"user\":{\"name\":\"John\"}}";
        var response = new GSResponse("test.method", json, null);
        
        var user = response.GetObject("user", null);
        Assert.NotNull(user);
        Assert.Equal("John", user!.GetString("name", null));
    }

    [Fact]
    public void GetArray_ExistingKey_ReturnsGSArray()
    {
        var json = "{\"errorCode\":0,\"items\":[\"a\",\"b\",\"c\"]}";
        var response = new GSResponse("test.method", json, null);
        
        var items = response.GetArray("items", null);
        Assert.NotNull(items);
        Assert.Equal(3, items!.Length);
    }

    #endregion

    #region Get<T> Path Access Tests

    [Fact]
    public void Get_SimplePath_ReturnsValue()
    {
        var json = "{\"errorCode\":0,\"name\":\"John\"}";
        var response = new GSResponse("test.method", json, null);
        
        var results = response.Get<string>("name").ToList();
        
        Assert.Single(results);
        Assert.Equal("John", results[0]);
    }

    [Fact]
    public void Get_NestedPath_ReturnsValue()
    {
        var json = "{\"errorCode\":0,\"user\":{\"profile\":{\"name\":\"John\"}}}";
        var response = new GSResponse("test.method", json, null);
        
        var results = response.Get<string>("user.profile.name").ToList();
        
        Assert.Single(results);
        Assert.Equal("John", results[0]);
    }

    [Fact]
    public void Get_ArrayIndexPath_ReturnsValue()
    {
        var json = "{\"errorCode\":0,\"users\":[{\"name\":\"John\"},{\"name\":\"Jane\"}]}";
        var response = new GSResponse("test.method", json, null);
        
        var results = response.Get<string>("users[1].name").ToList();
        
        Assert.Single(results);
        Assert.Equal("Jane", results[0]);
    }

    [Fact]
    public void Get_ArrayWildcardPath_ReturnsAllValues()
    {
        var json = "{\"errorCode\":0,\"users\":[{\"name\":\"John\"},{\"name\":\"Jane\"},{\"name\":\"Bob\"}]}";
        var response = new GSResponse("test.method", json, null);
        
        var results = response.Get<string>("users[*].name").ToList();
        
        Assert.Equal(3, results.Count);
        Assert.Contains("John", results);
        Assert.Contains("Jane", results);
        Assert.Contains("Bob", results);
    }

    #endregion

    #region Exception Tests

    [Fact]
    public void GetString_NullData_ThrowsException()
    {
        var response = new GSResponse("test.method", "", null);
        
        Assert.Throws<GSResponseNotInitializedException>(() => response.GetString("key", null));
    }

    [Fact]
    public void GetInt_NullData_ThrowsException()
    {
        var response = new GSResponse("test.method", "", null);
        
        Assert.Throws<GSResponseNotInitializedException>(() => response.GetInt("key", 0));
    }

    [Fact]
    public void Get_NullData_ThrowsException()
    {
        var response = new GSResponse("test.method", "", null);
        
        Assert.Throws<GSResponseNotInitializedException>(() => response.Get<string>("key").ToList());
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_IncludesErrorCode()
    {
        var json = "{\"errorCode\":403005,\"errorMessage\":\"Unauthorized\"}";
        var response = new GSResponse("test.method", json, null);
        
        var str = response.ToString();
        
        Assert.Contains("errorCode", str);
        Assert.Contains("403005", str);
    }

    [Fact]
    public void ToString_IncludesErrorMessage()
    {
        var json = "{\"errorCode\":403005,\"errorMessage\":\"Unauthorized\"}";
        var response = new GSResponse("test.method", json, null);
        
        var str = response.ToString();
        
        Assert.Contains("errorMessage", str);
        Assert.Contains("Unauthorized", str);
    }

    #endregion

    #region XML Response Tests

    [Fact]
    public void Constructor_FromXmlResponse_ParsesErrorCode()
    {
        var xml = "<?xml version=\"1.0\"?><response><errorCode>0</errorCode><errorMessage>OK</errorMessage></response>";
        var response = new GSResponse("test.method", xml, null);
        
        Assert.Equal(0, response.GetErrorCode());
        Assert.Equal("OK", response.GetErrorMessage());
    }

    [Fact]
    public void Constructor_FromXmlErrorResponse_ParsesErrorCode()
    {
        var xml = "<?xml version=\"1.0\"?><response><errorCode>400002</errorCode><errorMessage>Required parameter is missing</errorMessage></response>";
        var response = new GSResponse("test.method", xml, null);
        
        Assert.Equal(400002, response.GetErrorCode());
        Assert.Equal("Required parameter is missing", response.GetErrorMessage());
    }

    #endregion

    #region GetLog Tests

    [Fact]
    public void GetLog_ReturnsLogString()
    {
        var json = "{\"errorCode\":0}";
        var response = new GSResponse("test.method", json, null);
        
        var log = response.GetLog();
        
        Assert.NotNull(log);
        Assert.IsType<string>(log);
    }

    #endregion
}