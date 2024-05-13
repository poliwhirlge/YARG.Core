using YARG.Core.Chart;
using YARG.Core.Game;
using YARG.Core.Input;

namespace YARG.Core.Engine.ProKeys.Engines
{
    public class YargProKeysEngine : ProKeysEngine
    {
        public YargProKeysEngine(InstrumentDifficulty<ProKeysNote> chart, SyncTrack syncTrack, ProKeysEngineParameters engineParameters, EngineManager? engineManager,
            YargProfile yargProfile) : base(chart, syncTrack, engineParameters, engineManager, yargProfile)
        {
        }

        protected override void UpdateBot(double time)
        {
            throw new System.NotImplementedException();
        }

        protected override void MutateStateWithInput(GameInput gameInput)
        {
            throw new System.NotImplementedException();
        }

        protected override void UpdateHitLogic(double time)
        {
            throw new System.NotImplementedException();
        }

        protected override void CheckForNoteHit()
        {
            throw new System.NotImplementedException();
        }

        protected override bool CanNoteBeHit(ProKeysNote note)
        {
            throw new System.NotImplementedException();
        }

        protected override void AddScore(ProKeysNote note)
        {
            throw new System.NotImplementedException();
        }

        protected override int CalculateBaseScore()
        {
            throw new System.NotImplementedException();
        }
    }
}
