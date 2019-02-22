///////////////////////////////////////////////////////////////////////////////////
// Open 3D Model Viewer (open3mod) (v2.0)
// [FpsCameraController.cs]
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
using OpenTK;
using System.Diagnostics;


namespace open3mod
{
    public class FpsCameraController : ICameraController
    {
        private Matrix4 _view;
        private float _fovy = MainWindow.fovPreset;
        private float _digitalZoom = 1f;
        private float _digitalZoomCenter = 0.5f;
        private Matrix4 _orientation;
        private Vector3 _translation;
        private ScenePartMode _scenePartMode = ScenePartMode.All;

        //    private static readonly Vector3 StartPosition = new Vector3(-2.2f, 1.8f, -0.1f);    //good start at home
        //     private static readonly Vector3 StartPosition = new Vector3(1.5f, 1.9f, -0.2f); //good start at the studio
        private static readonly Vector3 StartPosition = new Vector3(0f, 0.7f, 1.8f);    //good start at home
        private bool _dirty = true;
        private bool _updateOrientation = true;
        private const float MovementBaseSpeed = 1.0f;
        private const float BaseZoomSpeed = 0.002f;
        private const float RotationSpeed = 0.5f;

        private float _pitchAngle = 0.0f;
        private float _rollAngle = 0.0f;
//        private float _yawAngle = -1.57f;
        private float _yawAngle = 0f;


        public FpsCameraController(float fovy, ScenePartMode scenePartMode)
        {
            _view = Matrix4.Identity;
            _translation = StartPosition;
            _fovy = fovy;
            _scenePartMode = scenePartMode;
            UpdateViewMatrix();
        }

        public void SetPivot(Vector3 pivot)
        { }

        public void SetViewNoOffset(Matrix4 view)
        {
            _view = view;
        }

        public Matrix4 GetView()
        {
            if (_dirty)
            {
                UpdateViewMatrix();
            }
            return OpenVRInterface.viewOffset * _view;
        }

        public Matrix4 GetViewNoOffset()
        {
            return _view;
        }

        public void SetParam(float fovy, ScenePartMode scenePartMode, CameraMode mode)
        {
            _scenePartMode = scenePartMode;
            Debug.Assert(mode == CameraMode.Fps);
            _fovy = fovy;
        }

        public void SetAllParam(float fovy, float digitalZoom, float digitalZoomCenter, ScenePartMode scenePartMode, CameraMode mode)
        {
            _scenePartMode = scenePartMode;
            //_cameraMode = mode; has no effect
            _fovy = fovy;
            _digitalZoom = digitalZoom;
            _digitalZoomCenter = digitalZoomCenter;
        }

        public float GetFOV()
        {
            return _fovy;
        }

        public float GetDigitalZoom()
        {
            return _digitalZoom;
        }

        public float GetDigitalZoomCenter()
        {
            return _digitalZoomCenter;
        }

        public CameraMode GetCameraMode()
        {
            return CameraMode.Fps;
        }

        public ScenePartMode GetScenePartMode()
        {
            return _scenePartMode;
        }

        public void SetFOV(float value)
        {
            _fovy = value;
        }

        public void SetDigitalZoom(float value)
        {
            _digitalZoom = value;
        }

        public void SetDigitalZoomCenter(float value)
        {
            _digitalZoomCenter = value;
        }

        public void SetScenePartMode(ScenePartMode value)
        {
            _scenePartMode = value;
        }

        public void Pan(float x, float y)
        {
        }

        public void MovementKey(float x, float y, float z)
        {
            var v = new Vector3(x, y, z) * MovementBaseSpeed;
            var o = GetOrientation();

            // TODO: somehow, the matrix is transposed so the normal TransformVector() API does not work.
            // It seems we messed up with OpenTK's matrix conventions.
            _translation += v.X * o.Row0.Xyz + v.Y * o.Row1.Xyz + v.Z * o.Row2.Xyz;
            _dirty = true;
        }

        public void MouseMove(int x, int y)
        {
            if (y != 0)
            {
                _pitchAngle += (float)(-y * RotationSpeed * Math.PI / 180.0);
            }

            if (x != 0)
            {
                _yawAngle += (float)(-x * RotationSpeed * Math.PI / 180.0);
            }

            _dirty = true;
            _updateOrientation = true;
        }


        public void Scroll(float z)
        {
            var o = GetOrientation();
            _translation -= o.Row2.Xyz *z*BaseZoomSpeed;
            _dirty = true;
        }


        private void UpdateViewMatrix()
        {
            // Derivation:
            //     view = (orientation*translation)^-1
            // =>  view = translation^-1 * orientation^-1
            // =>  view = translation^-1 * orientation^T      ; orientation is ONB
            // where translation^-1 is simply the negated translation vector.
            _view = GetOrientation();
            _view *= Matrix4.CreateFromAxisAngle(_view.Row0.Xyz, _pitchAngle);
            _view.Transpose();
            _view = Matrix4.CreateTranslation(-_translation) * _view;

            _dirty = false;
        }

        private Matrix4 GetOrientation()
        {
            if (_updateOrientation)
            {
                _updateOrientation = false;
                _orientation = Matrix4.CreateFromAxisAngle(Vector3.UnitY, _yawAngle);
                _orientation *= Matrix4.CreateFromAxisAngle(_orientation.Row0.Xyz, _pitchAngle);   
            }
            return _orientation;
        }

        public void LeapInput(float x, float y, float z, float pitch, float roll, float yaw)
        {
            Scroll(-z);
            //TODO parameters in Settings
            _pitchAngle += pitch * 0.05f;
            _yawAngle += -yaw * 0.05f;
            _updateOrientation = true;

            UpdateViewMatrix();
        }
    }
}

/* vi: set shiftwidth=4 tabstop=4: */ 