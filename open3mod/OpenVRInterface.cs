using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Specialized;
using Valve.VR;
using OpenTK;

namespace open3mod
{
    public class OpenVRInterface
    {
        private static ETrackingUniverseOrigin eOrg = ETrackingUniverseOrigin.TrackingUniverseStanding;

        public static EVRInitError EVRerror = EVRInitError.None;
        public static float fPredictedSecondsToPhotonsFromNow = 0f;
        public static Matrix4 viewOffset = Matrix4.Identity;
        public static Matrix4 viewOffsetShift = Matrix4.Identity;
        public static Matrix4 hmdRefPos = Matrix4.Identity;
        public static float trackerAboveLens = 0.15f; //height of controller above camera lens center
        public static float trackerBeforeLens = 0.05f; //distance between controller and lens optical cross
        public static float trackerAsideOfLens = 0.013f; //distance between controllers and lens centers
        public static float trackerOffXAxis = 0.1f; //radians - angle between controllers and lens Z axis 
        public static float lensAboveGround = 0.06f; //height of camera lens center above ground
        public static int maxPositionsBuffer = 5;
        public static uint virtualDevices = 1;
        public static uint totalDevices = OpenVR.k_unMaxTrackedDeviceCount + virtualDevices;
        public static Matrix4[,] allPositions = new Matrix4[maxPositionsBuffer, OpenVR.k_unMaxTrackedDeviceCount];//memory buffer only for real devices

        public static Matrix4[] trackedPositions = new Matrix4[totalDevices]; 
        public static bool[] activePositions = new bool[totalDevices]; //only active trackers
        public static Matrix4[] lensToGround = new Matrix4[totalDevices]; //shift from lens to the ground for each device wanted during reset
        public static Matrix4[] trackerToCamera = new Matrix4[totalDevices];

        public static ETrackedDeviceClass[] deviceClasses = new ETrackedDeviceClass[totalDevices]; //what sits at which index
        public static string[] deviceSNs = new string[totalDevices];
        public static string[] deviceName = new string[totalDevices];
        public static int[] deviceAdditionalDelay = new int[totalDevices];

        public static uint[] indexOfDevice = new uint[MainWindow.inputs];
        //existing devices:
        // HMD = 0, Cont1 = 1, Cont2 = 2, Virtual = 3;
        // Mapping:
        // A/ Video inputs to Camera Numbers(0..inputs-1) ...now automatic in Camera Preview
        // B/ Camera Numbers to Devices( + other devices may exists - trackers, and some Devices are virtual) .. now here in Setup devices
        // C/ Devices to ContIndexes
        // D/ Camera Numbers to Audio Pairs
        // Other Mapping exists for Device to CameraMode... this gives Camera to CameraMode

        //smer Z se nastavi resetem, po montáži se podle SN nebo jine fix identifikace priradi correct i shiftmatrixy pri loadingu
        public static float maxAdvance = 0.08f;

        public static Matrix4 OpenVRMatrixToOpenTKMatrix(HmdMatrix34_t matrix)
        {
            var newmatrix = new Matrix4();
            newmatrix.M11 = matrix.m0;
            newmatrix.M21 = matrix.m1;
            newmatrix.M31 = matrix.m2;
            newmatrix.M41 = matrix.m3;
            newmatrix.M12 = matrix.m4;
            newmatrix.M22 = matrix.m5;
            newmatrix.M32 = matrix.m6;
            newmatrix.M42 = matrix.m7;
            newmatrix.M13 = matrix.m8;
            newmatrix.M23 = matrix.m9;
            newmatrix.M33 = matrix.m10;
            newmatrix.M43 = matrix.m11;
            newmatrix.M14 = 0f;
            newmatrix.M24 = 0f;
            newmatrix.M34 = 0f;
            newmatrix.M44 = 1f;
            return newmatrix;
        }

