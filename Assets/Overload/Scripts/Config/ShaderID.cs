using UnityEngine;

namespace overload
{
    public static class ShaderID
    {
        public static readonly int _oceanCenter = Shader.PropertyToID("_oceanCenter");
        public static readonly int _oceanDimension = Shader.PropertyToID("_oceanDimension");
        public static readonly int _oceanUnitSize = Shader.PropertyToID("_oceanUnitSize");
        public static readonly int _oceanMaxHeight = Shader.PropertyToID("_oceanMaxHeight");
        public static readonly int _oceanWaveSpeed = Shader.PropertyToID("_oceanWaveSpeed");
        public static readonly int _oceanWaveFrequency = Shader.PropertyToID("_oceanWaveFrequency");
        public static readonly int _oceanFluxOffset = Shader.PropertyToID("_oceanFluxOffset");
        public static readonly int _oceanModelMatrices = Shader.PropertyToID("_oceanModelMatrices");

        public static readonly int _cameraVP = Shader.PropertyToID("_cameraVP");
        public static readonly int _time = Shader.PropertyToID("_time");
        public static readonly int _perInstanceData = Shader.PropertyToID("_perInstanceData");
        public static readonly int _indirectArgs = Shader.PropertyToID("_indirectArgs");
        public static readonly int _audioSpectrum = Shader.PropertyToID("_audioSpectrum");
    }
}
