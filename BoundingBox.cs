using SharpDX;
using Device = SharpDX.Direct3D11.Device;
using Buffer = SharpDX.Direct3D11.Buffer;
using SharpDX.Direct3D11;
using SharpDX.Direct3D;
using SharpDX.DXGI;

namespace SoftBody
{
    class BoundingBox
    {
        public static bool Draw { get; set; } = true;
        public const float BoundingBoxSize = 10;

        private readonly Vertex[] vertices = new Vertex[]
        {
            new Vertex(-BoundingBoxSize, -BoundingBoxSize, -BoundingBoxSize),
            new Vertex(-BoundingBoxSize, -BoundingBoxSize, BoundingBoxSize),
            new Vertex(-BoundingBoxSize, BoundingBoxSize, -BoundingBoxSize),
            new Vertex(-BoundingBoxSize, BoundingBoxSize, BoundingBoxSize),
            new Vertex(BoundingBoxSize, -BoundingBoxSize, -BoundingBoxSize),
            new Vertex(BoundingBoxSize, -BoundingBoxSize, BoundingBoxSize),
            new Vertex(BoundingBoxSize, BoundingBoxSize, -BoundingBoxSize),
            new Vertex(BoundingBoxSize, BoundingBoxSize, BoundingBoxSize),
        };

        public BezierCubeVertex[] Vertices { get; }

        Buffer vertexBuffer;
        Buffer indexBuffer;
        VertexBufferBinding vertexBinding;

        public BoundingBox(Device device)
        {
            vertexBuffer = Buffer.Create(device, BindFlags.VertexBuffer, vertices);
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
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineList;
            context.InputAssembler.SetIndexBuffer(indexBuffer, Format.R16_UInt, 0);
            context.InputAssembler.SetVertexBuffers(0, vertexBinding);
            context.DrawIndexed(24, 0, 0);
        }
    }
}
