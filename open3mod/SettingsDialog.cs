///////////////////////////////////////////////////////////////////////////////////
// Open 3D Model Viewer (open3mod) (v2.0)
// [SettingsDialog.cs]
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
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using OpenTK;
using System.Windows.Forms;
using System.Globalization;

namespace open3mod
{
    public partial class SettingsDialog : Form
    {
        private GraphicsSettings _gSettings;
        private MainWindow _mainWindow;
        IFormatProvider _provider = CultureInfo.CreateSpecificCulture("en-US");
        private string cam3offsRollText = "0";

        public SettingsDialog()
        {
            _gSettings = GraphicsSettings.Default;
            _gSettings.Reload();

            InitializeComponent();
            this.cam0offsX.MouseWheel += new System.Windows.Forms.MouseEventHandler(box_MouseWheel);
            this.cam0offsY.MouseWheel += new System.Windows.Forms.MouseEventHandler(box_MouseWheel);
            this.cam0offsZ.MouseWheel += new System.Windows.Forms.MouseEventHandler(box_MouseWheel);
            this.cam0offsPitch.MouseWheel += new System.Windows.Forms.MouseEventHandler(box_MouseWheel);
            this.cam3offsX.MouseWheel += new System.Windows.Forms.MouseEventHandler(box_MouseWheel);
            this.cam3offsY.MouseWheel += new System.Windows.Forms.MouseEventHandler(box_MouseWheel);
            this.cam3offsZ.MouseWheel += new System.Windows.Forms.MouseEventHandler(box_MouseWheel);
            this.cam3offsPitch.MouseWheel += new System.Windows.Forms.MouseEventHandler(box_MouseWheel);
            this.cam3offsYaw.MouseWheel += new System.Windows.Forms.MouseEventHandler(box_MouseWheel);

            InitTexResolution();
            InitTexFilter();
            InitMultiSampling();
            InitLightingQuality();
            InitRenderingBackend();
            InitCam0Offset();
            InitCam3Position();
            InitHMDOffset();
            InitNDIoverride();

            if (CoreSettings.CoreSettings.Default.AdditionalTextureFolders != null)
            {
                folderSetDisplaySearchPaths.Folders =
                    CoreSettings.CoreSettings.Default.AdditionalTextureFolders.Cast<string>().ToArray();
            }
            folderSetDisplaySearchPaths.Change += sender =>
                {
                    if(CoreSettings.CoreSettings.Default.AdditionalTextureFolders == null)
                    {
                        CoreSettings.CoreSettings.Default.AdditionalTextureFolders = new StringCollection();
                    }
                    var add = CoreSettings.CoreSettings.Default.AdditionalTextureFolders;
                 
                    add.Clear();
                    foreach (var v in folderSetDisplaySearchPaths.Folders)
                    {
                        add.Add(v);
                    }
                };
        }


        public MainWindow MainWindow
        {
            set { _mainWindow = value; }
        }

        private void box_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            boxOK = true;
            float test = readFloat((sender as TextBox).Text);
            if (!boxOK)
            {
                return;
            }
            boxOK = true;
            test = test + e.Delta / 120;
            (sender as TextBox).Text = test.ToString(_provider);
            OpenVRInterface.SetCam0Offset(cam0offs);
            OpenVRInterface.SetCam3Position(cam3pos);
        }



        private void OnOk(object sender, EventArgs e)
        {
            _gSettings.Save();
            OpenVRInterface.SetCam0Offset(cam0offs);
            OpenVRInterface.SetCam3Position(cam3pos);
            if (_mainWindow == null)
            {
                Close();
                return;
            }
            _mainWindow.CloseSettingsDialog();
        }


        private void InitTexResolution()
        {
            var bias = _gSettings.TexQualityBias;
            if (bias == 0)
            {
                comboBoxTexResolution.SelectedIndex = 0;
            }
            else if (bias == 1)
            {
                comboBoxTexResolution.SelectedIndex = 1;
            }
            else if (bias > 1)
            {
                comboBoxTexResolution.SelectedIndex = 2;
            }
            bias = _gSettings.NdiTexQualityBias;
            if (bias == 0)
            {
                comboBoxNdiTexResolution.SelectedIndex = 0;
            }
            else if (bias == 1)
            {
                comboBoxNdiTexResolution.SelectedIndex = 1;
            }
            else if (bias > 1)
            {
                comboBoxNdiTexResolution.SelectedIndex = 2;
            }
        }


