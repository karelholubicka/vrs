//========================================================================
// This conversion was produced by the Free Edition of
// C++ to C# Converter courtesy of Tangible Software Solutions.
// Order the Premium Edition at https://www.tangiblesoftwaresolutions.com
//========================================================================

using System;
using System.Collections.Generic;

//========= Copyright Valve Corporation ============//

#if OSX
// Apple's version of glut.h #undef's APIENTRY, redefine it
#define APIENTRY
#else
#endif


//C++ TO C# CONVERTER NOTE: The following #define macro was replaced in-line:
//ORIGINAL LINE: #define MAX_UNICODE_PATH_IN_UTF8 (MAX_UNICODE_PATH * 4)

#if POSIX
//C++ TO C# CONVERTER WARNING: The following #include directive was ignored:
//#include "unistd.h"
#endif

#if ! _WIN32
#define APIENTRY
#endif

#if ! _countof
//C++ TO C# CONVERTER TODO TASK: #define macros defined in multiple preprocessor conditionals can only be replaced within the scope of the preprocessor conditional:
//C++ TO C# CONVERTER NOTE: The following #define macro was replaced in-line:
//ORIGINAL LINE: #define _countof(x) (sizeof(x)/sizeof((x)[0]))
#define _countof
#endif

public class CGLRenderModel : System.IDisposable
{

	//-----------------------------------------------------------------------------
	// Purpose: Create/destroy GL Render Models
	//-----------------------------------------------------------------------------
	public CGLRenderModel(string sRenderModelName)
	{
		this.m_sModelName = sRenderModelName;
		m_glIndexBuffer = 0;
		m_glVertArray = 0;
		m_glVertBuffer = 0;
		m_glTexture = 0;
	}
	public void Dispose()
	{
		Cleanup();
	}


	//-----------------------------------------------------------------------------
	// Purpose: Allocates and populates the GL resources for a render model
	//-----------------------------------------------------------------------------
	public bool BInit(vr.RenderModel_t vrModel, vr.RenderModel_TextureMap_t vrDiffuseTexture)
	{
		// create and bind a VAO to hold state for this model
		glGenVertexArrays(1, m_glVertArray);
		glBindVertexArray(m_glVertArray);

		// Populate a vertex buffer
		glGenBuffers(1, m_glVertBuffer);
		glBindBuffer(GL_ARRAY_BUFFER, m_glVertBuffer);
		glBufferData(GL_ARRAY_BUFFER, sizeof(vr.RenderModel_Vertex_t) * vrModel.unVertexCount, vrModel.rVertexData, GL_STATIC_DRAW);

		// Identify the components in the vertex buffer
		glEnableVertexAttribArray(0);
		glVertexAttribPointer(0, 3, GL_FLOAT, GL_FALSE, sizeof(vr.RenderModel_Vertex_t), (object)offsetof(vr.RenderModel_Vertex_t, vPosition));
		glEnableVertexAttribArray(1);
		glVertexAttribPointer(1, 3, GL_FLOAT, GL_FALSE, sizeof(vr.RenderModel_Vertex_t), (object)offsetof(vr.RenderModel_Vertex_t, vNormal));
		glEnableVertexAttribArray(2);
		glVertexAttribPointer(2, 2, GL_FLOAT, GL_FALSE, sizeof(vr.RenderModel_Vertex_t), (object)offsetof(vr.RenderModel_Vertex_t, rfTextureCoord));

		// Create and populate the index buffer
		glGenBuffers(1, m_glIndexBuffer);
		glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, m_glIndexBuffer);
		glBufferData(GL_ELEMENT_ARRAY_BUFFER, sizeof(uint16_t) * vrModel.unTriangleCount * 3, vrModel.rIndexData, GL_STATIC_DRAW);

		glBindVertexArray(0);

		// create and populate the texture
		glGenTextures(1, m_glTexture);
		glBindTexture(GL_TEXTURE_2D, m_glTexture);

		glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, vrDiffuseTexture.unWidth, vrDiffuseTexture.unHeight, 0, GL_RGBA, GL_UNSIGNED_BYTE, vrDiffuseTexture.rubTextureMapData);

		// If this renders black ask McJohn what's wrong.
		glGenerateMipmap(GL_TEXTURE_2D);

		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR_MIPMAP_LINEAR);

		GLfloat fLargest = new GLfloat();
		glGetFloatv(GL_MAX_TEXTURE_MAX_ANISOTROPY_EXT, fLargest);
		glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_MAX_ANISOTROPY_EXT, fLargest);

		glBindTexture(GL_TEXTURE_2D, 0);

		m_unVertexCount = vrModel.unTriangleCount * 3;

		return true;
	}

	//-----------------------------------------------------------------------------
	// Purpose: Frees the GL resources for a render model
	//-----------------------------------------------------------------------------
	public void Cleanup()
	{
		if (m_glVertBuffer != null)
		{
			glDeleteBuffers(1, m_glIndexBuffer);
			glDeleteVertexArrays(1, m_glVertArray);
			glDeleteBuffers(1, m_glVertBuffer);
			m_glIndexBuffer = 0;
			m_glVertArray = 0;
			m_glVertBuffer = 0;
		}
	}

	//-----------------------------------------------------------------------------
	// Purpose: Draws the render model
	//-----------------------------------------------------------------------------
	public void Draw()
	{
		glBindVertexArray(m_glVertArray);

		glActiveTexture(GL_TEXTURE0);
		glBindTexture(GL_TEXTURE_2D, m_glTexture);

		glDrawElements(GL_TRIANGLES, m_unVertexCount, GL_UNSIGNED_SHORT, 0);

		glBindVertexArray(0);
	}
//C++ TO C# CONVERTER WARNING: 'const' methods are not available in C#:
//ORIGINAL LINE: const string & GetName() const
	public string GetName()
	{
		return m_sModelName;
	}

	private GLuint m_glVertBuffer = new GLuint();
	private GLuint m_glIndexBuffer = new GLuint();
	private GLuint m_glVertArray = new GLuint();
	private GLuint m_glTexture = new GLuint();
	private GLsizei m_unVertexCount = new GLsizei();
	private string m_sModelName;
}

//-----------------------------------------------------------------------------
// Purpose:
//------------------------------------------------------------------------------
public class CMainApplication : System.IDisposable
{

	//-----------------------------------------------------------------------------
	// Purpose: Constructor
	//-----------------------------------------------------------------------------
	public CMainApplication(int argc, string[] argv)
	{
		this.m_pCompanionWindow = null;
		this.m_pContext = null;
		this.m_nCompanionWindowWidth = 640;
		this.m_nCompanionWindowHeight = 320;
		this.m_unSceneProgramID = 0;
		this.m_unCompanionWindowBProgramID = 0;
		this.m_unControllerTransformProgramID = 0;
		this.m_unRenderModelProgramID = 0;
		this.m_pHMD = null;
		this.m_pRenderModels = null;
		this.m_bDebugOpenGL = false;
		this.m_bVerbose = false;
		this.m_bPerf = false;
		this.m_bVblank = false;
		this.m_bGlFinishHack = true;
		this.m_glControllerVertBuffer = 0;
		this.m_unControllerVAO = 0;
		this.m_unSceneVAO = 0;
		this.m_nSceneMatrixLocation = -1;
		this.m_nControllerMatrixLocation = -1;
		this.m_nRenderModelMatrixLocation = -1;
		this.m_iTrackedControllerCount = 0;
		this.m_iTrackedControllerCount_Last = -1;
		this.m_iValidPoseCount = 0;
		this.m_iValidPoseCount_Last = -1;
		this.m_iSceneVolumeInit = 20;
		this.m_strPoseClasses = "";
		this.m_bShowCubes = true;

		for (int i = 1; i < argc; i++)
		{
			if (!string.Compare(argv[i], "-gldebug", true))
			{
				m_bDebugOpenGL = true;
			}
			else if (!string.Compare(argv[i], "-verbose", true))
			{
				m_bVerbose = true;
			}
			else if (!string.Compare(argv[i], "-novblank", true))
			{
				m_bVblank = false;
			}
			else if (!string.Compare(argv[i], "-noglfinishhack", true))
			{
				m_bGlFinishHack = false;
			}
			else if (!string.Compare(argv[i], "-noprintf", true))
			{
				GlobalMembers.g_bPrintf = false;
			}
			else if (!string.Compare(argv[i], "-cubevolume", true) && (argc > i + 1) && (argv[i + 1] != '-'))
			{
				m_iSceneVolumeInit = Convert.ToInt32(argv[i + 1]);
				i++;
			}
		}
		// other initialization tasks are done in BInit
//C++ TO C# CONVERTER TODO TASK: The memory management function 'memset' has no equivalent in C#:
		memset(m_rDevClassChar, 0, sizeof(sbyte));
	}

	//-----------------------------------------------------------------------------
	// Purpose: Destructor
	//-----------------------------------------------------------------------------
	public virtual void Dispose()
	{
		// work is done in Shutdown
		dprintf("Shutdown");
	}


