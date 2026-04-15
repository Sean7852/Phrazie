using Phrazie.Core.Enums;

namespace Phrazie.Core.Models;

public class HotkeyBinding
{
    public HotkeyAction Action  { get; set; }
    public string       KeyName { get; set; } = string.Empty;
}
