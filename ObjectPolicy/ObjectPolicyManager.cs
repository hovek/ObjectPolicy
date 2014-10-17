using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Xml;
using System.Data.SqlClient;
using System.Windows.Data;
using System.Windows;
using WF = System.Windows.Forms;
using System.Xml.Schema;

namespace ObjectPolicy
{
	[Serializable]
	public struct ObjectPolicyApplyParameters
	{
		public LibraryType LibraryType;
		public bool SearchMembers;
		public bool IncludePrivateMembers;

		public ObjectPolicyApplyParameters(LibraryType libraryType, bool searchMembers = true, bool includePrivateMembers = false)
		{
			LibraryType = libraryType;
			SearchMembers = searchMembers;
			IncludePrivateMembers = includePrivateMembers;
		}
	}

	public enum LibraryType
	{
		WindowsForms,
		WFP
	}

	public struct ObjectInfo
	{
		private object _object;
		public object Object
		{
			get
			{
				return _object;
			}
			set
			{
				_object = value;
				Type = null;
			}
		}
		private Type _type;
		public Type Type
		{
			get
			{
				if (_type == null)
				{
					_type = Object.GetType();
				}

				return _type;
			}
			set
			{
				_type = value;
				TypeName = null;
				AssemblyName = null;
			}
		}
		private string _typeName;
		public string TypeName
		{
			get
			{
				if (_typeName == null)
				{
					_typeName = Type.FullName;
				}

				return _typeName;
			}
			set
			{
				_typeName = value;
			}
		}
		private string _assemblyName;
		public string AssemblyName
		{
			get
			{
				if (_assemblyName == null)
				{
					int commaIndex = Type.Assembly.FullName.IndexOf(',');
					if (commaIndex == -1)
					{
						commaIndex = Type.Assembly.FullName.Length;
					}
					_assemblyName = Type.Assembly.FullName.Substring(0, commaIndex);
				}

				return _assemblyName;
			}
			set
			{
				_assemblyName = value;
			}
		}

		public ObjectInfo(object member, Type type = null, string typeName = null, string assemblyName = null)
		{
			_object = member;
			_type = type;
			_typeName = typeName;
			_assemblyName = assemblyName;
		}
	}

	public class ObjectPolicyManager
	{
		private static ObjectInfoComparer objectInfoComparer = new ObjectInfoComparer();

		private static long id = 0;

		private static DelimiterInfo[] operatorDelimiters = new DelimiterInfo[] { 
			new DelimiterInfo("and",true, StringComparison.CurrentCultureIgnoreCase)
			, new DelimiterInfo("or",true, StringComparison.CurrentCultureIgnoreCase) 
			, new DelimiterInfo("!=")
			, new DelimiterInfo("<>")
			, new DelimiterInfo(">=")
			, new DelimiterInfo("<=")    
			, new DelimiterInfo("=")    
			, new DelimiterInfo(">")    
			, new DelimiterInfo("<")    
			, new DelimiterInfo("+")
			, new DelimiterInfo("-")
			, new DelimiterInfo("*")        
			, new DelimiterInfo("/")        
			, new DelimiterInfo("%")     
			, new DelimiterInfo(",")     
		};

		private static List<string> orderedOperators;
		static ObjectPolicyManager()
		{
			orderedOperators = new List<string>();
			orderedOperators.Add("*");
			orderedOperators.Add("/");
			orderedOperators.Add("%");
			orderedOperators.Add("+");
			orderedOperators.Add("-");
			orderedOperators.Add("!=");
			orderedOperators.Add("<>");
			orderedOperators.Add(">=");
			orderedOperators.Add("<=");
			orderedOperators.Add("=");
			orderedOperators.Add(">");
			orderedOperators.Add("<");
			orderedOperators.Add("and");
			orderedOperators.Add("or");
		}

		internal static long GetNewId()
		{
			id++;
			return id;
		}

		public static void ApplyObjectPolicies(LibraryType libraryType, List<ObjectPolicy> objectPolicies, object applyToObject, bool includeApplyToObject = true, bool searchMembers = true, bool includePrivateMembers = false, List<ObjectPolicy> appliedObjectPolicies = null)
		{
			List<ObjectInfo> objectInfos = getMembers(libraryType, applyToObject, null, includeApplyToObject, searchMembers, includePrivateMembers);

			foreach (ObjectPolicy op in objectPolicies)
			{
				List<ObjectInfo> appliedMembers = new List<ObjectInfo>();
				foreach (ObjectInfo objectInfo in objectInfos)
				{
					if (objectInfo.Type.IsClass && appliedMembers.Contains(objectInfo, objectInfoComparer))
					{
						continue;
					}

					if (op.IsAppliableTo(objectInfo))
					{
						if (objectInfo.Type.IsClass)
						{
							appliedMembers.Add(objectInfo);
						}

						ObjectPolicy opNew = (ObjectPolicy)op.Clone();
						opNew.ApplyObjectPolicies = new ApplyObjectPoliciesHandler(ApplyObjectPolicies);
						opNew.ApplyObjectPoliciesParameters = new ObjectPolicyApplyParameters(libraryType, searchMembers, includePrivateMembers);
						if (appliedObjectPolicies != null)
						{
							appliedObjectPolicies.Add(opNew);
						}
						opNew.Owner = objectInfo.Object;
					}
				}
			}
		}

		internal static List<string> GetNextPathDepth(string currentPath, List<string> paths)
		{
			List<string> retPath = new List<string>();

			int currentPathDepthCountHigherByOne = currentPath.Count(c => c == '.') + 1;
			foreach (string path in paths)
			{
				if (currentPathDepthCountHigherByOne == path.Count(c => c == '.')
					&& path.StartsWith(currentPath))
				{
					retPath.Add(path);
				}
			}

			return retPath;
		}

		private class ObjectInfoComparer : IEqualityComparer<ObjectInfo>
		{
			public bool Equals(ObjectInfo x, ObjectInfo y)
			{
				return object.Equals(x.Object, y.Object);
			}

			public int GetHashCode(ObjectInfo obj)
			{
				throw new NotImplementedException();
			}
		}