        private static HmdMatrix34_t OpenTKMatrixToOpenVRMatrix(Matrix4 matrix)
        {
            var newmatrix = new HmdMatrix34_t();
            newmatrix.m0 = matrix.M11;
            newmatrix.m1 = matrix.M21;
            newmatrix.m2 = matrix.M31;
            newmatrix.m3 = matrix.M41;
            newmatrix.m4 = matrix.M12;
            newmatrix.m5 = matrix.M22;
            newmatrix.m6 = matrix.M32;
            newmatrix.m7 = matrix.M42;
            newmatrix.m8 = matrix.M13;
            newmatrix.m9 = matrix.M23;
            newmatrix.m10 = matrix.M33;
            newmatrix.m11 = matrix.M43;
            return newmatrix;
        }
        public OpenVRInterface()
        {
            if (CoreSettings.CoreSettings.Default.UseTracking)
            {
                OpenVR.Init(ref EVRerror, EVRApplicationType.VRApplication_Overlay);
                if (EVRerror != EVRInitError.None)
                {
                    //   throw new Exception("An error occured while initializing OpenVR!");
                }
                OpenVR.GetGenericInterface(OpenVR.IVRCompositor_Version, ref EVRerror);
                if (EVRerror != EVRInitError.None)
                {
                    // throw new Exception("An error occured while initializing Compositor!");
                }
                OpenVR.GetGenericInterface(OpenVR.IVROverlay_Version, ref EVRerror);
                if (EVRerror != EVRInitError.None)
                {
                    // throw new Exception("An error occured while initializing Overlay!");
                }
                SetupDevices();
            }
            else
            {
                EVRerror = EVRInitError.Unknown;
            }
        }

        public static string GetSNForIndex(uint i)
        {
            return deviceSNs[i];
        }

        public static bool GetIndexForSN(string SN, out uint index)
        {
            index = 0;
            bool result = false;
            for (uint i = 0; i < deviceSNs.Length; i++)
            {
                if (deviceSNs[i] == SN)
                {
                    result = true;
                    index = i;
                }
            }
            return result;
        }

        public static string GetStringProperty(uint deviceId, ETrackedDeviceProperty prop)
        {
            var error = ETrackedPropertyError.TrackedProp_Success;
            var bufferSize = OpenVR.System.GetStringTrackedDeviceProperty(deviceId, prop, null, 0, ref error);
            if (bufferSize > 1)
            {
                var result = new System.Text.StringBuilder((int)bufferSize);
                OpenVR.System.GetStringTrackedDeviceProperty(deviceId, prop, result, bufferSize, ref error);
                return result.ToString();
            }
            return (error != ETrackedPropertyError.TrackedProp_Success) ? error.ToString() : "<unknown>";
        }

