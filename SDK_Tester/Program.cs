using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Gigya.Socialize.SDK;
using System.Collections;
using System.Diagnostics;

namespace SDK_Tester
{
    static class Program
    {
        // Define the API-Key and Secret key (the keys can be obtained from your site setup page on Gigya's website).
        const string apiKey = "3_VhmtkuYIX72Jmmuxi1C4eRfjmO2CGnLu5YtcpCJuwpbec4hNkhStGDSm2T2sEuQj";
        const string secretKey = "AhtCX5+ke9FsTGLfhwnIZ6tVBV8F1ziVzXM0r/cSCXM=";

        private static readonly SignedRequestTester SignedRequestTester = new SignedRequestTester();

        public static Form1 MainForm { get; set; }

        [STAThread]
        static void Main()
        {
            //RunGUITester();
            //TestAsyncGSRequest();
            CastTester();
            GetUserInfoTester();
            SignedRequestTester.Run();
        }

        public class MyClass
        {
            public string StringField;
            public string StringProperty { get; set; }

            public int IntField;
            public int IntProperty { get; set; }

            public MyInnerClass MyInnerClassProperty { get; set; }
        }

        public class MyInnerClass
        {
            public string StringField;
            public string StringProperty { get; set; }

            public int IntField;
            public int IntProperty { get; set; }

            public MyClass MyClassProperty { get; set; }
            public List<MyClass> MyClassListProperty { get; set; }

            [GSObject.Ignore]
            public List<MyClass> MyClassListIgnoreProperty { get; set; }
        }

        public class GigyaResponse
        {
            public int errorCode;
            public string errorMessage;
            public string errorDetails;
        }

        public class GigyaRequestParams
        {
            //public string apiKey = "";
            //public string secretKey = "";
            //public bool useHTTPS = false;

            public string format = "json";
        }
        //[GSRequest.Method("socialize.getUserInfo")]
        public class GetUserInfoRequestParams : GigyaRequestParams
        {
            public string uid;
            public string enabledProviders;
        }

        static void CastTester()
        {
	        MyClass myClass = new MyClass()
		    {
			    IntField = 10,
			    IntProperty = 11,
				StringField = "ShacharField",
				StringProperty = "ShacharProperty",
			};
	        myClass.MyInnerClassProperty = new MyInnerClass();
	        myClass.MyInnerClassProperty.IntField = 12;
	        myClass.MyInnerClassProperty.MyClassListProperty = new List<MyClass>();
			myClass.MyInnerClassProperty.MyClassListProperty.Add(new MyClass { IntField = 13 });
            GSObject gsObj = new GSObject(myClass);

            MyClass clone = gsObj.Cast<MyClass>();

            bool equals = AreEquals(myClass, clone);
            Debug.Assert(equals, "objects comparison failed");

            gsObj.Put("StringProperty", new GSObject(new GetUserInfoRequestParams()));
            clone = gsObj.Cast<MyClass>();
            bool serializeToString = clone.StringProperty.Equals(new GSObject(new GetUserInfoRequestParams()).ToString());
            Debug.Assert(serializeToString, "object to string serialize is not working");
        }

        private static void GetUserInfoTester()
        {

            GetUserInfoRequestParams reqParams = new GetUserInfoRequestParams
            {
                uid = "31x31",
                enabledProviders = "*,testnetwork,testnetwork2"
            };


            // Typed Class
            GSRequest req = new GSRequest(apiKey, secretKey, "socialize.getUserInfo", reqParams, false);
            GSResponse res = req.Send();

            GigyaResponse response = res.GetData<GigyaResponse>();
            Debug.Assert(response.errorCode > 0, "failed to cast to GigyaResponse #1");

            // GS Object
            req = new GSRequest(apiKey, secretKey, "socialize.getUserInfo", new GSObject(reqParams), false);
            res = req.Send();

            response = res.GetData<GigyaResponse>();
            Debug.Assert(response.errorCode > 0, "failed to cast to GigyaResponse #2");

            // Anonymous type
            req = new GSRequest(apiKey, secretKey, "socialize.getUserInfo", new
            {
                format = "json",
                uid = "31x31",
                arr = new List<string> { "1", "2", "3" },
                myClass = new MyClass(),
                myClassList = new List<MyClass> { new MyClass() },
                enabledProviders = "*,testnetwork,testnetwork2"
            }, false);

            res = req.Send();

            // Cast to GigyaResponse
            response = res.GetData<GigyaResponse>();
            Debug.Assert(response.errorCode > 0, "failed to cast to GigyaResponse #3");
        }

        private static bool AreEquals<T>(T a, T b)
        {
            return AreEquals(typeof(T), a, b);
        }