		private static List<ObjectInfo> getMembers(LibraryType libraryType, object applyToObject, List<ObjectInfo> objectInfos = null, bool includeApplyToObject = true, bool searchMembers = false, bool includePrivateMembers = false)
		{
			if (objectInfos == null)
			{
				objectInfos = new List<ObjectInfo>();
			}

			List<object> scopeMembers = new List<object>();
			if (includeApplyToObject)
			{
				scopeMembers.Add(applyToObject);
			}
			if (searchMembers)
			{
				scopeMembers.AddRange(ReflectionHelper.GetFieldAndPropertieValues(applyToObject, true, includePrivateMembers));
			}

			foreach (object scopeMember in scopeMembers)
			{
				objectInfos.Add(new ObjectInfo(scopeMember));
			}

			switch (libraryType)
			{
				case LibraryType.WindowsForms:
					if (applyToObject is WF.Control)
					{
						WF.Control.ControlCollection children = ((WF.Control)applyToObject).Controls;
						foreach (WF.Control c in children)
						{
							getMembers(libraryType, c, objectInfos, true, searchMembers, includePrivateMembers);
						}
					}
					break;
				case LibraryType.WFP:
					if (applyToObject is FrameworkElement || applyToObject is FrameworkContentElement)
					{
						System.Collections.IEnumerable children = LogicalTreeHelper.GetChildren((dynamic)applyToObject);
						foreach (object o in children)
						{
							if (o is FrameworkElement || o is FrameworkContentElement)
							{
								getMembers(libraryType, o, objectInfos, true, searchMembers, includePrivateMembers);
							}
						}
					}
					break;
			}

			return objectInfos;
		}

		public static List<ObjectPolicy> GetObjectPolicies(List<string> objectPolicies, object helper = null, GetTypeHandler getType = null)
		{
			List<XmlDocument> xmlDocs = objectPolicies.Aggregate<string, List<XmlDocument>>(
			   new List<XmlDocument>(),
			   (xds, s) => { XmlDocument xd = new XmlDocument(); xd.LoadXml(s); xds.Add(xd); return xds; });

			return GetObjectPolicies(xmlDocs, helper, getType);
		}

		public static List<ObjectPolicy> GetObjectPolicies(List<XmlDocument> objectPolicies, object helper = null, GetTypeHandler getType = null)
		{
			List<ObjectPolicy> objectPoliciesRet = new List<ObjectPolicy>();

			foreach (XmlDocument xmlDoc in objectPolicies)
			{
				objectPoliciesRet.AddRange(GetObjectPolicies(xmlDoc, helper, getType));
			}

			return objectPoliciesRet;
		}

		public static List<ObjectPolicy> GetObjectPolicies(XmlDocument objectPolicies, object helper = null, GetTypeHandler getType = null)
		{
			List<ObjectPolicy> objectPoliciesRet = new List<ObjectPolicy>();

			List<XmlNode> xmlObjectPolicies = getObjectsXmlNodes(objectPolicies);

			List<ObjectPolicy> addedObjectPolicies = new List<ObjectPolicy>();

			foreach (XmlNode xmlObjectPolicy in xmlObjectPolicies)
			{
				objectPoliciesRet.Add(getObjectPolicy(xmlObjectPolicy, null, ref addedObjectPolicies, helper, getType));
			}

			foreach (ObjectPolicy op in addedObjectPolicies)
			{
				foreach (IValueProvider<object> vp in op.ValueProvidersSetters.Values)
				{
					if (vp is ValueProviderSetter)
					{
						((ValueProviderSetter)vp).ValueChanged += op.ValueProviderValueChanged;
					}
				}
			}

			return objectPoliciesRet;
		}

		//private static bool postojiObjectPolicySaTimParentom(List<ObjectPolicy> addedObjectPolicies, ObjectPolicy parent)
		//{
		//    foreach (ObjectPolicy op in addedObjectPolicies)
		//    {
		//        if (op.Parent == parent)
		//        {
		//            return true;
		//        }
		//    }

		//    return false;
		//}

		//private static int getObjectPolicyDepth(ObjectPolicy op)
		//{
		//    ObjectPolicy parent = op;
		//    int depth = 1;
		//    while (parent != null)
		//    {
		//        depth++;
		//        parent = parent.Parent;
		//    }

		//    return depth;
		//}

		private static ObjectPolicy getObjectPolicy(XmlNode xmlObjectPolicy, ObjectPolicy parent, ref List<ObjectPolicy> addedObjectPolicies, object helper = null, GetTypeHandler getType = null)
		{
			//if (postojiObjectPolicySaTimParentom(addedObjectPolicies, parent))
			//{
			//    throw new Exception("Only one object tag per object i permitted.");
			//}
			//else if (getObjectPolicyDepth(parent) > 3)
			//{
			//    throw new Exception("Maximum object depth is 3.");
			//}

			ObjectPolicy op = new ObjectPolicy(parent, helper);
			addedObjectPolicies.Add(op);

			if (xmlObjectPolicy.Attributes["name"] != null)
			{
				op.Name = xmlObjectPolicy.Attributes["name"].Value;
			}

			if (xmlObjectPolicy.Attributes["type"] != null)
			{
				string type = xmlObjectPolicy.Attributes["type"].Value;
				if (getType != null)
				{
					op.Type = getType.Invoke(type);
				}

				if (op.Type == null)
				{
					op.Type = Type.GetType(type);
					if (op.Type == null)
					{
						List<KeyValuePair<string, string>> typeParts = StringHelper.ExtractStringParts(type, new DelimiterInfo[] { new DelimiterInfo(",") }, true);
						op.TypeString = typeParts[0].Key;
						if (typeParts.Count > 1)
						{
							op.Assembly = typeParts[1].Key;
						}
					}
				}
			}

			XmlNode xmlIdentityCondition = xmlObjectPolicy.SelectSingleNode("identityCondition");
			if (xmlIdentityCondition != null)
			{
				op.IdentityCondition = getValueProviderSetterRaw(xmlIdentityCondition.InnerXml.Trim(), op);
				foreach (KeyValuePair<string, IValueProvider<object>> vp in op.ValueProvidersSetters)
				{
					if (vp.Value is IDynamic)
					{
						((IDynamic)vp.Value).Dynamic = true;
					}
					if (vp.Value is IAllowBinding)
					{
						((IAllowBinding)vp.Value).AllowBinding = false;
					}
				}
				op.ValueProvidersSetters.Clear();
			}

			if (helper != null)
			{
				op.ValueProvidersSetters.Add("$", op.Helper_i);
			}

			//ako su elementi koji bi trebali biti u If nodu zapravo u Object nodu onda on na osnovu toga pravi ObjectPolicyIf
			//napravljeno da se ne zahtjeva If nod u Object nodu, već da Object nod može imati sve što i If node, sve osim Condition
			ObjectPolicyIf bareObjectPolicyIf = getObjectPolicyIf(xmlObjectPolicy, op, ref addedObjectPolicies, true, helper, getType);
			if (bareObjectPolicyIf != null)
			{
				op.ObjectPolicyIfs.Add(bareObjectPolicyIf);
			}

			XmlNodeList xmlObjectPolicyIfs = xmlObjectPolicy.SelectNodes("if");
			foreach (XmlNode xmlObjectPolicyIf in xmlObjectPolicyIfs)
			{
				op.ObjectPolicyIfs.Add(getObjectPolicyIf(xmlObjectPolicyIf, op, ref addedObjectPolicies, helper: helper, getType: getType));
			}

			XmlNodeList xmlSettingList;
			bool? allowBinding = null;
			bool? dynamic = null;
			XmlNode xmlSettings = xmlObjectPolicy.SelectSingleNode("settings");
			if (xmlSettings != null)
			{
				if (xmlSettings.Attributes["allowBinding"] != null)
				{
					allowBinding = bool.Parse(xmlSettings.Attributes["allowBinding"].Value);
				}
				if (xmlSettings.Attributes["dynamic"] != null)
				{
					dynamic = bool.Parse(xmlSettings.Attributes["dynamic"].Value);
				}

				xmlSettingList = xmlSettings.SelectNodes("setting");
			}
			else
			{
				xmlSettingList = xmlObjectPolicy.SelectNodes("setting");
			}

			applyPathSetting(xmlSettingList, op, allowBinding, dynamic);

			return op;
		}

