<?php

namespace Microsoft\Kiota\Serialization\Tests\Samples;

use Microsoft\Kiota\Abstractions\Enum;

class MaritalStatus extends Enum {
    public const SINGLE = 'single';
    public const COMPLICATED = 'complicated';
    public const DIVORCED = 'divorced';
    public const MARRIED = 'married';
}