        public static void SetupDevices()
        {
            var vrMatrix = new HmdMatrix34_t();
            TrackedDevicePose_t[] pTrackedDevicePoseArray = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
            for (uint i = 0; i < indexOfDevice.Length; i++) indexOfDevice[i] = totalDevices + 1;
            for (uint j = 0; j < maxPositionsBuffer; j++)
            {
                for (uint i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++)
                {
                    allPositions[j, i] = Matrix4.Identity;
                }
            }
            for (uint i = 0; i < trackedPositions.Length;  i++)
            {
                trackedPositions[i] = Matrix4.Identity;
                activePositions[i] = false;
                lensToGround[i] = Matrix4.Identity;
                trackerToCamera[i] = Matrix4.Identity;
                if ((EVRerror == EVRInitError.None)&& (i < OpenVR.k_unMaxTrackedDeviceCount)) //setup real devices
                    {
                    OpenVR.System.GetDeviceToAbsoluteTrackingPose(eOrg, fPredictedSecondsToPhotonsFromNow, pTrackedDevicePoseArray);
                    if (pTrackedDevicePoseArray[i].bPoseIsValid)
                    {
                        vrMatrix = pTrackedDevicePoseArray[i].mDeviceToAbsoluteTracking;
                        trackedPositions[i] = OpenVRMatrixToOpenTKMatrix(vrMatrix);
                        for (uint j = 0; j < maxPositionsBuffer; j++)
                        {
                                allPositions[j, i] = trackedPositions[i];//pre-fill
                        }
                        activePositions[i] = true;
                    }
                    deviceSNs[i] = GetStringProperty(i, (ETrackedDeviceProperty)1002);
                    deviceClasses[i] = OpenVR.System.GetTrackedDeviceClass(i);
                    if (deviceClasses[i] == ETrackedDeviceClass.Controller)
                    {
                        /* if (displayOrder[1] > OpenVR.k_unMaxTrackedDeviceCount)
                         {
                             displayOrder[1] = i;
                         }
                         else
                         {
                             if (displayOrder[2] > OpenVR.k_unMaxTrackedDeviceCount)
                             {
                                 displayOrder[2] = i;
                                 if (string.Compare(deviceSNs[1], deviceSNs[2]) > 0)
                                     {
                                     displayOrder[2] = displayOrder[1];
                                     displayOrder[1] = i;
                                 }
                             }
                         }*/
                        //keep SN: broken controller is for Z5, good is for NX100
                        // "LHR-FFD71D42" je OK,  "LHR-FFEBDB46" je s nefunkčním R talířem
                        if (deviceSNs[i] == "LHR-FFD71D42")
                        {
                            deviceName[i] = "NX";
                            indexOfDevice[1] = i;
                        }
                        else
                        {
                            deviceName[i] = "Z5";
                            deviceAdditionalDelay[i] = 1;
                            indexOfDevice[2] = i;
                        }

                        lensToGround[i] = Matrix4.CreateTranslation(0, -lensAboveGround , 0);
                        trackerToCamera[i] = Matrix4.CreateTranslation(-trackerAsideOfLens, -trackerAboveLens, trackerBeforeLens); //first we want it NOT to be zero
                        trackerToCamera[i] = Matrix4.CreateRotationX(trackerOffXAxis) * trackerToCamera[i]; //camera moved first from controller, then rotated
                                                                                                            //trackerToCamera[i] = trackerToCamera[i]* Matrix4.CreateRotationZ(0.5f); //camera turned first, then moved
                        if (indexOfDevice[1] == i)  trackerToCamera[i] = Matrix4.CreateRotationY(0.02f) * trackerToCamera[i] ; //NX need slight shift
                        viewOffsetShift = lensToGround[i] * trackerToCamera[i]; //this value should be saved with viewOffset
                        // LoadTrackerToCamera(i);we use just preset values here

                    }
                    if (deviceClasses[i] == ETrackedDeviceClass.HMD)
                    {
                        deviceName[i] = "EXT";
                        if (indexOfDevice[0] > totalDevices) indexOfDevice[0] = i;
                        lensToGround[i] = Matrix4.CreateTranslation(0, 0, 0);
                        trackerToCamera[i] = Matrix4.CreateTranslation(-trackerAsideOfLens, -trackerAboveLens, trackerBeforeLens); 
                        trackerToCamera[i] = Matrix4.CreateRotationX(trackerOffXAxis) * trackerToCamera[i]; //camera moved first from HMD, then rotated
                        LoadTrackerToCamera(i);//overwrites preset
                    }
                }
            }
            //    if (displayOrder[0] > OpenVR.k_unMaxTrackedDeviceCount) displayOrder[0] = 0; //need to find better way how to handle off - controllers
            //    if (displayOrder[1] > OpenVR.k_unMaxTrackedDeviceCount) displayOrder[1] = 0;
            //    if (displayOrder[2] > OpenVR.k_unMaxTrackedDeviceCount) displayOrder[2] = 0;

            //setup 1 virtual camera here
            uint index = OpenVR.k_unMaxTrackedDeviceCount + 0;
            deviceClasses[index] = ETrackedDeviceClass.DisplayRedirect;//kinda hack
            deviceSNs[index] = "VTCAM0";
            deviceName[index] = "Virt";
            indexOfDevice[3] = index;

            Matrix4 savedSettingsTest;
            bool valid = StringToMatrix4(CoreSettings.CoreSettings.Default.ViewOffset, out savedSettingsTest, out string dummyString);
            if (valid) viewOffset = savedSettingsTest;
            valid = StringToMatrix4(CoreSettings.CoreSettings.Default.ViewOffsetShift, out savedSettingsTest, out dummyString);
            if (valid) viewOffsetShift = savedSettingsTest;
            valid = StringToMatrix4(CoreSettings.CoreSettings.Default.HMDRefPos, out savedSettingsTest, out dummyString);
            if (valid) hmdRefPos = savedSettingsTest;

        }

