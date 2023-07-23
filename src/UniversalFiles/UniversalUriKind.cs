using System;

namespace Etherna.UniversalFiles
{
    [Flags]
    public enum UniversalUriKind
    {
        None = 0,
        LocalAbsolute = 1,
        LocalRelative = 2,
        OnlineAbsolute = 4,
        OnlineRelative = 8,
        Absolute = LocalAbsolute | OnlineAbsolute,
        Relative = LocalRelative | OnlineRelative,
        Local = LocalAbsolute | LocalRelative,
        Online = OnlineAbsolute | OnlineRelative,
        All = Absolute | Relative,
    }
}
