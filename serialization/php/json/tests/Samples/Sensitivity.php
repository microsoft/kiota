<?php

namespace Microsoft\Kiota\Serialization\Tests\Samples;

use Microsoft\Kiota\Abstractions\Enum;

class Sensitivity extends Enum {
    public const NORMAL = 'normal';
    public const PERSONAL = 'personal';
    public const PRIVATE = 'private';
    public const CONFIDENTIAL = 'confidential';
}