        private void OnChangeTextureResolution(object sender, EventArgs e)
        {
            switch(comboBoxTexResolution.SelectedIndex)
            {
                case 0:
                    _gSettings.TexQualityBias = 0;
                    
                    break;
                case 1:
                    _gSettings.TexQualityBias = 1;
                    break;
                case 2:
                    _gSettings.TexQualityBias = 3;
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }

            if(_mainWindow == null)
            {
                return;
            }
            foreach (var scene in _mainWindow.UiState.ActiveScenes())
            {
                scene.RequestReuploadTextures();
            }
        }

        private void OnChangeNdiTextureResolution(object sender, EventArgs e)
        {
            switch (comboBoxNdiTexResolution.SelectedIndex)
            {
                case 0:
                    _gSettings.NdiTexQualityBias = 0;

                    break;
                case 1:
                    _gSettings.NdiTexQualityBias = 1;
                    break;
                case 2:
                    _gSettings.NdiTexQualityBias = 3;
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }

            if (_mainWindow == null)
            {
                return;
            }
        }


        private void InitTexFilter()
        {
            comboBoxSetTextureFilter.SelectedIndex = _gSettings.TextureFilter;
        }


        private void OnChangeTextureFilter(object sender, EventArgs e)
        {
            Debug.Assert(comboBoxSetTextureFilter.SelectedIndex <= 3);
            _gSettings.TextureFilter = comboBoxSetTextureFilter.SelectedIndex;
           
            if (_mainWindow == null)
            {
                return;
            }
            foreach (var scene in _mainWindow.UiState.ActiveScenes()) 
            {
                scene.RequestReconfigureTextures();
            }
        }


        private void OnChangeMipSettings(object sender, EventArgs e)
        {
            foreach (var scene in _mainWindow.UiState.ActiveScenes())
            {
                scene.RequestReconfigureTextures();
            }
        }


        private void InitMultiSampling()
        {
            comboBoxSetMultiSampling.SelectedIndex = _gSettings.MultiSampling;
        }


        private void OnChangeMultiSamplingMode(object sender, EventArgs e)
        {
            Debug.Assert(comboBoxSetMultiSampling.SelectedIndex <= 3);
            if (_gSettings.MultiSampling != comboBoxSetMultiSampling.SelectedIndex)
            {
                _gSettings.MultiSampling = comboBoxSetMultiSampling.SelectedIndex;
                labelPleaseRestart.Visible = true;
            }
        }


        private void InitLightingQuality()
        {     
            comboBoxSetLightingMode.SelectedIndex = _gSettings.LightingQuality;
        }


        private void InitRenderingBackend()
        {
            comboBoxSetBackend.SelectedIndex = _gSettings.RenderingBackend;
        }

        public void ChangeRenderingBackend()
        {
            Debug.Assert(comboBoxSetBackend.SelectedIndex <= 1);
            comboBoxSetBackend.SelectedIndex = 1- comboBoxSetBackend.SelectedIndex;
        }


        private void OnChangeRenderingBackend(object sender, EventArgs e)
        {
            Debug.Assert(comboBoxSetBackend.SelectedIndex <= 1);
            _gSettings.RenderingBackend = comboBoxSetBackend.SelectedIndex;

            if (_mainWindow == null)
            {
                return;
            }
        //    lock (_mainWindow.Renderer.renderTargetLock)// this is deadlocking !!
            {
                foreach (var scene in _mainWindow.UiState.ActiveScenes())
                {
                    scene.RecreateRenderingBackend();
                }
                _mainWindow.Renderer.RecreateRenderingBackend();
            }
        }


        private void checkBoxBFCulling_CheckedChanged(object sender, EventArgs e)
        {
            foreach (var scene in _mainWindow.UiState.ActiveScenes())
            {
                scene.RequestRenderRefresh();
            }
        }

        private void checkBoxGenerateTangentSpace_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void checkBoxComputeNormals_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void OnLMWebsite(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.leapmotion.com"); 
        }

        private void trackBarKeyingColorH_ValueChanged(object sender, EventArgs e)
        {
            labelH.Text = "H:" + trackBarKeyingColorH.Value.ToString();
        }

        private void trackBarKeyingColorS_ValueChanged(object sender, EventArgs e)
        {
            labelS.Text = "S:" + trackBarKeyingColorS.Value.ToString();
        }

        private void trackBarKeyingColorV_ValueChanged(object sender, EventArgs e)
        {
            labelV.Text = "V:"+trackBarKeyingColorV.Value.ToString();
        }

