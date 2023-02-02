﻿using Server.Reawakened.Levels.Enums;

namespace Server.Reawakened.Levels.Extensions;

public static class GetJoinReason
{
    public static string GetJoinReasonError(this JoinReason reason) =>
        reason switch
        {
            JoinReason.Accepted => string.Empty,
            JoinReason.Full => "This room is currently full.",
            _ =>
                "You seem to have reached an error that shouldn't have happened! Please report this error to the developers."
        };
}
