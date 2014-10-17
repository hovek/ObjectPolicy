using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ObjectPolicy
{
	[Serializable]
	public class Condition : IValueProvider<object>
	{
		public IValueProvider<object> Value1;
		public IValueProvider<object> Value2;
		public IComparer<IValueProvider<object>> Comparer;

		public dynamic Value
		{
			get
			{
				return Comparer.Compare(Value1, Value2) == 1;
			}
		}
	}

	[Serializable]
	public class AndComparer : IComparer<IValueProvider<object>>
	{
		public int Compare(IValueProvider<object> x, IValueProvider<object> y)
		{
			return (bool)x.Value && (bool)y.Value ? 1 : 0;
		}
	}

	[Serializable]
	public class OrComparer : IComparer<IValueProvider<object>>
	{
		public int Compare(IValueProvider<object> x, IValueProvider<object> y)
		{
			return (bool)x.Value || (bool)y.Value ? 1 : 0;
		}
	}

	[Serializable]
	public class NotEqualsComparer : IComparer<IValueProvider<object>>
	{
		public int Compare(IValueProvider<object> x, IValueProvider<object> y)
		{
			return (dynamic)x.Value != (dynamic)y.Value ? 1 : 0;
		}
	}

	[Serializable]
	public class BiggerOrEqualToComparer : IComparer<IValueProvider<object>>
	{
		public int Compare(IValueProvider<object> x, IValueProvider<object> y)
		{
			return (dynamic)x.Value >= (dynamic)y.Value ? 1 : 0;
		}
	}

	[Serializable]
	public class LesOrEqualToComparer : IComparer<IValueProvider<object>>
	{
		public int Compare(IValueProvider<object> x, IValueProvider<object> y)
		{
			return (dynamic)x.Value <= (dynamic)y.Value ? 1 : 0;
		}
	}

	[Serializable]
	public class EqualityComparer : IComparer<IValueProvider<object>>
	{
		public int Compare(IValueProvider<object> x, IValueProvider<object> y)
		{
			return (dynamic)x.Value == (dynamic)y.Value ? 1 : 0;
		}
	}

	[Serializable]
	public class LesserComparer : IComparer<IValueProvider<object>>
	{
		public int Compare(IValueProvider<object> x, IValueProvider<object> y)
		{
			return (dynamic)x.Value < (dynamic)y.Value ? 1 : 0;
		}
	}

	[Serializable]
	public class BiggerComparer : IComparer<IValueProvider<object>>
	{
		public int Compare(IValueProvider<object> x, IValueProvider<object> y)
		{
			return (dynamic)x.Value > (dynamic)y.Value ? 1 : 0;
		}
	}
}
