package services;

/**
 * This is a Enum class representing languages for kiota code generation
 */
public enum KiotaGenerationLanguage {
    CSharp(0),
    Java(1),
    TypeScript(2),
    Python(3),
    Go(4),
    Swift(5),
    Ruby(6),
    Shell(7);

     private final int value;

    KiotaGenerationLanguage(int value) {
        this.value = value;
    }

    public int getValue() {
        return value;
    }
}