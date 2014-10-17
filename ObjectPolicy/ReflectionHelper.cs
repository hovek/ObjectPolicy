using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace ObjectPolicy
{
	public class ReflectionHelper
	{
		public static List<object> GetFieldAndPropertieValues(object o, bool excludeNull = false, bool privateMembers = false)
		{
			List<object> objects = new List<object>();
			List<MemberInfo> members = GetFieldsAndReadableProperties(o, privateMembers);

			foreach (MemberInfo mi in members)
			{
				object value;
				try
				{
					if (mi is FieldInfo)
					{
						value = ((FieldInfo)mi).GetValue(o);
						if (!excludeNull || value != null)
						{
							objects.Add(value);
						}
					}
					else
					{
						value = ((PropertyInfo)mi).GetValue(o, null);
						if (!excludeNull || value != null)
						{
							objects.Add(value);
						}
					}
				}
				catch
				{
				}
			}

			return objects;
		}

		public static FieldInfo GetField(Type type, string name)
		{
			FieldInfo member = null;
			List<Type> types = GetTypes(type);

			foreach (Type t in types)
			{
				member = t.GetField(name, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				if (member != null)
				{
					return member;
				}
			}

			return member;
		}

		public static PropertyInfo GetProperty(Type type, string name)
		{
			PropertyInfo member = null;
			List<Type> types = GetTypes(type);

			foreach (Type t in types)
			{
				member = t.GetProperty(name, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				if (member != null)
				{
					return member;
				}
			}

			return member;
		}

		public static MethodInfo GetMethod(Type type, string name, Type[] methodParameters = null)
		{
			MethodInfo member = null;
			List<Type> types = GetTypes(type);
			bool oneOfTypesIsNull = false;

			if (methodParameters == null)
			{
				methodParameters = new Type[0];
			}
			else
			{
				foreach (Type t in methodParameters)
				{
					if (t == null)
					{
						oneOfTypesIsNull = true;
						break;
					}
				}
			}

			if (!oneOfTypesIsNull)
			{
				foreach (Type t in types)
				{
					member = t.GetMethod(name, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, methodParameters, null);
					if (member != null)
					{
						return member;
					}
				}
			}
			else
			{
				foreach (Type t in types)
				{
					member = t.GetMethod(name, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
					if (member != null)
					{
						return member;
					}
				}
			}

			return member;
		}

		public static List<MemberInfo> GetFieldsAndReadableProperties(object o, bool privateMembers = false)
		{
			List<MemberInfo> members = new List<MemberInfo>();
			List<Type> types = GetTypes(o);

			BindingFlags bindingFlags = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
			if (privateMembers)
			{
				bindingFlags |= BindingFlags.NonPublic;
			}

			foreach (Type t in types)
			{
				members.AddRange(t.GetFields(bindingFlags));
				PropertyInfo[] pis = t.GetProperties(bindingFlags);
				foreach (PropertyInfo pi in pis)
				{
					if (pi.CanRead)
					{
						members.Add(pi);
					}
				}
			}

			return members;
		}

		public static List<Type> GetTypes(object o)
		{
			return GetTypes(o.GetType());
		}

		public static List<Type> GetTypes(Type t)
		{
			List<Type> types = new List<Type>();
			types.Add(t);

			while (t.BaseType != null)
			{
				t = t.BaseType;
				types.Add(t);
			}

			return types;
		}

		public static Type GetType<T>(T obj)
		{
			return typeof(T);
		}
	}
}