	//-----------------------------------------------------------------------------
	// Purpose:
	//-----------------------------------------------------------------------------
	public bool BInit()
	{
		if (SDL_Init(SDL_INIT_VIDEO | SDL_INIT_TIMER) < 0)
		{
			Console.Write("{0} - SDL could not initialize! SDL Error: {1}\n", System.Reflection.MethodBase.GetCurrentMethod().Name, SDL_GetError());
			return false;
		}

		// Loading the SteamVR Runtime
		vr.EVRInitError eError = vr.VRInitError_None;
		m_pHMD = vr.VR_Init(eError, vr.VRApplication_Scene);

		if (eError != vr.VRInitError_None)
		{
			m_pHMD = null;
			string buf = new string(new char[1024]);
			sprintf_s(buf, sizeof(sbyte), "Unable to init VR runtime: %s", vr.VR_GetVRInitErrorAsEnglishDescription(eError));
			SDL_ShowSimpleMessageBox(SDL_MESSAGEBOX_ERROR, "VR_Init Failed", buf, null);
			return false;
		}


		m_pRenderModels = (vr.IVRRenderModels)vr.VR_GetGenericInterface(vr.IVRRenderModels_Version, eError);
		if (m_pRenderModels == null)
		{
			m_pHMD = null;
			vr.VR_Shutdown();

			string buf = new string(new char[1024]);
			sprintf_s(buf, sizeof(sbyte), "Unable to get render model interface: %s", vr.VR_GetVRInitErrorAsEnglishDescription(eError));
			SDL_ShowSimpleMessageBox(SDL_MESSAGEBOX_ERROR, "VR_Init Failed", buf, null);
			return false;
		}

		int nWindowPosX = 100;
		int nWindowPosY = 100;
		uint unWindowFlags = SDL_WINDOW_OPENGL | SDL_WINDOW_SHOWN;

		SDL_GL_SetAttribute(SDL_GL_CONTEXT_MAJOR_VERSION, 4);
		SDL_GL_SetAttribute(SDL_GL_CONTEXT_MINOR_VERSION, 1);
		//SDL_GL_SetAttribute( SDL_GL_CONTEXT_PROFILE_MASK, SDL_GL_CONTEXT_PROFILE_COMPATIBILITY );
		SDL_GL_SetAttribute(SDL_GL_CONTEXT_PROFILE_MASK, SDL_GL_CONTEXT_PROFILE_CORE);

		SDL_GL_SetAttribute(SDL_GL_MULTISAMPLEBUFFERS, 0);
		SDL_GL_SetAttribute(SDL_GL_MULTISAMPLESAMPLES, 0);
		if (m_bDebugOpenGL)
		{
			SDL_GL_SetAttribute(SDL_GL_CONTEXT_FLAGS, SDL_GL_CONTEXT_DEBUG_FLAG);
		}

		m_pCompanionWindow = SDL_CreateWindow("hellovr", nWindowPosX, nWindowPosY, m_nCompanionWindowWidth, m_nCompanionWindowHeight, unWindowFlags);
		if (m_pCompanionWindow == null)
		{
			Console.Write("{0} - Window could not be created! SDL Error: {1}\n", System.Reflection.MethodBase.GetCurrentMethod().Name, SDL_GetError());
			return false;
		}

		m_pCompanionWindowB = SDL_CreateWindow("hellovrB", nWindowPosX, nWindowPosY + 300, m_nCompanionWindowWidth, m_nCompanionWindowHeight, unWindowFlags);
		if (m_pCompanionWindowB == null)
		{
			Console.Write("{0} - Window could not be created! SDL Error: {1}\n", System.Reflection.MethodBase.GetCurrentMethod().Name, SDL_GetError());
			return false;
		}

		m_pContext = SDL_GL_CreateContext(m_pCompanionWindow);
		if (m_pContext == null)
		{
			Console.Write("{0} - OpenGL context could not be created! SDL Error: {1}\n", System.Reflection.MethodBase.GetCurrentMethod().Name, SDL_GetError());
			return false;
		}
		m_pContextB = SDL_GL_CreateContext(m_pCompanionWindowB);
		if (m_pContext == null)
		{
			Console.Write("{0} - OpenGL context could not be created! SDL Error: {1}\n", System.Reflection.MethodBase.GetCurrentMethod().Name, SDL_GetError());
			return false;
		}

		glewExperimental = GL_TRUE;
		GLenum nGlewError = glewInit();
		if (nGlewError != GLEW_OK)
		{
			Console.Write("{0} - Error initializing GLEW! {1}\n", System.Reflection.MethodBase.GetCurrentMethod().Name, glewGetErrorString(nGlewError));
			return false;
		}
		glGetError(); // to clear the error caused deep in GLEW

		if (SDL_GL_SetSwapInterval(m_bVblank ? 1 : 0) < 0)
		{
			Console.Write("{0} - Warning: Unable to set VSync! SDL Error: {1}\n", System.Reflection.MethodBase.GetCurrentMethod().Name, SDL_GetError());
			return false;
		}


		m_strDriver = "No Driver";
		m_strDisplay = "No Display";

		m_strDriver = GlobalMembers.GetTrackedDeviceString(m_pHMD, vr.k_unTrackedDeviceIndex_Hmd, vr.Prop_TrackingSystemName_String);
		m_strDisplay = GlobalMembers.GetTrackedDeviceString(m_pHMD, vr.k_unTrackedDeviceIndex_Hmd, vr.Prop_SerialNumber_String);

		string strWindowTitle = "hellovr - " + m_strDriver + " " + m_strDisplay;
		SDL_SetWindowTitle(m_pCompanionWindow, strWindowTitle);
		strWindowTitle = "hellovrBB - " + m_strDriver + " " + m_strDisplay;
		SDL_SetWindowTitle(m_pCompanionWindowB, strWindowTitle);

		// cube array
		 m_iSceneVolumeWidth = m_iSceneVolumeInit;
		 m_iSceneVolumeHeight = m_iSceneVolumeInit;
		 m_iSceneVolumeDepth = m_iSceneVolumeInit;

		 m_fScale = 0.3f;
		 m_fScaleSpacing = 4.0f;

		 m_fNearClip = 0.1f;
		 m_fFarClip = 30.0f;

		 m_iTexture = 0;
		 m_uiVertcount = 0;

	// 		m_MillisecondsTimer.start(1, this);
	// 		m_SecondsTimer.start(1000, this);

		if (!BInitGL())
		{
			Console.Write("{0} - Unable to initialize OpenGL!\n", System.Reflection.MethodBase.GetCurrentMethod().Name);
			return false;
		}

		if (!BInitCompositor())
		{
			Console.Write("{0} - Failed to initialize VR Compositor!\n", System.Reflection.MethodBase.GetCurrentMethod().Name);
			return false;
		}

		return true;
	}

	//-----------------------------------------------------------------------------
	// Purpose: Initialize OpenGL. Returns true if OpenGL has been successfully
	//          initialized, false if shaders could not be created.
	//          If failure occurred in a module other than shaders, the function
	//          may return true or throw an error. 
	//-----------------------------------------------------------------------------
	public bool BInitGL()
	{
		if (m_bDebugOpenGL)
		{
			glDebugMessageCallback((GLDEBUGPROC)GlobalMembers.DebugCallback, null);
			glDebugMessageControl(GL_DONT_CARE, GL_DONT_CARE, GL_DONT_CARE, 0, null, GL_TRUE);
			glEnable(GL_DEBUG_OUTPUT_SYNCHRONOUS);
		}

		if (!CreateAllShaders())
		{
			return false;
		}

		SetupTexturemaps();
		SetupScene();
		SetupCameras();
		SetupStereoRenderTargets();
		SetupCompanionWindow();
		SetupRenderModels();

		return true;
	}

	//-----------------------------------------------------------------------------
	// Purpose: Initialize Compositor. Returns true if the compositor was
	//          successfully initialized, false otherwise.
	//-----------------------------------------------------------------------------
	public bool BInitCompositor()
	{
		vr.EVRInitError peError = vr.VRInitError_None;

		if (!vr.VRCompositor())
		{
			Console.Write("Compositor initialization failed. See log file for details\n");
			return false;
		}

		return true;
	}


	//-----------------------------------------------------------------------------
	// Purpose: Create/destroy GL Render Models
	//-----------------------------------------------------------------------------
	public void SetupRenderModels()
	{
//C++ TO C# CONVERTER TODO TASK: The memory management function 'memset' has no equivalent in C#:
		memset(m_rTrackedDeviceToRenderModel, 0, sizeof(CGLRenderModel));

		if (m_pHMD == null)
		{
			return;
		}

		for (uint32_t unTrackedDevice = vr.k_unTrackedDeviceIndex_Hmd + 1; unTrackedDevice < vr.k_unMaxTrackedDeviceCount; unTrackedDevice++)
		{
			if (!m_pHMD.IsTrackedDeviceConnected(unTrackedDevice))
			{
				continue;
			}

//C++ TO C# CONVERTER TODO TASK: The following line was determined to contain a copy constructor call - this should be verified and a copy constructor should be created:
//ORIGINAL LINE: SetupRenderModelForTrackedDevice(unTrackedDevice);
			SetupRenderModelForTrackedDevice(new uint32_t(unTrackedDevice));
		}

	}


	//-----------------------------------------------------------------------------
	// Purpose:
	//-----------------------------------------------------------------------------
	public void Shutdown()
	{
		if (m_pHMD != null)
		{
			vr.VR_Shutdown();
			m_pHMD = null;
		}

		foreach (CGLRenderModel * i in m_vecRenderModels)
		{
			deletei;
		}
		m_vecRenderModels.Clear();

		if (m_pContext != null)
		{
			if (m_bDebugOpenGL)
			{
				glDebugMessageControl(GL_DONT_CARE, GL_DONT_CARE, GL_DONT_CARE, 0, null, GL_FALSE);
				glDebugMessageCallback(null, null);
			}
			glDeleteBuffers(1, m_glSceneVertBuffer);

			if (m_unSceneProgramID != null)
			{
				glDeleteProgram(m_unSceneProgramID);
			}
			if (m_unControllerTransformProgramID != null)
			{
				glDeleteProgram(m_unControllerTransformProgramID);
			}
			if (m_unRenderModelProgramID != null)
			{
				glDeleteProgram(m_unRenderModelProgramID);
			}
			if (m_unCompanionWindowBProgramID != null)
			{
				glDeleteProgram(m_unCompanionWindowBProgramID);
			}

			glDeleteRenderbuffers(1, leftEyeDesc.m_nDepthBufferId);
			glDeleteTextures(1, leftEyeDesc.m_nRenderTextureId);
			glDeleteFramebuffers(1, leftEyeDesc.m_nRenderFramebufferId);
			glDeleteTextures(1, leftEyeDesc.m_nResolveTextureId);
			glDeleteFramebuffers(1, leftEyeDesc.m_nResolveFramebufferId);

			glDeleteRenderbuffers(1, rightEyeDesc.m_nDepthBufferId);
			glDeleteTextures(1, rightEyeDesc.m_nRenderTextureId);
			glDeleteFramebuffers(1, rightEyeDesc.m_nRenderFramebufferId);
			glDeleteTextures(1, rightEyeDesc.m_nResolveTextureId);
			glDeleteFramebuffers(1, rightEyeDesc.m_nResolveFramebufferId);

			if (m_unCompanionWindowVAO != 0)
			{
				glDeleteVertexArrays(1, m_unCompanionWindowVAO);
			}
			if (m_unCompanionWindowBVAO != 0)
			{
				glDeleteVertexArrays(1, m_unCompanionWindowBVAO);
			}
			if (m_unSceneVAO != 0)
			{
				glDeleteVertexArrays(1, m_unSceneVAO);
			}
			if (m_unControllerVAO != 0)
			{
				glDeleteVertexArrays(1, m_unControllerVAO);
			}
		}

		if (m_pCompanionWindow != null)
		{
			SDL_DestroyWindow(m_pCompanionWindow);
			m_pCompanionWindow = null;
		}

		SDL_Quit();
		if (m_pCompanionWindowB != null)
		{
			SDL_DestroyWindow(m_pCompanionWindowB);
			m_pCompanionWindowB = null;
		}

		SDL_Quit();
	}


