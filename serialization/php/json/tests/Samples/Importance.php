<?php

namespace Microsoft\Kiota\Serialization\Tests\Samples;

use Microsoft\Kiota\Abstractions\Enum;

class Importance extends Enum {
    public const LOW = 'low';
    public const NORMAL = 'normal';
    public const HIGH = 'high';
}
