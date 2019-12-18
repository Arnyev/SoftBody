using SharpDX;

namespace SoftBody
{

    public class Spring
    {
        public static float Mass = 1;
        public static float Elasticity = 3;
        public static float ElasticityControl = 130;
        public static float Viscosity = 1f;

        private readonly BezierCubeVertex VertexA;
        private readonly BezierCubeVertex VertexB;
        private readonly float StartingDistance;

        public Spring(BezierCubeVertex vertexA, BezierCubeVertex vertexB, float startingDistance)
        {
            VertexA = vertexA;
            VertexB = vertexB;
            StartingDistance = startingDistance;
        }

        public void Update()
        {
            var positionDiff = VertexB.Position - VertexA.Position; // od A do B
            var positionDiffNorm = Vector3.Normalize(positionDiff);
            var dist = positionDiff.Length();
            var anyStatic = VertexA.IsStatic | VertexB.IsStatic;
            var elasticity = anyStatic ? ElasticityControl : Elasticity;
            var elasticForce = elasticity * (dist - StartingDistance);

            var relativeVelocity = VertexA.Velocity - VertexB.Velocity;
            var relativeVelocityInDir = Vector3.Dot(relativeVelocity, positionDiffNorm);// jeśli się zbliżają to dodatnie
            var viscousForce = Viscosity * relativeVelocityInDir;
            var force = elasticForce - viscousForce;

            VertexA.Acceleration += force / Mass * positionDiffNorm;
            VertexB.Acceleration -= force / Mass * positionDiffNorm;

            //var elasticForceDir = elasticForce * positionDiffNorm;
            //var viscousForceDirA = -Viscosity * VertexA.Velocity;
            //var viscousForceDirB = -Viscosity * VertexB.Velocity;
            //VertexA.Acceleration += elasticForceDir / Mass + viscousForceDirA / Mass;
            //VertexB.Acceleration += -elasticForceDir / Mass + viscousForceDirB / Mass;
        }
    }
}
