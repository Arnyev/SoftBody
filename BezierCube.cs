using SharpDX;
using System;
using System.Collections.Generic;
using Device = SharpDX.Direct3D11.Device;
using Buffer = SharpDX.Direct3D11.Buffer;
using System.Linq;
using SharpDX.Direct3D11;
using SharpDX.Direct3D;
using SharpDX.DXGI;

namespace SoftBody
{
    public class BezierCube
    {
        public static bool DrawCube { get; set; }
        public static bool DrawSprings { get; set; } = true;
        public static float DeformationConstant { get; set; } = 1;


        const int SquareDivision = 12;
        public readonly BezierCubeVertex[,,] bezierPoints = new BezierCubeVertex[4, 4, 4];
        private readonly List<Spring> springs = new List<Spring>();

        Buffer vertexBuffer;
        Buffer springBuffer;

        VertexBufferBinding vertexBinding;
        VertexBufferBinding springBinding;

        uint[] triangleIndicesArray;
        uint[] springIndicesArray;

        Buffer triangleIndices;
        Buffer springIndices;

        public BezierCube(ControlCube controlCube, Device device)
        {
            var minPoint = controlCube.Vertices[0].Position;
            var maxPoint = controlCube.Vertices[7].Position;
            var diff = maxPoint - minPoint;
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    for (int k = 0; k < 4; k++)
                        bezierPoints[i, j, k] = new BezierCubeVertex(minPoint.X + i / 3.0f * diff.X, minPoint.Y + j / 3.0f * diff.Y, minPoint.Z + k / 3.0f * diff.Z);

            CreateInnerSprings(diff);
            CreateControlSprings(controlCube);

            vertexBuffer = Buffer.Create(device, BindFlags.VertexBuffer, GetAllTriangleVertices());
            vertexBinding = new VertexBufferBinding(vertexBuffer, Utilities.SizeOf<Vertex>(), 0);

            springBuffer = Buffer.Create(device, BindFlags.VertexBuffer, GetBezierVertices());
            springBinding = new VertexBufferBinding(springBuffer, Utilities.SizeOf<Vertex>(), 0);

            triangleIndicesArray = GetIndices();
            triangleIndices = Buffer.Create(device, BindFlags.IndexBuffer, triangleIndicesArray);

            springIndicesArray = GetSpringIndices();
            springIndices = Buffer.Create(device, BindFlags.IndexBuffer, springIndicesArray);
        }

        private Vertex[] GetBezierVertices() => bezierPoints.Flatten().Select(x => x.ToVertex).ToArray();

        public void Disturb()
        {
            var rand = new Random();
            foreach (var point in bezierPoints.Flatten())
                point.Position += DeformationConstant * new Vector3((float)rand.NextDouble() - 0.5f, (float)rand.NextDouble() - 0.5f, (float)rand.NextDouble() - 0.5f);
        }

        public void Render(DeviceContext context, VSBuffer cpuBuffer, Buffer vsBuffer)
        {
            context.UpdateSubresource(ref cpuBuffer, vsBuffer);

            context.UpdateSubresource(GetAllTriangleVertices(), vertexBuffer);
            context.UpdateSubresource(GetBezierVertices(), springBuffer);

            if (DrawCube)
            {
                context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
                context.InputAssembler.SetIndexBuffer(triangleIndices, Format.R32_UInt, 0);
                context.InputAssembler.SetVertexBuffers(0, vertexBinding);
                context.DrawIndexed(triangleIndicesArray.Length, 0, 0);
            }
            if(DrawSprings)
            {
                context.InputAssembler.PrimitiveTopology = PrimitiveTopology.LineList;
                context.InputAssembler.SetIndexBuffer(springIndices, Format.R32_UInt, 0);
                context.InputAssembler.SetVertexBuffers(0, springBinding);
                context.DrawIndexed(springIndicesArray.Length, 0, 0);
            }
        }

        private Vertex[] GetAllTriangleVertices()
        {
            var bezierArray = bezierPoints.Flatten().Select(x => x.Position).ToArray();
            var vertexArray1 = GetTriangleVertices(bezierArray.Take(16).ToArray());
            var vertexArray2 = GetTriangleVertices(bezierArray.Skip(48).ToArray());

            var vertexArray3 = GetTriangleVertices(bezierArray.Where((_, i) => i % 4 == 0).ToArray());
            var vertexArray4 = GetTriangleVertices(bezierArray.Where((_, i) => i % 4 == 3).ToArray());

            var vertexArray5 = GetTriangleVertices(bezierArray.Where((_, i) => (i % 16) / 4 == 0).ToArray());
            var vertexArray6 = GetTriangleVertices(bezierArray.Where((_, i) => (i % 16) / 4 == 3).ToArray());

            return vertexArray1.Concat(vertexArray2).Concat(vertexArray3).Concat(vertexArray4).Concat(vertexArray5).Concat(vertexArray6).ToArray();
        }