        public static Matrix4 GetViewFromPosition(Matrix4 position)
        {
            return Matrix4.Invert(position);
        }

        public static void SaveHMDReference()
        {
            uint contIndex = indexOfDevice[0];
            if (contIndex >= trackedPositions.Length) return;
            hmdRefPos = viewOffset * Matrix4.Invert(viewOffsetShift * trackedPositions[contIndex]) ;

            string saveStr = "HMDRefPos" + MainWindow.recentDataSeparator[0] + Matrix4ToString(hmdRefPos);
            CoreSettings.CoreSettings.Default.HMDRefPos = saveStr;
            CoreSettings.CoreSettings.Default.Save();
        }

        public static void ApplyHMDReference()
        {
            uint contIndex = indexOfDevice[0];
            if (contIndex >= trackedPositions.Length) return;
            //   hmdRefPos = Matrix4.CreateTranslation(-0.5,-0.5,-0.5)// uhne doleva, dolů, dozadu
            viewOffset = hmdRefPos * viewOffsetShift * trackedPositions[contIndex];
            string saveStr = "ViewOffset" + MainWindow.recentDataSeparator[0] + Matrix4ToString(viewOffset);
            CoreSettings.CoreSettings.Default.ViewOffset = saveStr;
            CoreSettings.CoreSettings.Default.Save();
        }

        public static void SetCam0Offset(Matrix4 cam0Offset)
        {
            if (indexOfDevice[0] >= trackerToCamera.Length) return;
            trackerToCamera[indexOfDevice[0]] = cam0Offset;
            SaveTrackerToCamera(indexOfDevice[0]);
        }

        public static void GrabCamToVirt()
        {
            if (indexOfDevice[1] >= trackedPositions.Length) return;
           // if (indexOfDevice[3] >= trackedPositions.Length) return; //this shoulf be always OK, camera exists
            trackedPositions[indexOfDevice[3]] = trackedPositions[indexOfDevice[1]];
        }

        public static void ScanPositions(long frameDelay)// frame delay positive values subtract from seconds to photons
        {
            var vrMatrix = new HmdMatrix34_t();
            TrackedDevicePose_t[] pTrackedDevicePoseArray = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
            for (int j = maxPositionsBuffer-1; j > 0; j--)
            {
                for (int i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++)
                {
                    allPositions[j, i] = allPositions[j-1, i];
                }
            }
            for (uint i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++)
            {
                if (activePositions[i] == true)
                {
                    OpenVR.System.GetDeviceToAbsoluteTrackingPose(eOrg, fPredictedSecondsToPhotonsFromNow - (float)frameDelay/1000, pTrackedDevicePoseArray);
                    if (pTrackedDevicePoseArray[i].bPoseIsValid)
                    {
                        vrMatrix = pTrackedDevicePoseArray[i].mDeviceToAbsoluteTracking;
                        allPositions[0,i] = OpenVRMatrixToOpenTKMatrix(vrMatrix);
                    }
                }
            }
            for (uint i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++)//no virtual positions
            {
                trackedPositions[i] = allPositions[maxPositionsBuffer-1, i];
            }
        }

