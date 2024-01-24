using UnityEngine;

namespace overload
{
    public struct InstanceData
    {
        public Matrix4x4 Matrix;
        public Matrix4x4 MatrixInverse;

        public static int Size()
        {
            return sizeof(float) * 4 * 4
                 + sizeof(float) * 4 * 4;
        }
    }
}