        private void UseTrackingCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            labelPleaseRestart.Visible = true;
        }

        private void autoStartCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            labelPleaseRestart.Visible = true;
        }

        int unit = 100;
        private void InitCam0Offset()
        {
            if (OpenVRInterface.indexOfDevice[0] >= OpenVRInterface.trackerToCamera.Length) return;
            Matrix4 position = OpenVRInterface.trackerToCamera[OpenVRInterface.indexOfDevice[0]];
            cam0offsX.Text = (position.M41 * unit).ToString("0.###", _provider);
            cam0offsY.Text = (position.M42 * unit).ToString("0.###", _provider);
            cam0offsZ.Text = (position.M43 * unit).ToString("0.###", _provider);
            Vector3 angles = OpenVRInterface.FromRotMatToEulerZYXInt(position);
            cam0offsPitch.Text = (angles.X*180/(float)Math.PI).ToString("0.###", _provider);
        }

        private void InitCam3Position()
        {
            if (OpenVRInterface.indexOfDevice[3] >= OpenVRInterface.trackerToCamera.Length) return;
            Matrix4 position = OpenVRInterface.trackerToCamera[OpenVRInterface.indexOfDevice[3]];
            cam3offsX.Text = (position.M41 * unit).ToString("0.###", _provider);
            cam3offsY.Text = (position.M42 * unit).ToString("0.###", _provider);
            cam3offsZ.Text = (position.M43 * unit).ToString("0.###", _provider);
            Vector3 angles = OpenVRInterface.FromRotMatToEulerZYXInt(position);
            cam3offsPitch.Text = (angles.X * 180 / (float)Math.PI).ToString("0.###", _provider);
            cam3offsYaw.Text = (angles.Y * 180 / (float)Math.PI).ToString("0.###", _provider);
            cam3offsRollText = (angles.Z * 180 / (float)Math.PI).ToString("0.###", _provider); // should be always zero
        }


        private void InitHMDOffset()
        {
            Matrix4 position = OpenVRInterface.hmdRefPos;
            hmdRefX.Text = (position.M41 * unit).ToString("0.###", _provider);
            hmdRefY.Text = (position.M42 * unit).ToString("0.###", _provider);
            hmdRefZ.Text = (position.M43 * unit).ToString("0.###", _provider);
            Vector3 angles = OpenVRInterface.FromRotMatToEulerZYXInt(position);
        }

        bool boxOK;
        Matrix4 cam0offs;
        private void cam0box_TextChanged(object sender, EventArgs e)
        {
            boxOK = true;
            float test = readFloat((sender as TextBox).Text);
            if (!boxOK)
            {
                MessageBox.Show("Invalid char entered", "Error", MessageBoxButtons.OK);
                return;
            }
            boxOK = true;
            Matrix4 transMatrix = Matrix4.CreateTranslation(readFloat(cam0offsX.Text) / unit, readFloat(cam0offsY.Text) / unit, readFloat(cam0offsZ.Text) / unit);
            Matrix4 orientMatrix = Matrix4.CreateRotationX(readFloat(cam0offsPitch.Text) * (float)Math.PI / 180);//degrees??
            cam0offs = orientMatrix * transMatrix;
            if (boxOK)
            {
                //labelYR.Text = readFloat(cam0offsX.Text).ToString(_provider) + " " + readFloat(cam0offsY.Text).ToString(_provider) + " " + readFloat(cam0offsZ.Text).ToString(_provider) + " " + readFloat(cam0offsPitch.Text).ToString(_provider);
            }
            else
            {
                MessageBox.Show("Offset Numbers Not Valid", "Error", MessageBoxButtons.OK);
            }

        }
        Matrix4 cam3pos;
        private void cam3box_TextChanged(object sender, EventArgs e)
        {
            boxOK = true;
            float test = readFloat((sender as TextBox).Text);
            if (!boxOK)
            {
                MessageBox.Show("Invalid char entered", "Error", MessageBoxButtons.OK);
                return;
            }
            boxOK = true;
            Matrix4 transMatrix = Matrix4.CreateTranslation(readFloat(cam3offsX.Text)/unit, readFloat(cam3offsY.Text) / unit, readFloat(cam3offsZ.Text) / unit);
            Matrix4 orientMatrix = Matrix4.Identity;
            orientMatrix = Matrix4.CreateRotationZ(readFloat(cam3offsRollText) * (float)Math.PI / 180) * orientMatrix;//degrees??
            orientMatrix = Matrix4.CreateRotationY(readFloat(cam3offsYaw.Text)  * (float)Math.PI / 180) * orientMatrix;//degrees??
            orientMatrix = Matrix4.CreateRotationX(readFloat(cam3offsPitch.Text)* (float)Math.PI / 180) * orientMatrix;//degrees??
            cam3pos = orientMatrix * transMatrix;
            if (boxOK)
            {
              //labelYR.Text = readFloat(cam0offsX.Text).ToString(_provider) + " " + readFloat(cam0offsY.Text).ToString(_provider) + " " + readFloat(cam0offsZ.Text).ToString(_provider) + " " + readFloat(cam0offsPitch.Text).ToString(_provider);
            }
            else
            {
                MessageBox.Show("Position Numbers Not Valid", "Error", MessageBoxButtons.OK);
            }

        }

        private float readFloat(string text)
        {
            if ((text == "-") || (text == ".")) return 0;
            if ((text == "-.")) return 0;
            double pos = 0;
            try
            {
                pos = Double.Parse(text, NumberStyles.Float, _provider);
            }
            catch (FormatException)
            {
                boxOK = false;
            }
            return (float)pos;
        }

        private void numBox_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            if ((e.KeyChar == 13) && (boxOK = true))
            {
                (sender as TextBox).SelectAll();
                OpenVRInterface.SetCam0Offset(cam0offs);
                InitCam0Offset();
                e.Handled = true;
                return;
            }
            numBox_CheckInput(sender, e);
        }

        private void cam0box_LeaveFocus(object sender, EventArgs e)
        {
            InitCam0Offset();
        }

        private void cam3box_LeaveFocus(object sender, EventArgs e)
        {
            InitCam3Position();
        }

        private void numBox3_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            if ((e.KeyChar == 13) && (boxOK = true))
            {
                (sender as TextBox).SelectAll();
                OpenVRInterface.SetCam3Position(cam3pos);
                InitCam3Position();
                e.Handled = true;
                return;
            }
            numBox_CheckInput(sender, e);
        }

        private void numBox_CheckInput(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            if (e.KeyChar == ',') e.KeyChar = '.';
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '.') && (e.KeyChar != '-'))
            {
                e.Handled = true;
            }
            //do not write before '-'
            if (((sender as TextBox).Text.Length > 0) && ((sender as TextBox).Text.ElementAt(0) == '-') && ((sender as TextBox).SelectionStart == 0)&& ((sender as TextBox).SelectionLength == 0))
            {
                e.Handled = true;
            }

            // only allow one decimal point

            if (e.KeyChar == '.') 
            {
                int pointPosition = (sender as TextBox).Text.IndexOf('.');
                if ((sender as TextBox).Text.IndexOf('.') > -1)
                {
                    if (((sender as TextBox).SelectionStart <= pointPosition) && ((sender as TextBox).SelectionStart + (sender as TextBox).SelectionLength > pointPosition)) return;
                    e.Handled = true;
                }
            }
            // only allow one minus 
            if (e.KeyChar == '-')
            {
                int pointPosition = (sender as TextBox).Text.IndexOf('-');
                if ((sender as TextBox).Text.IndexOf('-') > -1)
                {
                    if (((sender as TextBox).SelectionStart <= pointPosition) && ((sender as TextBox).SelectionStart + (sender as TextBox).SelectionLength > pointPosition)) return;
                }
                if ((sender as TextBox).SelectionStart != 0) e.Handled = true;
            }
            if (((sender as TextBox).Text.Length > (sender as TextBox).SelectionLength+5)  && (!char.IsControl(e.KeyChar)))
            {
                e.Handled = true; // prevents long numbers, but also entering - and .
            }

        }

        private void resetStudioZeroFromHMDRef_Click(object sender, EventArgs e)
        {
            OpenVRInterface.ApplyHMDReference();
        }

        private void saveHMDRef_Click(object sender, EventArgs e)
        {
            OpenVRInterface.SaveHMDReference();
            InitHMDOffset();
        }

        private void SelectText(object sender, EventArgs e)
        {
            (sender as TextBox).SelectAll();
        }

        private void rescanVRDevices_Click(object sender, EventArgs e)
        {
            _mainWindow.RescanDevices(sender,e);
        }

        private void grabCam0ToVirt_Click(object sender, EventArgs e)
        {
            OpenVRInterface.GrabCamToVirt(0);
            InitCam3Position();
        }
        private void grabCam1ToVirt_Click(object sender, EventArgs e)
        {
            OpenVRInterface.GrabCamToVirt(1);
            InitCam3Position();
        }

        private void InitNDIoverride()
        {
            comboNDI1override.SelectedIndex = CoreSettings.CoreSettings.Default.NDI1overrideSource;
            comboNDI2override.SelectedIndex = CoreSettings.CoreSettings.Default.NDI2overrideSource;
        }

        private void comboNDI1override_SelectedIndexChanged(object sender, EventArgs e)
        {
            CoreSettings.CoreSettings.Default.NDI1overrideSource = (byte)comboNDI1override.SelectedIndex;
        }

        private void comboNDI2override_SelectedIndexChanged(object sender, EventArgs e)
        {
            CoreSettings.CoreSettings.Default.NDI2overrideSource = (byte)comboNDI2override.SelectedIndex;
        }
    }
}

/* vi: set shiftwidth=4 tabstop=4: */ 