using SharpDX;
using System;
using System.Windows.Forms;

namespace SoftBody
{
    public class Camera
    {
        public bool Focused { get; set; }

        private readonly Control _control;
        private static readonly Vector3 Up = new Vector3(0, 1, 0);
        private const float MovementSpeed = 20.0f;
        private const float MouseSensitivity = 0.0005f;
        private DateTime _lastComputationTime;
        private int _wPressed;
        private int _sPressed;
        private int _aPressed;
        private int _dPressed;
        private int _qPressed;
        private int _ePressed;
        private float _pitchRotation;
        private float _yawRotation;
        private Vector3 CurrentDirection
        {
            get
            {
                var z = Math.Cos(_pitchRotation) * Math.Cos(_yawRotation);
                var x = Math.Cos(_pitchRotation) * Math.Sin(_yawRotation);
                var y = Math.Sin(_pitchRotation);
                return new Vector3((float)x, (float)y, (float)z);
            }
        }

        public Camera(Control keyboardControl, Control mouseControl)
        {
            _control = keyboardControl;

            mouseControl.MouseMove += Box_MouseMove;
            keyboardControl.KeyDown += Box_KeyDown;
            keyboardControl.KeyUp += Box_KeyUp;
            keyboardControl.LostFocus += Box_LostFocus;
        }

        public Vector3 Position { get; set; } = new Vector3(0, 0, -10);
        public Matrix ViewMatrix => Matrix.LookAtLH(Position, Position + CurrentDirection, Vector3.UnitY);

        public Matrix InverseViewMatrix
        {
            get
            {
                var viewMatrix = ViewMatrix;
                Matrix.Invert(ref viewMatrix, out Matrix result);
                return result;
            }
        }

        private void UpdateMovementDirection(Keys keyPressed, int valueUpDown)
        {
            switch (keyPressed)
            {
                case Keys.A:
                    _aPressed = valueUpDown;
                    break;
                case Keys.D:
                    _dPressed = valueUpDown;
                    break;
                case Keys.S:
                    _sPressed = valueUpDown;
                    break;
                case Keys.W:
                    _wPressed = valueUpDown;
                    break;
                case Keys.Q:
                    _qPressed = valueUpDown;
                    break;
                case Keys.E:
                    _ePressed = valueUpDown;
                    break;
            }
        }

        public void UpdatePosition()
        {
            var now = DateTime.Now;
            var span = now - _lastComputationTime;
            _lastComputationTime = now;

            if (!Focused)
                return;

            var durationScaled = (float)span.TotalSeconds * MovementSpeed;
            var zaxis = CurrentDirection;
            var xaxis = Vector3.Normalize(Vector3.Cross(Up, zaxis));

            Position += durationScaled * zaxis * (_wPressed - _sPressed);
            Position += durationScaled * xaxis * (_dPressed- _aPressed);
            Position += durationScaled * Up * (_qPressed - _ePressed);
        }

        private void Box_MouseMove(object sender, MouseEventArgs e)
        {
            if (!Focused)
                return;

            var centerX = _control.Width / 2;
            var centerY = _control.Height / 2;
            var diffX = e.X - centerX;
            var diffY = e.Y - centerY;
            if (diffY == 0 && diffX == 0)
                return;

            SetCursorInMiddle();

            _yawRotation += diffX * MouseSensitivity;
            _pitchRotation -= diffY * MouseSensitivity;
        }

        public void SetCursorInMiddle()
        {
            Cursor.Position = _control.PointToScreen(new System.Drawing.Point(_control.Width / 2, _control.Height / 2));
        }

        private void Box_LostFocus(object sender, EventArgs e)
        {
            _wPressed = _sPressed = _aPressed = _dPressed = 0;
        }

        private void Box_KeyDown(object sender, KeyEventArgs e)
        {
            UpdateMovementDirection(e.KeyCode, 1);
        }

        private void Box_KeyUp(object sender, KeyEventArgs e)
        {
            UpdateMovementDirection(e.KeyCode, 0);
        }
    }
}
