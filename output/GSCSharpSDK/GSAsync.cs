using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Text;

namespace Gigya.Socialize.SDK {


public class GSAsync : IAsyncResult {
    public object     AsyncState             { get; protected set; }
    public WaitHandle AsyncWaitHandle        { get; protected set; }
    public bool       CompletedSynchronously { get; protected set; }
    public bool       IsCompleted            { get; protected set; }

    public GSAsync(object AsyncState, WaitHandle AsyncWaitHandle, bool CompletedSynchronously, bool IsCompleted) {
        this.AsyncState             = AsyncState;
        this.AsyncWaitHandle        = AsyncWaitHandle;
        this.CompletedSynchronously = CompletedSynchronously;
        this.IsCompleted            = IsCompleted;
    }
}


/// <summary>
/// This class encapsulates 2 actions:
/// 
/// 1. Given a WebRequest and the request's body, it will asynchronously obtain the request stream, write the request
///    body, read the response headers, and read the response body. This class provides an IAsyncResult interface that
///    can be used to wait until all these operations are complete, atomically. The CompletedSynchronously property will
///    be set to true only if all operations completed synchronously. During this process we do not hold up any threads.
///    
/// 2. If an error occurs along the way, the exception is stored in the Error property. In such a case WebResponse and
///    ResponseBody may or may not be null. In either case, it is guaranteed that the AsyncWaitHandle event will be set
///    and your callback called (in that order).
/// </summary>
public class GSAsyncRequest : GSAsync {

    public HttpWebRequest  Request      { get; protected set; }
    public HttpWebResponse Response     { get; protected set; }
    public string          ResponseBody { get; protected set; }
    public Exception       Error        { get; protected set; }


    /// <param name="request">A newly-created WebRequest.</param>
    /// <param name="requestBody">The body of the request that will be asynchronously written.</param>
    /// <param name="callback">An optioanl callback function that will be called AFTER the AsyncWaitHandle event is set.</param>
    /// <param name="state">An optional state that is stored in AsyncState which you can access once your calback is called.</param>
    public GSAsyncRequest(HttpWebRequest request, byte[] requestBody, AsyncCallback callback, object state)
        : base(state, new ManualResetEvent(false), false, false)
    {
        Request          = request;
        this.requestBody = requestBody;
        this.callback    = callback;
    }


    // Step 1: We initiate an aync request to get the request stream. Do not call the synchronous GetRequestStream();
    //         it will silently degrade a subsequent call to BeginGetResponse() to be fully syncronous.
    //         If an error occurs we store it and call-back the user.

    public void BeginSend() {
        HandleError(() => {
            Request.BeginGetRequestStream(OnGotRequestStream, null);
        });
    }


    // Step 2: Once the request stream is obtained, we write the request body. NOTE: The request body can only be
    //         written syncronously; writing it using BeginWrite() seems to cause the EndGetResponse() at step 4 to fail
    //         ("Request cancelled"). [TODO] Revisit this issue in later .Net framework versions.
    
    void OnGotRequestStream(IAsyncResult ar) {
        HandleError(() => {
            CompletedSynchronously = ar.CompletedSynchronously;
            using (Stream reqStream = Request.EndGetRequestStream(ar))
                reqStream.Write(requestBody, 0, requestBody.Length); // Note: A fully-async BeginWrite here seems to later fail EndGetResponse()
            Request.BeginGetResponse(OnGotResponseHeaders, null);
        });
    }


    // Step 3: Once the response headers are obtained, we create a decoder based on the content encoding and start
    //         reading the body.

    void OnGotResponseHeaders(IAsyncResult ar) {
        HandleError(() => {
            CompletedSynchronously &= ar.CompletedSynchronously;
            Response = (HttpWebResponse)Request.EndGetResponse(ar);
            decoder = Encoding.GetEncoding(Response.CharacterSet ?? "utf-8").GetDecoder();
            Response.GetResponseStream().BeginRead(byteBuf, 0, byteBuf.Length, OnResponseChunkRead, null);
        });
    }


