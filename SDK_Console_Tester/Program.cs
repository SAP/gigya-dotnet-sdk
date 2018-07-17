using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Gigya.Socialize.SDK;
using Newtonsoft.Json.Linq;


static class Program {

    // Operations
    enum Operation {
        NONE,
        single,
        async,
        burst,
        func,
        input,
        servers,
    }

    static Operation gTestType;
    static string gInputFile = null;


    // Async-related parameters
    static int      gNumRequestsToSend;
    static int      gPeriodInSecs;
    static NCalc.Expression gEquation;
    static bool     gVerifyCert = false;

    static bool?    gPrintAsyncStatus;
    static bool     gPrintResponses   = false;
    static bool     gPrintLog         = false;
    static List<DnsEndPoint> gHostsFromConsul = new List<DnsEndPoint>();


    // Common parameters, can be overriden per request
    class Per_Request_Settings {
        public string   ApiKey;
        public string   UserKey;
        public string   Secret;
        public string   Method;
        public string   Domain;
        public string   Host;
        public int      RequestTimeout = Timeout.Infinite;
        public bool     UseHttps = true;
        public bool     SignRequests = true;
        public string   Extract;
        public string   ExtractPrefix;
        public Dictionary<string, string> Params = new Dictionary<string,string>();

        public Per_Request_Settings Clone() {
            var clone = (Per_Request_Settings)this.MemberwiseClone();
            clone.Params = new Dictionary<string, string>(Params);
            return clone;
        }
    }

    static Per_Request_Settings common_settings = new Per_Request_Settings();


