using Assimp;
using Assimp.Configs;
using SharpDX.Windows;
using System;
using System.Windows.Forms;

namespace SoftBody
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var form = new Form1();
            RenderLoop.Run(form, form.Pipeline.Redraw);
        }
    }
}
