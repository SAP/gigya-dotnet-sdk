using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Collections;
using System.Reflection;

namespace SDK_Tester
{
	public interface IRestComparer
	{
		List<CompareResultInfo> Differences { get; }
		bool Compare(string expected,string result,string schema);
	}

	public class JsonComparer : IRestComparer
	{

		public JsonComparer()
		{
			Differences = new List<CompareResultInfo>();
		}
		private string _lastKey = null;

		public List<CompareResultInfo> Differences
		{
			get;
			private set;
		}

		public bool ThrowException { get; set; }


		public static JavaScriptSerializer jss = new JavaScriptSerializer();
		public static Dictionary<string, object> DeserializeObject(string jsonString)
		{
			return jss.DeserializeObject(jsonString) as Dictionary<string, object>;
		}

		public static string SerializeObject(object o){
			return jss.Serialize(o);
		}

		public bool Compare(string expected, string result, string schema)
		{

			//normalize the string.
			expected = expected.Replace("\r\n", "");
			Dictionary<string, object> jsonExp = null;
				Dictionary<string, object> jsonRes = null;
				Dictionary<string, object> jsonSchema = null;
			try
			{
				jsonExp = jss.DeserializeObject(expected) as Dictionary<string, object>;
				jsonRes = jss.DeserializeObject(result) as Dictionary<string, object>;
				jsonSchema = string.IsNullOrEmpty(schema) ? null : jss.DeserializeObject(schema) as Dictionary<string, object>;
				return Compare(jsonExp, jsonRes, jsonSchema);
			}
			catch (Exception ex)
			{
				string cause = "Expected XML";
				if (jsonExp != null && jsonRes == null)
					cause = "Result XML";
				else if (jsonRes != null && jsonSchema == null)
					cause = "Schema";
				else
					cause = "Internal Application Error";
				Differences.Add(new CompareResultInfo
				{
					Schema = cause + " - "+ex.Message,
					Field = "Application Error",
					ApplicationError = true
				});
				return false;
			}
		}

		public bool Compare(object expected, object result, object oSchema)
		{

			// this method can get 3 types:
			//1. Dictionary<string,object> when jsonObject
			//2. object[] (when jsonArray),
			//3. Primitive (string,int etc...),
			Type expValType = expected.GetType();

			

			//case when its a jsonObject.
			if (expected is Dictionary<string, object>)
			{
				Dictionary<string, object> dictExp = expected as Dictionary<string, object>;
				Dictionary<string, object> dictRes = result as Dictionary<string, object>;

				if (dictRes == null)
				{
					var validatorResult = SchemaValidators.Validate(dictExp, dictRes, oSchema != null && oSchema.GetType() == typeof(String)? oSchema.ToString() : SchemaValidators.DefaultSchemaValidator);
					if (!validatorResult.Success)
					{
						if (ThrowException)
							throw new ApplicationException(validatorResult.Message);
						else
							Differences.Add(new CompareResultInfo
							{
								Field = _lastKey,
								Expected = SerializeObject(dictExp),
								Result = dictRes,
								Schema = SchemaValidators.DefaultSchemaValidator
							});
					}
					return false;
				}
						
				var schema = oSchema as Dictionary<string, object>;

				foreach (KeyValuePair<string, object> kvp in dictExp)
				{
					_lastKey = kvp.Key;
					object resValue = null;
					if (dictRes!= null && dictRes.ContainsKey(kvp.Key))
					{
						resValue = dictRes[kvp.Key];
					}
					else
					{
						resValue = null;
					}

					object expValue = dictExp[kvp.Key];
					var validator = schema != null && schema.ContainsKey(kvp.Key) ? schema[kvp.Key].ToString() : SchemaValidators.DefaultSchemaValidator;
					try
					{
						Compare(expValue, resValue, validator);
					}
					catch (Exception e)
					{
						if (ThrowException)
							throw new ApplicationException(String.Format("The failed key: {0} , {1}", kvp.Key, e.Message));
						else
							Differences.Add(new CompareResultInfo
							{
								Field = _lastKey,
								Result = resValue,
								Expected = expValue, 
								Schema = validator
							});
					}
				}
			}

			//case when jsonArray.
			else if (expected is object[])
			{
				var schema = oSchema as Dictionary<string, object>;

				IEnumerable enumerableExp = expected as IEnumerable;
				IEnumerator enuExp = enumerableExp.GetEnumerator();

				IEnumerable enumerableRes = result as IEnumerable;
				IEnumerator enuRes = enumerableRes == null ? null : enumerableRes.GetEnumerator();

				//there are 2 cases:
				// - one that should not happen ,
				//   when the items in the array are not primitive type we should do something else.

				// - if its primitive type than we should validate the array not the items in the array.
				while (enuExp.MoveNext())
				{
					if (enuRes != null && enuRes.MoveNext())
						Compare(enuExp.Current, enuRes.Current, schema);
					else
						Compare(enuExp.Current, null, schema);
				}
			}
			//probably primitive type.
			else if (expValType.IsPrimitive || expValType == typeof(String))
			{
				var schema = oSchema != null ? oSchema.ToString() : SchemaValidators.DefaultSchemaValidator;
				var validatorResult = SchemaValidators.Validate(expected, result, schema);
				if (!validatorResult.Success)
				{
					if (ThrowException)
						throw new ApplicationException(validatorResult.Message);
					else
						Differences.Add(new CompareResultInfo
						{
							Field = _lastKey,
							Result = result,
							Expected = expected,
							Schema = schema
						});

				}
			}
			else
			{
				throw new ApplicationException("Unknown compare senario!");
			}

			return true;

		}
	}