    static int Main(string[] args) {
        try {
            if (args.Length == 0 || !ParseArgs(args, common_settings) || !ValidateArgs(common_settings)) {
                PrintUsage();
                return 1;
            }

            if (!gVerifyCert)
                ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, sslPolicyErrors) => true;

            switch (gTestType) {
                case Operation.single:  PerformSyncSingle(common_settings); break;
                case Operation.async:   PerformAsyncSingle(common_settings); break;
                case Operation.burst:   PrintConfig(); PerformAsyncBurst(common_settings); break;
                case Operation.func:    PrintConfig(); PerformAsyncFunc(common_settings); break;
                case Operation.input:   ProcessInput(common_settings); break;
                case Operation.servers: CallServers(common_settings); break;
            }

            return 0;
        }
        catch (Exception e) {
            Console.Error.WriteLine(e);
            return 2;
        }
    }



    static bool ParseCommonArgs(string[] args, Per_Request_Settings settings) {
        return args.All(arg => ParseCommonArg(arg, settings));
    }



    static bool ParseCommonArg(string arg, Per_Request_Settings settings) {
        int tmpint;
        if (arg.StartsWith("apiKey="))
            settings.ApiKey = arg.Substring("apiKey=".Length);
        else if (arg.StartsWith("userKey="))
            settings.UserKey = arg.Substring("userKey=".Length);
        else if (arg.StartsWith("secret="))
            settings.Secret = arg.Substring("secret=".Length);
        else if (arg.StartsWith("method="))
            settings.Method = arg.Substring("method=".Length);
        else if (arg.StartsWith("domain="))
            settings.Domain = arg.Substring("domain=".Length);
        else if (arg.StartsWith("host"))
            settings.Host = arg.Substring("host=".Length);
        else if (arg.StartsWith("timeout=") && int.TryParse(arg.Substring("timeout=".Length), out tmpint))
            settings.RequestTimeout = tmpint;
        else if (arg == "http")
            settings.UseHttps = false;
        else if (arg == "noSign")
            settings.SignRequests = false;
        else if (arg.StartsWith("extract="))
        {
            var p = arg.Substring("extract=".Length).Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
            if (p.Length == 1)
            {
                settings.Extract = p[0];
                settings.ExtractPrefix = string.Empty;
            }
            else if (p.Length == 2)
            {
                settings.Extract = p[1];
                settings.ExtractPrefix = p[0] + '=';
            }
            else
            {
                Console.Error.WriteLine("extract example: 'extract=statusCode' or 'extract=PREFIX=statusCode'");
                return false;
            }
        }
        else if (arg.StartsWith("--") && (tmpint = arg.IndexOf('=')) != -1)
            settings.Params[arg.Substring(2, tmpint - 2)] = arg.Substring(tmpint + 1);
        else return false;
        return true;
    }



    static bool ParseArgs(string[] args, Per_Request_Settings settings) {
        Operation testType;
        int tmpint;
        bool tmpbool;

        foreach (var a in args) {
            if (ParseCommonArg(a, settings)) {
            }
            else if (a.StartsWith("requests=") && int.TryParse(a.Substring("requests=".Length), out tmpint))
                gNumRequestsToSend = tmpint;
            else if (a.StartsWith("pooling=") && bool.TryParse(a.Substring("pooling=".Length), out tmpbool))
                GSRequest.EnableConnectionPooling = tmpbool;
            else if (a.StartsWith("maxConns=") && int.TryParse(a.Substring("maxConns=".Length), out tmpint))
                GSRequest.MaxConcurrentConnections = tmpint;
            else if (a.StartsWith("requestsRateExpr="))
                gEquation = new NCalc.Expression(a.Substring("requestsRateExpr=".Length));
            else if (a.StartsWith("periodInSecs=") && int.TryParse(a.Substring("periodInSecs=".Length), out tmpint))
                gPeriodInSecs = tmpint;
            else if (a == "printResponses")
                gPrintResponses = true;
            else if (a == "printLog")
                gPrintLog = true;
            else if (a.StartsWith("printStatus=") && bool.TryParse(a.Substring("printStatus=".Length), out tmpbool))
                gPrintAsyncStatus = tmpbool;
            else if (a == "noThreadBlock")
                GSRequest.BlockWhenConnectionsExhausted = false;
            else if (a.StartsWith("input=")) {
                gInputFile = a.Substring("input=".Length);
                gTestType = Operation.input;
            }
            else if (a == "verifyCert")
                gVerifyCert = true;
            else if (a.StartsWith("consul=")) {
                if (!TryFetchHostsFromConsul(a))
                    return false;
            }
            else if (Enum.TryParse(a, out testType) && testType.ToString() == a)
                gTestType = testType;
            else {
                Console.Error.WriteLine("Unknown option/value '{0}'", a);
                return false;
            }
        }
        return true;
    }


    // consul=eu1:eu1a-consul1/comments-legaso-st1,Gator-st1
    static bool TryFetchHostsFromConsul(string arg)
    {
        string consulHost, dc;
        string[] parts, services;

        if (   (parts = arg.Substring("consul=".Length).Split('/')).Length != 2
            || (services = parts[1].Split(',')).Length == 0
            || services.Any(string.IsNullOrWhiteSpace)
            || (parts = parts[0].Split(':')).Length != 2
            || string.IsNullOrWhiteSpace(dc = parts[0])
            || string.IsNullOrWhiteSpace(consulHost = parts[1]))
            return false;

        foreach (string service in services)
        {
            var response = JObject.Parse(new HttpClient().GetStringAsync($"http://{consulHost}:8500/v1/query/{service}/execute?dc={dc}").Result);
            var nodes = response["Nodes"].Select(n => new DnsEndPoint(n["Node"]["Node"].ToString(), ushort.Parse(n["Service"]["Port"].ToString())));
            gHostsFromConsul.AddRange(nodes);
        }

        return true;
    }


    static bool ValidateArgs(Per_Request_Settings settings) {
        if (gTestType == Operation.NONE)
            gTestType = Operation.single;

        switch (gTestType) {
            case Operation.burst:
                if (gNumRequestsToSend == 0) {
                    Console.Error.WriteLine("Missing 'requests=' parameter");
                    return false;
                }
                break;
            case Operation.func:
                if (gPeriodInSecs == 0 || gEquation == null) {
                    Console.Error.WriteLine("Missing 'periodInSecs=' and/or 'requestsRateExpr=' params");
                    return false;
                }
                break;
            case Operation.async:  gPrintResponses = true; break;
            case Operation.single: gPrintResponses = true; break;
        }

        if (   gTestType != Operation.input
            && (   string.IsNullOrEmpty(settings.Secret)
                || string.IsNullOrEmpty(settings.Method))
                || (string.IsNullOrEmpty(settings.ApiKey) && string.IsNullOrEmpty(settings.UserKey)))
        {
            Console.Error.WriteLine("Either of apiKey,userKey, method or secret are missing");
            return false;
        }

        if (settings.Extract != null) {
            gPrintResponses = false;
            if (!gPrintAsyncStatus.HasValue)
                gPrintAsyncStatus = false;
        }

        return true;
    }



    static void PrintUsage() {
        var procName = Environment.GetCommandLineArgs()[0].Substring(Environment.GetCommandLineArgs()[0].LastIndexOf('\\') + 1);
        Console.Error.WriteLine(@"
Usage:  Tester.exe single|async|burst|func|input[=filename]|servers
        apiKey= userKey= secret= method= [domain=] [host=] [https] [noSign]
        [--requestParam=value ...] [extract=[prefix=]responseField]
        [pooling=true/false] [maxConns=] [timeout=] [noThreadBlock]
        [requests=] [periodInSecs=] [requestsRateExpr=]
        [printResponses] [printStatus=true/false] [printLog]

Operations:

  single: Performs a single synchronous request and prints the response.
          This is the default operation unless otherwise specified.

  async: Performs a single asynchronous request and prints the response.

  input: This operation will sends one asynchronous request per standard input
      line, or per line read from [=filename] if specified. Each line can
      specify tab-separated parameters. All common parameters (see below) are
      supported. Those parameters supplement or override the invocation
      parametes. For example:
         Tester.exe input secret=... https
            (input line #1):   apiKey=...\tmethod=...\t--param=value
            (input line #2):  userKey=...\tmethod=...\t--param=value
      This will issue https requests to two different sites of a partner,
      calling different methods with different parameters.

  burst: Asynchronously sends [requests=] copies of the request. Useful for
      generating load on a server and testing the various SDK async features.

  func: Asynchronously sends requests over [periodInSecs=] seconds, at a rate
      per second specified by a math function you provide with the
      [requestsRateExpr=] parameter. Your expression can include an 'x' param
      whose value is the number of seconds since the test started. E.g.:
         requestsRateExpr='10' will generate 10 requests/sec
         requestsRateExpr='(Sin(x/10)+1)*10' will oscillate between 0 and 20
                          requests per second.
         requestsRateExpr='if(x<10||x>70,10,0)' will send 10 requests per sec
                          for 10 seconds, pause for a minute, and then resume.
      You can use tools such as graphsketch.com to develop a function. See
      ncalc.codeplex.com for supported operators. Useful to hit the server with
      variable loads, and test SDK connection pooling expiration behavior.

  servers: Sends a copy of the request to each host obtained from Consul.
      See --consul.


Common parameters:

  [apiKey=] The site's API key. Required for all non- permissions.* APIs that
     are site-specific.

  [userKey=] An administrative user's key. Can be passed in addition to apiKey.

  [secret=] Mandatory. If userKey was not passed, then this should be the
     partner's secret (in base 64 encoding). Otherwise, the administrative
     user's secret.

  [method=] The API method to call, e.g. socialize.getuserInfo. Mandatory.

  [http] uses http insetad of https.

  [noSign] will not sign requests and send secret instead.

  [domain=] sends requests to this domain (e.g. ""eu1.gigya.com"") instead of
     the default domain (""gigya.com"").

  [host=] sends requests to this host (e.g. ""web501"").

  [consul=dc:<addr>/<serviceName>[,<serviceName>]...]
    Fetches a list of hosts from Consul. For example:
      us1:us1a-consul1/socialize-legacy-prod
      eu1:eu1a-consul1/comments-legaso-st1,Gator-st1
    Multiple consul params can be passed.

  [timeout=] defines how long to wait for a response, in milliseconds.

  [--requestParam=value] sets request-specific parameters, e.g. --uid=user_id

  [extract=[prefix=]responseField] extracts the responseField from the response
     and prints it instead of printing the whole response. responseField can be
     a path down an Xml/Json object. You can add a prefix to be printed before
     the extracted value, e.g. extract=myprefix=profile.email will print:
     myprefix=email@host.com . If you extract values from a response array, one
     line will be printed per item. This is useful when piping extracted values
     to other processes or as arguments to a subsequent 'input' operation.

  [printLog] prints the internal SDK log along with the response.

  [verifyCert] Do not ignore SSL certificate errors with the server.


Async-related parameters (for use with 'burst', 'func' and 'input' operations):

  [pooling=] defines whether to use a connection pool and reuse connections
     when sending requests. Default: true.

  [maxConns=] defines the maximum number of concurrent connections.

  [noThreadBlock] causes requests to be pipelined when all connections are in
     use instead of blocking the sending thread.

  [printStatus=true/false] toggles whether to print a status line per message
     sent/received. Default: true, unless [extract=] was specified.

  [printResponses] prints the full responses instead of just their status code.
", procName);
    }



    static void PrintConfig() {
        if (gPrintAsyncStatus == null || gPrintAsyncStatus.Value) {
            Console.WriteLine();
            Console.WriteLine("GSRequest.EnableConnectionPooling    = " + GSRequest.EnableConnectionPooling);
            Console.WriteLine("GSRequest.MAX_CONCURRENT_CONNECTIONS = " + GSRequest.MaxConcurrentConnections);
        }
    }


    static void PrintFullOrPartialResponse(GSRequest req, GSResponse res, Per_Request_Settings settings, int? request_id = null) {
        var sb = new StringBuilder();
        if (request_id.HasValue && (gPrintAsyncStatus == null || gPrintAsyncStatus.Value)) {
            sb.AppendFormat("#{0,3}", request_id);
            if (gTestType != Operation.servers)
                sb.Append("    RECV <<");
            if (!req.UseMethodDomain)
                sb.AppendFormat($"  host={req.APIDomain}");
            else if (settings.Domain != null)
                sb.AppendFormat("  domain={0}", settings.Domain);
            sb.Append("  errCode: ").Append(res.GetErrorCode()).Append("  ").Append(res.GetErrorMessage());
            if (res.GetErrorCode() != 0 && res.GetString("callId", null) != null)
                sb.Append("  callId:" + res.GetString("callId", null));
            sb.AppendLine();
        }
        if (gPrintLog)
            sb.AppendLine(res.GetLog());
        else if (gPrintResponses)
            sb.AppendLine(res.GetResponseText()); // one atomic write to prevent multithreaded garbage
        else if (res.GetErrorCode() != 0)
        {
            if (gTestType != Operation.servers)
                sb.AppendFormat("{0} {1}\n", res.GetErrorCode(), res.GetErrorMessage());
        }
        else if (settings.Extract != null)
            foreach (var s in res.Get<string>(settings.Extract, true))
                sb.AppendFormat("{0}{1}\n", settings.ExtractPrefix, s);
        Console.Write(sb); // one atomic write to prevent multithreaded garbage
    }


    static void ToggleSigning(this GSRequest request, bool sign)
    {
        if (!sign)
            request.GetType().GetField("SignRequests", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(request, false);
    }


    //========== single test ==================================================================

    static void PerformSyncSingle(Per_Request_Settings settings) {
        GSRequest request = new GSRequest(settings.ApiKey, settings.Secret, settings.Method, null, settings.UseHttps, settings.UserKey);
        request.ToggleSigning(settings.SignRequests);
        request.APIDomain = settings.Domain ?? settings.Host ?? request.APIDomain;
        request.UseMethodDomain = settings.Host == null;
        foreach (var kvp in settings.Params)
            request.SetParam(kvp.Key, kvp.Value);

        GSResponse res;
        if (settings.RequestTimeout == Timeout.Infinite)
            res = request.Send();
        else res = request.Send(settings.RequestTimeout);

        PrintFullOrPartialResponse(request, res, settings);
    }



    //========== async test =================================================================

    static void PerformAsyncSingle(Per_Request_Settings settings) {
        GSRequest request = new GSRequest(settings.ApiKey, settings.Secret, settings.Method, null, settings.UseHttps, settings.UserKey);
        request.ToggleSigning(settings.SignRequests);
        request.APIDomain = settings.Domain ?? settings.Host ?? request.APIDomain;
        request.UseMethodDomain = settings.Host == null;
        foreach (var kvp in settings.Params)
            request.SetParam(kvp.Key, kvp.Value);
        var iar = request.BeginSend(null, null);
        if (iar.AsyncWaitHandle.WaitOne(settings.RequestTimeout))
            PrintFullOrPartialResponse(request, request.EndSend(iar), settings);
        else Console.Error.WriteLine("TIMEOUT");
    }

    
    //========== input test ==================================================================

    static void ProcessInput(Per_Request_Settings settings) {
        var input = new StreamReader(gInputFile == null ? Console.OpenStandardInput() : File.OpenRead(gInputFile));
        responsesArrived = new CountdownEvent(1);
        string line;
        for (int line_num=1; (line = input.ReadLine()) != null; ++line_num) {
            responsesArrived.TryAddCount();
            PerformAsyncRequest(line_num, Make_Updated_Settings(line, settings));
        }
        responsesArrived.Signal();
        responsesArrived.Wait();
    }


    static Per_Request_Settings Make_Updated_Settings(string line, Per_Request_Settings settings) {
        var updated = settings.Clone();
        if (   !ParseCommonArgs(line.Split(new char[] {'\t'}, StringSplitOptions.RemoveEmptyEntries), updated)
            || !ValidateArgs(updated))
            throw new Exception(" while parsing input line arguments '" + line + "'");
        return updated;
    }


    //========== burst test ==================================================================


    static CountdownEvent responsesArrived;
    static int gSuccesses = 0, gFailures = 0;
    static ServicePoint servicePoint;


    static void PerformAsyncBurst(Per_Request_Settings settings) {
        if (gPrintAsyncStatus == null || gPrintAsyncStatus.Value)
            Console.WriteLine();
        responsesArrived = new CountdownEvent(gNumRequestsToSend);
        var requests = new Stopwatch();
        var responses = new Stopwatch();
        requests.Start();
        responses.Start();
        for (int i=0; i<gNumRequestsToSend; ++i )
            PerformAsyncRequest(i, settings);
        requests.Stop();
        responsesArrived.Wait();
        responses.Stop();
        Console.WriteLine();
        Console.WriteLine("Sending   requests  took {0:mm'm:'ss's.'fff'ms'} ({1,6:0.0} ms/req  ; {2,6:0.0} reqs /sec).",
            requests.Elapsed, requests.ElapsedMilliseconds / 1.0 / gNumRequestsToSend, gNumRequestsToSend * 1000.0 / requests.ElapsedMilliseconds);
        Console.WriteLine("Receiving responses took {0:mm'm:'ss's.'fff'ms'} ({1,6:0.0} ms/resp ; {2,6:0.0} resps/sec).",
            responses.Elapsed, responses.ElapsedMilliseconds / 1.0 / gNumRequestsToSend, gNumRequestsToSend * 1000.0 / responses.ElapsedMilliseconds);
        Console.WriteLine("Received {0} OK responses, {1} failed", gSuccesses, gFailures);
    }


    struct Async_State {
        public GSRequest request;
        public int       request_id;
        public Per_Request_Settings settings;
    }


    static void PerformAsyncRequest(int requestId, Per_Request_Settings settings) {
        GSRequest request = new GSRequest(settings.ApiKey, settings.Secret, settings.Method, null, settings.UseHttps, settings.UserKey);
        request.ToggleSigning(settings.SignRequests);
        request.APIDomain = settings.Domain ?? settings.Host ?? request.APIDomain;
        request.UseMethodDomain = settings.Host == null;
        foreach (var kvp in settings.Params)
            request.SetParam(kvp.Key, kvp.Value);
        var state = new Async_State { request = request, request_id = requestId, settings = settings };
        var async = request.BeginSend(OnResponseArrived, state) as GSAsyncReliableRequest;
        if (async != null) {
            if (settings.RequestTimeout != Timeout.Infinite)
                new Timer(OnRequestTimeout, Tuple.Create(request, async.GSAsyncRequest), settings.RequestTimeout, 0);
            if (gPrintAsyncStatus == null || gPrintAsyncStatus.Value) {
                string line = String.Format("#{0,3} >> SENT     conns={1,3}/{2,3}", state.request_id, GSRequest.CurrentConnectionsUsed, GSRequest.MaxConcurrentConnections);
                if (settings.Domain != null)
                    line += String.Format("  domain={0}", settings.Domain);
                Console.WriteLine(line); // one atomic write to prevent multithreaded garbage
            }
        }
    }


    static void OnResponseArrived(IAsyncResult iar) {
        try {
            var state = (Async_State)iar.AsyncState;
            var response = state.request.EndSend(iar);
            if (response.GetErrorCode() != 0)
                Interlocked.Increment(ref gFailures);
            else Interlocked.Increment(ref gSuccesses);
            PrintFullOrPartialResponse(state.request, response, state.settings, state.request_id);
        }
        catch (Exception e) {
            Console.Error.WriteLine(e);
        }
        finally {
            responsesArrived.Signal();
        }
    }


    static void OnRequestTimeout(object state) {
        var req = (Tuple<GSRequest, GSAsyncRequest>)state;
        if (!req.Item2.IsCompleted)
            req.Item1.Abort();
    }



    //========== func test ==================================================================

    // Note: Here's a pattern to generate traffic, use up all connections in the pool, then decrease traffic, let connections expire and increase traffic again.
    //       Useful for testing stale connections from pool. This assumes a low-latency IIS server with 20-seconds timeout:
    //          maxConns=20 periodInSecs=60 requestsRateExpr='Min(90, Abs(30-x)*3)'
    //       See http://graphsketch.com/?eqn1_eqn=abs(30-x)*3&x_min=0&x_max=60&y_min=0&y_max=90&x_tick=10&y_tick=10&do_grid=1

    static void PerformAsyncFunc(Per_Request_Settings settings) {
        double currSendRate = 0.0, requestsSendTarget = 0.0;
        DateTime start = DateTime.UtcNow, now = start, prev = start, end = start.AddSeconds(gPeriodInSecs), lastPrinted = DateTime.MinValue;
        Console.WriteLine();
        responsesArrived = new CountdownEvent(1);
        int reqId=0, lastPrintedId=-1;
        while ((now = DateTime.UtcNow) < end) {
            requestsSendTarget += (now - prev).TotalSeconds * currSendRate;
            prev = now;
            gEquation.Parameters["x"] = (now - start).TotalSeconds;
            currSendRate = Math.Max(0.0, gEquation.EvaluateDouble());
            if ((reqId % 10 == 0 && lastPrintedId != reqId) || (now - lastPrinted).TotalSeconds >= 10.0) {
                if (servicePoint == null)
                    Console.WriteLine("\n({0:mm'm:'ss's'}) Sending {1:0.0} requests/sec\n", now - start, currSendRate);
                else Console.WriteLine("\n({0:mm'm:'ss's'}) Sending {1:0.0} requests/sec, using {2}/{3} conections.\n",
                    now - start, currSendRate, servicePoint.CurrentConnections, servicePoint.ConnectionLimit);
                lastPrinted = now;
                lastPrintedId = reqId;
            }
            for (; reqId < (int)requestsSendTarget; ++reqId) {
                responsesArrived.TryAddCount();
                PerformAsyncRequest(reqId, settings);
            }
            Thread.Sleep(10);
        }
        responsesArrived.Signal();
        responsesArrived.Wait();
        Console.WriteLine();
        Console.WriteLine("Sent {0} requests, {1} failed.", reqId, gFailures);
    }


    static double EvaluateDouble(this NCalc.Expression expr) {
        object o = expr.Evaluate();
        if (o is int)
            return (int)o;
        else if (o is double)
            return (double)o;
        else if (o is short)
            return (short)o;
        else if (o is ushort)
            return (ushort)o;
        else if (o is float)
            return (float)o;
        else throw new Exception("Unsopported type '" + o.GetType().Name + "' in math function");
    }



    //========== multi-servers ==================================================================


    static void CallServers(Per_Request_Settings settings)
    {
        gHostsFromConsul.Sort((a, b) => a.Host.CompareTo(b.Host));
        responsesArrived = new CountdownEvent(gHostsFromConsul.Count);

        int requestId = 0;
        foreach (var server in gHostsFromConsul)
        {
            GSRequest request = new GSRequest(settings.ApiKey, settings.Secret, settings.Method, null, settings.UseHttps, settings.UserKey);
            request.ToggleSigning(settings.SignRequests);
            request.APIDomain = server.Host + ':' + server.Port;
            request.UseMethodDomain = false;
            foreach (var kvp in settings.Params)
                request.SetParam(kvp.Key, kvp.Value);

            request.BeginSend(OnResponseArrived, new Async_State { request = request, settings = settings, request_id = requestId++ });
        }

        responsesArrived.Wait();
    }


    //========== SDK path validation ==================================================================

    /* Test path patterns on response; you can manually check that returned values are ok.
     * The response should be a reply to: method=gcs.getObjectData --type=test --id=3f1056667f2942feb536fd1e15d3c4e5
     * To create the queried object:      method=gcs.setObjectData --type=test --id=3f1056667f2942feb536fd1e15d3c4e5 --updateBehavior=replace --data='{age:36, name:"daniel", isMale:true, nullTest:null, intArr:[1,2,3], intIntArr:[[1,2,3],[4,5,6],[],[{name:"Tahel", age:4}],{an:"object"},[1,2000000000000,3.3,"4",true,null]]}'
     * */
    private static void ValidatePath(GSResponse res) {
        string  [] statusReason = res.Get<string>  ("statusReason").ToArray();               // [ "OK" ]
        int     [] errorCode    = res.Get<int>     ("errorCode").ToArray();                  // [ 0 ]
        GSObject[] data         = res.Get<GSObject>("data").ToArray();                       // [ {obj} ]
        int     [] age          = res.Get<int>     ("data.age").ToArray();                   // [ 36 ]
        string  [] name         = res.Get<string>  ("data.name").ToArray();                  // [ "daniel" ]
        bool    [] isMale       = res.Get<bool>    ("data.isMale").ToArray();                // [ true ]
        GSObject[] nullTestObj  = res.Get<GSObject>("data.nullTest").ToArray();              // [ null ]
        GSArray [] nullTestArr  = res.Get<GSArray> ("data.nullTest").ToArray();              // [ null ]
        string  [] nullTestStr  = res.Get<string>  ("data.nullTest").ToArray();              // [ null ]
        int     [] nullTestInt  = res.Get<int>     ("data.nullTest").ToArray();              // [ ]
        GSArray [] arr          = res.Get<GSArray> ("data.intArr").ToArray();                // [ [1,2,3] ]
        int     [] ints         = res.Get<int>     ("data.intArr[*]").ToArray();             // [ 1,2,3 ]
        GSArray [] invalidArr   = res.Get<GSArray> ("data.wrong_name").ToArray();            // [] (empty; not found)
        int     [] arr0         = res.Get<int>     ("data.intArr[0]").ToArray();             // [ 1 ]
        int     [] arr10        = res.Get<int>     ("data.intArr[10]").ToArray();            // [] (empty; not found)
        GSArray [] internalArr  = res.Get<GSArray> ("data.intIntArr[1]").ToArray();          // [ [4,5,6] ]
        int     [] internalInt  = res.Get<int>     ("data.intIntArr[0][0]").ToArray();       // [ 1 ]
        GSObject[] internalObj  = res.Get<GSObject>("data.intIntArr[4]").ToArray();          // [ {"an" : "object"} ]
        string  [] field        = res.Get<string>  ("data.intIntArr[4].an").ToArray();       // [ "object" ]
        int     [] arrInt       = res.Get<int>     ("data.intIntArr[5][0]").ToArray();       // [ 1 ]
        long    [] arrLong      = res.Get<long>    ("data.intIntArr[5][1]").ToArray();       // [ 2000000000000 ]
        decimal [] arrDouble    = res.Get<decimal> ("data.intIntArr[5][2]").ToArray();       // [ 3.3 ]
        string  [] arrString    = res.Get<string>  ("data.intIntArr[5][3]").ToArray();       // [ "4" ]
        bool    [] arrBool      = res.Get<bool>    ("data.intIntArr[5][4]").ToArray();       // [ true ]
        int     [] ints2        = res.Get<int>     ("data.intIntArr[5][*]").ToArray();       // [ 1 ]
        int?    [] ints2n       = res.Get<int?>    ("data.intIntArr[5][*]").ToArray();       // [ 1,null ]
        int     [] ints3        = res.Get<int>     ("data.intIntArr[5][*]", true).ToArray(); // [ 1,3,4,1 ]
        int?    [] ints3n       = res.Get<int?>    ("data.intIntArr[5][*]", true).ToArray(); // [ 1,3,4,1,null ]
        string  [] strings      = res.Get<string>  ("data.intIntArr[5][*]", true).ToArray(); // [ "1","2000000000000","3.3","4","true",null ]
        string  [] an           = res.Get<string>  ("data.intIntArr[*].an").ToArray();       // [ "object" ]
        string  [] names        = res.Get<string>  ("data.intIntArr[*][*].name").ToArray();  // [ "Tahel" ]
    }

}
