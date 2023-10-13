using UnityEngine;

#if PRINT_DEBUG_LOGS
	public static class DebugController
	{
		public static void DebugNormal(string message)
		{
			Debug.Log($"{message}");
		}

		public static void DebugColored(string message, string hex)
		{
			Debug.Log($"<color={hex}>{message}</color>");
		}

		public static void DebugWarning(string message)
		{
			Debug.LogWarning($"{message}");
		}
	}
#endif
