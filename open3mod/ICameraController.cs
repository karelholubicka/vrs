///////////////////////////////////////////////////////////////////////////////////
// Open 3D Model Viewer (open3mod) (v2.0)
// [ICameraController.cs]
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

using OpenTK;

namespace open3mod
{
    /// <summary>
    /// Predefined camera modes. The map from ICameraController implementations
    /// to camera modes is not one-by-one because the X,Y and Z modes are 
    /// states of the more general Orbit mode and +HMD,Cont1,Cont2 are special tracking modes. 
    /// </summary>
    public enum CameraMode
    {
        // these indices must match the UI order
        X = 0,
        Y = 1,
        Z = 2,
        Orbit = 3,
        Fps = 4,
        HMD = 5,
        Cont1 = 6,
        Cont2 = 7,
        Virtual = 8,
        _Max = 9,
        
    }

    /// <summary>
    /// Predefined camera modes. The map from ICameraController implementations
    /// to camera modes is not one-by-one because the X,Y and Z modes are 
    /// states of the more general Orbit mode and HMD,Cont1,Cont2 are special tracking modes. 
    /// </summary>
    public enum ScenePartMode
    {
        //these modes require special handling in renderer
        Background = 0,
        GreenScreen = 1, //No camera overlay
        Foreground = 2,
        Others = 3,
        All = 4, //No camera overlay
        //these modes require special handling in renderer
        Output = 5,// Regular output = Bkgd + Canvas textured with Camera + Foreground
        Camera = 6,
        CameraCancelColor = 7,
        Keying = 8,
    }

    /// <summary>
    /// Base camera abstraction. 
    /// 
    /// A camera controller is assumed to be statefull, i.e. it maintains
    /// the current camera position and adjusts it to input.
    /// </summary>
    public interface ICameraController
    {
        /// <summary>
        /// Set the pivot point, which is supposed to be the rotation origin.
        /// The implementation may ignore the pivot point if such concept
        /// does not make sense for the kind of camera it represents.
        /// </summary>
        /// <param name="pivot"></param>
        void SetPivot(Vector3 pivot);

        /// <summary>
        /// If possible, loads the current view transformation matrix for the camera, at least to first update
        /// </summary>
        /// <returns>View matrix (rotation+translation, no scaling)</returns>
        void SetViewNoOffset(Matrix4 view);

        /// <summary>
        /// Obtains the current view transformation matrix for the camera
        /// </summary>
        /// <returns>View matrix (rotation+translation, no scaling)</returns>
        Matrix4 GetView();

        /// <summary>
        /// Obtains the current view transformation matrix for the camera without any update
        /// </summary>
        /// <returns>View matrix (rotation+translation, no scaling)</returns>
        Matrix4 GetViewNoOffset();

        /// <summary>
        /// Sets controller view parameters
        /// </summary>
        void SetParam(float fovy, ScenePartMode scenePartMode, CameraMode mode);

        /// <summary>
        /// Sets controller view parameters
        /// </summary>
        void SetAllParam(float fovy, float digitalZoom, float digitalZoomCenter, ScenePartMode scenePartMode, CameraMode mode);

        /// <summary>
        /// Obtains current field of view
        /// </summary>
        /// <returns>field of view for Y in radians</returns>
        float GetFOV();

        /// <summary>
        /// Obtains current digital zoom value
        /// </summary>
        /// <returns>zoom value (usually usable from 1/2 to 2)</returns>
        float GetDigitalZoom();

        /// <summary>
        /// Obtains current digital zoom center
        /// </summary>
        /// <returns>aligment of digital zoom - 0 towards left, 1 towards right</returns>
        float GetDigitalZoomCenter();

        /// <summary>
        /// Gets the mode of displaying
        /// </summary>
        /// <returns></returns>
        ScenePartMode GetScenePartMode();

        /// <summary>
        /// Gets the mode of the camera. The camera mode is allowed to change
        /// during calls to MovementKey() or MouseMove() or Scroll() (this allows
        /// one implementation class to handle multiple, related camera modes
        /// that are still kept separate in the UI).
        /// </summary>
        /// <returns></returns>
        CameraMode GetCameraMode();

        /// <summary>
        /// Obtains current field of view
        /// </summary>
        /// <returns>field of view for Y in radians</returns>
        void SetFOV(float value);

        /// <summary>
        /// Obtains current digital zoom value
        /// </summary>
        /// <returns>zoom value (usually usable from 1/2 to 2)</returns>
        void SetDigitalZoom(float value);

        /// <summary>
        /// Obtains current digital zoom center
        /// </summary>
        /// <returns>aligment of digital zoom - 0 towards left, 1 towards right</returns>
        void SetDigitalZoomCenter(float value);

        /// <summary>
        /// Sets the mode of displaying
        /// </summary>
        /// <returns></returns>
        void SetScenePartMode(ScenePartMode value);
     
        /// <summary>
        /// Processes mouse movement events
        /// </summary>
        /// <param name="x">X delta</param>
        /// <param name="y">Y delta</param>
        void MouseMove(int x, int y);

        /// <summary>
        /// Processes scroll events
        /// </summary>
        /// <param name="z">Signed scroll delta (knocks * DELTA_.. constants
        ///   from WinFors)</param>
        void Scroll(float z);

        /// <summary>
        /// Processes pan events (i.e. mousewheel pressed)
        /// </summary>
        /// <param name="x">X delta</param>
        /// <param name="y">Y delta</param>
        void Pan(float x, float y);

        /// <summary>
        /// Processes movement keys
        /// </summary>
        /// <param name="x">Signed X axis movement, normalized by time</param>
        /// <param name="y">Signed Y axis movement, normalized by time</param>
        /// <param name="z">Signed Z axis movement, normalized by time</param>
        void MovementKey(float x, float y, float z);

        /// <summary>
        /// Process the 3D Input of the LeapMotion device
        /// </summary>
        /// <param name="x">X delta</param>
        /// <param name="y">Y delta</param>
        /// <param name="z">Z delta</param>
        /// <param name="pitch">absolute rotation around the X axis</param>
        /// <param name="roll">absolute rotation around the Z axis</param>
        /// <param name="yaw">absolute rotation around the Y axis</param>
        void LeapInput(float x, float y, float z, float pitch, float roll, float yaw);
    }
}

/* vi: set shiftwidth=4 tabstop=4: */ 