/*
 * Copyright (C) 2011 Gigya, Inc.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Gigya.Socialize.SDK
{
    /// <summary>
    /// Wraps the server's response.
    /// If the request was sent with the format set to "xml", the getData() will return null and you should use getRawData() instead.
    /// We only parse response text into GSObject if request format is set "json" which is the default. 
    /// </summary>
    /// <remarks>Author: Tamir Korem. Updated by: Yaron Thurm</remarks>
    public class GSResponse
    {
        #region Private Members
        private static Dictionary<int, string> errorMsgDic = new Dictionary<int, string>();

        private int errorCode = 0;
        private string errorMessage = null;
        private string responseText = "";
        private GSObject data;
        private IDictionary<string, string> headers;
        private GSLogger logger = new GSLogger();
        #endregion


        #region Constructors
        static GSResponse()
        {
            errorMsgDic.Add(500026, "No Internet Connection");
            errorMsgDic.Add(400002, "Required parameter is missing");
        }

        // for constructing response when there is not request (error before request creation)
        public GSResponse(String method, GSObject clientParams, int errorCode, GSLogger logSoFar) :
            this(method, clientParams, errorCode, GetErrorMessage(errorCode), logSoFar)
        {
        }

        public GSResponse(String method, GSObject clientParams, int errorCode, String errorMessage, GSLogger logSoFar) :
            this(method, GetErrorResponseText(method, clientParams, errorCode, errorMessage ?? GetErrorMessage(errorCode)), logSoFar)
        {
        }

        public GSResponse(String method, string responseText, GSLogger logSoFar) :
            this(method, new Dictionary<string, string>(), responseText, logSoFar)
        {
        }

        public GSResponse(String method, Dictionary<string, string> headers, string responseText, GSLogger logSoFar)
        {
            logger.Write(logSoFar);
            this.headers = headers;
            this.responseText = responseText.Trim();

            if (responseText == null || responseText.Length == 0)
                return;
            else
                logger.Write("response", responseText);

            if (responseText.StartsWith("{")) // JSON format
            {
                try
                {
                    this.data = new GSObject(responseText);
                    this.errorCode = data.GetInt("errorCode", 0);
                    this.errorMessage = data.GetString("errorMessage", null);
                }
                catch (Exception ex)
                {
                    this.errorCode = 500;
                    this.errorMessage = ex.Message;
                }
            }
            else
            {
                // using string search to avoid dependency on parser
                String errCodeStr = GetStringBetween(responseText, "<errorCode>", "</errorCode>");
                if (errCodeStr != null)
                {
                    this.errorCode = int.Parse(errCodeStr);
                    this.errorMessage = GetStringBetween(responseText, "<errorMessage>", "</errorMessage>");
                }
                //initializing the data with XML transformed to GSObject 
                this.data = XmlToJSON(responseText);
            }
        }


        #endregion


        #region Public Methods
        #region - Get Params methods -
        /// <summary>
        /// Returns the value to which the specified key inside the response params is associated with, or defaultValue
        /// if the response params does not contain such a key.
        /// </summary>
        /// <param name="key">the key whose associated value is to be returned.
        /// The key can specify a dot-delimited path down the objects hierarchy, with brackets to access array items.
        /// For example, "users[0].identities[0].provider" (see socialize.exportUsers API).</param>
        /// The key can specify a dot-delimited path down the objects hierarchy, with brackets to access array items.
        /// For example, "users[0].identities[0].provider" (see socialize.exportUsers API).</param>
        /// <param name="defaultValue">the value to be returned if the request params doesn't contain the specified key.</param>
        /// <returns>the value to which the specified key is mapped, or the defaultValue if the request params
        /// does not contain such a key</returns>
        public string GetString(string key, string defaultValue)
        {
            if (data == null)
                throw new GSResponseNotInitializedException();
            return this.data.GetString(key, defaultValue);
        }
        /// <summary>
        /// Returns the value to which the specified key inside the response params is associated with, or defaultValue
        /// if the response params does not contain such a key.
        /// </summary>
        /// <param name="key">the key whose associated value is to be returned.
        /// The key can specify a dot-delimited path down the objects hierarchy, with brackets to access array items.
        /// For example, "users[0].identities[0].provider" (see socialize.exportUsers API).</param>
        /// <param name="defaultValue">the value to be returned if the request params doesn't contain the specified key.</param>
        /// <returns>the value to which the specified key is mapped, or the defaultValue if the request params
        /// does not contain such a key</returns>
        public long GetLong(string key, long defaultValue)
        {
            if (data == null)
                throw new GSResponseNotInitializedException();
            return this.data.GetLong(key, defaultValue);
        }
        /// <summary>
        /// Returns the value to which the specified key inside the response params is associated with, or defaultValue
        /// if the response params does not contain such a key.
        /// </summary>
        /// <param name="key">the key whose associated value is to be returned.
        /// The key can specify a dot-delimited path down the objects hierarchy, with brackets to access array items.
        /// For example, "users[0].identities[0].provider" (see socialize.exportUsers API).</param>
        /// <param name="defaultValue">the value to be returned if the request params doesn't contain the specified key.</param>
        /// <returns>the value to which the specified key is mapped, or the defaultValue if the request params
        /// does not contain such a key</returns>
        public int GetInt(string key, int defaultValue)
        {
            if (data == null)
                throw new GSResponseNotInitializedException();
            return this.data.GetInt(key, defaultValue);
        }
        /// <summary>
        /// Returns the value to which the specified key inside the response params is associated with, or defaultValue
        /// if the response params does not contain such a key.
        /// </summary>
        /// <param name="key">the key whose associated value is to be returned.
        /// The key can specify a dot-delimited path down the objects hierarchy, with brackets to access array items.
        /// For example, "users[0].identities[0].provider" (see socialize.exportUsers API).</param>
        /// <param name="defaultValue">the value to be returned if the request params doesn't contain the specified key.</param>
        /// <returns>the value to which the specified key is mapped, or the defaultValue if the request params
        /// does not contain such a key</returns>
        public double GetDouble(string key, double defaultValue)
        {
            if (data == null)
                throw new GSResponseNotInitializedException();
            return this.data.GetDouble(key, defaultValue);
        }
        /// <summary>
        /// Returns the value to which the specified key inside the response params is associated with, or defaultValue
        /// if the response params does not contain such a key.
        /// </summary>
        /// <param name="key">the key whose associated value is to be returned.
        /// The key can specify a dot-delimited path down the objects hierarchy, with brackets to access array items.
        /// For example, "users[0].identities[0].provider" (see socialize.exportUsers API).</param>
        /// <param name="defaultValue">the value to be returned if the request params doesn't contain the specified key.</param>
        /// <returns>the value to which the specified key is mapped, or the defaultValue if the request params
        /// does not contain such a key</returns>
        public bool GetBool(string key, bool defaultValue)
        {
            if (data == null)
                throw new GSResponseNotInitializedException();
            return this.data.GetBool(key, defaultValue);
        }
        /// <summary>
        /// Returns the value to which the specified key inside the response params is associated with, or defaultValue
        /// if the response params does not contain such a key.
        /// </summary>
        /// <param name="key">the key whose associated value is to be returned.
        /// The key can specify a dot-delimited path down the objects hierarchy, with brackets to access array items.
        /// For example, "users[0].identities[0].provider" (see socialize.exportUsers API).</param>
        /// <param name="defaultValue">the value to be returned if the request params doesn't contain the specified key.</param>
        /// <returns>the value to which the specified key is mapped, or the defaultValue if the request params
        /// does not contain such a key</returns>
        public GSObject GetObject(string key, GSObject defaultValue)
        {
            if (data == null)
                throw new GSResponseNotInitializedException();
            return this.data.GetObject(key, defaultValue);
        }
        /// <summary>
        /// Returns the value to which the specified key inside the response params is associated with, or defaultValue
        /// if the response params does not contain such a key.
        /// </summary>
        /// <param name="key">the key whose associated value is to be returned.
        /// The key can specify a dot-delimited path down the objects hierarchy, with brackets to access array items.
        /// For example, "users[0].identities[0].provider" (see socialize.exportUsers API).</param>
        /// <param name="defaultValue">the value to be returned if the request params doesn't contain the specified key.</param>
        /// <returns>the value to which the specified key is mapped, or the defaultValue if the request params
        /// does not contain such a key</returns>
        public GSArray GetArray(string key, GSArray defaultValue)
        {
            if (data == null)
                throw new GSResponseNotInitializedException();
            return this.data.GetArray(key, defaultValue);
        }


        /// <summary>
        /// Gets one or more nested objects.
        /// </summary>
        /// 
        /// <typeparam name="T">The type of the object(s) to obtain. Can be either of: bool, bool?, int, int?, long,
        /// long?, decimal, decimal?, string, GSObject, GSArray. Note that if the object(s) pointed to by the path do
        /// not match this type, they won't be returned unless attempt_conversion was set to true. Also note that if
        /// the response contains a null value and you request a non-nullable data type (bool, int, etc) then that
        /// response value will be ignored. If you do request a nullable or class type (int?, GSObject, etc) then a
        /// null object will be returned.</typeparam>
        /// 
        /// <param name="path">A dot-delimited path down the objects hierarchy. You can access specific array items
        /// using bracket notation ([]). The brackets can specify an explicit index or '*' to traverse all elements.
        /// For example, if you call the socialize.exportUsers method (see http://developers.gigya.com/037_API_reference/020_REST_API/socialize.exportUsers),
        /// then the path "users[0].identities[0].provider" will return a list with a single item being the first user's
        /// first social provider. The path "users[*].UID" will return a list containing the UID field in each object in
        /// the users array.</param>
        /// 
        /// <param name="attempt_conversion">If true, and if you requested string results, then the internal response
        /// objects will be converted to strings (e.g. 2 --> "2"). This includes arrays and compound objects. If you
        /// requested a primitive type, a parse operation will be attempted (e.g. "2" --> 2).</param>
        /// 
        /// <returns>A list of values which match the path and whose type matches the template parameter.</returns>
        public IEnumerable<T> Get<T>(string path, bool attempt_conversion = false)
        {
            if (data == null)
                throw new GSResponseNotInitializedException();
            else return data.Get<T>(TokenizePath(path), 0, attempt_conversion);
        }

        #endregion


        /// <summary>
        /// Returns the result code of the operation. Code '0' indicates success, any other number indicates failure.
        /// For the complete list of server error codes, see 
        /// "http://wiki.gigya.com/030_API_reference/030_Response_Codes_and_Errors". 
        /// </summary>
        /// <returns>the error code</returns>
        public int GetErrorCode()
        {
            return this.errorCode;
        }

        /// <summary>
        /// Returns a short textual description of the response error, for logging purposes.
        /// </summary>
        /// <returns>the error message string</returns>
        public string GetErrorMessage()
        {
            return this.errorMessage;
        }

        /// <summary>
        /// Returns the raw response data. 
        /// The raw response data is in JSON format, by default. If the request was sent with the format parameter set to "xml",
        /// the raw response data will be in XML format.
        /// </summary>
        /// <returns>the raw response data</returns>
        public string GetResponseText()
        {
            return this.responseText;
        }

        /// <summary>
        /// Returns the response data in GSObject. 
        /// Please refer to Gigya's REST API reference (http://wiki.gigya.com/030_API_reference/020_REST_API), 
        /// for a list of response data structure per method request.
        /// Note: If the request was sent with the format parameter set to "xml", the getData() will return 
        /// null and you should use getResponseText() method instead. 
        /// We only parse response text into GSObject if the request format is "json", which is the default.
        /// </summary>
        /// <returns>a GSObject containing the response data</returns>
        public GSObject GetData()
        {
            return this.data;
        }

        public T GetData<T>() where T : class,new()
        {
            return this.data.Cast<T>();
        }

        public IDictionary<string, string> GetHeaders()
        {
            return this.headers;
        }

        internal static String GetErrorMessage(int errorCode)
        {
            if (errorMsgDic.ContainsKey(errorCode))
                return errorMsgDic[errorCode];
            else
                return String.Empty;

        }
        /*
        internal String GetErrorResponseText(int errorCode, String errorMessage)
        {
            return GSRequest.GetErrorResponseText(this.method, errorCode, errorMessage);
        }

        internal String GetErrorResponseText(int errorCode)
        {
            return GetErrorResponseText(errorCode, null);
        }*/

        internal static String GetErrorResponseText(String method, GSObject clientParams, int errorCode, String errorMessage)
        {
            if (errorMessage == null || errorMessage.Length == 0)
                errorMessage = GetErrorMessage(errorCode);

            String format = "json";
            if (clientParams != null)
                format = clientParams.GetString("format", "json");

            if (format.Equals("json"))
            {
                return "{errorCode:" + errorCode + ",errorMessage:\"" + errorMessage + "\"}";
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                sb.Append("<" + method + "Response xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"urn:com:gigya:api http://socialize-api.gigya.com/schema\" xmlns=\"urn:com:gigya:api\">");
                sb.Append("<errorCode>" + errorCode + "</errorCode>");
                sb.Append("<errorMessage>" + errorMessage + "</errorMessager>");
                sb.Append("</" + method + "Response>");
                return sb.ToString();
            }
        }
        /// <summary>
        /// Returns the trace log of the response.
        /// </summary>
        /// <returns></returns>
        public string GetLog()
        {
            return this.logger.ToString();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("\terrorCode:");
            sb.Append(errorCode);
            sb.Append("\n\terrorMessage:");
            sb.Append(errorMessage);
            sb.Append("\n\tdata:");
            sb.Append(data);
            return sb.ToString();
        }

        #endregion


        #region Private helper methods

        private string GetStringBetween(string source, string prefix, string suffix)
        {
            if (source == null || source.Length == 0) return null;

            int prefixStart = source.IndexOf(prefix);
            int suffixStart = source.IndexOf(suffix);

            if (prefixStart == -1 || suffixStart == -1) return null;

            string ret = source.Substring(prefixStart + prefix.Length, suffixStart - (prefixStart + prefix.Length));
            return ret;
        }

        static Regex path_regex = new Regex(@"^(((^|\.)(?<token>[^\.\[\]]+))|(?<token>\[(([0-9]+)|(\*))\]))*$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        static internal string[] TokenizePath(string path)
        {
            var matches = path_regex.Match(path);
            if (matches.Success)
                return matches.Groups["token"].Captures.Cast<Capture>().Select(a => a.Value).ToArray();
            else throw new ArgumentException("Invalid path format");
        }

        #region XmlToJSON
        /// <summary>
        /// converts xml string to GSObject
        /// </summary>
        /// <param name="responseText">XML string </param>
        /// <returns>GSObject based on the XML string</returns>
        private GSObject XmlToJSON(string responseText)
        {
            GSObject dictionary = new GSObject();
            try
            {
                XmlDocument document = new XmlDocument();
                document.LoadXml(responseText);

                XmlToDictionaryEntry(document.DocumentElement, dictionary);
            }
            catch (Exception)
            {
                dictionary = null;
            }
            return dictionary;
        }

        /// <summary>
        /// Adds XML node as a dictionary entry
        /// </summary>
        /// <param name="node">the node to convert</param>
        /// <param name="dic">the GSObject to add the converted node to</param>
        private void XmlToDictionaryEntry(XmlElement node, GSObject dic)
        {

            // Build a sorted list of key-value pairs
            //  where   key is case-sensitive nodeName
            //          value is an ArrayList of string or XmlElement
            //  so that we know whether the nodeName is an array or not.
            SortedList childNodeNames = new SortedList();

            //  Add in all nodes
            foreach (XmlNode cnode in node.ChildNodes)
            {
                if (cnode is XmlText)
                    StoreChildNode(childNodeNames, "value", cnode.InnerText);
                else if (cnode is XmlElement)
                    StoreChildNode(childNodeNames, cnode.Name, cnode);
            }

            foreach (string childname in childNodeNames.Keys)
            {
                ArrayList alChild = (ArrayList)childNodeNames[childname];
                if (alChild.Count == 1)
                    OutputNode(childname, alChild[0], dic, true);
                else
                {
                    GSArray arr = new GSArray();
                    dic.Put(node.Name, arr);
                    for (int i = 0; i < alChild.Count; i++)
                    {
                        GSObject cDic = new GSObject();
                        OutputNode(null, alChild[i], cDic, false);
                        arr[i] = cDic;
                    }
                    dic.Put(childname, arr);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="childname"></param>
        /// <param name="alChild"></param>
        /// <param name="dictionary"></param>
        /// <param name="showNodeName"></param>
        private void OutputNode(string childname, object alChild, GSObject dictionary, bool showNodeName)
        {
            if (alChild == null)
            {
                if (showNodeName)
                    dictionary.Put(childname, String.Empty);
            }
            else if (alChild is string)
            {
                if (showNodeName)
                    dictionary.Put(childname, alChild.ToString());
            }
            else
                XmlToDictionaryEntry((XmlElement)alChild, dictionary);
        }

        private void StoreChildNode(SortedList childNodeNames, string nodeName, object nodeValue)
        {
            ArrayList ValuesAL;

            // Pre-process contraction of XmlElement-s
            if (nodeValue is XmlElement)
            {
                // Convert  <aa></aa> into "aa":null
                //          <aa>xx</aa> into "aa":"xx"
                XmlNode cnode = (XmlNode)nodeValue;

                if (cnode.Attributes.Count == 0)
                {
                    XmlNodeList children = cnode.ChildNodes;
                    if (children.Count == 0)
                        nodeValue = null;
                    else if (children.Count == 1 && (children[0] is XmlText))
                        nodeValue = ((XmlText)(children[0])).InnerText;
                }
                if ((cnode.NextSibling != null && cnode.NextSibling.Name == cnode.Name) || (cnode.PreviousSibling != null && cnode.PreviousSibling.Name == cnode.Name))
                {
                    object o = childNodeNames[cnode.ParentNode.Name];

                    if (o == null)
                    {
                        ValuesAL = new ArrayList();
                        childNodeNames[cnode.ParentNode.Name] = ValuesAL;
                    }
                    else
                        ValuesAL = (ArrayList)o;
                    ValuesAL.Add(nodeValue);
                    return;
                }
            }



            // Add nodeValue to ArrayList associated with each nodeName
            // If nodeName doesn't exist then add it
            object oValuesAL = childNodeNames[nodeName];

            if (oValuesAL == null)
            {
                ValuesAL = new ArrayList();
                childNodeNames[nodeName] = ValuesAL;
            }
            else
                ValuesAL = (ArrayList)oValuesAL;
            ValuesAL.Add(nodeValue);
        }
        #endregion XmlToJSON
        #endregion
    }
}
