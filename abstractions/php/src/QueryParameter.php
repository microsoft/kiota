<?php
/**
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.
 * Licensed under the MIT License.  See License in the project root
 * for license information.
 */


namespace Microsoft\Kiota\Abstractions;

/**
 * Class QueryParameter
 *
 * Attribute/annotation for query parameter class  properties
 *
 * @Annotation
 * @Target("PROPERTY")
 * @NamedArgumentConstructor
 * @package Microsoft\Kiota\Abstractions
 * @copyright 2022 Microsoft Corporation
 * @license https://opensource.org/licenses/MIT MIT License
 */
class QueryParameter
{
    /**
     * @var string
     */
    public string $name = "";

    public function __construct(string $name)
    {
        $this->name = $name;
    }
}