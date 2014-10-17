using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ObjectPolicy
{
	[Serializable]
	public class ObjectPolicyIf : List<ObjectPolicyIf>
	{
		public bool IsElseIf;
		public ObjectPolicy ObjectPolicyParent;
		public IValueProvider<object> Condition;
		public List<Setter> Setters = new List<Setter>();
		public List<ObjectPolicy> ObjectPolicies = new List<ObjectPolicy>();
		internal bool IsObjectPoliciesSet;
	}
}
