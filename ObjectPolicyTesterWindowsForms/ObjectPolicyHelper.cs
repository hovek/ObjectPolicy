using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ObjectPolicy
{
	public class ObjectPolicyHelper
	{
		public static Type GetType(string type)
		{
			return Type.GetType(type);
		}
	}
}
