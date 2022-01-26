<?php

namespace Microsoft\Kiota\Serialization\Tests\Samples;

use Microsoft\Kiota\Abstractions\Enum;

class FollowupFlagStatus extends Enum {
    public const NOT_FLAGGED = 'notFlagged';
    public const COMPLETE = 'complete';
    public const FLAGGED = 'flagged';
}
