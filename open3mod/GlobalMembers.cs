//========================================================================
// This conversion was produced by the Free Edition of
// C++ to C# Converter courtesy of Tangible Software Solutions.
// Order the Premium Edition at https://www.tangiblesoftwaresolutions.com
//========================================================================

public static class GlobalMembers
{
	public static void ThreadSleep(uint nMilliseconds)
	{
	#if _WIN32
		global::Sleep(nMilliseconds);
	#elif POSIX
		usleep(nMilliseconds * 1000);
	#endif
	}

	internal static bool g_bPrintf = true;

	//-----------------------------------------------------------------------------
	// Purpose: Outputs a set of optional arguments to debugging output, using
	//          the printf format setting specified in fmt*.
	//-----------------------------------------------------------------------------
	public static void dprintf(string fmt, params object[] LegacyParamArray)
	{
	//	va_list args;
		string buffer = new string(new char[2048]);

	int ParamCount = -1;
	//	va_start(args, fmt);
		vsprintf_s(buffer, fmt, args);
	//	va_end(args);

		if (g_bPrintf)
		{
			Console.Write("{0}", buffer);
		}

		OutputDebugStringA(buffer);
	}


	//-----------------------------------------------------------------------------
	// Purpose: Helper to get a string from a tracked device property and turn it
	//			into a std::string
	//-----------------------------------------------------------------------------
	public static string GetTrackedDeviceString(vr.IVRSystem pHmd, vr.TrackedDeviceIndex_t unDevice, vr.TrackedDeviceProperty prop, vr.TrackedPropertyError peError = null)
	{
		uint32_t unRequiredBufferLen = pHmd.GetStringTrackedDeviceProperty(unDevice, prop, null, 0, peError);
		if (unRequiredBufferLen == 0)
		{
			return "";
		}

		string pchBuffer = new string(new char[unRequiredBufferLen - 1]);
		unRequiredBufferLen = pHmd.GetStringTrackedDeviceProperty(unDevice, prop, pchBuffer, unRequiredBufferLen, peError);
		string sResult = ((char)pchBuffer).ToString();
		pchBuffer = null;
		return sResult;
	}


	//-----------------------------------------------------------------------------
	// Purpose: Outputs the string in message to debugging output.
	//          All other parameters are ignored.
	//          Does not return any meaningful value or reference.
	//-----------------------------------------------------------------------------
	//C++ TO C# CONVERTER TODO TASK: The #define macro 'APIENTRY' was defined in multiple preprocessor conditionals and cannot be replaced in-line:
//C++ TO C# CONVERTER NOTE: APIENTRY is not available in C#:
//ORIGINAL LINE: void APIENTRY DebugCallback(GLenum source, GLenum type, GLuint id, GLenum severity, GLsizei length, const sbyte* message, const object* userParam)
	public static void DebugCallback(GLenum source, GLenum type, GLuint id, GLenum severity, GLsizei length, string message, object userParam)
	{
		dprintf("GL Error: %s\n", message);
	}


	//-----------------------------------------------------------------------------
	// Purpose: Processes a single VR event
	//-----------------------------------------------------------------------------
	private void CMainApplication.ProcessVREvent(const vr.VREvent_t & event)
	{
		switch (event.eventType)
		{
		case vr.VREvent_TrackedDeviceActivated:
		{
				SetupRenderModelForTrackedDevice(event.trackedDeviceIndex);
				dprintf("Device %u attached. Setting up render model.\n", event.trackedDeviceIndex);
		}
			break;
		case vr.VREvent_TrackedDeviceDeactivated:
		{
				dprintf("Device %u detached.\n", event.trackedDeviceIndex);
		}
			break;
		case vr.VREvent_TrackedDeviceUpdated:
		{
				dprintf("Device %u updated.\n", event.trackedDeviceIndex);
		}
			break;
		}
	}


	//-----------------------------------------------------------------------------
	// Purpose:
	//-----------------------------------------------------------------------------
	static int Main(int argc, string[] args)
	{
		CMainApplication pMainApplication = new CMainApplication(argc, args);

		if (!pMainApplication.BInit())
		{
			pMainApplication.Shutdown();
			return 1;
		}

		pMainApplication.RunMainLoop();

		pMainApplication.Shutdown();

	}

}