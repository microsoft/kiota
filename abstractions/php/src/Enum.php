<?php
/**
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.
 * Licensed under the MIT License.  See License in the project root
 * for license information.
 *
 * Enum File
 * PHP version 7
 *
 * @category  Library
 * @package   Microsoft.Graph
 * @copyright 2016 Microsoft Corporation
 * @license   https://opensource.org/licenses/MIT MIT License
 * @version   GIT: 0.1.0
 * @link      https://graph.microsoft.io/
 */
namespace Microsoft\Kiota\Abstractions;


use InvalidArgumentException;
use ReflectionException;

/**
 * Class Enum
 *
 * @category Library
 * @package  Microsoft.Graph
 * @license  https://opensource.org/licenses/MIT MIT License
 * @link     https://graph.microsoft.io/
 */
abstract class Enum
{
    /** @var array<string, array<string>> */
    private static array $constants = [];
    /**
     * The value of the enum
     *
     * @var string
     */
    private $_value;

    /**
     * Create a new enum
     *
     * @param string $value The value of the enum
     *
     * @throws InvalidArgumentException|ReflectionException if enum value is invalid
     */
    public function __construct(string $value)
    {
        $components = explode(',', $value);

        foreach ($components as $component) {
            if (!self::has($component)) {
                throw new InvalidArgumentException("Invalid enum value $value");
            }
        }
        $this->_value = $value;
    }

    /**
     * Check if the enum has the given value
     *
     * @param mixed $value
     * @return bool the enum has the value
     * @throws ReflectionException
     */
    public static function has($value): bool
    {
        return in_array($value, self::toArray(), true);
    }

    /**
     * Check if the enum is defined
     *
     * @param mixed $value the value of the enum
     *
     * @return bool True if the value is defined
     */
    public function is($value): bool
    {
        return $this->_value === $value;
    }

    /**
     * Create a new class for the enum in question
     *
     * @return mixed
     * @throws ReflectionException
     */
    public static function toArray()
    {
        $class = static::class;

        if (!(array_key_exists($class, self::$constants)))
        {
            $reflectionObj = new \ReflectionClass($class);
            self::$constants[$class] = $reflectionObj->getConstants();
        }
        return self::$constants[$class];
    }

    /**
     * Get the value of the enum
     *
     * @return mixed value of the enum
     */
    public function value()
    {
        return $this->_value;
    }
}