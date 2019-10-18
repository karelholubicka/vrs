///////////////////////////////////////////////////////////////////////////////////
// Open 3D Model Viewer (open3mod) (v2.0)
// [MainWindow_Input.cs]
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
using System.Diagnostics;
using System.Windows.Forms;
using OpenTK;

namespace open3mod
{
    // The portions of the MainWindow code that deal with handling input from keyboard
    // and mouse and dispatching it to camera controllers, implement viewport dragging etc.
    public partial class MainWindow
    {
        private bool _mouseWheelDown;

        private bool _forwardPressed;
        private bool _leftPressed;
        private bool _rightPressed;
        private bool _backPressed;
        private bool _upPressed;
        private bool _downPressed;
        private bool _fovyUpPressed;
        private bool _fovyDownPressed;
        private bool _digZoomUpPressed;
        private bool _digZoomDownPressed;
        private bool _digZoomCenterLeftPressed;
        private bool _digZoomCenterRightPressed;
        private bool _shiftPressed;
        private int digZoomSpeed = 0;
        private int digZoomSpeedCenter = 0;


        private int _previousMousePosX = -1;
        private int _previousMousePosY = -1;
        private bool _mouseDown;
        private bool _mouseRightDown;

        private void ProcessKeys() //locked already before called, renderer already exists
        {
            var cam = UiState.ActiveTab.ActiveCameraController;
            if (cam == null)
            {
                return;
            }

            var dt = (float)_fps.LastFrameDelta;
            float x = 0.0f, y = 0.0f, z = 0.0f;

            var changed = false;
            var changedz = false;

            if (_forwardPressed)
            {
                changed = true;
                z -= dt;
            }
            if (_backPressed)
            {
                changed = true;
                z += dt;
            }

            if (_rightPressed)
            {
                changed = true;
                x += dt;
            }
            if (_leftPressed)
            {
                changed = true;
                x -= dt;
            }

            if (_upPressed)
            {
                changed = true;
                y += dt;
            }
            if (_downPressed)
            {
                changed = true;
                y -= dt;
            }

            if (changed) cam.MovementKey(x, y, z);

            float step = 1.005f;
            int stepSpeed = 1;
            int speedMax = 20;
            if (_shiftPressed) step = 1.05f;


            if ((cam.GetScenePartMode() > ScenePartMode.All))
            {
                cam = Renderer.cameraControllerFromCamera(Renderer.SelectedCamera());
            }

            float fov = cam.GetFOV();
            float digZoom = cam.GetDigitalZoom();
            float digZoomCenterX = cam.GetDigitalZoomCenterX();

            if (_fovyUpPressed)
            {
                changedz = true;
                fov = fov * step;
            }
            if (_fovyDownPressed)
            {
                changedz = true;
                fov = fov / step;
            }

            if (_digZoomUpPressed)
            {
                changedz = true;
                if (digZoomSpeed < speedMax) digZoomSpeed = digZoomSpeed + stepSpeed;
            }
            else
            {
                if (digZoomSpeed > 0)
                {
                    changedz = true;
                    digZoomSpeed = digZoomSpeed - stepSpeed;
                }
            }

            if (_digZoomDownPressed)
            {
                changedz = true;
                if (digZoomSpeed > -speedMax) digZoomSpeed = digZoomSpeed - stepSpeed;
            }
            else
            {
                if (digZoomSpeed < 0)
                {
                    changedz = true;
                    digZoomSpeed = digZoomSpeed + stepSpeed;
                }
            }

            if (_digZoomCenterLeftPressed)
            {
                changedz = true;
                if (digZoomSpeedCenter < speedMax) digZoomSpeedCenter = digZoomSpeedCenter + stepSpeed;
            }
            else
            {
                if (digZoomSpeedCenter > 0)
                {
                    changedz = true;
                    digZoomSpeedCenter = digZoomSpeedCenter - stepSpeed;
                }
            }


            if (_digZoomCenterRightPressed)
            {
                changedz = true;
                if (digZoomSpeedCenter > -speedMax) digZoomSpeedCenter = digZoomSpeedCenter - stepSpeed;
            }
            else
            {
                if (digZoomSpeedCenter < 0)
                {
                    changedz = true;
                    digZoomSpeedCenter = digZoomSpeedCenter + stepSpeed;
                }
            }


            if (digZoomSpeed !=0)
            {
                changedz = true;
                digZoom = digZoom * (1 + (float)digZoomSpeed/10000f);
            }
            if (digZoomSpeedCenter != 0)
            {
                changedz = true;
                digZoomCenterX = digZoomCenterX + ((float)digZoomSpeedCenter / 2000f);
            }

            if (!changedz)
            {
                return;
            }

            CheckBoundsFloat(ref fov, fovLimitLower, fovLimitUpper);
            CheckBoundsFloat(ref digZoom, digitalZoomLimitLower, digitalZoomLimitUpper);
            CheckBoundsFloat(ref digZoomCenterX, 0f, 1f);
            cam.SetFOV(fov);
            cam.SetDigitalZoom(digZoom);
            cam.SetDigitalZoomCenterX(digZoomCenterX);

        }

