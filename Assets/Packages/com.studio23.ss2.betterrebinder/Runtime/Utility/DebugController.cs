using UnityEngine;

#if PRINT_DEBUG_LOGS
	public static class DebugController
	{
		public static void DebugNormal(string message)
		{
			Debug.Log($"{message}");
		}

		public static void DebugWarning(string message)
		{
			Debug.LogWarning($"{message}");
		}
	}
#endif
