using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace ObjectPolicy
{
	public class Serializer
	{
		private Serializer()
		{ }

		public static void Serialize(object obj, string fileName)
		{
			Stream s = System.IO.File.Create(fileName);
			BinaryFormatter b = new BinaryFormatter();
			b.Serialize(s, obj);
			s.Close();
		}

		public static object DeSerialize(string fileName)
		{
			Stream s = System.IO.File.Open(fileName, FileMode.Open);
			BinaryFormatter b = new BinaryFormatter();
			object obj = b.Deserialize(s);
			s.Close();

			return obj;
		}

		public static MemoryStream Serialize(object obj)
		{
			MemoryStream s = new MemoryStream();
			BinaryFormatter b = new BinaryFormatter();
			b.Serialize(s, obj);
			return s;
		}

		public static object DeSerialize(MemoryStream s)
		{
			BinaryFormatter b = new BinaryFormatter();
			s.Position = 0;
			object obj = b.Deserialize(s);
			s.Close();

			return obj;
		}
	}
}
