using System.Collections.Generic;
using Firebend.JsonPatch.Models;

namespace Firebend.JsonPatch.Interfaces;

public interface IJsonDiffDetector
{
    /// <summary>
    /// Compares two objects and returns a list of <see cref="JsonDiff"/> detailing the changes
    /// </summary>
    /// <param name="original">
    /// The original object before alterations.
    /// </param>
    /// <param name="modified">
    /// The current state of the object after alterations.
    /// </param>
    /// <returns>
    /// A list of <see cref="JsonDiff"/>
    /// </returns>
    List<JsonDiff> DetectChanges(object original, object modified);
}