		private static void applyPathSetting(XmlNodeList xmlSettings, ObjectPolicy op, bool? allowBinding, bool? dynamic)
		{
			if (allowBinding != null || dynamic != null)
			{
				foreach (IValueProvider<object> vp in op.ValueProvidersSetters.Values)
				{
					if (dynamic != null && vp is IDynamic)
					{
						((IDynamic)vp).Dynamic = dynamic.Value;
					}
					if (allowBinding != null && vp is IAllowBinding)
					{
						((IAllowBinding)vp).AllowBinding = allowBinding.Value;
					}
				}
			}
			if (allowBinding == null)
			{
				allowBinding = false;
			}
			if (dynamic == null)
			{
				dynamic = false;
			}

			if (xmlSettings.Count > 0)
			{
				Dictionary<string, List<IValueProvider<object>>> valueProviders = new Dictionary<string, List<IValueProvider<object>>>();
				foreach (KeyValuePair<string, IValueProvider<object>> vp in op.ValueProvidersSetters)
				{
					string vpPath = getFullPathForSettings(vp.Key, false);

					if (!valueProviders.ContainsKey(vpPath))
					{
						valueProviders[vpPath] = new List<IValueProvider<object>>();
					}
					valueProviders[vpPath].Add(vp.Value);
				}

				foreach (XmlNode xmlSetting in xmlSettings)
				{
					XmlAttribute xmlAllowBinding = xmlSetting.Attributes["allowBinding"];
					XmlAttribute xmlDynamic = xmlSetting.Attributes["dynamic"];
					XmlAttribute xmlPath = xmlSetting.Attributes["path"];
					XmlAttribute xmlPathFrom = xmlSetting.Attributes["pathFrom"];
					XmlAttribute xmlPathTo = xmlSetting.Attributes["pathTo"];

					bool allowBindingInd = allowBinding.Value;
					bool dynamicInd = dynamic.Value;
					if (xmlAllowBinding != null)
					{
						allowBindingInd = bool.Parse(xmlAllowBinding.Value);
					}
					if (xmlDynamic != null)
					{
						dynamicInd = bool.Parse(xmlDynamic.Value);
					}
					string path = "";
					string pathFrom = "";
					string pathTo = "";
					if (xmlPath != null)
					{
						path = xmlPath.Value;
					}
					if (xmlPathFrom != null)
					{
						pathFrom = xmlPathFrom.Value;
					}
					if (xmlPathTo != null)
					{
						pathTo = xmlPathTo.Value;
					}

					applyPathSetting(op, valueProviders, allowBindingInd, dynamicInd, path, pathFrom, pathTo);
				}
			}
		}

