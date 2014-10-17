using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ObjectPolicy
{
	public class ObjectPolicyHelper
	{
		public static System.Windows.Thickness GetBorderThickness(double t)
		{
			return new System.Windows.Thickness(t);
		}

		public static Type GetType(string type)
		{
			return Type.GetType(type);
		}

		public System.Windows.Visibility GetVisibility(bool val)
		{
			return val ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
		}
	}
}
