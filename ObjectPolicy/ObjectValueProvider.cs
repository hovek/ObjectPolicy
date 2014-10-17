using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Windows.Data;
using System.ComponentModel;
using System.Windows;

namespace ObjectPolicy
{
	public interface IValueProvider<T>
	{
		T Value { get; }
	}

	public interface IValueSetter<T>
	{
		T Value { set; }
	}

	public interface IDynamic
	{
		bool Dynamic { get; set; }
	}

	public interface IAllowBinding
	{
		bool AllowBinding { get; set; }
	}

	public interface IPathable
	{
		string Path { get; }
	}

	public interface IReturnType
	{
		Type ReturnType { get; }
	}

	[Serializable]
	public class RawValueProvider : IValueProvider<object>
	{
		public RawValueProvider(dynamic value = null)
		{
			Value = value;
		}

		public dynamic Value
		{
			get;
			set;
		}
	}

	[Serializable]
	public class ObjectArrayValueProvider : IValueProvider<object>
	{
		public List<IValueProvider<object>> ValueProviders = new List<IValueProvider<object>>();

		public dynamic Value
		{
			get
			{
				dynamic[] objects = new dynamic[ValueProviders.Count];
				int i = 0;
				foreach (IValueProvider<object> vp in ValueProviders)
				{
					objects[i] = vp.Value;
					i++;
				}

				return objects;
			}
		}
	}

	[Serializable]
	public class MethodValueProvider : IValueProvider<object>, IPathable, IReturnType, IDynamic
	{
		private IValueProvider<object> targetValueProvider;
		private ObjectArrayValueProvider methodParameters;
		private MethodInfo methodInfo;
		private dynamic value;

		private object[] _objectMethodParameters;
		private object[] objectMethodParameters
		{
			get
			{
				return _objectMethodParameters;
			}
			set
			{
				_objectMethodParameters = value;
				_objectMethodParametersTypes = null;
			}
		}

		private Type[] _objectMethodParametersTypes;
		private Type[] objectMethodParametersTypes
		{
			get
			{
				if (_objectMethodParametersTypes == null && methodParameters != null)
				{
					_objectMethodParametersTypes = getTypes(objectMethodParameters, methodParameters.ValueProviders);
				}

				return _objectMethodParametersTypes;
			}
			set
			{
				_objectMethodParametersTypes = value;
			}
		}

		private static Type[] getTypes(object[] objs, List<IValueProvider<object>> vps)
		{
			Type[] types = new Type[vps.Count];

			if (objs != null)
			{
				for (int i = 0; i < vps.Count; i++)
				{
					dynamic value = objs[i];
					if (value != null)
					{
						types[i] = ReflectionHelper.GetType(value);
					}
					else
					{
						IValueProvider<object> vp = vps[i];
						if (vp is IReturnType)
						{
							types[i] = ((IReturnType)vp).ReturnType;
						}
						else
						{
							types[i] = null;
						}
					}
				}
			}

			return types;
		}

		internal object _Target;
		internal object Target
		{
			get
			{
				return _Target;
			}
			set
			{
				_Target = value;
				targetType = null;
			}
		}

		private Type _targetType;
		private Type targetType
		{
			get
			{
				if (_targetType == null && Target != null)
				{
					_targetType = Target.GetType();
				}

				return _targetType;
			}
			set
			{
				_targetType = value;
			}
		}

		private string _methodName;
		public string MethodName
		{
			get
			{
				return _methodName;
			}
		}

		private bool _dynamic;
		public bool Dynamic
		{
			get
			{
				return _dynamic;
			}
			set
			{
				_dynamic = value;
			}
		}

		private string _path;
		public string Path
		{
			get
			{
				string path = "";
				if (targetValueProvider != null && targetValueProvider is IPathable)
				{
					path = ((IPathable)targetValueProvider).Path;
				}

				return path + (path.Length > 0 && _path.Length > 0 ? "." : "") + _path;
			}
		}

		public Type ReturnType
		{
			get
			{
				if (methodInfo != null)
				{
					return methodInfo.ReturnType;
				}

				return null;
			}
		}