		private static void applyPathSetting(ObjectPolicy op, Dictionary<string, List<IValueProvider<object>>> valueProviders, bool allowBinding, bool dynamic, string path, string pathFrom, string pathTo)
		{
			if (path.Length > 0)
			{
				pathFrom = path;
				pathTo = path;
			}

			List<KeyValuePair<string, string>> pathPartsFrom = getPathPartsForSettings(pathFrom, true);
			List<KeyValuePair<string, string>> pathPartsTo = getPathPartsForSettings(pathTo, true);
			List<string> pathsForSetting = new List<string>();

			StringBuilder spPath = new StringBuilder();
			for (int i = 0; i < pathPartsTo.Count; i++)
			{
				KeyValuePair<string, string> pathPartTo = pathPartsTo[i];

				spPath.Append(pathPartTo.Key);
				if (i < pathPartsFrom.Count)
				{
					if (!pathPartTo.Key.Equals(pathPartsFrom[i].Key))
					{
						throw new Exception("Invalid settings path.");
					}
				}
				if (i > pathPartsFrom.Count - 2)
				{
					pathsForSetting.Add(spPath.ToString());
				}
				spPath.Append(pathPartTo.Value);
			}

			foreach (string pathSetting in pathsForSetting)
			{
				//u ovaj if ulazi ako cjeloviti path ne postoji, u tom slučaju pronalazi dio patha koji postoji i na taj path nadodaje nepostojece i stavlja ih u listu ValueProvidersSettersStandAlone
				if (!valueProviders.ContainsKey(pathSetting))
				{
					string bestMatchingPath = GetBestMatchingPath(pathSetting, valueProviders.Keys.ToList());
					string pathToBeAdded = pathSetting.Remove(0, bestMatchingPath.Length);
					List<Dictionary<string, IValueProvider<object>>> valueProvidersToAddPathTo = new List<Dictionary<string, IValueProvider<object>>>();
					if (bestMatchingPath.Length == 0)
					{
						valueProvidersToAddPathTo.Add(new Dictionary<string, IValueProvider<object>>());
					}
					else
					{
						foreach (IValueProvider<object> vp in valueProviders[bestMatchingPath])
						{
							Dictionary<string, IValueProvider<object>> valueProviderToAddPathTo = new Dictionary<string, IValueProvider<object>>();
							valueProviderToAddPathTo.Add(vp is IPathable ? ((IPathable)vp).Path : bestMatchingPath, vp);
							valueProvidersToAddPathTo.Add(valueProviderToAddPathTo);
						}
					}

					foreach (Dictionary<string, IValueProvider<object>> vp in valueProvidersToAddPathTo)
					{
						string fullPath;
						if (vp.Count != 0)
						{
							fullPath = vp.First().Key + pathToBeAdded;
						}
						else
						{
							fullPath = pathToBeAdded;
						}
						Dictionary<string, IValueProvider<object>> addedNewValueProviderSetter = new Dictionary<string, IValueProvider<object>>();
						IValueProvider<object> vpNew = getValueProviderSetter(fullPath, op, addedNewValueProviderSetter: addedNewValueProviderSetter, valueProvidersSettersCheckOverride: vp);
						op.ValueProvidersSettersStandAlone[fullPath] = vpNew;
						foreach (KeyValuePair<string, IValueProvider<object>> nvps in addedNewValueProviderSetter)
						{
							string pathForSetting = getFullPathForSettings(nvps.Key, true);
							if (!valueProviders.ContainsKey(pathForSetting))
							{
								valueProviders[pathForSetting] = new List<IValueProvider<object>>();
							}
							valueProviders[pathForSetting].Add(nvps.Value);
						}
					}
				}
				foreach (IValueProvider<object> vp in valueProviders[pathSetting])
				{
					if (vp is IDynamic)
					{
						((IDynamic)vp).Dynamic = dynamic;
					}
					if (vp is IAllowBinding)
					{
						((IAllowBinding)vp).AllowBinding = allowBinding;
					}
				}
			}
		}

		internal static string GetBestMatchingPath(string fullPath, List<string> allPaths)
		{
			string bestMatchingPath = "";
			int bestMatchingPathLen = 0;
			int fullPathLen = fullPath.Length;
			foreach (string path in allPaths)
			{
				if (path.Length > bestMatchingPathLen
					&& (
						(path.Length < fullPathLen && fullPath.StartsWith(path + "."))
						|| (path.Length == fullPathLen && fullPath.Equals(path))
					)
				)
				{
					bestMatchingPath = path;
					bestMatchingPathLen = path.Length;
					if (bestMatchingPathLen == fullPathLen)
					{
						break;
					}
				}
			}

			return bestMatchingPath;
		}

		internal static List<string> MergePaths(List<string> paths)
		{
			return paths.Where(path =>
				{
					int pathLen = path.Length;
					foreach (string p in paths)
					{
						if (p.Length > pathLen && p.StartsWith(path))
						{
							return false;
						}
					}

					return true;
				}).ToList();
		}

		private static string getFullPathForSettings(string path, bool trim)
		{
			return getPathPartsForSettings(path, trim).Aggregate<KeyValuePair<string, string>, StringBuilder, string>(
				new StringBuilder(),
				(sb, current) => { sb.Append(current.Key); sb.Append(current.Value); return sb; },
				(sb) => sb.ToString()
			);
		}

		private static List<KeyValuePair<string, string>> getPathPartsForSettings(string path, bool trim)
		{
			List<KeyValuePair<string, string>> pathParts = StringHelper.ExtractStringParts(path, new DelimiterInfo[] { new DelimiterInfo(".") }, trim);

			if (pathParts.Count > 1 && pathParts[0].Key == "this")
			{
				pathParts.RemoveAt(0);
			}

			for (int i = 0; i < pathParts.Count; i++)
			{
				List<KeyValuePair<string, string>> bracketParts = StringHelper.ExtractStringParts(pathParts[i].Key, new DelimiterInfo[] { new DelimiterInfo("("), new DelimiterInfo(")") }, trim);
				if (bracketParts.Count < 2)
				{
					continue;
				}

				StringBuilder sb = new StringBuilder();
				KeyValuePair<string, string> bpFirst = bracketParts[0];
				KeyValuePair<string, string> bpLast = bracketParts[bracketParts.Count - 1];
				sb.Append(bpFirst.Key);
				sb.Append(bpFirst.Value);
				sb.Append(")");
				sb.Append(bpLast.Key);
				pathParts[i] = new KeyValuePair<string, string>(sb.ToString(), pathParts[i].Value);
			}

			return pathParts;
		}

		private static ObjectPolicyIf getObjectPolicyIf(XmlNode xmlObject, ObjectPolicy parent, ref List<ObjectPolicy> addedObjectPolicies, bool isObjectPolicy = false, object helper = null, GetTypeHandler getType = null)
		{
			List<Setter> setters = new List<Setter>();
			XmlNodeList xmlSettersList;
			bool allowUndo = false;
			XmlNode xmlSetters = xmlObject.SelectSingleNode("setters");
			if (xmlSetters != null)
			{
				if (xmlSetters.Attributes["allowUndo"] != null)
				{
					allowUndo = bool.Parse(xmlSetters.Attributes["allowUndo"].Value);
				}

				xmlSettersList = xmlSetters.SelectNodes("setter");
			}
			else
			{
				xmlSettersList = xmlObject.SelectNodes("setter");
			}

			foreach (XmlNode xmlSetter in xmlSettersList)
			{
				setters.Add(getSetter(xmlSetter, allowUndo, parent));
			}

			List<ObjectPolicy> objectPolicies = new List<ObjectPolicy>();
			List<XmlNode> xmlObjectPolicies = getObjectsXmlNodes(xmlObject);
			foreach (XmlNode xmlOP in xmlObjectPolicies)
			{
				objectPolicies.Add(getObjectPolicy(xmlOP, parent, ref addedObjectPolicies, helper, getType: getType));
			}

			if (!isObjectPolicy || setters.Count > 0 || objectPolicies.Count > 0)
			{
				ObjectPolicyIf opif = new ObjectPolicyIf();

				opif.Setters.AddRange(setters);
				opif.ObjectPolicies.AddRange(objectPolicies);

				if (!isObjectPolicy)
				{
					if (xmlObject.Attributes["else"] != null && bool.Parse(xmlObject.Attributes["else"].Value))
					{
						opif.IsElseIf = true;
					}

					XmlNode xmlCondition = xmlObject.SelectSingleNode("condition");
					if (xmlCondition != null)
					{
						opif.Condition = getValueProviderSetterRaw(xmlCondition.InnerXml.Trim(), parent);
					}

					XmlNodeList xmlIfs = xmlObject.SelectNodes("if");
					foreach (XmlNode xmlIf in xmlIfs)
					{
						opif.Add(getObjectPolicyIf(xmlIf, parent, ref addedObjectPolicies, helper: helper, getType: getType));
					}
				}

				return opif;
			}

			return null;
		}