        private void UpdateActiveViewIfNeeded(MouseEventArgs e)
        {
            // check which viewport has been hit and activate it
            _ui.ActiveTab.ActiveViewIndex = MousePosToViewportIndex(e.X, e.Y);
        }


        /// <summary>
        /// Converts a mouse position to a viewport index - in other words,
        /// it calculates the index of the viewport that is hit by a click
        /// on a given mouse position.
        /// </summary>
        /// <param name="x">Mouse x, in client (pixel) coordinates</param>
        /// <param name="y">Mouse y, in client (pixel) coordinates</param>
        /// <returns>Tab.ViewIndex._Max if the mouse coordinate doesn't hit a
        /// viewport. If not, the ViewIndex of the tab that was hit.</returns>
        private Tab.ViewIndex MousePosToViewportIndex(int x, int y)
        {
            var xf = x / (float)renderControl1.ClientSize.Width;
            var yf = 1.0f - y / (float)renderControl1.ClientSize.Height;

            return _ui.ActiveTab.GetViewportIndexHit(xf, yf);
        }



        private void SetViewportSplitH(float f)
        {
            _ui.ActiveTab.SetViewportSplitH(f);
        }


        private void SetViewportSplitV(float f)
        {
            _ui.ActiveTab.SetViewportSplitV(f);
        }


        /// <summary>
        /// Converts a mouse position to a viewport separator. It therefore
        /// checks whether the mouse is in a region where dragging viewport
        /// borders is possible.
        /// </summary>
        /// <param name="x">Mouse x, in client (pixel) coordinates</param>
        /// <param name="y">Mouse y, in client (pixel) coordinates</param>
        /// <returns>Tab.ViewSeparator._Max if the mouse coordinate doesn't hit a
        /// viewport separator. If not, the separator that was hit.</returns>
        private Tab.ViewSeparator MousePosToViewportSeparator(int x, int y)
        {
            var xf = x / (float)renderControl1.ClientSize.Width;
            var yf = 1.0f - y / (float)renderControl1.ClientSize.Height;

            return _ui.ActiveTab.GetViewportSeparatorHit(xf, yf);
        }


        private void OnShowLogViewer(object sender, EventArgs e)
        {
            if (_logViewer == null)
            {
                _logViewer = new LogViewer(this);
                _logViewer.Closed += (o, args) =>
                {
                    _logViewer = null;
                };
                _logViewer.Show();
            }
        }


        partial void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (_renderer == null) return;
            UpdateActiveViewIfNeeded(e);

            _previousMousePosX = e.X;
            _previousMousePosY = e.Y;

