using System;
using System.Collections.Generic;
using UnityEngine;

namespace Camerafy.Camera
{
    /// <summary>
    /// A play mode for an animation.
    /// </summary>
    public enum AnimationPlayMode
    {
        /// <summary>
        /// Animation will be played one time only.
        /// </summary>
        Once,

        /// <summary>
        /// Animation will be repeated after finished.
        /// </summary>
        Loop,

        /// <summary>
        /// Animation will be played forth and and back.
        /// </summary>
        PingPong
    }

    [Serializable]
    public class Animation
    {
        /// <summary>
        /// Animation name.
        /// </summary>
        public String Name = "";

        /// <summary>
        /// List of all keyframes.
        /// </summary>        
        public List<Snapshot> Snapshots = new List<Snapshot>();

        /// <summary>
        /// A list holding all sub-distances, the distance between Snapshots.
        /// The first sub-distance corresponse to the distance between Snapshots one and two.
        /// </summary>
        public List<float> AccumulatedSubDistances = new List<float>();

        /// <summary>
        /// The total distance from first to last Snapshot
        /// </summary>
        public float TotalDistance = 0.0f;

        /// <summary>
        /// The total duration it takes to travel the 'TotalDistance'
        /// </summary>
        public float TotalDuration = 0.0f;

        /// <summary>
        /// Flag is true if animation should play reversed.
        /// </summary>
        public bool Reversed = false;

        /// <summary>
        /// The mode this animation will be played.
        /// </summary>
        public AnimationPlayMode Mode = AnimationPlayMode.Once;

        public Animation()
        { }

        public void AddSnapshot(Snapshot frame)
        {
            this.Snapshots.Add(frame);
            this.TotalDistance = this.Initialize();
        }

        public void AddSnapshots(IEnumerable<Snapshot> frames)
        {
            this.Snapshots.AddRange(frames);
            this.TotalDistance = this.Initialize();
        }

        public Snapshot GetFrameByTime(float time)
        {
            float progress = time / this.TotalDuration;
            return this.GetFrameByProgress(progress);
        }

        public Snapshot GetFrameByProgress(float progress)
        {
            float distance = this.TotalDistance * Mathf.Clamp01(progress);
            int frameIndex;
            float sub0 = 0.0f;
            float sub1 = 0.0f;

            for (frameIndex = 0; frameIndex < this.AccumulatedSubDistances.Count; ++frameIndex)
            {
                sub1 = this.AccumulatedSubDistances[frameIndex];

                if (sub1 >= distance)
                {
                    break;
                }
                else
                {
                    sub0 = this.AccumulatedSubDistances[frameIndex];
                }
            }

            Snapshot f0 = this.Snapshots[frameIndex];
            Snapshot f1 = this.Snapshots[frameIndex + 1];

            // progress inbetween Snapshots
            float subProgress = Mathf.Max(0.0f, distance - sub0) / Mathf.Max(float.Epsilon, sub1 - sub0);

            // return interpolated frame
            return Snapshot.Lerp(f0, f1, subProgress);
        }

        /// <summary>
        /// Called internally to pre-compute distance and accumulated distance values.
        /// </summary>
        /// <returns></returns>
        private float Initialize()
        {
            float distance = 0.0f;
            this.AccumulatedSubDistances.Clear();

            for (int i = 0; i < this.Snapshots.Count - 1; ++i)
            {
                float subDistance = (this.Snapshots[i].Position - this.Snapshots[i + 1].Position).magnitude;
                this.AccumulatedSubDistances.Add(distance + subDistance);
                distance += subDistance;
            }

            return distance;
        }

        /// <summary>
        /// Removes a snapshot from this animation.
        /// </summary>
        /// <param name="snapshot"></param>
        public void RemoveSnapshot(Snapshot snapshot)
        {
            this.Snapshots.Remove(snapshot);
            this.TotalDistance = this.Initialize();
        }

        /// <summary>
        /// Check if animation is valid.
        /// </summary>
        /// <returns></returns>
        public bool IsValid()
        {
            return (Mathf.Abs(this.TotalDuration) > 1e-5f);
        }
    }
}
