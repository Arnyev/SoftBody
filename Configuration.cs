namespace SoftBody
{
    class Configuration
    {
        public bool DrawSprings
        {
            get => BezierCube.DrawSprings;
            set => BezierCube.DrawSprings = value;
        }

        public bool DrawCube
        {
            get => BezierCube.DrawCube;
            set => BezierCube.DrawCube = value;
        }

        public bool DrawControlCube
        {
            get => ControlCube.Draw;
            set => ControlCube.Draw = value;
        }

        public bool DrawBoundingBox
        {
            get => BoundingBox.Draw;
            set => BoundingBox.Draw = value;
        }

        public bool DrawSphere
        {
            get => Sphere.Draw;
            set => Sphere.Draw = value;
        }

        public float DeformationConstant
        {
            get => BezierCube.DeformationConstant;
            set => BezierCube.DeformationConstant = value;
        }

        public float Mass
        {
            get => Spring.Mass;
            set => Spring.Mass = value;
        }

        public float Elasticity
        {
            get => Spring.Elasticity;
            set => Spring.Elasticity = value;
        }
        public float ElasticityControl
        {
            get => Spring.ElasticityControl;
            set => Spring.ElasticityControl = value;
        }
        public float Viscosity
        {
            get => Spring.Viscosity;
            set => Spring.Viscosity = value;
        }

        public float CollisionEllasticity
        {
            get => BezierCubeVertex.CollisionEllasticity;
            set => BezierCubeVertex.CollisionEllasticity = value;
        }
    }
}
