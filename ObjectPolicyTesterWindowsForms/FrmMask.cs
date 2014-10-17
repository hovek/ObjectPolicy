using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Schema;
using ObjectPolicy;
using System.Xml;
using System.Reflection;

namespace ObjectPolicyTesterWindowsForms
{
	public partial class FrmMask : Form, INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		private List<ObjectPolicy.ObjectPolicy> objectPoliciesRaw;
		private List<ObjectPolicy.ObjectPolicy> appliedObjectPolicies = new List<ObjectPolicy.ObjectPolicy>();
		public DictionaryHelper<string, object> Event = new DictionaryHelper<string, object>();

		public FrmMask()
		{
			InitializeComponent();
		}

		private void FrmMask_Load(object sender, EventArgs e)
		{
			XmlSchema schema = ObjectPolicyManager.GetValidationSchema();
			XmlDocument xmlDoc = new XmlDocument();
			xmlDoc.Schemas.Add(schema);
			xmlDoc.Load(Assembly.GetExecutingAssembly().GetManifestResourceStream("ObjectPolicyTesterWindowsForms.ObjectPolicy.xml"));
			xmlDoc.Validate(xmlValidation);
			objectPoliciesRaw = ObjectPolicyManager.GetObjectPolicies(xmlDoc, new ObjectPolicyHelper(), getType: ObjectPolicyHelper.GetType);
			ObjectPolicyManager.ApplyObjectPolicies(LibraryType.WindowsForms, objectPoliciesRaw, this, searchMembers: false, includePrivateMembers: true, appliedObjectPolicies: appliedObjectPolicies);
		}

		private void xmlValidation(object o, ValidationEventArgs e)
		{
			if (e.Exception != null)
			{
				throw e.Exception;
			}
		}

		protected void EventTrigger(object sender, string eventName)
		{
			Event.Add(eventName, sender);
			CallPropertyChanged("Event");
			if (Event.ContainsKey(eventName))
			{
				Event.Remove(eventName);
			}
		}

		protected void CallPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		private class ObjectPolicyHelper
		{
			public Dictionary<object, object> Settings = new Dictionary<object, object>();

			public void SetSetting(object key, object value)
			{
				Settings[key] = value;
			}

			public object GetSetting(object key)
			{
				return Settings[key];
			}

			public void MessageBoxShow(string text)
			{
				MessageBox.Show(text);
			}

			public static Type GetType(string type)
			{
				if (type.Equals("Form", StringComparison.CurrentCultureIgnoreCase))
				{
					return typeof(Form);
				}
				if (type.Equals("Control", StringComparison.CurrentCultureIgnoreCase))
				{
					return typeof(Control);
				}

				return null;
			}
		}
	}

	public class DictionaryHelper<TKey, TValue> : Dictionary<TKey, TValue>
	{
		public TValue GetValue(TKey key)
		{
			return this[key];
		}
	}
}
