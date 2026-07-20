/*
 * Copyright (C) 2024 SAP SE
 * Modern .NET 9 SDK - GSResponse
 */

using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Gigya.Socialize.SDK;

/// <summary>
/// Wraps the server's response.
/// If the request was sent with the format set to "xml", the GetData() will return null and you should use GetResponseText() instead.
/// We only parse response text into GSObject if request format is set "json" which is the default. 
/// </summary>
public class GSResponse
{
    private static readonly Dictionary<int, string> ErrorMessages = new()
    {
        { 500026, "No Internet Connection" },
        { 400002, "Required parameter is missing" }
    };

    private readonly int _errorCode;
    private readonly string? _errorMessage;
    private readonly string _responseText = "";
    private readonly GSObject? _data;
    private readonly IDictionary<string, string> _headers;
    private readonly GSLogger _logger = new();

    #region Constructors

    /// <summary>
    /// Constructs a response for an error condition (no server response).
    /// </summary>
    public GSResponse(string method, GSObject? clientParams, int errorCode, GSLogger? logSoFar)
        : this(method, clientParams, errorCode, GetErrorMessage(errorCode), logSoFar)
    {
    }

    /// <summary>
    /// Constructs a response for an error condition with custom message.
    /// </summary>
    public GSResponse(string method, GSObject? clientParams, int errorCode, string? errorMessage, GSLogger? logSoFar)
        : this(method, GetErrorResponseText(method, clientParams, errorCode, errorMessage ?? GetErrorMessage(errorCode)), logSoFar)
    {
    }

    /// <summary>
    /// Constructs a response from response text.
    /// </summary>
    public GSResponse(string method, string responseText, GSLogger? logSoFar)
        : this(method, new Dictionary<string, string>(), responseText, logSoFar)
    {
    }

    /// <summary>
    /// Constructs a response from headers and response text.
    /// </summary>
    public GSResponse(string method, Dictionary<string, string> headers, string responseText, GSLogger? logSoFar)
    {
        _logger.Write(logSoFar);
        _headers = headers;
        _responseText = responseText.Trim();

        if (string.IsNullOrEmpty(responseText))
            return;

        _logger.Write("response", responseText);

        if (responseText.StartsWith("{")) // JSON format
        {
            try
            {
                _data = new GSObject(responseText);
                _errorCode = _data.GetInt("errorCode", 0);
                _errorMessage = _data.GetString("errorMessage", null);
            }
            catch (Exception ex)
            {
                _errorCode = 500;
                _errorMessage = ex.Message;
            }
        }
        else // XML format
        {
            var errCodeStr = GetStringBetween(responseText, "<errorCode>", "</errorCode>");
            if (errCodeStr != null && int.TryParse(errCodeStr, out var parsedErrorCode))
            {
                _errorCode = parsedErrorCode;
                _errorMessage = GetStringBetween(responseText, "<errorMessage>", "</errorMessage>");
            }
            _data = XmlToGSObject(responseText);
        }
    }

    #endregion

    #region Get Methods (with default)

    /// <summary>
    /// Returns the value to which the specified key inside the response params is associated with, or defaultValue
    /// if the response params does not contain such a key.
    /// </summary>
    /// <param name="key">The key whose associated value is to be returned.
    /// The key can specify a dot-delimited path down the objects hierarchy, with brackets to access array items.
    /// For example, "users[0].identities[0].provider" (see socialize.exportUsers API).</param>
    /// <param name="defaultValue">The value to be returned if the request params doesn't contain the specified key.</param>
    /// <returns>The value to which the specified key is mapped, or the defaultValue if the request params
    /// does not contain such a key.</returns>
    public string? GetString(string key, string? defaultValue)
    {
        if (_data == null)
            throw new GSResponseNotInitializedException();
        return _data.GetString(key, defaultValue);
    }

