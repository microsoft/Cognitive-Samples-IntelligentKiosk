
using System.Collections.Generic;
using Windows.Foundation.Collections;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;

namespace VideoRuntimeComponent
{
    //INST new class created in specific project as described in https://docs.microsoft.com/en-us/answers/questions/591175/how-do-i-fix-the-34class-not-registered34-error-us.html
    public sealed class FrameRelayVideoEffect : IBasicVideoEffect
    {
        public static SoftwareBitmap LatestSoftwareBitmap { get; private set; }

        public bool IsReadOnly
        {
            get
            {
                return true;
            }
        }

        public IReadOnlyList<VideoEncodingProperties> SupportedEncodingProperties
        {
            get
            {
                return new List<VideoEncodingProperties>();
            }
        }

        public MediaMemoryTypes SupportedMemoryTypes
        {
            get
            {
                return MediaMemoryTypes.Cpu;
            }
        }

        public bool TimeIndependent
        {
            get
            {
                return true;
            }
        }

        public void Close(MediaEffectClosedReason reason)
        {
        }

        public void DiscardQueuedFrames()
        {
            LatestSoftwareBitmap = null;
        }

        public static void ResetState()
        {
            LatestSoftwareBitmap = null;
        }

        public void ProcessFrame(ProcessVideoFrameContext context)
        {
            LatestSoftwareBitmap = context.InputFrame.SoftwareBitmap;
        }

        public void SetEncodingProperties(VideoEncodingProperties encodingProperties, IDirect3DDevice device)
        {
        }

        public void SetProperties(IPropertySet configuration)
        {
        }
    }
}
