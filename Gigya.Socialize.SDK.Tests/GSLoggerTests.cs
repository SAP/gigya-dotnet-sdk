/*
 * Copyright (C) 2024 SAP SE
 * Characterization tests for GSLogger
 */

using Xunit;

namespace Gigya.Socialize.SDK.Tests;

/// <summary>
/// Characterization tests for GSLogger to verify behavior matches legacy SDK.
/// </summary>
public class GSLoggerTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_CreatesEmptyLogger()
    {
        var logger = new GSLogger();
        
        Assert.Equal("", logger.ToString());
    }

    #endregion

    #region Write(object) Tests

    [Fact]
    public void Write_Object_AppendsToLog()
    {
        var logger = new GSLogger();
        
        logger.Write("Test message");
        
        Assert.Contains("Test message", logger.ToString());
    }

    [Fact]
    public void Write_NullObject_DoesNotAppend()
    {
        var logger = new GSLogger();
        
        logger.Write((object?)null);
        
        Assert.Equal("", logger.ToString());
    }

    [Fact]
    public void Write_MultipleObjects_AppendsAll()
    {
        var logger = new GSLogger();
        
        logger.Write("First");
        logger.Write("Second");
        logger.Write("Third");
        
        var log = logger.ToString();
        Assert.Contains("First", log);
        Assert.Contains("Second", log);
        Assert.Contains("Third", log);
    }

    [Fact]
    public void Write_IntegerObject_ConvertsToString()
    {
        var logger = new GSLogger();
        
        logger.Write(42);
        
        Assert.Contains("42", logger.ToString());
    }

    #endregion

    #region Write(Exception) Tests

    [Fact]
    public void Write_Exception_AppendsStackTrace()
    {
        var logger = new GSLogger();
        
        try
        {
            throw new InvalidOperationException("Test exception");
        }
        catch (Exception ex)
        {
            logger.Write(ex);
        }
        
        var log = logger.ToString();
        // Stack trace should contain method name or file info
        Assert.True(log.Length > 0);
    }

    #endregion

    #region Write(string, object) Tests

    [Fact]
    public void Write_KeyValue_FormatsCorrectly()
    {
        var logger = new GSLogger();
        
        logger.Write("apiKey", "test-api-key");
        
        var log = logger.ToString();
        Assert.Contains("apiKey", log);
        Assert.Contains("test-api-key", log);
    }

    [Fact]
    public void Write_KeyWithNullValue_AppendsKeyOnly()
    {
        var logger = new GSLogger();
        
        logger.Write("key", null);
        
        var log = logger.ToString();
        Assert.Contains("key", log);
    }

    [Fact]
    public void Write_NullKeyWithValue_AppendsValueOnly()
    {
        var logger = new GSLogger();
        
        logger.Write(null, "value");
        
        var log = logger.ToString();
        Assert.Contains("value", log);
    }

    [Fact]
    public void Write_MultipleKeyValues_AppendsAll()
    {
        var logger = new GSLogger();
        
        logger.Write("key1", "value1");
        logger.Write("key2", "value2");
        logger.Write("key3", "value3");
        
        var log = logger.ToString();
        Assert.Contains("key1", log);
        Assert.Contains("value1", log);
        Assert.Contains("key2", log);
        Assert.Contains("value2", log);
        Assert.Contains("key3", log);
        Assert.Contains("value3", log);
    }

    #endregion

    #region WriteFormat Tests

    [Fact]
    public void WriteFormat_FormatsString()
    {
        var logger = new GSLogger();
        
        logger.WriteFormat("Hello {0}!", "World");
        
        Assert.Contains("Hello World!", logger.ToString());
    }

    [Fact]
    public void WriteFormat_MultipleArgs_FormatsAll()
    {
        var logger = new GSLogger();
        
        logger.WriteFormat("{0} + {1} = {2}", 1, 2, 3);
        
        Assert.Contains("1 + 2 = 3", logger.ToString());
    }

    [Fact]
    public void WriteFormat_NoArgs_ReturnsFormatString()
    {
        var logger = new GSLogger();
        
        logger.WriteFormat("No arguments here");
        
        Assert.Contains("No arguments here", logger.ToString());
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_EmptyLogger_ReturnsEmptyString()
    {
        var logger = new GSLogger();
        
        Assert.Equal("", logger.ToString());
    }

    [Fact]
    public void ToString_AfterWrites_ReturnsAllContent()
    {
        var logger = new GSLogger();
        logger.Write("Line 1");
        logger.Write("key", "value");
        logger.WriteFormat("Formatted: {0}", 42);
        
        var log = logger.ToString();
        
        Assert.Contains("Line 1", log);
        Assert.Contains("key", log);
        Assert.Contains("value", log);
        Assert.Contains("Formatted: 42", log);
    }

    [Fact]
    public void ToString_CanBeCalledMultipleTimes()
    {
        var logger = new GSLogger();
        logger.Write("Test");
        
        var log1 = logger.ToString();
        var log2 = logger.ToString();
        
        Assert.Equal(log1, log2);
    }

    #endregion

    #region Line Ending Tests

    [Fact]
    public void Write_KeyValue_AddsNewLine()
    {
        var logger = new GSLogger();
        
        logger.Write("key", "value");
        
        var log = logger.ToString();
        Assert.Contains("\r\n", log);
    }

    [Fact]
    public void Write_MultipleEntries_SeparatedByNewLines()
    {
        var logger = new GSLogger();
        
        logger.Write("key1", "value1");
        logger.Write("key2", "value2");
        
        var log = logger.ToString();
        var lines = log.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        
        Assert.True(lines.Length >= 2);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Logger_CanLogComplexScenario()
    {
        var logger = new GSLogger();
        
        logger.Write("=== Request Start ===");
        logger.Write("apiMethod", "accounts.getAccountInfo");
        logger.Write("apiKey", "test-api-key");
        logger.Write("params", "{\"UID\":\"test-uid\"}");
        logger.WriteFormat("Timestamp: {0}", DateTime.UtcNow.ToString("O"));
        logger.Write("=== Request End ===");
        
        var log = logger.ToString();
        
        Assert.Contains("Request Start", log);
        Assert.Contains("accounts.getAccountInfo", log);
        Assert.Contains("test-api-key", log);
        Assert.Contains("Request End", log);
    }

    #endregion
}