	//-----------------------------------------------------------------------------
	// Purpose:
	//-----------------------------------------------------------------------------
	public void RunMainLoop()
	{
		bool bQuit = false;

		SDL_StartTextInput();
		SDL_ShowCursor(SDL_DISABLE);

		while (!bQuit)
		{
			bQuit = HandleInput();

			RenderFrame();
		}

		SDL_StopTextInput();
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	//-----------------------------------------------------------------------------
	public bool HandleInput()
	{
		SDL_Event sdlEvent = new SDL_Event();
		bool bRet = false;

		while (SDL_PollEvent(sdlEvent) != 0)
		{
			if (sdlEvent.type == SDL_QUIT)
			{
				bRet = true;
			}
			else if (sdlEvent.type == SDL_KEYDOWN)
			{
				if (sdlEvent.key.keysym.sym == SDLK_ESCAPE || sdlEvent.key.keysym.sym == SDLK_q)
				{
					bRet = true;
				}
				if (sdlEvent.key.keysym.sym == SDLK_c)
				{
					m_bShowCubes = !m_bShowCubes;
				}
			}
		}

		// Process SteamVR events
		vr.VREvent_t event = new vr.VREvent_t();
		while (m_pHMD.PollNextEvent(event, sizeof(vr.VREvent_t)))
		{
			ProcessVREvent(event);
		}

		// Process SteamVR controller state
		for (vr.TrackedDeviceIndex_t unDevice = 0; unDevice < vr.k_unMaxTrackedDeviceCount; unDevice++)
		{
			vr.VRControllerState_t state = new vr.VRControllerState_t();
			if (m_pHMD.GetControllerState(unDevice, state, sizeof(vr.VRControllerState_t)))
			{
				m_rbShowTrackedDevice[unDevice] = state.ulButtonPressed == 0;
			}
		}

		return bRet;
	}
	public void ProcessVREvent(const vr.VREvent_t & event);

	//-----------------------------------------------------------------------------
	// Purpose:
	//-----------------------------------------------------------------------------
	public void RenderFrame()
	{
		// for now as fast as possible
		if (m_pHMD != null)
		{
			RenderControllerAxes();
			RenderStereoTargets();
			RenderCompanionWindow();
			RenderCompanionWindowB();

			vr.Texture_t leftEyeTexture = new vr.Texture_t((object)(uintptr_t)leftEyeDesc.m_nResolveTextureId, vr.TextureType_OpenGL, vr.ColorSpace_Gamma);
			vr.VRCompositor().Submit(vr.Eye_Left, leftEyeTexture);
			vr.Texture_t rightEyeTexture = new vr.Texture_t((object)(uintptr_t)rightEyeDesc.m_nResolveTextureId, vr.TextureType_OpenGL, vr.ColorSpace_Gamma);
			vr.VRCompositor().Submit(vr.Eye_Right, rightEyeTexture);
		}

		if (m_bVblank && m_bGlFinishHack)
		{
			//$ HACKHACK. From gpuview profiling, it looks like there is a bug where two renders and a present
			// happen right before and after the vsync causing all kinds of jittering issues. This glFinish()
			// appears to clear that up. Temporary fix while I try to get nvidia to investigate this problem.
			// 1/29/2014 mikesart
			glFinish();
		}

		{
		// SwapWindow
			SDL_GL_SwapWindow(m_pCompanionWindow);
		}

		{
		// Clear
			// We want to make sure the glFinish waits for the entire present to complete, not just the submission
			// of the command. So, we do a clear here right here so the glFinish will wait fully for the swap.
			glClearColor(0, 0, 0, 1);
			glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
		}

		// Flush and wait for swap.
		if (m_bVblank)
		{
			glFlush();
			glFinish();
		}

		// Spew out the controller and pose count whenever they change.
		if (m_iTrackedControllerCount != m_iTrackedControllerCount_Last || m_iValidPoseCount != m_iValidPoseCount_Last)
		{
			m_iValidPoseCount_Last = m_iValidPoseCount;
			m_iTrackedControllerCount_Last = m_iTrackedControllerCount;

			dprintf("PoseCount:%d(%s) Controllers:%d\n", m_iValidPoseCount, m_strPoseClasses, m_iTrackedControllerCount);
		}

		UpdateHMDMatrixPose();
	}


	//-----------------------------------------------------------------------------
	// Purpose:
	//-----------------------------------------------------------------------------
	public bool SetupTexturemaps()
	{
		string sExecutableDirectory = Path_StripFilename(Path_GetExecutablePath());
		string strFullPath = Path_MakeAbsolute("../cube_texture.png", sExecutableDirectory);

		List<byte> imageRGBA = new List<byte>();
		uint nImageWidth;
		uint nImageHeight;
		uint nError = lodepng.decode(imageRGBA, nImageWidth, nImageHeight, strFullPath);

		if (nError != 0)
		{
			return false;
		}

		glGenTextures(1, m_iTexture);
		glBindTexture(GL_TEXTURE_2D, m_iTexture);

		glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, nImageWidth, nImageHeight, 0, GL_RGBA, GL_UNSIGNED_BYTE, imageRGBA[0]);

		glGenerateMipmap(GL_TEXTURE_2D);

		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR_MIPMAP_LINEAR);

		GLfloat fLargest = new GLfloat();
		glGetFloatv(GL_MAX_TEXTURE_MAX_ANISOTROPY_EXT, fLargest);
		glTexParameterf(GL_TEXTURE_2D, GL_TEXTURE_MAX_ANISOTROPY_EXT, fLargest);

		glBindTexture(GL_TEXTURE_2D, 0);

