using SharpDX;
using System;
using Device = SharpDX.Direct3D11.Device;
using Buffer = SharpDX.Direct3D11.Buffer;
using System.Linq;
using SharpDX.Direct3D11;
using SharpDX.Direct3D;
using SharpDX.DXGI;
using Assimp;
using SharpDX.D3DCompiler;

namespace SoftBody
{
	class Mesh
	{
		public static bool Draw { get; set; } = true;
		private readonly BezierCubeVertex[,,] bezierPoints;

		Buffer vertexBuffer;
		VertexBufferBinding vertexBinding;

		uint[] triangleIndicesArray;
		Vertex[] baseVertices;
		Buffer triangleIndices;
		Buffer bezierPointsBuffer;
		VertexShader shader;

		public Mesh(Device device, DeviceContext context, BezierCubeVertex[,,] bezierPoints)
		{
			this.bezierPoints = bezierPoints;

			AssimpContext importer = new AssimpContext();
			var scene = importer.ImportFile("../1.obj", PostProcessPreset.TargetRealTimeMaximumQuality);
			var mesh = scene.Meshes[0];

			triangleIndicesArray = mesh.Faces.SelectMany(x => x.Indices.Select(y => (uint)y)).ToArray();

			triangleIndices = Buffer.Create(device, BindFlags.IndexBuffer, triangleIndicesArray);
			var minx = mesh.Vertices.Min(x => x.X);
			var miny = mesh.Vertices.Min(x => x.Y);
			var minz = mesh.Vertices.Min(x => x.Z);

			var maxx = mesh.Vertices.Max(x => x.X);
			var maxy = mesh.Vertices.Max(x => x.Y);
			var maxz = mesh.Vertices.Max(x => x.Z);

			var minv = new Vector3(minx, miny, minz);
			var maxv = new Vector3(maxx, maxy, maxz);
			baseVertices = mesh.Vertices.Zip(mesh.Normals, (x, y) => new Vertex(Normalize(minv, maxv, x), new Vector3(y.X, y.Y, y.Z))).ToArray();
			var averageX = baseVertices.Select(v => v.Position.X).Average();
			var averageY = baseVertices.Select(v => v.Position.Y).Average();
			var averageZ = baseVertices.Select(v => v.Position.Z).Average();
			var averageV = new Vector3(averageX-0.5f, averageY-0.5f, averageZ-0.5f);

			baseVertices = baseVertices.Select(x => new Vertex(x.Position - averageV, x.Normal)).ToArray();

			bezierPointsBuffer = new Buffer(device, Utilities.SizeOf<Vector4>() * 64, ResourceUsage.Default,
				BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);

			using (var vertexShaderByteCode = ShaderBytecode.CompileFromFile("../../meshVertexShader.hlsl", "main", "vs_5_0", ShaderFlags.None))
			{
				shader = new VertexShader(device, vertexShaderByteCode);
			}

			context.VertexShader.SetConstantBuffer(1, bezierPointsBuffer);

			vertexBuffer = Buffer.Create(device, BindFlags.VertexBuffer, baseVertices);
			vertexBinding = new VertexBufferBinding(vertexBuffer, Utilities.SizeOf<Vertex>(), 0);
		}

		private static Vector3 Normalize(Vector3 min, Vector3 max, Vector3D val)
		{
			var diff = max - min;
			var maxD = new[] { diff.X, diff.Y, diff.Z }.Max();

			var diffv = new Vector3(val.X, val.Y, val.Z) - min;
			return diffv / maxD;
		}

		public void Render(DeviceContext context, VSBuffer cpuBuffer, Buffer vsBuffer)
		{
			context.VertexShader.Set(shader);
			var bezierPointsArray = bezierPoints.Cast<BezierCubeVertex>()
	.Select(x => new Vector4(x.Position, 1)).ToArray();
			context.UpdateSubresource(bezierPointsArray, bezierPointsBuffer);

			if (!Draw)
				return;

			context.UpdateSubresource(ref cpuBuffer, vsBuffer);

			context.UpdateSubresource(baseVertices, vertexBuffer);

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
			var vertices = new Vertex[baseVertices.Length];


			for (int ind = 0; ind < baseVertices.Length; ind++)
			{
				var v = baseVertices[ind];

				var point = new Vector3();
				for (int i = 0; i < 4; i++)
					for (int j = 0; j < 4; j++)
						for (int k = 0; k < 4; k++)
							point += bezierPoints[i, j, k].Position * GetBernsteinValue(i, v.Position.X) * GetBernsteinValue(j, v.Position.Y) * GetBernsteinValue(k, v.Position.Z);

				var small = v.Position - 0.01f * v.Normal;
				var dpoint = new Vector3();
				for (int i = 0; i < 4; i++)
					for (int j = 0; j < 4; j++)
						for (int k = 0; k < 4; k++)
							dpoint += bezierPoints[i, j, k].Position * GetBernsteinValue(i, small.X) * GetBernsteinValue(j, small.Y) * GetBernsteinValue(k, small.Z);

				vertices[ind] = new Vertex(point, Vector3.Normalize(point - dpoint));
			}

			return vertices;
		}
	}
}
