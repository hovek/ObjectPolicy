using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace ObjectPolicy
{
	public class ObjectPolicyBinder : FrameworkElement
	{
		public static DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(object), typeof(ObjectPolicyBinder), new PropertyMetadata(new PropertyChangedCallback(changed)));
		public event EventHandler ValueChanged;

		private static void changed(DependencyObject obj, DependencyPropertyChangedEventArgs e)
		{
			ObjectPolicyBinder binder = (ObjectPolicyBinder)obj;
			binder.ValueChanged(obj, null);
		}
	}
}
