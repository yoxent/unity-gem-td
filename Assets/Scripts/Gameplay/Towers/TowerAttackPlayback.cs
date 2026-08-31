namespace GemTD.Gameplay.Towers
{
    /// <summary>
    /// Steps through a 1..N fire-state list. Combat only says "attack"; each prefab supplies how many clips.
    /// </summary>
    public struct TowerAttackPlayback
    {
        public bool IsPlaying { get; private set; }
        public int StepIndex { get; private set; }

        /// <summary>Play an authored clip so it fills <paramref name="windowSeconds"/>.</summary>
        public static float ClipSpeed(float authoredLength, float windowSeconds)
        {
            if (authoredLength <= 0.01f || windowSeconds <= 0.01f)
                return 1f;
            return authoredLength / windowSeconds;
        }

        public static float ContactDelay(float interval, float strikeNormalized)
        {
            if (interval <= 0f)
                return 0f;
            return interval * Clamp01(strikeNormalized);
        }

        /// <summary>
        /// Seconds the clip at <paramref name="stepIndex"/> should occupy.
        /// One clip fills the whole interval. Two clips: draw until contact, then release.
        /// </summary>
        public static float ClipWindow(float interval, float strikeNormalized, int stepIndex, int fireStepCount)
        {
            if (interval <= 0f)
                return 0f;
            if (fireStepCount <= 1)
                return interval;

            var contact = ContactDelay(interval, strikeNormalized);
            if (stepIndex <= 0)
                return contact > 0.01f ? contact : interval;

            var remainder = interval - contact;
            return remainder > 0.01f ? remainder : 0.01f;
        }

        static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;
            if (value > 1f)
                return 1f;
            return value;
        }

        float _windupElapsed;

        public bool TryStart(int fireStepCount, out int playIndex)
        {
            if (fireStepCount <= 0)
            {
                IsPlaying = false;
                StepIndex = 0;
                _windupElapsed = 0f;
                playIndex = -1;
                return false;
            }

            IsPlaying = true;
            StepIndex = 0;
            _windupElapsed = 0f;
            playIndex = 0;
            return true;
        }

        /// <summary>Current fire clip finished. True = play <paramref name="playIndex"/> next. False = return to idle.</summary>
        public bool TryAdvance(int fireStepCount, out int playIndex)
        {
            playIndex = -1;
            if (!IsPlaying)
                return false;

            var next = StepIndex + 1;
            if (next >= fireStepCount)
            {
                IsPlaying = false;
                StepIndex = 0;
                _windupElapsed = 0f;
                return false;
            }

            StepIndex = next;
            playIndex = next;
            return true;
        }

        /// <summary>
        /// When <paramref name="windupSeconds"/> is &gt; 0 and there is a next fire clip, leave step 0 after that time.
        /// Single-clip attacks keep playing for the whole FireInterval; combat spawns at strikeNormalized.
        /// </summary>
        public bool TryTickWindup(float dt, float windupSeconds, int fireStepCount, out int playIndex)
        {
            playIndex = -1;
            if (!IsPlaying || windupSeconds <= 0f || StepIndex != 0 || fireStepCount <= 1)
                return false;

            _windupElapsed += dt;
            if (_windupElapsed < windupSeconds)
                return false;

            return TryAdvance(fireStepCount, out playIndex);
        }

        public void Stop()
        {
            IsPlaying = false;
            StepIndex = 0;
            _windupElapsed = 0f;
        }
    }
}