            if (e.Button == MouseButtons.Middle)
            {
                _mouseWheelDown = true;
                return;
            }
            if(e.Button == MouseButtons.Right)
            {
                _mouseRightDown = true;
                return;
            }

            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            _mouseDown = true;

            var sep = MousePosToViewportSeparator(e.X, e.Y);
            if (sep != Tab.ViewSeparator._Max)
            {
                // start dragging viewport separators
                _dragSeparator = sep;
                SetViewportDragCursor(sep);
            }

            CoreSettings.CoreSettings.Default.ViewsStatus = _ui.ActiveTab.getViewsStatusString();

            // hack: the renderer handles the input for the HUD, so forward the event
            var index = MousePosToViewportIndex(e.X, e.Y);
            if (index == Tab.ViewIndex._Max)
            {
                return;
            }

            if (sep == Tab.ViewSeparator._Max)
            {
                var view = UiState.ActiveTab.ActiveViews[(int)index];
                Debug.Assert(view != null);
                _renderer.OnMouseClick(e, view.Bounds, index);
            }           
        }


        partial void OnMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _mouseDown = false;
            }
            if (e.Button == MouseButtons.Middle)
            {
                _mouseWheelDown = false;
            }
            if (e.Button == MouseButtons.Right)
            {
                _mouseRightDown = false;
            }
            if (!IsDraggingViewportSeparator)
            {
                return;
            }
            _dragSeparator = Tab.ViewSeparator._Max;
            Cursor = Cursors.Default;
        }


        public bool IsDraggingViewportSeparator
        {
            get { return _dragSeparator != Tab.ViewSeparator._Max; }
        }


        partial void OnMouseMove(object sender, MouseEventArgs e)
        {
            if(_mouseWheelDown)
            {
                if (UiState.ActiveTab.ActiveCameraController != null)
                {
                    UiState.ActiveTab.ActiveCameraController.Pan(e.X - _previousMousePosX, e.Y - _previousMousePosY);
                }
                _previousMousePosX = e.X;
                _previousMousePosY = e.Y;
                return;
            }

            var sep = _dragSeparator != Tab.ViewSeparator._Max ? _dragSeparator : MousePosToViewportSeparator(e.X, e.Y);
            if (sep != Tab.ViewSeparator._Max)
            {
                // show resize cursor
                SetViewportDragCursor(sep);

                // and adjust viewport separators
                if (IsDraggingViewportSeparator)
                {
                    if (sep == Tab.ViewSeparator.Horizontal)
                    {
                        SetViewportSplitV(1.0f - e.Y / (float)renderControl1.ClientSize.Height);
                    }
                    else if (sep == Tab.ViewSeparator.Vertical)
                    {
                        SetViewportSplitH(e.X / (float)renderControl1.ClientSize.Width);
                    }
                    else if (sep == Tab.ViewSeparator.Both)
                    {
                        SetViewportSplitV(1.0f - e.Y / (float)renderControl1.ClientSize.Height);
                        SetViewportSplitH(e.X / (float)renderControl1.ClientSize.Width);
                    }
                    else
                    {
                        Debug.Assert(false);
                    }
                }
                return;
            }

            Cursor = Cursors.Default;
            if (e.Delta != 0 && UiState.ActiveTab.ActiveCameraController != null)
            {
                UiState.ActiveTab.ActiveCameraController.Scroll(e.Delta);
            }

            // hack: the renderer handles the input for the HUD, so forward the event
            if (_renderer == null) return;
            var index = MousePosToViewportIndex(e.X, e.Y);
            if (index == Tab.ViewIndex._Max)
            {
                return;
            }
            var view = UiState.ActiveTab.ActiveViews[(int)index];
            Debug.Assert(view != null);
            _renderer.OnMouseMove(e, view.Bounds, index);

            if (!_mouseDown && !_mouseRightDown)
            {
                return;
            }

            if (UiState.ActiveTab.ActiveCameraController != null)
            {
                var vx = e.X - _previousMousePosX;
                var vy = e.Y - _previousMousePosY;
                if(_mouseRightDown)
                {
                    var viewMatrix = UiState.ActiveTab.ActiveCameraController == null ? Matrix4.Identity :
                     UiState.ActiveTab.ActiveCameraController.GetView();
                    Renderer.HandleLightRotationOnMouseMove(vx, vy, ref viewMatrix);
                }
                else
                {
                    UiState.ActiveTab.ActiveCameraController.MouseMove(vx, vy);
                }
            }
            _previousMousePosX = e.X;
            _previousMousePosY = e.Y;
        }


        private void SetViewportDragCursor(Tab.ViewSeparator sep)
        {
            switch (sep)
            {
                case Tab.ViewSeparator.Horizontal:
                    Cursor = Cursors.HSplit;
                    break;
                case Tab.ViewSeparator.Vertical:
                    Cursor = Cursors.VSplit;
                    break;
                case Tab.ViewSeparator.Both:
                    Cursor = Cursors.SizeAll;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        partial void OnMouseLeave(object sender, EventArgs e)
        {
            if (_mouseDown)
            {
                Capture = true;
            }

            Cursor = Cursors.Default;
        }


        partial void OnMouseEnter(object sender, EventArgs e)
        {
            Capture = false;
        }


        protected override bool IsInputKey(Keys keyData)
        {
           return true;
        }

        private void OnKeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
         //   e.Handled = true; 
        }

        partial void OnPreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
           e.IsInputKey = true;
        }

        partial void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (Renderer == null) return;

            if (TextBoxFocused) return;
            e.Handled = true;

            lock (Renderer.renderParameterLock)
            {
                float step = 1.001f;
                int index = tabControl1.SelectedIndex;
            switch (e.KeyData)
            {
                case Keys.PageUp:
                case Keys.Left:
                    index--;
                    if (index == -1) index = tabControl1.TabPages.Count-1;
                    SelectTab(tabControl1.TabPages[index]);
                    break;

                case Keys.Next:
                case Keys.Right:
                    index++;
                    if (index == tabControl1.TabPages.Count) index = 0;
                    SelectTab(tabControl1.TabPages[index]);
                    break;

                case Keys.F5:
                case Keys.Escape:
                       // ResetAnimations();
                        UiForTab(_ui.ActiveTab).GetInspector().Animations.OnPlay(sender, e);
                        break;

                case Keys.OemPeriod:
                    Renderer.SwitchActiveCameras();
      //              UiForTab(_ui.ActiveTab).GetInspector().Animations.SetTime(0);
                    break;

                case Keys.W:
                    _forwardPressed = true;
                    break;

                case Keys.A:
                    _leftPressed = true;
                    break;

                case Keys.S:
                    _backPressed = true;
                    break;

                case Keys.D:
                    _rightPressed = true;
                    break;

                case Keys.Up:
                    _upPressed = true;
                    break;

                case Keys.Down:
                    _downPressed = true;
                    break;

                case Keys.Multiply:
                    _shiftPressed = true;
                    break;

                case Keys.R:
                        // reset camera immediatelly
                        var cam = UiState.ActiveTab.ActiveCameraController;
                        if (cam == null)
                        {
                            return;
                        }
                        if ((cam.GetScenePartMode() > ScenePartMode.All))
                        {
                            cam = Renderer.cameraControllerFromCamera(Renderer.SelectedCamera());
                            cam.SetFOV(fovPreset);
                            cam.SetDigitalZoom(1f);
                            cam.SetDigitalZoomCenterX(0.5f);
                            cam.SetDigitalZoomCenterY(0.5f);
                        }
                        else
                        {
                            UiState.ActiveTab.ResetActiveCameraController();
                        }

                        break;

                case Keys.O:
                    //reset offset
                    OpenVRInterface.viewOffset = Matrix4.Identity;
                    break;

                    case Keys.Add:
                        _digZoomDownPressed = true;
                        break;

                    case Keys.Subtract:
                        _digZoomUpPressed = true;
                        break;

                    case Keys.NumPad7:
                        _digZoomCenterLeftPressed = true;
                        break;

                    case Keys.NumPad9:
                        _digZoomCenterRightPressed = true;
                        break;

                    case Keys.NumPad8:
                        _fovyUpPressed = true;
                        break;

                    case Keys.NumPad5:
                        _fovyDownPressed = true;
                        break;

                    case Keys.E:
                    // switch backend
                    if (_settings == null || _settings.IsDisposed) _settings = new SettingsDialog { MainWindow = this };
                    _settings.ChangeRenderingBackend();
                    break;
                case Keys.N:
                    // reset NDI streams
                    if (useIO) _renderer.FlushNDI();
                    break;

                    case Keys.H:
                        Renderer.SwitchToCamera(0);
                        break;
                    case Keys.B:
                        if (_renderer.renderIO && (UiState.ActiveTab.ActiveCameraController != null)) UiState.ActiveTab.ActiveCameraController.SetScenePartMode(ScenePartMode.Background);
                        break;
                    case Keys.F:
                        if (_renderer.renderIO && (UiState.ActiveTab.ActiveCameraController != null)) UiState.ActiveTab.ActiveCameraController.SetScenePartMode(ScenePartMode.Foreground);
                        break;
                    case Keys.V:
                        if (_renderer.renderIO && (UiState.ActiveTab.ActiveCameraController != null)) UiState.ActiveTab.ActiveCameraController.SetScenePartMode(ScenePartMode.Visible);
                        break;
                    case Keys.X:
                        if (_renderer.renderIO && (UiState.ActiveTab.ActiveCameraController != null)) UiState.ActiveTab.ActiveCameraController.SetScenePartMode(ScenePartMode.Others);
                        break;
                    case Keys.C:
                        if (_renderer.renderIO && (UiState.ActiveTab.ActiveCameraController != null)) UiState.ActiveTab.ActiveCameraController.SetScenePartMode(ScenePartMode.Camera);
                        break;
                    case Keys.L:
                        if (_renderer.renderIO && (UiState.ActiveTab.ActiveCameraController != null)) UiState.ActiveTab.ActiveCameraController.SetScenePartMode(ScenePartMode.All);
                        break;
                    case Keys.J:
                        if (_renderer.renderIO && (UiState.ActiveTab.ActiveCameraController != null)) UiState.ActiveTab.ActiveCameraController.SetScenePartMode(ScenePartMode.CameraCancelColor);
                        break;
                    case Keys.M:
                        if (_renderer.renderIO && (UiState.ActiveTab.ActiveCameraController != null)) UiState.ActiveTab.ActiveCameraController.SetScenePartMode(ScenePartMode.Output);
                        break;
                    case Keys.K:
                        if (_renderer.renderIO && (UiState.ActiveTab.ActiveCameraController != null)) UiState.ActiveTab.ActiveCameraController.SetScenePartMode(ScenePartMode.Keying);
                        break;
                    case Keys.G:
                        if (_renderer.renderIO && (UiState.ActiveTab.ActiveCameraController != null)) UiState.ActiveTab.ActiveCameraController.SetScenePartMode(ScenePartMode.GreenScreen);
                        break;
                    case Keys.Enter:
                        Renderer.SwitchActiveCameras();
                        Renderer.syncTrack(false, "SwitchCameras", 15);
                        e.Handled = true;
                        break;
                    case Keys.NumPad0:
                        if (_ui.ActiveTab.ActiveViewIndex != Tab.ViewIndex.Index4) _ui.ActiveTab.ActiveViewIndex = Tab.ViewIndex.Index0;
                        //Todo : index switching for 2 views only
                        Renderer.SwitchToCamera(0);
                        break;
                    case Keys.NumPad1:
                        if (_ui.ActiveTab.ActiveViewIndex != Tab.ViewIndex.Index4) _ui.ActiveTab.ActiveViewIndex = Tab.ViewIndex.Index1;
                        Renderer.SwitchToCamera(1);
                        break;
                    case Keys.NumPad2:
                        if (_ui.ActiveTab.ActiveViewIndex != Tab.ViewIndex.Index4) _ui.ActiveTab.ActiveViewIndex = Tab.ViewIndex.Index2;
                        Renderer.SwitchToCamera(2);
                        break;
                    case Keys.NumPad3:
                        if (_ui.ActiveTab.ActiveViewIndex != Tab.ViewIndex.Index4) _ui.ActiveTab.ActiveViewIndex = Tab.ViewIndex.Index3;
                        Renderer.SwitchToCamera(3);
                        break;
                    case Keys.NumPad4:
                        Renderer.SwitchToCamera(4);
                        break;
                    case Keys.U:
                        OpenVRInterface.fPredictedSecondsToPhotonsFromNow = OpenVRInterface.fPredictedSecondsToPhotonsFromNow - (step - 1) / 1f;
                        if (OpenVRInterface.fPredictedSecondsToPhotonsFromNow < -OpenVRInterface.maxAdvance) OpenVRInterface.fPredictedSecondsToPhotonsFromNow = -OpenVRInterface.maxAdvance;
                        CoreSettings.CoreSettings.Default.SecondsToPhotons = OpenVRInterface.fPredictedSecondsToPhotonsFromNow;
                        break;
                    case Keys.I:
                        OpenVRInterface.fPredictedSecondsToPhotonsFromNow = OpenVRInterface.fPredictedSecondsToPhotonsFromNow + (step - 1) / 1f;
                        if (OpenVRInterface.fPredictedSecondsToPhotonsFromNow > OpenVRInterface.maxAdvance) OpenVRInterface.fPredictedSecondsToPhotonsFromNow = OpenVRInterface.maxAdvance;
                        break;
                    case Keys.T:
                        if (capturePreview[Renderer.ActiveCamera] != null)
                        {
                            var delay = capturePreview[Renderer.ActiveCamera].GetAdditionalDelay();
                            delay++;
                            capturePreview[Renderer.ActiveCamera].SetAdditionalDelay(delay);
                        }
                        break;
                    case Keys.Y:
                    case Keys.Z:
                        if (capturePreview[Renderer.ActiveCamera] != null)
                        {
                            var delay = capturePreview[Renderer.ActiveCamera].GetAdditionalDelay();
                            delay--;
                            capturePreview[Renderer.ActiveCamera].SetAdditionalDelay(delay);
                        }
                        break;
                }
            }
        }


        partial void OnKeyUp(object sender, KeyEventArgs keyEventArgs)
        {
            switch (keyEventArgs.KeyData)
            {
                case Keys.W:
                    _forwardPressed = false;
                    break;

                case Keys.A:
                    _leftPressed = false;
                    break;

                case Keys.S:
                    _backPressed = false;
                    break;

                case Keys.D:
                    _rightPressed = false;
                    break;

                case Keys.Up:
                    _upPressed = false;
                    break;

                case Keys.Down:
                    _downPressed = false;
                    break;

                case Keys.Add:
                    _digZoomDownPressed = false;
                    break;

                case Keys.Subtract:
                    _digZoomUpPressed = false;
                    break;

                case Keys.NumPad7:
                    _digZoomCenterLeftPressed = false;
                    break;

                case Keys.NumPad9:
                    _digZoomCenterRightPressed = false;
                    break;

                case Keys.NumPad8:
                    _fovyUpPressed = false;
                    break;

                case Keys.NumPad5:
                    _fovyDownPressed = false;
                    break;

                case Keys.Multiply:
                    _shiftPressed = false;
                    break;
    }
}
    }
}

/* vi: set shiftwidth=4 tabstop=4: */ 