using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using Valve.VR;

namespace open3mod
{
    public class VRModelCameraController : ICameraController //only holds view for scene, does not apply offset - always in its place
    {
        private Matrix4 _view;
        private Matrix4 _trackerPosition;
        private Matrix4 _viewPosition;
        private float _fovy = MathHelper.PiOver4;
        private float _zNear = 0.01f;
        private float _zFar = 100.0f;
        private uint contIndex = 0; //controller OpenVR index
        private CameraMode _cameraMode;
        private ScenePartMode _scenePartMode = ScenePartMode.All;
    //    private Matrix4 invertGlobalScale = Matrix4.CreateScale(1/MainWindow.globalScale); // scene.Scale

        private void UpdateViewMatrix()
        {
            //Dummy controller, does nothing
        }

        public VRModelCameraController(CameraMode camMode, float fovy, ScenePartMode scenePartMode)
        {
            _fovy = fovy;
            _cameraMode = camMode;
            _scenePartMode = scenePartMode;
        }

        public void SetPivot(Vector3 pivot)
        {
        }

        public void SetViewNoOffset(Matrix4 view)
        {
            _view = view; 
        }

        public void SetView(Matrix4 view)
        {
            _view = view;
        }

        public Matrix4 GetView()
        {
            {
                return /*invertGlobalScale **/ _view;//we must exclude offset for models + does not resize with scene
            }
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
            _cameraMode = mode;
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