        private void CreateControlSprings(ControlCube controlCube)
        {
            var controlPoints = controlCube.Vertices;
            springs.Add(new Spring(bezierPoints[0, 0, 0], controlPoints[0], 0));
            springs.Add(new Spring(bezierPoints[0, 0, 3], controlPoints[1], 0));
            springs.Add(new Spring(bezierPoints[0, 3, 0], controlPoints[2], 0));
            springs.Add(new Spring(bezierPoints[0, 3, 3], controlPoints[3], 0));
            springs.Add(new Spring(bezierPoints[3, 0, 0], controlPoints[4], 0));
            springs.Add(new Spring(bezierPoints[3, 0, 3], controlPoints[5], 0));
            springs.Add(new Spring(bezierPoints[3, 3, 0], controlPoints[6], 0));
            springs.Add(new Spring(bezierPoints[3, 3, 3], controlPoints[7], 0));
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

        public Vertex[] GetTriangleVertices(Vector3[] bezierPoints)
        {
            var vertexCount = SquareDivision * SquareDivision;
            var vertices = new Vertex[vertexCount];

            var bernsteinValuesU = new float[SquareDivision, 4];
            var bernsteinValuesV = new float[SquareDivision, 4];

            for (int i = 0; i < SquareDivision; i++)
            {
                var u = i * 1.0f / (SquareDivision - 1);
                for (int j = 0; j < 4; j++)
                    bernsteinValuesU[i, j] = GetBernsteinValue(j, u);
            }

            for (int i = 0; i < SquareDivision; i++)
            {
                var v = i * 1.0f / (SquareDivision - 1);
                for (int j = 0; j < 4; j++)
                    bernsteinValuesV[i, j] = GetBernsteinValue(j, v);
            }

            for (int indexU = 0; indexU < SquareDivision; indexU++)
                for (int indexV = 0; indexV < SquareDivision; indexV++)
                {
                    var point = new Vector3();
                    for (int i = 0; i < 4; i++)
                        for (int j = 0; j < 4; j++)
                            point += bezierPoints[i * 4 + j] * bernsteinValuesU[indexU, i] * bernsteinValuesV[indexV, j];

                    vertices[indexU * SquareDivision + indexV] = new Vertex(point);
                }

            for (int indexU = 0; indexU < SquareDivision - 1; indexU++)
                for (int indexV = 0; indexV < SquareDivision - 1; indexV++)
                {
                    var ind = indexU * SquareDivision + indexV;
                    var v1 = vertices[ind].Position;
                    var v2 = vertices[ind + 1].Position;
                    var v3 = vertices[ind + SquareDivision].Position;
                    var d1 = v2 - v1;
                    var d2 = v3 - v1;
                    var normal = Vector3.Normalize(Vector3.Cross(d1, d2));

                    vertices[indexU * SquareDivision + indexV].Normal = normal;
                }

            return vertices;
        }

        public uint[] GetIndices()
        {
            uint singlePackVertexCount = SquareDivision * SquareDivision;
            var indices = new uint[(SquareDivision - 1) * (SquareDivision - 1) * 36];
            uint indexInTriangleIndices = 0;
            for (uint i = 0; i < 6; i++)
                for (uint indexU = 0; indexU < SquareDivision - 1; indexU++)
                    for (uint indexV = 0; indexV < SquareDivision - 1; indexV++)
                    {
                        var ind = indexU * SquareDivision + indexV + singlePackVertexCount * i;
                        bool isReverse = i == 1 || i == 3 || i == 4;
                        var ind2 = isReverse ? ind + SquareDivision : ind + 1;
                        var ind3 = isReverse ? ind + 1 : ind + SquareDivision;
                        var ind4 = ind + SquareDivision + 1;

                        indices[indexInTriangleIndices++] = ind;
                        indices[indexInTriangleIndices++] = ind2;
                        indices[indexInTriangleIndices++] = ind3;

                        indices[indexInTriangleIndices++] = ind2;
                        indices[indexInTriangleIndices++] = ind4;
                        indices[indexInTriangleIndices++] = ind3;
                    }

            return indices;
        }

        private void CreateInnerSprings(Vector3 diff)
        {
            var diffSmall = (float)(diff.Length() / (3.0 * Math.Sqrt(3)));
            var diffBig = (float)(diffSmall * Math.Sqrt(2));
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    for (int k = 0; k < 3; k++)
                        springs.Add(new Spring(bezierPoints[i, j, k], bezierPoints[i, j, k + 1], diffSmall));

            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 3; j++)
                    for (int k = 0; k < 4; k++)
                        springs.Add(new Spring(bezierPoints[i, j, k], bezierPoints[i, j + 1, k], diffSmall));

            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 4; j++)
                    for (int k = 0; k < 4; k++)
                        springs.Add(new Spring(bezierPoints[i, j, k], bezierPoints[i + 1, j, k], diffSmall));

            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    for (int k = 0; k < 4; k++)
                    {
                        springs.Add(new Spring(bezierPoints[i, j, k], bezierPoints[i + 1, j + 1, k], diffBig));
                        springs.Add(new Spring(bezierPoints[i, j + 1, k], bezierPoints[i + 1, j, k], diffBig));
                    }

            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 4; j++)
                    for (int k = 0; k < 3; k++)
                    {
                        springs.Add(new Spring(bezierPoints[i, j, k], bezierPoints[i + 1, j, k + 1], diffBig));
                        springs.Add(new Spring(bezierPoints[i, j, k + 1], bezierPoints[i + 1, j, k], diffBig));
                    }

            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 3; j++)
                    for (int k = 0; k < 3; k++)
                    {
                        springs.Add(new Spring(bezierPoints[i, j, k], bezierPoints[i, j + 1, k + 1], diffBig));
                        springs.Add(new Spring(bezierPoints[i, j, k + 1], bezierPoints[i, j + 1, k], diffBig));
                    }
        }

        private uint[] GetSpringIndices()
        {
            var springIndices = new List<uint>();

            for (uint i = 0; i < 4; i++)
                for (uint j = 0; j < 4; j++)
                    for (uint k = 0; k < 3; k++)
                    {
                        var ind = 16 * i + 4 * j + k;
                        springIndices.Add(ind);
                        springIndices.Add(ind + 1);
                    }

            for (uint i = 0; i < 4; i++)
                for (uint j = 0; j < 3; j++)
                    for (uint k = 0; k < 4; k++)
                    {
                        var ind = 16 * i + 4 * j + k;
                        springIndices.Add(ind);
                        springIndices.Add(ind + 4);
                    }

            for (uint i = 0; i < 3; i++)
                for (uint j = 0; j < 4; j++)
                    for (uint k = 0; k < 4; k++)
                    {
                        var ind = 16 * i + 4 * j + k;
                        springIndices.Add(ind);
                        springIndices.Add(ind + 16);
                    }

            for (uint i = 0; i < 3; i++)
                for (uint j = 0; j < 3; j++)
                    for (uint k = 0; k < 4; k++)
                    {
                        var ind = 16 * i + 4 * j + k;
                        springIndices.Add(ind);
                        springIndices.Add(ind + 20);
                        springIndices.Add(ind + 4);
                        springIndices.Add(ind + 16);
                    }

            for (uint i = 0; i < 3; i++)
                for (uint j = 0; j < 4; j++)
                    for (uint k = 0; k < 3; k++)
                    {
                        var ind = 16 * i + 4 * j + k;
                        springIndices.Add(ind);
                        springIndices.Add(ind + 17);
                        springIndices.Add(ind + 1);
                        springIndices.Add(ind + 16);
                    }

            for (uint i = 0; i < 4; i++)
                for (uint j = 0; j < 3; j++)
                    for (uint k = 0; k < 3; k++)
                    {
                        var ind = 16 * i + 4 * j + k;
                        springIndices.Add(ind);
                        springIndices.Add(ind + 5);
                        springIndices.Add(ind + 1);
                        springIndices.Add(ind + 4);
                    }

            return springIndices.ToArray();
        }

        public void Update(float dt)
        {
            var points = bezierPoints.Cast<BezierCubeVertex>().ToList();
            points.ForEach(x => x.Acceleration = new Vector3());
            springs.ForEach(x => x.Update());
            points.ForEach(x => x.Update(dt));
        }
    }
}
