using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ObjectPolicy
{
	public interface IMerger<TSource, TResult>
	{
		TResult Merge(TSource val1, TSource val2);
	}

	[Serializable]
	public class MergedValueProvider : IValueProvider<object>
	{
		public IValueProvider<object> Value1;
		public IValueProvider<object> Value2;
		public IMerger<IValueProvider<object>, object> Merger;

		public dynamic Value
		{
			get
			{
				return Merger.Merge(Value1, Value2);
			}
		}
	}

	[Serializable]
	public class PlusMerger : IMerger<IValueProvider<object>, object>
	{
		public dynamic Merge(IValueProvider<object> val1, IValueProvider<object> val2)
		{
			return (dynamic)val1.Value + (dynamic)val2.Value;
		}
	}

	[Serializable]
	public class MinusMerger : IMerger<IValueProvider<object>, object>
	{
		public dynamic Merge(IValueProvider<object> val1, IValueProvider<object> val2)
		{
			dynamic v1 = val1.Value;
			dynamic v2 = val2.Value;
			if (v1 is decimal || v2 is decimal)
			{
				return Convert.ToDecimal(v1) - Convert.ToDecimal(v2);
			}
			else
			{
				return Convert.ToInt32(v1) - Convert.ToInt32(v2);
			}
		}
	}

	[Serializable]
	public class MultiplyMerger : IMerger<IValueProvider<object>, object>
	{
		public dynamic Merge(IValueProvider<object> val1, IValueProvider<object> val2)
		{
			dynamic v1 = val1.Value;
			dynamic v2 = val2.Value;
			if (v1 is decimal || v2 is decimal)
			{
				return Convert.ToDecimal(v1) * Convert.ToDecimal(v2);
			}
			else
			{
				return Convert.ToInt32(v1) * Convert.ToInt32(v2);
			}
		}
	}

	[Serializable]
	public class DivisionMerger : IMerger<IValueProvider<object>, object>
	{
		public dynamic Merge(IValueProvider<object> val1, IValueProvider<object> val2)
		{
			return Convert.ToDecimal(val1.Value) / Convert.ToDecimal(val2.Value);
		}
	}

	[Serializable]
	public class ModMerger : IMerger<IValueProvider<object>, object>
	{
		public dynamic Merge(IValueProvider<object> val1, IValueProvider<object> val2)
		{
			dynamic v1 = val1.Value;
			dynamic v2 = val2.Value;
			if (v1 is decimal || v2 is decimal)
			{
				return Convert.ToDecimal(v1) % Convert.ToDecimal(v2);
			}
			else
			{
				return Convert.ToInt32(v1) % Convert.ToInt32(v2);
			}
		}
	}
}