		private static List<XmlNode> getObjectsXmlNodes(XmlNode node)
		{
			List<XmlNode> xmlNodes = new List<XmlNode>();
			XmlNodeList xmlObjectPolicies = node.SelectNodes("objects/object");
			foreach (XmlNode xn in xmlObjectPolicies)
			{
				xmlNodes.Add(xn);
			}
			xmlObjectPolicies = node.SelectNodes("object");
			foreach (XmlNode xn in xmlObjectPolicies)
			{
				xmlNodes.Add(xn);
			}

			return xmlNodes;
		}

		private static IValueProvider<object> getValueProviderSetterNew(string pathPart, ObjectPolicy op, string pathPrefix = "", IValueProvider<object> targetValueProvider = null, Dictionary<string, IValueProvider<object>> aditionalValueProvidersSetters = null, bool pathableElement = true, bool addToValueProvidersSettersDictionary = true)
		{
			IValueProvider<object> vps = null;

			if (pathPrefix.Length == 0)
			{
				if (pathPart == "this")
				{
					vps = new ValueProviderSetter(target: op, propertyOrFieldName: "Owner", path: "");
					if (addToValueProvidersSettersDictionary)
					{
						op.ValueProvidersSetters.Add(pathPart, vps);
					}
					return vps;
				}
				else if (pathPart.IndexOf("@") == 0)
				{
					string name = pathPart.Substring(1, pathPart.Length - 1);
					ObjectPolicy parent = op;
					while (parent != null)
					{
						if (parent.Name == name)
						{
							//return getValueProviderSetter("this", parent);
							vps = new ValueProviderSetter(target: parent, propertyOrFieldName: "Owner", path: pathPart);
							if (addToValueProvidersSettersDictionary)
							{
								op.ValueProvidersSetters.Add(pathPart, vps);
							}
							return vps;
						}
						parent = parent.Parent;
					}
					throw new Exception("Parent does not exist.");
				}
				else if (targetValueProvider == null)
				{
					targetValueProvider = getValueProviderSetter("this", op);
				}
			}

			//method
			if (pathPart.Contains("("))
			{
				string methodParametersName;
				int index;
				StringHelper.ExtractInner(pathPart, "(", ")", out methodParametersName, out index);
				methodParametersName = methodParametersName.Trim();
				string methodName = pathPart.Substring(0, index);
				ObjectArrayValueProvider methodParameters = null;
				if (methodParametersName.Length > 0)
				{
					methodParameters = (ObjectArrayValueProvider)aditionalValueProvidersSetters[methodParametersName];
				}
				vps = new MethodValueProvider(targetValueProvider: targetValueProvider, methodParameters: methodParameters, methodName: methodName, path: pathPart);
			}
			else
			{
				vps = new ValueProviderSetter(targetValueProvider: targetValueProvider, propertyOrFieldName: pathPart);
			}

			if (pathableElement)
			{
				StringBuilder fullPath = new StringBuilder();
				fullPath.Append(pathPrefix);
				if (pathPrefix.Length > 0)
				{
					fullPath.Append(".");
				}
				fullPath.Append(pathPart);

				if (addToValueProvidersSettersDictionary)
				{
					op.ValueProvidersSetters.Add(fullPath.ToString(), vps);
				}
			}

			return vps;
		}

		private static IValueProvider<object> getValueProviderSetter(string path, ObjectPolicy op, Dictionary<string, IValueProvider<object>> aditionalValueProvidersSetters = null, Dictionary<string, IValueProvider<object>> addedNewValueProviderSetter = null, bool addToValueProvidersSettersDictionary = true, Dictionary<string, IValueProvider<object>> valueProvidersSettersCheckOverride = null)
		{
			Dictionary<string, IValueProvider<object>> valueProvidersSetters = valueProvidersSettersCheckOverride == null ? op.ValueProvidersSetters : valueProvidersSettersCheckOverride;

			if (valueProvidersSetters.ContainsKey(path))
			{
				return valueProvidersSetters[path];
			}

			List<KeyValuePair<string, string>> pathElements = StringHelper.ExtractStringParts(path, new DelimiterInfo[] { new DelimiterInfo(".") });
			bool pathableElements = true;
			List<string> pathElementsToManifest = new List<string>();
			IValueProvider<object> valueProviderSetter = null;

			path = "";

			KeyValuePair<string, string> firstPathElement = pathElements[0];
			if (firstPathElement.Key.Substring(0, 1) == "#")
			{
				valueProviderSetter = aditionalValueProvidersSetters[firstPathElement.Key];
				if (valueProviderSetter is IPathable)
				{
					path = ((IPathable)valueProviderSetter).Path;
				}
				else
				{
					pathableElements = false;
				}
				pathElements.RemoveAt(0);
				pathElementsToManifest.AddRange(pathElements.Select<KeyValuePair<string, string>, string>(part => { return part.Key; }));
			}
			else
			{
				pathElements.Reverse();
				pathElementsToManifest.Add(pathElements[0].Key);
				pathElements.RemoveAt(0);

				while (pathElements.Count > 0)
				{
					StringBuilder sbPath = new StringBuilder();
					foreach (KeyValuePair<string, string> pathPart in pathElements)
					{
						if (sbPath.Length > 0)
						{
							sbPath.Insert(0, ".");
						}
						sbPath.Insert(0, pathPart.Key);
					}

					path = sbPath.ToString();

					if (valueProvidersSetters.ContainsKey(path))
					{
						valueProviderSetter = valueProvidersSetters[path];
						break;
					}

					pathElementsToManifest.Insert(0, pathElements[0].Key);
					pathElements.RemoveAt(0);

					path = "";
				}
			}

			foreach (string pathPart in pathElementsToManifest)
			{
				valueProviderSetter = getValueProviderSetterNew(pathPart, op, path, valueProviderSetter, aditionalValueProvidersSetters, pathableElements, addToValueProvidersSettersDictionary);
				path += (path.Length > 0 ? "." : "") + pathPart;
				if (addedNewValueProviderSetter != null)
				{
					addedNewValueProviderSetter[path] = valueProviderSetter;
				}
			}

			return valueProviderSetter;
		}

