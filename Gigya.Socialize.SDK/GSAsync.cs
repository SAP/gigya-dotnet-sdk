/*
 * Copyright (C) 2024 SAP SE
 * Modern .NET 9 SDK - GSAsync
 */

using System.Net;
using System.Text;

namespace Gigya.Socialize.SDK;

/// <summary>
/// Base class for asynchronous operations implementing IAsyncResult.
/// </summary>
[Obsolete("Use async/await pattern with SendAsync instead.")]
public class GSAsync : IAsyncResult
{
    /// <summary>
    /// Gets the user-defined object that qualifies or contains information about an asynchronous operation.
    /// </summary>
    public object? AsyncState { get; protected set; }

    /// <summary>
    /// Gets a WaitHandle that is used to wait for an asynchronous operation to complete.
    /// </summary>
    public WaitHandle AsyncWaitHandle { get; protected set; }

    /// <summary>
    /// Gets a value that indicates whether the asynchronous operation completed synchronously.
    /// </summary>
    public bool CompletedSynchronously { get; protected set; }

    /// <summary>
    /// Gets a value that indicates whether the asynchronous operation has completed.
    /// </summary>
    public bool IsCompleted { get; protected set; }

    /// <summary>
    /// Initializes a new instance of the GSAsync class.
    /// </summary>
    /// <param name="asyncState">User-defined state object.</param>
    /// <param name="asyncWaitHandle">Wait handle for the operation.</param>
    /// <param name="completedSynchronously">Whether the operation completed synchronously.</param>
    /// <param name="isCompleted">Whether the operation is completed.</param>
    public GSAsync(object? asyncState, WaitHandle asyncWaitHandle, bool completedSynchronously, bool isCompleted)
    {
        AsyncState = asyncState;
        AsyncWaitHandle = asyncWaitHandle;
        CompletedSynchronously = completedSynchronously;
        IsCompleted = isCompleted;
    }
}

/// <summary>
/// Encapsulates asynchronous HTTP request operations.
/// Given a WebRequest and the request's body, it will asynchronously obtain the request stream,
/// write the request body, read the response headers, and read the response body.
/// </summary>
[Obsolete("Use async/await pattern with SendAsync instead.")]
public class GSAsyncRequest : GSAsync
{
    /// <summary>
    /// Gets the HTTP request.
    /// </summary>
    public HttpWebRequest Request { get; protected set; }

    /// <summary>
    /// Gets the HTTP response.
    /// </summary>
    public HttpWebResponse? Response { get; protected set; }

    /// <summary>
    /// Gets the response body as a string.
    /// </summary>
    public string? ResponseBody { get; protected set; }

    /// <summary>
    /// Gets any error that occurred during the operation.
    /// </summary>
    public Exception? Error { get; protected set; }

    private readonly byte[] _requestBody;
    private readonly AsyncCallback? _callback;
    private readonly byte[] _byteBuf = new byte[10 * 1024];
    private readonly char[] _charBuf = new char[10 * 1024];
    private readonly StringBuilder _builder = new(10 * 1024);
    private Decoder? _decoder;

    /// <summary>
    /// Initializes a new instance of the GSAsyncRequest class.
    /// </summary>
    /// <param name="request">A newly-created WebRequest.</param>
    /// <param name="requestBody">The body of the request that will be asynchronously written.</param>
    /// <param name="callback">An optional callback function that will be called AFTER the AsyncWaitHandle event is set.</param>
    /// <param name="state">An optional state that is stored in AsyncState which you can access once your callback is called.</param>
    public GSAsyncRequest(HttpWebRequest request, byte[] requestBody, AsyncCallback? callback, object? state)
        : base(state, new ManualResetEvent(false), false, false)
    {
        Request = request;
        _requestBody = requestBody;
        _callback = callback;
    }

    /// <summary>
    /// Begins the asynchronous send operation.
    /// </summary>
    public void BeginSend()
    {
        HandleError(() =>
        {
            Request.BeginGetRequestStream(OnGotRequestStream, null);
        });
    }

    private void OnGotRequestStream(IAsyncResult ar)
    {
        HandleError(() =>
        {
            CompletedSynchronously = ar.CompletedSynchronously;
            using (var reqStream = Request.EndGetRequestStream(ar))
                reqStream.Write(_requestBody, 0, _requestBody.Length);
            Request.BeginGetResponse(OnGotResponseHeaders, null);
        });
    }

