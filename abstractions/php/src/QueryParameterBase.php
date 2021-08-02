<?php


namespace Microsoft\Kiota\Abstractions;


abstract class QueryParameterBase {

    /**
     * @param array<string,object>|null $target
     */
    public function addQueryParameters(?array &$target): void
    {
        if (is_null($target)) {
            throw new \InvalidArgumentException('$target');
        }
        $objectClassProperties = get_object_vars($this);

        $staticClassProperties = get_class_vars(get_class($this));
        // Static properties of the class are not included in the output for
        // get_object_vars($this). To solve this, I call get_class_vars which
        // includes static properties and then merge the results.
        $combinedProperties = array_merge($staticClassProperties, $objectClassProperties);

        foreach($combinedProperties as $classPropertyName => $classPropertyValue) {
            $target[$classPropertyName] = $classPropertyValue;
        }
    }

    public function getClass(): string {
        return get_class($this);
    }

}
