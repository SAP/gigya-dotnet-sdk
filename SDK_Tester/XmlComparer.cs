using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Windows.Forms;
using System.IO;
using System.Xml.Serialization;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using Gigya.Socialize.SDK.Tests.SDKTester;

namespace SDK_Tester    
{
	public class XmlComparer : IRestComparer
	{
		private string _lastKey = null;
		public bool ThrowException { get; set; }
		public XmlComparer()
		{
			Differences = new List<CompareResultInfo>();
		}
		#region IRestComparer Members

		public List<CompareResultInfo> Differences
		{
			get;
			private set;
		}

		public bool Compare(string expected, string result, string schema)
		{
			//normalize the string.
			expected = expected.Trim().Replace("\r\n", "").Replace("\\\"", "\\");
			result = result.Trim().Replace("\r\n", "").Replace("\\\"", "\\");
			XDocument xdocExpected = null, xdocResult = null;
			Dictionary<string, object> jsonSchema = null;
			try
			{
				xdocExpected = XDocument.Parse(expected);
				xdocResult = XDocument.Parse(result);
				jsonSchema = JsonComparer.DeserializeObject(schema);
				return Compare(xdocExpected.Document.FirstNode, xdocResult.Document.FirstNode, jsonSchema);
			}
			catch (Exception ex)
			{
				string cause = "Expected XML";
				if (xdocExpected != null && xdocResult == null)
					cause = "Result XML";
				else if (xdocResult != null && jsonSchema == null)
					cause = "Schema";
				else
					cause = "Internal Application Error";

				Differences.Add(new CompareResultInfo
				{
					Schema = cause + " - " + ex.Message,
					Field = "Application Error",
					ApplicationError = true
				});
				return false;
			}
		}
		#endregion

		public bool Compare(object oExpected, object oResult, object oSchema)
		{
			if (oExpected is XContainer)
			{
				var expected = oExpected as XContainer;
				var result = oResult as XContainer;

			
				var eEnu = expected.Elements().GetEnumerator();
				var rEnu = result == null ? null : result.Elements().GetEnumerator();

				while (eEnu.MoveNext())
				{
					if(rEnu != null)
						rEnu.MoveNext();

					var node = eEnu.Current;
					var resNode = rEnu == null ? null : rEnu.Current;

					var name = node.Name.LocalName.ToString();
					_lastKey = name;

					//xml object 
					if (node.Elements().Count() > 0)
					{
						var schema = oSchema as Dictionary<string, object>;
						var validator = schema != null && schema.ContainsKey(name) ? schema[name] : null;
						try
						{
							if (resNode == null)
							{
								var validatorResult = SchemaValidators.Validate((object)node, (object)resNode, oSchema != null && oSchema.GetType() == typeof(String) ? oSchema.ToString() : SchemaValidators.DefaultSchemaValidator);
								if (!validatorResult.Success)
								{
									if (ThrowException)
										throw new ApplicationException(validatorResult.Message);
									else
										Differences.Add(new CompareResultInfo
										{
											Field = _lastKey,
											Expected = node,
											Result = resNode == null ? null : (resNode as XElement).Value,
											Schema = SchemaValidators.DefaultSchemaValidator
										});
								}
							}
							else
								Compare(node, resNode, validator);
						}
						catch (Exception e)
						{
							if (ThrowException)
								throw new ApplicationException(String.Format("The failed key: {0} , {1}", node.Name, e.Message));
							else
								Differences.Add(new CompareResultInfo
								{
									Field = _lastKey,
									Result = resNode,
									Expected = node,
									Schema = SchemaValidators.DefaultSchemaValidator
								});
						}
					}
					//xml primitive
					else
					{
						var nExpected = node.Value;
						var schemaDict = oSchema as Dictionary<string, object>;
						var schema = schemaDict != null && schemaDict.ContainsKey(name) ? schemaDict[name].ToString() : SchemaValidators.DefaultSchemaValidator;
						var validatorResult = SchemaValidators.Validate(nExpected, resNode == null ? null : (resNode as XElement).Value, schema);
						if (!validatorResult.Success)
						{
							if (ThrowException)
								throw new ApplicationException(validatorResult.Message);
							else
								Differences.Add(new CompareResultInfo
								{
									Field = _lastKey,
									Expected = nExpected,
									Result = resNode == null ? null : (resNode as XElement).Value,
									Schema = schema
								});

						}
					}
				}

			}


			//xml primitive
			else
			{
				var expected = oExpected as XmlNode;
				var result = oResult as XmlNode;
				var schema = oSchema.ToString() == null ? SchemaValidators.DefaultSchemaValidator : oSchema.ToString();

				var validatorResult = SchemaValidators.Validate(expected.Value, result.Value, schema);
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


			return true;
		}

	}

	public class SchemaValidationError
	{
		public string Message { get; set; }
	}

	public class SchemaValidation
	{
		private string _schemaLocation;
		public List<SchemaValidationError> Errors { get; set; }

		public SchemaValidation(string schemaLocation)
		{
			_schemaLocation = schemaLocation;
			Errors = new List<SchemaValidationError>();
		}
		public bool Validate(TextReader tr)
		{
			bool success = validateXml(tr);
			return success;
		}

		private bool validateXml(TextReader tr)
		{
			var success = true;
			Errors.Clear();

			#region Validate
			XmlReaderSettings xrs = new XmlReaderSettings();
			xrs.ValidationType = ValidationType.Schema;
			xrs.ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings | XmlSchemaValidationFlags.ProcessIdentityConstraints;
			xrs.ValidationEventHandler += (o, e) =>
			{
				Errors.Add(new SchemaValidationError { Message = e.Message });
				success = false;
			};

			var xmlSchema = new XmlSchemaSet();
			string path = _schemaLocation;

			try
			{
				var xr = XmlReader.Create(path);
				xmlSchema.Add("urn:com:gigya:api", XmlReader.Create(path));

				xrs.Schemas.Add(xmlSchema);
			}
			catch (Exception)
			{
				//MessageBox.Show("Could not load schema - " + _schemaLocation);
				Errors.Add(new SchemaValidationError { Message = "Schema could not be loaded." });
				return false;
			}

			using (XmlReader xr = XmlReader.Create(tr, xrs))
			{
				while (xr.Read()) { }
			}
			#endregion

			return success;
		}
	}
}