    private void OnGotResponseHeaders(IAsyncResult ar)
    {
        HandleError(() =>
        {
            CompletedSynchronously &= ar.CompletedSynchronously;
            Response = (HttpWebResponse)Request.EndGetResponse(ar);
            _decoder = Encoding.GetEncoding(Response.CharacterSet ?? "utf-8").GetDecoder();
            Response.GetResponseStream()!.BeginRead(_byteBuf, 0, _byteBuf.Length, OnResponseChunkRead, null);
        });
    }

    private void OnResponseChunkRead(IAsyncResult ar)
    {
        HandleError(() =>
        {
            CompletedSynchronously &= ar.CompletedSynchronously;
            var read = Response!.GetResponseStream()!.EndRead(ar);
            _decoder!.Convert(_byteBuf, 0, read, _charBuf, 0, _charBuf.Length, read == 0, out _, out var charsUsed, out _);
            _builder.Append(_charBuf, 0, charsUsed);
            if (read > 0)
                Response.GetResponseStream()!.BeginRead(_byteBuf, 0, _byteBuf.Length, OnResponseChunkRead, null);
            else
            {
                ResponseBody = _builder.ToString();
                SignalCompleted(null);
            }
        });
    }

    private void HandleError(Action action)
    {
        try
        {
            action();
        }
        catch (Exception e)
        {
            if (IsCompleted)
                throw;
            else
                SignalCompleted(e);
        }
    }

    private void SignalCompleted(Exception? error)
    {
        IsCompleted = true;
        Error = error;
        ((ManualResetEvent)AsyncWaitHandle).Set();
        _callback?.Invoke(this);
    }
}

/// <summary>
/// Attempts to resend an asynchronous request as long as it fails to satisfy some criteria.
/// Provides a single wait handle that becomes signaled once the criteria is met.
/// </summary>
[Obsolete("Use async/await pattern with SendAsync instead.")]
public class GSAsyncReliableRequest : GSAsync
{
    /// <summary>
    /// Delegate for creating new requests.
    /// </summary>
    /// <param name="request">The created HTTP request.</param>
    /// <param name="requestBody">The request body.</param>
    public delegate void RequestFactory(out HttpWebRequest request, out byte[] requestBody);

    /// <summary>
    /// Gets the current GSAsyncRequest.
    /// </summary>
    public GSAsyncRequest GSAsyncRequest { get; protected set; } = null!;

    private readonly RequestFactory _requestFactory;
    private Func<GSAsyncRequest, bool>? _resendPredicate;
    private readonly AsyncCallback? _callback;

    /// <summary>
    /// Initializes a new instance of the GSAsyncReliableRequest class.
    /// </summary>
    /// <param name="requestFactory">Factory method to create new requests.</param>
    /// <param name="resendPredicate">Predicate to determine if request should be resent.</param>
    /// <param name="callback">Callback when operation completes.</param>
    /// <param name="state">User state object.</param>
    public GSAsyncReliableRequest(
        RequestFactory requestFactory,
        Func<GSAsyncRequest, bool> resendPredicate,
        AsyncCallback? callback,
        object? state)
        : base(state, new ManualResetEvent(false), false, false)
    {
        _requestFactory = requestFactory;
        _resendPredicate = resendPredicate;
        _callback = callback;
        FetchNewRequest();
    }

    /// <summary>
    /// Begins the reliable send operation.
    /// </summary>
    public void BeginReliableSend()
    {
        GSAsyncRequest.BeginSend();
    }

    /// <summary>
    /// Aborts the current request.
    /// </summary>
    public void Abort()
    {
        _resendPredicate = null;
        GSAsyncRequest.Request.Abort();
    }

    private void FetchNewRequest()
    {
        _requestFactory(out var webRequest, out var requestBody);
        GSAsyncRequest = new GSAsyncRequest(webRequest, requestBody, OnResult, null);
    }

    private void OnResult(IAsyncResult ar)
    {
        if (_resendPredicate != null && _resendPredicate(GSAsyncRequest))
        {
            FetchNewRequest();
            BeginReliableSend();
        }
        else
        {
            CompletedSynchronously = GSAsyncRequest.CompletedSynchronously;
            IsCompleted = GSAsyncRequest.IsCompleted;
            ((ManualResetEvent)AsyncWaitHandle).Set();
            _callback?.Invoke(this);
        }
    }
}