        public static void ScanAllPositions() //including non-active
        {
            var vrMatrix = new HmdMatrix34_t();
            TrackedDevicePose_t[] pTrackedDevicePoseArray = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
            for (uint i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++) //only real positions
            {
                OpenVR.System.GetDeviceToAbsoluteTrackingPose(eOrg, fPredictedSecondsToPhotonsFromNow, pTrackedDevicePoseArray);
                if (pTrackedDevicePoseArray[i].bPoseIsValid)
                {
                    vrMatrix = pTrackedDevicePoseArray[i].mDeviceToAbsoluteTracking;
                    trackedPositions[i] = OpenVRMatrixToOpenTKMatrix(vrMatrix);
                    for (uint j = 0; j < maxPositionsBuffer; j++)
                    {
                        allPositions[j, i] = trackedPositions[i];
                    }
                    activePositions[i] = true;
                }
                else
                {
                    activePositions[i] = false;
                }
            }
        }
        public const float Tau = 2.0f * (float)Math.PI;
        public static Vector3 FromRotMatToEulerZYXInt(Matrix4 mat)
        {
            //x''', y''', z''' are stored in rows of mat
            Vector3 angles = new Vector3(0, 0, 0);

            angles.Y = (float)-Math.Asin(mat.Row0.Z);
            if (Math.Abs(angles.Y) * 0x10000 / Tau > (float)0x4000 - 0.5)
            {
                angles.Z = 0;
                angles.X = (float)Math.Atan2(-mat.Row2.Y, mat.Row1.Y);
            }
            else
            {
                angles.Z = (float)Math.Atan2(mat.Row0.Y, mat.Row0.X);
                angles.X = (float)Math.Atan2(mat.Row1.Z, mat.Row2.Z);
            }
            return angles;
        }
        public static void ProcessAllButtons(Renderer renderer)
        {
            if (EVRerror != EVRInitError.None) return;
            for (uint i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++)//no virtual buttons
            {
                if (deviceClasses[i] == ETrackedDeviceClass.Controller)
                {
                    ProcessButtons(i, renderer);//Todo: distinguish between controllers somehow?
                }
            }
        }

        public static void ProcessButtons(uint contIndex, Renderer renderer)
        {
            // 4294967296 pad
            // 8589934592 shoot (even just partially)
            // 2 upper button - EVRButtonId.k_EButton_Gri
            // 4 side button - EVRButtonId.k_EButton_DPad_Up
            // status button nothing - switches to room mode, ALL IS OFF then!! Todo: check
            VRControllerState_t controllerState = new VRControllerState_t();
            //uint stateSize = sizeof(VRControllerState_t);
            uint stateSize = 64;
            OpenVR.System.GetControllerState(contIndex, ref controllerState, stateSize);
            ulong buttons = controllerState.ulButtonPressed;
            if (buttons == 4294967296)
            {

                {
                    var cam = renderer.cameraController(contIndex);
                    if (cam != null)
                    {
                        float _digitalZoom = cam.GetDigitalZoom();
                        float _digitalZoomCenter = cam.GetDigitalZoomCenter();
                        float step = 1.01f;
                        float centerTouch = 0.2f;
                        float centerNoTouch = 0.6f;
                        float stepCenter = 0.03f;
                        if ((controllerState.rAxis0.x > -centerNoTouch) && (controllerState.rAxis0.x < centerNoTouch))
                        {
                            if (controllerState.rAxis0.y < -centerTouch) _digitalZoom = _digitalZoom / step;
                            if (controllerState.rAxis0.y > centerTouch) _digitalZoom = _digitalZoom * step;
                        }
                        if ((controllerState.rAxis0.y > -centerNoTouch) && (controllerState.rAxis0.y < centerNoTouch))
                        {
                            if (controllerState.rAxis0.x < -centerTouch) _digitalZoomCenter = _digitalZoomCenter - stepCenter;
                            if (controllerState.rAxis0.x > centerTouch) _digitalZoomCenter = _digitalZoomCenter + stepCenter;
                        }
                        MainWindow.CheckBoundsFloat(ref _digitalZoom, MainWindow.digitalZoomLimitLower, MainWindow.digitalZoomLimitUpper);
                        MainWindow.CheckBoundsFloat(ref _digitalZoomCenter, 0f, 1f);
                        lock (renderer.renderParameterLock)
                        {
                            cam.SetDigitalZoom(_digitalZoom);
                            cam.SetDigitalZoomCenter(_digitalZoomCenter);
                        }
                    }
                }
            }

            if ((buttons & (ulong)EVRButtonId.k_EButton_Grip) == (ulong)EVRButtonId.k_EButton_Grip)
            {
                // reset offset to camera, keeps offset cameraToground 
                if ((buttons & (ulong)EVRButtonId.k_EButton_DPad_Up) == (ulong)EVRButtonId.k_EButton_DPad_Up)
                {
                    viewOffsetShift = lensToGround[contIndex] * trackerToCamera[contIndex];
                    viewOffset = viewOffsetShift * trackedPositions[contIndex];
                   // viewOffset = Matrix4.Invert(GetViewFromPosition(viewOffsetPosition));
                    string saveStr = "ViewOffset" + MainWindow.recentDataSeparator[0] + Matrix4ToString(viewOffset);
                    CoreSettings.CoreSettings.Default.ViewOffset = saveStr;
                    saveStr = "ViewOffsetShift" + MainWindow.recentDataSeparator[0] + Matrix4ToString(viewOffsetShift); //need to save because may be different for different controllers
                    CoreSettings.CoreSettings.Default.ViewOffsetShift = saveStr;
                    CoreSettings.CoreSettings.Default.Save();
                                    }
            }
        }