    /// <summary>
    /// Returns the value to which the specified key inside the response params is associated with, or defaultValue
    /// if the response params does not contain such a key.
    /// </summary>
    /// <param name="key">The key whose associated value is to be returned.
    /// The key can specify a dot-delimited path down the objects hierarchy, with brackets to access array items.
    /// For example, "users[0].identities[0].provider" (see socialize.exportUsers API).</param>
    /// <param name="defaultValue">The value to be returned if the request params doesn't contain the specified key.</param>
    /// <returns>The value to which the specified key is mapped, or the defaultValue if the request params
    /// does not contain such a key.</returns>
    public long GetLong(string key, long defaultValue)
    {
        if (_data == null)
            throw new GSResponseNotInitializedException();
        return _data.GetLong(key, defaultValue);
    }

    /// <summary>
    /// Returns the value to which the specified key inside the response params is associated with, or defaultValue
    /// if the response params does not contain such a key.
    /// </summary>
    /// <param name="key">The key whose associated value is to be returned.
    /// The key can specify a dot-delimited path down the objects hierarchy, with brackets to access array items.
    /// For example, "users[0].identities[0].provider" (see socialize.exportUsers API).</param>
    /// <param name="defaultValue">The value to be returned if the request params doesn't contain the specified key.</param>
    /// <returns>The value to which the specified key is mapped, or the defaultValue if the request params
    /// does not contain such a key.</returns>
    public int GetInt(string key, int defaultValue)
    {
        if (_data == null)
            throw new GSResponseNotInitializedException();
        return _data.GetInt(key, defaultValue);
    }

    /// <summary>
    /// Returns the value to which the specified key inside the response params is associated with, or defaultValue
    /// if the response params does not contain such a key.
    /// </summary>
    /// <param name="key">The key whose associated value is to be returned.
    /// The key can specify a dot-delimited path down the objects hierarchy, with brackets to access array items.
    /// For example, "users[0].identities[0].provider" (see socialize.exportUsers API).</param>
    /// <param name="defaultValue">The value to be returned if the request params doesn't contain the specified key.</param>
    /// <returns>The value to which the specified key is mapped, or the defaultValue if the request params
    /// does not contain such a key.</returns>
    public double GetDouble(string key, double defaultValue)
    {
        if (_data == null)
            throw new GSResponseNotInitializedException();
        return _data.GetDouble(key, defaultValue);
    }

    /// <summary>
    /// Returns the value to which the specified key inside the response params is associated with, or defaultValue
    /// if the response params does not contain such a key.
    /// </summary>
    /// <param name="key">The key whose associated value is to be returned.
    /// The key can specify a dot-delimited path down the objects hierarchy, with brackets to access array items.
    /// For example, "users[0].identities[0].provider" (see socialize.exportUsers API).</param>
    /// <param name="defaultValue">The value to be returned if the request params doesn't contain the specified key.</param>
    /// <returns>The value to which the specified key is mapped, or the defaultValue if the request params
    /// does not contain such a key.</returns>
    public bool GetBool(string key, bool defaultValue)
    {
        if (_data == null)
            throw new GSResponseNotInitializedException();
        return _data.GetBool(key, defaultValue);
    }

    /// <summary>
    /// Returns the value to which the specified key inside the response params is associated with, or defaultValue
    /// if the response params does not contain such a key.
    /// </summary>
    /// <param name="key">The key whose associated value is to be returned.
    /// The key can specify a dot-delimited path down the objects hierarchy, with brackets to access array items.
    /// For example, "users[0].identities[0].provider" (see socialize.exportUsers API).</param>
    /// <param name="defaultValue">The value to be returned if the request params doesn't contain the specified key.</param>
    /// <returns>The value to which the specified key is mapped, or the defaultValue if the request params
    /// does not contain such a key.</returns>
    public GSObject? GetObject(string key, GSObject? defaultValue)
    {
        if (_data == null)
            throw new GSResponseNotInitializedException();
        return _data.GetObject(key, defaultValue);
    }

