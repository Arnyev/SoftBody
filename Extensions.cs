namespace SoftBody
{
    public static class Extensions
    {
        public static T[] Flatten<T>(this T[,,] multiDim)
        {
            var array = new T[multiDim.Length];
            int ind = 0;

            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    for (int k = 0; k < 4; k++)
                        array[ind++] = multiDim[i, j, k];

            return array;
        }
    }
}
