using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data.EntityClient;
using System.Data.Objects;
using System.ComponentModel;
using op = ObjectPolicy;
using ObjectPolicy;
using System.Reflection;
using System.Xml.Schema;
using System.Xml;

namespace ObjectPolicyTester
{
	/// <summary>
	/// Interaction logic for ObjectPolicyTester.xaml
	/// </summary>
	public partial class Tester : Page
	{
		private List<op.ObjectPolicy> objectPoliciesBase;
		private List<op.ObjectPolicy> appliedObjectPolicies = new List<op.ObjectPolicy>();

		public Tester()
		{
			InitializeComponent();
		}

		private void loadPolicies()
		{
			string connectionString;
			if (System.Environment.MachineName.Equals("HOVE-PC"))
			{
				connectionString = Properties.Settings.Default["TestConnectionStringWork"].ToString();
			}
			else
			{
				connectionString = Properties.Settings.Default["TestConnectionString"].ToString();
			}

			EntityConnectionStringBuilder entityBuilder = new EntityConnectionStringBuilder();

			//Set the provider name.
			entityBuilder.Provider = "System.Data.SqlClient";

			// Set the provider-specific connection string.
			entityBuilder.ProviderConnectionString = connectionString;

			// Set the Metadata location.
			entityBuilder.Metadata = @"res://*/ObjectPolicy.csdl|res://*/ObjectPolicy.ssdl|res://*/ObjectPolicy.msl";

			TestEntities ctxTest = new TestEntities(entityBuilder.ToString());
			ObjectResult<ObjectPolicyEntity> objectPolicyEntities = ctxTest.GetObjectPolicy("WPF", MergeOption.NoTracking);

			List<string> objectPoliciesString = objectPolicyEntities.Aggregate<ObjectPolicyEntity, List<string>>(
				new List<string>(),
				(op, ope) => { op.Add(ope.Objects); return op; }
			);

			objectPoliciesBase = ObjectPolicyManager.GetObjectPolicies(objectPoliciesString, new ObjectPolicyHelper(), ObjectPolicyHelper.GetType);
		}

		private void Page_Loaded(object sender, RoutedEventArgs e)
		{
			loadPolicies();
			ObjectPolicyManager.ApplyObjectPolicies(LibraryType.WFP, objectPoliciesBase, this, searchMembers: false, appliedObjectPolicies: appliedObjectPolicies);
		}

		private void Button1_Click(object sender, RoutedEventArgs e)
		{
		}
	}
}
