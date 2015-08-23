using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using System.Threading;
using System.Reflection;
using System.Diagnostics;
using SDK_Tester.Properties;
using Gigya.Socialize.SDK;
using Gigya.Socialize.SDK.Tests.SDKTester;
using SDK_Tester;

namespace SDK_Tester
{
    public partial class Form1 : Form
    {
        #region members

        private static List<PropertyInfo> _methodProps = new List<PropertyInfo>();
        private static List<PropertyInfo> _testRunProps = new List<PropertyInfo>();
        private bool verifySignature;
        private TesterHelper _helper = new TesterHelper();

        public AutomationHelper.TestRunResult LastResult { get; set; }
        public AutomationHelper.TestRun TRun = null;
        public static string unreservedCharsstring = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";
        public static char[] unreservedChars;

        #endregion

        #region Ctor
        static Form1()
        {
            unreservedChars = unreservedCharsstring.ToCharArray();
            Array.Sort(unreservedChars);
            _methodProps = typeof(AutomationHelper.MethodResult).GetProperties().Where(p => p.GetCustomAttributes(typeof(AutomationHelper.ExportToCSVAttribute), false).Count() > 0).ToList();
            _testRunProps = typeof(AutomationHelper.TestRunResult).GetProperties().Where(p => p.GetCustomAttributes(typeof(AutomationHelper.ExportToCSVAttribute), false).Count() > 0).ToList();
        }

        public Form1()
        {
            InitializeComponent();
        }
        #endregion

        private void Form1_Load(object sender, EventArgs e)
        {
            this.bindForm();
            this.loadVersionTab();
        }

        private void bindForm()
        {
            this.cbMethods.DisplayMember = "Text";
            this.cbMethods.ValueMember = "Value";
            foreach (var method in this._helper.Methods)
                this.cbMethods.Items.Add(method);

            this.txtAPIKey.Text = Settings.Default.APIKey;
            this.txtSecKey.Text = Settings.Default.SecKey;
            this.txtUID.Text = Settings.Default.UID;
            this.txtAutomationFileName.Text = Settings.Default.LastTestRun;
            this.lblVerifiedSig.Text = "  Please select a method";
            this.txtApiDomain.Text = Settings.Default.ApiDomain;

            this.resultsDataGridView.CellDoubleClick += new DataGridViewCellEventHandler(this.resultsDataGridView_CellClick);
            this.automationHelper_MethodDataGridView.CellContentClick += new DataGridViewCellEventHandler(automationHelper_MethodDataGridView_CellContentClick);
            this.loadTRun();
        }

        private void loadVersionTab()
        {
            var ver = Assembly.GetExecutingAssembly().GetName().Version;
            this.Text = String.Format(this.Text, ver.Major + "." + ver.Minor + "." + ver.Revision);
        }

        #region Assembly Attribute Accessors