        //calculating trackerToCamera offset - now unused, we use preset offsets 
        /*
        if (condition) //  start measure trackerToCamera  - offset to controller, camera points 0,0,1 forward
        {
            trackerToCamera[contIndex] = Matrix4.Identity;
            viewOffset = Matrix4.Invert(GetViewFromPosition(trackedPositions[contIndex]));
        }
        //later...

          if (trackerToCamera[contIndex] == Matrix4.Identity)// touch pad alone = end measuring trackerToCamera matrix, camera points 0,0,-1 backwards
          {
              //Mtx.Trans * Mtx-Rot = otočím a posunu, Mtx.Rot * Mtx:Trans = osunu a natočím
              //CreateRotation +z naklání kameru doleva a +x ji zvedá nahoru
              Matrix4 difference = viewOffset * GetViewFromPosition(trackerToCamera[contIndex] * trackedPositions[contIndex]);
              Matrix4 diffTrans = Matrix4.CreateTranslation(-difference.M41, -difference.M42, -difference.M43);
              //    if (difference.M42 / 2 > 0.02) ;//this is problem? , was not turned about Y axis??
              Matrix4 diffAngl = difference * diffTrans;//eliminating translation
              Matrix4 turnedMatrix = Matrix4.CreateRotationX(0f) * Matrix4.CreateRotationZ(0f) * Matrix4.CreateRotationY(MathHelper.Pi);
              trackerToCamera[contIndex] = turnedMatrix * diffAngl;
              //now we have in trackerToCamera[contIndex] the correction (camera looks to Z+), but doubled. Need to half it so it can be added to tracker position.
              Vector3 rXYZ = FromRotMatToEulerZYXInt(trackerToCamera[contIndex]);
              trackerToCamera[contIndex] = Matrix4.CreateRotationX(rXYZ.X / 2) * Matrix4.CreateRotationY(rXYZ.Y / 2) * Matrix4.CreateRotationZ(rXYZ.Z / 2);

              trackerToCamera[contIndex] = trackerToCamera[contIndex] * Matrix4.CreateTranslation(difference.M41 / 2, difference.M42 / 2 - trackerAboveLens, difference.M43 / 2);
              viewOffset = trackerToCamera[contIndex] * viewOffset;

              //save trackerToCamera for this index
              string saveStr = deviceSNs[contIndex] + MainWindow.recentDataSeparator[0] + Matrix4ToString(trackerToCamera[contIndex]);
              var saved = CoreSettings.CoreSettings.Default.TrackerToCamera;
              if (saved == null)
              {
                  saved = CoreSettings.CoreSettings.Default.TrackerToCamera = new StringCollection();
                  CoreSettings.CoreSettings.Default.Save();
              }
              string[] oldData;
              int i = 0;
              if (saved != null)
              {
                  foreach (var s in saved)
                  {
                      oldData = s.Split(MainWindow.recentDataSeparator, StringSplitOptions.None);
                      if (oldData[0] == deviceSNs[contIndex])
                      {
                          saved.Remove(s);
                          break;
                      }
                      ++i;
                  }
                  saved.Insert(0, saveStr);
              }

              CoreSettings.CoreSettings.Default.Save();
          }
      }
      else*/

