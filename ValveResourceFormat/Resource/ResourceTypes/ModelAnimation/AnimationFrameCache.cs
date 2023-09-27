using System.Diagnostics;
using System.Numerics;

namespace ValveResourceFormat.ResourceTypes.ModelAnimation
{
    public class AnimationFrameCache
    {
        private Frame PrevFrame;
        private Frame NextFrame;
        private readonly Frame InterpolatedFrame;
        public Skeleton Skeleton { get; }

        public AnimationFrameCache(Skeleton skeleton)
        {
            PrevFrame = new Frame(skeleton);
            NextFrame = new Frame(skeleton);
            InterpolatedFrame = new Frame(skeleton);
            Skeleton = skeleton;
            Clear();
        }

        /// <summary>
        /// Clears interpolated frame bones and frame cache.
        /// Should be used on animation change.
        /// </summary>
        public void Clear()
        {
            PrevFrame.Clear(Skeleton);
            NextFrame.Clear(Skeleton);
        }

        /// <summary>
        /// Get the animation frame at a time.
        /// </summary>
        /// <param name="time">The time to get the frame for.</param>
        public Frame GetInterpolatedFrame(Animation anim, float time)
        {
            // Calculate the index of the current frame
            var frameIndex = (int)(time * anim.Fps) % anim.FrameCount;
            var nextFrameIndex = (frameIndex + 1) % anim.FrameCount;
            var t = ((time * anim.Fps) - frameIndex) % 1;

            // Get current and next frame
            var frame1 = GetFrame(anim, frameIndex);
            var frame2 = GetFrame(anim, nextFrameIndex);

            // Make sure second GetFrame call didn't return incorrect instance
            Debug.Assert(frame1.FrameIndex == frameIndex);
            Debug.Assert(frame2.FrameIndex == nextFrameIndex);

            // Interpolate bone positions, angles and scale
            for (var i = 0; i < frame1.Bones.Length; i++)
            {
                var frame1Bone = frame1.Bones[i];
                var frame2Bone = frame2.Bones[i];
                InterpolatedFrame.Bones[i].Position = Vector3.Lerp(frame1Bone.Position, frame2Bone.Position, t);
                InterpolatedFrame.Bones[i].Angle = Quaternion.Slerp(frame1Bone.Angle, frame2Bone.Angle, t);
                InterpolatedFrame.Bones[i].Scale = frame1Bone.Scale + (frame2Bone.Scale - frame1Bone.Scale) * t;
            }

            return InterpolatedFrame;
        }

        /// <summary>
        /// Get the animation frame at a given index.
        /// </summary>
        public Frame GetFrame(Animation anim, int frameIndex)
        {
            // Try to lookup cached (precomputed) frame - happens when GUI Autoplay runs faster than animation FPS
            if (frameIndex == PrevFrame.FrameIndex)
            {
                return PrevFrame;
            }
            if (frameIndex == NextFrame.FrameIndex)
            {
                return NextFrame;
            }

            // Only two frames are cached at a time to minimize memory usage, especially with Autoplay enabled
            Frame frame;
            if (frameIndex == (NextFrame.FrameIndex + 1) % anim.FrameCount)
            {
                frame = PrevFrame;
                PrevFrame = NextFrame;
                NextFrame = frame;
            }
            else
            {
                frame = NextFrame;
                NextFrame = PrevFrame;
                PrevFrame = frame;
            }

            // We make an assumption that frames within one animation
            // contain identical bone sets, so we don't clear frame here
            frame.FrameIndex = frameIndex;
            anim.DecodeFrame(frame);

            return frame;
        }
    }
}
