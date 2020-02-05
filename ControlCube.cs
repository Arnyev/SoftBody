using SharpDX.Direct3D11;
using SharpDX;
using SharpDX.Direct3D;
using Device = SharpDX.Direct3D11.Device;
using Buffer = SharpDX.Direct3D11.Buffer;
using SharpDX.DXGI;
using System.Linq;
using System.Windows.Forms;

namespace SoftBody
{
    public class ControlCube
    {
        public static bool Draw { get; set; } = true;
        public readonly Camera Camera;

        private readonly Vector4[] baseCubePositions = new Vector4[]
        {
            new Vector4(-5, -5, -5, 1),
            new Vector4(-5, -5, 5, 1),
            new Vector4(-5, 5, -5, 1),
            new Vector4(-5, 5, 5, 1),
            new Vector4(5, -5, -5, 1),
            new Vector4(5, -5, 5, 1),
            new Vector4(5, 5, -5, 1),
            new Vector4(5, 5, 5, 1),
        };

        public BezierCubeVertex[] Vertices { get; }

        Buffer vertexBuffer;
        Buffer indexBuffer;
        VertexBufferBinding vertexBinding;

        public ControlCube(Device device, Control keyboardControl, Control mouseControl)
        {
            Vertices = baseCubePositions.Select(v => new BezierCubeVertex(v.X, v.Y, v.Z, true)).ToArray();
            Camera = new Camera(keyboardControl, mouseControl);
            Camera.Focused = false;
            Camera.Position = new Vector3();

            var vertexArray = Vertices.Select(x => x.ToVertex).ToArray();

            vertexBuffer = Buffer.Create(device, BindFlags.VertexBuffer, vertexArray);
            vertexBinding = new VertexBufferBinding(vertexBuffer, Utilities.SizeOf<Vertex>(), 0);

            indexBuffer = Buffer.Create(device, BindFlags.IndexBuffer, new ushort[]
            {
                0, 1,
                0, 2,
                0, 4,
                1, 3,
                1, 5,
                2, 3,
                2, 6,
                3, 7,
                4, 5,
                4, 6,
                5, 7,
                6, 7,
            });
        }

        public void Render(DeviceContext context, VSBuffer cpuBuffer, Buffer vsBuffer)
        {
            if (!Draw)
                return;

            context.UpdateSubresource(ref cpuBuffer, vsBuffer);
            Camera.UpdatePosition();
            var matrix = Camera.InverseViewMatrix;

            for(int i=0;i<Vertices.Length;i++)
            {
                var vec = Vector4.Transform(baseCubePositions[i], matrix);
                Vertices[i].Position = new Vector3(vec.X, vec.Y, vec.Z);
            }

            var vertexArray = Vertices.Select(x => x.ToVertex).ToArray();

            context.UpdateSubresource(vertexArray, vertexBuffer);

            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineList;
            context.InputAssembler.SetIndexBuffer(indexBuffer, Format.R16_UInt, 0);
            context.InputAssembler.SetVertexBuffers(0, vertexBinding);
            context.DrawIndexed(24, 0, 0);
        }
    }
}
