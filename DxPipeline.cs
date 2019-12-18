using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.D3DCompiler;
using Device = SharpDX.Direct3D11.Device;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace SoftBody
{
    public class DxPipeline
    {
        public Camera Camera { get; }
        public Camera ControlCubeCamera => cube.Camera;

        Device device;
        DeviceContext context;
        SwapChain swapChain;
        RenderTargetView renderTargetView;
        DepthStencilView DepthStencilView;
        Viewport viewport;
        Buffer vsBuffer;
        Buffer psBuffer;

        ControlCube cube;
        BezierCube bezierCube;
        Sphere sphere;
        BoundingBox boundingBox;

        DateTime lastComputeTime;

        public DxPipeline(Control control, Form1 form)
        {
            Camera = new Camera(form, control);

            InitializeSwapChain(control);

            viewport = new Viewport(0, 0, control.Width, control.Height, 0, 1);
            context.Rasterizer.SetViewport(viewport);

            InitializeDepthStencil(control);
            InitializeDepthStencilState();
            InitializeShaders();

            vsBuffer = new Buffer(device, Utilities.SizeOf<VSBuffer>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
            psBuffer = new Buffer(device, Utilities.SizeOf<PSBuffer>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);

            context.VertexShader.SetConstantBuffer(0, vsBuffer);
            context.PixelShader.SetConstantBuffer(0, psBuffer);

            cube = new ControlCube(device, form, control);
            bezierCube = new BezierCube(cube, device);
            boundingBox = new BoundingBox(device);
            sphere = new Sphere(device, bezierCube.bezierPoints);
            lastComputeTime = DateTime.Now;
        }

        public void Disturb() => bezierCube.Disturb();

        private void InitializeDepthStencilState()
        {
            context.OutputMerger.DepthStencilState = new DepthStencilState(device, new DepthStencilStateDescription()
            {
                IsDepthEnabled = true,
                DepthComparison = Comparison.Less,
                DepthWriteMask = DepthWriteMask.All,
                IsStencilEnabled = false,
                StencilReadMask = 0xff, // 0xff (no mask)    
                StencilWriteMask = 0xff,// 0xff (no mask)    
                FrontFace = new DepthStencilOperationDescription()
                {
                    Comparison = Comparison.Always,
                    PassOperation = StencilOperation.Keep,
                    FailOperation = StencilOperation.Keep,
                    DepthFailOperation = StencilOperation.Increment
                },
                BackFace = new DepthStencilOperationDescription()
                {
                    Comparison = Comparison.Always,
                    PassOperation = StencilOperation.Keep,
                    FailOperation = StencilOperation.Keep,
                    DepthFailOperation = StencilOperation.Decrement
                }
            });
        }

        private void InitializeSwapChain(Control control)
        {
            SwapChainDescription swapChainDescription = new SwapChainDescription()
            {
                BufferCount = 1,//how many buffers are used for writing. it's recommended to have at least 2 buffers but this is an example
                Flags = SwapChainFlags.None,
                IsWindowed = true,
                ModeDescription = new ModeDescription(control.ClientSize.Width, control.ClientSize.Height, new Rational(60, 1), Format.R8G8B8A8_UNorm),
                OutputHandle = control.Handle,
                SampleDescription = new SampleDescription(1, 0), //the first number is how many samples to take, anything above one is multisampling.
                Usage = Usage.RenderTargetOutput,
                SwapEffect = SwapEffect.Discard,
            };

            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.None, swapChainDescription, out device, out swapChain);

            context = device.ImmediateContext;
        }

        void InitializeDepthStencil(Control control)
        {
            var DepthBuffer = new Texture2D(device, new Texture2DDescription()
            {
                Format = Format.D24_UNorm_S8_UInt,
                ArraySize = 1,
                MipLevels = 1,
                Width = control.Width,
                Height = control.Height,
                SampleDescription = swapChain.Description.SampleDescription,
                BindFlags = BindFlags.DepthStencil,
            });

            DepthStencilView = new DepthStencilView(device, DepthBuffer, new DepthStencilViewDescription()
            {
                Dimension = (swapChain.Description.SampleDescription.Count > 1 || swapChain.Description.SampleDescription.Quality > 0) ?
                DepthStencilViewDimension.Texture2DMultisampled : DepthStencilViewDimension.Texture2D
            });

            using (Texture2D backBuffer = swapChain.GetBackBuffer<Texture2D>(0))
            {
                renderTargetView = new RenderTargetView(device, backBuffer);
            }

            context.OutputMerger.SetTargets(DepthStencilView, renderTargetView);
        }

        private void InitializeShaders()
        {
            ShaderSignature inputSignature;
            VertexShader vertexShader;
            PixelShader pixelShader;

            using (var vertexShaderByteCode = ShaderBytecode.CompileFromFile("../../vertexShader.hlsl", "main", "vs_5_0", ShaderFlags.Debug))
            {
                inputSignature = ShaderSignature.GetInputSignature(vertexShaderByteCode);
                vertexShader = new VertexShader(device, vertexShaderByteCode);
            }

            using (var pixelShaderByteCode = ShaderBytecode.CompileFromFile("../../pixelShader.hlsl", "main", "ps_5_0", ShaderFlags.Debug))
            {
                pixelShader = new PixelShader(device, pixelShaderByteCode);
            }

            context.VertexShader.Set(vertexShader);
            context.PixelShader.Set(pixelShader);
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.InputAssembler.InputLayout = new InputLayout(device, inputSignature, new InputElement[]
            {
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0),
                new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0),
            });
        }

        public void Redraw()
        {
            context.ClearDepthStencilView(DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);
            context.ClearRenderTargetView(renderTargetView, Color.White);

            var psBuffer = new PSBuffer();
            psBuffer.SurfaceColor = new Vector4(0, 0, 1, 1);
            psBuffer.LightPosition = new Vector4(0, 100, 0, 1);
            context.UpdateSubresource(ref psBuffer, this.psBuffer);

            Camera.UpdatePosition();
            var vsBuffer = new VSBuffer();
            vsBuffer.View = Camera.ViewMatrix;
            vsBuffer.Projection = Matrix.PerspectiveFovLH((float)Math.PI / 3f, viewport.Width / (float)viewport.Height, 0.5f, 100f);
            vsBuffer.InvView = Camera.InverseViewMatrix;
            vsBuffer.World = Matrix.Identity;

            cube.Render(context, vsBuffer, this.vsBuffer);

            var now = DateTime.Now;
            var timeDiff = now - lastComputeTime;
            lastComputeTime = now;

            bezierCube.Update((float)timeDiff.TotalMilliseconds / 1000);

            bezierCube.Render(context, vsBuffer, this.vsBuffer);
            sphere.Render(context, vsBuffer, this.vsBuffer);
            boundingBox.Render(context, vsBuffer, this.vsBuffer);
            swapChain.Present(1, PresentFlags.None);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VSBuffer
    {
        public Matrix World;
        public Matrix View;
        public Matrix InvView;
        public Matrix Projection;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PSBuffer
    {
        public Vector4 SurfaceColor;
        public Vector4 LightPosition;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        public readonly Vector3 Position;
        public Vector3 Normal;

        public Vertex(Vector3 position, Vector3 normal)
        {
            Position = position;
            Normal = normal;
        }

        public Vertex(Vector3 position) : this(position, Vector3.Normalize(new Vector3(position.X, position.Y, position.Z))) { }
        public Vertex(float x, float y, float z, Vector3 normal) : this(new Vector3(x, y, z), normal) { }
        public Vertex(float x, float y, float z) : this(new Vector3(x, y, z), Vector3.Normalize(new Vector3(x, y, z))) { }

        public override string ToString()
        {
            return $"{Position}, {Normal}";
        }
    }
}
