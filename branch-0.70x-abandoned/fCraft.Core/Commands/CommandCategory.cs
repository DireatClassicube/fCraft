﻿// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;

namespace fCraft {
    /// <summary> Command categories. A command may belong to more than one category.
    /// Use binary flag logic to test whether a command belongs to a particular category. </summary>
    [Flags]
    public enum CommandCategory {
        /// <summary> Default command category. Do not use it. </summary>
        None = 0,

        /// <summary> Building-related commands: drawing, binding, copy/paste. </summary>
        Building = 1,

        /// <summary> Chat-related commands: messaging, ignoring, muting, etc. </summary>
        Chat = 2,

        /// <summary> Information commands: server, world, zone, rank, and player infos. </summary>
        Info = 4,

        /// <summary> Moderation commands: kick, ban, rank, tp/bring, etc. </summary>
        Moderation = 8,

        /// <summary> Server maintenance commands: reloading configs, editing PlayerDB, importing data, etc. </summary>
        Maintenance = 16,

        /// <summary> World-related commands: joining, loading, renaming, etc. </summary>
        World = 32,

        /// <summary> Zone-related commands: creating, editing, testing, etc. </summary>
        Zone = 64,

        /// <summary> Commands that are only used for diagnostics and debugging. </summary>
        Debug = 128
    }
}