		private static Setter getSetter(XmlNode xmlSetter, bool allowUndo, ObjectPolicy parent)
		{
			Setter setter = new Setter();

			setter.AllowUndo = allowUndo;
			if (xmlSetter.Attributes["allowUndo"] != null)
			{
				setter.AllowUndo = bool.Parse(xmlSetter.Attributes["allowUndo"].Value);
			}

			string path = "";
			if (xmlSetter.Attributes["path"] != null)
			{
				path = xmlSetter.Attributes["path"].Value;
			}
			else
			{
				XmlNode xmlSetterPath = xmlSetter.SelectSingleNode("path");
				if (xmlSetterPath != null)
				{
					path = xmlSetterPath.InnerText;
				}
			}

			IValueSetter<object> ovp = (IValueSetter<object>)getValueProviderSetterRaw(path, parent);
			setter.ValueSetter = ovp;

			XmlNode xmlSetterValue = xmlSetter.SelectSingleNode("value");
			if (xmlSetterValue != null)
			{
				setter.Value = getValueProviderSetterRaw(xmlSetterValue.InnerText.Trim(), parent);
			}
			else
			{
				XmlNode xmlSetterValueXAML = xmlSetter.SelectSingleNode("valueXAML");
				if (xmlSetterValueXAML != null)
				{
					setter.ValueObject = xmlSetterValueXAML.InnerXml;
				}
				else
				{
					setter.Value = getValueProviderSetterRaw(xmlSetter.InnerText.Trim(), parent);
				}
			}

			return setter;
		}

		private static IValueProvider<object> getValueProviderSetterRaw(string rawInput, ObjectPolicy op)
		{
			Dictionary<string, IValueProvider<object>> valueProviders = new Dictionary<string, IValueProvider<object>>();
			rawInput = convertStringValuesToRawValueProviders(rawInput, ref valueProviders);

			List<KeyValuePair<string, string>> parts = StringHelper.ExtractStringParts(rawInput, new DelimiterInfo[] { new DelimiterInfo("("), new DelimiterInfo(")"), new DelimiterInfo("["), new DelimiterInfo("]") });
			IValueProvider<object> valueProvider = null;
			while (true)
			{
				int? index = getIndexOfFirstMostInnerBracketsContent(parts);

				List<KeyValuePair<string, string>> paths;
				if (index != null)
				{
					KeyValuePair<string, string> beforePart = parts[index.Value - 1];
					KeyValuePair<string, string> currPart = parts[index.Value];
					KeyValuePair<string, string> afterPart = parts[index.Value + 1];

					paths = StringHelper.ExtractStringParts(currPart.Key, operatorDelimiters, true);

					convertPathsToValueProviders(ref paths, op, ref valueProviders);

					string pathBeforeMethod;
					string pathAfterMethod;
					KeyValuePair<string, string> methodPath = getMethodPath(beforePart.Key, afterPart.Key, out pathBeforeMethod, out pathAfterMethod);

					string valueProviderId;
					valueProvider = combineValueProviders(paths, valueProviders, out valueProviderId, currPart.Value == "]" || methodPath.Key.Length > 0);

					if (methodPath.Key.Length > 0)
					{
						List<KeyValuePair<string, string>> methodProvider = new List<KeyValuePair<string, string>>();
						string methodPathString = methodPath.Key + valueProviderId + methodPath.Value;
						methodProvider.Add(new KeyValuePair<string, string>(methodPathString, ""));
						Dictionary<string, IValueProvider<object>> newValueProviders = convertPathsToValueProviders(ref methodProvider, op, ref valueProviders);
						parts[index.Value - 1] = new KeyValuePair<string, string>(pathBeforeMethod + newValueProviders.First().Key + pathAfterMethod, afterPart.Value);
					}
					else
					{
						parts[index.Value - 1] = new KeyValuePair<string, string>(beforePart.Key + valueProviderId + afterPart.Key, afterPart.Value);
					}

					parts.RemoveAt(index.Value);
					parts.RemoveAt(index.Value);
				}
				else
				{
					paths = StringHelper.ExtractStringParts(parts[0].Key, operatorDelimiters, true);

					convertPathsToValueProviders(ref paths, op, ref valueProviders);
					string valueProviderId;
					valueProvider = combineValueProviders(paths, valueProviders, out valueProviderId, false);
					break;
				}
			}

			return valueProvider;
		}

		private static KeyValuePair<string, string> getMethodPath(string pathBeforeParams, string pathAfterParams, out string pathBeforeMethod, out string pathAfterMethod)
		{
			DelimiterInfo[] delimiters = new DelimiterInfo[operatorDelimiters.Length + 1];

			operatorDelimiters.CopyTo(delimiters, 0);
			delimiters[operatorDelimiters.Length] = new DelimiterInfo(".");

			KeyValuePair<string, string> lastPartBefore = StringHelper.ExtractStringParts(pathBeforeParams, operatorDelimiters).Last();
			KeyValuePair<string, string> firstPartAfter = StringHelper.ExtractStringParts(pathAfterParams, delimiters).First();

			if (lastPartBefore.Key.Trim().Length == 0)
			{
				pathBeforeMethod = "";
				pathAfterMethod = "";
				return new KeyValuePair<string, string>("", "");
			}

			pathBeforeMethod = pathBeforeParams.Substring(0, pathBeforeParams.Length - lastPartBefore.Key.Length) + " ";
			pathAfterMethod = " " + pathAfterParams.Substring(firstPartAfter.Key.Length, pathAfterParams.Length - firstPartAfter.Key.Length);
			return new KeyValuePair<string, string>(lastPartBefore.Key.TrimEnd() + "(", ")" + firstPartAfter.Key.TrimStart());
		}

