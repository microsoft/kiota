package services;

public enum LogLevel {
    TRACE(0),
    DEBUG(1),
    INFORMATION(2),
    WARNING(3),
    ERROR(4),
    CRITICAL(5),
    NONE(6);
    private final int value;
    LogLevel(int value) {
        this.value = value;
    }
    public int getValue() {
        return value;
    }
}