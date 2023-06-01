using System.Collections.Generic;

namespace Firebend.JsonPatch;

public interface IJsonDiffDetector
{
    List<JsonDiff> DetectChanges(object original, object modified);
}
