using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace ObjectPolicy
{
	public delegate Type GetTypeHandler(string type);
	public delegate void ApplyObjectPoliciesHandler(LibraryType libraryType, List<ObjectPolicy> objectPolicies, object applyToObject, bool includeApplyToObject = true, bool searchMembers = true, bool includePrivateMembers = false, List<ObjectPolicy> appliedObjectPolicies = null);

	[Serializable]
	public class ObjectPolicy : ICloneable, INotifyPropertyChanged
	{
		public ApplyObjectPoliciesHandler ApplyObjectPolicies;
		public ObjectPolicyApplyParameters ApplyObjectPoliciesParameters;

		public List<ObjectPolicy> AppliedObjectPolicies;

		private object _owner;
		public object Owner
		{
			get
			{
				return _owner;
			}
			set
			{
				bool changed = _owner != value;
				_owner = value;
				if (changed)
				{
					callPropertyChanged("Owner");
				}
			}
		}

		public object Helper
		{
			get
			{
				return Helper_i.Value;
			}
			set
			{
				Helper_i.Value = value;
			}
		}
		internal ValueProviderSetter Helper_i;
		internal ObjectPolicy Parent;
		public string Name;
		public Type Type;
		public string TypeString;
		public string Assembly;
		public IValueProvider<object> IdentityCondition;
		internal Dictionary<string, IValueProvider<object>> ValueProvidersSetters = new Dictionary<string, IValueProvider<object>>();
		internal Dictionary<string, IValueProvider<object>> ValueProvidersSettersStandAlone = new Dictionary<string, IValueProvider<object>>();
		public List<ObjectPolicyIf> ObjectPolicyIfs = new List<ObjectPolicyIf>();

		private Dictionary<string, IValueProvider<object>> _valueProvidersSettersStandAloneDynamic;
		private Dictionary<string, IValueProvider<object>> valueProvidersSettersStandAloneDynamic
		{
			get
			{
				if (_valueProvidersSettersStandAloneDynamic == null)
				{
					_valueProvidersSettersStandAloneDynamic = getNonchainedDynamicAndInitializeAbleValueProviders(ValueProvidersSettersStandAlone);
				}

				return _valueProvidersSettersStandAloneDynamic;
			}
		}

		//private Dictionary<string,List<IValueProvider<object>>> ValueProvidersSetters

		public ObjectPolicy(ObjectPolicy parent = null, object helper = null)
		{
			Parent = parent;
			Helper_i = new ValueProviderSetter(path: "$");
			Helper_i.Value = helper;
			this.PropertyChanged += ownerValueChanged;
		}

		private void ownerValueChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName != "Owner")
			{
				return;
			}

			this.PropertyChanged -= ownerValueChanged;

			//prvi puta nakon sto je Owneru dodjeljena vrijednost prolazi kroz sve valueProvidere koji nisu dio standalone dynamic providera
			//i imaju postavljeno allowBinding="true", ili su dio standalone ne dynamic providera
			List<string> pathsForInitialization = ValueProvidersSetters.Where(vp =>
				{
					if (vp.Key == "this")
					{
						return false;
					}

					foreach (string path in valueProvidersSettersStandAloneDynamic.Keys)
					{
						if (path.StartsWith(vp.Key))
						{
							return false;
						}
					}

					if (!(vp.Value is IAllowBinding && ((IAllowBinding)vp.Value).AllowBinding))
					{
						foreach (string path in ValueProvidersSettersStandAlone.Keys)
						{
							if (path.Equals(vp.Key))
							{
								return true;
							}
						}
						return false;
					}

					return true;
				}).Select<KeyValuePair<string, IValueProvider<object>>, string>(vp => { return vp.Key; }).ToList();

			List<string> mergedPathsForInitialization = ObjectPolicyManager.MergePaths(pathsForInitialization);
			foreach (string path in mergedPathsForInitialization)
			{
				object o = ValueProvidersSetters[path].Value;
			}

			checkValueProviders();
		}

		internal void ValueProviderValueChanged(object sender, EventArgs e)
		{
			checkValueProviders();
		}

		private void checkValueProviders()
		{
			foreach (IValueProvider<object> vp in valueProvidersSettersStandAloneDynamic.Values)
			{
				object o = vp.Value;
			}

			checkObjectPolicyIfs(ObjectPolicyIfs);
		}

		private static Dictionary<string, IValueProvider<object>> getNonchainedDynamicAndInitializeAbleValueProviders(Dictionary<string, IValueProvider<object>> valueProviders)
		{
			Dictionary<string, IValueProvider<object>> allDynamicValueProviders = new Dictionary<string, IValueProvider<object>>();
			foreach (KeyValuePair<string, IValueProvider<object>> vp in valueProviders)
			{
				if (vp.Value is IDynamic && ((IDynamic)vp.Value).Dynamic)
				{
					allDynamicValueProviders.Add(vp.Key, vp.Value);
				}
			}

			List<string> allDynamicPaths = allDynamicValueProviders.Keys.ToList();

			Dictionary<string, IValueProvider<object>> nonchainedDynamicValueProviders = new Dictionary<string, IValueProvider<object>>();

			foreach (KeyValuePair<string, IValueProvider<object>> vpDynamic in allDynamicValueProviders)
			{
				bool isNextPathsDepthDynamic = false;
				List<string> nextPathsDepth = ObjectPolicyManager.GetNextPathDepth(vpDynamic.Key, allDynamicPaths);
				foreach (string nextPathDepth in nextPathsDepth)
				{
					IValueProvider<object> vp = allDynamicValueProviders[nextPathDepth];
					isNextPathsDepthDynamic = ((IDynamic)vp).Dynamic;
				}
				if (!isNextPathsDepthDynamic)
				{
					nonchainedDynamicValueProviders.Add(vpDynamic.Key, vpDynamic.Value);
				}
			}

			return nonchainedDynamicValueProviders;
		}

		private void checkObjectPolicyIfs(List<ObjectPolicyIf> objectPolicyIfs)
		{
			bool prevConditionMet = false;
			foreach (ObjectPolicyIf objectPolicyIf in objectPolicyIfs)
			{
				if (!objectPolicyIf.IsElseIf || !prevConditionMet)
				{
					prevConditionMet = objectPolicyIf.Condition == null || (bool)objectPolicyIf.Condition.Value;
					if (prevConditionMet)
					{
						applyObjectPolicyIf(objectPolicyIf);
						checkObjectPolicyIfs(objectPolicyIf);
					}
					else
					{
						undoSetters(objectPolicyIf);
					}
				}
				else
				{
					continue;
				}
			}
		}

		private void applyObjectPolicyIf(ObjectPolicyIf objectPolicyIf)
		{
			foreach (Setter setter in objectPolicyIf.Setters)
			{
				setter.Set();
			}

			if (!objectPolicyIf.IsObjectPoliciesSet && objectPolicyIf.ObjectPolicies.Count > 0)
			{
				objectPolicyIf.IsObjectPoliciesSet = true;
				if (AppliedObjectPolicies == null)
				{
					AppliedObjectPolicies = new List<ObjectPolicy>();
				}
				ApplyObjectPolicies(ApplyObjectPoliciesParameters.LibraryType, objectPolicyIf.ObjectPolicies, Owner, false, ApplyObjectPoliciesParameters.SearchMembers, ApplyObjectPoliciesParameters.IncludePrivateMembers, AppliedObjectPolicies);
			}
		}

		private void undoSetters(ObjectPolicyIf objectPolicyIf)
		{
			foreach (ObjectPolicyIf opi in objectPolicyIf)
			{
				undoSetters(opi);
			}
			foreach (Setter setter in objectPolicyIf.Setters)
			{
				if (setter.AllowUndo)
				{
					setter.Undo();
				}
			}
		}

		public bool IsAppliableTo(ObjectInfo objectInfo)
		{
			if (this.Type != null)
			{
				if (!this.Type.IsInstanceOfType(objectInfo.Object))
				{
					return false;
				}
			}
			else
			{
				if ((TypeString != null && objectInfo.TypeName != null && TypeString != objectInfo.TypeName)
					|| (Assembly != null && objectInfo.AssemblyName != null && Assembly != objectInfo.AssemblyName))
				{
					return false;
				}
			}

			if (IdentityCondition == null)
			{
				return true;
			}

			_owner = objectInfo.Object;
			bool ret = (bool)IdentityCondition.Value;
			_owner = null;
			return ret;
		}

		internal List<ObjectPolicy> GetParents()
		{
			List<ObjectPolicy> parents = new List<ObjectPolicy>();
			ObjectPolicy parent = this.Parent;
			while (parent != null)
			{
				parents.Add(parent);
				parent = parent.Parent;
			}

			return parents;
		}

		internal List<ValueProviderSetter> GetValueProvidersSetters()
		{
			return getValueProvidersSetters(this);
		}

		private static List<ValueProviderSetter> getValueProvidersSetters(List<ObjectPolicyIf> opis)
		{
			List<ValueProviderSetter> vpss = new List<ValueProviderSetter>();
			foreach (ObjectPolicyIf opi in opis)
			{
				vpss.AddRange(getValueProvidersSetters(opi));
				foreach (ObjectPolicy opTemp in opi.ObjectPolicies)
				{
					vpss.AddRange(getValueProvidersSetters(opTemp));
				}
			}

			return vpss;
		}

		private static List<ValueProviderSetter> getValueProvidersSetters(ObjectPolicy op)
		{
			List<ValueProviderSetter> vpss = getValueProvidersSetters(op.ObjectPolicyIfs);
			foreach (IValueProvider<object> vp in op.ValueProvidersSetters.Values)
			{
				if (vp is ValueProviderSetter)
				{
					vpss.Add((ValueProviderSetter)vp);
				}
			}
			foreach (IValueProvider<object> vp in op.ValueProvidersSettersStandAlone.Values)
			{
				if (vp is ValueProviderSetter)
				{
					vpss.Add((ValueProviderSetter)vp);
				}
			}

			return vpss;
		}

		public object Clone()
		{
			List<ObjectPolicy> parents = GetParents();
			List<ValueProviderSetter> valueProvidersSetters = GetValueProvidersSetters();
			Dictionary<long, ObjectPolicy> idValueProviderSetterTargets = null;
			//remove target from value providers
			if (parents.Count > 0)
			{
				idValueProviderSetterTargets = new Dictionary<long, ObjectPolicy>();
				foreach (ObjectPolicy p in parents)
				{
					foreach (ValueProviderSetter vps in valueProvidersSetters)
					{
						if (vps.Target == p)
						{
							idValueProviderSetterTargets[vps.ID] = p;
							vps.Target = null;
						}
					}
				}
			}
			//remove helper from value providers
			Dictionary<long, object> idValueProviderSetterHelpers = new Dictionary<long, object>();
			foreach (ValueProviderSetter vps in valueProvidersSetters)
			{
				if (vps.Path == "$")
				{
					object value = vps.Value;
					if (value != null)
					{
						idValueProviderSetterHelpers[vps.ID] = value;
						vps.Value = null;
					}
				}
			}

			IValueProvider<object> identityCondition = IdentityCondition;
			IdentityCondition = null;
			ObjectPolicy parent = Parent;
			Parent = null;

			ObjectPolicy opCloned = (ObjectPolicy)Serializer.DeSerialize(Serializer.Serialize(this));

			IdentityCondition = identityCondition;
			Parent = parent;

			//set target to value providers
			if (parents.Count > 0)
			{
				List<ValueProviderSetter> clonedValueProvidersSetters = opCloned.GetValueProvidersSetters();
				foreach (KeyValuePair<long, ObjectPolicy> idTarget in idValueProviderSetterTargets)
				{
					foreach (ValueProviderSetter vps in valueProvidersSetters)
					{
						if (vps.ID == idTarget.Key)
						{
							vps.Target = idTarget.Value;
						}
					}
					foreach (ValueProviderSetter vps in clonedValueProvidersSetters)
					{
						if (vps.ID == idTarget.Key)
						{
							vps.Target = idTarget.Value;
						}
					}
				}
			}
			//set helper to value providers
			if (idValueProviderSetterHelpers.Count > 0)
			{
				List<ValueProviderSetter> clonedValueProvidersSetters = opCloned.GetValueProvidersSetters();
				foreach (KeyValuePair<long, object> idHelper in idValueProviderSetterHelpers)
				{
					foreach (ValueProviderSetter vps in valueProvidersSetters)
					{
						if (vps.ID == idHelper.Key)
						{
							vps.Value = idHelper.Value;
						}
					}
					foreach (ValueProviderSetter vps in clonedValueProvidersSetters)
					{
						if (vps.ID == idHelper.Key)
						{
							vps.Value = idHelper.Value;
						}
					}
				}
			}

			return opCloned;
		}

		private void callPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}
}