	public class CompareResultInfo
	{
		public bool ApplicationError { get; set; }
		public string Field { get; set; }
		public object Result { get; set; }
		public object Expected { get; set; }
		public string Schema { get; set; }
	}

	public class ValidateResult
	{
		public bool Success { get; set; }
		public string Message { get; set; }
	}

	public static class SchemaValidators
	{
		private static Type _type;
		private static MethodInfo[] _methods;
		static SchemaValidators()
		{
			_type = typeof(SchemaValidators);
			_methods = _type.GetMethods(BindingFlags.Static | BindingFlags.Public);
		}
		public static string DefaultSchemaValidator = "*";
		public static ValidateResult Validate(object exp, object res, string schema)
		{
			string name = "";
			if (schema == null || schema.ToString() == "*" || schema.ToString().Trim() == string.Empty) name = DefaultSchemaValidator; //default compare

			else if (schema.ToString().Trim() != string.Empty) name = schema.ToString().Trim();

			MethodInfo method = _methods
									.Where
									(m => m.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)
										||
										m.IsDefined(typeof(SchemaAttribute), false) &&
											((SchemaAttribute)m.GetCustomAttributes(typeof(SchemaAttribute), false).First())
												.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)
									 )
									.FirstOrDefault();
			if (method == null)
				throw new NotImplementedException(name);

			var ret = method.Invoke(null, new object[] { exp, res });
			return (ValidateResult)ret;
		}
		[Schema("ignore")]
		public static ValidateResult Ignore(object exp, object res)
		{
			ValidateResult ret = new ValidateResult();
			ret.Success = true;
			return ret;
		}
		[Schema("*")]
		public static ValidateResult Required(object exp, object res)
		{
			ValidateResult ret = new ValidateResult();
			if (res != null)
			{
				ret.Success = true;
			}
			else
			{
				ret.Success = false;
				ret.Message = "The return value was empty";
			}

			return ret;
		}

		[Schema("same")]
		public static ValidateResult Same(object exp, object res)
		{
			ValidateResult ret = new ValidateResult();
			if (exp is object[])
			{
				if (!(res is object[]))
				{
					ret.Success = false;
				}

				//make sure that the array are the same, ignoring order.
				List<object> expList = new List<object>((object[])exp);
				List<object> resList = new List<object>((object[])res);
				ret.Success = true;

				foreach (var item in expList)
				{
					if (!resList.Contains(item))
					{
						ret.Success = false;
						break;
					}
				}

			}
			else
			{
				ret.Success = exp.Equals(res);
			}

			if (!ret.Success)
			{
				ret.Message = string.Format("Comparison of expected value and result value failed: {0} != {1}", exp, res);
			}
			return ret;
		}

	}

	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
	sealed class SchemaAttribute : Attribute
	{

		public string Name { get; set; }
		public SchemaAttribute(string name)
		{
			Name = name;
		}

	}

}
