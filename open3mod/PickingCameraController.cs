///////////////////////////////////////////////////////////////////////////////////
// Open 3D Model Viewer (open3mod) (v2.0)
// [PickingCameraController.cs]
// (c) 2012-2015, Open3Mod Contributors
//
// Licensed under the terms and conditions of the 3-clause BSD license. See
// the LICENSE file in the root folder of the repository for the details.
//
// HIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE 
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
// ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; 
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND 
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT 
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS 
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
///////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.Diagnostics;


using OpenTK;
using Valve.VR;

namespace open3mod
{
    public class PickingCameraController : ICameraController
    {
        private Matrix4 _view;
        private Matrix4 _trackerPosition;
        private Matrix4 _viewPosition;
        private float _fovy = MathHelper.PiOver4;
        private float _zNear = 0.01f;
        private float _zFar = 100.0f;
        private uint contIndex = 0; //controller OpenVR index
        private static readonly Vector3 TrackingOffPosition = new Vector3(-0.0f, -0.7f, -1.8f);
        private CameraMode _cameraMode;
        private ScenePartMode _scenePartMode = ScenePartMode.All;

        // kladné x kamera vlevo, kladné y DOLU, kladné z kamera vpøed; //+ kamera èumák nahoru/- èumák dolù, radiány 
        //        var hmd = OpenVR.System;
        //        var compositor = OpenVR.Compositor;


        private void UpdateViewMatrix()
        {
            if (OpenVRInterface.EVRerror == EVRInitError.None)
            {
                _trackerPosition = OpenVRInterface.trackedPositions[contIndex];
                _viewPosition = OpenVRInterface.trackerToCamera[contIndex] * _trackerPosition;
                _view = OpenVRInterface.GetViewFromPosition(_viewPosition);
            }
            else
            {
                _view = Matrix4.CreateTranslation(TrackingOffPosition); //backup for offline condition
            }

        }

        public PickingCameraController(CameraMode camMode, float fovy, ScenePartMode scenePartMode)
        {
            _fovy = fovy;
            _scenePartMode = scenePartMode;
            SetVRCameraMode(camMode);

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
            UpdateViewMatrix();
        }


        public void SetPivot(Vector3 pivot)
        {
        }

        public void SetViewNoOffset(Matrix4 view)
        {
        }

        public Matrix4 GetView()
        {
            UpdateViewMatrix();
            return OpenVRInterface.viewOffset * _view;
        }

        public Matrix4 GetViewNoOffset()
        {
            return _view;
        }

        public float GetFOV()
        {
            return _fovy;
        }
        
        public void SetParam(float fovy, ScenePartMode scenePartMode, CameraMode mode)
        {
            _scenePartMode = scenePartMode;
            SetVRCameraMode(mode);
            _fovy = fovy;
        }

        public void Pan(float x, float y)
        {
            UpdateViewMatrix();
        }

        public void MovementKey(float x, float y, float z)
        {
            UpdateViewMatrix();
        }

        public CameraMode GetCameraMode()
        {
            return _cameraMode;
        }

        public ScenePartMode GetScenePartMode()
        {
            return _scenePartMode;
        }

        public void SetScenePartMode(ScenePartMode value)
        {
            _scenePartMode = value;
        }

        public void SetVRCameraMode(CameraMode mode)
        {
            Debug.Assert((mode == CameraMode.HMD) || (mode == CameraMode.Cont1) || (mode == CameraMode.Cont2));
            _cameraMode = mode;
            switch (_cameraMode)
            {
                case CameraMode.HMD:
                    contIndex = OpenVRInterface.displayOrder[0];
                    break;
                case CameraMode.Cont1:
                    contIndex = OpenVRInterface.displayOrder[1];
                    break;
                case CameraMode.Cont2:
                    contIndex = OpenVRInterface.displayOrder[2];
                    break;
            }
            if ((contIndex == 0) || (contIndex >= OpenVR.k_unMaxTrackedDeviceCount))
            {
                //    throw new Exception("No controller is ON!");
                contIndex = 0;
            }
         }

        public void MouseMove(int x, int y)
        {
            UpdateViewMatrix();
        }


        public void Scroll(float z)
        {
            UpdateViewMatrix();
        }

        public void LeapInput(float x, float y, float z, float pitch, float roll, float yaw)
        {
            UpdateViewMatrix();
        }
    }
}

/* vi: set shiftwidth=4 tabstop=4: */ 
/*
 * https://www.csharpcodi.com/vs2/4987/SM64DSe/Helper.cs/
 * */