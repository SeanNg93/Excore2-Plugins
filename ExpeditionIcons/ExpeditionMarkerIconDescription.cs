using System.Collections.Generic;
using ExileCore2.Shared.Enums;

namespace ExpeditionIcons;

public class ExpeditionMarkerIconDescription
{
    public IconPickerIndex IconPickerIndex { get; init; }
    public MapIconsIndex DefaultIcon { get; init; }
    public List<string> BaseEntityMetadataSubstrings { get; set; } = new List<string>();
    public bool IsWeightCustomizable { get; init; } = true;
}