using SharpDX;

namespace SoftBody
{
    public class BezierCubeVertex
    {
        public static float CollisionEllasticity { get; set; } = 1;

        public Vector3 Position;
        public Vector3 Velocity;
        public Vector3 Acceleration;
        public readonly bool IsStatic;

        public BezierCubeVertex(float x, float y, float z, bool isStatic = false)
        {
            Position = new Vector3(x, y, z);
            IsStatic = isStatic;
        }

        public void Update(float dt)
        {
            if (IsStatic)
                return;

            Velocity += Acceleration * dt;

            const float Max = BoundingBox.BoundingBoxSize;

            if ((Position.X > Max && Velocity.X > 0) || (Position.X < -Max && Velocity.X < 0))
                Velocity.X = -Velocity.X * CollisionEllasticity;

            if ((Position.Y > Max && Velocity.Y > 0) || (Position.Y < -Max && Velocity.Y < 0))
                Velocity.Y = -Velocity.Y * CollisionEllasticity;

            if ((Position.Z > Max && Velocity.Z > 0) || (Position.Z < -Max && Velocity.Z < 0))
                Velocity.Z = -Velocity.Z * CollisionEllasticity;

            Position += Velocity * dt;

            if (Position.X > Max)
                Position.X = Max;

            if (Position.X < -Max)
                Position.X = -Max;

            if (Position.Y > Max)
                Position.Y = Max;

            if (Position.Y < -Max)
                Position.Y = -Max;

            if (Position.Z > Max)
                Position.Z = Max;

            if (Position.Z < -Max)
                Position.Z = -Max;
        }

        public Vertex ToVertex => new Vertex(Position);
    }
}
