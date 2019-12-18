using System.Windows.Forms;

namespace SoftBody
{
    public partial class Form1 : Form
    {
        public DxPipeline Pipeline;

        public Form1()
        {
            InitializeComponent();
            KeyPreview = true;

            Pipeline = new DxPipeline(pictureBox1, this);
            KeyDown += MainForm_KeyDown;

            propertyGrid1.SelectedObject = new Configuration();
            disturbButton.Click += (s, e) => Pipeline.Disturb();
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.F1:
                    if (Pipeline.Camera.Focused)
                    {
                        Pipeline.Camera.Focused = false;
                        return;
                    }

                    Cursor.Position = pictureBox1.PointToScreen(new System.Drawing.Point(pictureBox1.Width / 2, pictureBox1.Height / 2));
                    Pipeline.Camera.Focused = true;
                    Pipeline.ControlCubeCamera.Focused = false;
                    disturbButton.Focus();
                    break;

                case Keys.F2:
                    if (Pipeline.ControlCubeCamera.Focused)
                    {
                        Pipeline.ControlCubeCamera.Focused = false;
                        return;
                    }

                    Cursor.Position = pictureBox1.PointToScreen(new System.Drawing.Point(pictureBox1.Width / 2, pictureBox1.Height / 2));
                    Pipeline.ControlCubeCamera.Focused = true;
                    Pipeline.Camera.Focused = false;
                    disturbButton.Focus();
                    break;
            }
        }
    }
}