		private static Dictionary<string, IValueProvider<object>> convertPathsToValueProviders(ref List<KeyValuePair<string, string>> paths, ObjectPolicy op, ref Dictionary<string, IValueProvider<object>> valueProviders)
		{
			Dictionary<string, IValueProvider<object>> newValueProviders = new Dictionary<string, IValueProvider<object>>();

			for (int i = 0; i < paths.Count; i++)
			{
				KeyValuePair<string, string> pathPart = paths[i];

				if (pathPart.Key.Trim().Length == 0)
				{
					paths.RemoveAt(i);
					continue;
				}

				List<KeyValuePair<string, string>> parts = StringHelper.ExtractStringParts(pathPart.Key, new DelimiterInfo[] { new DelimiterInfo(".") }, true);
				if (parts.Count == 1)
				{
					if (parts[0].Key.Substring(0, 1) == "#")
					{
						continue;
					}
				}
				else if (parts.Count > 1 && parts[0].Key == "this")
				{
					parts.RemoveAt(0);
				}

				string valueProviderId;
				RawValueProvider value;
				if (convertSpecialValueToRawValueProvider(pathPart.Key.Trim(), out value))
				{
					valueProviderId = "#" + GetNewId().ToString();
					paths[i] = new KeyValuePair<string, string>(valueProviderId, pathPart.Value);
					valueProviders.Add(valueProviderId, value);
					newValueProviders.Add(valueProviderId, value);
					continue;
				}

				StringBuilder parsedPart = new StringBuilder();
				foreach (KeyValuePair<string, string> part in parts)
				{
					parsedPart.Append(part.Key);
					parsedPart.Append(part.Value);
				}

				IValueProvider<object> valueProvider = getValueProviderSetter(parsedPart.ToString(), op, valueProviders);
				valueProviderId = "#" + GetNewId().ToString();
				paths[i] = new KeyValuePair<string, string>(valueProviderId, pathPart.Value);
				valueProviders.Add(valueProviderId, valueProvider);
				newValueProviders.Add(valueProviderId, valueProvider);
			}

			return newValueProviders;
		}

		private static bool convertSpecialValueToRawValueProvider(string text, out RawValueProvider value)
		{
			value = null;

			if (text == "true" || text == "false")
			{
				value = new RawValueProvider(bool.Parse(text));
				return true;
			}
			else if (text == "null")
			{
				value = new RawValueProvider(null);
				return true;
			}
			else
			{
				object number = getNumber(text);
				if (number != null)
				{
					value = new RawValueProvider(number);
					return true;
				}
			}

			return false;
		}

		private static object getNumber(string text)
		{
			StringBuilder stringNumber = new StringBuilder();

			bool wasDot = false;
			bool wasNumber = false;
			for (int i = 0; i < text.Length; i++)
			{
				char c = text[i];
				if (char.IsNumber(c))
				{
					wasNumber = true;
					stringNumber.Append(c);
				}
				else if (c == '.' && !wasDot)
				{
					wasDot = true;
					stringNumber.Append(c);
					if (i == 0)
					{
						stringNumber.Insert(0, "0");
					}
					else if (i == text.Length - 1)
					{
						stringNumber.Append("0");
					}
				}
				else
				{
					return null;
				}
			}

			if (wasNumber)
			{
				if (wasDot)
				{
					return decimal.Parse(stringNumber.ToString(), System.Globalization.NumberFormatInfo.InvariantInfo);
				}
				else
				{
					return int.Parse(stringNumber.ToString());
				}
			}
			else
			{
				return null;
			}
		}

