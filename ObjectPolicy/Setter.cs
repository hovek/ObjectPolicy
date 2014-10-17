using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ObjectPolicy
{
	[Serializable]
	public class Setter
	{
		public IValueSetter<object> ValueSetter;
		public bool AllowUndo = false;
		public IValueProvider<object> Value;
		public string ValueObject;
		private object valueObject;
		private object oldValue;
		private bool oldValueHasBeenSet = false;

		public void Set()
		{
			object newValue;
			if (Value != null)
			{
				newValue = Value.Value;
			}
			else
			{
				if (valueObject == null)
				{
					valueObject = System.Windows.Markup.XamlReader.Parse(ValueObject);
				}
				newValue = valueObject;
			}

			if (ValueSetter != null)
			{
				if (AllowUndo && !oldValueHasBeenSet)
				{
					object oldValueTemp = ((IValueProvider<object>)ValueSetter).Value;
					if (!oldValueHasBeenSet)
					{
						oldValue = oldValueTemp;
						oldValueHasBeenSet = true;
					}
				}

				ValueSetter.Value = newValue;
			}
		}

		public void Undo()
		{
			if (oldValueHasBeenSet)
			{
				ValueSetter.Value = oldValue;
				oldValueHasBeenSet = false;
			}
		}
	}
}
