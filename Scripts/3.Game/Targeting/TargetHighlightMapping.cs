#nullable enable
using System.Collections.Generic;

/// <summary>Maps main-view <see cref="TargetDescriptor.HighlightKey"/> / <see cref="TargetDescriptor.TakeAllHighlightKeys"/> to a descriptor index while targeting.</summary>
public static class TargetHighlightMapping
{
	/// <returns>Descriptor index, or <c>null</c> if <paramref name="highlightKey"/> does not match any current target.</returns>
	public static int? TryGetDescriptorIndex(IReadOnlyList<TargetDescriptor> descriptors, string highlightKey)
	{
		if (string.IsNullOrEmpty(highlightKey))
			return null;
		for (var i = 0; i < descriptors.Count; i++)
		{
			var d = descriptors[i];
			if (d.HighlightKey == highlightKey)
				return i;
			foreach (var k in d.TakeAllHighlightKeys)
			{
				if (k == highlightKey)
					return i;
			}
		}

		return null;
	}
}