    // Step 4: We repeatedly read byte chunks from the response body and convert them into characters. When the
    //         response is fully read, we store it in the ResponseBody and call back the user's callback.

    void OnResponseChunkRead(IAsyncResult ar) {
        HandleError(() => {
            CompletedSynchronously &= ar.CompletedSynchronously;
            bool completed;
            int bytesUsed, charsUsed, read = Response.GetResponseStream().EndRead(ar);
            decoder.Convert(byteBuf, 0, read, charBuf, 0, charBuf.Length, read == 0, out bytesUsed, out charsUsed, out completed);
            builder.Append(charBuf, 0, charsUsed);
            if (read > 0)
                Response.GetResponseStream().BeginRead(byteBuf, 0, byteBuf.Length, OnResponseChunkRead, null);
            else {
                ResponseBody = builder.ToString();
                SignalCompleted(null);
            }
        });
    }


    void HandleError(Action action) {
        try {
            action();
        }
        catch (Exception e) {
            if (IsCompleted) // This can happen if the inner action() calls SignalCompleted() which set IsCompleted to
                throw;       // true and called the user callback which triggered an unhandled exception.
            else SignalCompleted(e);
        }
    }


    void SignalCompleted(Exception error) {
        IsCompleted = true;
        Error = error;
        ((ManualResetEvent)AsyncWaitHandle).Set();
        if (callback != null)
            callback(this);
    }


    byte[]         byteBuf = new byte[10 * 1024];
    char[]         charBuf = new char[10 * 1024];
    StringBuilder  builder = new StringBuilder(10 * 1024);
    Decoder        decoder;
    byte[]         requestBody;
    AsyncCallback  callback;
}



/// <summary>
/// This class attempts to resend an asynchronous request as long as it fails to satisfy some criteria, while providing
/// a single wait handle that the user can wait on which will become signaled once the criteria is met. The requests are
/// performed using the GSAsyncRequest helper class (above). You need to supply a predicate which receives a
/// GSAsyncRequest object and determines whether to resend the request or not, by probing the GSAsyncRequest's Error
/// field (in case a transport error occured), Response field (for a HTTP error code) or ResponseBody field. You also
/// need to supply a factory method that re-generates a new request to be sent, since we cannot re-send the underlying
/// .Net WebRequest, nor clone it, nor would it be desirable to do so in scenarios where the protocol protects against
/// duplicate requests (such as OAuth v1).
/// </summary>
public class GSAsyncReliableRequest : GSAsync {

    public delegate void RequestFactory(out HttpWebRequest request, out byte[] requestBody);
    public GSAsyncRequest GSAsyncRequest { get; protected set; }


    public GSAsyncReliableRequest(RequestFactory requestFactory, Func<GSAsyncRequest, bool> resendPredicate, AsyncCallback callback, object state)
        : base(state, new ManualResetEvent(false), false, false)
    {
        this.requestFactory  = requestFactory;
        this.resendPredicate = resendPredicate;
        this.callback        = callback;
        FetchNewRequest();
    }


    public void BeginReliableSend() {
        GSAsyncRequest.BeginSend();
    }


    public void Abort() {
        resendPredicate = null;
        GSAsyncRequest.Request.Abort();
    }


    void FetchNewRequest() {
        HttpWebRequest webRequest;
        byte[] requestBody;
        requestFactory(out webRequest, out requestBody);
        GSAsyncRequest = new GSAsyncRequest(webRequest, requestBody, OnResult, null);
    }


    void OnResult(IAsyncResult ar) {
        if (resendPredicate(GSAsyncRequest)) {
            FetchNewRequest();
            BeginReliableSend();
        }
        else {
            CompletedSynchronously = GSAsyncRequest.CompletedSynchronously;
            IsCompleted = GSAsyncRequest.IsCompleted;
            ((ManualResetEvent)AsyncWaitHandle).Set();
            if (callback != null)
                callback(this);
        }
    }


    Func<GSAsyncRequest, bool> resendPredicate;
    RequestFactory requestFactory;
    AsyncCallback  callback;
}


}