        private static bool AreEquals(Type type, object a, object b)
        {
            Console.WriteLine(type.Name);
            bool equals = true;
            if (type.GetInterfaces().Contains(typeof(IList)))
            {
                var aList = a as IList;
                var bList = b as IList;

                if (aList.Count != bList.Count)
                    return false;

                var aEnu = aList.GetEnumerator();
                var bEnu = bList.GetEnumerator();
                while (aEnu.MoveNext())
                {
                    if (!bEnu.MoveNext())
                    {
                        equals = false;
                        break;
                    }
                    if ((null == bEnu.Current && null != aEnu.Current) || (null == aEnu.Current && null != bEnu.Current))
                        equals = false;
                    else
                        equals &= AreEquals(bEnu.Current.GetType(), aEnu.Current, bEnu.Current);
                }
            }
            else
            {



                if (!equals) return false;

                var fields = type.GetFields();
                var properties = type.GetProperties().Where(x => x.CanRead && x.CanWrite);

                foreach (var field in fields)
                {
                    var aVal = field.GetValue(a);
                    var bVal = field.GetValue(b);
                    if (field.FieldType.IsPrimitive || field.FieldType == typeof(String))
                    {
                        if (!(null == aVal && null == bVal))
                            equals &= aVal.Equals(bVal);
                    }
                    else
                    {
                        if (!(null == aVal && null == bVal))
                            equals &= AreEquals(field.FieldType, aVal, bVal);
                    }
                }

                foreach (var property in properties)
                {
                    var aVal = property.GetValue(a, null);
                    var bVal = property.GetValue(b, null);
                    if (property.PropertyType.IsPrimitive || property.PropertyType == typeof(String))
                    {
                        if (!(null == aVal && null == bVal))
                            equals &= aVal.Equals(bVal);
                    }
                    else
                    {
                        if (!(null == aVal && null == bVal))
                            equals &= AreEquals(property.PropertyType, aVal, bVal);
                    }
                }
            }
            return equals;
        }


        static void RunGUITester()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            MainForm = new Form1();
            Application.Run(MainForm);
        }


        static void TestGSObjectAndGSArray()
        {
            // Test object
            string st1 = "{'key1': 1.1, 'item1':'ITEM1', 'key2': {'item4':'ITEM4'}, 'array1':[1.3,'hello',{'item2':'ITEM2'},[11,12],[{'item3':'ITEM3'}]]}";
            GSObject gsObj = new GSObject(st1);
            st1 = gsObj.ToString();


            // Test array
            string st2 = "[1, 1.2, 'hello', true, null, {}, {'key1':'val1'}, [11, 22, 33, 33.3, false, null, {}, {'key2':'val2', 'key3':[111,222,333]}]]";
            GSArray gsArr = new GSArray(st2);
            st2 = gsArr.ToString();

            // Test extracting values from object
            object y = gsObj.GetArray("array1").GetArray(4).GetObject(0);
            foreach (var item in gsObj.GetArray("array1"))
            {
                object x = item.ToString();
            }
        }

        static void TestAsyncGSRequest()
        {
            string method = "socialize.getUserInfo";

            // Should give response from server
            GSRequest request = new GSRequest(apiKey, secretKey, method, false);
            request.SetParam("uid", "3111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111");
            request.SetParam("enabledProviders", "*,testnetwork,testnetwork2");
            request.APIDomain = "";

            GSResponse response = null;
            IAsyncResult iar = request.BeginSend((IAsyncResult innerIAsyncResult) =>
            {
                response = request.EndSend(innerIAsyncResult);
            }, 123);

            iar.AsyncWaitHandle.WaitOne();

            System.Console.WriteLine("Response: " + response.GetResponseText());
            // Should fail before getting to server

            //request = new GSRequest(apiKey, secretKey+"123", method, false);
            //request.SetParam("uid", "di1");
            //request.SetParam("enabledProviders", "*,testnetwork,testnetwork2");

            //response = null;
            //iar = request.BeginSend((IAsyncResult innerIAsyncResult) => {
            //    response = request.EndSend(innerIAsyncResult);
            //}, 123);

            //iar.AsyncWaitHandle.WaitOne();
        }


        static void TestGetUserInfo(string[] args)
        {
            // Step 1 - Defining the request
            string method = "socialize.getUserInfo";
            GSRequest request = new GSRequest(apiKey, secretKey, method, true);

            // Step 2 - Adding parameters
            request.SetParam("uid", "di1");  // set the "uid" parameter to user's ID
            request.SetParam("enabledProviders", "*,testnetwork,testnetwork2");
            request.SetParam("status", "I feel great 22222");  // set the "status" parameter to "I feel great"

            // Step 3 - Sending the request
            GSResponse response = request.Send();

            bool validate = SigUtils.ValidateUserSignature(
                response.GetString("UID", ""),
                response.GetString("signatureTimestamp", ""), secretKey,
                response.GetString("UIDSignature", ""));

            // Step 4 - handling the request's response.
            if (response.GetErrorCode() == 0)
            {    // SUCCESS! response status = OK   
                Console.WriteLine("Success in setStatus operation.");
            }
            else
            {  // Error
                Console.WriteLine("Got error on setStatus: {0}", response.GetErrorMessage());
            }
        }
    }
}