        public string AssemblyTitle
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                if (attributes.Length > 0) {
                    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                    if (titleAttribute.Title != "") {
                        return titleAttribute.Title;
                    }
                }
                return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
            }
        }

        public string AssemblyVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        public string AssemblyDescription
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
                if (attributes.Length == 0) {
                    return "";
                }
                return ((AssemblyDescriptionAttribute)attributes[0]).Description;
            }
        }

        public string AssemblyProduct
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                if (attributes.Length == 0) {
                    return "";
                }
                return ((AssemblyProductAttribute)attributes[0]).Product;
            }
        }

        public string AssemblyCopyright
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                if (attributes.Length == 0) {
                    return "";
                }
                return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
            }
        }

        public string AssemblyCompany
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
                if (attributes.Length == 0) {
                    return "";
                }
                return ((AssemblyCompanyAttribute)attributes[0]).Company;
            }
        }
        #endregion

        private void automationHelper_MethodDataGridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            var cell = (automationHelper_MethodDataGridView.Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewCheckBoxCell);
            if (cell == null) return;
            var val = (cell.EditedFormattedValue as Boolean?);
            (automationHelper_MethodBindingSource.Current as AutomationHelper.Method).Include = val.HasValue ? val.Value : false;
        }

        private void loadTRun()
        {
            if (!string.IsNullOrEmpty(txtAutomationFileName.Text)) {
                TRun = AutomationHelper.TestRun.LoadFromFile(txtAutomationFileName.Text);
                automationHelper_MethodBindingSource.DataSource = TRun.Methods;
                cbHTTP_HTTPS.Text = TRun.GetSecure().HasValue && TRun.GetSecure().Value ? "https" : "http";
            }
        }

        private void paintDataGridRows()
        {
            var ds = (resultsBindingSource.DataSource as List<AutomationHelper.MethodResult>);
            var index = -1;
            foreach (var item in ds) {
                index++;
                if (item.ErrorCode != 0) {

                    if (item.Differences.Count() != 0 && item.Differences.First().ApplicationError) {
                        resultsDataGridView.Rows[index].DefaultCellStyle.BackColor = Color.Yellow;
                    }
                    else
                        resultsDataGridView.Rows[index].DefaultCellStyle.BackColor = Color.FromArgb(255, 102, 102);
                }
                else if (!item.Validation) {
                    resultsDataGridView.Rows[index].DefaultCellStyle.BackColor = Color.SlateGray;
                }
            }
        }

        private void resultsDataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            ResponseViewer viewer = new ResponseViewer();
            viewer.SetText((resultsBindingSource.Current as AutomationHelper.MethodResult).Response.GetResponseText());
            viewer.Owner = this;
            viewer.ShowDialog();
        }
        
        private void btnSubmit_Click(object sender, EventArgs e)
        {
            Settings.Default.Save();

            string format;
            try {
                lblVerifiedSig.ForeColor = Color.Blue;
                btnSubmit.Text = "Loading...";
                btnSubmit.Enabled = false;
                string methodName = cbMethods.Text;
                verifySignature = false;
                if (!string.IsNullOrEmpty(methodName)) {
                    const string socializeDot = "socialize.";
                    if (!methodName.StartsWith(socializeDot, StringComparison.InvariantCultureIgnoreCase)) {
                        methodName = socializeDot + methodName;
                    }

                    if (methodName.IndexOf("getUserInfo", StringComparison.InvariantCultureIgnoreCase) > -1) {
                        verifySignature = true;
                    }
                    else if (methodName.IndexOf("getFriendsInfo", StringComparison.InvariantCultureIgnoreCase) > -1) {
                        verifySignature = true;
                    }
                }

                lblVerifiedSig.Text = "Please wait...";
                webBrowser1.Hide();
                this.Refresh();
                GSResponse res = _helper.Send(txtUID.Text, txtAPIKey.Text, txtSecKey.Text, methodName, txtQS.Text, chkHTTPS.Checked, txtApiDomain.Text, out format);
                HandleGSResponse(res, format);
            }
            catch (Exception ex) {
                //raise error.
                MessageBox.Show(ex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnSubmit.Text = "Submit";
                lblVerifiedSig.Text = "  <-- Please select a method";
                lblVerifiedSig.ForeColor = Color.Blue;
                btnSubmit.Enabled = true;
            }
        }

        private void verifyResponseSignature(GSResponse res, string format)
        {
            if (!format.Equals("xml", StringComparison.InvariantCultureIgnoreCase)) {
                GSObject data = res.GetData();
                if (null != data) {
                    if (cbMethods.Text.IndexOf("getUserInfo", StringComparison.InvariantCultureIgnoreCase) > -1) {
                        string uid = data.GetString("UID", "");
                        string uidSig = data.GetString("UIDSignature", "");
                        string sigTimestamp = data.GetString("signatureTimestamp", "");
                        if (SigUtils.ValidateUserSignature(uid, sigTimestamp, txtSecKey.Text, uidSig)) {
                            lblVerifiedSig.Text = "Signature is verified";
                            lblVerifiedSig.ForeColor = Color.Green;
                        }
                        else {
                            lblVerifiedSig.Text = "Invalid signature !!!";
                            lblVerifiedSig.ForeColor = Color.Red;
                        }
                    }


                    if (cbMethods.Text.IndexOf("getFriendsInfo", StringComparison.InvariantCultureIgnoreCase) > -1) {

                        GSArray friends = data.GetArray("friends");
                        if (null != friends && friends.Length > 0) {
                            GSObject firstFriend = friends.GetObject(0);
                            string friendSig = firstFriend.GetString("friendshipSignature");
                            string tsSig = firstFriend.GetString("signatureTimestamp");
                            string friendUID = firstFriend.GetString("UID");
                            if (SigUtils.ValidateFriendSignature(txtUID.Text, tsSig, friendUID, txtSecKey.Text, friendSig)) {
                                lblVerifiedSig.Text = "1ST friend's signature is verified";
                                lblVerifiedSig.ForeColor = Color.Green;
                            }
                            else {
                                lblVerifiedSig.Text = "Invalid signature (1ST friend's) !!!";
                                lblVerifiedSig.ForeColor = Color.Red;
                            }
                        }
                    }
                }
            }
        }

        private void HandleGSResponse(GSResponse res, string format)
        {
            lblVerifiedSig.Text = "";
            if (!format.Equals("xml", StringComparison.InvariantCultureIgnoreCase)) {
                format = "txt";
            }

            var filename = Path.Combine(Path.GetTempPath(), "1." + format);

            try {
                if (res.GetErrorCode() > 0) {
                    lblVerifiedSig.ForeColor = Color.Red;
                    lblVerifiedSig.Text = "Error !!!";
                    File.WriteAllText(filename, res.ToString());
                }
                else {
                    if (verifySignature) {
                        verifyResponseSignature(res, format);
                    }
                    else {
                        lblVerifiedSig.Text = "Data is ready";
                    }
                    File.WriteAllText(filename, res.GetResponseText());
                }
                webBrowser1.Navigate(filename);
                webBrowser1.Show();
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            btnSubmit.Text = "Submit";
            btnSubmit.Enabled = true;
        }

        private void cbMethods_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbMethods.SelectedIndex == -1) return;
            var method = (cbMethods.SelectedItem as TesterHelper.Method);
            if (method == null) return;

            txtQS.Text = method.Value;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Settings.Default.Save();
        }
              
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "XML files (*.xml)|*.xml";
            openFileDialog1.RestoreDirectory = true;
            var ret = openFileDialog1.ShowDialog();
            if (ret == DialogResult.OK) {
                Settings.Default.LastTestRun = openFileDialog1.FileName;
                Settings.Default.Save();
                txtAutomationFileName.Text = openFileDialog1.FileName;
                loadTRun();
            }


        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            toolStripButton1_Click(sender, e);
        }

        private void btnRunSelected_Click(object sender, EventArgs e)
        {
            var testRun = AutomationHelper.TestRun.LoadFromFile(txtAutomationFileName.Text);
            if (testRun == null) return;

            tabTestRun.SelectedIndex = 1;

            automationHelper_MethodBindingSource.ResetBindings(true);
            automationHelper_MethodBindingSource.ResumeBinding();
            var methods = automationHelper_MethodBindingSource.DataSource as List<AutomationHelper.Method>;
            if (TRun != null) {
                foreach (var method in methods) {
                    var fMethod = testRun.Methods.Where(m => m.TestName.Equals(method.TestName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                    if (fMethod != null) {
                        fMethod.Include = method.Include;
                    }
                }

                TRun = testRun;
            }

            testRun.RunAll = false;

            btnRunAll.Enabled = false;
            btnRunSelected.Enabled = false;
            var secure = cbHTTP_HTTPS.Text.Equals("https", StringComparison.InvariantCultureIgnoreCase);
            testRun.Run(secure, res => {

                resultsBindingSource.DataSource = res.Results;
                resultsBindingSource.ResetBindings(true);
                resultsBindingSource.ResumeBinding();
                paintDataGridRows();
            },
            (res) => {
                this.LastResult = res;
                btnRunAll.Enabled = true;
                btnRunSelected.Enabled = true;

            });
        }

        private void btnRunAll_Click(object sender, EventArgs e)
        {
            var testRun = AutomationHelper.TestRun.LoadFromFile(txtAutomationFileName.Text);
            if (testRun == null) return;

            tabTestRun.SelectedIndex = 1;
            testRun.RunAll = true;

            btnRunAll.Enabled = false;
            btnRunSelected.Enabled = false;
            var secure = cbHTTP_HTTPS.Text.Equals("https", StringComparison.InvariantCultureIgnoreCase);
            testRun.Run(secure, res => {
                resultsBindingSource.DataSource = res.Results;
                resultsBindingSource.ResetBindings(true);
                resultsBindingSource.ResumeBinding();
                paintDataGridRows();
            },
            (res) => {
                this.LastResult = res;
                btnRunAll.Enabled = true;
                btnRunSelected.Enabled = true;

            });
        }

        private void toolStripButton5_Click_1(object sender, EventArgs e)
        {

            try {
                StringBuilder sb = new StringBuilder();
                var res = resultsBindingSource.DataSource as List<AutomationHelper.MethodResult>;

                String comma = "";
                foreach (var p in _methodProps) {
                    sb.Append(comma + p.Name);
                    comma = ", ";
                }
                sb.Append("\n");
                comma = "";
                foreach (var m in res) {
                    foreach (var p in _methodProps) {
                        object ret = null;
                        try {
                            ret = p.GetValue(m, null);

                        }
                        catch (Exception) {

                        }
                        if (ret == null) ret = "";
                        sb.Append(comma + "\"" + ret.ToString() + "\"");
                        comma = ",";
                    }
                    sb.Append("\n");
                    comma = "";
                }

                var methodResult = new AutomationHelper.MethodResult();
                methodResult.TestName = "Total";
                methodResult.RunStarted = LastResult.RunStarted;
                methodResult.RunEnded = LastResult.RunEnded;
                sb.Append("\n");
                comma = "";
                foreach (var p in _methodProps) {
                    object ret = null;
                    try {
                        ret = p.GetValue(methodResult, null);

                    }
                    catch (Exception) {

                    }
                    if (ret == null) ret = "";
                    sb.Append(comma + "\"" + ret.ToString() + "\"");
                    comma = ",";
                }

                if (!string.IsNullOrEmpty(Settings.Default.ExportFilename))
                    saveFileDialog1.InitialDirectory = Settings.Default.ExportFilename;

                saveFileDialog1.FileName = "AutoTest_Results_" + DateTime.Now.ToString().Replace(" ", "_").Replace(",", "").Replace("/", "-").Replace(":", ".") + ".csv";
                saveFileDialog1.RestoreDirectory = true;

                openFileDialog1.Filter = "CSV files (*.csv)|*.csv";
                if (saveFileDialog1.ShowDialog() == DialogResult.OK) {
                    File.WriteAllText(saveFileDialog1.FileName, sb.ToString());

                    Settings.Default.ExportFilename = new FileInfo(saveFileDialog1.FileName).Directory.FullName;
                    Settings.Default.Save();

                    Process.Start(saveFileDialog1.FileName);
                }
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message, "Failed to export to CSV");
            }
        }

        private void automationHelper_MethodBindingSource_CurrentChanged(object sender, EventArgs e)
        {

        }

        private void tabTestRun_Selected(object sender, TabControlEventArgs e)
        {
            this.ts.Visible = true;
        }
    }



    public class TesterHelper
    {
        //fields
        private List<Method> _methods = new List<Method>();

        //props
        public List<Method> Methods
        {
            get { return _methods; }
        }
        
        //ctor
        public TesterHelper()
        {
            var filename = "methods.xml";
            if (!File.Exists(filename)) return;

            //read xml.
            var xml = File.ReadAllText(filename);
            var xDoc = XDocument.Parse(xml);

            //<root><NAME OF METHOD WITH NAMESPACE>QUERY STRING</NAME OF METHOD WITH NAMESPACE></root>
            var methods = xDoc.Root.Elements();
            foreach (var m in methods) {
                var method = new Method {
                    Text = m.Name.ToString(),
                    Value = m.Value
                };

                _methods.Add(method);
            }
        }

        public GSResponse Send(string uid, string apiKey, string secretKey, string apiMethod, string json, bool useHTTPS, string apiDomain, out string format)
        {

            GSObject dict = new GSObject(json);
            dict.Put("uid", uid);
            format = dict.GetString("format", "json");
            GSRequest req = new GSRequest(apiKey, secretKey, apiMethod, dict, useHTTPS);
            req.APIDomain ="";
            return req.Send();
        }

        public class Method
        {
            public string Text { get; set; }
            public string Value { get; set; }
        }

        public static string UrlEncode(string value)
        {
            System.Text.StringBuilder result = new System.Text.StringBuilder();
            char[] c = new char[1];

            foreach (char symbol in value) {
                if (Array.BinarySearch<char>(Form1.unreservedChars, symbol) >= 0) {
                    result.Append(symbol);
                }
                else {
                    c[0] = symbol;
                    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(c);
                    foreach (byte b in bytes) {
                        result.Append('%' + String.Format("{0:X2}", (int)b));
                    }
                }
            }

            return result.ToString();
        }
    }



    public class AutomationHelper
    {
        [XmlRoot("testRun")]
        public class TestRun
        {
            [XmlIgnore]
            public bool RunAll { get; set; }

            [XmlAttribute("secure")]
            public string Secure { get; set; }

            [XmlAttribute("apikey")]
            public string APIKey { get; set; }

            [XmlAttribute("seckey")]
            public string SecKey { get; set; }

            public bool? GetSecure()
            {
                if (String.IsNullOrEmpty(Secure)) return null;
                if (Secure.Equals("false", StringComparison.InvariantCultureIgnoreCase)) {
                    return false;
                }
                else if (Secure.Equals("true", StringComparison.InvariantCultureIgnoreCase)) {
                    return true;
                }
                else {
                    bool b;
                    if (Boolean.TryParse(Secure, out b)) {
                        return b;
                    }
                }
                return null;
            }

            public static TestRun LoadFromFile(string path)
            {
                if (File.Exists(path)) {
                    using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read)) {
                        try {
                            return (TestRun)new XmlSerializer(typeof(TestRun)).Deserialize(stream);
                        }
                        catch (Exception) {
                            MessageBox.Show("Xml is not valid");
                            return null;
                        }
                    }
                }
                else
                    return null;
            }

            [XmlElement("method")]
            public List<Method> Methods { get; set; }

            public void Run(bool secure, Action<TestRunResult> cb, Action<TestRunResult> onDone)
            {
                if (this.Methods == null) {
                    onDone(null);
                    return;
                }

                ThreadPool.QueueUserWorkItem(o => RunAsync(secure, cb, onDone));
            }

            private string getEncodedValues(string paramsString)
            {
                string ret = "";

                // Split by &
                string[] keyValuePairs = paramsString.Split(new char[] { '&' });
                // For every pair
                for (int i = 0; i < keyValuePairs.Length; i++) {
                    int indexOf = keyValuePairs[i].IndexOf("=");
                    if (indexOf == -1) continue;
                    string key = keyValuePairs[i].Substring(0, indexOf);
                    string value = keyValuePairs[i].Substring(indexOf + 1);
                    ret += key + "=" + GSRequest.UrlEncode(value) + "&";
                }
                ret = ret.Remove(ret.Length - 1);
                return ret;
            }

            private void RunAsync(bool secure, Action<TestRunResult> cb, Action<TestRunResult> onDone)
            {
                try {

                    var ret = new TestRunResult();
                    ret.RunStarted = DateTime.Now;
                    ret.Results = new List<MethodResult>();


                    var methods = this.Methods;
                    if (!this.RunAll) {
                        methods = this.Methods.Where(m => m.Include).ToList();
                    }

                    foreach (var method in methods) {
                        try {
                            if (method.Params == null) method.Params = string.Empty;

                            var methodRes = new MethodResult();
                            methodRes.RunStarted = DateTime.Now;
                            methodRes.TestName = method.TestName;
                            methodRes.TestDescription = method.TestDescription;

                            var dict = new GSObject();
                            if (Program.MainForm.chkPreEncodeParams.Checked)
                                dict.ParseQuerystring(this.getEncodedValues(method.Params.Trim()));
                            else
                                dict.ParseQuerystring(method.Params.Trim());

                            dict.Put("uid", dict.GetString("uid", null));

                            if (GetSecure().HasValue)
                                secure = GetSecure().Value;

                            GSRequest g = new GSRequest(this.APIKey, this.SecKey, method.Name, dict, secure);

                            var req = new GSRequest(this.APIKey, this.SecKey, method.Name, dict, secure);
                            var res = req.Send();

                            ret.Results.Add(methodRes);
                            methodRes.Request = req;
                            methodRes.Response = res;
                            methodRes.SchemaValidation = new SchemaValidation("http://socialize.api.gigya.com/schema");

                            if (dict.GetString("format").Equals("xml", StringComparison.InvariantCultureIgnoreCase)) {
                                var xc = new XmlComparer();
                                xc.Compare(method.Expected, res.GetResponseText(), method.Schema);
                                methodRes.Differences = xc.Differences;

                                var xdoc = XDocument.Parse(res.GetResponseText());
                                var xroot = (xdoc.Document.FirstNode as XContainer);
                                var errCode = xroot.Element(XName.Get("errorCode", "urn:com:gigya:api"));


                                int iErrCode;
                                if (errCode != null && int.TryParse(errCode.Value, out iErrCode)) {
                                    methodRes.ErrorCode = iErrCode;

                                    var errMsg = xroot.Element(XName.Get("errorMessage", "urn:com:gigya:api"));
                                    methodRes.ErrorMessage = errMsg == null ? string.Empty : errMsg.Value;
                                }

                                else {
                                    methodRes.ErrorCode = 0;
                                    methodRes.ErrorMessage = "";
                                }


                                if (methodRes.Differences.Count > 0 && methodRes.ErrorCode == 0)
                                    methodRes.ErrorCode = -1;

                                var statusCode = xroot.Element(XName.Get("statusCode", "urn:com:gigya:api"));
                                int iStatusCode;

                                if (statusCode != null && int.TryParse(statusCode.Value, out iStatusCode)) {
                                    methodRes.StatusCode = iStatusCode;
                                }
                                else {
                                    methodRes.StatusCode = 200;
                                }

                                var sr = new StringReader(res.GetResponseText());
                                methodRes.Validation = methodRes.SchemaValidation.Validate(sr);
                            }
                            else {
                                var jc = new JsonComparer();
                                jc.Compare(method.Expected, res.GetResponseText(), method.Schema);
                                methodRes.Differences = jc.Differences;

                                if (methodRes.Differences.Count > 0 && res.GetErrorCode() == 0)
                                    methodRes.ErrorCode = -1;
                                else
                                    methodRes.ErrorCode = res.GetErrorCode();

                                methodRes.ErrorMessage = res.GetErrorMessage();
                                methodRes.StatusCode = res.GetData().GetInt("statusCode", 200);
                                methodRes.Validation = true;
                            }

                            methodRes.RunEnded = DateTime.Now;


                            //invoke the callback under the UI thread.
                            Program.MainForm.Invoke(cb, ret);
                        }
                        catch (Exception ex) {
                            MessageBox.Show(ex.Message, ex.Message);
                        }
                    }



                    //invoke the callback under the UI thread.
                    Program.MainForm.Invoke(cb, ret);

                    //invoke the callback under the UI thread.
                    Program.MainForm.Invoke(onDone, ret);


                    ret.RunEnded = DateTime.Now;
                }
                catch (Exception ex) {
                    MessageBox.Show(ex.Message, ex.Message);
                }
            }
        }

        public class TestRunResult
        {
            [ExportToCSVAttribute(Order = 0)]
            public DateTime RunStarted { get; set; }

            [ExportToCSVAttribute(Order = 1)]
            public DateTime RunEnded { get; set; }

            [ExportToCSVAttribute(Order = 2)]
            public TimeSpan RunTook { get { return RunEnded - RunStarted; } }

            public List<MethodResult> Results { get; set; }
        }

        public class MethodResult
        {
            [ExportToCSVAttribute(Order = 1)]
            public string TestName { get; set; }

            [ExportToCSVAttribute(Order = 2)]
            public string TestDescription { get; set; }

            [ExportToCSVAttribute(Order = 3)]
            public bool IsValid { get { return Differences.Count == 0; } set { } }

            [ExportToCSVAttribute(Order = 4)]
            public DateTime RunStarted { get; set; }

            [ExportToCSVAttribute(Order = 5)]
            public DateTime RunEnded { get; set; }


            [ExportToCSVAttribute(Order = 6)]
            public TimeSpan RunTook { get { return RunEnded - RunStarted; } }


            public GSRequest Request { get; set; }

            public GSResponse Response { get; set; }

            [ExportToCSVAttribute(Order = 7)]
            public int StatusCode { get; set; }

            [ExportToCSVAttribute(Order = 8)]
            public int ErrorCode { get; set; }

            [ExportToCSVAttribute(Order = 9)]
            public String ErrorMessage { get; set; }

            [ExportToCSV(Order = 10)]
            public bool Validation { get; set; }


            public SchemaValidation SchemaValidation { get; set; }
            public List<CompareResultInfo> Differences { get; set; }

            public List<SchemaValidationError> ValidationErrors { get { return SchemaValidation.Errors; } set { SchemaValidation.Errors = value; } }
        }

        [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
        public class ExportToCSVAttribute : Attribute
        {
            public int Order;
        }

        public class Method : INotifyPropertyChanged
        {
            private bool _include;
            [XmlIgnore]
            public bool Include
            {
                get { return _include; }
                set
                {
                    _include = value; onPropertyChanged("Include");
                }
            }

            [XmlAttribute("name")]
            public string Name { get; set; }

            [XmlAttribute("testName")]
            public string TestName { get; set; }

            [XmlAttribute("testDescription")]
            public string TestDescription { get; set; }

            [XmlAttribute("namespace")]
            public string Namespace { get; set; }

            [XmlElement("params")]
            public string Params { get; set; }

            [XmlElement("expected")]
            public string Expected { get; set; }


            [XmlElement("schema")]
            public string Schema { get; set; }

            #region INotifyPropertyChanged Members
            private void onPropertyChanged(string prop)
            {
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }
            public event PropertyChangedEventHandler PropertyChanged;

            #endregion
        }
    }
}
