using SharpDX;
using System;
using Device = SharpDX.Direct3D11.Device;
using Buffer = SharpDX.Direct3D11.Buffer;
using System.Linq;
using SharpDX.Direct3D11;
using SharpDX.Direct3D;
using SharpDX.DXGI;

namespace SoftBody
{
    class Sphere
    {
        public static bool Draw { get; set; } = true;
        const int SquareDivision = 60;
        private readonly BezierCubeVertex[,,] bezierPoints;

        Buffer vertexBuffer;
        VertexBufferBinding vertexBinding;

        uint[] triangleIndicesArray;
        Buffer triangleIndices;
        Vertex[] triangleVertices;

        public Sphere(Device device, BezierCubeVertex[,,] bezierPoints)
        {
            this.bezierPoints = bezierPoints;

            vertexBuffer = Buffer.Create(device, BindFlags.VertexBuffer, GetTriangleVertices());
            vertexBinding = new VertexBufferBinding(vertexBuffer, Utilities.SizeOf<Vertex>(), 0);
            triangleVertices = GetTriangleVertices();
            triangleIndicesArray = GetIndices();
            triangleIndices = Buffer.Create(device, BindFlags.IndexBuffer, triangleIndicesArray);
        }

        public void Render(DeviceContext context, VSBuffer cpuBuffer, Buffer vsBuffer)
        {
            if (!Draw)
                return;

            context.UpdateSubresource(ref cpuBuffer, vsBuffer);

            context.UpdateSubresource(triangleVertices, vertexBuffer);

            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.InputAssembler.SetIndexBuffer(triangleIndices, Format.R32_UInt, 0);
            context.InputAssembler.SetVertexBuffers(0, vertexBinding);
            context.DrawIndexed(triangleIndicesArray.Length, 0, 0);
        }

        private float GetBernsteinValue(int index, float t)
        {
            switch (index)
            {
                case 0:
                    return (1.0f - t) * (1.0f - t) * (1.0f - t);
                case 1:
                    return 3 * t * (1.0f - t) * (1.0f - t);
                case 2:
                    return 3 * t * t * (1.0f - t);
                case 3:
                    return t * t * t;
            }

            return 0;
        }

        public Vertex[] GetTriangleVertices()
        {
            uint vertexCount = SquareDivision * SquareDivision;
            var vertices = new Vertex[vertexCount];

            var sinuses = Enumerable.Range(0, SquareDivision).Select(x => (float)Math.Sin(x * 2 * Math.PI / SquareDivision)).ToArray();
            var cosinuses = Enumerable.Range(0, SquareDivision).Select(x => (float)Math.Cos(x * 2 * Math.PI / SquareDivision)).ToArray();

            for (uint indexU = 0; indexU < SquareDivision; indexU++)
            {
                var y = sinuses[indexU] / 2 + 0.5f;
                var cosa = cosinuses[indexU];

                for (uint indexV = 0; indexV < SquareDivision; indexV++)
                {
                    var z = cosa * cosinuses[indexV] / 2 + 0.5f;
                    var x = cosa * sinuses[indexV] / 2 + 0.5f;
                    vertices[indexU * SquareDivision + indexV] = 
                        new Vertex(new Vector3(x, y, z), Vector3.Normalize(new Vector3(x-0.5f, y - 0.5f, z - 0.5f)));
                }
            }

            return vertices;
        }

        public uint[] GetIndices()
        {
            uint vertexCount = SquareDivision * SquareDivision;
            var indices = new uint[vertexCount * 6];
            uint indexInTriangleIndices = 0;
            for (uint indexU = 0; indexU < SquareDivision; indexU++)
                for (uint indexV = 0; indexV < SquareDivision; indexV++)
                {
                    var ind = indexU * SquareDivision + indexV;
                    var ind2 = (ind + 1) % vertexCount;
                    var ind3 = (ind + SquareDivision) % vertexCount;
                    var ind4 = (ind + SquareDivision + 1) % vertexCount;

                    indices[indexInTriangleIndices++] = ind;
                    indices[indexInTriangleIndices++] = ind2;
                    indices[indexInTriangleIndices++] = ind3;

                    indices[indexInTriangleIndices++] = ind2;
                    indices[indexInTriangleIndices++] = ind4;
                    indices[indexInTriangleIndices++] = ind3;
                }

            return indices;
        }
    }
}
