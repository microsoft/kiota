/** Defines a contract for models that can hold additional data besides the described properties. */
export interface AdditionalDataHolder {
  /**
   * Gets the additional data for this object that did not belong to the properties.
   * @return The additional data for this object.
   */
  additionalData: Map<string, unknown>;
}
