using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Gigya.Socialize.SDK.Tests.SDKTester
{
	public partial class ResponseViewer : Form
	{
		public ResponseViewer()
		{
			InitializeComponent();
		}

		public void SetText(string s)
		{
			textBox1.Text = s;
			textBox1.Select(0, 0);
		}
	}
}