    /// <summary>
    /// Returns the value to which the specified key inside the response params is associated with, or defaultValue
    /// if the response params does not contain such a key.
    /// </summary>
    /// <param name="key">The key whose associated value is to be returned.
    /// The key can specify a dot-delimited path down the objects hierarchy, with brackets to access array items.
    /// For example, "users[0].identities[0].provider" (see socialize.exportUsers API).</param>
    /// <param name="defaultValue">The value to be returned if the request params doesn't contain the specified key.</param>
    /// <returns>The value to which the specified key is mapped, or the defaultValue if the request params
    /// does not contain such a key.</returns>
    public GSArray? GetArray(string key, GSArray? defaultValue)
    {
        if (_data == null)
            throw new GSResponseNotInitializedException();
        return _data.GetArray(key, defaultValue);
    }

    /// <summary>
    /// Gets one or more nested objects.
    /// </summary>
    /// <typeparam name="T">The type of the object(s) to obtain. Can be either of: bool, bool?, int, int?, long,
    /// long?, decimal, decimal?, string, GSObject, GSArray. Note that if the object(s) pointed to by the path do
    /// not match this type, they won't be returned unless attemptConversion was set to true. Also note that if
    /// the response contains a null value and you request a non-nullable data type (bool, int, etc) then that
    /// response value will be ignored. If you do request a nullable or class type (int?, GSObject, etc) then a
    /// null object will be returned.</typeparam>
    /// <param name="path">A dot-delimited path down the objects hierarchy. You can access specific array items
    /// using bracket notation ([]). The brackets can specify an explicit index or '*' to traverse all elements.
    /// For example, if you call the socialize.exportUsers method (see http://developers.gigya.com/037_API_reference/020_REST_API/socialize.exportUsers),
    /// then the path "users[0].identities[0].provider" will return a list with a single item being the first user's
    /// first social provider. The path "users[*].UID" will return a list containing the UID field in each object in
    /// the users array.</param>
    /// <param name="attemptConversion">If true, and if you requested string results, then the internal response
    /// objects will be converted to strings (e.g. 2 --> "2"). This includes arrays and compound objects. If you
    /// requested a primitive type, a parse operation will be attempted (e.g. "2" --> 2).</param>
    /// <returns>A list of values which match the path and whose type matches the template parameter.</returns>
    public IEnumerable<T> Get<T>(string path, bool attemptConversion = false)
    {
        if (_data == null)
            throw new GSResponseNotInitializedException();
        return _data.Get<T>(path, attemptConversion);
    }

    #endregion

    #region Response Metadata

    /// <summary>
    /// Returns the result code of the operation. Code '0' indicates success, any other number indicates failure.
    /// For the complete list of server error codes, see 
    /// "http://wiki.gigya.com/030_API_reference/030_Response_Codes_and_Errors". 
    /// </summary>
    /// <returns>The error code.</returns>
    public int GetErrorCode() => _errorCode;

    /// <summary>
    /// Returns a short textual description of the response error, for logging purposes.
    /// </summary>
    /// <returns>The error message string.</returns>
    public string? GetErrorMessage() => _errorMessage;

    /// <summary>
    /// Returns the raw response data. 
    /// The raw response data is in JSON format, by default. If the request was sent with the format parameter set to "xml",
    /// the raw response data will be in XML format.
    /// </summary>
    /// <returns>The raw response data.</returns>
    public string GetResponseText() => _responseText;

    /// <summary>
    /// Returns the response data in GSObject. 
    /// Please refer to Gigya's REST API reference (http://wiki.gigya.com/030_API_reference/020_REST_API), 
    /// for a list of response data structure per method request.
    /// Note: If the request was sent with the format parameter set to "xml", the GetData() will return 
    /// null and you should use GetResponseText() method instead. 
    /// We only parse response text into GSObject if the request format is "json", which is the default.
    /// </summary>
    /// <returns>A GSObject containing the response data.</returns>
    public GSObject? GetData() => _data;

