using YARG.Core.Chart;
using YARG.Core.Game;

namespace YARG.Core.Engine.ProKeys
{
    public abstract class ProKeysEngine : BaseEngine<ProKeysNote, ProKeysEngineParameters,
        ProKeysStats, ProKeysEngineState>
    {
        protected ProKeysEngine(InstrumentDifficulty<ProKeysNote> chart, SyncTrack syncTrack,
            ProKeysEngineParameters engineParameters, EngineManager? engineManager, YargProfile yargProfile)
            : base(chart, syncTrack, engineParameters, false, engineManager, yargProfile)
        {
        }
    }
}