		public MethodValueProvider(object target = null, IValueProvider<object> targetValueProvider = null
			, ObjectArrayValueProvider methodParameters = null, string methodName = "", bool dynamic = false
			, string path = null)
		{
			this.Target = target;
			this.targetValueProvider = targetValueProvider;
			this.methodParameters = methodParameters;
			_methodName = methodName;
			_dynamic = dynamic;
			if (path == null)
			{
				_path = methodName;
			}
			else
			{
				_path = path;
			}
		}

		private object getTargetAndSetMethodInfo(out object[] objectMethodParameters)
		{
			object target = null;
			Type targetType = null;
			objectMethodParameters = null;
			Type[] objectMethodParametersTypes = null;

			if (Dynamic)
			{
				if (targetValueProvider != null)
				{
					target = targetValueProvider.Value;
					if (target != null)
					{
						targetType = target.GetType();
					}
					this.methodInfo = null;
				}
				else
				{
					target = Target;
					targetType = this.targetType;
				}
				if (this.methodParameters == null)
				{
					objectMethodParameters = this.objectMethodParameters;
					objectMethodParametersTypes = this.objectMethodParametersTypes;
				}
				else
				{
					objectMethodParameters = (object[])this.methodParameters.Value;
					objectMethodParametersTypes = getTypes(objectMethodParameters, this.methodParameters.ValueProviders);
				}
			}
			else
			{
				if (Target != null)
				{
					target = Target;
					targetType = this.targetType;
				}
				else if (targetValueProvider != null)
				{
					Target = targetValueProvider.Value;
					target = Target;
					targetType = this.targetType;
					this.methodInfo = null;
				}
				if (this.objectMethodParameters == null && this.methodParameters != null)
				{
					this.objectMethodParameters = (object[])this.methodParameters.Value;
				}
				objectMethodParameters = this.objectMethodParameters;
				objectMethodParametersTypes = this.objectMethodParametersTypes;
			}

			if (target != null && this.methodInfo == null)
			{
				this.methodInfo = ReflectionHelper.GetMethod(targetType, MethodName, objectMethodParametersTypes);
			}

			return target;
		}

		public dynamic Value
		{
			get
			{
				object[] objectMethodParameters;
				object target = getTargetAndSetMethodInfo(out objectMethodParameters);
				if (target != null)
				{
					return methodInfo.Invoke(target, objectMethodParameters);
				}
				else
				{
					return this.value;
				}
			}
		}
	}

	[Serializable]
	public class ValueProviderSetter : IValueProvider<object>, IValueSetter<object>, IPathable, IReturnType, IDynamic, IAllowBinding
	{
		internal readonly long ID;
		private object _target;
		internal object Target
		{
			get
			{
				return _target;
			}
			set
			{
				if (_target is INotifyPropertyChanged)
				{
					((INotifyPropertyChanged)_target).PropertyChanged -= propertyNotification_ValueChanged;
				}
				_target = value;
				targetType = null;
				setBinding(Target, targetType);
			}
		}
		private Type _targetType;
		private Type targetType
		{
			get
			{
				if (_targetType == null && Target != null)
				{
					_targetType = Target.GetType();
				}

				return _targetType;
			}
			set
			{
				_targetType = value;
			}
		}
		private IValueProvider<object> targetValueProvider;
		private FieldInfo fieldInfo;
		private PropertyInfo propertyInfo;
		private dynamic value;
		public event EventHandler ValueChanged;

		[NonSerialized]
		private ObjectPolicyBinder binder = null;

		private void setBinding(object target, Type targetType)
		{
			if (binder != null)
			{
				BindingOperations.ClearBinding(binder, ObjectPolicyBinder.ValueProperty);
				binder.ValueChanged -= binder_ValueChanged;
				binder = null;
			}

			if (!AllowBinding)
			{
				return;
			}

			if (targetType != null
				&& DependencyPropertyDescriptor.FromName(PropertyOrFieldName, targetType, targetType) != null)
			{
				binder = new ObjectPolicyBinder();
				binder.ValueChanged += binder_ValueChanged;
				Binding binding = new Binding();
				binding.Source = target;
				binding.Path = new PropertyPath(PropertyOrFieldName);
				binding.Mode = BindingMode.OneWay;
				binder.SetBinding(ObjectPolicyBinder.ValueProperty, binding);
			}
			else if (target is INotifyPropertyChanged)
			{
				INotifyPropertyChanged targetINotifyPropertyChanged = (INotifyPropertyChanged)target;
				targetINotifyPropertyChanged.PropertyChanged -= propertyNotification_ValueChanged;
				targetINotifyPropertyChanged.PropertyChanged += propertyNotification_ValueChanged;
			}
		}