		private static IValueProvider<object> combineValueProviders(List<KeyValuePair<string, string>> valueProvidersNamesAndOperators, Dictionary<string, IValueProvider<object>> valueProviders, out string valueProviderId, bool isObjectArray = false)
		{
			valueProviderId = "";

			if (valueProvidersNamesAndOperators.Count == 0)
			{
				return null;
			}

			foreach (string oper in orderedOperators)
			{
				for (int i = 0; i < valueProvidersNamesAndOperators.Count; i++)
				{
					KeyValuePair<string, string> currValueProviderId = valueProvidersNamesAndOperators[i];

					if (!currValueProviderId.Value.Equals(oper, StringComparison.CurrentCultureIgnoreCase))
					{
						continue;
					}

					MergedValueProvider mvp;
					Condition c;

					IValueProvider<object> newValueProvider = null;
					switch (oper)
					{
						case "*":
							mvp = new MergedValueProvider();
							newValueProvider = mvp;
							mvp.Merger = new MultiplyMerger();
							mvp.Value1 = valueProviders[currValueProviderId.Key];
							mvp.Value2 = valueProviders[valueProvidersNamesAndOperators[i + 1].Key];
							break;
						case "/":
							mvp = new MergedValueProvider();
							newValueProvider = mvp;
							mvp.Merger = new DivisionMerger();
							mvp.Value1 = valueProviders[currValueProviderId.Key];
							mvp.Value2 = valueProviders[valueProvidersNamesAndOperators[i + 1].Key];
							break;
						case "%":
							mvp = new MergedValueProvider();
							newValueProvider = mvp;
							mvp.Merger = new ModMerger();
							mvp.Value1 = valueProviders[currValueProviderId.Key];
							mvp.Value2 = valueProviders[valueProvidersNamesAndOperators[i + 1].Key];
							break;
						case "+":
							mvp = new MergedValueProvider();
							newValueProvider = mvp;
							mvp.Merger = new PlusMerger();
							mvp.Value1 = valueProviders[currValueProviderId.Key];
							mvp.Value2 = valueProviders[valueProvidersNamesAndOperators[i + 1].Key];
							break;
						case "-":
							mvp = new MergedValueProvider();
							newValueProvider = mvp;
							mvp.Merger = new MinusMerger();
							mvp.Value1 = valueProviders[currValueProviderId.Key];
							mvp.Value2 = valueProviders[valueProvidersNamesAndOperators[i + 1].Key];
							break;
						case "!=":
							c = new Condition();
							newValueProvider = c;
							c.Comparer = new NotEqualsComparer();
							c.Value1 = valueProviders[currValueProviderId.Key];
							c.Value2 = valueProviders[valueProvidersNamesAndOperators[i + 1].Key];
							break;
						case "<>":
							c = new Condition();
							newValueProvider = c;
							c.Comparer = new NotEqualsComparer();
							c.Value1 = valueProviders[currValueProviderId.Key];
							c.Value2 = valueProviders[valueProvidersNamesAndOperators[i + 1].Key];
							break;
						case ">=":
							c = new Condition();
							newValueProvider = c;
							c.Comparer = new BiggerOrEqualToComparer();
							c.Value1 = valueProviders[currValueProviderId.Key];
							c.Value2 = valueProviders[valueProvidersNamesAndOperators[i + 1].Key];
							break;
						case "<=":
							c = new Condition();
							newValueProvider = c;
							c.Comparer = new LesOrEqualToComparer();
							c.Value1 = valueProviders[currValueProviderId.Key];
							c.Value2 = valueProviders[valueProvidersNamesAndOperators[i + 1].Key];
							break;
						case "=":
							c = new Condition();
							newValueProvider = c;
							c.Comparer = new EqualityComparer();
							c.Value1 = valueProviders[currValueProviderId.Key];
							c.Value2 = valueProviders[valueProvidersNamesAndOperators[i + 1].Key];
							break;
						case ">":
							c = new Condition();
							newValueProvider = c;
							c.Comparer = new BiggerComparer();
							c.Value1 = valueProviders[currValueProviderId.Key];
							c.Value2 = valueProviders[valueProvidersNamesAndOperators[i + 1].Key];
							break;
						case "<":
							c = new Condition();
							newValueProvider = c;
							c.Comparer = new LesserComparer();
							c.Value1 = valueProviders[currValueProviderId.Key];
							c.Value2 = valueProviders[valueProvidersNamesAndOperators[i + 1].Key];
							break;
						case "and":
							c = new Condition();
							newValueProvider = c;
							c.Comparer = new AndComparer();
							c.Value1 = valueProviders[currValueProviderId.Key];
							c.Value2 = valueProviders[valueProvidersNamesAndOperators[i + 1].Key];
							break;
						case "or":
							c = new Condition();
							newValueProvider = c;
							c.Comparer = new OrComparer();
							c.Value1 = valueProviders[currValueProviderId.Key];
							c.Value2 = valueProviders[valueProvidersNamesAndOperators[i + 1].Key];
							break;
					}

					if (newValueProvider != null)
					{
						string valueProviderIdTemp = "#" + GetNewId().ToString();
						valueProviders.Add(valueProviderIdTemp, newValueProvider);
						valueProvidersNamesAndOperators[i] = new KeyValuePair<string, string>(valueProviderIdTemp, valueProvidersNamesAndOperators[i + 1].Value);
						valueProvidersNamesAndOperators.RemoveAt(i + 1);
						i--;
					}
				}
			}

			ObjectArrayValueProvider objectArrayValueProvider = null;

			if (valueProvidersNamesAndOperators.Count > 1 || isObjectArray)
			{
				objectArrayValueProvider = new ObjectArrayValueProvider();
				List<IValueProvider<object>> objectArrayValueProviders = new List<IValueProvider<object>>();
				foreach (KeyValuePair<string, string> vp in valueProvidersNamesAndOperators)
				{
					objectArrayValueProvider.ValueProviders.Add(valueProviders[vp.Key]);
				}

				valueProviderId = "#" + GetNewId().ToString();
				valueProviders.Add(valueProviderId, objectArrayValueProvider);
			}
			else
			{
				valueProviderId = valueProvidersNamesAndOperators[0].Key;
			}

			return objectArrayValueProvider == null ? valueProviders[valueProvidersNamesAndOperators[0].Key] : (IValueProvider<object>)objectArrayValueProvider;
		}

		private static int? getIndexOfFirstMostInnerBracketsContent(List<KeyValuePair<string, string>> parts)
		{
			for (int i = 0; i < parts.Count; i++)
			{
				if (parts[i].Value == ")" || parts[i].Value == "]")
				{
					return i;
				}
			}

			return null;
		}

		private static string convertStringValuesToRawValueProviders(string rawInput, ref Dictionary<string, IValueProvider<object>> rawValueProviders)
		{
			StringBuilder condition = new StringBuilder();
			bool isOpen = false;
			int curr = -1;
			int prev;
			int from = 0;
			int to = 0;
			while (true)
			{
				prev = curr;
				curr = rawInput.IndexOf('"', curr + 1);
				if (curr > -1)
				{
					if (isOpen && rawInput.Length > curr + 1 && rawInput.Substring(curr + 1, 1).Equals(@""""))
					{
						curr++;
						continue;
					}
					else
					{
						isOpen = !isOpen;
					}
					if (prev == -1)
					{
						to = curr;
					}
				}

				if (curr == -1 || (isOpen && prev != -1 && curr - prev > 1))
				{
					if (prev > -1)
					{
						condition.Append(rawInput.Substring(from, to - from));

						RawValueProvider rvp = new RawValueProvider();
						string value = rawInput.Substring(to + 1, prev - to - 1);
						rvp.Value = value.Replace(@"""""", @"""");
						string valueId = "#" + GetNewId().ToString();
						rawValueProviders.Add(valueId, rvp);

						condition.Append(valueId);
					}

					if (curr != -1)
					{
						from = prev + 1;
						to = curr;
					}
					else
					{
						condition.Append(rawInput.Substring(prev + 1, rawInput.Length - prev - 1));
						break;
					}
				}
			}

			return condition.ToString();
		}

		public static XmlSchema GetValidationSchema()
		{
			return XmlSchema.Read(typeof(ObjectPolicyManager).Assembly.GetManifestResourceStream("ObjectPolicy.ObjectPolicySchema.xsd"), null);
		}
	}
}
