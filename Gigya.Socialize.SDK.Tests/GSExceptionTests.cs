/*
 * Copyright (C) 2024 SAP SE
 * Characterization tests for GSException classes
 */

using Xunit;

namespace Gigya.Socialize.SDK.Tests;

/// <summary>
/// Characterization tests for GSException classes to verify behavior matches legacy SDK.
/// </summary>
public class GSExceptionTests
{
    #region GSException Tests

    [Fact]
    public void GSException_WithMessage_StoresMessage()
    {
        var exception = new GSException("Test error message");
        
        Assert.Equal("Test error message", exception.Message);
    }

    [Fact]
    public void GSException_WithMessageAndInnerException_StoresBoth()
    {
        var innerException = new InvalidOperationException("Inner error");
        var exception = new GSException("Outer error", innerException);
        
        Assert.Equal("Outer error", exception.Message);
        Assert.Same(innerException, exception.InnerException);
    }

    [Fact]
    public void GSException_InheritsFromException()
    {
        var exception = new GSException("Test");
        
        Assert.IsAssignableFrom<Exception>(exception);
    }

    [Fact]
    public void GSException_CanBeCaughtAsException()
    {
        Exception? caughtException = null;
        
        try
        {
            throw new GSException("Test error");
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }
        
        Assert.NotNull(caughtException);
        Assert.IsType<GSException>(caughtException);
    }

    [Fact]
    public void GSException_CanBeThrown()
    {
        Action action = () => throw new GSException("Test error");
        Assert.Throws<GSException>(action);
    }

    #endregion

    #region GSKeyNotFoundException Tests

    [Fact]
    public void GSKeyNotFoundException_WithMessage_StoresMessage()
    {
        var exception = new GSKeyNotFoundException("Key 'test' not found");
        
        Assert.Equal("Key 'test' not found", exception.Message);
    }

    [Fact]
    public void GSKeyNotFoundException_InheritsFromGSException()
    {
        var exception = new GSKeyNotFoundException("Test");
        
        Assert.IsAssignableFrom<GSException>(exception);
    }

    [Fact]
    public void GSKeyNotFoundException_CanBeCaughtAsGSException()
    {
        GSException? caughtException = null;
        
        try
        {
            throw new GSKeyNotFoundException("Key not found");
        }
        catch (GSException ex)
        {
            caughtException = ex;
        }
        
        Assert.NotNull(caughtException);
        Assert.IsType<GSKeyNotFoundException>(caughtException);
    }

    [Fact]
    public void GSKeyNotFoundException_CanBeThrown()
    {
        Action action = () => throw new GSKeyNotFoundException("Key not found");
        Assert.Throws<GSKeyNotFoundException>(action);
    }

    #endregion

    #region GSResponseNotInitializedException Tests

    [Fact]
    public void GSResponseNotInitializedException_HasDefaultMessage()
    {
        var exception = new GSResponseNotInitializedException();
        
        Assert.Contains("response that failed to arrive", exception.Message);
        Assert.Contains("check the response error code", exception.Message);
    }

    [Fact]
    public void GSResponseNotInitializedException_InheritsFromGSException()
    {
        var exception = new GSResponseNotInitializedException();
        
        Assert.IsAssignableFrom<GSException>(exception);
    }

    [Fact]
    public void GSResponseNotInitializedException_CanBeCaughtAsGSException()
    {
        GSException? caughtException = null;
        
        try
        {
            throw new GSResponseNotInitializedException();
        }
        catch (GSException ex)
        {
            caughtException = ex;
        }
        
        Assert.NotNull(caughtException);
        Assert.IsType<GSResponseNotInitializedException>(caughtException);
    }

    [Fact]
    public void GSResponseNotInitializedException_CanBeThrown()
    {
        Action action = () => throw new GSResponseNotInitializedException();
        Assert.Throws<GSResponseNotInitializedException>(action);
    }

    #endregion

    #region Exception Hierarchy Tests

    [Fact]
    public void ExceptionHierarchy_GSKeyNotFoundException_IsGSException()
    {
        var exception = new GSKeyNotFoundException("Test");
        
        Assert.True(exception is GSException);
        Assert.True(exception is Exception);
    }

    [Fact]
    public void ExceptionHierarchy_GSResponseNotInitializedException_IsGSException()
    {
        var exception = new GSResponseNotInitializedException();
        
        Assert.True(exception is GSException);
        Assert.True(exception is Exception);
    }

    [Fact]
    public void ExceptionHierarchy_CanCatchAllGSExceptions()
    {
        var exceptions = new List<GSException>();
        
        try { throw new GSException("Test"); }
        catch (GSException ex) { exceptions.Add(ex); }
        
        try { throw new GSKeyNotFoundException("Test"); }
        catch (GSException ex) { exceptions.Add(ex); }
        
        try { throw new GSResponseNotInitializedException(); }
        catch (GSException ex) { exceptions.Add(ex); }
        
        Assert.Equal(3, exceptions.Count);
    }

    #endregion
}