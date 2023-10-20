namespace Studio23.SS2.BetterRebinder.Core
{
	public static class DeviceIndexResolver
	{
		public static int DeviceIndex { get; private set; }

		public static int ResolveDeviceIndex(string deviceName)
		{
			deviceName = deviceName.ToLower();
			if (deviceName.Contains("keyboard") || deviceName.Contains("mouse"))
				DeviceIndex = 0;
			else if (deviceName.Contains("dual") || deviceName.Contains("playstation"))
				DeviceIndex = 1;
			else if (deviceName.Contains("xinput") || deviceName.Contains("xbox") || deviceName.Contains("gamepad"))
				DeviceIndex = 2;
			else
				DeviceIndex = 0;
			return DeviceIndex;
		}
	}
}