		private void propertyNotification_ValueChanged(object o, PropertyChangedEventArgs e)
		{
			if (ValueChanged != null && e.PropertyName.Equals(PropertyOrFieldName))
			{
				ValueChanged(this, null);
			}
		}

		private void binder_ValueChanged(object obj, EventArgs e)
		{
			if (ValueChanged != null)
			{
				ValueChanged(this, null);
			}
		}

		public Type ReturnType
		{
			get
			{
				if (propertyInfo != null)
				{
					return propertyInfo.PropertyType;
				}
				else if (fieldInfo != null)
				{
					return fieldInfo.FieldType;
				}

				return null;
			}
		}

		private string _propertyOrFieldName;
		public string PropertyOrFieldName
		{
			get
			{
				return _propertyOrFieldName;
			}
		}

		private bool _allowBinding;
		public bool AllowBinding
		{
			get
			{
				return _allowBinding;
			}
			set
			{
				bool changed = _allowBinding != value;
				_allowBinding = value;
				if (changed && _allowBinding)
				{
					setBinding(Target, targetType);
				}
			}
		}

		private bool _dynamic;
		public bool Dynamic
		{
			get
			{
				return _dynamic;
			}
			set
			{
				_dynamic = value;
			}
		}

		private string _path;
		public string Path
		{
			get
			{
				string path = "";
				if (targetValueProvider != null && targetValueProvider is IPathable)
				{
					path = ((IPathable)targetValueProvider).Path;
				}

				return path + (path.Length > 0 && _path.Length > 0 ? "." : "") + _path;
			}
		}

		public ValueProviderSetter(object target = null
			, IValueProvider<object> targetValueProvider = null, string propertyOrFieldName = ""
			, bool allowBinding = false, bool dynamic = false, EventHandler valueChangedHandler = null
			, string path = null)
		{
			ID = ObjectPolicyManager.GetNewId();
			this.Target = target;
			this.targetValueProvider = targetValueProvider;
			_propertyOrFieldName = propertyOrFieldName;
			AllowBinding = allowBinding;
			_dynamic = dynamic;
			if (valueChangedHandler != null)
			{
				ValueChanged += valueChangedHandler;
			}
			if (path == null)
			{
				_path = propertyOrFieldName;
			}
			else
			{
				_path = path;
			}
		}

		private object getTargetAndSetMemberInfo()
		{
			object target = null;
			Type targetType = null;

			if (Dynamic)
			{
				if (targetValueProvider != null)
				{
					target = targetValueProvider.Value;
					if (target != null)
					{
						targetType = target.GetType();
					}
					setBinding(target, targetType);
					this.propertyInfo = null;
					this.fieldInfo = null;
				}
				else
				{
					target = Target;
					targetType = this.targetType;
				}
			}
			else
			{
				if (Target != null)
				{
					target = Target;
					targetType = this.targetType;
				}
				else if (targetValueProvider != null)
				{
					Target = targetValueProvider.Value;
					target = Target;
					targetType = this.targetType;
					this.propertyInfo = null;
					this.fieldInfo = null;
				}
			}

			if (target != null && this.propertyInfo == null && this.fieldInfo == null)
			{
				this.propertyInfo = ReflectionHelper.GetProperty(targetType, PropertyOrFieldName);
				if (this.propertyInfo == null)
				{
					this.fieldInfo = ReflectionHelper.GetField(targetType, PropertyOrFieldName);
				}
			}

			return target;
		}

		public dynamic Value
		{
			get
			{
				object target = getTargetAndSetMemberInfo();
				if (target != null)
				{
					dynamic valueRet;
					if (propertyInfo != null)
					{
						valueRet = propertyInfo.GetValue(target, null);
					}
					else
					{
						valueRet = fieldInfo.GetValue(target);
					}
					return valueRet;
				}
				else
				{
					return this.value;
				}
			}
			set
			{
				object target = getTargetAndSetMemberInfo();
				if (target != null)
				{
					if (propertyInfo != null)
					{
						propertyInfo.SetValue(target, value, null);
					}
					else
					{
						fieldInfo.SetValue(target, value);
					}
				}
				else
				{
					this.value = value;
				}
			}
		}
	}
}