using System;
using System.Collections.Generic;

namespace CocosTest
{
	public static class Util
	{
		public static void Log(string s, params object[] arg)
		{
			if (s == null)
			{
				return;
			}

			var output = string.Format("[{0}] {1}", s, DateTime.Now.ToString("HH:mm:ss.fff"));
			Console.WriteLine(output, arg);
		}

		public static void Log(string s)
		{
			Log(s, null);
		}

		public static void Shuffle<T>(this IList<T> list)  
		{  
			var rng = new Random();  
			int n = list.Count;  
			while (n > 1) {  
				n--;  
				int k = rng.Next(n + 1);  
				T value = list[k];  
				list[k] = list[n];  
				list[n] = value;  
			}  
		}

		/// <summary>
		/// Localizes a string.
		/// </summary>
		/// <param name="constant">string constant to localize</param>
		/// <param name="arg">optional arguments</param>
		/// <returns>localized string</returns>
		public static string Localize(string constant, params object[] arg)
		{
			if (string.IsNullOrWhiteSpace(constant))
			{
				return string.Empty;
			}

			string s = constant;

			try
			{
				s = string.Format(constant, arg);
			}
			catch (Exception ex)
			{
				Log("Failed to localize: '{0}'; error: {1}", constant, ex);
			}

			return s;
		}

		/// <summary>
		/// Localizes a string.
		/// </summary>
		/// <param name="constant">string constant to localize</param>
		/// <returns>localized string</returns>
		public static string Localize(string constant)
		{
			if (string.IsNullOrWhiteSpace(constant))
			{
				return string.Empty;
			}

			return string.Format(constant, new object[0]);
		}
	}
}

