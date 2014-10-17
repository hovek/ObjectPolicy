using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ObjectPolicy
{
	public struct DelimiterInfo
	{
		public string Delimiter;
		public bool MustBeWhiteSpaceAroundDelimiter;
		public StringComparison StringComparison;

		public DelimiterInfo(string delimiter, bool mustBeWhiteSpaceAroundDelimiter = false, StringComparison stringComparison = System.StringComparison.CurrentCulture)
		{
			Delimiter = delimiter;
			MustBeWhiteSpaceAroundDelimiter = mustBeWhiteSpaceAroundDelimiter;
			StringComparison = stringComparison;
		}
	}

	public class StringHelper
	{
		public static string ExtractInner(string content, string last, string firstAfterLast, out string extracted, out int index)
		{
			index = content.LastIndexOf(last);
			if (index > -1)
			{
				int i = index + last.Length;
				int i2 = content.IndexOf(firstAfterLast, i);
				if (i2 > -1)
				{
					extracted = content.Substring(i, i2 - i);
					return content.Remove(i, i2 - i);
				}
			}

			extracted = "";
			return content;
		}

		public static List<KeyValuePair<string, string>> ExtractStringParts(string text
			, DelimiterInfo[] delimiters
			, bool trimResults = false)
		{
			List<KeyValuePair<string, string>> parts = new List<KeyValuePair<string, string>>();
			parts.Add(new KeyValuePair<string, string>(text, ""));

			for (int i = 0; i < delimiters.Length; i++)
			{
				DelimiterInfo delimiterInfo = delimiters[i];
				int delimiterLen = delimiterInfo.Delimiter.Length;
				List<KeyValuePair<string, string>> partsPrev = new List<KeyValuePair<string, string>>(parts);
				parts.Clear();

				for (int ip = 0; ip < partsPrev.Count; ip++)
				{
					string part = partsPrev[ip].Key;
					int partLen = part.Length;
					int rez = part.IndexOf(delimiterInfo.Delimiter, 0, delimiterInfo.StringComparison);
					if (rez > -1
						&&
						(!delimiterInfo.MustBeWhiteSpaceAroundDelimiter
							|| (
								rez > 0 && string.IsNullOrWhiteSpace(part.Substring(rez - 1, 1))
								&& rez + delimiterLen < partLen && string.IsNullOrWhiteSpace(part.Substring(rez + delimiterLen, 1))
							)
						)
					)
					{
						parts.Add(new KeyValuePair<string, string>(part.Substring(0, rez), delimiterInfo.Delimiter));
						partsPrev.Insert(ip + 1, new KeyValuePair<string, string>(part.Substring(rez + delimiterLen, partLen - rez - delimiterLen), partsPrev[ip].Value));
					}
					else
					{
						parts.Add(new KeyValuePair<string, string>(part, partsPrev[ip].Value));
					}
				}
			}

			if (trimResults)
			{
				for (int i = 0; i < parts.Count; i++)
				{
					KeyValuePair<string, string> p = parts[i];
					parts[i] = new KeyValuePair<string, string>(p.Key.Trim(), p.Value);
				}
			}

			return parts;
		}
	}
}