        public static void SaveTrackerToCamera(uint contIndex)
        {
            //save trackerToCamera for this index
            string saveStr = deviceSNs[contIndex] + MainWindow.recentDataSeparator[0] + Matrix4ToString(trackerToCamera[contIndex]);
            var saved = CoreSettings.CoreSettings.Default.TrackerToCamera;
            if (saved == null)
            {
                saved = CoreSettings.CoreSettings.Default.TrackerToCamera = new StringCollection();
                CoreSettings.CoreSettings.Default.Save();
            }
            string[] oldData;
            int i = 0;
            if (saved != null)
            {
                foreach (var s in saved)
                {
                    oldData = s.Split(MainWindow.recentDataSeparator, StringSplitOptions.None);
                    if (oldData[0] == deviceSNs[contIndex])
                    {
                        saved.Remove(s);
                        break;
                    }
                    ++i;
                }
                saved.Insert(0, saveStr);
            }
            CoreSettings.CoreSettings.Default.Save();
        }

        public static void LoadTrackerToCamera(uint contIndex)
        {
            var saved = CoreSettings.CoreSettings.Default.TrackerToCamera;
            string[] savedData;
            string SN = OpenVRInterface.deviceSNs[contIndex];
            Matrix4 outMatrix;
            if (saved != null)
            {
                foreach (var s in saved)
                {
                    savedData = s.Split(MainWindow.recentDataSeparator, StringSplitOptions.None);
                    if (savedData[0] == SN)
                    {
                        bool valid = OpenVRInterface.StringToMatrix4(s, out outMatrix, out SN);
                        if (valid)
                        {
                            OpenVRInterface.trackerToCamera[contIndex] = outMatrix;
                            break;
                        }
                    }
                }
            }
        }
        public static string vector4ToString(Vector4 src)
        {
            string outStr = src.X.ToString(System.Globalization.CultureInfo.InvariantCulture);
            outStr = outStr + MainWindow.recentItemSeparator[0] + src.Y.ToString(System.Globalization.CultureInfo.InvariantCulture);
            outStr = outStr + MainWindow.recentItemSeparator[0] + src.Z.ToString(System.Globalization.CultureInfo.InvariantCulture);
            outStr = outStr + MainWindow.recentItemSeparator[0] + src.W.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return outStr;
        }

        public static string Matrix4ToString(Matrix4 src)
        {
            string outStr = vector4ToString(src.Row0);
            outStr = outStr + MainWindow.recentDataSeparator[0] + vector4ToString(src.Row1);
            outStr = outStr + MainWindow.recentDataSeparator[0] + vector4ToString(src.Row2);
            outStr = outStr + MainWindow.recentDataSeparator[0] + vector4ToString(src.Row3);
            return outStr;
        }

        public static bool StringToVector4(string src, out Vector4 outVector)
        {
            string[] itemStrings = src.Split(MainWindow.recentItemSeparator, StringSplitOptions.None);
            bool valid = true;
            outVector = Vector4.Zero;
            try
            {
                outVector.X = float.Parse(itemStrings[0], System.Globalization.CultureInfo.InvariantCulture);
                outVector.Y = float.Parse(itemStrings[1], System.Globalization.CultureInfo.InvariantCulture);
                outVector.Z = float.Parse(itemStrings[2], System.Globalization.CultureInfo.InvariantCulture);
                outVector.W = float.Parse(itemStrings[3], System.Globalization.CultureInfo.InvariantCulture);
            }
            catch
            {
                valid = false;
            }
            return valid;

        }
        public static bool StringToMatrix4(string src, out Matrix4 outMatrix, out string firstString)
        {
            string[] vectorStrings = src.Split(MainWindow.recentDataSeparator, StringSplitOptions.None);//first string is left for index (or SN)
            bool valid = true;
            firstString = vectorStrings[0];
            if (String.IsNullOrEmpty(firstString)) valid = false;
            outMatrix = Matrix4.Identity;
            try
            {
                valid = valid && StringToVector4(vectorStrings[1], out outMatrix.Row0);
                valid = valid && StringToVector4(vectorStrings[2], out outMatrix.Row1);
                valid = valid && StringToVector4(vectorStrings[3], out outMatrix.Row2);
                valid = valid && StringToVector4(vectorStrings[4], out outMatrix.Row3);
            }
            catch
            {
                valid = false;
            }
            if (!valid) outMatrix = Matrix4.Identity;
            return valid;
        }

    }
}
