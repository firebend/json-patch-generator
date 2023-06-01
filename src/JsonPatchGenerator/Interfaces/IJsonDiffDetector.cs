using System.Collections.Generic;
using Firebend.JsonPatch.Models;

namespace Firebend.JsonPatch.Interfaces;

public interface IJsonDiffDetector
{
    List<JsonDiff> DetectChanges(object original, object modified);
}