    /// <summary>
    /// Returns the response data cast to a typed object.
    /// </summary>
    public T? GetData<T>() where T : class, new()
    {
        return _data?.Cast<T>();
    }

    /// <summary>
    /// Returns the response headers.
    /// </summary>
    /// <returns>A dictionary containing the response headers.</returns>
    public IDictionary<string, string> GetHeaders() => _headers;

    /// <summary>
    /// Returns the trace log of the response.
    /// </summary>
    /// <returns>The trace log string.</returns>
    public string GetLog() => _logger.ToString();

    /// <summary>
    /// Returns a string representation of the response.
    /// </summary>
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append("\terrorCode:");
        sb.Append(_errorCode);
        sb.Append("\n\terrorMessage:");
        sb.Append(_errorMessage);
        sb.Append("\n\tdata:");
        sb.Append(_data);
        return sb.ToString();
    }

    #endregion

    #region Static Helpers

    internal static string GetErrorMessage(int errorCode)
    {
        return ErrorMessages.TryGetValue(errorCode, out var message) ? message : string.Empty;
    }

    internal static string GetErrorResponseText(string method, GSObject? clientParams, int errorCode, string? errorMessage)
    {
        errorMessage ??= GetErrorMessage(errorCode);

        var format = clientParams?.GetString("format", "json") ?? "json";

        if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            // Generate valid JSON with quoted keys
            return $"{{\"errorCode\":{errorCode},\"errorMessage\":\"{EscapeJsonString(errorMessage)}\"}}";
        }
        else
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.Append($"<{method}Response xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"urn:com:gigya:api http://socialize-api.gigya.com/schema\" xmlns=\"urn:com:gigya:api\">");
            sb.Append($"<errorCode>{errorCode}</errorCode>");
            sb.Append($"<errorMessage>{errorMessage}</errorMessage>");
            sb.Append($"</{method}Response>");
            return sb.ToString();
        }
    }

    #endregion

    #region Private Helpers

    private static string EscapeJsonString(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    private static string? GetStringBetween(string source, string prefix, string suffix)
    {
        if (string.IsNullOrEmpty(source)) return null;

        var prefixStart = source.IndexOf(prefix, StringComparison.Ordinal);
        var suffixStart = source.IndexOf(suffix, StringComparison.Ordinal);

        if (prefixStart == -1 || suffixStart == -1) return null;

        return source.Substring(prefixStart + prefix.Length, suffixStart - (prefixStart + prefix.Length));
    }

    private static readonly Regex PathRegex = new(
        @"^(((^|\.)(?<token>[^\.\[\]]+))|(?<token>\[(([0-9]+)|(\*))\]))*$",
        RegexOptions.Compiled | RegexOptions.ExplicitCapture);

    internal static string[] TokenizePath(string path)
    {
        var matches = PathRegex.Match(path);
        if (matches.Success)
            return matches.Groups["token"].Captures.Select(c => c.Value).ToArray();
        throw new ArgumentException("Invalid path format");
    }

    private static GSObject? XmlToGSObject(string responseText)
    {
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(responseText);
            
            var result = new GSObject();
            if (doc.DocumentElement != null)
            {
                XmlElementToGSObject(doc.DocumentElement, result);
            }
            return result;
        }
        catch
        {
            return null;
        }
    }

    private static void XmlElementToGSObject(XmlElement node, GSObject dict)
    {
        foreach (XmlNode child in node.ChildNodes)
        {
            if (child is XmlText textNode)
            {
                dict.Put("value", textNode.InnerText);
            }
            else if (child is XmlElement element)
            {
                if (element.ChildNodes.Count == 1 && element.FirstChild is XmlText text)
                {
                    dict.Put(element.Name, text.InnerText);
                }
                else if (element.ChildNodes.Count == 0)
                {
                    dict.Put(element.Name, "");
                }
                else
                {
                    var childObj = new GSObject();
                    XmlElementToGSObject(element, childObj);
                    dict.Put(element.Name, childObj);
                }
            }
        }
    }

    #endregion
}