		return (m_iTexture != 0);
	}


	//-----------------------------------------------------------------------------
	// Purpose: create a sea of cubes
	//-----------------------------------------------------------------------------
	public void SetupScene()
	{
		if (m_pHMD == null)
		{
			return;
		}

		List<float> vertdataarray = new List<float>();

		Matrix4 matScale = new Matrix4();
		matScale.scale(m_fScale, m_fScale, m_fScale);
		Matrix4 matTransform = new Matrix4();
		matTransform.translate(-((float)m_iSceneVolumeWidth * m_fScaleSpacing) / 2.0f, -((float)m_iSceneVolumeHeight * m_fScaleSpacing) / 2.0f, -((float)m_iSceneVolumeDepth * m_fScaleSpacing) / 2.0f);

		Matrix4 mat = matScale * matTransform;

		for (int z = 0; z < m_iSceneVolumeDepth; z++)
		{
			for (int y = 0; y < m_iSceneVolumeHeight; y++)
			{
				for (int x = 0; x < m_iSceneVolumeWidth; x++)
				{
//C++ TO C# CONVERTER TODO TASK: The following line was determined to contain a copy constructor call - this should be verified and a copy constructor should be created:
//ORIGINAL LINE: AddCubeToScene(mat, vertdataarray);
					AddCubeToScene(new Matrix4(mat), vertdataarray);
//C++ TO C# CONVERTER TODO TASK: The following line was determined to be a copy assignment (rather than a reference assignment) - this should be verified and a 'CopyFrom' method should be created:
//ORIGINAL LINE: mat = mat * Matrix4().translate(m_fScaleSpacing, 0, 0);
					mat.CopyFrom(mat * new Matrix4().translate(m_fScaleSpacing, 0, 0));
				}
//C++ TO C# CONVERTER TODO TASK: The following line was determined to be a copy assignment (rather than a reference assignment) - this should be verified and a 'CopyFrom' method should be created:
//ORIGINAL LINE: mat = mat * Matrix4().translate(-((float)m_iSceneVolumeWidth) * m_fScaleSpacing, m_fScaleSpacing, 0);
				mat.CopyFrom(mat * new Matrix4().translate(-((float)m_iSceneVolumeWidth) * m_fScaleSpacing, m_fScaleSpacing, 0));
			}
//C++ TO C# CONVERTER TODO TASK: The following line was determined to be a copy assignment (rather than a reference assignment) - this should be verified and a 'CopyFrom' method should be created:
//ORIGINAL LINE: mat = mat * Matrix4().translate(0, -((float)m_iSceneVolumeHeight) * m_fScaleSpacing, m_fScaleSpacing);
			mat.CopyFrom(mat * new Matrix4().translate(0, -((float)m_iSceneVolumeHeight) * m_fScaleSpacing, m_fScaleSpacing));
		}
		m_uiVertcount = vertdataarray.Count / 5;

		glGenVertexArrays(1, m_unSceneVAO);
		glBindVertexArray(m_unSceneVAO);

		glGenBuffers(1, m_glSceneVertBuffer);
		glBindBuffer(GL_ARRAY_BUFFER, m_glSceneVertBuffer);
		glBufferData(GL_ARRAY_BUFFER, sizeof(float) * vertdataarray.Count, vertdataarray[0], GL_STATIC_DRAW);

		GLsizei stride = sizeof(VertexDataScene);
		uintptr_t offset = 0;

		glEnableVertexAttribArray(0);
		glVertexAttribPointer(0, 3, GL_FLOAT, GL_FALSE, stride, (object)offset);

		offset += sizeof(Vector3);
		glEnableVertexAttribArray(1);
		glVertexAttribPointer(1, 2, GL_FLOAT, GL_FALSE, stride, (object)offset);

		glBindVertexArray(0);
		glDisableVertexAttribArray(0);
		glDisableVertexAttribArray(1);

	}

	//-----------------------------------------------------------------------------
	// Purpose:
	//-----------------------------------------------------------------------------
	public void AddCubeToScene(Matrix4 mat, List<float> vertdata)
	{
		// Matrix4 mat( outermat.data() );

		Vector4 A = mat * new Vector4(0F, 0F, 0F, 1F);
		Vector4 B = mat * new Vector4(1F, 0F, 0F, 1F);
		Vector4 C = mat * new Vector4(1F, 1F, 0F, 1F);
		Vector4 D = mat * new Vector4(0F, 1F, 0F, 1F);
		Vector4 E = mat * new Vector4(0F, 0F, 1F, 1F);
		Vector4 F = mat * new Vector4(1F, 0F, 1F, 1F);
		Vector4 G = mat * new Vector4(1F, 1F, 1F, 1F);
		Vector4 H = mat * new Vector4(0F, 1F, 1F, 1F);

		// triangles instead of quads
		AddCubeVertex(E.x, E.y, E.z, 0F, 1F, vertdata); //Front
		AddCubeVertex(F.x, F.y, F.z, 1F, 1F, vertdata);
		AddCubeVertex(G.x, G.y, G.z, 1F, 0F, vertdata);
		AddCubeVertex(G.x, G.y, G.z, 1F, 0F, vertdata);
		AddCubeVertex(H.x, H.y, H.z, 0F, 0F, vertdata);
		AddCubeVertex(E.x, E.y, E.z, 0F, 1F, vertdata);

		AddCubeVertex(B.x, B.y, B.z, 0F, 1F, vertdata); //Back
		AddCubeVertex(A.x, A.y, A.z, 1F, 1F, vertdata);
		AddCubeVertex(D.x, D.y, D.z, 1F, 0F, vertdata);
		AddCubeVertex(D.x, D.y, D.z, 1F, 0F, vertdata);
		AddCubeVertex(C.x, C.y, C.z, 0F, 0F, vertdata);
		AddCubeVertex(B.x, B.y, B.z, 0F, 1F, vertdata);

		AddCubeVertex(H.x, H.y, H.z, 0F, 1F, vertdata); //Top
		AddCubeVertex(G.x, G.y, G.z, 1F, 1F, vertdata);
		AddCubeVertex(C.x, C.y, C.z, 1F, 0F, vertdata);
		AddCubeVertex(C.x, C.y, C.z, 1F, 0F, vertdata);
		AddCubeVertex(D.x, D.y, D.z, 0F, 0F, vertdata);
		AddCubeVertex(H.x, H.y, H.z, 0F, 1F, vertdata);

		AddCubeVertex(A.x, A.y, A.z, 0F, 1F, vertdata); //Bottom
		AddCubeVertex(B.x, B.y, B.z, 1F, 1F, vertdata);
		AddCubeVertex(F.x, F.y, F.z, 1F, 0F, vertdata);
		AddCubeVertex(F.x, F.y, F.z, 1F, 0F, vertdata);
		AddCubeVertex(E.x, E.y, E.z, 0F, 0F, vertdata);
		AddCubeVertex(A.x, A.y, A.z, 0F, 1F, vertdata);

		AddCubeVertex(A.x, A.y, A.z, 0F, 1F, vertdata); //Left
		AddCubeVertex(E.x, E.y, E.z, 1F, 1F, vertdata);
		AddCubeVertex(H.x, H.y, H.z, 1F, 0F, vertdata);
		AddCubeVertex(H.x, H.y, H.z, 1F, 0F, vertdata);
		AddCubeVertex(D.x, D.y, D.z, 0F, 0F, vertdata);
		AddCubeVertex(A.x, A.y, A.z, 0F, 1F, vertdata);

		AddCubeVertex(F.x, F.y, F.z, 0F, 1F, vertdata); //Right
		AddCubeVertex(B.x, B.y, B.z, 1F, 1F, vertdata);
		AddCubeVertex(C.x, C.y, C.z, 1F, 0F, vertdata);
		AddCubeVertex(C.x, C.y, C.z, 1F, 0F, vertdata);
		AddCubeVertex(G.x, G.y, G.z, 0F, 0F, vertdata);
		AddCubeVertex(F.x, F.y, F.z, 0F, 1F, vertdata);
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	//-----------------------------------------------------------------------------
	public void AddCubeVertex(float fl0, float fl1, float fl2, float fl3, float fl4, List<float> vertdata)
	{
		vertdata.Add(fl0);
		vertdata.Add(fl1);
		vertdata.Add(fl2);
		vertdata.Add(fl3);
		vertdata.Add(fl4);
	}


	//-----------------------------------------------------------------------------
	// Purpose: Draw all of the controllers as X/Y/Z lines
	//-----------------------------------------------------------------------------
	public void RenderControllerAxes()
	{
		// don't draw controllers if somebody else has input focus
		if (m_pHMD.IsInputFocusCapturedByAnotherProcess())
		{
			return;
		}

		List<float> vertdataarray = new List<float>();

		m_uiControllerVertcount = 0;
		m_iTrackedControllerCount = 0;

		for (vr.TrackedDeviceIndex_t unTrackedDevice = vr.k_unTrackedDeviceIndex_Hmd + 1; unTrackedDevice < vr.k_unMaxTrackedDeviceCount; ++unTrackedDevice)
		{
			if (!m_pHMD.IsTrackedDeviceConnected(unTrackedDevice))
			{
				continue;
			}

			if (m_pHMD.GetTrackedDeviceClass(unTrackedDevice) != vr.TrackedDeviceClass_Controller)
			{
				continue;
			}

			m_iTrackedControllerCount += 1;

			if (!m_rTrackedDevicePose[unTrackedDevice].bPoseIsValid)
			{
				continue;
			}

			Matrix4 mat = m_rmat4DevicePose[unTrackedDevice];

			Vector4 center = mat * new Vector4(0F, 0F, 0F, 1F);

			for (int i = 0; i < 3; ++i)
			{
				Vector3 color = new Vector3(0F, 0F, 0F);
				Vector4 point = new Vector4(0F, 0F, 0F, 1F);
				point[i] += 0.05f; // offset in X, Y, Z
				color[i] = 1.0; // R, G, B
				point = mat * point;
				vertdataarray.Add(center.x);
				vertdataarray.Add(center.y);
				vertdataarray.Add(center.z);

				vertdataarray.Add(color.x);
				vertdataarray.Add(color.y);
				vertdataarray.Add(color.z);

				vertdataarray.Add(point.x);
				vertdataarray.Add(point.y);
				vertdataarray.Add(point.z);

				vertdataarray.Add(color.x);
				vertdataarray.Add(color.y);
				vertdataarray.Add(color.z);

				m_uiControllerVertcount += 2;
			}

			Vector4 start = mat * new Vector4(0F, 0F, -0.02f, 1F);
			Vector4 end = mat * new Vector4(0F, 0F, -39.0f, 1F);
			Vector3 color = new Vector3(.92f, .92f, .71f);

			vertdataarray.Add(start.x);
			vertdataarray.Add(start.y);
			vertdataarray.Add(start.z);
			vertdataarray.Add(color.x);
			vertdataarray.Add(color.y);
			vertdataarray.Add(color.z);

			vertdataarray.Add(end.x);
			vertdataarray.Add(end.y);
			vertdataarray.Add(end.z);
			vertdataarray.Add(color.x);
			vertdataarray.Add(color.y);
			vertdataarray.Add(color.z);
			m_uiControllerVertcount += 2;
		}

		// Setup the VAO the first time through.
		if (m_unControllerVAO == 0)
		{
			glGenVertexArrays(1, m_unControllerVAO);
			glBindVertexArray(m_unControllerVAO);

			glGenBuffers(1, m_glControllerVertBuffer);
			glBindBuffer(GL_ARRAY_BUFFER, m_glControllerVertBuffer);

			GLuint stride = 2 * 3 * sizeof(float);
			uintptr_t offset = 0;

			glEnableVertexAttribArray(0);
			glVertexAttribPointer(0, 3, GL_FLOAT, GL_FALSE, stride, (object)offset);

			offset += sizeof(Vector3);
			glEnableVertexAttribArray(1);
			glVertexAttribPointer(1, 3, GL_FLOAT, GL_FALSE, stride, (object)offset);

			glBindVertexArray(0);
		}

		glBindBuffer(GL_ARRAY_BUFFER, m_glControllerVertBuffer);

		// set vertex data if we have some
		if (vertdataarray.Count > 0)
		{
			//$ TODO: Use glBufferSubData for this...
			glBufferData(GL_ARRAY_BUFFER, sizeof(float) * vertdataarray.Count, vertdataarray[0], GL_STREAM_DRAW);
		}
	}


	//-----------------------------------------------------------------------------
	// Purpose:
	//-----------------------------------------------------------------------------
	public bool SetupStereoRenderTargets()
	{
		if (m_pHMD == null)
		{
			return false;
		}

		m_pHMD.GetRecommendedRenderTargetSize(m_nRenderWidth, m_nRenderHeight);

//C++ TO C# CONVERTER TODO TASK: The following line was determined to contain a copy constructor call - this should be verified and a copy constructor should be created:
//ORIGINAL LINE: CreateFrameBuffer(m_nRenderWidth, m_nRenderHeight, leftEyeDesc);
		CreateFrameBuffer(new uint32_t(m_nRenderWidth), new uint32_t(m_nRenderHeight), leftEyeDesc);
//C++ TO C# CONVERTER TODO TASK: The following line was determined to contain a copy constructor call - this should be verified and a copy constructor should be created:
//ORIGINAL LINE: CreateFrameBuffer(m_nRenderWidth, m_nRenderHeight, rightEyeDesc);
		CreateFrameBuffer(new uint32_t(m_nRenderWidth), new uint32_t(m_nRenderHeight), rightEyeDesc);

		return true;
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	//-----------------------------------------------------------------------------
	public void SetupCompanionWindow()
	{
		if (m_pHMD == null)
		{
			return;
		}

		List<VertexDataWindow> vVerts = new List<VertexDataWindow>();

		// left eye verts
		vVerts.Add(new VertexDataWindow(new Vector2(-1F, -1F), new Vector2(0F, 1F)));
		vVerts.Add(new VertexDataWindow(new Vector2(0F, -1F), new Vector2(1F, 1F)));
		vVerts.Add(new VertexDataWindow(new Vector2(-1F, 1F), new Vector2(0F, 0F)));
		vVerts.Add(new VertexDataWindow(new Vector2(0F, 1F), new Vector2(1F, 0F)));

		// right eye verts
		vVerts.Add(new VertexDataWindow(new Vector2(0F, -1F), new Vector2(0F, 1F)));
		vVerts.Add(new VertexDataWindow(new Vector2(1F, -1F), new Vector2(1F, 1F)));
		vVerts.Add(new VertexDataWindow(new Vector2(0F, 1F), new Vector2(0F, 0F)));
		vVerts.Add(new VertexDataWindow(new Vector2(1F, 1F), new Vector2(1F, 0F)));

		GLushort[] vIndices = {0, 1, 3, 0, 3, 2, 4, 5, 7, 4, 7, 6};
//C++ TO C# CONVERTER WARNING: This 'sizeof' ratio was replaced with a direct reference to the array length:
//ORIGINAL LINE: m_uiCompanionWindowIndexSize = (sizeof(vIndices)/sizeof((vIndices)[0]));
		m_uiCompanionWindowIndexSize = (vIndices.Length);

		glGenVertexArrays(1, m_unCompanionWindowVAO);
		glBindVertexArray(m_unCompanionWindowVAO);

		glGenBuffers(1, m_glCompanionWindowIDVertBuffer);
		glBindBuffer(GL_ARRAY_BUFFER, m_glCompanionWindowIDVertBuffer);
		glBufferData(GL_ARRAY_BUFFER, vVerts.Count * sizeof(VertexDataWindow), vVerts[0], GL_STATIC_DRAW);

		glGenBuffers(1, m_glCompanionWindowIDIndexBuffer);
		glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, m_glCompanionWindowIDIndexBuffer);
		glBufferData(GL_ELEMENT_ARRAY_BUFFER, m_uiCompanionWindowIndexSize * sizeof(GLushort), vIndices[0], GL_STATIC_DRAW);

		glEnableVertexAttribArray(0);
		glVertexAttribPointer(0, 2, GL_FLOAT, GL_FALSE, sizeof(VertexDataWindow), (object)offsetof(VertexDataWindow, position));

		glEnableVertexAttribArray(1);
		glVertexAttribPointer(1, 2, GL_FLOAT, GL_FALSE, sizeof(VertexDataWindow), (object)offsetof(VertexDataWindow, texCoord));

		glBindVertexArray(0);

		glDisableVertexAttribArray(0);
		glDisableVertexAttribArray(1);

		glBindBuffer(GL_ARRAY_BUFFER, 0);
		glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, 0);
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	//-----------------------------------------------------------------------------
	public void SetupCameras()
	{
//C++ TO C# CONVERTER TODO TASK: The following line was determined to be a copy assignment (rather than a reference assignment) - this should be verified and a 'CopyFrom' method should be created:
//ORIGINAL LINE: m_mat4ProjectionLeft = GetHMDMatrixProjectionEye(vr::Eye_Left);
		m_mat4ProjectionLeft.CopyFrom(GetHMDMatrixProjectionEye(vr.Eye_Left));
//C++ TO C# CONVERTER TODO TASK: The following line was determined to be a copy assignment (rather than a reference assignment) - this should be verified and a 'CopyFrom' method should be created:
//ORIGINAL LINE: m_mat4ProjectionRight = GetHMDMatrixProjectionEye(vr::Eye_Right);
		m_mat4ProjectionRight.CopyFrom(GetHMDMatrixProjectionEye(vr.Eye_Right));
//C++ TO C# CONVERTER TODO TASK: The following line was determined to be a copy assignment (rather than a reference assignment) - this should be verified and a 'CopyFrom' method should be created:
//ORIGINAL LINE: m_mat4eyePosLeft = GetHMDMatrixPoseEye(vr::Eye_Left);
		m_mat4eyePosLeft.CopyFrom(GetHMDMatrixPoseEye(vr.Eye_Left));
//C++ TO C# CONVERTER TODO TASK: The following line was determined to be a copy assignment (rather than a reference assignment) - this should be verified and a 'CopyFrom' method should be created:
//ORIGINAL LINE: m_mat4eyePosRight = GetHMDMatrixPoseEye(vr::Eye_Right);
		m_mat4eyePosRight.CopyFrom(GetHMDMatrixPoseEye(vr.Eye_Right));
	}


	//-----------------------------------------------------------------------------
	// Purpose:
	//-----------------------------------------------------------------------------
	public void RenderStereoTargets()
	{
		glClearColor(0.0f, 0.0f, 0.0f, 1.0f);
		glEnable(GL_MULTISAMPLE);

		// Left Eye
		glBindFramebuffer(GL_FRAMEBUFFER, leftEyeDesc.m_nRenderFramebufferId);
		 glViewport(0, 0, m_nRenderWidth, m_nRenderHeight);
		 RenderScene(vr.Eye_Left);
		 glBindFramebuffer(GL_FRAMEBUFFER, 0);

		glDisable(GL_MULTISAMPLE);

		 glBindFramebuffer(GL_READ_FRAMEBUFFER, leftEyeDesc.m_nRenderFramebufferId);
		glBindFramebuffer(GL_DRAW_FRAMEBUFFER, leftEyeDesc.m_nResolveFramebufferId);

		glBlitFramebuffer(0, 0, m_nRenderWidth, m_nRenderHeight, 0, 0, m_nRenderWidth, m_nRenderHeight, GL_COLOR_BUFFER_BIT, GL_LINEAR);

		 glBindFramebuffer(GL_READ_FRAMEBUFFER, 0);
		glBindFramebuffer(GL_DRAW_FRAMEBUFFER, 0);

		glEnable(GL_MULTISAMPLE);

		// Right Eye
		glBindFramebuffer(GL_FRAMEBUFFER, rightEyeDesc.m_nRenderFramebufferId);
		 glViewport(0, 0, m_nRenderWidth, m_nRenderHeight);
		 RenderScene(vr.Eye_Right);
		 glBindFramebuffer(GL_FRAMEBUFFER, 0);

		glDisable(GL_MULTISAMPLE);

		 glBindFramebuffer(GL_READ_FRAMEBUFFER, rightEyeDesc.m_nRenderFramebufferId);
		glBindFramebuffer(GL_DRAW_FRAMEBUFFER, rightEyeDesc.m_nResolveFramebufferId);

		glBlitFramebuffer(0, 0, m_nRenderWidth, m_nRenderHeight, 0, 0, m_nRenderWidth, m_nRenderHeight, GL_COLOR_BUFFER_BIT, GL_LINEAR);

		 glBindFramebuffer(GL_READ_FRAMEBUFFER, 0);
		glBindFramebuffer(GL_DRAW_FRAMEBUFFER, 0);
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	//-----------------------------------------------------------------------------
	public void RenderCompanionWindow()
	{
		glDisable(GL_DEPTH_TEST);
		glViewport(0, 0, m_nCompanionWindowWidth, m_nCompanionWindowHeight);

		glBindVertexArray(m_unCompanionWindowVAO);
		glUseProgram(m_unCompanionWindowBProgramID);

		// render left eye (first half of index array )
		glBindTexture(GL_TEXTURE_2D, leftEyeDesc.m_nResolveTextureId);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
		glDrawElements(GL_TRIANGLES, m_uiCompanionWindowIndexSize / 2, GL_UNSIGNED_SHORT, 0);

		// render right eye (second half of index array )
		glBindTexture(GL_TEXTURE_2D, rightEyeDesc.m_nResolveTextureId);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
		glDrawElements(GL_TRIANGLES, m_uiCompanionWindowIndexSize / 2, GL_UNSIGNED_SHORT, (object)(uintptr_t)(m_uiCompanionWindowIndexSize));

		glBindVertexArray(0);
		glUseProgram(0);
	}
	public void RenderCompanionWindowB()
	{
		glDisable(GL_DEPTH_TEST);
		glViewport(0, 0, m_nCompanionWindowWidth, m_nCompanionWindowHeight);

		glBindVertexArray(m_unCompanionWindowVAO);
		glUseProgram(m_unCompanionWindowBProgramID);

		// render left eye (first half of index array )
		glBindTexture(GL_TEXTURE_2D, leftEyeDesc.m_nResolveTextureId);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
		glDrawElements(GL_TRIANGLES, m_uiCompanionWindowIndexSize / 2, GL_UNSIGNED_SHORT, 0);

		// render right eye (second half of index array )
		glBindTexture(GL_TEXTURE_2D, rightEyeDesc.m_nResolveTextureId);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_CLAMP_TO_EDGE);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_CLAMP_TO_EDGE);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
		glDrawElements(GL_TRIANGLES, m_uiCompanionWindowIndexSize / 2, GL_UNSIGNED_SHORT, (object)(uintptr_t)(m_uiCompanionWindowIndexSize));

		glBindVertexArray(0);
		glUseProgram(0);
	}

	//-----------------------------------------------------------------------------
	// Purpose: Renders a scene with respect to nEye.
	//-----------------------------------------------------------------------------
	public void RenderScene(vr.Hmd_Eye nEye)
	{
		glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
		glEnable(GL_DEPTH_TEST);

		if (m_bShowCubes)
		{
			glUseProgram(m_unSceneProgramID);
//C++ TO C# CONVERTER TODO TASK: The following line was determined to contain a copy constructor call - this should be verified and a copy constructor should be created:
//ORIGINAL LINE: glUniformMatrix4fv(m_nSceneMatrixLocation, 1, GL_FALSE, GetCurrentViewProjectionMatrix(nEye).get());
			glUniformMatrix4fv(m_nSceneMatrixLocation, 1, GL_FALSE, GetCurrentViewProjectionMatrix(new vr.Hmd_Eye(nEye)).get());
			glBindVertexArray(m_unSceneVAO);
			glBindTexture(GL_TEXTURE_2D, m_iTexture);
			glDrawArrays(GL_TRIANGLES, 0, m_uiVertcount);
			glBindVertexArray(0);
		}

		bool bIsInputCapturedByAnotherProcess = m_pHMD.IsInputFocusCapturedByAnotherProcess();

		if (!bIsInputCapturedByAnotherProcess)
		{
			// draw the controller axis lines
			glUseProgram(m_unControllerTransformProgramID);
//C++ TO C# CONVERTER TODO TASK: The following line was determined to contain a copy constructor call - this should be verified and a copy constructor should be created:
//ORIGINAL LINE: glUniformMatrix4fv(m_nControllerMatrixLocation, 1, GL_FALSE, GetCurrentViewProjectionMatrix(nEye).get());
			glUniformMatrix4fv(m_nControllerMatrixLocation, 1, GL_FALSE, GetCurrentViewProjectionMatrix(new vr.Hmd_Eye(nEye)).get());
			glBindVertexArray(m_unControllerVAO);
			glDrawArrays(GL_LINES, 0, m_uiControllerVertcount);
			glBindVertexArray(0);
		}

		// ----- Render Model rendering -----
		glUseProgram(m_unRenderModelProgramID);

		for (uint32_t unTrackedDevice = 0; unTrackedDevice < vr.k_unMaxTrackedDeviceCount; unTrackedDevice++)
		{
			if (m_rTrackedDeviceToRenderModel[unTrackedDevice] == null || !m_rbShowTrackedDevice[unTrackedDevice])
			{
				continue;
			}

			vr.TrackedDevicePose_t pose = m_rTrackedDevicePose[unTrackedDevice];
			if (!pose.bPoseIsValid)
			{
				continue;
			}

			if (bIsInputCapturedByAnotherProcess && m_pHMD.GetTrackedDeviceClass(unTrackedDevice) == vr.TrackedDeviceClass_Controller)
			{
				continue;
			}

			Matrix4 matDeviceToTracking = m_rmat4DevicePose[unTrackedDevice];
//C++ TO C# CONVERTER TODO TASK: The following line was determined to contain a copy constructor call - this should be verified and a copy constructor should be created:
//ORIGINAL LINE: Matrix4 matMVP = GetCurrentViewProjectionMatrix(nEye) * matDeviceToTracking;
			Matrix4 matMVP = GetCurrentViewProjectionMatrix(new vr.Hmd_Eye(nEye)) * matDeviceToTracking;
			glUniformMatrix4fv(m_nRenderModelMatrixLocation, 1, GL_FALSE, matMVP.get());

			m_rTrackedDeviceToRenderModel[unTrackedDevice].Draw();
		}

		glUseProgram(0);
	}


	//-----------------------------------------------------------------------------
	// Purpose: Gets a Matrix Projection Eye with respect to nEye.
	//-----------------------------------------------------------------------------
	public Matrix4 GetHMDMatrixProjectionEye(vr.Hmd_Eye nEye)
	{
		if (m_pHMD == null)
		{
			return new Matrix4();
		}

		vr.HmdMatrix44_t mat = m_pHMD.GetProjectionMatrix(nEye, m_fNearClip, m_fFarClip);

		return new Matrix4(mat.m[0][0], mat.m[1][0], mat.m[2][0], mat.m[3][0], mat.m[0][1], mat.m[1][1], mat.m[2][1], mat.m[3][1], mat.m[0][2], mat.m[1][2], mat.m[2][2], mat.m[3][2], mat.m[0][3], mat.m[1][3], mat.m[2][3], mat.m[3][3]);
	}

	//-----------------------------------------------------------------------------
	// Purpose: Gets an HMDMatrixPoseEye with respect to nEye.
	//-----------------------------------------------------------------------------
	public Matrix4 GetHMDMatrixPoseEye(vr.Hmd_Eye nEye)
	{
		if (m_pHMD == null)
		{
			return new Matrix4();
		}

		vr.HmdMatrix34_t matEyeRight = m_pHMD.GetEyeToHeadTransform(nEye);
		Matrix4 matrixObj = new Matrix4(matEyeRight.m[0][0], matEyeRight.m[1][0], matEyeRight.m[2][0], 0.0F, matEyeRight.m[0][1], matEyeRight.m[1][1], matEyeRight.m[2][1], 0.0F, matEyeRight.m[0][2], matEyeRight.m[1][2], matEyeRight.m[2][2], 0.0F, matEyeRight.m[0][3], matEyeRight.m[1][3], matEyeRight.m[2][3], 1.0f);

		return matrixObj.invert();
	}

	//-----------------------------------------------------------------------------
	// Purpose: Gets a Current View Projection Matrix with respect to nEye,
	//          which may be an Eye_Left or an Eye_Right.
	//-----------------------------------------------------------------------------
	public Matrix4 GetCurrentViewProjectionMatrix(vr.Hmd_Eye nEye)
	{
		Matrix4 matMVP = new Matrix4();
		if (nEye == vr.Eye_Left)
		{
//C++ TO C# CONVERTER TODO TASK: The following line was determined to be a copy assignment (rather than a reference assignment) - this should be verified and a 'CopyFrom' method should be created:
//ORIGINAL LINE: matMVP = m_mat4ProjectionLeft * m_mat4eyePosLeft * m_mat4HMDPose;
			matMVP.CopyFrom(m_mat4ProjectionLeft * m_mat4eyePosLeft * m_mat4HMDPose);
		}
		else if (nEye == vr.Eye_Right)
		{
//C++ TO C# CONVERTER TODO TASK: The following line was determined to be a copy assignment (rather than a reference assignment) - this should be verified and a 'CopyFrom' method should be created:
//ORIGINAL LINE: matMVP = m_mat4ProjectionRight * m_mat4eyePosRight * m_mat4HMDPose;
			matMVP.CopyFrom(m_mat4ProjectionRight * m_mat4eyePosRight * m_mat4HMDPose);
		}

		return matMVP;
	}

	//-----------------------------------------------------------------------------
	// Purpose:
	//-----------------------------------------------------------------------------
	public void UpdateHMDMatrixPose()
	{
		if (m_pHMD == null)
		{
			return;
		}

		vr.VRCompositor().WaitGetPoses(m_rTrackedDevicePose, vr.k_unMaxTrackedDeviceCount, null, 0);

		m_iValidPoseCount = 0;
		m_strPoseClasses = "";
		for (int nDevice = 0; nDevice < vr.k_unMaxTrackedDeviceCount; ++nDevice)
		{
			if (m_rTrackedDevicePose[nDevice].bPoseIsValid)
			{
				m_iValidPoseCount++;
				m_rmat4DevicePose[nDevice] = ConvertSteamVRMatrixToMatrix4(m_rTrackedDevicePose[nDevice].mDeviceToAbsoluteTracking);
				if (m_rDevClassChar[nDevice] == 0)
				{
					switch (m_pHMD.GetTrackedDeviceClass(nDevice))
					{
					case vr.TrackedDeviceClass_Controller:
						m_rDevClassChar = StringFunctions.ChangeCharacter(m_rDevClassChar, nDevice, 'C');
						break;
					case vr.TrackedDeviceClass_HMD:
						m_rDevClassChar = StringFunctions.ChangeCharacter(m_rDevClassChar, nDevice, 'H');
						break;
					case vr.TrackedDeviceClass_Invalid:
						m_rDevClassChar = StringFunctions.ChangeCharacter(m_rDevClassChar, nDevice, 'I');
						break;
					case vr.TrackedDeviceClass_GenericTracker:
						m_rDevClassChar = StringFunctions.ChangeCharacter(m_rDevClassChar, nDevice, 'G');
						break;
					case vr.TrackedDeviceClass_TrackingReference:
						m_rDevClassChar = StringFunctions.ChangeCharacter(m_rDevClassChar, nDevice, 'T');
						break;
					default:
						m_rDevClassChar = StringFunctions.ChangeCharacter(m_rDevClassChar, nDevice, '?');
						break;
					}
				}
				m_strPoseClasses += m_rDevClassChar[nDevice];
			}
		}

		if (m_rTrackedDevicePose[vr.k_unTrackedDeviceIndex_Hmd].bPoseIsValid)
		{
//C++ TO C# CONVERTER TODO TASK: The following line was determined to be a copy assignment (rather than a reference assignment) - this should be verified and a 'CopyFrom' method should be created:
//ORIGINAL LINE: m_mat4HMDPose = m_rmat4DevicePose[vr::k_unTrackedDeviceIndex_Hmd];
			m_mat4HMDPose.CopyFrom(m_rmat4DevicePose[vr.k_unTrackedDeviceIndex_Hmd]);
			m_mat4HMDPose.invert();
		}
	}


	//-----------------------------------------------------------------------------
	// Purpose: Converts a SteamVR matrix to our local matrix class
	//-----------------------------------------------------------------------------
	public Matrix4 ConvertSteamVRMatrixToMatrix4(vr.HmdMatrix34_t matPose)
	{
		Matrix4 matrixObj = new Matrix4(matPose.m[0][0], matPose.m[1][0], matPose.m[2][0], 0.0F, matPose.m[0][1], matPose.m[1][1], matPose.m[2][1], 0.0F, matPose.m[0][2], matPose.m[1][2], matPose.m[2][2], 0.0F, matPose.m[0][3], matPose.m[1][3], matPose.m[2][3], 1.0f);
		return matrixObj;
	}


	//-----------------------------------------------------------------------------
	// Purpose: Compiles a GL shader program and returns the handle. Returns 0 if
	//			the shader couldn't be compiled for some reason.
	//-----------------------------------------------------------------------------
	public GLuint CompileGLShader(string pchShaderName, string pchVertexShader, string pchFragmentShader)
	{
		GLuint unProgramID = glCreateProgram();

		GLuint nSceneVertexShader = glCreateShader(GL_VERTEX_SHADER);
		glShaderSource(nSceneVertexShader, 1, pchVertexShader, null);
		glCompileShader(nSceneVertexShader);

		GLint vShaderCompiled = GL_FALSE;
		glGetShaderiv(nSceneVertexShader, GL_COMPILE_STATUS, vShaderCompiled);
		if (vShaderCompiled != GL_TRUE)
		{
			dprintf("%s - Unable to compile vertex shader %d!\n", pchShaderName, nSceneVertexShader);
			glDeleteProgram(unProgramID);
			glDeleteShader(nSceneVertexShader);
			return 0;
		}
		glAttachShader(unProgramID, nSceneVertexShader);
		glDeleteShader(nSceneVertexShader); // the program hangs onto this once it's attached

		GLuint nSceneFragmentShader = glCreateShader(GL_FRAGMENT_SHADER);
		glShaderSource(nSceneFragmentShader, 1, pchFragmentShader, null);
		glCompileShader(nSceneFragmentShader);

		GLint fShaderCompiled = GL_FALSE;
		glGetShaderiv(nSceneFragmentShader, GL_COMPILE_STATUS, fShaderCompiled);
		if (fShaderCompiled != GL_TRUE)
		{
			dprintf("%s - Unable to compile fragment shader %d!\n", pchShaderName, nSceneFragmentShader);
			glDeleteProgram(unProgramID);
			glDeleteShader(nSceneFragmentShader);
			return 0;
		}

		glAttachShader(unProgramID, nSceneFragmentShader);
		glDeleteShader(nSceneFragmentShader); // the program hangs onto this once it's attached

		glLinkProgram(unProgramID);

		GLint programSuccess = GL_TRUE;
		glGetProgramiv(unProgramID, GL_LINK_STATUS, programSuccess);
		if (programSuccess != GL_TRUE)
		{
			dprintf("%s - Error linking program %d!\n", pchShaderName, unProgramID);
			glDeleteProgram(unProgramID);
			return 0;
		}

		glUseProgram(unProgramID);
		glUseProgram(0);

		return unProgramID;
	}

	//-----------------------------------------------------------------------------
	// Purpose: Creates all the shaders used by HelloVR SDL
	//-----------------------------------------------------------------------------
	public bool CreateAllShaders()
	{
//C++ TO C# CONVERTER TODO TASK: The following line was determined to be a copy assignment (rather than a reference assignment) - this should be verified and a 'CopyFrom' method should be created:
//ORIGINAL LINE: m_unSceneProgramID = CompileGLShader("Scene", "#version 410\n" + "uniform mat4 matrix;\n" + "layout(location = 0) in vec4 position;\n" + "layout(location = 1) in vec2 v2UVcoordsIn;\n" + "layout(location = 2) in vec3 v3NormalIn;\n" + "out vec2 v2UVcoords;\n" + "void main()\n" + "{\n" + "	v2UVcoords = v2UVcoordsIn;\n" + "	gl_Position = matrix * position;\n" + "}\n", "#version 410 core\n" + "uniform sampler2D mytexture;\n" + "in vec2 v2UVcoords;\n" + "out vec4 outputColor;\n" + "void main()\n" + "{\n" + "   outputColor = texture(mytexture, v2UVcoords);\n" + "}\n");
		m_unSceneProgramID.CopyFrom(CompileGLShader("Scene", "#version 410\n" + "uniform mat4 matrix;\n" + "layout(location = 0) in vec4 position;\n" + "layout(location = 1) in vec2 v2UVcoordsIn;\n" + "layout(location = 2) in vec3 v3NormalIn;\n" + "out vec2 v2UVcoords;\n" + "void main()\n" + "{\n" + "	v2UVcoords = v2UVcoordsIn;\n" + "	gl_Position = matrix * position;\n" + "}\n", "#version 410 core\n" + "uniform sampler2D mytexture;\n" + "in vec2 v2UVcoords;\n" + "out vec4 outputColor;\n" + "void main()\n" + "{\n" + "   outputColor = texture(mytexture, v2UVcoords);\n" + "}\n"));
			// Vertex Shader
			// Fragment Shader
		m_nSceneMatrixLocation = glGetUniformLocation(m_unSceneProgramID, "matrix");
		if (m_nSceneMatrixLocation == -1)
		{
			dprintf("Unable to find matrix uniform in scene shader\n");
			return false;
		}

//C++ TO C# CONVERTER TODO TASK: The following line was determined to be a copy assignment (rather than a reference assignment) - this should be verified and a 'CopyFrom' method should be created:
//ORIGINAL LINE: m_unControllerTransformProgramID = CompileGLShader("Controller", "#version 410\n" + "uniform mat4 matrix;\n" + "layout(location = 0) in vec4 position;\n" + "layout(location = 1) in vec3 v3ColorIn;\n" + "out vec4 v4Color;\n" + "void main()\n" + "{\n" + "	v4Color.xyz = v3ColorIn; v4Color.a = 1.0;\n" + "	gl_Position = matrix * position;\n" + "}\n", "#version 410\n" + "in vec4 v4Color;\n" + "out vec4 outputColor;\n" + "void main()\n" + "{\n" + "   outputColor = v4Color;\n" + "}\n");
		m_unControllerTransformProgramID.CopyFrom(CompileGLShader("Controller", "#version 410\n" + "uniform mat4 matrix;\n" + "layout(location = 0) in vec4 position;\n" + "layout(location = 1) in vec3 v3ColorIn;\n" + "out vec4 v4Color;\n" + "void main()\n" + "{\n" + "	v4Color.xyz = v3ColorIn; v4Color.a = 1.0;\n" + "	gl_Position = matrix * position;\n" + "}\n", "#version 410\n" + "in vec4 v4Color;\n" + "out vec4 outputColor;\n" + "void main()\n" + "{\n" + "   outputColor = v4Color;\n" + "}\n"));
			// vertex shader
			// fragment shader
		m_nControllerMatrixLocation = glGetUniformLocation(m_unControllerTransformProgramID, "matrix");
		if (m_nControllerMatrixLocation == -1)
		{
			dprintf("Unable to find matrix uniform in controller shader\n");
			return false;
		}

//C++ TO C# CONVERTER TODO TASK: The following line was determined to be a copy assignment (rather than a reference assignment) - this should be verified and a 'CopyFrom' method should be created:
//ORIGINAL LINE: m_unRenderModelProgramID = CompileGLShader("render model", "#version 410\n" + "uniform mat4 matrix;\n" + "layout(location = 0) in vec4 position;\n" + "layout(location = 1) in vec3 v3NormalIn;\n" + "layout(location = 2) in vec2 v2TexCoordsIn;\n" + "out vec2 v2TexCoord;\n" + "void main()\n" + "{\n" + "	v2TexCoord = v2TexCoordsIn;\n" + "	gl_Position = matrix * vec4(position.xyz, 1);\n" + "}\n", "#version 410 core\n" + "uniform sampler2D diffuse;\n" + "in vec2 v2TexCoord;\n" + "out vec4 outputColor;\n" + "void main()\n" + "{\n" + "   outputColor = texture( diffuse, v2TexCoord);\n" + "}\n");
		m_unRenderModelProgramID.CopyFrom(CompileGLShader("render model", "#version 410\n" + "uniform mat4 matrix;\n" + "layout(location = 0) in vec4 position;\n" + "layout(location = 1) in vec3 v3NormalIn;\n" + "layout(location = 2) in vec2 v2TexCoordsIn;\n" + "out vec2 v2TexCoord;\n" + "void main()\n" + "{\n" + "	v2TexCoord = v2TexCoordsIn;\n" + "	gl_Position = matrix * vec4(position.xyz, 1);\n" + "}\n", "#version 410 core\n" + "uniform sampler2D diffuse;\n" + "in vec2 v2TexCoord;\n" + "out vec4 outputColor;\n" + "void main()\n" + "{\n" + "   outputColor = texture( diffuse, v2TexCoord);\n" + "}\n"));
			// vertex shader
			//fragment shader
		m_nRenderModelMatrixLocation = glGetUniformLocation(m_unRenderModelProgramID, "matrix");
		if (m_nRenderModelMatrixLocation == -1)
		{
			dprintf("Unable to find matrix uniform in render model shader\n");
			return false;
		}

//C++ TO C# CONVERTER TODO TASK: The following line was determined to be a copy assignment (rather than a reference assignment) - this should be verified and a 'CopyFrom' method should be created:
//ORIGINAL LINE: m_unCompanionWindowBProgramID = CompileGLShader("CompanionWindow", "#version 410 core\n" + "layout(location = 0) in vec4 position;\n" + "layout(location = 1) in vec2 v2UVIn;\n" + "noperspective out vec2 v2UV;\n" + "void main()\n" + "{\n" + "	v2UV = v2UVIn;\n" + "	gl_Position = position;\n" + "}\n", "#version 410 core\n" + "uniform sampler2D mytexture;\n" + "noperspective in vec2 v2UV;\n" + "out vec4 outputColor;\n" + "void main()\n" + "{\n" + "		outputColor = texture(mytexture, v2UV);\n" + "}\n");
		m_unCompanionWindowBProgramID.CopyFrom(CompileGLShader("CompanionWindow", "#version 410 core\n" + "layout(location = 0) in vec4 position;\n" + "layout(location = 1) in vec2 v2UVIn;\n" + "noperspective out vec2 v2UV;\n" + "void main()\n" + "{\n" + "	v2UV = v2UVIn;\n" + "	gl_Position = position;\n" + "}\n", "#version 410 core\n" + "uniform sampler2D mytexture;\n" + "noperspective in vec2 v2UV;\n" + "out vec4 outputColor;\n" + "void main()\n" + "{\n" + "		outputColor = texture(mytexture, v2UV);\n" + "}\n"));
			// vertex shader
			// fragment shader

		return m_unSceneProgramID != 0 && m_unControllerTransformProgramID != 0 && m_unRenderModelProgramID != 0 && m_unCompanionWindowBProgramID != 0;
	}


	//-----------------------------------------------------------------------------
	// Purpose: Create/destroy GL a Render Model for a single tracked device
	//-----------------------------------------------------------------------------
	public void SetupRenderModelForTrackedDevice(vr.TrackedDeviceIndex_t unTrackedDeviceIndex)
	{
		if (unTrackedDeviceIndex >= vr.k_unMaxTrackedDeviceCount)
		{
			return;
		}

		// try to find a model we've already set up
//C++ TO C# CONVERTER TODO TASK: The following line was determined to contain a copy constructor call - this should be verified and a copy constructor should be created:
//ORIGINAL LINE: string sRenderModelName = GetTrackedDeviceString(m_pHMD, unTrackedDeviceIndex, vr::Prop_RenderModelName_String);
		string sRenderModelName = GlobalMembers.GetTrackedDeviceString(m_pHMD, new vr.TrackedDeviceIndex_t(unTrackedDeviceIndex), vr.Prop_RenderModelName_String);
		CGLRenderModel pRenderModel = FindOrLoadRenderModel(sRenderModelName);
		if (pRenderModel == null)
		{
//C++ TO C# CONVERTER TODO TASK: The following line was determined to contain a copy constructor call - this should be verified and a copy constructor should be created:
//ORIGINAL LINE: string sTrackingSystemName = GetTrackedDeviceString(m_pHMD, unTrackedDeviceIndex, vr::Prop_TrackingSystemName_String);
			string sTrackingSystemName = GlobalMembers.GetTrackedDeviceString(m_pHMD, new vr.TrackedDeviceIndex_t(unTrackedDeviceIndex), vr.Prop_TrackingSystemName_String);
			dprintf("Unable to load render model for tracked device %d (%s.%s)", unTrackedDeviceIndex, sTrackingSystemName, sRenderModelName);
		}
		else
		{
			m_rTrackedDeviceToRenderModel[unTrackedDeviceIndex] = pRenderModel;
			m_rbShowTrackedDevice[unTrackedDeviceIndex] = true;
		}
	}

	//-----------------------------------------------------------------------------
	// Purpose: Finds a render model we've already loaded or loads a new one
	//-----------------------------------------------------------------------------
	public CGLRenderModel FindOrLoadRenderModel(string pchRenderModelName)
	{
		CGLRenderModel pRenderModel = null;
		foreach (CGLRenderModel * i in m_vecRenderModels)
		{
			if (!string.Compare(i.GetName().c_str(), pchRenderModelName, true))
			{
				pRenderModel = i;
				break;
			}
		}

		// load the model if we didn't find one
		if (pRenderModel == null)
		{
			vr.RenderModel_t pModel;
			vr.EVRRenderModelError error = new vr.EVRRenderModelError();
			while (true)
			{
				error = vr.VRRenderModels().LoadRenderModel_Async(pchRenderModelName, pModel);
				if (error != vr.VRRenderModelError_Loading)
				{
					break;
				}

				GlobalMembers.ThreadSleep(1);
			}

			if (error != vr.VRRenderModelError_None)
			{
				dprintf("Unable to load render model %s - %s\n", pchRenderModelName, vr.VRRenderModels().GetRenderModelErrorNameFromEnum(error));
				return null; // move on to the next tracked device
			}

			vr.RenderModel_TextureMap_t pTexture;
			while (true)
			{
				error = vr.VRRenderModels().LoadTexture_Async(pModel.diffuseTextureId, pTexture);
				if (error != vr.VRRenderModelError_Loading)
				{
					break;
				}

				GlobalMembers.ThreadSleep(1);
			}

			if (error != vr.VRRenderModelError_None)
			{
				dprintf("Unable to load render texture id:%d for render model %s\n", pModel.diffuseTextureId, pchRenderModelName);
				vr.VRRenderModels().FreeRenderModel(pModel);
				return null; // move on to the next tracked device
			}

			pRenderModel = new CGLRenderModel(pchRenderModelName);
			if (!pRenderModel.BInit(pModel, pTexture))
			{
				GlobalMembers.dprintf("Unable to create GL model from render model %s\n", pchRenderModelName);
				if (pRenderModel != null)
				{
					pRenderModel.Dispose();
				}
				pRenderModel = null;
			}
			else
			{
				m_vecRenderModels.Add(pRenderModel);
			}
			vr.VRRenderModels().FreeRenderModel(pModel);
			vr.VRRenderModels().FreeTexture(pTexture);
		}
		return pRenderModel;
	}

	private bool m_bDebugOpenGL;
	private bool m_bVerbose;
	private bool m_bPerf;
	private bool m_bVblank;
	private bool m_bGlFinishHack;

	private vr.IVRSystem m_pHMD;
	private vr.IVRRenderModels m_pRenderModels;
	private string m_strDriver;
	private string m_strDisplay;
	private vr.TrackedDevicePose_t[] m_rTrackedDevicePose = Arrays.InitializeWithDefaultInstances<TrackedDevicePose_t>(vr.k_unMaxTrackedDeviceCount);
	private Matrix4[] m_rmat4DevicePose = Arrays.InitializeWithDefaultInstances<Matrix4>(vr.k_unMaxTrackedDeviceCount);
	private bool[] m_rbShowTrackedDevice = new bool[vr.k_unMaxTrackedDeviceCount];

	private SDL_Window m_pCompanionWindow;
	private SDL_Window m_pCompanionWindowB;
	private uint32_t m_nCompanionWindowWidth = new uint32_t();
	private uint32_t m_nCompanionWindowHeight = new uint32_t();

	private SDL_GLContext m_pContext = new SDL_GLContext();
	private SDL_GLContext m_pContextB = new SDL_GLContext();

	private int m_iTrackedControllerCount;
	private int m_iTrackedControllerCount_Last;
	private int m_iValidPoseCount;
	private int m_iValidPoseCount_Last;
	private bool m_bShowCubes;

	private string m_strPoseClasses; // what classes we saw poses for this frame
	private string m_rDevClassChar = new string(new char[vr.k_unMaxTrackedDeviceCount]); // for each device, a character representing its class

	private int m_iSceneVolumeWidth;
	private int m_iSceneVolumeHeight;
	private int m_iSceneVolumeDepth;
	private float m_fScaleSpacing;
	private float m_fScale;

	private int m_iSceneVolumeInit; // if you want something other than the default 20x20x20

	private float m_fNearClip;
	private float m_fFarClip;

	private GLuint m_iTexture = new GLuint();

	private uint m_uiVertcount;

	private GLuint m_glSceneVertBuffer = new GLuint();
	private GLuint m_unSceneVAO = new GLuint();
	private GLuint m_unCompanionWindowVAO = new GLuint();
	private GLuint m_glCompanionWindowIDVertBuffer = new GLuint();
	private GLuint m_glCompanionWindowIDIndexBuffer = new GLuint();
	private uint m_uiCompanionWindowIndexSize;
	private GLuint m_unCompanionWindowBVAO = new GLuint();
	private GLuint m_glCompanionWindowBIDVertBuffer = new GLuint();
	private GLuint m_glCompanionWindowBIDIndexBuffer = new GLuint();
	private uint m_uiCompanionWindowBIndexSize;

	private GLuint m_glControllerVertBuffer = new GLuint();
	private GLuint m_unControllerVAO = new GLuint();
	private uint m_uiControllerVertcount;

	private Matrix4 m_mat4HMDPose = new Matrix4();
	private Matrix4 m_mat4eyePosLeft = new Matrix4();
	private Matrix4 m_mat4eyePosRight = new Matrix4();

	private Matrix4 m_mat4ProjectionCenter = new Matrix4();
	private Matrix4 m_mat4ProjectionLeft = new Matrix4();
	private Matrix4 m_mat4ProjectionRight = new Matrix4();

	private class VertexDataScene
	{
		public Vector3 position = new Vector3();
		public Vector2 texCoord = new Vector2();
	}

	private class VertexDataWindow
	{
		public Vector2 position = new Vector2();
		public Vector2 texCoord = new Vector2();

		public VertexDataWindow(Vector2 pos, Vector2 tex)
		{
			this.position = new Vector2(pos);
			this.texCoord = new Vector2(tex);
		}
	}

	private GLuint m_unSceneProgramID = new GLuint();
	private GLuint m_unCompanionWindowBProgramID = new GLuint();
	private GLuint m_unControllerTransformProgramID = new GLuint();
	private GLuint m_unRenderModelProgramID = new GLuint();

	private GLint m_nSceneMatrixLocation = new GLint();
	private GLint m_nControllerMatrixLocation = new GLint();
	private GLint m_nRenderModelMatrixLocation = new GLint();

	private class FramebufferDesc
	{
		public GLuint m_nDepthBufferId = new GLuint();
		public GLuint m_nRenderTextureId = new GLuint();
		public GLuint m_nRenderFramebufferId = new GLuint();
		public GLuint m_nResolveTextureId = new GLuint();
		public GLuint m_nResolveFramebufferId = new GLuint();
	}
	private FramebufferDesc leftEyeDesc = new FramebufferDesc();
	private FramebufferDesc rightEyeDesc = new FramebufferDesc();


	//-----------------------------------------------------------------------------
	// Purpose: Creates a frame buffer. Returns true if the buffer was set up.
	//          Returns false if the setup failed.
	//-----------------------------------------------------------------------------
	private bool CreateFrameBuffer(int nWidth, int nHeight, FramebufferDesc framebufferDesc)
	{
		glGenFramebuffers(1, framebufferDesc.m_nRenderFramebufferId);
		glBindFramebuffer(GL_FRAMEBUFFER, framebufferDesc.m_nRenderFramebufferId);

		glGenRenderbuffers(1, framebufferDesc.m_nDepthBufferId);
		glBindRenderbuffer(GL_RENDERBUFFER, framebufferDesc.m_nDepthBufferId);
		glRenderbufferStorageMultisample(GL_RENDERBUFFER, 4, GL_DEPTH_COMPONENT, nWidth, nHeight);
		glFramebufferRenderbuffer(GL_FRAMEBUFFER, GL_DEPTH_ATTACHMENT, GL_RENDERBUFFER, framebufferDesc.m_nDepthBufferId);

		glGenTextures(1, framebufferDesc.m_nRenderTextureId);
		glBindTexture(GL_TEXTURE_2D_MULTISAMPLE, framebufferDesc.m_nRenderTextureId);
		glTexImage2DMultisample(GL_TEXTURE_2D_MULTISAMPLE, 4, GL_RGBA8, nWidth, nHeight, true);
		glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D_MULTISAMPLE, framebufferDesc.m_nRenderTextureId, 0);

		glGenFramebuffers(1, framebufferDesc.m_nResolveFramebufferId);
		glBindFramebuffer(GL_FRAMEBUFFER, framebufferDesc.m_nResolveFramebufferId);

		glGenTextures(1, framebufferDesc.m_nResolveTextureId);
		glBindTexture(GL_TEXTURE_2D, framebufferDesc.m_nResolveTextureId);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
		glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAX_LEVEL, 0);
		glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA8, nWidth, nHeight, 0, GL_RGBA, GL_UNSIGNED_BYTE, null);
		glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, framebufferDesc.m_nResolveTextureId, 0);

		// check FBO status
		GLenum status = glCheckFramebufferStatus(GL_FRAMEBUFFER);
		if (status != GL_FRAMEBUFFER_COMPLETE)
		{
			return false;
		}

		glBindFramebuffer(GL_FRAMEBUFFER, 0);

		return true;
	}

	private uint32_t m_nRenderWidth = new uint32_t();
	private uint32_t m_nRenderHeight = new uint32_t();

	private List< CGLRenderModel  > m_vecRenderModels = new List< CGLRenderModel  >();
	private CGLRenderModel[] m_rTrackedDeviceToRenderModel = Arrays.InitializeWithDefaultInstances<CGLRenderModel>(vr.k_unMaxTrackedDeviceCount);
}